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
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace ChoETL
{
    [DataContract]
    public class ChoJSONRecordConfiguration : ChoFileRecordConfiguration
    {
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
        public override bool IsDynamicObject
        {
            get
            {
                return base.IsDynamicObject; // && !UseJSONSerialization;
            }

            set
            {
                base.IsDynamicObject = value;
            }
        }
        internal Dictionary<string, ChoJSONRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }

        public ChoJSONRecordFieldConfiguration this[string name]
        {
            get
            {
                return JSONRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoJSONRecordConfiguration() : this(null)
        {

        }

        internal ChoJSONRecordConfiguration(Type recordType) : base(recordType)
        {
            JSONRecordFieldConfigurations = new List<ChoJSONRecordFieldConfiguration>();

            Formatting = Newtonsoft.Json.Formatting.Indented;
            if (recordType != null)
            {
                Init(recordType);
            }
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoJSONRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoJSONRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
            }

            DiscoverRecordFields(recordType);
        }

        internal void UpdateFieldTypesIfAny(Dictionary<string, Type> dict)
        {
            if (dict == null)
                return;

            foreach (var key in dict.Keys)
            {
                if (RecordFieldConfigurationsDict.ContainsKey(key) && dict[key] != null)
                    RecordFieldConfigurationsDict[key].FieldType = dict[key];
            }
        }

        public override void MapRecordFields<T>()
        {
            DiscoverRecordFields(typeof(T));
        }

        public override void MapRecordFields(Type recordType)
        {
            DiscoverRecordFields(recordType);
        }

        private void DiscoverRecordFields(Type recordType)
        {
            if (!IsDynamicObject) // recordType != typeof(ExpandoObject))
            {
                JSONRecordFieldConfigurations.Clear();

                if (ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()).Any())
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()))
                    {
                        var obj = new ChoJSONRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().First());
                        obj.FieldType = pd.PropertyType;
                        JSONRecordFieldConfigurations.Add(obj);
                    }
                }
                else
                {
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType))
                    {
                        var obj = new ChoJSONRecordFieldConfiguration(pd.Name, (string)null);
                        obj.FieldType = pd.PropertyType;
                        JSONRecordFieldConfigurations.Add(obj);
                    }
                }
            }
        }

        public override void Validate(object state)
        {
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
                    long startIndex = 0;
                    long size = 0;
                    string jpath = null;
                    ChoJSONRecordFieldAttribute attr = null;
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoJSONRecordFieldAttribute>().Any()))
                    {
                        attr = ChoTypeDescriptor.GetPropetyAttribute<ChoJSONRecordFieldAttribute>(pd);

                        var obj = new ChoJSONRecordFieldConfiguration(pd.Name, jpath);
                        obj.FieldType = pd.PropertyType;
                        JSONRecordFieldConfigurations.Add(obj);

                        startIndex += size;
                    }
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

            RecordFieldConfigurationsDict = JSONRecordFieldConfigurations.Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name);

            LoadNCacheMembers(JSONRecordFieldConfigurations);
        }
    }
}
