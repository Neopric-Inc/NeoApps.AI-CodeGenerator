using System.Collections.Concurrent;
using System.Text;

namespace NeoApps.Mcp.Services;

public enum JobState { Running, Succeeded, Failed }

public sealed class Job
{
    private readonly StringBuilder _log = new();
    private const int MaxLogChars = 400_000;

    public required string Id { get; init; }
    public required string Kind { get; init; }
    public required string Project { get; init; }
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.Now;
    public DateTimeOffset? FinishedAt { get; internal set; }
    public JobState State { get; internal set; } = JobState.Running;
    public object? Result { get; internal set; }
    public string? Error { get; internal set; }
    internal Task Completion { get; set; } = Task.CompletedTask;

    /// <summary>Optional live echo (used by the CLI to stream progress).</summary>
    public Action<string>? Echo { get; init; }

    public void Log(string line)
    {
        Echo?.Invoke(line);
        lock (_log)
        {
            _log.AppendLine(line);
            if (_log.Length > MaxLogChars) _log.Remove(0, _log.Length - MaxLogChars);
        }
    }

    public string LogTail(int lines)
    {
        string all;
        lock (_log) all = _log.ToString();
        return string.Join('\n', all.Split('\n').Where(l => l.Length > 0).TakeLast(lines));
    }

    public object Summary(int logLines = 25) => new
    {
        jobId = Id,
        kind = Kind,
        project = Project,
        state = State.ToString().ToLowerInvariant(),
        startedAt = StartedAt,
        finishedAt = FinishedAt,
        elapsedSeconds = (int)((FinishedAt ?? DateTimeOffset.Now) - StartedAt).TotalSeconds,
        result = Result,
        error = Error,
        logTail = LogTail(logLines),
        hint = State == JobState.Running ? $"Still running. Call neoapps_job_status with jobId \"{Id}\" to check again." : null,
    };
}

/// <summary>
/// Runs long operations (code generation, docker builds) in the background so tool calls
/// return quickly; callers poll with neoapps_job_status.
/// </summary>
public sealed class JobManager(NeoAppsSettings settings)
{
    /// <summary>When set, every job streams its log lines here (CLI mode).</summary>
    public Action<string>? Echo { get; set; }

    private readonly ConcurrentDictionary<string, Job> _jobs = new();

    public Job Start(string kind, string project, Func<Job, CancellationToken, Task<object?>> work)
    {
        var job = new Job { Id = $"{kind}-{Guid.NewGuid().ToString("N")[..8]}", Kind = kind, Project = project, Echo = Echo };
        _jobs[job.Id] = job;
        job.Completion = Task.Run(async () =>
        {
            try
            {
                job.Result = await work(job, CancellationToken.None);
                job.State = JobState.Succeeded;
            }
            catch (Exception ex)
            {
                job.Error = ex.Message;
                job.Log("ERROR: " + ex);
                job.State = JobState.Failed;
            }
            finally
            {
                job.FinishedAt = DateTimeOffset.Now;
            }
        });
        return job;
    }

    /// <summary>Waits up to the inline window, then returns the job summary (finished or not).</summary>
    public async Task<object> StartAndWaitAsync(string kind, string project, Func<Job, CancellationToken, Task<object?>> work, CancellationToken ct)
    {
        var job = Start(kind, project, work);
        await Task.WhenAny(job.Completion, Task.Delay(settings.InlineWait, ct));
        return job.Summary();
    }

    public Job? Get(string id) => _jobs.TryGetValue(id, out var j) ? j : null;

    public IEnumerable<Job> All => _jobs.Values.OrderByDescending(j => j.StartedAt);
}
