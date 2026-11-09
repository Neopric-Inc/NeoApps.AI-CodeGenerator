using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using nkv.MicroService.Utility;
using StackExchange.Redis;

namespace nkv.MicroService.API.Controllers
{
    /// <summary>
    /// Access management for the generated UI: who am I and what may I do, team members, roles and their
    /// per-table permissions, and the UI configuration designed in the app.
    ///
    /// RBAC model (unchanged from the generator): permissionmatrix rows per (role, user, entity), scoped to a
    /// tenant via owner_name. Rows with user_id NULL are the role's template; member rows are copied from it.
    /// </summary>
    [Authorize]
    [ApiController]
    public class AccessController : ControllerBase
    {
        private const string Route = APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/Access";
        private const int AdminRoleId = 1;
        private const string UiConfigType = "neoapps-ui-config";
        private readonly string _connectionString;
        private readonly IConnectionMultiplexer _redis;

        public AccessController(IConfiguration configuration, IConnectionMultiplexer redis)
        {
            _connectionString = configuration.GetConnectionString("MySQLDatabase");
            _redis = redis;
        }

        private string Username => User.FindFirst(ClaimTypes.Name)?.Value;
        private int UserId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var v) ? v : 0;
        private int RoleId => int.TryParse(User.FindFirst(ClaimTypes.Role)?.Value, out var v) ? v : 0;
        private string Tenant => User.FindFirst(ClaimTypes.Actor)?.Value ?? Username;
        private bool IsAdmin => RoleId == AdminRoleId;
        /// <summary>Data tenant of the caller from the signed token (DataGuard tenancy); null = platform user.</summary>
        private int? DataTenant => DataGuard.Current?.Tenant;
        /// <summary>Role templates and the UI config are shared by every tenant, so only platform administrators change them.</summary>
        private bool IsPlatformAdmin => IsAdmin && DataTenant == null;

        // ------------------------------------------------------------------ me

        [HttpGet]
        [Route(Route + "/me")]
        public ActionResult Me()
        {
            using var conn = Open();
            var permissions = new Dictionary<string, object>();
            using (var cmd = new MySqlCommand(@"SELECT e.entity_name, p.can_read, p.can_write, p.can_update, p.can_delete
                FROM permissionmatrix p JOIN entities e ON e.entity_id = p.entity_id
                WHERE p.user_id = @uid AND p.isActive = 1", conn))
            {
                cmd.Parameters.AddWithValue("@uid", UserId);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    permissions[r.GetString(0).ToLowerInvariant()] = new
                    {
                        read = Flag(r, 1), create = Flag(r, 2), update = Flag(r, 3), delete = Flag(r, 4)
                    };
            }
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", new
            {
                user_id = UserId,
                username = Username,
                role_id = RoleId,
                role_name = Scalar<string>(conn, "SELECT role_name FROM roles WHERE role_id = @id", ("@id", RoleId)),
                tenant = Tenant,
                tenant_id = DataTenant,
                tenant_assigned = DataGuard.Current?.Unassigned != true,
                is_platform = DataTenant == null && DataGuard.Current?.Unassigned != true,
                tenancy = DataGuard.TenancyEnabled,
                tenant_table = DataGuard.TenantTable,
                is_admin = IsAdmin,
                permissions,
                hidden_fields = DataGuard.HiddenFieldMap() // per-role hidden columns (never sent by the API)
            }));
        }

        // ------------------------------------------------------------------ roles

