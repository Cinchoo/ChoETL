using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ChoETL
{
    public enum ChoArrayBracketNotation
    {
        None,
        Square,
        Parenthesis
    }

    public static class ChoETLSettings
    {
        internal const string NULL_VALUE = "{NULL_VALUE}";
        internal const string EMPTY_VALUE = "{EMPTY_VALUE}";

        public static ChoArrayBracketNotation ArrayBracketNotation
        {
            get; set;
        }
        private static char _keySeparator = '.';
        public static char KeySeparator
        {
            get { return _keySeparator; }
            set
            {
                if (value != ChoCharEx.NUL)
                    _keySeparator = value;
            }
        }
        private static char _nestedKeySeparator = '_';
        public static char NestedKeySeparator
        {
            get { return _nestedKeySeparator; }
            set
            {
                if (value != ChoCharEx.NUL)
                    _nestedKeySeparator = value;
            }
        }
        private static char _arrayIndexSeparator = '_';
        public static char ArrayIndexSeparator
        {
            get { return _arrayIndexSeparator; }
            set
            {
                if (value != ChoCharEx.NUL)
                    _arrayIndexSeparator = value;
            }
        }

        private static int _valueNameStartIndex = 0;
        public static int ValueNameStartIndex
        {
            get { return _valueNameStartIndex; }
            set { if (value > 0) _valueNameStartIndex = value; }
        }

        private static string _valueNamePrefix = "Value";
        public static string ValueNamePrefix
        {
            get { return _valueNamePrefix; }
            set { _valueNamePrefix = value; }
        }
        internal static string GetValueNamePrefixOrDefault(string prefix = null)
        {
            if (prefix == null)
                return ValueNamePrefix.IsNullOrWhiteSpace() ? "Value" : _valueNamePrefix;
            else
                return prefix;
        }

        public static Func<string, string> ToPlural { get; set; }
        public static Func<string, string> ToSingular { get; set; }
    }

    public enum DictionaryType
    {
        Regular,
        Sorted,
        Ordered,
        Concurrent
    }

    public static class ChoDynamicObjectSettings
    {
        private static string _xmlValueToken = "#text";
        public static string XmlValueToken
        {
            get { return _xmlValueToken; }
            set
            {
                if (value.IsNullOrWhiteSpace())
                    return;
                _xmlValueToken = value;
            }
        }
        public static bool ThrowExceptionIfPropNotExists = false;
        public static DictionaryType DictionaryType = DictionaryType.Ordered;
        public static bool UseAutoConverter = false;
        public static StringComparer DictionaryComparer = StringComparer.CurrentCultureIgnoreCase;
        internal static StringComparer DictionaryComparerInternal
        {
            get { return DictionaryComparer == null ? StringComparer.CurrentCultureIgnoreCase : DictionaryComparer; }
        }

        public static Func<string, object, bool?> XmlArrayQualifier;
        public static Func<string, object, bool?> JsonArrayQualifier;

        public static bool? IsXmlArray(string key, object value, Func<string, object, bool?> xmlArrayQualifierOverride = null)
        {
            if (xmlArrayQualifierOverride == null)
            {
                if (XmlArrayQualifier != null)
                    return XmlArrayQualifier(key, value);
                else
                    return null;
            }
            else
                return xmlArrayQualifierOverride(key, value);
        }

        public static bool? IsJsonArray(string key, object value, Func<string, object, bool?> jsonArrayQualifierOverride = null)
        {
            if (jsonArrayQualifierOverride == null)
            {
                if (JsonArrayQualifier != null)
                    return JsonArrayQualifier(key, value);
                else
                    return null;
            }
            else
                return jsonArrayQualifierOverride(key, value);
        }
    }

    [Serializable]
    public class ChoDynamicObject : DynamicObject, IDictionary<string, object>, ISerializable //, IList<object>, IList //, IXmlSerializable
    {
        internal string GetKeySeparator()
        {
            return _keySeparator;
        }
        public const string DefaultName = "dynamic";
        private string _keySeparator = "";
        private string _attributePrefix = "@";

        //internal static readonly string ValueToken = "#text";

        [IgnoreDataMember]
        private readonly static Dictionary<string, Type> _intrinsicTypes = new Dictionary<string, Type>();

        #region Instance Members

        internal bool IsHeaderOnlyObject = false;

        private readonly object _padLock = new object();
        [IgnoreDataMember]
        private IDictionary<string, object> _kvpDict = ChoDynamicObjectSettings.DictionaryType == DictionaryType.Concurrent ? new ConcurrentDictionary<string, object>(ChoDynamicObjectSettings.DictionaryComparerInternal)
            : ChoDynamicObjectSettings.DictionaryType == DictionaryType.Ordered ?
            new OrderedDictionary<string, object>(ChoDynamicObjectSettings.DictionaryComparerInternal) as IDictionary<string, object>
            : (ChoDynamicObjectSettings.DictionaryType == DictionaryType.Sorted ?
                new SortedDictionary<string, object>(ChoDynamicObjectSettings.DictionaryComparerInternal) as IDictionary<string, object>
                : new Dictionary<string, object>(ChoDynamicObjectSettings.DictionaryComparerInternal) as IDictionary<string, object>);
        [IgnoreDataMember]
        private Func<IDictionary<string, object>> _func = null;
        private bool _watchChange = false;
        [IgnoreDataMember]
        private Dictionary<string, Type> _memberTypes = new Dictionary<string, Type>(StringComparer.CurrentCultureIgnoreCase);

        private bool _isInitialized = false;
        private event EventHandler<EventArgs> _afterLoaded;
        public event EventHandler<EventArgs> AfterLoaded
        {
            add
            {
                _afterLoaded += value;
                if (_isInitialized)
                    value(this, null);
            }
            remove
            {
                _afterLoaded -= value;
            }
        }
        [ChoIgnoreMember]
        public bool ThrowExceptionIfPropNotExists
        {
            get;
            set;
        } = ChoDynamicObjectSettings.ThrowExceptionIfPropNotExists;

        public void SetMemberType(string fn, Type fieldType)
        {
            if (fn.IsNullOrWhiteSpace())
                return;

            if (_memberTypes.ContainsKey(fn))
                _memberTypes[fn] = fieldType;
            else
                _memberTypes.Add(fn, fieldType);
        }

        public Type GetMemberType(string fn)
        {
            if (fn.IsNullOrWhiteSpace())
                return null;

            return _memberTypes.ContainsKey(fn) ? _memberTypes[fn] : null;
        }

        [ChoIgnoreMember]
        public bool IsFixed
        {
            get;
            set;
        }
        [ChoIgnoreMember]
        public bool IsReadOnly
        {
            get;
            set;
        }

        private string _dynamicObjectName;
        [ChoIgnoreMember]
        public virtual string DynamicObjectName
        {
            get { return _dynamicObjectName; }
            set
            {
                if (!value.IsNullOrWhiteSpace() && value.IndexOf(":") > 0)
                {
                    if (value.Substring(0, value.IndexOf(":")) == ChoXmlNamespaceManager.DefaultNSToken)
                    {
                        _dynamicObjectName = value.Substring(value.IndexOf(":") + 1);
                        return;
                    }
                }
                _dynamicObjectName = value;
            }
        }
        [ChoIgnoreMember]
        public int PollIntervalInSec
        {
            get;
            set;
        }

        //[NonSerialized]
        //private Func<string, string> _KeyResolver = null;
        //public Func<string, string> KeyResolver
        //{
        //    get { return _KeyResolver; }
        //    set { _KeyResolver = value; }
        //}

        [IgnoreDataMember]
        public Dictionary<string, string> AlternativeKeys
        {
            get;
            set;
        }

        public bool? UseXmlArray
        {
            get;
            set;
        }

        public string ValueNamePrefix { get; private set; }
        public int ValueNameStartIndex { get; private set; }

        #endregion Instance Members

        #region Constructors

        static ChoDynamicObject()
        {
            _intrinsicTypes.Add("bool", typeof(System.Boolean));
            _intrinsicTypes.Add("byte", typeof(System.Byte));
            _intrinsicTypes.Add("sbyte", typeof(System.SByte));
            _intrinsicTypes.Add("char", typeof(System.Char));
            _intrinsicTypes.Add("decimal", typeof(System.Decimal));
            _intrinsicTypes.Add("double", typeof(System.Double));
            _intrinsicTypes.Add("float", typeof(System.Single));
            _intrinsicTypes.Add("int", typeof(System.Int32));
            _intrinsicTypes.Add("uint", typeof(System.UInt32));
            _intrinsicTypes.Add("long", typeof(System.Int64));
            _intrinsicTypes.Add("ulong", typeof(System.UInt64));
            _intrinsicTypes.Add("object", typeof(System.Object));
            _intrinsicTypes.Add("short", typeof(System.Int16));
            _intrinsicTypes.Add("ushort", typeof(System.UInt16));
            _intrinsicTypes.Add("string", typeof(System.String));
            _intrinsicTypes.Add("dynamic", typeof(ChoDynamicObject));
        }

        public ChoDynamicObject() : this(false, null)
        {
            DynamicObjectName = DefaultName;
        }

        public ChoDynamicObject(string name, char? keySeparator = null) : this(false, keySeparator)
        {
            DynamicObjectName = name.IsNullOrWhiteSpace() ? DefaultName : name.Trim();
        }

        public ChoDynamicObject(bool watchChange = false, char? keySeparator = null) : this(null, watchChange, keySeparator)
        {
            _watchChange = watchChange;
        }

        public ChoDynamicObject(IDictionary<string, object> kvpDict, char? keySeparator = null) : this(null, false, keySeparator)
        {
            _kvpDict = kvpDict;
            if (_kvpDict != null)
            {
                foreach (var kvp in _kvpDict.ToArray())
                {
                    if (!(kvp.Value is ChoDynamicObject) && kvp.Value is IDictionary<string, object> dobj)
                    {
                        _kvpDict[kvp.Key] = new ChoDynamicObject(dobj);
                    }
                }
            }
            if (kvpDict is ChoDynamicObject dobj1)
            {
                DynamicObjectName = dobj1.DynamicObjectName;
                SetNSPrefix(dobj1.GetNSPrefix());
            }
        }

        public ChoDynamicObject(IList<object> list, string valueNamePrefix = null, int? valueNameStartIndex = null, char? keySeparator = null) 
            : this(null, false, keySeparator)
        {
            if (valueNamePrefix.IsNullOrWhiteSpace())
                valueNamePrefix = ChoETLSettings.ValueNamePrefix;
            if (valueNamePrefix.IsNullOrWhiteSpace())
                valueNamePrefix = "Value";
            if (valueNameStartIndex == null)
                valueNameStartIndex = ChoETLSettings.ValueNameStartIndex;

            ValueNamePrefix = valueNamePrefix;
            ValueNameStartIndex = valueNameStartIndex.Value;

            _kvpDict = list.Select((item, index) => new KeyValuePair<string, object>($"{valueNamePrefix}{index + valueNameStartIndex}", item)).
                ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public ChoDynamicObject(ExpandoObject kvpDict, char? keySeparator = null) : this(null, false, keySeparator)
        {
            _kvpDict = (IDictionary<string, object>)kvpDict;
            if (_kvpDict != null)
            {
                foreach (var kvp in _kvpDict.ToArray())
                {
                    if (kvp.Value is ExpandoObject dobj)
                    {
                        _kvpDict[kvp.Key] = new ChoDynamicObject(dobj);
                    }
                }
            }
        }

        public ChoDynamicObject(dynamic kvpDict, char? keySeparator = null) : this(null, false, keySeparator)
        {
            _kvpDict = (IDictionary<string, object>)kvpDict;
        }

        public ChoDynamicObject(Func<IDictionary<string, object>> func, bool watchChange = false, char? keySeparator = null)
        {
            SetKeySeparator(keySeparator);

            if (DynamicObjectName.IsNullOrWhiteSpace())
                DynamicObjectName = DefaultName;
            //ThrowExceptionIfPropNotExists = false;
            IsFixed = false;
            IsReadOnly = false;

            _func = func;
            _watchChange = watchChange;

            Task.Run(() =>
            {
                Initialize();
                _afterLoaded.Raise(this, null);

                if (watchChange)
                {
                    int pollIntervalInSec = PollIntervalInSec;
                    if (pollIntervalInSec <= 0)
                        pollIntervalInSec = 5;

                    System.Threading.Timer timer = null;
                    timer = new System.Threading.Timer((e) =>
                    {
                        timer.Change(Timeout.Infinite, Timeout.Infinite);
                        try
                        {
                            Initialize();
                        }
                        catch { }
                        timer.Change((long)TimeSpan.FromSeconds(pollIntervalInSec).TotalMilliseconds, Timeout.Infinite);
                    }, null, (long)TimeSpan.FromSeconds(pollIntervalInSec).TotalMilliseconds, Timeout.Infinite);
                }
            });
        }

        private void SetKeySeparator(char? keySeparator)
        {
            if (keySeparator == null)
                keySeparator = ChoETLSettings.KeySeparator;

            if (keySeparator != ChoCharEx.NUL)
                _keySeparator = keySeparator.ToString();
        }

        protected ChoDynamicObject(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
            {
                _kvpDict.Add(entry.Name, entry.Value);
            }
        }

        #endregion Constructors

        public void Refresh()
        {
            Initialize();
        }

        public dynamic RenameKey(string oldKey, string newKey)
        {
            if (oldKey.IsNullOrWhiteSpace() || newKey.IsNullOrWhiteSpace())
                return this;

            if (!_kvpDict.ContainsKey(oldKey))
                return this;

            var value = _kvpDict[oldKey];
            _kvpDict.Remove(oldKey);
            if (value is ChoDynamicObject)
                ((ChoDynamicObject)value).DynamicObjectName = newKey;
            _kvpDict.Add(newKey, value);

            return this;
        }

        public dynamic RenameKeyAt(int index, string newKey)
        {
            if (index >= _kvpDict.Count)
                return this;

            var kvp = _kvpDict.ElementAt(index);
            var value = kvp.Value;
            var key = kvp.Key;
            _kvpDict.Remove(key);
            if (value is ChoDynamicObject)
                ((ChoDynamicObject)value).DynamicObjectName = newKey;
            _kvpDict.Add(newKey, value);

            return this;
        }

        public dynamic ExpandArrayToObjects(Func<int, string> keyGenerator = null)
        {
            var max = 0;
            if (_kvpDict.Values.OfType<IList>().Any())
                max = _kvpDict.Values.OfType<IList>().Max(v => v != null ? v.Count : 0);
            if (max == 0)
                return this;

            Dictionary<string, object> list = new Dictionary<string, object>();
            for (int index = 0; index < max; index++)
            {
                Dictionary<string, object> ele = new Dictionary<string, object>();
                foreach (var kvp in _kvpDict)
                {
                    var value = kvp.Value != null && kvp.Value is IList && index < ((IList)kvp.Value).Count ? ((IList)kvp.Value)[index] : null;
                    ele.Add(kvp.Key, value);
                }

                list.Add(keyGenerator == null ? index.ToString() : keyGenerator(index), new ChoDynamicObject(ele));
            }

            return new ChoDynamicObject(list);
        }

        public dynamic Transpose()
        {
            var ret = ChoUtility.Transpose((IDictionary<string, object>)this);
            return new ChoDynamicObject(ret.GroupBy(g => g.Key.ToNString(), StringComparer.OrdinalIgnoreCase).ToDictionary(kvp => kvp.Key.ToNString(), kvp => (object)kvp.Last(), StringComparer.OrdinalIgnoreCase));
        }

        public dynamic ConvertToNestedObject(char separator = '/', char? arrayIndexSeparator = null, char? arrayEndIndexSeparator = null, bool allowNestedArrayConversion = true,
            int? maxArraySize = null, string valueNamePrefix = null, int? valueNameStartIndex = null)
        {
            return ChoExpandoObjectEx.ConvertToNestedObject(this, separator, arrayIndexSeparator, arrayEndIndexSeparator,
                allowNestedArrayConversion, maxArraySize == null ? 100 : maxArraySize.Value, valueNamePrefix, valueNameStartIndex);
        }

        public dynamic ConvertMembersToArrayIfAny(char? arrayIndexSeparator = null, char? arrayEndIndexSeparator = null, bool allowNestedConversion = true)
        {
            return ChoExpandoObjectEx.ConvertMembersToArrayIfAny(this, arrayIndexSeparator, arrayEndIndexSeparator, allowNestedConversion);
        }

        public dynamic ConvertMembersToArrayIfAny(char? arrayIndexSeparator, bool allowNestedConversion = true)
        {
            return ChoExpandoObjectEx.ConvertMembersToArrayIfAny(this, arrayIndexSeparator, null, allowNestedConversion);
        }

        public dynamic ConvertToFlattenObject(bool ignoreDictionaryFieldPrefix)
        {
            return ChoExpandoObjectEx.ConvertToFlattenObject(this, null, null, null, ignoreDictionaryFieldPrefix);
        }

        public dynamic ConvertToFlattenObject(char? nestedKeySeparator = null, char? arrayIndexSeparator = null, char? arrayEndIndexSeparator = null,
            bool ignoreDictionaryFieldPrefix = false)
        {
            return ChoExpandoObjectEx.ConvertToFlattenObject(this, nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix);
        }

        public string GetDescription(string name)
        {
            var m1 = ChoType.GetMembers(GetType()).Where(m => !ChoType.IsReadOnlyMember(m)).FirstOrDefault();
            if (m1 == null)
                return null;

            if (ChoType.HasMemberAttribute<DescriptionAttribute>(m1))
                return m1.GetCustomAttribute<DescriptionAttribute>().Description;
            else
                return null;
        }

        public object NormalizeValue(string name, object value, Type targetType = null)
        {
            var m1 = ChoType.GetMembers(GetType()).Where(m => !ChoType.IsReadOnlyMember(m)
                && (m.GetCustomAttribute<ChoPropertyAttribute>() != null && m.GetCustomAttribute<ChoPropertyAttribute>().Name == name) || m.Name == name).FirstOrDefault();
            if (m1 == null)
                return value;

            value = ChoConvert.ConvertTo(value, m1, targetType == null ? typeof(string) : targetType);

            var attr = ChoType.GetMemberAttribute<ChoPropertyAttribute>(m1);
            if (value != null && value is string)
            {
                string mv = value as string;
                if (attr != null)
                {
                    switch (attr.TrimOption)
                    {
                        case ChoPropertyValueTrimOption.Trim:
                            mv = mv.Trim();
                            break;
                        case ChoPropertyValueTrimOption.TrimEnd:
                            mv = mv.TrimEnd();
                            break;
                        case ChoPropertyValueTrimOption.TrimStart:
                            mv = mv.TrimStart();
                            break;
                    }
                }
                return mv;
            }

            return value;
        }

        public object CleanValue(string name, object value)
        {
            var m1 = ChoType.GetMembers(GetType()).Where(m => !ChoType.IsReadOnlyMember(m)
                && (m.GetCustomAttribute<ChoPropertyAttribute>() != null && m.GetCustomAttribute<ChoPropertyAttribute>().Name == name) || m.Name == name).FirstOrDefault();
            if (m1 == null)
                return value;

            value = ChoConvert.ConvertFrom(value, m1);

            var attr = ChoType.GetMemberAttribute<ChoPropertyAttribute>(m1);
            if (value != null && value is string)
            {
                string mv = value as string;
                if (attr != null)
                {
                    switch (attr.TrimOption)
                    {
                        case ChoPropertyValueTrimOption.Trim:
                            mv = mv.Trim();
                            break;
                        case ChoPropertyValueTrimOption.TrimEnd:
                            mv = mv.TrimEnd();
                            break;
                        case ChoPropertyValueTrimOption.TrimStart:
                            mv = mv.TrimStart();
                            break;
                    }
                }
                return mv;
            }

            return value;
        }

        protected virtual IDictionary<string, object> Seed()
        {
            return null;
        }
        protected virtual bool HasTypedProperty(string propName)
        {
            return ChoType.HasGetProperty(GetType(), propName);
        }

        #region DynamicObject Overrides

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var ret = GetPropertyValue(binder.Name, out result);

            if (ChoDynamicObjectSettings.UseAutoConverter)
            {
                //if (!HasTypedProperty(binder.Name))
                result = new ChoAutoConverter(result == null ? GetDefaultValue(binder.Name) : result, this);
            }
            return ret;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            var key = binder.Name;
            return SetPropertyValue(key, value);
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;

            if ((indexes.Length == 1) && indexes[0] != null)
            {
                if (indexes[0] is string)
                {
                    var key = indexes[0] as string;
                    return GetPropertyValue(key, out result);
                }
                else if (indexes[0] is int)
                {
                    var index = (int)indexes[0];

                    IDictionary<string, object> kvpDict = _kvpDict;
                    if (kvpDict != null)
                        result = kvpDict.ElementAt(index).Value;
                    return true;
                }
            }

            return false;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if ((indexes.Length == 1) && indexes[0] != null)
            {
                if (indexes[0] is string)
                {
                    var key = indexes[0] as string;
                    return SetPropertyValue(key, value);
                }
                else if (indexes[0] is int)
                {
                    var index = (int)indexes[0];

                    IDictionary<string, object> kvpDict = _kvpDict;
                    if (kvpDict != null)
                        kvpDict[kvpDict.ElementAt(index).Key] = value;
                    return true;
                }
            }
            return true;
        }

        #endregion DynamicObject Overrides

        #region Instance Members (Protected/Public)

        public dynamic Clone()
        {
            return new ChoDynamicObject(CloneDictionary(_kvpDict));
        }

        private IDictionary<string, object> CloneDictionary(IDictionary<string, object> dict)
        {
            if (dict == null)
                return null;

            Dictionary<string, object> result = new Dictionary<string, object>();
            foreach (var kvp in dict)
            {
                result.Add(kvp.Key, CloneObject(kvp.Value));
            }

            return result;
        }

        private IDictionary CloneDictionary(IDictionary dict)
        {
            if (dict == null)
                return null;

            IDictionary result = new Hashtable();
            foreach (var key in dict.Keys)
            {
                result.Add(key, CloneObject(dict[key]));
            }

            return result;
        }

        private object CloneObject(object value)
        {
            if (value == null)
                return null;
            else if (value is IDictionary)
                return CloneDictionary(value as IDictionary);
            else if (value is IDictionary<string, object>)
                return CloneDictionary(value as IDictionary<string, object>);
            else if (value is IList)
            {
                IList<object> list = new List<object>();
                foreach (var item in value as IList)
                    list.Add(CloneObject(item));

                return list;
            }
            else if (value is ICloneable)
                return ((ICloneable)value).Clone();
            else
                return value;
        }

        public void Merge(IDictionary<string, object> obj, bool skipIfSrcExists = false)
        {
            if (obj == null)
                return;

            foreach (var kvp in obj)
            {
                if (ContainsKey(kvp.Key))
                {
                    if (skipIfSrcExists)
                        continue;
                }

                SetPropertyValue(kvp.Key, kvp.Value);
                //this[kvp.Key] = kvp.Value;
            }
        }

        public virtual bool ContainsProperty(string key)
        {
            if (key.IsNullOrEmpty())
                return false;

            IDictionary<string, object> kvpDict = _kvpDict;
            if (NameContains(key, _keySeparator))
            {
                if (ContainsNestedProperty(key))
                    return true;
            }
            return kvpDict != null && (kvpDict.ContainsKey(key) || kvpDict.ContainsKey("{0}{1}".FormatString(_attributePrefix, key)));
        }

        //private bool ContainsNestedProperty(string key, )
        //{

        //}

        private bool ContainsNestedProperty(string key)
        {
            var current = _kvpDict;
            var subKeys = key.SplitNTrim(_keySeparator);
            foreach (var subKey in subKeys.Take(subKeys.Length - 1))
            {
                if (subKey.IsNullOrWhiteSpace())
                    return false;

                if (current.ContainsKey(subKey))
                {
                    var obj = current[subKey];
                    if (obj is IDictionary<string, object>)
                        current = obj as IDictionary<string, object>;
                    else
                        return false;
                }
                else
                    return false;
            }

            return current == null || !current.ContainsKey(subKeys[subKeys.Length - 1]) ? false : true;
        }

        protected virtual bool GetPropertyValue(string name, out object result)
        {
            result = null;
            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                if (name.StartsWith("Contains"))
                {
                    name = name.Substring(8);
                    result = kvpDict.ContainsKey(name);
                    return true;
                }

                if (NameContains(name, _keySeparator) && ContainsNestedProperty(name))
                {
                    result = AfterKVPLoaded(name, GetNestedPropertyValue(name));
                    return true;
                }
                else if (kvpDict.ContainsKey(name))
                {
                    result = AfterKVPLoaded(name, kvpDict[name]);
                    return true;
                }
                else if (kvpDict.ContainsKey("{0}{1}".FormatString(_attributePrefix, name)))
                {
                    result = AfterKVPLoaded(name, kvpDict["{0}{1}".FormatString(_attributePrefix, name)]);
                    return true;
                }
                else
                {
                    if (name.StartsWith("_"))
                    {
                        string normalizedName = name.Substring(1);
                        if (kvpDict.ContainsKey(normalizedName))
                        {
                            result = AfterKVPLoaded(name, kvpDict[normalizedName]);
                            return true;
                        }
                        else if (ThrowExceptionIfPropNotExists)
                            return false;
                    }
                    else if (AlternativeKeys != null && AlternativeKeys.ContainsKey(name))
                    {
                        var newName = AlternativeKeys[name];
                        if (!newName.IsNullOrWhiteSpace())
                        {
                            if (kvpDict.ContainsKey(newName))
                            {
                                result = AfterKVPLoaded(newName, kvpDict[newName]);
                                return true;
                            }
                            else if (kvpDict.ContainsKey("{0}{1}".FormatString(_attributePrefix, newName)))
                            {
                                result = AfterKVPLoaded(name, kvpDict["{0}{1}".FormatString(_attributePrefix, newName)]);
                                return true;
                            }
                        }
                    }
                }
            }

            if (ThrowExceptionIfPropNotExists)
                return false;
            else
                return true;
        }
        private bool NameContains(string name, string separator)
        {
            if (separator.IsNullOrEmpty())
                return false;

            return name.Contains(separator);
        }

        private bool _SetPropertyValue(string name, object value)
        {
            if (IsReadOnly)
                return false;

            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                //if (AlternativeKeys != null && AlternativeKeys.ContainsKey(name))
                //{
                //    var newName = AlternativeKeys[name];
                //    if (!newName.IsNullOrWhiteSpace())
                //        name = newName;
                //}
                if (NameContains(name, _keySeparator))
                {
                    SetNestedPropertyValue(name, value);
                    return true;
                }

                if (!kvpDict.ContainsKey(name))
                {
                    //if (ThrowExceptionIfPropNotExists)
                    //    return false;
                    if (IsFixed)
                        return true;
                    else
                        kvpDict.Add(name, value);
                }
                else
                    kvpDict[name] = value;
            }

            return true;
        }

        protected virtual bool SetPropertyValue(string name, object value)
        {
            if (IsReadOnly)
                return false;

            if (name != ChoDynamicObjectSettings.XmlValueToken && name.IndexOf(":") < 0)
            {
                if (!_prefix.IsNullOrWhiteSpace() && !name.StartsWith("@xmlns:", StringComparison.InvariantCultureIgnoreCase))
                    name = "{0}:{1}".FormatString(_prefix, name);
            }

            return _SetPropertyValue(name, value);
        }
        protected virtual object GetDefaultValue(string name)
        {
            var mi = ChoType.GetMemberInfo(GetType(), name);

            return mi != null ? ChoType.GetRawDefaultValue(mi) : null;
        }

        private void Initialize()
        {
            try
            {
                if (!Monitor.TryEnter(_padLock, 1 * 1000))
                    return;

                IDictionary<string, object> kvpDict = null;

                if (_func != null)
                    kvpDict = _func();
                else
                    kvpDict = Seed();

                if (kvpDict == null)
                    return;

                IDictionary<string, object> mkvpDict = _kvpDict;
                bool hasDiff = mkvpDict == null || kvpDict.Except(mkvpDict).Concat(mkvpDict.Except(kvpDict)).Any();
                if (!hasDiff)
                    return;

                _kvpDict = kvpDict;

                ChoPropertyAttribute attr = null;
                object memberValue = null;
                string propName = null;
                //scan through members and load them
                foreach (var prop in ChoType.GetMembers(GetType()).Where(m => m.GetCustomAttribute<ChoIgnoreMemberAttribute>() != null && !ChoType.IsReadOnlyMember(m)))
                {
                    attr = ChoType.GetMemberAttribute<ChoPropertyAttribute>(prop);
                    try
                    {
                        SetDefaultValue(prop, true);

                        propName = attr != null && !attr.Name.IsNullOrWhiteSpace() ? attr.Name : prop.Name;

                        if (kvpDict.ContainsKey(propName))
                            memberValue = AfterKVPLoaded(prop.Name, kvpDict[propName]);
                        else
                            memberValue = AfterKVPLoaded(prop.Name, null);

                        if (memberValue != null && memberValue is string)
                        {
                            string mv = memberValue as string;
                            if (attr != null)
                            {
                                switch (attr.TrimOption)
                                {
                                    case ChoPropertyValueTrimOption.Trim:
                                        mv = mv.Trim();
                                        break;
                                    case ChoPropertyValueTrimOption.TrimEnd:
                                        mv = mv.TrimEnd();
                                        break;
                                    case ChoPropertyValueTrimOption.TrimStart:
                                        mv = mv.TrimStart();
                                        break;
                                }
                            }

                            memberValue = mv;
                        }

                        if (ChoType.GetMemberType(prop) == typeof(string))
                        {
                            if (!((string)memberValue).IsNullOrEmpty())
                                ChoType.ConvertNSetMemberValue(this, prop, memberValue);
                        }
                        else
                        {
                            if (memberValue != null)
                                ChoType.ConvertNSetMemberValue(this, prop, memberValue);
                        }
                        ChoValidator.ValidateFor(this, prop);
                    }
                    catch (Exception ex)
                    {
                        //ChoLog.Error("{0}: Error loading '{1}' property. {2}".FormatString(NName, prop.Name, ex.Message));
                        SetDefaultValue(prop, false);
                    }
                }
            }
            catch (Exception outerEx)
            {
                //ChoLog.Error("{0}: Error loading options. {1}".FormatString(NName, outerEx.Message));
            }
            finally
            {
                Monitor.Exit(_padLock);
            }
        }

        [ChoIgnoreMember]
        private string NName
        {
            get { return DynamicObjectName.IsNullOrWhiteSpace() ? GetType().Name : DynamicObjectName; }
            set { DynamicObjectName = value; }
        }

        public void ResetName()
        {
            DynamicObjectName = DefaultName;
        }

        private void SetDefaultValue(MemberInfo mi, bool saveDefaultValue = false)
        {
            object defaultValue = GetDefaultValue(mi.Name); // ChoType.GetRawDefaultValue(mi);
            try
            {
                ChoType.SetMemberValue(this, mi, defaultValue);
                //ChoLog.Error("{0}: Assigned default value '{1}' to '{2}' property.".FormatString(NName, defaultValue.ToNString(), mi.Name));
            }
            catch (Exception ex)
            {
                //ChoLog.Error("{0}: Error assigning default value '{1}' to '{2}' property. {3}".FormatString(NName, defaultValue.ToNString(), mi.Name, ex.Message));
            }
        }

        public static ChoDynamicObject New(IDictionary<object, object> dict)
        {
            Dictionary<string, object> dict1 = new Dictionary<string, object>();
            if (dict != null)
            {
                foreach (var kvp in dict)
                {
                    if (!dict1.ContainsKey(kvp.Key.ToNString()))
                        dict1.Add(kvp.Key.ToNString(), kvp.Value);
                }
            }
            return new ChoDynamicObject(dict1);
        }

        public static ChoDynamicObject New(Func<IEnumerable<KeyValuePair<string, object>>> func)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (func != null)
            {
                foreach (var kvp in func())
                {
                    if (!dict.ContainsKey(kvp.Key))
                        dict.Add(kvp.Key, kvp.Value);
                }
            }
            return new ChoDynamicObject(dict);
        }

        public static ChoDynamicObject New(string key, object value)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add(key, value);
            return new ChoDynamicObject(dict);

        }

        public static ChoDynamicObject New(string[] keys, params object[] values)
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            if (keys != null)
            {
                int counter = 0;
                foreach (var key in keys)
                {
                    if (counter < values.Length)
                        dict.Add(key, values[counter]);
                    else
                        dict.Add(key, null);
                    counter++;
                }
            }
            return new ChoDynamicObject(dict);
        }
        public bool RemoveNestedPropertyValue(string propName)
        {
            return ChoObjectEx.RemoveNestedPropertyValue(_kvpDict, propName);
        }
        public object GetNestedPropertyValue(string propName)
        {
            return ChoObjectEx.GetNestedPropertyValue(_kvpDict, propName);
        }

        public void SetNestedPropertyValue(string propName, object propValue)
        {
            ChoObjectEx.SetNestedPropertyValue(_kvpDict, propName, propValue, true, () => new ChoDynamicObject());
        }

        public IDictionary<string, object> GetDefaults()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();

            ChoPropertyAttribute attr = null;
            object memberValue = null;
            string propName = null;
            //scan through members and load them
            foreach (var prop in ChoType.GetMembers(GetType()).Where(m => m.GetCustomAttribute<ChoIgnoreMemberAttribute>() != null && !ChoType.IsReadOnlyMember(m)))
            {
                attr = ChoType.GetMemberAttribute<ChoPropertyAttribute>(prop);
                try
                {
                    propName = attr != null && !attr.Name.IsNullOrWhiteSpace() ? attr.Name : prop.Name;
                    memberValue = ChoType.GetDefaultValue(ChoType.GetMemberInfo(GetType(), prop.Name));

                    if (memberValue != null && memberValue is string)
                    {
                        string mv = memberValue as string;
                        if (attr != null)
                        {
                            switch (attr.TrimOption)
                            {
                                case ChoPropertyValueTrimOption.Trim:
                                    mv = mv.Trim();
                                    break;
                                case ChoPropertyValueTrimOption.TrimEnd:
                                    mv = mv.TrimEnd();
                                    break;
                                case ChoPropertyValueTrimOption.TrimStart:
                                    mv = mv.TrimStart();
                                    break;
                            }
                        }

                        memberValue = mv;
                    }

                    if (!dict.ContainsKey(propName))
                        dict.Add(propName, memberValue);
                }
                catch (Exception ex)
                {
                    //ChoLog.Error("{0}: Error getting default value for '{1}' property. {2}".FormatString(NName, prop.Name, ex.Message));
                    SetDefaultValue(prop, false);
                }
            }

            return dict;
        }

        #endregion

        protected virtual object AfterKVPLoaded(string key, object value)
        {
            return value;
        }

        public ICollection<string> Keys
        {
            get
            {
                IDictionary<string, object> kvpDict = _kvpDict;
                return kvpDict != null ? kvpDict.Keys : null;
            }
        }

        public ICollection<object> Values
        {
            get
            {
                IDictionary<string, object> kvpDict = _kvpDict;
                return kvpDict != null ? kvpDict.Values : null;
            }
        }

        public string[] KeysArray
        {
            get
            {
                IDictionary<string, object> kvpDict = _kvpDict;
                return kvpDict != null ? kvpDict.Keys.ToArray() : new string[] { };
            }
        }

        public object[] ValuesArray
        {
            get
            {
                IDictionary<string, object> kvpDict = _kvpDict;
                return kvpDict != null ? kvpDict.Values.ToArray() : new object[] { };
            }
        }

        public int Count
        {
            get
            {
                //if (_list.Count > 0)
                //	return _list.Count;

                IDictionary<string, object> kvpDict = _kvpDict;
                return kvpDict != null ? kvpDict.Count : 0;
            }
        }

        //public bool IsFixedSize
        //{
        //	get { return false; }
        //}

        //readonly object _syncRoot = new object();
        //public object SyncRoot
        //{
        //	get { return _syncRoot; }
        //}

        //public bool IsSynchronized
        //{
        //	get { return false; }
        //}

        //public object this[int index]
        //{
        //	get
        //	{
        //		return _list[index];
        //	}
        //	set
        //	{
        //		_list[index] = value;
        //	}
        //}

        //public KeyValuePair<string, object> this[int index]
        //{
        //    get
        //    {
        //        IDictionary<string, object> kvpDict = _kvpDict;
        //        if (kvpDict != null)
        //        {
        //            return kvpDict.ElementAtOrDefault(index);
        //        }
        //        return new KeyValuePair<string, object>();
        //    }
        //}

        public object this[string key]
        {
            get
            {
                object result = null;
                GetPropertyValue(key, out result);
                return result;
            }

            set
            {
                SetPropertyValue(key, value);
            }
        }

        public T GetValue<T>(string key)
        {
            return ChoUtility.CastTo<T>(this[key]);
        }


        public object GetValue(string key)
        {
            return this[key];
        }

        public void SetValue(string key, object value)
        {
            if (ContainsKey(key))
                this[key] = value;
        }

        public bool ContainsKey(string key)
        {
            return ContainsProperty(key);
        }

        public void Add(string key, object value)
        {
            SetPropertyValue(key, value);
        }

        public void AddToDictionary(string key, object value)
        {
            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
                kvpDict.Add(key, value);
        }

        public bool Remove(string key)
        {
            if (NameContains(key, _keySeparator) && ContainsNestedProperty(key))
            {
                return RemoveNestedPropertyValue(key);
            }

            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null && kvpDict.ContainsKey(key))
            {
                kvpDict.Remove(key);
            }
            return false;
        }

        public ChoDynamicObject SetDictionary(IDictionary<string, object> dict)
        {
            _kvpDict = dict;
            return this;
        }

        public bool TryGetValue(string key, out object value)
        {
            return GetPropertyValue(key, out value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            SetPropertyValue(item.Key, item.Value);
        }

        public void Clear()
        {
            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                kvpDict.Clear();
            }
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                return kvpDict.Contains(item);
            }
            return false;
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                foreach (var kvp in kvpDict)
                {
                    array[arrayIndex++] = kvp;
                }
            }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                return kvpDict.Contains(item);
            }
            return false;
        }

        public IEnumerator<KeyValuePair<string, object>> GetXmlEnumerator()
        {
            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                foreach (var kvp in kvpDict)
                {
                    if (!kvp.Key.StartsWith(_attributePrefix) && IsXmlAttribute(kvp.Key))
                        yield return new KeyValuePair<string, object>($"{_attributePrefix}{kvp.Key}", kvp.Value);
                    else
                        yield return kvp;
                }
            }
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                foreach (var kvp in kvpDict)
                {
                    yield return kvp;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return Keys;
        }

        public ChoDynamicObject Flatten(char? nestedKeySeparator = null, char? arrayIndexSeparator = null, char? arrayEndIndexSeparator = null, 
            bool ignoreDictionaryFieldPrefix = false, Func<string, string> columnMap = null, StringComparer cmp = null,
            string valueNamePrefix = null, bool ignoreRootDictionaryFieldPrefix = false)
        {
            _kvpDict = _kvpDict.Flatten(nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, ignoreDictionaryFieldPrefix,
                valueNamePrefix, ignoreRootDictionaryFieldPrefix).GroupBy(kvp => columnMap == null || columnMap(kvp.Key).IsNullOrWhiteSpace() ? kvp.Key : columnMap(kvp.Key)).ToDictionary(kvp => columnMap == null || columnMap(kvp.Key).IsNullOrWhiteSpace() ? kvp.Key : columnMap(kvp.Key), kvp => kvp.First().Value,
                cmp == null ? StringComparer.InvariantCultureIgnoreCase : cmp);
            return this;
        }
        public IDictionary<string, object> FlattenToDictionary(char? nestedKeySeparator = null, char? arrayIndexSeparator = null, char? arrayEndIndexSeparator = null,
            bool ignoreDictionaryFieldPrefix = false, string valueNamePrefix = null, bool ignoreRootDictionaryFieldPrefix = false)
        {
            return ChoDictionaryEx.FlattenToDictionary(_kvpDict, nestedKeySeparator, arrayIndexSeparator, arrayEndIndexSeparator, 
                ignoreDictionaryFieldPrefix, valueNamePrefix, ignoreRootDictionaryFieldPrefix);
        }
        public string Dump()
        {
            return ChoUtility.ToStringEx(this);
        }
        public string DumpAsJson()
        {
            return ChoUtility.DumpAsJson(this);
        }

        public void Print()
        {
            ChoUtility.Print(this);
        }

        public void Print(TextWriter writer)
        {
            ChoUtility.Print(this, writer);
        }

        public void PrintAsJson(TextWriter writer = null)
        {
            ChoUtility.PrintAsJson(this, writer);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        private Type GetType(string typeName)
        {
            if (_intrinsicTypes.ContainsKey(typeName))
                return _intrinsicTypes[typeName];
            else
                return Type.GetType(typeName);
        }

        public string GetXml(string tag = null, ChoNullValueHandling nullValueHandling = ChoNullValueHandling.Empty, string nsPrefix = null,
            bool emitDataType = false, string EOLDelimiter = null, bool? useXmlArray = null,
            bool useJsonNamespaceForObjectType = false, ChoXmlNamespaceManager nsMgr = null,
            ChoIgnoreFieldValueMode? ignoreFieldValueMode = null,
            bool? turnOffPluralization = null, Func<string, object, bool?> xmlArrayQualifierOverride = null,
            bool useOriginalNodeName = false
            )
        {
            if (EOLDelimiter == null)
                EOLDelimiter = Environment.NewLine;

            if (nsPrefix.IsNullOrWhiteSpace())
                nsPrefix = _prefix;

            if (tag.IsNullOrWhiteSpace())
            {
                tag = nsPrefix.IsNullOrWhiteSpace() ? NName : $"{nsPrefix}:{NName}";
            }
            else
            {
                if (tag.IndexOf(":") < 0)
                {
                    tag = nsPrefix.IsNullOrWhiteSpace() ? tag : $"{nsPrefix}:{tag}";
                }
                else
                    nsPrefix = tag.Substring(0, tag.IndexOf(":"));
            }

            if (!nsPrefix.IsNullOrWhiteSpace() && nsMgr?.GetNamespaceForPrefix(nsPrefix) == null)
                return null;

            var obj = AsShallowDictionary();
            if (ignoreFieldValueMode != null)
            {
                foreach (var key in obj.Keys.ToArray())
                {
                    if ((ignoreFieldValueMode | ChoIgnoreFieldValueMode.DBNull) == ChoIgnoreFieldValueMode.DBNull)
                    {
                        if (obj[key] == DBNull.Value)
                            obj.Remove(key);
                    }
                    else if ((ignoreFieldValueMode | ChoIgnoreFieldValueMode.Empty) == ChoIgnoreFieldValueMode.Empty)
                    {
                        if (obj[key] is string && obj[key].ToNString().IsEmpty())
                            obj.Remove(key);
                    }
                    else if ((ignoreFieldValueMode | ChoIgnoreFieldValueMode.Null) == ChoIgnoreFieldValueMode.Null)
                    {
                        if (obj[key] == null)
                            obj.Remove(key);
                    }
                    else if ((ignoreFieldValueMode | ChoIgnoreFieldValueMode.WhiteSpace) == ChoIgnoreFieldValueMode.WhiteSpace)
                    {
                        if (obj[key] is string && obj[key].ToNString().IsNullOrWhiteSpace())
                            obj.Remove(key);
                    }
                }
            }

            if (obj.Count == 0 && nullValueHandling == ChoNullValueHandling.Ignore)
                return String.Empty;

            bool hasAttrs = false;
            StringBuilder msg = new StringBuilder("<{0}".FormatString(tag));
            foreach (string key in obj.Keys.Where(k => IsAttribute(k) && k != ChoDynamicObjectSettings.XmlValueToken))
            {
                hasAttrs = true;

                if (key.Substring(1) == "type" && nsMgr != null)
                {
                    if (useJsonNamespaceForObjectType)
                    {
                        if (nsMgr.GetNamespaceForPrefix("json") != null)
                        {
                            msg.AppendFormat(@" {0}:{1}=""{2}""", "json", key.Substring(1), this[key]);
                        }
                        if (nsMgr.GetNamespaceForPrefix("xsi") != null)
                        {
                            msg.AppendFormat(@" {0}:{1}=""{2}""", "xsi", key.Substring(1), this[key]);
                        }
                    }
                    else if (nsMgr.GetNamespaceForPrefix("xsi") != null)
                    {
                        msg.AppendFormat(@" {0}:{1}=""{2}""", "xsi", key.Substring(1), this[key]);
                    }
                }
                else
                {
                    var key1 = key.StartsWith(_attributePrefix) ? key.Substring(1) : key;
                    if (key1.IndexOf(":") > 0)
                    {
                        var nsPrefix1 = key.Substring(0, key1.IndexOf(":"));
                        if (nsMgr.GetNamespaceForPrefix(nsPrefix1) == null)
                            continue;
                    }
                    msg.AppendFormat(@" {0}=""{1}""", key1, this[key1]);
                }
            }
            if (IsJsonArrayElement(tag))
            {
                if (nsMgr.GetNamespaceForPrefix("json") != null)
                {
                    msg.AppendFormat(@" {0}:Array=""{1}""", "json", GetJsonArrayElementFlag(tag).ToNString().ToLower());
                }
            }
            if (IsNillableElement(tag))
            {
                if (nsMgr.GetNamespaceForPrefix("xsi") != null)
                {
                    msg.AppendFormat(@" {0}:nil=""{1}""", "xsi", GetNillableElementFlag(tag).ToNString().ToLower());
                }
            }
            if (ContainsKey(ChoDynamicObjectSettings.XmlValueToken))
            {
                if (hasAttrs)
                {
                    msg.AppendFormat(">");
                    msg.AppendFormat("{0}{1}", EOLDelimiter, Indent(this[ChoDynamicObjectSettings.XmlValueToken].ToNString(), EOLDelimiter));
                    msg.AppendFormat("{0}</{1}>", EOLDelimiter, tag);
                }
                else
                {
                    object value = this[ChoDynamicObjectSettings.XmlValueToken];

                    if (emitDataType)
                    {

                    }

                    msg.AppendFormat(">");
                    msg.AppendFormat("{0}", value.ToNString());
                    msg.AppendFormat("</{0}>", tag);
                }
            }
            else if (obj.Keys.Any(k => !IsAttribute(k)))
            {
                object value = null;
                msg.AppendFormat(">");
                foreach (string key in obj.Keys.Where(k => !IsAttribute(k)))
                {
                    string nsPrefix1 = key.IndexOf(":") < 0 ? _prefix : key.Substring(0, key.IndexOf(":"));
                    if (!nsPrefix1.IsNullOrWhiteSpace() && nsMgr.GetNamespaceForPrefix(nsPrefix1) == null)
                        return null;

                    value = this[key];
                    var x = IsCDATA(key);

                    var useXmlArrayOverride = ChoDynamicObjectSettings.IsXmlArray(key, value, xmlArrayQualifierOverride);
                    if (useXmlArrayOverride != null)
                        useXmlArray = useXmlArrayOverride;

                    GetXml(msg, value, key, nullValueHandling, nsPrefix, IsCDATA(key), emitDataType, EOLDelimiter: EOLDelimiter, useXmlArray: useXmlArray,
                        useJsonNamespaceForObjectType, nsMgr,
                        ignoreFieldValueMode, turnOffPluralization == null ? false : turnOffPluralization.Value, xmlArrayQualifierOverride,
                        useOriginalNodeName
                        );
                }
                msg.AppendFormat("{0}</{1}>", EOLDelimiter, tag);
            }
            //else if (_list != null && _list.Count > 0)
            //{
            //             msg.AppendFormat(">");
            //	foreach (var obj in _list)
            //	{
            //		if (obj == null) continue;
            //		GetXml(msg, obj, tag.ToSingular());
            //	}
            //	msg.AppendFormat("{0}</{1}>", EOLDelimiter, tag);
            //}
            else
            {
                msg.AppendFormat(" />");
            }

            return msg.ToString();
        }

        private string Indent(string value, string delimiter, int indentValue = 1)
        {
            if (value == null)
                return value;

            if (delimiter.IsNullOrEmpty())
                return value;

            return value.Indent(indentValue, "  ");
        }

        private void GetXml(StringBuilder msg, object value, string key, ChoNullValueHandling nullValueHandling, string nsPrefix = null,
            bool isCDATA = false, bool emitDataType = false, string EOLDelimiter = null, bool? useXmlArray = null,
            bool useJsonNamespaceForObjectType = false, ChoXmlNamespaceManager nsMgr = null,
            ChoIgnoreFieldValueMode? ignoreFieldValueMode = null,
            bool turnOffPluralization = false, Func<string, object, bool?> xmlArrayQualifierOverride = null,
            bool useOriginalNodeName = false)
        {
            if (EOLDelimiter == null)
                EOLDelimiter = Environment.NewLine;

            if (value is ChoDynamicObject)
            {
                var dobj = value as ChoDynamicObject;
                if (dobj.NName == ChoDynamicObject.DefaultName && !key.IsNullOrWhiteSpace())
                    dobj.NName = key;

                msg.AppendFormat("{0}{1}", EOLDelimiter, Indent(((ChoDynamicObject)value).GetXml(dobj.NName, nullValueHandling, nsPrefix, emitDataType, EOLDelimiter: EOLDelimiter, useXmlArray: useXmlArray, useJsonNamespaceForObjectType, nsMgr,
                        ignoreFieldValueMode, turnOffPluralization, xmlArrayQualifierOverride, useOriginalNodeName), EOLDelimiter));
            }
            else
            {
                if (value != null)
                {
                    if (value.GetType().IsSimple())
                    {
                        if (isCDATA)
                        {
                            msg.AppendFormat("{0}{1}", EOLDelimiter, Indent("<{0}><![CDATA[{1}]]></{0}>".FormatString(key, value), EOLDelimiter));
                        }
                        else
                            msg.AppendFormat("{0}{1}", EOLDelimiter, Indent("<{0}>{1}</{0}>".FormatString(key, value), EOLDelimiter));
                    }
                    else
                    {
                        if (UseXmlArray != null)
                            useXmlArray = UseXmlArray.Value;
                        else if (value is IList)
                        {
                            var ret = ((IList)(value)).OfType<object>().Select(o => o.GetType()).Distinct().Count() > 1;
                            if (ret)
                                useXmlArray = true;
                        }

                        var useXmlArrayOverride = ChoDynamicObjectSettings.IsXmlArray(key, this, xmlArrayQualifierOverride); ;
                        if (useXmlArrayOverride != null)
                            useXmlArray = useXmlArrayOverride.Value;

                        if (IsJsonArrayElement(key))
                        {
                            useXmlArray = true;
                        }

                        int indent = 1;
                        if (useXmlArray != null && useXmlArray.Value)
                        {
                            var origKey = key;
                            if (!turnOffPluralization)
                                key = value is IList ? key.ToPlural() != key ? key.ToPlural() : key.Length > 1 && key.EndsWith("s", StringComparison.InvariantCultureIgnoreCase) ? key : "{0}s".FormatString(key) : key;

                            StringBuilder msg1 = new StringBuilder();
                            msg1.AppendFormat("<{0}".FormatString(key));
                          
                            if (IsJsonArrayElement(origKey))
                            {
                                if (nsMgr.GetNamespaceForPrefix("json") != null)
                                {
                                    msg1.AppendFormat(@" {0}:Array=""{1}""", "json", GetJsonArrayElementFlag(origKey).ToNString().ToLower());
                                }
                            }
                            if (IsNillableElement(origKey))
                            {
                                if (nsMgr.GetNamespaceForPrefix("xsi") != null)
                                {
                                    msg1.AppendFormat(@" {0}:nil=""{1}""", "xsi", GetNillableElementFlag(origKey).ToNString().ToLower());
                                }
                            }
                            msg1.Append(">");

                            msg.AppendFormat("{0}{1}", EOLDelimiter, 
                                Indent(msg1.ToString(), EOLDelimiter));

                            indent = 2;
                        }
                        if (value is IList)
                        {
                            foreach (var val in ((IList)value)) //.OfType<ChoDynamicObject>())
                            {
                                if (val is ChoDynamicObject collValue)
                                {
                                    if (collValue.NName == ChoDynamicObject.DefaultName && !key.IsNullOrWhiteSpace())
                                        collValue.NName = key.ToSingular();

                                    var useXmlArrayEx = useXmlArray != null && useXmlArray.Value;

                                    var name = collValue.NName == DefaultName ? (useXmlArrayEx ? key.ToSingular() : key) : collValue.NName;
                                    if (!useXmlArrayEx && useOriginalNodeName && !key.IsNullOrWhiteSpace())
                                        name = key;

                                    msg.AppendFormat("{0}{1}", EOLDelimiter, Indent(collValue.GetXml(name, nullValueHandling, nsPrefix, emitDataType, EOLDelimiter: EOLDelimiter,
                                        useXmlArray: useXmlArray, useJsonNamespaceForObjectType, nsMgr, ignoreFieldValueMode, turnOffPluralization, xmlArrayQualifierOverride, useOriginalNodeName), EOLDelimiter, indent));
                                }
                                else
                                    msg.AppendFormat("{0}{1}", EOLDelimiter, Indent(ChoUtility.XmlSerialize(val, null, EOLDelimiter, nullValueHandling, nsPrefix, emitDataType,
                                useXmlArray, useJsonNamespaceForObjectType, nsMgr, ignoreFieldValueMode, key: key == DefaultName ? null : key, 
                                turnOffPluralization, xmlArrayQualifierOverride, useOriginalNodeName), EOLDelimiter, indent));
                            }
                        }
                        else
                            msg.AppendFormat("{0}{1}", EOLDelimiter, Indent(ChoUtility.XmlSerialize(value, null, EOLDelimiter, nullValueHandling, nsPrefix, emitDataType,
                                useXmlArray, useJsonNamespaceForObjectType, nsMgr, ignoreFieldValueMode, key: key, turnOffPluralization, xmlArrayQualifierOverride, useOriginalNodeName), EOLDelimiter, indent));

                        if (useXmlArray != null && useXmlArray.Value)
                            msg.AppendFormat("{0}{1}", EOLDelimiter, Indent("</{0}>".FormatString(key), EOLDelimiter));
                    }
                }
                else
                {
                    switch (nullValueHandling)
                    {
                        case ChoNullValueHandling.Empty:
                            msg.AppendFormat("{0}{1}", EOLDelimiter, Indent(@"<{0}/>".FormatString(key), EOLDelimiter));
                            break;
                        case ChoNullValueHandling.Ignore:
                            break;
                        default:
                            msg.AppendFormat("{0}{1}", EOLDelimiter, Indent(@"<{0} xsi:nil=""true"" xmlns:xsi=""{1}""/>".FormatString(key, ChoXmlSettings.XmlSchemaInstanceNamespace), EOLDelimiter));
                            break;
                    }
                }
            }
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(DefaultName);

            object value = null;
            foreach (string key in this.Keys)
            {
                if (key == "Value")
                    value = this[key];
                else if (key == DefaultName)
                {
                    ((ChoDynamicObject)this[key]).WriteXml(writer);
                }
                else
                {
                    writer.WriteAttributeString(key, this[key].ToNString());
                }
            }
            if (value != null)
            {
                if (value is ChoDynamicObject)
                {
                    ((ChoDynamicObject)value).WriteXml(writer);
                }
                else
                {
                    if (value.GetType().IsSimple())
                        writer.WriteString(value.ToNString());
                    else
                    {
                        XmlSerializer valueSerializer = ChoNullNSXmlSerializerFactory.GetXmlSerializer(value.GetType());
                        valueSerializer.Serialize(writer, value);
                    }
                }
            }
            writer.WriteEndElement();
        }

        public bool HasText()
        {
            return ContainsKey(ChoDynamicObjectSettings.XmlValueToken);
        }
        public object GetText()
        {
            return ContainsKey(ChoDynamicObjectSettings.XmlValueToken) ? this[ChoDynamicObjectSettings.XmlValueToken] : null;
        }
        public void SetText(object value)
        {
            if (ContainsKey(ChoDynamicObjectSettings.XmlValueToken))
                this[ChoDynamicObjectSettings.XmlValueToken] = value;
            else
                Add(ChoDynamicObjectSettings.XmlValueToken, value);
        }

        private HashSet<string> _attributes = new HashSet<string>();
        public bool IsXmlAttribute(string attrName)
        {
            return _attributes.Contains(attrName);
        }
        public object GetAttribute(string attrName)
        {
            if (_attributes.Contains(attrName))
                return this[attrName];
            else
                return null;
        }
        public void SetAsAttribute(string key)
        {
            if (key.IsNullOrEmpty())
                return;
            if (key.StartsWith(_attributePrefix)) return;
            RenameKey(key, $"{_attributePrefix}{key}");
        }
        public void SetAttribute(string attrName, object value)
        {
            if (!_attributes.Contains(attrName))
                _attributes.Add(attrName);

            SetPropertyValue(attrName, value);
        }
        public bool IsAttribute(string attrName)
        {
            return _attributes.Contains(attrName) || attrName.StartsWith("@") || attrName.StartsWith("$");
        }
        private Dictionary<string, bool> _nillableElements = new Dictionary<string, bool>();
        public void SetAsNillableElement(string elementName, bool flag = false)
        {
            if (_nillableElements.ContainsKey(elementName))
                _nillableElements[elementName] = flag;
            else
                _nillableElements.Add(elementName, flag);
        }
        public bool IsNillableElement(string elementName)
        {
            return _nillableElements.ContainsKey(elementName);
        }
        public bool? GetNillableElementFlag(string elementName)
        {
            if (_nillableElements.ContainsKey(elementName))
                return _nillableElements[elementName];
            else
                return null;
        }

        private Dictionary<string, bool> _jsonArrayElements = new Dictionary<string, bool>();
        public void SetAsJsonArrayElement(string elementName, bool flag = false)
        {
            if (_jsonArrayElements.ContainsKey(elementName))
                _jsonArrayElements[elementName] = flag;
            else
                _jsonArrayElements.Add(elementName, flag);
        }
        public bool IsJsonArrayElement(string elementName)
        {
            return _jsonArrayElements.ContainsKey(elementName);
        }
        public bool? GetJsonArrayElementFlag(string elementName)
        {
            if (_jsonArrayElements.ContainsKey(elementName))
                return _jsonArrayElements[elementName];
            else
                return null;
        }

        private HashSet<string> _cDatas = new HashSet<string>();
        public void SetElement(string elementName, object value, bool isCDATA = false)
        {
            if (isCDATA)
            {
                if (!_cDatas.Contains(elementName))
                    _cDatas.Add(elementName);

                SetPropertyValue(elementName, value);
            }
            else
            {
                if (_cDatas.Contains(elementName))
                    _cDatas.Remove(elementName);

                SetPropertyValue(elementName, value);
            }
        }
        public void SetNSElement(string name, object value, bool isCDATA = false, string ns = null)
        {
            if (isCDATA)
            {
                if (!_cDatas.Contains(name))
                    _cDatas.Add(name);

                _SetPropertyValue(name, value);
            }
            else
            {
                if (_cDatas.Contains(name))
                    _cDatas.Remove(name);

                _SetPropertyValue(name, value);
            }

            if (ns != null)
                SetNSPrefix(ns);
        }

        public bool IsCDATA(string elementName)
        {
            return _cDatas.Contains(elementName);
        }
        //public int IndexOf(object item)
        //{
        //	return _list.IndexOf(item);
        //}

        //public void Insert(int index, object item)
        //{
        //	_list.Insert(index, item);
        //}

        //public void RemoveAt(int index)
        //{
        //	_list.RemoveAt(index);
        //}

        //public void Add(object item)
        //{
        //	_list.Add(item);
        //}

        //public bool Contains(object item)
        //{
        //	return _list.Contains(item);
        //}

        //public void CopyTo(object[] array, int arrayIndex)
        //{
        //	_list.CopyTo(array, arrayIndex);
        //}

        //public bool Remove(object item)
        //{
        //	return _list.Remove(item);
        //}
        //List<object> _list = new List<object>();
        //IEnumerator<object> IEnumerable<object>.GetEnumerator()
        //{
        //	return _list.GetEnumerator();
        //}

        //int IList.Add(object value)
        //{
        //	return ((IList)_list).Add(value);
        //}

        //void IList.Remove(object value)
        //{
        //	_list.Remove(value);
        //}

        //public void CopyTo(Array array, int index)
        //{
        //	_list.CopyTo(array.Cast<object>().ToArray(), index);
        //}
        //public int ListCount
        //{
        //	get { return _list.Count;  }
        //}
        //public object GetListItemAt(int index)
        //{
        //	return _list[index];
        //}

        public DataTable AsDataTable(string tableName = null,
            CultureInfo ci = null, Action<IDictionary<string, Type>> membersDiscovered = null,
            string[] selectedFields = null, string[] excludeFields = null)
        {
            return _kvpDict.Flatten().AsDataTable(tableName, ci, membersDiscovered,
                selectedFields, excludeFields);
        }

        public int Fill(DataTable dt)
        {
            return _kvpDict.Flatten().Fill(dt);
        }

        public IDataReader AsDataReader(Action<IDictionary<string, Type>> membersDiscovered = null, string[] selectedFields = null,
            string[] excludeFields = null)
        {
            return _kvpDict.Flatten().AsDataReader(membersDiscovered, selectedFields, excludeFields);
        }

        public static ChoDynamicObject FromDictionary(IDictionary kvpDict)
        {
            ChoDynamicObject obj = new ChoDynamicObject();
            if (kvpDict != null)
            {
                foreach (var key in kvpDict.Keys)
                    obj.AddOrUpdate(key.ToNString(), kvpDict[key]);
            }

            return obj;
        }

        public static ChoDynamicObject FromKeyValuePairs(IEnumerable<KeyValuePair<string, object>> kvps, string name = null)
        {
            ChoDynamicObject obj = new ChoDynamicObject(name);
            if (kvps != null)
            {
                foreach (var kvp in kvps)
                {
                    obj.AddOrUpdate(kvp.Key.ToNString(), kvp.Value);
                }
            }

            return obj;
        }

        public static ChoDynamicObject From(IEnumerable<KeyValuePair<string, object>> kvps, string name = null)
        {
            ChoDynamicObject obj = new ChoDynamicObject(name);
            if (kvps != null)
            {
                foreach (var kvp in kvps)
                {
                    obj.AddOrUpdate(kvp.Key.ToNString(), kvp.Value);
                }
            }

            return obj;
        }

        public dynamic[] Zip()
        {
            List<dynamic> result = new List<dynamic>();
            var kvpDict = _kvpDict;

            if (kvpDict == null)
                return result.ToArray();

            int index = 0;
            int length = 0;
            foreach (var kvp in kvpDict)
            {
                var value = kvp.Value as IList;
                if (value != null)
                    length = length < value.Count ? value.Count : length;
            }

            while (index < length)
            {
                ChoDynamicObject obj = new ChoDynamicObject();
                foreach (var kvp in kvpDict)
                {
                    var value = kvp.Value as IList;

                    if (value == null)
                    {
                        if (index == 0)
                            obj.Add(kvp.Key, kvp.Value);
                        else
                            obj.Add(kvp.Key, null);
                    }
                    else
                    {
                        if (index < value.Count)
                        {
                            obj.Add(kvp.Key, value[index]);
                        }
                        else
                        {
                            obj.Add(kvp.Key, null);
                        }
                    }

                }

                result.Add(obj);
                index++;
            }

            return result.ToArray();
        }

        private string _prefix = null;
        public void SetNSPrefix(string prefix)
        {
            if (prefix != ChoXmlNamespaceManager.DefaultNSToken && !prefix.IsNullOrWhiteSpace())
                _prefix = prefix;
        }
        public string GetNSPrefix()
        {
            return _prefix;
        }

        public ChoDynamicObject AddNamespace(string prefix, string uri, bool childrenOnly = false)
        {
            ChoGuard.ArgumentNotNullOrEmpty(prefix, nameof(prefix));
            ChoGuard.ArgumentNotNullOrEmpty(uri, nameof(uri));

            SetAttribute("@xmlns:{0}".FormatString(prefix), uri);
            AddPrefixNS(prefix, childrenOnly);
            return this;
        }

        private void AddPrefixNS(string prefix, bool childrenOnly = false)
        {
            _kvpDict = PrefixNS(prefix, _kvpDict);

            if (!childrenOnly)
            {
                _prefix = prefix;
                if (!_prefix.IsNullOrWhiteSpace())
                    DynamicObjectName = "{0}:{1}".FormatString(prefix, DynamicObjectName.IndexOf(":") > 0 ? DynamicObjectName.Substring(DynamicObjectName.IndexOf(":") + 1) : DynamicObjectName);
            }
        }

        private object PrefixNS(string prefix, object value)
        {
            if (value == null)
                return value;

            if (value is ChoDynamicObject)
            {
                ((ChoDynamicObject)value).AddPrefixNS(prefix);
                return value;
            }
            else if (value is IDictionary<string, object>)
            {
                return PrefixNS(prefix, value as IDictionary<string, object>);
            }
            else if (value is IList)
            {
                var ret = ((IList)value).OfType<object>().Select(value1 =>
                {
                    if (value1 is ChoDynamicObject)
                    {
                        ((ChoDynamicObject)value1).AddPrefixNS(prefix);
                        return value1;
                    }
                    else if (value1 is IDictionary<string, object>)
                    {
                        return PrefixNS(prefix, value1 as IDictionary<string, object>);
                    }
                    else
                    {
                        return PrefixNS(prefix, value1);
                    }
                });

                return value is Array ? (object)ret.ToArray() : (object)ret.ToList();
            }
            else
            {
                return value;
            }
        }

        private IDictionary<string, object> PrefixNS(string prefix, IDictionary<string, object> kvpDict)
        {
            if (kvpDict == null)
                return kvpDict;

            return kvpDict.ToDictionary(kvp => kvp.Key.StartsWith("@xmlns", StringComparison.InvariantCultureIgnoreCase) || IsAttribute(kvp.Key) ? kvp.Key : "{0}:{1}".FormatString(prefix, kvp.Key.IndexOf(":") > 0 ? kvp.Key.Substring(kvp.Key.IndexOf(":") + 1) : kvp.Key),
                kvp => PrefixNS(prefix, kvp.Value));
        }

        public object ConvertToObject<T>()
            where T : class, new()
        {
            return ChoObjectEx.ConvertToObject<T>(this);
        }

        public object ConvertToObject(Type type)
        {
            return ChoObjectEx.ConvertToObject(this, type);
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            foreach (KeyValuePair<string, object> kvp in _kvpDict)
            {
                info.AddValue(kvp.Key, kvp.Value);
            }
        }

        private IDictionary<string, object> AsShallowDictionary()
        {
            var dict = _kvpDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            return dict;
        }

        public IDictionary<string, object> AsDictionary(bool keepNSPrefix = false, ChoIgnoreFieldValueMode? ignoreFieldValueMode = null)
        {
            var dict = _kvpDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            Dictionary<string, object> retDict = new Dictionary<string, object>();
            string newKey = null;
            foreach (var key in dict.Keys.ToArray())
            {
                newKey = key;
                if (!keepNSPrefix)
                {
                    if (key.IndexOf(":") > 0)
                        newKey = key.Substring(key.IndexOf(":") + 1);
                }

                if (dict[key] is ChoDynamicObject)
                    retDict.Add(newKey, ((ChoDynamicObject)dict[key]).AsDictionary(keepNSPrefix, ignoreFieldValueMode));
                else
                    retDict.Add(newKey, dict[key]);
            }

            return retDict;
        }

        public IDictionary<string, object> AsXmlDictionary()
        {
            var dict = _kvpDict.ToDictionary(kvp => IsXmlAttribute(kvp.Key) ? $"{_attributePrefix}{kvp.Key}" : kvp.Key, kvp => kvp.Value);
            foreach (var key in dict.Keys.ToArray())
            {
                if (dict[key] is ChoDynamicObject)
                    dict[key] = ((ChoDynamicObject)dict[key]).AsXmlDictionary();
            }

            return dict;
        }

        public T ToObject<T>()
            where T : class, new()
        {
            T ret = ((IDictionary<string, object>)this).ToObject<T>();

            return ret;
        }

        public dynamic Cast()
        {
            return this;
        }

        public ChoDynamicObject IgnoreNullValues()
        {
            ChoDynamicObject dest = new ChoDynamicObject();
            foreach (var kvp in (ChoDynamicObject)this)
            {
                if (kvp.Value is ChoDynamicObject dobj)
                {
                    if (HasAllNullValues(dobj))
                        continue;

                    dest.Add(kvp.Key, kvp.Value);
                }
                else if (kvp.Value is IList list)
                {
                    List<object> output = new List<object>();
                    foreach (var item in list)
                    {
                        if (item is ChoDynamicObject dobj1)
                        {
                            if (HasAllNullValues(dobj1))
                                continue;
                        }

                        output.Add(item);
                    }
                    if (output.Count > 0)
                        dest.Add(kvp.Key, output.ToArray());
                }
                else
                    dest.Add(kvp.Key, kvp.Value);
            }

            return HasAllNullValues(dest) ? null : dest;

        }
        private bool HasAllNullValues(ChoDynamicObject src)
        {
            foreach (var v in src.Values)
            {
                if (v == null)
                    continue;
                else if (v is IList list)
                {
                    foreach (var item in list.OfType<ChoDynamicObject>())
                    {
                        if (!HasAllNullValues(item))
                            return false;
                    }
                }
                else if (v is ChoDynamicObject v1)
                {
                    if (HasAllNullValues(v1))
                        continue;
                    else
                        return false;
                }
                else
                    return false;
            }

            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoPropertyAttribute : Attribute
    {
        public string Name { get; set; }
        public ChoPropertyValueTrimOption TrimOption { get; set; }
    }

    public static class ChoDynamicObjectEx
    {
        public static IEnumerable<dynamic> AsDynamicEnumerable(this DataTable dt)
        {
            if (dt == null)
                yield break;

            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (var dr in dt.Rows.Cast<DataRow>())
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                    dict.Add(dt.Columns[i].ColumnName, dr[dt.Columns[i]]);

                yield return new ChoDynamicObject(dict);
            }
        }

        public static IEnumerable<dynamic> AsDynamicEnumerable(this IDataReader dr)
        {
            if (dr == null)
                yield break;

            while (dr.Read())
            {
                var dict = Enumerable.Range(0, dr.FieldCount)
                                 .ToDictionary(dr.GetName, dr.GetValue);
                yield return new ChoDynamicObject(dict);
            }
        }

        public static T ToObject<T>(this ChoDynamicObject obj)
            where T : class, new()
        {
            T ret = ((IDictionary<string, object>)obj).ToObject<T>();

            return ret;
        }
    }
}