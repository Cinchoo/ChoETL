using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [Serializable]
    public class ChoMissingRecordFieldException : ApplicationException
    {
        public ChoMissingRecordFieldException()
            : base()
        {
        }

        public ChoMissingRecordFieldException(string message)
            : base(message)
        {
        }

        public ChoMissingRecordFieldException(string message, Exception e)
            : base(message, e)
        {
        }

        protected ChoMissingRecordFieldException(SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }
    }
}
