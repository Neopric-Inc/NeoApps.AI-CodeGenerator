// neoapps-migrator <up|baseline|status|down:VERSION> --connection "<MySQL connection string>"
using NeoApps.Migrations;

var mode = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "status";
var i = Array.IndexOf(args, "--connection");
var connection = i >= 0 && i + 1 < args.Length ? args[i + 1] : Environment.GetEnvironmentVariable("NEOAPPS_MIGRATE_CONNECTION");
if (string.IsNullOrWhiteSpace(connection))
{
    Console.Error.WriteLine("usage: neoapps-migrator <up|baseline|status|down:VERSION> --connection \"server=...;database=...\"");
    return 2;
}
try
{
    return MigrationHost.Run(connection, typeof(Program).Assembly, mode);
}
catch (Exception ex)
{
    Console.Error.WriteLine("migration failed: " + (ex.InnerException?.Message ?? ex.Message));
    return 1;
}
