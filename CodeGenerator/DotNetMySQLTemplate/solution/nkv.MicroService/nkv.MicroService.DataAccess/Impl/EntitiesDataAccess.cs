using MySql.Data.MySqlClient;
using nkv.MicroService.DataAccess.Interface;
using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System.Collections.Generic;
using System;

namespace nkv.MicroService.DataAccess.Impl
{
    public class EntitiesDataAccess : IEntitiesDataAccess
    {
        private MySqlDatabaseConnector mySqlDatabaseConnector { get; set; }
        public EntitiesDataAccess(MySqlDatabaseConnector _mySqlDatabaseConnector)
        {
            mySqlDatabaseConnector = _mySqlDatabaseConnector;
        }
        public int GetAllTotalRecordEntities()
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalCount FROM entities t";
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return reader.GetInt32("TotalCount");
                        }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return 0;
        }
        public int GetSearchTotalRecordEntities(string searchKey)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalCount FROM entities t WHERE t.entity_name LIKE CONCAT('%',@SearchKey,'%')";
                    cmd.Parameters.AddWithValue("@SearchKey", searchKey);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return reader.GetInt32("TotalCount");
                        }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return 0;
        }
        public List<EntitiesModel> GetAllEntities(int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var ret = new List<EntitiesModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM entities t ORDER BY column LIMIT @Offset, @ItemsPerPage";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new EntitiesModel()
                            {
                                entity_id = reader.GetValue<Int32>("entity_id"),
                                entity_name = reader.GetValue<String>("entity_name"),
                                createdBy = reader.IsDBNull(Helper.GetColumnOrder(reader, "createdBy")) ? (String?)null : reader.GetString("createdBy"),
                                modifiedBy = reader.IsDBNull(Helper.GetColumnOrder(reader, "modifiedBy")) ? (String?)null : reader.GetString("modifiedBy"),
                                createdAt = reader.IsDBNull(Helper.GetColumnOrder(reader, "createdAt")) ? (String?)null : reader.GetDateTime("createdAt").ToString(),
                                modifiedAt = reader.IsDBNull(Helper.GetColumnOrder(reader, "modifiedAt")) ? (String?)null : reader.GetDateTime("modifiedAt").ToString(),
                                isActive = (int)reader.GetValue<sbyte>("isActive"),
                            };

                            ret.Add(t);
                        }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }

            return ret;
        }

        public List<EntitiesModel> SearchEntities(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var ret = new List<EntitiesModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT t.* FROM entities t WHERE t.entity_name LIKE CONCAT('%',@SearchKey,'%') ORDER BY column LIMIT @Offset, @ItemsPerPage";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    cmd.Parameters.AddWithValue("@SearchKey", searchKey);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new EntitiesModel()
                            {
                                entity_id = reader.GetValue<Int32>("entity_id"),
                                entity_name = reader.GetValue<String>("entity_name"),
                                createdBy = reader.IsDBNull(Helper.GetColumnOrder(reader, "createdBy")) ? (String?)null : reader.GetString("createdBy"),
                                modifiedBy = reader.IsDBNull(Helper.GetColumnOrder(reader, "modifiedBy")) ? (String?)null : reader.GetString("modifiedBy"),
                                createdAt = reader.IsDBNull(Helper.GetColumnOrder(reader, "createdAt")) ? (String?)null : reader.GetDateTime("createdAt").ToString(),
                                modifiedAt = reader.IsDBNull(Helper.GetColumnOrder(reader, "modifiedAt")) ? (String?)null : reader.GetDateTime("modifiedAt").ToString(),
                                isActive = (int)reader.GetValue<sbyte>("isActive"),
                            };

                            ret.Add(t);
                        }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return ret;
        }

        public EntitiesModel GetEntitiesByID(int entity_id)
        {

            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM entities t  WHERE t.entity_id= @entity_id";
                    cmd.Parameters.AddWithValue("@entity_id", entity_id);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return new EntitiesModel()
                            {
                                entity_id = reader.GetValue<Int32>("entity_id"),
                                entity_name = reader.GetValue<String>("entity_name"),
                                createdBy = reader.IsDBNull(Helper.GetColumnOrder(reader, "createdBy")) ? (String?)null : reader.GetString("createdBy"),
                                modifiedBy = reader.IsDBNull(Helper.GetColumnOrder(reader, "modifiedBy")) ? (String?)null : reader.GetString("modifiedBy"),
                                createdAt = reader.IsDBNull(Helper.GetColumnOrder(reader, "createdAt")) ? (String?)null : reader.GetDateTime("createdAt").ToString(),
                                modifiedAt = reader.IsDBNull(Helper.GetColumnOrder(reader, "modifiedAt")) ? (String?)null : reader.GetDateTime("modifiedAt").ToString(),
                                isActive = (int)reader.GetValue<sbyte>("isActive"),
                            };
                        }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return null;
        }
        public EntitiesModel GetEntitiesByName(string entity_name)
        {

            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM entities t  WHERE t.entity_name= @entity_name";
                    cmd.Parameters.AddWithValue("@entity_name", entity_name);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return new EntitiesModel()
                            {
                                entity_id = reader.GetValue<Int32>("entity_id"),
                                entity_name = reader.GetValue<String>("entity_name"),
                                createdBy = reader.IsDBNull(Helper.GetColumnOrder(reader, "createdBy")) ? (String?)null : reader.GetString("createdBy"),
                                modifiedBy = reader.IsDBNull(Helper.GetColumnOrder(reader, "modifiedBy")) ? (String?)null : reader.GetString("modifiedBy"),
                                createdAt = reader.IsDBNull(Helper.GetColumnOrder(reader, "createdAt")) ? (String?)null : reader.GetDateTime("createdAt").ToString(),
                                modifiedAt = reader.IsDBNull(Helper.GetColumnOrder(reader, "modifiedAt")) ? (String?)null : reader.GetDateTime("modifiedAt").ToString(),
                                isActive = (int)reader.GetValue<sbyte>("isActive"),
                            };
                        }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return null;
        }


        public bool UpdateEntities(EntitiesModel model)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE entities SET modifiedAt=@modifiedAt,modifiedBy=@modifiedBy,entity_id=@entity_id,entity_name=@entity_name WHERE entity_id=@entity_id";
                    cmd.Parameters.AddWithValue("@entity_id", model.entity_id);
                    cmd.Parameters.AddWithValue("@entity_name", model.entity_name);
                    //cmd.Parameters.AddWithValue("@createdAt", model.createdAt);
                    //cmd.Parameters.AddWithValue("@createdBy", model.createdBy);
                    cmd.Parameters.AddWithValue("@modifiedAt", model.modifiedAt);
                    cmd.Parameters.AddWithValue("@modifiedBy", model.modifiedBy);
                    var recs = cmd.ExecuteNonQuery();
                    if (recs > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return false;
        }

        public long AddEntities(EntitiesModel model)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO entities (createdAt,createdBy,modifiedAt,modifiedBy,entity_id,entity_name) Values (@createdAt,@createdBy,@modifiedAt,@modifiedBy,@entity_id,@entity_name);";
                    cmd.Parameters.AddWithValue("@entity_id", model.entity_id);
                    cmd.Parameters.AddWithValue("@entity_name", model.entity_name);
                    cmd.Parameters.AddWithValue("@createdAt", model.createdAt);
                    cmd.Parameters.AddWithValue("@createdBy", model.createdBy);
                    cmd.Parameters.AddWithValue("@modifiedAt", model.modifiedAt);
                    cmd.Parameters.AddWithValue("@modifiedBy", model.modifiedBy);
                    var recs = cmd.ExecuteNonQuery();
                    if (recs == 1)
                    {
                        return cmd.LastInsertedId;
                    }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return -1;

        }

        public bool DeleteEntities(int entity_id)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"DELETE FROM entities Where entity_id=@entity_id";
                    cmd.Parameters.AddWithValue("@entity_id", entity_id);
                    var recs = cmd.ExecuteNonQuery();
                    if (recs > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return false;
        }
        public List<EntitiesModel> FilterEntities(List<FilterModel> filterBy, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy)
        {
            var ret = new List<EntitiesModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM Entities t {filterColumns} ORDER BY column LIMIT @Offset, @ItemsPerPage ";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);

                    if (filterBy != null && filterBy.Count > 0)
                    {
                        var whereClause = string.Empty;
                        int paramCount = 0;
                        foreach (var r in filterBy)
                        {
                            if (!string.IsNullOrEmpty(r.ColumnName))
                            {
                                paramCount++;
                                if (!string.IsNullOrEmpty(whereClause))
                                {
                                    whereClause = whereClause + " " + andOr + " ";
                                }
                                whereClause = whereClause + "t." + r.ColumnName + " " + UtilityCommon.ConvertFilterToSQLString(r.ColumnCondition) + " @" + r.ColumnName + paramCount;
                            }
                        }
                        whereClause = whereClause.Trim();
                        cmd.CommandText = cmd.CommandText.Replace("{filterColumns}", "Where " + whereClause);
                    }
                    else
                    {
                        cmd.CommandText = cmd.CommandText.Replace("{filterColumns}", "");
                    }
                    if (orderBy != null && orderBy.Count > 0)
                    {
                        cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    }
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.CommandText = cmd.CommandText.Replace("@Offset", $"{offset}");
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    cmd.CommandText = cmd.CommandText.Replace("@ItemsPerPage", $"{itemsPerPage}");

                    if (filterBy != null && filterBy.Count > 0)
                    {
                        int paramCount = 0;
                        foreach (var r in filterBy)
                        {
                            paramCount++;
                            if (!string.IsNullOrEmpty(r.ColumnName))
                            {
                                // Console.WriteLine("Before cmd is : " + cmd.CommandText);
                                // whereClause = whereClause.Replace("@" + r.ColumnName + paramCount, r.ColumnValue);
                                // Console.WriteLine("Replace " + "@" + r.ColumnName + paramCount + " With " + r.ColumnValue);
                                cmd.Parameters.AddWithValue("@" + r.ColumnName + paramCount, r.ColumnValue);
                                cmd.CommandText = cmd.CommandText.Replace("@" + r.ColumnName + paramCount, $"'{r.ColumnValue}'");
                            }
                        }
                    }
                    Console.WriteLine("==================================================");
                    Console.WriteLine("SQL Command : " + cmd.CommandText);
                    Console.WriteLine("==================================================");

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new EntitiesModel()
                            {
                                entity_id = reader.GetValue<Int32>("entity_id"),
                                entity_name = reader.GetValue<String>("entity_name"),
                                createdBy = reader.IsDBNull(Helper.GetColumnOrder(reader, "createdBy")) ? (String?)null : reader.GetString("createdBy"),
                                modifiedBy = reader.IsDBNull(Helper.GetColumnOrder(reader, "modifiedBy")) ? (String?)null : reader.GetString("modifiedBy"),
                                createdAt = reader.IsDBNull(Helper.GetColumnOrder(reader, "createdAt")) ? (String?)null : reader.GetDateTime("createdAt").ToString(),
                                modifiedAt = reader.IsDBNull(Helper.GetColumnOrder(reader, "modifiedAt")) ? (String?)null : reader.GetDateTime("modifiedAt").ToString(),
                                isActive = (int)reader.GetValue<sbyte>("isActive"),
                            };

                            ret.Add(t);
                        }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return ret;
        }

        public int GetFilterTotalRecordEntities(List<FilterModel> filterBy, string andOr)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalRecord FROM Entities t {filterColumns}";
                    // cmd.CommandText = @"{filterCountQuery}";
                    if (filterBy != null && filterBy.Count > 0)
                    {
                        int paramCount = 0;
                        var whereClause = string.Empty;
                        foreach (var r in filterBy)
                        {
                            if (!string.IsNullOrEmpty(r.ColumnName))
                            {
                                paramCount++;
                                if (!string.IsNullOrEmpty(whereClause))
                                {
                                    whereClause = whereClause + " " + andOr + " ";
                                }
                                whereClause = whereClause + "t." + r.ColumnName + " " + UtilityCommon.ConvertFilterToSQLString(r.ColumnCondition) + " @" + r.ColumnName + paramCount;
                                // cmd.Parameters.AddWithValue("@" + r.ColumnName + paramCount, r.ColumnValue);
                                // Console.WriteLine("whereClause is : " + whereClause);
                                // whereClause = whereClause.Replace("@" + r.ColumnName + paramCount, r.ColumnValue);
                                // Console.WriteLine("Replace " + "@" + r.ColumnName + paramCount + " With " + r.ColumnValue);
                            }
                        }
                        whereClause = whereClause.Trim();
                        cmd.CommandText = cmd.CommandText.Replace("{filterColumns}", "Where " + whereClause);
                    }
                    else
                    {
                        cmd.CommandText = cmd.CommandText.Replace("{filterColumns}", "");
                    }


                    if (filterBy != null && filterBy.Count > 0)
                    {
                        int paramCount = 0;
                        foreach (var r in filterBy)
                        {
                            paramCount++;
                            if (!string.IsNullOrEmpty(r.ColumnName))
                            {
                                // Console.WriteLine("Before cmd is : " + cmd.CommandText);
                                // whereClause = whereClause.Replace("@" + r.ColumnName + paramCount, r.ColumnValue);
                                // Console.WriteLine("Replace " + "@" + r.ColumnName + paramCount + " With " + r.ColumnValue);
                                cmd.Parameters.AddWithValue("@" + r.ColumnName + paramCount, r.ColumnValue);
                                cmd.CommandText = cmd.CommandText.Replace("@" + r.ColumnName + paramCount, $"'{r.ColumnValue}'");
                            }
                        }
                    }

                    Console.WriteLine("==================================================");
                    Console.WriteLine("SQL Command : " + cmd.CommandText);
                    Console.WriteLine("==================================================");


                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            Console.WriteLine("==================================================");
                            Console.WriteLine("reader.GetInt32('TotalRecord') : " + reader.GetInt32("TotalRecord"));
                            Console.WriteLine("==================================================");

                            return reader.GetInt32("TotalRecord");
                        }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return 0;
        }
        public bool DeleteMultipleEntities(List<DeleteMultipleModel> deleteParam, string andOr)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    MySqlTransaction transaction = connection.BeginTransaction();
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"DELETE FROM Entities Where";
                    int count = 0;

                    foreach (var r in deleteParam)
                    {
                        if (count == 0)
                        {
                            cmd.CommandText = cmd.CommandText + " " + r.ColumnName + "=@" + r.ColumnName;
                        }
                        else
                        {
                            cmd.CommandText = cmd.CommandText + " " + andOr + " " + r.ColumnName + "=@" + r.ColumnName;
                        }
                        cmd.Parameters.AddWithValue("@" + r.ColumnName, r.ColumnValue);
                        count++;
                    }
                    var recs = cmd.ExecuteNonQuery();
                    if (recs > 0)
                    {
                        transaction.Commit();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            finally { mySqlDatabaseConnector.CloseConnection(); }
            return false;
        }
    }
}
