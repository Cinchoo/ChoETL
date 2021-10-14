using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoFieldTypeAssessor
    {
        public readonly static ChoFieldTypeAssessor Instance = new ChoFieldTypeAssessor();

        private readonly ConcurrentBag<Func<object, ChoTypeConverterFormatSpec, CultureInfo, Type>> _fieldTypeAssessors = new ConcurrentBag<Func<object, ChoTypeConverterFormatSpec, CultureInfo, Type>>();

        public Type AssessType(object target, ChoTypeConverterFormatSpec fs, CultureInfo ci)
        {
            if (target == null)
                return null;

            Type ft = null;
            var list = _fieldTypeAssessors.ToArray();
            foreach (var item in list)
            {
                ft = item(target, fs, ci);
                if (ft != null)
                    return ft;
            }

            return null;
        }
    }
}
