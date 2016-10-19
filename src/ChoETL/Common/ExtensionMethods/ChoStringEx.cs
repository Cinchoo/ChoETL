namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Globalization;
    using System.Reflection;
    using System.ComponentModel;
    using System.Xml;
    using System.Xml.Serialization;
    using System.IO;
    using System.Xml.Linq;

    #endregion NameSpaces

    public static class ChoString
    {
        #region Constants

        private const string HeaderDelimiter = "--";

        #endregion Constants

        #region Shared Data Members (Private)

        private static readonly Regex _splitNameRegex = new Regex(@"[\W_]+", RegexOptions.Compiled);
        private static Regex _headerRegex = new Regex(@"^\W*{0}.*".FormatString(HeaderDelimiter), RegexOptions.Compiled | RegexOptions.Singleline);

        #endregion Shared Data Members (Private)

        #region Conversion Methods

        public static long ToInt16(this string value)
        {
            Int16 result = 0;

            if (!string.IsNullOrEmpty(value))
                Int16.TryParse(value, out result);

            return result;
        }

        public static long ToInt32(this string value)
        {
            Int32 result = 0;

            if (!string.IsNullOrEmpty(value))
                Int32.TryParse(value, out result);

            return result;
        }

        public static long ToInt64(this string value)
        {
            Int64 result = 0;

            if (!string.IsNullOrEmpty(value))
                Int64.TryParse(value, out result);

            return result;
        }

        #endregion Conversion Methods

        #region Match Overloads

        public static bool Match(this string value, string pattern)
        {
            return Regex.IsMatch(value, pattern);
        }

        #endregion Match Overloads

        #region ToEnumValue Overloads

        public static object ToEnumvalue(this string fullyQualifiedEnumValue)
        {
            Regex regEx = new Regex(@"^(?<enumType>(\w+\.)*)(?<enumValue>\w+)$|^(?<enumType>(\w+\.)*)(?<enumValue>\w+)(?<assemblyName>\,\s*.*)$");
            Match match = regEx.Match(fullyQualifiedEnumValue);
            if (!match.Success)
                throw new ApplicationException(String.Format("Incorrect format value [{0}] passed.", fullyQualifiedEnumValue));

            string typeName = match.Groups["enumType"].ToString().Substring(0, match.Groups["enumType"].ToString().Length - 1);
            if (!String.IsNullOrEmpty(match.Groups["assemblyName"].ToString()))
                typeName += match.Groups["assemblyName"];

            Type enumType = Type.GetType(typeName);
            if (enumType == null)
                throw new ApplicationException(String.Format("Can't find [{0}] type.", match.Groups["enumType"].ToString()));

            return Enum.Parse(enumType, match.Groups["enumValue"].ToString());
        }

        #endregion ToEnumValue Overloads

        #region ToSpacedWords Overloads

        /// <summary>
        /// Takes a NameIdentifier and spaces it out into words "Name Identifier".
        /// </summary>
        /// <param name="text">A string value which will be break into spaced words</param>
        /// <returns>A new System.String having spaced words.</returns>
        public static string ToSpacedWords(this String text)
        {
            if (String.IsNullOrEmpty(text)) return text;
            return Regex.Replace(text, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");
        }

        #endregion ToSpacedWords Overloads

        #region ToCamelCase Member (Public)

        /// <summary>
        /// Converts a string to use camelCase.
        /// </summary>
        /// <param name="value">A string value to convert</param>
        /// <returns>A new System.String converted to Camel Case</returns>
        public static string ToCamelCase(this string value)
        {
            if (value == null || value.Trim().Length == 0)
                return value;

            string output = ToPascalCase(value);
            if (output.Length > 2)
                return char.ToLower(output[0]) + output.Substring(1);
            else
                return output.ToLower();
        }

        #endregion ToCamelCase Member (Public)

        #region ToProperCase Overloads

        /// <summary>
        /// Converts a string to Proper Case. This is an alias for ToPascalCase
        /// </summary>
        /// <param name="value">string to convert</param>
        /// <returns>The string converted to Proper Case</returns>
        public static string ToProperCase(this string value)
        {
            return ToPascalCase(value);
        }

        #endregion ToProperCase Overloads

        #region ToPascalCase Overloads

        /// <summary>
        /// Converts a string to use PascalCase.
        /// </summary>
        /// <param name="value">Text to convert</param>
        /// <returns></returns>
        public static string ToPascalCase(this string value)
        {
            if (String.IsNullOrEmpty(value)) return value;

            string[] names = _splitNameRegex.Split(value);
            StringBuilder output = new StringBuilder();

            if (names.Length > 1)
            {
                foreach (string name in names)
                {
                    if (name.Length > 1)
                    {
                        output.Append(char.ToUpper(name[0]));
                        output.Append(name.Substring(1).ToLower());
                    }
                    else
                        output.Append(name);
                }
            }
            else if (value.Length > 1)
            {
                output.Append(char.ToUpper(value[0]));
                output.Append(value.Substring(1));
            }
            else
                output.Append(value.ToUpper());

            return output.ToString();
        }

        #endregion ToPascalCase Overloads

        #region IsAlphaNumeric member

        private readonly static Regex _alphaNumericPattern = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);

        /// <summary>
        /// Function to test a string value contains only alphanumeric characters.
        /// </summary>
        /// <param name="text">A string to test for alphanumeric characters.</param>
        /// <returns>true, if the passed text is a valid alphanumeric characters. Otherwise false.</returns>
        public static bool IsAlphaNumeric(this string text)
        {
            Regex alphaNumericPattern = new Regex("[^a-zA-Z0-9]");
            return !alphaNumericPattern.IsMatch(text);
        }

        #endregion IsAlphaNumeric member

        #region IsAlpha member

        private readonly static Regex _alphaPattern = new Regex("[^a-zA-Z]", RegexOptions.Compiled);

        // Function To test for Alphabets.
        /// <summary>
        /// Function to test a string value contains only alphabets.
        /// </summary>
        /// <param name="text">A string to test for alphabets.</param>
        /// <returns>true, if the passed text is a valid alphabets. Otherwise false.</returns>
        public static bool IsAlpha(this string text)
        {
            return !_alphaPattern.IsMatch(text);
        }

        #endregion IsAlpha member

        #region IsNumber Member

        private readonly static Regex _notNumberPattern = new Regex("[^0-9.-]");
        private readonly static Regex _objTwoMinusPattern = new Regex("[0-9]*[-][0-9]*[-][0-9]*");
        private readonly static Regex _numberPattern = new Regex("(^([-]|[.]|[-.]|[0-9])[0-9]*[.]*[0-9]+$)|(^([-]|[0-9])[0-9]*$)");

        /// <summary>
        /// Function to test a string value for a number.
        /// </summary>
        /// <param name="text">A string to test for number.</param>
        /// <returns>true, if the passed text is a valid number. Otherwise false.</returns>
        public static bool IsNumber(this string text)
        {
            if (String.IsNullOrEmpty(text)) return false;

            return !_notNumberPattern.IsMatch(text) &&
                !_objTwoDotPattern.IsMatch(text) &&
                !_objTwoMinusPattern.IsMatch(text) &&
                _numberPattern.IsMatch(text);
        }

        #endregion IsNumber Member

        #region IsPositiveNumber Member

        private readonly static Regex _notPositivePattern = new Regex("[^0-9.]", RegexOptions.Compiled);
        private readonly static Regex _objPositivePattern = new Regex("^[.][0-9]+$|[0-9]*[.]*[0-9]+$", RegexOptions.Compiled);
        private readonly static Regex _objTwoDotPattern = new Regex("[0-9]*[.][0-9]*[.][0-9]*", RegexOptions.Compiled);

        /// <summary>
        /// Function to test a string value for positive number both integer & real.
        /// </summary>
        /// <param name="text">A string to test for positive number.</param>
        /// <returns>true, if the passed text is a valid positive number. Otherwise false.</returns>
        public static bool IsPositiveNumber(string text)
        {
            if (String.IsNullOrEmpty(text)) return false;

            return !_notPositivePattern.IsMatch(text) &&
                _objPositivePattern.IsMatch(text) &&
                !_objTwoDotPattern.IsMatch(text);
        }

        #endregion IsPositiveNumber Member

        #region IsNaturalNumber Member

        private readonly static Regex _notNaturalPattern = new Regex("[^0-9]", RegexOptions.Compiled);
        private readonly static Regex _naturalPattern = new Regex("0*[1-9][0-9]*", RegexOptions.Compiled);

        /// <summary>
        /// Method to test a string value for positive integers.
        /// </summary>
        /// <param name="text">A string to test for positive integer.</param>
        /// <returns>true, if the passed text is a valid positive integer. Otherwise false.</returns>
        public static bool IsNaturalNumber(this string text)
        {
            if (String.IsNullOrEmpty(text)) return false;

            return !_notNaturalPattern.IsMatch(text) &&
                _naturalPattern.IsMatch(text);
        }

        #endregion IsNaturalNumber Member

        #region IsWholeNumber Member

        private readonly static Regex _notWholePattern = new Regex("[^0-9]", RegexOptions.Compiled);

        /// <summary>
        /// Method to test a string value for positive integers with zero inclusive 
        /// </summary>
        /// <param name="text">A string to test for whole number.</param>
        /// <returns>true, if the passed text is a valid whole number. Otherwise false.</returns>
        public static bool IsWholeNumber(this string text)
        {
            if (String.IsNullOrEmpty(text)) return false;

            return !_notWholePattern.IsMatch(text);
        }

        #endregion IsWholeNumber Member

        #region IsInteger Member

        private readonly static Regex _notIntPattern = new Regex("[^0-9-]", RegexOptions.Compiled);
        private readonly static Regex _objIntPattern = new Regex("^-[0-9]+$|^[0-9]+$", RegexOptions.Compiled);

        /// <summary>
        /// Function to test the string value for integers both Positive & Negative.
        /// </summary>
        /// <param name="text">A string to test for integer.</param>
        /// <returns>true, if the passed text has valid integer value. Otherwise false.</returns>
        public static bool IsInteger(this string text)
        {
            if (String.IsNullOrEmpty(text)) return false;

            return !_notIntPattern.IsMatch(text) && _objIntPattern.IsMatch(text);
        }

        #endregion IsInteger Member

        #region IsBoolean member

        /// <summary>
        /// Function to test the string for boolean.
        /// </summary>
        /// <param name="text">A string to test for boolean.</param>
        /// <returns>true, if the passed text has valid boolean value. Otherwise false.</returns>
        public static bool IsBoolean(this string text)
        {
            if (String.IsNullOrEmpty(text)) return false;

            return text.Trim().ToLower() == "true" || text.Trim().ToLower() == "false";
        }

        #endregion IsBoolean member

        #region IsByte member

        private readonly static Regex _bytePattern = new Regex("^[0-2][0-5][0-5]$|^[0-9]{1,2}$", RegexOptions.Compiled);

        /// <summary>
        /// Function to test the string for byte.
        /// </summary>
        /// <param name="text">A string to test for byte value.</param>
        /// <returns>true, if the passed text has valid byte value. Otherwise false.</returns>
        public static bool IsByte(this string text)
        {
            if (String.IsNullOrEmpty(text)) return false;

            return _bytePattern.IsMatch(text);
        }

        #endregion IsByte member

        #region IsSByte member

        private readonly static Regex _sBytePattern = new Regex("^[-][0-1][0-2][0-8]$|^[0-1][0-2][0-7]$|^[-][0-9]{1,2}$|^[0-9]{1,2}$", RegexOptions.Compiled);

        /// <summary>
        /// Function to test the string for signed byte.
        /// </summary>
        /// <param name="text">A string to test for signed byte value.</param>
        /// <returns>true, if the passed text has valid signed byte value. Otherwise false.</returns>
        public static bool IsSByte(this string text)
        {
            if (String.IsNullOrEmpty(text)) return false;

            return _sBytePattern.IsMatch(text);
        }

        #endregion IsSByte member

        #region IsPlural member

        private static readonly List<string> _invariants = new List<string>(new string[] { "alias", "news" });

        private static readonly Regex _pluralRegex1 = new Regex("(?<keep>[^aeiou])ies$", RegexOptions.Compiled);
        private static readonly Regex _pluralRegex2 = new Regex("(?<keep>[aeiou]y)s$", RegexOptions.Compiled);
        private static readonly Regex _pluralRegex3 = new Regex("(?<keep>[sxzh])es$", RegexOptions.Compiled);
        private static readonly Regex _pluralRegex4 = new Regex("(?<keep>[^sxzhy])s$", RegexOptions.Compiled);

        /// <summary>
        /// Determines if a string is in plural form based on some simple rules.
        /// </summary>
        /// <param name="text">The string to be checked for plural.</param>
        /// <returns>true, if the passed string is a plural string. Otherwise false. 
        /// If the passed string is one of the default invariant string list, it will return true.</returns>
        public static bool IsPlural(this string text)
        {
            return IsPlural(text, _invariants);
        }

        /// <summary>
        /// Determines if a string is in plural form based on some simple rules.
        /// </summary>
        /// <param name="text">The string to be checked for plural.</param>
        /// <returns>true, if the passed string is a plural string. Otherwise false. 
        /// If the passed string is one of the passed invariant string list, it will return true.</returns>
        public static bool IsPlural(this string text, List<string> invariants)
        {
            if (invariants != null && invariants.Contains(text)) return true;

            if (_pluralRegex1.IsMatch(text)
                || _pluralRegex2.IsMatch(text)
                || _pluralRegex3.IsMatch(text)
                || _pluralRegex4.IsMatch(text)
                )
                return true;

            return false;
        }

        #endregion IsPlural member

        #region ToPlural Members

        private static readonly Regex _singleRegex1 = new Regex("(?<keep>[^aeiou])y$", RegexOptions.Compiled);
        private static readonly Regex _singleRegex2 = new Regex("(?<keep>[aeiou]y)$", RegexOptions.Compiled);
        private static readonly Regex _singleRegex3 = new Regex("(?<keep>[sxzh])$", RegexOptions.Compiled);
        private static readonly Regex _singleRegex4 = new Regex("(?<keep>[^sxzhy])$", RegexOptions.Compiled);

        /// <summary>
        /// Converts a string to plural based on some simple rules.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToPlural(this string text)
        {
            return ToPlural(text, _invariants);
        }

        public static string ToPlural(this string text, List<string> invariants)
        {
            // handle invariants
            if (invariants != null && invariants.Contains(text)) return text;
            if (!IsSingular(text)) return text;

            if (_singleRegex1.IsMatch(text))
                return _singleRegex1.Replace(text, "${keep}ies");
            else if (_singleRegex2.IsMatch(text))
                return _singleRegex2.Replace(text, "${keep}s");
            else if (_singleRegex3.IsMatch(text))
                return _singleRegex3.Replace(text, "${keep}es");
            else if (_singleRegex4.IsMatch(text))
                return _singleRegex4.Replace(text, "${keep}s");

            return text;
        }

        #endregion ToPlural Members

        #region IsSingular Members

        /// <summary>
        /// Determines if a string is in singular form based on some simple rules.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsSingular(this string text)
        {
            return IsSingular(text, _invariants);
        }

        public static bool IsSingular(this string text, List<string> invariants)
        {
            if (invariants != null && invariants.Contains(text)) return true;

            return !IsPlural(text);
        }

        #endregion IsSingular Members

        #region ToSingular Members

        /// <summary>
        /// Converts a string to singular based on some simple rules.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToSingular(this string text)
        {
            return ToSingular(text, _invariants);
        }

        public static string ToSingular(this string text, List<string> invariants)
        {
            if (invariants != null && invariants.Contains(text)) return text;
            if (!IsPlural(text)) return text;

            if (_pluralRegex1.IsMatch(text))
                return _pluralRegex1.Replace(text, "${keep}y");
            else if (_pluralRegex2.IsMatch(text))
                return _pluralRegex2.Replace(text, "${keep}");
            else if (_pluralRegex3.IsMatch(text))
                return _pluralRegex3.Replace(text, "${keep}");
            else if (_pluralRegex4.IsMatch(text))
                return _pluralRegex4.Replace(text, "${keep}");

            return text;
        }

        #endregion ToSingular Members

        #region ToKeyValuePairs Members

        public static IEnumerable<KeyValuePair<string, string>> ToKeyValuePairs(this string text, char Separator = ';', char keyValueSeparator = '=')
        {
            if (!text.IsNullOrEmpty())
            {
                foreach (string keyValue in text.Split(Separator.ToString(), ChoStringSplitOptions.RemoveEmptyEntries))
                {
                    if (keyValue.IsNullOrEmpty())
                        continue;

                    int kvSeparatorIndex = keyValue.IndexOf(keyValueSeparator);
                    if (kvSeparatorIndex <= 0)
                        continue;
                    yield return new KeyValuePair<string, string>(keyValue.Left(kvSeparatorIndex), keyValue.Right(keyValue.Length - 1 - kvSeparatorIndex));

                    //string[] keyValueTokens = keyValue.SplitNTrim(keyValueSeparator.ToString(), ChoStringSplitOptions.RemoveEmptyEntries);
                    //if (keyValueTokens != null && keyValueTokens.Length > 0)
                    //{
                    //    string key = keyValueTokens[0];
                    //    string value = null;
                    //    if (keyValueTokens.Length > 1)
                    //        value = String.Join(keyValueSeparator.ToString(), keyValueTokens.Skip(1));

                    //    yield return new KeyValuePair<string, string>(keyValue.Left(kvSeparatorIndex), keyValue.Right(keyValue.Length - kvSeparatorIndex));
                    //}
                }
            }
        }

        public static Dictionary<string, string> ToDictionary(this string text, char Separator = ';', char keyValueSeparator = '=')
        {
            Dictionary<string, string> dict = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase    );
            if (!text.IsNullOrEmpty())
            {
                foreach (KeyValuePair<string, string> kvp in ToKeyValuePairs(text, Separator, keyValueSeparator))
                {
                    dict.AddOrUpdate(kvp.Key, kvp.Value);
                }
            }
            return dict;
        }

        #endregion ToKeyValuePairs Members

        #region ToObjectFromXml Overloads

        public static T ToObjectFromXml<T>(this string xml, XmlAttributeOverrides overrides = null)
        {
            return (T)ToObjectFromXml(xml, typeof(T), overrides);
        }

        public static object ToObjectFromXml(this string xml, Type type, XmlAttributeOverrides overrides = null)
        {
            if (xml.IsNullOrWhiteSpace())
                return null;
            if (type == null)
                throw new ArgumentNullException("Missing type.");

            using (StringReader reader = new StringReader(xml))
            {
                XmlSerializer serializer = overrides != null ? new XmlSerializer(type, overrides) : new XmlSerializer(type);
                return serializer.Deserialize(reader);
            }
        }

        #endregion ToObjectFromXml Overloads

        #region Repeat Overloads

        public static string Repeat(this string stringToRepeat, int repeat)
        {
            var builder = new StringBuilder(repeat * stringToRepeat.Length);
            for (int i = 0; i < repeat; i++)
            {
                builder.Append(stringToRepeat);
            }
            return builder.ToString();
        }

        #endregion Repeat Overloads

        #region Truncate Overloads

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) { return value; } 
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        #endregion Truncate Overloads

        public static string Left(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            maxLength = Math.Abs(maxLength);

            return (value.Length <= maxLength
                   ? value
                   : value.Substring(0, maxLength)
                   );
        }

        public static object ToObject(this string inString)
        {
            if (inString.IsNullOrWhiteSpace()) return inString;

            if (inString.IsInteger())
            {
                int intValue;

                if (Int32.TryParse(inString, out intValue))
                    return intValue;

                long longValue;
                if (Int64.TryParse(inString, out longValue))
                    return longValue;

                double doubleValue;
                if (Double.TryParse(inString, out doubleValue))
                    return doubleValue;

                return inString;
            }
            else if (inString.IsNumber())
                return double.Parse(inString);
            else
                return inString;
        }

        public static bool ContainsHeader(this string msg)
        {
            if (msg.IsNullOrEmpty())
                return false;

            return _headerRegex.IsMatch(msg);
        }

        /// <summary>
        /// Remove any non-word characters from a name (word characters are a-z, A-Z, 0-9, _)
        /// so that it may be used in code
        /// </summary>
        /// <param name="name">name to be cleaned</param>
        /// <returns>Cleaned up object name</returns>
        public static string GetCleanName(this string name)
        {
            return Regex.Replace(name, @"[\W]", "");
        }

        public static string ToDbFieldName(this string name)
        {
            if (name == null) return null;

            // Replace invalid characters with empty strings.
            try
            {
                return Regex.Replace(name, @"[^\w\.@-]", "",
                                     RegexOptions.None, TimeSpan.FromSeconds(1.5));
            }
            // If we timeout when replacing invalid characters, 
            // we should return Empty.
            catch (RegexMatchTimeoutException)
            {
                return String.Empty;
            }
        }

        /// <summary>
        /// Return a tab
        /// </summary>
        public static string Tab()
        {
            return Tab(1);
        }

        /// <summary>
        /// Return a specified number of tabs
        /// </summary>
        /// <param name="n">Number of tabs</param>
        /// <returns>n tabs</returns>
        public static string Tab(int n)
        {
            return new String('\t', n);
        }

        /// <summary>
        /// Return a newline
        /// </summary>
        public static string Newline()
        {
            return Newline(1);
        }

        /// <summary>
        /// Return a specified number of newlines
        /// </summary>
        /// <param name="n">Number of newlines</param>
        /// <returns>n tabs</returns>
        public static string Newline(int n)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                result.Append(Environment.NewLine);
            }
            return result.ToString();
        }

        /// <summary>
        /// Return a newline and the specified number of tabs
        /// </summary>
        /// <param name="n">Number of tabs</param>
        /// <returns>newline with specified number of tabs</returns>
        public static string NewlineAndTabs(int n)
        {
            return (Newline() + Tab(n));
        }

        /// <summary>
        /// Checks the string for any characters present
        /// </summary>
        /// <param name="name">String to check</param>
        /// <returns>True if characters are present, otherwise false</returns>
        public static bool HasCharacters(this string name)
        {
            return Regex.IsMatch(name, @"[a-zA-Z]");
        }

        /// <summary>
        /// Checks the string for any numerics present
        /// </summary>
        /// <param name="name">String to check</param>
        /// <returns>True if numerics are present, otherwise false</returns>		
        public static bool HasNumerics(this string name)
        {
            return Regex.IsMatch(name, @"[0-9]");
        }

        /// <summary>
        /// Checks the string to see if it starts with a numeric
        /// </summary>
        /// <param name="name">String to check</param>
        /// <returns>True if string starts with a numeric, otherwise false</returns>
        public static bool StartsWithNumeric(this string name)
        {
            return Regex.IsMatch(name, @"^[0-9]+");
        }

        /// <summary>
        /// Checks the string to see if it starts with a character
        /// </summary>
        /// <param name="name">String to check</param>
        /// <returns>True if string starts with a character, otherwise false</returns>
        public static bool StartsWithCharacter(this string name)
        {
            return Regex.IsMatch(name, @"^[a-zA-Z]+");
        }

        /// <summary>
        /// Checks the string to see if it is a valid variable name
        /// </summary>
        /// <param name="name">String to check</param>
        /// <returns>True if string is a valid variable name, otherwise false</returns>
        /// <remarks>Checks for (_ | {AlphaCharacter})({WordCharacter})*</remarks>
        public static bool IsValidVariableName(this string name)
        {
            return Regex.IsMatch(name, @"(_ | [a-zA-Z])([a-zA-Z_0-9])*");
        }

        /// <summary>
        /// Checks the string to see if it is a valid variable name
        /// </summary>
        /// <param name="name">String to check</param>
        /// <returns>True if string is a valid variable name, otherwise false</returns>
        /// <remarks>Checks for (_ | {AlphaCharacter})({WordCharacter})*</remarks>
        public static bool IsValidIdentifier(this string name)
        {
            return Regex.IsMatch(name, @"([a-zA-Z])([a-zA-Z_0-9])*");
        }

        /// <summary>
        /// Wraps long lines at the specified column number breaking on the specified break
        /// character.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="columnNumber"></param>
        /// <param name="lineContinuationCharacter">the character for the language that indicates a line continuation</param>
        /// <param name="breakCharacter">The character that should be used for breaking the string</param>
        /// <returns>a wrapped line</returns>
        public static string WrapLongLines(this string text, int columnNumber)
        {
            return WrapLongLines(text, columnNumber, String.Empty, ' ', 4);
        }

        /// <summary>
        /// Wraps long lines at the specified column number breaking on the specified break
        /// character.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="columnNumber"></param>
        /// <param name="lineContinuationCharacter">the character for the language that indicates a line continuation</param>
        /// <param name="breakCharacter">The character that should be used for breaking the string</param>
        /// <returns>a wrapped line</returns>
        public static string WrapLongLines(this string text, int columnNumber, int tabs)
        {
            return WrapLongLines(text, columnNumber, String.Empty, ' ', tabs);
        }

        /// <summary>
        /// Wraps long lines at the specified column number breaking on the specified break
        /// character.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="columnNumber"></param>
        /// <param name="lineContinuationCharacter">the character for the language that indicates a line continuation</param>
        /// <param name="breakCharacter">The character that should be used for breaking the string</param>
        /// <returns>a wrapped line</returns>
        public static string WrapLongLines(this string text, int columnNumber, string lineContinuationCharacter, char breakCharacter)
        {
            return WrapLongLines(text, columnNumber, lineContinuationCharacter, breakCharacter, 4);
        }

        /// <summary>
        /// Wraps long lines at the specified column number breaking on the specified break
        /// character.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="columnNumber"></param>
        /// <param name="lineContinuationCharacter">the character for the language that indicates a line continuation</param>
        /// <param name="breakCharacter">The character that should be used for breaking the string</param>
        /// <param name="tabs">Number of tabs to indent the wrapped lines</param>
        /// <returns>a wrapped line</returns>
        public static string WrapLongLines(this string text, int columnNumber, string lineContinuationCharacter, char breakCharacter, int tabs)
        {
            if (String.IsNullOrEmpty(text)) return text;

            // if the line is less than column number just return it
            if (text.Length <= columnNumber)
                return text;

            StringBuilder result = new StringBuilder();
            int stringLength = text.Length;
            string subString;
            int startPosition = 0;

            // loop through ever column number characters
            while (startPosition < stringLength)
            {
                // check if the startPosition + columnNumber is greater than the stringLength
                if ((startPosition + columnNumber) > stringLength)
                {
                    // the substring is less than the columnNumber we're at the 
                    // last part so just add it and exit				
                    subString = text.Substring(startPosition);
                    result.Append(subString);
                    break;
                }
                // get the substring we're working with
                subString = text.Substring(startPosition, columnNumber);

                // not at the end so get the position of the last space
                int lastBreak = subString.LastIndexOf(breakCharacter);
                lastBreak++;
                // check that we got one
                result.Append(subString.Substring(0, lastBreak));
                result.Append(lineContinuationCharacter);
                result.Append(Newline());
                result.Append(Tab(tabs));

                // set the next position
                startPosition += lastBreak;
            }

            return result.ToString();
        }

        #region Contains Overloads (Public)

        public static bool Contains(char inChar, char[] findInChars)
        {
            foreach (char findInChar in findInChars)
            {
                if (findInChar == inChar) return true;
            }
            return false;
        }

        public static bool Contains(string text, int index, char[] findInChars)
        {
            char inChar = text[index];
            foreach (char findInChar in findInChars)
            {
                if (findInChar == inChar) return true;
            }
            return false;
        }

        public static bool Contains(string text, int index, string findInText)
        {
            index = index - (findInText.Length - 1);
            if (index < 0) return false;

            return text.IndexOf(findInText, index) == index;
        }

        #endregion Contains Overloads (Public)

        #region Other Members (Private)

        private static bool Contains(string text, int index, object findInChars)
        {
            if (findInChars is char[])
                return Contains(text, index, ((char[])findInChars));
            else if (findInChars is string)
                return Contains(text, index, ((string)findInChars));
            else
                return false;
        }

        private static string NormalizeString(string inString)
        {
            if (inString == null || inString.Length == 0) return inString;
            if (inString.Contains("\"\""))
                return inString.Replace("\"\"", "\"");
            //else if (inString.Contains("''"))
            //    return inString.Replace("''", "'");
            else
                return inString;
        }

        #endregion Contains Member (Private)

        #region NTrim Method

        public static string NTrim(this string text)
        {
            return text == null ? null : text.Trim();
        }

        #endregion NTrim Method

        #region ToByteArray Method

        public static byte[] ToByteArray(this string text)
        {
            if (text.IsNullOrEmpty())
                throw new ArgumentException("Text");

            byte[] byteArray = new byte[text.Length / 3];
            int i = 0;
            int j = 0;
            do
            {
                byteArray[j++] = byte.Parse(text.Substring(i, 3));
                i += 3;
            }
            while (i < text.Length);

            return byteArray;
        }

        #endregion ToByteArray Method

        #region Compare Overloads

        //
        // Summary:
        //     Compares two specified System.String objects.
        //
        // Parameters:
        //   strA:
        //     The first System.String.
        //
        //   strB:
        //     The second System.String.
        //
        // Returns:
        //     A 32-bit signed integer indicating the lexical relationship between the two
        //     comparands.Value Condition Less than zero strA is less than strB. Zero strA
        //     equals strB. Greater than zero strA is greater than strB.
        public static int Compare(this string strA, string strB)
        {
            return String.Compare(strA, strB);
        }

        //
        // Summary:
        //     Compares two specified System.String objects, ignoring or honoring their
        //     case.
        //
        // Parameters:
        //   strA:
        //     The first System.String.
        //
        //   strB:
        //     The second System.String.
        //
        //   ignoreCase:
        //     A System.Boolean indicating a case-sensitive or insensitive comparison. (true
        //     indicates a case-insensitive comparison.)
        //
        // Returns:
        //     A 32-bit signed integer indicating the lexical relationship between the two
        //     comparands.Value Condition Less than zero strA is less than strB. Zero strA
        //     equals strB. Greater than zero strA is greater than strB.
        public static int Compare(string strA, string strB, bool ignoreCase)
        {
            return String.Compare(strA, strB, ignoreCase);
        }

        //
        // Summary:
        //     Compares two specified System.String objects. A parameter specifies whether
        //     the comparison uses the current or invariant culture, honors or ignores case,
        //     and uses word or ordinal sort rules.
        //
        // Parameters:
        //   strA:
        //     The first System.String object.
        //
        //   strB:
        //     The second System.String object.
        //
        //   comparisonType:
        //     One of the System.StringComparison values.
        //
        // Returns:
        //     A 32-bit signed integer indicating the lexical relationship between the two
        //     comparands.Value Condition Less than zero strA is less than strB. Zero strA
        //     equals strB. Greater than zero strA is greater than strB.
        //
        // Exceptions:
        //   System.ArgumentException:
        //     comparisonType is not a System.StringComparison value.
        //
        //   System.NotSupportedException:
        //     System.StringComparison is not supported.
        public static int Compare(string strA, string strB, StringComparison comparisonType)
        {
            return String.Compare(strA, strB, comparisonType);
        }

        //
        // Summary:
        //     Compares two specified System.String objects, ignoring or honoring their
        //     case, and using culture-specific information to influence the comparison.
        //
        // Parameters:
        //   strA:
        //     The first System.String.
        //
        //   strB:
        //     The second System.String.
        //
        //   ignoreCase:
        //     A System.Boolean indicating a case-sensitive or insensitive comparison. (true
        //     indicates a case-insensitive comparison.)
        //
        //   culture:
        //     A System.Globalization.CultureInfo object that supplies culture-specific
        //     comparison information.
        //
        // Returns:
        //     A 32-bit signed integer indicating the lexical relationship between the two
        //     comparands.Value Condition Less than zero strA is less than strB. Zero strA
        //     equals strB. Greater than zero strA is greater than strB.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     culture is null.
        public static int Compare(string strA, string strB, bool ignoreCase, CultureInfo culture)
        {
            return String.Compare(strA, strB, ignoreCase, culture);
        }

        public static int Compare(string strA, string strB, CultureInfo culture, CompareOptions options)
        {
            return String.Compare(strA, strB, culture, options);
        }

        //
        // Summary:
        //     Compares substrings of two specified System.String objects.
        //
        // Parameters:
        //   strA:
        //     The first System.String.
        //
        //   indexA:
        //     The position of the substring within strA.
        //
        //   strB:
        //     The second System.String.
        //
        //   indexB:
        //     The position of the substring within strB.
        //
        //   length:
        //     The maximum number of characters in the substrings to compare.
        //
        // Returns:
        //     A 32-bit signed integer indicating the lexical relationship between the two
        //     comparands.Value Condition Less than zero The substring in strA is less than
        //     the substring in strB. Zero The substrings are equal, or length is zero.
        //     Greater than zero The substring in strA is greater than the substring in
        //     strB.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     indexA is greater than strA.System.String.Length.-or- indexB is greater than
        //     strB.System.String.Length.-or- indexA, indexB, or length is negative. -or-Either
        //     indexA or indexB is null, and length is greater than zero.
        public static int Compare(string strA, int indexA, string strB, int indexB, int length)
        {
            return String.Compare(strA, indexA, strB, indexB, length);
        }

        //
        // Summary:
        //     Compares substrings of two specified System.String objects, ignoring or honoring
        //     their case.
        //
        // Parameters:
        //   strA:
        //     The first System.String.
        //
        //   indexA:
        //     The position of the substring within strA.
        //
        //   strB:
        //     The second System.String.
        //
        //   indexB:
        //     The position of the substring within strB.
        //
        //   length:
        //     The maximum number of characters in the substrings to compare.
        //
        //   ignoreCase:
        //     A System.Boolean indicating a case-sensitive or insensitive comparison. (true
        //     indicates a case-insensitive comparison.)
        //
        // Returns:
        //     A 32-bit signed integer indicating the lexical relationship between the two
        //     comparands.ValueCondition Less than zero The substring in strA is less than
        //     the substring in strB. Zero The substrings are equal, or length is zero.
        //     Greater than zero The substring in strA is greater than the substring in
        //     strB.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     indexA is greater than strA.System.String.Length.-or- indexB is greater than
        //     strB.System.String.Length.-or- indexA, indexB, or length is negative. -or-Either
        //     indexA or indexB is null, and length is greater than zero.
        public static int Compare(string strA, int indexA, string strB, int indexB, int length, bool ignoreCase)
        {
            return String.Compare(strA, indexA, strB, indexB, length, ignoreCase);
        }

        //
        // Summary:
        //     Compares substrings of two specified System.String objects.
        //
        // Parameters:
        //   strA:
        //     The first System.String object.
        //
        //   indexA:
        //     The position of the substring within strA.
        //
        //   strB:
        //     The second System.String object.
        //
        //   indexB:
        //     The position of the substring within strB.
        //
        //   length:
        //     The maximum number of characters in the substrings to compare.
        //
        //   comparisonType:
        //     One of the System.StringComparison values.
        //
        // Returns:
        //     A 32-bit signed integer indicating the lexical relationship between the two
        //     comparands.Value Condition Less than zero The substring in the strA parameter
        //     is less than the substring in the strB parameter.Zero The substrings are
        //     equal, or the length parameter is zero. Greater than zero The substring in
        //     strA is greater than the substring in strB.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     indexA is greater than strA.System.String.Length.-or- indexB is greater than
        //     strB.System.String.Length.-or- indexA, indexB, or length is negative. -or-Either
        //     indexA or indexB is null, and length is greater than zero.
        //
        //   System.ArgumentException:
        //     comparisonType is not a System.StringComparison value.
        public static int Compare(string strA, int indexA, string strB, int indexB, int length, StringComparison comparisonType)
        {
            return String.Compare(strA, indexA, strB, indexB, length, comparisonType);
        }

        //
        // Summary:
        //     Compares substrings of two specified System.String objects, ignoring or honoring
        //     their case, and using culture-specific information to influence the comparison.
        //
        // Parameters:
        //   strA:
        //     The first System.String.
        //
        //   indexA:
        //     The position of the substring within strA.
        //
        //   strB:
        //     The second System.String.
        //
        //   indexB:
        //     The position of the substring within the strB.
        //
        //   length:
        //     The maximum number of characters in the substrings to compare.
        //
        //   ignoreCase:
        //     A System.Boolean indicating a case-sensitive or insensitive comparison. (true
        //     indicates a case-insensitive comparison.)
        //
        //   culture:
        //     A System.Globalization.CultureInfo object that supplies culture-specific
        //     comparison information.
        //
        // Returns:
        //     An integer indicating the lexical relationship between the two comparands.Value
        //     Condition Less than zero The substring in strA is less than the substring
        //     in strB. Zero The substrings are equal, or length is zero. Greater than zero
        //     The substring in strA is greater than the substring in strB.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     indexA is greater than strA.System.String.Length.-or- indexB is greater than
        //     strB.System.String.Length.-or- indexA, indexB, or length is negative. -or-Either
        //     indexA or indexB is null, and length is greater than zero.
        //
        //   System.ArgumentNullException:
        //     culture is null.
        public static int Compare(string strA, int indexA, string strB, int indexB, int length, bool ignoreCase, CultureInfo culture)
        {
            return String.Compare(strA, indexA, strB, indexB, length, ignoreCase, culture);
        }

        #endregion Compare Overloads

        #region CompareOrdinal Overloads

        //
        // Summary:
        //     Compares two specified System.String objects by evaluating the numeric values
        //     of the corresponding System.Char objects in each string.
        //
        // Parameters:
        //   strA:
        //     The first System.String.
        //
        //   strB:
        //     The second System.String.
        //
        // Returns:
        //     An integer indicating the lexical relationship between the two comparands.ValueCondition
        //     Less than zero strA is less than strB. Zero strA and strB are equal. Greater
        //     than zero strA is greater than strB.
        public static int CompareOrdinal(string strA, string strB)
        {
            return CompareOrdinal(strA, strB);
        }

        //
        // Summary:
        //     Compares substrings of two specified System.String objects by evaluating
        //     the numeric values of the corresponding System.Char objects in each substring.
        //
        // Parameters:
        //   strA:
        //     The first System.String.
        //
        //   indexA:
        //     The starting index of the substring in strA.
        //
        //   strB:
        //     The second System.String.
        //
        //   indexB:
        //     The starting index of the substring in strB.
        //
        //   length:
        //     The maximum number of characters in the substrings to compare.
        //
        // Returns:
        //     A 32-bit signed integer indicating the lexical relationship between the two
        //     comparands.ValueCondition Less than zero The substring in strA is less than
        //     the substring in strB. Zero The substrings are equal, or length is zero.
        //     Greater than zero The substring in strA is greater than the substring in
        //     strB.
        //
        // Exceptions:
        //   System.ArgumentOutOfRangeException:
        //     indexA is greater than strA. System.String.Length.-or- indexB is greater
        //     than strB. System.String.Length.-or- indexA, indexB, or length is negative.
        public static int CompareOrdinal(string strA, int indexA, string strB, int indexB, int length)
        {
            return CompareOrdinal(strA, indexA, strB, indexB, length);
        }

        #endregion CompareOrdinal Overloads

        #region ContainsXml Methods

        public static bool ContainsXml(this string input)
        {
            if (input.IsNullOrWhiteSpace()) return false;

            try
            {
                XElement x = XElement.Parse("<wrapper>" + input + "</wrapper>");
                return !(x.DescendantNodes().Count() == 1 && x.DescendantNodes().First().NodeType == XmlNodeType.Text);
            }
            catch (XmlException)
            {
                return true;
            }
        }

        #endregion ContainsHTML Methods

        #region Join Overloads

        public static string Join<T>(IEnumerable<T> list, Func<T, string> valueFunc, string Separator = ",", string prefix = null, string postFix = null)
        {
            ChoGuard.ArgumentNotNull(list, "List");
            ChoGuard.ArgumentNotNull(valueFunc, "ValueFunc");

            bool first = true;
            StringBuilder msg = new StringBuilder();
            foreach (var value in list)
            {
                if (first)
                {
                    if (!prefix.IsNullOrEmpty())
                        msg.Append(prefix);
                    first = false;
                }
                else if (!Separator.IsNullOrEmpty())
                    msg.Append(Separator);

                msg.AppendFormat("{0}", valueFunc(value));
            }

            if (!postFix.IsNullOrEmpty())
                msg.Append(postFix);
            
            return msg.ToString();
        }

        #endregion Join Overloads

        #region ToMaskedConnectionString Methods

        public static string ToMaskedSqlConnectionString(this string connectionString)
        {
            System.Data.SqlClient.SqlConnectionStringBuilder sb = new System.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            sb.Password = "XXXXX";
            return sb.ToString();

        }

        #endregion ToMaskedConnectionString Methods
    }
}
