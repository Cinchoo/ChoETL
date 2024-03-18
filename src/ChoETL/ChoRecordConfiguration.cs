using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
    [DataContract]
    public abstract class ChoRecordConfiguration
    {
        protected Lazy<object> _recObject;
        protected ChoTypeConverterFormatSpec _typeConverterFormatSpec = null;

        public Func<string, object, object> ValueConverterBack
        {
            get;
            set;
        }
        public Func<string, object, object> ValueConverter
        {
            get;
            set;
        }

        public dynamic Context
        {
            get;
            protected set;
        } = new ChoDynamicObject();

        public char? ItemSeparator
        {
            get;
            set;
        }
        public Func<object, bool> Validator
        {
            get;
            set;
        }
        public ChoTypeConverterFormatSpec TypeConverterFormatSpec
        {
            get { return _typeConverterFormatSpec == null ? ChoTypeConverterFormatSpec.Instance : _typeConverterFormatSpec; }
            set { _typeConverterFormatSpec = value; }
        }
        internal ChoTypeConverterFormatSpec CreateTypeConverterSpecsIfNull()
        {
            if (_typeConverterFormatSpec == null)
                _typeConverterFormatSpec = new ChoTypeConverterFormatSpec();

            return _typeConverterFormatSpec;
        }
        private ChoFieldTypeAssessor _fieldTypeAssessor = null;
        public ChoFieldTypeAssessor FieldTypeAssessor
        {
            get { return _fieldTypeAssessor == null ? ChoFieldTypeAssessor.Instance : _fieldTypeAssessor; }
            set { _fieldTypeAssessor = value; }
        }
        protected Type RecordType
        {
            get;
            set;
        }

        private Type _recordMapType;
        protected Type RecordMapType
        {
            get { return _recordMapType == null ? RecordType : _recordMapType; }
            set { _recordMapType = value; }
        }

        internal Type RecordTypeInternal
        {
            get => RecordType;
            set => RecordType = value;
        }
        internal Type RecordMapTypeInternal => RecordMapType;

        [DataMember]
        public ChoErrorMode? ErrorMode
        {
            get;
            set;
        }
        [DataMember]
        public ChoIgnoreFieldValueMode? IgnoreFieldValueMode
        {
            get;
            set;
        }
        [DataMember]
        public bool AutoDiscoverColumns
        {
            get;
            set;
        }
        [DataMember]
        public bool ThrowAndStopOnMissingField
        {
            get;
            set;
        }
        [DataMember]
        public ChoObjectValidationMode ObjectValidationMode
        {
            get;
            set;
        }
        protected Type SourceType
        {
            get;
            set;
        }
        internal Type SourceTypeInternal
        {
            get => SourceType;
            set => SourceType = value;
        }
        [DataMember]
        public long NotifyAfter { get; set; }

        private bool _isDynamicObject = true;
        protected virtual bool IsDynamicObject
        {
            get { return _isDynamicObject; }
            set { _isDynamicObject = value; }
        }
        internal bool IsDynamicObjectInternal
        {
            get => IsDynamicObject;
            set => IsDynamicObject = value;
        }
        protected Dictionary<string, PropertyInfo> PIDict = null;
        protected Dictionary<string, PropertyDescriptor> PDDict = null;
        internal Dictionary<string, PropertyInfo> PIDictInternal
        {
            get => PIDict;
            set => PIDict = value;
        }
        internal Dictionary<string, PropertyDescriptor> PDDictInternal
        {
            get => PDDict;
            set => PDDict = value;
        }

        internal bool HasConfigValidators = false;
        internal Dictionary<string, ValidationAttribute[]> ValDict = null;
        internal string[] PropertyNames;
        private HashSet<string> _ignoredFields = new HashSet<string>();
        public HashSet<string> IgnoredFields
        {
            get { return _ignoredFields; }
            set
            {
                if (value != null)
                    _ignoredFields = value;
                else
                    _ignoredFields.Clear();
            }
        }
        public abstract IEnumerable<ChoRecordFieldConfiguration> RecordFieldConfigurations
        {
            get;
        }

        internal ChoRecordConfiguration(Type recordType = null)
        {
            RecordType = recordType.GetUnderlyingType();
            ErrorMode = ChoErrorMode.ThrowAndStop; //  ChoErrorMode.ReportAndContinue;
            AutoDiscoverColumns = true;
            ThrowAndStopOnMissingField = true;
            ObjectValidationMode = ChoObjectValidationMode.Off;
            IsDynamicObject = RecordType.IsDynamicType();
        }

        public Action<ChoRecordConfiguration> StateInitializer
        {
            get;
            set;
        }
        protected virtual void ResetStates()
        {
            Context = new ChoDynamicObject();
            StateInitializer?.Invoke(this);
        }
        internal void ResetStatesInternal()
        {
            ResetStates();
        }

        protected virtual void Init(Type recordType)
        {
            if (recordType == null)
                return;

            var tc = recordType.GetCustomAttribute(typeof(ChoTypeConverterAttribute)) as ChoTypeConverterAttribute;
            if (tc != null)
            {
                var c = tc.CreateInstance();
                if (c is IChoValueConverter)
                    ChoTypeConverter.Global.Add(recordType, c as IChoValueConverter);
                else if (c is TypeConverter)
                    ChoTypeConverter.Global.Add(recordType, c as TypeConverter);
#if !NETSTANDARD2_0
                else if (c is IValueConverter)
                    ChoTypeConverter.Global.Add(recordType, c as IValueConverter);
#endif
            }

            var st = recordType.GetCustomAttribute(typeof(ChoSourceTypeAttribute)) as ChoSourceTypeAttribute;
            if (st != null)
            {
                SourceType = st.Type;
            }

            _recObject = new Lazy<object>(() => ChoActivator.CreateInstance(RecordType));
            ChoRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                ErrorMode = recObjAttr.ErrorMode;
                IgnoreFieldValueMode = recObjAttr.IgnoreFieldValueModeInternal;
                ThrowAndStopOnMissingField = recObjAttr.ThrowAndStopOnMissingField;
                ObjectValidationMode = recObjAttr.ObjectValidationMode;
            }
        }

        internal void ValidateInternal(object state)
        {
            Validate(state);
        }
        //public abstract void MapRecordFields<T>();
        //public abstract void MapRecordFields(params Type[] recordTypes);
        protected virtual void Validate(object state)
        {
            if (!IsDynamicObject)
            {
                //PIDict = ChoType.GetProperties(RecordType).ToDictionary(p => p.Name);
                PDDictInternal = new Dictionary<string, PropertyDescriptor>(StringComparer.InvariantCultureIgnoreCase);
                if (PIDictInternal != null)
                {
                    foreach (var fn in PIDictInternal.Keys)
                        PDDictInternal.Add(fn, ChoTypeDescriptor.GetProperty(RecordType, fn));
                }
            }
        }

        internal void LoadFieldConfigurationAttributesInternal(ChoRecordFieldConfiguration fc, Type reflectedType)
        {
            LoadFieldConfigurationAttributes(fc, reflectedType);
        }

        protected void LoadFieldConfigurationAttributes(ChoRecordFieldConfiguration fc, Type reflectedType)
        {
            if (!IsDynamicObject)
            {
                if (fc.PDInternal != null && fc.PIInternal != null)
                    return;
                    
                var recordType = reflectedType; // fc.ReflectedType == null ? RecordType : fc.ReflectedType;

                string name = null;
                object defaultValue = null;
                object fallbackValue = null;
                name = fc.Name;

                fc.ReflectedType = reflectedType;
                fc.PDInternal = ChoTypeDescriptor.GetProperty(recordType, name);
                fc.PIInternal = ChoType.GetProperty(recordType, name);

                if (fc.PDInternal == null || fc.PIInternal == null)
                    return;

                //Load default value
                defaultValue = ChoType.GetRawDefaultValue(fc.PDInternal);
                if (defaultValue != null)
                {
                    fc.DefaultValue = defaultValue;
                    fc.IsDefaultValueSpecifiedInternal = true;
                }
                //Load fallback value
                fallbackValue = ChoType.GetRawFallbackValue(fc.PDInternal);
                if (fallbackValue != null)
                {
                    fc.FallbackValue = fallbackValue;
                    fc.IsFallbackValueSpecifiedInternal = true;
                }

                //Load Converters
                fc.PropConvertersInternal = ChoTypeDescriptor.GetTypeConverters(fc.PIInternal);
                fc.PropConverterParamsInternal = ChoTypeDescriptor.GetTypeConverterParams(fc.PIInternal);

                //Load Custom Serializer
                fc.PropCustomSerializer = ChoTypeDescriptor.GetCustomSerializer(fc.PIInternal);
                fc.PropCustomSerializerParams = ChoTypeDescriptor.GetCustomSerializerParams(fc.PIInternal);

                if (fc.SourceType == null)
                    fc.SourceType = fc.GetSourceTypeFromConvertersIfAny();
            }
        }
        protected virtual void LoadNCacheMembers(IEnumerable<ChoRecordFieldConfiguration> fcs)
        {
            if (!IsDynamicObject)
            {
                string name = null;
                object defaultValue = null;
                object fallbackValue = null;
                foreach (var fc in fcs)
                {
                    //if (fc is ChoFileRecordFieldConfiguration)
                    //    name = ((ChoFileRecordFieldConfiguration)fc).FieldName;
                    //else
                    name = fc.Name;

                    fc.PDInternal = PDDictInternal.ContainsKey(name) ? PDDictInternal[name] :
                        (PDDictInternal.Any(p => p.Value.Name == name) ? PDDictInternal.Where(p => p.Value.Name == name).Select(p => p.Value).FirstOrDefault() : null);
                    fc.PIInternal = PIDictInternal.ContainsKey(name) ? PIDictInternal[name] :
           (PIDictInternal.Any(p => p.Value.Name == name) ? PIDictInternal.Where(p => p.Value.Name == name).Select(p => p.Value).FirstOrDefault() : null);

                    if (fc.PDInternal == null || fc.PIInternal == null)
                        continue;

                    //Load default value
                    defaultValue = ChoType.GetRawDefaultValue(fc.PDInternal);
                    if (defaultValue != null)
                    {
                        fc.DefaultValue = defaultValue;
                        fc.IsDefaultValueSpecifiedInternal = true;
                    }
                    //Load fallback value
                    fallbackValue = ChoType.GetRawFallbackValue(fc.PDInternal);
                    if (fallbackValue != null)
                    {
                        fc.FallbackValue = fallbackValue;
                        fc.IsFallbackValueSpecifiedInternal = true;
                    }

                    //Load Converters
                    fc.PropConvertersInternal = ChoTypeDescriptor.GetTypeConverters(fc.PIInternal);
                    fc.PropConverterParamsInternal = ChoTypeDescriptor.GetTypeConverterParams(fc.PIInternal);

                    //Load Custom Serializer
                    fc.PropCustomSerializer = ChoTypeDescriptor.GetCustomSerializer(fc.PIInternal);
                    fc.PropCustomSerializerParams = ChoTypeDescriptor.GetCustomSerializerParams(fc.PIInternal);

                    if (fc.SourceType == null)
                        fc.SourceType = fc.GetSourceTypeFromConvertersIfAny();
                }

                PropertyNames = PDDictInternal.Keys.ToArray();
            }

            //Validators
            HasConfigValidators = (from fc in fcs
                                   where fc.Validators != null
                                   select fc).FirstOrDefault() != null;

            if (!HasConfigValidators)
            {
                if (!IsDynamicObject)
                {
                    string name = null;
                    foreach (var fc in fcs)
                    {
                        if (fc is ChoFileRecordFieldConfiguration)
                            name = ((ChoFileRecordFieldConfiguration)fc).FieldName;
                        else
                            name = fc.Name;

                        if (!PDDictInternal.ContainsKey(name))
                            continue;
                        fc.Validators = ChoTypeDescriptor.GetPropetyAttributes<ValidationAttribute>(fc.PDInternal).ToArray();
                    }
                }
            }

            ValDict = (from fc in fcs
                       select new KeyValuePair<string, ValidationAttribute[]>(fc is ChoFileRecordFieldConfiguration ? ((ChoFileRecordFieldConfiguration)fc).FieldName : fc.Name, fc.Validators))
                       .GroupBy(i => i.Key).Select(g => g.First()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        protected virtual void Clone(ChoRecordConfiguration config)
        {
            if (config == null)
                return;

            config.ErrorMode = ErrorMode;
            config.IgnoreFieldValueMode = IgnoreFieldValueMode;
            config.AutoDiscoverColumns = AutoDiscoverColumns;
            config.ThrowAndStopOnMissingField = ThrowAndStopOnMissingField;
            config.ObjectValidationMode = ObjectValidationMode;
            config.NotifyAfter = NotifyAfter;
        }

        private readonly object _typeTypeConverterCacheLock = new object();
        private readonly Dictionary<Type, object[]> _typeTypeConverterCache = new Dictionary<Type, object[]>();
        private readonly Dictionary<Type, object[]> _typeTypeConverterParamsCache = new Dictionary<Type, object[]>();

        public Func<Type, object, object> ConverterForType { get; set; }
        public Func<Type, object, object> ConverterParamsForType { get; set; }
        protected object[] GetConvertersForTypePrivate(Type fieldType, object value = null)
        {
            if (fieldType == null) return null;

            if (ConverterForType != null)
            {
                var conv = ConverterForType(fieldType, value);
                if (conv != null)
                    return new object[] { conv };
            }

            if (_typeTypeConverterCache.ContainsKey(fieldType))
                return _typeTypeConverterCache[fieldType];

            lock (_typeTypeConverterCacheLock)
            {
                if (_typeTypeConverterCache.ContainsKey(fieldType))
                    return _typeTypeConverterCache[fieldType];
                else
                {
                    return ChoTypeDescriptor.GetTypeConvertersForType(fieldType);
                }
            }
        }
        internal object[] GetConvertersForType(Type fieldType, object value = null)
        {
            return GetConvertersForTypePrivate(fieldType, value);
        }

        protected object[] GetConverterParamsForTypePrivate(Type fieldType, object value = null)
        {
            if (fieldType == null) return null;

            if (ConverterParamsForType != null)
            {
                var conv = ConverterParamsForType(fieldType, value);
                if (conv != null)
                    return new object[] { conv };
            }

            if (_typeTypeConverterParamsCache.ContainsKey(fieldType))
                return _typeTypeConverterParamsCache[fieldType];

            lock (_typeTypeConverterCacheLock)
            {
                if (_typeTypeConverterParamsCache.ContainsKey(fieldType))
                    return _typeTypeConverterParamsCache[fieldType];
                else
                {
                    return ChoTypeDescriptor.GetTypeConverterParamsForType(fieldType);
                }
            }
        }
        internal object[] GetConverterParamsForType(Type fieldType, object value = null)
        {
            return GetConverterParamsForTypePrivate(fieldType, value);
        }

        public void ClearTypeConvertersForType<T>()
        {
            ClearTypeConvertersForType(typeof(T));
        }
        public void ClearTypeConvertersForType(Type objType)
        {
            if (objType == null)
                return;
            if (!_typeTypeConverterCache.ContainsKey(objType))
                return;

            lock (_typeTypeConverterCacheLock)
            {
                if (!_typeTypeConverterCache.ContainsKey(objType))
                    return;

                _typeTypeConverterCache.Remove(objType);
            }
        }
        public void RegisterTypeConvertersForType(Type objType, object[] converters)
        {
            if (objType == null)
                return;
            if (converters == null)
                converters = new object[] { };

            lock (_typeTypeConverterCacheLock)
            {
                if (!_typeTypeConverterCache.ContainsKey(objType))
                    _typeTypeConverterCache.Add(objType, converters);
                else
                    _typeTypeConverterCache[objType] = converters;
            }
        }

        public void RegisterTypeConverter(Type type)
        {
#if !NETSTANDARD2_0
            if (typeof(IValueConverter).IsAssignableFrom(type))
            {
                var attr = ChoType.GetCustomAttribute<ChoSourceTypeAttribute>(type, true);
                if (attr == null || attr.Type == null) return;
                RegisterTypeConverterForTypeInternal(attr.Type, ChoActivator.CreateInstance(type));
                return;
            }
#endif
            if (typeof(IChoValueConverter).IsAssignableFrom(type))
            {
                var attr = ChoType.GetCustomAttribute<ChoSourceTypeAttribute>(type, true);
                if (attr == null || attr.Type == null) return;
                RegisterTypeConverterForTypeInternal(attr.Type, ChoActivator.CreateInstance(type));
            }
        }

        public void RegisterTypeConverter<T>()
        {
            RegisterTypeConverter(typeof(T));
        }

#if !NETSTANDARD2_0

        public void RegisterTypeConverterForType<T>(IValueConverter converter)
        {
            RegisterTypeConverterForTypeInternal(typeof(T), (object)converter);
        }
#endif
        public void RegisterTypeConverterForType<T>(IChoValueConverter converter)
        {
            RegisterTypeConverterForTypeInternal(typeof(T), (object)converter);
        }
#if !NETSTANDARD2_0

        public void RegisterTypeConverterForType(Type objType, IValueConverter converter)
        {
            RegisterTypeConverterForTypeInternal(objType, (object)converter);
        }
#endif
        public void RegisterTypeConverterForType(Type objType, IChoValueConverter converter)
        {
            RegisterTypeConverterForTypeInternal(objType, (object)converter);
        }

        private void RegisterTypeConverterForTypeInternal(Type objType, object converter)
        {
            if (objType == null)
                return;
            if (converter == null)
                return;

            lock (_typeTypeConverterCacheLock)
            {
                if (!_typeTypeConverterCache.ContainsKey(objType))
                    _typeTypeConverterCache.Add(objType, new object[] { converter });
                else
                    _typeTypeConverterCache[objType] = _typeTypeConverterCache[objType].Union(new object[] { converter }).ToArray();
            }
        }
    }
}
