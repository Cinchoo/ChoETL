namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Linq;

    #endregion

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ChoTypeConverterAttribute : Attribute
    {
        #region Instance Properties

        internal int? PriorityInternal;
        public int Priority
        {
            get { throw new NotSupportedException(); }
            set { PriorityInternal = value; }
        }

        private Type _converterType;
        public Type ConverterType
        {
            get { return _converterType; }
        }

        internal object ParametersObject
        {
            get
            {
                if (ParametersDict != null && ParametersDict.Count > 0)
                    return ParametersDict;
                else
                    return ParametersArray;
            }
        }
        internal object[] ParametersArray { get; set; }

        internal IDictionary<string, string> ParametersDict { get; set; }

        private string _parameters;
        public string Parameters
        {
            get { return _parameters; }
            set
            {
                if (_parameters != value)
                {
                    _parameters = value;
                    if (value != null)
                    {
                        if (value.Contains("="))
                        {
                            ParametersDict = value.ToKeyValuePairs().ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.InvariantCultureIgnoreCase);
                        }
                        else
                            ParametersArray = value.SplitNTrim(new char[] { ',', ';' }, ChoStringSplitOptions.None, '\'').AsTypedEnumerable<object>().ToArray();
                    }
                    else
                        ParametersArray = null;
                }
            }
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

            //if (ChoGuard.IsArgumentNotNullOrEmpty(Parameters) && ChoType.HasConstructor(ConverterType, ParametersArray1))
            //    return ChoType.CreateInstance(ConverterType, ParametersArray1);
            //else if (ChoType.HasConstructor(ConverterType, new object[] { String.Empty }))
            //    return ChoType.CreateInstance(ConverterType, new object[] { ParametersArray1 != null && ParametersArray1.Length > 0 ? ParametersArray1[0] : String.Empty });
            //else
            if ((ParametersArray == null || ParametersArray.Length == 0) && (ParametersDict == null || ParametersDict.Count == 0))
                return ChoActivator.CreateInstance(ConverterType);
            else
            {
                if (ParametersDict != null || ParametersDict.Count > 0)
                {
                    try
                    {
                        return ChoActivator.CreateInstance(ConverterType, ParametersDict);
                    }
                    catch
                    {
                        return ChoActivator.CreateInstance(ConverterType);
                    }
                }
                else
                {
                    try
                    {
                        return ChoActivator.CreateInstance(ConverterType, ParametersArray);
                    }
                    catch
                    {
                        return ChoActivator.CreateInstance(ConverterType);
                    }
                }
            }
        }

        #endregion Instance Members (Internal)

        public override object TypeId
        {
            get
            {
                return ChoIntRandom.Next(1, Int32.MaxValue);
            }
        }
    }

    public class ChoCustomSerializerAttribute : ChoTypeConverterAttribute
    {
        #region Constructors

        public ChoCustomSerializerAttribute(Type converterType) : base(converterType)
        {
        }

        public ChoCustomSerializerAttribute(string typeConverterTypeName) : this(ChoType.GetType(typeConverterTypeName))
        {
        }

        #endregion Constructors
    }
}
