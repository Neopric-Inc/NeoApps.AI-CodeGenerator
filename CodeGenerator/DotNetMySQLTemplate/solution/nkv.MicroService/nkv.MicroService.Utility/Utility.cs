using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

namespace nkv.MicroService.Utility
{
    public static class UtilityCommon
    {
        /// <summary>
        /// Parses "col|ASC,other|DESC". Column names are interpolated into SQL by the data layer, so only plain
        /// identifiers and ASC/DESC are accepted; anything else is ignored (prevents SQL injection via orderBy).
        /// </summary>
        public static List<OrderByModel> ConvertStringOrderToOrderModel(string orderBy)
        {
            List<OrderByModel> orderModelList = new List<OrderByModel>();
            if (string.IsNullOrEmpty(orderBy)) return orderModelList;
            foreach (var record in orderBy.Split(','))
            {
                var oStr = record.Split('|');
                var column = oStr[0].Trim();
                var dir = oStr.Length >= 2 ? oStr[1].Trim().ToUpperInvariant() : "DESC";
                if (!System.Text.RegularExpressions.Regex.IsMatch(column, "^[A-Za-z_][A-Za-z0-9_]{0,63}$")) continue;
                if (dir != "ASC" && dir != "DESC") dir = "DESC";
                orderModelList.Add(new OrderByModel { ColumnName = column, OrderDir = dir });
            }
            return orderModelList;
        }

        public static bool IsImage(IFormFile file)
        {
            var headers = new List<byte[]>
                {
                    Encoding.ASCII.GetBytes("BM"),      // BMP
                    Encoding.ASCII.GetBytes("GIF"),     // GIF
                    new byte[] { 137, 80, 78, 71 },     // PNG
                    new byte[] { 73, 73, 42 },          // TIFF
                    new byte[] { 77, 77, 42 },          // TIFF
                    new byte[] { 255, 216, 255, 224 },  // JPEG
                    new byte[] { 255, 216, 255, 225 }   // JPEG CANON
                };

            return headers.Any(x => x.SequenceEqual(ConvertToBytes(file).Take(x.Length)));
        }

        public static byte[] ConvertToBytes(IFormFile image)
        {
            byte[] CoverImageBytes = null;
            BinaryReader reader = new BinaryReader(image.OpenReadStream());
            CoverImageBytes = reader.ReadBytes((int)image.Length);
            return CoverImageBytes;
        }

        public static string ConvertFilterToSQLString(FilterConditionEnum filterConditionEnum)
        {
            switch (filterConditionEnum)
            {
                case FilterConditionEnum.Equals:
                    return "=";
                case FilterConditionEnum.Contains:
                    return "LIKE";
                case FilterConditionEnum.NotContains:
                    return "NOT LIKE";
                case FilterConditionEnum.GreaterThan:
                    return ">";
                case FilterConditionEnum.GreaterThanEqual:
                    return ">=";
                case FilterConditionEnum.LessThan:
                    return "<";
                case FilterConditionEnum.LessThanEqual:
                    return "<=";
                default:
                    return "LIKE";
            }
        }
    }
}

