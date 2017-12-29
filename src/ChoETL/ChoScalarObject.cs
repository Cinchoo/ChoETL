using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [Serializable]
    public sealed class ChoScalarObject : IChoScalarObject
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
    public sealed class ChoScalarObject<T> : IChoScalarObject
    {
        public T Value
        {
            get;
            set;
        }

        public ChoScalarObject()
        {

        }
        public ChoScalarObject(T data)
        {
            Value = data;
        }
    }

    public interface IChoScalarObject
    {

    }
}
