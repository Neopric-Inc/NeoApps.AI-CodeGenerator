using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nkv.MicroService.Manager.RabitMQAPI.API
{
    public interface IChannelManager
    {
        IModel GetChannel();
    }
    public class ChannelManager : IChannelManager
    {
        private readonly IConnection rabbitMqConnection;
        private IModel channel;

        private static readonly object lockObject = new object();

        public ChannelManager(IConnection _rabbitMqConnection)
        {
            rabbitMqConnection = _rabbitMqConnection;
            CreateChannel();
        }

        private void CreateChannel()
        {
            if (channel == null || channel.IsClosed)
            {
                lock (lockObject)
                {
                    if (channel == null || channel.IsClosed)
                    {
                        channel = rabbitMqConnection.CreateModel();
                        InitializeQueues();
                    }
                }
            }
        }
        private void InitializeQueues()
        {
            //// Create a list of MyData objects
            //List<MessageQueueChannels> dataList = new List<MessageQueueChannels>();

            //// Add data to the list

            

            //foreach (var data in dataList)
            //{
            //    channel.QueueDeclare(data.ChannelName, false, false, false, null);
            //}
        }
        private static RabitMQAsyncProducer instance;

        public IModel GetChannel()
        {
            if (channel == null || channel.IsClosed)
            {
                lock (lockObject)
                {
                    if (channel == null || channel.IsClosed)
                    {
                        channel = rabbitMqConnection.CreateModel();
                    }
                }
            }
            return channel;
        }
    }
}
