namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    #endregion NameSpaces

    public class ChoWildcard : Regex
    {
        #region Constructors

        /// <summary>
        /// Initializes a wildcard with the given search pattern.
        /// </summary>
        /// <param name="pattern">The wildcard pattern to match.</param>
        public ChoWildcard(string pattern)
            : base(WildcardToRegex(pattern))
        {
        }

        /// <summary>
        /// Initializes a wildcard with the given search pattern and options.
        /// </summary>
        /// <param name="pattern">The wildcard pattern to match.</param>
        /// <param name="options">A combination of one or more
        /// <see cref="System.Text.RegexOptions"/>.</param>
        public ChoWildcard(string pattern, RegexOptions options)
            : base(WildcardToRegex(pattern), options)
        {
        }

        #endregion Constructors

        #region Shared Members (Public)

        /// <summary>
        /// Converts a wildcard to a regex.
        /// </summary>
        /// <param name="pattern">The wildcard pattern to convert.</param>
        /// <returns>A regex equivalent of the given wildcard.</returns>
        public static string WildcardToRegex(string pattern)
        {
            if (pattern.IsNullOrWhiteSpace())
                return pattern;

            //return "^" + Regex.Escape(pattern).Replace("\\*", "(.*)"). Replace("\\?", "(.)") + "$";
            int index = 0;
            int counter = 0;
            StringBuilder output = new StringBuilder();

            char ch;
            pattern = Regex.Escape(pattern);
            while (index < pattern.Length)
            {
                ch = pattern[index];
                if (ch == '\\')
                {
                    if (index + 1 < pattern.Length &&
                        (pattern[index + 1] == '*' || pattern[index + 1] == '?')
                        )
                    {
                        if (pattern[index + 1] == '*')
                            output.AppendFormat("(?<M{0}>.*)", ++counter);
                        else
                            output.AppendFormat("(?<S{0}>.)", ++counter);
                        index++;
                    }
                    else //if (pattern[index + 1] == '\\')
                    {
                        if (index + 1 < pattern.Length)
                            output.Append(ch);
                    }
                }
                else
                    output.Append(ch);

                index++;
            }

            return "^" + output.ToString() + "$";
        }

        internal static bool IsWildcardPattern(string pattern)
        {
            if (pattern.IsNullOrWhiteSpace())
                return false;

            pattern = Regex.Escape(pattern);
            if (pattern.IndexOf("\\*") >= 0)
                return true;
            else if (pattern.IndexOf("\\?") >= 0)
                return true;
            else
                return false;
        }

        #endregion Shared Members (Public)
    }

    //public class ChoWildcardReplace : Regex
    //{
    //    #region Constructors

    //    /// <summary>
    //    /// Initializes a wildcard with the given search pattern.
    //    /// </summary>
    //    /// <param name="pattern">The wildcard pattern to match.</param>
    //    public ChoWildcardReplace(string pattern)
    //        : base(WildcardToRegex(pattern))
    //    {
    //    }

    //    /// <summary>
    //    /// Initializes a wildcard with the given search pattern and options.
    //    /// </summary>
    //    /// <param name="pattern">The wildcard pattern to match.</param>
    //    /// <param name="options">A combination of one or more
    //    /// <see cref="System.Text.RegexOptions"/>.</param>
    //    public ChoWildcardReplace(string pattern, RegexOptions options)
    //        : base(WildcardToRegex(pattern), options)
    //    {
    //    }

    //    #endregion Constructors

    //    #region Shared Members (Public)

    //    /// <summary>
    //    /// Converts a wildcard to a regex.
    //    /// </summary>
    //    /// <param name="pattern">The wildcard pattern to convert.</param>
    //    /// <returns>A regex equivalent of the given wildcard.</returns>
    //    public static string WildcardToRegex(string pattern)
    //    {
    //        int index = 0;
    //        int counter = 0;
    //        StringBuilder output = new StringBuilder();

    //        char ch;
    //        pattern = Regex.Escape(pattern);
    //        while (index < pattern.Length)
    //        {
    //            ch = pattern[index];
    //            if (ch == '\\')
    //            {
    //                if (index + 1 < pattern.Length && 
    //                    (pattern[index + 1] == '*' || pattern[index + 1] == '?')
    //                    )
    //                {
    //                    index++;
    //                    output.AppendFormat("${0}", ++counter);
    //                }
    //                else //if (pattern[index + 1] == '\\')
    //                {
    //                    if (index + 1 < pattern.Length)
    //                        output.Append(ch);
    //                }
    //            }
    //            else
    //                output.Append(ch);

    //            index++;
    //        }
            
    //        return output.ToString();
    //    }

    //    #endregion Shared Members (Public)
    //}
}
