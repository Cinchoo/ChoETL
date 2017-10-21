using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoFixedLengthRecordConfiguration : ChoFileRecordConfiguration
    {
        [DataMember]
        public ChoFixedLengthFileHeaderConfiguration FileHeaderConfiguration
        {
            get;
            set;
        }
        [DataMember]
        public List<ChoFixedLengthRecordFieldConfiguration> FixedLengthRecordFieldConfigurations
        {
            get;
            private set;
        }
        [DataMember]
        public int RecordLength
        {
            get;
            set;
        }
        public ChoFixedLengthFieldDefaultSizeConfiguation FixedLengthFieldDefaultSizeConfiguation
        {
            get;
            set;
        }

        internal Dictionary<string, ChoFixedLengthRecordFieldConfiguration> RecordFieldConfigurationsDict
        {
            get;
            private set;
        }
        internal Dictionary<string, ChoFixedLengthRecordFieldConfiguration> RecordFieldConfigurationsDict2
        {
            get;
            private set;
        }


        public ChoFixedLengthRecordFieldConfiguration this[string name]
        {
            get
            {
                return FixedLengthRecordFieldConfigurations.Where(i => i.Name == name).FirstOrDefault();
            }
        }

        public ChoFixedLengthRecordConfiguration() : this(null)
        {

        }

        internal ChoFixedLengthRecordConfiguration(Type recordType) : base(recordType)
        {
            FixedLengthRecordFieldConfigurations = new List<ChoFixedLengthRecordFieldConfiguration>();

            if (recordType != null)
            {
                Init(recordType);
            }

            FileHeaderConfiguration = new ChoFixedLengthFileHeaderConfiguration(recordType, Culture);
        }

        protected override void Init(Type recordType)
        {
            base.Init(recordType);

            ChoFixedLengthRecordObjectAttribute recObjAttr = ChoType.GetAttribute<ChoFixedLengthRecordObjectAttribute>(recordType);
            if (recObjAttr != null)
            {
                RecordLength = recObjAttr.RecordLength;
            }

            DiscoverRecordFields(recordType);
        }
        internal bool AreAllFieldTypesNull
        {
            get;
            set;
        }

        internal void UpdateFieldTypesIfAny(IDictionary<string, Type> dict)
        {
            if (dict == null)
                return;

            foreach (var key in dict.Keys)
            {
                if (RecordFieldConfigurationsDict.ContainsKey(key) && dict[key] != null)
                    RecordFieldConfigurationsDict[key].FieldType = dict[key];
            }

            AreAllFieldTypesNull = RecordFieldConfigurationsDict.All(kvp => kvp.Value.FieldType == null);
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
            if (!IsDynamicObject)
            {
                FixedLengthRecordFieldConfigurations.Clear();

                foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(recordType).Where(pd => pd.Attributes.OfType<ChoFixedLengthRecordFieldAttribute>().Any()))
                {
                    //if (!pd.PropertyType.IsSimple())
                    //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));
                    var obj = new ChoFixedLengthRecordFieldConfiguration(pd.Name, pd.Attributes.OfType<ChoFixedLengthRecordFieldAttribute>().First());
                    obj.FieldType = pd.PropertyType;
                    FixedLengthRecordFieldConfigurations.Add(obj);
                }
            }
        }

        public override void Validate(object state)
        {
            base.Validate(state);

            string line = null;
            string[] fieldNames = null;
            if (state is Tuple<long, string>)
                line = ((Tuple<long, string>)state).Item2;
            else
                fieldNames = state as string[];

            if (RecordLength <= 0 && line != null)
                RecordLength = line.Length;

            //Validate Header
            if (FileHeaderConfiguration != null)
            {
                if (FileHeaderConfiguration.FillChar != null)
                {
                    if (FileHeaderConfiguration.FillChar.Value == ChoCharEx.NUL)
                        throw new ChoRecordConfigurationException("Invalid '{0}' FillChar specified.".FormatString(FileHeaderConfiguration.FillChar));
                    if (EOLDelimiter.Contains(FileHeaderConfiguration.FillChar.Value))
                        throw new ChoRecordConfigurationException("FillChar [{0}] can't be one of EOLDelimiter characters [{1}]".FormatString(FileHeaderConfiguration.FillChar.Value, EOLDelimiter));
                    if (Comments != null)
                    {
                        if ((from comm in Comments
                             where comm.Contains(FileHeaderConfiguration.FillChar.Value.ToString())
                             select comm).Any())
                            throw new ChoRecordConfigurationException("One of the Comments contains FillChar. Not allowed.");
                    }
                }
            }

            //string[] headers = state as string[];
            if (AutoDiscoverColumns
                && FixedLengthRecordFieldConfigurations.Count == 0 /*&& headers != null*/)
            {
                if (RecordType != null && !IsDynamicObject
                    && ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoFixedLengthRecordFieldAttribute>().Any()).Any())
                {
                    int startIndex = 0;
                    int size = 0;
                    foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(RecordType).Where(pd => pd.Attributes.OfType<ChoFixedLengthRecordFieldAttribute>().Any()))
                    {
                        //if (!pd.PropertyType.IsSimple())
                        //    throw new ChoRecordConfigurationException("Property '{0}' is not a simple type.".FormatString(pd.Name));

                        if (FixedLengthFieldDefaultSizeConfiguation == null)
                            size = ChoFixedLengthFieldDefaultSizeConfiguation.Instance.GetSize(pd.PropertyType);
                        else
                            size = FixedLengthFieldDefaultSizeConfiguation.GetSize(pd.PropertyType);

                        var obj = new ChoFixedLengthRecordFieldConfiguration(pd.Name, startIndex, size);
                        obj.FieldType = pd.PropertyType;
                        FixedLengthRecordFieldConfigurations.Add(obj);

                        startIndex += size;
                    }

                    //RecordLength = startIndex;
                }
                else if (!line.IsNullOrEmpty())
                {
                    int index = 0;
                    if (IsDynamicObject)
                    {
                        foreach (var item in DiscoverColumns(line))
                        {
                            var obj = new ChoFixedLengthRecordFieldConfiguration(FileHeaderConfiguration.HasHeaderRecord ? item.Item1 : "Column{0}".FormatString(++index), item.Item2, item.Item3);
                            FixedLengthRecordFieldConfigurations.Add(obj);
                        }
                    }
                    else
                    {
                        Tuple<string, int, int>[] tuples = DiscoverColumns(line);
                        foreach (PropertyDescriptor pd in ChoTypeDescriptor.GetProperties(RecordType))
                        {
                            if (index < tuples.Length)
                            {
                                var obj = new ChoFixedLengthRecordFieldConfiguration(FileHeaderConfiguration.HasHeaderRecord ? tuples[index].Item1 : pd.Name, tuples[index].Item2, tuples[index].Item3);
                                FixedLengthRecordFieldConfigurations.Add(obj);
                                index++;
                            }
                            else
                                break;
                        }
                    }
                }
                else if (!fieldNames.IsNullOrEmpty())
                {
                    int startIndex = 0;
                    int fieldLength = ChoFixedLengthFieldDefaultSizeConfiguation.Instance.GetSize(typeof(string));
                    foreach (string fn in fieldNames)
                    {
                        var obj = new ChoFixedLengthRecordFieldConfiguration(fn, startIndex, fieldLength);
                        FixedLengthRecordFieldConfigurations.Add(obj);
                        startIndex += fieldLength;
                    }
                }
            }

            if (FixedLengthRecordFieldConfigurations.Count == 0)
                throw new ChoRecordConfigurationException("No record fields specified.");

            //Derive record length from fields
            if (RecordLength <= 0)
            {
                int maxStartIndex = FixedLengthRecordFieldConfigurations.Max(f => f.StartIndex);
                int maxSize = FixedLengthRecordFieldConfigurations.Where(f => f.StartIndex == maxStartIndex).Max(f1 => f1.Size.Value);
                var fc = FixedLengthRecordFieldConfigurations.Where(f => f.StartIndex == maxStartIndex && f.Size.Value == maxSize).FirstOrDefault();
                if (fc != null)
                {
                    RecordLength = fc.StartIndex + fc.Size.Value;
                }
            }

            if (RecordLength <= 0)
                throw new ChoRecordConfigurationException("RecordLength must be > 0");

            //Check if any field has empty names
            if (FixedLengthRecordFieldConfigurations.Where(i => i.FieldName.IsNullOrWhiteSpace()).Count() > 0)
                throw new ChoRecordConfigurationException("Some fields has empty field name specified.");

            //Check field names for duplicate
            string[] dupFields = FixedLengthRecordFieldConfigurations.GroupBy(i => i.FieldName, FileHeaderConfiguration.StringComparer)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToArray();

            if (dupFields.Length > 0)
                throw new ChoRecordConfigurationException("Duplicate field names [Name: {0}] specified to record fields.".FormatString(String.Join(",", dupFields)));

            //Find duplicate fields with start index
            ChoFixedLengthRecordFieldConfiguration dupRecConfig = FixedLengthRecordFieldConfigurations.GroupBy(i => i.StartIndex).Where(g => g.Count() > 1).Select(g => g.FirstOrDefault()).FirstOrDefault();
            if (dupRecConfig != null)
                throw new ChoRecordConfigurationException("Found duplicate '{0}' record field with same start index.".FormatString(dupRecConfig.FieldName));

            //Check any overlapping fields specified
            foreach (var f in FixedLengthRecordFieldConfigurations)
            {
                if (f.StartIndex + f.Size.Value > RecordLength)
                    throw new ChoRecordConfigurationException("Found '{0}' record field out of bounds of record length.".FormatString(f.FieldName));
            }

            RecordFieldConfigurationsDict = FixedLengthRecordFieldConfigurations.OrderBy(i => i.StartIndex).Where(i => !i.Name.IsNullOrWhiteSpace()).ToDictionary(i => i.Name, FileHeaderConfiguration.StringComparer);
            RecordFieldConfigurationsDict2 = FixedLengthRecordFieldConfigurations.OrderBy(i => i.StartIndex).Where(i => !i.FieldName.IsNullOrWhiteSpace()).ToDictionary(i => i.FieldName, FileHeaderConfiguration.StringComparer);

            //Validate each record field
            foreach (var fieldConfig in FixedLengthRecordFieldConfigurations)
                fieldConfig.Validate(this);

            if (!FileHeaderConfiguration.HasHeaderRecord)
            {
            }
            else
            {
            }

            LoadNCacheMembers(FixedLengthRecordFieldConfigurations);
        }

        private Tuple<string, int, int>[] DiscoverColumns(string line)
        {
            List<Tuple<string, int, int>> words = new List<Tuple<string, int, int>>();
            if (!line.IsNullOrEmpty())
            {
                //const string text = "   Test42  a       yxx ";
                var result = new StringBuilder(line.Length);
                int i = 0;
                while (i < line.Length - 1)
                {
                    result.Append(line[i]);
                    if ((line[i] != '-'
                        && line[i] != '.'
                        && line[i] != '/'
                        && line[i] != '\\') && (Char.IsWhiteSpace(line[i]) && !Char.IsWhiteSpace(line[i + 1])
                        //|| char.IsUpper(line[i + 1])
                        //|| !char.IsDigit(line[i]) && char.IsDigit(line[i + 1])
                        ))
                    {
                        if (Char.IsWhiteSpace(line[i]) && Char.IsWhiteSpace(line[i + 1]))
                        {

                        }
                        else
                        {
                            if (!result.ToString().IsNullOrWhiteSpace())
                            {
                                words.Add(new Tuple<string, int, int>(result.ToString(), i - (result.Length - 1), result.Length));
                                result.Clear();
                            }
                        }
                    }

                    i++;
                }
                result.Append(line[line.Length - 1]);
                string word = result.ToString();
                if (!word.IsNullOrWhiteSpace() || words.Count == 0)
                    words.Add(new Tuple<string, int, int>(result.ToString(), i - (result.Length - 1), result.Length));
                else
                {
                    Tuple<string, int, int> tuple = words[words.Count - 1];
                    words.RemoveAt(words.Count - 1);
                    words.Add(new Tuple<string, int, int>(tuple.Item1 + result.ToString(), tuple.Item2, tuple.Item3 + result.Length));
                }
            }
            //foreach (var item in words)
            //    Console.WriteLine(item.Item1 + " " + item.Item2 + " " + item.Item3);
            return words.ToArray();
        }
    }
}
