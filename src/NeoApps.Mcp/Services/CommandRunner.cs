using System.Diagnostics;
using System.Text;

namespace NeoApps.Mcp.Services;

public sealed record CommandResult(int ExitCode, string Output)
{
    public bool Ok => ExitCode == 0;
    public string Tail(int lines = 40) => string.Join('\n', Output.Split('\n').TakeLast(lines)).Trim();
}

public static class CommandRunner
{
    /// <summary>Runs a process with an argument list (no shell), streaming each output line to <paramref name="onLine"/>.</summary>
    public static async Task<CommandResult> RunAsync(
        string fileName,
        IEnumerable<string> args,
        string workingDirectory,
        Action<string>? onLine = null,
        IDictionary<string, string>? env = null,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true, // never let a child read the MCP stdin
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment["DOTNET_NOLOGO"] = "1";
        if (env is not null)
            foreach (var (k, v) in env) psi.Environment[k] = v;

        var output = new StringBuilder();
        void Append(string? line)
        {
            if (line is null) return;
            lock (output) output.AppendLine(line);
            onLine?.Invoke(line);
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => Append(e.Data);
        process.ErrorDataReceived += (_, e) => Append(e.Data);

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new CommandResult(-1, $"Failed to start '{fileName}': {ex.Message}");
        }
        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout ?? TimeSpan.FromMinutes(20));
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* exited */ }
            Append(ct.IsCancellationRequested ? "Cancelled." : "Timed out.");
            return new CommandResult(-1, output.ToString());
        }
        return new CommandResult(process.ExitCode, output.ToString());
    }

    /// <summary>True when an executable is on PATH (or is an absolute path that exists).</summary>
    public static bool Exists(string fileName)
    {
        if (Path.IsPathRooted(fileName)) return File.Exists(fileName);
        var exts = OperatingSystem.IsWindows() ? new[] { ".exe", ".cmd", "" } : new[] { "" };
        return (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(dir => exts.Any(ext => File.Exists(Path.Combine(dir, fileName + ext))));
    }
}
