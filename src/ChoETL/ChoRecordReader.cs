using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordReader
    {
        public readonly Type RecordType;
        internal TraceSwitch TraceSwitch;

        public ChoRecordReader(Type recordType)
        {
            ChoGuard.ArgumentNotNull(recordType, "RecordType");

            RecordType = recordType;
            TraceSwitch = ChoETLFramework.TraceSwitch;
        }

        public abstract IEnumerable<object> AsEnumerable(object source, Func<object, bool?> filterFunc = null);
        public abstract void LoadSchema(object source);

        protected T CreateCallbackRecordObject<T>(Type recordType)
            where T : class
        {
            T callbackRecord = default(T);

            try
            {
                MetadataTypeAttribute attr = recordType.GetCustomAttribute<MetadataTypeAttribute>();
                if (attr == null)
                {
                    if (typeof(T).IsAssignableFrom(recordType))
                        callbackRecord = Activator.CreateInstance(recordType) as T;
                }
                else
                {
                    if (attr.MetadataClassType != null && typeof(T).IsAssignableFrom(attr.MetadataClassType))
                        callbackRecord = Activator.CreateInstance(attr.MetadataClassType) as T;
                }
            }
            catch
            {

            }

            return callbackRecord;
        }
    }
}
