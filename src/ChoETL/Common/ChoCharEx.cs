namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    #endregion NameSpaces

    public static class ChoCharEx
    {
        #region Constants (Public)

        [Description("Horizontal Tab")]
        public const char HorizontalTab = '\t';

        [Description("Null Character")]
        public const char NUL = '\0';

        [Description("Veritcal Tab")]
        public const char VerticalTab = '\v';

        [Description("Escape")]
        public const char Escape = (char)0x1B;

        [Description("Backspace")]
        public const char BackSpace = '\b';

        [Description("Carriage Return")]
        public const char CarriageReturn = '\r';

        [Description("LineFeed")]
        public const char LineFeed = '\n';

        [Description("Formfeed")]
        public const char Formfeed = '\f';

        [Description("Alert")]
        public const char BEL = '\a';

        [Description("Backslash")]
        public const char Backslash = '\\';

        [Description("Question Mark")]
        public const char QuestionMark = (char)0x63;

        [Description("Single Quotation Mark")]
        public const char SingleQuotationMark = '\'';

        [Description("Double Quotation Mark")]
        public const char DoubleQuotationMark = '\"';

        #endregion Constants (Public)

        #region Repeat Overloads

        public static string Repeat(this char chatToRepeat, int repeat)
        {
            return new string(chatToRepeat, repeat);
        }

        #endregion Repeat Overloads
    }
}
