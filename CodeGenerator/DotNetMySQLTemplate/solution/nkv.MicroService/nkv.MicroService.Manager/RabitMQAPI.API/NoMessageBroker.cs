using System.Collections.Generic;

namespace nkv.MicroService.Manager.RabitMQAPI.API
{
    /// <summary>
    /// Used when the app has no RabbitMQ (ConnectionStrings:RabitMQ empty, e.g. serverless on Azure). The create/update/
    /// delete events these producers send only feed the legacy workflow consumer; data-rule events use the outbox.
    /// </summary>
    public sealed class NoMessageBroker : IRabitMQProducer, IRabitMQAsyncProducer
    {
        public void SendProductMessage<T>(T message, string queue_name, string token) { }
        public void SendProductMessage<T>(T message, Dictionary<string, int> id, string queue_name, string token) { }
        public void SendProductMessage(Dictionary<string, int> id, string queue_name, string token) { }
        public void SendAsyncMessage<T>(T message, string name, string token) { }
        public void SendAsyncMessage<T>(T message, Dictionary<string, int> id, string name, string token) { }
        public void SendAsyncMessage(Dictionary<string, int> id, string name, string token) { }
    }
}
