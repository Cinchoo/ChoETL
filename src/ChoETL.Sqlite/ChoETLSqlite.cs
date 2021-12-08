using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using ChoETL;
using System.IO;
using System.Dynamic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.ComponentModel;
using System.Reflection;
using System.Configuration;
using System.Data.SQLite.EF6;
using System.Data.Entity.Core.Common;
using System.Diagnostics;

namespace ChoETL
{
    public static class ChoETLSQLite
    {
        public static IQueryable<T> StageOnSQLite<T>(this IEnumerable<T> items, ChoETLSqliteSettings sqliteSettings = null)
            where T : class
        {
            Dictionary<string, PropertyInfo> PIDict = null;

            if (!typeof(T).IsDynamicType() && typeof(T) != typeof(object))
                PIDict = ChoType.GetProperties(typeof(T)).ToDictionary(p => p.Name);

            sqliteSettings = ValidateSettings<T>(sqliteSettings);
            LoadDataToDb(items, sqliteSettings, PIDict);

            if (!typeof(T).IsDynamicType() && typeof(T) != typeof(object))
            {
                var ctx = new ChoETLSQLiteDbContext<T>(sqliteSettings.GetConnectionString());
                ctx.Log = sqliteSettings.Log;
                var dbSet = ctx.Set<T>();
                return dbSet;
            }
            else
                return Enumerable.Empty<T>().AsQueryable();
        }

        public static IEnumerable<T> StageOnSQLite<T>(this IEnumerable<T> items, string conditions, ChoETLSqliteSettings sqliteSettings = null)
        {
            sqliteSettings = ValidateSettings<dynamic>(sqliteSettings);
            LoadDataToDb(items, sqliteSettings, null);

            string sql = "SELECT * FROM {0}".FormatString(sqliteSettings.TableName);
            if (!conditions.IsNullOrWhiteSpace())
                sql += " {0}".FormatString(conditions);

            SQLiteConnection conn = new SQLiteConnection(sqliteSettings.GetConnectionString());
            conn.Open();
            SQLiteCommand command2 = new SQLiteCommand(sql, conn);
            return command2.ExecuteReader(CommandBehavior.CloseConnection).ToEnumerable<T>();
        }

        private static ChoETLSqliteSettings ValidateSettings<T>(ChoETLSqliteSettings sqliteSettings) 
        {
            if (sqliteSettings == null)
                sqliteSettings = ChoETLSqliteSettings.Instance;
            else
                sqliteSettings.Validate();

            if (sqliteSettings.TableName.IsNullOrWhiteSpace())
            {
                if (typeof(T).IsDynamicType())
                    sqliteSettings.TableName = sqliteSettings.TableName.IsNullOrWhiteSpace() ? "TmpTable" : sqliteSettings.TableName;
                else
                    sqliteSettings.TableName = sqliteSettings.TableName.IsNullOrWhiteSpace() ? typeof(T).Name : sqliteSettings.TableName;
            }

            return sqliteSettings;
        }

