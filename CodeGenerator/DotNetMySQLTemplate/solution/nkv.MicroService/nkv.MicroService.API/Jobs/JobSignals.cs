using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using StackExchange.Redis;

namespace nkv.MicroService.API.Jobs
{
    /// <summary>
    /// Redis pub/sub for jobs: "this job changed" pings that wake long-polling feeds at once, and cancel requests that
    /// reach whichever worker instance runs the job. Channels are prefixed with the database name, so apps sharing a Redis
    /// don't see each other. If Redis is down, feeds fall back to checking MySQL every second.
    /// </summary>
    public sealed class JobSignals
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<JobSignals> _log;
        private readonly string _changed, _cancel;
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<TaskCompletionSource, byte>> _waiters = new();
        private ISubscriber _sub;
        private int _subscribed;

        public JobSignals(IServiceProvider services, IConfiguration configuration, ILogger<JobSignals> log)
        {
            _services = services;
            _log = log;
            var db = new MySqlConnectionStringBuilder(configuration.GetConnectionString("MySQLDatabase")).Database;
            _changed = $"neoapps:{db}:job:";
            _cancel = $"neoapps:{db}:job-cancel";
        }

        private ISubscriber Sub()
        {
            if (_sub != null) return _sub;
            try { _sub = _services.GetRequiredService<IConnectionMultiplexer>().GetSubscriber(); }
            catch (Exception ex) { _log.LogWarning("Redis unavailable for job signals: {Message}", ex.Message); }
            return _sub;
        }

        private void EnsureSubscribed()
        {
            if (Interlocked.CompareExchange(ref _subscribed, 1, 0) != 0) return;
            try
            {
                Sub()?.Subscribe(RedisChannel.Pattern(_changed + "*"), (ch, _) =>
                {
                    var id = ((string)ch).Substring(_changed.Length);
                    if (_waiters.TryGetValue(id, out var set)) foreach (var t in set.Keys) t.TrySetResult();
                });
                if (_sub == null) _subscribed = 0;
            }
            catch (Exception ex) { _subscribed = 0; _log.LogWarning("Couldn't subscribe to job signals: {Message}", ex.Message); }
        }

        /// <summary>Tells every open feed of this job that it changed.</summary>
        public void Changed(string jobId)
        {
            try { Sub()?.Publish(RedisChannel.Literal(_changed + jobId), "1", CommandFlags.FireAndForget); }
            catch { /* feeds still see the change on their next check */ }
        }

        /// <summary>
        /// Starts listening for the next change of a job. Call it before reading the job, then await
        /// <see cref="Listener.WaitAsync"/>, so a change between the read and the wait isn't missed.
        /// </summary>
        public Listener Listen(string jobId)
        {
            EnsureSubscribed();
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.GetOrAdd(jobId, _ => new ConcurrentDictionary<TaskCompletionSource, byte>())[tcs] = 0;
            return new Listener(this, jobId, tcs, _subscribed == 1);
        }

        public sealed class Listener : IDisposable
        {
            private readonly JobSignals _owner;
            private readonly string _jobId;
            private readonly TaskCompletionSource _tcs;
            private readonly bool _live;

            internal Listener(JobSignals owner, string jobId, TaskCompletionSource tcs, bool live)
            {
                _owner = owner; _jobId = jobId; _tcs = tcs; _live = live;
            }

            /// <summary>Waits until the job changes or <paramref name="timeout"/> passes (at most 1s when Redis is unavailable).</summary>
            public async Task WaitAsync(TimeSpan timeout, CancellationToken ct)
            {
                if (!_live) timeout = TimeSpan.FromSeconds(Math.Min(1, timeout.TotalSeconds));
                try { await Task.WhenAny(_tcs.Task, Task.Delay(timeout, ct)); }
                catch (OperationCanceledException) { }
            }

            public void Dispose()
            {
                if (_owner._waiters.TryGetValue(_jobId, out var set))
                {
                    set.TryRemove(_tcs, out _);
                    if (set.IsEmpty) _owner._waiters.TryRemove(new System.Collections.Generic.KeyValuePair<string, ConcurrentDictionary<TaskCompletionSource, byte>>(_jobId, set));
                }
            }
        }

        public void RequestCancel(string jobId)
        {
            try { Sub()?.Publish(RedisChannel.Literal(_cancel), jobId, CommandFlags.FireAndForget); }
            catch { /* the worker also notices the status change before its next step */ }
        }

        public void OnCancelRequested(Action<string> handler)
        {
            try { Sub()?.Subscribe(RedisChannel.Literal(_cancel), (_, v) => handler(v)); }
            catch (Exception ex) { _log.LogWarning("Couldn't subscribe to job cancellations: {Message}", ex.Message); }
        }
    }
}
