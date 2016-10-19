using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoSqlHelper
    {
        public static int ExecuteNonQueryEx(this IDbCommand command)
        {
            try
            {
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error executing '{0}' sql".FormatString(command.CommandText), ex);
            }
        }

        public static object ExecuteScalarEx(this IDbCommand command)
        {
            return null;
        }

        public static IDataReader ExecuteReaderEx(this IDbCommand command)
        {
            return null;

        }

        public static IDataReader ExecuteReaderEx(this IDbCommand command, CommandBehavior behavior)
        {
            return null;

        }

        public static bool IsSELECTStatement(string sql)
        {
            if (sql.IsNullOrWhiteSpace())
                throw new ApplicationException("Missing sql/table name.");

            if (sql.Trim().StartsWith("SELECT ", StringComparison.CurrentCultureIgnoreCase))
                return true;
            else
                return false;
        }

        public static string GetTableNameFromSql(string sql)
        {
            if (IsSELECTStatement(sql))
            {
                int fromTokenIndex = sql.IndexOf("FROM ", StringComparison.CurrentCultureIgnoreCase) + "FROM ".Length;
                return sql.Substring(fromTokenIndex);
            }
            else
                throw new ApplicationException("Not a valid SELECT statement.");
        }

        public static string GetKeyFieldNames(string fields)
        {
            int firstComma = fields.IndexOf(",");
            if (firstComma <= 0)
                throw new ApplicationException("Not a valid SELECT statement.");
            else
                return fields.Substring(0, firstComma);
        }

        public static string GetFieldNamesText(string sql)
        {
            StringBuilder sqlFieldNames = new StringBuilder();

            foreach (string fieldName in GetFieldNamesFromSql(sql))
            {
                if (sqlFieldNames.Length > 0)
                {
                    sqlFieldNames.Append(", ");
                }
                if (fieldName.StartsWith("[") && fieldName.EndsWith("]"))
                    sqlFieldNames.Append("{0}".FormatString(fieldName));
                else
                    sqlFieldNames.Append("[{0}]".FormatString(fieldName));
            }

            return sqlFieldNames.ToString();
        }

        public static IEnumerable<string> GetFieldNamesFromSql(string sql)
        {
            if (IsSELECTStatement(sql))
            {
                int selectTokenIndex = sql.IndexOf("SELECT ", StringComparison.CurrentCultureIgnoreCase) + "SELECT ".Length;
                sql = sql.Substring(selectTokenIndex);

                int fromTokenIndex = sql.IndexOf("FROM ", StringComparison.CurrentCultureIgnoreCase);

                return sql.Substring(0, fromTokenIndex).SplitNTrim();
            }
            else
            {
                throw new ApplicationException("Not a valid SELECT statement.");
            }
        }

        public static bool IsSimpleSELECTStatement(string sql)
        {
            int index = sql.IndexOf("FROM ", StringComparison.CurrentCultureIgnoreCase);
            if (index <= 0) return false;

            return !sql.Substring(index + "FROM ".Length).Contains(" ");
        }
    }
}
