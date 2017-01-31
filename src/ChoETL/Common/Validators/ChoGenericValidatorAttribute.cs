using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class ChoGenericValidatorAttribute : ValidationAttribute
    {
        private string _defaultValueCodeSnippet { get; set; }
        public ChoGenericValidatorAttribute(string validationCodeSnippet)
        {
            _defaultValueCodeSnippet = validationCodeSnippet;
        }

        public override bool IsValid(object value)
        {
            Delegate _ops = ConstructOperation(value, typeof(bool));
            if (_ops != null)
                return (bool)_ops.DynamicInvoke(value);
            else
                return base.IsValid(value);
        }

        private Delegate ConstructOperation(object value, Type targetType)
        {
            if (_defaultValueCodeSnippet.IsNullOrEmpty()) return null;

            int opi = this._defaultValueCodeSnippet.IndexOf("=>");
            if (opi < 0) return null; // throw new Exception("No lambda operator =>");
            string param = this._defaultValueCodeSnippet.Substring(0, opi).NTrim();
            string body = this._defaultValueCodeSnippet.Substring(opi + 2).NTrim();
            ParameterExpression p = Expression.Parameter(
                value.GetType(), param);
            LambdaExpression lambda = DynamicExpression.ParseLambda(
                new ParameterExpression[] { p }, targetType, body, value);
            return lambda.Compile();
        }
    }
}
