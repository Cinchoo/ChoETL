using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoProcess
    {
        private static double f = 1024.0;
        private static Process p = Process.GetCurrentProcess();

        public static string GetMemorySnapshot()
        {
            StringBuilder msg = new StringBuilder();

            /*
                PrivateMemorySize
                    The number of bytes that the associated process has allocated that cannot be shared with other processes.
                PeakVirtualMemorySize
                    The maximum amount of virtual memory that the process has requested.
                PeakPagedMemorySize
                    The maximum amount of memory that the associated process has allocated that could be written to the virtual paging file.
                PagedSystemMemorySize
                    The amount of memory that the system has allocated on behalf of the associated process that can be written to the virtual memory paging file.
                PagedMemorySize
                    The amount of memory that the associated process has allocated that can be written to the virtual memory paging file.
                NonpagedSystemMemorySize
                    The amount of memory that the system has allocated on behalf of the associated process that cannot be written to the virtual memory paging file.
            */
            msg.AppendLine("Private memory: {0}".FormatString((p.PrivateMemorySize64 / f).ToString("#,##0")));
            msg.AppendLine("Working Set: {0}".FormatString((p.WorkingSet64 / f).ToString("#,##0")));
            msg.AppendLine("Peak virtual memory: {0}".FormatString((p.PeakVirtualMemorySize64 / f).ToString("#,##0")));
            msg.AppendLine("Peak paged memory: {0}".FormatString((p.PeakPagedMemorySize64 / f).ToString("#,##0")));
            msg.AppendLine("Paged system memory: {0}".FormatString((p.PagedSystemMemorySize64 / f).ToString("#,##0")));
            msg.AppendLine("Paged memory: {0}".FormatString((p.PagedMemorySize64 / f).ToString("#,##0")));
            msg.AppendLine("Nonpaged system memory: {0}".FormatString((p.NonpagedSystemMemorySize64 / f).ToString("#,##0")));

            return msg.ToString();
        }

        public static string GetMemorySnapshotHelp()
        {
            StringBuilder msg = new StringBuilder();

            msg.AppendLine("PrivateMemorySize");
            msg.AppendLine("\tThe number of bytes that the associated process has allocated that cannot be shared with other processes.");
            msg.AppendLine("PeakVirtualMemorySize");
            msg.AppendLine("\tThe maximum amount of virtual memory that the process has requested.");
            msg.AppendLine("PeakPagedMemorySize");
            msg.AppendLine("\tThe maximum amount of memory that the associated process has allocated that could be written to the virtual paging file.");
            msg.AppendLine("PagedSystemMemorySize");
            msg.AppendLine("\tThe amount of memory that the system has allocated on behalf of the associated process that can be written to the virtual memory paging file.");
            msg.AppendLine("PagedMemorySize");
            msg.AppendLine("\tThe amount of memory that the associated process has allocated that can be written to the virtual memory paging file.");
            msg.AppendLine("NonpagedSystemMemorySize");
            msg.AppendLine("\tThe amount of memory that the system has allocated on behalf of the associated process that cannot be written to the virtual memory paging file.");

            return msg.ToString();
        }
    }
}
