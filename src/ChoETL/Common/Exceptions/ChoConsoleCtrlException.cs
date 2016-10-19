using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [Serializable]
    public class ChoConsoleCtrlException : ApplicationException
    {
        public ChoConsoleCtrlException()
            : base()
        {
        }

        public ChoConsoleCtrlException(string message)
            : base(message)
        {
        }

        public ChoConsoleCtrlException(string message, Exception e)
            : base(message, e)
        {
        }

        protected ChoConsoleCtrlException(SerializationInfo si, StreamingContext sc)
            : base(si, sc)
        {
        }
    }
}
