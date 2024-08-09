using MySql.Data.MySqlClient;
using System;
using nkv.MicroService.Utility;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace nkv.MicroService.DataAccess
{
    public static class Helper
    {
        public static int GetColumnOrder(MySqlDataReader reader, string name)
        {
            int columnOrdinal = reader.GetOrdinal(name);
            return columnOrdinal;
        }
        public static T GetValue<T>(this MySqlDataReader sqlDataReader, string columnName)
        {
            var value = sqlDataReader[columnName];

            return value == System.DBNull.Value ? default(T) : (T)value;
        }
        public static string GetMD5HashData(string data)
        {
            //create new instance of md5
            MD5 md5 = MD5.Create();

            //convert the input text to array of bytes
            byte[] hashData = md5.ComputeHash(Encoding.Default.GetBytes(data));

            //create new instance of StringBuilder to save hashed data
            StringBuilder returnValue = new StringBuilder();

            //loop for each byte and add it to StringBuilder
            for (int i = 0; i < hashData.Length; i++)
            {
                returnValue.Append(hashData[i].ToString());
            }

            // return hexadecimal string
            return returnValue.ToString();

        }

        public static bool ValidateMD5HashData(string inputData, string storedHashData)
        {
            //hash input text and save it string variable
            string getHashInputData = GetMD5HashData(inputData);

            if (string.Compare(getHashInputData, storedHashData) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static string ConverOrderListToSQL(string commandText, List<OrderByModel> orderBy)
        {
            if (orderBy != null && orderBy.Count > 0)
            {
                var newCommandText = commandText;
                Regex yourRegex = new Regex(@"(?<=BY\s+).*?(?=\s+LIMIT)");
                string orderStr = "";
                foreach (var r in orderBy)
                {
                    orderStr = orderStr + r.ColumnName + " " + r.OrderDir + ",";
                }
                orderStr = orderStr.Trim(',');
                newCommandText = yourRegex.Replace(newCommandText, orderStr);
                Console.WriteLine("when NOT NULL newcommandText :" + newCommandText);

                // Console.WriteLine("newCommandText : ", newCommandText);
                return newCommandText;
            }
            else
            {
                Regex yourRegex = new Regex(@"\sORDER\sBY\scolumn\b");
                // string pattern = @"\sORDER\sBY\sbackend_stack_id\b";

                var newcommandText = commandText;
                newcommandText = yourRegex.Replace(newcommandText, "");
                Console.WriteLine("when NULL newcommandText :" + newcommandText);

                return newcommandText;
            }
        }
    }
}
