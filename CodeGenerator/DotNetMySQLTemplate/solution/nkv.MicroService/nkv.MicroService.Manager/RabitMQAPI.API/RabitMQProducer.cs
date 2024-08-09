using nkv.MicroService.Utility;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.CodeDom;
using nkv.MicroService.Manager.Impl;
using System.Text;
using Microsoft.Extensions.Configuration;
using nkv.MicroService.Model;
using Renci.SshNet.Messages;
using System.Collections.Generic;

namespace nkv.MicroService.Manager.RabitMQAPI.API
{
    public class RabitMQProducer : IRabitMQProducer
    {
        private IConnection _connection { get; set; }
        public RabitMQProducer(IConnection connection)
        {
            _connection = connection;
        }
        public void SendProductMessage<T>(T message, string queue_name, string token)
        {

            using
            var channel = _connection.CreateModel();
            channel.QueueDeclare(queue_name, exclusive: false);


            var jsonstring = JsonConvert.SerializeObject(message);
            PacketModel pkt = new PacketModel(jsonstring, queue_name, token);


            //Serialize the message
            var json = JsonConvert.SerializeObject(pkt);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "", routingKey: queue_name, body: body);

            channel.Close();

        }
        public void SendProductMessage<T>(T message, Dictionary<string, int> id, string queue_name, string token)
        {
            var jsonstring = JsonConvert.SerializeObject(message);
            PacketModel pkt = new PacketModel(jsonstring, id, queue_name, token);

            using
            var channel = _connection.CreateModel();
            channel.QueueDeclare(queue_name, exclusive: false);
            //Serialize the message
            var json = JsonConvert.SerializeObject(pkt);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "", routingKey: queue_name, body: body);

            channel.Close();

        }

        public void SendProductMessage(Dictionary<string, int> id, string queue_name, string token)
        {

            using
            var channel = _connection.CreateModel();

            channel.QueueDeclare(queue_name, exclusive: false);
            //Serialize the message
            var json = JsonConvert.SerializeObject("hi");
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "", routingKey: queue_name, body: body);

            channel.Close();
        }
    }
}
