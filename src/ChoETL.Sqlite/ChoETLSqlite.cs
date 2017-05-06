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

namespace ChoETL
{
    public static class ChoETLSQLite
    {
        public static IQueryable<T> StageOnSQLite<T>(this IEnumerable<T> items, ChoETLSqliteSettings sqliteSettings = null)
            where T : class
        {
            // Use the default SqlCe35 provider.
            if (typeof(T) == typeof(ExpandoObject))
                throw new NotSupportedException();

            if (sqliteSettings == null)
                sqliteSettings = ChoETLSqliteSettings.Instance;
            else
                sqliteSettings.Validate();

            sqliteSettings.TableName = typeof(T).Name;

            if (File.Exists(sqliteSettings.DatabaseFilePath))
                File.Delete(sqliteSettings.DatabaseFilePath);

            SQLiteConnection.CreateFile(sqliteSettings.DatabaseFilePath);

            bool isFirstItem = true;
            Dictionary<string, PropertyInfo> PIDict = ChoType.GetProperties(typeof(T)).ToDictionary(p => p.Name);

            //Open sqlite connection, store the data
            using (var conn = new SQLiteConnection(@"DataSource={0}".FormatString(sqliteSettings.DatabaseFilePath)))
            {
                SQLiteCommand insertCmd = null;
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
                                SQLiteCommand command = new SQLiteCommand(item.CreateTableScript(sqliteSettings.ColumnDataMapper, sqliteSettings.TableName), conn);
                                command.ExecuteNonQuery();

                                insertCmd = CreateInsertCommand(item, sqliteSettings.TableName, conn, PIDict);
                            }
                        }

                        PopulateParams(insertCmd, item, PIDict);
                        insertCmd.ExecuteNonQuery();
                    }

                    trans.Commit();
                }
            }

            var ctx = new ChoETLSQLiteDbContext<T>(sqliteSettings.DatabaseFilePath);
            var dbSet = ctx.Set<T>();
            return dbSet;
        }

        public static IEnumerable<ExpandoObject> StageOnSQLite(this IEnumerable<ExpandoObject> items, string conditions = null, ChoETLSqliteSettings sqliteSettings = null)
        {
            if (sqliteSettings == null)
                sqliteSettings = ChoETLSqliteSettings.Instance;
            else
                sqliteSettings.Validate();

            if (File.Exists(sqliteSettings.DatabaseFilePath))
                File.Delete(sqliteSettings.DatabaseFilePath);

            SQLiteConnection.CreateFile(sqliteSettings.DatabaseFilePath);

            bool isFirstItem = true;

            //Open sqlite connection, store the data
            var conn = new SQLiteConnection(@"DataSource={0}".FormatString(sqliteSettings.DatabaseFilePath));
            SQLiteCommand insertCmd = null;
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
                            SQLiteCommand command = new SQLiteCommand(item.CreateTableScript(sqliteSettings.ColumnDataMapper, sqliteSettings.TableName), conn);
                            command.ExecuteNonQuery();

                            insertCmd = CreateInsertCommand(item, sqliteSettings.TableName, conn);
                        }
                    }

                    PopulateParams(insertCmd, item, null);
                    insertCmd.ExecuteNonQuery();
                }

                trans.Commit();
            }

            string sql = "SELECT * FROM {0}".FormatString(sqliteSettings.TableName);
            if (!conditions.IsNullOrWhiteSpace())
                sql += " {0}".FormatString(conditions);

            SQLiteCommand command2 = new SQLiteCommand(sql, conn);
            return command2.ExecuteReader().ToEnumerable<ExpandoObject>();
        }

        private static SQLiteCommand CreateInsertCommand(object target, string tableName, SQLiteConnection conn, Dictionary<string, PropertyInfo> PIDict = null)
        {
            Type objectType = target is Type ? target as Type : target.GetType();
            StringBuilder script = new StringBuilder();

            if (target is ExpandoObject)
            {
                tableName = tableName.IsNullOrWhiteSpace() ? "Table" : tableName;
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
                SQLiteCommand command2 = new SQLiteCommand(script.ToString(), conn);

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
                SQLiteCommand command2 = new SQLiteCommand(script.ToString(), conn);
                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(objectType))
                {
                    pv = PIDict[pd.Name].GetValue(target);
                    command2.Parameters.AddWithValue("@{0}".FormatString(pd.Name), pv == null ? DBNull.Value : pv);
                }

                return command2;
            }
        }

        private static void PopulateParams(SQLiteCommand cmd, object target, Dictionary<string, PropertyInfo> PIDict)
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

