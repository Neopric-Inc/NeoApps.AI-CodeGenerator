using NoCodeAppGenerator.RabbitmqProducer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoCodeAppGenerator.GeneratorManagers
{
    public class GeneratorManager : IGeneratorManager
    {
        private readonly IRabitMQAsyncProducer _rabitMQAsyncProducer;

        public GeneratorManager(IRabitMQAsyncProducer rabitMQAsyncProducer)
        {
            _rabitMQAsyncProducer = rabitMQAsyncProducer;
        }

        public void send_to_consumer<T>(T model)
        {
            _rabitMQAsyncProducer.SendAsyncMessage(model, model.GetType().Name);
        }
    }
}
