using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [Serializable]
    public class ChoBadDataException : ApplicationException
    {
        public ChoBadDataException()
            : base()
        {
        }

        public ChoBadDataException(string message)
            : base(message)
        {
        }

        public ChoBadDataException(string message, Exception e)
            : base(message, e)
        {
        }

        protected ChoBadDataException(SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }
    }
}
