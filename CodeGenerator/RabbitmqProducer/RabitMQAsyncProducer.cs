using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Configuration;
using System.Threading.Channels;
namespace NoCodeAppGenerator.RabbitmqProducer
{
    
    public class RabitMQAsyncProducer : IRabitMQAsyncProducer
    {
        private IConnection _connection { get; set; }
        private readonly IChannelManager channelManager;
        private static readonly object lockObject = new object();
        string queueName = "DatabaseEventsModel";

        public RabitMQAsyncProducer(RabbitMQ.Client.IConnection connection, IChannelManager _channelManager)
        {
            _connection = connection;
            this.channelManager = _channelManager;
        }

        public void SendAsyncMessage<T>(T message, string name)
        {

            var channel = channelManager.GetChannel();
            channel.QueueDeclare(queueName, false, false, false, null);
            channel.BasicConsume(queueName, false, new EventingBasicConsumer(channel));

            var jsonstring = JsonConvert.SerializeObject(message);
            PacketModel pkt = new PacketModel(jsonstring, name);
            channel.BasicAcks += (sender, ea) => {

                Console.WriteLine(ea.DeliveryTag);
                Console.WriteLine("Ack recieved");

            };
            channel.ConfirmSelect();


            var messageProperties = channel.CreateBasicProperties();
            messageProperties.Persistent = true;

            //Serialize the message
            var json = JsonConvert.SerializeObject(pkt);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "", routingKey: queueName, body: body);
            channel.WaitForConfirmsOrDie();//exception handling-on nacks throws exception

            channel.Close();


        }
        public void SendAsyncMessage<T>(T message, int id, string name)
        {
            var channel = channelManager.GetChannel();
            //    channel.QueueDeclare(name, false, false, false, null);
            channel.BasicConsume(name, false, new EventingBasicConsumer(channel));

            var jsonstring = JsonConvert.SerializeObject(message);
            PacketModel pkt = new PacketModel(jsonstring, id, name);
            channel.BasicAcks += (sender, ea) => {

                Console.WriteLine(ea.DeliveryTag);
                Console.WriteLine("Ack recieved");

            };
            channel.ConfirmSelect();


            var messageProperties = channel.CreateBasicProperties();
            messageProperties.Persistent = true;

            //Serialize the message
            var json = JsonConvert.SerializeObject(pkt);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "", routingKey: name, body: body);
            channel.WaitForConfirmsOrDie();//exception handling-on nacks throws exception

            channel.Close();

        }
        public void SendAsyncMessage<T>(int id, string name)
        {
            var channel = channelManager.GetChannel();
            //  channel.QueueDeclare(name, false, false, false, null);
            channel.BasicConsume(name, false, new EventingBasicConsumer(channel));

            PacketModel pkt = new PacketModel(id, name);
            channel.BasicAcks += (sender, ea) => {

                Console.WriteLine(ea.DeliveryTag);
                Console.WriteLine("Ack recieved");

            };
            channel.ConfirmSelect();


            var messageProperties = channel.CreateBasicProperties();
            messageProperties.Persistent = true;

            //Serialize the message
            var json = JsonConvert.SerializeObject(pkt);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: "", routingKey: name, body: body);
            channel.WaitForConfirmsOrDie();//exception handling-on nacks throws exception

            channel.Close();

        }

        //public void SendAsyncMessage<T>(T message,string name)
        //{
        //    using
        //    var channel = _connection.CreateModel();
        //    channel.QueueDeclare(name, false, false, false, null);
        //    channel.BasicConsume(name, false, new EventingBasicConsumer(channel));

        //    var jsonstring = JsonConvert.SerializeObject(message);
        //    PacketModel pkt = new PacketModel(jsonstring, name);
        //    channel.BasicAcks += (sender, ea) => {

        //        Console.WriteLine(ea.DeliveryTag);
        //        Console.WriteLine("Ack recieved");

        //    };
        //    channel.ConfirmSelect();


        //    var messageProperties = channel.CreateBasicProperties();
        //    messageProperties.Persistent = true;

        //    //Serialize the message
        //    var json = JsonConvert.SerializeObject(pkt);
        //    var body = Encoding.UTF8.GetBytes(json);

        //    channel.BasicPublish(exchange: "", routingKey: name, body: body);
        //    channel.WaitForConfirmsOrDie();//exception handling-on nacks throws exception

        //    channel.Close();


        //}
        //public void SendAsyncMessage<T>(T message, int id, string name)
        //{
        //    using
        //    var channel = _connection.CreateModel();
        //    channel.QueueDeclare(name, false, false, false, null);
        //    channel.BasicConsume(name, false, new EventingBasicConsumer(channel));

        //    var jsonstring = JsonConvert.SerializeObject(message);
        //    PacketModel pkt = new PacketModel(jsonstring, id, name);
        //    channel.BasicAcks += (sender, ea) => {

        //        Console.WriteLine(ea.DeliveryTag);
        //        Console.WriteLine("Ack recieved");

        //    };
        //    channel.ConfirmSelect();


        //    var messageProperties = channel.CreateBasicProperties();
        //    messageProperties.Persistent = true;

        //    //Serialize the message
        //    var json = JsonConvert.SerializeObject(pkt);
        //    var body = Encoding.UTF8.GetBytes(json);

        //    channel.BasicPublish(exchange: "", routingKey: name, body: body);
        //    channel.WaitForConfirmsOrDie();//exception handling-on nacks throws exception

        //    channel.Close();

        //}
        //public void SendAsyncMessage<T>(int id,string name)
        //{
        //    using
        //    var channel = _connection.CreateModel();
        //    channel.QueueDeclare(name, false, false, false, null);
        //    channel.BasicConsume(name, false, new EventingBasicConsumer(channel));

        //    PacketModel pkt = new PacketModel(id, name);
        //    channel.BasicAcks += (sender, ea) => {

        //        Console.WriteLine(ea.DeliveryTag);
        //        Console.WriteLine("Ack recieved");

        //    };
        //    channel.ConfirmSelect();


        //    var messageProperties = channel.CreateBasicProperties();
        //    messageProperties.Persistent = true;

        //    //Serialize the message
        //    var json = JsonConvert.SerializeObject(pkt);
        //    var body = Encoding.UTF8.GetBytes(json);

        //    channel.BasicPublish(exchange: "", routingKey: name, body: body);
        //    channel.WaitForConfirmsOrDie();//exception handling-on nacks throws exception

        //    channel.Close();

        //}

    }
}
