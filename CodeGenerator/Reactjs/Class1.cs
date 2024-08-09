using MySql.Data.MySqlClient;
using System;

public sealed class MySqlConnectionManager
{
    private static MySqlConnectionManager instance;
    private static readonly object lockObject = new object();
    private MySqlConnection connection;

    private MySqlConnectionManager(string connectionString)
    {
        connection = new MySqlConnection(connectionString);
    }

    public static MySqlConnectionManager Instance
    {
        get
        {
            if (instance == null)
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        throw new InvalidOperationException("Connection string not set. Call Initialize method first.");
                    }
                }
            }
            return instance;
        }
    }

    public static void Initialize(string connectionString)
    {
        if (instance == null)
        {
            lock (lockObject)
            {
                if (instance == null)
                {
                    instance = new MySqlConnectionManager(connectionString);
                }
            }
        }
    }

    public MySqlConnection GetConnection()
    {
        // Check if the connection is closed or broken, and reestablish it if necessary
        if (connection.State == System.Data.ConnectionState.Closed || connection.State == System.Data.ConnectionState.Broken)
        {
            connection.Open();
        }

        return connection;
    }
}
