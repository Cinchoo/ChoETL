using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    public class ChoBSONRecordConfiguration : ChoFileRecordConfiguration
    {
        private readonly Dictionary<string, dynamic> _indexMapDict = new Dictionary<string, dynamic>();
        internal readonly Dictionary<Type, Dictionary<string, ChoBSONRecordFieldConfiguration>> AvroRecordFieldConfigurationsForType = new Dictionary<Type, Dictionary<string, ChoBSONRecordFieldConfiguration>>();

        public JsonSerializerSettings JsonSerializerSettings
        {
            get;
        }

        [DataMember]
        public List<ChoBSONRecordFieldConfiguration> BSONRecordFieldConfigurations
        {
            get;
            private set;
        }
        public readonly dynamic Context = new ChoDynamicObject();

        internal bool AreAllFieldTypesNull
        {
            get;
            set;
        }
        internal Dictionary<string, string> AlternativeKeys
        {
            get;
            set;
        }
        internal Dictionary<string, ChoBSONRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }
        internal Dictionary<string, ChoBSONRecordFieldConfiguration> RecordFieldConfigurationsDict2
        {
            get;
            private set;
        }
        internal int MaxFieldPosition
        {
            get;
            set;
        }

        public override IEnumerable<ChoRecordFieldConfiguration> RecordFieldConfigurations
        {
            get
            {
                foreach (var fc in BSONRecordFieldConfigurations)
                    yield return fc;
            }
        }

        public bool IgnoreHeader { get; internal set; }

        public ChoBSONRecordFieldConfiguration this[string name]
        {
            get
            {
                return BSONRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoBSONRecordConfiguration() : this(null)
        {
        }

        internal ChoBSONRecordConfiguration(Type recordType) : base(recordType)
        {
            JsonSerializerSettings = new JsonSerializerSettings();
            BSONRecordFieldConfigurations = new List<ChoBSONRecordFieldConfiguration>();

            if (recordType != null)
            {
                Init(recordType);
            }

            //RecordSelector = new Func<object, Type>((value) =>
            //{
            //    Tuple<long, string> kvp = value as Tuple<long, string>;
            //    string line = kvp.Item2;
            //    if (line.IsNullOrEmpty()) return RecordTypeConfiguration.DefaultRecordType;

            //    if (RecordTypeCodeExtractor != null)
            //    {
            //        string rt = RecordTypeCodeExtractor(line);
            //        return RecordTypeConfiguration[rt];
            //    }
            //    else
            //    {
            //        if (RecordTypeConfiguration.Position <= 0)
            //            return RecordTypeConfiguration.DefaultRecordType;

            //        string[] fieldValues = line.Split(Delimiter, StringSplitOptions, QuoteChar);
            //        if (fieldValues.Length > 0 && RecordTypeConfiguration.Position - 1 < fieldValues.Length)
            //        {
            //            if (RecordTypeConfiguration.Contains(fieldValues[RecordTypeConfiguration.Position - 1]))
            //                return RecordTypeConfiguration[fieldValues[RecordTypeConfiguration.Position - 1]];
            //        }

            //        return RecordTypeConfiguration.DefaultRecordType;
            //    }
            //});
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoBSONRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoBSONRecordObjectAttribute>(recordType);
            if (IgnoreFieldValueMode == null)
                IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Empty;

            if (BSONRecordFieldConfigurations.Count == 0)
                DiscoverRecordFields(recordType);
        }

        public ChoBSONRecordConfiguration MapRecordFields<T>()
        {
            DiscoverRecordFields(typeof(T));
            return this;
        }

        public ChoBSONRecordConfiguration MapRecordFields(params Type[] recordTypes)
        {
            if (recordTypes == null)
                return this;

            int pos = 0;
            DiscoverRecordFields(recordTypes.Where(rt => rt != null).FirstOrDefault(), ref pos, true);
            foreach (var rt in recordTypes.Skip(1).Where(rt => rt != null))
                DiscoverRecordFields(rt, ref pos, false);

            return this;
        }

        private void DiscoverRecordFields(Type recordType, bool clear = true,
            List<ChoBSONRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = BSONRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                recordFieldConfigurations.Clear();

            int position = 0;
            DiscoverRecordFields(recordType, ref position, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoBSONRecordFieldAttribute>().Any()).Any(), 
                null, recordFieldConfigurations);
        }

        internal void UpdateFieldTypesIfAny(IDictionary<string, Type> dict)
        {
            if (dict == null || RecordFieldConfigurationsDict == null)
                return;

            foreach (var key in dict.Keys)
            {
                if (RecordFieldConfigurationsDict.ContainsKey(key) && dict[key] != null)
                    RecordFieldConfigurationsDict[key].FieldType = dict[key];
            }

            AreAllFieldTypesNull = RecordFieldConfigurationsDict.All(kvp => kvp.Value.FieldType == null);
        }

        private void DiscoverRecordFields(Type recordType)
        {
            int pos = 0;
            DiscoverRecordFields(recordType, ref pos, false);
        }

        private void DiscoverRecordFields(Type recordType, ref int pos, bool clear,
            List<ChoBSONRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;

            if (RecordMapType == null)
                RecordMapType = recordType;

            if (recordFieldConfigurations == null)
                recordFieldConfigurations = BSONRecordFieldConfigurations;

            if (clear && recordFieldConfigurations != null)
                ClearFields();

            DiscoverRecordFields(recordType, ref pos, null,
                ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoBSONRecordFieldAttribute>().Any()).Any(), 
                null, recordFieldConfigurations);
        }

        private void DiscoverRecordFields(Type recordType, ref int position, string declaringMember = null,
            bool optIn = false, PropertyDescriptor propDesc = null, List<ChoBSONRecordFieldConfiguration> recordFieldConfigurations = null)
        {
            if (recordType == null)
                return;
            if (!recordType.IsDynamicType())
            {
                Type pt = null;
                if (optIn) 
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        pt = pd.PropertyType.GetUnderlyingType();
                        if (!pt.IsSimple() && !typeof(IEnumerable).IsAssignableFrom(pt))
                            DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, null, recordFieldConfigurations);
                        else if (pd.Attributes.OfType<ChoBSONRecordFieldAttribute>().Any())
                        {
                            var obj = new ChoBSONRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoBSONRecordFieldAttribute>().First(), pd.Attributes.OfType<Attribute>().ToArray());
                            obj.FieldType = pt;
                            obj.PropertyDescriptor = pd;
                            obj.DeclaringMember = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
                            if (!recordFieldConfigurations.Any(c => c.Name == pd.Name))
                                recordFieldConfigurations.Add(obj);
                        }
                    }
                }
                else
                {
                    if ((recordType.IsGenericType && recordType.GetGenericTypeDefinition() == typeof(IList<>) || typeof(IList).IsAssignableFrom(recordType))
                        && !typeof(ArrayList).IsAssignableFrom(recordType)
                        /*&& !recordType.IsInterface*/)
                    {
                        if (propDesc != null)
                        {
                            RangeAttribute dnAttr = propDesc.Attributes.OfType<RangeAttribute>().FirstOrDefault();

                            if (dnAttr == null)
                            {
                                ChoBSONRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                recordFieldConfigurations.Add(obj);
                            }
                            else if (dnAttr != null && dnAttr.Minimum.CastTo<int>() >= 0 && dnAttr.Maximum.CastTo<int>() > 0
                                && dnAttr.Minimum.CastTo<int>() <= dnAttr.Maximum.CastTo<int>())
                            {
                                recordType = recordType.GetItemType().GetUnderlyingType();

                                if (recordType.IsSimple())
                                {
                                    for (int range = dnAttr.Minimum.CastTo<int>(); range <= dnAttr.Maximum.CastTo<int>(); range++)
                                    {
                                        ChoBSONRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, range);
                                        recordFieldConfigurations.Add(obj);
                                    }
                                }
                                else
                                {
                                    for (int range = dnAttr.Minimum.CastTo<int>(); range <= dnAttr.Maximum.CastTo<int>(); range++)
                                    {
                                        foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                                        {
                                            pt = pd.PropertyType.GetUnderlyingType();
                                            if (pt != typeof(object) && !pt.IsSimple() /*&& !typeof(IEnumerable).IsAssignableFrom(pt)*/)
                                            {
                                                //DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd);
                                            }
                                            else
                                            {
                                                ChoBSONRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, range, propDesc.GetDisplayName());

                                                recordFieldConfigurations.Add(obj);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (recordType.IsGenericType && (recordType.GetGenericTypeDefinition() == typeof(Dictionary<,>) || recordType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                        /*&& typeof(string) == recordType.GetGenericArguments()[0]*/)
                    {
                        if (propDesc != null)
                        {
                            ChoDictionaryKeyAttribute[] dnAttrs = propDesc.Attributes.OfType<ChoDictionaryKeyAttribute>().ToArray();
                            if (dnAttrs.IsNullOrEmpty())
                            {
                                ChoBSONRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                recordFieldConfigurations.Add(obj);
                            }
                            else
                            {
                                var keys = (from a in dnAttrs
                                            where a != null && !a.Keys.IsNullOrWhiteSpace()
                                            select a.Keys.SplitNTrim()).SelectMany(a => a).ToArray();

                                foreach (var key in keys)
                                {
                                    if (!key.IsNullOrWhiteSpace())
                                    {
                                        ChoBSONRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, dictKey: key);

                                        recordFieldConfigurations.Add(obj);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (recordType == typeof(object)
                            //|| typeof(IEnumerable).IsAssignableFrom(recordType)
                            //|| typeof(ICollection).IsAssignableFrom(recordType)
                            )
                        {

                        }
                        else if (recordType.IsSimple())
                        {
                            ChoBSONRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, propDesc);
                            if (!recordFieldConfigurations.Any(c => c.Name == propDesc.Name))
                                recordFieldConfigurations.Add(obj);
                        }
                        else
                        {
                            foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                            {
                                pt = pd.PropertyType.GetUnderlyingType();

                                if (pt == typeof(object)
                                    || typeof(ArrayList).IsAssignableFrom(pt)
                                    || typeof(Hashtable).IsAssignableFrom(pt)
                                    )
                                {
                                    continue;
                                }

                                if (pt != typeof(object) && !pt.IsSimple()  /*&& !typeof(IEnumerable).IsAssignableFrom(pt)*/)
                                {
                                    if (declaringMember == pd.Name)
                                    {

                                    }
                                    else
                                    {
                                        if (propDesc == null)
                                            DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd, recordFieldConfigurations);
                                        else
                                            DiscoverRecordFields(pt, ref position, declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name), optIn, pd, recordFieldConfigurations);
                                    }
                                }
                                else
                                {
                                    ChoBSONRecordFieldConfiguration obj = NewFieldConfiguration(ref position, declaringMember, pd, null, declaringMember == null ? propDesc.GetDisplayName() : propDesc.GetDisplayName(String.Empty));
                                    if (!recordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                        recordFieldConfigurations.Add(obj);
                                }
                            }
                        }
                    }
                }
            }
        }

        internal ChoBSONRecordFieldConfiguration NewFieldConfiguration(ref int position, string declaringMember, PropertyDescriptor pd,
            int? arrayIndex = null, string displayName = null, string dictKey = null, bool ignoreAttrs = false, Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            ChoBSONRecordFieldConfiguration obj = null;

            if (displayName.IsNullOrEmpty())
            {
                if (pd != null)
                    obj = new ChoBSONRecordFieldConfiguration(declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name));
                else
                    obj = new ChoBSONRecordFieldConfiguration("Value");
            }
            else if (pd != null)
            {
                if (displayName.IsNullOrWhiteSpace())
                    obj = new ChoBSONRecordFieldConfiguration("{0}".FormatString(pd.Name));
                else
                    obj = new ChoBSONRecordFieldConfiguration("{0}.{1}".FormatString(displayName, pd.Name));
            }
            else
                obj = new ChoBSONRecordFieldConfiguration(displayName);

            //obj.FieldName = pd != null ? pd.Name : displayName;

            mapper?.Invoke(new ChoBSONRecordFieldConfigurationMap(obj));

            obj.DictKey = dictKey;
            obj.ArrayIndex = arrayIndex;
            obj.FieldType = pd != null ? pd.PropertyType : null; // pt;
            obj.PropertyDescriptor = pd;
            if (pd != null)
                obj.DeclaringMember = declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name);
            else
                obj.DeclaringMember = displayName;

            if (arrayIndex == null && pd != null)
            {
                if (!ignoreAttrs)
                {
                    ChoFieldPositionAttribute fpAttr = pd.Attributes.OfType<ChoFieldPositionAttribute>().FirstOrDefault();
                }
            }
            else
            {

            }

            if (!ignoreAttrs && pd != null)
            {
                StringLengthAttribute slAttr = pd.Attributes.OfType<StringLengthAttribute>().FirstOrDefault();
                if (slAttr != null && slAttr.MaximumLength > 0)
                    obj.Size = slAttr.MaximumLength;
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
                            obj.FieldName = dpAttr.ShortName.Trim();
                        else if (!dpAttr.Name.IsNullOrWhiteSpace())
                            obj.FieldName = dpAttr.Name.Trim();

                        obj.Order = dpAttr.Order;
                    }
                    else
                    {
                        ColumnAttribute clAttr = pd.Attributes.OfType<ColumnAttribute>().FirstOrDefault();
                        if (clAttr != null)
                        {
                            obj.Order = clAttr.Order;
                            if (!clAttr.Name.IsNullOrWhiteSpace())
                                obj.FieldName = clAttr.Name;
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
            }

            if (pd != null && pd.ComponentType != null)
            {
                if (ContainsRecordConfigForType(pd.ComponentType))
                {
                    var st = GetRecordConfigForType(pd.ComponentType).OfType<ChoBSONRecordFieldConfiguration>();
                    if (st != null && st.Any(fc => fc.Name == pd.Name))
                    {
                        var f = st.FirstOrDefault(fc => fc.Name == pd.Name);
                        if (f != null)
                        {
                            obj.FieldName = f.FieldName;
                        }
                    }
                }
            }

            if (arrayIndex != null)
            {
                var arrayIndexSeparator = ArrayIndexSeparator == null ? '_' : ArrayIndexSeparator.Value;

                if (_recObject.Value is IChoArrayItemFieldNameOverrideable)
                {
                    obj.Name = obj.FieldName = ((IChoArrayItemFieldNameOverrideable)_recObject.Value).GetFieldName(displayName.IsNullOrWhiteSpace() ? declaringMember : displayName, obj.FieldName, arrayIndexSeparator, arrayIndex.Value);
                }
                else
                {
                    obj.Name = obj.FieldName = obj.FieldName + arrayIndexSeparator + arrayIndex.Value;
                }
            }
            else if (!dictKey.IsNullOrWhiteSpace())
            {
                obj.FieldName = dictKey;
            }

            return obj;
        }

        protected override void LoadNCacheMembers(IEnumerable<ChoRecordFieldConfiguration> fcs)
        {
            if (!IsDynamicObject)
            {
                string name = null;
                object defaultValue = null;
                object fallbackValue = null;
                foreach (var fc in fcs.OfType<ChoBSONRecordFieldConfiguration>())
                {
                    name = fc.Name;

                    if (!PDDict.ContainsKey(name))
                    {
                        if (!PDDict.ContainsKey(fc.FieldName))
                            continue;

                        name = fc.FieldName;
                    }

                    fc.PD = PDDict[name];
                    fc.PI = PIDict[name];

                    //Load default value
                    defaultValue = ChoType.GetRawDefaultValue(PDDict[name]);
                    if (defaultValue != null)
                    {
                        fc.DefaultValue = defaultValue;
                        fc.IsDefaultValueSpecified = true;
                    }
                    //Load fallback value
                    fallbackValue = ChoType.GetRawFallbackValue(PDDict[name]);
                    if (fallbackValue != null)
                    {
                        fc.FallbackValue = fallbackValue;
                        fc.IsFallbackValueSpecified = true;
                    }

                    //Load Converters
                    fc.PropConverters = ChoTypeDescriptor.GetTypeConverters(fc.PI);
                    fc.PropConverterParams = ChoTypeDescriptor.GetTypeConverterParams(fc.PI);

                }
            }
            base.LoadNCacheMembers(fcs);
        }

        public override void Validate(object state)
        {
            base.Validate(state);

            string[] fieldNames = null;
            IDictionary<string, object> jObject = null;
            if (state is Tuple<long, IDictionary<string, object>>)
                jObject = ((Tuple<long, IDictionary<string, object>>)state).Item2;
            else
                fieldNames = state as string[];

            if (AutoDiscoverColumns
                && BSONRecordFieldConfigurations.Count == 0)
            {
                if (RecordType != null && !IsDynamicObject /*&& RecordType != typeof(ExpandoObject)*/
                    && ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoBSONRecordFieldAttribute>().Any()).Any())
                {
                    MapRecordFields(RecordType);
                }
                else if (jObject != null)
                {
                    Dictionary<string, ChoBSONRecordFieldConfiguration> dict = new Dictionary<string, ChoBSONRecordFieldConfiguration>(StringComparer.CurrentCultureIgnoreCase);
                    string name = null;
                    int index = 0;
                    foreach (var kvp in jObject)
                    {
                        name = kvp.Key;
                        if (!dict.ContainsKey(name))
                            dict.Add(name, new ChoBSONRecordFieldConfiguration(name));
                        else
                        {
                            throw new ChoRecordConfigurationException("Duplicate field(s) [Name(s): {0}] found.".FormatString(name));
                        }
                    }

                    foreach (ChoBSONRecordFieldConfiguration obj in dict.Values)
                        BSONRecordFieldConfigurations.Add(obj);
                }
                else if (!fieldNames.IsNullOrEmpty())
                {
                    int index = 0;

                    foreach (string fn in fieldNames)
                    {
                        if (IgnoredFields.Contains(fn))
                            continue;

                        var obj = new ChoBSONRecordFieldConfiguration(fn);
                        BSONRecordFieldConfigurations.Add(obj);
                    }
                }

                //if (headers != null && IsDynamicObject)
                //{
                //    int index = 0;
                //    ParquetRecordFieldConfigurations = (from header in headers
                //                                    where !IgnoredFields.Contains(header)
                //                                    select new ChoParquetRecordFieldConfiguration(header, ++index)
                //                                    ).ToList();
                //}
                //else
                //{
                //    MapRecordFields(RecordType);
                //}
            }
            else
            {
            }

            //Index map initialization
            foreach (var value in _indexMapDict.Values)
            {
                BuildIndexMap(value.fieldName, value.fieldType, value.minumum, value.maximum,
                    value.fieldName, value.displayName,
                    value.mapper);
            }

            //Validate each record field
            foreach (var fieldConfig in BSONRecordFieldConfigurations)
                fieldConfig.Validate(this);

            if (false)
            {
            }
            else
            {
                //Check if any field has empty names 
                if (BSONRecordFieldConfigurations.Where(i => i.FieldName.IsNullOrWhiteSpace()).Count() > 0)
                    throw new ChoRecordConfigurationException("Some fields has empty field name specified.");

                //Check field names for duplicate
                string[] dupFields = BSONRecordFieldConfigurations.GroupBy(i => i.FieldName/*, FileHeaderConfiguration.StringComparer*/)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key).ToArray();

                if (dupFields.Length > 0)
                    throw new ChoRecordConfigurationException("Duplicate field name(s) [Name: {0}] found.".FormatString(String.Join(",", dupFields)));
            }

            PIDict = new Dictionary<string, System.Reflection.PropertyInfo>(StringComparer.InvariantCultureIgnoreCase);
            PDDict = new Dictionary<string, PropertyDescriptor>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var fc in BSONRecordFieldConfigurations)
            {
                if (fc.PropertyDescriptor == null && !IsDynamicObject)
                {
                    var pd = ChoTypeDescriptor.GetProperty(RecordType, fc.Name);
                    if (pd == null)
                        pd = ChoTypeDescriptor.GetProperty(RecordType, fc.DeclaringMember);

                    if (pd != null)
                    {
                        fc.PropertyDescriptor = pd;
                        if (fc.FieldType == null)
                            fc.FieldType = pd.PropertyType.GetUnderlyingType();
                    }
                }

                var pd1 = fc.DeclaringMember.IsNullOrWhiteSpace() ? ChoTypeDescriptor.GetProperty(RecordType, fc.Name)
                    : ChoTypeDescriptor.GetProperty(RecordType, fc.DeclaringMember);
                if (pd1 != null)
                    fc.PropertyDescriptor = pd1;

                if (fc.PropertyDescriptor == null)
                    fc.PropertyDescriptor = TypeDescriptor.GetProperties(RecordType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => pd.Name == fc.Name).FirstOrDefault();
                if (fc.PropertyDescriptor == null)
                    continue;

                PIDict.Add(fc.FieldName, fc.PropertyDescriptor.ComponentType.GetProperty(fc.PropertyDescriptor.Name));
                PDDict.Add(fc.FieldName, fc.PropertyDescriptor);
            }

            RecordFieldConfigurationsDict = BSONRecordFieldConfigurations.Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName/*, FileHeaderConfiguration.StringComparer*/);
            //RecordFieldConfigurationsDictGroup = RecordFieldConfigurationsDict.GroupBy(kvp => kvp.Key.Contains(".") ? kvp.Key.SplitNTrim(".").First() : kvp.Key).ToDictionary(i => i.Key, i => i.ToArray());
            RecordFieldConfigurationsDict2 = BSONRecordFieldConfigurations.Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName/*, FileHeaderConfiguration.StringComparer*/);

            try
            {
                if (IsDynamicObject)
                    AlternativeKeys = RecordFieldConfigurationsDict2.ToDictionary(kvp =>
                    {
                        if (kvp.Key == kvp.Value.Name)
                            return kvp.Value.Name.ToValidVariableName();
                        else
                            return kvp.Value.Name;
                    }, kvp => kvp.Key/*, FileHeaderConfiguration.StringComparer*/);
                else
                    AlternativeKeys = RecordFieldConfigurationsDict2.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Name/*, FileHeaderConfiguration.StringComparer*/);
            }
            catch { }

            //FCArray = RecordFieldConfigurationsDict.ToArray();

            LoadNCacheMembers(BSONRecordFieldConfigurations);
        }

        private void ValidateChar(char src, string name)
        {
            if (src == ChoCharEx.NUL)
                throw new ChoRecordConfigurationException("Invalid 'NUL' {0} specified.".FormatString(name));
            if (EOLDelimiter.Contains(src))
                throw new ChoRecordConfigurationException("{2} [{0}] can't be one of EOLDelimiter characters [{1}]".FormatString(src, EOLDelimiter, name));
            if ((from comm in Comments
                 where comm.Contains(src.ToString())
                 select comm).Any())
                throw new ChoRecordConfigurationException("One of the Comments contains {0}. Not allowed.".FormatString(name));
        }

        public ChoBSONRecordConfiguration ClearFields()
        {
            _indexMapDict.Clear();
            AvroRecordFieldConfigurationsForType.Clear();
            BSONRecordFieldConfigurations.Clear();
            return this;
        }

        public ChoBSONRecordConfiguration IgnoreField<T, TProperty>(Expression<Func<T, TProperty>> field)
        {
            if (BSONRecordFieldConfigurations.Count == 0)
                MapRecordFields<T>();

            var fc = BSONRecordFieldConfigurations.Where(f => f.DeclaringMember == field.GetFullyQualifiedMemberName()).FirstOrDefault();
            if (fc != null)
                BSONRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoBSONRecordConfiguration IgnoreField(string fieldName)
        {
            var fc = BSONRecordFieldConfigurations.Where(f => f.DeclaringMember == fieldName || f.FieldName == fieldName).FirstOrDefault();
            if (fc != null)
                BSONRecordFieldConfigurations.Remove(fc);

            return this;
        }

        public ChoBSONRecordConfiguration Map<T, TProperty>(Expression<Func<T, TProperty>> field, string fieldName = null)
        {
            Map(field, m => m.FieldName(fieldName));
            return this;
        }

        public ChoBSONRecordConfiguration Map(string propertyName, string fieldName)
        {
            Map(propertyName, m => m.FieldName(fieldName));
            return this;
        }

        public ChoBSONRecordConfiguration Map(string propertyName, Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            var cf = GetFieldConfiguration(propertyName);
            mapper?.Invoke(new ChoBSONRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoBSONRecordConfiguration Map<T, TField>(Expression<Func<T, TField>> field, Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoBSONRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(), 
                pd, fqm/*, subType == typeof(T) ? null : subType*/);
            mapper?.Invoke(new ChoBSONRecordFieldConfigurationMap(cf));
            return this;
        }

        public void ClearRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForType(rt))
                AvroRecordFieldConfigurationsForType.Remove(rt);
        }

        public void MapRecordFieldsForType(Type rt)
        {
            if (rt == null)
                return;

            if (ContainsRecordConfigForType(rt))
                return;

            List<ChoBSONRecordFieldConfiguration> recordFieldConfigurations = new List<ChoBSONRecordFieldConfiguration>();
            DiscoverRecordFields(rt, true, recordFieldConfigurations);

            AvroRecordFieldConfigurationsForType.Add(rt, recordFieldConfigurations.ToDictionary(item => item.Name, StringComparer.InvariantCultureIgnoreCase));
        }

        internal void AddFieldForType(Type rt, ChoBSONRecordFieldConfiguration rc)
        {
            if (rt == null || rc == null)
                return;

            if (!AvroRecordFieldConfigurationsForType.ContainsKey(rt))
                AvroRecordFieldConfigurationsForType.Add(rt, new Dictionary<string, ChoBSONRecordFieldConfiguration>(StringComparer.InvariantCultureIgnoreCase));

            if (AvroRecordFieldConfigurationsForType[rt].ContainsKey(rc.Name))
                AvroRecordFieldConfigurationsForType[rt][rc.Name] = rc;
            else
                AvroRecordFieldConfigurationsForType[rt].Add(rc.Name, rc);
        }

        public override bool ContainsRecordConfigForType(Type rt)
        {
            return AvroRecordFieldConfigurationsForType.ContainsKey(rt);
        }

        public override ChoRecordFieldConfiguration[] GetRecordConfigForType(Type rt)
        {
            if (ContainsRecordConfigForType(rt))
                return AvroRecordFieldConfigurationsForType[rt].Values.ToArray();
            else
                return null;
        }

        public override Dictionary<string, ChoRecordFieldConfiguration> GetRecordConfigDictionaryForType(Type rt)
        {
            if (ContainsRecordConfigForType(rt))
                return AvroRecordFieldConfigurationsForType[rt].ToDictionary(kvp => kvp.Key, kvp => (ChoRecordFieldConfiguration)kvp.Value);
            else
                return null;
        }

        public ChoBSONRecordConfiguration MapForType<T, TField>(Expression<Func<T, TField>> field,
            string fieldName = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoBSONRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoBSONRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            var cf1 = new ChoBSONRecordFieldConfigurationMap(cf).FieldName(fieldName);

            return this;
        }

        internal void WithField(string name, int? position, Type fieldType = null, bool? quoteField = null,
            ChoFieldValueTrimOption? fieldValueTrimOption = ChoFieldValueTrimOption.Trim, string fieldName = null,
            Func<object, object> valueConverter = null,
            Func<dynamic, object> valueSelector = null, Func<string> headerSelector = null,
            object defaultValue = null, object fallbackValue = null, string altFieldNames = null,
            string fullyQualifiedMemberName = null, string formatText = null,
            string nullValue = null, Type recordType = null, Type subRecordType = null,
            ChoFieldValueJustification? fieldValueJustification = null)
        {
            if (!name.IsNullOrEmpty())
            {
                if (subRecordType == recordType)
                    subRecordType = null;

                if (fieldName.IsNullOrWhiteSpace())
                    fieldName = name;
                if (subRecordType != null)
                    MapRecordFieldsForType(subRecordType);

                string fnTrim = name.NTrim();
                ChoBSONRecordFieldConfiguration fc = null;
                PropertyDescriptor pd = null;
                if (BSONRecordFieldConfigurations.Any(o => o.Name == fnTrim))
                {
                    fc = BSONRecordFieldConfigurations.Where(o => o.Name == fnTrim).First();
                    BSONRecordFieldConfigurations.Remove(fc);
                }
                else if (subRecordType != null)
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                }
                else
                {
                    pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName.IsNullOrWhiteSpace() ? name : fullyQualifiedMemberName);
                }

                var nfc = new ChoBSONRecordFieldConfiguration(fnTrim)
                {
                    FieldType = fieldType,
                    QuoteField = quoteField,
                    FieldValueTrimOption = fieldValueTrimOption,
                    FieldValueJustification = fieldValueJustification,
                    FieldName = fieldName,
                    ValueConverter = valueConverter,
                    ValueSelector = valueSelector,
                    HeaderSelector = headerSelector,
                    DefaultValue = defaultValue,
                    FallbackValue = fallbackValue,
                    FormatText = formatText,
                    NullValue = nullValue,
                };
                if (fullyQualifiedMemberName.IsNullOrWhiteSpace())
                {
                    nfc.PropertyDescriptor = fc != null ? fc.PropertyDescriptor : pd;
                    nfc.DeclaringMember = fc != null ? fc.DeclaringMember : fullyQualifiedMemberName;
                }
                else
                {
                    if (subRecordType == null)
                        pd = ChoTypeDescriptor.GetNestedProperty(recordType, fullyQualifiedMemberName);
                    else
                        pd = ChoTypeDescriptor.GetNestedProperty(subRecordType, fullyQualifiedMemberName);

                    nfc.PropertyDescriptor = pd;
                    nfc.DeclaringMember = fullyQualifiedMemberName;
                }
                if (pd != null)
                {
                    if (nfc.FieldType == null)
                        nfc.FieldType = pd.PropertyType;
                }

                if (subRecordType == null)
                    BSONRecordFieldConfigurations.Add(nfc);
                else
                    AddFieldForType(subRecordType, nfc);
            }
        }

        internal ChoBSONRecordFieldConfiguration GetFieldConfiguration(string propertyName, ChoBSONRecordFieldAttribute attr = null, Attribute[] otherAttrs = null,
            PropertyDescriptor pd = null, string fqm = null, Type subType = null)
        {
            if (subType != null)
            {
                MapRecordFieldsForType(subType);
                var fc = new ChoBSONRecordFieldConfiguration(propertyName, attr, otherAttrs);
                AddFieldForType(subType, fc);

                return fc;
            }
            else
            {
                //if (!ParquetRecordFieldConfigurations.Any(fc => fc.Name == propertyName))
                //    ParquetRecordFieldConfigurations.Add(new ChoParquetRecordFieldConfiguration(propertyName, attr, otherAttrs));

                //return ParquetRecordFieldConfigurations.First(fc => fc.Name == propertyName);
                if (fqm == null)
                    fqm = propertyName;

                propertyName = propertyName.SplitNTrim(".").LastOrDefault();
                if (!BSONRecordFieldConfigurations.Any(fc => fc.DeclaringMember == fqm && fc.ArrayIndex == null))
                {
                    int fieldPosition = 0;

                    var c = new ChoBSONRecordFieldConfiguration(propertyName, attr, otherAttrs);
                    if (pd != null)
                    {
                        c.PropertyDescriptor = pd;
                        c.FieldType = pd.PropertyType.GetUnderlyingType();
                    }

                    c.DeclaringMember = fqm;

                    BSONRecordFieldConfigurations.Add(c);
                }

                return BSONRecordFieldConfigurations.First(fc => fc.DeclaringMember == fqm && fc.ArrayIndex == null);
            }
        }

        public ChoBSONRecordConfiguration IndexMap(string fieldName, Type fieldType, int minumum, int maximum, Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            IndexMapInternal(fieldName, fieldType, minumum, maximum, fieldName, fieldName, mapper);
            return this;
        }

        public ChoBSONRecordConfiguration IndexMap<T, TField>(Expression<Func<T, TField>> field, int minumum,
            int maximum, Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            Type fieldType = field.GetPropertyType().GetUnderlyingType();
            var fqn = field.GetFullyQualifiedMemberName();
            var dn = field.GetPropertyDescriptor().GetDisplayName();

            if ((fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IList<>) || typeof(IList).IsAssignableFrom(fieldType))
                && !typeof(ArrayList).IsAssignableFrom(fieldType)
                && minumum >= 0 && maximum >= 0 && minumum <= maximum)
            {
                IndexMapInternal(fqn, fieldType, minumum, maximum,
                    field.GetFullyQualifiedMemberName(), field.GetPropertyDescriptor().GetDisplayName(), mapper);
            }
            return this;
        }

        internal void IndexMapInternal(string fieldName, Type fieldType, int minumum, int maximum,
            string fullyQualifiedMemberName = null, string displayName = null,
            Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            if (_indexMapDict.ContainsKey(fieldName))
                _indexMapDict.Remove(fieldName);
            _indexMapDict.AddOrUpdate(fieldName, new
            {
                fieldType,
                minumum,
                maximum,
                fieldName,
                displayName,
                mapper
            });
        }

        internal void BuildIndexMap(string fieldName, Type fieldType, int minumum, int maximum,
            string fullyQualifiedMemberName = null, string displayName = null,
            Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            if (fullyQualifiedMemberName == null)
                fullyQualifiedMemberName = fieldName;

            fieldName = fieldName.SplitNTrim(".").LastOrDefault();

            if (fieldType == null)
                return;

            if (fullyQualifiedMemberName.IsNullOrWhiteSpace())
                fullyQualifiedMemberName = fieldName;
            //if (displayName.IsNullOrWhiteSpace())
            //    displayName = fieldName;

            Type recordType = fieldType;
            var fqn = fieldName;

            if ((recordType.IsGenericType && recordType.GetGenericTypeDefinition() == typeof(IList<>) || typeof(IList).IsAssignableFrom(recordType)) 
                && !typeof(ArrayList).IsAssignableFrom(recordType)
                && minumum >= 0 && maximum >= 0 && minumum <= maximum
                /*&& !fieldType.IsInterface*/)
            {
                var itemType = recordType.GetItemType().GetUnderlyingType();
                if (itemType.IsSimple())
                {
                    var fcs1 = BSONRecordFieldConfigurations.Where(o => o.DeclaringMember == fullyQualifiedMemberName).ToArray();
                    foreach (var fc in fcs1)
                    {
                        displayName = fcs1.First().FieldName;
                        BSONRecordFieldConfigurations.Remove(fc);
                    }

                    for (int index = minumum; index <= maximum; index++)
                    {
                        var nfc = new ChoBSONRecordFieldConfiguration(fieldName) { ArrayIndex = index };
                        mapper?.Invoke(new ChoBSONRecordFieldConfigurationMap(nfc));

                        if (displayName != null)
                            nfc.FieldName = displayName;

                        string lFieldName = null;
                        if (ArrayIndexSeparator == null)
                            lFieldName = nfc.FieldName + "_" + index;
                        else
                            lFieldName = nfc.FieldName + ArrayIndexSeparator + index;

                        nfc.DeclaringMember = nfc.Name;
                        nfc.Name = lFieldName;
                        nfc.FieldName = lFieldName;
                        nfc.ArrayIndex = index;

                        nfc.FieldType = recordType;
                        BSONRecordFieldConfigurations.Add(nfc);
                    }
                }
                else
                {
                    int priority = 0;

                    //Remove collection config member
                    var fcs1 = BSONRecordFieldConfigurations.Where(o => o.FieldName == fqn).ToArray();
                    foreach (var fc in fcs1)
                    {
                        BSONRecordFieldConfigurations.Remove(fc);
                    }

                    //Remove any unused config
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(itemType))
                    {
                        var fcs = BSONRecordFieldConfigurations.Where(o => o.DeclaringMember == "{0}.{1}".FormatString(fullyQualifiedMemberName, pd.Name)
                        && o.ArrayIndex != null && (o.ArrayIndex < minumum || o.ArrayIndex > maximum)).ToArray();

                        foreach (var fc in fcs)
                            BSONRecordFieldConfigurations.Remove(fc);
                    }

                    for (int index = minumum; index <= maximum; index++)
                    {
                        foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(itemType))
                        {
                            var fc = BSONRecordFieldConfigurations.Where(o => o.DeclaringMember == "{0}.{1}".FormatString(fullyQualifiedMemberName, pd.Name)
                            && o.ArrayIndex != null && o.ArrayIndex == index).FirstOrDefault();

                            if (fc != null) continue;

                            Type pt = pd.PropertyType.GetUnderlyingType();
                            if (pt != typeof(object) && !pt.IsSimple())
                            {
                            }
                            else
                            {
                                int fieldPosition = 0;
                                ChoBSONRecordFieldConfiguration obj = NewFieldConfiguration(ref fieldPosition, fullyQualifiedMemberName, pd, index, displayName, ignoreAttrs: false,
                                    mapper: mapper);

                                //if (!ParquetRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                                BSONRecordFieldConfigurations.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        public ChoBSONRecordConfiguration DictionaryMap(string fieldName, Type fieldType,
            string[] keys, Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            DictionaryMapInternal(fieldName, fieldType, fieldName, keys, null, mapper);
            return this;
        }

        public ChoBSONRecordConfiguration DictionaryMap<T, TField>(Expression<Func<T, TField>> field,
            string[] keys, Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            Type fieldType = field.GetPropertyType().GetUnderlyingType();
            var fqn = field.GetFullyQualifiedMemberName();
            PropertyDescriptor pd = field.GetPropertyDescriptor();

            DictionaryMapInternal(pd.Name, fieldType, fqn, keys, pd, mapper);
            return this;
        }

        internal ChoBSONRecordConfiguration DictionaryMapInternal(string fieldName, Type fieldType, string fqn,
            string[] keys, PropertyDescriptor pd = null, Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            List<ChoBSONRecordFieldConfiguration> fcsList = new List<ChoBSONRecordFieldConfiguration>();
            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(IDictionary<,>)
                && typeof(string) == fieldType.GetGenericArguments()[0]
                && keys != null && keys.Length > 0)
            {
                //Remove collection config member
                var fcs1 = BSONRecordFieldConfigurations.Where(o => o.FieldName == fqn).ToArray();
                foreach (var fc in fcs1)
                    BSONRecordFieldConfigurations.Remove(fc);

                //Remove any unused config
                var fcs = BSONRecordFieldConfigurations.Where(o => o.DeclaringMember == fieldName
                && !o.DictKey.IsNullOrWhiteSpace() && !keys.Contains(o.DictKey)).ToArray();

                foreach (var fc in fcs)
                    BSONRecordFieldConfigurations.Remove(fc);

                foreach (var key in keys)
                {
                    if (!key.IsNullOrWhiteSpace())
                    {
                        var fc = BSONRecordFieldConfigurations.Where(o => o.DeclaringMember == fieldName
                            && !o.DictKey.IsNullOrWhiteSpace() && key == o.DictKey).FirstOrDefault();

                        if (fc != null) continue;

                        //ChoParquetRecordFieldConfiguration obj = NewFieldConfiguration(ref position, null, propDesc, dictKey: key);
                        int fieldPosition = 0;
                        ChoBSONRecordFieldConfiguration obj = NewFieldConfiguration(ref fieldPosition, null, pd, displayName: fieldName, dictKey: key, mapper: mapper);

                        //if (!ParquetRecordFieldConfigurations.Any(c => c.Name == (declaringMember == null ? pd.Name : "{0}.{1}".FormatString(declaringMember, pd.Name))))
                        //ParquetRecordFieldConfigurations.Add(obj);
                        BSONRecordFieldConfigurations.Add(obj);
                    }
                }
            }
            return this;
        }

        #region Fluent API

        public ChoBSONRecordConfiguration Configure(Action<ChoBSONRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        #endregion 
    }

    public class ChoBSONRecordConfiguration<T> : ChoBSONRecordConfiguration
    {
        public ChoBSONRecordConfiguration()
        {
            MapRecordFields<T>();
        }

        public new ChoBSONRecordConfiguration<T> ClearFields()
        {
            base.ClearFields();
            return this;
        }

        public ChoBSONRecordConfiguration<T> Ignore<TProperty>(Expression<Func<T, TProperty>> field)
        {
            base.IgnoreField(field);
            return this;
        }

        public ChoBSONRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field, string fieldName = null)
        {
            base.Map(field, fieldName);
            return this;
        }

        public ChoBSONRecordConfiguration<T> Map<TProperty>(Expression<Func<T, TProperty>> field,
            Action<ChoBSONRecordFieldConfigurationMap> setup)
        {
            base.Map(field, setup);
            return this;
        }

        public ChoBSONRecordConfiguration<T> MapForType<TClass>(Expression<Func<TClass, object>> field, string fieldName = null)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            ChoBSONRecordFieldConfiguration cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoBSONRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);

            new ChoBSONRecordFieldConfigurationMap(cf).FieldName(fieldName);
            return this;
        }

        public ChoBSONRecordConfiguration<T> MapForType<TClass, TField>(Expression<Func<TClass, TField>> field, Action<ChoBSONRecordFieldConfigurationMap> mapper)
        {
            var subType = field.GetReflectedType();
            var fn = field.GetMemberName();
            var pd = field.GetPropertyDescriptor();
            var fqm = field.GetFullyQualifiedMemberName();

            var cf = GetFieldConfiguration(fn, pd.Attributes.OfType<ChoBSONRecordFieldAttribute>().FirstOrDefault(), pd.Attributes.OfType<Attribute>().ToArray(),
                pd, fqm, subType);
            mapper?.Invoke(new ChoBSONRecordFieldConfigurationMap(cf));
            return this;
        }

        public ChoBSONRecordConfiguration<T> IndexMap<TField>(Expression<Func<T, TField>> field, int minumum,
            int maximum, Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            base.IndexMap(field, minumum, maximum, mapper);
            return this;
        }

        public ChoBSONRecordConfiguration<T> DictionaryMap<TField>(Expression<Func<T, TField>> field,
            string[] keys, Action<ChoBSONRecordFieldConfigurationMap> mapper = null)
        {
            base.DictionaryMap(field, keys, mapper);
            return this;
        }

        #region Fluent API

        public ChoBSONRecordConfiguration<T> Configure(Action<ChoBSONRecordConfiguration<T>> action)
        {
            if (action != null)
                action(this);

            return this;
        }

        public new ChoBSONRecordConfiguration<T> MapRecordFields<TClass>()
        {
            base.MapRecordFields(typeof(TClass));
            return this;
        }

        public new ChoBSONRecordConfiguration<T> MapRecordFields(params Type[] recordTypes)
        {
            base.MapRecordFields(recordTypes);
            return this;
        }

        #endregion
    }
}
