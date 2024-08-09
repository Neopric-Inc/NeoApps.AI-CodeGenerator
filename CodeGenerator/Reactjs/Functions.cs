using HelloWorld;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using NoCodeAppGenerator;
using NoCodeAppGenerator.ParameterModel;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using BCrypt;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ThirdParty.Json.LitJson;
using static System.Net.Mime.MediaTypeNames;

namespace ReactJsProjectDemo
{
    public class ComponentMapping
    {
        public string Component { get; set; }
        public string associated_table { get; set; }
        public string Description { get; set; }
    }

    public class RootObject
    {
        public List<ComponentMapping> Mappings { get; set; }
    }
    public class Functions
    {
        public string databaseName;
        public Functions(string Db)
        {
            databaseName = Db;
        }
        public static bool CheckDatabaseExists(string connectionString, string databaseName)
        {
            try
            {
                //using (var connection = new MySqlConnection(connectionString))
                //{
                //    connection.Open();
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                var command = new MySqlCommand("SHOW DATABASES WHERE `database` = @databaseName", connection);
                command.Parameters.AddWithValue("@databaseName", databaseName);
                using (var reader = command.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in CheckDatabaseExists: " + e.ToString());
                Program.errors_list.Add("Error in CheckDatabaseExists: " + e.ToString());
                return false;
            }
            //}
        }
        public bool MakeAndCopyDirectory(string src, string des)
        {
            try
            {
                if (!Directory.Exists(des))
                    Directory.CreateDirectory(des);

                CopyAll(new DirectoryInfo(src), new DirectoryInfo(des));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in MakeAndCopyDirectory: " + e.ToString());
                Program.errors_list.Add("Error in MakeAndCopyDirectory: " + e.ToString());
                return false;
            }
            return true;
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            try
            {
                if (source.FullName.ToLower() == target.FullName.ToLower())
                {
                    return;
                }

                // Check if the target directory exists; if not, create it.
                if (Directory.Exists(target.FullName) == false)
                {
                    Directory.CreateDirectory(target.FullName);
                }

                // Copy each file into the new directory.
                foreach (FileInfo fi in source.GetFiles())
                {
                    fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                }

                // Copy each subdirectory using recursion.
                foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
                {
                    DirectoryInfo nextTargetSubDir =
                        target.CreateSubdirectory(diSourceSubDir.Name);
                    CopyAll(diSourceSubDir, nextTargetSubDir);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in CopyAll: " + e.ToString());
                Program.errors_list.Add("Error in CopyAll: " + e.ToString());
            }
        }

        List<string> function(string connectionString, string tbl)
        {
            List<string> ans = new List<string>();
            try
            {
                List<string> PK = GetPrimaryKey(connectionString, tbl);
                string query = "SELECT column_name FROM information_schema.columns WHERE table_name = '" + tbl + "' AND DATA_TYPE = 'varchar' AND IS_NULLABLE = 'NO' LIMIT 1";
                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                //{
                //    connection.Open();
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            string columnName = reader["column_name"].ToString();
                            if (!PK.Contains(columnName))
                                ans.Add(columnName);
                        }
                    }
                }
                //}
                if (ans.Count > 0)
                    return ans;
                query = "SELECT column_name FROM information_schema.columns WHERE table_name = '" + tbl + "' AND column_key != 'PRI' LIMIT 1";
                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                //{
                //    connection.Open();
                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            reader.Read();
                            string columnName = reader["column_name"].ToString();
                            if (!PK.Contains(columnName))
                                ans.Add(columnName);
                        }
                    }
                }
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in function: " + e.ToString());
                Program.errors_list.Add("Error in function: " + e.ToString());
            }
            return ans;
        }
        List<string> GetAllTables(string des, string uid, string username, string password, string databaseName, string script, string port, string DBExists, string server)
        {
            List<string> ans = new List<string>();
            try
            {
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";";
                if (DBExists == "NO" || !CheckDatabaseExists(connectionString, databaseName))
                {
                    //using (WebClient client = new WebClient())
                    //{
                    //    client.DownloadFile(script, @des + "SQLScript.sql");
                    //}
                    createDBfromSQL myDB = new createDBfromSQL(databaseName, @des + "SQLScript.sql", connectionString);
                    myDB.createDBbyPassingSQSL();
                    connectionString += "database=" + databaseName + ";";
                    string createTableSql = "CREATE TABLE IF NOT EXISTS messageQueue (id INT AUTO_INCREMENT PRIMARY KEY, queueName VARCHAR(255) Unique,PrimaryKey VARCHAR(255))";
                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                    //{
                    //    connection.Open();
                    //MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                    using (MySqlCommand command = new MySqlCommand(createTableSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    //    connection.Close();
                    //}
                }
                string createTableSql2 = "CREATE TABLE IF NOT EXISTS messageQueue (id INT AUTO_INCREMENT PRIMARY KEY, queueName VARCHAR(255) Unique,PrimaryKey VARCHAR(255))";
                if (!connectionString.Contains("database="))
                    connectionString += "database=" + databaseName;
                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                //{
                //    connection.Open();
                using (MySqlCommand command = new MySqlCommand(createTableSql2, connection))
                {
                    command.ExecuteNonQuery();
                }
                //    connection.Close();
                //}
                string createTableSql3 = @"CREATE TABLE IF NOT EXISTS `dnd_ui_versions` (
  `dnd_ui_version_id` int NOT NULL AUTO_INCREMENT,
  `layout` json NOT NULL,
  `components` json NOT NULL,
  `ui_pages` json NOT NULL,
  `dnd_ui_type` varchar(45) NOT NULL,
  `createdBy` varchar(45) NOT NULL,
  `modifiedBy` varchar(45) NOT NULL,
  `createdAt` datetime NOT NULL,
  `modifiedAt` datetime NOT NULL,
  `isActive` tinyint(1) NOT NULL,
  PRIMARY KEY(`dnd_ui_version_id`)
);
CREATE TABLE IF NOT EXISTS `project_dnd_ui_versions` (
  `project_dnd_ui_version_id` int NOT NULL AUTO_INCREMENT,
  `project_id` int NOT NULL,
  `dnd_ui_version_id` int NOT NULL,
  `createdBy` varchar(255) NOT NULL,
  `modifiedBy` varchar(255) NOT NULL,
  `createdAt` datetime NOT NULL,
  `modifiedAt` datetime NOT NULL,
  `isActive` tinyint(1) NOT NULL,
  PRIMARY KEY (`project_dnd_ui_version_id`),
  KEY `fk_project_dnd_ui_versions_table` (`dnd_ui_version_id`),
  CONSTRAINT `fk_project_dnd_ui_versions_table` FOREIGN KEY (`dnd_ui_version_id`) REFERENCES `dnd_ui_versions` (`dnd_ui_version_id`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;


CREATE TABLE IF NOT EXISTS `workflow` (
  `id` int NOT NULL AUTO_INCREMENT,
  `steps` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `triggerpoint` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `modifiedBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `createdBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedAt` datetime NOT NULL,
  `createdAt` datetime NOT NULL,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;


CREATE TABLE IF NOT EXISTS `workflows` (
  `workflow_id` int NOT NULL AUTO_INCREMENT,
  `workflow_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `workflow_description` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `steps` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `triggerpoint` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `modifiedBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `createdBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedAt` datetime NOT NULL,
  `createdAt` datetime NOT NULL,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`workflow_id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `workflow_builds` (
  `workflow_build_id` int NOT NULL AUTO_INCREMENT,
  `workflow_id` int NOT NULL,
  `workflow_build_status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `workflow_build_start_time` datetime NOT NULL,
  `workflow_build_end_time` datetime NOT NULL,
  `modifiedBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `createdBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedAt` datetime NOT NULL,
  `createdAt` datetime NOT NULL,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`workflow_build_id`),
  KEY `workflow_id` (`workflow_id`),
  CONSTRAINT `workflow_builds_ibfk_1` FOREIGN KEY (`workflow_id`) REFERENCES `workflows` (`workflow_id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `workflows_projects` (
  `workflow_id` int NOT NULL,
  `project_id` int NOT NULL,
  `modifiedBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `createdBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedAt` datetime NOT NULL,
  `createdAt` datetime NOT NULL,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`workflow_id`,`project_id`),
  CONSTRAINT `workflows_projects_ibfk_1` FOREIGN KEY (`workflow_id`) REFERENCES `workflows` (`workflow_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
CREATE TABLE IF NOT EXISTS `workflow_runs` (
  `workflow_run_id` int NOT NULL AUTO_INCREMENT,
  `workflow_build_id` int NOT NULL,
  `workflow_run_status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `workflow_run_start_time` datetime NOT NULL,
  `workflow_run_end_time` datetime NOT NULL,
  `modifiedBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `createdBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedAt` datetime NOT NULL,
  `createdAt` datetime NOT NULL,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`workflow_run_id`),
  KEY `workflow_build_id` (`workflow_build_id`),
  CONSTRAINT `workflow_runs_ibfk_1` FOREIGN KEY (`workflow_build_id`) REFERENCES `workflow_builds` (`workflow_build_id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `workflow_deployments` (
  `workflow_deployment_id` int NOT NULL AUTO_INCREMENT,
  `workflow_run_id` int NOT NULL,
  `workflow_deployment_status` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `workflow_deployment_start_time` datetime NOT NULL,
  `workflow_deployment_end_time` datetime NOT NULL,
  `modifiedBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `createdBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedAt` datetime NOT NULL,
  `createdAt` datetime NOT NULL,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`workflow_deployment_id`),
  KEY `workflow_run_id` (`workflow_run_id`),
  CONSTRAINT `workflow_deployments_ibfk_1` FOREIGN KEY (`workflow_run_id`) REFERENCES `workflow_runs` (`workflow_run_id`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;


CREATE TABLE IF NOT EXISTS `workflow_triggers` (
  `workflow_trigger_id` int NOT NULL AUTO_INCREMENT,
  `workflow_id` int NOT NULL,
  `trigger_name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `trigger_type` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `createdBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedAt` datetime NOT NULL,
  `createdAt` datetime NOT NULL,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`workflow_trigger_id`),
  KEY `workflow_id` (`workflow_id`),
  CONSTRAINT `workflow_triggers_ibfk_1` FOREIGN KEY (`workflow_id`) REFERENCES `workflows` (`workflow_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;



CREATE TABLE IF NOT EXISTS `workflow_trigger_conditions` (
  `workflow_trigger_id` int NOT NULL,
  `condition_type` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `condition_value` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `createdBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedAt` datetime NOT NULL,
  `createdAt` datetime NOT NULL,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`workflow_trigger_id`,`condition_type`,`condition_value`),
  CONSTRAINT `workflow_trigger_conditions_ibfk_1` FOREIGN KEY (`workflow_trigger_id`) REFERENCES `workflow_triggers` (`workflow_trigger_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
";
                string connectionSstring = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                Console.WriteLine(connectionSstring);
                //using (MySqlConnection connection = new MySqlConnection(connectionSstring))
                //{
                //    connection.Open();
                using (MySqlCommand command = new MySqlCommand(createTableSql3, connection))
                {
                    Console.WriteLine("Executing");
                    command.ExecuteNonQuery();
                }
                //    connection.Close();
                //}
                //using (MySqlConnection conn = new MySqlConnection(connectionString))
                //{
                //    conn.Open();
                DataTable tables = connection.GetSchema("Tables");
                foreach (DataRow row in tables.Rows)
                {
                    ans.Add(row["TABLE_NAME"].ToString());
                }
                //}
                string query = "REPLACE INTO messageQueue (queueName,PrimaryKey) Values (@name,@PrimaryKey)";
                foreach (string i in ans)
                {
                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                    //{
                    //    connection.Open();
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", char.ToUpper(i[0]) + i.Substring(1) + "Model");
                        command.Parameters.AddWithValue("@PrimaryKey", GetPrimaryKey(connectionString, i)[0]);
                        command.ExecuteNonQuery();
                    }
                    //    connection.Close();
                    //}
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in GetAllTables: " + e.ToString());
                Program.errors_list.Add("Error in GetAllTables: " + e.Message);
            }

            return ans;
        }
        List<string> GetPrimaryKey(string connectionString, string tbl)
        {
            string query = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_KEY = 'PRI' AND TABLE_SCHEMA=@schemaName";
            List<string> ans = new List<string>();
            try
            {
                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                //{
                //    connection.Open();
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tbl);
                    command.Parameters.AddWithValue("@schemaName", databaseName);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string columnName = reader.GetString("COLUMN_NAME");
                            ans.Add(columnName);
                        }
                    }
                }
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in GetPrimaryKey: " + e.ToString());
                Program.errors_list.Add("Error in GetPrimaryKey: " + e.Message);
            }
            return ans;
        }
        List<string> GetPrimaryKeyType(string connectionString, string tbl)
        {
            string query = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND COLUMN_KEY = 'PRI' AND TABLE_SCHEMA=@schemaName";
            List<string> ans = new List<string>();
            try
            {
                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                //{
                //    connection.Open();
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tbl);
                    command.Parameters.AddWithValue("@schemaName", databaseName);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dataType = reader.GetString("DATA_TYPE");
                            ans.Add(dataType);
                        }
                    }
                }
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in GetPrimaryKeyType: " + e.ToString());
                Program.errors_list.Add("Error in GetPrimaryKeyType: " + e.Message);
            }
            return ans;
        }
        List<string> GetAllColumns(string connectionString, string tbl)
        {
            string query = "SELECT COLUMN_NAME, DATA_TYPE,IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND TABLE_SCHEMA=@schemaName";
            List<string> ans = new List<string>();
            try
            {
                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                //{
                //    connection.Open();
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tbl);
                    command.Parameters.AddWithValue("@schemaName", databaseName);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string columnName = reader.GetString("COLUMN_NAME");
                            string dataType = reader.GetString("DATA_TYPE");
                            string Nullable = reader.GetString("IS_NULLABLE");
                            ans.Add(columnName);
                            ans.Add(dataType);
                            ans.Add(Nullable);
                        }
                    }
                }
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in GetAllColumns: " + e.ToString());
                Program.errors_list.Add("Error in GetAllColumns: " + e.Message);
            }
            return ans;
        }
        string Generate(string funcType, string tbl, int numOfArgs, List<string> args, string reqType, string fileName, string backendChoice)
        {
            try
            {
                string tbl1 = char.ToUpper(tbl[0]) + tbl.Substring(1);
                string ans = "export const " + funcType + tbl1 + " = (";
                foreach (string arg in args)
                {
                    ans += arg;
                    if (arg != args.Last())
                        ans += ",";
                    else
                        ans += ")";
                }
                ans += " => {\nreturn APIService.api()." + reqType;
                if (backendChoice == "PHP")
                {
                    if (reqType == "get")
                    {
                        ans += "(`/" + tbl + "/" + fileName + ".php?";
                        foreach (string arg in args)
                        {
                            if (arg == args.First())
                                ans += arg + "=${" + arg + "}";
                            else
                                ans += "&" + arg + "=${" + arg + "}";
                        }
                        ans += "`)\n}\n";
                    }
                    else if (reqType == "delete")
                    {
                        ans += "(`/" + tbl + "/" + fileName + ".php`,";
                        ans += args[0];
                        ans += ")\n}\n";
                    }
                    else
                    {
                        ans += "(`/" + tbl + "/" + fileName + ".php`,";
                        ans += "{";
                        foreach (string arg in args)
                        {
                            ans += arg + ":" + arg;
                            if (arg != args.Last())
                                ans += ",";
                        }
                        ans += "})\n}\n";
                    }
                }
                else
                {
                    if (funcType == "getAll")
                    {
                        ans += "(`/" + tbl + "?";
                        foreach (string i in args)
                        {
                            if (i == "pageno")
                                ans += "page" + "=${" + i + "}";
                            else if (i == "pagesize")
                                ans += "itemsPerPage" + "=${" + i + "}";
                            else
                                ans += i + "=${" + i + "}";
                            if (i != args.Last())
                                ans += "&";
                        }
                        ans += "`)\n}\n";
                    }
                    else if (funcType == "getOneAggregate")
                    {
                        ans += "(`/Aggregate/" + tbl + "/";
                        foreach (string i in args)
                        {
                            ans += "${" + i + "}";
                            if (i != args.Last())
                                ans += "/";
                        }
                        ans += "`)\n}\n";
                    }
                    else if (funcType == "getOneRelational")
                    {
                        ans += "(`/" + tbl + "/Relational/";
                        foreach (string i in args)
                        {
                            ans += "${" + i + "}";
                            if (i != args.Last())
                                ans += "/";
                        }
                        ans += "`)\n}\n";
                    }
                    if (funcType == "getAllRelational")
                    {
                        ans += "(`/" + tbl + "/Relational?";
                        foreach (string i in args)
                        {
                            if(i == "pageno")
                                ans += "page"+ "=${" + i + "}";
                            else if(i== "pagesize")
                                ans += "itemsPerPage" + "=${" + i + "}";
                            else
                                ans += i + "=${" + i + "}";
                            if (i != args.Last())
                                ans += "&";
                        }
                        ans += "`)\n}\n";
                    }
                    else if (funcType == "getOneReporting")
                    {
                        ans += "(`/" + tbl + "/Reporting/";
                        foreach (string i in args)
                        {
                            ans += "${" + i + "}";
                            if (i != args.Last())
                                ans += "/";
                        }
                        ans += "`)\n}\n";
                    }
                    if (funcType == "getAllReporting")
                    {
                        ans += "(`/" + tbl + "/Reporting?";
                        foreach (string i in args)
                        {
                            if (i == "pageno")
                                ans += "page" + "=${" + i + "}";
                            else if (i == "pagesize")
                                ans += "itemsPerPage" + "=${" + i + "}";
                            else
                                ans += i + "=${" + i + "}";
                            if (i != args.Last())
                                ans += "&";
                        }
                        ans += "`)\n}\n";
                    }
                    else if (funcType == "getOne")
                    {
                        ans += "(`/" + tbl + "/${id}`)\n}\n";
                    }
                    else if (funcType == "add")
                    {
                        ans += "(`/" + tbl + "/" + "`," + args[0] + ")\n}\n";
                    }
                    else if (funcType == "addTransactional")
                    {
                        ans += "(`/" + tbl + "/Transactional/" + "`," + args[0] + ")\n}\n";
                    }
                    else if (funcType == "filter")
                    {
                        ans += "(`/" + tbl + "/filter" + "`," + args[0] + ")\n}\n";
                    }
                    else if (funcType == "update")
                    {
                        ans += "(`/" + tbl + "/";
                        foreach (string i in args)
                        {
                            if (i != args.Last())
                                ans += "${" + i + "}/";
                        }
                        ans += "`," + args.Last();
                        ans += ")\n}\n";
                    }
                    else if (funcType == "updateTransactional")
                    {
                        ans += "(`/" + tbl + "/Transactional/";
                        foreach (string i in args)
                        {
                            if (i != args.Last())
                                ans += "${" + i + "}/";
                        }
                        ans += "`," + args.Last();
                        ans += ")\n}\n";
                    }
                    else if (funcType == "delete")
                    {
                        ans += "(`/" + tbl + "/";
                        foreach (string i in args)
                        {
                            ans += "${" + i + "}/";
                        }
                        ans += "`)\n}\n";
                    }
                    else if (funcType == "search")
                    {
                        ans += "(`/" + tbl + "/search?searchKey=${" + args[0] + "}";
                        for (int i = 1; i < args.Count; i++)
                        {
                            ans += "&";
                            if (args[i] == "pageno")
                                ans += "page" + "=${" + args[i] + "}";
                            else if (args[i] == "pagesize")
                                ans += "itemsPerPage" + "=${" + args[i] + "}";
                            else
                                ans += args[i] + "=${" + args[i] + "}";
                        }
                        ans += "`)\n}\n";
                    }
                }
                return ans;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error in Generate: " + e.ToString());
                Program.errors_list.Add("Error in Generate: " + e.ToString());
                return ""; // Return an empty string or some predefined error message
            }
        }
        public string getCalendarFormTemplate(string text, string tbl, string src, string des, string server, string uid, string username, string password, string databaseName, string script, string statusOfGeneration, string projectName, string DBexists, string port, string backendChoice, string projectType, string swaggerurl = "")
        {
            string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
            text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
            text = text.Replace("{tableName}", tbl);
            List<string> FK = new List<string>();
            HashSet<string> FKC = new HashSet<string>();
            //using (MySqlConnection connection = new MySqlConnection(connectionString))
            //{
            //    connection.Open();
            MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
            List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
            string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
            MySqlCommand command = new MySqlCommand(sql, connection);
            string foreignKeyColumn = "", refTable = "";
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                    refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                    string dataType = reader["DATA_TYPE"].ToString();
                    string forkey1 = reader["COLUMN_NAME"].ToString();
                    FK.Add(foreignKeyColumn);
                    FK.Add(refTable);
                    if (refTable != tbl)
                        FKC.Add(refTable);
                    FK.Add(dataType);
                    FK.Add(forkey1);
                }
            }
            //}
            if (FK.Count != 0)
            {
                string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                string final = "";
                foreach (string s in FKC)
                {
                    //string s = FK[i + 1];
                    string table1 = char.ToUpper(s[0]) + s.Substring(1);
                    string template1 = template.Replace("{table1}", table1);
                    final += template1;
                }
                text = text.Replace("{importFKRedux}", final);
            }
            else
                text = text.Replace("{importFKRedux}", "\n");
            if (FK.Count != 0)
            {
                string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                string final = "";
                //for (int i = 0; i < FK.Count; i += 4)
                foreach (string s in FKC)
                {
                    //string s = FK[i + 1];
                    string table1 = char.ToUpper(s[0]) + s.Substring(1);
                    string FKK = s;// FK[i + 1];
                    string template1 = "";
                    template1 = template.Replace("{table2}", table1);
                    template1 = template1.Replace("{FK}", FKK);
                    final += template1;
                    if (tbl == "projects_repositories")
                        Console.WriteLine(template1);
                }
                text = text.Replace("{importFKService}", final);
            }
            else
                text = text.Replace("{importFKService}", "\n");
            List<string> cols = GetAllColumns(connectionString, tbl);
            List<string> PK = GetPrimaryKey(connectionString, tbl);
            List<string> PrimaryKeyDatatypes = GetPrimaryKeyType(connectionString, tbl);
            string valid = "";
            for (int i = 0; i < cols.Count; i += 3)
            {
                if (FK.Contains(cols[i]))
                { }
                else if (PK.Contains(cols[i]))
                { }
                else
                {
                    valid += cols[i] + ": yup." + "string" + "()" + ".test(\"validator-custom-name\", function (value) { const validation = ValidationControl(value,config." + cols[i] + "_error_control,config." + cols[i] + "_error_message);if (!validation.isValid) {return this.createError({path: this.path,message: validation.errorMessage});} else {return true;}})";
                    //if (cols[i + 2] != "YES")
                    //    valid += ".required('" + cols[i] + " is required')";
                    valid += ",\n";
                }
            }
            HashSet<string> uniquePairs = new HashSet<string>();
            for (int i = 0; i < FK.Count; i += 4)
            {
                if (uniquePairs.Contains(FK[i] + "|" + FK[i + 1]) || !FKC.Contains(FK[i + 1]))
                    continue;
                uniquePairs.Add(FK[i] + "|" + FK[i + 1]);
                valid += FK[i] + ": yup." + "string" + "()" + ".required('" + FK[i + 1] + " is required'),\n";
            }
            //{CustomerId: yup.number(),PaymentId: yup.number().required('PaymentId is required'),DateCreated: yup.date(),DateShipped: yup.date().required('DateShipped is required'),ShippingId: yup.string().required('ShippingId is required'),Status: yup.string().required('Status is required'),}),}); ";
            text = text.Replace("{yupValidationList}", valid);
            string result = "";
            for (int i = 0; i < cols.Count; i += 3)
            {
                string dataType = utility.GetJSType(cols[i + 1]);
                if (utility.GetvalJSType(cols[i + 1]) == "date")
                    result += cols[i] + ":format(new Date(), \"yyyy-MM-dd\")";
                else if (dataType == "Number")
                    result += cols[i] + ":0";
                else
                    result += cols[i] + ":''";
                if (i != cols.Count - 3)
                    result += ",";
            }
            text = text.Replace("{ColumnListWithValue}", result);
            if (FK.Count != 0)
            {
                string final = "";
                string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                //for (int i = 1; i < FK.Count; i += 4)
                foreach (string s in FKC)
                {
                    //string text1 = template.Replace("{FK}", FK[i]);
                    string text1 = template.Replace("{FK}", s);
                    final += text1;
                }
                if (tbl == "projects_repositories")
                    Console.WriteLine(final);
                ; text = text.Replace("{fkReduxInit}", final);
            }
            else
                text = text.Replace("{fkReduxInit}", "\n");
            string abcd = "";
            //for (int i = 0; i < FK.Count; i += 4)
            foreach (string s in FKC)
            {
                string tableName = s;
                string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                string modelName = char.ToUpper(s[0]) + s.Substring(1);
                string pageNumber = "Constant.defaultPageNumber";
                string pageSize = "Constant.defaultDropdownPageSize";
                string searchKey = "''";
                string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.totalRecords, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                abcd += formControl;
            }
            if (FK.Count == 0)
                text = text.Replace("{useEffectForFK}", "\n");
            else
                text = text.Replace("{useEffectForFK}", abcd);
            text = text.Replace("{action}", "Add");
            string last_replacement = "";
            for (int i = 0; i < cols.Count; i += 3)
            {
                string col = cols[i];
                if (PK.Contains(col))
                    continue;
                if (FK.Contains(col))
                    continue;
                string temp = @"{columnName.includes(""{col}"") && (
<>
                {!config[""{col}_isHidden""] && (
                    <>
                      {config[""{col}_isNewline""] && < Grid xs ={ 12}
                md ={ 12}></ Grid >}
                      < Grid
                        item
                        xs = { config[""{col}_grid_control""] }
                        md ={ config[""{col}_grid_control""]}
                      >
                        < Form.Group >
                          {
                    (() =>
                    {
                    switch (config[""{col}_control""])
                    {
                        case ""rich text editor"":
                            return (

                              <>

                                < InputLabel >
                                      {
                                config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < Field name = ""{col}"" >
                                      { ({ field }) => (
                                        < ReactQuill
                                          value ={ field.value}
                    onChange ={
                        (newValue) =>
                        {
                            formik.setFieldValue(
                              ""{col}"",
                              newValue
                            );
                        }}
                                        />
                                      )}
                                    </Field>
                                  </>
                                );
                              case ""file"":
                                return (
                                  <>
                                    <input
                                      type = { config[""{col}_control""] }
                                      name=""{col}""
                                      key={uniquekey
    }
    id=""{col}""
                                      className=""form-control""
                                      onChange={formik.handleChange
}
onBlur ={ formik.handleBlur}
placeholder ={
    config[""{col}_form_new_name""] !==
    undefined
      ? config[""{col}_form_new_name""]
      : ""{col}""
                                      }
                                    />

                                    < Button
                                      type = ""button""
                                        sx={{my:1}}
                                      className = ""p-1 mb-1 mt-1 d-flex justify-content-center""
                                      variant = ""contained""
                                      onClick ={
    async(event) => {
        var inf =
          document.getElementById(""{col}"");
        await handleFileupload(
          inf,
          formik.setFieldValue,
          ""{col}""
        );
    }
}
disabled ={ isLoading == ""{col}""}
                                    >
                                      {isLoading === ""{col}"" ? (
                                        <CircularProgress
                                          size={24}
                                          color=""inherit""
                                        />
                                      ) : (
                                        ""Upload""
                                      )}
                                    </ Button >
                                  </>
                                );
                              case ""textarea"":
    return (

      < TextareaAutosize
                                    minRows ={ 3}
    name = ""{col}""
                                    key ={ uniquekey}
    id = ""{col}""
                                    className = ""form-control""
                                    value ={
        formik.values[""{col}""]}
    onChange ={ formik.handleChange}
    onBlur ={ formik.handleBlur}
                                  />
                                );
                                case ""signature"":
                              return (
                                < ListItem
                                  sx ={ { justifyContent: ""space-between"" } }
                                >
                                  < InputLabel >
                                    {
                    config[""{col}_form_new_name""] !==
                                    undefined
                                      ? config[""{col}_form_new_name""]
                                      : ""{col}""}
                                  </ InputLabel >
                                  < Button
                                    variant = ""contained""
                                    color = ""primary""
                                    onClick ={ handleOpenSignatureDialog}
                startIcon ={< CloudUploadIcon />}
                                  >
                                    Open Signature Dialog
                                  </ Button >

                                  < SignatureDialog
                                    open ={ openSignatureDialog}
                setFieldValue ={ formik.setFieldValue}
                value ={ ""{col}""}
                config ={ config}
                handleFileupload ={ handleFileupload}
                onClose ={ handleCloseSignatureDialog}
                                  />
                                </ ListItem >
                              );


case ""datetime"":
    return (

      <>

        < InputLabel >
                                      {
        config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < TextField
                                      label ={
        config[""{col}_form_new_name""] !==
        undefined
          ? config[""{col}_form_new_name""]
          : ""{col}""
                                      }
    type = ""datetime-local""
                                      name = ""{col}""
                                      id = ""{col}""
                                      className = ""form-control""
                                      value ={
        moment(formik.values[""{col}""]).format(
                                        ""YYYY-MM-DD hh:mm:ss""
                                      )}
    onChange ={ formik.handleChange}
    onBlur ={ formik.handleBlur}
                                    />
                                  </>
                                );
case ""date"":
    return (

      <>

        < InputLabel >
                                      {
        config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < TextField
                                      type ={
        config[""{col}_control""]
          ? config[""{col}_control""]
          : ""date""
                                      }
    name = ""{col}""
                                      key ={ uniquekey}
    id = ""{col}""
                                      className = ""form-control""
                                      value ={
        moment(formik.values[""{col}""]).format(
                                        ""YYYY-MM-DD""
                                      )}
    onChange ={ formik.handleChange}
    onBlur ={ formik.handleBlur}
                                    />
                                  </>
                                );
default:
    return (

      <>

        < InputLabel >
                                      {
        config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < TextField
                                      type ={
        config[""{col}_control""]
          ? config[""{col}_control""]
          : ""text""
                                      }
    name = ""{col}""
                                      key ={ uniquekey}
    id = ""{col}""
                                      className = ""form-control""
                                      value ={formik.values[""{col}""]}
    onChange ={ formik.handleChange}
    onBlur ={ formik.handleBlur}
                                    />
                                  </>
                                );
}
                          })()}

                          {
    formik.errors.{col} && (
                            < Form.Control.Feedback type = ""invalid"" >
                              { formik.errors.{col}}
                            </ Form.Control.Feedback >
                          )}
                        </ Form.Group >
                      </ Grid >
                    </>
                )}
</>
                  )}";
                temp = temp.Replace("{col}", col);
                last_replacement += temp;
                last_replacement += "\n";
            }
            for (int i = 0; i < FK.Count; i += 4)
            {
                string col1 = FK[i + 3];
                string col2 = FK[i];
                List<string> ans = function(connectionString, FK[i + 1]);
                string temp1 = @"
{
  columnName.includes(""{col}"") && (
<>
{!config[""{col}_isHidden""] && (
  <>
    {config[""{col}_isNewline""] && <Grid xs={12} md={12}></Grid>}
    <Grid item xs={config[""{col}_grid_control""]} md={config[""{col}_grid_control""]}>
      <Form.Group>
        {(() =>
          {
            switch (config[""{col}_control""])
            {
              case ""dropdown"":
                return (
                  <>
                    <label className=""form-control-label"">
                      {config[""{col}_form_new_name""] !== undefined
                        ? config[""{col}_form_new_name""]
                        : ""{col}""}
                    </label>
                    <FormControl
                      component=""select""
                      name=""{col}""
                      className=""form-control""
                      value={formik.values[""{col}""]}
                      onChange={formik.handleChange}
                      onBlur={formik.handleBlur}
                    >
                      <option value={0}>Select {modelName}</option>
                      {{tableName}Data.list.map((item, i) =>
                      {
                        return (
                          <option value={item.{col1}} key={`${tableName}-${i}`}>
                            {config.{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{col}_ref]}
                          </option>
                        );
                      })}
                    </FormControl>
                  </>
                );
              case ""radio"":
                return (
                  <>
                    <InputLabel id=""{col}-label"">
                      {config[""{col}_form_new_name""] !== undefined
                        ? config[""{col}_form_new_name""]
                        : ""{col}""}
                    </InputLabel>
                    <RadioGroup
                      name=""{col}""
                      value={values.{col}}
                      onChange={handleChange}
                      onBlur={handleBlur}
                    >
                      <Grid container>
                        {"" ""}
                        
                        {{tableName}Data.list.map((item, i) => (
                          <Grid
                            item
                            xs={12}
                            sm={6}
                            md={4}
                            lg={3}
                            key={`${tableName}-${i}`}
                          >
                            {"" ""}
                            
                            <FormControlLabel
                              value={item.{col1}}
                              control={<Radio />}
                              label={
                            config.{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{col}_ref]
                          }
                            />
                          </Grid>
                        ))}
                      </Grid>
                    </RadioGroup>
                  </>
                );
              default:
                return (
                  <>
                    <InputLabel id=""{col}-label"">
                      {config[""{col}_form_new_name""] !== undefined
                        ? config[""{col}_form_new_name""]
                        : ""{col}""}
                    </InputLabel>
                    <FormControl
                      component=""select""
                      name=""{col}""
                      className=""form-control""
                      value={formik.values[""{col}""]}
                      onChange={formik.handleChange}
                      onBlur={formik.handleBlur}
                    >
                      <option value={0}>Select {modelName}</option>
                      {{tableName}Data.list.map((item, i) =>
                      {
                        return (
                          <option value={item.{col1}} key={`${tableName}-${i}`}>
                            {config.{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{col}_ref]}
                          </option>
                        );
                      })}
                    </FormControl>
                  </>
                );
            }
          })()}
      </Form.Group>
    </Grid>
  </>
)}
</>
)}";

                // Now the string is properly formatted for readability.

                //                                +@"<Form.Group>
                //<label className=""form-control-label"">{col}</label>
                //<Form.Control as=""select""  name=""{col}"" className=""form-control"" value={formik.formik.values.{col}}
                //onChange={formik.formik.handleChange}
                //onBlur ={formik.formik.handleBlur}
                //isInvalid ={!!formik.touched.{col} && !!formik.formik.errors.{col}}
                //isValid ={!!formik.touched.{col} && !formik.formik.errors.{col}}
                //>
                //<option value={0}>Select {modelName} </option> 
                //{
                //{{tableName}Data.list.map((item, i) => {
                //return <option value={item.{col}} key={`{tableName}-${i}`}>{item.{col}}</option>
                //})}
                //</Form.Control>
                //{
                //    formik.formik.errors.{col} && (
                //    <Form.Control.Feedback type=""invalid"">
                //        {formik.errors.{col}}
                //    </Form.Control.Feedback>
                //)}
                //</Form.Group>"
                temp1 = temp1.Replace("{col}", col1);
                temp1 = temp1.Replace("{col1}", col2);
                temp1 = temp1.Replace("{modelName}", char.ToUpper(FK[i + 1][0]) + FK[i + 1].Substring(1));
                temp1 = temp1.Replace("{tableName}", FK[i + 1]);
                last_replacement += temp1;
                last_replacement += "\n";
            }
            text = text.Replace("{formGroupWithValidation}", last_replacement);
            string PrimaryKeyConversion = "", PrimaryKeyInitialization = "";
            for (int i = 0; i < PK.Count; i++)
            {
                PrimaryKeyInitialization += "values." + PK[i];
                if (i != PK.Count - 1)
                    PrimaryKeyInitialization += ", ";
            }
            for (int i = 0; i < cols.Count; i += 3)
            {
                string dataType = utility.GetJSType(cols[i + 1]);
                if (dataType != "String" && dataType != "Date")
                {
                    if (FK.Contains(cols[i]))
                    {
                        string temp = "";
                        for (int j = 0; j < FK.Count; j++)
                        {
                            if (FK[j].ToLower() == cols[i].ToLower())
                            {
                                if (j % 4 == 0)
                                    temp = FK[j];
                                else if (j % 4 == 3)
                                    temp = FK[j - 3];
                            }
                        }
                        PrimaryKeyConversion += "values." + cols[i] + " = " + dataType + "(values." + temp + ")\n";
                    }
                    else
                        PrimaryKeyConversion += "values." + cols[i] + " = " + dataType + "(values." + cols[i] + ")\n";
                }
            }
            text = text.Replace("{PrimaryKeyConversion}", PrimaryKeyConversion);
            text = text.Replace("{PrimaryKeyInitialization}", PrimaryKeyInitialization);

            return text;
        }

        public string getFormTemplate(string text, string tbl, string src, string des, string server, string uid, string username, string password, string databaseName, string script, string statusOfGeneration, string projectName, string DBexists, string port, string backendChoice, string projectType, string swaggerurl = "")
        {
            string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
            text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
            text = text.Replace("{tableName}", tbl);
            List<string> FK = new List<string>();
            HashSet<string> FKC = new HashSet<string>();
            //using (MySqlConnection connection = new MySqlConnection(connectionString))
            //{
            //    connection.Open();
            MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
            List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
            string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
            MySqlCommand command = new MySqlCommand(sql, connection);
            string foreignKeyColumn = "", refTable = "";
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                    refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                    string dataType = reader["DATA_TYPE"].ToString();
                    string forkey1 = reader["COLUMN_NAME"].ToString();
                    //Kaival - Wrong logic its adding 4 values in array and fetching or incrementing by 4.

                    FK.Add(foreignKeyColumn);
                    FK.Add(refTable);
                    if (refTable != tbl)
                        FKC.Add(refTable);
                    FK.Add(dataType);
                    FK.Add(forkey1);
                }
                if (tbl.ToLower().Equals(("QuestionConditions").ToString().ToLower()))
                {
                    Console.WriteLine(string.Join(",", FK.Select(item => "'" + item + "'")));
                }
            }
            //}
            if (FK.Count != 0)
            {
                string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                string final = "";
                foreach (string s in FKC)
                {
                    //string s = FK[i + 1];
                    string table1 = char.ToUpper(s[0]) + s.Substring(1);
                    string template1 = template.Replace("{table1}", table1);
                    final += template1;
                }
                text = text.Replace("{importFKRedux}", final);
            }
            else
                text = text.Replace("{importFKRedux}", "\n");
            if (FK.Count != 0)
            {
                string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                string final = "";
                //for (int i = 0; i < FK.Count; i += 4)
                foreach (string s in FKC)
                {
                    //string s = FK[i + 1];
                    string table1 = char.ToUpper(s[0]) + s.Substring(1);
                    string FKK = s;// FK[i + 1];
                    string template1 = "";
                    template1 = template.Replace("{table2}", table1);
                    template1 = template1.Replace("{FK}", FKK);
                    final += template1;
                    if (tbl == "projects_repositories")
                        Console.WriteLine(template1);
                }
                text = text.Replace("{importFKService}", final);
            }
            else
                text = text.Replace("{importFKService}", "\n");
            List<string> cols = GetAllColumns(connectionString, tbl);
            List<string> PK = GetPrimaryKey(connectionString, tbl);
            List<string> PrimaryKeyDatatypes = GetPrimaryKeyType(connectionString, tbl);
            string valid = "";
            for (int i = 0; i < cols.Count; i += 3)
            {
                if (FK.Contains(cols[i]))
                { }
                else if (PK.Contains(cols[i]))
                { }
                else
                {
                    valid += cols[i] + ": yup." + "string" + "()" + ".test(\"validator-custom-name\", function (value) { const validation = ValidationControl(value,config." + cols[i] + "_error_control,config." + cols[i] + "_error_message);if (!validation.isValid) {return this.createError({path: this.path,message: validation.errorMessage});} else {return true;}})";
                    //if (cols[i + 2] != "YES")
                    //    valid += ".required('" + cols[i] + " is required')";
                    valid += ",\n";
                }
            }
            HashSet<string> uniquePairs = new HashSet<string>();
            for (int i = 0; i < FK.Count; i += 4)
            {
                
                //Kaival - Below code is not considering all FK and if table has two FK for same table it was not working so added i+3
                //if (uniquePairs.Contains(FK[i] + "|" + FK[i + 1]) || !FKC.Contains(FK[i + 1]))
                //    continue;
                uniquePairs.Add(FK[i] + "|" + FK[i + 1]);
                valid += FK[i+3] + ": yup." + "string" + "()" + ".required('" + FK[i + 3] + " is required'),\n";
                
            }
            //{CustomerId: yup.number(),PaymentId: yup.number().required('PaymentId is required'),DateCreated: yup.date(),DateShipped: yup.date().required('DateShipped is required'),ShippingId: yup.string().required('ShippingId is required'),Status: yup.string().required('Status is required'),}),}); ";
            text = text.Replace("{yupValidationList}", valid);
            string result = "";
            for (int i = 0; i < cols.Count; i += 3)
            {
                if (utility.GetvalJSType(cols[i + 1]) == "date")
                    result += cols[i] + ":format(new Date(), \"yyyy-MM-dd\")";
                else
                    result += cols[i] + ":''";
                if (i != cols.Count - 3)
                    result += ",";
            }
            text = text.Replace("{ColumnListWithValue}", result);
            if (FK.Count != 0)
            {
                string final = "";
                string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                //for (int i = 1; i < FK.Count; i += 4)
                foreach (string s in FKC)
                {
                    //string text1 = template.Replace("{FK}", FK[i]);
                    string text1 = template.Replace("{FK}", s);
                    final += text1;
                }
                if (tbl == "projects_repositories")
                    Console.WriteLine(final);
                ; text = text.Replace("{fkReduxInit}", final);
            }
            else
                text = text.Replace("{fkReduxInit}", "\n");
            string abcd = "";
            //for (int i = 0; i < FK.Count; i += 4)
            foreach (string s in FKC)
            {
                string tableName = s;
                string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                string modelName = char.ToUpper(s[0]) + s.Substring(1);
                string pageNumber = "Constant.defaultPageNumber";
                string pageSize = "Constant.defaultDropdownPageSize";
                string searchKey = "''";
                string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                abcd += formControl;
            }
            if (FK.Count == 0)
                text = text.Replace("{useEffectForFK}", "\n");
            else
                text = text.Replace("{useEffectForFK}", abcd);
            text = text.Replace("{action}", "Add");
            string last_replacement = "";
            for (int i = 0; i < cols.Count; i += 3)
            {
                string col = cols[i];
                if (PK.Contains(col))
                    continue;
                if (FK.Contains(col))
                    continue;
                string temp = @"
{!config[""{col}_isHidden""] && (
                    <>
                      {config[""{col}_isNewline""] && < Grid xs ={ 12}
                md ={ 12}></ Grid >}
                      < Grid
                        item
                        xs = { config[""{col}_grid_control""] }
                        md ={ config[""{col}_grid_control""]}
                      >
                        < Form.Group >
                          {
                    (() =>
                    {
                    switch (config[""{col}_control""])
                    {
                        case ""rich text editor"":
                            return (

                              <>

                                < InputLabel >
                                      {
                                config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < Field name = ""{col}"" >
                                      { ({ field }) => (
                                        < ReactQuill
                                          value ={ field.value}
                    onChange ={
                        (newValue) =>
                        {
                            setFieldValue(
                              ""{col}"",
                              newValue
                            );
                        }}
                                        />
                                      )}
                                    </Field>
                                  </>
                                );
                              case ""file"":
                                return (
                                  <>
                                    <input
                                      type = { config[""{col}_control""] }
                                      name=""{col}""
                                      key={uniquekey
    }
    id=""{col}""
                                      className=""form-control""
                                      onChange={handleChange
}
onBlur ={ handleBlur}
placeholder ={
    config[""{col}_form_new_name""] !==
    undefined
      ? config[""{col}_form_new_name""]
      : ""{col}""
                                      }
                                    />

                                    < Button
                                      type = ""button""
                                       sx={{my:1}}
                                      className = ""p-1 mb-1 mt-1 d-flex justify-content-center""
                                      variant = ""contained""
                                      onClick ={
    async(event) => {
        var inf =
          document.getElementById(""{col}"");
        await handleFileupload(
          inf,
          setFieldValue,
          ""{col}""
        );
    }
}
disabled ={ isLoading == ""{col}""}
                                    >
                                      {isLoading === ""{col}"" ? (
                                        <CircularProgress
                                          size={24}
                                          color=""inherit""
                                        />
                                      ) : (
                                        ""Upload""
                                      )}
                                    </ Button >
                                  </>
                                );
                              case ""textarea"":
    return (

      < TextareaAutosize
                                    minRows ={ 3}
    name = ""{col}""
                                    key ={ uniquekey}
    id = ""{col}""
                                    className = ""form-control""
                                    value ={
        values.{col}
                                    }
    onChange ={ handleChange}
    onBlur ={ handleBlur}
                                  />
                                );
case ""datetime"":
    return (

      <>

        < InputLabel >
                                      {
        config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < TextField
                                      label ={
        config[""{col}_form_new_name""] !==
        undefined
          ? config[""{col}_form_new_name""]
          : ""{col}""
                                      }
    type = ""datetime-local""
                                      name = ""{col}""
                                      id = ""{col}""
                                      className = ""form-control""
                                      value ={
        moment(values[""{col}""]).format(
                                        ""YYYY-MM-DD hh:mm:ss""
                                      )}
    onChange ={ handleChange}
    onBlur ={ handleBlur}
                                    />
                                  </>
                                );
                                                               case ""signature"":
                              return (
                                < ListItem
                                  sx ={ { justifyContent: ""space-between"" } }
                                >
                                  < InputLabel >
                                    {
                    config[""{col}_form_new_name""] !==
                                    undefined
                                      ? config[""{col}_form_new_name""]
                                      : ""{col}""}
                                  </ InputLabel >
                                  < Button
                                    variant = ""contained""
                                    color = ""primary""
                                    onClick ={ handleOpenSignatureDialog}
                startIcon ={< CloudUploadIcon />}
                                  >
                                    Open Signature Dialog
                                  </ Button >

                                  < SignatureDialog
                                    open ={ openSignatureDialog}
                setFieldValue ={ setFieldValue}
                value ={ ""{col}""}
                config ={ config}
                handleFileupload ={ handleFileupload}
                onClose ={ handleCloseSignatureDialog}
                                  />
                                </ ListItem >
                              );


case ""date"":
    return (

      <>

        < InputLabel >
                                      {
        config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < TextField
                                      type ={
        config[""{col}_control""]
          ? config[""{col}_control""]
          : ""date""
                                      }
    name = ""{col}""
                                      key ={ uniquekey}
    id = ""{col}""
                                      className = ""form-control""
                                      value ={
        moment(values[""{col}""]).format(
                                        ""YYYY-MM-DD""
                                      )}
    onChange ={ handleChange}
    onBlur ={ handleBlur}
                                    />
                                  </>
                                );
default:
    return (

      <>

        < InputLabel >
                                      {
        config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < TextField
                                      type ={
        config[""{col}_control""]
          ? config[""{col}_control""]
          : ""text""
                                      }
    name = ""{col}""
                                      key ={ uniquekey}
    id = ""{col}""
                                      className = ""form-control""
                                      value ={values[""{col}""]}
    onChange ={ handleChange}
    onBlur ={ handleBlur}
                                    />
                                  </>
                                );
}
                          })()}

                          {
    errors.{col} && (
                            < Form.Control.Feedback type = ""invalid"" >
                              { errors.{col}}
                            </ Form.Control.Feedback >
                          )}
                        </ Form.Group >
                      </ Grid >
                    </>
)}
                  ";
                temp = temp.Replace("{col}", col);
                last_replacement += temp;
                last_replacement += "\n";
            }
            for (int i = 0; i < FK.Count; i += 4)
            {
                string col1 = FK[i + 3];
                string col2 = FK[i];
                List<string> ans = function(connectionString, FK[i + 1]);
                string temp1 = @"
{!config[""{col}_isHidden""] && (
  <>
    {config[""{col}_isNewline""] && <Grid xs={12} md={12}></Grid>}
    <Grid item xs={config[""{col}_grid_control""]} md={config[""{col}_grid_control""]}>
      <Form.Group>
        {(() =>
          {
            switch (config[""{col}_control""])
            {
              case ""dropdown"":
                return (
                  <>
                    <InputLabel id=""{col}-label"">
                      {config[""{col}_form_new_name""] !== undefined
                        ? config[""{col}_form_new_name""]
                        : ""{col}""}
                    </InputLabel>
                    <FormControl fullWidth>
                      <Select
                        labelId=""{col}-label""
                        id=""{col}-select""
                        value={values.{col}}
                        name=""{col}""
                        onChange={handleChange}
                        onBlur={handleBlur}
                      >
                        <MenuItem value={0}>Select {modelName}</MenuItem>
                        {{tableName}Data.list.map((item, i) => (
                          <MenuItem
                            value={item.{col1}}
                            key={`${tableName}-${i}`}
                          >
                            {config.{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{col}_ref]}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </>
                );
              case ""radio"":
                return (
                  <>
                    <InputLabel id=""{col}-label"">
                      {config[""{col}_form_new_name""] !== undefined
                        ? config[""{col}_form_new_name""]
                        : ""{col}""}
                    </InputLabel>
                    <RadioGroup
                      name=""{col}""
                      value={values.{col}}
                      onChange={handleChange}
                      onBlur={handleBlur}
                    >
                      <Grid container>
                        {"" ""}
                        
                        {{tableName}Data.list.map((item, i) => (
                          <Grid
                            item
                            xs={12}
                            sm={6}
                            md={4}
                            lg={3}
                            key={`${tableName}-${i}`}
                          >
                            {"" ""}
                           
                            <FormControlLabel
                              value={item.{col1}}
                              control={<Radio />}
                              label={
                            config.{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{col}_ref]
                          }
                            />
                          </Grid>
                        ))}
                      </Grid>
                    </RadioGroup>
                  </>
                );
              default:
                return (
                  <>
                    <InputLabel id=""{col}-label"">
                      {config[""{col}_form_new_name""] !== undefined
                        ? config[""{col}_form_new_name""]
                        : ""{col}""}
                    </InputLabel>
                    <FormControl fullWidth>
                      <Select
                        labelId=""{col}-label""
                        id=""{col}-select""
                        value={values.{col}}
                        name=""{col}""
                        onChange={handleChange}
                        onBlur={handleBlur}
                      >
                        <MenuItem value={0}>Select {modelName}</MenuItem>
                        {{tableName}Data.list.map((item, i) => (
                          <MenuItem
                            value={item.{col1}}
                            key={`${tableName}-${i}`}
                          >
                            {config.{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{col}_ref]}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </>
                );
            }
          })()}
      </Form.Group>
    </Grid>
  </>
)}
";

                //                                +@"<Form.Group>
                //<label className=""form-control-label"">{col}</label>
                //<Form.Control as=""select""  name=""{col}"" className=""form-control"" value={formik.values.{col}}
                //onChange={formik.handleChange}
                //onBlur ={formik.handleBlur}
                //isInvalid ={!!formik.touched.{col} && !!formik.errors.{col}}
                //isValid ={!!formik.touched.{col} && !formik.errors.{col}}
                //>
                //<option value={0}>Select {modelName} </option> 
                //{
                //{{tableName}Data.list.map((item, i) => {
                //return <option value={item.{col}} key={`{tableName}-${i}`}>{item.{col}}</option>
                //})}
                //</Form.Control>
                //{
                //    formik.errors.{col} && (
                //    <Form.Control.Feedback type=""invalid"">
                //        {formik.errors.{col}}
                //    </Form.Control.Feedback>
                //)}
                //</Form.Group>"
                temp1 = temp1.Replace("{col}", col1);
                temp1 = temp1.Replace("{col1}", col2);
                temp1 = temp1.Replace("{modelName}", char.ToUpper(FK[i + 1][0]) + FK[i + 1].Substring(1));
                temp1 = temp1.Replace("{tableName}", FK[i + 1]);
                last_replacement += temp1;
                last_replacement += "\n";
            }
            text = text.Replace("{formGroupWithValidation}", last_replacement);
            string PrimaryKeyConversion = "", PrimaryKeyInitialization = "";
            for (int i = 0; i < PK.Count; i++)
            {
                PrimaryKeyInitialization += "values." + PK[i];
                if (i != PK.Count - 1)
                    PrimaryKeyInitialization += ", ";
            }
            for (int i = 0; i < cols.Count; i += 3)
            {
                string dataType = utility.GetJSType(cols[i + 1]);
                if (dataType != "String" && dataType != "Date")
                {   //Kaival - Below code is creating issue.
                    //if (FK.Contains(cols[i]))
                    //{
                    //    string temp = "";
                    //    for (int j = 0; j < FK.Count; j++)
                    //    {
                    //        if (FK[j].ToLower() == cols[i].ToLower())
                    //        {
                    //            if (j % 4 == 0)
                    //                temp = FK[j];
                    //            else if (j % 4 == 3)
                    //                temp = FK[j - 3];
                    //        }
                    //    }
                    //    PrimaryKeyConversion += "values." + cols[i] + " = " + dataType + "(values." + temp + ")\n";
                    //}
                    //else
                    //Kaival - Pass null if its 0 and database table column is nullable and FK.
                    if (cols[i + 2] == "YES")
                    {
                        PrimaryKeyConversion += "values." + cols[i] + " = " + dataType + "(values." + cols[i] + ")  === 0 ? null : "+ dataType + "(values." + cols[i] + ") \n";
                    }
                    else
                    {
                        PrimaryKeyConversion += "values." + cols[i] + " = " + dataType + "(values." + cols[i] + ")\n";
                    }
                   
                    PrimaryKeyConversion += "values." + cols[i] + " = " + dataType + "(values." + cols[i] + ")\n";
                }
            }
            text = text.Replace("{PrimaryKeyConversion}", PrimaryKeyConversion);
            text = text.Replace("{PrimaryKeyInitialization}", PrimaryKeyInitialization);

            return text;
        }
        public string getComponentTemplate(string text, string viewName, string tbl, string src, string des, string server, string uid, string username, string password, string databaseName, string script, string statusOfGeneration, string projectName, string DBexists, string port, string backendChoice, string projectType, string swaggerurl = "")
        {
            string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
            text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
            text = text.Replace("{tableName}", tbl).Replace("{viewName}", viewName);
            List<string> FK = new List<string>();
            HashSet<string> FKC = new HashSet<string>();
            //using (MySqlConnection connection = new MySqlConnection(connectionString))
            //{
            //    connection.Open();
            MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
            List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
            string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
            MySqlCommand command = new MySqlCommand(sql, connection);
            string foreignKeyColumn = "", refTable = "";
            if (tbl.ToLower().Equals(("QuestionConditions").ToString().ToLower()))
            {
                Console.WriteLine(string.Join(",", FK.Select(item => "'" + item + "'")));
            }
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                    refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                    string dataType = reader["DATA_TYPE"].ToString();
                    string forkey1 = reader["COLUMN_NAME"].ToString();
                    FK.Add(foreignKeyColumn);
                    if (refTable != tbl)
                        FKC.Add(refTable);
                    FK.Add(refTable);
                    FK.Add(dataType);
                    FK.Add(forkey1);
                }
            }
            //}
            if (FK.Count != 0)
            {
                string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                string final = "";
                //for (int i = 0; i < FK.Count; i += 4)
                foreach (string s in FKC)
                {
                    //string s = FK[i + 1];
                    string table1 = char.ToUpper(s[0]) + s.Substring(1);
                    string template1 = template.Replace("{table1}", table1);
                    final += template1;
                }
                text = text.Replace("{importFKRedux}", final);
            }
            else
                text = text.Replace("{importFKRedux}", "\n");
            if (FK.Count != 0)
            {
                string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                string final = "";
                //for (int i = 0; i < FK.Count; i += 4)
                foreach (string s in FKC)
                {
                    //string s = FK[i + 1];
                    string table1 = char.ToUpper(s[0]) + s.Substring(1);
                    string FKK = s;// FK[i + 1];
                    string template1 = "";
                    template1 = template.Replace("{table2}", table1);
                    template1 = template1.Replace("{FK}", FKK);
                    final += template1;
                    if (tbl == "projects_repositories")
                        Console.WriteLine(template1);
                }
                text = text.Replace("{importFKService}", final);
            }
            else
                text = text.Replace("{importFKService}", "\n");
            if (FK.Count != 0)
            {
                string final = "";
                string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                //for (int i = 1; i < FK.Count; i += 4)
                foreach (string s in FKC)
                {
                    //string text1 = template.Replace("{FK}", FK[i]);
                    string text1 = template.Replace("{FK}", s);
                    final += text1;
                }
                if (tbl == "projects_repositories")
                    Console.WriteLine(final);
                ; text = text.Replace("{fkReduxInit}", final);
            }
            else
                text = text.Replace("{fkReduxInit}", "\n");
            string abcd = "";
            //for (int i = 0; i < FK.Count; i += 4)
            foreach (string s in FKC)
            {
                string tableName = s;//FK[i + 1];
                string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                string modelName = char.ToUpper(s[0]) + s.Substring(1);
                string pageNumber = "Constant.defaultPageNumber";
                string pageSize = "Constant.defaultDropdownPageSize";
                string searchKey = "''";
                string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                abcd += formControl;
            }
            if (FK.Count == 0)
                text = text.Replace("{useEffectForFK}", "\n");
            else
                text = text.Replace("{useEffectForFK}", abcd);
            List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
            string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
            string tableCols = "";
            for (int i = 0; i < columns.Count; i += 3)
            {
                bool temp = false; int j = 0;
                for (j = 0; j < FK.Count; j += 4)
                {
                    if (FK[j+3] == columns[i])
                    {
                        temp = true;
                        break;
                    }
                }
                if (temp)
                    tableCols += "{field:\"" + columns[i] + "\",headerName: config." + columns[i] + "_new_name ? config." + columns[i] + "_new_name:'" + columns[i] + "',sortable: true,renderCell:(params) => { const element = " + FK[j + 1] + "Data.list.find((element) => element." + FK[j] + "=== params.row." + columns[i] + ");return (<span> {element ? config[\"" + columns[i] + "_ref\"] === undefined? params.row." + columns[i] + ": element[config[\"" + columns[i] + "_ref\"]] :\"\"} </span>); } ,hide: config.hasOwnProperty(\"" + columns[i] + "_visible\") ? !config." + columns[i] + "_visible: false,flex:1},\n";
                else
                    tableCols += "{field:\"" + columns[i] + "\",headerName: config." + columns[i] + "_new_name ? config." + columns[i] + "_new_name:'" + columns[i] + "',sortable: true,hide: config.hasOwnProperty(\"" + columns[i] + "_visible\") ? !config." + columns[i] + "_visible: false,flex:1,renderCell: (params) => (<RowSelectorComponent inputType={config[\"" + columns[i] + "_control\"]} field={params.row." + columns[i] + "} display_control={config[\"" + columns[i] + "_display_control\"]} bucket_id={config[\"" + columns[i] + "_bucket_name\"]} bucket_folder={config[\"" + columns[i] + "_bucket_folder\"]} />),},\n";
            }
            text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{tableColumn}", tableCols);

            return text;
        }
        public string getOneTransactionalFormTemplate(string text, string tbl, string src, string des, string server, string uid, string username, string password, string databaseName, string script, string statusOfGeneration, string projectName, string DBexists, string port, string backendChoice, string projectType, string swaggerurl = "")
        {
            string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
            text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
            text = text.Replace("{tableName}", tbl);
            List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
            string PrimaryKeyConversion = "";
            string final1 = "";
            string final2 = "";
            string valid = "";
            string result = "";
            string final3 = "";
            string abcd = "";
            string last_element1 = "";
            List<string> unqTable = new List<string>();
            unqTable.Add(tbl);
            foreach (var referencingTable in Program.Transactional[tbl].Sequence)
            {
                string referencingTableSmall = referencingTable.ToLower();
                string referencingTableTmp = char.ToUpper(referencingTableSmall[0]) + referencingTableSmall.Substring(1);
                List<string> FK1 = new List<string>();
                HashSet<string> FKC1 = new HashSet<string>();
                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                //{
                //    connection.Open();
                MySqlConnection connection1 = MySqlConnectionManager.Instance.GetConnection();
                List<string> PKTemp1 = GetPrimaryKey(connectionString, referencingTableSmall);
                string sql1 = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + referencingTable + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                MySqlCommand command1 = new MySqlCommand(sql1, connection1);
                string foreignKeyColumn1 = "", refTable1 = "";
                using (MySqlDataReader reader = command1.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        foreignKeyColumn1 = reader["REFERENCED_COLUMN_NAME"].ToString();
                        refTable1 = reader["REFERENCED_TABLE_NAME"].ToString();
                        string dataType = reader["DATA_TYPE"].ToString();
                        string forkey1 = reader["COLUMN_NAME"].ToString();
                        FK1.Add(foreignKeyColumn1);
                        FK1.Add(refTable1);
                        if (refTable1 != referencingTableSmall)
                            FKC1.Add(refTable1);
                        FK1.Add(dataType);
                        FK1.Add(forkey1);
                    }
                }
                //}
                if (FK1.Count != 0)
                {
                    string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                    string template1 = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                    string template2 = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                    foreach (string s in FKC1)
                    {
                        if (unqTable.Contains(s))
                            continue;
                        //string s = FK[i + 1];
                        string table1 = char.ToUpper(s[0]) + s.Substring(1);
                        string template11 = template.Replace("{table1}", table1);
                        final1 += template11;
                        string FKK = s;
                        template11 = template1.Replace("{table2}", table1);
                        template11 = template11.Replace("{FK}", FKK);
                        final2 += template11;
                        string text1 = template2.Replace("{FK}", s);
                        final3 += text1;
                        string tableName = s;
                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                        string pageNumber = "Constant.defaultPageNumber";
                        string pageSize = "Constant.defaultDropdownPageSize";
                        string searchKey = "''";
                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                        abcd += formControl;
                        unqTable.Add(s);
                    }
                }

                List<string> cols1 = GetAllColumns(connectionString, referencingTableSmall);
                List<string> PK1 = GetPrimaryKey(connectionString, referencingTableSmall);
                List<string> PrimaryKeyDatatypes1 = GetPrimaryKeyType(connectionString, referencingTableSmall);
                if (Program.Transactional[tbl].Relations[referencingTable] == "many")
                {
                    valid += referencingTableSmall + ": yup.array().of(yup.object().shape({";
                    for (int i = 0; i < cols1.Count; i += 3)
                    {
                        if (FK1.Contains(cols1[i]))
                        { }
                        else if (PK1.Contains(cols1[i]))
                        { }
                        else
                        {
                            valid += cols1[i] + ": yup." + "string" + "()" + ".test(\"validator-custom-name\", function (value) { const validation = ValidationControl(value,config." + cols1[i] + "_error_control,config." + cols1[i] + "_error_message);if (!validation.isValid) {return this.createError({path: this.path,message: validation.errorMessage});} else {return true;}})";
                            valid += ",\n";
                        }
                    }
                    if (Program.Transactional[tbl].Relations[referencingTable] == "many")
                    {
                        PrimaryKeyConversion += "values." + referencingTableSmall + ".forEach((temp) => {";
                        for (int i = 0; i < cols1.Count; i += 3)
                        {
                            if (PKTemp.Contains(cols1[i]) || PKTemp1.Contains(cols1[i]))
                                continue;
                            string dataType1 = utility.GetJSType(cols1[i + 1]);
                            if (dataType1 != "String" && dataType1 != "Date")
                            {
                                if (FK1.Contains(cols1[i]))
                                {
                                    string temp = "";
                                    for (int j = 0; j < FK1.Count; j++)
                                    {
                                        if (FK1[j].ToLower() == cols1[i].ToLower())
                                        {
                                            if (j % 4 == 0)
                                                temp = FK1[j];
                                            else if (j % 4 == 3)
                                                temp = FK1[j - 3];
                                        }
                                    }
                                    PrimaryKeyConversion += "temp." + cols1[i] + " = " + dataType1 + "(temp." + temp + ")\n";
                                }
                                else
                                    PrimaryKeyConversion += "temp." + cols1[i] + " = " + dataType1 + "(temp." + cols1[i] + ")\n";
                            }
                        }
                        PrimaryKeyConversion += "});\n";
                    }
                    else
                    {
                        for (int i = 0; i < cols1.Count; i += 3)
                        {
                            if (PKTemp.Contains(cols1[i]) || PKTemp1.Contains(cols1[i]))
                                continue;
                            string dataType1 = utility.GetJSType(cols1[i + 1]);
                            if (dataType1 != "String" && dataType1 != "Date")
                            {
                                if (FK1.Contains(cols1[i]))
                                {
                                    string temp = "";
                                    for (int j = 0; j < FK1.Count; j++)
                                    {
                                        if (FK1[j].ToLower() == cols1[i].ToLower())
                                        {
                                            if (j % 4 == 0)
                                                temp = FK1[j];
                                            else if (j % 4 == 3)
                                                temp = FK1[j - 3];
                                        }
                                    }
                                    PrimaryKeyConversion += "values." + referencingTableSmall + "." + cols1[i] + " = " + dataType1 + "(values." + referencingTableSmall + "." + temp + ")\n";
                                }
                                else
                                    PrimaryKeyConversion += "values." + referencingTableSmall + "." + cols1[i] + " = " + dataType1 + "(values." + referencingTableSmall + "." + cols1[i] + ")\n";
                            }
                        }
                    }
                    HashSet<string> uniquePairs1 = new HashSet<string>();
                    for (int i = 0; i < FK1.Count; i += 4)
                    {
                        if (uniquePairs1.Contains(FK1[i] + "|" + FK1[i + 1]) || !FKC1.Contains(FK1[i + 1]) || PKTemp.Contains(FK1[i]))
                            continue;
                        uniquePairs1.Add(FK1[i] + "|" + FK1[i + 1]);
                        valid += FK1[i] + ": yup." + "string" + "()" + ".required('" + FK1[i + 1] + " is required'),\n";
                    }
                    valid += "  })),";
                    result += referencingTableSmall + ": [ {";
                    for (int i = 0; i < cols1.Count; i += 3)
                    {
                        if (PKTemp.Contains(cols1[i]) || PKTemp1.Contains(cols1[i]))
                            continue;
                        if (utility.GetvalJSType(cols1[i + 1]) == "date")
                            result += cols1[i] + ":format(new Date(), \"yyyy-MM-dd\")";
                        else
                            result += cols1[i] + ":''";
                        if (i != cols1.Count - 3)
                            result += ",";
                    }
                    result += "},],";

                    string le1 = @"              <Grid item xs={12}>
                < Typography variant = ""h5"" component = ""h2"" sx ={ { mb: 2 } }>
                  {
                    action === ""add""
                    ? ""Add {modelName}""
                    : ""Edit {modelName}""}
                </ Typography >
                < FieldArray name = ""{tableName}"" >
                  {
                    ({ push, remove }) => ( <div>
                    {values.{tableName}.map((data, index) =>
                    {
                        return (
  
                          < div >";
                    for (int i = 0; i < cols1.Count; i += 3)
                    {
                        string col = cols1[i];
                        if (PK1.Contains(col))
                            continue;
                        if (FK1.Contains(col))
                            continue;
                        string temp = @"
    <>
        {config[""{ModelName}_{col}_isNewline""] && <Grid xs={12} md={12}></Grid>}
        <Grid
            item
            xs={config[""{ModelName}_{col}_grid_control""]}
            md={config[""{ModelName}_{col}_grid_control""]}
        >
            <Form.Group>
                {(() =>
                {
                    switch (config[""{ModelName}_{col}_control""])
                    {
                        case ""rich text editor"":
                            return (
                                <>
                                    <InputLabel>
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                                            ? config[""{ModelName}_{col}_form_new_name""]
                                            : ""{col}""}
                                    </InputLabel>
                                    <Field name=""{col}"">
                                        {({ field }) => (
                                            <ReactQuill
                                                value={field.value}
                                    onChange={(event) => {
                                    const newValue = event;
                                    setFieldValue(
                                      `{TableName}[${index}].{col}`,
                                      newValue
                                    );
                                  }}
                                            />
                                        )}
                                    </Field>
                                </>
                            );
                        case ""file"":
                            return (
                                <>
                                    <input
                                        type={config[""{ModelName}_{col}_control""]}
                                        name=""{col}""
                                        key={uniquekey}
                                        id=""{col}${index}""
                                        className=""form-control""
                                        //onChange={handleChange}
                                        onBlur={handleBlur}
                                        placeholder={
                                            config[""{ModelName}_{col}_form_new_name""] !== undefined
                                                ? config[""{ModelName}_{col}_form_new_name""]
                                                : ""{col}""
                                        }
                                    />
                                    <Button
                                        type=""button""
                                        sx={{my:1}}
                                        className=""p-1 mb-1 mt-1 d-flex justify-content-center""
                                        variant=""contained""
                                        onClick={async (event) => {
                                            var inf = document.getElementById(""{col}${index}"");
                                            await handleFileupload(inf, setFieldValue, ""{col}"");
                                        }}
                                        disabled={isLoading == ""{col}""}
                                    >
                                        {isLoading == ""{col}"" ? ""Uploading..."" : ""Upload""}
                                    </Button>
                                </>
                            );
                        case ""textarea"":
                            return (
                                <TextareaAutosize
                                    minRows={3}
                                    name=""{col}""
                                    key={uniquekey}
                                    id=""{col}""
                                    className=""form-control""
                                    value={values.{TableName}[index].{col}}
                                    onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}[${index}].{col}`,
                                      newValue
                                    );
                                  }}
                                    onBlur={handleBlur}
                                />
                            );
                        case ""datetime"":
                            return (
                                <>
                                    <InputLabel>
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                                            ? config[""{ModelName}_{col}_form_new_name""]
                                            : ""{col}""}
                                    </InputLabel>
                                    <TextField
                                        label={
                                            config[""{ModelName}_{col}_form_new_name""] !== undefined
                                                ? config[""{ModelName}_{col}_form_new_name""]
                                                : ""{col}""
                                        }
                                        type=""datetime-local""
                                        name=""{col}""
                                        id=""{col}""
                                        className=""form-control""
                                        value={moment(values.{TableName}[index].{col}).format(""YYYY-MM-DD hh:mm:ss"")}
                                    onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}[${index}].{col}`,
                                      newValue
                                    );
                                  }}
                                        onBlur={handleBlur}
                                    />
                                </>
                            );
                        case ""signature"":
                            return (
                                <ListItem sx={{ justifyContent: ""space-between"" }}>
                                    <InputLabel>
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                                            ? config[""{ModelName}_{col}_form_new_name""]
                                            : ""{col}""}
                                    </InputLabel>
                                    <Button
                                        variant=""contained""
                                        color=""primary""
                                        onClick={handleOpenSignatureDialog}
                                        startIcon={<CloudUploadIcon />}
                                    >
                                        Open Signature Dialog
                                    </Button>
                                    <SignatureDialog
                                        open={openSignatureDialog}
                                        setFieldValue={setFieldValue}
                                        value={values.{TableName}[index].{col}}
                                        config={config}
                                        handleFileupload={handleFileupload}
                                        onClose={handleCloseSignatureDialog}
                                    />
                                </ListItem>
                            );
                        case ""date"":
                            return (
                                <>
                                    <InputLabel>
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                                            ? config[""{ModelName}_{col}_form_new_name""]
                                            : ""{col}""}
                                    </InputLabel>
                                    <TextField
                                        type={
                                            config[""{ModelName}_{col}_control""]
                                                ? config[""{ModelName}_{col}_control""]
                                                : ""date""
                                        }
                                        name=""{col}""
                                        key={uniquekey}
                                        id=""{col}""
                                        className=""form-control""
                                        value={moment(values.{TableName}[index].{col}).format(""YYYY-MM-DD"")}
                                       onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}[${index}].{col}`,
                                      newValue
                                    );
                                  }}
                                        onBlur={handleBlur}
                                    />
                                </>
                            );
                        default:
                            return (
                                <>
                                    <InputLabel>
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                                            ? config[""{ModelName}_{col}_form_new_name""]
                                            : ""{col}""}
                                    </InputLabel>
                                    <TextField
                                        type={
                                            config[""{ModelName}_{col}_control""]
                                                ? config[""{ModelName}_{col}_control""]
                                                : ""text""
                                        }
                                        name=""{col}""
                                        key={uniquekey}
                                        id=""{col}""
                                        className=""form-control""
                                        value={values.{TableName}[index].{col}}
                                      onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}[${index}].{col}`,
                                      newValue
                                    );
                                  }}
                                        onBlur={handleBlur}
                                    />
                                </>
                            );
                    }
                })()}
                {errors.{col} && (
                    <Form.Control.Feedback type=""invalid"">
                        {errors.{col}}
                    </Form.Control.Feedback>
                )}
            </Form.Group>
        </Grid>
    </>
";

                        temp = temp.Replace("{col}", col).Replace("{ModelName}", referencingTableTmp);
                        string tblr = temp.Replace("{TableName}", referencingTableSmall);
                        le1 += tblr;
                        le1 += "\n";
                    }
                    for (int i = 0; i < FK1.Count; i += 4)
                    {
                        if (PKTemp.Contains(FK1[i]))
                            continue;
                        string col1 = FK1[i + 3];
                        string col2 = FK1[i];
                        List<string> ans = function(connectionString, FK1[i + 1]);
                        string temp1 = @"
  <>
    {config[""{ModelName}_{col}_isNewline""] && <Grid xs={12} md={12}></Grid>}
    <Grid item xs={config[""{ModelName}_{col}_grid_control""]} md={config[""{ModelName}_{col}_grid_control""]}>
      <Form.Group>
        {(() =>
          {
            switch (config[""{ModelName}_{col}_control""])
            {
              case ""dropdown"":
                return (
                  <>
                    <label className=""form-control-label"">
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                    ? config[""{ModelName}_{col}_form_new_name""]
                    : config.{ModelName}_{col}_ref === undefined
                    ? ""{col}""
                    : config.{ModelName}_{col}_ref}
                    </label>
                    <FormControl
                      component=""select""
                      name=""{col}""
                      className=""form-control""
                       value={
                                      values.{TableName}[index][
                                        ""{col}""
                                      ]
                                    }
                                onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}[${index}].{col}`,
                                      newValue
                                    );
                                  }}
                      onBlur={handleBlur}
                    >
                      <option value={0}>Select {modelName}</option>
                      {{tableName}Data.list.map((item, i) =>
                      {
                        return (
                          <option value={item.{col1}} key={`${tableName}-${i}`}>
                            {config.{ModelName}_{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{ModelName}_{col}_ref]}
                          </option>
                        );
                      })}
                    </FormControl>
                  </>
                );
              case ""radio"":
                return (
                  <>
                    {{tableName}Data.list.map((item, i) =>
                    {
                      return (
                        <FormControlLabel
                          control={
                            <Radio
                              value={item.{col1}}
                              name=""{col}""
                              className=""form-control""
                              onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}[${index}].{col}`,
                                      newValue
                                    );
                                  }}
                              onBlur={handleBlur}
                              checked={item.{col1} === values.{TableName}[index].{col} ? true : null}
                            />
                          }
                          label={
                            config.{ModelName}_{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{ModelName}_{col}_ref]
                          }
                          key={`${tableName}-${i}`}
                        />
                      );
                    })}
                  </>
                );
              default:
                return (
                  <>
                    <label className=""form-control-label"">
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                    ? config[""{ModelName}_{col}_form_new_name""]
                    : config.{ModelName}_{col}_ref === undefined
                    ? ""{col}""
                    : config.{ModelName}_{col}_ref}
                    </label>
                    <FormControl
                      component=""select""
                      name=""{col}""
                      className=""form-control""
                      value={
                                      values.{TableName}[index][
                                        ""{col}""
                                      ]
                                    }
                                onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}[${index}].{col}`,
                                      newValue
                                    );
                                  }}
                      onBlur={handleBlur}
                    >
                      <option value={0}>Select {modelName}</option>
                      {{tableName}Data.list.map((item, i) =>
                      {
                        return (
                          <option value={item.{col1}} key={`${tableName}-${i}`}>
                            {config.{ModelName}_{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{ModelName}_{col}_ref]}
                          </option>
                        );
                      })}
                    </FormControl>
                  </>
                );
            }
          })()}
          {errors.{TableName} && errors.{TableName}[index] && errors.{TableName}[index][""{col}""] && (
                  <Form.Control.Feedback type=""invalid"">
                    {errors.{TableName} && errors.{TableName}[index] && errors.{TableName}[index][""{col}""]}
                  </Form.Control.Feedback>
                )}
      </Form.Group>
    </Grid>
  </>";
                        temp1 = temp1.Replace("{col}", col1);
                        temp1 = temp1.Replace("{col1}", col2);
                        temp1 = temp1.Replace("{modelName}", char.ToUpper(FK1[i + 1][0]) + FK1[i + 1].Substring(1));
                        temp1 = temp1.Replace("{tableName}", FK1[i + 1]);
                        temp1 = temp1.Replace("{TableName}", referencingTableSmall);
                        le1 += temp1;
                        le1 = le1.Replace("{ModelName}", referencingTableTmp);
                        le1 = le1.Replace("{TableName}", referencingTableSmall);
                    }
                    le1 += @"{index > 0 && (
                              <Grid
                                item
                                xs={12}
                                sx={{  marginTop: -2, marginBottom: -2,marginRight : -2  }}
                              >
                                <Box display=""flex"" justifyContent=""flex-end"">
                                  <Tooltip title=""Remove Item"">
                                    <IconButton
                                      onClick={() => remove(index)}
                                      color=""secondary""
                                      aria-label=""Remove Social Media Account""
                                    >
                                      <RemoveCircle />
                                    </IconButton>
                                  </Tooltip>
                                </Box>
                              </Grid>
                            )}
                          </div>
                      );
                    })}
                      <Button
                        type=""button""
                        onClick={() =>
                          push({";
                    for (int i = 0; i < cols1.Count; i += 3)
                    {
                        if (PKTemp.Contains(cols1[i]) || PKTemp1.Contains(cols1[i]))
                            continue;
                        if (utility.GetvalJSType(cols1[i + 1]) == "date")
                            le1 += cols1[i] + ":format(new Date(), \"yyyy-MM-dd\")";
                        else
                            le1 += cols1[i] + ":''";
                        if (i != cols1.Count - 3)
                            le1 += ",";
                    }
                    le1 += @"})
                        }
                        variant=""outlined""
                        color=""primary""
                        startIcon={<AddCircle />}
                      >
                        Add New Item
                      </Button>
                    </div>
                  )}
                </FieldArray>
              </Grid>";
                    le1 += "\n";
                    le1 = le1.Replace("{tableName}", referencingTableSmall).Replace("{modelName}", referencingTableTmp);
                    le1 = le1.Replace("{ModelName}", referencingTableTmp);
                    le1 = le1.Replace("{TableName}", referencingTableSmall);
                    last_element1 += le1;
                }
                else
                {
                    valid += referencingTableSmall + ": yup.object().shape({";
                    for (int i = 0; i < cols1.Count; i += 3)
                    {
                        if (FK1.Contains(cols1[i]))
                        { }
                        else if (PK1.Contains(cols1[i]))
                        { }
                        else
                        {
                            valid += cols1[i] + ": yup." + "string" + "()" + ".test(\"validator-custom-name\", function (value) { const validation = ValidationControl(value,config." + cols1[i] + "_error_control,config." + cols1[i] + "_error_message);if (!validation.isValid) {return this.createError({path: this.path,message: validation.errorMessage});} else {return true;}})";
                            valid += ",\n";
                        }
                    }
                    HashSet<string> uniquePairs1 = new HashSet<string>();
                    for (int i = 0; i < FK1.Count; i += 4)
                    {
                        if (uniquePairs1.Contains(FK1[i] + "|" + FK1[i + 1]) || !FKC1.Contains(FK1[i + 1]) || PKTemp.Contains(FK1[i]))
                            continue;
                        uniquePairs1.Add(FK1[i] + "|" + FK1[i + 1]);
                        valid += FK1[i] + ": yup." + "string" + "()" + ".required('" + FK1[i + 1] + " is required'),\n";
                    }
                    valid += "  }),";
                    result += referencingTableSmall + ": {";
                    for (int i = 0; i < cols1.Count; i += 3)
                    {
                        if (PKTemp.Contains(cols1[i]) || PKTemp1.Contains(cols1[i]))
                            continue;
                        if (utility.GetvalJSType(cols1[i + 1]) == "date")
                            result += cols1[i] + ":format(new Date(), \"yyyy-MM-dd\")";
                        else
                            result += cols1[i] + ":''";
                        if (i != cols1.Count - 3)
                            result += ",";
                    }
                    result += "},";

                    string le1 = @"              <Grid item xs={12}>
                < Typography variant = ""h5"" component = ""h2"" sx ={ { mb: 2 } }>
                  {
                    action === ""add""
                    ? ""Add {modelName}""
                    : ""Edit {modelName}""}
                </ Typography >
  
                          < div >";
                    for (int i = 0; i < cols1.Count; i += 3)
                    {
                        string col = cols1[i];
                        if (PK1.Contains(col))
                            continue;
                        if (FK1.Contains(col))
                            continue;
                        string temp = @"
    <>
        {config[""{ModelName}_{col}_isNewline""] && <Grid xs={12} md={12}></Grid>}
        <Grid
            item
            xs={config[""{ModelName}_{col}_grid_control""]}
            md={config[""{ModelName}_{col}_grid_control""]}
        >
            <Form.Group>
                {(() =>
                {
                    switch (config[""{ModelName}_{col}_control""])
                    {
                        case ""rich text editor"":
                            return (
                                <>
                                    <InputLabel>
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                                            ? config[""{ModelName}_{col}_form_new_name""]
                                            : ""{col}""}
                                    </InputLabel>
                                    <Field name=""{col}"">
                                        {({ field }) => (
                                            <ReactQuill
                                                value={field.value}
                                    onChange={(event) => {
                                    const newValue = event;
                                    setFieldValue(
                                      `{TableName}.{col}`,
                                      newValue
                                    );
                                  }}
                                            />
                                        )}
                                    </Field>
                                </>
                            );
                        case ""file"":
                            return (
                                <>
                                    <input
                                        type={config[""{ModelName}_{col}_control""]}
                                        name=""{col}""
                                        key={uniquekey}
                                        id=""{col}${index}""
                                        className=""form-control""
                                        //onChange={handleChange}
                                        onBlur={handleBlur}
                                        placeholder={
                                            config[""{ModelName}_{col}_form_new_name""] !== undefined
                                                ? config[""{ModelName}_{col}_form_new_name""]
                                                : ""{col}""
                                        }
                                    />
                                    <Button
                                        type=""button""
                                        sx={{my:1}}
                                        className=""p-1 mb-1 mt-1 d-flex justify-content-center""
                                        variant=""contained""
                                        onClick={async (event) => {
                                            var inf = document.getElementById(""{col}${index}"");
                                            await handleFileupload(inf, setFieldValue, ""{col}"");
                                        }}
                                        disabled={isLoading == ""{col}""}
                                    >
                                        {isLoading == ""{col}"" ? ""Uploading..."" : ""Upload""}
                                    </Button>
                                </>
                            );
                        case ""textarea"":
                            return (
                                <TextareaAutosize
                                    minRows={3}
                                    name=""{col}""
                                    key={uniquekey}
                                    id=""{col}""
                                    className=""form-control""
                                    value={values.{TableName}[""{col}""]}
                                    onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}.{col}`,
                                      newValue
                                    );
                                  }}
                                    onBlur={handleBlur}
                                />
                            );
                        case ""datetime"":
                            return (
                                <>
                                    <InputLabel>
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                                            ? config[""{ModelName}_{col}_form_new_name""]
                                            : ""{col}""}
                                    </InputLabel>
                                    <TextField
                                        label={
                                            config[""{ModelName}_{col}_form_new_name""] !== undefined
                                                ? config[""{ModelName}_{col}_form_new_name""]
                                                : ""{col}""
                                        }
                                        type=""datetime-local""
                                        name=""{col}""
                                        id=""{col}""
                                        className=""form-control""
                                        value={moment(values.{TableName}[""{col}""]).format(""YYYY-MM-DD hh:mm:ss"")}
                                    onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}.{col}`,
                                      newValue
                                    );
                                  }}
                                        onBlur={handleBlur}
                                    />
                                </>
                            );
                        case ""signature"":
                            return (
                                <ListItem sx={{ justifyContent: ""space-between"" }}>
                                    <InputLabel>
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                                            ? config[""{ModelName}_{col}_form_new_name""]
                                            : ""{col}""}
                                    </InputLabel>
                                    <Button
                                        variant=""contained""
                                        color=""primary""
                                        onClick={handleOpenSignatureDialog}
                                        startIcon={<CloudUploadIcon />}
                                    >
                                        Open Signature Dialog
                                    </Button>
                                    <SignatureDialog
                                        open={openSignatureDialog}
                                        setFieldValue={setFieldValue}
                                        value={values.{TableName}[""{col}""]}
                                        config={config}
                                        handleFileupload={handleFileupload}
                                        onClose={handleCloseSignatureDialog}
                                    />
                                </ListItem>
                            );
                        case ""date"":
                            return (
                                <>
                                    <InputLabel>
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                                            ? config[""{ModelName}_{col}_form_new_name""]
                                            : ""{col}""}
                                    </InputLabel>
                                    <TextField
                                        type={
                                            config[""{ModelName}_{col}_control""]
                                                ? config[""{ModelName}_{col}_control""]
                                                : ""date""
                                        }
                                        name=""{col}""
                                        key={uniquekey}
                                        id=""{col}""
                                        className=""form-control""
                                        value={moment(values.{TableName}[""{col}""]).format(""YYYY-MM-DD"")}
                                       onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}.{col}`,
                                      newValue
                                    );
                                  }}
                                        onBlur={handleBlur}
                                    />
                                </>
                            );
                        default:
                            return (
                                <>
                                    <InputLabel>
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                                            ? config[""{ModelName}_{col}_form_new_name""]
                                            : ""{col}""}
                                    </InputLabel>
                                    <TextField
                                        type={
                                            config[""{ModelName}_{col}_control""]
                                                ? config[""{ModelName}_{col}_control""]
                                                : ""text""
                                        }
                                        name=""{col}""
                                        key={uniquekey}
                                        id=""{col}""
                                        className=""form-control""
                                        value={values.{TableName}[""{col}""]}
                                      onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}.{col}`,
                                      newValue
                                    );
                                  }}
                                        onBlur={handleBlur}
                                    />
                                </>
                            );
                    }
                })()}
                {errors.{col} && (
                    <Form.Control.Feedback type=""invalid"">
                        {errors.{col}}
                    </Form.Control.Feedback>
                )}
            </Form.Group>
        </Grid>
    </>
";

                        temp = temp.Replace("{col}", col);
                        le1 += temp;
                        le1 += "\n";
                    }
                    for (int i = 0; i < FK1.Count; i += 4)
                    {
                        if (PKTemp.Contains(FK1[i]))
                            continue;
                        string col1 = FK1[i + 3];
                        string col2 = FK1[i];
                        List<string> ans = function(connectionString, FK1[i + 1]);
                        string temp1 = @"
  <>
    {config[""{ModelName}_{col}_isNewline""] && <Grid xs={12} md={12}></Grid>}
    <Grid item xs={config[""{ModelName}_{col}_grid_control""]} md={config[""{ModelName}_{col}_grid_control""]}>
      <Form.Group>
        {(() =>
          {
            switch (config[""{ModelName}_{col}_control""])
            {
              case ""dropdown"":
                return (
                  <>
                    <label className=""form-control-label"">
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                    ? config[""{ModelName}_{col}_form_new_name""]
                    : config.{ModelName}_{col}_ref === undefined
                    ? ""{col}""
                    : config.{ModelName}_{col}_ref}
                    </label>
                    <FormControl
                      component=""select""
                      name=""{col}""
                      className=""form-control""
                       value={
                                      values.{TableName}[
                                        ""{col}""
                                      ]
                                    }
                                onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}.{col}`,
                                      newValue
                                    );
                                  }}
                      onBlur={handleBlur}
                    >
                      <option value={0}>Select {modelName}</option>
                      {{tableName}Data.list.map((item, i) =>
                      {
                        return (
                          <option value={item.{col1}} key={`${tableName}-${i}`}>
                            {config.{ModelName}_{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{ModelName}_{col}_ref]}
                          </option>
                        );
                      })}
                    </FormControl>
                  </>
                );
              case ""radio"":
                return (
                  <>
                    {{tableName}Data.list.map((item, i) =>
                    {
                      return (
                        <FormControlLabel
                          control={
                            <Radio
                              value={item.{col1}}
                              name=""{col}""
                              className=""form-control""
                              onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}.{col}`,
                                      newValue
                                    );
                                  }}
                              onBlur={handleBlur}
                              checked={item.{col1} === values.{TableName}[""{col}""] ? true : null}
                            />
                          }
                          label={
                            config.{ModelName}_{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{ModelName}_{col}_ref]
                          }
                          key={`${tableName}-${i}`}
                        />
                      );
                    })}
                  </>
                );
              default:
                return (
                  <>
                    <label className=""form-control-label"">
                                        {config[""{ModelName}_{col}_form_new_name""] !== undefined
                    ? config[""{ModelName}_{col}_form_new_name""]
                    : config.{ModelName}_{col}_ref === undefined
                    ? ""{col}""
                    : config.{ModelName}_{col}_ref}
                    </label>
                    <FormControl
                      component=""select""
                      name=""{col}""
                      className=""form-control""
                      value={
                                      values.{TableName}[
                                        ""{col}""
                                      ]
                                    }
                                onChange={(event) => {
                                    const newValue = event.target.value;
                                    setFieldValue(
                                      `{TableName}.{col}`,
                                      newValue
                                    );
                                  }}
                      onBlur={handleBlur}
                    >
                      <option value={0}>Select {modelName}</option>
                      {{tableName}Data.list.map((item, i) =>
                      {
                        return (
                          <option value={item.{col1}} key={`${tableName}-${i}`}>
                            {config.{ModelName}_{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{ModelName}_{col}_ref]}
                          </option>
                        );
                      })}
                    </FormControl>
                  </>
                );
            }
          })()}
          {errors.{TableName} && errors.{TableName} && errors.{TableName}[""{col}""] && (
                  <Form.Control.Feedback type=""invalid"">
                    {errors.{TableName} && errors.{TableName} && errors.{TableName}[""{col}""]}
                  </Form.Control.Feedback>
                )}
      </Form.Group>
    </Grid>
  </>";
                        temp1 = temp1.Replace("{col}", col1);
                        temp1 = temp1.Replace("{col1}", col2);
                        temp1 = temp1.Replace("{modelName}", char.ToUpper(FK1[i + 1][0]) + FK1[i + 1].Substring(1));
                        temp1 = temp1.Replace("{tableName}", FK1[i + 1]).Replace("{TableName}", referencingTableSmall);
                        le1 += temp1;
                    }
                    le1 += @"
                          </div>
              </Grid>";
                    le1 += "\n";
                    le1 = le1.Replace("{tableName}", referencingTableSmall).Replace("{modelName}", referencingTableTmp);
                    string tempName = le1.Replace("{ModelName}", referencingTableTmp);
                    string tempName1 = tempName.Replace("{TableName}", referencingTableSmall);
                    last_element1 += tempName1;
                }
                //for (int i = 0; i < FK.Count; i += 4)
            }
            text = text.Replace("{subFormGroupWithValidation}", last_element1);
            List<string> FK = new List<string>();
            HashSet<string> FKC = new HashSet<string>();
            //using (MySqlConnection connection = new MySqlConnection(connectionString))
            //{
            //    connection.Open();
            MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
            string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
            MySqlCommand command = new MySqlCommand(sql, connection);
            string foreignKeyColumn = "", refTable = "";
            using (MySqlDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                    refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                    string dataType = reader["DATA_TYPE"].ToString();
                    string forkey1 = reader["COLUMN_NAME"].ToString();
                    FK.Add(foreignKeyColumn);
                    FK.Add(refTable);
                    if (refTable != tbl)
                        FKC.Add(refTable);
                    FK.Add(dataType);
                    FK.Add(forkey1);
                }
            }
            //}
            if (FK.Count != 0)
            {
                string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                foreach (string s in FKC)
                {
                    if (unqTable.Contains(s))
                        continue;
                    //string s = FK[i + 1];
                    string table1 = char.ToUpper(s[0]) + s.Substring(1);
                    string template1 = template.Replace("{table1}", table1);
                    final1 += template1;
                }
                text = text.Replace("{importFKRedux}", final1);
            }
            else
                text = text.Replace("{importFKRedux}", "\n");
            if (FK.Count != 0)
            {
                string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                //for (int i = 0; i < FK.Count; i += 4)
                foreach (string s in FKC)
                {
                    if (unqTable.Contains(s))
                        continue;
                    //string s = FK[i + 1];
                    string table1 = char.ToUpper(s[0]) + s.Substring(1);
                    string FKK = s;// FK[i + 1];
                    string template1 = "";
                    template1 = template.Replace("{table2}", table1);
                    template1 = template1.Replace("{FK}", FKK);
                    final2 += template1;
                    if (tbl == "projects_repositories")
                        Console.WriteLine(template1);
                }
                text = text.Replace("{importFKService}", final2);
            }
            else
                text = text.Replace("{importFKService}", "\n");
            List<string> cols = GetAllColumns(connectionString, tbl);
            List<string> PK = GetPrimaryKey(connectionString, tbl);
            List<string> PrimaryKeyDatatypes = GetPrimaryKeyType(connectionString, tbl);
            for (int i = 0; i < cols.Count; i += 3)
            {
                if (FK.Contains(cols[i]))
                { }
                else if (PK.Contains(cols[i]))
                { }
                else
                {
                    valid += cols[i] + ": yup." + "string" + "()" + ".test(\"validator-custom-name\", function (value) { const validation = ValidationControl(value,config." + cols[i] + "_error_control,config." + cols[i] + "_error_message);if (!validation.isValid) {return this.createError({path: this.path,message: validation.errorMessage});} else {return true;}})";
                    //if (cols[i + 2] != "YES")
                    //    valid += ".required('" + cols[i] + " is required')";
                    valid += ",\n";
                }
            }
            HashSet<string> uniquePairs = new HashSet<string>();
            for (int i = 0; i < FK.Count; i += 4)
            {
                if (uniquePairs.Contains(FK[i] + "|" + FK[i + 1]) || !FKC.Contains(FK[i + 1]))
                    continue;
                uniquePairs.Add(FK[i] + "|" + FK[i + 1]);
                valid += FK[i] + ": yup." + "string" + "()" + ".required('" + FK[i + 1] + " is required'),\n";
            }
            //{CustomerId: yup.number(),PaymentId: yup.number().required('PaymentId is required'),DateCreated: yup.date(),DateShipped: yup.date().required('DateShipped is required'),ShippingId: yup.string().required('ShippingId is required'),Status: yup.string().required('Status is required'),}),}); ";
            text = text.Replace("{yupValidationList}", valid);
            for (int i = 0; i < cols.Count; i += 3)
            {
                if (PK.Contains(cols[i]))
                    continue;
                if (utility.GetvalJSType(cols[i + 1]) == "date")
                    result += cols[i] + ":format(new Date(), \"yyyy-MM-dd\")";
                else
                    result += cols[i] + ":''";
                if (i != cols.Count - 3)
                    result += ",";
            }
            text = text.Replace("{ColumnListWithValue}", result);
            if (FK.Count != 0)
            {
                string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                //for (int i = 1; i < FK.Count; i += 4)
                foreach (string s in FKC)
                {
                    if (unqTable.Contains(s))
                        continue;
                    //string text1 = template.Replace("{FK}", FK[i]);
                    string text1 = template.Replace("{FK}", s);
                    final3 += text1;
                }
                ; text = text.Replace("{fkReduxInit}", final3);
            }
            else
                text = text.Replace("{fkReduxInit}", "\n");
            //for (int i = 0; i < FK.Count; i += 4)
            foreach (string s in FKC)
            {
                if (unqTable.Contains(s))
                    continue;
                string tableName = s;
                string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                string modelName = char.ToUpper(s[0]) + s.Substring(1);
                string pageNumber = "Constant.defaultPageNumber";
                string pageSize = "Constant.defaultDropdownPageSize";
                string searchKey = "''";
                string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                abcd += formControl;
            }
            if (FK.Count == 0)
                text = text.Replace("{useEffectForFK}", "\n");
            else
                text = text.Replace("{useEffectForFK}", abcd);
            text = text.Replace("{action}", "Add");
            string last_replacement = "";
            for (int i = 0; i < cols.Count; i += 3)
            {
                string col = cols[i];
                if (PK.Contains(col))
                    continue;
                if (FK.Contains(col))
                    continue;
                string temp = @"
                    <>
                      {config[""{col}_isNewline""] && < Grid xs ={ 12}
                md ={ 12}></ Grid >}
                      < Grid
                        item
                        xs = { config[""{col}_grid_control""] }
                        md ={ config[""{col}_grid_control""]}
                      >
                        < Form.Group >
                          {
                    (() =>
                    {
                    switch (config[""{col}_control""])
                    {
                        case ""signature"":
                              return (
                                < ListItem
                                  sx ={ { justifyContent: ""space-between"" } }
                                >
                                  < InputLabel >
                                    {
                    config[""{col}_form_new_name""] !==
                                    undefined
                                      ? config[""{col}_form_new_name""]
                                      : ""{col}""}
                                  </ InputLabel >
                                  < Button
                                    variant = ""contained""
                                    color = ""primary""
                                    onClick ={ handleOpenSignatureDialog}
                startIcon ={< CloudUploadIcon />}
                                  >
                                    Open Signature Dialog
                                  </ Button >

                                  < SignatureDialog
                                    open ={ openSignatureDialog}
                setFieldValue ={ setFieldValue}
                value ={ ""{col}""}
                config ={ config}
                handleFileupload ={ handleFileupload}
                onClose ={ handleCloseSignatureDialog}
                                  />
                                </ ListItem >
                              );
                        case ""rich text editor"":
                            return (

                              <>

                                < InputLabel >
                                      {
                                config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < Field name = ""{col}"" >
                                      { ({ field }) => (
                                        < ReactQuill
                                          value ={ field.value}
                    onChange ={
                        (newValue) =>
                        {
                            setFieldValue(
                              ""{col}"",
                              newValue
                            );
                        }}
                                        />
                                      )}
                                    </Field>
                                  </>
                                );
                              case ""file"":
                                return (
                                  <>
                                    <input
                                      type = { config[""{col}_control""] }
                                      name=""{col}""
                                      key={uniquekey
    }
    id=""{col}""
                                      className=""form-control""
                                      onChange={handleChange
}
onBlur ={ handleBlur}
placeholder ={
    config[""{col}_form_new_name""] !==
    undefined
      ? config[""{col}_form_new_name""]
      : ""{col}""
                                      }
                                    />

                                    < Button
                                      type = ""button""
                                        sx={{my:1}}
                                      className = ""p-1 mb-1 mt-1 d-flex justify-content-center""
                                      variant = ""contained""
                                      onClick ={
    async(event) => {
        var inf =
          document.getElementById(""{col}"");
        await handleFileupload(
          inf,
          setFieldValue,
          ""{col}""
        );
    }
}
disabled ={ isLoading == ""{col}""}
                                    >
                                      {isLoading === ""{col}"" ? (
                                        <CircularProgress
                                          size={24}
                                          color=""inherit""
                                        />
                                      ) : (
                                        ""Upload""
                                      )}
                                    </ Button >
                                  </>
                                );
                              case ""textarea"":
    return (

      < TextareaAutosize
                                    minRows ={ 3}
    name = ""{col}""
                                    key ={ uniquekey}
    id = ""{col}""
                                    className = ""form-control""
                                    value ={values.{col}}
    onChange ={ handleChange}
    onBlur ={ handleBlur}
                                  />
                                );
case ""datetime"":
    return (

      <>

        < InputLabel >
                                      {
        config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < TextField
                                      label ={
        config[""{col}_form_new_name""] !==
        undefined
          ? config[""{col}_form_new_name""]
          : ""{col}""
                                      }
    type = ""datetime-local""
                                      name = ""{col}""
                                      id = ""{col}""
                                      className = ""form-control""
                                      value ={
        moment(values[""{col}""]).format(
                                        ""YYYY-MM-DD hh:mm:ss""
                                      )}
    onChange ={ handleChange}
    onBlur ={ handleBlur}
                                    />
                                  </>
                                );
case ""date"":
    return (

      <>

        < InputLabel >
                                      {
        config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < TextField
                                      type ={
        config[""{col}_control""]
          ? config[""{col}_control""]
          : ""date""
                                      }
    name = ""{col}""
                                      key ={ uniquekey}
    id = ""{col}""
                                      className = ""form-control""
                                      value ={
        moment(values[""{col}""]).format(
                                        ""YYYY-MM-DD""
                                      )}
    onChange ={ handleChange}
    onBlur ={ handleBlur}
                                    />
                                  </>
                                );
default:
    return (

      <>

        < InputLabel >
                                      {
        config[""{col}_form_new_name""] !==
                                      undefined
                                        ? config[""{col}_form_new_name""]
                                        : ""{col}""}
                                    </ InputLabel >
                                    < TextField
                                      type ={
        config[""{col}_control""]
          ? config[""{col}_control""]
          : ""text""
                                      }
    name = ""{col}""
                                      key ={ uniquekey}
    id = ""{col}""
                                      className = ""form-control""
                                      value ={values[""{col}""]}
    onChange ={ handleChange}
    onBlur ={ handleBlur}
                                    />
                                  </>
                                );
}
                          })()}

                          {
    errors.{col} && (
                            < Form.Control.Feedback type = ""invalid"" >
                              { errors.{col}}
                            </ Form.Control.Feedback >
                          )}
                        </ Form.Group >
                      </ Grid >
                    </>
                  ";

                temp = temp.Replace("{col}", col);
                last_replacement += temp;
                last_replacement += "\n";
            }
            for (int i = 0; i < FK.Count; i += 4)
            {
                string col1 = FK[i + 3];
                string col2 = FK[i];
                List<string> ans = function(connectionString, FK[i + 1]);
                string temp1 = @"
  <>
    {config[""{col}_isNewline""] && <Grid xs={12} md={12}></Grid>}
    <Grid item xs={config[""{col}_grid_control""]} md={config[""{col}_grid_control""]}>
      <Form.Group>
        {(() =>
          {
            switch (config[""{col}_control""])
            {
              case ""dropdown"":
                return (
                  <>
                    <InputLabel id=""{col}-label"">
                      {config[""{col}_form_new_name""] !== undefined
                        ? config[""{col}_form_new_name""]
                        : ""{col}""}
                    </InputLabel>
                    <FormControl fullWidth>
                      <Select
                        labelId=""{col}-label""
                        id=""{col}-select""
                        value={values.{col}}
                        name=""{col}""
                        onChange={handleChange}
                        onBlur={handleBlur}
                      >
                        <MenuItem value={0}>Select {modelName}</MenuItem>
                        {{tableName}Data.list.map((item, i) => (
                          <MenuItem
                            value={item.{col1}}
                            key={`${tableName}-${i}`}
                          >
                            {config.{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{col}_ref]}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </>
                );
              case ""radio"":
                return (
                  <>
                    <InputLabel id=""{col}-label"">
                      {config[""{col}_form_new_name""] !== undefined
                        ? config[""{col}_form_new_name""]
                        : ""{col}""}
                    </InputLabel>
                    <RadioGroup
                      name=""{col}""
                      value={values.{col}}
                      onChange={handleChange}
                      onBlur={handleBlur}
                    >
                      <Grid container>
                        {"" ""}
                        
                        {{tableName}Data.list.map((item, i) => (
                          <Grid
                            item
                            xs={12}
                            sm={6}
                            md={4}
                            lg={3}
                            key={`${tableName}-${i}`}
                          >
                            {"" ""}
                            
                            <FormControlLabel
                              value={item.{col1}}
                              control={<Radio />}
                              label={
                            config.{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{col}_ref]
                          }
                            />
                          </Grid>
                        ))}
                      </Grid>
                    </RadioGroup>
                  </>
                );
              default:
                return (
                  <>
                    <InputLabel id=""{col}-label"">
                      {config[""{col}_form_new_name""] !== undefined
                        ? config[""{col}_form_new_name""]
                        : ""{col}""}
                    </InputLabel>
                    <FormControl fullWidth>
                      <Select
                        labelId=""{col}-label""
                        id=""{col}-select""
                        value={values.{col}}
                        name=""{col}""
                        onChange={handleChange}
                        onBlur={handleBlur}
                      >
                        <MenuItem value={0}>Select {modelName}</MenuItem>
                        {{tableName}Data.list.map((item, i) => (
                          <MenuItem
                            value={item.{col1}}
                            key={`${tableName}-${i}`}
                          >
                            {config.{col}_ref === undefined
                              ? item[""{col1}""]
                              : item[config.{col}_ref]}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </>
                );
            }
          })()}
      </Form.Group>
    </Grid>
  </>";
                //                                +@"<Form.Group>
                //<label className=""form-control-label"">{col}</label>
                //<Form.Control component=""select""  name=""{col}"" className=""form-control"" value={formik.values.{col}}
                //onChange={formik.handleChange}
                //onBlur ={formik.handleBlur}
                //isInvalid ={!!formik.touched.{col} && !!formik.errors.{col}}
                //isValid ={!!formik.touched.{col} && !formik.errors.{col}}
                //>
                //<option value={0}>Select {modelName} </option> 
                //{
                //{{tableName}Data.list.map((item, i) => {
                //return <option value={item.{col}} key={`{tableName}-${i}`}>{item.{col}}</option>
                //})}
                //</Form.Control>
                //{
                //    formik.errors.{col} && (
                //    <Form.Control.Feedback type=""invalid"">
                //        {formik.errors.{col}}
                //    </Form.Control.Feedback>
                //)}
                //</Form.Group>"
                temp1 = temp1.Replace("{col}", col1);
                temp1 = temp1.Replace("{col1}", col2);
                temp1 = temp1.Replace("{modelName}", char.ToUpper(FK[i + 1][0]) + FK[i + 1].Substring(1));
                temp1 = temp1.Replace("{tableName}", FK[i + 1]);
                last_replacement += temp1;
                last_replacement += "\n";
            }
            text = text.Replace("{formGroupWithValidation}", last_replacement);
            string PrimaryKeyInitialization = "";
            for (int i = 0; i < PK.Count; i++)
            {
                PrimaryKeyInitialization += "values." + PK[i];
                if (i != PK.Count - 1)
                    PrimaryKeyInitialization += ", ";
            }
            for (int i = 0; i < cols.Count; i += 3)
            {
                if (PK.Contains(cols[i]))
                    continue;
                string dataType = utility.GetJSType(cols[i + 1]);
                if (dataType != "String" && dataType != "Date")
                {
                    if (FK.Contains(cols[i]))
                    {
                        string temp = "";
                        for (int j = 0; j < FK.Count; j++)
                        {
                            if (FK[j].ToLower() == cols[i].ToLower())
                            {
                                if (j % 4 == 0)
                                    temp = FK[j];
                                else if (j % 4 == 3)
                                    temp = FK[j - 3];
                            }
                        }
                        PrimaryKeyConversion += "values." + cols[i] + " = " + dataType + "(values." + temp + ")\n";
                    }
                    else
                        PrimaryKeyConversion += "values." + cols[i] + " = " + dataType + "(values." + cols[i] + ")\n";
                }
            }
            text = text.Replace("{PrimaryKeyConversion}", PrimaryKeyConversion);
            text = text.Replace("{PrimaryKeyInitialization}", PrimaryKeyInitialization);

            return text;
        }
        public Dictionary<string, string> CreateMap(List<ComponentMapping> mappings)
        {
            // Create a dictionary for quick lookups
            Dictionary<string, string> tableToComponentMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Populate the dictionary
            foreach (var mapping in mappings)
            {
                tableToComponentMap[mapping.associated_table.ToLower()] = mapping.Component;
            }

            return tableToComponentMap;
        }

        public string FindAssociatedComponent(Dictionary<string, string> tableToComponentMap, string table)
        {
            Console.WriteLine(table);
            // Use the dictionary for quick lookups
            if (tableToComponentMap.TryGetValue(table, out var associatedComponent))
            {
                Console.WriteLine(associatedComponent);
                return associatedComponent;
            }

            // Return null if no match is found
            return null;
        }
        public string ProcessInput(string value)
        {
            string result;

            switch (value)
            {
                case "DefaultView":
                    result = "DefaultView";
                    // Perform actions for option 1
                    break;
                case "TableView1":
                    result = "TableView1";
                    // Perform actions for option 1
                    break;
                case "TableView2":
                    result = "TableView2";
                    // Perform actions for option 1
                    break;
                case "TableView3":
                    result = "TableView3";
                    // Perform actions for option 1
                    break;
                case "TableView4":
                    result = "TableView4";
                    // Perform actions for option 1
                    break;
                case "TableView6":
                    result = "TableView6";
                    // Perform actions for option 1
                    break;
                case "TableView7":
                    result = "TableView7";
                    // Perform actions for option 1
                    break;
                case "TableView8":
                    result = "TableView8";
                    // Perform actions for option 1
                    break;
                case "TableView9":
                    result = "TableView9";
                    // Perform actions for option 1
                    break;
                case "TableView10":
                    result = "TableView10";
                    // Perform actions for option 1
                    break;
                case "TableView11":
                    result = "TableView11";
                    // Perform actions for option 1
                    break;

                case "Grid_view_1":
                    result = "GridView1";
                    // Perform actions for option 2
                    break;
                case "Grid_view_2":
                    result = "GridView2";
                    // Perform actions for option 2
                    break;
                case "Grid_view_3":
                    result = "GridView3";
                    // Perform actions for option 2
                    break;
                case "Grid_view_4":
                    result = "GridView4";
                    // Perform actions for option 2
                    break;
                case "Grid_view_5":
                    result = "GridView5";
                    // Perform actions for option 2
                    break;
                case "Grid_view_6":
                    result = "GridView6";
                    // Perform actions for option 2
                    break;


                case "Grouped_list_1":
                    result = "GroupList1";
                    // Perform actions for option 2
                    break;
                case "Grouped_list_2":
                    result = "GroupList2";
                    // Perform actions for option 2
                    break;
                case "Grouped_list_3":
                    result = "GroupList3";
                    // Perform actions for option 2
                    break;
                case "Grouped_list_4":
                    result = "GroupList4";
                    // Perform actions for option 2
                    break;
                case "Grouped_list_5":
                    result = "GroupList5";
                    // Perform actions for option 2
                    break;
                case "Grouped_list_6":
                    result = "GroupList6";
                    // Perform actions for option 2
                    break;
                case "Grouped_list_7":
                    result = "GroupList7";
                    // Perform actions for option 2
                    break;
                case "Grouped_list_8":
                    result = "GroupList8";
                    // Perform actions for option 2
                    break;
                case "Grouped_list_9":
                    result = "GroupList9";
                    // Perform actions for option 2
                    break;
                case "Grouped_list_10":
                    result = "GroupList10";
                    // Perform actions for option 2
                    break;
                case "Detail_list_1":
                    result = "DetailList1";
                    // Perform actions for option 2
                    break;
                case "Detail_list_2":
                    result = "DetailList2";
                    // Perform actions for option 2
                    break;
                case "Detail_list_3":
                    result = "DetailList3";
                    // Perform actions for option 2
                    break;
                case "Detail_list_4":
                    result = "DetailList4";
                    // Perform actions for option 2
                    break;
                case "Detail_list_5":
                    result = "DetailList5";
                    // Perform actions for option 2
                    break;
                case "Detail_list_6":
                    result = "DetailList6";
                    // Perform actions for option 2
                    break;
                case "Detail_list_7":
                    result = "DetailList7";
                    // Perform actions for option 2
                    break;
                case "Calendar":
                    result = "Calendar";
                    // Perform actions for option 2
                    break;

                default:
                    result = value;
                    // Perform actions for invalid option
                    break;
            }

            return result;
        }

        public string GenerateViewMapToTables(List<string> tables,Dictionary<string, string> tableToComponentMap)
        {
            // Create a StringBuilder to build the React TypeScript code
            StringBuilder codeBuilder = new StringBuilder();

            // Start the function definition
            codeBuilder.AppendLine("export const getViewList = (tableName) => {");
            codeBuilder.AppendLine("  switch (tableName) {");

            // Generate case statements for each mapping
            foreach (var tbl1 in tables)
            {
                if(FindAssociatedComponent(tableToComponentMap, tbl1) != null)
                {
                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                    {
                        continue;
                    }
                    string tablename = char.ToLower(tbl1[0]) + tbl1.Substring(1);
                    codeBuilder.AppendLine($"    case \"{tablename}\":");
                    string component = ProcessInput(FindAssociatedComponent(tableToComponentMap, tbl1));
                    component = char.ToLower(component[0]) + component.Substring(1);
                    codeBuilder.AppendLine($"      return [\"{component}\", \"defaultView\"];");
                    codeBuilder.AppendLine("      break;");
                }
                else
                {
                    string tablename = char.ToLower(tbl1[0]) + tbl1.Substring(1);
                    codeBuilder.AppendLine($"    case \"{tablename}\":");
                    codeBuilder.AppendLine($"      return [\"defaultView\"];");
                    codeBuilder.AppendLine("      break;");
                }
                
            }

            // Close the switch and the function
            codeBuilder.AppendLine("    default:");
            codeBuilder.AppendLine("      return [\"defaultView\"];");
            codeBuilder.AppendLine("  }");
            codeBuilder.AppendLine("};");

            // Create a variable declaration for getViewList
            //codeBuilder.AppendLine("export const getViewList = [");
            //codeBuilder.AppendLine("  // Add your components here as needed");
            //codeBuilder.AppendLine("];");

            // Return the generated code as a string
            return codeBuilder.ToString();
        }


        public void ProcessFile(string src, string des, string server, string uid, string username, string password, string databaseName, string script, string statusOfGeneration, string projectName, string DBexists, string port, string backendChoice, string projectType, string front_template_json = "", string swaggerurl = "")
        {
            List<string> tables = GetAllTables(des, uid, username, password, databaseName, script, port, DBexists, server);
            if (projectType == "nodered")
            {
                try
                {
                    var files = Directory.GetFiles(@src);
                    foreach (string file in files)
                    {
                        List<List<string>> temp_storage = new List<List<string>>();
                        string lastElement = Path.GetFileName(file);
                        //Console.WriteLine(filename);
                        Console.WriteLine(lastElement);
                        int wkid = 0;
                        if (lastElement == "GET_ALL.txt")
                        {
                            string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                            string temptext = File.ReadAllText(file);
                            string final = "[";
                            int xcod = 100, ycod = 20;
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                List<string> FK = new List<string>();
                                List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                //HashSet<string> FKC = new HashSet<string>();
                                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                //{
                                //    connection.Open();
                                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                MySqlCommand command = new MySqlCommand(sql, connection);
                                string foreignKeyColumn = "", refTable = "";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                        refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                        string dataType = reader["DATA_TYPE"].ToString();
                                        string forkey1 = reader["COLUMN_NAME"].ToString();
                                        FK.Add(foreignKeyColumn);
                                        FK.Add(refTable);
                                        //if (refTable != tbl)
                                        //    FKC.Add(refTable);
                                        FK.Add(dataType);
                                        FK.Add(forkey1);
                                    }
                                }
                                //}
                                final += temptext.Replace("{{UNIQUE_ID_FOR_SUBFLOW_GETALL}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_GETALL_1}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_GETALL_2}}", (wkid++).ToString())
                                    .Replace("{{UINQUE_ID_FOR_HTTP_GETALL}}", (wkid++).ToString());
                                final += ",";

                                string temptext2 = File.ReadAllText(src + "/HTTP_IN_POST.txt");
                                final += temptext2
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_HTTP_IN_POST}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_HTTP_NODE_HTTP_IN_POST}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_HTTP_IN_POST}}", (wkid++).ToString());
                                final += ",";
                                string temptext3 = File.ReadAllText(src + "/POST.txt");
                                final += temptext3
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_POST}}", (wkid++).ToString()).Replace("{{UNIQUE_ID_FOR_SUBFLOW_HTTP_IN_POST}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_POST}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_HTTP_POST}}", (wkid++).ToString());
                                final += ",";

                                string temptext4 = File.ReadAllText(src + "/LOOP_PUT.txt");
                                string temptext5 = File.ReadAllText(src + "/PUT_SIMPLE.txt");
                                string temptext6 = File.ReadAllText(src + "/DELETE_SIMPLE.txt");
                                string temptext1 = File.ReadAllText(src + "/LOOP_DELETE.txt");
                                foreach (string pk in PKTemp)
                                {
                                    final += temptext1.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", pk)
                                        .Replace("{{UNIQUE_ID_FOR_SUBFLOW_LOOP_DELETE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_FUNCTION_LOOP_DELETE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_HTTP_LOOP_DELETE}}", (wkid++).ToString());
                                    final += ",";
                                    final += temptext4.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", pk)
                                        .Replace("{{UNIQUE_ID_FOR_SUBFLOW_LOOP_PUT}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_FUNCTION_LOOP_PUT}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_HTTP_LOOP_PUT}}", (wkid++).ToString());
                                    final += ",";
                                    final += temptext5.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", pk)
                                        .Replace("{{UNIQUE_ID_FOR_SUBFLOW_PUT_SIMPLE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_FUNCTION_PUT_SIMPLE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_HTTP_PUT_SIMPLE}}", (wkid++).ToString());
                                    final += ",";
                                    final += temptext6.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", pk)
                                        .Replace("{{UNIQUE_ID_FOR_SUBFLOW_DELETE_SIMPLE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_FUNCTION_DELETE_SIMPLE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_HTTP_DELETE_SIMPLE}}", (wkid++).ToString());
                                    final += ",";
                                }
                                string ttext = File.ReadAllText(src + "/LOOP_GET_BY_ID.txt");
                                string stext = File.ReadAllText(src + "/GET_BY_ID_SIMPLE.txt");
                                if (FK.Count != 0)
                                {
                                    for (int i = 0; i < FK.Count; i += 4)
                                    {
                                        final += ttext.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", PKTemp[0])
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE}}", char.ToUpper(FK[i + 1][0]) + FK[i + 1].Substring(1))
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE_OBJECT}}", FK[i + 1].ToLower())
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE_KEY}}", FK[i + 3])
                                            .Replace("{{UNIQUE_ID_FOR_SUBFLOW_GET_BY_ID_REFER}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_HTTP_GET_BY_ID_REFER}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_FUNCTION_GET_BY_ID_REFER_1}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_FUNCTION_GET_BY_ID_REFER_2}}", (wkid++).ToString());
                                        final += ",";
                                        final += stext.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", PKTemp[0])
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE}}", char.ToUpper(FK[i + 1][0]) + FK[i + 1].Substring(1))
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE_OBJECT}}", FK[i + 1].ToLower())
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE_KEY}}", FK[i + 3])
                                            .Replace("{{UNIQUE_ID_FOR_SUBFLOW_GET_BY_ID_REFER}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_HTTP_GET_BY_ID_REFER}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_FUNCTION_GET_BY_ID_REFER_1}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_FUNCTION_GET_BY_ID_REFER_2}}", (wkid++).ToString());
                                        final += ",";
                                    }
                                }
                                string ttext1 = File.ReadAllText(src + "/ADDED_WORKFLOW.txt");
                                final += ttext1.Replace("{{UNIQUE_ID_FOR_TAB_ADDED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_HTTPIN_ADDED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_ADDED_TRIGGER}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_INSTANCE_ADDED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_ADDED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_DEBUG_ADDED}}", (wkid++).ToString());
                                final += ",";
                                string ttext2 = File.ReadAllText(src + "/UPDATED_WORKFLOW.txt");
                                final += ttext2.Replace("{{UNIQUE_ID_FOR_TAB_UPDATED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_HTTPIN_UPDATED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_UPDATED_TRIGGER}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_INSTANCE_UPDATED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_UPDATED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_DEBUG_UPDATED}}", (wkid++).ToString());
                                final += ",";
                                string ttext3 = File.ReadAllText(src + "/DELETED_WORKFLOW.txt");
                                final += ttext3.Replace("{{UNIQUE_ID_FOR_TAB_DELETED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_HTTPIN_DELETED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_DELETED_TRIGGER}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_INSTANCE_DELETED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_DELETED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_DEBUG_DELETED}}", (wkid++).ToString());
                                final += ",";
                                ycod += 10;
                                final = final.Replace("{{SWAGGER_SCHEMA_NAME}}", char.ToUpper(tbl1[0]) + tbl1.Substring(1))
                                    .Replace("{{SWAGGER_SCHEMA_OBJECT}}", tbl1).Replace("{{SWAGGER_URL}}", swaggerurl);
                            }
                            string allchart = File.ReadAllText(src + "/allchart.txt");
                            final += allchart.Replace("{{UNIQUE_ID_FOR_All_CHART_DATA_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_All_CHART_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_All_CHART_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_All_CHART_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_All_CHART_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            //Console.WriteLine("Allchart : " + final);
                            string simplebarchart = File.ReadAllText(src + "/SimpleBarChart.txt");
                            final += simplebarchart.Replace("{{UNIQUE_ID_FOR_SIMPLE_BAR_CHART_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_BAR_CHART_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_BAR_CHART_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_BAR_CHART_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_BAR_CHART_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            //Console.WriteLine("SimpleBarChart : " + final);
                            string simplelinechart = File.ReadAllText(src + "/SimpleLineChart.txt");
                            final += simplelinechart.Replace("{{UNIQUE_ID_FOR_SIMPLE_LINE_CHART_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_LINE_CHART_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_LINE_CHART_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_LINE_CHART_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_LINE_CHART_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            //Console.WriteLine("SimpleLineChart : " + final);
                            string simplepiechart = File.ReadAllText(src + "/SimplePieChart.txt");
                            final += simplepiechart.Replace("{{UNIQUE_ID_FOR_SIMPLE_PIE_CHART_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_PIE_CHART_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_PIE_CHART_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_PIE_CHART_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_PIE_CHART_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            //Console.WriteLine("Allchart : " + final);
                            string simplebarchart2 = File.ReadAllText(src + "/SimpleBarChart2.txt");
                            final += simplebarchart2.Replace("{{UNIQUE_ID_FOR_SIMPLE_BAR_CHART2_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_BAR_CHART2_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_BAR_CHART2_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_BAR_CHART2_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_BAR_CHART2_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            //Console.WriteLine("SimpleBarChart2 : " + final);
                            string shadowlinechart = File.ReadAllText(src + "/ShadowLineChart.txt");
                            final += shadowlinechart.Replace("{{UNIQUE_ID_FOR_SHADOW_LINE_CHART_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SHADOW_LINE_CHART_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SHADOW_LINE_CHART_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SHADOW_LINE_CHART_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SHADOW_LINE_CHART_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            //Console.WriteLine("ShadowLineChart : " + final);
                            string complexlinechart = File.ReadAllText(src + "/ComplexLineChart.txt");
                            final += complexlinechart.Replace("{{UNIQUE_ID_FOR_COMPLEX_LINE_CHART_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_COMPLEX_LINE_CHART_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_COMPLEX_LINE_CHART_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_COMPLEX_LINE_CHART_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_COMPLEX_LINE_CHART_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            //Console.WriteLine("ComplexLineChart : " + final);
                            string complexpiechart = File.ReadAllText(src + "/ComplexPieChart.txt");
                            final += complexpiechart.Replace("{{UNIQUE_ID_FOR_COMPLEX_PIE_CHART_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_COMPLEX_PIE_CHART_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_COMPLEX_PIE_CHART_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_COMPLEX_PIE_CHART_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_COMPLEX_PIE_CHART_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            string straightlinechart = File.ReadAllText(src + "/StraightLineChart.txt");
                            final += straightlinechart.Replace("{{UNIQUE_ID_FOR_STRAIGHT_LINE_CHART_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_STRAIGHT_LINE_CHART_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_STRAIGHT_LINE_CHART_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_STRAIGHT_LINE_CHART_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_STRAIGHT_LINE_CHART_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            //Console.WriteLine("StraightLineChart : " + final);
                            string simplepiechart2 = File.ReadAllText(src + "/SimplePieChart2.txt");
                            final += simplepiechart2.Replace("{{UNIQUE_ID_FOR_SIMPLE_PIE_CHART2_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_PIE_CHART2_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_PIE_CHART2_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_PIE_CHART2_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_SIMPLE_PIE_CHART2_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            //Console.WriteLine("SimplePieChart2 : " + final);
                            string horizontalbarchart = File.ReadAllText(src + "/HorizontalBarChart.txt");
                            final += horizontalbarchart.Replace("{{UNIQUE_ID_FOR_HORIZONTAL_BAR_CHART_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_HORIZONTAL_BAR_CHART_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_HORIZONTAL_BAR_CHART_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_HORIZONTAL_BAR_CHART_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_HORIZONTAL_BAR_CHART_TOKEN_FUNCTION}}", (wkid++).ToString());
                            final += ",";
                            //Console.WriteLine("HorizontalBarChart : " + final);
                            string multibarchart = File.ReadAllText(src + "/MultiBarChart.txt");
                            final += multibarchart.Replace("{{UNIQUE_ID_FOR_MULTI_BAR_CHART_FLOW}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_MULTI_BAR_CHART_HTTP_IN}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_MULTI_BAR_CHART_FUNCTION}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_MULTI_BAR_CHART_HTTP_RESPONSE}}", (wkid++).ToString())
                                .Replace("{{UNIQUE_ID_FOR_MULTI_BAR_CHART_TOKEN_FUNCTION}}", (wkid++).ToString());
                   
                            //Console.WriteLine("MultiBarChart : " + final);
                            string final1 = final.Substring(0, final.Length - 1);
                            final1 += "]";
                            //Console.WriteLine("Final Flows.json " + final1);
                            File.WriteAllText(@des + "/flows.json", final1);
                        }
                        else if (lastElement == "settings.txt")
                        {
                            //Dev1-Code for replacing username and password in settings.js of nodered

                            MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                            string adminUsername = "", adminPassword = "";
                            string sql1 = @"SELECT  t.* FROM users t  WHERE t.user_id= 1";
                            MySqlCommand command = new MySqlCommand(sql1, connection);
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    adminUsername = reader["username"].ToString();
                                    adminPassword = reader["password_hash"].ToString();
                                    //adminPassword = reader.GetValue<String>("password_hash");
                                }
                            }
                            Console.WriteLine("Admin" +adminUsername);
                            Console.WriteLine("Admin" + adminPassword);
                            adminPassword = BCrypt.Net.BCrypt.HashPassword(adminPassword, 8);
                            string finalText = "";
                            string temptext = File.ReadAllText(file);
                            finalText = temptext.Replace("{{adminusername}}", adminUsername).Replace("{{adminPassword}}", adminPassword);
                            File.WriteAllText(@des + "/settings.js", finalText);
                        }
                        else if (lastElement == "Dockerfile-node.txt")
                        {
                            string temptext = File.ReadAllText(file);
                            File.WriteAllText(@des + "/Dockerfile-node", temptext);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in Processfile in nodered section while creating files: " + e.ToString());
                    Program.errors_list.Add("Error in Processfile in nodered section while creating files: " + e.Message);
                }
            }
            else if (projectType == "dnd")
            {
                RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(front_template_json);
                //Console.WriteLine(rootObject);
                // Create a dictionary for quick lookups
                Dictionary<string, string> tableToComponentMap = new Dictionary<string, string>();
                if (rootObject != null)
                {
                    tableToComponentMap = CreateMap(rootObject.Mappings);
                }

                Console.WriteLine(tableToComponentMap);
                foreach (string tbl in tables)
                {
                    Console.WriteLine(tbl);
                    string tbl1 = tbl.ToLower();
                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" ||  tbl1 == "entities" || tbl1 == "permissionmatrix")
                    {
                        continue;
                    }
                    if (!Directory.Exists(@des + "/src/components/" + tbl))
                        Directory.CreateDirectory(@des + "/src/components/" + tbl);
                    if (!Directory.Exists(@des + "/src/redux/slices/" + tbl))
                        Directory.CreateDirectory(@des + "/src/redux/slices/" + tbl);
                }
                try
                {
                    var files = Directory.GetFiles(@src + "/..");
                    foreach (string file in files)
                    {
                        List<List<string>> temp_storage = new List<List<string>>();
                        Console.WriteLine(file);
                        //string lastElement = file.Split('/').ToList().Last();
                        //Dev1:-use when you run in local
                        //with above also
                        //lastElement = lastElement.Split('\\').ToList().Last();
                        string lastElement = Path.GetFileName(file);
                        Console.WriteLine(lastElement);
                        if (lastElement == "FilterModal.txt")
                        {
                            string template = "import { I" + "{table1}" + "iData } from \"redux/slices/{table2}\";\n";
                            string template2 = "{table2}: getPropertiesFromObject(I{table1}iData),";
                            string template4 = "case \"{table2}\":\nsetSelectedReduxSlice({table2}Data);\nbreak;";
                            string template6 = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                            string final = "";
                            string final1 = "";
                            string final2 = "";
                            string final3 = "";
                            string text = File.ReadAllText(file);
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string table1 = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                string template3 = template2.Replace("{table1}", table1).Replace("{table2}", tbl);
                                string template5 = template4.Replace("{table1}", table1).Replace("{table2}", tbl);
                                string template1 = template.Replace("{table1}", table1).Replace("{table2}", tbl);
                                string text1 = template6.Replace("{FK}", tbl);
                                final += template1;
                                final1 += template3;
                                final2 += template5;
                                final3 += text1;
                            }
                            text = text.Replace("{importFKRedux}", final).Replace("{getProperties}", final1).Replace("{switchCaseData}", final2).Replace("{fkReduxInit}", final3);
                            File.WriteAllText(@des + "/src/Dnd/Dnd Designer/Components/FilterRowsCRUD/FilterModal.tsx", text);
                        }

                        else if (lastElement == "kanbanIndex.txt")
                        {

                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Kanban")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    text = text.Replace("{tableName}", tbl);
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string finalCond = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        string templateCond = "if (slice === \"{FK}\") {return {FK}Data;}";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                            text1 = templateCond.Replace("{FK}", s);
                                            finalCond += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        text = text.Replace("{fkReduxInit}", final).Replace("{FKCondition}", finalCond);
                                    }
                                    else
                                    {
                                        text = text.Replace("{fkReduxInit}", "\n").Replace("{FKCondition}", "\n");
                                    }
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl);
                                    if (primaryKeys.Count > 0)
                                    {
                                        text = text.Replace("{pkKeyReplacement}", primaryKeys[0]);
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);

                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban/" + "indexKanban.tsx", text);
                                    //Dev1 decide to add that condition or not
                                    string text3 = File.ReadAllText(src + "/../form.txt");
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban/" + "form.tsx", text3);
                                }
                            }
                        }
                        else if (lastElement == "tableViewFunctionConfig.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();


                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix" || tbl1 == "s3bucket" || tbl1 == "s3bucket_folders")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file);
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);

                                bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));

                                if (!exists)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/" + "tableViewFunctionConfig.tsx", text);
                            }
                        }
                        else if (lastElement == "kanbanColumn.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Kanban")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    text = text.Replace("{tableName}", tbl);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl);
                                    if (primaryKeys.Count > 0)
                                    {
                                        text = text.Replace("{pkKeyReplacement}", primaryKeys[0]);
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);

                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban/" + "kanban-column.tsx", text);
                                }
                            }
                        }
                        else if (lastElement == "kanbanCard.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Kanban")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    text = text.Replace("{tableName}", tbl);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl);
                                    if (primaryKeys.Count > 0)
                                    {
                                        text = text.Replace("{pkKeyReplacement}", primaryKeys[0]);
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);

                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban/" + "kanban-card.tsx", text);
                                }
                            }
                        }
                        else if (lastElement == "tableTemplate1.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView1")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView1");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView1");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView1/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView1/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView1", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView1/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "tableTemplate2.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView2")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView2");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView2");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView2/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView2/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView2", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView2/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "tableTemplate3.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView3")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView3");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView3");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView3/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView3/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView3", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView3/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "tableTemplate4.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView4")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView4");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView4");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView4/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView4/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView4", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView4/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "tableTemplate5.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView5")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView5");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView5");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView5/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView5/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView5", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView5/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "tableTemplate6.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView6")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView6");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView6");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView6/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView6/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView6", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView6/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "tableTemplate7.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView7")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView7");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView7");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView7/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView7/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView7", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView7/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "tableTemplate8.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView8")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView8");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView8");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView8/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView8/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView8", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView8/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "tableTemplate9.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView9")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView9");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView9");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView9/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView9/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView9", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView9/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "tableTemplate10.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView10")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView10");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView10");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView10/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView10/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView10", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView10/" + "index.tsx", text2);
                                }
                            }
                        }

                        else if (lastElement == "tableTemplate11.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "TableView11")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView11");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView11");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView11/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView11/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "TableView11", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/TableView11/" + "index.tsx", text2);
                                }
                            }
                        }

                        else if (lastElement == "kanbanCardModal.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Kanban")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    text = text.Replace("{tableName}", tbl);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl);
                                    if (primaryKeys.Count > 0)
                                    {
                                        text = text.Replace("{pkKeyReplacement}", primaryKeys[0]);
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);

                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban/" + "kanban-card-modal.tsx", text);
                                }
                            }
                        }
                        else if (lastElement == "kanbanCardAdd.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Kanban")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    text = text.Replace("{tableName}", tbl);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl);
                                    if (primaryKeys.Count > 0)
                                    {
                                        text = text.Replace("{pkKeyReplacement}", primaryKeys[0]);
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);

                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban/" + "kanban-card-add.tsx", text);

                                }
                            }
                        }
                        else if (lastElement == "kanbanTypes.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Kanban")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    text = text.Replace("{tableName}", tbl);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl);
                                    if (primaryKeys.Count > 0)
                                    {
                                        text = text.Replace("{pkKeyReplacement}", primaryKeys[0]);
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);

                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/kanban/" + "kanban-types.tsx", text);
                                }
                            }
                        }
                        else if (lastElement == "oneTransactionForm.txt")
                        {

                            foreach (string tbl in tables)
                            {

                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix" || !Program.Transactional.ContainsKey(tbl))
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file);
                                string text2 = getOneTransactionalFormTemplate(text, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "form.tsx", text);
                                bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView");

                                if (!exists)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                if (!exists1)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView");
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView/" + "form.tsx", text2);

                            }
                        }
                        else if (lastElement == "calendarIndex.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Calendar")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    text = text.Replace("{tableName}", tbl);
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string finalCond = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        string templateCond = "if (slice === \"{FK}\") {return {FK}Data;}";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                            text1 = templateCond.Replace("{FK}", s);
                                            finalCond += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        text = text.Replace("{fkReduxInit}", final).Replace("{FKCondition}", finalCond);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl);
                                    if (primaryKeys.Count > 0)
                                    {
                                        text = text.Replace("{pkKeyReplacement}", primaryKeys[0]);
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);

                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar");

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar/" + "index.tsx", text);

                                }
                            }

                        }
                        else if (lastElement == "calendar-toolbar.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Calendar")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    text = text.Replace("{tableName}", tbl);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);

                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar");

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar/" + "calendar-toolbar.tsx", text);
                                }
                            }
                        }
                        else if (lastElement == "calendar-event-dialog.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Calendar")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    string text2 = getCalendarFormTemplate(text, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "form.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar");

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar/" + "calendar-event-dialog.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "quill-editor.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Calendar")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);

                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar");

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/Calendar/" + "quill-editor.tsx", text);
                                }
                            }
                        }
                        else if (lastElement == "chartIndex.txt")
                        {
                            string text = File.ReadAllText(file);

                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chartIndex.tsx", text);
                        }
                        else if (lastElement == "chart1.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart1.tsx", text);
                        }
                        else if (lastElement == "chart2.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart2.tsx", text);
                        }
                        else if (lastElement == "chart3.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart3.tsx", text);
                        }
                        else if (lastElement == "chart4.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart4.tsx", text);
                        }
                        else if (lastElement == "chart6.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart6.tsx", text);
                        }
                        else if (lastElement == "chart7.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart7.tsx", text);
                        }
                        else if (lastElement == "chart8.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart8.tsx", text);
                        }
                        else if (lastElement == "chart9.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart9.tsx", text);
                        }
                        else if (lastElement == "chart10.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart10.tsx", text);
                        }
                        else if (lastElement == "chart11.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart11.tsx", text);
                        }
                        else if (lastElement == "chart12.txt")
                        {
                            string text = File.ReadAllText(file);
                            bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            if (!exists)
                                System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts");
                            File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/Charts/" + "chart12.tsx", text);
                        }

                        else if (lastElement == "component.txt")
                        {
                            foreach (string tbl in tables)
                            {

                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    continue;

                                string text = File.ReadAllText(file);
                                string text2 = getComponentTemplate(text, "DefaultView", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");

                                //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "index.tsx", text);
                                bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView");

                                if (!exists)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                if (!exists1)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView");
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView/" + "index.tsx", text2);


                            }

                        }

                        else if (lastElement == "AutoCompleteTemplate.txt")
                        {
                            foreach (string tbl in tables)
                            {

                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                var cols = GetAllColumns(connectionString, tbl);
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    if (!GetPrimaryKey(connectionString, tbl).Contains(cols[i]))
                                    {
                                        text = text.Replace("{secondaryKeyList}", cols[i]);
                                        break;
                                    }
                                }
                                text = text.Replace("{primaryKeyList}", GetPrimaryKey(connectionString, tbl)[0]).Replace("{componentName}", char.ToUpper(tbl[0]) + tbl.Substring(1) + "AutoComplete").Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/syncfusion_components/Autocomplete/" + tbl + "AutoComplete.tsx", text);
                            }

                        }
                        else if (lastElement == "listviewTemplate.txt")
                        {
                            foreach (string tbl in tables)
                            {

                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                var cols = GetAllColumns(connectionString, tbl);
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    if (!GetPrimaryKey(connectionString, tbl).Contains(cols[i]))
                                    {
                                        text = text.Replace("{viewKeyList}", cols[i]);
                                        break;
                                    }
                                }
                                text = text.Replace("{componentName}", char.ToUpper(tbl[0]) + tbl.Substring(1) + "ListView").Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/syncfusion_components/ListView/" + tbl + "ListView.tsx", text);
                            }

                        }
                        else if (lastElement == "gridTemplate.txt")
                        {
                            foreach (string tbl in tables)
                            {

                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                text = text.Replace("{componentName}", char.ToUpper(tbl[0]) + tbl.Substring(1) + "GridView").Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                string temp_text = "const old_id{i} = args.previousData.{PK};", replace2 = "", replace = "", replace1 = "";
                                List<string> PrimaryKeys = GetPrimaryKey(connectionString, tbl);
                                int cnt = 1;
                                foreach (string col in PrimaryKeys)
                                {
                                    replace += temp_text.Replace("{i}", cnt.ToString()).Replace("{PK}", col) + "\n";
                                    replace1 += "old_id" + cnt + ",";
                                    replace2 += "(args.data)[0]." + col + ",";
                                    cnt++;
                                }
                                replace1 = replace1.Substring(0, replace1.Length - 1);
                                replace2 = replace2.Substring(0, replace2.Length - 1);
                                text = text.Replace("{primaryKeyList}", replace).Replace("{primaryKeyListArgs}", replace1).Replace("{primaryKeyListArgs1}", replace2);
                                string columns = "[";
                                List<string> columnss = GetAllColumns(connectionString, tbl);
                                for (int i = 0; i < columnss.Count; i += 3)
                                {
                                    if (PrimaryKeys.Contains(columnss[i]))
                                        columns += $@"
        {{
            field: '{columnss[i]}',
            headerText: '{columnss[i]}',
            type:'{utility.GetJSType(columnss[i + 1])}',
            isPrimaryKey: true
         }},";
                                    else if (columnss[i] == columnss[columnss.Count - 3])
                                        columns += $@"
         {{
            field: '{columnss[i]}',
            type:'{utility.GetJSType(columnss[i + 1])}',
            headerText: '{columnss[i]}',
         }}";
                                    else
                                        columns += $@"
         {{
            field: '{columnss[i]}',
            type:'{utility.GetJSType(columnss[i + 1])}',
            headerText: '{columnss[i]}',
         }},";
                                    columns += "\n\t";
                                }
                                columns += "];";
                                text = text.Replace("{fetchColumns}", columns);
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/syncfusion_components/Grid/" + tbl + "GridView.tsx", text);
                            }
                        }
                        else if (lastElement == "dropdownlistTemplate.txt")
                        {
                            foreach (string tbl in tables)
                            {

                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                var cols = GetAllColumns(connectionString, tbl);
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    if (!GetPrimaryKey(connectionString, tbl).Contains(cols[i]))
                                    {
                                        text = text.Replace("{secondaryKeyList}", cols[i]);
                                        break;
                                    }
                                }
                                string replace = "<div className=\"header-row\">\n";
                                string replace1 = "<div className=\"item-row\">";
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    replace += $"<div className=\"header-column\">{cols[i]}</div>" + "\n";
                                    replace1 += $"<div className=\"header-column\">{{data.{cols[i]}}}</div>" + "\n";
                                }
                                replace += "</div>";
                                replace1 += "</div>";
                                text = text.Replace("{codeForAllFieldsItem}", replace1).Replace("{codeForAllFields}", replace).Replace("{componentName}", char.ToUpper(tbl[0]) + tbl.Substring(1) + "DropDownList").Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/syncfusion_components/DropDownList/" + tbl + "DropDownList.tsx", text);

                            }
                        }
                        else if (lastElement == "constants.txt")
                        {
                            string text = File.ReadAllText(file);
                            string import_replacement = "", functionMap = "", mapComponent = "", InterfaceReplacement = "", columnConfig = "";
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                List<string> FK = new List<string>();
                                HashSet<string> FKC = new HashSet<string>();
                                List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                //{
                                //    connection.Open();
                                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                MySqlCommand command = new MySqlCommand(sql, connection);
                                string foreignKeyColumn = "", refTable = "";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                        refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                        string dataType = reader["DATA_TYPE"].ToString();
                                        string forkey1 = reader["COLUMN_NAME"].ToString();
                                        FK.Add(foreignKeyColumn);
                                        FK.Add(refTable);
                                        if (refTable != tbl)
                                            FKC.Add(refTable);
                                        FK.Add(dataType);
                                        FK.Add(forkey1);
                                    }
                                }
                                //
                                columnConfig += $"{char.ToUpper(tbl[0]) + tbl.Substring(1)} :" + "{innerContent:{\"input-type\":\"text\",},tableHeading:{\"input-type\":\"text\",},addFormHeading:{\"input-type\":\"text\",},editFormHeading:{\"input-type\":\"text\",},headerSize:{\"input-type\":\"list\",options:[\"h1\",\"h2\",\"h3\",\"h4\",\"h5\",\"h6\"],},backgroundColor:{\"input-type\":\"heading-color\"},color:{\"input-type\":\"heading-color\"},tableHeadBackgroundColor:{\"input-type\":\"table-head-color\",},HeadColor:{\"input-type\":\"table-head-color\",},tableBackgroundColor:{\"input-type\":\"table-color\",},HeadRowBackgroundColor:{\"input-type\":\"table-color\",},HeadRowColor:{\"input-type\":\"row-color\",},RowBackgroundColor:{\"input-type\":\"row-color\",},RowColor:{\"input-type\":\"row-color\",},fontFamily:{\"input-type\":\"list\",options:[\"Arial\",\"Helvetica\",\"Verdana\",\"Georgia\",\"CourierNew\",\"cursive\",],},";
                                if (Program.Transactional.ContainsKey(tbl))
                                {
                                    columnConfig += "subColumns: {\"input-type\": \"subGroup\",subGroupColumns : {";
                                    foreach (var referencingTable in Program.Transactional[tbl].Sequence)
                                    {
                                        List<string> FK1 = new List<string>();
                                        HashSet<string> FKC1 = new HashSet<string>();
                                        List<string> PKTemp1 = GetPrimaryKey(connectionString, referencingTable);
                                        //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                        //{
                                        //    connection.Open();
                                        MySqlConnection connection1 = MySqlConnectionManager.Instance.GetConnection();
                                        string sql1 = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + referencingTable + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                        MySqlCommand command1 = new MySqlCommand(sql1, connection1);
                                        string foreignKeyColumn1 = "", refTable1 = "";
                                        using (MySqlDataReader reader = command.ExecuteReader())
                                        {
                                            while (reader.Read())
                                            {
                                                foreignKeyColumn1 = reader["REFERENCED_COLUMN_NAME"].ToString();
                                                refTable1 = reader["REFERENCED_TABLE_NAME"].ToString();
                                                string dataType1 = reader["DATA_TYPE"].ToString();
                                                string forkey11 = reader["COLUMN_NAME"].ToString();
                                                FK1.Add(foreignKeyColumn1);
                                                FK1.Add(refTable1);
                                                if (refTable1 != referencingTable)
                                                    FKC1.Add(refTable1);
                                                FK1.Add(dataType1);
                                                FK1.Add(forkey11);
                                            }
                                        }

                                        columnConfig += (referencingTable + ":{\"input-type\": \"group\",\"columns-list\":[");
                                        List<string> columns1 = GetAllColumns(connectionString, referencingTable);
                                        for (int i = 0; i < columns1.Count; i += 3)
                                        {
                                            bool temp = false, temp1 = false; int j = 0, j1 = 0;
                                            for (j = 0; j < FK1.Count; j += 4)
                                            {
                                                if (FK1[j + 3] == columns1[i])
                                                {
                                                    temp = true;
                                                    break;
                                                }
                                            }
                                            for (j1 = 0; j1 < PKTemp1.Count; j1++)
                                            {
                                                if (PKTemp1[j1] == columns1[i])
                                                {
                                                    temp1 = true;
                                                    break;
                                                }
                                            }
                                            if (temp)
                                                columnConfig += "{name: '" + columns1[i] + "',pkey:false,fkey:true,icontrol: getList('" + columns1[i + 1] + "',true),type: '" + columns1[i + 1] + "',slice: '" + FK[j + 1] + "',},";
                                            else if (temp1)
                                                columnConfig += "{name: '" + columns1[i] + "',pkey:true,fkey:false,icontrol: getList('" + columns1[i + 1] + "'),type: '" + columns1[i + 1] + "',slice: '',},";
                                            else
                                                columnConfig += "{name: '" + columns1[i] + "',pkey:false,fkey:false,icontrol: getList('" + columns1[i + 1] + "'),type: '" + columns1[i + 1] + "',slice: '',},";
                                        }
                                        columnConfig += "],\"error-control-list\": [\"password\", \"email\", \"text\", \"number\"],},";
                                    }
                                    columnConfig += "},},";
                                }

                                columnConfig += "columns: {\"input-type\": \"group\",\"columns-list\":[";
                                List<string> columns = GetAllColumns(connectionString, tbl);
                                for (int i = 0; i < columns.Count; i += 3)
                                {
                                    bool temp = false, temp1 = false; int j = 0, j1 = 0;
                                    for (j = 0; j < FK.Count; j += 4)
                                    {
                                        if (FK[j + 3] == columns[i])
                                        {
                                            temp = true;
                                            break;
                                        }
                                    }
                                    for (j1 = 0; j1 < PKTemp.Count; j1++)
                                    {
                                        if (PKTemp[j1] == columns[i])
                                        {
                                            temp1 = true;
                                            break;
                                        }
                                    }
                                    if (temp)
                                        columnConfig += "{name: '" + columns[i] + "',pkey:false,fkey:true,icontrol: getList('" + columns[i + 1] + "',true),type: '" + columns[i + 1] + "',slice: '" + FK[j + 1] + "',},";
                                    else if (temp1)
                                        columnConfig += "{name: '" + columns[i] + "',pkey:true,fkey:false,icontrol: getList('" + columns[i + 1] + "'),type: '" + columns[i + 1] + "',slice: '',},";
                                    else
                                        columnConfig += "{name: '" + columns[i] + "',pkey:false,fkey:false,icontrol: getList('" + columns[i + 1] + "'),type: '" + columns[i + 1] + "',slice: '',},";
                                }
                                columnConfig += "],\"error-control-list\": [\"password\", \"email\", \"text\", \"number\",\"url\"],},row:{\"input-type\": \"filter-form\",\"columns-list\": [";
                                for (int i = 0; i < columns.Count; i += 3)
                                {
                                    columnConfig += "'" + columns[i] + "',";
                                }
                                columnConfig += "],\"column-condition\": [\"==\", \"!=\", \">\", \"<\"],},navConfig : {\"input-type\": \"nav\",\"columns-list\": [";
                                for (int i = 0; i < columns.Count; i += 3)
                                {
                                    columnConfig += "'" + columns[i] + "',";
                                }
                                columnConfig += "],\"nav-list\":getNavNameList(),}},";

                                import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList}} from \"../../Draggable Components/syncfusion_components/DropDownList/{tbl}DropDownList\";" + "\n";
                                import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView}} from \"../../Draggable Components/syncfusion_components/Grid/{tbl}GridView\";" + "\n";
                                import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView}} from \"../../Draggable Components/syncfusion_components/ListView/{tbl}ListView\";" + "\n";
                                import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete}} from \"Dnd/Draggable Components/syncfusion_components/Autocomplete/{tbl}AutoComplete\";" + "\n";
                                import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder}} from \"Dnd/Draggable Components/syncfusion_components/QueryBuilder/{tbl}QueryBuilder\";" + "\n";
                                import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}}} from \"Dnd/Draggable Components/Previous_Components/CustomComponents/{char.ToUpper(tbl[0]) + tbl.Substring(1)}\";" + "\n";
                                functionMap += $@"/*case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList config={{config}} />
            break;*/ " + "\n\t";
                                functionMap += $@"/*case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView config={{config}} />
            break; */" + "\n\t";
                                functionMap += $@"/*case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView config={{config}} />
            break; */" + "\n\t";
                                functionMap += $@"/*case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete config={{config}} />
            break; */" + "\n\t";
                                functionMap += $@"/*case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder config={{config}} />
            break;*/ " + "\n\t";
                                functionMap += $@"case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)} config={{config}} openLink={{openLink}}
          id={{id}}
          handleConfigurationChange={{handleConfigurationChange}}  />
            break; " + "\n\t";
                                mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList," + "\n\t";
                                mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView," + "\n\t";
                                mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView," + "\n\t";
                                mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete," + "\n\t";
                                mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder," + "\n\t";
                                mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}," + "\n\t";
                                InterfaceReplacement += @$"/*{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}DropDownList"",
            icon_name: ""ApiIcon"",
        }},
    }},*/" + "\n\t";
                                InterfaceReplacement += @$"/*{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}GridView"",
            icon_name: ""ApiIcon"",
        }},
    }},*/" + "\n\t";
                                InterfaceReplacement += @$"/*{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}ListView"",
            icon_name: ""ApiIcon"",
        }},
    }},*/" + "\n\t";
                                InterfaceReplacement += @$"/*{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}AutoComplete"",
            icon_name: ""ApiIcon"",
        }},
    }},*/" + "\n\t";
                                InterfaceReplacement += @$"/*{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}QueryBuilder"",
            icon_name: ""ApiIcon"",
        }},
    }},*/" + "\n\t";
                                InterfaceReplacement += @$"{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)},
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}"",
            icon_name: ""ApiIcon"",
        }},
    }}," + "\n\t";
                            }
                            mapComponent = mapComponent.Substring(0, mapComponent.Length - 1);
                            text = text.Replace("{InterfaceReplacement}", InterfaceReplacement).Replace("{importComponents}", import_replacement).Replace("{functionTOmap}", functionMap).Replace("{mapNametoComponent}", mapComponent).Replace("{columnConfig}", columnConfig);

                            string viewList = "";
                       
                                viewList = GenerateViewMapToTables(tables, tableToComponentMap);
                                text = text.Replace("{viewList}", viewList);
                            
                            
                            File.WriteAllText(@des + "/src/Dnd/Dnd Designer/Utility/constants.tsx", text);
                        }
                        else if (lastElement == "table.txt")
                        {
                            foreach (string tbl in tables)
                            {

                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                List<string> FK = new List<string>();
                                HashSet<string> FKC = new HashSet<string>();
                                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                //{
                                //    connection.Open();
                                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                MySqlCommand command = new MySqlCommand(sql, connection);
                                string foreignKeyColumn = "", refTable = "";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                        refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                        string dataType = reader["DATA_TYPE"].ToString();
                                        string forkey1 = reader["COLUMN_NAME"].ToString();
                                        FK.Add(foreignKeyColumn);
                                        FK.Add(refTable);
                                        if (refTable != tbl)
                                            FKC.Add(refTable);
                                        FK.Add(dataType);
                                        FK.Add(forkey1);
                                    }
                                }
                                //}
                                if (FK.Count != 0)
                                {
                                    string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                    string final = "";
                                    /*for (int i = 0; i < FK.Count; i += 4)
                                    {
                                        string s = FK[i + 1];*/
                                    foreach (string s in FKC)
                                    {
                                        string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                        string template1 = template.Replace("{table1}", table1);
                                        final += template1;
                                    }
                                    text = text.Replace("{importFKRedux}", final);
                                }
                                else
                                    text = text.Replace("{importFKRedux}", "\n");
                                if (FK.Count != 0)
                                {
                                    string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                    string final = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        //string s = FK[i + 1];
                                        string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                        string FKK = s;// FK[i + 1];
                                        string template1 = "";
                                        template1 = template.Replace("{table2}", table1);
                                        template1 = template1.Replace("{FK}", FKK);
                                        final += template1;
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(template1);
                                    }
                                    text = text.Replace("{importFKService}", final);
                                }
                                else
                                    text = text.Replace("{importFKService}", "\n");
                                if (FK.Count != 0)
                                {
                                    string final = "";
                                    string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                    //for (int i = 1; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        //string text1 = template.Replace("{FK}", FK[i]);
                                        string text1 = template.Replace("{FK}", s);
                                        final += text1;
                                    }
                                    if (tbl == "projects_repositories")
                                        Console.WriteLine(final);
                                    ; text = text.Replace("{fkReduxInit}", final);
                                }
                                else
                                    text = text.Replace("{fkReduxInit}", "\n");
                                string abcd = "";
                                //for (int i = 0; i < FK.Count; i += 4)
                                foreach (string s in FKC)
                                {
                                    string tableName = s;//FK[i + 1];
                                    string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                    string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                    string pageNumber = "Constant.defaultPageNumber";
                                    string pageSize = "Constant.defaultDropdownPageSize";
                                    string searchKey = "''";
                                    string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                    formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                    abcd += formControl;
                                }
                                if (FK.Count == 0)
                                    text = text.Replace("{useEffectForFK}", "\n");
                                else
                                    text = text.Replace("{useEffectForFK}", abcd);
                                List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                string tableCols = "";
                                for (int i = 0; i < columns.Count; i += 3)
                                {
                                    tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                }
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView");

                                if (!exists)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                if (!exists1)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView");
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView/" + "table.tsx", text);
                                string sktemp = File.ReadAllText(src + "/../tableskeleton.txt");
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/" + "skeletone.tsx", sktemp);


                            }
                        }
                        else if (lastElement == "querybuilerTemplate.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                List<string> PrimaryKey = GetPrimaryKey(connectionString, tbl);
                                text = text.Replace("{primaryKeyList}", PrimaryKey[0]).Replace("{componentName}", char.ToUpper(tbl[0]) + tbl.Substring(1) + "QueryBuilder").Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl);
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/syncfusion_components/QueryBuilder/" + tbl + "QueryBuilder.tsx", text);
                            }
                        }
                        else if (lastElement == "aggregateservice.txt")
                        {
                            string text = File.ReadAllText(file);
                            string serviceFunction = "";
                            foreach (ApiFlow tp in Program.aggregateModel)
                            {
                                List<string> pm = new List<string>();
                                foreach (ApiParameter apiparameter in tp.Parameters)
                                {
                                    pm.Add(apiparameter.Name);
                                }
                                serviceFunction += Generate("getOneAggregate", tp.DTO.Name, pm.Count, pm, "get", "read_one", backendChoice);
                            }
                            text = text.Replace("{serviceFunction}", serviceFunction);
                            File.WriteAllText(@des + "/src/services/aggregateService.ts", text);
                        }
                        else if (lastElement == "service.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), passit = new List<string>();
                                for (int i = 0; i < primaryKeys.Count; i++)
                                {
                                    int x = i + 1;
                                    passit.Add("id" + x);
                                }
                                passit.Add("data");

                                string serviceFunction = "";
                                if (ApiGeneratorFunctions.referencedBy.ContainsKey(tbl) || ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                {

                                    serviceFunction += Generate("getOneRelational", tbl, primaryKeys.Count, primaryKeys, "get", "read_one", backendChoice) +
                                        Generate("getAllRelational", tbl, 2, new List<string> { "pageno", "pagesize" }, "get", "read", backendChoice);
                                }
                                if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                {
                                    serviceFunction += Generate("getOneReporting", tbl, primaryKeys.Count, primaryKeys, "get", "read_one", backendChoice) +
                                        Generate("getAllReporting", tbl, 2, new List<string> { "pageno", "pagesize" }, "get", "read", backendChoice);
                                }
                                if (Program.Transactional.ContainsKey(tbl))
                                {
                                    serviceFunction += Generate("addTransactional", tbl, 1, new List<string> { "data" }, "post", "create", backendChoice)
                                        + Generate("updateTransactional", tbl, passit.Count, passit, "put", "update", backendChoice);
                                }
                                serviceFunction += Generate("getAll", tbl, 2, new List<string> { "pageno", "pagesize" }, "get", "read", backendChoice) + Generate("getOne", tbl, 1, new List<string> { "id" }, "get", "read_one", backendChoice) + Generate("search", tbl, 3, new List<string> { "key", "pageno", "pagesize" }, "get", "search", backendChoice) + Generate("add", tbl, 1, new List<string> { "data" }, "post", "create", backendChoice) + Generate("update", tbl, passit.Count, passit, "put", "update", backendChoice) + Generate("delete", tbl, primaryKeys.Count, primaryKeys, "delete", "delete", backendChoice) + Generate("filter", tbl, 1, new List<string> { "data" }, "post", "create", backendChoice);
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{serviceFunction}", serviceFunction);
                                File.WriteAllText(@des + "/src/services/" + tbl + "Service.ts", text);
                            }
                        }
                        else if (lastElement == "auth.txt")
                        {
                            File.WriteAllText(@des + "/src/services/" + "auth" + "Service.ts", File.ReadAllText(file));
                        }
                        else if (lastElement == "aggregateslice.txt")
                        {
                            string text = File.ReadAllText(file);
                            StringBuilder interfacebuilder = new StringBuilder();
                            foreach (ApiFlow tp in Program.aggregateModel)
                            {
                                string tableInterface = "export interface I" + tp.DTO.Name + " {\n";
                                foreach (Property pp in tp.DTO.Properties)
                                {
                                    string dataType = utility.GetJsAggregateType(pp.Type).ToLower();
                                    if (dataType.Contains("date"))
                                        dataType = "Date";
                                    tableInterface += pp.Name + ":" + dataType + ",\n";
                                }
                                tableInterface += "}";
                                interfacebuilder.AppendLine(tableInterface);
                            }
                            text = text.Replace("{aggregateInterface}", interfacebuilder.ToString());
                            string tbl = "aggregate";
                            if (!Directory.Exists(@des + "/src/redux/slices/" + tbl))
                                Directory.CreateDirectory(@des + "/src/redux/slices/" + tbl);
                            File.WriteAllText(@des + "/src/redux/slices/" + tbl + "/index.ts", text);
                        }
                        else if (lastElement == "slice.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();

                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";", tableInterface = "export interface I" + char.ToUpper(tbl[0]) + tbl.Substring(1) + " {\n", tableInterfaceData = "", relationalInterface = "export interface I" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "Relational" + " {\n", reportingInterface = "export interface I" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "Reporting" + " {\n", transactionalInterface = "export interface I" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "Transactional" + " {\n";
                                StringBuilder interfaceimport = new StringBuilder();
                                List<string> PK = GetPrimaryKey(connectionString, tbl), type = GetPrimaryKeyType(connectionString, tbl), cols = GetAllColumns(connectionString, tbl);
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    if (cols[i + 2] == "YES")
                                    {
                                        string dataType = utility.GetJSType(cols[i + 1]).ToLower();
                                        if (dataType.Contains("date"))
                                            dataType = "Date";
                                        tableInterface += cols[i] + "?:" + dataType + ",\n";
                                        relationalInterface += cols[i] + "?:" + dataType + ",\n";
                                        reportingInterface += cols[i] + "?:" + dataType + ",\n";
                                        transactionalInterface += cols[i] + "?:" + dataType + ",\n";
                                    }
                                    else
                                    {
                                        string dataType = utility.GetJSType(cols[i + 1]).ToLower();
                                        if (dataType.Contains("date"))
                                            dataType = "Date";
                                        tableInterface += cols[i] + ":" + dataType + ",\n";
                                        relationalInterface += cols[i] + ":" + dataType + ",\n";
                                        reportingInterface += cols[i] + ":" + dataType + ",\n";
                                        transactionalInterface += cols[i] + "?:" + dataType + ",\n";
                                    }
                                    tableInterfaceData += cols[i] + ":null,\n";

                                }
                                tableInterface += "}";
                                //if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                //{
                                //    foreach (var foreignKeyInfo in ApiGeneratorFunctions.tablesWithForeignKeys[tbl])
                                //    {
                                //        string referencedTableName = char.ToUpper(foreignKeyInfo.ReferencedTableName[0]) + foreignKeyInfo.ReferencedTableName.Substring(1);
                                //        string importtemplate = @"import { {ifname} } from ""../{tbname}"";";
                                //        importtemplate = importtemplate.Replace("{ifname}", "I" + referencedTableName + "Relational").Replace("{tbname}", foreignKeyInfo.ReferencedTableName);
                                //        interfaceimport.AppendLine(importtemplate);
                                //        relationalInterface += referencedTableName + ":" + "I" + referencedTableName + "Relational" + ",\n";
                                //        // Generate the property for the related entity
                                //    }
                                //}

                                //if (ApiGeneratorFunctions.referencedBy.ContainsKey(tbl))
                                //{
                                //    foreach (var referencingTable in ApiGeneratorFunctions.referencedBy[tbl])
                                //    {
                                //        string referencingTableTmp = char.ToUpper(referencingTable[0]) + referencingTable.Substring(1);
                                //        string importtemplate = @"import { {ifname} } from ""../{tbname}"";";
                                //        importtemplate = importtemplate.Replace("{ifname}", "I" + referencingTableTmp + "Relational").Replace("{tbname}", referencingTable);
                                //        interfaceimport.AppendLine(importtemplate);
                                //        relationalInterface += referencingTableTmp + ":" + "Array<I" + referencingTableTmp + "Relational>" + ",\n";
                                //        // Generate the property for the list of related entities
                                //    }
                                //}
                                relationalInterface += "}";

                                //if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                //{
                                //    foreach (var foreignKeyInfo in ApiGeneratorFunctions.tablesWithForeignKeys[tbl])
                                //    {
                                //        string referencedTableName = char.ToUpper(foreignKeyInfo.ReferencedTableName[0]) + foreignKeyInfo.ReferencedTableName.Substring(1);
                                //        string referencedTableNameSmall = foreignKeyInfo.ReferencedTableName;
                                //        HashSet<string> common_column = Program.getForeignKey(tbl, connectionString);
                                //        List<string> colms = GetAllColumns(connectionString, referencedTableNameSmall);
                                //        for (int i = 0; i < colms.Count; i += 3)
                                //        {
                                //            if (common_column.Contains(colms[i]))
                                //                continue;
                                //            if (colms[i + 2] == "YES")
                                //            {
                                //                string dataType = utility.GetJSType(colms[i + 1]).ToLower();
                                //                if (dataType.Contains("date"))
                                //                    dataType = "Date";
                                //                if (colms[i] == "isActive" || colms[i] == "createdBy" || colms[i] == "modifiedBy" || colms[i] == "createdAt" || colms[i] == "modifiedAt")
                                //                    reportingInterface += referencedTableNameSmall + "_" + colms[i] + "?:" + dataType + ",\n";
                                //                else
                                //                    reportingInterface += colms[i] + "?:" + dataType + ",\n";
                                //            }
                                //            else
                                //            {
                                //                string dataType = utility.GetJSType(colms[i + 1]).ToLower();
                                //                if (dataType.Contains("date"))
                                //                    dataType = "Date";
                                //                if (colms[i] == "isActive" || colms[i] == "createdBy" || colms[i] == "modifiedBy" || colms[i] == "createdAt" || colms[i] == "modifiedAt")
                                //                    reportingInterface += referencedTableNameSmall + "_" + colms[i] + ":" + dataType + ",\n";
                                //                else
                                //                    reportingInterface += colms[i] + ":" + dataType + ",\n";
                                //            }
                                //        }
                                //    }
                                //}
                                reportingInterface += "}";
                                if (Program.Transactional.ContainsKey(tbl))
                                {
                                    foreach (var referencingTable in Program.Transactional[tbl].Sequence)
                                    {
                                        string referencingTableSmall = referencingTable.ToLower();
                                        string referencingTableTmp = char.ToUpper(referencingTableSmall[0]) + referencingTableSmall.Substring(1);
                                        string importtemplate = @"import { {ifname} } from ""../{tbname}"";";
                                        importtemplate = importtemplate.Replace("{ifname}", "I" + referencingTableTmp).Replace("{tbname}", referencingTableSmall);
                                        interfaceimport.AppendLine(importtemplate);
                                        // Generate the property for the list of related entities
                                        if (Program.Transactional[tbl].Relations[referencingTable] == "many")
                                        {
                                            transactionalInterface += referencingTableTmp + ":" + "Array<I" + referencingTableTmp + ">" + ",\n";
                                        }
                                        else
                                        {
                                            transactionalInterface += referencingTableTmp + ":" + "I" + referencingTableTmp + ",\n";
                                        }
                                    }
                                }
                                transactionalInterface += "}";
                                //Dev1-Relational
                                if (ApiGeneratorFunctions.referencedBy.ContainsKey(tbl) || ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                {
                                    //text = text.Replace("{relationalInterface}", relationalInterface);
                                    text = text.Replace("{relationalInterface}", "");
                                }
                                else
                                {
                                    text = text.Replace("{relationalInterface}", "");
                                }
                                if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                    //text = text.Replace("{reportingInterface}", reportingInterface);
                                    text = text.Replace("{reportingInterface}", "");
                                else
                                    text = text.Replace("{reportingInterface}", "");
                                if (Program.Transactional.ContainsKey(tbl))
                                    text = text.Replace("{transactionalInterface}", transactionalInterface);
                                else
                                    text = text.Replace("{transactionalInterface}", "");

                                //Dev2
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{tableInterface}", tableInterface).Replace("{tableInterfaceData}", tableInterfaceData).Replace("{interfaceimport}", interfaceimport.ToString());
                                File.WriteAllText(@des + "/src/redux/slices/" + tbl + "/index.ts", text);
                                IEnumerable<string> lines = File.ReadLines(@des + "/src/redux/slices/" + tbl + "/index.ts");
                                string secondLastLine = lines.Reverse().Skip(2).First();
                                string temp = "", path = "";
                                Stack<char> st = new Stack<char>();
                                for (int i = 0; i < secondLastLine.Count(); i++)
                                {
                                    if (secondLastLine[i] == '{')
                                    {
                                        while (i < secondLastLine.Count() && secondLastLine[i] != '}')
                                            temp += secondLastLine[i++];
                                        temp += "}";
                                        break;
                                    }
                                }
                                path = "redux/slices/" + tbl;
                                List<string> l = new List<string>();
                                l.Add(temp);
                                l.Add(path);
                                temp_storage.Add(l);
                            }
                            string text1 = File.ReadAllText(Path.Combine(file, "../actions.txt"));
                            string replacement = "";
                            for (int i = 0; i < temp_storage.Count; i++)
                            {
                                replacement += "export " + temp_storage[i][0] + " from '" + temp_storage[i][1] + "';\n";
                            }
                            text1 = text1.Replace("{actionList}", replacement);
                            File.WriteAllText(@des + "/src/redux/actions.ts", text1);
                            text1 = File.ReadAllText(Path.Combine(file, "../reducers.txt"));
                            replacement = "";
                            string second = "";
                            for (int i = temp_storage.Count - 1; i >= 0; i--)
                            {
                                string last = temp_storage[i][1].Split("/").Last();
                                replacement += "import " + last + " from '" + temp_storage[i][1] + "';\n";
                                second += last;
                                if (i != 0)
                                    second += ",";
                            }
                            text1 = text1.Replace("{reducerImport}", replacement);
                            text1 = text1.Replace("{reducerList}", second);
                            File.WriteAllText(@des + "/src/redux/reducers.ts", text1);
                            text1 = File.ReadAllText(Path.Combine(file, "../import.txt"));
                            replacement = "";
                            //for (int i = temp_storage.Count - 1; i >= 0; i--)
                            //{
                            //    string last = temp_storage[i][1].Split("/").Last();
                            //    replacement += "export { " + char.ToUpper(last[0]) + last.Substring(1) + " } from \"" + "./" + last + "\";\n";
                            //}
                            //text1 = text1.Replace("{importComponent}", replacement);
                            text1 = text1.Replace("{importComponent}", "");
                            File.WriteAllText(@des + "/src/components/index.ts", text1);
                            text1 = File.ReadAllText(Path.Combine(file, "../routes.txt"));
                            replacement = "";
                            string new_rep = "import { ";
                            for (int i = temp_storage.Count - 1; i >= 0; i--)
                            {
                                string last = temp_storage[i][1].Split("/").Last();
                                new_rep += char.ToUpper(last[0]) + last.Substring(1);
                                if (i != 0)
                                    new_rep += ", ";
                                if (i == 0)
                                    new_rep += " } from \"components\";";
                                replacement += "<Route path=\"/" + last + "\" element={<AuthenticatedRoute element={<" + char.ToUpper(last[0]) + last.Substring(1) + "/>} />}></Route>\n";

                            }
                            text1 = text1.Replace("{routePathList}", replacement);
                            text1 = text1.Replace("{importComponents}", new_rep);
                            File.WriteAllText(@des + "/src/pages/index.tsx", text1);
                            string rep = "";
                            for (int i = temp_storage.Count - 1; i >= 0; i--)
                            {
                                string last = temp_storage[i][1].Split("/").Last();
                                string template = "{ title: '{modelName}', path: '/{tableName}', icon: 'fas fa-fw fa-table', subMenu: [] },\n";
                                template = template.Replace("{modelName}", char.ToUpper(last[0]) + last.Substring(1));
                                template = template.Replace("{tableName}", last);
                                rep += template;
                            }
                            string text2 = File.ReadAllText(Path.Combine(file, "../menu.txt"));
                            text2 = text2.Replace("{menuItems}", rep);
                            File.WriteAllText(@des + "/src/template/" + "MenuItems.ts", text2);
                        }
                        else if (lastElement == "form.txt")
                        {
                            foreach (string tbl in tables)
                            {

                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix" || Program.Transactional.ContainsKey(tbl))
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file);
                                string text2 = getFormTemplate(text, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "form.tsx", text);
                                bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView");

                                if (!exists)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                if (!exists1)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView");
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DefaultView/" + "form.tsx", text2);

                            }
                        }
                        else if (lastElement == "table_gv1.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grid_view_1")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView1");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView1");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView1/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView1/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GridView1", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView1/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gv2.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grid_view_2")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView2");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView2");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView2/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView2/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GridView2", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView2/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gv3.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grid_view_3")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView3");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView3");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView3/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView3/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GridView3", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView3/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gv4.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grid_view_4")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView4");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView4");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView4/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView4/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GridView4", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView4/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gv5.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grid_view_5")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView5");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView5");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView5/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView5/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GridView5", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView5/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gv6.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grid_view_6")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView6");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView6");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView6/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView6/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GridView6", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GridView6/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_dl1.txt")
                        {
                            Console.WriteLine("**********************************INTO table_dl1.txt ******************************");
                            foreach (string tbl in tables)
                            {
                                Console.WriteLine("\n" + tbl + "\n");
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Detail_list_1")
                                {
                                    Console.WriteLine("#########IN##############");
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList1");
                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList1");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList1/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList1/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "DetailList1", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList1/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_dl2.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Detail_list_2")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList2");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList2");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList2/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList2/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "DetailList2", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList2/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_dl3.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Detail_list_3")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList3");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList3");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList3/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList3/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "DetailList3", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList3/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_dl6.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Detail_list_6")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList6");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList6");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList6/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList6/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "DetailList6", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList6/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_dl7.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Detail_list_7")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList7");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList7");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList7/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList7/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "DetailList7", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/DetailList7/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl1.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_1")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList1");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList1");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList1/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList1/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList1", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList1/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl2.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_2")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList2");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList2");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList2/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList2/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList2", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList2/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl3.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_3")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList3");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList3");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList3/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList3/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList3", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList3/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl4.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_4")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList4");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList4");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList4/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList4/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList4", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList4/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl5.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_5")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList5");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList5");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList5/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList5/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList5", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList5/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl6.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_6")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList6");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList6");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList6/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList6/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList6", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList6/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl7.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_7")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList7");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList7");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList7/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList7/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList7", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList7/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl8.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_8")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList8");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList8");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList8/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList8/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList8", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList8/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl9.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_9")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList9");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList9");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList9/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList9/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList9", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList9/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl10.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_10")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList10");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList10");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList10/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList10/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList10", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList10/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_gl11.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == "Grouped_list_11")
                                {
                                    string tbl1 = tbl.ToLower();


                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        /*for (int i = 0; i < FK.Count; i += 4)
                                        {
                                            string s = FK[i + 1];*/
                                        foreach (string s in FKC)
                                        {
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;//FK[i + 1];
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace("{primaryKeyData}", PKTemp[0]).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    //File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    bool exists1 = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList11");

                                    if (!exists1)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList11");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList11/" + "table.tsx", text);
                                    string text3 = "";
                                    if (!Program.Transactional.ContainsKey(tbl))
                                    {
                                        text3 = File.ReadAllText(src + "/../form.txt");
                                        text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    else
                                    {
                                        text3 = File.ReadAllText(src + "/../oneTransactionForm.txt");
                                        text3 = getOneTransactionalFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    }
                                    text3 = getFormTemplate(text3, tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList11/" + "form.tsx", text3);
                                    string text2 = File.ReadAllText(src + "/../component.txt");
                                    text2 = getComponentTemplate(text2, "GroupList11", tbl, src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendChoice, projectType, "");
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/GroupList11/" + "index.tsx", text2);
                                }
                            }
                        }
                        else if (lastElement == "table_index.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (FindAssociatedComponent(tableToComponentMap, tbl) != null && ProcessInput(FindAssociatedComponent(tableToComponentMap, tbl)) != null)
                                {
                                    if (FindAssociatedComponent(tableToComponentMap, tbl) == "Kanban")
                                    {


                                        string text1 = @"import React, { useState, useEffect } from ""react"";
        import Kanban from ""./kanban/indexKanban"";
        export const {modelName} = (props) => {
          const config = props.config;
          console.log(config.selectedView);
          switch (config.selectedView) {
        case ""Kanban"":
              return <Kanban {...props} />;
              break;
        }
        };
        ";
                                        text1 = text1.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                        File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/" + "index.tsx", text1);
                                        continue;
                                    }
                                    if (FindAssociatedComponent(tableToComponentMap, tbl) == "Calendar")
                                    {


                                        string text1 = @"import React, { useState, useEffect } from ""react"";
        import Calendar from ""./Calendar"";
        export const {modelName} = (props) => {
          const config = props.config;
          console.log(config.selectedView);
          switch (config.selectedView) {
        case ""Calendar"":
              return <Calendar {...props} />;
              break;
        }
        };
        ";
                                        text1 = text1.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                        File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/" + "index.tsx", text1);
                                        continue;
                                    }
                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix" || tbl1 == "s3bucket" || tbl1 == "s3bucket_folders")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    Console.WriteLine("text 1: " + text);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    string viewname = ProcessInput(FindAssociatedComponent(tableToComponentMap, tbl));
                                    text = text.Replace("{viewName}", viewname);
                                    text = text.Replace("{viewNameSmall}", char.ToLower(viewname[0]) + viewname.Substring(1));
                                    //Console.WriteLine("text 1: " + text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/" + "index.tsx", text);
                                }
                            }
                        }
                        else if (lastElement == "table_index_2.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (FindAssociatedComponent(tableToComponentMap, tbl) == null || tbl.ToLower() == "s3bucket" || tbl.ToLower() == "s3bucket_folders")
                                {

                                    string tbl1 = tbl.ToLower();

                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file);
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    //Dev1
                                    //Logic to create directory if not exist
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));

                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    File.WriteAllText(@des + @"/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/" + "index.tsx", text);

                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in Processfile in dnd section while creating files: " + e.ToString());
                    Program.errors_list.Add("Error in Processfile in dnd section while creating files: " + e.Message);
                }
            }
            else if (projectType == "workflow")
            {
                foreach (string tbl in tables)
                {
                    string tbl1 = tbl.ToLower();
                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                    {
                        continue;
                    }
                    if (!Directory.Exists(@des + "/src/components/" + tbl))
                        Directory.CreateDirectory(@des + "/src/components/" + tbl);
                    if (!Directory.Exists(@des + "/src/redux/slices/" + tbl))
                        Directory.CreateDirectory(@des + "/src/redux/slices/" + tbl);
                }
                try
                {
                    var files = Directory.GetFiles(@src + "/..");
                    foreach (string file in files)
                    {
                        List<List<string>> temp_storage = new List<List<string>>();
                        string lastElement = file.Split('/').ToList().Last();
                        Console.WriteLine(lastElement);
                        int wkid = 0;
                        if (lastElement == "GET_ALL.txt")
                        {
                            string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                            string temptext = File.ReadAllText(file);
                            string final = "[";
                            int xcod = 100, ycod = 20;
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                List<string> FK = new List<string>();
                                List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                //HashSet<string> FKC = new HashSet<string>();
                                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                //{
                                //    connection.Open();
                                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                MySqlCommand command = new MySqlCommand(sql, connection);
                                string foreignKeyColumn = "", refTable = "";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                        refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                        string dataType = reader["DATA_TYPE"].ToString();
                                        string forkey1 = reader["COLUMN_NAME"].ToString();
                                        FK.Add(foreignKeyColumn);
                                        FK.Add(refTable);
                                        //if (refTable != tbl)
                                        //    FKC.Add(refTable);
                                        FK.Add(dataType);
                                        FK.Add(forkey1);
                                    }
                                }
                                //}
                                final += temptext.Replace("{{UNIQUE_ID_FOR_SUBFLOW_GETALL}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_GETALL_1}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_GETALL_2}}", (wkid++).ToString())
                                    .Replace("{{UINQUE_ID_FOR_HTTP_GETALL}}", (wkid++).ToString());
                                final += ",";

                                string temptext2 = File.ReadAllText(src + "/../HTTP_IN_POST.txt");
                                final += temptext2
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_HTTP_IN_POST}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_HTTP_NODE_HTTP_IN_POST}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_HTTP_IN_POST}}", (wkid++).ToString());
                                final += ",";
                                string temptext3 = File.ReadAllText(src + "/../POST.txt");
                                final += temptext3
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_POST}}", (wkid++).ToString()).Replace("{{UNIQUE_ID_FOR_SUBFLOW_HTTP_IN_POST}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_POST}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_HTTP_POST}}", (wkid++).ToString());
                                final += ",";

                                string temptext4 = File.ReadAllText(src + "/../LOOP_PUT.txt");
                                string temptext5 = File.ReadAllText(src + "/../PUT_SIMPLE.txt");
                                string temptext6 = File.ReadAllText(src + "/../DELETE_SIMPLE.txt");
                                string temptext1 = File.ReadAllText(src + "/../LOOP_DELETE.txt");
                                foreach (string pk in PKTemp)
                                {
                                    final += temptext1.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", pk)
                                        .Replace("{{UNIQUE_ID_FOR_SUBFLOW_LOOP_DELETE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_FUNCTION_LOOP_DELETE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_HTTP_LOOP_DELETE}}", (wkid++).ToString());
                                    final += ",";
                                    final += temptext4.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", pk)
                                        .Replace("{{UNIQUE_ID_FOR_SUBFLOW_LOOP_PUT}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_FUNCTION_LOOP_PUT}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_HTTP_LOOP_PUT}}", (wkid++).ToString());
                                    final += ",";
                                    final += temptext5.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", pk)
                                        .Replace("{{UNIQUE_ID_FOR_SUBFLOW_PUT_SIMPLE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_FUNCTION_PUT_SIMPLE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_HTTP_PUT_SIMPLE}}", (wkid++).ToString());
                                    final += ",";
                                    final += temptext6.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", pk)
                                        .Replace("{{UNIQUE_ID_FOR_SUBFLOW_DELETE_SIMPLE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_FUNCTION_DELETE_SIMPLE}}", (wkid++).ToString())
                                        .Replace("{{UNIQUE_ID_FOR_HTTP_DELETE_SIMPLE}}", (wkid++).ToString());
                                    final += ",";
                                }
                                string ttext = File.ReadAllText(src + "/../LOOP_GET_BY_ID.txt");
                                string stext = File.ReadAllText(src + "/../GET_BY_ID_SIMPLE.txt");
                                if (FK.Count != 0)
                                {
                                    for (int i = 0; i < FK.Count; i += 4)
                                    {
                                        final += ttext.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", PKTemp[0])
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE}}", char.ToUpper(FK[i + 1][0]) + FK[i + 1].Substring(1))
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE_OBJECT}}", FK[i + 1].ToLower())
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE_KEY}}", FK[i + 3])
                                            .Replace("{{UNIQUE_ID_FOR_SUBFLOW_GET_BY_ID_REFER}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_HTTP_GET_BY_ID_REFER}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_FUNCTION_GET_BY_ID_REFER_1}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_FUNCTION_GET_BY_ID_REFER_2}}", (wkid++).ToString());
                                        final += ",";
                                        final += stext.Replace("{{SWAGGER_SCHEMA_PRIMARY_KEY}}", PKTemp[0])
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE}}", char.ToUpper(FK[i + 1][0]) + FK[i + 1].Substring(1))
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE_OBJECT}}", FK[i + 1].ToLower())
                                            .Replace("{{SWAGGER_SCHEMA_REFERENCE_KEY}}", FK[i + 3])
                                            .Replace("{{UNIQUE_ID_FOR_SUBFLOW_GET_BY_ID_REFER}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_HTTP_GET_BY_ID_REFER}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_FUNCTION_GET_BY_ID_REFER_1}}", (wkid++).ToString())
                                            .Replace("{{UNIQUE_ID_FOR_FUNCTION_GET_BY_ID_REFER_2}}", (wkid++).ToString());
                                        final += ",";
                                    }
                                }
                                string ttext1 = File.ReadAllText(src + "/../ADDED_WORKFLOW.txt");
                                final += ttext1.Replace("{{UNIQUE_ID_FOR_TAB_ADDED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_HTTPIN_ADDED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_ADDED_TRIGGER}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_INSTANCE_ADDED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_ADDED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_DEBUG_ADDED}}", (wkid++).ToString());
                                final += ",";
                                string ttext2 = File.ReadAllText(src + "/../UPDATED_WORKFLOW.txt");
                                final += ttext2.Replace("{{UNIQUE_ID_FOR_TAB_UPDATED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_HTTPIN_UPDATED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_UPDATED_TRIGGER}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_INSTANCE_UPDATED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_UPDATED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_DEBUG_UPDATED}}", (wkid++).ToString());
                                final += ",";
                                string ttext3 = File.ReadAllText(src + "/../DELETED_WORKFLOW.txt");
                                final += ttext3.Replace("{{UNIQUE_ID_FOR_TAB_DELETED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_HTTPIN_DELETED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_DELETED_TRIGGER}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_SUBFLOW_INSTANCE_DELETED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_FUNCTION_DELETED}}", (wkid++).ToString())
                                    .Replace("{{UNIQUE_ID_FOR_DEBUG_DELETED}}", (wkid++).ToString());
                                final += ",";
                                ycod += 10;
                                final = final.Replace("{{SWAGGER_SCHEMA_NAME}}", char.ToUpper(tbl1[0]) + tbl1.Substring(1))
                                    .Replace("{{SWAGGER_SCHEMA_OBJECT}}", tbl1).Replace("{{SWAGGER_URL}}", swaggerurl);
                            }
                            string final1 = final.Substring(0, final.Length - 1);
                            final1 += "]";
                            File.WriteAllText(@des + "/flows.json", final1);
                        }
                        else if (lastElement == "settings.txt")
                        {
                            string temptext = File.ReadAllText(file);
                            File.WriteAllText(@des + "/settings.js", temptext);
                        }
                        else if (lastElement == "service.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), passit = new List<string>();
                                for (int i = 0; i < primaryKeys.Count; i++)
                                {
                                    int x = i + 1;
                                    passit.Add("id" + x);
                                }
                                passit.Add("data");
                                string serviceFunction = Generate("getAll", tbl, 2, new List<string> { "pageno", "pagesize" }, "get", "read", backendChoice) + Generate("getOne", tbl, 1, new List<string> { "id" }, "get", "read_one", backendChoice) + Generate("search", tbl, 3, new List<string> { "key", "pageno", "pagesize" }, "get", "search", backendChoice) + Generate("add", tbl, 1, new List<string> { "data" }, "post", "create", backendChoice) + Generate("update", tbl, passit.Count, passit, "put", "update", backendChoice) + Generate("delete", tbl, primaryKeys.Count, primaryKeys, "delete", "delete", backendChoice) + Generate("filter", tbl, 1, new List<string> { "data" }, "post", "create", backendChoice);
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{serviceFunction}", serviceFunction);
                                File.WriteAllText(@des + "/src/services/" + tbl + "Service.ts", text);
                            }
                        }
                        else if (lastElement == "auth.txt")
                        {
                            File.WriteAllText(@des + "/src/services/" + "auth" + "Service.ts", File.ReadAllText(file));
                        }
                        else if (lastElement == "slice.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();

                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";", tableInterface = "export interface I" + char.ToUpper(tbl[0]) + tbl.Substring(1) + " {\n", tableInterfaceData = "";
                                List<string> PK = GetPrimaryKey(connectionString, tbl), type = GetPrimaryKeyType(connectionString, tbl), cols = GetAllColumns(connectionString, tbl);
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    if (cols[i + 2] == "YES")
                                    {
                                        string dataType = utility.GetJSType(cols[i + 1]).ToLower();
                                        if (dataType.Contains("date"))
                                            dataType = "Date";
                                        tableInterface += cols[i] + "?:" + dataType + ",\n";
                                    }
                                    else
                                    {
                                        string dataType = utility.GetJSType(cols[i + 1]).ToLower();
                                        if (dataType.Contains("date"))
                                            dataType = "Date";
                                        tableInterface += cols[i] + ":" + dataType + ",\n";
                                    }
                                    tableInterfaceData += cols[i] + ":null,\n";

                                }
                                tableInterface += "}";
                                //Dev2
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{tableInterface}", tableInterface).Replace("{tableInterfaceData}", tableInterfaceData);
                                File.WriteAllText(@des + "/src/redux/slices/" + tbl + "/index.ts", text);
                                IEnumerable<string> lines = File.ReadLines(@des + "/src/redux/slices/" + tbl + "/index.ts");
                                string secondLastLine = lines.Reverse().Skip(2).First();
                                string temp = "", path = "";
                                Stack<char> st = new Stack<char>();
                                for (int i = 0; i < secondLastLine.Count(); i++)
                                {
                                    if (secondLastLine[i] == '{')
                                    {
                                        while (i < secondLastLine.Count() && secondLastLine[i] != '}')
                                            temp += secondLastLine[i++];
                                        temp += "}";
                                        break;
                                    }
                                }
                                path = "redux/slices/" + tbl;
                                List<string> l = new List<string>();
                                l.Add(temp);
                                l.Add(path);
                                temp_storage.Add(l);
                            }
                            string text1 = File.ReadAllText(Path.Combine(file, "../actions.txt"));
                            string replacement = "";
                            for (int i = 0; i < temp_storage.Count; i++)
                            {
                                replacement += "export " + temp_storage[i][0] + " from '" + temp_storage[i][1] + "';\n";
                            }
                            text1 = text1.Replace("{actionList}", replacement);
                            File.WriteAllText(@des + "/src/redux/actions.ts", text1);
                            text1 = File.ReadAllText(Path.Combine(file, "../reducers.txt"));
                            replacement = "";
                            string second = "";
                            for (int i = temp_storage.Count - 1; i >= 0; i--)
                            {
                                string last = temp_storage[i][1].Split("/").Last();
                                replacement += "import " + last + " from '" + temp_storage[i][1] + "';\n";
                                second += last;
                                if (i != 0)
                                    second += ",";
                            }
                            text1 = text1.Replace("{reducerImport}", replacement);
                            text1 = text1.Replace("{reducerList}", second);
                            File.WriteAllText(@des + "/src/redux/reducers.ts", text1);
                            text1 = File.ReadAllText(Path.Combine(file, "../import.txt"));
                            replacement = "";
                            //for (int i = temp_storage.Count - 1; i >= 0; i--)
                            //{
                            //    string last = temp_storage[i][1].Split("/").Last();
                            //    replacement += "export { " + char.ToUpper(last[0]) + last.Substring(1) + " } from \"" + "./" + last + "\";\n";
                            //}
                            //text1 = text1.Replace("{importComponent}", replacement);
                            text1 = text1.Replace("{importComponent}", "");
                            File.WriteAllText(@des + "/src/components/index.ts", text1);
                            text1 = File.ReadAllText(Path.Combine(file, "../routes.txt"));
                            replacement = "";
                            string new_rep = "import { ";
                            for (int i = temp_storage.Count - 1; i >= 0; i--)
                            {
                                string last = temp_storage[i][1].Split("/").Last();
                                new_rep += char.ToUpper(last[0]) + last.Substring(1);
                                if (i != 0)
                                    new_rep += ", ";
                                if (i == 0)
                                    new_rep += " } from \"components\";";
                                replacement += "<Route path=\"/" + last + "\" element={<AuthenticatedRoute element={<" + char.ToUpper(last[0]) + last.Substring(1) + "/>} />}></Route>\n";

                            }
                            text1 = text1.Replace("{routePathList}", replacement);
                            text1 = text1.Replace("{importComponents}", new_rep);
                            File.WriteAllText(@des + "/src/pages/index.tsx", text1);
                            string rep = "";
                            for (int i = temp_storage.Count - 1; i >= 0; i--)
                            {
                                string last = temp_storage[i][1].Split("/").Last();
                                string template = "{ title: '{modelName}', path: '/{tableName}', icon: 'fas fa-fw fa-table', subMenu: [] },\n";
                                template = template.Replace("{modelName}", char.ToUpper(last[0]) + last.Substring(1));
                                template = template.Replace("{tableName}", last);
                                rep += template;
                            }
                            string text2 = File.ReadAllText(Path.Combine(file, "../menu.txt"));
                            text2 = text2.Replace("{menuItems}", rep);
                            File.WriteAllText(@des + "/src/template/" + "MenuItems.ts", text2);
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in Processfile in workflow section while creating files: " + e.ToString());
                    Program.errors_list.Add("Error in Processfile in workflow section while creating files: " + e.Message);
                }
            }
            else
            {
                foreach (string tbl in tables)
                {
                    string tbl1 = tbl.ToLower();
                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                    {
                        continue;
                    }
                    if (!Directory.Exists(@des + "/src/components/" + tbl))
                        Directory.CreateDirectory(@des + "/src/components/" + tbl);
                    if (!Directory.Exists(@des + "/src/redux/slices/" + tbl))
                        Directory.CreateDirectory(@des + "/src/redux/slices/" + tbl);
                }
                try
                {
                    var files = Directory.GetFiles(@src + "/..");
                    foreach (string file in files)
                    {
                        List<List<string>> temp_storage = new List<List<string>>();
                        Console.WriteLine(file);
                        string lastElement = file.Split("/").ToList().Last();
                        Console.WriteLine(lastElement);

                        if (lastElement == "service.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), passit = new List<string>();
                                for (int i = 0; i < primaryKeys.Count; i++)
                                {
                                    int x = i + 1;
                                    passit.Add("id" + x);
                                }
                                passit.Add("data");
                                string serviceFunction = Generate("getAll", tbl, 2, new List<string> { "pageno", "pagesize" }, "get", "read", backendChoice) + Generate("getOne", tbl, 1, new List<string> { "id" }, "get", "read_one", backendChoice) + Generate("search", tbl, 3, new List<string> { "key", "pageno", "pagesize" }, "get", "search", backendChoice) + Generate("add", tbl, 1, new List<string> { "data" }, "post", "create", backendChoice) + Generate("update", tbl, passit.Count, passit, "put", "update", backendChoice) + Generate("delete", tbl, primaryKeys.Count, primaryKeys, "delete", "delete", backendChoice) + Generate("filter", tbl, 1, new List<string> { "data" }, "post", "create", backendChoice);
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{serviceFunction}", serviceFunction);
                                File.WriteAllText(@des + "/src/services/" + tbl + "Service.ts", text);
                            }
                        }
                        else if (lastElement == "auth.txt")
                        {
                            File.WriteAllText(@des + "/src/services/" + "auth" + "Service.ts", File.ReadAllText(file));
                        }
                        else if (lastElement == "slice.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();

                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file), connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";", tableInterface = "export interface I" + char.ToUpper(tbl[0]) + tbl.Substring(1) + " {\n", tableInterfaceData = "";
                                List<string> PK = GetPrimaryKey(connectionString, tbl), type = GetPrimaryKeyType(connectionString, tbl), cols = GetAllColumns(connectionString, tbl);
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    if (cols[i + 2] == "YES")
                                    {
                                        string dataType = utility.GetJSType(cols[i + 1]).ToLower();
                                        if (dataType.Contains("date"))
                                            dataType = "Date";
                                        tableInterface += cols[i] + "?:" + dataType + ",\n";
                                    }
                                    else
                                    {
                                        string dataType = utility.GetJSType(cols[i + 1]).ToLower();
                                        if (dataType.Contains("date"))
                                            dataType = "Date";
                                        tableInterface += cols[i] + ":" + dataType + ",\n";
                                    }
                                    tableInterfaceData += cols[i] + ":null,\n";

                                }
                                tableInterface += "}";
                                //Dev2
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{tableInterface}", tableInterface).Replace("{tableInterfaceData}", tableInterfaceData);
                                File.WriteAllText(@des + "/src/redux/slices/" + tbl + "/index.ts", text);
                                IEnumerable<string> lines = File.ReadLines(@des + "/src/redux/slices/" + tbl + "/index.ts");
                                string secondLastLine = lines.Reverse().Skip(2).First();
                                string temp = "", path = "";
                                Stack<char> st = new Stack<char>();
                                for (int i = 0; i < secondLastLine.Count(); i++)
                                {
                                    if (secondLastLine[i] == '{')
                                    {
                                        while (i < secondLastLine.Count() && secondLastLine[i] != '}')
                                            temp += secondLastLine[i++];
                                        temp += "}";
                                        break;
                                    }
                                }
                                path = "redux/slices/" + tbl;
                                List<string> l = new List<string>();
                                l.Add(temp);
                                l.Add(path);
                                temp_storage.Add(l);
                            }
                            string text1 = File.ReadAllText(Path.Combine(file, "../actions.txt"));
                            string replacement = "";
                            for (int i = 0; i < temp_storage.Count; i++)
                            {
                                replacement += "export " + temp_storage[i][0] + " from '" + temp_storage[i][1] + "';\n";
                            }
                            text1 = text1.Replace("{actionList}", replacement);
                            File.WriteAllText(@des + "/src/redux/actions.ts", text1);
                            text1 = File.ReadAllText(Path.Combine(file, "../reducers.txt"));
                            replacement = "";
                            string second = "";
                            for (int i = temp_storage.Count - 1; i >= 0; i--)
                            {
                                string last = temp_storage[i][1].Split("/").Last();
                                replacement += "import " + last + " from '" + temp_storage[i][1] + "';\n";
                                second += last;
                                if (i != 0)
                                    second += ",";
                            }
                            text1 = text1.Replace("{reducerImport}", replacement);
                            text1 = text1.Replace("{reducerList}", second);
                            File.WriteAllText(@des + "/src/redux/reducers.ts", text1);
                            text1 = File.ReadAllText(Path.Combine(file, "../import.txt"));
                            replacement = "";
                            for (int i = temp_storage.Count - 1; i >= 0; i--)
                            {
                                string last = temp_storage[i][1].Split("/").Last();
                                replacement += "export { " + char.ToUpper(last[0]) + last.Substring(1) + " } from \"" + "./" + last + "\";\n";
                            }
                            text1 = text1.Replace("{importComponent}", replacement);
                            //text1 = text1.Replace("{importComponent}", "");
                            File.WriteAllText(@des + "/src/components/index.ts", text1);
                            text1 = File.ReadAllText(Path.Combine(file, "../routes.txt"));
                            replacement = "";
                            string new_rep = "import { ";
                            for (int i = temp_storage.Count - 1; i >= 0; i--)
                            {
                                string last = temp_storage[i][1].Split("/").Last();
                                new_rep += char.ToUpper(last[0]) + last.Substring(1);
                                if (i != 0)
                                    new_rep += ", ";
                                if (i == 0)
                                    new_rep += " } from \"components\";";
                                replacement += "<Route path=\"/" + last + "\" element={<AuthenticatedRoute element={<" + char.ToUpper(last[0]) + last.Substring(1) + "/>} />}></Route>\n";

                            }
                            text1 = text1.Replace("{routePathList}", replacement);
                            text1 = text1.Replace("{importComponents}", new_rep);
                            File.WriteAllText(@des + "/src/pages/index.tsx", text1);
                            string rep = "";
                            for (int i = temp_storage.Count - 1; i >= 0; i--)
                            {
                                string last = temp_storage[i][1].Split("/").Last();
                                string template = "{ title: '{modelName}', path: '/{tableName}', icon: 'fas fa-fw fa-table', subMenu: [] },\n";
                                template = template.Replace("{modelName}", char.ToUpper(last[0]) + last.Substring(1));
                                template = template.Replace("{tableName}", last);
                                rep += template;
                            }
                            string text2 = File.ReadAllText(Path.Combine(file, "../menu.txt"));
                            text2 = text2.Replace("{menuItems}", rep);
                            File.WriteAllText(@des + "/src/template/" + "MenuItems.ts", text2);
                        }
                        else if (lastElement == "component.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file);
                                string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                text = text.Replace("{tableName}", tbl);
                                List<string> FK = new List<string>();
                                HashSet<string> FKC = new HashSet<string>();
                                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                //{
                                //    connection.Open();
                                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                MySqlCommand command = new MySqlCommand(sql, connection);
                                string foreignKeyColumn = "", refTable = "";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                        refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                        string dataType = reader["DATA_TYPE"].ToString();
                                        string forkey1 = reader["COLUMN_NAME"].ToString();
                                        FK.Add(foreignKeyColumn);
                                        FK.Add(refTable);
                                        if (refTable != tbl)
                                            FKC.Add(refTable);
                                        FK.Add(dataType);
                                        FK.Add(forkey1);
                                    }
                                }
                                // }
                                if (FK.Count != 0)
                                {
                                    string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                    string final = "";
                                    foreach (string s in FKC)
                                    {
                                        //string s = FK[i + 1];
                                        string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                        string template1 = template.Replace("{table1}", table1);
                                        final += template1;
                                    }
                                    text = text.Replace("{importFKRedux}", final);
                                }
                                else
                                    text = text.Replace("{importFKRedux}", "\n");
                                if (FK.Count != 0)
                                {
                                    string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                    string final = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        //string s = FK[i + 1];
                                        string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                        string FKK = s;// FK[i + 1];
                                        string template1 = "";
                                        template1 = template.Replace("{table2}", table1);
                                        template1 = template1.Replace("{FK}", FKK);
                                        final += template1;
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(template1);
                                    }
                                    text = text.Replace("{importFKService}", final);
                                }
                                else
                                    text = text.Replace("{importFKService}", "\n");
                                if (FK.Count != 0)
                                {
                                    string final = "";
                                    string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                    //for (int i = 1; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        //string text1 = template.Replace("{FK}", FK[i]);
                                        string text1 = template.Replace("{FK}", s);
                                        final += text1;
                                    }
                                    if (tbl == "projects_repositories")
                                        Console.WriteLine(final);
                                    ; text = text.Replace("{fkReduxInit}", final);
                                }
                                else
                                    text = text.Replace("{fkReduxInit}", "\n");
                                string abcd = "";
                                //for (int i = 0; i < FK.Count; i += 4)
                                foreach (string s in FKC)
                                {
                                    string tableName = s;
                                    string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                    string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                    string pageNumber = "Constant.defaultPageNumber";
                                    string pageSize = "Constant.defaultDropdownPageSize";
                                    string searchKey = "''";
                                    string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                    formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                    abcd += formControl;
                                }
                                if (FK.Count == 0)
                                    text = text.Replace("{useEffectForFK}", "\n");
                                else
                                    text = text.Replace("{useEffectForFK}", abcd);
                                List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                string tableCols = "";
                                for (int i = 0; i < columns.Count; i += 3)
                                {
                                    bool temp = false; int j = 0;
                                    for (j = 0; j < FK.Count; j += 4)
                                    {
                                        if (FK[j] == columns[i])
                                        {
                                            temp = true;
                                            break;
                                        }
                                    }
                                    if (temp)
                                        tableCols += "{name: config." + columns[i] + "_new_name ? config." + columns[i] + "_new_name:'" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true,cell: (row) =>" + FK[j + 1] + "Data.list.map((element) => { if(element." + columns[i] + " === row." + columns[i] + "){ return (<span> {config[\"" + columns[i] + "_ref\"] === undefined? row." + columns[i] + ": element[config[\"" + columns[i] + "_ref\"]]} </span>); } }),visible: config.hasOwnProperty(\"" + columns[i] + "_visible\") ? !config." + columns[i] + "_visible: false},\n";
                                    else
                                        tableCols += "{name: config." + columns[i] + "_new_name ? config." + columns[i] + "_new_name:'" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true,visible: config.hasOwnProperty(\"" + columns[i] + "_visible\") ? config." + columns[i] + "_visible: true},\n";

                                }
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{tableColumn}", tableCols);
                                File.WriteAllText(@des + "/src/components/" + tbl + "/" + "index.tsx", text);
                                bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));

                                if (!exists)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/" + "index.tsx", text);
                            }
                        }
                        else if (lastElement == "table.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                if (tbl.ToLower() == "s3bucket" || tbl.ToLower() == "s3bucket_folders")
                                {
                                    string tbl1 = tbl.ToLower();
                                    if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                    {
                                        continue;
                                    }
                                    string text = File.ReadAllText(file), connectionString = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                    List<string> FK = new List<string>();
                                    HashSet<string> FKC = new HashSet<string>();
                                    //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                    //{
                                    //    connection.Open();
                                    MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                    List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                    string sql = @"SELECT DISTINCT
        kcu.CONSTRAINT_NAME,
        kcu.COLUMN_NAME,
        kcu.REFERENCED_TABLE_NAME,
        kcu.REFERENCED_COLUMN_NAME,
        cols.DATA_TYPE
    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
    JOIN INFORMATION_SCHEMA.COLUMNS AS cols
        ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
    WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                    MySqlCommand command = new MySqlCommand(sql, connection);
                                    string foreignKeyColumn = "", refTable = "";
                                    using (MySqlDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                            refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                            string dataType = reader["DATA_TYPE"].ToString();
                                            string forkey1 = reader["COLUMN_NAME"].ToString();
                                            FK.Add(foreignKeyColumn);
                                            FK.Add(refTable);
                                            if (refTable != tbl)
                                                FKC.Add(refTable);
                                            FK.Add(dataType);
                                            FK.Add(forkey1);
                                        }
                                    }
                                    //}
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                        string final = "";
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string template1 = template.Replace("{table1}", table1);
                                            final += template1;
                                        }
                                        text = text.Replace("{importFKRedux}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKRedux}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                        string final = "";
                                        //for (int i = 0; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string s = FK[i + 1];
                                            string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                            string FKK = s;// FK[i + 1];
                                            string template1 = "";
                                            template1 = template.Replace("{table2}", table1);
                                            template1 = template1.Replace("{FK}", FKK);
                                            final += template1;
                                            if (tbl == "projects_repositories")
                                                Console.WriteLine(template1);
                                        }
                                        text = text.Replace("{importFKService}", final);
                                    }
                                    else
                                        text = text.Replace("{importFKService}", "\n");
                                    if (FK.Count != 0)
                                    {
                                        string final = "";
                                        string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                        //for (int i = 1; i < FK.Count; i += 4)
                                        foreach (string s in FKC)
                                        {
                                            //string text1 = template.Replace("{FK}", FK[i]);
                                            string text1 = template.Replace("{FK}", s);
                                            final += text1;
                                        }
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(final);
                                        ; text = text.Replace("{fkReduxInit}", final);
                                    }
                                    else
                                        text = text.Replace("{fkReduxInit}", "\n");
                                    string abcd = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        string tableName = s;
                                        string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                        string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                        string pageNumber = "Constant.defaultPageNumber";
                                        string pageSize = "Constant.defaultDropdownPageSize";
                                        string searchKey = "''";
                                        string formControl = $@"
    useEffect(() => {{
        if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
            dispatch(reset{modelName}ToInit());
            get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
                if (response && response.records) {{
                    dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
                }} else {{
                    dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
                }}
            }})
        }}
    }},[{tableName}Data.list.length])" + "\n";
                                        formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                        abcd += formControl;
                                    }
                                    if (FK.Count == 0)
                                        text = text.Replace("{useEffectForFK}", "\n");
                                    else
                                        text = text.Replace("{useEffectForFK}", abcd);
                                    List<string> primaryKeys = GetPrimaryKey(connectionString, tbl), columns = GetAllColumns(connectionString, tbl);
                                    string PriListParams = string.Join(",", primaryKeys.Select(pk => $"rowData.{pk}"));
                                    string tableCols = "";
                                    for (int i = 0; i < columns.Count; i += 3)
                                    {
                                        tableCols += "{name: '" + columns[i] + "', selector: row => row." + columns[i] + ", sortable: true},\n";
                                    }
                                    text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1)).Replace("{tableName}", tbl).Replace("{primaryKeyList}", PriListParams).Replace("{tableColumn}", tableCols).Replace(",[rData.list.length]", "").Replace("setSearch('');", "");
                                    File.WriteAllText(@des + "/src/components/" + tbl + "/" + "table.tsx", text);
                                    bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    if (!exists)
                                        System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                    File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/" + "table.tsx", text);
                                }
                            }
                        }
                        else if (lastElement == "constants.txt")
                        {
                            string text = File.ReadAllText(file);
                            string import_replacement = "", functionMap = "", mapComponent = "", InterfaceReplacement = "", columnConfig = "";
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                List<string> FK = new List<string>();
                                HashSet<string> FKC = new HashSet<string>();
                                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                //{
                                //    connection.Open();
                                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                MySqlCommand command = new MySqlCommand(sql, connection);
                                string foreignKeyColumn = "", refTable = "";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                        refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                        string dataType = reader["DATA_TYPE"].ToString();
                                        string forkey1 = reader["COLUMN_NAME"].ToString();
                                        FK.Add(foreignKeyColumn);
                                        FK.Add(refTable);
                                        if (refTable != tbl)
                                            FKC.Add(refTable);
                                        FK.Add(dataType);
                                        FK.Add(forkey1);
                                    }
                                }
                                //}
                                columnConfig += $"{char.ToUpper(tbl[0]) + tbl.Substring(1)} :" + "{innerContent:{\"input-type\":\"text\",},headerSize:{\"input-type\":\"list\",options:[\"h1\",\"h2\",\"h3\",\"h4\",\"h5\",\"h6\"],},backgroundColor:{\"input-type\":\"heading-color\"},color:{\"input-type\":\"heading-color\"},tableHeadBackgroundColor:{\"input-type\":\"table-head-color\",},HeadColor:{\"input-type\":\"table-head-color\",},tableBackgroundColor:{\"input-type\":\"table-color\",},HeadRowBackgroundColor:{\"input-type\":\"table-color\",},HeadRowColor:{\"input-type\":\"row-color\",},RowBackgroundColor:{\"input-type\":\"row-color\",},RowColor:{\"input-type\":\"row-color\",},fontFamily:{\"input-type\":\"list\",options:[\"Arial\",\"Helvetica\",\"Verdana\",\"Georgia\",\"CourierNew\",\"cursive\",],},";
                                columnConfig += "columns: {\"input-type\": \"group\",\"columns-list\":[";
                                List<string> columns = GetAllColumns(connectionString, tbl);
                                for (int i = 0; i < columns.Count; i += 3)
                                {
                                    bool temp = false; int j = 0;
                                    for (j = 0; j < FK.Count; j += 4)
                                    {
                                        if (FK[j + 3] == columns[i])
                                        {
                                            temp = true;
                                            break;
                                        }
                                    }
                                    if (temp)
                                        columnConfig += "{name: '" + columns[i] + "',fkey:true,icontrol: getList('" + columns[i + 1] + "'),type: '" + columns[i + 1] + "',slice: '" + FK[j + 1] + "',},";
                                    else
                                        columnConfig += "{name: '" + columns[i] + "',fkey:false,icontrol: getList('" + columns[i + 1] + "'),type: '" + columns[i + 1] + "',slice: '',},";
                                }
                                columnConfig += "],\"error-control-list\": [\"password\", \"email\", \"text\", \"number\"],},row:{\"input-type\": \"filter-form\",\"columns-list\": [";
                                for (int i = 0; i < columns.Count; i += 3)
                                {
                                    columnConfig += "'" + columns[i] + "',";
                                }
                                columnConfig += "],\"column-condition\": [\"==\", \"!=\", \">\",\"<\"],}},";


                                //import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList}} from \"../../Draggable Components/syncfusion_components/DropDownList/{tbl}DropDownList\";" + "\n";
                                //import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView}} from \"../../Draggable Components/syncfusion_components/Grid/{tbl}GridView\";" + "\n";
                                //import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView}} from \"../../Draggable Components/syncfusion_components/ListView/{tbl}ListView\";" + "\n";
                                //import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete}} from \"Dnd/Draggable Components/syncfusion_components/Autocomplete/{tbl}AutoComplete\";" + "\n";
                                //import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder}} from \"Dnd/Draggable Components/syncfusion_components/QueryBuilder/{tbl}QueryBuilder\";" + "\n";
                                import_replacement += $"import {{{char.ToUpper(tbl[0]) + tbl.Substring(1)}}} from \"Dnd/Draggable Components/Previous_Components/CustomComponents/{char.ToUpper(tbl[0]) + tbl.Substring(1)}\";" + "\n";
                                functionMap += $@"/*case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList config={{config}} />
            break;*/ " + "\n\t";
                                functionMap += $@"/*case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView config={{config}} />
            break; */" + "\n\t";
                                functionMap += $@"/*case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView config={{config}} />
            break; */" + "\n\t";
                                functionMap += $@"/*case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete config={{config}} />
            break; */" + "\n\t";
                                functionMap += $@"/*case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder config={{config}} />
            break;*/ " + "\n\t";
                                functionMap += $@"case ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}"":
            return < {char.ToUpper(tbl[0]) + tbl.Substring(1)} config={{config}} />
            break; " + "\n\t";
                                //mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList," + "\n\t";
                                //mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView," + "\n\t";
                                //mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView," + "\n\t";
                                //mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete," + "\n\t";
                                //mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder," + "\n\t";
                                mapComponent += $"\"{char.ToUpper(tbl[0]) + tbl.Substring(1)}\": {char.ToUpper(tbl[0]) + tbl.Substring(1)}," + "\n\t";
                                InterfaceReplacement += @$"/*{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)}DropDownList,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}DropDownList"",
            icon_name: ""ApiIcon"",
        }},
    }},*/" + "\n\t";
                                InterfaceReplacement += @$"/*{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)}GridView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}GridView"",
            icon_name: ""ApiIcon"",
        }},
    }},*/" + "\n\t";
                                InterfaceReplacement += @$"/*{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)}ListView,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}ListView"",
            icon_name: ""ApiIcon"",
        }},
    }},*/" + "\n\t";
                                InterfaceReplacement += @$"/*{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)}AutoComplete,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}AutoComplete"",
            icon_name: ""ApiIcon"",
        }},
    }},*/" + "\n\t";
                                InterfaceReplacement += @$"/*{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)}QueryBuilder,
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}QueryBuilder"",
            icon_name: ""ApiIcon"",
        }},
    }},*/" + "\n\t";
                                InterfaceReplacement += @$"{{
        id: shortid.generate(),
        type: SIDEBAR_ITEM,
        component: {{
            type: ""{char.ToUpper(tbl[0]) + tbl.Substring(1)}"",
            content: {char.ToUpper(tbl[0]) + tbl.Substring(1)},
            icon: < ApiIcon className = 'dnd sidebarIcon' />,
            component_name: ""{tbl}"",
            icon_name: ""ApiIcon"",
        }},
    }}," + "\n\t";
                            }
                            mapComponent = mapComponent.Substring(0, mapComponent.Length - 1);
                            text = text.Replace("{InterfaceReplacement}", InterfaceReplacement).Replace("{importComponents}", import_replacement).Replace("{functionTOmap}", functionMap).Replace("{mapNametoComponent}", mapComponent).Replace("{columnConfig}", columnConfig);
                            File.WriteAllText(@des + "/src/Dnd/Dnd Designer/Utility/constants.tsx", text);
                        }
                        else if (lastElement == "form.txt")
                        {
                            foreach (string tbl in tables)
                            {
                                string tbl1 = tbl.ToLower();
                                if (tbl1 == "messagequeue" || tbl1 == "dnd_ui_versions" || tbl1 == "workflows" || tbl1 == "project_dnd_ui_versions" || tbl1 == "workflow" || tbl1 == "workflow_builds" || tbl1 == "workflows_projects" || tbl1 == "workflow_runs" || tbl1 == "workflow_deployments" || tbl1 == "workflow_triggers" || tbl1 == "workflow_trigger_conditions" || tbl1 == "roles" || tbl1 == "users" || tbl1 == "entities" || tbl1 == "permissionmatrix")
                                {
                                    continue;
                                }
                                string text = File.ReadAllText(file);
                                string connectionString = "server=" + server + ";uid=" + uid + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                                text = text.Replace("{modelName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                                text = text.Replace("{tableName}", tbl);
                                List<string> FK = new List<string>();
                                HashSet<string> FKC = new HashSet<string>();
                                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                                //{
                                //    connection.Open();
                                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                                List<string> PKTemp = GetPrimaryKey(connectionString, tbl);
                                string sql = @"SELECT DISTINCT
    kcu.CONSTRAINT_NAME,
    kcu.COLUMN_NAME,
    kcu.REFERENCED_TABLE_NAME,
    kcu.REFERENCED_COLUMN_NAME,
    cols.DATA_TYPE
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS kcu
JOIN INFORMATION_SCHEMA.COLUMNS AS cols
    ON kcu.TABLE_NAME = cols.TABLE_NAME AND kcu.COLUMN_NAME = cols.COLUMN_NAME
WHERE kcu.TABLE_NAME = '" + tbl + "' AND kcu.TABLE_SCHEMA='" + databaseName + "' AND kcu.REFERENCED_TABLE_NAME IS NOT NULL;";
                                MySqlCommand command = new MySqlCommand(sql, connection);
                                string foreignKeyColumn = "", refTable = "";
                                using (MySqlDataReader reader = command.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        foreignKeyColumn = reader["REFERENCED_COLUMN_NAME"].ToString();
                                        refTable = reader["REFERENCED_TABLE_NAME"].ToString();
                                        string dataType = reader["DATA_TYPE"].ToString();
                                        string forkey1 = reader["COLUMN_NAME"].ToString();
                                        FK.Add(foreignKeyColumn);
                                        FK.Add(refTable);
                                        if (refTable != tbl)
                                            FKC.Add(refTable);
                                        FK.Add(dataType);
                                        FK.Add(forkey1);
                                    }
                                }
                                //}
                                if (FK.Count != 0)
                                {
                                    string template = "import { reset" + "{table1}" + "ToInit, set" + "{table1}" + "List, set" + "{table1}" + "Message } from \"redux/actions\";\n";
                                    string final = "";
                                    foreach (string s in FKC)
                                    {
                                        //string s = FK[i + 1];
                                        string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                        string template1 = template.Replace("{table1}", table1);
                                        final += template1;
                                    }
                                    text = text.Replace("{importFKRedux}", final);
                                }
                                else
                                    text = text.Replace("{importFKRedux}", "\n");
                                if (FK.Count != 0)
                                {
                                    string template = "import { get" + "{table2}" + " } from \"services/" + "{FK}" + "Service\";\n";
                                    string final = "";
                                    //for (int i = 0; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        //string s = FK[i + 1];
                                        string table1 = char.ToUpper(s[0]) + s.Substring(1);
                                        string FKK = s;// FK[i + 1];
                                        string template1 = "";
                                        template1 = template.Replace("{table2}", table1);
                                        template1 = template1.Replace("{FK}", FKK);
                                        final += template1;
                                        if (tbl == "projects_repositories")
                                            Console.WriteLine(template1);
                                    }
                                    text = text.Replace("{importFKService}", final);
                                }
                                else
                                    text = text.Replace("{importFKService}", "\n");
                                List<string> cols = GetAllColumns(connectionString, tbl);
                                List<string> PK = GetPrimaryKey(connectionString, tbl);
                                List<string> PrimaryKeyDatatypes = GetPrimaryKeyType(connectionString, tbl);
                                string valid = "";
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    if (FK.Contains(cols[i]))
                                    { }
                                    else if (PK.Contains(cols[i]))
                                    { }
                                    else
                                    {
                                        valid += cols[i] + ": yup." + utility.GetJSType(cols[i + 1]).ToLower() + "()";
                                        //if (cols[i + 2] != "YES")
                                        //    valid += ".required('" + cols[i] + " is required')";
                                        valid += ",\n";
                                    }
                                }
                                HashSet<string> uniquePairs = new HashSet<string>();
                                for (int i = 0; i < FK.Count; i += 4)
                                {
                                    if (uniquePairs.Contains(FK[i] + "|" + FK[i + 1]) || !FKC.Contains(FK[i + 1]))
                                        continue;
                                    uniquePairs.Add(FK[i] + "|" + FK[i + 1]);
                                    valid += FK[i] + ": yup." + "string" + "()" + ".required('" + FK[i + 1] + " is required'),\n";
                                }
                                //{CustomerId: yup.number(),PaymentId: yup.number().required('PaymentId is required'),DateCreated: yup.date(),DateShipped: yup.date().required('DateShipped is required'),ShippingId: yup.string().required('ShippingId is required'),Status: yup.string().required('Status is required'),}),}); ";
                                text = text.Replace("{yupValidationList}", valid);
                                string result = "";
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    if (utility.GetvalJSType(cols[i + 1]) == "date")
                                        result += cols[i] + ":format(new Date(), \"yyyy-MM-dd\")";
                                    else
                                        result += cols[i] + ":''";
                                    if (i != cols.Count - 3)
                                        result += ",";
                                }
                                text = text.Replace("{ColumnListWithValue}", result);
                                if (FK.Count != 0)
                                {
                                    string final = "";
                                    string template = "const " + "{FK}" + "Data = useSelector((state: RootState) => state." + "{FK}" + ");\n";
                                    //for (int i = 1; i < FK.Count; i += 4)
                                    foreach (string s in FKC)
                                    {
                                        //string text1 = template.Replace("{FK}", FK[i]);
                                        string text1 = template.Replace("{FK}", s);
                                        final += text1;
                                    }
                                    if (tbl == "projects_repositories")
                                        Console.WriteLine(final);
                                    ; text = text.Replace("{fkReduxInit}", final);
                                }
                                else
                                    text = text.Replace("{fkReduxInit}", "\n");
                                string abcd = "";
                                //for (int i = 0; i < FK.Count; i += 4)
                                foreach (string s in FKC)
                                {
                                    string tableName = s;
                                    string currtblName = char.ToUpper(tbl[0]) + tbl.Substring(1);
                                    string modelName = char.ToUpper(s[0]) + s.Substring(1);
                                    string pageNumber = "Constant.defaultPageNumber";
                                    string pageSize = "Constant.defaultDropdownPageSize";
                                    string searchKey = "''";
                                    string formControl = $@"
useEffect(() => {{
    if ({tableName}Data && {tableName}Data.list && {tableName}Data.list.length === 0) {{
        dispatch(reset{modelName}ToInit());
        get{modelName}({pageNumber}, {pageSize}, {searchKey}).then((response) => {{
            if (response && response.records) {{
                dispatch(set{modelName}List({{ pageNo: {pageNumber}, pageSize: {pageSize}, list: response.records, totalCount: response.total_count, searchKey: {searchKey} }}));
            }} else {{
                dispatch(set{currtblName}Message(""No Record Found For {modelName}""));
            }}
        }})
    }}
}},[{tableName}Data.list.length])" + "\n";
                                    formControl = formControl.Replace("{tableName}", tableName).Replace("{modelName}", modelName).Replace("{pageNumber}", pageNumber).Replace("{pageSize}", pageSize).Replace("{searchKey}", searchKey);
                                    abcd += formControl;
                                }
                                if (FK.Count == 0)
                                    text = text.Replace("{useEffectForFK}", "\n");
                                else
                                    text = text.Replace("{useEffectForFK}", abcd);
                                text = text.Replace("{action}", "Add");
                                string last_replacement = "";
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    string col = cols[i];
                                    if (PK.Contains(col))
                                        continue;
                                    if (FK.Contains(col))
                                        continue;
                                    string temp = @"<Form.Group>
<label className=""form -control-label"">{config[""{col}_new_name""] !== undefined ? config[""{col}_new_name""]:""{col}""}</label>
<Form.Control type=""" + utility.GetvalJSType(cols[i + 1]) + @""" name=""{col}"" className=""form-control"" value={values.{col}}
onChange={handleChange}
onBlur ={handleBlur}
isInvalid ={!!touched.{col} && !!errors.{col}}
isValid ={!!touched.{col} && !errors.{col}}
></Form.Control>
{
    errors.{col} && (
    <Form.Control.Feedback type=""invalid"">
        {errors.{col}}
    </Form.Control.Feedback>
)}
</Form.Group>";
                                    temp = temp.Replace("{col}", col);
                                    last_replacement += temp;
                                    last_replacement += "\n";
                                }
                                for (int i = 0; i < FK.Count; i += 4)
                                {
                                    string col1 = FK[i + 3];
                                    string col2 = FK[i];
                                    List<string> ans = function(connectionString, FK[i + 1]);
                                    string temp1 = @"<Form.Group>
                <label className=""form-control-label"">
                  {config.{col}_ref === undefined
                    ? ""{col}"" 
                    : config.{col}_ref}
                </label>
                {(config.{col}_control === ""dropdown"" || config.{col}_control === undefined) && (
                  <Form.Control
                    as=""select""
                    name=""{col}""
                    className=""form-control""
                    value={values.{col}}
                    onChange={handleChange}
                    onBlur={handleBlur}
                    isInvalid={!!touched.{col} && !!errors.{col}}
                    isValid={!!touched.{col} && !errors.{col}}
                  >
                    <option value={0}>Select {modelName} </option>
                    {{tableName}Data.list.map((item, i) => {
                      return (
                        <option value={item.{col1}} key={`{tableName}-${i}`}>
                          {config.{col}_ref === undefined
                            ? item[""{col1}""]
                            : item[config.{col}_ref]}
                        </option>
                      );
                    })}
                  </Form.Control>
                )}
                {config.{col}_control === ""radio"" &&
                  {tableName}Data.list.map((item, i) => {
                    return (
                      <Form.Check
                        type={config.{col}_control}
                        value={item.{col1}}
                        name=""{col}""
                        className=""form-control""
                        // value={values.{col}}
                        onChange={handleChange}
                        onBlur={handleBlur}
                        key={`{tableName}-${i}`}
                        label={
                          config.{col}_ref === undefined
                            ? item[""{col1}""]
                            : item[config.{col}_ref]
                        }
                        checked={
                          item.{col1} === values.{col} ? true : null
                        }
                      />
                    );
                  })}
                {errors.{col} && (
                  <Form.Control.Feedback type=""invalid"">
                    {errors.{col}}
                  </Form.Control.Feedback>
                )}
              </Form.Group>";
                                    //                                +@"<Form.Group>
                                    //<label className=""form-control-label"">{col}</label>
                                    //<Form.Control as=""select""  name=""{col}"" className=""form-control"" value={formik.values.{col}}
                                    //onChange={formik.handleChange}
                                    //onBlur ={formik.handleBlur}
                                    //isInvalid ={!!formik.touched.{col} && !!formik.errors.{col}}
                                    //isValid ={!!formik.touched.{col} && !formik.errors.{col}}
                                    //>
                                    //<option value={0}>Select {modelName} </option> 
                                    //{
                                    //{{tableName}Data.list.map((item, i) => {
                                    //return <option value={item.{col}} key={`{tableName}-${i}`}>{item.{col}}</option>
                                    //})}
                                    //</Form.Control>
                                    //{
                                    //    formik.errors.{col} && (
                                    //    <Form.Control.Feedback type=""invalid"">
                                    //        {formik.errors.{col}}
                                    //    </Form.Control.Feedback>
                                    //)}
                                    //</Form.Group>"
                                    temp1 = temp1.Replace("{col}", col1);
                                    temp1 = temp1.Replace("{col1}", col2);
                                    temp1 = temp1.Replace("{modelName}", char.ToUpper(FK[i + 1][0]) + FK[i + 1].Substring(1));
                                    temp1 = temp1.Replace("{tableName}", FK[i + 1]);
                                    last_replacement += temp1;
                                    last_replacement += "\n";
                                }
                                text = text.Replace("{formGroupWithValidation}", last_replacement);
                                string PrimaryKeyConversion = "", PrimaryKeyInitialization = "";
                                for (int i = 0; i < PK.Count; i++)
                                {
                                    PrimaryKeyInitialization += "values." + PK[i];
                                    if (i != PK.Count - 1)
                                        PrimaryKeyInitialization += ", ";
                                }
                                for (int i = 0; i < cols.Count; i += 3)
                                {
                                    string dataType = utility.GetJSType(cols[i + 1]);
                                    if (dataType != "String" && dataType != "Date")
                                    {
                                        if (FK.Contains(cols[i]))
                                        {
                                            string temp = "";
                                            for (int j = 0; j < FK.Count; j++)
                                            {
                                                if (FK[j].ToLower() == cols[i].ToLower())
                                                {
                                                    if (j % 4 == 0)
                                                        temp = FK[j];
                                                    else if (j % 4 == 3)
                                                        temp = FK[j - 3];
                                                }
                                            }
                                            PrimaryKeyConversion += $"values.{cols[i]} = config[{temp}_isHidden] ? {dataType}(config[{temp}_defaultValue]) : {dataType}(values.{temp});\n";
                                            //PrimaryKeyConversion += "values." + cols[i] + " = " + "config[" + temp +"_isHidden] ? "+ dataType + "(config[" + temp + "_defaultValue]) : " + dataType + "(values." + temp + ")\n";
                                            //PrimaryKeyConversion += "values." + cols[i] + " = " + dataType + "(values." + temp + ")\n";
                                        }
                                        else
                                            PrimaryKeyConversion += $"values.{cols[i]} = config[{cols[i]}_isHidden] ? {dataType}(config[{cols[i]}_defaultValue]) : {dataType}(values.{cols[i]});\n";
                                        //PrimaryKeyConversion += "values." + cols[i] + " = " + "config[" + cols[i] + "_isHidden] ? " + dataType + "(config[" + cols[i] + "_defaultValue]) : " + dataType + "(values." + cols[i] + ")\n";
                                        //PrimaryKeyConversion += "values." + cols[i] + " = " + dataType + "(values." + cols[i] + ")\n";
                                    }
                                    else
                                    {
                                        //values.modifiedAt = config["modifiedAt_isHidden"] ? config["modifiedAt_defaultValue"] : values.modifiedAt;
                                        PrimaryKeyConversion += $"values.{cols[i]} = config[{cols[i]}_isHidden] ? config[{cols[i]}_defaultValue] : values.{cols[i]};\n";
                                    }
                                }
                                text = text.Replace("{PrimaryKeyConversion}", PrimaryKeyConversion);
                                text = text.Replace("{PrimaryKeyInitialization}", PrimaryKeyInitialization);
                                File.WriteAllText(@des + "/src/components/" + tbl + "/" + "form.tsx", text);
                                bool exists = System.IO.Directory.Exists(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));

                                if (!exists)
                                    System.IO.Directory.CreateDirectory(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1));
                                File.WriteAllText(@des + "/src/Dnd/Draggable Components/Previous_Components/CustomComponents/" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "/" + "form.tsx", text);

                            }
                        }

                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error in Processfile in frontend section while creating files: " + e.ToString());
                    Program.errors_list.Add("Error in Processfile in frontend section while creating files: " + e.Message);
                }
            }
        }
    }
}