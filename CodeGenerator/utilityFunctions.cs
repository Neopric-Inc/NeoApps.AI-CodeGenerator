using System;
using System.Collections;
using System.IO;
using NoCodeAppGenerator;
using static Google.Protobuf.Reflection.FieldOptions.Types;

namespace HelloWorld
{
    class utility
    {
        // Map MYSQL datatype to c# datatype & put that in Model file
        public static string GetJsAggregateType(string str)
        {
            string aggregateType = str.ToLower();
            string jsType = "";
            try
            {
                if (aggregateType == "sbyte" || aggregateType == "byte" || aggregateType == "short" || aggregateType == "ushort" || aggregateType == "int" || aggregateType == "uint" || aggregateType == "long" || aggregateType == "ulong" || aggregateType == "float" || aggregateType == "double" || aggregateType == "decimal")
                {
                    jsType = "Number";
                }
                else if (aggregateType == "datetime")
                {
                    jsType = "Date";
                }
                else if (aggregateType == "char" || aggregateType == "string" || aggregateType == "char[]")
                {
                    jsType = "String";
                }
                else if (aggregateType == "bool")
                {
                    jsType = "Boolean";
                }
                else
                {
                    jsType = "Unknown";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetJsAggregateType:");
                Console.WriteLine(ex.ToString());
                Program.errors_list.Add("Error in GetJsAggregateType: " + ex.ToString());
                jsType = "Unknown"; // Return a fallback value in case of an error
            }
            return jsType;
        }

        public static string GetJSType(string str)
        {
            string mysqlType = str.ToUpper();
            string jsType = ""; // this will hold the corresponding JavaScript data type

            try
            {
                if (mysqlType == "BIT" || mysqlType == "TINYINT" || mysqlType == "BOOL" || mysqlType == "SMALLINT" || mysqlType == "MEDIUMINT" || mysqlType == "INT" || mysqlType == "BIGINT" || mysqlType == "YEAR" || mysqlType == "DECIMAL" || mysqlType == "FLOAT" || mysqlType == "DOUBLE")
                {
                    jsType = "Number";
                }
                else if (mysqlType == "DATE" || mysqlType == "DATETIME" || mysqlType == "TIMESTAMP" || mysqlType == "TIME")
                {
                    jsType = "Date";
                }
                else if (mysqlType == "CHAR" || mysqlType == "VARCHAR" || mysqlType == "TINYTEXT" || mysqlType == "TEXT" || mysqlType == "MEDIUMTEXT" || mysqlType == "LONGTEXT" || mysqlType == "ENUM" || mysqlType == "SET")
                {
                    jsType = "String";
                }
                else if (mysqlType == "BINARY" || mysqlType == "VARBINARY" || mysqlType == "TINYBLOB" || mysqlType == "BLOB" || mysqlType == "MEDIUMBLOB" || mysqlType == "LONGBLOB")
                {
                    jsType = "Buffer";
                }
                else
                {
                    jsType = "Unknown";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetJSType:");
                Console.WriteLine(ex.ToString());
                Program.errors_list.Add("Error in GetJSType: " + ex.ToString());
                jsType = "Unknown"; // Return a fallback value in case of an error
            }
            return jsType;
        }

        public static string GetvalJSType(string str)
        {
            string mysqlType = str.ToUpper();
            string jsType = ""; // this will hold the corresponding JavaScript data type
            try
            {
                if (mysqlType == "DATE" || mysqlType == "DATETIME" || mysqlType == "TIMESTAMP" || mysqlType == "TIME")
                {
                    jsType = "date";
                }
                else
                {
                    jsType = "text";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in GetJSType:");
                Console.WriteLine(ex.ToString());
                Program.errors_list.Add("Error in GetJSType: " + ex.ToString());
                jsType = "text"; // Return a fallback value in case of an error
            }
            return jsType;
        }
        public static string getType(string str)
        {
            string t = str.ToLower();
            if (t.Contains("smallint"))
                return "short";
            else if (t.Contains("date") || t.Contains("stamp"))
                return "DateTime";
            else if (t.Contains("bigint"))
                return "Int64";
            else if (t.Contains("int") || t.Contains("year"))
                return "int";
            else if (t.Contains("float"))
                return "float";
            else if (t.Contains("double") || t.Contains("real"))
                return "double";
            else if (t.Contains("char") || t.Contains("text") || t.Contains("var"))
                return "string";
            else if (t.Contains("bit") || t.Contains("bool"))
                return "boolean";
            else
                return "string";
        }

        // Annotate the Model Field with appropriate 
        // Incomplete Module
        public static string getKeyAnnotation(string isNull)
        {
            if (isNull == "NO")
                return "[Required]";
            return "";
        }

        // is Field is Nullable then put "?" in Model file
        public static string isNullable(string isNull)
        {
            if (isNull == "YES")
                return "YES";
            else
                return "";
        }
        public static string getInt(string type,string isNull,string keytp){
            if(type=="int" && isNull!="YES" && keytp!="PRI")
                return "[Range(Int.MinValue,Int.MaxValue)]";
            return "";
        }
        // find all occurence of string from file & replace it with given string;
        public static void replaceAll(string fileName, string originalText, string ReplaceText)
        {
            try
            {
                string str = File.ReadAllText(fileName);
                str = str.Replace(originalText, ReplaceText);
                File.WriteAllText(fileName, str);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in replaceAll:");
                Console.WriteLine(ex.ToString());
                Program.errors_list.Add("Error in replaceAll: " + ex.Message);
            }
        }
        public static void printArrayList(ArrayList arr)
        {
            // This will Print all details of schema from arrayList.
            for (int i = 0; i < arr.Count; i++)
            {
                obj o = (obj)arr[i];
                Console.WriteLine("----------------------------------------\n");
                Console.WriteLine("Field Name :- " + o.field + "\n");
                Console.WriteLine("Field Type :- " + o.type + "\n");
                Console.WriteLine("is Field Nullable? :- " + o.isNull + "\n");
                Console.WriteLine("Key for Field :- " + utility.keyType(o.whichKey) + "\n");
                Console.WriteLine("Default Value :- " + o.defaultVal + "\n");
                Console.WriteLine("Extra Attributes :- " + o.extra + "\n");
            }
            Console.WriteLine("----------------------------------------\n");
        }

        // Returns Which Type of Key Constraint is there on perticular Column
        public static string keyType(string k)
        {
            if (k == "PRI")
                return "Primary_Key";
            else if (k == "MUL")
                return "Foreign_Key";
            return "";
        }

        // this functio will extract the database name from .sql script(because there is no default way to achieve this)
        public static string extractDatabaseName(string fileName)
        {
            string script = File.ReadAllText(fileName);

            int startPos = script.LastIndexOf("-- Database: `") + "-- Database: `".Length;
            int length = script.IndexOf("`\n--") - startPos;
            // Console.WriteLine(startPos + " " + length);
            string databaseName = script.Substring(startPos, length);

            return databaseName;
        }
    }
}