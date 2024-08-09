using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nkv.MicroService.Consumer
{
    public interface IQueuedetailsDataAccess
    {
        public List<string> fetchQueueName();
        
        public string findPrimaryKey(string name);
    }
}
