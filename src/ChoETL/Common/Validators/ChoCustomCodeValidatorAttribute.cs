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

        private string _defaultValueCodeSnippet { get; set; }
        private ChoCodeDomProvider cs = null;

        public ChoCustomCodeValidatorAttribute(string validationCodeSnippet)
        {
            ChoGuard.ArgumentNotNullOrEmpty(validationCodeSnippet, "CodeSnippet");

            _defaultValueCodeSnippet = validationCodeSnippet;
        }

        public override bool IsValid(object value)
        {
            if (cs == null)
                cs = ConstructOperation(value);

            if (cs != null)
                return (bool)Convert.ChangeType(cs.ExecuteFunc(ParamType == null ? value : Convert.ChangeType(value, ParamType)), typeof(bool));
            else
                return base.IsValid(value);
        }

        private ChoCodeDomProvider ConstructOperation(object value)
        {
            if (_defaultValueCodeSnippet.IsNullOrEmpty()) return null;

            string[] namespaces = NameSpaces.IsNullOrWhiteSpace() ? null : NameSpaces.SplitNTrim(';');

            int opi = this._defaultValueCodeSnippet.IndexOf("=>");
            if (opi < 0) return null; // throw new Exception("No lambda operator =>");
            string param = this._defaultValueCodeSnippet.Substring(0, opi).NTrim();

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

            string codeBlock = this._defaultValueCodeSnippet.Substring(opi + 2).NTrim();

            if (!codeBlock.Contains(";") && !codeBlock.StartsWith("return"))
                codeBlock = "return {0};".FormatString(codeBlock);

            var cd = new ChoCodeDomProvider(new string[] { codeBlock }, namespaces, Language);
            cd.BuildFunc(param, ParamType == null ? value.GetType() : ParamType);

            return cd;
        }
    }
}