        private static void LoadDataToDb<T>(IEnumerable<T> items, ChoETLSqliteSettings sqliteSettings, Dictionary<string, PropertyInfo> PIDict = null) 
        {
            ChoGuard.ArgumentNotNull(items, nameof(items));

            if (File.Exists(sqliteSettings.GetDatabaseFilePath()))
                File.Delete(sqliteSettings.GetDatabaseFilePath());

            SQLiteConnection.CreateFile(sqliteSettings.GetDatabaseFilePath());

            bool isFirstItem = true;
            bool isFirstBatch = true;
            long notifyAfter = sqliteSettings.NotifyAfter;
            long batchSize = sqliteSettings.BatchSize;

            sqliteSettings.WriteLog($"Opening connection to `{sqliteSettings.GetConnectionString()}`...");
            sqliteSettings.WriteLog($"Starting import...");
            //Open sqlite connection, store the data
            var conn = new SQLiteConnection(sqliteSettings.GetConnectionString());
            SQLiteCommand insertCmd = null;
            try
            {
                conn.Open();

                SQLiteTransaction trans = sqliteSettings.TurnOnTransaction ? conn.BeginTransaction() : null;
                try
                {
                    int index = 0;
                    foreach (var item in items)
                    {
                        index++;
                        if (isFirstItem)
                        {
                            isFirstItem = false;
                            if (item != null)
                            {
                                if (isFirstBatch)
                                {
                                    isFirstBatch = false;
                                    try
                                    {
                                        SQLiteCommand command = new SQLiteCommand(item.CreateTableScript(sqliteSettings.DBColumnDataTypeMapper, sqliteSettings.TableName), conn);
                                        command.ExecuteNonQuery();
                                    }
                                    catch { }

                                    //Truncate table
                                    try
                                    {
                                        SQLiteCommand command = new SQLiteCommand("DELETE FROM [{0}]".FormatString(sqliteSettings.TableName), conn, trans);
                                        command.ExecuteNonQuery();
                                    }
                                    catch { }
                                }
                                if (insertCmd != null)
                                    insertCmd.Dispose();

                                insertCmd = CreateInsertCommand(item, sqliteSettings.TableName, conn, trans, PIDict);
                                insertCmd.Prepare();
                            }
                        }

                        PopulateParams(insertCmd, item, PIDict);
                        insertCmd.ExecuteNonQuery();

                        if (notifyAfter > 0 && index % notifyAfter == 0)
                        {
                            if (sqliteSettings.RaisedRowsUploaded(index))
                            {
                                sqliteSettings.WriteLog(sqliteSettings.TraceSwitch.TraceVerbose, "Abort requested.");
                                break;
                            }
                        }

                        if (batchSize > 0 && index % batchSize == 0)
                        {
                            if (trans != null && trans.Connection != null)
                            {
                                trans.Commit();
                                trans = null;
                            }
                            if (insertCmd != null)
                                insertCmd.Dispose();

                            isFirstItem = true;
                            conn.Close();
                            conn = null;
                            conn = new SQLiteConnection(sqliteSettings.GetConnectionString());
                            conn.Open();
                            trans = sqliteSettings.TurnOnTransaction ? conn.BeginTransaction() : null;
                        }
                    }

                    if (trans != null && trans.Connection != null)
                    {
                        trans.Commit();
                        trans = null;
                    }
                }
                catch
                {
                    if (trans != null && trans.Connection != null)
                    {
                        trans.Rollback();
                        trans = null;
                    }

                    throw;
                }
                sqliteSettings.WriteLog($"Import completed successfully.");
            }
            catch (Exception ex)
            {
                sqliteSettings.WriteLog(sqliteSettings.TraceSwitch.TraceError, $"Import failed. {ex.Message}.");
                throw;
            }
            finally
            {
                if (insertCmd != null)
                    insertCmd.Dispose();
                if (conn != null)
                    conn.Close();
                System.Data.SQLite.SQLiteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        private static SQLiteCommand CreateInsertCommand(object target, string tableName, SQLiteConnection conn, SQLiteTransaction trans, Dictionary<string, PropertyInfo> PIDict)
        {
            Type objectType = target is Type ? target as Type : target.GetType();
            StringBuilder script = new StringBuilder();

            if (target.GetType().IsDynamicType())
            {
                var eo = target as IDictionary<string, Object>;
                if (eo.Count == 0)
                    throw new InvalidDataException("No properties found in expando object.");

                script.Append("INSERT INTO " + tableName);
                script.Append("(");

                bool isFirst = true;
                foreach (KeyValuePair<string, object> kvp in eo)
                {
                    if (isFirst)
                    {
                        script.Append(kvp.Key);
                        isFirst = false;
                    }
                    else
                        script.AppendFormat(", {0}", kvp.Key);
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
                SQLiteCommand command2 = trans == null ? new SQLiteCommand(script.ToString(), conn) : new SQLiteCommand(script.ToString(), conn);

                foreach (KeyValuePair<string, object> kvp in eo)
                {
                    command2.Parameters.AddWithValue($"@{kvp.Key}", kvp.Value == null ? DBNull.Value : kvp.Value);
                }

                return command2;
            }
            else
            {
                if (!ChoTypeDescriptor.GetProperties(objectType).Any())
                    throw new InvalidDataException("No properties found in '{0}' object.".FormatString(objectType.Name));

                object pv = null;
                script.Append("INSERT INTO " + tableName);
                script.Append("(");
                bool isFirst = true;
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(objectType))
                {
                    if (isFirst)
                    {
                        script.Append(pd.Name);
                        isFirst = false;
                    }
                    else
                        script.AppendFormat(", {0}", pd.Name);
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
                SQLiteCommand command2 = trans == null ? new SQLiteCommand(script.ToString(), conn) : new SQLiteCommand(script.ToString(), conn);
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(objectType))
                {
                    pv = PIDict[pd.Name].GetValue(target);
                    command2.Parameters.AddWithValue($"@{pd.Name}", pv == null ? DBNull.Value : pv);
                }

                return command2;
            }
        }

        private static void PopulateParams(SQLiteCommand cmd, object target, Dictionary<string, PropertyInfo> PIDict)
        {
            if (target.GetType().IsDynamicType())
            {
                var eo = target as IDictionary<string, Object>;
                int count = 0;
                foreach (var kvp in eo)
                {
                    cmd.Parameters[count].Value = kvp.Value == null ? DBNull.Value : kvp.Value;
                    count++;
                }
                //foreach (KeyValuePair<string, object> kvp in eo)
                //{
                //    cmd.Parameters[$"@{kvp.Key}"].Value = kvp.Value == null ? DBNull.Value : kvp.Value;
                //}
            }
            else
            {
                object pv = null;
                int count = 0;
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(target.GetType()))
                {
                    pv = PIDict[pd.Name].GetValue(target);
                    cmd.Parameters[count].Value = pv == null ? DBNull.Value : pv;
                    count++;
                }

                //foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(target.GetType()))
                //{
                //    pv = PIDict[pd.Name].GetValue(target);
                //    cmd.Parameters[$"@{pd.Name}"].Value = pv == null ? DBNull.Value : pv;
                //}
            }
        }
    }
}

