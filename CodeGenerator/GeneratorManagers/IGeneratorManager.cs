using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoCodeAppGenerator.GeneratorManagers
{
    public interface IGeneratorManager
    {
        void send_to_consumer<T>(T model);
    }
}
