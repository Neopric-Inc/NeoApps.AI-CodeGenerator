using MySql.Data.MySqlClient;
using nkv.MicroService.DataAccess.Interface;
using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System.Collections.Generic;
using System;

namespace nkv.MicroService.DataAccess.Impl
{
    public class PermissionmatrixDataAccess : IPermissionmatrixDataAccess
    {
        private MySqlDatabaseConnector mySqlDatabaseConnector { get; set; }
        public PermissionmatrixDataAccess(MySqlDatabaseConnector _mySqlDatabaseConnector)
        {
            mySqlDatabaseConnector = _mySqlDatabaseConnector;
        }
        public int GetAllTotalRecordPermissionmatrix()
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalCount FROM permissionmatrix t";
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
        public int GetSearchTotalRecordPermissionmatrix(string searchKey)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalCount FROM permissionmatrix t WHERE ";
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
        public string GetPermissionmatrixOwnerIDByUserID(int user_id)
        {

            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.owner_name FROM permissionmatrix t  WHERE t.user_id=@user_id AND t.isActive=1";
                    cmd.Parameters.AddWithValue("@user_id", user_id);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return reader.GetValue<string>("owner_name");
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

        public List<PermissionMatrixModel> GetAllPermissionmatrix(int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var ret = new List<PermissionMatrixModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM permissionmatrix t ORDER BY column LIMIT @Offset, @ItemsPerPage";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new PermissionMatrixModel()
                            {
                                permission_id = reader.GetValue<Int32>("permission_id"),
                                role_id = reader.GetValue<Int32>("role_id"),
                                entity_id = reader.GetValue<Int32>("entity_id"),
                                can_read = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_read")) ? (SByte?)null : reader.GetSByte("can_read"),
                                can_write = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_write")) ? (SByte?)null : reader.GetSByte("can_write"),
                                can_update = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_update")) ? (SByte?)null : reader.GetSByte("can_update"),
                                can_delete = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_delete")) ? (SByte?)null : reader.GetSByte("can_delete"),
                                user_id = reader.IsDBNull(Helper.GetColumnOrder(reader, "user_id")) ? (Int32?)null : reader.GetInt32("user_id"),
                                owner_name = reader.IsDBNull(Helper.GetColumnOrder(reader, "owner_name")) ? (String?)null : reader.GetString("owner_name"),

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

        public List<PermissionMatrixModel> SearchPermissionmatrix(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var ret = new List<PermissionMatrixModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT t.* FROM permissionmatrix t WHERE  ORDER BY column LIMIT @Offset, @ItemsPerPage";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    cmd.Parameters.AddWithValue("@SearchKey", searchKey);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new PermissionMatrixModel()
                            {
                                permission_id = reader.GetValue<Int32>("permission_id"),
                                role_id = reader.GetValue<Int32>("role_id"),
                                entity_id = reader.GetValue<Int32>("entity_id"),
                                can_read = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_read")) ? (SByte?)null : reader.GetSByte("can_read"),
                                can_write = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_write")) ? (SByte?)null : reader.GetSByte("can_write"),
                                can_update = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_update")) ? (SByte?)null : reader.GetSByte("can_update"),
                                can_delete = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_delete")) ? (SByte?)null : reader.GetSByte("can_delete"),
                                user_id = reader.IsDBNull(Helper.GetColumnOrder(reader, "user_id")) ? (Int32?)null : reader.GetInt32("user_id"),
                                owner_name = reader.IsDBNull(Helper.GetColumnOrder(reader, "owner_name")) ? (String?)null : reader.GetString("owner_name"),

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

        public PermissionMatrixModel GetPermissionmatrixByID(int permission_id)
        {

            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM permissionmatrix t  WHERE t.permission_id= @permission_id";
                    cmd.Parameters.AddWithValue("@permission_id", permission_id);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return new PermissionMatrixModel()
                            {
                                permission_id = reader.GetValue<Int32>("permission_id"),
                                role_id = reader.GetValue<Int32>("role_id"),
                                entity_id = reader.GetValue<Int32>("entity_id"),
                                can_read = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_read")) ? (SByte?)null : reader.GetSByte("can_read"),
                                can_write = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_write")) ? (SByte?)null : reader.GetSByte("can_write"),
                                can_update = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_update")) ? (SByte?)null : reader.GetSByte("can_update"),
                                can_delete = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_delete")) ? (SByte?)null : reader.GetSByte("can_delete"),
                                user_id = reader.IsDBNull(Helper.GetColumnOrder(reader, "user_id")) ? (Int32?)null : reader.GetInt32("user_id"),
                                owner_name = reader.IsDBNull(Helper.GetColumnOrder(reader, "owner_name")) ? (String?)null : reader.GetString("owner_name"),

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
        public PermissionMatrixModel GetPermissionmatrixByUserRoleID(int role_id, int user_id, int entity_id)
        {

            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM permissionmatrix t  WHERE t.role_id= @role_id and t.user_id=@user_id and t.entity_id=@entity_id";
                    cmd.Parameters.AddWithValue("@role_id", role_id);
                    cmd.Parameters.AddWithValue("@user_id", user_id);
                    cmd.Parameters.AddWithValue("@entity_id", entity_id);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return new PermissionMatrixModel()
                            {
                                permission_id = reader.GetValue<Int32>("permission_id"),
                                role_id = reader.GetValue<Int32>("role_id"),
                                entity_id = reader.GetValue<Int32>("entity_id"),
                                can_read = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_read")) ? (SByte?)null : reader.GetSByte("can_read"),
                                can_write = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_write")) ? (SByte?)null : reader.GetSByte("can_write"),
                                can_update = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_update")) ? (SByte?)null : reader.GetSByte("can_update"),
                                can_delete = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_delete")) ? (SByte?)null : reader.GetSByte("can_delete"),
                                user_id = reader.IsDBNull(Helper.GetColumnOrder(reader, "user_id")) ? (Int32?)null : reader.GetInt32("user_id"),
                                owner_name = reader.IsDBNull(Helper.GetColumnOrder(reader, "owner_name")) ? (String?)null : reader.GetString("owner_name"),

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

        public bool UpdatePermissionmatrix(PermissionMatrixModel model)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE permissionmatrix SET can_delete=@can_delete,can_read=@can_read,can_update=@can_update,can_write=@can_write,entity_id=@entity_id,modifiedAt=@modifiedAt,modifiedBy=@modifiedBy,owner_name=@owner_name,permission_id=@permission_id,role_id=@role_id,user_id=@user_id,isActive=@isActive WHERE permission_id=@permission_id";
                    cmd.Parameters.AddWithValue("@can_delete", model.can_delete);
                    cmd.Parameters.AddWithValue("@can_read", model.can_read);
                    cmd.Parameters.AddWithValue("@can_update", model.can_update);
                    cmd.Parameters.AddWithValue("@can_write", model.can_write);
                    //cmd.Parameters.AddWithValue("@createdAt", model.createdAt);
                    //cmd.Parameters.AddWithValue("@createdBy", model.createdBy);
                    cmd.Parameters.AddWithValue("@entity_id", model.entity_id);
                    cmd.Parameters.AddWithValue("@modifiedAt", model.modifiedAt);
                    cmd.Parameters.AddWithValue("@modifiedBy", model.modifiedBy);
                    cmd.Parameters.AddWithValue("@owner_name", model.owner_name);
                    cmd.Parameters.AddWithValue("@permission_id", model.permission_id);
                    cmd.Parameters.AddWithValue("@role_id", model.role_id);
                    cmd.Parameters.AddWithValue("@user_id", model.user_id);
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

        public long AddPermissionmatrix(PermissionMatrixModel model)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO permissionmatrix (can_delete,can_read,can_update,can_write,createdAt,createdBy,entity_id,modifiedAt,modifiedBy,owner_name,permission_id,role_id,user_id,isActive) Values (@can_delete,@can_read,@can_update,@can_write,@createdAt,@createdBy,@entity_id,@modifiedAt,@modifiedBy,@owner_name,@permission_id,@role_id,@user_id,@isActive);";
                    cmd.Parameters.AddWithValue("@can_delete", model.can_delete);
                    cmd.Parameters.AddWithValue("@can_read", model.can_read);
                    cmd.Parameters.AddWithValue("@can_update", model.can_update);
                    cmd.Parameters.AddWithValue("@can_write", model.can_write);
                    cmd.Parameters.AddWithValue("@createdAt", model.createdAt);
                    cmd.Parameters.AddWithValue("@createdBy", model.createdBy);
                    cmd.Parameters.AddWithValue("@entity_id", model.entity_id);
                    cmd.Parameters.AddWithValue("@modifiedAt", model.modifiedAt);
                    cmd.Parameters.AddWithValue("@modifiedBy", model.modifiedBy);
                    cmd.Parameters.AddWithValue("@owner_name", model.owner_name);
                    cmd.Parameters.AddWithValue("@permission_id", model.permission_id);
                    cmd.Parameters.AddWithValue("@role_id", model.role_id);
                    cmd.Parameters.AddWithValue("@user_id", model.user_id);
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

        public bool DeletePermissionmatrix(int permission_id)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"DELETE FROM permissionmatrix Where permission_id=@permission_id";
                    cmd.Parameters.AddWithValue("@permission_id", permission_id);
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
        public List<PermissionMatrixModel> FilterPermissionmatrix(List<FilterModel> filterBy, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy)
        {
            var ret = new List<PermissionMatrixModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM Permissionmatrix t {filterColumns} ORDER BY column LIMIT @Offset, @ItemsPerPage ";
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
                            var t = new PermissionMatrixModel()
                            {
                                permission_id = reader.GetValue<Int32>("permission_id"),
                                role_id = reader.GetValue<Int32>("role_id"),
                                entity_id = reader.GetValue<Int32>("entity_id"),
                                can_read = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_read")) ? (SByte?)null : reader.GetSByte("can_read"),
                                can_write = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_write")) ? (SByte?)null : reader.GetSByte("can_write"),
                                can_update = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_update")) ? (SByte?)null : reader.GetSByte("can_update"),
                                can_delete = reader.IsDBNull(Helper.GetColumnOrder(reader, "can_delete")) ? (SByte?)null : reader.GetSByte("can_delete"),
                                user_id = reader.IsDBNull(Helper.GetColumnOrder(reader, "user_id")) ? (Int32?)null : reader.GetInt32("user_id"),
                                owner_name = reader.IsDBNull(Helper.GetColumnOrder(reader, "owner_name")) ? (String?)null : reader.GetString("owner_name"),

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

        public int GetFilterTotalRecordPermissionmatrix(List<FilterModel> filterBy, string andOr)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalRecord FROM Permissionmatrix t {filterColumns}";
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
        public bool DeleteMultiplePermissionmatrix(List<DeleteMultipleModel> deleteParam, string andOr)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    MySqlTransaction transaction = connection.BeginTransaction();
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"DELETE FROM Permissionmatrix Where";
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
