using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoRecordFieldTypeAssessmentEventArgs : EventArgs
    {
        public ChoRecordFieldTypeAssessmentEventArgs(IDictionary<string, Type> fieldTypes, IDictionary<string, object> fieldValues, bool isLastScanRow = false)
        {
            FieldTypes = fieldTypes;
            FieldValues = fieldValues;
            IsLastScanRow = isLastScanRow;
        }

        public IDictionary<string, Type> FieldTypes { get; private set; }
        public IDictionary<string, object> FieldValues { get; private set; }

        public bool IsLastScanRow { get; private set; }
    }
}
