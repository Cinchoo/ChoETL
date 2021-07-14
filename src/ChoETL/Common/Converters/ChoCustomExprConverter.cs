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
    public class ChoCustomExprConverter : IValueConverter, IChoCollectionConverter
#else
    public class ChoCustomExprConverter : IChoValueConverter, IChoCollectionConverter
#endif
    {
        public Delegate ConvertOperation { get; private set; }
        public string ConvertCode { get; set; }
        public Delegate ConvertBackOperation { get; private set; }
        public string ConvertBackCode { get; set; }

        public ChoCustomExprConverter()
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
                this.ConvertOperation = ConstructOperation(ConvertCode, value, targetType);
            }
            return this.ConvertOperation.DynamicInvoke(value);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            string convertBackCode = parameter.GetValueAt<string>(1);
            ConvertBackCode = convertBackCode.IsNullOrWhiteSpace() ? ConvertBackCode : convertBackCode;
            if (ConvertBackCode.IsNullOrWhiteSpace())
                return value;

            if (this.ConvertBackOperation == null)
            {
                this.ConvertBackOperation = ConstructOperation(ConvertBackCode, value, targetType);
            }
            return this.ConvertBackOperation.DynamicInvoke(value);
        }

        private Delegate ConstructOperation(string codeSnippet, object value, Type targetType)
        {
            int opi = codeSnippet.IndexOf("=>");
            if (opi < 0) throw new Exception("No lambda operator =>");
            string param = codeSnippet.Substring(0, opi).NTrim();
            string body = codeSnippet.Substring(opi + 2).NTrim();
            ParameterExpression p = Expression.Parameter(
                value.GetType(), param);
            LambdaExpression lambda = DynamicExpression.ParseLambda(
                new ParameterExpression[] { p }, targetType, body, value);
            return lambda.Compile();
        }
    }

}
