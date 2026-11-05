using NeoApps.Jobs;
using System.Text.Json.Nodes;

namespace DentalClinic.API.Custom;

/// <summary>
/// Custom background job: Generate and submit insurance claims for completed treatments.
/// Trigger: POST /v1/api/Jobs {"kind": "billing.insurance-claims", "input": {}}
/// </summary>
public sealed class InsuranceClaimsJob : JobHandler
{
    public override string Kind => "billing.insurance-claims";

    public override string Validate(JobUser user, JsonObject input)
        => "Generate and submit insurance claims for completed treatments without existing claims";

    public override async Task RunAsync(JobContext ctx)
    {
        ctx.Progress("Finding completed treatments without claims", 10);

        var cmd = ctx.Db.CreateCommand();
        cmd.CommandText = @"
            SELECT 
                t.treatment_id,
                t.patient_id,
                t.treatment_code,
                t.treatment_name,
                t.tooth_numbers,
                t.fee,
                t.insurance_covered,
                t.patient_pay,
                p.insurance_provider,
                p.insurance_member_id,
                p.insurance_group_number,
                a.appointment_id,
                a.appointment_date
            FROM treatments t
            JOIN patients p ON t.patient_id = p.patient_id
            JOIN appointments a ON t.appointment_id = a.appointment_id
            WHERE t.status = 'completed' 
              AND t.isActive = 1
              AND NOT EXISTS (
                  SELECT 1 FROM insurance_claims ic 
                  WHERE ic.treatment_id = t.treatment_id AND ic.isActive = 1
              )
              AND p.insurance_provider IS NOT NULL
              AND p.insurance_provider != ''";
        DataGuard.Apply(cmd);

        var treatments = await cmd.ReadAllAsync();

        if (treatments.Count == 0)
        {
            ctx.Complete(new { submitted = 0 }, "No completed treatments need claims.");
            return;
        }

        ctx.Progress($"Found {treatments.Count} treatments to process", 30);

        // Build proposals for approval
        var proposals = treatments.Select(t => new
        {
            treatment_id = t.treatment_id,
            patient_id = t.patient_id,
            appointment_id = t.appointment_id,
            treatment_code = t.treatment_code,
            treatment_name = t.treatment_name,
            tooth_numbers = t.tooth_numbers,
            fee = t.fee,
            insurance_covered = t.insurance_covered,
            patient_pay = t.patient_pay,
            insurance_provider = t.insurance_provider,
            insurance_member_id = t.insurance_member_id,
            insurance_group_number = t.insurance_group_number,
            appointment_date = t.appointment_date,
            claim_status = "ready_to_submit"
        }).ToList();

        ctx.AwaitApproval(proposals, $"{proposals.Count} insurance claims ready. Review and approve to create claim records.");
    }

    public override Task ResumeAsync(JobContext ctx, JsonObject decision)
    {
        var approved = decision["approved"]?.GetValue<int>() ?? 0;
        
        if (approved == 0)
        {
            ctx.Complete(new { submitted = 0, message: "No claims approved" }, "Claims submission cancelled.");
            return Task.CompletedTask;
        }

        ctx.Progress("Creating insurance claim records", 50);

        var claims = decision["records"] as JsonArray;
        if (claims == null || claims.Count == 0)
        {
            ctx.Complete(new { submitted = 0, message: "No claim records in decision" }, "Failed: missing records");
            return Task.CompletedTask;
        }

        var created = 0;
        foreach (var claim in claims)
        {
            try
            {
                var c = claim as JsonObject;
                if (c == null) continue;

                var insertCmd = ctx.Db.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO insurance_claims (
                        treatment_id, patient_id, appointment_id,
                        claim_number, claim_date, claim_status,
                        treatment_code, treatment_name, tooth_numbers,
                        fee_billed, insurance_covered, patient_responsibility,
                        insurance_provider, insurance_member_id, insurance_group_number,
                        service_date, notes, createdBy, modifiedBy, createdAt, modifiedAt, isActive
                    ) VALUES (
                        @treatment_id, @patient_id, @appointment_id,
                        @claim_number, @claim_date, @claim_status,
                        @treatment_code, @treatment_name, @tooth_numbers,
                        @fee_billed, @insurance_covered, @patient_responsibility,
                        @insurance_provider, @insurance_member_id, @insurance_group_number,
                        @service_date, @notes, @createdBy, @modifiedBy, NOW(), NOW(), 1
                    )";

                var claimNumber = $"CLM-{DateTime.Now:yyyyMMdd}-{created + 1:D4}";
                
                insertCmd.Parameters.AddWithValue("@treatment_id", c["treatment_id"]?.GetValue<int>() ?? 0);
                insertCmd.Parameters.AddWithValue("@patient_id", c["patient_id"]?.GetValue<int>() ?? 0);
                insertCmd.Parameters.AddWithValue("@appointment_id", c["appointment_id"]?.GetValue<int>() ?? 0);
                insertCmd.Parameters.AddWithValue("@claim_number", claimNumber);
                insertCmd.Parameters.AddWithValue("@claim_date", DateTime.Now);
                insertCmd.Parameters.AddWithValue("@claim_status", "submitted");
                insertCmd.Parameters.AddWithValue("@treatment_code", c["treatment_code"]?.GetValue<string>() ?? "");
                insertCmd.Parameters.AddWithValue("@treatment_name", c["treatment_name"]?.GetValue<string>() ?? "");
                insertCmd.Parameters.AddWithValue("@tooth_numbers", c["tooth_numbers"]?.GetValue<string>() ?? "");
                insertCmd.Parameters.AddWithValue("@fee_billed", c["fee"]?.GetValue<decimal>() ?? 0);
                insertCmd.Parameters.AddWithValue("@insurance_covered", c["insurance_covered"]?.GetValue<decimal>() ?? 0);
                insertCmd.Parameters.AddWithValue("@patient_responsibility", c["patient_pay"]?.GetValue<decimal>() ?? 0);
                insertCmd.Parameters.AddWithValue("@insurance_provider", c["insurance_provider"]?.GetValue<string>() ?? "");
                insertCmd.Parameters.AddWithValue("@insurance_member_id", c["insurance_member_id"]?.GetValue<string>() ?? "");
                insertCmd.Parameters.AddWithValue("@insurance_group_number", c["insurance_group_number"]?.GetValue<string>() ?? "");
                insertCmd.Parameters.AddWithValue("@service_date", c["appointment_date"]?.GetValue<DateTime>() ?? DateTime.Now);
                insertCmd.Parameters.AddWithValue("@notes", $"Auto-created via insurance claims job. Submitted by {ctx.User.Actor}");
                insertCmd.Parameters.AddWithValue("@createdBy", ctx.User.Actor);
                insertCmd.Parameters.AddWithValue("@modifiedBy", ctx.User.Actor);

                DataGuard.Apply(insertCmd);
                await insertCmd.ExecuteNonQueryAsync();
                created++;
            }
            catch (Exception ex)
            {
                ctx.Progress($"Error creating claim: {ex.Message}", 90);
            }
        }

        ctx.Complete(new { submitted = created }, $"Successfully created {created} insurance claim records.");
        return Task.CompletedTask;
    }
}