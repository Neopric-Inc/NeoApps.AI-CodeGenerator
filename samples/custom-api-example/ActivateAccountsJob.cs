using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using nkv.MicroService.API.Jobs;
using nkv.MicroService.Utility;

namespace nkv.MicroService.API.Custom
{
    /// <summary>
    /// Example background job for an app with an `accounts` table that has a `status` column (e.g. samples/billflow).
    /// Copy this file to workspace/&lt;App&gt;/custom-api/ and regenerate: it finds accounts still onboarding (reporting
    /// progress), pauses for approval, then marks the approved ones active. Start it with
    /// POST /v1/api/Jobs {"kind": "billing.activate-accounts"} or from a custom screen with startJob(...).
    /// Every query runs as the user who started the job, so it only sees and changes what they could.
    /// </summary>
    public sealed class ActivateAccountsJob : JobHandler
    {
        public override string Kind => "billing.activate-accounts";

        public override string Validate(JobUser user, JsonObject input) => "Activate onboarding accounts";

        public override async Task RunAsync(JobContext ctx)
        {
            var cs = ctx.Services.GetRequiredService<IConfiguration>().GetConnectionString("MySQLDatabase");
            using var c = new MySqlConnection(cs);
            c.Open();
            using var q = new MySqlCommand("SELECT account_id, company_name FROM accounts WHERE status='onboarding' AND isActive=1", c);
            var found = new JsonArray();
            using (var r = DataGuard.Apply(q).ExecuteReader())
                while (r.Read()) found.Add(new JsonObject { ["account_id"] = r.GetInt32(0), ["company_name"] = r.GetString(1) });
            for (var i = 0; i < found.Count; i++)
            {
                ctx.Cancel.ThrowIfCancellationRequested();
                ctx.Progress($"Checked {found[i]!["company_name"]}", (i + 1) * 100 / found.Count);
                await Task.Delay(300, ctx.Cancel);
            }
            if (found.Count == 0) { ctx.Complete(new JsonObject { ["accounts"] = found }, "No accounts are onboarding."); return; }
            ctx.AwaitApproval(new JsonObject { ["accounts"] = found }, $"{found.Count} account(s) ready to activate.");
        }

        public override Task ResumeAsync(JobContext ctx, JsonObject decision)
        {
            // decision = the proposal as approved (the generic job screen sends it back unchanged; a custom screen may trim it)
            var ids = (decision["accounts"] as JsonArray ?? new JsonArray()).Select(a => a!["account_id"]!.GetValue<int>()).ToList();
            var cs = ctx.Services.GetRequiredService<IConfiguration>().GetConnectionString("MySQLDatabase");
            using var c = new MySqlConnection(cs);
            c.Open();
            var n = 0;
            foreach (var id in ids)
            {
                using var u = new MySqlCommand("UPDATE accounts SET status='active' WHERE account_id=@id", c);
                u.Parameters.AddWithValue("@id", id);
                n += DataGuard.NonQuery(u);
            }
            ctx.Complete(new JsonObject { ["activated"] = n }, $"Activated {n} account(s).");
            return Task.CompletedTask;
        }
    }
}