        [HttpGet]
        [Route(Route + "/roles")]
        public ActionResult Roles()
        {
            if (!IsAdmin) return Forbid();
            using var conn = Open();
            var roles = new List<object>();
            using var cmd = new MySqlCommand(@"SELECT r.role_id, r.role_name,
                    (SELECT COUNT(DISTINCT p.user_id) FROM permissionmatrix p WHERE p.role_id = r.role_id AND p.owner_name = @t AND p.user_id IS NOT NULL) AS members
                FROM roles r WHERE r.isActive = 1 ORDER BY r.role_id", conn);
            cmd.Parameters.AddWithValue("@t", Tenant);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                roles.Add(new { role_id = r.GetInt32(0), role_name = r.GetString(1), members = r.GetInt32(2), is_admin = r.GetInt32(0) == AdminRoleId });
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", roles));
        }

        public class RoleInput { public string role_name { get; set; } }

        [HttpPost]
        [Route(Route + "/roles")]
        public ActionResult CreateRole(RoleInput input)
        {
            if (!IsPlatformAdmin) return Forbid();
            if (string.IsNullOrWhiteSpace(input?.role_name)) return BadRequest(new APIResponse(ResponseCode.ERROR, "role_name is required"));
            using var conn = Open();
            using var cmd = new MySqlCommand("INSERT INTO roles (role_name, createdBy, modifiedBy, isActive) VALUES (@n, @u, @u, 1)", conn);
            cmd.Parameters.AddWithValue("@n", input.role_name.Trim());
            cmd.Parameters.AddWithValue("@u", Username);
            cmd.ExecuteNonQuery();
            var id = (int)cmd.LastInsertedId;
            // New roles start read-only on every table.
            SaveRoleTemplate(conn, id, Entities(conn).Select(e => new PermissionInput { entity = e.Value, read = true }).ToList());
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Role created", new { role_id = id }));
        }

        public class PermissionInput
        {
            public string entity { get; set; }
            public bool read { get; set; }
            public bool create { get; set; }
            public bool update { get; set; }
            public bool delete { get; set; }
        }

        [HttpGet]
        [Route(Route + "/roles/{roleId}/permissions")]
        public ActionResult RolePermissions(int roleId)
        {
            if (!IsAdmin) return Forbid();
            using var conn = Open();
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", RoleTemplate(conn, roleId)));
        }

        [HttpPut]
        [Route(Route + "/roles/{roleId}/permissions")]
        public ActionResult SaveRolePermissions(int roleId, List<PermissionInput> input)
        {
            if (!IsPlatformAdmin) return Forbid();
            if (roleId == AdminRoleId) return BadRequest(new APIResponse(ResponseCode.ERROR, "The Administrator role always has full access."));
            using var conn = Open();
            SaveRoleTemplate(conn, roleId, input ?? new List<PermissionInput>());
            // Apply to every member of this role in the tenant.
            foreach (var userId in MembersOfRole(conn, roleId))
                WriteUserPermissions(conn, userId, roleId);
            ClearPermissionCache();
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Permissions saved"));
        }

        // ------------------------------------------------------------------ members

        [HttpGet]
        [Route(Route + "/members")]
        public ActionResult Members()
        {
            if (!IsAdmin) return Forbid();
            using var conn = Open();
            var list = new List<object>();
            using var cmd = new MySqlCommand(@"SELECT u.user_id, u.username, u.role_id, r.role_name, u.isActive, u.createdAt, ut.tenant_id, ut.user_id IS NOT NULL AS assigned
                FROM users u LEFT JOIN roles r ON r.role_id = u.role_id LEFT JOIN user_tenants ut ON ut.user_id = u.user_id
                WHERE u.user_id IN (SELECT DISTINCT p.user_id FROM permissionmatrix p WHERE p.owner_name = @t AND p.user_id IS NOT NULL)
                  AND (@dt IS NULL OR ut.tenant_id = @dt)
                ORDER BY u.user_id", conn);
            cmd.Parameters.AddWithValue("@t", Tenant);
            cmd.Parameters.AddWithValue("@dt", (object)DataTenant ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                list.Add(new
                {
                    user_id = r.GetInt32(0), username = r.GetString(1), role_id = r.GetInt32(2),
                    role_name = r.IsDBNull(3) ? null : r.GetString(3), isActive = Convert.ToInt32(r.GetValue(4)),
                    createdAt = r.IsDBNull(5) ? (DateTime?)null : r.GetDateTime(5), is_you = r.GetInt32(0) == UserId,
                    tenant_id = r.IsDBNull(6) ? (int?)null : r.GetInt32(6),
                    // no mapping: only an Administrator is a platform user; anyone else has no data access until assigned
                    tenant_assigned = Convert.ToInt32(r.GetValue(7)) == 1 || r.GetInt32(2) == AdminRoleId
                });
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", list));
        }

        public class MemberInput
        {
            public string username { get; set; }
            public string password { get; set; }
            public int role_id { get; set; }
            public int? isActive { get; set; }
            /// <summary>Data tenant of the member. Ignored for tenant administrators (members always join their tenant).
            /// Platform administrators: a tenant id, 0 = platform user, omitted = unassigned (no data access). Omit on update to keep it.</summary>
            public int? tenant_id { get; set; }
        }

        [HttpPost]
        [Route(Route + "/members")]
        public ActionResult AddMember(MemberInput input)
        {
            if (!IsAdmin) return Forbid();
            if (string.IsNullOrWhiteSpace(input?.username)) return BadRequest(new APIResponse(ResponseCode.ERROR, "username is required"));
            if (AuthGuard.PasswordProblem(input.password, input.username) is string weak) return BadRequest(new APIResponse(ResponseCode.ERROR, weak));
            using var conn = Open();
            if (Scalar<long>(conn, "SELECT COUNT(*) FROM users WHERE username = @u", ("@u", input.username.Trim())) > 0)
                return BadRequest(new APIResponse(ResponseCode.ERROR, "That username is taken"));
            using var cmd = new MySqlCommand(@"INSERT INTO users (username, password_hash, role_id, createdBy, modifiedBy, isActive)
                VALUES (@u, @p, @r, @by, @by, 1)", conn);
            cmd.Parameters.AddWithValue("@u", input.username.Trim());
            cmd.Parameters.AddWithValue("@p", PasswordHasher.Hash(input.password));
            cmd.Parameters.AddWithValue("@r", input.role_id);
            cmd.Parameters.AddWithValue("@by", Username);
            cmd.ExecuteNonQuery();
            var userId = (int)cmd.LastInsertedId;
            // tenancy: tenant admins add members to their own tenant; platform admins choose (0 = platform user, omitted = unassigned).
            // Without tenancy nothing is stored, so enabling it later never turns existing members into platform users.
            if (DataGuard.TenancyEnabled)
            {
                if (DataTenant != null) DataGuard.SetTenantOfUser(conn, userId, DataTenant);
                else if (input.tenant_id.HasValue) DataGuard.SetTenantOfUser(conn, userId, input.tenant_id > 0 ? input.tenant_id : null);
            }
            WriteUserPermissions(conn, userId, input.role_id);
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Member added", new { user_id = userId }));
        }

        public class AssignTenantInput
        {
            public int tenant_id { get; set; }
            /// <summary>Members to assign; omit to assign every member that has no tenant yet (upgrading an existing app).</summary>
            public List<int> user_ids { get; set; }
        }

        [HttpPost]
        [Route(Route + "/members/assign-tenant")]
        public ActionResult AssignTenant(AssignTenantInput input)
        {
            if (!IsPlatformAdmin || !DataGuard.TenancyEnabled) return Forbid();
            using var conn = Open();
            if (DataGuard.TenantTable != null && DataGuard.TenantTablePk != null &&
                Scalar<long>(conn, $"SELECT COUNT(*) FROM `{DataGuard.TenantTable}` WHERE `{DataGuard.TenantTablePk}` = @t AND isActive = 1", ("@t", input.tenant_id)) == 0)
                return BadRequest(new APIResponse(ResponseCode.ERROR, "Unknown tenant"));
            var members = MembersOfTenant(conn);
            List<int> targets;
            if (input.user_ids != null && input.user_ids.Count > 0) targets = input.user_ids.Where(members.Contains).ToList();
            else
            {
                targets = new List<int>();
                using var cmd = new MySqlCommand(@"SELECT u.user_id FROM users u LEFT JOIN user_tenants ut ON ut.user_id = u.user_id
                    WHERE ut.user_id IS NULL AND u.role_id <> @admin", conn);
                cmd.Parameters.AddWithValue("@admin", AdminRoleId);
                using var r = cmd.ExecuteReader();
                while (r.Read()) if (members.Contains(r.GetInt32(0))) targets.Add(r.GetInt32(0));
            }
            foreach (var id in targets) DataGuard.SetTenantOfUser(conn, id, input.tenant_id);
            return Ok(new APIResponse(ResponseCode.SUCCESS, $"{targets.Count} member(s) assigned", new { assigned = targets }));
        }

        [HttpPut]
        [Route(Route + "/members/{userId}")]
        public ActionResult UpdateMember(int userId, MemberInput input)
        {
            if (!IsAdmin) return Forbid();
            if (userId == UserId && input.role_id != AdminRoleId) return BadRequest(new APIResponse(ResponseCode.ERROR, "You can't remove your own admin role."));
            using var conn = Open();
            if (!MembersOfTenant(conn).Contains(userId)) return NotFound(new APIResponse(ResponseCode.ERROR, "Member not found"));
            var memberName = Scalar<string>(conn, "SELECT username FROM users WHERE user_id = @id", ("@id", userId));
            if (!string.IsNullOrWhiteSpace(input.password) && AuthGuard.PasswordProblem(input.password, memberName) is string weak)
                return BadRequest(new APIResponse(ResponseCode.ERROR, weak));
            var before = (role: Scalar<int>(conn, "SELECT role_id FROM users WHERE user_id = @id", ("@id", userId)),
                          active: Convert.ToInt32(Scalar<object>(conn, "SELECT isActive FROM users WHERE user_id = @id", ("@id", userId))));
            var sets = new List<string> { "role_id = @r", "modifiedBy = @by", "modifiedAt = NOW()" };
            if (input.isActive.HasValue) sets.Add("isActive = @a");
            if (!string.IsNullOrWhiteSpace(input.password)) sets.Add("password_hash = @p");
            using (var cmd = new MySqlCommand($"UPDATE users SET {string.Join(", ", sets)} WHERE user_id = @id", conn))
            {
                cmd.Parameters.AddWithValue("@r", input.role_id);
                cmd.Parameters.AddWithValue("@by", Username);
                cmd.Parameters.AddWithValue("@a", input.isActive ?? 1);
                cmd.Parameters.AddWithValue("@p", string.IsNullOrWhiteSpace(input.password) ? "" : PasswordHasher.Hash(input.password));
                cmd.Parameters.AddWithValue("@id", userId);
                cmd.ExecuteNonQuery();
            }
            if (DataGuard.TenancyEnabled && DataTenant == null && input.tenant_id.HasValue)
                DataGuard.SetTenantOfUser(conn, userId, input.tenant_id > 0 ? input.tenant_id : null);
            WriteUserPermissions(conn, userId, input.role_id, active: input.isActive ?? 1);
            ClearPermissionCache();
            // new password, role or deactivation: end the member's existing sessions
            var pwChanged = !string.IsNullOrWhiteSpace(input.password);
            if (pwChanged || before.role != input.role_id || (input.isActive.HasValue && input.isActive.Value != before.active))
                AuthGuard.RevokeTokens(userId, passwordChanged: pwChanged);
            if (pwChanged) AuthGuard.Unlock(memberName);
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Member updated"));
        }

        /// <summary>Signs the caller out everywhere (all of their tokens stop working).</summary>
        [HttpPost]
        [Route(Route + "/logout")]
        public ActionResult Logout()
        {
            AuthGuard.RevokeTokens(UserId);
            SessionCookie.Clear(Response);
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Signed out"));
        }

        public class PasswordChangeInput { public string current_password { get; set; } public string new_password { get; set; } }

        /// <summary>Change your own password (needs the current one); other sessions end.</summary>
        [HttpPut]
        [Route(Route + "/me/password")]
        public ActionResult ChangeOwnPassword(PasswordChangeInput input)
        {
            using var conn = Open();
            var hash = Scalar<string>(conn, "SELECT password_hash FROM users WHERE user_id = @id AND isActive = 1", ("@id", UserId));
            if (hash == null || !PasswordHasher.Verify(input?.current_password ?? "", hash))
                return BadRequest(new APIResponse(ResponseCode.ERROR, "The current password is wrong."));
            if (AuthGuard.PasswordProblem(input.new_password, Username) is string weak) return BadRequest(new APIResponse(ResponseCode.ERROR, weak));
            Exec(conn, "UPDATE users SET password_hash = @p, modifiedBy = @u, modifiedAt = NOW() WHERE user_id = @id",
                ("@p", PasswordHasher.Hash(input.new_password)), ("@u", Username), ("@id", UserId));
            AuthGuard.RevokeTokens(UserId, passwordChanged: true);
            SessionCookie.Clear(Response);
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Password changed. Sign in again."));
        }

        /// <summary>Administrators: end all sessions of a member, or clear a sign-in lockout.</summary>
        [HttpPost]
        [Route(Route + "/members/{userId}/sign-out")]
        public ActionResult SignOutMember(int userId)
        {
            if (!IsAdmin) return Forbid();
            using var conn = Open();
            if (!MembersOfTenant(conn).Contains(userId)) return NotFound(new APIResponse(ResponseCode.ERROR, "Member not found"));
            AuthGuard.RevokeTokens(userId);
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Member signed out everywhere"));
        }

        [HttpPost]
        [Route(Route + "/members/{userId}/unlock")]
        public ActionResult UnlockMember(int userId)
        {
            if (!IsAdmin) return Forbid();
            using var conn = Open();
            if (!MembersOfTenant(conn).Contains(userId)) return NotFound(new APIResponse(ResponseCode.ERROR, "Member not found"));
            AuthGuard.Unlock(Scalar<string>(conn, "SELECT username FROM users WHERE user_id = @id", ("@id", userId)));
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Sign-in lockout cleared"));
        }

        // ------------------------------------------------------------------ UI configuration

        [HttpGet]
        [Route(Route + "/ui-config")]
        public ActionResult GetUiConfig()
        {
            using var conn = Open();
            var json = Scalar<string>(conn, @"SELECT layout FROM dnd_ui_versions WHERE dnd_ui_type = @t AND createdBy = @o AND isActive = 1
                ORDER BY dnd_ui_version_id DESC LIMIT 1", ("@t", UiConfigType), ("@o", Truncate(Tenant)));
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", json == null ? null : JsonDocument.Parse(json).RootElement));
        }

        [HttpPut]
        [Route(Route + "/ui-config")]
        public ActionResult SaveUiConfig([FromBody] JsonElement config)
        {
            if (!IsPlatformAdmin) return Forbid();
            using var conn = Open();
            Exec(conn, "UPDATE dnd_ui_versions SET isActive = 0 WHERE dnd_ui_type = @t AND createdBy = @o", ("@t", UiConfigType), ("@o", Truncate(Tenant)));
            Exec(conn, @"INSERT INTO dnd_ui_versions (layout, components, ui_pages, dnd_ui_type, createdBy, modifiedBy, isActive)
                VALUES (@l, '[]', '[]', @t, @o, @u, 1)", ("@l", config.GetRawText()), ("@t", UiConfigType), ("@o", Truncate(Tenant)), ("@u", Truncate(Username)));
            return Ok(new APIResponse(ResponseCode.SUCCESS, "UI configuration saved"));
        }

        // ------------------------------------------------------------------ helpers

        private MySqlConnection Open()
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        private static bool Flag(MySqlDataReader r, int i) => !r.IsDBNull(i) && Convert.ToInt32(r.GetValue(i)) == 1;
        private static string Truncate(string s) => s == null ? "" : s.Length <= 45 ? s : s.Substring(0, 45);

        private static T Scalar<T>(MySqlConnection conn, string sql, params (string, object)[] ps)
        {
            using var cmd = new MySqlCommand(sql, conn);
            foreach (var (k, v) in ps) cmd.Parameters.AddWithValue(k, v);
            var o = cmd.ExecuteScalar();
            return o == null || o is DBNull ? default : (T)Convert.ChangeType(o, typeof(T));
        }

        private static void Exec(MySqlConnection conn, string sql, params (string, object)[] ps)
        {
            using var cmd = new MySqlCommand(sql, conn);
            foreach (var (k, v) in ps) cmd.Parameters.AddWithValue(k, v);
            cmd.ExecuteNonQuery();
        }

        private static Dictionary<int, string> Entities(MySqlConnection conn)
        {
            var map = new Dictionary<int, string>();
            using var cmd = new MySqlCommand("SELECT entity_id, entity_name FROM entities WHERE isActive = 1", conn);
            using var r = cmd.ExecuteReader();
            while (r.Read()) map[r.GetInt32(0)] = r.GetString(1).ToLowerInvariant();
            return map;
        }

        private List<int> MembersOfTenant(MySqlConnection conn)
        {
            var ids = new List<int>();
            using var cmd = new MySqlCommand(@"SELECT DISTINCT p.user_id FROM permissionmatrix p LEFT JOIN user_tenants ut ON ut.user_id = p.user_id
                WHERE p.owner_name = @t AND p.user_id IS NOT NULL AND (@dt IS NULL OR ut.tenant_id = @dt)", conn);
            cmd.Parameters.AddWithValue("@t", Tenant);
            cmd.Parameters.AddWithValue("@dt", (object)DataTenant ?? DBNull.Value);
            using var r = cmd.ExecuteReader();
            while (r.Read()) ids.Add(r.GetInt32(0));
            return ids;
        }

        private List<int> MembersOfRole(MySqlConnection conn, int roleId)
        {
            var ids = new List<int>();
            using var cmd = new MySqlCommand(@"SELECT u.user_id FROM users u WHERE u.role_id = @r
                AND u.user_id IN (SELECT DISTINCT user_id FROM permissionmatrix WHERE owner_name = @t AND user_id IS NOT NULL)", conn);
            cmd.Parameters.AddWithValue("@r", roleId);
            cmd.Parameters.AddWithValue("@t", Tenant);
            using var r = cmd.ExecuteReader();
            while (r.Read()) ids.Add(r.GetInt32(0));
            return ids;
        }

        /// <summary>Role template rows (user_id NULL) for this tenant; Administrator is always full access.</summary>
        private List<PermissionInput> RoleTemplate(MySqlConnection conn, int roleId)
        {
            var entities = Entities(conn);
            var rows = new Dictionary<int, PermissionInput>();
            if (roleId != AdminRoleId)
            {
                using var cmd = new MySqlCommand(@"SELECT entity_id, can_read, can_write, can_update, can_delete FROM permissionmatrix
                    WHERE role_id = @r AND owner_name = @t AND user_id IS NULL AND isActive = 1", conn);
                cmd.Parameters.AddWithValue("@r", roleId);
                cmd.Parameters.AddWithValue("@t", Tenant);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                    rows[r.GetInt32(0)] = new PermissionInput { read = Flag(r, 1), create = Flag(r, 2), update = Flag(r, 3), delete = Flag(r, 4) };
            }
            return entities.OrderBy(e => e.Value).Select(e =>
            {
                var full = roleId == AdminRoleId;
                rows.TryGetValue(e.Key, out var p);
                return new PermissionInput
                {
                    entity = e.Value,
                    read = full || (p?.read ?? false), create = full || (p?.create ?? false),
                    update = full || (p?.update ?? false), delete = full || (p?.delete ?? false)
                };
            }).ToList();
        }

        private void SaveRoleTemplate(MySqlConnection conn, int roleId, List<PermissionInput> input)
        {
            var byName = Entities(conn).ToDictionary(e => e.Value, e => e.Key);
            Exec(conn, "DELETE FROM permissionmatrix WHERE role_id = @r AND owner_name = @t AND user_id IS NULL", ("@r", roleId), ("@t", Tenant));
            foreach (var p in input)
            {
                if (p?.entity == null || !byName.TryGetValue(p.entity.ToLowerInvariant(), out var entityId)) continue;
                Exec(conn, @"INSERT INTO permissionmatrix (role_id, entity_id, can_read, can_write, can_update, can_delete, user_id, owner_name, createdBy, modifiedBy, isActive)
                    VALUES (@r, @e, @cr, @cw, @cu, @cd, NULL, @t, @u, @u, 1)",
                    ("@r", roleId), ("@e", entityId), ("@cr", p.read ? 1 : 0), ("@cw", p.create ? 1 : 0),
                    ("@cu", p.update ? 1 : 0), ("@cd", p.delete ? 1 : 0), ("@t", Tenant), ("@u", Username));
            }
        }

        /// <summary>Replaces a member's permission rows with the role template.</summary>
        private void WriteUserPermissions(MySqlConnection conn, int userId, int roleId, int active = 1)
        {
            var template = RoleTemplate(conn, roleId);
            var byName = Entities(conn).ToDictionary(e => e.Value, e => e.Key);
            Exec(conn, "DELETE FROM permissionmatrix WHERE user_id = @id AND owner_name = @t", ("@id", userId), ("@t", Tenant));
            foreach (var p in template)
            {
                Exec(conn, @"INSERT INTO permissionmatrix (role_id, entity_id, can_read, can_write, can_update, can_delete, user_id, owner_name, createdBy, modifiedBy, isActive)
                    VALUES (@r, @e, @cr, @cw, @cu, @cd, @id, @t, @u, @u, @a)",
                    ("@r", roleId), ("@e", byName[p.entity]), ("@cr", p.read ? 1 : 0), ("@cw", p.create ? 1 : 0),
                    ("@cu", p.update ? 1 : 0), ("@cd", p.delete ? 1 : 0), ("@id", userId), ("@t", Tenant), ("@u", Username), ("@a", active));
            }
        }

        /// <summary>CheckPermissionAttribute caches decisions in Redis for 30 minutes; drop this tenant's entries.</summary>
        private void ClearPermissionCache()
        {
            try
            {
                foreach (var endpoint in _redis.GetEndPoints())
                {
                    var server = _redis.GetServer(endpoint);
                    var keys = server.Keys(pattern: $"*:{Tenant}:*").ToArray();
                    if (keys.Length > 0) _redis.GetDatabase().KeyDelete(keys);
                }
            }
            catch (Exception)
            {
                // Cache entries expire on their own; never fail the request because of Redis.
            }
        }
    }
}
