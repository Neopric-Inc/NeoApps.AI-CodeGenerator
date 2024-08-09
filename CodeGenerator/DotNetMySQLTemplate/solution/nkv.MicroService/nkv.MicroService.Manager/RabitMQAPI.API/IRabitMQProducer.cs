using nkv.MicroService.Manager.Impl;
using System.Configuration;
using System.Collections.Generic;

namespace nkv.MicroService.Manager.RabitMQAPI.API
{
    public interface IRabitMQProducer
    {
        public void SendProductMessage<T>(T message, string queue_name, string token);
        public void SendProductMessage<T>(T message, Dictionary<string, int> id, string queue_name, string token);
        public void SendProductMessage(Dictionary<string, int> id, string queue_name, string token);
    }
}
