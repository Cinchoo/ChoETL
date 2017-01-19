using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChoETL
{
    public enum ChoBooleanFormatSpec { ZeroOrOne, YOrN, TrueOrFalse, YesOrNo };
    public enum ChoEnumFormatSpec { Value, Name, Description };

    public class ChoTypeConverterFormatSpec
    {
        public static readonly ThreadLocal<ChoTypeConverterFormatSpec> Instance = new ThreadLocal<ChoTypeConverterFormatSpec>(() => new ChoTypeConverterFormatSpec());

        public string DateTimeFormat { get; set; }
        public ChoBooleanFormatSpec BooleanFormat { get; set; }
        public ChoEnumFormatSpec EnumFormat { get; set; }

        public NumberStyles? CurrencyNumberStyle { get; set; }
        public string CurrencyFormat { get; set; }

        public NumberStyles? BigIntegerNumberStyle { get; set; }
        public string BigIntegerFormat { get; set; }

        public NumberStyles? ByteNumberStyle { get; set; }
        public string ByteFormat { get; set; }

        public NumberStyles? SByteNumberStyle { get; set; }
        public string SByteFormat { get; set; }

        public NumberStyles? DecimalNumberStyle { get; set; }
        public string DecimalFormat { get; set; }

        public NumberStyles? DoubleNumberStyle { get; set; }
        public string DoubleFormat { get; set; }

        public NumberStyles? FloatNumberStyle { get; set; }
        public string FloatFormat { get; set; }

        public string IntFormat { get; set; }
        public NumberStyles? IntNumberStyle { get; set; }

        public string UIntFormat { get; set; }
        public NumberStyles? UIntNumberStyle { get; set; }

        public NumberStyles? LongNumberStyle { get; set; }
        public string LongFormat { get; set; }

        public NumberStyles? ULongNumberStyle { get; set; }
        public string ULongFormat { get; set; }

        public NumberStyles? ShortNumberStyle { get; set; }
        public string ShortFormat { get; set; }

        public NumberStyles? UShortNumberStyle { get; set; }
        public string UShortFormat { get; set; }

        public ChoTypeConverterFormatSpec()
        {
            DateTimeFormat = "d";
            CurrencyNumberStyle = NumberStyles.Currency;
            CurrencyFormat = "C";
            EnumFormat = ChoEnumFormatSpec.Value;
        }
    }
}
