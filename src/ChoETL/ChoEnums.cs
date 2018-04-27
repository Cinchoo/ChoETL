using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public enum ChoFieldValueJustification { None, Left, Right }
    public enum ChoFieldValueTrimOption { None, TrimStart, TrimEnd, Trim }
    public enum ChoErrorMode { IgnoreAndContinue, ReportAndContinue, ThrowAndStop };
    [Flags]
    public enum ChoObjectValidationMode
    {
        Off = 0,
        MemberLevel,
        ObjectLevel,
    };
    [Flags]
    public enum ChoIgnoreFieldValueMode
    {
        None = 0,
        Null = 1,
        DBNull = 2,
        Empty = 4,
        WhiteSpace = 8,
        Any = Null | DBNull | Empty | WhiteSpace
    }

    public enum ChoNullValueHandling
    {
        Null,
        Ignore,
        Empty,
        Default
    }

    public enum ChoEmptyLineHandling
    {
        Ignore,
        Null,
        Default
    }
}
