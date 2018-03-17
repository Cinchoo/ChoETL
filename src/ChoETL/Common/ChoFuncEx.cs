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
    public static class ChoFuncEx
    {
        public static T RunWithIgnoreError<T>(this Func<T> func, T defaultValue = default(T))
        {
            if (func == null) return defaultValue;

            try
            {
                return func();
            }
            catch (NotImplementedException)
            {
                return defaultValue;
            }
        }
        public static T? RunWithIgnoreErrorNullableReturn<T>(this Func<T> func)
            where T : struct
        {
            if (func == null) return null;

            try
            {
                return func();
            }
            catch (NotImplementedException)
            {
                return null;
            }
        }
    }
}