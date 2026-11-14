using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Controllers
{
    /// <summary>
    /// The signed-in person's notifications (the bell): list, unread count, mark read, and their email setting.
    /// Rows are written by DataGuard from ui.config.json "notifications"; each person only ever sees their own.
    /// </summary>
    [Authorize]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private const string Route = APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/Notifications";

        private int UserId => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        private static MySqlConnection Open()
        {
            var c = new MySqlConnection(DataGuard.ConnectionString);
            c.Open();
            return c;
        }

        [HttpGet]
        [Route(Route)]
        public ActionResult List(int limit = 20)
        {
            if (UserId == 0) return Unauthorized();
            using var c = Open();
            var items = new List<object>();
            using (var q = new MySqlCommand(@"SELECT notification_id, title, table_name, record_id, actor, createdAt, read_at FROM user_notifications
                WHERE user_id = @u ORDER BY notification_id DESC LIMIT @n", c))
            {
                q.Parameters.AddWithValue("@u", UserId);
                q.Parameters.AddWithValue("@n", Math.Clamp(limit, 1, 100));
                using var r = q.ExecuteReader();
                while (r.Read())
                    items.Add(new
                    {
                        id = r.GetInt64(0), title = r.GetString(1), table = r.IsDBNull(2) ? null : r.GetString(2), record = r.IsDBNull(3) ? null : r.GetString(3),
                        actor = r.IsDBNull(4) ? null : r.GetString(4), createdAt = DateTime.SpecifyKind(r.GetDateTime(5), DateTimeKind.Utc), read = !r.IsDBNull(6),
                    });
            }
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", new { unread = Unread(c), items }));
        }

        [HttpGet]
        [Route(Route + "/unread")]
        public ActionResult UnreadCount()
        {
            if (UserId == 0) return Unauthorized();
            using var c = Open();
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", new { unread = Unread(c) }));
        }

        private long Unread(MySqlConnection c)
        {
            using var q = new MySqlCommand("SELECT COUNT(*) FROM user_notifications WHERE user_id = @u AND read_at IS NULL", c);
            q.Parameters.AddWithValue("@u", UserId);
            return Convert.ToInt64(q.ExecuteScalar());
        }

        [HttpPost]
        [Route(Route + "/{id}/read")]
        public ActionResult MarkRead(long id) => Mark("AND notification_id = @id", id);

        [HttpPost]
        [Route(Route + "/read-all")]
        public ActionResult MarkAllRead() => Mark("", 0);

        private ActionResult Mark(string filter, long id)
        {
            if (UserId == 0) return Unauthorized();
            using var c = Open();
            using var q = new MySqlCommand($"UPDATE user_notifications SET read_at = UTC_TIMESTAMP() WHERE user_id = @u AND read_at IS NULL {filter}", c);
            q.Parameters.AddWithValue("@u", UserId);
            q.Parameters.AddWithValue("@id", id);
            q.ExecuteNonQuery();
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", new { unread = Unread(c) }));
        }

        public class SettingsInput
        {
            public string email { get; set; }
            public bool emailOn { get; set; }
        }

        [HttpGet]
        [Route(Route + "/settings")]
        public ActionResult GetSettings()
        {
            if (UserId == 0) return Unauthorized();
            using var c = Open();
            using var q = new MySqlCommand("SELECT email, email_on FROM user_notification_settings WHERE user_id = @u", c);
            q.Parameters.AddWithValue("@u", UserId);
            using var r = q.ExecuteReader();
            var found = r.Read();
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", new
            {
                email = found && !r.IsDBNull(0) ? r.GetString(0) : null,
                emailOn = found && Convert.ToInt32(r.GetValue(1)) == 1,
                emailAvailable = NotificationMailer.Enabled,
            }));
        }

        [HttpPut]
        [Route(Route + "/settings")]
        public ActionResult SaveSettings(SettingsInput input)
        {
            if (UserId == 0) return Unauthorized();
            var email = input?.email?.Trim();
            if (!string.IsNullOrEmpty(email) && (email.Length > 255 || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")))
                return BadRequest(new APIResponse(ResponseCode.ERROR, "Enter a valid email address."));
            using var c = Open();
            using var q = new MySqlCommand(@"INSERT INTO user_notification_settings (user_id, email, email_on) VALUES (@u, @e, @on)
                ON DUPLICATE KEY UPDATE email = @e, email_on = @on", c);
            q.Parameters.AddWithValue("@u", UserId);
            q.Parameters.AddWithValue("@e", string.IsNullOrEmpty(email) ? null : email);
            q.Parameters.AddWithValue("@on", input?.emailOn == true && !string.IsNullOrEmpty(email) ? 1 : 0);
            q.ExecuteNonQuery();
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Saved"));
        }
    }
}
