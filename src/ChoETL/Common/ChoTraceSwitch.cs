using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class EPRSTraceSwitch
    {
        private static TraceSwitch _switch;
        public static TraceSwitch Switch
        {
            get { return _switch; }
            set
            {
                if (value == null) return;
                _switch = value;
            }
        }

        static EPRSTraceSwitch()
        {
            Switch = new TraceSwitch("ChoSwitch", "Cho Trace Switch", "Verbose");
        }
    }
}
