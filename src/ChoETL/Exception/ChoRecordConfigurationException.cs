using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [Serializable]
    public class ChoRecordConfigurationException : ApplicationException
    {
        public ChoRecordConfigurationException()
            : base()
        {
        }

        public ChoRecordConfigurationException(string message)
            : base(message)
        {
        }

        public ChoRecordConfigurationException(string message, Exception e)
            : base(message, e)
        {
        }

        protected ChoRecordConfigurationException(SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }
    }
}
