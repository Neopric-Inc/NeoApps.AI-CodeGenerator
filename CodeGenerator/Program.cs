using Amazon.Runtime.Internal.Transform;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Docker.DotNet;
using Docker.DotNet.Models;
using HelloWorld;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NoCodeAppGenerator.GeneratorManagers;
using NoCodeAppGenerator.RabbitmqProducer;
using Org.BouncyCastle.Crypto;
using RabbitMQ.Client;
using ReactJsProjectDemo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;
using VSLangProj;
using NoCodeAppGenerator.ParameterModel;
using MySqlX.XDevAPI.Relational;
using static System.Net.Mime.MediaTypeNames;
using System.Globalization;


namespace NoCodeAppGenerator
{
    public class Getters
    {
        private readonly IConfiguration _configuration;
        private static string accessKey;
        private static string secretKey;
        private static string bucketName;
        private static string endpoint;
        public Getters(IConfiguration configuration)
        {
            _configuration = configuration;

            accessKey = _configuration["DigitalOceanSpacesConfig:AccessKey"];
            secretKey = _configuration["DigitalOceanSpacesConfig:SecretKey"];
            bucketName = _configuration["DigitalOceanSpacesConfig:BucketName"];
            endpoint = _configuration["DigitalOceanSpacesConfig:Endpoint"];
        }
        //private static readonly string accessKey = "DO0089PZREULYLFACQZJ";
        //private static readonly string secretKey = "8kJiHv7lv0mW5Py+9XnIMwzD3VRxcJdevO7F8tEu4Eo";
        //private static readonly string bucketName = "nocodes3dev";
        //private static readonly string endpoint = "https://nyc3.digitaloceanspaces.com";

        //shifting
        public List<string> uploadToS3(string userId, string projectId, string stackId, string codeId, string path)
        {
            List<string> links = new List<string>();
            AmazonS3Config config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true
            };
            IAmazonS3 client = new AmazonS3Client(accessKey, secretKey, config);
            //N new Dct
            //var x = Directory.GetFiles(path);
            //Console.WriteLine(@Directory.GetCurrentDirectory() + @"\..\..\..\..\app\NoCodeAppGenerator");
            //for (int i = 0; i < x.Length; i++)
            //{
            //string fileName = x[i].Split("/").Last();
            string fileName = path.Split("/").Last();
            string key = $@"{userId}/{projectId}/{stackId}/{codeId}/{fileName}";
            TransferUtility transferUtility = new TransferUtility(client);
            Console.WriteLine($"Uploading {fileName} ...");
            try
            {
                //transferUtility.Upload(x[i], bucketName, key);
                transferUtility.Upload(path, bucketName, key);

                Console.WriteLine($"Uploaded {fileName} Successfully!");
                GetPreSignedUrlRequest request = new GetPreSignedUrlRequest
                {
                    BucketName = bucketName,
                    Key = key,
                    Expires = DateTime.Now.AddMinutes(15),
                    Verb = HttpVerb.GET
                };
                Console.WriteLine($"Generating {fileName} Download Link....");
                try
                {
                    string presignedUrl = client.GetPreSignedURL(request);
                    Console.WriteLine($"Generated {fileName} Download Link!It is Valid for 20 minutes.");
                    Console.WriteLine("Link:" + presignedUrl);
                    links.Add(presignedUrl);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error!! Occured while generating Download Link:" + e.Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error!! Occured While uploading:" + e.Message);
            }


