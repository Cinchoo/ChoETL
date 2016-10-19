namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.Text.RegularExpressions;

    #endregion NameSpaces

    public sealed class ChoStringMsgBuilder : IDisposable
    {
        #region Constants

        public static string Empty = "Empty";

        #endregion

        #region Instance Data Members (Private)

        private StringBuilder _msg = new StringBuilder();

        #endregion

        #region Constructors

        public ChoStringMsgBuilder()
        {
        }

        public ChoStringMsgBuilder(string header)
        {
            Regex r = new Regex(string.Format(@"\s*--.*{0}", Environment.NewLine));
            Match match = r.Match(header);
            if (match.Success)
                _msg.Append(header);
            else
                _msg.AppendFormat("-- {0} -- {1}", header, Environment.NewLine);
        }

        public ChoStringMsgBuilder(string headerFormat, params object[] args) : this(String.Format(headerFormat, args))
        {
        }

        #endregion

        #region Instance Properties (Public)

        public int Length
        {
            get { return _msg.Length; }
        }

        #endregion Instance Properties (Public)

        #region Instance Members (Public)

        public void AppendLine()
        {
            _msg.Append(Environment.NewLine);
        }

        public void AppendLine(string msg)
        {
            _msg.AppendFormat("{0}{1}", Normalize(msg), Environment.NewLine);
        }

        public void AppendLine(string format, params object[] args)
        {
            _msg.AppendFormat("{0}{1}", Normalize(String.Format(format, args)), Environment.NewLine);
        }

        public void AppendLineIfNoNL(string msg)
        {
            if (msg == null)
                return;

            if (msg.EndsWith(Environment.NewLine))
                Append(msg);
            else
                AppendLine(msg);
        }

        public void AppendLineIfNoNL(string format, params object[] args)
        {
            string msg = Normalize(String.Format(format, args));
            if (msg.EndsWith(Environment.NewLine))
                _msg.Append(msg);
            else
                _msg.AppendFormat("{0}{1}", msg, Environment.NewLine);
        }

        public void AppendFormatLine(string msg)
        {
            _msg.AppendFormat("{0}{1}", Normalize(msg.Indent(1)), Environment.NewLine);
        }

        public void AppendFormatLine(string format, params object[] args)
        {
            _msg.AppendFormat("{0}{1}", String.Format(format, args).Indent(1), Environment.NewLine);
        }

        public void AppendFormat(string msg)
        {
            _msg.Append(msg.Indent(1));
        }

        public void AppendFormat(string format, params object[] args)
        {
            _msg.Append(String.Format(format, args).Indent(1));
        }

        public void Append(string msg)
        {
            _msg.Append(msg);
        }

        public void Append(string format, params object[] args)
        {
            _msg.AppendFormat(format, args);
        }

        public void AppendNewLine()
        {
            _msg.Append(Environment.NewLine);
        }

        public new string ToString()
        {
            return _msg.ToString();
        }

        #endregion

        #region Instance Members (Public)

        public string Normalize(string msg)
        {
            if (msg == null || msg.Length == 0 || !msg.StartsWith("--")) return msg;

            Regex r = new Regex(string.Format(@"--.*--\s*{0}(?<body>.*){0}", Environment.NewLine));
            //Regex r = new Regex(string.Format(@"\s*--.*--\s*{0}(?<body>.*){0}", Environment.NewLine));

            Match match = r.Match(msg);
            if (match.Success)
                return match.Groups["body"].ToString();

            return msg;
        }

        #endregion Instance Members (Public)

        #region IDisposable Members

        public void Dispose()
        {
            _msg = null;
        }

        #endregion

        #region Finalizers

        ~ChoStringMsgBuilder()
        {
            Dispose();
        }

        #endregion
    }
}
