using System;
using System.Collections;
using MySqlConnector;
using System.IO;
using System.Text.RegularExpressions;
using NoCodeAppGenerator;
using NoCodeAppGenerator.Infrastructure;
using System.Data;

namespace HelloWorld
{

    // This class is solely responsible for creating Database tables from .sql script
    public class createDBfromSQL
    {
        public string databaseName;
        private string filePath;
        private string connectionString;
        // static connection for developement purpose
        // static string connectionString = "server=localhost;database=test;uid=root;port=3306;ConvertZeroDateTime=True;";
        // static MySqlConnection connection = new MySqlConnection(connectionString);

        public createDBfromSQL(string databaseName, string filePath, string connectionString)
        {
            this.databaseName = databaseName; ;
            this.filePath = filePath;
            this.connectionString = connectionString;
        }
        public void createDBbyPassingSQSL()
        {
             MySqlConnection connection = new MySqlConnection(connectionString);
            string extractedDatabaseName = extractDatabaseName(filePath, databaseName);
            string scriptText = File.ReadAllText(filePath);
            scriptText = scriptText.Replace(extractedDatabaseName, databaseName);
            File.WriteAllText(filePath, scriptText);
            //connectionString += "database=" + databaseName + ";";
            Console.WriteLine("Connect to MySQL DB..... \n");
            if (!connectionString.Contains("database="))
                connectionString += "database=" + databaseName + ";";
            connection = new MySqlConnection(connectionString);
            using (connection)
            {
                try
                {
                    Console.WriteLine(connectionString);
                    connection.Open();
                    Console.WriteLine("Connection is :- " + connection.State.ToString() + Environment.NewLine);

                    // Executing actual script
                    // Let scripts declare tables in any order (LLM-written schemas often reference a table before creating it).
                    using (var fkOff = new MySqlCommand("SET FOREIGN_KEY_CHECKS=0", connection)) fkOff.ExecuteNonQuery();
                    MySqlScript script = new MySqlScript(connection, File.ReadAllText(filePath));
                    script.Delimiter = ";";

                    script.Error += new MySqlScriptErrorEventHandler(script_Error);
                    script.ScriptCompleted += new EventHandler(script_ScriptCompleted);
                    // script.StatementExecuted += new MySqlStatementExecutedEventHandler(script_StatementExecuted);

                    int count = script.Execute();

                    Console.WriteLine("Executed " + count + " statement(s).");
                    using (var fkOn = new MySqlCommand("SET FOREIGN_KEY_CHECKS=1", connection)) fkOn.ExecuteNonQuery();
                    AddUserColumns(connection);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error:");
                    Console.WriteLine(ex.ToString());
                    NoCodeAppGenerator.Program.errors_list.Add("Error in CreateDBbyPassingSQL: " + ex.ToString());
                }
                finally
                {
                    if (connection != null && connection.State != ConnectionState.Closed)
                    {
                        connection.Close();
                        Console.WriteLine("\nConnection is: " + connection.State.ToString() + Environment.NewLine);
                    }
                }

            }
        }
        /// <summary>
        /// Adds created_by_user / modified_by_user (who really created or last changed a row; createdBy holds the workspace
        /// owner used for scoping) to every app table. DataGuard stamps them on every write.
        /// </summary>
        static readonly System.Collections.Generic.HashSet<string> BuiltInTables = new(StringComparer.OrdinalIgnoreCase)
        {
            "roles", "users", "entities", "permissionmatrix", "messagequeue", "s3bucket", "s3bucket_folders", "dnd_ui_versions",
            "project_dnd_ui_versions", "workflow", "workflows", "workflows_projects", "workflow_builds", "workflow_runs",
            "workflow_deployments", "workflow_triggers", "workflow_trigger_conditions", "ai_files", "ai_chunks", "ai_conversations",
            "ai_messages", "ai_runs", "versioninfo", "user_tenants", "login_attempts", "user_security"
        };

        public static void AddUserColumns(MySqlConnection connection)
        {
            var missing = new System.Collections.Generic.List<(string table, string col)>();
            using (var q = new MySqlCommand(@"SELECT t.table_name, x.col FROM information_schema.columns t
                    JOIN (SELECT 'created_by_user' col UNION ALL SELECT 'modified_by_user') x
                    WHERE t.table_schema = DATABASE() AND LOWER(t.column_name) = 'createdby'
                      AND NOT EXISTS (SELECT 1 FROM information_schema.columns c WHERE c.table_schema = t.table_schema
                                      AND c.table_name = t.table_name AND LOWER(c.column_name) = x.col)", connection))
            using (var r = q.ExecuteReader()) while (r.Read()) if (!BuiltInTables.Contains(r.GetString(0))) missing.Add((r.GetString(0), r.GetString(1)));
            foreach (var (table, col) in missing)
                using (var a = new MySqlCommand($"ALTER TABLE `{table}` ADD COLUMN `{col}` VARCHAR(255) NULL", connection)) a.ExecuteNonQuery();
        }

        public static string extractDatabaseName(string fileName, string DB)
        {
            try
            {
                string script = File.ReadAllText(fileName);
                string pattern = @"CREATE DATABASE (\w+);";
                Match match = Regex.Match(script, pattern);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in ExtractDatabaseName:");
                Console.WriteLine(ex.ToString());
                NoCodeAppGenerator.Program.errors_list.Add("Error in ExtractDatabaseName: " + ex.ToString());
            }
            return DB;
        }
        // create database if not exist (from our script)
        void createDatabase(string filePath, string realDatabaseName, ref MySqlConnection connection)
        {
            Console.WriteLine("-------------------------------------------");
            string extractedDatabaseName = extractDatabaseName(filePath, realDatabaseName);
            Console.WriteLine(extractedDatabaseName);

            {
                Console.WriteLine("Oops!!! Connection DATABASE & script DATABASE is NOT same.");

                using (connection)
                {
                    try
                    {
                        connection.Open();
                        //Console.WriteLine("Connection is :- " + connection.State.ToString() + Environment.NewLine);
                        MySqlCommand command = connection.CreateCommand();
                        command.CommandType = System.Data.CommandType.Text;
                        Console.WriteLine(extractedDatabaseName);
                        command.CommandText = "CREATE DATABASE IF NOT EXISTS `" + extractedDatabaseName + "`;";

                        command.ExecuteNonQuery();
                        Console.WriteLine("-----------------------");
                        Console.WriteLine("DATABASE HAS BEEN CREATED.");
                        Console.WriteLine("-----------------------");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("CREATE DATABASE IF NOT EXISTS `" + extractedDatabaseName + "`;");
                        Console.WriteLine("Error!!" + ex.ToString());
                    }
                    connection.Close();
                    Console.WriteLine("Connection is :- " + connection.State.ToString() + Environment.NewLine);
                }
                string connectionString = "server=localhost;uid=root;port=3306;database=" + extractedDatabaseName + ";" + "ConvertZeroDateTime=True;";
                connection = new MySqlConnection(connectionString);

            }
            Console.WriteLine("-------------------------------------------");
        }

        void script_ScriptCompleted(object sender, EventArgs e)
        {
            /// EventArgs e will be EventArgs.Empty for this method
            Console.WriteLine("script_ScriptCompleted!");
        }

        void script_Error(Object sender, MySqlScriptErrorEventArgs args)
        {
            Console.WriteLine("script_Error: " + args.Exception.ToString());
            NoCodeAppGenerator.Program.errors_list.Add("script_Error: " + args.Exception.Message);
        }
    }
}