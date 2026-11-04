using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using nkv.MicroService.API.Ai;
using nkv.MicroService.API.Jobs;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Controllers
{
    /// <summary>
    /// Background jobs: start one (POST /Jobs {kind, input}), follow its live feed (GET /Jobs/{id}/events, long-poll),
    /// approve or reject it when it pauses for a person, cancel it, and list recent jobs.
    /// </summary>
    [Authorize]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private const string Route = APIEndpoint.Version + "/" + APIEndpoint.APIRoute + "/Jobs";
        private readonly JobStore _store;
        private readonly JobQueue _queue;
        private readonly JobSignals _signals;
        private readonly IEnumerable<JobHandler> _handlers;
        private readonly ILogger<JobsController> _log;

        public JobsController(JobStore store, JobQueue queue, JobSignals signals, IEnumerable<JobHandler> handlers, ILogger<JobsController> log)
        {
            _store = store; _queue = queue; _signals = signals; _handlers = handlers; _log = log;
        }

        private JobUser Me()
        {
            var name = User.FindFirst(ClaimTypes.Name)?.Value;
            return new JobUser(
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0,
                name,
                int.TryParse(User.FindFirst(ClaimTypes.Role)?.Value, out var r) ? r : 0,
                User.FindFirst(ClaimTypes.Actor)?.Value ?? name);
        }

        private static int? Tenant() => DataGuard.Current?.Tenant;

        private JobHandler Handler(string kind) => _handlers.FirstOrDefault(h => string.Equals(h.Kind, kind, StringComparison.OrdinalIgnoreCase));

        /// <summary>The job if this user may see it (their own; admins: anyone's in their workspace and tenant), else null.</summary>
        private JobRow Visible(string id)
        {
            var me = Me();
            var job = string.IsNullOrEmpty(id) ? null : _store.Get(id);
            if (job == null || job.Owner != me.Owner || job.TenantId != Tenant()) return null;
            return job.UserId == me.UserId || me.IsAdmin ? job : null;
        }

        private ActionResult Run(Func<object> action)
        {
            try { return Ok(action()); }
            catch (Exception ex) when (ex is JobException or AiException) { return BadRequest(new { message = ex.Message }); }
            catch (DataGuardException gx) { return StatusCode(gx.StatusCode, gx.Body); }
            catch (Exception ex)
            {
                _log.LogError(ex, "Jobs request failed");
                return StatusCode(500, new { message = "Something went wrong. Reference: " + HttpContext.TraceIdentifier });
            }
        }

        public sealed class StartRequest
        {
            public string Kind { get; set; }
            public JsonObject Input { get; set; }
        }

        [HttpPost, Route(Route)]
        public ActionResult Start([FromBody] StartRequest req) => Run(() =>
        {
            var handler = Handler(req?.Kind) ?? throw new JobException($"Unknown job kind '{req?.Kind}'.");
            var me = Me();
            var input = req.Input ?? new JsonObject();
            var title = handler.Validate(me, input);
            var id = _store.Create(handler.Kind, title, me, Tenant(), input);
            _queue.Enqueue(id);
            return _store.Get(id).ToJson(withResult: false);
        });

        [HttpGet, Route(Route)]
        public ActionResult List([FromQuery] string status, [FromQuery] int limit = 50) =>
            Run(() => _store.List(Me(), Tenant(), status, limit).Select(j => j.ToJson(withResult: false)).ToList());

        [HttpGet, Route(Route + "/{id}")]
        public ActionResult Get(string id) => Run(() => (object)Visible(id)?.ToJson(withResult: true) ?? throw new JobException("Job not found."));

        /// <summary>
        /// Events after <paramref name="after"/> (an event id). With none yet, waits up to <paramref name="wait"/> seconds (max 25)
        /// for the next one, so clients see progress the moment it happens without hammering the API.
        /// </summary>
        [HttpGet, Route(Route + "/{id}/events")]
        public async Task<ActionResult> Events(string id, [FromQuery] long after = 0, [FromQuery] int wait = 20, CancellationToken ct = default)
        {
            try
            {
                using var listener = _signals.Listen(id);
                var job = Visible(id);
                if (job == null) return BadRequest(new { message = "Job not found." });
                var events = _store.Events(id, after);
                if (events.Count == 0 && !job.IsTerminal && wait > 0)
                {
                    await listener.WaitAsync(TimeSpan.FromSeconds(Math.Min(wait, 25)), ct);
                    job = _store.Get(id);
                    events = _store.Events(id, after);
                }
                return Ok(new JsonObject
                {
                    ["job"] = job.ToJson(withResult: job.Status is "awaiting_approval" or "completed"),
                    ["events"] = new JsonArray(events.Select(e => (JsonNode)e).ToArray()),
                });
            }
            catch (OperationCanceledException) { return StatusCode(499); }
        }

        public sealed class DecisionRequest
        {
            public JsonObject Decision { get; set; }
            public string Reason { get; set; }
        }

        [HttpPost, Route(Route + "/{id}/approve")]
        public ActionResult Approve(string id, [FromBody] DecisionRequest req) => Run(() =>
        {
            var job = Visible(id) ?? throw new JobException("Job not found.");
            var handler = Handler(job.Kind) ?? throw new JobException("This kind of job is no longer available.");
            if (!handler.CanApprove(Me(), job)) throw new JobException("You can't approve this job.");
            if (!_store.Transition(id, "awaiting_approval", "approved", decision: req?.Decision ?? new JsonObject()))
                throw new JobException("This job isn't waiting for approval.");
            _store.AddEvent(id, "info", $"Approved by {Me().Username}.", null, null);
            _queue.Enqueue(id);
            return _store.Get(id).ToJson(withResult: false);
        });

        [HttpPost, Route(Route + "/{id}/reject")]
        public ActionResult Reject(string id, [FromBody] DecisionRequest req) => Run(() =>
        {
            var job = Visible(id) ?? throw new JobException("Job not found.");
            var handler = Handler(job.Kind);
            if (handler != null && !handler.CanApprove(Me(), job)) throw new JobException("You can't reject this job.");
            if (!_store.Transition(id, "awaiting_approval", "rejected", finished: true, error: string.IsNullOrWhiteSpace(req?.Reason) ? null : req.Reason))
                throw new JobException("This job isn't waiting for approval.");
            _store.AddEvent(id, "warn", $"Rejected by {Me().Username}{(string.IsNullOrWhiteSpace(req?.Reason) ? "" : ": " + req.Reason)}.", null, null);
            return _store.Get(id).ToJson(withResult: false);
        });

        [HttpPost, Route(Route + "/{id}/cancel")]
        public ActionResult Cancel(string id) => Run(() =>
        {
            var job = Visible(id) ?? throw new JobException("Job not found.");
            if (job.IsTerminal) throw new JobException("This job has already finished.");
            if (!_store.Transition(id, job.Status, "cancelled", finished: true)) throw new JobException("This job just changed; try again.");
            _store.AddEvent(id, "warn", $"Cancelled by {Me().Username}.", null, null);
            _signals.RequestCancel(id); // a running job stops at its next step
            return _store.Get(id).ToJson(withResult: false);
        });
    }
}
