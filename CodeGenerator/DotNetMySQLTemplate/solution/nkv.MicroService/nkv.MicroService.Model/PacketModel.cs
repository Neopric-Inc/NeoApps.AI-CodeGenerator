using System;
using System.Collections.Generic;
using System.Text;

namespace nkv.MicroService.Model
{
    public class PacketModel
    {
        public string classString;

        public Dictionary<string, int> id = new Dictionary<string, int>();

        public string className;
        public string token;
        public PacketModel() { }
        public PacketModel(string classString, Dictionary<string, int> id, string className, string token)
        {
            this.classString = classString;
            this.id = id;
            this.className = className;
            this.token = token;
        }
        public PacketModel(string classString, string className, string token)
        {
            this.classString = classString;
            this.id.Add("id", int.MaxValue);
            this.className = className;
            this.token = token;
        }
        public PacketModel(Dictionary<string, int> id, string className, string token)
        {
            this.classString = null;
            this.id = id;
            this.className = className;
            this.token = token;
        }
    }
}
