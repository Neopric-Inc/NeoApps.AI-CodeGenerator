using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoCodeAppGenerator
{
    public class PacketModel
    {
        public string classString;

        public int id;

        public string className;

        public PacketModel() { }
        public PacketModel(string classString, int id, string className)
        {
            this.classString = classString;
            this.id = id;
            this.className = className;
        }
        public PacketModel(string classString, string className)
        {
            this.classString = classString;
            id = int.MaxValue;
            this.className = className;
        }
        public PacketModel(int id, string className)
        {
            this.classString = null;
            this.id = id;
            this.className = className;
        }
    }
}
