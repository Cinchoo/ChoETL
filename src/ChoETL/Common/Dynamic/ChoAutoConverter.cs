using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoAutoConverter : DynamicObject
    {
        public readonly object Value;
        private ChoDynamicObject _dynamicObject;

        public ChoAutoConverter(object value, ChoDynamicObject dynamicObject = null)
        {
            Value = value;
            _dynamicObject = dynamicObject;
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            result = ChoUtility.CastObjectTo(Value, binder.Type, _dynamicObject == null ? null : ChoType.GetDefaultValue(binder.Type));
            return true;
        }
    }
}
