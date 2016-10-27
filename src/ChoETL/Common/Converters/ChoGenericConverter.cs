using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    public class ChoGenericConverter : IValueConverter
    {
        public Delegate ConvertOperation { get; private set; }
        public string ConvertCode { get; private set; }
        public Delegate ConvertBackOperation { get; private set; }
        public string ConvertBackCode { get; private set; }

        public ChoGenericConverter()
        {
        }

        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            ConvertCode = parameter.GetValueAt<string>(0);
            if (ConvertCode.IsNullOrWhiteSpace())
                return value;

            if (this.ConvertOperation == null)
            {
                this.ConvertOperation = ConstructOperation(value, targetType);
            }
            return this.ConvertOperation.DynamicInvoke(value);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            ConvertBackCode = parameter.GetValueAt<string>(1);
            if (ConvertBackCode.IsNullOrWhiteSpace())
                return value;

            if (this.ConvertOperation == null)
            {
                this.ConvertBackOperation = ConstructOperation(value, targetType);
            }
            return this.ConvertBackOperation.DynamicInvoke(value);
        }

        private Delegate ConstructOperation(object value, Type targetType)
        {
            int opi = this.ConvertCode.IndexOf("=>");
            if (opi < 0) throw new Exception("No lambda operator =>");
            string param = this.ConvertCode.Substring(0, opi).NTrim();
            string body = this.ConvertCode.Substring(opi + 2).NTrim();
            ParameterExpression p = Expression.Parameter(
                value.GetType(), param);
            LambdaExpression lambda = DynamicExpression.ParseLambda(
                new ParameterExpression[] { p }, targetType, body, value);
            return lambda.Compile();
        }
    }

}
