using System;
using System.Collections;
using System.IO;
using MySqlConnector;

namespace HelloWorld
{
    public class createModelfromDB
    {
        private string connectionString;
        private string tableName { get; set; }
        private string projectName;
        private ArrayList arr { get; set; }

        public createModelfromDB(string connectionString, string projectName)
        {
            // for DateTime field type
            this.connectionString = connectionString + "ConvertZeroDateTime=True;";
            // this.tableName = tableName;
            this.projectName = projectName;
        }
        public ArrayList showTables(ref MySqlConnection connection)
        {
            ArrayList tableLists = new ArrayList();
            // create MySQL command , set Type & Execute it 
            // it will show users all tables & ask to choose one
            try
            {
                MySqlCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.Text;
                command.CommandText = "SHOW TABLES";
                MySqlDataReader reader = command.ExecuteReader();
                var data = "[Tables]\n";
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        tableLists.Add(reader.GetString(0));
                        data += reader.GetString(0) + Environment.NewLine;
                    }
                    Console.WriteLine(data);
                }
                else
                {
                    Console.WriteLine("-- No Tables In DataBase --");
                }
                reader.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in showTables:");
                Console.WriteLine(ex.ToString());
                NoCodeAppGenerator.Program.errors_list.Add("Error in showTables: " + ex.ToString());
            }
            return tableLists;
        }
        public void createModelbyPassingDB()
        {
            // static connection for developement purpose
            // static string connectionString = "server=localhost;database=myeshop;uid=root;port=3306;ConvertZeroDateTime=True;";
            // static MySqlConnection connection = new MySqlConnection(connectionString);

            // get the connection string from user (we have to do it using GUI).

            Console.WriteLine(connectionString);
            // connect to MySQL Database
            MySqlConnection connection = new MySqlConnection(connectionString);

            Console.WriteLine("Connect to MySQL DB..... \n");

            // ArrayList that contains all schema related details for perticular table.
            arr = new ArrayList();

            using (connection)
            {
                try
                {
                    connection.Open();

                    ArrayList tableList = showTables(ref connection);

                    // foreach (var table in tableList)
                    // {
                    //     Console.WriteLine(table + " , ");
                    // }
                    Console.WriteLine("Connection is :- " + connection.State.ToString() + Environment.NewLine);
                    foreach (string tableName in tableList)
                    {
                        // Choose which table to create Model file

                        MySqlCommand command = connection.CreateCommand();
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "SHOW COLUMNS FROM " + tableName;
                        MySqlDataReader reader = command.ExecuteReader();
                        // MySqlDataReader reader = command.ExecuteReader();

                        // untill table has column
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                // bind all schema value from table to out class "obj"
                                obj temp = new obj();
                                temp.field = reader[0].ToString();
                                temp.type = reader[1].ToString();
                                temp.isNull = reader[2].ToString();
                                temp.whichKey = reader[3].ToString();
                                temp.defaultVal = reader[4].ToString();
                                temp.extra = reader[5].ToString();
                                // data += reader.GetString(0) + "\t" + reader.GetString(1) + "\t" + reader.GetString(2) + "\t" + reader.GetString(3) + "\t" + reader.GetString(4) + "\t" + reader.GetString(5) + Environment.NewLine;
                                // data += reader[0].ToString() + "\t" + reader[1].ToString() + "\t" + reader[2].ToString() + "\t" + reader[3].ToString() + "\t" + reader[4].ToString() + "\t" + reader[5].ToString() + Environment.NewLine;
                                arr.Add(temp);
                            }
                            // Console.WriteLine(data);
                        }
                        else
                        {
                            Console.WriteLine("-- DataBase Empty --");
                        }
                        createModel(tableName, projectName, arr);
                        string temp_connection = "server=localhost;uid=root;port=3306;database=myeshopAPItemp";
                        //MySqlConnection connection1 = new MySqlConnection(temp_connection);
                        //using (connection1)
                        //{
                        //    try
                        //    {
                        //        connection1.Open();
                        //        string sql = "INSERT INTO messageQueue (queueName) VALUES (@val1)";
                        //        using (MySqlCommand cmd = new MySqlCommand(sql, connection1))
                        //        {
                        //            cmd.Parameters.AddWithValue("@val1", tableName+"Model");
                        //            int rows = cmd.ExecuteNonQuery();
                        //            Console.WriteLine("Rows affected: {0}", rows);
                        //        }
                        //    }
                        //    catch(Exception e)
                        //    {
                        //        Console.WriteLine("Error:" + e.ToString());
                        //    }
                        //}
                        // Print arrayList for Testing
                        // utility.printArrayList(arr);

                        // Closing the connection.
                        reader.Close();
                        
                        Console.WriteLine("Connection is :- " + connection.State.ToString() + Environment.NewLine);

                    }
                    connection.Close();
                    // pass Table Name , Project Name & ArrayList in Argument & create actual Model 

                }
                catch (MySql.Data.MySqlClient.MySqlException ex)
                {
                    Console.WriteLine("Error in createModelbyPassingDB: " + ex.ToString());
                    NoCodeAppGenerator.Program.errors_list.Add("Error in createModelbyPassingDB: " + ex.Message);
                }
                
            }
        }
        // Handle the creation of Model file
        public void createModel(string tableName, string projectName, ArrayList arr)
        {
            string fileName = "./Output/solution/" + projectName + "/" + projectName + ".Model/" + tableName + "Model.cs";
            try
            {
                // check if file exist , if exist then delete & write new data.
                if (File.Exists(fileName))
                {
                    Console.WriteLine("File is Already Present.");
                    File.Delete(fileName);
                }
                // copy the data from template model file
                File.Copy("./"+"model.txt", fileName);
                utility.replaceAll(fileName, "{projectName}", projectName);
                utility.replaceAll(fileName, "{tableName}", tableName);

                // "modelProperty" contains all data related to all field.
                string modelProperty = "";
                for (int i = 0; i < arr.Count; i++)
                {
                    obj o = (obj)(arr[i]);
                    // annotate the field
                    modelProperty += (utility.getKeyAnnotation(o.isNull)=="YES" && utility.keyType(o.field)!="PRI")?"[REQUIRED]":"" + "\n\t\t";
                    // real writing of model field
                    modelProperty += "public " + utility.getType(o.type) ;
                    if(utility.isNullable(o.isNull)=="YES")
                    {
                        if(utility.getType(o.type)!="string"){
                            modelProperty+="?";
                        }
                    }
                    modelProperty+= " " + o.field + " { get; set; }\n\t\t";
                }
                utility.replaceAll(fileName, "{modelProperties}", modelProperty);
            }
            catch (Exception ex)
            {

                Console.WriteLine("Error in createModel: " + ex.ToString());
                NoCodeAppGenerator.Program.errors_list.Add("Error in createModel: " + ex.Message);
            }

            Console.WriteLine($"{tableName} Model has been Generated Successfully.");
        }
    }
}