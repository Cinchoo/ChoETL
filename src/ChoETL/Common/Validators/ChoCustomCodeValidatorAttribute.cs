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
    public class ChoCustomCodeValidatorAttribute : ValidationAttribute
    {
        public Type ParamType
        {
            get;
            set;
        }

        private string _defaultValueCodeSnippet { get; set; }
        public ChoCustomCodeValidatorAttribute(string validationCodeSnippet)
        {
            _defaultValueCodeSnippet = validationCodeSnippet;
        }

        public override bool IsValid(object value)
        {
            return (bool)ConstructOperation(value, typeof(bool));
        }

        private object ConstructOperation(object value, Type targetType)
        {
            if (_defaultValueCodeSnippet.IsNullOrEmpty()) return null;

            int opi = this._defaultValueCodeSnippet.IndexOf("=>");
            if (opi < 0) return null; // throw new Exception("No lambda operator =>");
            string param = this._defaultValueCodeSnippet.Substring(0, opi).NTrim();
            string codeBlock = this._defaultValueCodeSnippet.Substring(opi + 2).NTrim();

            if (!codeBlock.Contains(";") && !codeBlock.StartsWith("return"))
                codeBlock = "return {0};".FormatString(codeBlock);

            using (ChoCodeDomProvider cs = new ChoCodeDomProvider(new string[] { codeBlock }))
                return Convert.ChangeType(cs.ExecuteFunc(param, ParamType == null ? value.GetType() : ParamType, value), targetType);
        }
    }
}
