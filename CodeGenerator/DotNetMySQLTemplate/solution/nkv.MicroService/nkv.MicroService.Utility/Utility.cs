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
        public static List<OrderByModel> ConvertStringOrderToOrderModel(string orderBy)
        {
            List<OrderByModel> orderModelList = new List<OrderByModel>();
            if (!string.IsNullOrEmpty(orderBy))
            {
                var orderStr = orderBy.Split(',');
                foreach (var record in orderStr)
                {
                    if (!string.IsNullOrEmpty(record))
                    {
                        var oStr = record.Split('|');
                        if (oStr.Length >= 2)
                        {
                            OrderByModel orderModel = new OrderByModel()
                            {
                                ColumnName = oStr[0],
                                OrderDir = oStr[1]
                            };
                            orderModelList.Add(orderModel);
                        }
                        else if (oStr.Length == 1)
                        {
                            OrderByModel orderModel = new OrderByModel()
                            {
                                ColumnName = oStr[0],
                                OrderDir = "DESC"
                            };
                            orderModelList.Add(orderModel);
                        }
                    }
                }
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

