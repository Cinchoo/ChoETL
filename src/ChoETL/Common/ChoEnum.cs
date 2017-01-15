namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.Reflection;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Linq;

    #endregion NameSpaces

    /// <summary>
    /// Static utility class used to convert enum to and from description.
    /// Enum values should be decorated with DescriptionAttribute in order to use this class
    /// 
    /// Ex:
    ///     [Flags]
    ///     public enum Coolness : int
    ///     {
    ///         Nothing = 0,
    ///         [Description("Hot Weather")]
    ///         Hot = (1 << 0),
    ///         [Description("Cold Weather")]
    ///         Cold = (1 << 1),
    ///         [Description("Chill Weather")]
    ///         Chill = (1 << 2),
    ///     }
    /// </summary>
    public static class ChoEnum
    {
        /// <summary>
        /// Includes an enumerated type and returns the new value
        /// </summary>
        public static T Include<T>(this Enum value, T append)
        {
            Type type = value.GetType();

            //determine the values
            object result = value;
            _Value parsed = new _Value(append, type);
            if (parsed.Signed is long)
            {
                result = Convert.ToInt64(value) | (long)parsed.Signed;
            }
            else if (parsed.Unsigned is ulong)
            {
                result = Convert.ToUInt64(value) | (ulong)parsed.Unsigned;
            }

            //return the final value
            return (T)Enum.Parse(type, result.ToString());
        }

        /// <summary>
        /// Removes an enumerated type and returns the new value
        /// </summary>
        public static T Remove<T>(this Enum value, T remove)
        {
            Type type = value.GetType();

            //determine the values
            object result = value;
            _Value parsed = new _Value(remove, type);
            if (parsed.Signed is long)
            {
                result = Convert.ToInt64(value) & ~(long)parsed.Signed;
            }
            else if (parsed.Unsigned is ulong)
            {
                result = Convert.ToUInt64(value) & ~(ulong)parsed.Unsigned;
            }

            //return the final value
            return (T)Enum.Parse(type, result.ToString());
        }

        public static bool Has<T>(this Enum value, T[] check)
        {
            if (check == null) return false;

            if (check.Length == 1)
                return Has(value, check[0]);
            else
            {
                foreach (T x in check)
                {
                    if (!Has(value, x))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Checks if an enumerated type contains a value
        /// </summary>
        public static bool Has<T>(this Enum value, T check)
        {
            Type type = value.GetType();

            //determine the values
            object result = value;
            _Value parsed = new _Value(check, type);
            if (parsed.Signed is long)
            {
                return (Convert.ToInt64(value) &
          (long)parsed.Signed) == (long)parsed.Signed;
            }
            else if (parsed.Unsigned is ulong)
            {
                return (Convert.ToUInt64(value) &
          (ulong)parsed.Unsigned) == (ulong)parsed.Unsigned;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if an enumerated type is missing a value
        /// </summary>
        public static bool Missing<T>(this Enum obj, T value)
        {
            return !ChoEnum.Has<T>(obj, value);
        }


        //class to simplfy narrowing values between 
        //a ulong and long since either value should
        //cover any lesser value
        private class _Value
        {

            //cached comparisons for tye to use
            private static Type _UInt64 = typeof(ulong);
            private static Type _UInt32 = typeof(long);

            public long? Signed;
            public ulong? Unsigned;

            public _Value(object value, Type type)
            {

                //make sure it is even an enum to work with
                if (!type.IsEnum)
                {
                    throw new ArgumentException("Value provided is not an enumerated type!");
                }

                //then check for the enumerated value
                Type compare = Enum.GetUnderlyingType(type);

                //if this is an unsigned long then the only
                //value that can hold it would be a ulong
                if (compare.Equals(_Value._UInt32) || compare.Equals(_Value._UInt64))
                {
                    this.Unsigned = Convert.ToUInt64(value);
                }
                //otherwise, a long should cover anything else
                else
                {
                    this.Signed = Convert.ToInt64(value);
                }

            }

        }

        /// <summary>
        /// Method used to convert a enum value to correponding description value attached to.
        /// </summary>
        /// <param name="enumValue">A enum value</param>
        /// <returns>Description value attached to the enum value if there is a match, otherwise Enumvalue.ToString() will be returned</returns>
        public static string ToDescription(this Enum enumValue)
        {
            return ChoEnumTypeDescCache.GetEnumDescription(enumValue);
        }

        public static IEnumerable<Tuple<int, string>> ToEnumPairValues<T>()
        {
            var type = typeof(T);
            if (!type.IsEnum)
                throw new ArgumentException("Type is not an enum.");

            var names = Enum.GetNames(type);
            var values = Enum.GetValues(type);
            var pairs =
                Enumerable.Range(0, names.Length)
                .Select(i => new Tuple<int, string>((int)values.GetValue(i), (string)names.GetValue(i)))
                .OrderBy(pair => pair.Item2);
            return pairs;
        }
    }
}
