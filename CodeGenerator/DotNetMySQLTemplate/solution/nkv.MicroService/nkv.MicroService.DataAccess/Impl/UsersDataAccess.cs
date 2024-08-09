using MySql.Data.MySqlClient;
using nkv.MicroService.DataAccess.Interface;
using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System.Collections.Generic;
using System;

namespace nkv.MicroService.DataAccess.Impl
{
    public class UsersDataAccess : IUsersDataAccess
    {
        private MySqlDatabaseConnector mySqlDatabaseConnector { get; set; }
        public UsersDataAccess(MySqlDatabaseConnector _mySqlDatabaseConnector)
        {
            mySqlDatabaseConnector = _mySqlDatabaseConnector;
        }
        public int GetAllTotalRecordUsers()
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalCount FROM users t";
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
        public int GetSearchTotalRecordUsers(string searchKey)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalCount FROM users t WHERE t.password_hash LIKE CONCAT('%',@SearchKey,'%') OR t.username LIKE CONCAT('%',@SearchKey,'%')";
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
        public List<UsersModel> GetAllUsers(int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var ret = new List<UsersModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM users t ORDER BY column LIMIT @Offset, @ItemsPerPage";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new UsersModel()
                            {
                                user_id = reader.GetValue<Int32>("user_id"),
                                username = reader.GetValue<String>("username"),
                                password_hash = reader.GetValue<String>("password_hash"),
                                role_id = reader.GetValue<Int32>("role_id"),
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

        public List<UsersModel> SearchUsers(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var ret = new List<UsersModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT t.* FROM users t WHERE t.password_hash LIKE CONCAT('%',@SearchKey,'%') OR t.username LIKE CONCAT('%',@SearchKey,'%') ORDER BY column LIMIT @Offset, @ItemsPerPage";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    cmd.Parameters.AddWithValue("@SearchKey", searchKey);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new UsersModel()
                            {
                                user_id = reader.GetValue<Int32>("user_id"),
                                username = reader.GetValue<String>("username"),
                                password_hash = reader.GetValue<String>("password_hash"),
                                role_id = reader.GetValue<Int32>("role_id"),
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

        public UsersModel GetUsersByID(int user_id)
        {

            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM users t  WHERE t.user_id= @user_id";
                    cmd.Parameters.AddWithValue("@user_id", user_id);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return new UsersModel()
                            {
                                user_id = reader.GetValue<Int32>("user_id"),
                                username = reader.GetValue<String>("username"),
                                password_hash = reader.GetValue<String>("password_hash"),
                                role_id = reader.GetValue<Int32>("role_id"),
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

        public UsersModel AuthenticateUser(string username, string password_hash)
        {

            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM users t  WHERE t.username= @username and t.password_hash=@password_hash";
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password_hash", password_hash);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return new UsersModel()
                            {
                                user_id = reader.GetValue<Int32>("user_id"),
                                username = reader.GetValue<String>("username"),
                                password_hash = reader.GetValue<String>("password_hash"),
                                role_id = reader.GetValue<Int32>("role_id"),
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


        public bool UpdateUsers(UsersModel model)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE users SET modifiedAt=@modifiedAt,modifiedBy=@modifiedBy,password_hash=@password_hash,role_id=@role_id,user_id=@user_id,username=@username,isActive=@isActive WHERE user_id=@user_id";
                    //cmd.Parameters.AddWithValue("@createdAt", model.modifiedAt);
                    //cmd.Parameters.AddWithValue("@createdBy", model.createdBy);
                    cmd.Parameters.AddWithValue("@modifiedAt", model.createdAt);
                    cmd.Parameters.AddWithValue("@modifiedBy", model.modifiedBy);
                    cmd.Parameters.AddWithValue("@password_hash", model.password_hash);
                    cmd.Parameters.AddWithValue("@role_id", model.role_id);
                    cmd.Parameters.AddWithValue("@user_id", model.user_id);
                    cmd.Parameters.AddWithValue("@username", model.username);
                    cmd.Parameters.AddWithValue("@isActive", model.isActive);
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

        public long AddUsers(UsersModel model)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO users (createdAt,createdBy,modifiedAt,modifiedBy,password_hash,role_id,user_id,username,isActive) Values (@createdAt,@createdBy,@modifiedAt,@modifiedBy,@password_hash,@role_id,@user_id,@username,@isActive);";
                    cmd.Parameters.AddWithValue("@createdAt", model.createdAt);
                    cmd.Parameters.AddWithValue("@createdBy", model.createdBy);
                    cmd.Parameters.AddWithValue("@modifiedAt", model.createdAt);
                    cmd.Parameters.AddWithValue("@modifiedBy", model.modifiedBy);
                    cmd.Parameters.AddWithValue("@password_hash", model.password_hash);
                    cmd.Parameters.AddWithValue("@role_id", model.role_id);
                    cmd.Parameters.AddWithValue("@user_id", model.user_id);
                    cmd.Parameters.AddWithValue("@username", model.username);
                    cmd.Parameters.AddWithValue("@isActive", model.isActive);
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

        public bool DeleteUsers(int user_id)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"DELETE FROM users Where user_id=@user_id";
                    cmd.Parameters.AddWithValue("@user_id", user_id);
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
        public List<UsersModel> FilterUsers(List<FilterModel> filterBy, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy)
        {
            var ret = new List<UsersModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM Users t {filterColumns} ORDER BY column LIMIT @Offset, @ItemsPerPage ";
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
                            var t = new UsersModel()
                            {
                                user_id = reader.GetValue<Int32>("user_id"),
                                username = reader.GetValue<String>("username"),
                                password_hash = reader.GetValue<String>("password_hash"),
                                role_id = reader.GetValue<Int32>("role_id"),
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

        public int GetFilterTotalRecordUsers(List<FilterModel> filterBy, string andOr)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalRecord FROM Users t {filterColumns}";
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
        public bool DeleteMultipleUsers(List<DeleteMultipleModel> deleteParam, string andOr)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    MySqlTransaction transaction = connection.BeginTransaction();
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"DELETE FROM Users Where";
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
