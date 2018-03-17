using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoActionEx
    {
        public static void RunWithIgnoreError(this Action action)
        {
            if (action == null) return;

            try
            {
                action();
            }
            catch (NotImplementedException)
            {
            }
        }

        public static void ExecuteInConstrainedRegion(this Action action)
        {
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                action();
            }
        }
    }
}