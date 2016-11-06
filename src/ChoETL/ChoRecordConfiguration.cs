using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoRecordConfiguration
    {
        public ChoErrorMode ErrorMode
        {
            get;
            set;
        }
        public ChoIgnoreFieldValueMode IgnoreFieldValueMode
        {
            get;
            set;
        }
        public bool AutoDiscoverColumns
        {
            get;
            set;
        }
        public bool ThrowAndStopOnMissingField
        {
            get;
            set;
        }
        public ChoObjectValidationMode ObjectValidationMode
        {
            get;
            set;
        }

        public ChoRecordConfiguration(Type recordType = null)
        {
            ErrorMode = ChoErrorMode.ThrowAndStop;
            AutoDiscoverColumns = true;
            ThrowAndStopOnMissingField = true;
            ObjectValidationMode = ChoObjectValidationMode.MemberLevel;
        }

        protected virtual void Init(Type recordType)
        {
            ChoRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                ErrorMode = recObjAttr.ErrorMode;
                IgnoreFieldValueMode = recObjAttr.IgnoreFieldValueMode;
                ThrowAndStopOnMissingField = recObjAttr.ThrowAndStopOnMissingField;
                ObjectValidationMode = recObjAttr.ObjectValidationMode;
            }
        }

        public abstract void MapRecordFields<T>();
        public abstract void MapRecordFields(Type recordType);
    }
}
