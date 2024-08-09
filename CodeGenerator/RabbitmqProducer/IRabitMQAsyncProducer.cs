namespace NoCodeAppGenerator.RabbitmqProducer
{
    public interface IRabitMQAsyncProducer
    {
        public void SendAsyncMessage<T>(T message,string name);
        public void SendAsyncMessage<T>(T message, int id, string name);
        public void SendAsyncMessage<T>(int id, string name);
    }
}
