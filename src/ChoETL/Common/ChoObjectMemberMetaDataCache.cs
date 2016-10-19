using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    public class ChoObjectMemberMetaDataCache
    {
        public static readonly ChoObjectMemberMetaDataCache Default = new ChoObjectMemberMetaDataCache();

        private readonly bool _turnOnMetaDataCache = true;
        private readonly object _padlock = new object();
        private readonly Dictionary<Type, bool> _isLockedCache = new Dictionary<Type, bool>();
        private readonly Dictionary<Type, Dictionary<string, object>> _defaultsCache = new Dictionary<Type, Dictionary<string, object>>();
        private readonly Dictionary<Type, Dictionary<string, bool>> _isReqCache = new Dictionary<Type, Dictionary<string, bool>>();
        private readonly Dictionary<Type, Dictionary<string, object[]>> _converterParams = new Dictionary<Type, Dictionary<string, object[]>>();

        public ChoObjectMemberMetaDataCache()
        {
            _turnOnMetaDataCache = ChoETLFramework.GetIniValue<bool>("TurnOnMetaDataCache", true);
        }

        public object GetDefaultValue(MemberInfo mi)
        {
            ChoGuard.ArgumentNotNull(mi, "MemberInfo");
            Type declaringType = mi.ReflectedType;
            if (!typeof(ChoRecord).IsAssignableFrom(declaringType)
                || declaringType.IsDynamicRecord())
                return null;

            InitDefaults(declaringType);

            if (!_defaultsCache.ContainsKey(declaringType)
                || !_defaultsCache[declaringType].ContainsKey(mi.Name))
                return null;

            return _defaultsCache[declaringType][mi.Name];
        }

        public bool IsRequired(MemberInfo mi)
        {
            ChoGuard.ArgumentNotNull(mi, "MemberInfo");
            Type declaringType = mi.ReflectedType;
            if (!typeof(ChoRecord).IsAssignableFrom(declaringType)
                || declaringType.IsDynamicRecord())
                return false;

            InitIsRequireds(declaringType);

            if (!_isReqCache.ContainsKey(declaringType)
                || !_isReqCache[declaringType].ContainsKey(mi.Name))
                return false;

            return _isReqCache[declaringType][mi.Name];
        }

        public object[] GetConverterParams(MemberInfo mi)
        {
            ChoGuard.ArgumentNotNull(mi, "MemberInfo");
            Type declaringType = mi.ReflectedType;
            if (!typeof(ChoRecord).IsAssignableFrom(declaringType)
                || declaringType.IsDynamicRecord())
                return null;

            InitConverterParams(declaringType);

            if (!_converterParams.ContainsKey(declaringType)
                || !_converterParams[declaringType].ContainsKey(mi.Name))
                return null;

            return _converterParams[declaringType][mi.Name];
        }

        private void InitIsLockedCache(Type declaringType)
        {
            if (!_turnOnMetaDataCache) return;

            ChoIniFile iniFile = ChoIniFile.New(declaringType.FullName);
            if (!_isLockedCache.ContainsKey(declaringType))
            {
                lock (_padlock)
                {
                    if (!_isLockedCache.ContainsKey(declaringType))
                        _isLockedCache.Add(declaringType, iniFile.GetValue<bool>("IsLocked"));
                }
            }
        }

        private void InitDefaults(Type declaringType)
        {
            if (_defaultsCache.ContainsKey(declaringType))
                return;

            lock (_padlock)
            {
                if (_defaultsCache.ContainsKey(declaringType))
                    return;

                InitIsLockedCache(declaringType);
                LoadDefaults(declaringType);
            }
        }

        private void InitIsRequireds(Type declaringType)
        {
            if (_isReqCache.ContainsKey(declaringType))
                return;

            lock (_padlock)
            {
                if (_isReqCache.ContainsKey(declaringType))
                    return;

                InitIsLockedCache(declaringType);
                LoadIsRequireds(declaringType);
            }
        }

        private void InitConverterParams(Type declaringType)
        {
            if (_converterParams.ContainsKey(declaringType))
                return;

            lock (_padlock)
            {
                if (_converterParams.ContainsKey(declaringType))
                    return;

                InitIsLockedCache(declaringType);
                LoadConverterParams(declaringType);
            }
        }

        private void LoadConverterParams(Type declaringType)
        {
            ChoIniFile iniFile = GetIniSection(declaringType, "FORMATTER");

            var dict = new Dictionary<string, object[]>();
            string parameters = null;
            ChoMemberAttribute memberAttribute = null;
            foreach (MemberInfo memberInfo in ChoType.GetMembers(declaringType))
            {
                memberAttribute = ChoType.GetAttribute<ChoMemberAttribute>(memberInfo);
                if (memberAttribute == null)
                    continue;

                try
                {
                    if (_turnOnMetaDataCache)
                        parameters = iniFile.GetValue(memberInfo.Name);

                    List<object> p = new List<object>();
                    if (!parameters.IsNullOrWhiteSpace())
                    {
                        foreach (string kv in parameters.SplitNTrim(';'))
                            p.Add(kv.SplitNTrim(','));
                    }

                    dict.Add(memberInfo.Name, parameters.IsNullOrWhiteSpace() ? null : p.ToArray());
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Failed to retrieve converter params for '{0}' member type from INI file. {1}".FormatString(ChoType.GetMemberName(memberInfo), ex.Message));
                }
            }
            _converterParams.Add(declaringType, dict);
        }

        private void LoadIsRequireds(Type declaringType)
        {
            ChoIniFile iniFile = GetIniSection(declaringType, "REQUIRED");

            var dict = new Dictionary<string, bool>();
            bool isRequired = false;
            ChoMemberAttribute memberAttribute = null;
            foreach (MemberInfo memberInfo in ChoType.GetMembers(declaringType))
            {
                memberAttribute = ChoType.GetAttribute<ChoMemberAttribute>(memberInfo);
                if (memberAttribute == null)
                    continue;

                try
                {
                    if (_turnOnMetaDataCache)
                        isRequired = iniFile.GetValue(memberInfo.Name, memberAttribute.IsRequired, _isLockedCache[declaringType]);
                    else
                        isRequired = memberAttribute.IsRequired;

                    dict.Add(memberInfo.Name, isRequired);
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Incorrect IsRequired value specified for '{0}' member type in INI file. Defaulted to false. {1}".FormatString(ChoType.GetMemberName(memberInfo), ex.Message));
                }
            }

            _isReqCache.Add(declaringType, dict);
        }

        private void LoadDefaults(Type declaringType)
        {
            ChoIniFile iniFile = GetIniSection(declaringType, "DEFAULT_VALUE");

            var dict = new Dictionary<string, object>();
            object defaultValue = null;
            string memberName = null; 

            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(declaringType).AsTypedEnumerable<PropertyDescriptor>().Where(pd => !pd.Attributes.OfType<ChoIgnoreMemberAttribute>().Any()))
            {
                try
                {
                    memberName = pd.Name;
                    if (_turnOnMetaDataCache) 
                        defaultValue = iniFile.GetValue(pd.Name, ChoType.GetDefaultValue(pd), _isLockedCache[declaringType]);
                    else
                        defaultValue = ChoType.GetDefaultValue(pd);

                    if (defaultValue is string)
                    {
                        defaultValue = ((string)defaultValue).ExpandProperties();
                    }

                    defaultValue = ChoConvert.ConvertFrom(defaultValue, pd.PropertyType);

                    dict.Add(pd.Name, defaultValue);
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while converting default value '{0}' to '{1}' member type. {2}".FormatString(defaultValue, ChoType.GetMemberName(pd), ex.Message));
                }
            }

            foreach (FieldInfo memberInfo in ChoType.GetFields(declaringType))
            {
                try
                {
                    memberName = memberInfo.Name;
                    if (_turnOnMetaDataCache)
                        defaultValue = iniFile.GetValue(memberInfo.Name, ChoType.GetDefaultValue(memberInfo), _isLockedCache[declaringType]);
                    else
                        defaultValue = ChoType.GetDefaultValue(memberInfo);

                    if (defaultValue is string)
                    {
                        defaultValue = ((string)defaultValue).ExpandProperties();
                    }

                    defaultValue = ChoConvert.ConvertFrom(defaultValue, memberInfo.FieldType);

                    dict.Add(memberInfo.Name, defaultValue);
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while converting default value '{0}' to '{1}' member type. {2}".FormatString(defaultValue, ChoType.GetMemberName(memberInfo), ex.Message));
                }
            }

            _defaultsCache.Add(declaringType, dict);
        }

        private ChoIniFile GetIniSection(Type type, string sectionName)
        {
            return ChoIniFile.New(type.FullName, sectionName);
        }
    }
}
