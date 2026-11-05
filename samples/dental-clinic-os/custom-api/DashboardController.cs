using Microsoft.AspNetCore.Mvc;
using NeoApps.API.Controllers;

namespace DentalClinic.API.Custom;

/// <summary>
/// Custom API endpoints for Dental Clinic.
/// Accessible at /v1/api/Custom/[controller]/[action]
/// </summary>
[Route("v1/api/Custom/[controller]")]
[ApiController]
public class DashboardController : NeoAppsController
{
    /// <summary>
    /// Get today's dashboard statistics
    /// GET /v1/api/Custom/Dashboard/TodayStats
    /// </summary>
    [HttpGet("TodayStats")]
    public async Task<IActionResult> TodayStats()
    {
        try
        {
            var cmd = Db.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    COUNT(*) as total_appointments,
                    SUM(CASE WHEN appointment_type = 'cleaning' OR appointment_type = 'checkup' THEN 1 ELSE 0 END) as hygiene_count,
                    SUM(CASE WHEN appointment_type IN ('crown', 'root_canal', 'extraction', 'filling') THEN 1 ELSE 0 END) as treatment_count,
                    SUM(CASE WHEN status = 'confirmed' THEN 1 ELSE 0 END) as confirmed_count,
                    SUM(CASE WHEN status = 'completed' THEN 1 ELSE 0 END) as completed_count
                FROM appointments 
                WHERE DATE(appointment_date) = CURDATE() 
                  AND isActive = 1";
            DataGuard.Apply(cmd);

            var result = await cmd.ReadSingleAsync();
            return Ok(new
            {
                total_appointments = result?["total_appointments"]?.GetValue<int>() ?? 0,
                hygiene_appointments = result?["hygiene_count"]?.GetValue<int>() ?? 0,
                treatment_appointments = result?["treatment_count"]?.GetValue<int>() ?? 0,
                confirmed = result?["confirmed_count"]?.GetValue<int>() ?? 0,
                completed = result?["completed_count"]?.GetValue<int>() ?? 0,
                date = DateTime.Today.ToString("yyyy-MM-dd")
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get outstanding balance summary
    /// GET /v1/api/Custom/Dashboard/OutstandingBalance
    /// </summary>
    [HttpGet("OutstandingBalance")]
    public async Task<IActionResult> OutstandingBalance()
    {
        try
        {
            var cmd = Db.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    COUNT(*) as overdue_count,
                    SUM(balance_due) as total_overdue,
                    SUM(CASE WHEN status = 'sent' THEN balance_due ELSE 0 END) as sent_balance,
                    SUM(CASE WHEN status = 'partial' THEN balance_due ELSE 0 END) as partial_balance
                FROM invoices 
                WHERE status IN ('sent', 'partial', 'overdue') 
                  AND isActive = 1";
            DataGuard.Apply(cmd);

            var result = await cmd.ReadSingleAsync();
            return Ok(new
            {
                overdue_invoices = result?["overdue_count"]?.GetValue<int>() ?? 0,
                total_overdue = result?["total_overdue"]?.GetValue<decimal>() ?? 0,
                sent_balance = result?["sent_balance"]?.GetValue<decimal>() ?? 0,
                partial_balance = result?["partial_balance"]?.GetValue<decimal>() ?? 0
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get treatment production summary for date range
    /// GET /v1/api/Custom/Dashboard/Production?from=2026-01-01&to=2026-12-31
    /// </summary>
    [HttpGet("Production")]
    public async Task<IActionResult> Production([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        try
        {
            from ??= DateTime.Today.AddMonths(-1);
            to ??= DateTime.Today;

            var cmd = Db.CreateCommand();
            cmd.CommandText = @"
                SELECT 
                    COUNT(*) as total_treatments,
                    SUM(fee) as total_fees,
                    SUM(insurance_covered) as total_insurance,
                    SUM(patient_pay) as total_patient,
                    SUM(CASE WHEN status = 'completed' THEN 1 ELSE 0 END) as completed_count,
                    SUM(CASE WHEN status = 'completed' THEN fee ELSE 0 END) as completed_fees
                FROM treatments 
                WHERE DATE(createdAt) BETWEEN @from AND @to 
                  AND isActive = 1";
            cmd.Parameters.AddWithValue("@from", from.Value.Date);
            cmd.Parameters.AddWithValue("@to", to.Value.Date);
            DataGuard.Apply(cmd);

            var result = await cmd.ReadSingleAsync();
            return Ok(new
            {
                period = new { from = from.Value.ToString("yyyy-MM-dd"), to = to.Value.ToString("yyyy-MM-dd") },
                total_treatments = result?["total_treatments"]?.GetValue<int>() ?? 0,
                total_fees = result?["total_fees"]?.GetValue<decimal>() ?? 0,
                total_insurance = result?["total_insurance"]?.GetValue<decimal>() ?? 0,
                total_patient = result?["total_patient"]?.GetValue<decimal>() ?? 0,
                completed_treatments = result?["completed_count"]?.GetValue<int>() ?? 0,
                completed_fees = result?["completed_fees"]?.GetValue<decimal>() ?? 0
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}