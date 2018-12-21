using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
#if !NETSTANDARD2_0
    public class ChoCustomCodeConverter : IValueConverter
#else
    public class ChoCustomCodeConverter : IChoValueConverter
#endif
    {
        public ChoCodeDomProvider ConvertOperation { get; private set; }
        public string ConvertCode { get; set; }
        public ChoCodeDomProvider ConvertBackOperation { get; private set; }
        public string ConvertBackCode { get; set; }
        public ChoCodeProviderLanguage Language
        {
            get;
            set;
        }

        public string NameSpaces
        {
            get;
            set;
        }

        public ChoCustomCodeConverter()
        {
        }

        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            string convertCode = parameter.GetValueAt<string>(0);
            ConvertCode = convertCode.IsNullOrWhiteSpace() ? ConvertCode : convertCode;
            if (ConvertCode.IsNullOrWhiteSpace())
                return value;

            if (this.ConvertOperation == null)
            {
                this.ConvertOperation = ConstructOperation(ConvertCode, value);
            }
            return System.Convert.ChangeType(ConvertOperation.ExecuteFunc(value), targetType);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            string convertBackCode = parameter.GetValueAt<string>(1);
            ConvertBackCode = convertBackCode.IsNullOrWhiteSpace() ? ConvertBackCode : convertBackCode;
            if (ConvertBackCode.IsNullOrWhiteSpace())
                return value;

            if (this.ConvertOperation == null)
            {
                this.ConvertBackOperation = ConstructOperation(ConvertBackCode, value);
            }
            return System.Convert.ChangeType(ConvertBackOperation.ExecuteFunc(value), targetType);
        }

        private ChoCodeDomProvider ConstructOperation(string codeSnippet, object value)
        {
            if (codeSnippet.IsNullOrEmpty()) return null;

            string[] namespaces = NameSpaces.IsNullOrWhiteSpace() ? null : NameSpaces.SplitNTrim(';');
            int opi = codeSnippet.IndexOf("=>");
            if (opi < 0) return null; // throw new Exception("No lambda operator =>");
            string param = codeSnippet.Substring(0, opi).NTrim();

            if (Language == ChoCodeProviderLanguage.VB)
            {
                if (!ChoCodeDomProvider.IsValidVBIdentifier(param))
                    throw new ApplicationException("Invalid VB identifier found.");
            }
            else
            {
                if (!ChoCodeDomProvider.IsValidCSharpIdentifier(param))
                    throw new ApplicationException("Invalid C# identifier found.");
            }

            string codeBlock = codeSnippet.Substring(opi + 2).NTrim();

            if (!codeBlock.Contains(";") && !codeBlock.StartsWith("return"))
                codeBlock = "return {0};".FormatString(codeBlock);

            var cd = new ChoCodeDomProvider(new string[] { codeBlock }, namespaces, Language);
            cd.BuildFunc(param, value.GetType());

            return cd;
        }
    }

}
