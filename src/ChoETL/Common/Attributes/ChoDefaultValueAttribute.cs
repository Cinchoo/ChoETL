using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.All)]
    public class ChoDefaultValueAttribute :Attribute //: DefaultValueAttribute
    {
        private Delegate _defaultValueOps { get; set; }
        private string _defaultValue { get; set; }
        public ChoDefaultValueAttribute(string defaultValueCodeSnippet)// : base(0)
        {
            _defaultValue = defaultValueCodeSnippet;
            this._defaultValueOps = ConstructOperation(defaultValueCodeSnippet);
        }

        public object Value
        {
            get
            {
                if (_defaultValueOps == null)
                    return _defaultValue;
                else
                    return _defaultValueOps.DynamicInvoke();
            }
        }
        private Delegate ConstructOperation(string defaultValueCodeSnippet)
        {
            if (defaultValueCodeSnippet.IsNullOrWhiteSpace())
                return null;

            int opi = defaultValueCodeSnippet.IndexOf("=>");
            if (opi < 0) return null;

            string param = defaultValueCodeSnippet.Substring(0, opi).NTrim();
            string body = defaultValueCodeSnippet.Substring(opi + 2).NTrim();
            LambdaExpression lambda = DynamicExpression.ParseLambda(typeof(object), body);
            return lambda.Compile();
        }
    }
}
