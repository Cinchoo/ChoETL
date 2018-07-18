using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoPropertyBag : DynamicObject
    {
        #region Instance Members

        private Dictionary<string, object> _kvpDict = null;
        private Func<Dictionary<string, object>> _func = null;
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

        #endregion Instance Members

        #region Constructors

        public ChoPropertyBag(bool watchChange = false) : this(null, watchChange)
        {
            _watchChange = watchChange;
        }

        public ChoPropertyBag(Dictionary<string, object> kvpDict) : this(null, false)
        {
            _kvpDict = kvpDict;
        }

        public ChoPropertyBag(IEnumerable<object> list) : this(null, false)
        {
            if (list != null)
            {
                int index = 0;
                _kvpDict = list.ToDictionary(x => String.Format("Field{0}", ++index), v => v, StringComparer.InvariantCultureIgnoreCase);
            }
        }

        public ChoPropertyBag(Func<Dictionary<string, object>> func, bool watchChange = false)
        {
            ThrowExceptionIfPropNotExists = false;
            IsFixed = false;
            IsReadOnly = false;

            _func = func;
            _watchChange = watchChange;
            if (_func == null)
                _kvpDict = new Dictionary<string, object>();

            Task.Run(() =>
            {
                Initialize();
                _afterLoaded.Raise(this, null);

                if (watchChange)
                {
                    int pollIntervalInSec = 60;
#if DEBUG
                    pollIntervalInSec = 10;
#endif

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

        protected virtual Dictionary<string, object> Seed()
        {
            return null;
        }

        [ChoIgnoreMember]
        public virtual string BagName
        {
            get;
            set;
        }

        #region DynamicObject Overrides

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            Dictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict == null)
                return base.GetDynamicMemberNames();
            else
                return kvpDict.Keys;
        }

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
            }
            return true;
        }

        #endregion DynamicObject Overrides

        #region Instance Members (Protected/Public)

        public virtual bool ContainsProperty(string key)
        {
            Dictionary<string, object> kvpDict = _kvpDict;
            return kvpDict != null && kvpDict.ContainsKey(key);
        }

        protected virtual bool GetPropertyValue(string name, out object result)
        {
            result = null;

            Dictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null && kvpDict.ContainsKey(name))
                result = AfterKVPLoaded(name, kvpDict[name]);
            else
            {
                if (ThrowExceptionIfPropNotExists)
                    return false;
            }
            return true;
        }

        protected virtual bool SetPropertyValue(string name, object value)
        {
            if (IsReadOnly)
                return false;

            Dictionary<string, object> kvpDict = _kvpDict;
            if (kvpDict != null)
            {
                if (!kvpDict.ContainsKey(name))
                {
                    if (ThrowExceptionIfPropNotExists)
                        return false;
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
                Dictionary<string, object> kvpDict = null;

                if (_watchChange)
                {
                    if (_func != null)
                        kvpDict = _func();
                    else
                        kvpDict = Seed();

                    if (kvpDict == null)
                        return;

                    Dictionary<string, object> mkvpDict = _kvpDict;
                    bool hasDiff = mkvpDict == null || kvpDict.Except(mkvpDict).Concat(mkvpDict.Except(kvpDict)).Any();
                    if (!hasDiff)
                        return;

                    _kvpDict = kvpDict;
                }
                else
                    kvpDict = _kvpDict;

                ERPSPropertyAttribute attr = null;
                object memberValue = null;
                string propName = null;
                //scan through members and load them
                foreach (var prop in ChoType.GetMembers(GetType()).Where(m => !m.HasAttribute(typeof(ChoIgnoreMemberAttribute)) && !ChoType.IsReadOnlyMember(m)))
                {
                    attr = ChoType.GetMemberAttribute<ERPSPropertyAttribute>(prop);
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

                        ChoType.ConvertNSetMemberValue(this, prop, memberValue);
                        ChoValidator.ValidateFor(this, prop);
                    }
                    catch (Exception ex)
                    {
                        ChoETLLog.Error("{0}: Error loading '{1}' property. {2}".FormatString(NName, prop.Name, ex.Message));
                        SetDefaultValue(prop, false);
                    }
                }
            }
            catch (Exception outerEx)
            {
                ChoETLLog.Error("{0}: Error loading options. {1}".FormatString(NName, outerEx.Message));
            }
        }

        private string NName
        {
            get { return BagName.IsNullOrWhiteSpace() ? GetType().Name : BagName; }
        }

        private void SetDefaultValue(MemberInfo mi, bool saveDefaultValue = false)
        {
            object defaultValue = GetDefaultValue(mi.Name); // ChoType.GetRawDefaultValue(mi);
            try
            {
                ChoType.SetMemberValue(this, mi, defaultValue);
                ChoETLLog.Error("{0}: Assigned default value '{1}' to '{2}' property.".FormatString(NName, defaultValue.ToNString(), mi.Name));
            }
            catch (Exception ex)
            {
                ChoETLLog.Error("{0}: Error assigning default value '{1}' to '{2}' property. {3}".FormatString(NName, defaultValue.ToNString(), mi.Name, ex.Message));
            }
        }


        #endregion

        protected virtual object AfterKVPLoaded(string key, object value)
        {
            return value;
        }
    }

    public enum ChoPropertyValueTrimOption { None, TrimStart, TrimEnd, Trim }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ERPSPropertyAttribute : Attribute
    {
        public string Name { get; set; }
        public ChoPropertyValueTrimOption TrimOption { get; set; }
    }

}