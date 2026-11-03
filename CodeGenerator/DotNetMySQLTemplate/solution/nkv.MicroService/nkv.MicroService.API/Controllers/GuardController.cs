using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Controllers
{
    /// <summary>Data rules in force (tenancy, lifecycles, read-only data, audit, outbox, jobs) and operational actions for administrators.</summary>
    [Authorize]
    [ApiController]
    public class GuardController : ControllerBase
    {
        private const string Route = APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/Guard";
        private bool IsPlatformAdmin => User.FindFirst(ClaimTypes.Role)?.Value == "1" && DataGuard.Current?.Tenant == null;

        [HttpGet, Route(Route + "/rules")]
        public ActionResult Rules()
        {
            var summary = DataGuard.RulesSummary();
            summary["outboxStats"] = new JsonObject { ["published"] = GuardJobs.Published, ["failed"] = GuardJobs.Failed };
            summary["jobs"] = GuardJobs.JobsSummary();
            return Ok(new APIResponse(ResponseCode.SUCCESS, "OK", summary));
        }

        [HttpPost, Route(Route + "/outbox/publish")]
        public ActionResult PublishNow()
        {
            if (!IsPlatformAdmin) return Forbid();
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Published", new { sent = GuardJobs.PublishOutbox() }));
        }

        [HttpPost, Route(Route + "/jobs/run")]
        public ActionResult RunJobs()
        {
            if (!IsPlatformAdmin) return Forbid();
            return Ok(new APIResponse(ResponseCode.SUCCESS, "Jobs run", GuardJobs.RunScheduledNow()));
        }
    }
}
