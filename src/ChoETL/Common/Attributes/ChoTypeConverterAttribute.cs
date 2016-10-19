namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Windows.Data;

    #endregion

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ChoTypeConverterAttribute : Attribute
    {
        #region Instance Properties

        private int _priority;
        public int Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        private Type _converterType;
        public Type ConverterType
        {
            get { return _converterType; }
        }

        private object[] _parameters;
        public object[] Parameters
        {
            get { return _parameters == null ? new object[] { } : _parameters; }
            set { _parameters = value; }
        }

        public object Parameter
        {
            get { throw new NotSupportedException(); }
            set { _parameters = new object[] { value }; }
        }

        #endregion Instance Properties

        #region Constructors

        protected ChoTypeConverterAttribute()
        {
        }

        public ChoTypeConverterAttribute(Type converterType)
        {
            if (converterType != null)
            {
                //if (typeof(TypeConverter).IsAssignableFrom(converterType)
                //    || typeof(IValueConverter).IsAssignableFrom(converterType)
                //    )
                _converterType = converterType;
                //else
                //throw new ApplicationException("Invalid `{0}` Converter Type passed".FormatString(converterType.FullName));
            }
        }

        public ChoTypeConverterAttribute(string typeConverterTypeName) : this(ChoType.GetType(typeConverterTypeName))
        {
        }

        #endregion Constructors

        #region Instance Members (Internal)

        public virtual object CreateInstance()
        {
            if (ConverterType == null)
                return null;

            if (ChoGuard.IsArgumentNotNullOrEmpty(Parameters) && ChoType.HasConstructor(ConverterType, Parameters))
                return ChoType.CreateInstance(ConverterType, Parameters);
            else if (ChoType.HasConstructor(ConverterType, new object[] { String.Empty }))
                return ChoType.CreateInstance(ConverterType, new object[] { Parameters != null && Parameters.Length > 0 ? Parameters[0] : String.Empty });
            else
                return Activator.CreateInstance(ConverterType);
        }

        #endregion Instance Members (Internal)
    }
}
