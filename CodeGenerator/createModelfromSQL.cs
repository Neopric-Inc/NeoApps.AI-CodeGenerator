using System;
using System.Collections;
using System.IO;
using MySqlConnector;

namespace HelloWorld
{
    public class createModelfromSQL
    {
        private string connectionString;
        private string projectName;
        private string databaseName;
        private string filePath;
        public static string extractDatabaseName(string fileName, string DB)
        {
            string script = File.ReadAllText(fileName);

            int startPos = script.LastIndexOf("-- Database: `") + "-- Database: `".Length;
            int length = script.IndexOf("`\n--") - startPos;
            // Console.WriteLine(startPos + " " + length);
            if (length <= 0)
            {
                return DB;
            }
            string databaseName = script.Substring(startPos, length);

            return databaseName;
        }
        public createModelfromSQL(string connectionString, string filePath, string projectName,string databaseName)
        {
            this.filePath = filePath;
            this.databaseName = databaseName;
            // for DateTime field type
            this.connectionString = connectionString +"database=" +databaseName+";ConvertZeroDateTime=True;";
            // this.tableName = tableName;
            this.projectName = projectName;
        }

        // This function will create Model from SQL script
        public void createModelbyPassingSQL()
        {
            // Creating Temporary database from SQL script
            createDBfromSQL myDB = new createDBfromSQL(connectionString,databaseName, filePath);
            myDB.createDBbyPassingSQSL();
            Console.WriteLine(connectionString);
            // Creating Models from That temporary Database
            createModelfromDB myModel = new createModelfromDB(connectionString, projectName);
            myModel.createModelbyPassingDB();
            // Cleaning the temporary generated Database
        }

        public void cleanDBgeneratedfromSQL()
        {
            MySqlConnection connection = new MySqlConnection(connectionString);

            using (connection)
            {
                try
                {
                    connection.Open();
                    MySqlCommand command = connection.CreateCommand();
                    command.CommandType = System.Data.CommandType.Text;
                    // Deleting all content of database
                    command.CommandText = "DROP DATABASE " + databaseName;
                    command.ExecuteNonQuery();
                    // Recreating clean database
                    command.CommandText = "CREATE DATABASE " + databaseName;
                    command.ExecuteNonQuery();
                    Console.WriteLine("Database Generated for Internal Purpose has been Cleaned Successfully.");

                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    Console.WriteLine("Oops!!! , Error!! :- " + ex.Message.ToString());
                }
            }
        }
    }
}