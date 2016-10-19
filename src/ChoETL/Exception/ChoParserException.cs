using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [Serializable]
    public class ChoParserException : ApplicationException
    {
        public ChoParserException()
            : base()
        {
        }

        public ChoParserException(string message)
            : base(message)
        {
        }

        public ChoParserException(string message, Exception e)
            : base(message, e)
        {
        }

        protected ChoParserException(SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }
    }
}
