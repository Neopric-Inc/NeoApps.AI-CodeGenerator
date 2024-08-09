using nkv.MicroService.Utility;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nkv.MicroService.Consumer
{
    public class QueuedetailsDataAccess : IQueuedetailsDataAccess
    {

        private MySqlDatabase MySqlDatabase { get; set; }
        List<string> queues = new List<string>();
        public QueuedetailsDataAccess(MySqlDatabase mysqlDb)
        {
            MySqlDatabase = mysqlDb;
        }
        
        public List<string> fetchQueueName()
        {
            var cmd = MySqlDatabase.Connection.CreateCommand() as MySqlCommand;
            {fetchQueueName}
            using (var reader = cmd.ExecuteReader()){
                while (reader.Read())
                {
                    int i = 0;
                    queues.Add(reader.GetString(i++));
                }
                reader.Close();
            }
            return queues;
        }
        public string findPrimaryKey(string name)
        {
            var cmd = MySqlDatabase.Connection.CreateCommand() as MySqlCommand;
            {findPimaryKey}
            string key = "Not Found";
            using (var reader = cmd.ExecuteReader()){
                while(reader.Read())
                {
                    Console.WriteLine(reader.GetString(0));
                    key = reader.GetString(0);
                }
                reader.Close();
            }
            return key;
        }
        
    }
}
