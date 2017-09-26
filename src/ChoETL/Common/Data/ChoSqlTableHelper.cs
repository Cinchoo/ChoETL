using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoSqlTableHelper
    {
        public static readonly Lazy<Dictionary<Type, string>> ColumnDataMapper = new Lazy<Dictionary<Type, string>>(() =>
        {
            Dictionary<Type, String> dataMapper = new Dictionary<Type, string>();
            dataMapper.Add(typeof(int), "INT");
            dataMapper.Add(typeof(uint), "INT");
            dataMapper.Add(typeof(long), "BIGINT");
            dataMapper.Add(typeof(ulong), "BIGINT");
            dataMapper.Add(typeof(short), "SMALLINT");
            dataMapper.Add(typeof(ushort), "SMALLINT");
            dataMapper.Add(typeof(byte), "TINYINT");

            dataMapper.Add(typeof(string), "NVARCHAR(500)");
            dataMapper.Add(typeof(bool), "BIT");
            dataMapper.Add(typeof(DateTime), "DATETIME");
            dataMapper.Add(typeof(float), "FLOAT");
            dataMapper.Add(typeof(double), "FLOAT");
            dataMapper.Add(typeof(decimal), "DECIMAL(18,0)");
            dataMapper.Add(typeof(Guid), "UNIQUEIDENTIFIER");
            dataMapper.Add(typeof(TimeSpan), "TIME");
            dataMapper.Add(typeof(ChoCurrency), "MONEY");

            return dataMapper;
        });
        private static dynamic ToExpandoObject(this IDataRecord record)
        {
            var expandoObject = new ExpandoObject() as IDictionary<string, object>;

            for (var i = 0; i < record.FieldCount; i++)
                expandoObject.Add(record.GetName(i), record[i]);

            return expandoObject;
        }

        public static IEnumerable<T> ToEnumerable<T>(this IDataReader reader)
        {
            if (typeof(T) == typeof(ExpandoObject))
            {
                while (reader.Read())
                {
                    yield return ToExpandoObject(reader);
                }
            }
            else
                yield break;
        }

        public static string CreateInsertScript(this object target, string tableName = null)
        {
            Type objectType = target is Type ? target as Type : target.GetType();
            StringBuilder script = new StringBuilder();

            if (target is IDictionary<string, object>)
            {
                tableName = tableName.IsNullOrWhiteSpace() ? "Table" : tableName;
                var eo = target as IDictionary<string, Object>;

                script.Append("INSERT INTO [" + tableName);
                script.Append("](");

                bool isFirst = true;
                foreach (KeyValuePair<string, object> kvp in eo)
                {
                    if (isFirst)
                    {
                        script.Append(kvp.Key);
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
                        script.AppendFormat("{0}", kvp.Value == null ? "NULL" : "'{0}'".FormatString(kvp.Value.ToString()));
                        isFirst = false;
                    }
                    else
                        script.AppendFormat(", {0}", kvp.Value == null ? "NULL" : "'{0}'".FormatString(kvp.Value.ToString()));
                }
                script.AppendLine(")");
            }
            else
            {
            }
            return script.ToString();
        }

        public static string CreateTableScript(this object target, Dictionary<Type, string> columnDataMapper = null, string tableName = null, string keyColumns = null)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            Type objectType = target is Type ? target as Type : target.GetType();

            columnDataMapper = columnDataMapper ?? ColumnDataMapper.Value;

            StringBuilder script = new StringBuilder();

            if (target is IDictionary<string, object>)
            {
                tableName = tableName.IsNullOrWhiteSpace() ? "Table" : tableName;
                var eo = target as IDictionary<string, Object>;

                return CreateTableScript(tableName, eo.ToDictionary(kvp => kvp.Key, kvp1 => kvp1.Value.GetNType()), keyColumns.SplitNTrim(), columnDataMapper);
            }
            else
            {
                if (tableName.IsNullOrWhiteSpace())
                {
                    TableAttribute attr = TypeDescriptor.GetAttributes(objectType).OfType<TableAttribute>().FirstOrDefault();
                    if (attr != null && !attr.Name.IsNullOrWhiteSpace())
                        tableName = attr.Name;
                    else
                        tableName = objectType.Name;
                }

                string[] keyColumnArray = null;
                if (keyColumns.IsNullOrEmpty())
                    keyColumnArray = ChoTypeDescriptor.GetProperties(objectType).Where(pd => pd.Attributes.OfType<KeyAttribute>().Any()).Select(p => p.Name).ToArray();
                else
                    keyColumnArray = keyColumns.SplitNTrim();

                return CreateTableScript(tableName, ChoTypeDescriptor.GetProperties(objectType).ToDictionary(pd => pd.Name, pd => pd.PropertyType, null), keyColumnArray, columnDataMapper);
            }
        }
        private static string CreateTableScript(string tableName, Dictionary<string, Type> propDict, string[] keyColumns, Dictionary<Type, string> columnDataMapper)
        {
            int index = 0;
            Type pt = null;
            bool hasColumns = false;
            StringBuilder script = new StringBuilder();

            foreach (KeyValuePair<string, Type> kvp in propDict)
            {
                pt = kvp.Value.GetUnderlyingType();

                if (index > 0)
                {
                    script.Append(",");
                    script.Append(Environment.NewLine);
                }
                else
                {
                    script.AppendLine("CREATE TABLE [" + tableName + "](");
                    hasColumns = true;
                }

                if (columnDataMapper.ContainsKey(pt))
                {
                    script.Append($"\t[{kvp.Key}] {columnDataMapper[pt]}");
                }
                else
                {
                    script.AppendFormat("\t[{0}] {1}", kvp.Key, columnDataMapper.ContainsKey(typeof(string)) ? columnDataMapper[typeof(string)] : "NVARCHAR(500)");
                }

                index++;
            }

            if (hasColumns)
            {
                index = 0;

                //If there are key columns, add them
                foreach (string keyColumnName in keyColumns)
                {
                    if (index == 0)
                    {
                        script.Append(",");
                        script.Append(Environment.NewLine);
                        script.Append($"\tPRIMARY KEY ([{keyColumnName}]");
                    }
                    else
                    {
                        script.Append($", [{keyColumnName}]");
                    }

                    index++;
                }
                if (index > 0)
                {
                    script.AppendLine(")");
                }
                script.Append(Environment.NewLine);
                script.AppendLine(")");
            }
            return script.ToString();
        }
    }
}