            return links;
        }
    }
    public class obj
    {
        public int Id { get; set; }
        public string Content { get; set; }

        public obj() { }
        public obj(int Id, string Content)
        {
            this.Id = Id;
            this.Content = Content;
        }
    }

    public class APIFlowJson
    {
        public List<TransactionalParameterModel> transactionalAPI { get; set; }

        public List<ApiFlow> aggregationAPI { get; set; }
    }

    class Program : Renaming
    {
        public static string databaseName;
        public static bool isRbac = true;

        public static Dictionary<string, TransactionalModel> Transactional = new Dictionary<string, TransactionalModel>();


        /*public static List<TransactionalParameterModel> transactionalModel = new List<TransactionalParameterModel>
        {
             new TransactionalParameterModel
             {
                ParentEntity = "categorytypes", // Specify "Invoice" as the parent entity
                transactional = new TransactionalModel
                {
                    Sequence = new List<string> { "categories" }, // Exclude "Invoice" from the sequence
                    Relations = new Dictionary<string, string>
                    {
                        { "categories", "one" }   
                    }
                }
             }
        };*/

        /* public static List<TransactionalParameterModel> transactionalModel = new List<TransactionalParameterModel>
         {
              new TransactionalParameterModel
              {
                 ParentEntity = "social_media_users", // Specify "Invoice" as the parent entity
                 transactional = new TransactionalModel
                 {
                     Sequence = new List<string> { "socialmediaaccounts" }, // Exclude "Invoice" from the sequence
                     Relations = new Dictionary<string, string>
                     {
                         { "socialmediaaccounts", "many" }
                     }
                 }
              }
         };*/

        public static List<TransactionalParameterModel> transactionalModel = new List<TransactionalParameterModel>();

        /*public static List<ApiFlow> aggregateModel = new List<ApiFlow>
        {
             new ApiFlow
        {
            Endpoint = "/reports/total-income",
            Method = "GET",
            Description = "Get the total income for a specific account",
            Parameters = new List<ApiParameter>
            {
                new ApiParameter
                {
                    Name = "accountId",
                    Type = "int",
                    Required = true,
                    Description = "ID of the account"
                }
            },
            DTO = new DTO
            {
                Name = "TotalIncomeDTO",
                Properties = new List<Property>
        {
            new Property
            {
                Name = "totalIncome",
                Type = "decimal",
                Required = true,
                Description = "Total income for the account"
            }
        }
            },
            Query = new DataBaseQuery
            {
                Type = "select",
                Tables = new List<string> { "Transactions" },
                Columns = new List<string> { "SUM(amount) as totalIncome" },
                Joins = new List<Join>
        {
            new Join
            {
                Type = "inner",
                Table = "Accounts",
                On = "Transactions.account_id = Accounts.account_id"
            }
        },
                Conditions = new List<Condition>
        {
            new Condition
            {
                Table = "Transactions",
                Column = "account_id",
                Operator = "=",
                Parameter = "accountId"
            }
        },
                GroupBy = new List<string>(),
                OrderBy = new List<string>(),
                Limit = ""
            }
        }
    };*/
        public static List<ApiFlow> aggregateModel = new List<ApiFlow>();

        /*public static List<ApiFlow> aggregateModel = new List<ApiFlow>
{
    new ApiFlow
        {
            Endpoint = "/api/aggregation/content-engagement",
            Method = "GET",
            Description = "Get engagement statistics for a specific piece of content",
            Parameters = new List<ApiParameter>
        {
            new ApiParameter
            {
                Name = "content_id",
                Type = "int",
                Required = true,
                Description = "ID of the content"
            }
        },
            DTO = new DTO
            {
                Name = "ContentEngagementDTO",
                Properties = new List<Property>
            {
                new Property
                {
                    Name = "content_id",
                    Type = "int",
                    Required = true,
                    Description = "ID of the content"
                },
                new Property
                {
                    Name = "total_likes",
                    Type = "int",
                    Required = true,
                    Description = "Total number of likes for the content"
                },
                new Property
                {
                    Name = "total_views",
                    Type = "int",
                    Required = true,
                    Description = "Total number of views for the content"
                },
                new Property
                {
                    Name = "total_comments",
                    Type = "int",
                    Required = true,
                    Description = "Total number of comments for the content"
                }
                // Add more properties for other aspects as needed (e.g., shares, clicks)
            }
            },
            Query = new DataBaseQuery
            {
                Type = "SELECT",
                Tables = new List<string> { "Content", "Engagement" },
                Columns = new List<string>
            {
                "Content.content_id",
                "SUM(CASE WHEN Engagement.engagement_type_id = 1 THEN 1 ELSE 0 END) AS total_likes", // Assuming engagement_type_id 1 corresponds to likes
                "SUM(CASE WHEN Engagement.engagement_type_id = 2 THEN 1 ELSE 0 END) AS total_views", // Assuming engagement_type_id 2 corresponds to views
                "SUM(CASE WHEN Engagement.engagement_type_id = 3 THEN 1 ELSE 0 END) AS total_comments" // Assuming engagement_type_id 3 corresponds to comments
                // Add more SUM statements for other aspects as needed (e.g., shares, clicks)
            },
                Joins = new List<Join>
            {
                new Join
                {
                    Type = "INNER",
                    Table = "Engagement",
                    On = "Content.content_id = Engagement.content_id"
                }
            },
                Conditions = new List<Condition>
            {
                new Condition
                {
                    Table = "Content",
                    Column = "content_id",
                    Operator = "=",
                    Parameter = "content_id"
                }
            },
                GroupBy = new List<string> { "Content.content_id" },
                OrderBy = new List<string>(), // Initialize as empty list
                Limit = "" // Initialize as an empty string
            }
    }
};*/



        // Now you have an instance of ApiFlow representing the aggregation-based API flow.


        public static void makedictionary()
        {
            foreach (TransactionalParameterModel tp in transactionalModel)
            {
                Transactional.Add(tp.ParentEntity.ToLower(), tp.transactional);
            }
        }


        public static string redisConn;
        public static List<string> errors_list = new List<string>();
        static private Dictionary<string, string> InterfacesAndImpl = new Dictionary<string, string>();
        public static List<string> GetTables(string connection_string)
        {
            List<string> table = new List<string>();
            //using (MySqlConnection conn = new MySqlConnection(connection_string))
            //{
            //    conn.Open();
            try
            {
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                DataTable tables = connection.GetSchema("Tables");
                foreach (DataRow row in tables.Rows)
                {
                    table.Add(row["TABLE_NAME"].ToString());
                }
                //}
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error: {ex.Message}");
            }
            return table;
        }
        List<obj> createModels(string projectName, string apiVersion, string databaseName, string filePath, string server, string uid, string password, string port)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(filePath, "C:/Users/govar/Downloads/SQLScipt.sql");
            }
            filePath = "C:/Users/govar/Downloads/SQLScipt.sql";
            string connectionString = "server=" + server + ";uid=" + uid + ";port=" + port;
            createDBfromSQL myDB = new createDBfromSQL(databaseName, filePath, connectionString);
            try
            {
                myDB.createDBbyPassingSQSL();
                string conn1 = connectionString;
                conn1 += ";database=" + databaseName + ";";
                string createTableSql = "CREATE TABLE messageQueue (id INT AUTO_INCREMENT PRIMARY KEY, queueName VARCHAR(255) Unique,PrimaryKey VARCHAR(255))";
                //using (MySqlConnection connection = new MySqlConnection(conn1))
                //{
                //    connection.Open();
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand command = new MySqlCommand(createTableSql, connection))
                {
                    command.ExecuteNonQuery();
                }
                //connection.Close();
                //}
                createModelfromDB dbc = new createModelfromDB(conn1, projectName);
                dbc.createModelbyPassingDB();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error!!" + e.Message.ToString());
                obj ob = new obj();
                List<obj> anss = new List<obj>();
                anss.Add(ob);
                return anss;
            }

            //using (WebClient client = new WebClient())
            //{
            //    client.DownloadFile(filePath, "SQLScipt");
            //}
            List<obj> ans = new List<obj>();
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory() + "/DotNet_Output/solution/" + projectName + "/" + projectName + ".Model", "*Model" + ".cs", SearchOption.AllDirectories);
            int Id = 1;
            foreach (string file in files)
            {
                string text = File.ReadAllText(file);
                obj ob = new obj(Id, text);
                Id++;
                ans.Add(ob);
            }
            return ans;
        }
        public static bool CheckDatabaseExists(string connectionString, string databaseName)
        {
            //using (var connection = new MySqlConnection(connectionString))
            //{
            try
            {
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (var command = new MySqlCommand($"SHOW DATABASES WHERE `database` = '{databaseName}'", connection))
                {
                    //connection.Open();
                    //if (connection.State == System.Data.ConnectionState.Open)
                    //{
                    //    Console.WriteLine("Connection is opened in CheckDatabaseExists()");
                    //}
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["Database"].ToString() == databaseName)
                            {
                                //connection.Close();
                                //if (connection.State == System.Data.ConnectionState.Closed)
                                //{
                                //    Console.WriteLine("Connection is closed in CheckDatabaseExists()");
                                //}
                                return true;
                            }
                        }
                    }
                }
                //    connection.Close();
                //    if (connection.State == System.Data.ConnectionState.Closed)
                //    {
                //        Console.WriteLine("Connection is closed in CheckDatabaseExists()");
                //    }
                //}
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error: {ex.Message}");
            }
            return false;
        }
        public static List<string> getPrimaryKey(string tableName, string connectionString)
        {
            string query = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @tableName AND TABLE_SCHEMA=" + "\"" + databaseName + "\"" + " AND COLUMN_KEY = 'PRI' ORDER BY COLUMN_NAME ASC";
            List<string> ans = new List<string>();
            //using (MySqlConnection connection = new MySqlConnection(connectionString))
            //{
            //    connection.Open();
            //    if (connection.State == System.Data.ConnectionState.Open)
            //    {
            //        Console.WriteLine("Connection is opened in getPrimaryKey()");
            //    }
            try
            {
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            string columnName = reader.GetString("COLUMN_NAME");
                            string dataType = reader.GetString("DATA_TYPE");
                            ans.Add(columnName);
                            ans.Add(dataType);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in GetPrimaryKey for table '{tableName}': {ex.Message}");
            }
            //    connection.Close();
            //    if (connection.State == System.Data.ConnectionState.Closed)
            //    {
            //        Console.WriteLine("Connection is closed in getPrimaryKey()");
            //    }
            //}
            return ans;
        }

        public static HashSet<string> getForeignKey(string tableName, string connectionString)
        {
            string query = $@"SELECT
    COLUMN_NAME
FROM
    INFORMATION_SCHEMA.KEY_COLUMN_USAGE
WHERE
    TABLE_NAME = '{tableName}'
    AND TABLE_SCHEMA = '{databaseName}'
    AND REFERENCED_TABLE_NAME IS NOT NULL ORDER BY COLUMN_NAME ASC";
            HashSet<string> ans = new HashSet<string>();
            //using (MySqlConnection connection = new MySqlConnection(connectionString))
            //{
            //    connection.Open();
            //    if (connection.State == System.Data.ConnectionState.Open)
            //    {
            //        Console.WriteLine("Connection is opened in getPrimaryKey()");
            //    }
            try
            {
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            string columnName = reader.GetString("COLUMN_NAME");
                            ans.Add(columnName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in GetForeignKey for table '{tableName}': {ex.Message}");
            }
            //    connection.Close();
            //    if (connection.State == System.Data.ConnectionState.Closed)
            //    {
            //        Console.WriteLine("Connection is closed in getPrimaryKey()");
            //    }
            //}
            return ans;
        }

        static List<string> getAllFields(string connectionString, string tableName)
        {
            List<string> ans = new List<string>();
            //using (MySqlConnection conn = new MySqlConnection(connectionString))
            //{
            //    conn.Open();
            try
            {
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                string query = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS " +
                                   "WHERE TABLE_NAME = @tableName " + "AND TABLE_SCHEMA=" + "\"" + databaseName + "\"" +
                                   "AND DATA_TYPE IN ('varchar', 'char', 'text')";

                using (MySqlCommand cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@tableName", tableName);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Get the name of the column
                            string columnName = reader.GetString("COLUMN_NAME");
                            ans.Add(columnName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in GetAllFields for table '{tableName}': {ex.Message}");
            }
            //}
            return ans;
        }
        public static List<string> getAllColumns(string tableName, string connectionString)
        {
            string query = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @myDB AND TABLE_NAME = @tableName";
            List<string> ans = new List<string>();

            //using (MySqlConnection connection = new MySqlConnection(connectionString))
            //{
            //    connection.Open();
            //    if (connection.State == System.Data.ConnectionState.Open)
            //    {
            //        Console.WriteLine("Connection is opened in getAllColumns()");
            //    }
            try
            {
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    command.Parameters.AddWithValue("@myDB", databaseName);
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            string columnName = reader.GetString("COLUMN_NAME");
                            string dataType = reader.GetString("DATA_TYPE");
                            ans.Add(columnName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in GetAllColumns for table '{tableName}': {ex.Message}");
            }
            //connection.Close();
            //if (connection.State == System.Data.ConnectionState.Closed)
            //{
            //    Console.WriteLine("Connection is closed in getAllColumns()");
            //}
            //}
            return ans;
        }
        static void mapFill(ref Dictionary<string, string> map, string connectionString, string projectName, string rabbitMQConn)
        {
            try
            {
                map.Add("{projectName}", projectName);
                map.Add("{connectionString}", connectionString);
                map.Add("{RabbitMQConnectionString}", rabbitMQConn);
            }
            catch (Exception ex)
            {
                // Handle the error in a way that makes sense for your application
                errors_list.Add($"Error in MapFill: {ex.Message}");
            }
        }
        static void ProcessController(ref Dictionary<string, string> map, string connection_string, string tbl, string rabbitMQConn)
        {
            try
            {
                mapFill(ref map, connection_string, projectName, rabbitMQConn);
                map.Add("{tableName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                List<string> ans = getPrimaryKey(tbl, connection_string);
                string url = "{", argu = "", param = "", validation = "";
                for (int i = 0; i < ans.Count; i += 2)
                {
                    string dataType = utility.getType(ans[i + 1]);
                    if (dataType.Contains("Int"))
                        dataType = "long";
                    url += ans[i];
                    if (i != ans.Count - 1 && i != ans.Count - 2)
                        url += "}/{";
                    argu += dataType + " " + ans[i];
                    if (i != ans.Count - 1 && i != ans.Count - 2)
                        argu += ",";
                    param += ans[i];
                    if (i != ans.Count - 1 && i != ans.Count - 2)
                        param += ",";
                    if (dataType != "string")
                    {
                        validation += "if (" + ans[i] + "<= 0) { ValidationResult.AddEmptyFieldError(" + "\"" + ans[i] + "\"" + "); }";
                        if (i != ans.Count - 1 && i != ans.Count - 2)
                            validation += "\n";
                    }
                }
                url += "}";
                map.Add("{primaryKeyListURL}", url);
                map.Add("{primaryKeyList}", argu);
                map.Add("{primaryKeyListValidation}", validation);
                map.Add("{primaryKeyListParam}", param);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessController: {ex.Message}");
            }
        }
        static void ProcessInterfaceDataAccess(ref Dictionary<string, string> map, string connection_string, string tbl, string rabbitMQConn)
        {
            try
            {
                mapFill(ref map, connection_string, projectName, rabbitMQConn);
                map.Add("{tableName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                List<string> ans = getPrimaryKey(tbl, connection_string);
                string primaryKeyListURL = "", primaryKeyList = "";
                for (int i = 0; i < ans.Count; i += 2)
                {
                    primaryKeyListURL += "{" + ans[i] + "}";
                    if (i != ans.Count - 1 && i != ans.Count - 2)
                        primaryKeyListURL += "/";
                    string dataType = utility.getType(ans[i + 1]);
                    if (dataType.Contains("Int"))
                        dataType = "long";
                    primaryKeyList += dataType + " " + ans[i];
                    if (i != ans.Count - 1 && i != ans.Count - 2)
                        primaryKeyList += ",";
                }
                map.Add("{primaryKeyListParam}", primaryKeyList);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessInterfaceDataAccess: {ex.Message}");
            }
        }
        static void ProcessStringQuery(ref string query, ref string qry, List<string> tblInfo)
        {
            try
            {
                foreach (string field in tblInfo)
                {
                    query += "t.";
                    qry += "t.";
                    query += field;
                    qry += field;
                    query += " LIKE CONCAT('%',@SearchKey,'%')";
                    qry += " LIKE CONCAT('%',@SearchKey,'%')";
                    if (field != tblInfo[^1])
                        query += " OR ";
                    if (field != tblInfo[^1])
                        qry += " OR ";
                }
            }
            catch (Exception ex)
            {
                // Handle the error in a way that makes sense for your application
                errors_list.Add($"Error in ProcessStringQuery: {ex.Message}");
            }
        }
        static void ProcessPrimaryKeyListParams(ref string primaryParams, List<string> PK)
        {
            try
            {
                for (int i = 0; i < PK.Count; i += 2)
                {
                    string dataType = utility.getType(PK[i + 1]);
                    if (dataType.Contains("Int"))
                        dataType = "long";
                    if (i != PK.Count - 1 && i != PK.Count - 2)
                        primaryParams += dataType + " " + PK[i] + ",";
                    else
                        primaryParams += dataType + " " + PK[i];
                }
            }
            catch (Exception ex)
            {
                // Handle the error in a way that makes sense for your application
                errors_list.Add($"Error in ProcessPrimaryKeyListParams: {ex.Message}");
            }
        }
        static void ProcessSingleQuery(ref string singleQuery, ref string singleParams, List<string> PK,string referencingColumnName)
        {
            try
            {
                for (int i = 0; i < PK.Count; i += 2)
                {
                    if (i == PK.Count - 1 || i == PK.Count - 2)
                    {
                        singleQuery += "t." + PK[i];
                        singleQuery += "= @";
                        singleQuery += PK[i];
                    }
                    else
                    {
                        singleQuery += "t." + PK[i];
                        singleQuery += "= @";
                        singleQuery += PK[i];
                        singleQuery += " AND ";
                    }
                    if (referencingColumnName != "")
                    {
                        singleParams += "(" + "\"@" + PK[i] + "\"," + referencingColumnName + ");";
                    }
                    else
                    {
                        singleParams += "(" + "\"@" + PK[i] + "\"," + PK[i] + ");";
                    }
                 
                    if (i != PK.Count - 1 && i != PK.Count - 2)
                        singleParams += "\ncmd.Parameters.AddWithValue";
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessSingleQuery: {ex.Message}");
            }

        }
        static void ProcessUpdateQuery(ref string update_query, ref string params_query, ref string insert_q, List<string> columns, List<string> PK)
        {
            try
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i] != "createdBy" && columns[i] != "createdAt")
                    {
                        update_query += columns[i];
                        update_query += "=";
                        update_query += "@" + columns[i];
                        if (i != columns.Count - 1)
                            update_query += ",";
                    }
                    
                }
                update_query += " WHERE ";
                for (int i = 0; i < PK.Count; i += 2)
                {
                    update_query += PK[i];
                    update_query += "=@";
                    update_query += PK[i];
                    if (i != PK.Count - 1 && i != PK.Count - 2)
                        update_query += " AND ";
                }
                for (int i = 0; i < columns.Count; i++)
                {
                    if (columns[i] != "createdBy" && columns[i] != "createdAt")
                    {
                        params_query += "cmd.Parameters.AddWithValue(\"@";
                        params_query += columns[i];
                        params_query += "\"";
                        params_query += ", model.";
                        params_query += columns[i];
                        params_query += ");";
                        if (i != columns.Count - 1)
                            params_query += "\n";
                    } 
                }
                //Kaival - modifiedAt =DateTime.UtcNow
                params_query = params_query.Replace("model.modifiedAt", "DateTime.UtcNow");
                params_query = params_query.Replace("model.isActive", "1");
                // Replace ModifiedAt with Utc Datetime.Now.
                for (int i = 0; i < columns.Count; i++)
                {
                    insert_q += "cmd.Parameters.AddWithValue(" + "\"" + "@";
                    insert_q += columns[i] + "\"";
                    insert_q += ", model.";
                    insert_q += columns[i];
                    insert_q += ");";
                    if (i != columns.Count - 1)
                        insert_q += "\n";
                }
                //Kaival - modifiedAt =DateTime.UtcNow
                insert_q = insert_q.Replace("model.createdAt", "DateTime.UtcNow");
                insert_q = insert_q.Replace("model.modifiedAt", "DateTime.UtcNow");
                insert_q = insert_q.Replace("model.isActive", "1");
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessUpdateQuery: {ex.Message}");
            }
        }
        static void ProcessInsertQuery(ref string insert_query, List<string> columns, string tbl)
        {
            try
            {
                List<string> li = columns;
                List<string> li1 = new List<string>();
                foreach (string i in li)
                {
                    string i1 = i.Insert(0, "@");
                    li1.Add(i1);
                }
                insert_query = "INSERT INTO " + tbl + " (" + string.Join(",", li) + ") Values (" + string.Join(",", li1) + ");";
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessInsertQuery: {ex.Message}");
            }
        }
        static void ProcessSelectAllModelInit(ref string selectAllModelInit, string tbl, string connection_string,string referencingColumnName)
        {
            try
            {
                HashSet<string> set = new HashSet<string>();
                getColsMetadata(ref selectAllModelInit, tbl, connection_string, ref set, referencingColumnName);
                selectAllModelInit = selectAllModelInit.Remove(selectAllModelInit.Length - 1);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessSelectAllModelInit: {ex.Message}");
            }

        }

        static void ProcessSelectAllModelInitReporting(ref string selectAllModelInit, string tbl, string connection_string, string reftable,string referencingColumnName)
        {
            try
            {
                HashSet<string> set = new HashSet<string>();
                ApiGeneratorFunctions.getColsMetadataReporting(ref selectAllModelInit, tbl, connection_string, ref set, reftable,referencingColumnName);
                selectAllModelInit = selectAllModelInit.Remove(selectAllModelInit.Length - 1);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessSelectAllModelInit: {ex.Message}");
            }

        }

        static void ProcessSelectgetAllModelInitReporting(ref string selectAllModelInit, string tbl, string connection_string, string reftable,string referencingColumn)
        {
            try
            {
                HashSet<string> set = new HashSet<string>();
                ApiGeneratorFunctions.getAllColsMetadataReporting(ref selectAllModelInit, tbl, connection_string, ref set, reftable, referencingColumn);
                selectAllModelInit = selectAllModelInit.Remove(selectAllModelInit.Length - 1);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessSelectAllModelInit: {ex.Message}");
            }

        }
        static void ProcessDeleteQuery(ref string delete_query, string tbl, List<string> PK)
        {
            try
            {
                delete_query = "UPDATE " + tbl + " SET isActive=0 Where ";
                for (int i = 0; i < PK.Count; i += 2)
                {
                    delete_query += PK[i];
                    delete_query += "=@";
                    delete_query += PK[i];
                    if (i != PK.Count - 1 && i != PK.Count - 2)
                        delete_query += " AND ";
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessDeleteQuery: {ex.Message}");
            }
        }
        static void ProcessDataAccess(ref Dictionary<string, string> map, string connection_string, string tbl, string rabbitMQConn,string referencingColumnName)
        {
            try
            {
                List<string> PK = getPrimaryKey(tbl, connection_string);
                mapFill(ref map, connection_string, projectName, rabbitMQConn);
                map.Add("{tableName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                map.Add("{selectAllRecordCountQuery}", "SELECT count(*) TotalCount FROM " + tbl + " t WHERE t.isActive=1");
                map.Add("{selectAllRecordCountQueryByCreatedBy}", "SELECT count(*) TotalCount FROM " + tbl + " t WHERE t.isActive=1 AND t.createdBy=@ownername");
                List<string> tblInfo = getAllFields(connection_string, tbl);
                string query = "SELECT count(*) TotalCount FROM " + tbl + " t WHERE t.isActive=1 AND ", querybycreatedby = "SELECT count(*) TotalCount FROM " + tbl + " t WHERE t.isActive=1 AND t.createdBy=@ownername AND ", qrybycreatedBy = "SELECT t.* FROM " + tbl + " t WHERE t.isActive=1 AND t.createdBy=@ownername AND ", qry = "SELECT t.* FROM " + tbl + " t WHERE t.isActive=1 AND ";
                ProcessStringQuery(ref query, ref qry, tblInfo);
                ProcessStringQuery(ref querybycreatedby, ref qrybycreatedBy, tblInfo);
                map.Add("{searchRecordCountQuery}", query);
                map.Add("{searchRecordCountQueryByCreatedBy}", querybycreatedby);
                map.Add("{selectAllQuery}", "SELECT  t.* FROM " + tbl + " t  WHERE t.isActive=1 ORDER BY column LIMIT @Offset, @ItemsPerPage");
                map.Add("{selectAllQueryByCreatedBy}", "SELECT  t.* FROM " + tbl + " t  WHERE t.isActive=1 AND t.createdBy=@ownername ORDER BY column LIMIT @Offset, @ItemsPerPage");
                map.Add("{searchQuery}", qry + " ORDER BY column LIMIT @Offset, @ItemsPerPage");
                map.Add("{searchQueryByCreatedBy}", qrybycreatedBy + " ORDER BY column LIMIT @Offset, @ItemsPerPage");
                map.Add("{convertSQL}", "cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);");
                string singleQueryByCreatedBy = "SELECT  t.* FROM " + tbl + " t  WHERE t.isActive=1 AND t.createdBy=@ownername AND ";
                string singleQuery = "SELECT  t.* FROM " + tbl + " t  WHERE t.isActive=1 AND ";
                string singleParams = "cmd.Parameters.AddWithValue";
                string singleParamsbycreatedby = singleParams;
                ProcessSingleQuery(ref singleQuery, ref singleParams, PK, "");
                ProcessSingleQuery(ref singleQueryByCreatedBy, ref singleParamsbycreatedby, PK, "");
                map.Add("{selectOneQuery}", singleQuery);
                map.Add("{selectOneQueryByCreatedBy}", singleQueryByCreatedBy);
                map.Add("{getByIDQueryParameter}", singleParams);
                List<string> columns = getAllColumns(tbl, connection_string);
                string update_query = "UPDATE " + tbl + " SET ";
                string params_query = "", insert_q = "";
                ProcessUpdateQuery(ref update_query, ref params_query, ref insert_q, columns, PK);
                map.Add("{updateQuery}", update_query);
                map.Add("{updateQueryParameters}", params_query);
                map.Add("{insertQueryParameter}", insert_q);
                map.Add("{deleteQueryParameter}", singleParams);
                string primaryParams = "";
                ProcessPrimaryKeyListParams(ref primaryParams, PK);
                map.Add("{primaryKeyListParam}", primaryParams);
                string insert_query = "";
                ProcessInsertQuery(ref insert_query, columns, tbl);
                map.Add("{insertQuery}", insert_query);
                string delete_query = "";
                ProcessDeleteQuery(ref delete_query, tbl, PK);
                map.Add("{deleteQuery}", delete_query);
                string selectAllModelInit = "";
                ProcessSelectAllModelInit(ref selectAllModelInit, tbl, connection_string, "");
                map.Add("{selectAllModelInit}", selectAllModelInit);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessDataAccess: {ex.Message}");
            }
        }
        static void ProcessInterfaceManager(ref Dictionary<string, string> map, string connection_string, string tbl, string rabbitMQConn)
        {
            try
            {
                mapFill(ref map, connection_string, projectName, rabbitMQConn);
                map.Add("{tableName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                List<string> ans = getPrimaryKey(tbl, connection_string);
                string primaryKeyList = "";
                ProcessPrimaryKeyListParams(ref primaryKeyList, ans);
                map.Add("{primaryKeyListParam}", primaryKeyList);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessInterfaceManager: {ex.Message}");
            }
        }
        static List<string> PrimaryKeyTerms(ref string primaryKeyList, ref string primaryKeyListParam, ref string updatePrimaryKeyList, List<string> PK)
        {
            List<string> ans = new List<string>();
            try
            {
                for (int i = 0; i < PK.Count; i += 2)
                {
                    primaryKeyList += PK[i];
                    if (i != PK.Count - 1 && i != PK.Count - 2)
                        primaryKeyList += ",";
                    string dataType = utility.getType(PK[i + 1]);
                    if (dataType.Contains("Int"))
                        dataType = "long";
                    primaryKeyListParam += dataType + " " + PK[i];
                    if (i != PK.Count - 1 && i != PK.Count - 2)
                        primaryKeyListParam += ",";
                }

                updatePrimaryKeyList += "model." + PK[PK.Count - 2] + "=" + PK[PK.Count - 2] + ";";
                ans.Add(primaryKeyList);
                ans.Add(primaryKeyListParam);
                ans.Add(updatePrimaryKeyList);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in PrimaryKeyTerms: {ex.Message}");
            }
            return ans;
        }
        static void ProcessManager(ref Dictionary<string, string> map, string connection_string, string tbl, string rabbitMQConn)
        {
            try
            {
                mapFill(ref map, connection_string, projectName, rabbitMQConn);
                map.Add("{tableName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                List<string> PK = getPrimaryKey(tbl, connection_string);
                string a = "", b = "", c = "";
                List<string> ans = PrimaryKeyTerms(ref a, ref b, ref c, PK);
                map.Add("{primaryKeyList}", a);
                map.Add("{primaryKeyListParam}", b);
                map.Add("{updatePrimaryKeyList}", c);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessManager: {ex.Message}");
            }
        }
        static void ProcessModel(ref Dictionary<string, string> map, string connection_string, string tbl, string rabbitMQConn)
        {
            try
            {
                List<string> PK = getPrimaryKey(tbl, connection_string);
                mapFill(ref map, connection_string, projectName, rabbitMQConn);
                map.Add("{tableName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                string ans = "";
                Dictionary<string, string> dict = getAllInfo(ref ans, tbl, connection_string, PK[0]);
                map.Add("{modelProperties}", ans);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessModel: {ex.Message}");
            }
        }

        static string ProcessReportingModel(ref Dictionary<string, string> map, string connection_string, string tbl, string rabbitMQConn, HashSet<string> commoncolumn,string referencingColumnName)
        {
            try
            {
                List<string> PK = getPrimaryKey(tbl, connection_string);
                mapFill(ref map, connection_string, projectName, rabbitMQConn);
                map.Add("{tableName}", char.ToUpper(tbl[0]) + tbl.Substring(1));
                string ans = "";
                Dictionary<string, string> dict = getAllInfoReporting(ref ans, tbl, connection_string, commoncolumn, referencingColumnName);
                map.Add("{modelProperties}", ans);
                return ans;
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessReportingModel: {ex.Message}");
                return "";
            }
        }
        static string GenerateUsername()
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_";

            int length = random.Next(6, 9); // Random length between 6 and 8
            StringBuilder usernameBuilder = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                char randomChar = chars[random.Next(chars.Length)];
                usernameBuilder.Append(randomChar);
            }

            return usernameBuilder.ToString();
        }

        static string GeneratePassword()
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            const string specialChars = "!@#$%_+?";

            int length = 8;
            StringBuilder passwordBuilder = new StringBuilder();

            // Add a capital letter
            passwordBuilder.Append(chars[random.Next(26)]);

            // Add a small letter
            passwordBuilder.Append(chars[random.Next(26, 52)]);

            // Add a number
            passwordBuilder.Append(chars[random.Next(52, 62)]);

            // Add a random special character
            passwordBuilder.Append(specialChars[random.Next(specialChars.Length)]);

            // Fill the rest of the password with random characters
            for (int i = 4; i < length; i++)
            {
                char randomChar = chars[random.Next(chars.Length)];
                passwordBuilder.Append(randomChar);
            }

            // Shuffle the characters in the password for randomness
            char[] passwordArray = passwordBuilder.ToString().ToCharArray();
            for (int i = passwordArray.Length - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                char temp = passwordArray[i];
                passwordArray[i] = passwordArray[j];
                passwordArray[j] = temp;
            }

            return new string(passwordArray);
        }
        public static void ProcessFiles(string adminUsername, string adminPassword, string path, string server, string uid, string username, string password, string databaseName, string script, string statusOfGeneration, string projectName, string DBexists, string port, string rabbitMQConn, string noderedurl, string swgurl,string project_id)
        {
            try
            {
                string[] drpath = Directory.GetDirectories(@"../", (uid + "_*"));
                string fname = drpath[0].Split("/").Last();
                var list_of_files = Directory.GetFiles(@path + "/DotNetMySQLTemplate/");
                Array.Sort(list_of_files);
                path += "/";
                string connection_string = "";
                connection_string += "server=" + server + ";uid=" + uid + ";" + "username=" + username + ";" + "password=" + password + ";" + "port=" + port + ";TreatTinyAsBoolean=false;";
                Console.WriteLine(CheckDatabaseExists(connection_string, databaseName));
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                if (DBexists == "YES")
                {
                    connection_string += "database=" + databaseName + ";";
                }
                else
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile(script, "./SQLScript.sql");
                    }
                    
                    //Kaival - Executing users table first. 

                    if (isRbac)
                    {
                        Console.WriteLine("Simple_Rbac script running for tables");
                        string createTableSql2 = @$"
CREATE TABLE Roles (
  role_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  role_name VARCHAR(50) NOT NULL,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive tinyint(1) NOT NULL DEFAULT '1'
);
INSERT INTO Roles (role_name, isActive) 
VALUES ('Administrator', 1);

CREATE TABLE Users (
  user_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(50) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  role_id INT NOT NULL,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive tinyint(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (role_id) REFERENCES Roles(role_id)
  
);
INSERT INTO Users (username, password_hash, role_id,isActive)
VALUES ('{adminUsername}', '{adminPassword}', 1, 1);

CREATE TABLE Entities (
  entity_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  entity_name VARCHAR(100) NOT NULL,
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive tinyint(1) NOT NULL DEFAULT '1'
);

CREATE TABLE PermissionMatrix (
  permission_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
  role_id INT NOT NULL,
  entity_id INT NOT NULL,
  can_read TINYINT(1),
  can_write TINYINT(1),
  can_update TINYINT(1),
  can_delete TINYINT(1),
  user_id INT, -- Attribute for user identity
  owner_name VARCHAR(255), -- Attribute for owner of the resource
  createdBy VARCHAR(255),
  modifiedBy VARCHAR(255),
  createdAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  modifiedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  isActive tinyint(1) NOT NULL DEFAULT '1',
  FOREIGN KEY (role_id) REFERENCES Roles(role_id),
  FOREIGN KEY (entity_id) REFERENCES Entities(entity_id)
  
);

CREATE TRIGGER prevent_admin_insert
BEFORE INSERT ON Users
FOR EACH ROW
BEGIN
  IF NEW.username = '{adminUsername}' AND NEW.password_hash = '{adminPassword}' THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Cannot insert the specified record';
  END IF;
END;

CREATE TRIGGER prevent_admin_delete
BEFORE DELETE ON Users
FOR EACH ROW
BEGIN
  IF OLD.username = '{adminUsername}' AND OLD.password_hash = '{adminPassword}' THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Cannot delete the specified record';
  END IF;
END;

CREATE TRIGGER prevent_admin_update
BEFORE UPDATE ON Users
FOR EACH ROW
BEGIN
  IF OLD.username = '{adminUsername}' AND OLD.password_hash = '{adminPassword}' THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Cannot update the specified record';
  END IF;
END;
";
                        using (MySqlCommand command = new MySqlCommand(createTableSql2, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }



                    createDBfromSQL myDB = new createDBfromSQL(databaseName, "./SQLScript.sql", connection_string);
                    myDB.createDBbyPassingSQSL();
                    if (isRbac)
                    {
                        Console.WriteLine("Triggger_Rbac script for trigger is running......");
                        string createTableSql3 = @$"INSERT INTO Entities (entity_name)
SELECT table_name
FROM information_schema.tables
WHERE table_schema = '{databaseName}' -- Replace 'your_database_name' with your actual database name
AND table_type = 'BASE TABLE';

INSERT INTO PermissionMatrix (role_id, entity_id, can_read, can_write, can_update, can_delete, user_id,owner_name)
SELECT r.role_id, e.entity_id, 1, 1, 1, 1, u.user_id,u.username
FROM Roles r
CROSS JOIN Entities e
JOIN Users u ON u.role_id = r.role_id
WHERE r.role_name = 'Administrator';

CREATE TRIGGER prevent_permission_matrix_insert
BEFORE INSERT ON PermissionMatrix
FOR EACH ROW
BEGIN
  DECLARE user_role_id INT;
  
  SELECT role_id INTO user_role_id FROM Users WHERE user_id = NEW.user_id;
  
  IF user_role_id <> 1 THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'Only administrators are allowed to add entries to PermissionMatrix';
  END IF;
END;

CREATE TRIGGER prevent_permission_matrix_update
BEFORE UPDATE ON PermissionMatrix
FOR EACH ROW
BEGIN
  DECLARE user_role_id INT;
  
  SELECT role_id INTO user_role_id FROM Users WHERE user_id = NEW.user_id;
  
  IF user_role_id <> 1 THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'Only administrators are allowed to update entries in PermissionMatrix';
  END IF;
END;

CREATE TRIGGER prevent_permission_matrix_delete
BEFORE DELETE ON PermissionMatrix
FOR EACH ROW
BEGIN
  DECLARE user_role_id INT;
  
  SELECT role_id INTO user_role_id FROM Users WHERE user_id = OLD.user_id;
  
  IF user_role_id <> 1 THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'Only administrators are allowed to delete entries from PermissionMatrix';
  END IF;
END;


";
                        using (MySqlCommand command = new MySqlCommand(createTableSql3, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    MasterCredentialModel masterCredentialModel = new MasterCredentialModel()
                    {
                        project_id = Convert.ToInt32(project_id),
                        username = adminUsername,
                        password = adminPassword
                    };

                    connection_string += "database=" + databaseName + ";";
                    string conn1 = connection_string;
                    string createTableSql = "CREATE TABLE IF NOT EXISTS messageQueue (id INT AUTO_INCREMENT PRIMARY KEY, queueName VARCHAR(255) Unique,PrimaryKey VARCHAR(255))";
                    //using (MySqlConnection connection = new MySqlConnection(conn1))
                    //{
                    //    connection.Open();
                    //    if (connection.State == System.Data.ConnectionState.Open)
                    //    {
                    //        Console.WriteLine("First Connection is opened in ProcessFiles()");
                    //    }
                    using (MySqlCommand command = new MySqlCommand(createTableSql, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                    //connection.Close();
                    //if (connection.State == System.Data.ConnectionState.Closed)
                    //{
                    //    Console.WriteLine("First Connection is closed in ProcessFiles()");
                    //}
                    //}
                }
                string createTableSql1 = "CREATE TABLE IF NOT EXISTS messageQueue (id INT AUTO_INCREMENT PRIMARY KEY, queueName VARCHAR(255) Unique,PrimaryKey VARCHAR(255))";
                string conn2 = connection_string;
                //using (MySqlConnection connection = new MySqlConnection(conn2))
                //{
                //    connection.Open();
                //    if (connection.State == System.Data.ConnectionState.Open)
                //    {
                //        Console.WriteLine("Second Connection is opened in ProcessFiles()");
                //    }
                using (MySqlCommand command = new MySqlCommand(createTableSql1, connection))
                {
                    command.ExecuteNonQuery();
                }

                //connection.Close();
                //if (connection.State == System.Data.ConnectionState.Closed)
                //{
                //    Console.WriteLine("Second Connection is closed in ProcessFiles()");
                //}
                //}
                //using (MySqlConnection connection = new MySqlConnection(connection_string))
                //{
                //    connection.Open();
                //    if (connection.State == System.Data.ConnectionState.Open)
                //    {
                //        Console.WriteLine("Second Connection is opened in ProcessFiles()");
                //    }
                foreach (string tbl in GetTables(connection_string))
                {
                    List<string> PK = getPrimaryKey(tbl, connection_string);
                    string PrimaryKey = "";
                    for (int i = 0; i < PK.Count; i += 2)
                    {
                        PrimaryKey += PK[i];
                    }
                    string query = "REPLACE INTO messageQueue (queueName,PrimaryKey) Values (@name,@PrimaryKey)";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", char.ToUpper(tbl[0]) + tbl.Substring(1) + "Model");
                        command.Parameters.AddWithValue("@PrimaryKey", PrimaryKey);
                        command.ExecuteNonQuery();
                    }
                }
                //    connection.Close();
                //    if (connection.State == System.Data.ConnectionState.Closed)
                //    {
                //        Console.WriteLine("Second Connection is closed in ProcessFiles()");
                //    }
                //}
                ApiGeneratorFunctions.GetForeignKeyInfo(connection_string);
                makedictionary();
                foreach (string file in list_of_files)
                {
                    Console.WriteLine(file);
                    Dictionary<string, string> map = new Dictionary<string, string>();
                    var list = file.Split("/");
                    string last = list[^1];
                    if (last == "appsettings.txt")
                    {
                        Console.WriteLine("inside appsettings.txt");
                        map.Add("{projectName}", projectName);
                        map.Add("{connectionString}", connection_string);
                        map.Add("{RabbitMQConnectionString}", rabbitMQConn);
                        map.Add("{NODERED_URL}", noderedurl);
                        map.Add("{SWAGGER_URL}", swgurl);
                        map.Add("{redisConnectionString}", redisConn);
                        map.Add("{SubBucketName}", projectName + "_" + DateTime.Now.Ticks);
                        string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".API/", text = File.ReadAllText(file);
                        ProcessSingleFile(ref text, map);
                        File.WriteAllText(@destination + "/appsettings.json", text);
                        map.Clear();
                    }
                    else if (last == "controller.txt")
                    {
                        Console.WriteLine("inside controller.txt");
                        string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".API/Controllers/";
                        List<string> table = GetTables(connection_string);
                        foreach (string tbl in table)
                        {
                            if (tbl.ToLower() == "entities" || tbl.ToLower() == "permissionmatrix" || tbl.ToLower() == "users")
                            {
                                Console.WriteLine("escape" + tbl);
                                continue;
                            }
                            string text = File.ReadAllText(file);
                            
                            
                            ProcessController(ref map, connection_string, tbl, rabbitMQConn);
                            
                            string relational = @"
        [CheckPermission(""{tableName}"", ""Get"")]
        [HttpGet]
        [Route(APIEndpoint.DefaultRoute + ""/Relational/{primaryKeyListURL}"")]
        public ActionResult Get{tableName}Relational({primaryKeyList})
        {try
            {
			{primaryKeyListValidation}
            
            if (ValidationResult.IsError)
            {
                return BadRequest(new APIResponse(ResponseCode.ERROR, ""Validation failed"", ValidationResult));
            }
            string ownername = HttpContext.Items[""OwnerName""] as string;
            return Ok(Manager.Get{tableName}Relational(ownername,{primaryKeyListParam}));
			}catch(Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, ""Exception"", ex.Message));
            }
        }

        [CheckPermission(""{tableName}"", ""Get"")]
        [HttpGet]
        [Route(APIEndpoint.DefaultRoute + ""/Relational"")]
        public ActionResult GetAll{tableName}Relational(int page = 1, int itemsPerPage = 100,string orderBy = null)
        {try
            {
            if (page <= 0)
            {
                ValidationResult.AddFieldError(""Id"", ""Invalid page number"");
            }
            if (ValidationResult.IsError)
            {
                return BadRequest(new APIResponse(ResponseCode.ERROR, ""Validation failed"", ValidationResult));
            }
			List<OrderByModel> orderModelList = UtilityCommon.ConvertStringOrderToOrderModel(orderBy);
            string ownername = HttpContext.Items[""OwnerName""] as string;            
return Ok(Manager.GetAll{tableName}Relational(ownername,page, itemsPerPage,orderModelList));
			}catch(Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, ""Exception"", ex.Message));
            }
        }
";

                            ProcessSingleFile(ref relational, map);
                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                map.Add("{RelationalApiInegrationController}", relational);
                            else
                                map.Add("{RelationalApiInegrationController}", "");

                            string reporting = @"
        [CheckPermission(""{tableName}"", ""Get"")]
        [HttpGet]
        [Route(APIEndpoint.DefaultRoute + ""/Reporting/{primaryKeyListURL}"")]
        public ActionResult Get{tableName}Reporting({primaryKeyList})
        {try
            {
			{primaryKeyListValidation}
            
            if (ValidationResult.IsError)
            {
                return BadRequest(new APIResponse(ResponseCode.ERROR, ""Validation failed"", ValidationResult));
            }
string ownername = HttpContext.Items[""OwnerName""] as string;
            return Ok(Manager.Get{tableName}Reporting(ownername,{primaryKeyListParam}));
			}catch(Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, ""Exception"", ex.Message));
            }
        }

        [CheckPermission(""{tableName}"", ""Get"")]
        [HttpGet]
        [Route(APIEndpoint.DefaultRoute + ""/Reporting"")]
        public ActionResult GetAll{tableName}Reporting(int page = 1, int itemsPerPage = 100,string orderBy = null)
        {try
            {
            if (page <= 0)
            {
                ValidationResult.AddFieldError(""Id"", ""Invalid page number"");
            }
            if (ValidationResult.IsError)
            {
                return BadRequest(new APIResponse(ResponseCode.ERROR, ""Validation failed"", ValidationResult));
            }
			List<OrderByModel> orderModelList = UtilityCommon.ConvertStringOrderToOrderModel(orderBy);
            string ownername = HttpContext.Items[""OwnerName""] as string;            
return Ok(Manager.GetAll{tableName}Reporting(ownername,page, itemsPerPage,orderModelList));
			}catch(Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, ""Exception"", ex.Message));
            }
        }
";

                            ProcessSingleFile(ref reporting, map);
                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                map.Add("{ReportingApiInegrationController}", reporting);
                            else
                                map.Add("{ReportingApiInegrationController}", "");


                            string transactional = @"
        [CheckPermission(""{tableName}"", ""Post"")]
        [HttpPost]
        [Route(APIEndpoint.DefaultRoute + ""/Transactional"")]
        public ActionResult Post({tableName}TransactionalModel model)
        {try
            {
            return Ok(Manager.Add{tableName}Transactional(model));
			}catch(Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, ""Exception"", ex.Message));
            }
        }

        [CheckPermission(""{tableName}"", ""Put"")]
        [HttpPut]
        [Route(APIEndpoint.DefaultRoute + ""/Transactional/{primaryKeyListURL}"")]
        public ActionResult Put({primaryKeyList}, {tableName}TransactionalModel model)
        {try
            {
			{primaryKeyListValidation}
            
            if (ValidationResult.IsError)
            {
                return BadRequest(new APIResponse(ResponseCode.ERROR, ""Validation failed"", ValidationResult));
            }
            return Ok(Manager.Update{tableName}Transactional({primaryKeyListParam}, model));
			}catch(Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, ""Exception"", ex.Message));
            }
        }
        
";


                            ProcessSingleFile(ref transactional, map);
                            if (Transactional.ContainsKey(tbl))
                                map.Add("{TransactionalApiInegrationController}", transactional);
                            else
                                map.Add("{TransactionalApiInegrationController}", "");



                            ProcessSingleFile(ref text, map);


                            List<string> tblColumns = getAllColumns(tbl, "");
                            /////Kaival - Checking if isCreateby and ModifiedBy is existing exactly same so if its same then replacing model value with owner name so it can be avoided from frontend payload. 
                            ///Comment out if it wont required
                            bool isCreatedByExist = false;
                            bool isModifiedByExist = false;
                            foreach (string columnname in tblColumns)
                            {
                                if (columnname.Equals("createdBy"))
                                {
                                    isCreatedByExist = true;
                                }
                                if (columnname.Equals("modifiedBy"))
                                {
                                    isModifiedByExist = true;
                                }

                            }
                            if (isCreatedByExist && isModifiedByExist)
                            {
                                text = text.Replace("{{createdByModifiedByReplacer}}", "string ownername = HttpContext.Items[\"OwnerName\"] as string;\r\n                model.createdBy = ownername;\r\n                model.modifiedBy = ownername;");
                            }
                            else
                            {
                                text = text.Replace("{{createdByModifiedByReplacer}}", "");
                            }
                            ////////////////////////////////////////////////


                            string name = char.ToUpper(tbl[0]) + tbl.Substring(1) + "Controller.cs";
                            File.WriteAllText(@destination + name, text);
                            map.Clear();
                        }
                    }
                    else if (last == "idataaccess.txt")
                    {
                        Console.WriteLine("inside idataaccess.txt");
                        string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".DataAccess/Interface/";
                        //Directory.CreateDirectory(destination);
                        List<string> table = GetTables(connection_string);
                        foreach (string tbl in table)
                        {
                            if (tbl.ToLower() == "entities" || tbl.ToLower() == "permissionmatrix" || tbl.ToLower() == "users")
                            {
                                continue;
                            }
                            string text = File.ReadAllText(file);
                            ProcessInterfaceDataAccess(ref map, connection_string, tbl, rabbitMQConn);
                            string relational = @"{tableName}RelationalModel Get{tableName}Relational(string ownername,{primaryKeyListParam});
                List<{tableName}RelationalModel> GetAll{tableName}Relational(string ownername,int page=1,int itemsPerPage=100,List<OrderByModel> orderBy = null);";
                            ProcessSingleFile(ref relational, map);

                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                
                                map.Add("{RelationalApiInegrationIDataaccess}", relational);
                            else
                                map.Add("{RelationalApiInegrationIDataaccess}", "");
                            string reporting = @"{tableName}ReportingModel Get{tableName}Reporting(string ownername,{primaryKeyListParam});
List<{tableName}ReportingModel> GetAll{tableName}Reporting(string ownername,int page=1,int itemsPerPage=100,List<OrderByModel> orderBy = null);";
                            ProcessSingleFile(ref reporting, map);
                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                map.Add("{ReportingApiInegrationIDataaccess}", reporting);
                            else
                                map.Add("{ReportingApiInegrationIDataaccess}", "");
                            string transactional = @"int Add{tableName}Transactional({tableName}TransactionalModel model);
                        bool Update{tableName}Transactional({tableName}TransactionalModel model);";
                            ProcessSingleFile(ref transactional, map);
                            if (Transactional.ContainsKey(tbl))
                                map.Add("{TransactionalApiInegrationIDataaccess}", transactional);
                            else
                                map.Add("{TransactionalApiInegrationIDataaccess}", "");
                            ProcessSingleFile(ref text, map);
                            string name = "I" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "DataAccess.cs";
                            File.WriteAllText(@destination + name, text);
                            InterfacesAndImpl.Add(name.Substring(0, name.Length - 3), name.Substring(1, name.Length - 4));
                            map.Clear();
                        }
                    }
                    else if (last == "dataaccess.txt")
                    {
                        Console.WriteLine("inside dataaccess.txt");
                        string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".DataAccess/Impl/";
                        //Directory.CreateDirectory(destination);
                        List<string> table = GetTables(connection_string);
                        foreach (string tbl in table)
                        {
                            if (tbl.ToLower() == "s3bucket_folders" || tbl.ToLower() == "entities" || tbl.ToLower() == "permissionmatrix" || tbl.ToLower() == "users")
                            {
                                continue;
                            }
                            string text = File.ReadAllText(file);
                            ProcessDataAccess(ref map, connection_string, tbl, rabbitMQConn,"");
                            Boolean checkrelational = false;
                            Boolean checkreporting = false;
                            Boolean checktransactional = false;
                            string relationalapipath = @path + "/DotNetMySQLTemplate/relational_dataaccess.txt";
                            string reportingapipath = @path + "/DotNetMySQLTemplate/reporting_dataaccess.txt";
                            string transactionalapipath = @path + "/DotNetMySQLTemplate/transactional_dataaccess.txt";
                            string relationalapi = File.ReadAllText(relationalapipath);
                            string reportingapi = File.ReadAllText(reportingapipath);
                            string transactionalapi = File.ReadAllText(transactionalapipath);
                            string pos1path = @path + "/DotNetMySQLTemplate/pos1.txt";
                            string getallpos1path = @path + "/DotNetMySQLTemplate/getallpos1.txt";
                            string getallpos2path = @path + "/DotNetMySQLTemplate/getallpos2.txt";
                            string pos2path = @path + "/DotNetMySQLTemplate/pos2.txt";
                            string reportingpospath = @path + "/DotNetMySQLTemplate/reporting_pos.txt";
                            string getallreportingpospath = @path + "/DotNetMySQLTemplate/getallreportingpos.txt";
                            string functioncreationtransactionalpath = @path + "/DotNetMySQLTemplate/functioncreationtransactional.txt";
                            map.Add("{tableNameSmall}", tbl);
                            if (ApiGeneratorFunctions.referencedBy.ContainsKey(tbl))
                            {
                                StringBuilder referencedByPropertiesBuilder = new StringBuilder();
                                StringBuilder pos1fulfilledPropertiesBuilder = new StringBuilder();
                                StringBuilder getallpos1fulfilledPropertiesBuilder = new StringBuilder();
                                Dictionary<string, List<ForeignKeyInfo>> keyValuePairs = ApiGeneratorFunctions.tablesWithForeignKeys;
                                List<ForeignKeyInfo> fetchedList = new List<ForeignKeyInfo>();
                                if (keyValuePairs.ContainsKey(tbl))
                                {
                                    fetchedList = keyValuePairs[tbl];
                                }
                                foreach (var foreignKeyInfo in fetchedList)
                                {

                                    string referencedTableName = char.ToUpper(foreignKeyInfo.ReferencedTableName[0]) + foreignKeyInfo.ReferencedTableName.Substring(1);
                                    string referencedTableNameSmall = foreignKeyInfo.ReferencedTableName;
                                    string referencingColumnName = foreignKeyInfo.ColumnName;
                                    string referencingTable =foreignKeyInfo.TableName;
                                    checkrelational = true;
                                    string referencingTableTmp = char.ToUpper(referencingTable[0]) + referencingTable.Substring(1);
                                    // Generate the property for the list of related entities
                                    List<string> allref = ApiGeneratorFunctions.referencedBy[tbl];
                                    string propertyLine = "";
                                    if (allref[allref.Count - 1] == referencingTable)
                                        propertyLine = $"    {referencingColumnName}_{referencedTableName} = new {referencedTableName}RelationalModel(),";
                                    else
                                        propertyLine = $"    {referencingColumnName}_{referencedTableName} = new {referencedTableName}RelationalModel(),";
                                    referencedByPropertiesBuilder.AppendLine(propertyLine);
                                    string pos1 = File.ReadAllText(pos1path);
                                    string getallpos1 = File.ReadAllText(getallpos1path);
                                    List<string> PK1 = getPrimaryKey(tbl, connection_string);
                                    string singleQuery = "SELECT  t.* FROM " + referencingTable + " t  WHERE t.isActive=1 AND t.createdBy=@ownername AND ";
                                    string singleParams = "cmd.Parameters.AddWithValue";
                                    ProcessSingleQuery(ref singleQuery, ref singleParams, PK1,"");  
                                    string singleParamsgetall = "cmd.Parameters.AddWithValue";
                                    //Kaival - Checkpoint
                                    ApiGeneratorFunctions.ProcessSingleQueryGetAll(ref singleParamsgetall, tbl, PK1,referencingColumnName);
                                    string selectAllRfModelInit = "";
                                    ProcessSelectAllModelInit(ref selectAllRfModelInit, referencedTableNameSmall, connection_string,"");
                                    pos1 = pos1.Replace("{tableNameSmall}", tbl).Replace("{rftableNameMain}", referencedTableName).Replace("{rftableName}", referencingColumnName + "_" + referencedTableName).Replace("{rftablenameSmall}", referencedTableNameSmall).Replace("{selectOneQuery}", singleQuery).Replace("{getByIDQueryParameter}", singleParams)
                                            .Replace("{selectAllRfModelInit}", selectAllRfModelInit);
                                    pos1fulfilledPropertiesBuilder.AppendLine(pos1);
                                    getallpos1 = getallpos1.Replace("{tableNameSmall}", tbl).Replace("{rftableNameMain}", referencedTableName).Replace("{rftableName}", referencingColumnName+"_"+referencedTableName).Replace("{rftablenameSmall}", referencedTableNameSmall).Replace("{selectOneQuery}", singleQuery).Replace("{getByIDQueryParameter}", singleParamsgetall)
                                            .Replace("{selectAllRfModelInit}", selectAllRfModelInit);
                                    getallpos1fulfilledPropertiesBuilder.AppendLine(getallpos1);
                                }
                                map.Add("{pos1fulfilledintiallize}", referencedByPropertiesBuilder.ToString());
                                map.Add("{pos1fulfilled}", pos1fulfilledPropertiesBuilder.ToString());
                                map.Add("{getallpos1fulfilled}", getallpos1fulfilledPropertiesBuilder.ToString());
                            }
                            else
                            {
                                map.Add("{pos1fulfilledintiallize}", "");
                                map.Add("{pos1fulfilled}", "");
                                map.Add("{getallpos1fulfilled}", "");
                            }

                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                            {
                                StringBuilder pos2fulfilledPropertiesBuilder = new StringBuilder();
                                StringBuilder getallpos2fulfilledPropertiesBuilder = new StringBuilder();
                                StringBuilder reportingposfulfilledPropertiesBuilder = new StringBuilder();
                                StringBuilder getallreportingposfulfilledPropertiesBuilder = new StringBuilder();

                                Dictionary<string, List<ForeignKeyInfo>> keyValuePairs = ApiGeneratorFunctions.tablesWithForeignKeys;
                                List<ForeignKeyInfo> fetchedList = new List<ForeignKeyInfo>();
                                if (keyValuePairs.ContainsKey(tbl))
                                {
                                    fetchedList = keyValuePairs[tbl];
                                }
                                foreach (var foreignKeyInfo in fetchedList)
                                {
                                  
                                
                                    checkrelational = true;
                                    checkreporting = true;
                                    string referencedTableName = char.ToUpper(foreignKeyInfo.ReferencedTableName[0]) + foreignKeyInfo.ReferencedTableName.Substring(1);
                                    string referencedTableNameSmall = foreignKeyInfo.ReferencedTableName;
                                    string referencingColumnName = foreignKeyInfo.ColumnName;
                              

                                    // Generate the property for the related entity
                                    string pos2 = File.ReadAllText(pos2path);
                                    string getallpos2 = File.ReadAllText(getallpos2path);
                                    string reportingpos = File.ReadAllText(reportingpospath);
                                    string getallreportingpos = File.ReadAllText(getallreportingpospath);
                                    List<string> PK1 = getPrimaryKey(referencedTableNameSmall, connection_string);
                                    string singleQuery = "SELECT  t.* FROM " + referencedTableNameSmall + " t  WHERE t.isActive=1 AND t.createdBy=@ownername AND ";
                                    string singleParams = "cmd.Parameters.AddWithValue";
                                    //Kaival - Checkpoint reporting
                                    ApiGeneratorFunctions.ProcessSingleQueryWithObjectName(ref singleQuery, ref singleParams, PK1, tbl,referencingColumnName);
                                    string singleParamsgetall = "cmd.Parameters.AddWithValue";
                                    ApiGeneratorFunctions.ProcessSingleQueryGetAll(ref singleParamsgetall, tbl, PK1,referencingColumnName);
                                    string selectAllRfModelInit = "";
                                    ProcessSelectAllModelInit(ref selectAllRfModelInit, referencedTableNameSmall, connection_string,"");
                                    pos2 = pos2.Replace("{tableNameSmall}", tbl).Replace("{rftableNameMain}", referencedTableName).Replace("{rftableName}", referencingColumnName+"_"+ referencedTableName).Replace("{rftablenameSmall}", referencedTableNameSmall).Replace("{selectOneQuery}", singleQuery).Replace("{getByIDQueryParameter}", singleParams)
                                            .Replace("{selectAllRfModelInit}", selectAllRfModelInit);
                                    pos2fulfilledPropertiesBuilder.AppendLine(pos2);
                                    getallpos2 = getallpos2.Replace("{tableNameSmall}", tbl).Replace("{rftableNameMain}", referencedTableName).Replace("{rftableName}", referencingColumnName + "_" + referencedTableName).Replace("{rftablenameSmall}", referencedTableNameSmall).Replace("{selectOneQuery}", singleQuery).Replace("{getByIDQueryParameter}", singleParamsgetall)
                                            .Replace("{selectAllRfModelInit}", selectAllRfModelInit);
                                    getallpos2fulfilledPropertiesBuilder.AppendLine(getallpos2);
                                    string selectgetAllRfRpModelInit = "";
                                    ProcessSelectgetAllModelInitReporting(ref selectgetAllRfRpModelInit, referencedTableNameSmall, connection_string, tbl,referencingColumnName);
                                    getallreportingpos = getallreportingpos.Replace("{tableNameSmall}", tbl).Replace("{selectOneQuery}", singleQuery).Replace("{getByIDQueryParameter}", singleParamsgetall)
                                            .Replace("{selectgetAllRfRpModelInit}", selectgetAllRfRpModelInit);
                                    getallreportingposfulfilledPropertiesBuilder.AppendLine(getallreportingpos);
                                    string selectAllRfRpModelInit = "";
                                    ProcessSelectAllModelInitReporting(ref selectAllRfRpModelInit, referencedTableNameSmall, connection_string, tbl, referencingColumnName);
                                    reportingpos = reportingpos.Replace("{tableNameSmall}", tbl).Replace("{selectOneQuery}", singleQuery).Replace("{getByIDQueryParameter}", singleParams)
                                            .Replace("{selectAllRfRpModelInit}", selectAllRfRpModelInit);
                                    reportingposfulfilledPropertiesBuilder.AppendLine(reportingpos);
                                }
                                map.Add("{pos2fulfilled}", pos2fulfilledPropertiesBuilder.ToString());
                                map.Add("{posreportingfulfilled}", reportingposfulfilledPropertiesBuilder.ToString());
                                map.Add("{getallpos2fulfilled}", getallpos2fulfilledPropertiesBuilder.ToString());
                                map.Add("{getallposreportingfulfilled}", getallreportingposfulfilledPropertiesBuilder.ToString());
                            }
                            else
                            {
                                map.Add("{pos2fulfilled}", "");
                                map.Add("{posreportingfulfilled}", "");
                                map.Add("{getallpos2fulfilled}", "");
                                map.Add("{getallposreportingfulfilled}", "");
                            }
                            if (Transactional.ContainsKey(tbl))
                            {
                                StringBuilder deleterelatedfunctioncallPropertiesBuilder = new StringBuilder();
                                StringBuilder addrelatedfunctioncallPropertiesBuilder = new StringBuilder();
                                StringBuilder updaterelatedfunctioncallPropertiesBuilder = new StringBuilder();
                                StringBuilder AddOperationForRelatedEntityPropertiesBuilder = new StringBuilder();
                                foreach (var referencingTable in Transactional[tbl].Sequence)
                                {
                                    checktransactional = true;
                                    string referencingTableSmall = referencingTable.ToLower();
                                    string referencingTableTmp = char.ToUpper(referencingTableSmall[0]) + referencingTableSmall.Substring(1);
                                    // Generate the property for the list of related entities
                                    string refcolumn = ApiGeneratorFunctions.GetForeignKeyColumnname(referencingTableSmall, tbl);
                                    string propertyLine2 = $@"
                                        Delete{referencingTableTmp}ById(model.{refcolumn},cmd,ref isroll);
                                    ";
                                    deleterelatedfunctioncallPropertiesBuilder.AppendLine(propertyLine2);
                                    if (Transactional[tbl].Relations[referencingTable] == "many")
                                    {
                                        string propertyLine = $@"
                                        foreach (var {referencingTableSmall} in model.{referencingTableTmp})
                                        {{
                                            {referencingTableSmall}.{refcolumn} = insertedId;
                                            Add{referencingTableTmp}({referencingTableSmall}, cmd,ref isroll);
                                        }}
                                    ";
                                        string propertyLine1 = $@"
                                        foreach (var {referencingTableSmall} in model.{referencingTableTmp})
                                        {{
                                            {referencingTableSmall}.{refcolumn} = model.{refcolumn};
                                            Add{referencingTableTmp}({referencingTableSmall}, cmd,ref isroll);
                                        }}
                                    ";
                                        addrelatedfunctioncallPropertiesBuilder.AppendLine(propertyLine);
                                        updaterelatedfunctioncallPropertiesBuilder.AppendLine(propertyLine1);
                                    }
                                    else
                                    {
                                        string propertyLine = $@"
                                            model.{referencingTableTmp}.{refcolumn} = insertedId;
                                            Add{referencingTableTmp}(model.{referencingTableTmp}, cmd,ref isroll);
                                    ";
                                        string propertyLine1 = $@"
                                            model.{referencingTableTmp}.{refcolumn} = model.{refcolumn};
                                            Add{referencingTableTmp}(model.{referencingTableTmp}, cmd,ref isroll);
                                    ";
                                        addrelatedfunctioncallPropertiesBuilder.AppendLine(propertyLine);
                                        updaterelatedfunctioncallPropertiesBuilder.AppendLine(propertyLine1);
                                    }

                                    string functioncreationtransactional = File.ReadAllText(functioncreationtransactionalpath);
                                    List<string> columns = getAllColumns(referencingTableSmall, connection_string);
                                    string insert_query = "";
                                    ProcessInsertQuery(ref insert_query, columns, referencingTableSmall);
                                    string insert_q = "";
                                    ApiGeneratorFunctions.insertParameter(ref insert_q, columns);
                                    functioncreationtransactional = functioncreationtransactional.Replace("{referencingTableTmp}", referencingTableTmp).Replace("{InsertQuery}", insert_query).Replace("{parameterAdd}", insert_q).Replace("{refcolumn}", refcolumn);
                                    AddOperationForRelatedEntityPropertiesBuilder.AppendLine(functioncreationtransactional);
                                }
                                map.Add("{AddOperationForRelatedEntity}", AddOperationForRelatedEntityPropertiesBuilder.ToString());
                                map.Add("{addrelatedfunctioncall}", addrelatedfunctioncallPropertiesBuilder.ToString());
                                map.Add("{updaterelatedfunctioncall}", updaterelatedfunctioncallPropertiesBuilder.ToString());
                                map.Add("{deleterelatedfunctioncall}", deleterelatedfunctioncallPropertiesBuilder.ToString());
                            }
                            else
                            {
                                map.Add("{AddOperationForRelatedEntity}", "");
                                map.Add("{addrelatedfunctioncall}", "");
                                map.Add("{updaterelatedfunctioncall}", "");
                                map.Add("{deleterelatedfunctioncall}", "");
                            }
                            ProcessSingleFile(ref relationalapi, map);
                            ProcessSingleFile(ref reportingapi, map);
                            ProcessSingleFile(ref transactionalapi, map);
                            if (checkrelational)
                            {
                                map.Add("{RelationalApiInegration}", relationalapi);
                            }
                            else
                            {
                                map.Add("{RelationalApiInegration}", "");
                            }
                            if (checkreporting)
                            {
                                map.Add("{ReportingApiInegration}", reportingapi);
                            }
                            else
                            {
                                map.Add("{ReportingApiInegration}", "");
                            }
                            if (checktransactional)
                            {
                                map.Add("{TransactionalApiInegration}", transactionalapi);
                            }
                            else
                            {
                                map.Add("{TransactionalApiInegration}", "");
                            }

                            ProcessSingleFile(ref text, map);
                            List<string> tblColumns = getAllColumns(tbl, "");
                            bool isCreatedAtExist = false;
                            bool isModifiedAtExist = false;
                            foreach (string columnname in tblColumns)
                            {
                                if (columnname.Equals("createdAt"))
                                {
                                    isCreatedAtExist = true;
                                }
                                if (columnname.Equals("modifiedAt"))
                                {
                                    isModifiedAtExist = true;
                                }

                            }
                            if (isCreatedAtExist && isModifiedAtExist)
                            {
                                text = text.Replace("{{createdAtModifiedAtReplacerPost}}", "model.createdAt = DateTime.UtcNow.ToString(\"yyyy-MM-ddTHH:mm:ss\");\r\n                model.modifiedAt = DateTime.UtcNow.ToString(\"yyyy-MM-ddTHH:mm:ss\");");
                                text = text.Replace("{{createdAtModifiedAtReplacerPut}}", "if (string.IsNullOrEmpty(model.createdAt))\r\n                {\r\n                    model.createdAt = DateTime.UtcNow.ToString(\"yyyy-MM-dd HH:mm:ss\");\r\n                }\r\n                else\r\n                {\r\n                    string[] formats = new string[]\r\n{\r\n    // Standard formats\r\n    \"MM/dd/yyyy HH:mm:ss\",\r\n    \"yyyy-MM-dd HH:mm:ss\",\r\n    \"MM/dd/yyyy HH:mm:ss.fff\",\r\n    \"yyyy-MM-dd HH:mm:ss.fff\",\r\n    \"MM/dd/yyyy H:mm:ss\",   // Example: 02/15/2024 6:28:16\r\n    \"yyyy-MM-dd H:mm:ss\",   // Example: 2024-02-15 6:28:16\r\n    \"MM/dd/yyyy H:mm:ss tt\",  // Example: 02/15/2024 6:28:16 AM\r\n    \"yyyy-MM-dd H:mm:ss tt\",  // Example: 2024-02-15 6:28:16 AM\r\n    // Other common formats\r\n    \"MM/dd/yyyy\",\r\n    \"yyyy-MM-dd\",\r\n    \"yyyy-dd-MM\",\r\n    \"MM-dd-yyyy\",\r\n    \"dd/MM/yyyy\",\r\n    \"dd-MM-yyyy\",\r\n    \"MM/dd/yy\",\r\n    \"yy/MM/dd\",\r\n    \"dd-MMM-yyyy\",\r\n    \"MMM dd, yyyy\",\r\n    // Date and time with milliseconds\r\n    \"MM/dd/yyyy HH:mm:ss.fff\",\r\n    \"yyyy-MM-dd HH:mm:ss.fff\",\r\n    // Date and time with timezone\r\n    \"yyyy-MM-ddTHH:mm:sszzz\",\r\n    \"yyyy-MM-ddTHH:mm:sszz\",\r\n    \"yyyy-MM-ddTHH:mm:ssZ\",\r\n    // Add more formats here as needed to cover all possible datetime representations\r\n};\r\n\r\n                    // Try parsing the provided datetime string into a DateTime object\r\n                    if (!DateTime.TryParseExact(model.createdAt, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime createdAtDateTime))\r\n                    {\r\n                        throw new ArgumentException(\"Invalid datetime format for createdAt field.\");\r\n                    }\r\n\r\n                    // Format the parsed datetime into the desired MySQL format\r\n                    model.createdAt = createdAtDateTime.ToString(\"yyyy-MM-dd HH:mm:ss\");\r\n                }\r\n                model.modifiedAt = DateTime.UtcNow.ToString(\"yyyy-MM-ddTHH:mm:ss\");");
                            }
                            else
                            {
                                text = text.Replace("{{createdAtModifiedAtReplacerPost}}", "");
                                text = text.Replace("{{createdAtModifiedAtReplacerPut}}", "");
                            }
                            List<string> PK = getPrimaryKey(tbl, connection_string);
                            if (PK.Count > 1)
                                text = text.Replace("{MultiplePrimaryKeyCheck}", "if(recs==1)\n\treturn recs;");
                            else
                                text = text.Replace("{MultiplePrimaryKeyCheck}", "");

                            string name = char.ToUpper(tbl[0]) + tbl.Substring(1) + "DataAccess.cs";
                            File.WriteAllText(@destination + name, text);
                            map.Clear();
                        }
                    }
                    else if (last == "aggregate_dataaccess.txt")
                    {
                        if (aggregateModel.Count > 0)
                        {
                            string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".DataAccess/Impl/";
                            string text = File.ReadAllText(file);
                            string aggregatefunctionpath = @path + "/DotNetMySQLTemplate/aggregatefunction.txt";
                            StringBuilder aggregatePropertiesBuilder = new StringBuilder();
                            foreach (ApiFlow tp in aggregateModel)
                            {
                                string aggregatefunction = File.ReadAllText(aggregatefunctionpath);

                                List<string> pm = new List<string>();
                                foreach (ApiParameter apiparameter in tp.Parameters)
                                {
                                    pm.Add(apiparameter.Name);
                                    pm.Add(apiparameter.Type);
                                }

                                string primaryParams = "";
                                ProcessPrimaryKeyListParams(ref primaryParams, pm);
                                StringBuilder ParameterBuilder = new StringBuilder();
                                foreach (Condition condition in tp.Query.Conditions)
                                {
                                    string singleParams = "cmd.Parameters.AddWithValue";
                                    singleParams += "(" + "\"@" + condition.Parameter + "\"," + condition.Parameter + ");";
                                    ParameterBuilder.AppendLine(singleParams);
                                }
                                StringBuilder PropBuilder = new StringBuilder();
                                foreach (Property properties in tp.DTO.Properties)
                                {
                                    string prop = $"{properties.Name} = ({properties.Type})reader[\"{properties.Name}\"],";
                                    PropBuilder.AppendLine(prop);
                                }
                                string query = GenerateSql.generateSQL(tp);
                                aggregatefunction = aggregatefunction.Replace("{DTOclass}", tp.DTO.Name).Replace("{primaryKeyListParam}", primaryParams)
                                    .Replace("{aggregateQuery}", query).Replace("{aggregateQueryParameter}", ParameterBuilder.ToString())
                                    .Replace("{DTOPropertiesModelInit}", PropBuilder.ToString());
                                //MySqlConnection conn = new MySqlConnection(connection_string);
                                //conn.Open();
                                MySqlConnection conn = MySqlConnectionManager.Instance.GetConnection();
                                aggregatePropertiesBuilder.AppendLine(aggregatefunction);
                                map.Clear();
                            }
                            text = text.Replace("{allaggregatefunction}", aggregatePropertiesBuilder.ToString());
                            string name = "AggregateDataAccess.cs";
                            File.WriteAllText(@destination + name, text);
                            map.Clear();
                        }
                    }
                    else if (last == "aggregate_idataaccess.txt")
                    {
                        if (aggregateModel.Count > 0)
                        {
                            string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".DataAccess/Interface/";
                            string text = File.ReadAllText(file);
                            StringBuilder aggregatePropertiesBuilder = new StringBuilder();
                            foreach (ApiFlow tp in aggregateModel)
                            {
                                string aggregatefunction = @"{DTOclass}Model Get{DTOclass}({primaryKeyListParam});";

                                List<string> pm = new List<string>();
                                foreach (ApiParameter apiparameter in tp.Parameters)
                                {
                                    pm.Add(apiparameter.Name);
                                    pm.Add(apiparameter.Type);
                                }

                                string primaryParams = "";
                                ProcessPrimaryKeyListParams(ref primaryParams, pm);
                                aggregatefunction = aggregatefunction.Replace("{DTOclass}", tp.DTO.Name).Replace("{primaryKeyListParam}", primaryParams);
                                //MySqlConnection conn = new MySqlConnection(connection_string);
                                //conn.Open();
                                MySqlConnection conn = MySqlConnectionManager.Instance.GetConnection();
                                aggregatePropertiesBuilder.AppendLine(aggregatefunction);
                                map.Clear();
                            }
                            text = text.Replace("{allaggregatefunctionidataaccess}", aggregatePropertiesBuilder.ToString());
                            string name = "IAggregateDataAccess.cs";
                            File.WriteAllText(@destination + name, text);
                            map.Clear();
                        }
                    }
                    else if (last == "aggregate_manager.txt")
                    {
                        if (aggregateModel.Count > 0)
                        {
                            string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".Manager/Impl/";
                            string text = File.ReadAllText(file);
                            StringBuilder aggregatePropertiesBuilder = new StringBuilder();
                            foreach (ApiFlow tp in aggregateModel)
                            {
                                string aggregatefunction = @"
        public APIResponse Get{DTOclass}({primaryKeyListParam})
        {
            var result = DataAccess.Get{DTOclass}({primaryKeyList});
            if (result != null)
            {
                return new APIResponse(ResponseCode.SUCCESS, ""Record Found"", result);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, ""No Record Found"");
            }
        }";

                                List<string> pm = new List<string>();
                                foreach (ApiParameter apiparameter in tp.Parameters)
                                {
                                    pm.Add(apiparameter.Name);
                                    pm.Add(apiparameter.Type);
                                }

                                string primaryParams = "";
                                ProcessPrimaryKeyListParams(ref primaryParams, pm);

                                string primaryKeyList = "";
                                ApiGeneratorFunctions.Filledparams(ref primaryKeyList, pm);
                                aggregatefunction = aggregatefunction.Replace("{DTOclass}", tp.DTO.Name).Replace("{primaryKeyListParam}", primaryParams)
                                    .Replace("{primaryKeyList}", primaryKeyList);
                                //MySqlConnection conn = new MySqlConnection(connection_string);
                                //conn.Open();
                                MySqlConnection conn = MySqlConnectionManager.Instance.GetConnection();
                                aggregatePropertiesBuilder.AppendLine(aggregatefunction);
                                map.Clear();
                            }
                            text = text.Replace("{allaggregatefunctionmanager}", aggregatePropertiesBuilder.ToString());
                            string name = "AggregateManager.cs";
                            File.WriteAllText(@destination + name, text);
                            map.Clear();
                        }
                    }
                    else if (last == "aggregate_imanager.txt")
                    {
                        if (aggregateModel.Count > 0)
                        {
                            string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".Manager/Interface/";
                            string text = File.ReadAllText(file);
                            StringBuilder aggregatePropertiesBuilder = new StringBuilder();
                            foreach (ApiFlow tp in aggregateModel)
                            {
                                string aggregatefunction = @"APIResponse Get{DTOclass}({primaryKeyListParam});";

                                List<string> pm = new List<string>();
                                foreach (ApiParameter apiparameter in tp.Parameters)
                                {
                                    pm.Add(apiparameter.Name);
                                    pm.Add(apiparameter.Type);
                                }

                                string primaryParams = "";
                                ProcessPrimaryKeyListParams(ref primaryParams, pm);
                                aggregatefunction = aggregatefunction.Replace("{DTOclass}", tp.DTO.Name).Replace("{primaryKeyListParam}", primaryParams);
                                //MySqlConnection conn = new MySqlConnection(connection_string);
                                //conn.Open();
                                MySqlConnection conn = MySqlConnectionManager.Instance.GetConnection();
                                aggregatePropertiesBuilder.AppendLine(aggregatefunction);
                                map.Clear();
                            }
                            text = text.Replace("{allaggregatefunctionimanager}", aggregatePropertiesBuilder.ToString());
                            string name = "IAggregateManager.cs";
                            File.WriteAllText(@destination + name, text);
                            map.Clear();
                        }
                    }
                    else if (last == "aggregate_controller.txt")
                    {
                        if (aggregateModel.Count > 0)
                        {
                            string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".API/Controllers/";
                            string text = File.ReadAllText(file);
                            StringBuilder aggregatePropertiesBuilder = new StringBuilder();
                            foreach (ApiFlow tp in aggregateModel)
                            {
                                string aggregatefunction = @"
        [CheckPermission(""{tbname}"", ""Get"")]
        [HttpGet]
        [Route(APIEndpoint.DefaultRoute + ""/{DTOclass}/{primaryKeyListURL}"")]
        public ActionResult Get{DTOclass}({primaryKeyList})
        {try
            {
			{primaryKeyListValidation}
            
            if (ValidationResult.IsError)
            {
                return BadRequest(new APIResponse(ResponseCode.ERROR, ""Validation failed"", ValidationResult));
            }
            return Ok(Manager.Get{DTOclass}({primaryKeyListParam}));
			}catch(Exception ex)
            {
                return StatusCode(500, new APIResponse(ResponseCode.ERROR, ""Exception"", ex.Message));
            }
        }
";

                                List<string> pm = new List<string>();
                                foreach (ApiParameter apiparameter in tp.Parameters)
                                {
                                    pm.Add(apiparameter.Name);
                                    pm.Add(apiparameter.Type);
                                }

                                string primaryKeyListURL = "", primaryKeyList = "", primaryKeyListValidation = "", primaryKeyListParam = "";
                                ApiGeneratorFunctions.CreateParamsAggregateContoller(ref primaryKeyListURL, ref primaryKeyList, ref primaryKeyListParam, ref primaryKeyListValidation, pm);
                                aggregatefunction = aggregatefunction.Replace("{DTOclass}", tp.DTO.Name).Replace("{primaryKeyListURL}", primaryKeyListURL)
                                    .Replace("{primaryKeyList}", primaryKeyList).Replace("{primaryKeyListValidation}", primaryKeyListValidation)
                                    .Replace("{primaryKeyListParam}", primaryKeyListParam).Replace("{tbname}", tp.Query.Tables[0]);
                                //MySqlConnection conn = new MySqlConnection(connection_string);
                                //conn.Open();
                                MySqlConnection conn = MySqlConnectionManager.Instance.GetConnection();
                                aggregatePropertiesBuilder.AppendLine(aggregatefunction);
                                map.Clear();
                            }
                            text = text.Replace("{allaggregatefunctioncontroller}", aggregatePropertiesBuilder.ToString());
                            string name = "AggregateController.cs";
                            File.WriteAllText(@destination + name, text);
                            map.Clear();
                        }
                    }
                    else if (last == "imanager.txt")
                    {
                        string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".Manager/Interface/";
                        //Directory.CreateDirectory(destination);
                        List<string> table = GetTables(connection_string);
                        foreach (string tbl in table)
                        {
                            if (tbl.ToLower() == "entities" || tbl.ToLower() == "permissionmatrix" || tbl.ToLower() == "users")
                            {
                                continue;
                            }
                            string text = File.ReadAllText(file);
                            ProcessInterfaceManager(ref map, connection_string, tbl, rabbitMQConn);

                            string relational = @"APIResponse Get{tableName}Relational(string ownername,{primaryKeyListParam});
APIResponse GetAll{tableName}Relational(string ownername,int page, int itemsPerPage,List<OrderByModel> orderBy);";
                            ProcessSingleFile(ref relational, map);
                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                map.Add("{RelationalApiInegrationIManager}", relational);
                            else
                                map.Add("{RelationalApiInegrationIManager}", "");
                            string reporting = @"APIResponse Get{tableName}Reporting(string ownername,{primaryKeyListParam});
APIResponse GetAll{tableName}Reporting(string ownername,int page, int itemsPerPage,List<OrderByModel> orderBy);";
                            ProcessSingleFile(ref reporting, map);
                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                map.Add("{ReportingApiInegrationIManager}", reporting);
                            else
                                map.Add("{ReportingApiInegrationIManager}", "");
                            string transactional = @"APIResponse Add{tableName}Transactional({tableName}TransactionalModel model);
APIResponse Update{tableName}Transactional({primaryKeyListParam},{tableName}TransactionalModel model);";
                            ProcessSingleFile(ref transactional, map);
                            if (Transactional.ContainsKey(tbl))
                                map.Add("{TransactionalApiInegrationIManager}", transactional);
                            else
                                map.Add("{TransactionalApiInegrationIManager}", "");
                            ProcessSingleFile(ref text, map);
                            /* string relationalcontroller = "";

                             List<string> FK = getForeignKey(tbl, connection_string);
                             for (int i = 0; i < FK.Count; i += 3)
                             {
                                 relationalcontroller += $@"
                             APIResponse GetBy{FK[i]}(int {FK[i]});
                         ";
                             }

                             text.Replace("{relationalcontroller}", relationalcontroller);*/
                            string name = "I" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "Manager.cs";
                            File.WriteAllText(@destination + name, text);
                            InterfacesAndImpl.Add(name.Substring(0, name.Length - 3), name.Substring(1, name.Length - 4));
                            map.Clear();
                        }
                    }
                    else if (last == "manager.txt")
                    {
                        string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".Manager/Impl/";
                        //Directory.CreateDirectory(destination);
                        List<string> table = GetTables(connection_string);
                        foreach (string tbl in table)
                        {
                            if (tbl.ToLower() == "entities" || tbl.ToLower() == "permissionmatrix" || tbl.ToLower() == "users")
                            {
                                continue;
                            }
                            string text = File.ReadAllText(file);
                            List<string> PK = getPrimaryKey(tbl, connection_string);
                            string primarykey = "";
                            for (int i = 0; i < PK.Count; i += 2)
                            {
                                if (PK[i + 1].ToLower() == "int")
                                {
                                    primarykey += @$"primary_key.Add(""{PK[i]}"", {PK[i]});" + "\n";
                                }
                            }
                            map.Add("{primaryKeyListSingle}", primarykey);

                            string temp = "", primaryKeyListAll = "";
                            for (int i = 0; i < PK.Count; i += 2)
                            {
                                string dataType = utility.getType(PK[i + 1]);
                                primaryKeyListAll += PK[i];
                                if (dataType.Contains("string"))
                                    continue;
                                temp += "model." + PK[i] + "=";
                                temp += "Convert.ToInt32(result);\n";
                            }
                            map.Add("{addPrimaryKeyList}", temp);
                            text = text.Replace("{convertPrimaryKey}", temp);
                            text = text.Replace("{primaryKeyListAll}", primaryKeyListAll);
                            ProcessManager(ref map, connection_string, tbl, rabbitMQConn);

                            string relational = @"
        public APIResponse Get{tableName}Relational(string ownername,{primaryKeyListParam})
        {
            var result = DataAccess.Get{tableName}Relational(ownername,{primaryKeyList});
            if (result != null)
            {
                return new APIResponse(ResponseCode.SUCCESS, ""Record Found"", result);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, ""No Record Found"");
            }
        }    

        public APIResponse GetAll{tableName}Relational(string ownername,int page = 1, int itemsPerPage = 100,List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.GetAll{tableName}Relational(ownername,page,itemsPerPage,orderBy);
            if (result != null && result.Count > 0)
            { 
                var totalRecords = DataAccess.GetAllTotalRecord{tableName}ByCreatedBy(ownername);
                var response = new { records = result, pageNumber = page, pageSize = itemsPerPage, totalRecords = totalRecords };
                return new APIResponse(ResponseCode.SUCCESS, ""Record Found"", response);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, ""No Record Found"");
            }
        }
                            ";
                            ProcessSingleFile(ref relational, map);
                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                map.Add("{RelationalApiInegrationManager}", relational);
                            else
                                map.Add("{RelationalApiInegrationManager}", "");


                            string reporting = @"
        public APIResponse Get{tableName}Reporting(string ownername,{primaryKeyListParam})
        {
            var result = DataAccess.Get{tableName}Reporting(ownername,{primaryKeyList});
            if (result != null)
            {
                return new APIResponse(ResponseCode.SUCCESS, ""Record Found"", result);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, ""No Record Found"");
            }
        }          

        public APIResponse GetAll{tableName}Reporting(string ownername,int page = 1, int itemsPerPage = 100,List<OrderByModel> orderBy = null)
        {
            var result = DataAccess.GetAll{tableName}Reporting(ownername,page,itemsPerPage,orderBy);
            if (result != null && result.Count > 0)
            { 
                var totalRecords = DataAccess.GetAllTotalRecord{tableName}ByCreatedBy(ownername);
                var response = new { records = result, pageNumber = page, pageSize = itemsPerPage, totalRecords = totalRecords };
                return new APIResponse(ResponseCode.SUCCESS, ""Record Found"", response);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, ""No Record Found"");
            }
        }
                            ";
                            ProcessSingleFile(ref reporting, map);
                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                                map.Add("{ReportingApiInegrationManager}", reporting);
                            else
                                map.Add("{ReportingApiInegrationManager}", "");

                            string transactional = @"
        public APIResponse Add{tableName}Transactional({tableName}TransactionalModel model)
        {
            var result = DataAccess.Add{tableName}Transactional(model);
            if (result > 0)
            {
		{addPrimaryKeyList}
		_rabitMQAsyncProducer.SendAsyncMessage(model, model.GetType().Name);
                return new APIResponse(ResponseCode.SUCCESS, ""Record Created"", result);
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, ""Record Not Created"");
            }
        }

        public APIResponse Update{tableName}Transactional({primaryKeyListParam}, {tableName}TransactionalModel model)
        {
			{updatePrimaryKeyList}
           
            var result = DataAccess.Update{tableName}Transactional(model);
            if (result)
            {
                Dictionary<string, int> primary_key = new Dictionary<string, int>();
                {primaryKeyListSingle}
 		_rabitMQAsyncProducer.SendAsyncMessage(model, primary_key, model.GetType().Name);
                return new APIResponse(ResponseCode.SUCCESS, ""Record Updated"");
            }
            else
            {
                return new APIResponse(ResponseCode.ERROR, ""Record Not Updated"");
            }
        }
                            ";
                            ProcessSingleFile(ref transactional, map);
                            if (Transactional.ContainsKey(tbl))
                                map.Add("{TransactionalApiInegrationManager}", transactional);
                            else
                                map.Add("{TransactionalApiInegrationManager}", "");
                            ProcessSingleFile(ref text, map);
                            string name = char.ToUpper(tbl[0]) + tbl.Substring(1) + "Manager.cs";
                            File.WriteAllText(@destination + name, text);
                            map.Clear();
                        }
                    }
                    else if (last == "model.txt")
                    {
                        string dest = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".Model/";
                        List<string> tables = GetTables(connection_string);
                        foreach (string tbl in tables)
                        {
                            string text = File.ReadAllText(file);
                            ProcessModel(ref map, connection_string, tbl, rabbitMQConn);
                            string name = char.ToUpper(tbl[0]) + tbl.Substring(1) + "Model.cs";
                            ProcessSingleFile(ref text, map);
                            File.WriteAllText(@dest + name, text);
                            //MySqlConnection conn = new MySqlConnection(connection_string);
                            //conn.Open();
                            MySqlConnection conn = MySqlConnectionManager.Instance.GetConnection();

                            map.Clear();
                        }
                    }
                    else if (last == "relationalmodel.txt")
                    {
                        string dest = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".Model/";
                        List<string> tables = GetTables(connection_string);
                        foreach (string tbl in tables)
                        {
                            if (tbl.ToLower() == "entities" || tbl.ToLower() == "permissionmatrix" || tbl.ToLower() == "users")
                            {
                                continue;
                            }
                            string text = File.ReadAllText(file);
                            ProcessModel(ref map, connection_string, tbl, rabbitMQConn);
                            string name = char.ToUpper(tbl[0]) + tbl.Substring(1) + "RelationalModel.cs";
                            StringBuilder foreignKeyPropertiesBuilder = new StringBuilder();

                            

                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                            {
                                Dictionary<string, List<ForeignKeyInfo>> keyValuePairs = ApiGeneratorFunctions.tablesWithForeignKeys;
                                List<ForeignKeyInfo> fetchedList = new List<ForeignKeyInfo>();
                                if (keyValuePairs.ContainsKey(tbl))
                                {
                                    fetchedList = keyValuePairs[tbl];
                                }
                                foreach (var foreignKeyInfo in fetchedList)
                                {
                                    string referencedTableName = char.ToUpper(foreignKeyInfo.ReferencedTableName[0]) + foreignKeyInfo.ReferencedTableName.Substring(1);
                                    string referencingColumnName = foreignKeyInfo.ColumnName;
                                    // Generate the property for the related entity
                                    string propertyLine = $"    public {referencedTableName}RelationalModel {referencingColumnName}_{referencedTableName} {{ get; set; }}";
                                        foreignKeyPropertiesBuilder.AppendLine(propertyLine);
       
                                }
                            }
                            map.Add("{ForeignKeyProperties}", foreignKeyPropertiesBuilder.ToString());

                            StringBuilder referencedByPropertiesBuilder = new StringBuilder();
                            if (ApiGeneratorFunctions.referencedBy.ContainsKey(tbl))
                            {
                                //foreach (var referencingTable in ApiGeneratorFunctions.referencedBy[tbl])
                                //{
                                //    string referencingTableTmp = char.ToUpper(referencingTable[0]) + referencingTable.Substring(1);
                                //    // Generate the property for the list of related entities
                                //    string propertyLine = $"    public List<{referencingTableTmp}RelationalModel> {referencingTableTmp} {{ get; set; }}";
                                //    referencedByPropertiesBuilder.AppendLine(propertyLine);
                                //}
                                Dictionary<string, List<ForeignKeyInfo>> keyValuePairs = ApiGeneratorFunctions.tablesWithForeignKeys;
                                List<ForeignKeyInfo> fetchedList = new List<ForeignKeyInfo>();
                                if (keyValuePairs.ContainsKey(tbl))
                                {
                                    fetchedList = keyValuePairs[tbl];
                                }
                                foreach (var foreignKeyInfo in fetchedList)
                                {
                                    string referencedTableName = char.ToUpper(foreignKeyInfo.ReferencedTableName[0]) + foreignKeyInfo.ReferencedTableName.Substring(1);
                                    string referencingColumnName = foreignKeyInfo.ColumnName;
                                    // Generate the property for the related entity
                                    string propertyLine = $"    public List<{referencedTableName}RelationalModel> {referencingColumnName}_{referencedTableName} {{ get; set; }}";
                                    foreignKeyPropertiesBuilder.AppendLine(propertyLine);

                                }
                            }
                            map.Add("{ReferencedByProperties}", referencedByPropertiesBuilder.ToString());
                            ProcessSingleFile(ref text, map);
                            File.WriteAllText(@dest + name, text);
                            //MySqlConnection conn = new MySqlConnection(connection_string);
                            //conn.Open();
                            MySqlConnection conn = MySqlConnectionManager.Instance.GetConnection();

                            map.Clear();
                        }
                    }
                    else if (last == "reportingmodel.txt")
                    {
                        string dest = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".Model/";
                        List<string> tables = GetTables(connection_string);
                        foreach (string tbl in tables)
                        {
                            if (tbl.ToLower() == "entities" || tbl.ToLower() == "permissionmatrix" || tbl.ToLower() == "users")
                            {
                                continue;
                            }
                            string text = File.ReadAllText(file);
                            ProcessModel(ref map, connection_string, tbl, rabbitMQConn);
                            string name = char.ToUpper(tbl[0]) + tbl.Substring(1) + "ReportingModel.cs";
                            StringBuilder foreignKeyPropertiesBuilder = new StringBuilder();




                            if (ApiGeneratorFunctions.tablesWithForeignKeys.ContainsKey(tbl))
                            {

                                Dictionary<string, List<ForeignKeyInfo>> keyValuePairs = ApiGeneratorFunctions.tablesWithForeignKeys;
                                List<ForeignKeyInfo> fetchedList = new List<ForeignKeyInfo>();
                                if (keyValuePairs.ContainsKey(tbl))
                                {
                                    fetchedList = keyValuePairs[tbl];
                                }
                                foreach (var foreignKeyInfo in fetchedList)
                                {

                                    HashSet<string> common_column = getForeignKey(tbl, connection_string);

                                    string referencedTableName = char.ToUpper(foreignKeyInfo.ReferencedTableName[0]) + foreignKeyInfo.ReferencedTableName.Substring(1);
                                    string referencingColumnName = foreignKeyInfo.ColumnName;
                                    Dictionary<string, string> reporting = new Dictionary<string, string>();
                                    string propertyLine = ProcessReportingModel(ref reporting, connection_string, referencedTableName, rabbitMQConn, common_column, referencingColumnName);
                                    // Generate the property for the related entity
                                    foreignKeyPropertiesBuilder.AppendLine(propertyLine);

                                }


                            }


                              
                                    
                                
                            
                            map.Add("{ForeignKeyProperties}", foreignKeyPropertiesBuilder.ToString());

                            ProcessSingleFile(ref text, map);
                            File.WriteAllText(@dest + name, text);
                            //MySqlConnection conn = new MySqlConnection(connection_string);
                            //conn.Open();
                            MySqlConnection conn = MySqlConnectionManager.Instance.GetConnection();

                            map.Clear();
                        }
                    }
                    else if (last == "transactionalmodel.txt")
                    {
                        string dest = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".Model/";
                        foreach (TransactionalParameterModel tp in transactionalModel)
                        {
                            string text = File.ReadAllText(file);
                            string parent = tp.ParentEntity.ToLower();
                            ProcessModel(ref map, connection_string, parent, rabbitMQConn);
                            string name = char.ToUpper(parent[0]) + parent.Substring(1) + "TransactionalModel.cs";
                            StringBuilder foreignKeyPropertiesBuilder = new StringBuilder();
                            foreach (string rm in tp.transactional.Sequence)
                            {
                                string rmsmall = rm.ToLower();
                                string rmupper = char.ToUpper(rmsmall[0]) + rmsmall.Substring(1);
                                if (tp.transactional.Relations[rm] == "one")
                                {
                                    string propertyLine = $"    public {rmupper}Model {rmupper} {{ get; set; }}";
                                    foreignKeyPropertiesBuilder.AppendLine(propertyLine);
                                }
                                else
                                {
                                    string propertyLine = $"    public List<{rmupper}Model> {rmupper} {{ get; set; }}";
                                    foreignKeyPropertiesBuilder.AppendLine(propertyLine);
                                }
                                // Generate the property for the related entity

                            }

                            map.Add("{ForeignKeyProperties}", foreignKeyPropertiesBuilder.ToString());

                            ProcessSingleFile(ref text, map);
                            File.WriteAllText(@dest + name, text);
                            //MySqlConnection conn = new MySqlConnection(connection_string);
                            //conn.Open();
                            MySqlConnection conn = MySqlConnectionManager.Instance.GetConnection();

                            map.Clear();
                        }
                    }
                    else if (last == "aggregatemodel.txt")
                    {
                        string dest = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".Model/";
                        foreach (ApiFlow tp in aggregateModel)
                        {
                            string text = File.ReadAllText(file);

                            string name = tp.DTO.Name + "Model.cs";
                            // Generate the property for the related entity
                            string dto_class = GenerateSql.GenerateDTO(tp);

                            map.Add("{DTOClass}", dto_class);

                            ProcessSingleFile(ref text, map);
                            File.WriteAllText(@dest + name, text);
                            //MySqlConnection conn = new MySqlConnection(connection_string);
                            //conn.Open();
                            MySqlConnection conn = MySqlConnectionManager.Instance.GetConnection();

                            map.Clear();
                        }
                    }
                    else if (last == "startup.txt")
                    {
                        ProcessStartup(ref map, connection_string, projectName, rabbitMQConn);
                        string destination = path + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" + projectName + ".API/", text = File.ReadAllText(file);
                        ProcessSingleFile(ref text, map);
                        File.WriteAllText(@destination + "/Startup.cs", text);
                        map.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessFiles: {ex.Message}");
            }
        }
        static void ProcessStartup(ref Dictionary<string, string> map, string connection_string, string projectName, string rabbitMQConn)
        {
            try
            {
                mapFill(ref map, connection_string, projectName, rabbitMQConn);
                string serviceDependency = "";
                ProcessServiceDependency(ref serviceDependency, InterfacesAndImpl);
                map.Add("{serviceDependency}", serviceDependency);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessStartup: {ex.Message}");
            }
        }
        static void ProcessServiceDependency(ref string serviceDependency, Dictionary<string, string> InterfacesAndImpl)
        {
            try
            {
                Console.WriteLine(InterfacesAndImpl.Count);
                if (isRbac)
                {
                    InterfacesAndImpl.Add("IEntitiesDataAccess", "EntitiesDataAccess");
                    InterfacesAndImpl.Add("IPermissionmatrixDataAccess", "PermissionmatrixDataAccess");
                    InterfacesAndImpl.Add("IUsersDataAccess", "UsersDataAccess");
                    InterfacesAndImpl.Add("IUsersManager", "UsersManager");
                    InterfacesAndImpl.Add("IEntitiesManager", "EntitiesManager");
                    InterfacesAndImpl.Add("IPermissionmatrixManager", "PermissionmatrixManager");
                }
                if (aggregateModel.Count > 0)
                {
                    InterfacesAndImpl.Add("IAggregateManager", "AggregateManager");
                    InterfacesAndImpl.Add("IAggregateDataAccess", "AggregateDataAccess");
                }
                foreach (KeyValuePair<string, string> pair in InterfacesAndImpl)
                {
                    serviceDependency += "services.AddTransient<";
                    serviceDependency += pair.Key;
                    serviceDependency += ",";
                    serviceDependency += pair.Value;
                    serviceDependency += ">();";
                    serviceDependency += "\n";
                }
                if (serviceDependency.Length > 0)
                    serviceDependency = serviceDependency.Remove(serviceDependency.Length - 1);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessServiceDependency: {ex.Message}");
            }

        }
        public static string getType(string str)
        {
            try
            {
                string t = str.ToLower();
                if (t.Contains("smallint"))
                    return "short";
                else if (t.Contains("date"))
                    return "DateTime";
                else if (t.Contains("bigint"))
                    return "long";
                else if (t.Contains("int") || t.Contains("year"))
                    return "int";
                else if (t.Contains("float"))
                    return "float";
                else if (t.Contains("decimal"))
                    return "decimal";
                else if (t.Contains("double") || t.Contains("real"))
                    return "double";
                else if (t.Contains("char") || t.Contains("text") || t.Contains("var"))
                    return "string";
                else if (t.Contains("bit"))
                    return "SByte";
                else if (t.Contains("bool"))
                    return "Boolean";
                else
                    return "string";
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in GetType: {ex.Message}");
                return "string"; // Return a default value or handle as per your application's logic
            }
        }

        internal static Dictionary<string, string> getAllInfo(ref string text, string tbl, string connectionString, string PK)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            //using var conn = new MySqlConnection(connectionString);
            //conn.Open();
            try
            {
                //List<string> tblColumns = getAllColumns(tbl, "");
                //bool isCreatedByExist = false;
                //bool isModifiedByExist = false;
                //bool isCreatedAtExist = false;
                //bool isModifiedAtExist = false;
                //foreach (string columnname in tblColumns)
                //{
                //    if (columnname.Equals("createdBy"))
                //    {
                //        isCreatedByExist = true;
                //    }
                //    if (columnname.Equals("modifiedBy"))
                //    {
                //        isModifiedByExist = true;
                //    }

                //}
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                DataTable columns = connection.GetSchema("Columns", new[] { null, null, tbl });
                foreach (DataRow column in columns.Rows)
                {
                    dict.Add("Column_Name", column["COLUMN_NAME"].ToString());
                    dict.Add("Nullable", column["IS_NULLABLE"].ToString());
                    if (column["DATA_TYPE"].ToString().ToLower().Contains("date") || column["DATA_TYPE"].ToString().ToLower().Contains("time"))
                        dict.Add("DataType", "varchar");
                    else
                        dict.Add("DataType", column["DATA_TYPE"].ToString().ToLower());
                    if (dict["Nullable"] == "NO" && dict["Column_Name"] != PK && getType(dict["DataType"]) == "int")
                        text += "[Range(int.MinValue,int.MaxValue)]\n";
                    if (dict["Nullable"] == "NO" && dict["Column_Name"] != PK && getType(dict["DataType"]) != "int")
                    {
                        //if()
                        // :- add [Required] only if the column name is not createdBy,modifiedBy,createdAt,modifiedAt
                        if (dict["Column_Name"] != "createdBy" && dict["Column_Name"] != "createdAt" && dict["Column_Name"] != "modifiedAt" && dict["Column_Name"] != "modifiedBy") 
                            text += "[Required]\n";
                    }
                        
                    text += "public ";
                    text += getType(dict["DataType"]);
                    if (dict["Nullable"] == "YES" && !getType(dict["DataType"]).Contains("string"))
                        text += "?";
                    text += " " + dict["Column_Name"] + "{get; set;}\n";
                    dict.Clear();
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in GetAllInfo: {ex.Message}");
            }
            return dict;
        }

        internal static Dictionary<string, string> getAllInfoReporting(ref string text, string tbl, string connectionString, HashSet<string> commoncolumn,string referencingColumnName)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            //using var conn = new MySqlConnection(connectionString);
            //conn.Open();
            try
            {
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                DataTable columns = connection.GetSchema("Columns", new[] { null, null, tbl });
                foreach (DataRow column in columns.Rows)
                {
                    //if (commoncolumn.Contains(column["COLUMN_NAME"].ToString()))
                    //    continue;
                    dict.Add("Column_Name", column["COLUMN_NAME"].ToString());
                    dict.Add("Nullable", column["IS_NULLABLE"].ToString());
                    if (column["DATA_TYPE"].ToString().ToLower().Contains("date") || column["DATA_TYPE"].ToString().ToLower().Contains("time"))
                        dict.Add("DataType", "varchar");
                    else
                        dict.Add("DataType", column["DATA_TYPE"].ToString().ToLower());
                    if (dict["Nullable"] == "NO" && !commoncolumn.Contains(column["COLUMN_NAME"].ToString()) && getType(dict["DataType"]) == "int")
                        text += "[Range(int.MinValue,int.MaxValue)]\n";
                    if (dict["Nullable"] == "NO" && !commoncolumn.Contains(column["COLUMN_NAME"].ToString()) && getType(dict["DataType"]) != "int")
                        text += "[Required]\n";
                    text += "public ";
                    text += getType(dict["DataType"]);
                    if (dict["Nullable"] == "YES" && !getType(dict["DataType"]).Contains("string"))
                        text += "?";
                    if (dict["Column_Name"] == "isActive" || dict["Column_Name"] == "createdBy" || dict["Column_Name"] == "modifiedBy" || dict["Column_Name"] == "createdAt" || dict["Column_Name"] == "modifiedAt")
                        text += " " + referencingColumnName + "_" + tbl + "_" + dict["Column_Name"] + "{get; set;}\n";
                    else
                        text += " "+referencingColumnName+"_" + tbl + "_" + dict["Column_Name"] + "{get; set;}\n";
                    dict.Clear();
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in GetAllInfo: {ex.Message}");
            }
            return dict;
        }

        internal static void getColsMetadata(ref string selectAllModelInit, string tbl, string connectionString, ref HashSet<string> set,string referencingColumnName)
        {
            try
            {
                //using var conn = new MySqlConnection(connectionString);
                //conn.Open();
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                // Get the metadata for the "users" table
                DataTable columns = connection.GetSchema("Columns", new[] { null, null, tbl });
                Dictionary<string, string> dict = new Dictionary<string, string>
            {
                { "int","Int32" },
                {"varchar","String" },
                { "longtext","String" },
                {"datetime","datetime" },
                {"date","datetime" },
                {"decimal","Decimal"},
                {"bigint","Int64"},
                {"double","double"},
                {"smallint","short"},
                {"tinyint","SByte"},
                {"boolean","SByte" },
                {"timestamp","datetime"},
                {"time","datetime"},
                {"year","Int32"},
                {"char","String"},
                {"text","String"},
                {"enum","String" },
                {"mediumint","Int32" },
                {"float","Float" },
                {"binary","byte[]" },
                {"varbinary","byte[]" },
                {"blob","byte[]" },
                {"tinyblob","byte[]" },
                {"mediumblob","byte[]" },
                {"longblob","byte[]" },
                {"set","String" }
            };
                // Loop through the columns and print their metadata
                foreach (DataRow column in columns.Rows)
                {
                        string  Column_Name_Left = "";
                    if (referencingColumnName != "")
                    {
                        Column_Name_Left = referencingColumnName + "_" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "_" + column["COLUMN_NAME"].ToString();
                    }
                    else
                    {
                        Column_Name_Left = column["COLUMN_NAME"].ToString();

                    }
                    if (column["IS_NULLABLE"].ToString() == "YES" && column["DATA_TYPE"].ToString() != "varchar")
                    {
                        string data = utility.getType(column["DATA_TYPE"].ToString());
                        if (data.Contains("Date"))
                            selectAllModelInit += Column_Name_Left + "= " + "reader.IsDBNull(Helper.GetColumnOrder(reader," + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")) ? " + "(" + "String" + "?)null : reader.Get" + data + "(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ").ToString()";
                        else if (data.Contains("float"))
                            selectAllModelInit += Column_Name_Left + "= " + "reader.IsDBNull(Helper.GetColumnOrder(reader," + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")) ? " + "(" + dict[column["DATA_TYPE"].ToString()].ToLower() + "?)null : reader.Get" + dict[column["DATA_TYPE"].ToString()] + "(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        else
                            selectAllModelInit += Column_Name_Left + "= " + "reader.IsDBNull(Helper.GetColumnOrder(reader," + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")) ? " + "(" + dict[column["DATA_TYPE"].ToString()] + "?)null : reader.Get" + dict[column["DATA_TYPE"].ToString()] + "(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        if (column.ItemArray.Length != columns.Rows.Count - 1)
                            selectAllModelInit += ",\n";
                    }
                    else
                    {
                        string data = utility.getType(column["DATA_TYPE"].ToString());
                        if (data.Contains("Date"))
                            selectAllModelInit += Column_Name_Left + "= " + "reader.GetValue<" + data + ">(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ").ToString()";
                        else if (data.Contains("float"))
                            selectAllModelInit += Column_Name_Left + "= " + "reader.GetValue<" + dict[column["DATA_TYPE"].ToString()].ToLower() + ">(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        else
                            selectAllModelInit += Column_Name_Left + "= " + "reader.GetValue<" + dict[column["DATA_TYPE"].ToString()] + ">(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        if (column.ItemArray.Length != columns.Rows.Count - 1)
                            selectAllModelInit += ",\n";
                    }


                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in GetColsMetadata: {ex.Message}");
            }

        }

        internal static void ProcessSingleFile(ref string text, Dictionary<string, string> map)
        {
            try
            {
                foreach (KeyValuePair<string, string> i in map)
                {
                    text = text.Replace(i.Key, i.Value);
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                errors_list.Add($"Error in ProcessSingleFile: {ex.Message}");
            }
        }
        public string getConnectionString(string userid, string root, string server, string port, string password, string databaseName)
        {
            string connectionString = $"server={server};uid={userid};username={root};password={password};database={databaseName};Convert Zero Datetime=true;Treat Tiny As Boolean=False";
            return connectionString;
        }
        public static void Main(string[] args)
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
           .Build();
     
            string parameter = Environment.GetEnvironmentVariable("PARAMETER");
            Console.WriteLine("parameter:" + parameter);
            // Define a regular expression pattern to match key-value pairs
            string pattern = @"\b(\w+)\s*:\s*(.*?)(?=,\w+\s*:|$)";
            MatchCollection matches = Regex.Matches(parameter, pattern);

            // Create a dictionary to store the key-value pairs
            var keyValueDict = new Dictionary<string, string>();
            foreach (Match match in matches)
            {
                string key = match.Groups[1].Value;
                string value = match.Groups[2].Value.Trim('\'', '\"');
                if (value == "null" || value == "null}")
                {
                    value = null;
                }

                keyValueDict[key] = value;
            }

          string project_id = keyValueDict["project_id"];
            string server = keyValueDict["server"];
            string uid = keyValueDict["uid"];
            string username = keyValueDict["username"];
            string password = keyValueDict["password"];
            databaseName = keyValueDict["databaseName"];
            string script = keyValueDict["script"];
            string statusOfGeneration = keyValueDict["statusOfGeneration"];
            string projectName = keyValueDict["projectName"];
            string DBexists = keyValueDict["DBexists"];
            string port = keyValueDict["port"];
            string rabbitMQConn = keyValueDict["rabbitMQConn"];
            redisConn = keyValueDict["redisConn"];
            string apiflowurl = null;
            if (keyValueDict.ContainsKey("apiflowurl"))
            {
                //Kaival - apiflow url is coming as always "" - no matter chatbot used or not. So checking for whether its blank if its not used chatbot then it will be blank else it will have value.
                apiflowurl = keyValueDict["apiflowurl"];
                if (string.IsNullOrEmpty(apiflowurl))
                {
                    apiflowurl = null;
                }
            }
            string front_template_url = null;
            if (keyValueDict.ContainsKey("fronttemplateurl"))
            {
                Console.WriteLine("Frontend Template URL : " + keyValueDict["fronttemplateurl"]);
                front_template_url = keyValueDict["fronttemplateurl"];
                if (string.IsNullOrEmpty(front_template_url))
                {
                    front_template_url = null;
                }
                Console.WriteLine("Frontend Template URL : " + front_template_url);
            }
            string Technology_Frontend = keyValueDict["Technology_Frontend"];
            string Baackend_technology = keyValueDict["Backend_technology"];
            string buttonClicked = keyValueDict["buttonClicked"];
            string projectType = keyValueDict["projectType"];
            string swgurl = keyValueDict["swgurl"];
            string noderedurl = keyValueDict["noderedurl"];


            if (noderedurl != null)
            {
                noderedurl = noderedurl.Replace("}", "");
            }
            // ... and so on
            if (rabbitMQConn.Contains("amqp"))
            {

                // Find the starting index of the username
                int start = rabbitMQConn.IndexOf("user") + "user".Length;

                // Find the ending index of the username (before the ":")
                int end = rabbitMQConn.IndexOf(":", start);

                // Extract the username from the input string
                string user = rabbitMQConn.Substring(start, end - start);

                // Replace "user" with "password" in the input string
                string modifiedString = "password" + user;

                Console.WriteLine("Original String: " + rabbitMQConn);
                Console.WriteLine("Username: " + username);
                Console.WriteLine("Modified String: " + modifiedString);
                string abc = rabbitMQConn.Replace("***", modifiedString);
                rabbitMQConn = abc;
                Console.WriteLine(rabbitMQConn);
            }

            Console.WriteLine("project_id: " + project_id);
            Console.WriteLine("server: " + server);
            Console.WriteLine("uid: " + uid);
            Console.WriteLine("username: " + username);
            Console.WriteLine("password: " + password);
            Console.WriteLine("databaseName: " + databaseName);
            Console.WriteLine("script: " + script);
            Console.WriteLine("statusOfGeneration: " + statusOfGeneration);
            Console.WriteLine("projectName: " + projectName);
            Console.WriteLine("DBexists: " + DBexists);
            Console.WriteLine("port: " + port);
            Console.WriteLine("rabbitMQConn: " + rabbitMQConn);
            Console.WriteLine("redisConn: " + redisConn);
            Console.WriteLine("apiflowurl: " + apiflowurl);
            Console.WriteLine("Technology_Frontend: " + Technology_Frontend);
            Console.WriteLine("Backend_technology: " + Baackend_technology);
            Console.WriteLine("buttonClicked: " + buttonClicked);
            Console.WriteLine("projectType: " + projectType);
            Console.WriteLine("swgurl: " + swgurl);
            { }
            Console.WriteLine("noderedurl: " + noderedurl);
            /* Console.WriteLine("transactionalapi: " + transactionalapi);*/

            /*List<TransactionalParameterModel> tp = JsonConvert.DeserializeObject<List<TransactionalParameterModel>>(transactionalapi);*/

            if (buttonClicked.ToLower() == "generate")
            {
                try
                {
                    string conn_string = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";TreatTinyAsBoolean=false;";
                    MySqlConnectionManager.Initialize(conn_string);
                    if (apiflowurl != null)
                    {
                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(apiflowurl, "./apiflowjson.txt");
                        }
                        string apiflowjson = File.ReadAllText("./apiflowjson.txt");
                        APIFlowJson apiflow = JsonConvert.DeserializeObject<APIFlowJson>(apiflowjson);
                        transactionalModel = apiflow.transactionalAPI;
                        aggregateModel = apiflow.aggregationAPI;
                    }
                    string upath = "";
                    if (Technology_Frontend != null && Technology_Frontend.ToLower() == "reactts")
                    {
                        if (projectType == "dnd")
                        {
                            ReactTsProject.ReactTs(server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, rabbitMQConn, Technology_Frontend, Baackend_technology, projectType,front_template_url);
                            string[] drpath = Directory.GetDirectories(@"../", (uid + "_*"));
                            string fname = drpath[0].Split("/").Last();
                            var files = Directory.GetFiles($"../{fname}/{projectName}/ReactTs_Output1", "*.env*");
                            foreach (string i in files)
                            {
                                string text = File.ReadAllText(i);
                                text = text.Replace(@"REACT_APP_API_BASE_URL=http://localhost:82/myeshop", $"REACT_APP_API_BASE_URL={rabbitMQConn}");
                                string nodered_url = rabbitMQConn.Replace("/backend/v1/api", "/nodered");
                                text = text.Replace(@"REACT_APP_NODERED_BASE_URL=http://localhost:82/myeshop", $"REACT_APP_NODERED_BASE_URL={nodered_url}");
                                File.WriteAllText(i, text);
                            }
                            try
                            {
                                string[] dlf = Directory.GetFiles(drpath[0] + "/" + projectName + "/zip/", "*_generatedFrontend_dnd_*");
                                if (dlf.Length != 0)
                                    File.Delete(dlf[0]);
                                upath = drpath[0] + "/" + projectName + "/zip/" + uid + "_" + projectName + "_generatedFrontend_dnd_" + DateTime.Now.Ticks + ".zip";
                                ZipFile.CreateFromDirectory(drpath[0] + "/" + projectName + "/ReactTs_Output1", upath);

                            }
                            catch (Exception e)
                            {
                                Console.Write("Error!! While Generating Zip!");
                                errors_list.Add("Error while generating zip");
                            }
                        }
                        else if (projectType == "workflow")
                        {
                            string front_templateURL = "";
                            var textt = File.ReadAllText("./ReactTsTemplate2/ReactTsProject/src/components/addWorkflow/index.tsx");
                            textt = textt.Replace("{rabbitMqConn}", rabbitMQConn);
                            File.WriteAllText("./ReactTsTemplate2/ReactTsProject/src/components/addWorkflow/index.tsx", textt);
                            ReactTsProject.ReactTs(server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, rabbitMQConn, Technology_Frontend, Baackend_technology, projectType, front_templateURL);
                            string[] drpath = Directory.GetDirectories(@"../", (uid + "_*"));
                            string fname = drpath[0].Split("/").Last();
                            var files = Directory.GetFiles($"../{fname}/{projectName}/ReactTs_Output2", "*.env*");
                            foreach (string i in files)
                            {
                                string text = File.ReadAllText(i);
                                text = text.Replace(@"REACT_APP_API_BASE_URL=http://localhost:82/myeshop", $"REACT_APP_API_BASE_URL={rabbitMQConn}");

                                File.WriteAllText(i, text);
                            }
                            try
                            {
                                string[] dlf = Directory.GetFiles(drpath[0] + "/" + projectName + "/zip/", "*_generatedFrontend_workflow_*");
                                if (dlf.Length != 0)
                                    File.Delete(dlf[0]);
                                upath = drpath[0] + "/" + projectName + "/zip/" + uid + "_" + projectName + "_generatedFrontend_workflow_" + DateTime.Now.Ticks + ".zip";
                                ZipFile.CreateFromDirectory(drpath[0] + "/" + projectName + "/ReactTs_Output2", upath);

                            }
                            catch (Exception e)
                            {

                                errors_list.Add("Error while generating zip");
                                Console.Write("Error!! While Generating Zip!");
                            }
                        }
                        else if (projectType == "nodered")

                        {
                            string front_templateURL = "";
                            ReactTsProject.ReactTs(server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, rabbitMQConn, Technology_Frontend, Baackend_technology, projectType, front_templateURL);
                            string[] drpath = Directory.GetDirectories(@"../", (uid + "_*"));
                            string fname = drpath[0].Split("/").Last();
                            try
                            {
                                string[] dlf = Directory.GetFiles(drpath[0] + "/" + projectName + "/zip/", "*_generatedFrontend_nodered_*");
                                if (dlf.Length != 0)
                                    File.Delete(dlf[0]);
                                upath = drpath[0] + "/" + projectName + "/zip/" + uid + "_" + projectName + "_generatedFrontend_nodered_" + DateTime.Now.Ticks + ".zip";
                                ZipFile.CreateFromDirectory(drpath[0] + "/" + projectName + "/ReactTs_Output4", upath);

                            }
                            catch (Exception e)
                            {

                                errors_list.Add("Error while generating zip");
                                Console.Write("Error!! While Generating Zip!");
                            }
                        }
                        else
                        {
                            string front_templateURL = "";
                            projectType = "frontend";
                            ReactTsProject.ReactTs(server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, rabbitMQConn, Technology_Frontend, Baackend_technology, projectType, front_templateURL);
                            string[] drpath = Directory.GetDirectories(@"../", (uid + "_*"));
                            string fname = drpath[0].Split("/").Last();
                            var files = Directory.GetFiles($"../{fname}/{projectName}/ReactTs_Output3", "*.env*");
                            foreach (string i in files)
                            {
                                string text = File.ReadAllText(i);
                                text = text.Replace(@"REACT_APP_API_BASE_URL=http://localhost:82/myeshop", $"REACT_APP_API_BASE_URL={rabbitMQConn}");

                                File.WriteAllText(i, text);
                            }
                            try
                            {
                                string[] dlf = Directory.GetFiles(drpath[0] + "/" + projectName + "/zip/", "*_generatedFrontend_app_*");
                                if (dlf.Length != 0)
                                    File.Delete(dlf[0]);
                                upath = drpath[0] + "/" + projectName + "/zip/" + uid + "_" + projectName + "_generatedFrontend_app_" + DateTime.Now.Ticks + ".zip";
                                ZipFile.CreateFromDirectory(drpath[0] + "/" + projectName + "/ReactTs_Output3", upath);
                            }
                            catch (Exception e)
                            {

                                errors_list.Add("Error while generating zip");
                                Console.Write("Error!! While Generating Zip!");
                            }
                        }
                    }
                    if (Baackend_technology != null && Baackend_technology.ToLower() == "dotnet")
                    {
                        projectType = "backend_and_consumer";
                        var kubectlCommand = "tool install --global Swashbuckle.AspNetCore.Cli --version 6.5.0";
                        var kubectlProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "dotnet",
                                Arguments = $"{kubectlCommand}",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                            }
                        };
                        kubectlProcess.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                        //kubectlProcess.ErrorDataReceived += (sender, e) => Console.WriteLine("Error!!"+e.Data);
                        kubectlProcess.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data != null)
                            {
                                Console.WriteLine("Error!!" + e.Data);
                                errors_list.Add("Error!!" + e.Data);
                            }
                        };
                        kubectlProcess.Start();
                        kubectlProcess.WaitForExit();
                        kubectlProcess.BeginOutputReadLine();
                        kubectlProcess.BeginErrorReadLine();
                        string adminUsername = GenerateUsername();
                        string adminPassword = GeneratePassword();
                        DotNet_MySQL.DotNet_MySQL_Template(adminUsername, adminPassword,server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, rabbitMQConn, Technology_Frontend, Baackend_technology, projectType, noderedurl, swgurl,project_id);
                        string[] drpath = Directory.GetDirectories("/", (uid + "_*"));
                        string fname = drpath[0].Split("/").Last();
                        string[] upt = Directory.GetFiles($"/{fname}/{projectName}/zip/", "generatedBackend*.zip");
                        upath = upt[0];
                        //Thread.Sleep(500000);



                    }
                    IConfiguration configuration1 = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json")
               .Build();

                    // Pass the configuration to other classes as needed
                    var getters = new Getters(configuration1);
                    string link = null;
                    link = getters.uploadToS3(uid, "150".ToString(), "23".ToString(), "22441".ToString(), upath)[0];

                    if (link != null && errors_list.Count == 0)
                    {
                        GeneratorResponseModel generatorResponseModel = new GeneratorResponseModel()
                        {
                            project_id = Convert.ToInt32(project_id),
                            classifier = projectType,
                            link = link,
                            is_successful = 1
                        };
                       
                    }
                    else
                    {
                        GeneratorResponseModel generatorResponseModel = new GeneratorResponseModel()
                        {
                            project_id = Convert.ToInt32(project_id),
                            classifier = projectType,
                            link = "",
                            error_log = errors_list,
                            is_successful = 0
                        };
                       
                    }
                }
                catch (Exception ex)
                {
                    // If an exception occurs, catch it and add to the errors_list
                    errors_list.Add($"Error in generating process: {ex.Message}");
                    GeneratorResponseModel generatorResponseModel = new GeneratorResponseModel()
                    {
                        project_id = Convert.ToInt32(project_id),
                        classifier = projectType,
                        link = "",
                        error_log = errors_list,
                        is_successful = 0
                    };
                   
                }

            }
            /*else if (buttonClicked.ToLower() == "redeploy")
            {
                if (Baackend_technology != null)
                {
                    //    var textt = File.ReadAllText("./deploy.yaml");
                    //    textt = textt.Replace("{arg}", rabbitMqArg);
                    //    textt = textt.Replace("{rabbitMqUserName}", rabbitMqUserName);
                    //    textt = textt.Replace("{rabbitMqPassword}", rabbitMqPassword);
                    //    textt = textt.Replace("{managementNodePort}", managementNodePort);
                    //    textt = textt.Replace("{amqpNodePort}", amqpNodePort);
                    //    File.WriteAllText("./deploy.yaml", textt);
                    //var textt = File.ReadAllText("./deploy_consumer.yaml");
                    //textt = textt.Replace("{arg}", rabbitMqArg);
                    //textt = textt.Replace("{arg1}", imageName + "-consumer");
                    //textt = textt.Replace("{arg2}", imageTag);
                    //textt = textt.Replace("{arg3}", imagePullSecretName);
                    //textt = textt.Replace("{projectName}", projectName);
                    //File.WriteAllText("./deploy_consumer.yaml", textt);
                    //    var kubectlCommand = "--kubeconfig config.yaml apply -f deploy.yaml";
                    //    var kubectlProcess = new Process
                    //    {
                    //        StartInfo = new ProcessStartInfo
                    //        {
                    //            FileName = "/app/bin/kubectl",
                    //            Arguments = $"{kubectlCommand}",
                    //            UseShellExecute = false,
                    //            RedirectStandardOutput = true,
                    //            RedirectStandardError = true,
                    //        }
                    //    };
                    //    kubectlProcess.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                    //    //kubectlProcess.ErrorDataReceived += (sender, e) => Console.WriteLine("Error!!" + e.Data);
                    //    kubectlProcess.ErrorDataReceived += (sender, e) =>
                    //    {
                    //        if (e.Data != null)
                    //            Console.WriteLine("Error!!" + e.Data);
                    //    };
                    //    kubectlProcess.Start();
                    //    kubectlProcess.WaitForExit();
                    //    kubectlProcess.BeginOutputReadLine();
                    //    kubectlProcess.BeginErrorReadLine();
                    var kubectlCommand = $"--kubeconfig config.yaml rollout restart deployment/{rabbitMqArg}-consumer-deployment";
                    Console.WriteLine(kubectlCommand);
                    var kubectlProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "/app/bin/kubectl",
                            Arguments = $"{kubectlCommand}",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                        }
                    };
                    kubectlProcess.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                    //kubectlProcess.ErrorDataReceived += (sender, e) => Console.WriteLine("Error!!" + e.Data);
                    kubectlProcess.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null)
                            Console.WriteLine("Error!!" + e.Data);
                    };
                    kubectlProcess.Start();
                    kubectlProcess.WaitForExit();
                    kubectlProcess.BeginOutputReadLine();
                    kubectlProcess.BeginErrorReadLine();
                }
                var chartDirectory = "./";
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "/app/bin/helm",
                    Arguments = $"create {chartName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                var process = new Process
                {
                    StartInfo = processStartInfo
                };
                process.Start();
                process.WaitForExit();
                var exitCode = process.ExitCode;

                if (exitCode == 0)
                {
                    Console.WriteLine($"Helm chart created at {chartDirectory}");
                    Directory.Delete(@$"{chartName}/templates/tests", true);
                    var files = Directory.GetFiles($@"{chartName}/templates");
                    foreach (string file in files)
                    {
                        var name = file.Split("/").Last();
                        if (name == "hpa.yaml" || name == "serviceaccount.yaml" || name == "NOTES.txt")
                            File.Delete(file);
                        else if (name == "deployment.yaml")
                        {
                            string[] text = File.ReadAllLines(file);
                            string[] output = new string[39];
                            for (int i = 0; i < 39; i++)
                            {
                                if (i == 26)
                                    continue;
                                output[i] = text[i];
                            }
                            File.WriteAllLines(file, output);
                        }
                        else if (name == "ingress.yaml")
                        {
                            string[] text = File.ReadAllLines(file);
                            string[] output = new string[text.Length];
                            for (int i = 0, j = 0; i < text.Length; i++)
                            {
                                if (i == 40)
                                {
                                    string temp = text[i];
                                    temp = temp.Replace("host: {{ .host | quote }}", "http:");
                                    output[j++] = temp;
                                    continue;
                                }
                                if (i == 41)
                                {
                                    continue;
                                }
                                output[j++] = text[i];
                            }
                            File.WriteAllLines(file, output);
                        }
                    }
                    try
                    {
                        var command = $"upgrade --kubeconfig config.yaml {chartName} {chartName} --set image.repository={imageName},image.tag={imageTag},service.port=80,imagePullSecrets[0].name={imagePullSecretName},serviceAccount.create=false,ingress.enabled=true,ingress.annotations.\"nginx\\.ingress\\.kubernetes\\.io/rewrite-target\"=\"{extraArg}/\\$2\",ingress.hosts[0].paths[0].path=\"{extraArg}(/|$)(.*)\",ingress.hosts[0].paths[0].pathType=Prefix,ingress.className=nginx";
                        //command = "push registry.digitalocean.com/nocodedevprototype/backend:latest";
                        Console.WriteLine(command);
                        var installHelmChart = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "/app/bin/helm",
                                Arguments = $"{command}",
                                UseShellExecute = false,
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                            }
                        };
                        installHelmChart.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                        //installHelmChart.ErrorDataReceived += (sender, e) => Console.WriteLine("Error!!" + e.Data);
                        installHelmChart.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data != null)
                                Console.WriteLine(e.Data);
                        };
                        installHelmChart.Start();
                        installHelmChart.WaitForExit();
                        installHelmChart.BeginOutputReadLine();
                        installHelmChart.BeginErrorReadLine();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }*/
            else
            {
                string[] drpath = Directory.GetDirectories(@"../", (uid + "_*"));
                if (drpath.Length != 0)
                {
                    string fname = drpath[0].Split("/").Last();
                    try
                    {
                        Directory.SetCurrentDirectory(Directory.GetCurrentDirectory() + @"/../" + fname + "/" + projectName);
                        var dct = Directory.GetDirectories("./", "*");
                        foreach (string file in dct)
                        {
                            Directory.Delete(file, true);
                        }
                        //Directory.Delete("./ReactTs_Output", true);
                        //Directory.Delete("./DotNet_Output", true);
                        //var files = Directory.GetFiles("./zip", "*.zip");
                        //foreach (string file in files)
                        //{
                        //    File.Delete(file);
                        //}
                        Console.Write("cleanup successfull!");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error!! Occured:" + e.Message);
                    }
                }
            }
        }
    }
    class DotNet_MySQL : Program
    {
        public static void DotNet_MySQL_Template(string adminUsername, string adminPassword, string server, string uid, string username, string password, string databaseName, string script, string statusOfGeneration, string projectName, string DBexists, string port, string rabbitMQConn, string frontEndTechnology, string backendTechnology, string projectType, string noderedurl, string swgurl,string project_id)
        {
            try
            {
                string currentDir = Directory.GetCurrentDirectory();
                DirectoryWork.MakeDirs(currentDir, projectName, uid);
                string[] drpath = Directory.GetDirectories(@"../", (uid + "_*"));
                string fname = drpath[0].Split("/").Last();
                Renaming rr = new Renaming(projectName);
                rr.RenameDirs(fname, projectName);
                string[] tempd = Directory.GetFiles(@currentDir + $"/../{fname}/{projectName}/zip/", $"generatedBackend*.zip");
                foreach (string temp in tempd)
                    File.Delete(temp);
                string ZipDes = @currentDir + $"/../{fname}/{projectName}/zip/generatedBackend{DateTime.Now.Ticks}.zip";
                Console.WriteLine(ZipDes);
                string newDir = Path.Combine(currentDir);
                Directory.SetCurrentDirectory(newDir);
                currentDir = Directory.GetCurrentDirectory();
                var list = currentDir.Split("/");
                string last = list[list.Length - 1];
                // Console.WriteLine(currentDir);
                string new_dir3 = currentDir, new_dir5 = currentDir;
                string connectionSstring1 = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();


                //INSERT INTO `s3bucket_folders` (`folder_id`, `folder_name`,`modifiedBy`, `createdBy`) VALUES (1,'default','{adminUsername}','{adminUsername}');
                string createTableSql1 = $@"CREATE TABLE IF NOT EXISTS `dnd_ui_versions` (
  `dnd_ui_version_id` int NOT NULL AUTO_INCREMENT,
  `layout` longtext NOT NULL,
  `components` longtext NOT NULL,
  `ui_pages` longtext NOT NULL,
  `dnd_ui_type` varchar(45) NOT NULL,
  `createdBy` varchar(45) NOT NULL,
  `modifiedBy` varchar(45) NOT NULL,
   `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `isActive` tinyint(1) NOT NULL,
  PRIMARY KEY(`dnd_ui_version_id`)
);
CREATE TABLE IF NOT EXISTS `s3bucket` (
  `bucket_id` int NOT NULL AUTO_INCREMENT,
  `bucket_name` varchar(255) NOT NULL,
  `bucket_url` varchar(500) NOT NULL,
  `modifiedBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `createdBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
   `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY(`bucket_id`)
);
INSERT INTO `s3bucket` (`bucket_name`, `bucket_url`,`modifiedBy`, `createdBy`,`isActive`) VALUES ('nocodes3dev','DO0089PZREULYLFACQZJ','{adminUsername}','{adminUsername}','1');
CREATE TABLE IF NOT EXISTS `s3bucket_folders` (
  `folder_id` int NOT NULL AUTO_INCREMENT,
  `folder_name` varchar(45) NOT NULL,
  `modifiedBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `createdBy` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
  `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`folder_id`)
);

CREATE TABLE IF NOT EXISTS `project_dnd_ui_versions` (
  `project_dnd_ui_version_id` int NOT NULL AUTO_INCREMENT,
  `project_id` int NOT NULL,
  `dnd_ui_version_id` int NOT NULL,
  `createdBy` varchar(255) NOT NULL,
  `modifiedBy` varchar(255) NOT NULL,
  `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
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
  `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
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
  `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
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
  `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
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
  `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
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
 `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
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
  `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
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
  `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
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
  `modifiedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `createdAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `isActive` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`workflow_trigger_id`),
  CONSTRAINT `workflow_trigger_conditions_ibfk_1` FOREIGN KEY (`workflow_trigger_id`) REFERENCES `workflow_triggers` (`workflow_trigger_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;
";
                Console.WriteLine(connectionSstring1);
                //using (MySqlConnection connection = new MySqlConnection(connectionSstring1))
                //{
                //    connection.Open();
                //    if (connection.State == System.Data.ConnectionState.Open)
                //    {
                //        Console.WriteLine("First Connection is opened in DotNet_MySQL_Template()");
                //    }
                //MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand command = new MySqlCommand(createTableSql1, connection))
                {
                    Console.WriteLine("Executing");
                    command.ExecuteNonQuery();
                }
                //    connection.Close();
                //    if (connection.State == System.Data.ConnectionState.Closed)
                //    {
                //        Console.WriteLine("First Connection is closed in DotNet_MySQL_Template()");
                //    }
                //}
                ProcessFiles( adminUsername,  adminPassword, @currentDir, server, uid, username, password, databaseName,
                             script, statusOfGeneration, projectName, DBexists, port,
                             rabbitMQConn, noderedurl, swgurl,project_id);

                // ProcessFiles(@currentDir, "localhost", "", "root", "", "myeshopAPI",
                // "http://localhost/sqlScript/myeshop.sql", "", "myeshopAPI", "YES",
                // "3306");
                string new_dir = Path.GetFullPath(Path.Combine(currentDir, $"../{fname}/{projectName}/DotNet_Output/solution")); ;
                string new_dir1 = Path.GetFullPath(Path.Combine(currentDir, $"../{fname}/{projectName}/DotNet_Output/"));
                string zipSource = new_dir1;
                string new_dir4 = currentDir;
                string new_dir2 =
                    Path.Combine(currentDir, $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" +
                                                 projectName + ".sln");
                newDir = Path.Combine(currentDir, $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName +
                                                  "/" + projectName + ".Utility");
                string temp_path =
                    Path.Combine(currentDir, $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" +
                                                 projectName + ".DataAccess");
                // Console.WriteLine(new_dir2);

                Directory.SetCurrentDirectory(newDir);
                currentDir = Directory.GetCurrentDirectory();
                string new_dir8 = new_dir5 + "/DotNetMySQLTemplate/solution/nkv.MicroService/nkv.MicroService.Consumer";
                string src = new_dir8;
                new_dir5 += "/";
                var flies = Directory.GetFiles(src);
                string connection_string = "";
                connection_string += "server=" + server + ";uid=" + uid + ";" + "username=" + username +
                                 ";" + "password=" + password + ";" + "port=" + port +
                                 ";TreatTinyAsBoolean=false;";
                connection_string += "database=" + databaseName + ";";
                List<string> tables = GetTables(connection_string);
                foreach (string file in flies)
                {
                    string fileName = Path.GetFileName(file);
                    if (fileName == "appsettings.json")
                    {
                        string text = File.ReadAllText(file);
                        text = text.Replace("{connectionString}", connection_string)
                            .Replace("{RabbitMqURL}", rabbitMQConn);
                        File.WriteAllText(
                            new_dir5 + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName +
                                                       "/" + projectName +
                                                       ".Consumer/appsettings.json",
                            text);
                    }
                    else if (fileName == "ConsumeMessage.cs")
                    {
                        string text = File.ReadAllText(file);
                        string final = "";
                        foreach (string tbl in tables)
                        {
                            string template =
                                @"if (Equals(className, ""{modelName}Model"")){
                                if (msg.classString != null)
                                {
                            //Deserialized Object into {modelName}Model type
                            {modelName}Model {tableName}Model = JsonConvert.DeserializeObject<{modelName}Model>(msg.classString);
                        Console.WriteLine({tableName}Model);

                        //Parsed the Object into JSon Object 
                        var obj = JObject.Parse(msg.classString);
                        //Console.WriteLine(obj);
                        //Console.WriteLine(obj[DataAccess.findPrimaryKey(className)]);
                        var workflowExec = new workflowExecuter();
                               var res = await Task.Run(async () => await workflowExec.TriggerWorkflowAsync(configuration.GetConnectionString(""NodeRedEndPoint""), {tableName}Model, action, className,token));
                                }
                                else
                                {
                                    dynamic obj = new ExpandoObject();
                                    foreach (var kvp in msg.id)
                                    {
                                        string key = kvp.Key;
                                        int value = kvp.Value;
                                        ((IDictionary<string, object>)obj)[key] = value;

                                    }
                                    var workflowExec = new workflowExecuter();
                                    var res = await Task.Run(async () => await workflowExec.TriggerWorkflowAsync(configuration.GetConnectionString(""NodeRedEndPoint""), obj, action, className,token));
                                }

                    }
                    ";
                            if (tbl != tables.First())
                                template =
                                    @"else if (Equals(className, ""{modelName}Model"")){
                                    if (msg.classString != null)
                                    {
                            //Deserialized Object into {modelName}Model type
                            {modelName}Model {tableName}Model = JsonConvert.DeserializeObject<{modelName}Model>(msg.classString);
                        Console.WriteLine({tableName}Model);

                        //Parsed the Object into JSon Object 
                        var obj = JObject.Parse(msg.classString);
                        //Console.WriteLine(obj);
                        //Console.WriteLine(obj[DataAccess.findPrimaryKey(className)]);
                        var workflowExec = new workflowExecuter();
                               var res = await Task.Run(async () => await workflowExec.TriggerWorkflowAsync(configuration.GetConnectionString(""NodeRedEndPoint""), {tableName}Model, action, className,token));
                                    }
                                    else
                                    {
                                        dynamic obj = new ExpandoObject();
                                        foreach (var kvp in msg.id)
                                        {
                                            string key = kvp.Key;
                                            int value = kvp.Value;
                                            ((IDictionary<string, object>)obj)[key] = value;

                                        }
                                        var workflowExec = new workflowExecuter();
                                        var res = await Task.Run(async () => await workflowExec.TriggerWorkflowAsync(configuration.GetConnectionString(""NodeRedEndPoint""), obj, action, className,token));
                                    }
                    }
                    ";
                            template = template
                                           .Replace("{modelName}",
                                                    char.ToUpper(tbl[0]) + tbl.Substring(1))
                                           .Replace("{tableName}", tbl);
                            final += template + "\n";
                        }
                        text = text.Replace("{printMessageModel}", final);
                        File.WriteAllText(
                            new_dir5 + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName +
                                                       "/" + projectName +
                                                       ".Consumer/ConsumeMessage.cs",
                            text);
                    }
                    else if (fileName == "QueuedetailsDataAccess.cs")
                    {
                        string text = File.ReadAllText(file);
                        text = text.Replace(
                            "{fetchQueueName}",
                            "cmd.CommandText = @\"SELECT queueName FROM messageQueue t\";\n");
                        string pri =
                            $"cmd.CommandText = @\"SELECT PrimaryKey FROM messageQueue t WHERE t.queueName LIKE @name\";\n cmd.Parameters.AddWithValue(\"@name\", name);\n";
                        text = text.Replace("{findPimaryKey}", pri);
                        File.WriteAllText(
                            new_dir5 +
                                         $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/" +
                                             projectName +
                                             ".Consumer/QueuedetailsDataAccess.cs",
                            text);
                    }
                    else if (fileName == "SyncClass.cs")
                    {
                        string text = File.ReadAllText(file), final = "";
                        text = text.Replace("{server}", server);
                        foreach (string tbl in tables)
                        {
                            string template = "";
                            var cols = getAllColumns(tbl, connection_string);
                            List<string> cols1 = new List<string>();
                            var PK = getPrimaryKey(tbl, connection_string);
                            foreach (string i in cols)
                            {
                                if (!PK.Contains(i))
                                    cols1.Add(i);
                            }
                            if (tbl == tables.First())
                            {
                                template =
                                    @"if (Equals(classN, ""{modelName}Model""))
                    {

                            {modelName}Model {tableName}Model = JsonConvert.DeserializeObject<{modelName}Model>(packet.classString);

                            if ({tableName}Model != null)
                            {
                                Console.WriteLine(""product object :{0}\t{1}"", packet.classString
                            
                        ";
                                if (cols1.Count > 0)
                                    template += ",{tableName}Model." + cols1[0] + ");}}";
                                else
                                    template += ",{tableName}Model." + PK[0] + ");}}";
                            }
                            else
                            {
                                template =
                                    @"else if (Equals(classN, ""{modelName}Model""))
                    {

                            {modelName}Model {tableName}Model = JsonConvert.DeserializeObject<{modelName}Model>(packet.classString);

                            if ({tableName}Model != null)
                            {
                                Console.WriteLine(""product object :{0}\t{1}"", packet.classString";
                                if (cols1.Count > 0)
                                    template += ",{tableName}Model." + cols1[0] + ");}}";
                                else
                                    template += ",{tableName}Model." + PK[0] + ");}}";
                            }
                            template = template
                                           .Replace("{modelName}",
                                                    char.ToUpper(tbl[0]) + tbl.Substring(1))
                                           .Replace("{tableName}", tbl);
                            final += template + "\n";
                        }
                        text = text.Replace("{GenerateModelMessage}", final);
                        File.WriteAllText(
                            new_dir5 + $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName +
                                                       "/" + projectName +
                                                       ".Consumer/SyncClass.cs",
                            text);
                    }
                }
                var files = Directory.GetFiles(currentDir, "*.cs");
                foreach (string file in files)
                {
                    string text = File.ReadAllText(file);
                    text = text.Replace("nkv.MicroService", projectName);
                    File.WriteAllText(file, text);
                }
                var di = Directory.EnumerateDirectories(
                    new_dir, "*" + "nkv.MicroService" + "*", SearchOption.AllDirectories);
                foreach (string path in di)
                {
                    string n = path.Replace("nkv.MicroService", projectName);
                    Directory.Move(path, n);
                }
                var fi = Directory.EnumerateFiles(new_dir, "*" + "nkv.MicroService" + "*",
                                                  SearchOption.AllDirectories);
                foreach (string path in fi)
                {
                    string n = path.Replace("nkv.MicroService", projectName);
                    File.Move(path, n);
                }
                var slnFile = Directory.EnumerateFiles(new_dir, "*" + ".sln",
                                                       SearchOption.AllDirectories);
                foreach (string i in slnFile)
                {
                    string text = File.ReadAllText(i);
                    text = text.Replace("nkv.MicroService", projectName);

                    File.WriteAllText(i, text);
                }
                var csprojfile = Directory.EnumerateFiles(new_dir, "*" + ".csproj",
                                                          SearchOption.AllDirectories);
                foreach (string i in csprojfile)
                {
                    string text = File.ReadAllText(i);
                    text = text.Replace("nkv.MicroService", projectName);
                    File.WriteAllText(i, text);
                }
                var dockerfile1 = Directory.EnumerateFiles(new_dir, "*" + ".*",
                                                          SearchOption.AllDirectories);
                foreach (string i in dockerfile1)
                {
                    string text = File.ReadAllText(i);
                    text = text.Replace("nkv.MicroService", projectName);
                    File.WriteAllText(i, text);
                }
                var csfile = Directory.EnumerateFiles(new_dir, "*" + ".cs",
                                                      SearchOption.AllDirectories);
                foreach (string i in csfile)
                {
                    string text = File.ReadAllText(i);
                    text = text.Replace("nkv.MicroService", projectName);
                    File.WriteAllText(i, text);
                }
                var csfile1 = Directory.EnumerateFiles(temp_path, "helper" + ".cs",
                                                       SearchOption.AllDirectories);
                foreach (string i in csfile1)
                {
                    string text = File.ReadAllText(i);
                    text = text.Replace("nkv.MicroService", projectName);
                    File.WriteAllText(i, text);
                }
                Console.WriteLine(new_dir);
                Console.WriteLine(new_dir1);
                var filePaths = Directory.EnumerateFiles(
                    new_dir1 + "/PostmanJson", "*" + ".txt", SearchOption.AllDirectories);
                foreach (string filePath in filePaths)
                {

                    string text = File.ReadAllText(filePath);
                    text = text.Replace("{projectName}", projectName);
                    text =
                        text.Replace("{newGuid}", "c9896e5a-2840-49ab-8f82-e7ee96f65de7");
                    text =
                        text.Replace("{newGuid}", "c9896e5a-2840-49ab-8f82-e7ee96f65de7");
                    text = text.Replace("{itemListString}", "");
                    var json = JsonConvert.DeserializeObject<JObject>(text);
                    string jsonFilePath = Path.ChangeExtension(filePath, ".json");
                    File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(json));
                }
                string text1 = File.ReadAllText("/app/script.sh");
                if (backendTechnology == "DotNet")
                    text1 = text1.Replace("{backendTechnologyFolder}", $"{fname}/{projectName}/DotNet_Output");
                text1 = text1.Replace("{backendTechnology}", projectName);
                string scriptPathh = Path.Combine("/app/script1.sh");
                Console.WriteLine(scriptPathh);
                File.WriteAllText("/app/script1.sh", text1);
                // try
                // {
                //     // Set the working directory for the shell script
                //     //string new_dir3 = Path.Combine(Directory.GetCurrentDirectory(), "project");
                //     // Create a new process to execute the shell script
                //     var process = new Process();

                //     // Set the filename of the shell script
                //     string scriptPath = Path.Combine(new_dir3, "script1.sh");
                //     Console.WriteLine(scriptPath);
                //     // Set the arguments to pass to the script
                //     process.StartInfo.FileName = "/bin/bash";
                //     process.StartInfo.Arguments = $"{scriptPath}";

                //     // Set the working directory for the script
                //     process.StartInfo.WorkingDirectory = Path.Combine(new_dir3);

                //     // Set the output and error streams to be redirected
                //     process.StartInfo.RedirectStandardOutput = true;
                //     process.StartInfo.RedirectStandardError = true;

                //     // Set the UseShellExecute property to false to use the redirected streams
                //     process.StartInfo.UseShellExecute = false;
                //     List<string> error = new List<string>();
                //     // Set the event handlers for the output and error streams
                //     process.OutputDataReceived += (sender, e) => { Console.WriteLine(e.Data); };
                //     process.ErrorDataReceived += (sender, e) =>
                //     {
                //         error.Add(e.Data);
                //         if(e.Data!=null)
                //             Console.WriteLine("Error!!" + e.Data);
                //     };
                //     // Start the process
                //     process.Start();
                //     // Begin reading the output and error streams
                //     process.BeginOutputReadLine();
                //     process.BeginErrorReadLine();

                //     // Wait for the process to complete
                //     process.WaitForExit();
                //     // Get the exit code of the process
                //     int exitCode = process.ExitCode;
                //     if (exitCode == 0)
                //     {
                //         Console.WriteLine("No Errors were found!");
                //     }
                //     else
                //     {
                //         foreach (string err in error)
                //         {
                //             Console.WriteLine(err);
                //         }
                //     }
                // }
                // catch (Exception ex)
                // {
                //     Console.WriteLine("An Error!! occurred while executing the shell script: " + ex.Message);
                // }

                var filess = Directory.GetFiles(
                    Path.Combine(new_dir4, $"../{fname}/{projectName}/DotNet_Output/solution/" + projectName + "/"),
                    "*.*", SearchOption.AllDirectories);
                foreach (string file in filess)
                {
                    string text = File.ReadAllText(file);
                    text = text.Replace("{projectName}", projectName);
                    File.WriteAllText(file, text);
                }
                Directory.SetCurrentDirectory(Directory.GetCurrentDirectory() + "/..");
                Console.Write(Directory.GetCurrentDirectory() + "\n");

                var restore = $"restore";
                var startDockerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"{restore}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                };
                startDockerProcess.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                //startDockerProcess.ErrorDataReceived += (sender, e) => (e.Data != null)? Console.WriteLine("Error!!" + e.Data):"";
                startDockerProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        Console.WriteLine("Error!!" + e.Data);
                };
                startDockerProcess.Start();
                startDockerProcess.WaitForExit();
                startDockerProcess.BeginOutputReadLine();
                startDockerProcess.BeginErrorReadLine();
                var build = $"build";
                startDockerProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"{build}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                };
                //startDockerProcess.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
                //startDockerProcess.ErrorDataReceived += (sender, e) =>
                //{
                //    if (e.Data != null)
                //        Console.WriteLine("Error!!" + e.Data);
                //};
                //// startDockerProcess.ErrorDataReceived += (sender, e) => Console.WriteLine("Error!!" + e.Data);
                //startDockerProcess.Start();
                //startDockerProcess.WaitForExit();
                //startDockerProcess.BeginOutputReadLine();
                //startDockerProcess.BeginErrorReadLine();
                Console.WriteLine("Starting build process.");
                startDockerProcess.Start();
                Console.WriteLine("Started build process.");
                string output = startDockerProcess.StandardOutput.ReadToEnd();
                string error = startDockerProcess.StandardError.ReadToEnd();

                Console.WriteLine("Output:");
                Console.WriteLine(output);
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine("Error!!");
                    Console.WriteLine(error);
                }
                Console.WriteLine("Waiting to exit the process...");
                startDockerProcess.WaitForExit();
                Console.WriteLine("Exited");
                Console.WriteLine("Completed build process.");
               // WorkflowGenerator.generateWorkflowCalled();
                File.Delete($"./{projectName}.API/swagger.json");
                string[] textttt = File.ReadAllLines($"./{projectName}.API/./{projectName}.API.csproj");
                for (int i = 0; i < textttt.Length; i++)
                {
                    if (i == 36 || i == 37 || i == 38)
                        textttt[i] = "";
                }
                File.WriteAllLines($"./{projectName}.API/./{projectName}.API.csproj", textttt);
                string connectionString = connection_string;
                //using (MySqlConnection connection = new MySqlConnection(connectionString))
                //{
                //    connection.Open();
                //    if (connection.State == System.Data.ConnectionState.Open)
                //    {
                //        Console.WriteLine("Second Connection is opened in DotNet_MySQL_Template()");
                //    }
                //foreach (string tbl in tables)
                //{
                //    string tbl1 = tbl.ToLower();
                //    string texttttt = File.ReadAllText($"../Workflows/DELETE/{tbl1}_DELETE.json");
                //    string text11111 = File.ReadAllText($"../Workflows/POST/{tbl1}_POST.json");
                //    string text22222 = File.ReadAllText($"../Workflows/PUT/{tbl1}_PUT.json");
                //    string insert = $"INSERT INTO workflows (workflow_name,workflow_description,steps, triggerpoint, modifiedBy,createdBy,modifiedAt,createdAt,isActive) VALUES ('{tbl}_adding','Adding {tbl} table to workflows', '[{text11111}]','{{\"action\":\"{tbl}_adding\"}}', 'abc','abc',NOW(),NOW(),1);";
                //    string insert1 = $"INSERT INTO workflows (workflow_name,workflow_description,steps, triggerpoint, modifiedBy,createdBy,modifiedAt,createdAt,isActive) VALUES ('{tbl}_updating','Updating {tbl} table to workflows', '[{text22222}]','{{\"action\":\"{tbl}_updating\"}}', 'abc','abc',NOW(),NOW(),1);";
                //    string insert2 = $"INSERT INTO workflows (workflow_name,workflow_description,steps, triggerpoint, modifiedBy,createdBy,modifiedAt,createdAt,isActive) VALUES ('{tbl}_deleting','Deleting {tbl} table from workflows', '[{texttttt}]','{{\"action\":\"{tbl}_deleting\"}}', 'abc','abc',NOW(),NOW(),1);";

                //    using (MySqlCommand command = new MySqlCommand(insert, connection))
                //    {
                //        command.ExecuteNonQuery();
                //    }
                //    using (MySqlCommand command1 = new MySqlCommand(insert1, connection))
                //    {
                //        command1.ExecuteNonQuery();
                //    }
                //    using (MySqlCommand command2 = new MySqlCommand(insert2, connection))
                //    {
                //        command2.ExecuteNonQuery();
                //    }
                //}
                ////    connection.Close();
                //    if (connection.State == System.Data.ConnectionState.Closed)
                //    {
                //        Console.WriteLine("Second Connection is closed in DotNet_MySQL_Template()");
                //    }
                //}

                ZipFile.CreateFromDirectory(zipSource, ZipDes);
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the errors_list
                errors_list.Add($"Error in DotnetMysqlTemplate generation process: {ex.Message}");
            }
        }
    }
    class ReactTsProject
    {
        public static void ReactTs(string server, string uid, string username, string password, string databaseName, string script, string statusOfGeneration, string projectName, string DBexists, string port, string rabbitMQConn, string frontendTechnology, string backendTechnology, string projectType,string front_template_url)
        {
            try
            {
                string createTableSql12345 = @"CREATE TABLE IF NOT EXISTS `dnd_ui_versions` (
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
                string connectionSstring12345 = "server=" + server + ";username=" + username + ";password=" + password + ";port=" + port + ";database=" + databaseName + ";";
                Console.WriteLine(connectionSstring12345);
                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                //using (MySqlConnection connection = new MySqlConnection(connectionSstring12345))
                //{
                //    connection.Open();
                //    if (connection.State == System.Data.ConnectionState.Open)
                //    {
                //        Console.WriteLine("Connection is opened in ReactTs()");
                //    }
                using (MySqlCommand command = new MySqlCommand(createTableSql12345, connection))
                {
                    Console.WriteLine("Executing");
                    command.ExecuteNonQuery();
                }
                //    connection.Close();
                //    if (connection.State == System.Data.ConnectionState.Closed)
                //    {
                //        Console.WriteLine("Connection is closed in ReactTs()");
                //    }
                //}

                string curDir = Directory.GetCurrentDirectory();
                //Dev2
                if (projectType == "dnd")
                {
                    Console.WriteLine(front_template_url);
                    string front_template_json ="";
                    //string front_template_json = @"{
                    //    ""mappings"": [
                    //        {
                    //            ""component"": ""Detail_list_1"",
                    //            ""associated_table"": ""Users"",
                    //            ""description"": ""Ideal for showcasing user statistics and metrics.""
                    //        },
                    //	{
                    //            ""component"": ""Detail_list_1"",
                    //            ""associated_table"": ""Appusers"",
                    //            ""description"": ""Ideal for showcasing user statistics and metrics.""
                    //        },
                    //	{
                    //            ""component"": ""TableView6"",
                    //            ""associated_table"": ""expense_categories"",
                    //            ""description"": ""Manages surveytype details in a table.""
                    //        },
                    //	{
                    //            ""component"": ""Grouped_list_7"",
                    //            ""associated_table"": ""reminders"",
                    //            ""description"": ""Replicates communication interfaces for notifications.""
                    //        },
                    //	{
                    //            ""component"": ""TableView2"",
                    //            ""associated_table"": ""expenses"",
                    //            ""description"": ""Tabular view for managing reports.""
                    //        },
                    //	{
                    //            ""component"": ""Grid_view_2"",
                    //            ""associated_table"": ""expense_split_rules"",
                    //            ""description"": ""Displays permissions associated with surveys.""
                    //        },
                    //	{
                    //            ""component"": ""Grouped_list_2"",
                    //            ""associated_table"": ""group_table"",
                    //            ""description"": ""Segregates condition values by criteria like text and modification details.""
                    //        },
                    //	{
                    //            ""component"": ""Grouped_list_4"",
                    //            ""associated_table"": ""group_balances"",
                    //            ""description"": ""Centrally manages mobile settings and preferences.""
                    //        },
                    //        {
                    //            ""component"": ""Grid_view_4"",
                    //            ""associated_table"": ""currencies"",
                    //            ""description"": ""Displays session details in a user-friendly grid format.""
                    //        },
                    //	{
                    //            ""component"": ""Grouped_list_10"",
                    //            ""associated_table"": ""debt_calculations"",
                    //            ""description"": ""Tracks survey access logs.""
                    //        },
                    //	{
                    //            ""component"": ""TableView2"",
                    //            ""associated_table"": ""group_memberships"",
                    //            ""description"": ""Displays AI user prompts in a table.""
                    //        },
                    //        {
                    //            ""component"": ""Grouped_list_1"",
                    //            ""associated_table"": ""user_preferences"",
                    //            ""description"": ""Suitable for organizing workflow entities.""
                    //        } 
                    //    ]
                    //}";
                    if (front_template_url != null)
                    {
                        Console.WriteLine("downloading frontend template file");
                        using (WebClient client = new WebClient())
                        {
                            client.DownloadFile(front_template_url, "./fronttemplatejson.txt");
                        }
                        front_template_json = File.ReadAllText("./fronttemplatejson.txt");
                        Console.WriteLine(front_template_json);
                    }
                    string src = Path.Combine(curDir, "ReactTsTemplate1/ReactTsProject");

                    string des = Path.Combine(curDir, "../ReactTs_Output1");
                    string[] dirss = System.IO.Directory.GetDirectories(Path.Combine(curDir, "../"), (uid + "_*"));
                    string pt = "";
                    if (dirss.Length == 0)
                    {
                        pt = Path.Combine(curDir, "../", (uid + "_" + DateTime.Now.Ticks));
                        System.IO.Directory.CreateDirectory(pt);
                        pt = pt + "/" + projectName;
                    }
                    else
                        pt = (dirss[0] + "/" + projectName);
                    bool ext = Directory.Exists(pt);
                    if (!ext)
                    {
                        Directory.CreateDirectory(pt);
                        Directory.CreateDirectory(pt + "/zip");
                    }
                    des = pt + "/ReactTs_Output1";
                    if (Directory.Exists(des))
                        Directory.Delete(des, true);
                    Directory.CreateDirectory(des);
                    Functions p = new Functions(databaseName);
                    p.MakeAndCopyDirectory(src, des);
                    ApiGeneratorFunctions.GetForeignKeyInfo(connectionSstring12345);
                    Program.makedictionary();
                    p.ProcessFile(src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendTechnology, projectType, front_template_json : front_template_json);
                    var dirs = Directory.GetFiles(des, "*.*", SearchOption.AllDirectories);
                    foreach (string fileName in dirs)
                    {
                        string contents = File.ReadAllText(fileName);
                        contents = contents.Replace("{projectName}", projectName);
                        File.WriteAllText(fileName, contents);
                    }
                }
                else if (projectType == "workflow")
                {
                    string src = Path.Combine(curDir, "ReactTsTemplate2/ReactTsProject");

                    string des = Path.Combine(curDir, "../ReactTs_Output2");
                    string[] dirss = System.IO.Directory.GetDirectories(Path.Combine(curDir, "../"), (uid + "_*"));
                    string pt = "";
                    if (dirss.Length == 0)
                    {
                        pt = Path.Combine(curDir, "../", (uid + "_" + DateTime.Now.Ticks));
                        System.IO.Directory.CreateDirectory(pt);
                        pt = pt + "/" + projectName;
                    }
                    else
                        pt = (dirss[0] + "/" + projectName);
                    bool ext = Directory.Exists(pt);
                    if (!ext)
                    {
                        Directory.CreateDirectory(pt);
                        Directory.CreateDirectory(pt + "/zip");
                    }
                    des = pt + "/ReactTs_Output2";
                    if (Directory.Exists(des))
                        Directory.Delete(des, true);
                    Directory.CreateDirectory(des);
                    Functions p = new Functions(databaseName);
                    p.MakeAndCopyDirectory(src, des);
                    p.ProcessFile(src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendTechnology, projectType, swaggerurl : rabbitMQConn);
                    var dirs = Directory.GetFiles(des, "*.*", SearchOption.AllDirectories);
                    foreach (string fileName in dirs)
                    {
                        string contents = File.ReadAllText(fileName);
                        contents = contents.Replace("{projectName}", projectName);
                        File.WriteAllText(fileName, contents);
                    }
                }
                else if (projectType == "nodered")
                {
                    string src = Path.Combine(curDir, "ReactTsTemplate4");

                    string des = Path.Combine(curDir, "../ReactTs_Output4");
                    string[] dirss = System.IO.Directory.GetDirectories(Path.Combine(curDir, "../"), (uid + "_*"));
                    string pt = "";
                    if (dirss.Length == 0)
                    {
                        pt = Path.Combine(curDir, "../", (uid + "_" + DateTime.Now.Ticks));
                        System.IO.Directory.CreateDirectory(pt);
                        pt = pt + "/" + projectName;
                    }
                    else
                        pt = (dirss[0] + "/" + projectName);
                    bool ext = Directory.Exists(pt);
                    if (!ext)
                    {
                        Directory.CreateDirectory(pt);
                        Directory.CreateDirectory(pt + "/zip");
                    }
                    des = pt + "/ReactTs_Output4";
                    if (Directory.Exists(des))
                        Directory.Delete(des, true);
                    Directory.CreateDirectory(des);
                    Functions p = new Functions(databaseName);
                    //p.MakeAndCopyDirectory(src, des);
                    p.ProcessFile(src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendTechnology, projectType, swaggerurl: rabbitMQConn);
                    var dirs = Directory.GetFiles(des, "*.*", SearchOption.AllDirectories);
                    foreach (string fileName in dirs)
                    {
                        string contents = File.ReadAllText(fileName);
                        contents = contents.Replace("{projectName}", projectName);
                        File.WriteAllText(fileName, contents);
                    }
                }
                else
                {
                    string src = Path.Combine(curDir, "ReactTsTemplate3/ReactTsProject");

                    string des = Path.Combine(curDir, "../ReactTs_Output3");
                    string[] dirss = System.IO.Directory.GetDirectories(Path.Combine(curDir, "../"), (uid + "_*"));
                    string pt = "";
                    if (dirss.Length == 0)
                    {
                        pt = Path.Combine(curDir, "../", (uid + "_" + DateTime.Now.Ticks));
                        System.IO.Directory.CreateDirectory(pt);
                        pt = pt + "/" + projectName;

                    }
                    else
                        pt = (dirss[0] + "/" + projectName);
                    bool ext = Directory.Exists(pt);
                    if (ext)
                    {
                        Directory.Delete(pt, true);
                    }
                    Directory.CreateDirectory(pt);
                    Directory.CreateDirectory(pt + "/zip");

                    des = pt + "/ReactTs_Output3";
                    if (Directory.Exists(des))
                        Directory.Delete(des, true);
                    Directory.CreateDirectory(des);
                    Functions p = new Functions(databaseName);
                    p.MakeAndCopyDirectory(src, des);
                    p.ProcessFile(src, des, server, uid, username, password, databaseName, script, statusOfGeneration, projectName, DBexists, port, backendTechnology, projectType);
                    var dirs = Directory.GetFiles(des, "*.*", SearchOption.AllDirectories);
                    foreach (string fileName in dirs)
                    {
                        string contents = File.ReadAllText(fileName);
                        contents = contents.Replace("{projectName}", projectName);
                        File.WriteAllText(fileName, contents);
                    }
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the errors_list
                Program.errors_list.Add($"Error in ReactTS generation process: {ex.Message}");
            }

        }
    }
};
