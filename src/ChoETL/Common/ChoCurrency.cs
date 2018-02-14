using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [ChoDataTableColumnType(typeof(double))]
    public struct ChoCurrency : IEquatable<ChoCurrency>, IConvertible
    {
        public ChoCurrency(decimal amount)
        {
            this.Currency = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencySymbol;
            this.Amount = amount;
        }

        public ChoCurrency(double amount)
        {
            this.Currency = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencySymbol;
            this.Amount = (Decimal)amount;
        }

        public ChoCurrency(int amount)
        {
            this.Currency = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencySymbol;
            this.Amount = (Decimal)amount;
        }

        public ChoCurrency(long amount)
        {
            this.Currency = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.CurrencySymbol;
            this.Amount = (Decimal)amount;
        }

        public string Currency { get; private set; }
        public decimal Amount { get; private set; }

        public bool Equals(ChoCurrency other)
        {
            if (object.ReferenceEquals(other, null)) return false;
            if (object.ReferenceEquals(other, this)) return true;
            return this.Currency.Equals(other.Currency) && this.Amount.Equals(other.Amount);
        }

        public override bool Equals(object obj)
        {
            return Equals((ChoCurrency)obj);
        }

        public override int GetHashCode()
        {
            return this.Currency.GetHashCode() ^ this.Amount.GetHashCode();
        }

        public static bool TryParse(string text, out ChoCurrency currency)
        {
            currency = null;
            Decimal result;
            if (Decimal.TryParse(text,
                NumberStyles.Currency,
                CultureInfo.CurrentCulture,
                out result))
            {
                currency = new ChoCurrency(result);
                return true;
            }
            return false;
        }

        public static bool TryParse(string text, NumberStyles style, IFormatProvider provider, out ChoCurrency currency)
        {
            currency = null;
            Decimal result;
            if (Decimal.TryParse(text,
                style,
                provider,
                out result))
            {
                currency = new ChoCurrency(result);
                return true;
            }
            return false;
        }

        public static ChoCurrency operator +(ChoCurrency first, ChoCurrency second)
        {
            return new ChoCurrency(first.Amount + second.Amount);
        }

        public static ChoCurrency operator -(ChoCurrency first, ChoCurrency second)
        {
            return new ChoCurrency(first.Amount - second.Amount);
        }

        public static implicit operator ChoCurrency(decimal value)
        {
            return new ChoCurrency(value);
        }

        public static implicit operator ChoCurrency(double value)
        {
            return new ChoCurrency(value);
        }

        public static implicit operator ChoCurrency(int value)
        {
            return new ChoCurrency((double)value);
        }

        public static implicit operator ChoCurrency(long value)
        {
            return new ChoCurrency((double)value);
        }

        public static implicit operator ChoCurrency(string value)
        {
            if (value.IsNullOrWhiteSpace())
                return new ChoCurrency(0.0);
            else
                return new ChoCurrency(double.Parse(value, NumberStyles.Currency));
        }

        public static implicit operator decimal(ChoCurrency o)
        {
            return o.Amount;
        }

        public static implicit operator double(ChoCurrency o)
        {
            return (double)o.Amount;
        }

        // Overload the conversion from DBBool to string:
        public static implicit operator string (ChoCurrency x)
        {
            return x.Amount.ToString("c");
        }

        public override string ToString()
        {
            return (string)this;
        }

        public TypeCode GetTypeCode()
        {
            throw new NotImplementedException();
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public char ToChar(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public byte ToByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public short ToInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public int ToInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public long ToInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public float ToSingle(IFormatProvider provider)
        {
            return (float)Amount;
        }

        public double ToDouble(IFormatProvider provider)
        {
            return (double)Amount;
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            return Amount;
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public string ToString(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
    }
}
