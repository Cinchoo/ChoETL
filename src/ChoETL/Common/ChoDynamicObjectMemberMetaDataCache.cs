using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace ChoETL
{
    public class ChoDynamicObjectMemberMetaDataCache
    {
        public static readonly ChoDynamicObjectMemberMetaDataCache Default = new ChoDynamicObjectMemberMetaDataCache();

        private readonly bool _turnOnMetaDataCache = true;
        private readonly object _padlock = new object();
        private readonly Dictionary<Type, bool> _isLockedCache = new Dictionary<Type, bool>();
        private readonly Dictionary<string, Dictionary<string, string>> _defaultsCache = new Dictionary<string, Dictionary<string, string>>();
        private readonly Dictionary<string, Dictionary<string, object>> _fallbackCache = new Dictionary<string, Dictionary<string, object>>();
        private readonly Dictionary<string, Dictionary<string, string>> _formatsCache = new Dictionary<string, Dictionary<string, string>>();
        private readonly Dictionary<string, Dictionary<string, string>> _fieldExprCache = new Dictionary<string, Dictionary<string, string>>();
        private readonly Dictionary<string, Dictionary<string, Type>> _dataTypeCache = new Dictionary<string, Dictionary<string, Type>>();
        private readonly Dictionary<string, Dictionary<string, ChoCSVRecordFieldAttribute>> _csvRecordFieldOptionCache = new Dictionary<string, Dictionary<string, ChoCSVRecordFieldAttribute>>();
        private readonly Dictionary<string, OrderedDictionary> _flRecordFieldOptionCache = new Dictionary<string, OrderedDictionary>();
        private readonly Dictionary<string, Dictionary<string, ChoDbRecordFieldAttribute>> _dbRecordFieldOptionCache = new Dictionary<string, Dictionary<string, ChoDbRecordFieldAttribute>>();
        private readonly Dictionary<string, Dictionary<string, List<Tuple<IValueConverter, string>>>> _valueConverterCache = new Dictionary<string, Dictionary<string, List<Tuple<IValueConverter, string>>>>();
        private readonly Dictionary<string, Dictionary<string, Tuple<IValueConverter, string>>> _seqGeneratorCache = new Dictionary<string, Dictionary<string, Tuple<IValueConverter, string>>>();

        public ChoDynamicObjectMemberMetaDataCache()
        {
            _turnOnMetaDataCache = ChoETLFramework.GetIniValue<bool>("TurnOnDynamicMetaDataCache", true);
        }

        //public IEnumerable<string> GetFieldNames(ChoIniFile iniFile1)
        //{
        //    ChoGuard.ArgumentNotNull(iniFile1, "iniFile");

        //    ChoIniFile iniFile = iniFile1.OpenRecordParamsSection();
        //    string fieldNames = iniFile1.GetFieldNames();

        //    if (fieldNames.IsNullOrWhiteSpace())
        //        throw new ApplicationException("Missing field names in the '{0}' ini file.".FormatString(iniFile.FilePath));

        //    return fieldNames.SplitNTrim();
        //}

        public Dictionary<string, string> GetDefaultValues(ChoIniFile iniFile)
        {
            ChoGuard.ArgumentNotNull(iniFile, "iniFile");

            InitDefaults(iniFile);

            if (!_defaultsCache.ContainsKey(iniFile.Key))
                return new Dictionary<string, string>();

            return _defaultsCache[iniFile.Key];
        }

        private void InitDefaults(ChoIniFile iniFile)
        {
            if (_defaultsCache.ContainsKey(iniFile.Key))
                return;

            lock (_padlock)
            {
                if (_defaultsCache.ContainsKey(iniFile.Key))
                    return;

                LoadDefaults(iniFile);
            }
        }

        private void LoadDefaults(ChoIniFile iniFile1)
        {
            ChoIniFile iniFile = iniFile1.GetSection("DEFAULT_VALUE");

            var dict = new Dictionary<string, string>();
            string defaultValue = null;
            Dictionary<string, string> iniKeyValuesDict = iniFile.KeyValuesDict;
            foreach (string fieldName in iniKeyValuesDict.Keys)
            {
                if (fieldName.IsNullOrWhiteSpace()) continue;
                if (!iniKeyValuesDict.ContainsKey(fieldName)) continue;

                try
                {
                    defaultValue = iniKeyValuesDict[fieldName].ExpandProperties();
                    dict.Add(fieldName, defaultValue);
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while converting default value '{0}' for '{1}' member. {2}".FormatString(defaultValue, fieldName, ex.Message));
                }
            }
            _defaultsCache.Add(iniFile1.Key, dict);
        }

        public Dictionary<string, object> GetFallbackValues(ChoIniFile iniFile)
        {
            ChoGuard.ArgumentNotNull(iniFile, "iniFile");

            InitFallbacks(iniFile);

            if (!_fallbackCache.ContainsKey(iniFile.Key))
                return new Dictionary<string, object>();

            return _fallbackCache[iniFile.Key];
        }

        private void InitFallbacks(ChoIniFile iniFile)
        {
            if (_fallbackCache.ContainsKey(iniFile.Key))
                return;

            lock (_padlock)
            {
                if (_fallbackCache.ContainsKey(iniFile.Key))
                    return;

                LoadFallbacks(iniFile);
            }
        }

        private void LoadFallbacks(ChoIniFile iniFile1)
        {
            ChoIniFile iniFile = iniFile1.GetSection("FALLBACK_VALUE");

            var dict = new Dictionary<string, object>();
            object fallbackValue = null;
            Dictionary<string, string> iniKeyValuesDict = iniFile.KeyValuesDict;
            Type fieldType = null;
            foreach (string fieldName in iniKeyValuesDict.Keys)
            {
                if (fieldName.IsNullOrWhiteSpace()) continue;
                if (!iniKeyValuesDict.ContainsKey(fieldName)) continue;

                try
                {
                    fallbackValue = iniKeyValuesDict[fieldName].ExpandProperties();
                    fieldType = typeof(string);
                    if (ChoDynamicObjectMemberMetaDataCache.Default.GetDataTypes(iniFile1).ContainsKey(fieldName))
                        fieldType = ChoDynamicObjectMemberMetaDataCache.Default.GetDataTypes(iniFile1)[fieldName];

                    //Look for converters and convert the value
                    if (ChoDynamicObjectMemberMetaDataCache.Default.GetValueConverters(iniFile1).ContainsKey(fieldName))
                    {
                        foreach (Tuple<IValueConverter, string> vcPair in ChoDynamicObjectMemberMetaDataCache.Default.GetValueConverters(iniFile1)[fieldName])
                        {
                            if (!vcPair.Item1.IsNull())
                            {
                                fallbackValue = vcPair.Item1.Convert(fallbackValue, fieldType, vcPair.Item2, null);
                            }
                        }
                    }

                    fallbackValue = Convert.ChangeType(fallbackValue, fieldType);

                    dict.Add(fieldName, fallbackValue);
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while converting fallback value '{0}' for '{1}' member. {2}".FormatString(fallbackValue, fieldName, ex.Message));
                }
            }
            _fallbackCache.Add(iniFile1.Key, dict);
        }

        public Dictionary<string, string> GetFormatters(ChoIniFile iniFile)
        {
            ChoGuard.ArgumentNotNull(iniFile, "iniFile");

            InitFormats(iniFile);

            if (!_formatsCache.ContainsKey(iniFile.Key))
                return new Dictionary<string, string>();

            return _formatsCache[iniFile.Key];
        }

        private void InitFormats(ChoIniFile iniFile)
        {
            if (_formatsCache.ContainsKey(iniFile.Key))
                return;

            lock (_padlock)
            {
                if (_formatsCache.ContainsKey(iniFile.Key))
                    return;

                LoadFormats(iniFile);
            }
        }

        private void LoadFormats(ChoIniFile iniFile1)
        {
            ChoIniFile iniFile = iniFile1.GetSection("FORMAT");

            var dict = new Dictionary<string, string>();
            string value = null;
            Dictionary<string, string> iniKeyValuesDict = iniFile.KeyValuesDict;
            foreach (string fieldName in iniKeyValuesDict.Keys) //GetFieldNames(iniFile1))
            {
                if (fieldName.IsNullOrWhiteSpace()) continue;
                if (!iniKeyValuesDict.ContainsKey(fieldName)) continue;

                try
                {
                    value = iniKeyValuesDict[fieldName].ExpandProperties();
                    dict.Add(fieldName, value);
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while loading format value '{0}' for '{1}' member. {2}".FormatString(value, fieldName, ex.Message));
                }
            }
            _formatsCache.Add(iniFile1.Key, dict);
        }

        public Dictionary<string, Type> GetDataTypes(ChoIniFile iniFile)
        {
            ChoGuard.ArgumentNotNull(iniFile, "iniFile");

            InitDataTypes(iniFile);

            if (!_dataTypeCache.ContainsKey(iniFile.Key))
                return new Dictionary<string, Type>();

            return _dataTypeCache[iniFile.Key];
        }

        private void InitDataTypes(ChoIniFile iniFile)
        {
            if (_dataTypeCache.ContainsKey(iniFile.Key))
                return;

            lock (_padlock)
            {
                if (_dataTypeCache.ContainsKey(iniFile.Key))
                    return;

                LoadDataTypes(iniFile);
            }
        }

        private void LoadDataTypes(ChoIniFile iniFile1)
        {
            ChoIniFile iniFile = iniFile1.GetSection("DATATYPE");

            var dict = new Dictionary<string, Type>();
            Type value = null;
            Dictionary<string, string> iniKeyValuesDict = iniFile.KeyValuesDict;
            foreach (string fieldName in iniKeyValuesDict.Keys) //GetFieldNames(iniFile1))
            {
                if (fieldName.IsNullOrWhiteSpace()) continue;
                if (!iniKeyValuesDict.ContainsKey(fieldName)) continue;

                try
                {
                    value = Type.GetType(iniKeyValuesDict[fieldName]);
                    if (value != null)
                        dict.Add(fieldName, value);
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while loading format value '{0}' for '{1}' member. {2}".FormatString(value, fieldName, ex.Message));
                }
            }
            _dataTypeCache.Add(iniFile1.Key, dict);
        }

        public Dictionary<string, ChoCSVRecordFieldAttribute> GetCSVRecordFieldOptions(ChoIniFile iniFile)
        {
            ChoGuard.ArgumentNotNull(iniFile, "iniFile");

            InitCSVRecordFieldOptions(iniFile);

            if (!_csvRecordFieldOptionCache.ContainsKey(iniFile.Key))
                return new Dictionary<string, ChoCSVRecordFieldAttribute>();

            return _csvRecordFieldOptionCache[iniFile.Key];
        }

        private void InitCSVRecordFieldOptions(ChoIniFile iniFile)
        {
            if (_csvRecordFieldOptionCache.ContainsKey(iniFile.Key))
                return;

            lock (_padlock)
            {
                if (_csvRecordFieldOptionCache.ContainsKey(iniFile.Key))
                    return;

                LoadCSVRecordFieldOptions(iniFile);
            }
        }

        private void LoadCSVRecordFieldOptions(ChoIniFile iniFile1)
        {
            ChoIniFile iniFile = iniFile1.GetSection("CSV_RECORD_FIELD");

            var dict = new Dictionary<string, ChoCSVRecordFieldAttribute>();
            ChoCSVRecordFieldAttribute value;
            Dictionary<string, string> iniKeyValuesDict = iniFile.KeyValuesDict;
            if (iniKeyValuesDict.Count == 0)
                throw new ApplicationException("Missing CSV field names in the '{0}' ini file.".FormatString(iniFile1.FilePath));
            foreach (string fieldName in iniKeyValuesDict.Keys) //GetFieldNames(iniFile1))
            {
                if (fieldName.IsNullOrWhiteSpace()) continue;
                if (!iniKeyValuesDict.ContainsKey(fieldName)) continue;

                value = new ChoCSVRecordFieldAttribute(0);
                value.FieldValueTrimOption = ChoFieldValueTrimOption.Trim;
                value.FieldValueJustification = ChoFieldValueJustification.Left;
                foreach (KeyValuePair<string, string> kvp in iniKeyValuesDict[fieldName].ToKeyValuePairs())
                {
                    try
                    {
                        ChoType.ConvertNSetMemberValue(value, kvp.Key, kvp.Value);
                    }
                    catch (Exception ex)
                    {
                        ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while loading record field '{0}' for '{1}' field. {2}".FormatString(iniKeyValuesDict[fieldName], fieldName, ex.Message));
                    }
                }
                value.Validate();
                dict.Add(fieldName, value);
            }
            _csvRecordFieldOptionCache.Add(iniFile1.Key, dict);
        }

        public OrderedDictionary GetFixedLengthRecordFieldOptions(ChoIniFile iniFile)
        {
            ChoGuard.ArgumentNotNull(iniFile, "iniFile");

            InitFixedLengthRecordFieldOptions(iniFile);

            if (!_flRecordFieldOptionCache.ContainsKey(iniFile.Key))
                return new OrderedDictionary();

            return _flRecordFieldOptionCache[iniFile.Key];
        }

        private void InitFixedLengthRecordFieldOptions(ChoIniFile iniFile)
        {
            if (_flRecordFieldOptionCache.ContainsKey(iniFile.Key))
                return;

            lock (_padlock)
            {
                if (_flRecordFieldOptionCache.ContainsKey(iniFile.Key))
                    return;

                LoadFixedLengthRecordFieldOptions(iniFile);
            }
        }

        private void LoadFixedLengthRecordFieldOptions(ChoIniFile iniFile1)
        {
            ChoIniFile iniFile = iniFile1.GetSection("FIXED_LENGTH_RECORD_FIELD");

            var dict = new OrderedDictionary();
            ChoFixedLengthRecordFieldAttribute value;
            Dictionary<string, string> iniKeyValuesDict = iniFile.KeyValuesDict;
            if (iniKeyValuesDict.Count == 0)
                throw new ApplicationException("Missing fixed length field names in the '{0}' ini file.".FormatString(iniFile1.FilePath));
            foreach (string fieldName in iniKeyValuesDict.Keys) //GetFieldNames(iniFile1))
            {
                if (fieldName.IsNullOrWhiteSpace()) continue;
                if (!iniKeyValuesDict.ContainsKey(fieldName)) continue;

                try
                {
                    value = new ChoFixedLengthRecordFieldAttribute();
                    value.FieldValueTrimOption = ChoFieldValueTrimOption.Trim;
                    value.FieldValueJustification = ChoFieldValueJustification.Left;
                    foreach (KeyValuePair<string, string> kvp in iniKeyValuesDict[fieldName].ToKeyValuePairs())
                    {
                        try
                        {
                            ChoType.ConvertNSetMemberValue(value, kvp.Key, kvp.Value);
                        }
                        catch (Exception ex)
                        {
                            ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while loading record field option '{0}' for '{1}' field. {2}".FormatString(iniKeyValuesDict[fieldName], fieldName, ex.Message));
                        }
                    }
                    value.Validate();
                    dict.Add(fieldName, value);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException("[FieldName: {0}] - {1}".FormatString(fieldName, ex.Message));
                }
            }
            _flRecordFieldOptionCache.Add(iniFile1.Key, dict);
        }

        public Dictionary<string, List<Tuple<IValueConverter, string>>> GetValueConverters(ChoIniFile iniFile)
        {
            ChoGuard.ArgumentNotNull(iniFile, "iniFile");

            InitValueConverters(iniFile);

            if (!_valueConverterCache.ContainsKey(iniFile.Key))
                return new Dictionary<string, List<Tuple<IValueConverter, string>>>();

            return _valueConverterCache[iniFile.Key];
        }

        private void InitValueConverters(ChoIniFile iniFile)
        {
            if (_valueConverterCache.ContainsKey(iniFile.Key))
                return;

            lock (_padlock)
            {
                if (_valueConverterCache.ContainsKey(iniFile.Key))
                    return;

                LoadValueConverters(iniFile);
            }
        }

        private void LoadValueConverters(ChoIniFile iniFile1)
        {
            ChoIniFile iniFile = iniFile1.GetSection("CONVERTER");

            var dict = new Dictionary<string, List<Tuple<IValueConverter, string>>>();
            IValueConverter value = null;
            Dictionary<string, string> iniKeyValuesDict = iniFile.KeyValuesDict;
            foreach (string fieldName in iniKeyValuesDict.Keys) //GetFieldNames(iniFile1))
            {
                if (fieldName.IsNullOrWhiteSpace()) continue;
                if (!iniKeyValuesDict.ContainsKey(fieldName)) continue;
                if (dict.ContainsKey(fieldName)) continue;

                dict.Add(fieldName, new List<Tuple<IValueConverter, string>>());

                if (!iniKeyValuesDict[fieldName].IsNullOrWhiteSpace())
                {
                    foreach (string val in iniKeyValuesDict[fieldName].SplitNTrim(";"))
                    {
                        if (val.IsNullOrWhiteSpace()) continue;

                        string vcType = null;
                        string vcParam = null;
                        foreach (KeyValuePair<string, string> convParams in val.ToKeyValuePairs('&'))
                        {
                            if (convParams.Key == "Type")
                                vcType = convParams.Value;
                            else if (convParams.Key == "Parameter")
                                vcParam = convParams.Value;
                        }

                        if (!vcType.IsNullOrWhiteSpace())
                        {
                            try
                            {
                                value = Activator.CreateInstance(ChoType.GetType(vcType)) as IValueConverter;
                                if (value != null)
                                    dict[fieldName].Add(new Tuple<IValueConverter, string>(value, vcParam));
                            }
                            catch (Exception ex)
                            {
                                ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while loading format value '{0}' for '{1}' member. {2}".FormatString(value, fieldName, ex.Message));
                            }
                        }
                    }
                }
            }
            _valueConverterCache.AddOrUpdate(iniFile1.Key, dict);
        }

        public Dictionary<string, string> GetFieldExprs(ChoIniFile iniFile)
        {
            ChoGuard.ArgumentNotNull(iniFile, "iniFile");

            InitFieldExprs(iniFile);

            if (!_fieldExprCache.ContainsKey(iniFile.Key))
                return new Dictionary<string, string>();

            return _fieldExprCache[iniFile.Key];
        }

        private void InitFieldExprs(ChoIniFile iniFile)
        {
            if (_fieldExprCache.ContainsKey(iniFile.Key))
                return;

            lock (_padlock)
            {
                if (_fieldExprCache.ContainsKey(iniFile.Key))
                    return;

                LoadFieldExprs(iniFile);
            }
        }

        private void LoadFieldExprs(ChoIniFile iniFile1)
        {
            ChoIniFile iniFile = iniFile1.GetSection("EXPRESSION_FIELD");

            var dict = new Dictionary<string, string>();
            string value = null;
            Dictionary<string, string> iniKeyValuesDict = iniFile.KeyValuesDict;
            foreach (string fieldName in iniKeyValuesDict.Keys)
            {
                if (fieldName.IsNullOrWhiteSpace()) continue;
                if (!iniKeyValuesDict.ContainsKey(fieldName)) continue;

                try
                {
                    value = iniKeyValuesDict[fieldName].ExpandProperties();
                    dict.Add(fieldName, value);
                }
                catch (Exception ex)
                {
                    ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while loading field expression value '{0}' for '{1}' member. {2}".FormatString(value, fieldName, ex.Message));
                }
            }
            _fieldExprCache.Add(iniFile1.Key, dict);
        }

        public object ConvertValue(ChoIniFile iniFile, string fieldName, object fieldValue, Type fieldType = null)
        {
            if (fieldType == null)
            {
                if (iniFile != null && ChoDynamicObjectMemberMetaDataCache.Default.GetDataTypes(iniFile).ContainsKey(fieldName))
                    fieldType = ChoDynamicObjectMemberMetaDataCache.Default.GetDataTypes(iniFile)[fieldName];

                if (fieldType == null)
                {
                    if (fieldValue == null)
                        return null;

                    fieldType = fieldValue.GetType();
                }
            }

            if (fieldType == null)
                return fieldValue;

            //Look for converters and convert the value
            if (iniFile != null && ChoDynamicObjectMemberMetaDataCache.Default.GetValueConverters(iniFile).ContainsKey(fieldName))
            {
                foreach (Tuple<IValueConverter, string> vcPair in ChoDynamicObjectMemberMetaDataCache.Default.GetValueConverters(iniFile)[fieldName])
                {
                    if (!vcPair.Item1.IsNull())
                    {
                        fieldValue = vcPair.Item1.Convert(fieldValue, fieldType, vcPair.Item2, null);
                    }
                }
            }

            if (!fieldValue.IsNullOrDbNull())
                fieldValue = Convert.ChangeType(fieldValue, fieldType);

            if (iniFile != null && ChoDynamicObjectMemberMetaDataCache.Default.GetFormatters(iniFile).ContainsKey(fieldName))
            {
                string format = ChoDynamicObjectMemberMetaDataCache.Default.GetFormatters(iniFile)[fieldName];
                if (!format.IsNullOrWhiteSpace())
                {
                    fieldValue = String.Format("{0:" + format + "}", fieldValue);
                }
            }

            return fieldValue;

        }

        public object ConvertBackValue(ChoIniFile iniFile, string fieldName, object fieldValue, Type fieldType = null)
        {
            if (fieldType == null)
            {
                if (iniFile != null && ChoDynamicObjectMemberMetaDataCache.Default.GetDataTypes(iniFile).ContainsKey(fieldName))
                    fieldType = ChoDynamicObjectMemberMetaDataCache.Default.GetDataTypes(iniFile)[fieldName];

                if (fieldType == null)
                {
                    if (fieldValue == null)
                        return null;

                    fieldType = fieldValue.GetType();
                }
            }

            if (fieldType == null)
                return fieldValue;

            //Look for converters and convert the value
            if (iniFile != null && ChoDynamicObjectMemberMetaDataCache.Default.GetValueConverters(iniFile).ContainsKey(fieldName))
            {
                foreach (Tuple<IValueConverter, string> vcPair in ChoDynamicObjectMemberMetaDataCache.Default.GetValueConverters(iniFile)[fieldName].AsEnumerable().Reverse())
                {
                    if (!vcPair.Item1.IsNull())
                    {
                        fieldValue = vcPair.Item1.ConvertBack(fieldValue, fieldType, vcPair.Item2, null);
                    }
                }
            }

            if (!fieldValue.IsNullOrDbNull())
                fieldValue = Convert.ChangeType(fieldValue, fieldType);

            if (iniFile != null && ChoDynamicObjectMemberMetaDataCache.Default.GetFormatters(iniFile).ContainsKey(fieldName))
            {
                string format = ChoDynamicObjectMemberMetaDataCache.Default.GetFormatters(iniFile)[fieldName];
                if (!format.IsNullOrWhiteSpace())
                {
                    fieldValue = String.Format("{0:" + format + "}", fieldValue);
                }
            }

            return fieldValue;
        }

        public Type GetDynamicMemberType(ChoIniFile iniFile, string memberName)
        {
            Type fieldType = typeof(string);
            if (iniFile != null && ChoDynamicObjectMemberMetaDataCache.Default.GetDataTypes(iniFile).ContainsKey(memberName))
                fieldType = ChoDynamicObjectMemberMetaDataCache.Default.GetDataTypes(iniFile)[memberName];

            return Nullable.GetUnderlyingType(fieldType) ?? fieldType;
        }

        public Dictionary<string, ChoDbRecordFieldAttribute> GetDbRecordFieldOptions(ChoIniFile iniFile)
        {
            ChoGuard.ArgumentNotNull(iniFile, "iniFile");

            InitDbRecordFieldOptions(iniFile);

            if (!_dbRecordFieldOptionCache.ContainsKey(iniFile.Key))
                return new Dictionary<string, ChoDbRecordFieldAttribute>();

            return _dbRecordFieldOptionCache[iniFile.Key];
        }

        private void InitDbRecordFieldOptions(ChoIniFile iniFile)
        {
            if (_dbRecordFieldOptionCache.ContainsKey(iniFile.Key))
                return;

            lock (_padlock)
            {
                if (_dbRecordFieldOptionCache.ContainsKey(iniFile.Key))
                    return;

                LoadDbRecordFieldOptions(iniFile);
            }
        }

        private void LoadDbRecordFieldOptions(ChoIniFile iniFile1)
        {
            ChoIniFile iniFile = iniFile1.GetSection("Db_RECORD_FIELD");

            var dict = new Dictionary<string, ChoDbRecordFieldAttribute>();
            ChoDbRecordFieldAttribute value;
            Dictionary<string, string> iniKeyValuesDict = iniFile.KeyValuesDict;
            foreach (string fieldName in iniKeyValuesDict.Keys)
            {
                if (fieldName.IsNullOrWhiteSpace()) continue;
                if (!iniKeyValuesDict.ContainsKey(fieldName)) continue;

                value = new ChoDbRecordFieldAttribute();
                foreach (KeyValuePair<string, string> kvp in iniKeyValuesDict[fieldName].ToKeyValuePairs())
                {
                    try
                    {
                        ChoType.ConvertNSetMemberValue(value, kvp.Key, kvp.Value);
                    }
                    catch (Exception ex)
                    {
                        ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while loading record field '{0}' for '{1}' field. {2}".FormatString(iniKeyValuesDict[fieldName], fieldName, ex.Message));
                    }
                }
                value.Validate();
                dict.Add(fieldName, value);
            }
            _dbRecordFieldOptionCache.Add(iniFile1.Key, dict);
        }

        public Dictionary<string, Tuple<IValueConverter, string>> GetSequenceGenerators(ChoIniFile iniFile)
        {
            ChoGuard.ArgumentNotNull(iniFile, "iniFile");

            InitSequences(iniFile);

            if (!_seqGeneratorCache.ContainsKey(iniFile.Key))
                return new Dictionary<string, Tuple<IValueConverter, string>>();

            return _seqGeneratorCache[iniFile.Key];
        }

        private void InitSequences(ChoIniFile iniFile)
        {
            if (_seqGeneratorCache.ContainsKey(iniFile.Key))
                return;

            lock (_padlock)
            {
                if (_seqGeneratorCache.ContainsKey(iniFile.Key))
                    return;

                LoadSequences(iniFile);
            }
        }

        private void LoadSequences(ChoIniFile iniFile1)
        {
            ChoIniFile iniFile = iniFile1.GetSection("SEQUENCE_VALUE_GENERATOR");

            var dict = new Dictionary<string, Tuple<IValueConverter, string>>();
            IValueConverter value = null;
            Dictionary<string, string> iniKeyValuesDict = iniFile.KeyValuesDict;
            foreach (string fieldName in iniKeyValuesDict.Keys)
            {
                if (fieldName.IsNullOrWhiteSpace()) continue;
                if (!iniKeyValuesDict.ContainsKey(fieldName)) continue;
                if (dict.ContainsKey(fieldName)) continue;

                if (!iniKeyValuesDict[fieldName].IsNullOrWhiteSpace())
                {
                    string val = iniKeyValuesDict[fieldName];

                    string vcType = null;
                    string vcParam = null;
                    foreach (KeyValuePair<string, string> convParams in val.ToKeyValuePairs())
                    {
                        if (convParams.Key == "Type")
                            vcType = convParams.Value;
                        else if (convParams.Key == "Parameter")
                            vcParam = convParams.Value.Replace('&', ';');
                    }

                    if (!vcType.IsNullOrWhiteSpace())
                    {
                        try
                        {
                            value = Activator.CreateInstance(ChoType.GetType(vcType)) as IValueConverter;
                            if (value != null)
                                dict.Add(fieldName, new Tuple<IValueConverter, string>(value, vcParam));
                        }
                        catch (Exception ex)
                        {
                            ChoETLFramework.WriteLog(ChoETLFramework.Switch.TraceError, "Error while loading format value '{0}' for '{1}' member. {2}".FormatString(value, fieldName, ex.Message));
                        }
                    }
                }
            }
            _seqGeneratorCache.Add(iniFile1.Key, dict);
        }

        private ChoFileHeaderAttribute _headerAttr = null;
        public ChoFileHeaderAttribute GetFileHeaderAttribute(ChoIniFile iniFile)
        {
            if (_headerAttr != null)
                return _headerAttr;

            lock (_padlock)
            {
                if (_headerAttr != null)
                    return _headerAttr;
                
                _headerAttr = ChoFileHeaderAttribute.Default;
                ChoIniFile iniFile1 = iniFile.GetSection("FILE_HEADER");
                _headerAttr.FieldValueJustification = iniFile1.GetValue<ChoFieldValueJustification>("FieldValueJustification", _headerAttr.FieldValueJustification, true);
                _headerAttr.FieldValueTrimOption = iniFile1.GetValue<ChoFieldValueTrimOption>("FieldValueTrimOption", _headerAttr.FieldValueTrimOption, true);
                _headerAttr.FillChar = iniFile1.GetValue<char>("FillChar", _headerAttr.FillChar, true);
                _headerAttr.Truncate = iniFile1.GetValue<bool>("Truncate", _headerAttr.Truncate, true);
            }

            return _headerAttr;
        }
    }
}
