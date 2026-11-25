using System;
using System.Text;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using nkv.MicroService.Utility;
using RabbitMQ.Client;

namespace nkv.MicroService.API.Jobs
{
    /// <summary>
    /// Puts job ids on a durable queue. Workers take them one at a time per free slot, so heavy jobs never pile up on the
    /// web server, and a job whose worker dies is handed to another worker.
    ///   Azure Service Bus  when ServiceBus:ConnectionString or ServiceBus:Namespace (managed identity) is set; queue
    ///                      ServiceBus:JobsQueue (default "jobs"). Serverless: nothing runs while the app is idle.
    ///   RabbitMQ           otherwise (ConnectionStrings:RabitMQ), one queue per app database, as before.
    /// </summary>
    public sealed class JobQueue
    {
        private readonly IServiceProvider _services;
        private readonly object _lock = new();
        private IModel _channel;
        private readonly ServiceBusSender _sender;

        public JobQueue(IServiceProvider services, IConfiguration configuration)
        {
            _services = services;
            Bus = CreateServiceBus(configuration);
            if (Bus != null)
            {
                Name = configuration["ServiceBus:JobsQueue"] is { Length: > 0 } q ? q : "jobs";
                _sender = Bus.CreateSender(Name);
            }
            else Name = "neoapps.jobs." + Db.DatabaseName(configuration.GetConnectionString("MySQLDatabase"));
        }

        /// <summary>The queue name (Service Bus queue or RabbitMQ queue).</summary>
        public string Name { get; }

        /// <summary>A queue is configured (Service Bus or RabbitMQ).</summary>
        public bool HasBroker => Bus != null || _services.GetService<IConnection>() != null;

        /// <summary>The Service Bus client when the app uses Service Bus; null for RabbitMQ.</summary>
        public ServiceBusClient Bus { get; }

        public static ServiceBusClient CreateServiceBus(IConfiguration configuration)
        {
            var cs = configuration["ServiceBus:ConnectionString"];
            var ns = configuration["ServiceBus:Namespace"]; // e.g. myapp-bus.servicebus.windows.net
            if (!string.IsNullOrWhiteSpace(cs)) return new ServiceBusClient(cs);
            if (!string.IsNullOrWhiteSpace(ns)) return new ServiceBusClient(ns, new DefaultAzureCredential());
            return null;
        }

        public static void Declare(IModel channel, string name) =>
            channel.QueueDeclare(queue: name, durable: true, exclusive: false, autoDelete: false, arguments: null);

        /// <summary>Message body of a scheduler wake-up (see <see cref="JobScheduler"/>); never a job id.</summary>
        public const string TickBody = "neoapps:schedule-tick";

        /// <summary>
        /// Service Bus only: books a scheduler wake-up for <paramref name="atUtc"/>. The message stays invisible until then,
        /// so the app can scale to zero and the queue trigger wakes it on time. Returns the sequence number (to cancel it).
        /// </summary>
        public long ScheduleTick(DateTime atUtc)
        {
            if (_sender == null) throw new InvalidOperationException("Scheduled wake-ups need Azure Service Bus.");
            return _sender.ScheduleMessageAsync(new ServiceBusMessage(TickBody) { MessageId = "tick:" + atUtc.ToString("yyyyMMddHHmmss") + ":" + Guid.NewGuid().ToString("N") },
                new DateTimeOffset(DateTime.SpecifyKind(atUtc, DateTimeKind.Utc))).GetAwaiter().GetResult();
        }

        /// <summary>Cancels a booked wake-up; false when it already fired or doesn't exist.</summary>
        public bool CancelTick(long sequence)
        {
            if (_sender == null) return false;
            try { _sender.CancelScheduledMessageAsync(sequence).GetAwaiter().GetResult(); return true; }
            catch (Exception) { return false; }
        }

        public void Enqueue(string jobId)
        {
            if (_sender != null)
            {
                _sender.SendMessageAsync(new ServiceBusMessage(jobId) { MessageId = jobId + ":" + Guid.NewGuid().ToString("N") }).GetAwaiter().GetResult();
                return;
            }
            if (!HasBroker) throw new JobException("Background jobs aren't set up on this server: it needs Azure Service Bus (ServiceBus:*) or RabbitMQ.");
            lock (_lock)
            {
                if (_channel == null || _channel.IsClosed)
                {
                    _channel = _services.GetRequiredService<IConnection>().CreateModel();
                    Declare(_channel, Name);
                }
                var props = _channel.CreateBasicProperties();
                props.Persistent = true;
                props.MessageId = jobId;
                _channel.BasicPublish(exchange: "", routingKey: Name, basicProperties: props, body: Encoding.UTF8.GetBytes(jobId));
            }
        }
    }
}
