using nkv.MicroService.DataAccess.Interface;
using nkv.MicroService.Model;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using nkv.MicroService.Utility;
using System.Collections;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Dynamic;

namespace nkv.MicroService.Consumer
{
    public class ConsumeMessage
    {
        private static int NumThreads = 10;
        private IModel channel;
        private IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
        private IConnection connection { get; set; }

        private string queueName = "DatabaseEventsModel";
        public ConsumeMessage(IConnection c)
        {
            connection = c;
            channel = connection.CreateModel();
            channel.QueueDeclare(queue: queueName,
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
        }
        public void consumeMessage()
        {

            //Set Event object which listen message from chanel that is sent by producer
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, eventArgs) =>
            {

                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                ThreadPool.QueueUserWorkItem(async (state) =>
                {

                    channel.BasicAck(deliveryTag: eventArgs.DeliveryTag, multiple: false);

                    PacketModel msg = JsonConvert.DeserializeObject<PacketModel>(message);
                    if (msg == null)
                    {
                        Console.WriteLine("msg is null");
                    }
                    else
                    {
                        //coverting ffrom UTF8 to Unicode
                        byte[] utf8Bytes = Encoding.UTF8.GetBytes(msg.className);
                        byte[] unicodeBytes = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, utf8Bytes);

                        //Getting string from Unicode bytes
                        string className = Encoding.Unicode.GetString(unicodeBytes);
                        var action = "";
                        string token = msg.token;
                        Console.WriteLine("Token Found: " + token);
                        if (msg.id.Values.First() == int.MaxValue)
                        {
                            Console.WriteLine("added model");
                            action = "added";
                        }
                        else if (msg.classString == null)
                        {

                            Console.WriteLine("Deleted model");
                            action = "deleted";
                        }
                        else
                        {
                            Console.WriteLine("updated model");
                            action = "updated";
                        }
                        {printMessageModel}

                    }
                });
            };


            channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            while (true)
            {
                Thread.Sleep(1000);
                ThreadPool.GetAvailableThreads(out var workerThreads, out var ioThreads);
                if (workerThreads == 0 && NumThreads < 100)
                // Increase the number of threads if all threads are busy
                {
                    NumThreads++;
                    ThreadPool.SetMaxThreads(NumThreads, NumThreads);
                }
            }
        }
    }
}
