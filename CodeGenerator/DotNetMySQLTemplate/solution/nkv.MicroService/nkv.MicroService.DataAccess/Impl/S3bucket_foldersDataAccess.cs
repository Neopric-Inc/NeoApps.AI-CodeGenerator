using MySql.Data.MySqlClient;
using nkv.MicroService.DataAccess.Interface;
using nkv.MicroService.Model;
using nkv.MicroService.Utility;
using System.Collections.Generic;
using System;
using System.Globalization;

namespace nkv.MicroService.DataAccess.Impl
{
    public class S3bucket_foldersDataAccess : IS3bucket_foldersDataAccess
    {
        private MySqlDatabaseConnector mySqlDatabaseConnector { get; set; }
        public S3bucket_foldersDataAccess(MySqlDatabaseConnector _mySqlDatabaseConnector)
        {
            mySqlDatabaseConnector = _mySqlDatabaseConnector;
        }
        public int GetAllTotalRecordS3bucket_folders()
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalCount FROM s3bucket_folders t WHERE t.isActive=1";
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
        public int GetAllTotalRecordS3bucket_foldersByCreatedBy(string ownername)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalCount FROM s3bucket_folders t WHERE t.isActive=1 AND t.createdBy=@ownername";
                    cmd.Parameters.AddWithValue("@ownername", ownername);
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
        public int GetSearchTotalRecordS3bucket_folders(string searchKey)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalCount FROM s3bucket_folders t WHERE t.isActive=1 AND t.folder_name LIKE CONCAT('%',@SearchKey,'%') OR t.modifiedBy LIKE CONCAT('%',@SearchKey,'%') OR t.createdBy LIKE CONCAT('%',@SearchKey,'%')";
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
        public int GetSearchTotalRecordS3bucket_foldersByCreatedBy(string ownername, string searchKey)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalCount FROM s3bucket_folders t WHERE t.isActive=1 AND t.createdBy=@ownername AND t.folder_name LIKE CONCAT('%',@SearchKey,'%') OR t.modifiedBy LIKE CONCAT('%',@SearchKey,'%') OR t.createdBy LIKE CONCAT('%',@SearchKey,'%')";
                    cmd.Parameters.AddWithValue("@SearchKey", searchKey);
                    cmd.Parameters.AddWithValue("@ownername", ownername);
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
        public List<S3bucket_foldersModel> GetAllS3bucket_folders(int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var ret = new List<S3bucket_foldersModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM s3bucket_folders t  WHERE t.isActive=1 ORDER BY column LIMIT @Offset, @ItemsPerPage";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new S3bucket_foldersModel()
                            {
                                folder_id = reader.GetValue<Int32>("folder_id"),
                                folder_name = reader.GetValue<String>("folder_name"),
                                modifiedBy = reader.GetValue<String>("modifiedBy"),
                                createdBy = reader.GetValue<String>("createdBy"),
                                modifiedAt = reader.GetValue<DateTime>("modifiedAt").ToString(),
                                createdAt = reader.GetValue<DateTime>("createdAt").ToString(),
                                isActive = reader.GetValue<SByte>("isActive"),
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

        public List<S3bucket_foldersModel> GetAllS3bucket_foldersByCreatedBy(string ownername, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var ret = new List<S3bucket_foldersModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM s3bucket_folders t  WHERE t.isActive=1 AND t.createdBy=@ownername ORDER BY column LIMIT @Offset, @ItemsPerPage";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@ownername", ownername);
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new S3bucket_foldersModel()
                            {
                                folder_id = reader.GetValue<Int32>("folder_id"),
                                folder_name = reader.GetValue<String>("folder_name"),
                                modifiedBy = reader.GetValue<String>("modifiedBy"),
                                createdBy = reader.GetValue<String>("createdBy"),
                                modifiedAt = reader.GetValue<DateTime>("modifiedAt").ToString(),
                                createdAt = reader.GetValue<DateTime>("createdAt").ToString(),
                                isActive = reader.GetValue<SByte>("isActive"),
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
        public List<S3bucket_foldersModel> SearchS3bucket_folders(string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var ret = new List<S3bucket_foldersModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT t.* FROM s3bucket_folders t WHERE t.isActive=1 AND t.folder_name LIKE CONCAT('%',@SearchKey,'%') OR t.modifiedBy LIKE CONCAT('%',@SearchKey,'%') OR t.createdBy LIKE CONCAT('%',@SearchKey,'%') ORDER BY column LIMIT @Offset, @ItemsPerPage";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    cmd.Parameters.AddWithValue("@SearchKey", searchKey);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new S3bucket_foldersModel()
                            {
                                folder_id = reader.GetValue<Int32>("folder_id"),
                                folder_name = reader.GetValue<String>("folder_name"),
                                modifiedBy = reader.GetValue<String>("modifiedBy"),
                                createdBy = reader.GetValue<String>("createdBy"),
                                modifiedAt = reader.GetValue<DateTime>("modifiedAt").ToString(),
                                createdAt = reader.GetValue<DateTime>("createdAt").ToString(),
                                isActive = reader.GetValue<SByte>("isActive"),
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
        public List<S3bucket_foldersModel> SearchS3bucket_foldersByCreatedBy(string ownername, string searchKey, int page = 1, int itemsPerPage = 100, List<OrderByModel> orderBy = null)
        {
            var ret = new List<S3bucket_foldersModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT t.* FROM s3bucket_folders t WHERE t.isActive=1 AND t.createdBy=@ownername AND t.folder_name LIKE CONCAT('%',@SearchKey,'%') OR t.modifiedBy LIKE CONCAT('%',@SearchKey,'%') OR t.createdBy LIKE CONCAT('%',@SearchKey,'%') ORDER BY column LIMIT @Offset, @ItemsPerPage";
                    cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    cmd.Parameters.AddWithValue("@SearchKey", searchKey);
                    cmd.Parameters.AddWithValue("@ownername", ownername);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            var t = new S3bucket_foldersModel()
                            {
                                folder_id = reader.GetValue<Int32>("folder_id"),
                                folder_name = reader.GetValue<String>("folder_name"),
                                modifiedBy = reader.GetValue<String>("modifiedBy"),
                                createdBy = reader.GetValue<String>("createdBy"),
                                modifiedAt = reader.GetValue<DateTime>("modifiedAt").ToString(),
                                createdAt = reader.GetValue<DateTime>("createdAt").ToString(),
                                isActive = reader.GetValue<SByte>("isActive"),
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
        public S3bucket_foldersModel GetS3bucket_foldersByID(int folder_id)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM s3bucket_folders t  WHERE t.isActive=1 AND t.folder_id= @folder_id";
                    cmd.Parameters.AddWithValue("@folder_id", folder_id);

                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return new S3bucket_foldersModel()
                            {
                                folder_id = reader.GetValue<Int32>("folder_id"),
                                folder_name = reader.GetValue<String>("folder_name"),
                                modifiedBy = reader.GetValue<String>("modifiedBy"),
                                createdBy = reader.GetValue<String>("createdBy"),
                                modifiedAt = reader.GetValue<DateTime>("modifiedAt").ToString(),
                                createdAt = reader.GetValue<DateTime>("createdAt").ToString(),
                                isActive = reader.GetValue<SByte>("isActive"),
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

        public S3bucket_foldersModel GetS3bucket_foldersByIDByCreatedBy(string ownername, int folder_id)
        {

            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM s3bucket_folders t  WHERE t.isActive=1 AND t.createdBy=@ownername AND t.folder_id= @folder_id";
                    cmd.Parameters.AddWithValue("@folder_id", folder_id);
                    cmd.Parameters.AddWithValue("@ownername", ownername);
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                        {
                            return new S3bucket_foldersModel()
                            {
                                folder_id = reader.GetValue<Int32>("folder_id"),
                                folder_name = reader.GetValue<String>("folder_name"),
                                modifiedBy = reader.GetValue<String>("modifiedBy"),
                                createdBy = reader.GetValue<String>("createdBy"),
                                modifiedAt = reader.GetValue<DateTime>("modifiedAt").ToString(),
                                createdAt = reader.GetValue<DateTime>("createdAt").ToString(),
                                isActive = reader.GetValue<SByte>("isActive"),
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






        public bool UpdateS3bucket_folders(S3bucket_foldersModel model)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    int lastUnderscoreIndex = model.folder_name.LastIndexOf('_');
                    if (lastUnderscoreIndex == -1 || !DateTime.TryParseExact(model.folder_name.Substring(lastUnderscoreIndex + 1), "yyyyMMddHHmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                    {
                        // If no valid timestamp found, generate and append one
                        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
                        model.folder_name = $"{model.folder_name}_{timestamp}";
                    }
                    cmd.CommandText = @"UPDATE s3bucket_folders SET folder_id=@folder_id,folder_name=@folder_name,modifiedBy=@modifiedBy,modifiedAt=@modifiedAt,isActive=@isActive WHERE folder_id=@folder_id";
                    cmd.Parameters.AddWithValue("@folder_id", model.folder_id);
                    cmd.Parameters.AddWithValue("@folder_name", model.folder_name);
                    cmd.Parameters.AddWithValue("@modifiedBy", model.modifiedBy);
                    cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@isActive", 1);
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

        public long AddS3bucket_folders(S3bucket_foldersModel model)
        {
            try
            {

                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    // Generate a timestamp with milliseconds
                    string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

                    // Concatenate the timestamp with the folder_name using an underscore
                    string folderNameWithTimestamp = $"{model.folder_name}_{timestamp}";
                    model.folder_name = folderNameWithTimestamp;
                    cmd.CommandText = @"INSERT INTO s3bucket_folders (folder_id,folder_name,modifiedBy,createdBy,modifiedAt,createdAt,isActive) Values (@folder_id,@folder_name,@modifiedBy,@createdBy,@modifiedAt,@createdAt,@isActive);";
                    cmd.Parameters.AddWithValue("@folder_id", model.folder_id);
                    cmd.Parameters.AddWithValue("@folder_name", model.folder_name);
                    cmd.Parameters.AddWithValue("@modifiedBy", model.modifiedBy);
                    cmd.Parameters.AddWithValue("@createdBy", model.createdBy);
                    cmd.Parameters.AddWithValue("@modifiedAt", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("@isActive", 1);
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

        public bool DeleteS3bucket_folders(int folder_id)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"UPDATE s3bucket_folders SET isActive=0 Where folder_id=@folder_id";
                    cmd.Parameters.AddWithValue("@folder_id", folder_id);
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
        public List<S3bucket_foldersModel> FilterS3bucket_folders(string ownername, List<FilterModel> filterBy, string andOr, int page, int itemsPerPage, List<OrderByModel> orderBy)
        {
            var ret = new List<S3bucket_foldersModel>();
            int offset = (page - 1) * itemsPerPage;
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT  t.* FROM S3bucket_folders t {filterColumns} ORDER BY column LIMIT @Offset, @ItemsPerPage ";
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
                        cmd.CommandText = cmd.CommandText.Replace("{filterColumns}", "Where t.isActive=1 AND t.createdBy=@ownername AND " + whereClause);
                    }
                    else
                    {
                        cmd.CommandText = cmd.CommandText.Replace("{filterColumns}", "Where t.isActive=1 AND t.createdBy=@ownername");
                    }
                    if (orderBy != null && orderBy.Count > 0)
                    {
                        cmd.CommandText = Helper.ConverOrderListToSQL(cmd.CommandText, orderBy);
                    }
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    cmd.CommandText = cmd.CommandText.Replace("@Offset", $"{offset}");
                    cmd.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                    cmd.CommandText = cmd.CommandText.Replace("@ItemsPerPage", $"{itemsPerPage}");
                    cmd.Parameters.AddWithValue("@ownername", ownername);
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
                            var t = new S3bucket_foldersModel()
                            {
                                folder_id = reader.GetValue<Int32>("folder_id"),
                                folder_name = reader.GetValue<String>("folder_name"),
                                modifiedBy = reader.GetValue<String>("modifiedBy"),
                                createdBy = reader.GetValue<String>("createdBy"),
                                modifiedAt = reader.GetValue<DateTime>("modifiedAt").ToString(),
                                createdAt = reader.GetValue<DateTime>("createdAt").ToString(),
                                isActive = reader.GetValue<SByte>("isActive"),
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

        public int GetFilterTotalRecordS3bucket_folders(string ownername, List<FilterModel> filterBy, string andOr)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = @"SELECT count(*) TotalRecord FROM S3bucket_folders t {filterColumns}";
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
                        cmd.CommandText = cmd.CommandText.Replace("{filterColumns}", "Where t.isActive=1 AND t.createdBy=@ownername AND " + whereClause);
                    }
                    else
                    {
                        cmd.CommandText = cmd.CommandText.Replace("{filterColumns}", "Where t.isActive=1 AND t.createdBy=@ownername");
                    }

                    cmd.Parameters.AddWithValue("@ownername", ownername);
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
        public bool DeleteMultipleS3bucket_folders(List<DeleteMultipleModel> deleteParam, string andOr)
        {
            try
            {
                mySqlDatabaseConnector.OpenConnection();
                var connection = mySqlDatabaseConnector.GetConnection();
                using (var cmd = connection.CreateCommand())
                {
                    MySqlTransaction transaction = connection.BeginTransaction();
                    cmd.Transaction = transaction;
                    cmd.CommandText = @"UPDATE S3bucket_folders SET isActive=0 Where";
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
