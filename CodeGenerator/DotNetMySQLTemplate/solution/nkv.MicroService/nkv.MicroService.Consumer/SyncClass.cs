using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using nkv.MicroService.Model;
using Newtonsoft.Json;

namespace nkv.MicroService.Consumer
{
    public class SyncClass
    {
        public void syncMethod()
        {
            var factory = new ConnectionFactory
            {
                UserName = "guest",
                Password = "guest",
                HostName = "{server}"
            };
            //Create the RabbitMQ connection using connection factory details as i mentioned above
            var connection = factory.CreateConnection();
            //Here we create channel with session and model
            using
            var syncChannel = connection.CreateModel();
            //declare the queue after mentioning name and a few property related to that
            syncChannel.QueueDeclare("myeshop.API", exclusive: false);
            //Set Event object which listen message from chanel which is sent by producer
            var consumer = new EventingBasicConsumer(syncChannel);
            consumer.Received += (model, eventArgs) => {
                //Console.WriteLine("event args:{0}", eventArgs);
                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);


                PacketModel packet = JsonConvert.DeserializeObject<PacketModel>(message);
                if (packet == null)
                    Console.WriteLine("exception thrown...packet is null");
                else
                {
                    Console.WriteLine(packet.className);
                    //Console.WriteLine(message);
                    byte[] utf8Bytes = Encoding.UTF8.GetBytes(packet.className);

                    //Converting to Unicode from UTF8 bytes
                    byte[] unicodeBytes = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, utf8Bytes);

                    //Getting string from Unicode bytes
                    string classN = Encoding.Unicode.GetString(unicodeBytes);
                    //Console.WriteLine(msg);
                    {GenerateModelMessage}
                    else
                    {
                        Console.WriteLine("No model present with this name:{0}", classN);
                    }
                }
            };
            //read the message
            syncChannel.BasicConsume(queue: "myeshop.API", autoAck: true, consumer: consumer);
            Console.ReadKey();
        }
    }
}
