using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoETLSqlServerSettings
    {
        public static readonly ChoETLSqlServerSettings Instance = new ChoETLSqlServerSettings();

        public string TableName { get; set; } = "TmpTable";
        public long NotifyAfter { get; set; }
        public long BatchSize { get; set; }
        public Action<string> Log { get; set; } = Console.WriteLine;
        public TraceSwitch TraceSwitch { get; set; } = new TraceSwitch(nameof(ChoETLSqlServerSettings), nameof(ChoETLSqlServerSettings), "Verbose");
        public bool TurnOnTransaction { get; set; } = true;
        public Dictionary<Type, string> DBColumnDataTypeMapper { get; set; } = ChoSqlTableHelper.DBColumnDataTypeMapper.Value;
        public event EventHandler<ChoRowsUploadedEventArgs> RowsUploaded;

        private string _connectionString = null;
        public string ConnectionString
        {
            get { return _connectionString; }
            set
            {
                if (value.IsNullOrWhiteSpace())
                    return;

                StringBuilder cs = new StringBuilder();
                Dictionary<string, string> kvpDict = value.ToDictionary();
                string initialCatalog = null;
                bool isLocalDb = false;
                foreach (var kvp in kvpDict)
                {
                    if (String.Compare(kvp.Key, "AttachDbFilename") == 0)
                    {
                        isLocalDb = true;
                        DbFilePath = ChoPath.GetFullPath(kvp.Value);
                    }
                    else if (String.Compare(kvp.Key, "Initial Catalog") == 0)
                    {
                        initialCatalog = kvp.Value;
                    }
                }
                if (isLocalDb && DbFilePath.IsNullOrWhiteSpace())
                    throw new ApplicationException("Missing db file path in connection string.");

                if (isLocalDb && initialCatalog.IsNullOrWhiteSpace())
                    initialCatalog = Path.GetFileNameWithoutExtension(DbFilePath);
                IsLocalDb = isLocalDb;

                foreach (var kvp in kvpDict)
                {
                    if (String.Compare(kvp.Key, "AttachDbFilename") == 0)
                    {
                    }
                    else if (String.Compare(kvp.Key, "Initial Catalog") == 0)
                    {
                    }
                    else
                    {
                        if (cs.Length > 0)
                            cs.Append(";");

                        cs.AppendFormat("{0}={1}", kvp.Key, kvp.Value);
                    }
                }
                if (cs.Length > 0)
                    cs.Append(";");
                cs.AppendFormat("{0}={1}", "Initial Catalog", initialCatalog);
                if (cs.Length > 0)
                    cs.Append(";");
                cs.AppendFormat("{0}={1}", "AttachDbFilename", DbFilePath);

                _connectionString = cs.ToString();
            }
        }

        internal string MasterDbConnectionString
        {
            get
            {
                //return @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=master;Integrated Security = True; Connect Timeout = 30";

                StringBuilder cs = new StringBuilder();
                Dictionary<string, string> kvpDict = ConnectionString.ToDictionary();
                string initialCatalog = "master";
                string dbFilePath = null;
                bool isLocalDb = false;
                foreach (var kvp in kvpDict)
                {
                    if (String.Compare(kvp.Key, "AttachDbFilename") == 0)
                    {
                        isLocalDb = true;
                        dbFilePath = ChoPath.GetFullPath(kvp.Value);
                    }
                }
                if (isLocalDb && dbFilePath.IsNullOrWhiteSpace())
                    throw new ApplicationException("Missing db file path in connection string.");

                foreach (var kvp in kvpDict)
                {
                    if (String.Compare(kvp.Key, "AttachDbFilename") == 0)
                    {
                    }
                    else if (String.Compare(kvp.Key, "Initial Catalog") == 0)
                    {
                    }
                    else
                    {
                        if (cs.Length > 0)
                            cs.Append(";");

                        cs.AppendFormat("{0}={1}", kvp.Key, kvp.Value);
                    }
                }
                if (cs.Length > 0)
                    cs.Append(";");
                cs.AppendFormat("{0}={1}", "Initial Catalog", initialCatalog);

                return cs.ToString();
            }
        }

        internal bool IsLocalDb
        {
            get;
            private set;
        }
        internal string DbFilePath
        {
            get;
            private set;
        }
        public ChoETLSqlServerSettings()
        {
            ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=localdb.mdf;Integrated Security = True; Connect Timeout = 30";
        }

        public void Validate()
        {
            if (ConnectionString.IsNullOrWhiteSpace())
                throw new ArgumentNullException("ConnectionString");
            if (TableName.IsNullOrWhiteSpace())
                throw new ArgumentNullException("TableName");
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

        public ChoETLSqlServerSettings WithTableName(string tableName)
        {
            TableName = tableName;
            return this;
        }

        public ChoETLSqlServerSettings WithNotifyAfter(long notifyAfter)
        {
            NotifyAfter = notifyAfter;
            return this;
        }

        public ChoETLSqlServerSettings WithBatchSize(long batchSize)
        {
            BatchSize = batchSize;
            return this;
        }

        public ChoETLSqlServerSettings OnRowsUploaded(Action<object, ChoRowsUploadedEventArgs> rowsUploaded)
        {
            RowsUploaded += (o, e) => rowsUploaded(o, e);
            return this;
        }

        public ChoETLSqlServerSettings Configure(Action<ChoETLSqlServerSettings> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion Fluent API
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
