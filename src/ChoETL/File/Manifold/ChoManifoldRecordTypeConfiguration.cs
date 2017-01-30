using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoManifoldRecordTypeConfiguration
    {
        public int StartIndex
        {
            get;
            set;
        }

        public int Size
        {
            get;
            set;
        }

        private readonly Dictionary<string, Type> _recordTypeCodes = new Dictionary<string, Type>();
        public Type this[string recordTypeCode]
        {
            get
            {
                ChoGuard.ArgumentNotNullOrEmpty(recordTypeCode, "RecordTypeCode");
                if (recordTypeCode.Length != Size)
                    throw new ArgumentException($"Invalid record type code [{recordTypeCode}] passed. Expected of '{Size}' length.");

                if (_recordTypeCodes.ContainsKey(recordTypeCode))
                    return _recordTypeCodes[recordTypeCode];
                else
                    return null;
            }
            set
            {
                ChoGuard.ArgumentNotNullOrEmpty(recordTypeCode, "RecordTypeCode");
                if (recordTypeCode.Length != Size)
                    throw new ArgumentException($"Invalid record type code [{recordTypeCode}] passed. Expected of '{Size}' length.");

                if (_recordTypeCodes.ContainsKey(recordTypeCode))
                    _recordTypeCodes[recordTypeCode] = value;
                else
                    _recordTypeCodes.Add(recordTypeCode, value);
            }
        }
    }
}
