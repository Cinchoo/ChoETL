using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChoETL;
using System.IO;
using System.Dynamic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.ComponentModel;
using System.Reflection;
using System.Configuration;
using System.Data.Entity.Core.Common;
using System.Data.SqlClient;

namespace ChoETL
{
    public static class ChoETLSqlServer
    {
        public static IQueryable<T> StageOnSqlServerUsingBcp<T>(this IEnumerable<T> items, ChoETLSqlServerSettings sqlServerSettings = null)
            where T : class
        {
            if (typeof(T) == typeof(ExpandoObject))
                throw new NotSupportedException();

            Dictionary<string, PropertyInfo> PIDict = ChoType.GetProperties(typeof(T)).ToDictionary(p => p.Name);

            sqlServerSettings = ValidateSettings<T>(sqlServerSettings);
            CreateDatabaseIfLocalDb(sqlServerSettings);
            var firstItem = items.FirstOrDefault();
            if (firstItem != null)
            {
                using (var conn1 = new SqlConnection(sqlServerSettings.ConnectionString))
                {
                    conn1.Open();
                    try
                    {
                        SqlCommand command = new SqlCommand(firstItem.CreateTableScript(sqlServerSettings.ColumnDataMapper, sqlServerSettings.TableName), conn1);
                        command.ExecuteNonQuery();
                    }
                    catch { }
                    //Truncate table
                    try
                    {
                        SqlCommand command = new SqlCommand("TRUNCATE TABLE [{0}]".FormatString(sqlServerSettings.TableName), conn1);
                        command.ExecuteNonQuery();
                    }
                    catch
                    {
                        try
                        {
                            SqlCommand command = new SqlCommand("DELETE FROM [{0}]".FormatString(sqlServerSettings.TableName), conn1);
                            command.ExecuteNonQuery();
                        }
                        catch { }
                    }
                }
            }
            else
                throw new ApplicationException("Empty items found.");
            using (SqlBulkCopy bcp = new SqlBulkCopy(sqlServerSettings.ConnectionString))
            {
                bcp.DestinationTableName = sqlServerSettings.TableName;
                bcp.EnableStreaming = true;

                bcp.NotifyAfter = 10;
                //bcp.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs e)
                //{
                //    Console.WriteLine(e.RowsCopied.ToString("#,##0") + " rows copied.");
                //};
                bcp.WriteToServer(new ChoEnumerableDataReader(items));
            }

            var ctx = new ChoETLSqlServerDbContext<T>(sqlServerSettings.ConnectionString);
            var dbSet = ctx.Set<T>();
            return dbSet;
        }

        public static IQueryable<T> StageOnSqlServer<T>(this IEnumerable<T> items, ChoETLSqlServerSettings sqlServerSettings = null)
            where T : class
        {
            if (typeof(T) == typeof(ExpandoObject))
                throw new NotSupportedException();

            Dictionary<string, PropertyInfo> PIDict = ChoType.GetProperties(typeof(T)).ToDictionary(p => p.Name);

            sqlServerSettings = ValidateSettings<T>(sqlServerSettings);
            LoadDataToDb(items, sqlServerSettings, PIDict);

            var ctx = new ChoETLSqlServerDbContext<T>(sqlServerSettings.ConnectionString);
            var dbSet = ctx.Set<T>();
            return dbSet;
        }
        //public static IEnumerable<ExpandoObject> StageOnSqlServerUsingBcp(this IEnumerable<ExpandoObject> items, string conditions = null, ChoETLSqlServerSettings sqlServerSettings = null)
        //{
        //    sqlServerSettings = ValidateSettings<ExpandoObject>(sqlServerSettings);
        //    CreateDatabaseIfLocalDb(sqlServerSettings);

        //    var firstItem = items.FirstOrDefault();
        //    if (firstItem != null)
        //    {
        //        using (var conn1 = new SqlConnection(sqlServerSettings.ConnectionString))
        //        {
        //            conn1.Open();
        //            try
        //            {
        //                SqlCommand command = new SqlCommand(firstItem.CreateTableScript(sqlServerSettings.ColumnDataMapper, sqlServerSettings.TableName), conn1);
        //                command.ExecuteNonQuery();
        //            }
        //            catch { }
        //            //Truncate table
        //            try
        //            {
        //                SqlCommand command = new SqlCommand("TRUNCATE TABLE [{0}]".FormatString(sqlServerSettings.TableName), conn1);
        //                command.ExecuteNonQuery();
        //            }
        //            catch
        //            {
        //                try
        //                {
        //                    SqlCommand command = new SqlCommand("DELETE FROM [{0}]".FormatString(sqlServerSettings.TableName), conn1);
        //                    command.ExecuteNonQuery();
        //                }
        //                catch { }
        //            }
        //        }
        //    }
        //    else
        //        throw new ApplicationException("Empty items found.");

