using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ChoETL
{
    [Serializable]
    public class ChoDynamicObject : DynamicObject, IDictionary<string, object> //, IXmlSerializable
    {
        private readonly static Dictionary<string, Type> _intrinsicTypes = new Dictionary<string, Type>();

        #region Instance Members

        private readonly object _padLock = new object();
        private IDictionary<string, object> _kvpDict = new Dictionary<string, object>();
        [IgnoreDataMember]
        private Func<IDictionary<string, object>> _func = null;
        private bool _watchChange = false;

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

        [ChoIgnoreMember]
        public virtual string DynamicObjectName
        {
            get;
            set;
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

        public Dictionary<string, string> AlternativeKeys
        {
            get;
            set;
        }

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

        public ChoDynamicObject() : this(false)
        {

        }

        public ChoDynamicObject(bool watchChange = false) : this(null, watchChange)
        {
            _watchChange = watchChange;
        }

        public ChoDynamicObject(IDictionary<string, object> kvpDict) : this(null, false)
        {
            _kvpDict = kvpDict;
        }

        public ChoDynamicObject(Func<IDictionary<string, object>> func, bool watchChange = false)
        {
            ThrowExceptionIfPropNotExists = false;
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

        #endregion Constructors

        public void Refresh()
        {
            Initialize();
        }

        public dynamic ConvertToNestedObject(char separator = '/')
        {
            return ChoExpandoObjectEx.ConvertToNestedObject(this, separator);
        }
        public dynamic ConvertToFlattenObject(char separator = '/')
        {
            return ChoExpandoObjectEx.ConvertToFlattenObject(this, separator);
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

        #region DynamicObject Overrides

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return GetPropertyValue(binder.Name, out result);
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

        public virtual bool ContainsProperty(string key)
        {
            IDictionary<string, object> kvpDict = _kvpDict;
            return kvpDict != null && kvpDict.ContainsKey(key);
        }

        protected virtual bool GetPropertyValue(string name, out object result)
        {
            result = null;

            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                if (AlternativeKeys != null && AlternativeKeys.ContainsKey(name))
                {
                    var newName = AlternativeKeys[name];
                    if (!newName.IsNullOrWhiteSpace())
                        name = newName;
                }

                if (kvpDict.ContainsKey(name))
                    result = AfterKVPLoaded(name, kvpDict[name]);
                else
                {
                    if (ThrowExceptionIfPropNotExists)
                        return false;
                }
            }
            return true;
        }

        protected virtual bool SetPropertyValue(string name, object value)
        {
            if (IsReadOnly)
                return false;

            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                if (AlternativeKeys != null && AlternativeKeys.ContainsKey(name))
                {
                    var newName = AlternativeKeys[name];
                    if (!newName.IsNullOrWhiteSpace())
                        name = newName;
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

        protected virtual object GetDefaultValue(string name)
        {
            return ChoType.GetRawDefaultValue(ChoType.GetMemberInfo(GetType(), name));
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

        public int Count
        {
            get
            {
                IDictionary<string, object> kvpDict = _kvpDict;
                return kvpDict != null ? kvpDict.Count : 0;
            }
        }

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

        public bool ContainsKey(string key)
        {
            return ContainsProperty(key);
        }

        public void Add(string key, object value)
        {
            SetPropertyValue(key, value);
        }

        public bool Remove(string key)
        {
            IDictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null && kvpDict.ContainsKey(key))
            {
                kvpDict.Remove(key);
            }
            return false;
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

        public string Dump()
        {
            return ChoUtility.ToStringEx(this);
        }
        public string DumpAsJson()
        {
            return ChoUtility.DumpAsJson(this);
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

        public void ReadXml(XmlReader reader)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(string));
            XmlSerializer valueSerializer = new XmlSerializer(typeof(object));

            bool wasEmpty = reader.IsEmptyElement;
            reader.Read();

            if (wasEmpty)
                return;

            reader.ReadStartElement("dynamic");
            while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
            {
                reader.ReadStartElement("kvp");

                reader.ReadStartElement("key");
                string key = (string)keySerializer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("value");
                reader.MoveToContent();
                Type type = GetType(reader.LocalName);

                object value = null;
                if (type == typeof(ChoDynamicObject))
                {
                    ChoDynamicObject dobj = new ChoDynamicObject();
                    dobj.ReadXml(XmlReader.Create(new StringReader(reader.ReadOuterXml())));
                    value = dobj;
                }
                else
                {
                    ChoNullNSXmlSerializer serializer = new ChoNullNSXmlSerializer(type);
                    value = serializer.Deserialize(reader);
                }
                //object value = ChoUtility.XmlDeserialize<object>(reader); // (TValue)valueSerializer.Deserialize(reader);
                reader.ReadEndElement();

                this.Add(key, value);

                reader.ReadEndElement();
                reader.MoveToContent();
            }
            reader.ReadEndElement();
            //reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            XmlSerializer keySerializer = new XmlSerializer(typeof(string));

            writer.WriteStartElement("dynamic");

            foreach (string key in this.Keys)
            {
                writer.WriteStartElement("kvp");

                writer.WriteStartElement("key");
                keySerializer.Serialize(writer, key);
                writer.WriteEndElement();

                writer.WriteStartElement("value");
                object value = this[key];
                if (value != null)
                {
                    if (value is ChoDynamicObject)
                    {
                        ((ChoDynamicObject)value).WriteXml(writer);
                    }
                    else
                    {
                        XmlSerializer valueSerializer = new XmlSerializer(value.GetType());
                        valueSerializer.Serialize(writer, value);
                    }
                }

                writer.WriteEndElement();

                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ChoPropertyAttribute : Attribute
    {
        public string Name { get; set; }
        public ChoPropertyValueTrimOption TrimOption { get; set; }
    }

}