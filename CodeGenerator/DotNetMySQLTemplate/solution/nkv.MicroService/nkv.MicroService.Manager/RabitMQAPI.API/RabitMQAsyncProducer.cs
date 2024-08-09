using nkv.MicroService.Model;

using Newtonsoft.Json;

using nkv.MicroService.Utility;

using RabbitMQ.Client;

using RabbitMQ.Client.Events;

using Renci.SshNet.Messages;

using System;

using System.Text;

using System.Configuration;

using System.Collections.Generic;


namespace nkv.MicroService.Manager.RabitMQAPI.API
{
    public class RabitMQAsyncProducer : IRabitMQAsyncProducer
    {
        private IConnection _connection { get; set; }
        public RabitMQAsyncProducer(IConnection connection)
        {
            _connection = connection;
        }

        string queueName = "DatabaseEventsModel";
        public void SendAsyncMessage<T>(T message, string name, string token)
        {

            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queueName, false, false, false, null);

                // Set up a basic consumer, even though it's not used in your current code
                /*var consumer = new EventingBasicConsumer(channel);*/

                // Set up ACK handling
                channel.BasicAcks += (sender, ea) =>
                {
                    Console.WriteLine(ea.DeliveryTag);
                    Console.WriteLine("Ack received");
                };

                // Enable publisher confirms
                channel.ConfirmSelect();

                var messageProperties = channel.CreateBasicProperties();
                messageProperties.Persistent = true;

                // Serialize the message
                var jsonstring = JsonConvert.SerializeObject(message);
                var pkt = new PacketModel(jsonstring, name, token);
                var json = JsonConvert.SerializeObject(pkt);
                var body = Encoding.UTF8.GetBytes(json);

                // Publish the message
                channel.BasicPublish(exchange: "", routingKey: queueName, body: body);

                try
                {
                    // Wait for confirms with a timeout
                    channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    // Handle exceptions related to confirms here
                    Console.WriteLine("Exception during confirms: " + ex.Message);
                }
            } // The channel will be closed automatically when leaving the using block


        }
        public void SendAsyncMessage<T>(T message, Dictionary<string, int> id, string name, string token)
        {
            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queueName, false, false, false, null);

                // Set up a basic consumer, even though it's not used in your current code
                /*var consumer = new EventingBasicConsumer(channel);*/

                // Set up ACK handling
                channel.BasicAcks += (sender, ea) =>
                {
                    Console.WriteLine(ea.DeliveryTag);
                    Console.WriteLine("Ack received");
                };

                // Enable publisher confirms
                channel.ConfirmSelect();

                var messageProperties = channel.CreateBasicProperties();
                messageProperties.Persistent = true;

                // Serialize the message
                var jsonstring = JsonConvert.SerializeObject(message);
                PacketModel pkt = new PacketModel(jsonstring, id, name, token);
                var json = JsonConvert.SerializeObject(pkt);
                var body = Encoding.UTF8.GetBytes(json);

                // Publish the message
                channel.BasicPublish(exchange: "", routingKey: queueName, body: body);

                try
                {
                    // Wait for confirms with a timeout
                    channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    // Handle exceptions related to confirms here
                    Console.WriteLine("Exception during confirms: " + ex.Message);
                }
            } // The channel will be closed automatically when leaving the using block

        }
        public void SendAsyncMessage(Dictionary<string, int> id, string name, string token)
        {
            using (var channel = _connection.CreateModel())
            {
                channel.QueueDeclare(queueName, false, false, false, null);

                // Set up a basic consumer, even though it's not used in your current code
                /*var consumer = new EventingBasicConsumer(channel);*/

                // Set up ACK handling
                channel.BasicAcks += (sender, ea) =>
                {
                    Console.WriteLine(ea.DeliveryTag);
                    Console.WriteLine("Ack received");
                };

                // Enable publisher confirms
                channel.ConfirmSelect();

                var messageProperties = channel.CreateBasicProperties();
                messageProperties.Persistent = true;

                // Serialize the message
                PacketModel pkt = new PacketModel(id, name, token);
                var json = JsonConvert.SerializeObject(pkt);
                var body = Encoding.UTF8.GetBytes(json);

                // Publish the message
                channel.BasicPublish(exchange: "", routingKey: queueName, body: body);

                try
                {
                    // Wait for confirms with a timeout
                    channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    // Handle exceptions related to confirms here
                    Console.WriteLine("Exception during confirms: " + ex.Message);
                }
            } // The channel will be closed automatically when leaving the using block

        }

    }

}