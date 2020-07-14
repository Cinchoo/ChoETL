using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoRecordTypeConfiguration
    {
        public Type DefaultRecordType { get; set; }

        private readonly Dictionary<string, Type> _recordTypeCodes = new Dictionary<string, Type>();
        public bool Contains(string recordTypeCode)
        {
            return _recordTypeCodes.ContainsKey(recordTypeCode);
        }

        public Type this[string recordTypeCode]
        {
            get
            {
                ChoGuard.ArgumentNotNullOrEmpty(recordTypeCode, "RecordTypeCode");

                if (_recordTypeCodes.ContainsKey(recordTypeCode))
                    return _recordTypeCodes[recordTypeCode];
                else
                    return null;
            }
            set
            {
                ChoGuard.ArgumentNotNullOrEmpty(recordTypeCode, "RecordTypeCode");
                //if (recordTypeCode.Length != Size)
                //    throw new ArgumentException($"Invalid record type code [{recordTypeCode}] passed. Expected of '{Size}' length.");

                if (_recordTypeCodes.ContainsKey(recordTypeCode))
                    _recordTypeCodes[recordTypeCode] = value;
                else
                    _recordTypeCodes.Add(recordTypeCode, value);
            }
        }

        public void RegisterType(Type recordType)
        {
            if (recordType == null)
                return;

            ChoRecordTypeCodeAttribute attr = ChoType.GetAttribute<ChoRecordTypeCodeAttribute>(recordType);
            if (attr == null)
                return;

            if (_recordTypeCodes.ContainsKey(attr.Code))
                return;
                //throw new ChoRecordConfigurationException($"Duplicate record type '{attr.Code}' code defined in '{recordType.Name}'.");

            this[attr.Code] = recordType;
        }
    }
}
