using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoDataReaderExtension
    {
        public static IEnumerable<IDataReader> AsEnumerable(this IDataReader source)
        {
            return ChoEnumeratorWrapper.BuildEnumerable(source.Read, () => source);
        }
    }
}
