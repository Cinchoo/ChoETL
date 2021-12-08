using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoETLSqliteSettings
    {
        public static readonly ChoETLSqliteSettings Instance = new ChoETLSqliteSettings();

        public string DatabaseFilePath { get; set; } = "local.db";
        public string ConnectionString { get; set; }
        public string TableName { get; set; } = "TmpTable";
        public long NotifyAfter { get; set; }
        public long BatchSize { get; set; }
        public Action<string> Log { get; set; } = Console.WriteLine;
        public TraceSwitch TraceSwitch { get; set; } = new TraceSwitch(nameof(ChoETLSqliteSettings), nameof(ChoETLSqliteSettings), "Verbose");
        public bool TurnOnTransaction { get; set; } = true;
        public Dictionary<Type, string> DBColumnDataTypeMapper { get; set; } = ChoSqliteTableHelper.DBColumnDataTypeMapper.Value;

        public event EventHandler<ChoRowsUploadedEventArgs> RowsUploaded;

        public void Validate()
        {
            if (DatabaseFilePath.IsNullOrWhiteSpace())
                throw new ArgumentNullException("DatabaseFilePath");
        }

        internal string GetDatabaseFilePath()
        {
            if (ConnectionString.IsNullOrWhiteSpace())
                return DatabaseFilePath;
            else
                return ConnectionString.ToKeyValuePairs().Where(kvp => kvp.Key == "DataSource").Select(kvp => kvp.Value).FirstOrDefault();
        }
        internal string GetConnectionString()
        {
            if (ConnectionString.IsNullOrWhiteSpace())
                return $"DataSource={DatabaseFilePath}";
            else
                return ConnectionString;
        }
        internal bool RaisedRowsUploaded(long noOfRowsUploaded)
        {
            EventHandler<ChoRowsUploadedEventArgs> rowsUploaded = RowsUploaded;
            if (rowsUploaded == null)
                return false;

            var ea = new ChoRowsUploadedEventArgs(noOfRowsUploaded);
            rowsUploaded(null, ea);
            return ea.Abort;
        }
        internal void WriteLog(bool condition, string msg)
        {
            Action<string> log = Log;
            if (condition && log != null)
                log(msg);
        }
        internal void WriteLog(string msg)
        {
            WriteLog(TraceSwitch.TraceVerbose, msg);
        }

        #region Fluent API

        public ChoETLSqliteSettings WithDatabaseFilePath(string databaseFilePath)
        {
            DatabaseFilePath = databaseFilePath;
            return this;
        }

        public ChoETLSqliteSettings WithConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
            return this;
        }

        public ChoETLSqliteSettings WithTableName(string tableName)
        {
            TableName = tableName;
            return this;
        }

        public ChoETLSqliteSettings WithNotifyAfter(long notifyAfter)
        {
            NotifyAfter = notifyAfter;
            return this;
        }

        public ChoETLSqliteSettings WithBatchSize(long batchSize)
        {
            BatchSize = batchSize;
            return this;
        }

        public ChoETLSqliteSettings OnRowsUploaded(Action<object, ChoRowsUploadedEventArgs> rowsUploaded)
        {
            RowsUploaded += (o, e) => rowsUploaded(o, e);
            return this;
        }

        public ChoETLSqliteSettings Configure(Action<ChoETLSqliteSettings> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion
    }
    public class ChoRowsUploadedEventArgs : EventArgs
    {
        public ChoRowsUploadedEventArgs(long rowsUploaded)
        {
            RowsUploaded = rowsUploaded;
        }

        public bool Abort { get; set; }
        public long RowsUploaded { get; }
    }

    public static class ChoSqliteTableHelper
    {
        public static readonly Lazy<Dictionary<Type, string>> DBColumnDataTypeMapper = new Lazy<Dictionary<Type, string>>(() =>
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
    }
}
