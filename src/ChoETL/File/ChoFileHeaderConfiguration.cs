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
        public char FillChar
        {
            get;
            set;
        }
        public ChoFieldValueJustification Justification
        {
            get;
            set;
        }
        public ChoFieldValueTrimOption TrimOption
        {
            get;
            set;
        }
        public bool Truncate
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
            Justification = ChoFieldValueJustification.Left;
            TrimOption = ChoFieldValueTrimOption.Trim;
            Truncate = false;
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
                //FillChar = recObjAttr.FillChar == '\0' ? ' ' : recObjAttr.FillChar;
                Justification = recObjAttr.Justification;
                TrimOption = recObjAttr.TrimOption;
                Truncate = recObjAttr.Truncate;
            }
        }

        internal void Validate(ChoRecordConfiguration config)
        {
            StringComparer = StringComparer.Create(_culture == null ? CultureInfo.CurrentCulture : _culture, IgnoreCase);

            //if (FillChar == ChoCharEx.NUL)
            //    throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FillChar));

            if (config is ChoCSVRecordConfiguration)
            {
                ChoCSVRecordConfiguration csvConfig = config as ChoCSVRecordConfiguration;

                if (csvConfig.Delimiter.Contains(FillChar))
                    throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of Delimiter characters [{1}]".FormatString(FillChar, csvConfig.Delimiter));
                if (csvConfig.EOLDelimiter.Contains(FillChar))
                    throw new ChoRecordConfigurationException("FillChar [{0}] can't be one EOLDelimiter characters [{1}]".FormatString(FillChar, csvConfig.EOLDelimiter));
                if ((from comm in csvConfig.Comments
                     where comm.Contains(FillChar.ToString())
                     select comm).Any())
                    throw new ChoRecordConfigurationException("One of the Comments contains FillChar. Not allowed.");
            }
        }
    }
}
