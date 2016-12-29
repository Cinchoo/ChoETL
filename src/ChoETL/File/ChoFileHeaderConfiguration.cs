using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoFileHeaderConfiguration
    {
        public bool HasHeaderRecord
        {
            get;
            set;
        }
        public bool IgnoreCase
        {
            get;
            set;
        }
        public char? FillChar
        {
            get;
            set;
        }
        public ChoFieldValueJustification? Justification
        {
            get;
            set;
        }
        public ChoFieldValueTrimOption TrimOption
        {
            get;
            set;
        }
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
            HasHeaderRecord = false;
            IgnoreCase = true;
            //FillChar = ' ';
            //Justification = ChoFieldValueJustification.Left;
            TrimOption = ChoFieldValueTrimOption.Trim;
            //Truncate = false;
            _culture = culture;
            StringComparer = StringComparer.Create(_culture == null ? CultureInfo.CurrentCulture : _culture, IgnoreCase);

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
                FillChar = recObjAttr.FillCharInternal;
                Justification = recObjAttr.JustificationInternal;
                if (recObjAttr.TrimOptionInternal != null) TrimOption = recObjAttr.TrimOptionInternal.Value;
                Truncate = recObjAttr.TruncateInternal;
            }
        }
    }
}
