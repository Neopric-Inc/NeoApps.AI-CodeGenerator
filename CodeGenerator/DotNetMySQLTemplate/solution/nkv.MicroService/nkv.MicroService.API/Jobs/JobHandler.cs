using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace nkv.MicroService.API.Jobs
{
    /// <summary>
    /// A kind of background job. Subclass it anywhere in the API assembly, including the app's Custom/ folder
    /// (copied from workspace/&lt;App&gt;/custom-api/), and it is registered automatically. Jobs run on the worker
    /// (RabbitMQ queue), report live progress, can pause for a person's approval and resume afterwards, and every query
    /// they make is scoped to the user who started them.
    /// </summary>
    public abstract class JobHandler
    {
        /// <summary>Unique name, e.g. "ai.workflow" or "billing.monthly-invoices". Clients start a job with POST /Jobs {kind, input}.</summary>
        public abstract string Kind { get; }

        /// <summary>
        /// Runs inside the web request that starts the job, before it is queued: check permissions and input here and throw
        /// <see cref="JobException"/> to refuse with a message. Returns the job's title as people will see it.
        /// </summary>
        public virtual string Validate(JobUser user, JsonObject input) => Kind;

        /// <summary>
        /// Does the work on the worker. Report progress with <see cref="JobContext.Progress"/>. To pause for a person, call
        /// <see cref="JobContext.AwaitApproval"/> and return; <see cref="ResumeAsync"/> runs once they approve.
        /// Returning without calling Complete/AwaitApproval completes the job.
        /// </summary>
        public abstract Task RunAsync(JobContext ctx);

        /// <summary>Who may approve or reject the job when it pauses. Default: the person who started it, or an administrator.</summary>
        public virtual bool CanApprove(JobUser approver, JobRow job) => approver.UserId == job.UserId || approver.IsAdmin;

        /// <summary>Runs on the worker after a person approves. <paramref name="decision"/> is the body they sent (may be edited).</summary>
        public virtual Task ResumeAsync(JobContext ctx, JsonObject decision)
        {
            ctx.Complete(decision, "Approved.");
            return Task.CompletedTask;
        }
    }

    /// <summary>The person a job runs as.</summary>
    public sealed record JobUser(int UserId, string Username, int RoleId, string Owner)
    {
        public bool IsAdmin => RoleId == 1;
    }

    /// <summary>A message a person should see (bad input, missing permission). Other exceptions are logged and shown generically.</summary>
    public sealed class JobException : Exception
    {
        public JobException(string message) : base(message) { }
    }

    /// <summary>What a running job can do: report progress, keep state, pause for approval, finish.</summary>
    public sealed class JobContext
    {
        internal JobContext(JobStore store, string jobId, string kind, JobUser user, JsonObject input, JsonObject state,
            IServiceProvider services, CancellationToken cancel)
        {
            Store = store; JobId = jobId; Kind = kind; User = user; Input = input ?? new JsonObject(); State = state ?? new JsonObject();
            Services = services; Cancel = cancel;
        }

        internal JobStore Store { get; }
        internal bool Paused { get; private set; }
        internal bool Finished { get; private set; }

        public string JobId { get; }
        public string Kind { get; }
        public JobUser User { get; }
        /// <summary>The input the job was started with.</summary>
        public JsonObject Input { get; }
        /// <summary>Handler-owned state kept between <see cref="JobHandler.RunAsync"/> and <see cref="JobHandler.ResumeAsync"/>. Save with <see cref="SaveState"/>.</summary>
        public JsonObject State { get; }
        /// <summary>Services of this job's DI scope.</summary>
        public IServiceProvider Services { get; }
        /// <summary>Cancelled when someone cancels the job, it times out, or the app shuts down.</summary>
        public CancellationToken Cancel { get; }

        /// <summary>Adds a line to the job's live feed (and optionally a 0-100 progress value).</summary>
        public void Progress(string message, int? percent = null, JsonNode data = null) =>
            Store.AddEvent(JobId, "info", message, percent, data);

        public void Warn(string message) => Store.AddEvent(JobId, "warn", message, null, null);

        public void SaveState() => Store.SaveState(JobId, State);

        /// <summary>Pauses the job until a person approves or rejects <paramref name="proposal"/> (shown to them as the job's result).</summary>
        public void AwaitApproval(JsonNode proposal, string message)
        {
            Store.SaveState(JobId, State);
            if (Store.Transition(JobId, "running", "awaiting_approval", result: proposal))
                Store.AddEvent(JobId, "approval", message ?? "Waiting for approval.", null, null);
            Paused = true;
        }

        /// <summary>Finishes the job successfully with <paramref name="result"/>.</summary>
        public void Complete(JsonNode result, string message = null)
        {
            if (Store.Transition(JobId, "running", "completed", result: result, finished: true))
                Store.AddEvent(JobId, "done", message ?? "Done.", 100, null);
            Finished = true;
        }
    }
}
