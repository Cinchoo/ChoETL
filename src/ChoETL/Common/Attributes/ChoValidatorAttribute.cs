namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.ComponentModel;
    using System.Collections.Generic;

    #endregion

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ChoValidatorAttribute : ChoTypeConverterAttribute
    {
        public ChoValidatorAttribute(Type converterType)
            : base(converterType)
        {
        }

        public ChoValidatorAttribute(string typeConverterTypeName)
            : base(typeConverterTypeName)
        {
        }
    }
}
