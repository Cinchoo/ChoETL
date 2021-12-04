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

        public string DatabaseFilePath = "local.db";
        public string ConnectionString = null;
        public string TableName = null;
        public Dictionary<Type, string> ColumnDataMapper = ChoSqlTableHelper.ColumnDataMapper.Value;
        public long NotifyAfter { get; set; }
        public event EventHandler<ChoRowsUploadedEventArgs> RowsUploaded;
        public Action<string> Log = Console.WriteLine;
        public TraceSwitch TraceSwitch = new TraceSwitch(nameof(ChoETLSqliteSettings), nameof(ChoETLSqliteSettings));
        public bool TurnOnTransaction = true;
        public long BatchSize = 0;

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
            WriteLog(true, msg);
        }
        public ChoETLSqliteSettings Configure(Action<ChoETLSqliteSettings> action)
        {
            if (action != null)
                action(this);

            return this;
        }

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
}
