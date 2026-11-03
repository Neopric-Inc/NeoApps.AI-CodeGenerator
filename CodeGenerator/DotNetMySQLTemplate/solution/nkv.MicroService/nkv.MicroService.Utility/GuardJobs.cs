using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;

namespace nkv.MicroService.Utility
{
    /// <summary>
    /// Background jobs driven by ui.config.json:
    ///  - outbox publisher: pending rows of the "outbox" table are published to RabbitMQ (topic exchange, routing key = event type,
    ///    MessageId = outbox row id so consumers can de-duplicate), with retries and a dead-letter state after maxAttempts.
    ///  - "jobs": scheduled rules (deadline/SLA breach flags and escalations, usage limits) - see GuardJobs.Schedules.cs.
    /// </summary>
    public static partial class GuardJobs
    {
        static Timer _outboxTimer;
        static int _outboxBusy;
        static string _rabbitUri;
        static IConnection _rabbit;
        static string _exchange = "neoapps.events";
        static int _maxAttempts = 5;
        public static int Published, Failed;

        public static void Start(IServiceProvider services)
        {
            if (!DataGuard.Ready) return;
            var cfg = services.GetService<IConfiguration>();
            _rabbitUri = cfg?.GetConnectionString("RabitMQ");
            DataGuard.SetSigningSecret(cfg?["AppSettings:Secret"]);
            if (DataGuard.Config["outbox"] is JsonObject ob)
            {
                _exchange = ob["exchange"]?.GetValue<string>() ?? _exchange;
                _maxAttempts = ob["maxAttempts"]?.GetValue<int>() ?? _maxAttempts;
                var every = ob["publishEverySeconds"]?.GetValue<int>() ?? 5;
                if (DataGuard.OutboxTable != null && !string.IsNullOrEmpty(_rabbitUri))
                    _outboxTimer = new Timer(_ => PublishOutbox(), null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(every));
            }
            StartImpl(services);
        }
        static partial void StartImpl(IServiceProvider services);

        /// <summary>Publishes pending outbox rows once. Safe to call concurrently (one run at a time, rows locked with SKIP LOCKED).</summary>
        public static int PublishOutbox()
        {
            if (Interlocked.Exchange(ref _outboxBusy, 1) == 1) return 0;
            var sent = 0;
            try
            {
                var table = DataGuard.OutboxTable;
                var pk = DataGuard.PrimaryKeys[table];
                IModel channel;
                try
                {
                    if (_rabbit == null || !_rabbit.IsOpen) _rabbit = new ConnectionFactory { Uri = new Uri(_rabbitUri), AutomaticRecoveryEnabled = true }.CreateConnection("neoapps-outbox");
                    channel = _rabbit.CreateModel();
                    channel.ExchangeDeclare(_exchange, ExchangeType.Topic, durable: true);
                    channel.ExchangeDeclare(_exchange + ".dlx", ExchangeType.Fanout, durable: true);
                    channel.ConfirmSelect();
                }
                catch (Exception ex) { Console.WriteLine("Outbox: RabbitMQ unavailable, will retry: " + ex.Message); return 0; }
                using (channel)
                using (var c = new MySqlConnection(DataGuard.ConnectionString))
                {
                    c.Open();
                    using var tx = c.BeginTransaction();
                    var rows = new List<(object id, string type, string payload, int attempts, string tenant)>();
                    var hasTenant = DataGuard.Columns[table].Contains(DataGuard.TenantColumn);
                    using (var q = new MySqlCommand($@"SELECT `{pk}`, event_type, payload, attempts{(hasTenant ? $", `{DataGuard.TenantColumn}`" : "")} FROM `{table}`
                        WHERE status IN ('pending','failed') AND isActive = 1 ORDER BY `{pk}` LIMIT 100 FOR UPDATE SKIP LOCKED", c, tx))
                    using (var r = q.ExecuteReader())
                        while (r.Read())
                            rows.Add((r.GetValue(0), r.GetString(1), r.IsDBNull(2) ? "{}" : r.GetString(2), r.IsDBNull(3) ? 0 : Convert.ToInt32(r.GetValue(3)),
                                      hasTenant && !r.IsDBNull(4) ? r.GetValue(4).ToString() : null));
                    foreach (var row in rows)
                    {
                        string status; var attempts = row.attempts + 1;
                        try
                        {
                            var props = channel.CreateBasicProperties();
                            props.Persistent = true;
                            props.MessageId = $"{table}:{row.id}";
                            props.ContentType = "application/json";
                            props.Type = row.type;
                            props.Headers = new Dictionary<string, object> { ["tenant"] = row.tenant ?? "", ["attempt"] = attempts };
                            channel.BasicPublish(_exchange, row.type, mandatory: false, props, Encoding.UTF8.GetBytes(row.payload));
                            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(10));
                            status = "published"; sent++; Published++;
                        }
                        catch (Exception ex)
                        {
                            Failed++;
                            status = attempts >= _maxAttempts ? "dead_letter" : "failed";
                            Console.WriteLine($"Outbox: event {row.id} ({row.type}) attempt {attempts} failed: {ex.Message}");
                            if (status == "dead_letter")
                                try { channel.BasicPublish(_exchange + ".dlx", row.type, false, null, Encoding.UTF8.GetBytes(row.payload)); } catch { }
                        }
                        using var u = new MySqlCommand($"UPDATE `{table}` SET status = @s, attempts = @a{(status == "published" && DataGuard.Columns[table].Contains("published_at") ? ", published_at = UTC_TIMESTAMP()" : "")} WHERE `{pk}` = @id", c, tx);
                        u.Parameters.AddWithValue("@s", status); u.Parameters.AddWithValue("@a", attempts); u.Parameters.AddWithValue("@id", row.id);
                        u.ExecuteNonQuery();
                    }
                    tx.Commit();
                }
            }
            catch (Exception ex) { Console.WriteLine("Outbox publisher error: " + ex.Message); }
            finally { Interlocked.Exchange(ref _outboxBusy, 0); }
            return sent;
        }
    }
}
