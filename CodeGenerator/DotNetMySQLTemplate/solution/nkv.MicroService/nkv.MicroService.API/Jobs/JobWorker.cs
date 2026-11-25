using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using nkv.MicroService.API.Ai;
using nkv.MicroService.Utility;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace nkv.MicroService.API.Jobs
{
    /// <summary>
    /// Runs queued jobs. Hosted inside the API by default (Jobs:Worker=true); for heavy workloads run more instances of the
    /// same image with Jobs__Worker=true and set it to false on the web-facing ones. Jobs:Concurrency (default 2) is how many
    /// jobs one instance runs at once, Jobs:TimeoutMinutes (30) stops a stuck job, Jobs:MaxAttempts (3) gives up on a job
    /// that keeps getting interrupted. Each job runs as the user who started it (DataGuard.RunAs), so it sees and changes
    /// exactly what that user could.
    /// </summary>
    public sealed class JobWorker : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly JobStore _store;
        private readonly JobQueue _queue;
        private readonly JobSignals _signals;
        private readonly ILogger<JobWorker> _log;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _running = new();

        public JobWorker(IServiceProvider services, IConfiguration configuration, JobStore store, JobQueue queue, JobSignals signals, ILogger<JobWorker> log)
        {
            _services = services; _configuration = configuration; _store = store; _queue = queue; _signals = signals; _log = log;
        }

        private int Setting(string key, int fallback) => int.TryParse(_configuration["Jobs:" + key], out var v) && v > 0 ? v : fallback;

        protected override async Task ExecuteAsync(CancellationToken stop)
        {
            if (string.Equals(_configuration["Jobs:Worker"], "false", StringComparison.OrdinalIgnoreCase)) return;
            // DataGuard must be ready before any job runs: without it queries would not be scoped to the job's user.
            while (!DataGuard.Ready && !stop.IsCancellationRequested) await Task.Delay(500, stop);

            _signals.OnCancelRequested(id => { if (_running.TryGetValue(id, out var c)) c.Cancel(); });
            if (_queue.Bus != null) { await RunServiceBus(stop); return; }
            if (!_queue.HasBroker) { _log.LogWarning("Background jobs are off: no Azure Service Bus (ServiceBus:*) or RabbitMQ configured."); return; }

            var channel = _services.GetRequiredService<IConnection>().CreateModel();
            JobQueue.Declare(channel, _queue.Name);
            channel.BasicQos(0, (ushort)Setting("Concurrency", 2), false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (_, ea) =>
            {
                var jobId = Encoding.UTF8.GetString(ea.Body.ToArray());
                if (jobId == JobQueue.TickBody) { try { lock (channel) channel.BasicAck(ea.DeliveryTag, false); } catch { } return; }
                var tag = ea.DeliveryTag;
                var redelivered = ea.Redelivered;
                _ = Task.Run(async () =>
                {
                    var ack = true;
                    try { ack = await Process(jobId, redelivered, stop); }
                    catch (Exception ex) { _log.LogError(ex, "Job {JobId} crashed the worker loop", jobId); }
                    if (!ack) return; // app is stopping: leave the message unacknowledged so another worker picks it up
                    try { lock (channel) channel.BasicAck(tag, false); }
                    catch (Exception ex) { _log.LogWarning("Couldn't acknowledge job {JobId}: {Message}", jobId, ex.Message); }
                });
            };
            lock (channel) channel.BasicConsume(_queue.Name, autoAck: false, consumer);
            _log.LogInformation("Job worker listening on {Queue}", _queue.Name);

            try { await Task.Delay(Timeout.Infinite, stop); }
            catch (OperationCanceledException) { }
            try { lock (channel) channel.Close(); } catch { }
        }

        /// <summary>Azure Service Bus: the message lock is renewed while the job runs; on shutdown the message is abandoned
        /// so another instance (or this one after a restart) picks the job up again.</summary>
        private async Task RunServiceBus(CancellationToken stop)
        {
            await using var processor = _queue.Bus.CreateProcessor(_queue.Name, new Azure.Messaging.ServiceBus.ServiceBusProcessorOptions
            {
                MaxConcurrentCalls = Setting("Concurrency", 2),
                AutoCompleteMessages = false,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(Setting("TimeoutMinutes", 30) + 5),
            });
            processor.ProcessMessageAsync += async args =>
            {
                var jobId = args.Message.Body.ToString();
                if (jobId == JobQueue.TickBody)
                {
                    // a scheduler wake-up: run what's due, book the next one, then take the message off the queue
                    try { await _services.GetRequiredService<JobScheduler>().OnTick(stop); }
                    catch (Exception ex) { _log.LogError(ex, "Scheduler wake-up failed"); }
                    await args.CompleteMessageAsync(args.Message, CancellationToken.None);
                    return;
                }
                var ack = true;
                try { ack = await Process(jobId, args.Message.DeliveryCount > 1, stop); }
                catch (Exception ex) { _log.LogError(ex, "Job {JobId} crashed the worker loop", jobId); }
                if (ack) await args.CompleteMessageAsync(args.Message, CancellationToken.None);
                else await args.AbandonMessageAsync(args.Message, cancellationToken: CancellationToken.None);
            };
            processor.ProcessErrorAsync += args =>
            {
                _log.LogWarning("Service Bus job queue: {Message}", args.Exception.Message);
                return Task.CompletedTask;
            };
            await processor.StartProcessingAsync(stop);
            _log.LogInformation("Job worker listening on Service Bus queue {Queue}", _queue.Name);
            try { await Task.Delay(Timeout.Infinite, stop); }
            catch (OperationCanceledException) { }
            try { await processor.StopProcessingAsync(); } catch { }
        }

        /// <summary>Runs or resumes one job. Returns false when the message should go back on the queue (shutdown).</summary>
        private async Task<bool> Process(string jobId, bool redelivered, CancellationToken stop)
        {
            var job = _store.Get(jobId);
            if (job == null) return true;

            bool resume;
            if (job.Status == "queued") resume = false;
            else if (job.Status == "approved") resume = true;
            else if (job.Status == "running" && redelivered)
            {
                // a worker stopped in the middle of this job: run it again, from the approval if there was one
                if (job.Attempts >= Setting("MaxAttempts", 3))
                {
                    if (_store.Transition(jobId, "running", "failed", error: "The job was interrupted too many times.", finished: true))
                        _store.AddEvent(jobId, "error", "Stopped: the job was interrupted too many times.", null, null);
                    return true;
                }
                _store.AddEvent(jobId, "warn", "Restarting after an interruption.", null, null);
                resume = job.Decision != null;
                if (!_store.Transition(jobId, "running", "running", countAttempt: true)) return true;
                return await Run(job, resume, stop);
            }
            else return true; // finished, cancelled, or waiting for a person

            if (!_store.Transition(jobId, job.Status, "running", started: true, countAttempt: true)) return true; // someone else got it
            return await Run(job, resume, stop);
        }

        private async Task<bool> Run(JobRow job, bool resume, CancellationToken stop)
        {
            var id = job.JobId;
            void Fail(string message, string level = "error")
            {
                if (_store.Transition(id, "running", "failed", error: message, finished: true))
                    _store.AddEvent(id, level, message, null, null);
            }

            var now = _store.CurrentUser(job.UserId);
            if (now is not { active: true }) { Fail("The account that started this job is no longer active."); return true; }
            var user = new JobUser(job.UserId, job.Username, now.Value.roleId, job.Owner);
            var caller = new DataGuard.Caller
            {
                User = job.Username, UserId = job.UserId, RoleId = now.Value.roleId, Owner = job.Owner,
                IsAi = job.Kind.StartsWith("ai.", StringComparison.OrdinalIgnoreCase), CorrelationId = "job-" + id,
                Via = job.Kind == "data.import" ? "import" : null,
            };
            if (DataGuard.TenancyEnabled)
            {
                var (tenant, unassigned) = DataGuard.ResolveTenant(job.UserId, now.Value.roleId);
                if (unassigned || tenant != job.TenantId) { Fail("The user's tenant changed since this job was started."); return true; }
                caller.Tenant = tenant;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stop);
            cts.CancelAfter(TimeSpan.FromMinutes(Setting("TimeoutMinutes", 30)));
            _running[id] = cts;
            JobContext ctx = null;
            try
            {
                using var scope = _services.CreateScope();
                using var _ = DataGuard.RunAs(caller);
                var handler = scope.ServiceProvider.GetServices<JobHandler>().FirstOrDefault(h => h.Kind == job.Kind);
                if (handler == null) { Fail($"No handler for job kind '{job.Kind}'."); return true; }
                ctx = new JobContext(_store, id, job.Kind, user, job.Input, job.State, scope.ServiceProvider, cts.Token);
                _store.AddEvent(id, "info", resume ? "Approved: continuing." : "Started.", null, null);
                if (resume) await handler.ResumeAsync(ctx, job.Decision ?? new System.Text.Json.Nodes.JsonObject());
                else await handler.RunAsync(ctx);
                if (!ctx.Paused && !ctx.Finished) ctx.Complete(null);
                return true;
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
                _log.LogInformation("Job {JobId} interrupted by shutdown; it will be retried", id);
                return false;
            }
            catch (OperationCanceledException)
            {
                if (_store.Get(id)?.Status != "cancelled") Fail($"Stopped: the job took longer than {Setting("TimeoutMinutes", 30)} minutes.");
                return true;
            }
            catch (Exception ex) when (ex is JobException or AiException or DataGuardException)
            {
                Fail(ex is DataGuardException dg ? dg.Message : ex.Message);
                return true;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Job {JobId} ({Kind}) failed", id, job.Kind);
                Fail("The job ran into an error. Reference: job-" + id);
                return true;
            }
            finally { _running.TryRemove(id, out _); ctx?.CleanupTemp(); }
        }
    }
}
