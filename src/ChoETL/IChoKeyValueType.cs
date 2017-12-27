using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoKeyValueType
    {
        object Key { get; set; }
        object Value { get; set; }
    }
}