        //    using (SqlBulkCopy bcp = new SqlBulkCopy(sqlServerSettings.ConnectionString))
        //    {
        //        bcp.DestinationTableName = sqlServerSettings.TableName;
        //        bcp.EnableStreaming = true;

        //        bcp.NotifyAfter = 10;
        //        bcp.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs e)
        //        {
        //            Console.WriteLine(e.RowsCopied.ToString("#,##0") + " rows copied.");
        //        };
        //        bcp.WriteToServer(new ChoEnumerableDataReader(items));
        //    }

        //    string sql = "SELECT * FROM {0}".FormatString(sqlServerSettings.TableName);
        //    if (!conditions.IsNullOrWhiteSpace())
        //        sql += " {0}".FormatString(conditions);

        //    SqlConnection conn = new SqlConnection(sqlServerSettings.ConnectionString);
        //    conn.Open();

        //    SqlCommand command2 = new SqlCommand(sql, conn);
        //    return command2.ExecuteReader(CommandBehavior.CloseConnection).ToEnumerable<ExpandoObject>();
        //}

        public static IEnumerable<ExpandoObject> StageOnSqlServer(this IEnumerable<ExpandoObject> items, string conditions = null, ChoETLSqlServerSettings sqlServerSettings = null)
        {
            sqlServerSettings = ValidateSettings<ExpandoObject>(sqlServerSettings);
            LoadDataToDb(items, sqlServerSettings, null);

            string sql = "SELECT * FROM {0}".FormatString(sqlServerSettings.TableName);
            if (!conditions.IsNullOrWhiteSpace())
                sql += " {0}".FormatString(conditions);

            SqlConnection conn = new SqlConnection(sqlServerSettings.ConnectionString);
            conn.Open();

            SqlCommand command2 = new SqlCommand(sql, conn);
            return command2.ExecuteReader(CommandBehavior.CloseConnection).ToEnumerable<ExpandoObject>();
        }

        private static ChoETLSqlServerSettings ValidateSettings<T>(ChoETLSqlServerSettings SqlServerSettings) where T : class
        {
            if (SqlServerSettings == null)
                SqlServerSettings = ChoETLSqlServerSettings.Instance;
            else
                SqlServerSettings.Validate();

            if (typeof(T) == typeof(ExpandoObject))
                SqlServerSettings.TableName = SqlServerSettings.TableName.IsNullOrWhiteSpace() ? "Table" : SqlServerSettings.TableName;
            else
                SqlServerSettings.TableName = typeof(T).Name;

            return SqlServerSettings;
        }

        private static void LoadDataToDb<T>(IEnumerable<T> items, ChoETLSqlServerSettings sqlServerSettings, Dictionary<string, PropertyInfo> PIDict) where T : class
        {
            bool isFirstItem = true;

            CreateDatabaseIfLocalDb(sqlServerSettings);

            //Open SqlServer connection, store the data
            using (var conn = new SqlConnection(sqlServerSettings.ConnectionString))
            {
                SqlCommand insertCmd = null;
                conn.Open();

                using (var trans = conn.BeginTransaction())
                {
                    foreach (var item in items)
                    {
                        if (isFirstItem)
                        {
                            isFirstItem = false;
                            if (item != null)
                            {
                                //Create table if not exists
                                try
                                {
                                    SqlCommand command = new SqlCommand(item.CreateTableScript(sqlServerSettings.ColumnDataMapper, sqlServerSettings.TableName), conn, trans);
                                    command.ExecuteNonQuery();
                                }
                                catch { }

                                //Truncate table
                                try
                                {
                                    SqlCommand command = new SqlCommand("TRUNCATE TABLE [{0}]".FormatString(sqlServerSettings.TableName), conn, trans);
                                    command.ExecuteNonQuery();
                                }
                                catch
                                {
                                    try
                                    {
                                        SqlCommand command = new SqlCommand("DELETE FROM [{0}]".FormatString(sqlServerSettings.TableName), conn, trans);
                                        command.ExecuteNonQuery();
                                    }
                                    catch { }
                                }
                                insertCmd = CreateInsertCommand(item, sqlServerSettings.TableName, conn, trans, PIDict);
                            }
                        }

                        PopulateParams(insertCmd, item, PIDict);
                        insertCmd.ExecuteNonQuery();
                    }

                    try
                    {
                        trans.Commit();
                    }
                    catch { }
                }
            }
        }

