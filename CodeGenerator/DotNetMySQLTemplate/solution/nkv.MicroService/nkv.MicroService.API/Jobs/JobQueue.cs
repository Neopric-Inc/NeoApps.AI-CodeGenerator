using System.Text;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using RabbitMQ.Client;

namespace nkv.MicroService.API.Jobs
{
    /// <summary>
    /// Puts job ids on a durable RabbitMQ queue (one per app database). Workers take them one at a time per free slot,
    /// so heavy jobs never pile up on the web server, and a job whose worker dies is handed to another worker.
    /// </summary>
    public sealed class JobQueue
    {
        private readonly IConnection _connection;
        private readonly object _lock = new();
        private IModel _channel;

        public JobQueue(IConnection connection, IConfiguration configuration)
        {
            _connection = connection;
            Name = "neoapps.jobs." + new MySqlConnectionStringBuilder(configuration.GetConnectionString("MySQLDatabase")).Database;
        }

        public string Name { get; }

        public static void Declare(IModel channel, string name) =>
            channel.QueueDeclare(queue: name, durable: true, exclusive: false, autoDelete: false, arguments: null);

        public void Enqueue(string jobId)
        {
            lock (_lock)
            {
                if (_channel == null || _channel.IsClosed)
                {
                    _channel = _connection.CreateModel();
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
