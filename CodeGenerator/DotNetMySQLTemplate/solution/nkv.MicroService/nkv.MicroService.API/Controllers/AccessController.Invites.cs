using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Controllers
{
    /// <summary>
    /// Invite links: an administrator picks a role (and tenant) and shares a link; the person opens it, chooses their own
    /// username and password, and joins with that role. Nobody handles anyone else's password.
    /// Links can be single-use or for up to N people, expire, be closed and reopened, and be replaced (old link stops).
    /// The link is "/join/&lt;id&gt;.&lt;code&gt;", the code an HMAC of a random nonce with the app's secret. Administrator roles
    /// can't be given by link (add administrators in Members).
    /// </summary>
    public partial class AccessController
    {
        private const string PublicInviteRoute = APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/public/invite";
        private readonly byte[] _secret;
        private readonly string _secretText;
        private string _actingOwner, _actingUser; // set while a person joins by link (no signed-in admin)

        private static bool _invitesReady;
        private static void EnsureInvites(DbConnection c)
        {
            if (_invitesReady) return;
            using (var cmd = Db.Command(Db.CreateTableIfMissing("member_invites", $@"invite_id {Db.AutoId},
                nonce VARCHAR(64) NOT NULL, role_id INT NOT NULL, tenant_set {Db.BoolType} NOT NULL DEFAULT 0, tenant_id INT NULL,
                label VARCHAR(200) NULL, max_uses INT NOT NULL DEFAULT 1, uses INT NOT NULL DEFAULT 0, expires_at DATETIME NULL,
                is_open {Db.BoolType} NOT NULL DEFAULT 1, owner_name VARCHAR(255) NOT NULL, created_by_user VARCHAR(255) NOT NULL,
                createdAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP, last_used_at DATETIME NULL, email VARCHAR(254) NULL"), c)) Db.Execute(cmd);
            // tables made before email invites: add the column
            try { using var probe = Db.Command("SELECT email FROM member_invites WHERE 1 = 0", c); probe.ExecuteNonQuery(); }
            catch (DbException) { using var add = Db.Command("ALTER TABLE member_invites ADD email VARCHAR(254) NULL", c); add.ExecuteNonQuery(); }
            _invitesReady = true;
        }

        private DbConnection OpenInvites()
        {
            var c = Open();
            EnsureInvites(c);
            return c;
        }

        private string InviteCode(long id, string nonce)
        {
            using var h = new HMACSHA256(_secret);
            return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes($"invite:{id}:{nonce}"))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private sealed class Invite
        {
            public long Id; public string Nonce, Label, Owner, CreatedBy, RoleName, Email; public int RoleId, MaxUses, Uses;
            public bool TenantSet, IsOpen; public int? TenantId; public DateTime? ExpiresAt;
        }

        private static Invite ReadInvite(DbDataReader r) => new()
        {
            Id = Convert.ToInt64(r["invite_id"]), Nonce = (string)r["nonce"], RoleId = Convert.ToInt32(r["role_id"]),
            TenantSet = Convert.ToInt32(r["tenant_set"]) == 1, TenantId = r["tenant_id"] is DBNull ? null : Convert.ToInt32(r["tenant_id"]),
            Label = r["label"] as string, MaxUses = Convert.ToInt32(r["max_uses"]), Uses = Convert.ToInt32(r["uses"]),
            ExpiresAt = r["expires_at"] is DateTime e ? DateTime.SpecifyKind(e, DateTimeKind.Utc) : null,
            IsOpen = Convert.ToInt32(r["is_open"]) == 1, Owner = (string)r["owner_name"], CreatedBy = (string)r["created_by_user"],
            RoleName = r["role_name"] as string, Email = r["email"] as string,
        };

        private const string InviteSelect = "SELECT i.*, r.role_name FROM member_invites i LEFT JOIN roles r ON r.role_id = i.role_id";

        private static Invite LoadInvite(DbConnection c, long id)
        {
            using var q = Db.Command(InviteSelect + " WHERE i.invite_id = @id", c);
            q.Parameters.AddWithValue("@id", id);
            using var r = q.ExecuteReader();
            return r.Read() ? ReadInvite(r) : null;
        }

        private object DescribeInvite(Invite i) => new
        {
            id = i.Id, role_id = i.RoleId, role_name = i.RoleName, tenant_id = i.TenantSet ? i.TenantId ?? 0 : (int?)null, label = i.Label,
            email = i.Email, max_uses = i.MaxUses, uses = i.Uses, expires_at = i.ExpiresAt, is_open = i.IsOpen, created_by = i.CreatedBy,
            url = $"/join/{i.Id}.{InviteCode(i.Id, i.Nonce)}",
        };

        private Invite MyInvite(DbConnection c, long id)
        {
            var i = LoadInvite(c, id);
            return i != null && i.Owner == Tenant ? i : null;
        }

        public class InviteInput
        {
            public int role_id { get; set; }
            /// <summary>Platform administrators with tenancy: tenant id, 0 = platform user, omitted = unassigned. Tenant admins: ignored.</summary>
            public int? tenant_id { get; set; }
            public string label { get; set; }
            public int? max_uses { get; set; }
            public DateTime? expires_at { get; set; }
            /// <summary>Email invites: one single-use link per address, emailed to them; opening it signs them in (no password).</summary>
            public List<string> emails { get; set; }
        }

        [HttpGet]
        [Route(Route + "/invites")]
        public ActionResult Invites()
        {
            if (!IsAdmin || _secret == null) return Forbid();
            using var c = OpenInvites();
            using var q = Db.Command(InviteSelect + " WHERE i.owner_name = @o ORDER BY i.invite_id DESC", c);
            q.Parameters.AddWithValue("@o", Tenant);
            var items = new List<object>();
            using (var r = q.ExecuteReader()) while (r.Read()) items.Add(DescribeInvite(ReadInvite(r)));
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", items));
        }

        [HttpPost]
        [Route(Route + "/invites")]
        public ActionResult CreateInvite(InviteInput input)
        {
            if (!IsAdmin || _secret == null) return Forbid();
            if (input == null || input.role_id == AdminRoleId)
                return BadRequest(new APIResponse(ResponseCode.ERROR, "Administrators can't be invited by link. Add them in Members."));
            using var c = OpenInvites();
            var roleName = Scalar<string>(c, "SELECT role_name FROM roles WHERE role_id = @r AND isActive = 1", ("@r", input.role_id));
            if (roleName == null) return BadRequest(new APIResponse(ResponseCode.ERROR, "Unknown role"));
            if (roleName.Equals("Public", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new APIResponse(ResponseCode.ERROR, "The Public role is for visitors without an account. Pick another role."));
            if (input.max_uses is < 1 or > 1000) return BadRequest(new APIResponse(ResponseCode.ERROR, "Number of people must be between 1 and 1000"));
            if (input.expires_at is { } exp && exp.ToUniversalTime() <= DateTime.UtcNow) return BadRequest(new APIResponse(ResponseCode.ERROR, "The expiry date is in the past"));
            bool tenantSet = false; int? tenantId = null;
            if (DataGuard.TenancyEnabled)
            {
                if (DataTenant != null) { tenantSet = true; tenantId = DataTenant; }
                else if (input.tenant_id.HasValue) { tenantSet = true; tenantId = input.tenant_id > 0 ? input.tenant_id : null; }
            }
            var label = string.IsNullOrWhiteSpace(input.label) ? null : input.label.Trim();
            if (label?.Length > 200) label = label[..200];
            long Insert(string email, int maxUses)
            {
                using var cmd = Db.Command(@"INSERT INTO member_invites (nonce, role_id, tenant_set, tenant_id, label, max_uses, expires_at, owner_name, created_by_user, email)
                    VALUES (@n, @r, @ts, @t, @l, @m, @e, @o, @by, @em)", c);
                cmd.Parameters.AddWithValue("@n", Convert.ToHexString(RandomNumberGenerator.GetBytes(24)));
                cmd.Parameters.AddWithValue("@r", input.role_id);
                cmd.Parameters.AddWithValue("@ts", tenantSet ? 1 : 0);
                cmd.Parameters.AddWithValue("@t", (object)tenantId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@l", (object)label ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@m", maxUses);
                cmd.Parameters.AddWithValue("@e", input.expires_at is { } e ? e.ToUniversalTime() : DBNull.Value);
                cmd.Parameters.AddWithValue("@o", Tenant);
                cmd.Parameters.AddWithValue("@by", Username);
                cmd.Parameters.AddWithValue("@em", (object)email ?? DBNull.Value);
                return Db.InsertReturningId(cmd);
            }
            if (input.emails is { Count: > 0 })
            {
                var emails = input.emails.Select(NormalEmail).Where(x => x.Length > 0).Distinct().ToList();
                if (emails.Count > 50) return BadRequest(new APIResponse(ResponseCode.ERROR, "Invite up to 50 people at a time"));
                var bad = emails.FirstOrDefault(x => x.Length > 254 || !EmailPattern.IsMatch(x));
                if (bad != null) return BadRequest(new APIResponse(ResponseCode.ERROR, $"{bad} isn't a valid email address"));
                EnsureEmailTables(c);
                var results = new List<object>();
                foreach (var email in emails)
                {
                    if (MemberByEmail(c, email) != null) { results.Add(new { email, sent = false, already = true }); continue; }
                    var inv = LoadInvite(c, Insert(email, 1));
                    var sent = NotificationMailer.SendInvite(email, Username, roleName, $"/join/{inv.Id}.{InviteCode(inv.Id, inv.Nonce)}", AppName);
                    results.Add(new { email, sent, already = false, invite = DescribeInvite(inv) });
                }
                return Ok(new APIResponse(ResponseCode.SUCCESS, "Invites created", results));
            }
            var id = Insert(null, input.max_uses ?? 1);
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Invite link created", DescribeInvite(LoadInvite(c, id))));
        }

        public class InviteUpdate { public bool? is_open { get; set; } public bool regenerate { get; set; } }

        [HttpPut]
        [Route(Route + "/invites/{id:long}")]
        public ActionResult UpdateInvite(long id, InviteUpdate u)
        {
            if (!IsAdmin || _secret == null) return Forbid();
            using var c = OpenInvites();
            var i = MyInvite(c, id);
            if (i == null || u == null) return NotFound(new APIResponse(ResponseCode.ERROR, "Invite not found"));
            Exec(c, "UPDATE member_invites SET is_open = @o, nonce = @n WHERE invite_id = @id",
                ("@o", (u.is_open ?? i.IsOpen) ? 1 : 0), ("@n", u.regenerate ? Convert.ToHexString(RandomNumberGenerator.GetBytes(24)) : i.Nonce), ("@id", id));
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Saved", DescribeInvite(LoadInvite(c, id))));
        }

        [HttpDelete]
        [Route(Route + "/invites/{id:long}")]
        public ActionResult DeleteInvite(long id)
        {
            if (!IsAdmin || _secret == null) return Forbid();
            using var c = OpenInvites();
            if (MyInvite(c, id) == null) return NotFound(new APIResponse(ResponseCode.ERROR, "Invite not found"));
            Exec(c, "DELETE FROM member_invites WHERE invite_id = @id", ("@id", id));
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Invite deleted"));
        }

        // ------------------------------------------------------------------ joining (no sign-in)

        private Invite UsableInvite(DbConnection c, string token, out ActionResult problem)
        {
            problem = null;
            Invite i = null;
            var parts = (token ?? "").Split('.');
            if (_secret != null && parts.Length == 2 && parts[1].Length <= 64 && long.TryParse(parts[0], out var id) && id > 0)
            {
                i = LoadInvite(c, id);
                if (i != null)
                {
                    var want = Encoding.ASCII.GetBytes(InviteCode(i.Id, i.Nonce));
                    var got = Encoding.ASCII.GetBytes(parts[1]);
                    if (want.Length != got.Length || !CryptographicOperations.FixedTimeEquals(want, got)) i = null;
                }
            }
            string msg = null; var status = 410;
            if (i == null) { msg = "This invite link isn't valid. Ask for a new one."; status = 404; }
            else if (!i.IsOpen) msg = "This invite link has been turned off. Ask for a new one.";
            else if (i.ExpiresAt is { } e && e <= DateTime.UtcNow) msg = "This invite link has expired. Ask for a new one.";
            else if (i.Uses >= i.MaxUses) msg = "This invite link has already been used. Ask for a new one.";
            else if (i.RoleId == AdminRoleId || i.RoleName == null || i.RoleName.Equals("Public", StringComparison.OrdinalIgnoreCase)) { msg = "This invite link isn't valid. Ask for a new one."; status = 404; }
            if (msg != null) { problem = StatusCode(status, new { message = msg }); return null; }
            return i;
        }

        [AllowAnonymous, EnableRateLimiting("public"), HttpGet, Route(PublicInviteRoute + "/{token}")]
        public ActionResult OpenInvite(string token)
        {
            using var c = OpenInvites();
            var i = UsableInvite(c, token, out var problem);
            if (problem != null) return problem;
            return Ok(new { role = i.RoleName, label = i.Label, invitedBy = i.CreatedBy, email = i.Email });
        }

        public class JoinInput { public string username { get; set; } public string password { get; set; } }

        [AllowAnonymous, EnableRateLimiting("public"), HttpPost, Route(PublicInviteRoute + "/{token}")]
        public ActionResult Join(string token, JoinInput input)
        {
            using (var peek = OpenInvites())
            {
                var inv = UsableInvite(peek, token, out var p0);
                if (p0 != null) return p0;
                if (inv.Email != null) return JoinByEmail(peek, inv);
            }
            var username = input?.username?.Trim();
            if (string.IsNullOrEmpty(username) || username.Length > 100 || !System.Text.RegularExpressions.Regex.IsMatch(username, @"^[A-Za-z0-9._@-]+$"))
                return BadRequest(new { message = "Choose a username with letters, digits, dots, dashes or @ (no spaces)." });
            if (AuthGuard.PasswordProblem(input.password, username) is string weak) return BadRequest(new { message = weak });
            using var c = OpenInvites();
            var i = UsableInvite(c, token, out var problem);
            if (problem != null) return problem;
            if (Scalar<long>(c, "SELECT COUNT(*) FROM users WHERE username = @u", ("@u", username)) > 0)
                return BadRequest(new { message = "That username is taken. Try another." });
            // take a place on the link first, atomically, so "number of people" holds under load
            using (var take = Db.Command($@"UPDATE member_invites SET uses = uses + 1, last_used_at = {Db.UtcNow}
                WHERE invite_id = @id AND is_open = 1 AND uses < max_uses", c))
            {
                take.Parameters.AddWithValue("@id", i.Id);
                if (take.ExecuteNonQuery() == 0) return StatusCode(410, new { message = "This invite link has already been used. Ask for a new one." });
            }
            _actingOwner = i.Owner; _actingUser = $"{i.CreatedBy} (invite)";
            using var tx = c.BeginTransaction();
            try
            {
                using var cmd = Db.Command(@"INSERT INTO users (username, password_hash, role_id, createdBy, modifiedBy, isActive)
                    VALUES (@u, @p, @r, @by, @by, 1)", c);
                cmd.Transaction = tx;
                cmd.Parameters.AddWithValue("@u", username);
                cmd.Parameters.AddWithValue("@p", PasswordHasher.Hash(input.password));
                cmd.Parameters.AddWithValue("@r", i.RoleId);
                cmd.Parameters.AddWithValue("@by", _actingUser);
                Db.Execute(cmd);
                var userId = (int)Db.LastInsertedId(cmd);
                tx.Commit();
                if (DataGuard.TenancyEnabled && i.TenantSet) DataGuard.SetTenantOfUser(c, userId, i.TenantId);
                WriteUserPermissions(c, userId, i.RoleId);
                ClearPermissionCache();
            }
            catch (DbException)
            {
                try { tx.Rollback(); } catch { /* already committed or gone */ }
                Exec(c, "UPDATE member_invites SET uses = uses - 1 WHERE invite_id = @id AND uses > 0", ("@id", i.Id));
                return BadRequest(new { message = "That username is taken. Try another." });
            }
            return Ok(new { message = "Welcome! Your account is ready." });
        }

        /// <summary>
        /// An emailed, single-use invite: the link reached that inbox, so opening it is the proof. The account is created with
        /// the email as its username and no usable password (they sign in later with an emailed code), and signed in now.
        /// </summary>
        private ActionResult JoinByEmail(DbConnection c, Invite i)
        {
            EnsureEmailTables(c);
            var email = i.Email;
            if (MemberByEmail(c, email) != null || Scalar<long>(c, "SELECT COUNT(*) FROM users WHERE username = @u", ("@u", email)) > 0)
                return StatusCode(409, new { message = "You're already a member. Sign in with your email instead." });
            using (var take = Db.Command($@"UPDATE member_invites SET uses = uses + 1, last_used_at = {Db.UtcNow}
                WHERE invite_id = @id AND is_open = 1 AND uses < max_uses", c))
            {
                take.Parameters.AddWithValue("@id", i.Id);
                if (take.ExecuteNonQuery() == 0) return StatusCode(410, new { message = "This invite link has already been used. Ask for a new one." });
            }
            _actingOwner = i.Owner; _actingUser = $"{i.CreatedBy} (invite)";
            int userId;
            try
            {
                using var cmd = Db.Command(@"INSERT INTO users (username, password_hash, role_id, createdBy, modifiedBy, isActive)
                    VALUES (@u, @p, @r, @by, @by, 1)", c);
                cmd.Parameters.AddWithValue("@u", email);
                // no password: a random one nobody knows; they sign in with emailed codes
                cmd.Parameters.AddWithValue("@p", PasswordHasher.Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
                cmd.Parameters.AddWithValue("@r", i.RoleId);
                cmd.Parameters.AddWithValue("@by", _actingUser);
                Db.Execute(cmd);
                userId = (int)Db.LastInsertedId(cmd);
            }
            catch (DbException)
            {
                Exec(c, "UPDATE member_invites SET uses = uses - 1 WHERE invite_id = @id AND uses > 0", ("@id", i.Id));
                return StatusCode(409, new { message = "You're already a member. Sign in with your email instead." });
            }
            SetEmail(c, userId, email, verified: true);
            if (DataGuard.TenancyEnabled && i.TenantSet) DataGuard.SetTenantOfUser(c, userId, i.TenantId);
            WriteUserPermissions(c, userId, i.RoleId);
            ClearPermissionCache();
            TokenController.IssueSession(this, _secretText, userId, email, i.RoleId, i.Owner);
            return Ok(new { message = "Welcome! You're in.", signedIn = true });
        }
    }
}
