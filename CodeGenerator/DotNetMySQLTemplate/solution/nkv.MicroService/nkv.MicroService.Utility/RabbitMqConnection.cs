using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
namespace nkv.MicroService.Utility
{
    public class RabbitMqConnection
    {
        public IConnection connection;
        public RabbitMqConnection(string connectionString)
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri(connectionString);
            connection = factory.CreateConnection();

        }
    }
}
