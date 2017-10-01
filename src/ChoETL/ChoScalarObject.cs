using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [Serializable]
    internal class ChoScalarObject
    {
        public object Value
        {
            get;
            set;
        }

        public ChoScalarObject()
        {

        }
        public ChoScalarObject(object data)
        {
            Value = data;
        }
    }

}
