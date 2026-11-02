using System;
using System.Diagnostics;
using System.Text;

namespace NoCodeAppGenerator.Infrastructure;

public readonly record struct ProcessResult(int ExitCode, string Output);

/// <summary>
/// Runs a child process and drains stdout/stderr concurrently. The previous code called
/// WaitForExit() before BeginOutputReadLine(), which deadlocks once the pipe buffer fills.
/// </summary>
public static class ProcessRunner
{
    public static ProcessResult Run(string fileName, string arguments, string workingDirectory, TimeSpan? timeout = null)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";

        var output = new StringBuilder();
        using var process = new Process { StartInfo = psi };
        process.OutputDataReceived += (_, e) => { if (e.Data != null) lock (output) output.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (output) output.AppendLine(e.Data); };

        Console.WriteLine($"> {fileName} {arguments}  (in {workingDirectory})");
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(timeout ?? TimeSpan.FromMinutes(10)))
        {
            try { process.Kill(entireProcessTree: true); } catch { /* already gone */ }
            return new ProcessResult(-1, output + $"{Environment.NewLine}Timed out.");
        }
        process.WaitForExit(); // flush async readers
        return new ProcessResult(process.ExitCode, output.ToString());
    }
}
