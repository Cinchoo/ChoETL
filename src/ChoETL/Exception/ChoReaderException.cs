using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [Serializable]
    public class ChoReaderException : ApplicationException
    {
        public ChoReaderException()
            : base()
        {
        }

        public ChoReaderException(string message)
            : base(message)
        {
        }

        public ChoReaderException(string message, Exception e)
            : base(message, e)
        {
        }

        protected ChoReaderException(SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }
    }

    [Serializable]
    public class ChoWriterException : ApplicationException
    {
        public ChoWriterException()
            : base()
        {
        }

        public ChoWriterException(string message)
            : base(message)
        {
        }

        public ChoWriterException(string message, Exception e)
            : base(message, e)
        {
        }

        protected ChoWriterException(SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }
    }
}
