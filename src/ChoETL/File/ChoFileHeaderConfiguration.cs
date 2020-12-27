using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public abstract class ChoFileHeaderConfiguration
    {
        [DataMember]
        public long HeaderLineAt
        {
            get;
            set;
        }
        [DataMember]
        public bool HasHeaderRecord
        {
            get;
            set;
        }
        [DataMember]
        public bool IgnoreHeader
        {
            get;
            set;
        }
        [DataMember]
        public bool IgnoreColumnsWithEmptyHeader
        {
            get;
            set;
        }
        [DataMember]
        public bool KeepColumnsWithEmptyHeader
        {
            get;
            set;
        }
        public bool? QuoteAllHeaders
        {
            get;
            set;
        }
        private bool _ignoreCase = true;
        [DataMember]
        public bool IgnoreCase
        {
            get { return _ignoreCase; }
            set
            {
                _ignoreCase = value;
                StringComparer = StringComparer.Create(_culture == null ? CultureInfo.CurrentCulture : _culture, IgnoreCase);
            }
        }
        [DataMember]
        public char? FillChar
        {
            get;
            set;
        }
        [DataMember]
        public ChoFieldValueJustification? Justification
        {
            get;
            set;
        }
        [DataMember]
        public ChoFieldValueTrimOption TrimOption
        {
            get;
            set;
        }
        [DataMember]
        public bool? Truncate
        {
            get;
            set;
        }

        internal StringComparer StringComparer
        {
            get;
            private set;
        }
        private CultureInfo _culture;

        public ChoFileHeaderConfiguration(Type recordType = null, CultureInfo culture = null)
        {
            HeaderLineAt = 0;
            HasHeaderRecord = false;
            IgnoreCase = true;
            //FillChar = ' ';
            //Justification = ChoFieldValueJustification.Left;
            TrimOption = ChoFieldValueTrimOption.Trim;
            //Truncate = false;
            _culture = culture;

            if (recordType != null)
            {
                Init(recordType);
            }
        }

        private void Init(Type recordType)
        {
            ChoFileHeaderAttribute recObjAttr = ChoType.GetAttribute<ChoFileHeaderAttribute>(recordType);
            if (recObjAttr != null)
            {
                HasHeaderRecord = true;
                IgnoreCase = recObjAttr.IgnoreCase;
                IgnoreHeader = recObjAttr.IgnoreHeader;
                IgnoreColumnsWithEmptyHeader = recObjAttr.IgnoreColumnsWithEmptyHeader;
                HeaderLineAt = recObjAttr.HeaderLineAt;
                FillChar = recObjAttr.FillCharInternal;
                Justification = recObjAttr.JustificationInternal;
                QuoteAllHeaders = recObjAttr.QuoteAllInternal;
                if (recObjAttr.TrimOptionInternal != null) TrimOption = recObjAttr.TrimOptionInternal.Value;
                Truncate = recObjAttr.TruncateInternal;
            }
        }

        internal bool IsEqual(String strA, String strB)
        {
            if (IgnoreCase)
                return String.Compare(strA, strB, IgnoreCase) == 0;
            else
                return strA == strB;
        }

        internal bool StartsWith(String strA, String strB)
        {
            if (IgnoreCase)
                return strA.StartsWith(strB, IgnoreCase, _culture);
            else
                return strA.StartsWith(strB);
        }
    }
}
