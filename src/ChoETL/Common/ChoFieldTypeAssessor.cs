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

        private ConcurrentBag<Func<string, object, ChoTypeConverterFormatSpec, CultureInfo, Type>> _fieldTypeAssessors = 
            new ConcurrentBag<Func<string, object, ChoTypeConverterFormatSpec, CultureInfo, Type>>();
        public Func<string, object, ChoTypeConverterFormatSpec, CultureInfo, Type> FieldTypeAssessor
        {
            get;
            set;
        }

        public ChoFieldTypeAssessor()
        {

        }

        public ChoFieldTypeAssessor(ConcurrentBag<Func<string, object, ChoTypeConverterFormatSpec, CultureInfo, Type>> fieldTypeAssessors)
        {
            _fieldTypeAssessors = fieldTypeAssessors;
        }

        public ChoFieldTypeAssessor(Func<string, object, ChoTypeConverterFormatSpec, CultureInfo, Type> fieldTypeAssessor)
        {
            FieldTypeAssessor = fieldTypeAssessor;
        }

        public Type AssessType(string key, object value, ChoTypeConverterFormatSpec fs, CultureInfo ci)
        {
            if (value == null)
                return null;
           
            Type ft = null;

            Func<string, object, ChoTypeConverterFormatSpec, CultureInfo, Type> fieldTypeAssessor = FieldTypeAssessor;
            if (fieldTypeAssessor != null)
            {
                ft = fieldTypeAssessor(key, value, fs, ci);
                if (ft != null)
                    return ft;
            }

            ConcurrentBag<Func<string, object, ChoTypeConverterFormatSpec, CultureInfo, Type>> fieldTypeAssessors = _fieldTypeAssessors;
            if (fieldTypeAssessors == null)
                return null;

            var list = fieldTypeAssessors.ToArray();
            foreach (var item in list)
            {
                ft = item(key, value, fs, ci);
                if (ft != null)
                    return ft;
            }

            return null;
        }

        public void Add(Func<string, object, ChoTypeConverterFormatSpec, CultureInfo, Type> fta)
        {
            if (fta == null)
                return;

            _fieldTypeAssessors.Add(fta);
        }

        public void Remove(Func<string, object, ChoTypeConverterFormatSpec, CultureInfo, Type> fta)
        {
            if (fta == null)
                return;

            _fieldTypeAssessors = new ConcurrentBag<Func<string, object, ChoTypeConverterFormatSpec, CultureInfo, Type>>(_fieldTypeAssessors.Except(new[] { fta }));
        }
    }
}
