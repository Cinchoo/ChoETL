using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoETLSqliteSettings
    {
        public static readonly ChoETLSqliteSettings Instance = new ChoETLSqliteSettings();

        public string DatabaseFilePath = "local.db";
        public string TableName = null;
        public Dictionary<Type, string> ColumnDataMapper = ChoSqlTableHelper.ColumnDataMapper.Value;

        public void Validate()
        {
            if (DatabaseFilePath.IsNullOrWhiteSpace())
                throw new ArgumentNullException("DatabaseFilePath");
        }
    }
}
