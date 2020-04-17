using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoJSONRecordConfiguration : ChoFileRecordConfiguration
    {
        internal Dictionary<Type, Dictionary<string, ChoJSONRecordFieldConfiguration>> JSONRecordFieldConfigurationsForType = new Dictionary<Type, Dictionary<string, ChoJSONRecordFieldConfiguration>>();

        public bool AllowComplexJSONPath
        {
            get;
            set;
        }

        [DataMember]
        public List<ChoJSONRecordFieldConfiguration> JSONRecordFieldConfigurations
        {
            get;
            private set;
        }
        [DataMember]
        public string JSONPath
        {
            get;
            set;
        }
        [DataMember]
        public bool UseJSONSerialization
        {
            get;
            set;
        }
        public JsonSerializerSettings JsonSerializerSettings
        {
            get;
            set;
        }
        private Lazy<JsonSerializer> _JsonSerializer = null;
        public JsonSerializer JsonSerializer
        {
            get { return _JsonSerializer.Value; }
        }

        [DataMember]
        public bool? SupportMultipleContent
        {
            get;
            set;
        }
        [DataMember]
        public Newtonsoft.Json.Formatting Formatting
        {
            get;
            set;
        }
        [DataMember]
        public ChoNullValueHandling NullValueHandling
        {
            get;
            set;
        }
        internal bool FlatToNestedObjectSupport
        {
            get;
            set;
        }
        public bool IgnoreNodeName
        {
            get;
            set;
        }
        public string NodeName
        {
            get;
            set;
        }
        public bool IgnoreRootName
        {
            get;
            set;
        }
        public string RootName
        {
            get;
            set;
        }
        public bool? SingleElement
        {
            get;
            set;
        }
        public bool EnableXmlAttributePrefix { get; set; }

        private Func<JObject, JObject> _customNodeSelecter = null;
        public Func<JObject, JObject> CustomNodeSelecter
        {
            get { return _customNodeSelecter; }
            set { if (value == null) return; _customNodeSelecter = value; }
        }

        //private bool _isDynamicObject = true;
        //internal virtual bool IsDynamicObject
        //{
        //    get { return _isDynamicObject; }
        //    set { _isDynamicObject = value; }
        //}

        //public override bool IsDynamicObject
        //{
        //    get
        //    {
        //        return base.IsDynamicObject; // && !UseJSONSerialization;
        //    }

        //    set
        //    {
        //        base.IsDynamicObject = value;
        //    }
        //}
        internal Dictionary<string, ChoJSONRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }

        internal bool IsInitialized
        {
            get;
            set;
        }
        internal string DataSetName
        {
            get;
            set;
        }

        public override IEnumerable<ChoRecordFieldConfiguration> RecordFieldConfigurations
        {
            get
            {
                foreach (var fc in JSONRecordFieldConfigurations)
                    yield return fc;
            }
        }

        public ChoJSONRecordFieldConfiguration this[string name]
        {
            get
            {
                return JSONRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }
        public readonly dynamic Context = new ChoDynamicObject();

        public ChoJSONRecordConfiguration() : this(null)
        {

        }

        internal ChoJSONRecordConfiguration(Type recordType) : base(recordType)
        {
            _JsonSerializer = new Lazy<JsonSerializer>(() =>
            {
                return JsonSerializerSettings == null ? null : JsonSerializer.Create(JsonSerializerSettings);
            });

            JSONRecordFieldConfigurations = new List<ChoJSONRecordFieldConfiguration>();

            Formatting = Newtonsoft.Json.Formatting.Indented;
            if (recordType != null)
            {
                Init(recordType);
            }
        }

        internal void Add(Type rt, ChoJSONRecordFieldConfiguration rc)
        {
            if (rt == null || rc == null)
                return;

            if (!JSONRecordFieldConfigurationsForType.ContainsKey(rt))
                JSONRecordFieldConfigurationsForType.Add(rt, new Dictionary<string, ChoJSONRecordFieldConfiguration>(StringComparer.InvariantCultureIgnoreCase));

            if (JSONRecordFieldConfigurationsForType[rt].ContainsKey(rc.Name))
                JSONRecordFieldConfigurationsForType[rt][rc.Name] = rc;
            else
                JSONRecordFieldConfigurationsForType[rt].Add(rc.Name, rc);
        }

        internal bool ContainsRecordConfigForType(Type rt)
        {
            return JSONRecordFieldConfigurationsForType.ContainsKey(rt);
        }

        internal ChoJSONRecordFieldConfiguration[] GetRecordConfigForType(Type rt)
        {
            if (ContainsRecordConfigForType(rt))
                return JSONRecordFieldConfigurationsForType[rt].Values.ToArray();
            else
                return null;
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            if (recordType == null)
                return;

            ChoJSONRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoJSONRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
            }

            if (JSONRecordFieldConfigurations.Count == 0)
                DiscoverRecordFields(recordType, false);
        }

        internal void Reset()
        {
            IsInitialized = false;
            JSONRecordFieldConfigurations.Clear();
        }

        internal void UpdateFieldTypesIfAny(Dictionary<string, Type> dict)
        {
            if (dict == null || RecordFieldConfigurationsDict == null)
                return;

            foreach (var key in dict.Keys)
            {
                if (RecordFieldConfigurationsDict.ContainsKey(key) && dict[key] != null)
                    RecordFieldConfigurationsDict[key].FieldType = dict[key];
            }
        }

        public override void MapRecordFields<T>()
        {
            MapRecordFields(typeof(T));
        }

        public void MapRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForType(rt))
                return;

            List<ChoJSONRecordFieldConfiguration> recordFieldConfigurations = new List<ChoJSONRecordFieldConfiguration>();
            DiscoverRecordFields(rt, true, recordFieldConfigurations);

            JSONRecordFieldConfigurationsForType.Add(rt, recordFieldConfigurations.ToDictionary(item => item.Name, StringComparer.InvariantCultureIgnoreCase));
        }

        public override void MapRecordFields(params Type[] recordTypes)
        {
            if (recordTypes == null)
                return;

            DiscoverRecordFields(recordTypes.FirstOrDefault());
            foreach (var rt in recordTypes.Skip(1))
                DiscoverRecordFields(rt, false, JSONRecordFieldConfigurations);
        }

        private void DiscoverRecordFields(Type recordType, bool clear = true,
            List<ChoJSONRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordFieldConfigurations == null)
                recordFieldConfigurations = JSONRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                recordFieldConfigurations.Clear();
            DiscoverRecordFields(recordType, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()).Any(), recordFieldConfigurations);
        }

        private void DiscoverRecordFields(Type recordType, string declaringMember, bool optIn = false,
            List<ChoJSONRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (!recordType.IsDynamicType())
            {
                Type pt = null;
                if (ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        bool optIn1 = ChoTypeDescriptor.GetProperties(pt).Where(pd1 => pd1.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()).Any();
                        if (optIn1 && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt) && FlatToNestedObjectSupport)
                        {
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn1);
                        }
                        else if (pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any())
                        {
                            var obj = new ChoJSONRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().First(), pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptor = pd;
                            obj.DeclaringMember = declaringMember == null ? null : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            if (recordFieldConfigurations != null)
                            {
                                if (!recordFieldConfigurations.Any(c => c.Name == pd.Name))
                                    recordFieldConfigurations.Add(obj);
                            }
                        }
                    }
                }
                else
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        JsonIgnoreAttribute jiAttr = pd.Attributes.OfType<JsonIgnoreAttribute>().FirstOrDefault();
                        if (jiAttr != null)
                            continue;

                        pt = pd.PropertyType.GetUnderlyingType();
                        if (pt != typeof(object) && !pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt) && FlatToNestedObjectSupport)
                        {
                            DiscoverRecordFields(pt, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn);
                        }
                        else
                        {
                            var obj = new ChoJSONRecordFieldConfiguration(pd.Name, (string)null);
                            obj.FieldType = pt;
                            obj.PropertyDescriptor = pd;
                            obj.DeclaringMember = declaringMember == null ? null : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            StringLengthAttribute slAttr = pd.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
                            if (slAttr != null && slAttr.MaximumLength > 0)
                                obj.Size = slAttr.MaximumLength;
                            ChoUseJSONSerializationAttribute sAttr = pd.Attributes.OfType<ChoUseJSONSerializationAttribute>().FirstOrDefault();
                            if (sAttr != null)
                                obj.UseJSONSerialization = true;
                            ChoJSONPathAttribute jpAttr = pd.Attributes.OfType<ChoJSONPathAttribute>().FirstOrDefault();
                            if (jpAttr != null)
                                obj.JSONPath = jpAttr.JSONPath;

                            JsonPropertyAttribute jAttr = pd.Attributes.OfType<JsonPropertyAttribute>().FirstOrDefault();
                            if (jAttr != null && !jAttr.PropertyName.IsNullOrWhiteSpace())
                            {
                                obj.FieldName = jAttr.PropertyName;
                            }
                            else
                            {
                                DisplayNameAttribute dnAttr = pd.Attributes.OfType<DisplayNameAttribute>().FirstOrDefault();
                                if (dnAttr != null && !dnAttr.DisplayName.IsNullOrWhiteSpace())
                                {
                                    obj.FieldName = dnAttr.DisplayName.Trim();
                                }
                                else
                                {
                                    DisplayAttribute dpAttr = pd.Attributes.OfType<DisplayAttribute>().FirstOrDefault();
                                    if (dpAttr != null)
                                    {
                                        if (!dpAttr.ShortName.IsNullOrWhiteSpace())
                                            obj.FieldName = dpAttr.ShortName;
                                        else if (!dpAttr.Name.IsNullOrWhiteSpace())
                                            obj.FieldName = dpAttr.Name;
                                    }
                                }
                            }
                            DisplayFormatAttribute dfAttr = pd.Attributes.OfType<DisplayFormatAttribute>().FirstOrDefault();
                            if (dfAttr != null && !dfAttr.DataFormatString.IsNullOrWhiteSpace())
                            {
                                obj.FormatText = dfAttr.DataFormatString;
                            }
                            if (dfAttr != null && !dfAttr.NullDisplayText.IsNullOrWhiteSpace())
                            {
                                obj.NullValue = dfAttr.NullDisplayText;
                            }

                            if (recordFieldConfigurations != null)
                            {
                                if (!recordFieldConfigurations.Any(c => c.Name == pd.Name))
                                    recordFieldConfigurations.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        public override void Validate(object state)
        {
            if (RecordType != null)
            {
                Init(RecordType);
            }

            base.Validate(state);

            string[] fieldNames = null;
            JObject jObject = null;
            if (state is Tuple<long, JObject>)
                jObject = ((Tuple<long, JObject>)state).Item2;
            else
                fieldNames = state as string[];

            if (AutoDiscoverColumns
                && JSONRecordFieldConfigurations.Count == 0)
            {
                if (RecordType != null && !IsDynamicObject /*&& RecordType != typeof(ExpandoObject)*/
                    && ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()).Any())
                {
                    MapRecordFields(RecordType);
                }
                else if (jObject != null)
                {
                    Dictionary<string, ChoJSONRecordFieldConfiguration> dict = new Dictionary<string, ChoJSONRecordFieldConfiguration>(StringComparer.CurrentCultureIgnoreCase);
                    string name = null;
                    foreach (var attr in jObject.Properties())
                    {
                        name = attr.Name;
                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoJSONRecordFieldConfiguration(name, (string)null));
                        else
                        {
                            throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));
                        }
                    }

                    foreach (ChoJSONRecordFieldConfiguration obj in dict.Values)
                        JSONRecordFieldConfigurations.Add(obj);
                }
                else if (!fieldNames.IsNullOrEmpty())
                {
                    foreach (string fn in fieldNames)
                    {
                        if (IgnoredFields.Contains(fn))
                            continue;

                        var obj = new ChoJSONRecordFieldConfiguration(fn, (string)null);
                        JSONRecordFieldConfigurations.Add(obj);
                    }
                }
            }
            else
            {
                foreach (var fc in JSONRecordFieldConfigurations)
                {
                    fc.ComplexJPathUsed = !(fc.JSONPath.IsNullOrWhiteSpace() || String.Compare(fc.FieldName, fc.JSONPath, true) == 0);
                }
            }

            if (JSONRecordFieldConfigurations.Count <= 0)
                throw new ChoRecordConfigurationException("No record fields specified.");

            //Validate each record field
            foreach (var fieldConfig in JSONRecordFieldConfigurations)
                fieldConfig.Validate(this);

            //Check field position for duplicate
            string[] dupFields = JSONRecordFieldConfigurations.GroupBy(i => i.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToArray();

            if (dupFields.Length > 0)
                throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(String.Join(",", dupFields)));

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>();
            PDDict = new Dictionary<string, PropertyDescriptor>();
            foreach (var fc in JSONRecordFieldConfigurations)
            {
                if (fc.PropertyDescriptor == null)
                    fc.PropertyDescriptor = ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Name == fc.Name).FirstOrDefault();
                if (fc.PropertyDescriptor == null)
                    continue;

                PIDict.Add(fc.Name, fc.PropertyDescriptor.ComponentType.GetProperty(fc.PropertyDescriptor.Name));
                PDDict.Add(fc.Name, fc.PropertyDescriptor);
            }

            RecordFieldConfigurationsDict = JSONRecordFieldConfigurations.Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name);

            LoadNCacheMembers(JSONRecordFieldConfigurations);
        }

        public ChoJSONRecordConfiguration Configure(Action<ChoJSONRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public ChoJSONRecordConfiguration Map(string propertyName, Action<ChoJSONRecordFieldConfigurationMap> mapper)
        {
            var cf = GetFieldConfiguration(propertyName);
            mapper?.Invoke(new ChoJSONRecordFieldConfigurationMap(cf));
            return this;
        }
        
        public ChoJSONRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, string jsonPath)
        {
            Map(field, m => m.JSONPath(jsonPath));
            return this;
        }

        public ChoJSONRecordConfiguration Map<T, TField>(Expression<Func<T, TField>> field, Action<ChoJSONRecordFieldConfigurationMap> mapper)
        {
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray());
            mapper?.Invoke(new ChoJSONRecordFieldConfigurationMap(cf));
            return this;
        }

        internal ChoJSONRecordFieldConfiguration GetFieldConfiguration(string propertyName, ChoJSONRecordFieldAttribute attr = null, Attribute[] otherAttrs = null)
        {
            if (!JSONRecordFieldConfigurations.Any(fc => fc.Name == propertyName))
                JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration(propertyName, attr, otherAttrs));

            return JSONRecordFieldConfigurations.First(fc => fc.Name == propertyName);
        }

        internal ChoJSONRecordFieldConfiguration GetFieldConfiguration(string fn)
        {
            fn = fn.NTrim();
            if (!JSONRecordFieldConfigurations.Any(fc => fc.Name == fn))
                JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration(fn, (string)null));

            return JSONRecordFieldConfigurations.First(fc => fc.Name == fn);
        }

        //protected override void LoadNCacheMembers(IEnumerable<ChoRecordFieldConfiguration> fcs)
        //{
        //    base.LoadNCacheMembers(fcs);

        //    if (!IsDynamicObject)
        //    {
        //        foreach (var fc in fcs.OfType<ChoJSONRecordFieldConfiguration>())
        //        {
        //            if (!PDDict.ContainsKey(fc.Name))
        //                continue;

        //            var attr = ChoType.GetMemberAttribute<JsonConverterAttribute>(PIDict[fc.Name]);
        //            if (attr != null && attr.ConverterType != null)
        //            {
        //                fc.JSONPropConverters = new object[] { Activator.CreateInstance(attr.ConverterType) };
        //                fc.JSONPropConverterParams = new object[] { attr.ConverterParameters };
        //            }
        //        }
        //    }
        //}

    }

    public class ChoJSONRecordConfiguration<T> : ChoJSONRecordConfiguration
    {
        public ChoJSONRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, string jsonPath)
        {
            base.Map(field, jsonPath);
            return this;
        }

        public ChoJSONRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, Action<ChoJSONRecordFieldConfigurationMap> setup)
        {
            base.Map(field, setup);
            return this;
        }
    }
}
