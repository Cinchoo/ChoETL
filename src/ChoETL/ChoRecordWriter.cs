using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordWriter
    {
        public event EventHandler<ChoEventArgs<IDictionary<string, Type>>> MembersDiscovered;
      
        public Type RecordType
        {
            get;
            protected set;
        }
        public TraceSwitch TraceSwitch;
        public event EventHandler<ChoRowsWrittenEventArgs> RowsWritten;

        public abstract ChoRecordConfiguration RecordConfiguration
        {
            get;
        }

        static ChoRecordWriter()
        {
            ChoETLFramework.Initialize();
        }

        protected ChoRecordWriter(Type recordType, bool allowCollection = false)
        {
            ChoGuard.ArgumentNotNull(recordType, "RecordType");

            if (!allowCollection)
            {
                if (!recordType.IsDynamicType() && typeof(ICollection).IsAssignableFrom(recordType))
                    throw new ChoReaderException("Invalid recordtype passed.");
            }

            RecordType = recordType;
            TraceSwitch = ChoETLFramework.TraceSwitch;
        }

        protected bool RaisedRowsWritten(long rowsWritten, bool isFinal = false)
        {
            EventHandler<ChoRowsWrittenEventArgs> rowsWrittenEvent = RowsWritten;
            if (rowsWrittenEvent == null)
                return false;

            var ea = new ChoRowsWrittenEventArgs(rowsWritten);
            rowsWrittenEvent(this, ea);
            return ea.Abort;
        }

        protected void RaiseMembersDiscovered(IDictionary<string, Type> fieldTypes)
        {
            EventHandler<ChoEventArgs<IDictionary<string, Type>>> membersDiscovered = MembersDiscovered;
            if (membersDiscovered != null)
            {
                var ea = new ChoEventArgs<IDictionary<string, Type>>(fieldTypes);
                membersDiscovered(this, ea);
            }
            InitializeRecordFieldConfiguration(RecordConfiguration);
        }

        protected void InitializeRecordConfiguration(ChoRecordConfiguration configuration)
        {
            if (configuration == null || configuration.IsDynamicObjectInternal || configuration.RecordTypeInternal == null)
                return;

            if (!typeof(IChoNotifyRecordConfigurable).IsAssignableFrom(configuration.RecordMapTypeInternal))
                return;

            var obj = ChoActivator.CreateInstance(configuration.RecordMapTypeInternal) as IChoNotifyRecordConfigurable;
            if (obj != null)
                obj.RecondConfigure(configuration);
        }

        protected void InitializeRecordFieldConfiguration(ChoRecordConfiguration configuration)
        {
            if (configuration == null/* || configuration.IsDynamicObject || configuration.RecordType == null*/)
                return;

            if (!typeof(IChoNotifyRecordFieldConfigurable).IsAssignableFrom(configuration.RecordMapTypeInternal))
                return;

            var obj = ChoActivator.CreateInstance(configuration.RecordMapTypeInternal) as IChoNotifyRecordFieldConfigurable;
            if (obj == null)
                return;

            foreach (var fc in configuration.RecordFieldConfigurations)
                obj.RecondFieldConfigure(fc);
        }

        public abstract IEnumerable<object> WriteTo(object writer, IEnumerable<object> records, Func<object, bool> predicate = null);

        protected object GetDeclaringRecord(string declaringMember, object rec, int? arrayIndex = null, List<int> nestedArrayIndex = null)
        {
            return ChoType.GetDeclaringRecord(declaringMember, rec, arrayIndex, nestedArrayIndex);
        }
    }
}
