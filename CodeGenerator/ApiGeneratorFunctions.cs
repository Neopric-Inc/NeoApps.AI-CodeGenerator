using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3.Model;
using EnvDTE80;
using HelloWorld;
using MySql.Data.MySqlClient;
using NoCodeAppGenerator;
namespace NoCodeAppGenerator
{
    public class ForeignKeyInfo
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string ReferencedTableName { get; set; }
        public string ReferencedColumnName { get; set; }
    }
    public class ApiGeneratorFunctions
    {
        public static Dictionary<string, List<ForeignKeyInfo>> tablesWithForeignKeys = new Dictionary<string, List<ForeignKeyInfo>>();
        public static Dictionary<string, List<string>> referencedBy = new Dictionary<string, List<string>>();
        public static void GetForeignKeyInfo(string connectionString)
        {
            try
            {
                string query = @"
                SELECT
                    TABLE_NAME,
                    COLUMN_NAME,
                    REFERENCED_TABLE_NAME,
                    REFERENCED_COLUMN_NAME
                FROM
                    INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE
                    TABLE_SCHEMA = @DatabaseName
                    AND REFERENCED_TABLE_NAME IS NOT NULL
                ORDER BY TABLE_NAME ASC";

                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {


                    command.Parameters.AddWithValue("@DatabaseName", Program.databaseName);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader["TABLE_NAME"].ToString();
                            string columnName = reader["COLUMN_NAME"].ToString();
                            string referencedTableName = reader["REFERENCED_TABLE_NAME"].ToString();
                            string referencedColumnName = reader["REFERENCED_COLUMN_NAME"].ToString();

                            ForeignKeyInfo info = new ForeignKeyInfo
                            {
                                TableName = tableName,
                                ColumnName = columnName,
                                ReferencedTableName = referencedTableName,
                                ReferencedColumnName = referencedColumnName
                            };

                            if (tableName == "messagequeue" || tableName == "dnd_ui_versions" || tableName == "workflows" || tableName == "project_dnd_ui_versions" || tableName == "workflow" || tableName == "workflow_builds" || tableName == "workflows_projects" || tableName == "workflow_runs" || tableName == "workflow_deployments" || tableName == "workflow_triggers" || tableName == "workflow_trigger_conditions" || tableName == "roles" || tableName == "users" || tableName == "entities" || tableName == "permissionmatrix" || referencedTableName == "messagequeue" || referencedTableName == "dnd_ui_versions" || referencedTableName == "workflows" || referencedTableName == "project_dnd_ui_versions" || referencedTableName == "workflow" || referencedTableName == "workflow_builds" || referencedTableName == "workflows_projects" || referencedTableName == "workflow_runs" || referencedTableName == "workflow_deployments" || referencedTableName == "workflow_triggers" || referencedTableName == "workflow_trigger_conditions" || referencedTableName == "roles" || referencedTableName == "users" || referencedTableName == "entities" || referencedTableName == "permissionmatrix")
                            {
                                continue;
                            }

                            if (!tablesWithForeignKeys.ContainsKey(tableName))
                            {
                                tablesWithForeignKeys[tableName] = new List<ForeignKeyInfo>();
                            }

                            tablesWithForeignKeys[tableName].Add(info);

                            if (!referencedBy.ContainsKey(referencedTableName))
                            {
                                referencedBy[referencedTableName] = new List<string>();
                            }

                            referencedBy[referencedTableName].Add(tableName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                Program.errors_list.Add($"Error in GetForeignkeyInfo: {ex.Message}");
            }
        }


        public static string GetForeignKeyColumnname(string tablename, string referencedtablename)
        {
            try
            {
                string query = @"
                SELECT 
                    COLUMN_NAME
                FROM 
                    INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                WHERE 
                    TABLE_SCHEMA = @DatabaseName
                    AND TABLE_NAME = @TableName
                    AND REFERENCED_TABLE_NAME = @ReferencedTableName
                ";

                MySqlConnection connection = MySqlConnectionManager.Instance.GetConnection();
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {


                    command.Parameters.AddWithValue("@DatabaseName", Program.databaseName);
                    command.Parameters.AddWithValue("@TableName", tablename);
                    command.Parameters.AddWithValue("@ReferencedTableName", referencedtablename);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string columnName = reader["COLUMN_NAME"].ToString();
                            return columnName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                Program.errors_list.Add($"Error in GetForeignKeyColumnname: {ex.Message}");
            }
            return "";
        }

        public static void ProcessSingleQueryGetAll(ref string singleParams, string tablesmall, List<string> PK, string referencingColumnName)
        {
            try
            {
                for (int i = 0; i < PK.Count; i += 2)
                {
                    //if (referencingColumnName != PK[i])
                    //{
                        singleParams += "(" + "\"@" + PK[i] + "\"," + tablesmall + "[i]." + referencingColumnName + ");";
                    //}
                    //else
                    //{
                    //    singleParams += "(" + "\"@" + PK[i] + "\"," + tablesmall + "[i]." + PK[i] + ");";
                    //}
                    if (i != PK.Count - 1 && i != PK.Count - 2)
                        singleParams += "\ncmd.Parameters.AddWithValue";
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                Program.errors_list.Add($"Error in ProcessSingleQuery: {ex.Message}");
            }

        }

        public static void Filledparams(ref string primaryKeyList, List<string> PK)
        {
            try
            {
                for (int i = 0; i < PK.Count; i += 2)
                {
                    primaryKeyList += PK[i];
                    if (i != PK.Count - 1 && i != PK.Count - 2)
                        primaryKeyList += ",";
                }

            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                Program.errors_list.Add($"Error in Filledparams: {ex.Message}");
            }
        }


        public static void CreateParamsAggregateContoller(ref string url, ref string argu, ref string param, ref string validation, List<string> ans)
        {
            try
            {
                url = "{"; argu = ""; param = ""; validation = "";
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
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                Program.errors_list.Add($"Error in ProcessController: {ex.Message}");
            }
        }

        public static void ProcessSingleQueryWithObjectName(ref string singleQuery, ref string singleParams, List<string> PK, string refrencename,string referencingColumnName)
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
                    //if (referencingColumnName != PK[i])
                    //{
                    //    singleParams += "(" + "\"@" + PK[i] + "\"," + refrencename + "." + PK[i] + ");";
                    //}
                    //else
                    //{
                        singleParams += "(" + "\"@" + PK[i] + "\"," + refrencename + "." + referencingColumnName + ");";
                   // }
                    if (i != PK.Count - 1 && i != PK.Count - 2)
                        singleParams += "\ncmd.Parameters.AddWithValue";
                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                Program.errors_list.Add($"Error in ProcessSingleQuery: {ex.Message}");
            }

        }


        public static void insertParameter(ref string insert_q, List<string> columns)
        {
            try
            {
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
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                Program.errors_list.Add($"Error in ProcessUpdateQuery: {ex.Message}");
            }
        }


        public static void getAllColsMetadataReporting(ref string selectAllModelInit, string tbl, string connectionString, ref HashSet<string> set, string reftable,string referencingColumn)
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
                   
                    selectAllModelInit += reftable + "[i].";

                    //if (column["COLUMN_NAME"].ToString() == "isActive" || column["COLUMN_NAME"].ToString() == "createdBy" || column["COLUMN_NAME"].ToString() == "modifiedBy" || column["COLUMN_NAME"].ToString() == "createdAt" || column["COLUMN_NAME"].ToString() == "modifiedAt")
                    //{
                    //    selectAllModelInit += tbl + "_" + column["COLUMN_NAME"].ToString();
                    //}
                    //else
                    //{
                    //    selectAllModelInit += column["COLUMN_NAME"].ToString();
                    //}
                    if (referencingColumn != "")
                    {
                        selectAllModelInit += referencingColumn+"_"+ char.ToUpper(tbl[0]) + tbl.Substring(1) + "_"+ column["COLUMN_NAME"].ToString();
                    }
                    else
                    {
                        selectAllModelInit += column["COLUMN_NAME"].ToString();
                    }
                    
                    if (column["IS_NULLABLE"].ToString() == "YES" && column["DATA_TYPE"].ToString() != "varchar")
                    {
                        string data = utility.getType(column["DATA_TYPE"].ToString());
                        if (data.Contains("Date"))
                            selectAllModelInit += "= " + "reader.IsDBNull(Helper.GetColumnOrder(reader," + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")) ? " + "(" + "String" + "?)null : reader.Get" + data + "(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ").ToString()";
                        else if (data.Contains("float"))
                            selectAllModelInit += "= " + "reader.IsDBNull(Helper.GetColumnOrder(reader," + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")) ? " + "(" + dict[column["DATA_TYPE"].ToString()].ToLower() + "?)null : reader.Get" + dict[column["DATA_TYPE"].ToString()] + "(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        else
                            selectAllModelInit += "= " + "reader.IsDBNull(Helper.GetColumnOrder(reader," + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")) ? " + "(" + dict[column["DATA_TYPE"].ToString()] + "?)null : reader.Get" + dict[column["DATA_TYPE"].ToString()] + "(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        selectAllModelInit += ";\n";
                    }
                    else
                    {
                        string data = utility.getType(column["DATA_TYPE"].ToString());
                        if (data.Contains("Date"))
                            selectAllModelInit += "= " + "reader.GetValue<" + data + ">(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ").ToString()";
                        else if (data.Contains("float"))
                            selectAllModelInit += "= " + "reader.GetValue<" + dict[column["DATA_TYPE"].ToString()].ToLower() + ">(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        else
                            selectAllModelInit += "= " + "reader.GetValue<" + dict[column["DATA_TYPE"].ToString()] + ">(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        selectAllModelInit += ";\n";
                    }


                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                Program.errors_list.Add($"Error in GetColsMetadata: {ex.Message}");
            }

        }
        public static void getColsMetadataReporting(ref string selectAllModelInit, string tbl, string connectionString, ref HashSet<string> set, string reftable,string referencingColumnName)
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
                    selectAllModelInit += reftable + ".";
                    //if (column["COLUMN_NAME"].ToString() == "isActive" || column["COLUMN_NAME"].ToString() == "createdBy" || column["COLUMN_NAME"].ToString() == "modifiedBy" || column["COLUMN_NAME"].ToString() == "createdAt" || column["COLUMN_NAME"].ToString() == "modifiedAt")
                    //{
                    //    selectAllModelInit += tbl + "_" + column["COLUMN_NAME"].ToString();
                    //}
                    //else
                    //{
                    //    selectAllModelInit += column["COLUMN_NAME"].ToString();
                    //}
                    selectAllModelInit += referencingColumnName + "_" + char.ToUpper(tbl[0]) + tbl.Substring(1) + "_" + column["COLUMN_NAME"].ToString();
                    if (column["IS_NULLABLE"].ToString() == "YES" && column["DATA_TYPE"].ToString() != "varchar")
                    {
                        string data = utility.getType(column["DATA_TYPE"].ToString());
                        if (data.Contains("Date"))
                            selectAllModelInit += "= " + "reader.IsDBNull(Helper.GetColumnOrder(reader," + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")) ? " + "(" + "String" + "?)null : reader.Get" + data + "(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ").ToString()";
                        else if (data.Contains("float"))
                            selectAllModelInit += "= " + "reader.IsDBNull(Helper.GetColumnOrder(reader," + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")) ? " + "(" + dict[column["DATA_TYPE"].ToString()].ToLower() + "?)null : reader.Get" + dict[column["DATA_TYPE"].ToString()] + "(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        else
                            selectAllModelInit += "= " + "reader.IsDBNull(Helper.GetColumnOrder(reader," + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")) ? " + "(" + dict[column["DATA_TYPE"].ToString()] + "?)null : reader.Get" + dict[column["DATA_TYPE"].ToString()] + "(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        selectAllModelInit += ";\n";
                    }
                    else
                    {
                        string data = utility.getType(column["DATA_TYPE"].ToString());
                        if (data.Contains("Date"))
                            selectAllModelInit += "= " + "reader.GetValue<" + data + ">(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ").ToString()";
                        else if (data.Contains("float"))
                            selectAllModelInit += "= " + "reader.GetValue<" + dict[column["DATA_TYPE"].ToString()].ToLower() + ">(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        else
                            selectAllModelInit += "= " + "reader.GetValue<" + dict[column["DATA_TYPE"].ToString()] + ">(" + "\"" + column["COLUMN_NAME"].ToString() + "\"" + ")";
                        selectAllModelInit += ";\n";
                    }


                }
            }
            catch (Exception ex)
            {
                // If an exception occurs, catch it and add to the error list
                Program.errors_list.Add($"Error in GetColsMetadata: {ex.Message}");
            }

        }
    }
}


