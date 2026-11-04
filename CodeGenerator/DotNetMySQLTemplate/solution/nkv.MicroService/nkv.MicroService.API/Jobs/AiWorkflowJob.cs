using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using nkv.MicroService.API.Ai;

namespace nkv.MicroService.API.Jobs
{
    /// <summary>
    /// "ai.workflow": runs an AI workflow from ui.config.json in the background. Claude reads the source and proposes rows
    /// (live feed shows each step), the job pauses with the proposal, and once a person approves (possibly with rows
    /// removed) the rows are created in one transaction as "&lt;user&gt; via AI".
    /// Input: {"workflow": "&lt;id&gt;", "recordId": "...", "note": "..."}. Approve with {"records": [...]}.
    /// </summary>
    public sealed class AiWorkflowJob : JobHandler
    {
        private readonly AiService _ai;

        public AiWorkflowJob(AiService ai) => _ai = ai;

        public override string Kind => "ai.workflow";

        private static AiUser AsAiUser(JobUser u) => new(u.UserId, u.Username, u.RoleId, u.Owner);

        public override string Validate(JobUser user, JsonObject input)
        {
            var workflow = input["workflow"]?.ToString();
            if (string.IsNullOrEmpty(workflow)) throw new JobException("Say which workflow to run (input.workflow).");
            var label = _ai.CheckWorkflow(AsAiUser(user), workflow, input["recordId"]?.ToString());
            return input["recordId"] is { } r ? $"{label} (record {r})" : label;
        }

        public override async Task RunAsync(JobContext ctx)
        {
            var proposal = await _ai.PreviewWorkflow(AsAiUser(ctx.User), ctx.Input["workflow"]!.ToString(), ctx.Input["recordId"]?.ToString(),
                ctx.Input["note"]?.ToString(), ctx.Cancel, m => ctx.Progress(m));
            var rows = (proposal["records"] as JsonArray)?.Count ?? 0;
            foreach (var w in (proposal["warnings"] as JsonArray ?? new JsonArray()).Select(w => w?.ToString()).Where(w => w != null)) ctx.Warn(w);
            if (rows == 0) { ctx.Complete(proposal, "Nothing to create."); return; }
            ctx.AwaitApproval(proposal, $"Proposal ready: {rows} row{(rows == 1 ? "" : "s")}. Review and approve to create them.");
        }

        public override Task ResumeAsync(JobContext ctx, JsonObject decision)
        {
            var records = decision["records"] as JsonArray ?? throw new JobException("Approve with the rows to create (decision.records).");
            ctx.Progress($"Creating {records.Count} row{(records.Count == 1 ? "" : "s")}");
            var result = _ai.ApplyWorkflow(AsAiUser(ctx.User), ctx.Input["workflow"]!.ToString(), ctx.Input["recordId"]?.ToString(), records);
            ctx.Complete(result, result["summary"]?.ToString());
            return Task.CompletedTask;
        }
    }
}
