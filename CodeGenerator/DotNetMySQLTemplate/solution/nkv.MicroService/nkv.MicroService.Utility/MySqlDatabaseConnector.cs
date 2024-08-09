using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nkv.MicroService.Utility
{
    public interface IDatabaseConnection
    {
        IDbConnection GetConnection();

    }
    public class MySqlDatabaseConnector : IDisposable
    {
        private MySqlConnection mySqlConnection;
        public bool disposed = false;
        private readonly string _connectionString;

        public MySqlDatabaseConnector(string connectionString)
        {
            mySqlConnection = new MySqlConnection(connectionString);
        }
        public MySqlConnection GetConnection()
        {

            return mySqlConnection;
        }
        public void OpenConnection()
        {
            if (mySqlConnection.State == ConnectionState.Closed)
            {
                Console.WriteLine("Connection Opened to execute Query");
                mySqlConnection.Open();
            }
        }

        public void CloseConnection()
        {
            if (mySqlConnection != null && mySqlConnection.State != ConnectionState.Closed)
            {
                Console.WriteLine("Connection Closed");
                mySqlConnection.Close();

            }
        }

        public void Dispose()
        {
            CloseConnection();
            mySqlConnection.Dispose();
        }
    }
}
