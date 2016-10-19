using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoEventArgs<T> : EventArgs
    {
        public readonly T Value;

        public ChoEventArgs(T value)
        {
            Value = value;
        }
    }
}
