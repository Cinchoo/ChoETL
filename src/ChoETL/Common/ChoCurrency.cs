using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public struct ChoCurrency : IEquatable<ChoCurrency>
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
    }
}
