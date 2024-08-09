using System.Collections.Generic;


namespace nkv.MicroService.Manager.RabitMQAPI.API
{
    public interface IRabitMQAsyncProducer
    {
        public void SendAsyncMessage<T>(T message, string name, string token);
        public void SendAsyncMessage<T>(T message, Dictionary<string, int> id, string name, string token);
        public void SendAsyncMessage(Dictionary<string, int> id, string name, string token);
    }
}
