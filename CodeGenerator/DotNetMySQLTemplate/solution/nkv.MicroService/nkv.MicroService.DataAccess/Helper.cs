using System;
using System.Data.Common;
using nkv.MicroService.Utility;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace nkv.MicroService.DataAccess
{
    public static class Helper
    {
        public static int GetColumnOrder(DbDataReader reader, string name)
        {
            int columnOrdinal = reader.GetOrdinal(name);
            return columnOrdinal;
        }
        public static T GetValue<T>(this DbDataReader sqlDataReader, string columnName)
        {
            var value = sqlDataReader[columnName];
            if (value == System.DBNull.Value) return default(T);
            if (value is T t) return t;
            // SQL Server returns byte/bool where MySQL returns sbyte, etc.
            var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, target, System.Globalization.CultureInfo.InvariantCulture);
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
                Regex yourRegex = new Regex(@"(?<=BY\s+).*?(?=\s+(LIMIT|OFFSET)\b)");
                // the query's own table (generated text, not request input); column names come from its schema
                var table = Regex.Match(commandText, @"\bFROM\s+[`\[]?(\w+)", RegexOptions.IgnoreCase).Groups[1].Value;
                string orderStr = "";
                foreach (var r in orderBy)
                {
                    orderStr = orderStr + DataGuard.KnownColumn(table, r.ColumnName) + " " + (string.Equals(r.OrderDir?.Trim(), "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC") + ",";
                }
                orderStr = orderStr.Trim(',');
                newCommandText = yourRegex.Replace(newCommandText, orderStr);
                ApiHardening.Sql("order by: " + newCommandText);

                // Console.WriteLine("newCommandText : ", newCommandText);
                return newCommandText;
            }
            else
            {
                Regex yourRegex = new Regex(@"\sORDER\sBY\scolumn\b");
                // string pattern = @"\sORDER\sBY\sbackend_stack_id\b";

                var newcommandText = commandText;
                // SQL Server pages with OFFSET/FETCH, which needs an ORDER BY
                newcommandText = yourRegex.Replace(newcommandText, Db.IsSqlServer ? " ORDER BY (SELECT NULL)" : "");
                ApiHardening.Sql(newcommandText);

                return newcommandText;
            }
        }
    }
}
