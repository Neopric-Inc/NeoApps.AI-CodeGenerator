using System.Data.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace nkv.MicroService.Utility
{
    public class MySqlDatabase : IDisposable
    {
        public DbConnection Connection;

        public MySqlDatabase(string connectionString)
        {
            Connection = Db.Connection(connectionString);
            this.Connection.Open();
        }

        public void Dispose()
        {
            Connection.Close();
        }
    }
}