        private static void CreateDatabaseIfLocalDb(ChoETLSqlServerSettings sqlServerSettings)
        {
            if (!sqlServerSettings.IsLocalDb)
                return;

            string dbFilePath = sqlServerSettings.DbFilePath;
            if (dbFilePath.IsNullOrWhiteSpace())
                return;

            if (File.Exists(dbFilePath))
            {
                if (String.Compare(Path.GetFileNameWithoutExtension(dbFilePath), "localdb", true) == 0)
                {
                    foreach (string dbFileName in Directory.GetFiles(Path.GetDirectoryName(dbFilePath), "localdb*.*"))
                    {
                        try
                        {
                            File.Delete(dbFileName);
                        }
                        catch { }
                    }
                }
            }

            CreateDatabase(Path.GetFileNameWithoutExtension(dbFilePath), dbFilePath, sqlServerSettings.MasterDbConnectionString);
        }
        public static bool CreateDatabase(string dbName, string dbFileName, string connectionString)
        {
            try
            {
                DetachDatabase(dbName, connectionString);
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = connection.CreateCommand();

                    cmd.CommandText = String.Format("CREATE DATABASE {0} ON (NAME = N'{0}', FILENAME = '{1}')", dbName, dbFileName);
                    cmd.ExecuteNonQuery();
                }

                if (File.Exists(dbFileName)) return true;
                else return false;
            }
            catch
            {
                throw;
            }
        }

        public static bool DetachDatabase(string dbName, string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlCommand cmd = connection.CreateCommand();
                    cmd.CommandText = String.Format("exec sp_detach_db '{0}'", dbName);
                    cmd.ExecuteNonQuery();

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
        private static SqlCommand CreateInsertCommand(object target, string tableName, SqlConnection conn, SqlTransaction trans, Dictionary<string, PropertyInfo> PIDict = null)
        {
            Type objectType = target is Type ? target as Type : target.GetType();
            StringBuilder script = new StringBuilder();

            if (target is ExpandoObject)
            {
                tableName = tableName.IsNullOrWhiteSpace() ? "Table" : tableName;
                var eo = target as IDictionary<string, Object>;
                if (eo.Count == 0)
                    throw new InvalidDataException("No properties found in expando object.");

                script.Append("INSERT INTO [" + tableName);
                script.Append("] (");

                bool isFirst = true;
                foreach (KeyValuePair<string, object> kvp in eo)
                {
                    if (isFirst)
                    {
                        script.AppendFormat("[{0}]", kvp.Key);
                        isFirst = false;
                    }
                    else
                        script.AppendFormat(", [{0}]", kvp.Key);
                }
                script.Append(") VALUES (");
                isFirst = true;
                foreach (KeyValuePair<string, object> kvp in eo)
                {
                    if (isFirst)
                    {
                        script.AppendFormat("@{0}", kvp.Key);
                        isFirst = false;
                    }
                    else
                        script.AppendFormat(", @{0}", kvp.Key);
                }
                script.AppendLine(")");
                SqlCommand command2 = new SqlCommand(script.ToString(), conn, trans);

                foreach (KeyValuePair<string, object> kvp in eo)
                {
                    command2.Parameters.AddWithValue("@{0}".FormatString(kvp.Key), kvp.Value == null ? DBNull.Value : kvp.Value);
                }

                return command2;
            }
            else
            {
                if (!ChoTypeDescriptor.GetProperties(objectType).Any())
                    throw new InvalidDataException("No properties found in '{0}' object.".FormatString(objectType.Name));

                object pv = null;
                script.Append("INSERT INTO [" + tableName);
                script.Append("] (");
                bool isFirst = true;
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(objectType))
                {
                    if (isFirst)
                    {
                        script.AppendFormat("[{0}]", pd.Name);
                        isFirst = false;
                    }
                    else
                        script.AppendFormat(", [{0}]", pd.Name);
                }
                script.Append(") VALUES (");
                isFirst = true;
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(objectType))
                {
                    if (isFirst)
                    {
                        script.AppendFormat("@{0}", pd.Name);
                        isFirst = false;
                    }
                    else
                        script.AppendFormat(", @{0}", pd.Name);
                }
                script.AppendLine(")");
                SqlCommand command2 = new SqlCommand(script.ToString(), conn, trans);
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(objectType))
                {
                    pv = PIDict[pd.Name].GetValue(target);
                    command2.Parameters.AddWithValue("@{0}".FormatString(pd.Name), pv == null ? DBNull.Value : pv);
                }

                return command2;
            }
        }

        private static void PopulateParams(SqlCommand cmd, object target, Dictionary<string, PropertyInfo> PIDict)
        {
            if (target is ExpandoObject)
            {
                var eo = target as IDictionary<string, Object>;
                foreach (KeyValuePair<string, object> kvp in eo)
                {
                    cmd.Parameters["@{0}".FormatString(kvp.Key)].Value = kvp.Value == null ? DBNull.Value : kvp.Value;
                }
            }
            else
            {
                object pv = null;
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(target.GetType()))
                {
                    pv = PIDict[pd.Name].GetValue(target);
                    cmd.Parameters["@{0}".FormatString(pd.Name)].Value = pv == null ? DBNull.Value : pv;
                }
            }
        }
    }
}

