using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChoETL
{
    [Serializable]
    public class ChoSqlBulkCopyException : ApplicationException
    {
        public ChoSqlBulkCopyException()
            : base()
        {
        }

        public ChoSqlBulkCopyException(string message)
            : base(message)
        {
        }

        public ChoSqlBulkCopyException(string message, Exception e)
            : base(message, e)
        {
        }

        protected ChoSqlBulkCopyException(SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }

        public static ChoSqlBulkCopyException New(SqlBulkCopy bcp, SqlException ex)
        {
            if (ex == null || bcp == null)
                return new ChoSqlBulkCopyException();

            if (ex.Message.Contains("Received an invalid column length from the bcp client for colid"))
            {
                string pattern = @"\d+";
                Match match = Regex.Match(ex.Message.ToString(), pattern);
                var index = Convert.ToInt32(match.Value) - 1;

                FieldInfo fi = typeof(SqlBulkCopy).GetField("_sortedColumnMappings", BindingFlags.NonPublic | BindingFlags.Instance);
                var sortedColumns = fi.GetValue(bcp);
                var items = (Object[])sortedColumns.GetType().GetField("_items", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(sortedColumns);

                FieldInfo itemdata = items[index].GetType().GetField("_metadata", BindingFlags.NonPublic | BindingFlags.Instance);
                var metadata = itemdata.GetValue(items[index]);

                var column = metadata.GetType().GetField("column", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(metadata);
                var length = metadata.GetType().GetField("length", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetValue(metadata);
                throw new ChoSqlBulkCopyException(String.Format("Column: {0} contains data with a length greater than: {1}", column, length), ex);
            }
            return new ChoSqlBulkCopyException(ex.Message, ex);
        }
    }
}
