using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoManifoldRecordConfiguration : ChoFileRecordConfiguration
    {
        public Func<string, Type> RecordSelector { get; set; }
        public bool IgnoreIfNoRecordParserExists { get; set; }
        public ChoManifoldFileHeaderConfiguration FileHeaderConfiguration
        {
            get;
            set;
        }

        private readonly Dictionary<Type, ChoFileRecordConfiguration> SubRecordConfigurations = new Dictionary<Type, ChoFileRecordConfiguration>();
        public ChoFileRecordConfiguration this[Type recordType]
        {
            get
            {
                ChoGuard.ArgumentNotNull(recordType, "RecordType");

                if (SubRecordConfigurations.ContainsKey(recordType))
                    return SubRecordConfigurations[recordType];
                else
                    return null;
            }
            set
            {
                ChoGuard.ArgumentNotNull(recordType, "RecordType");

                if (SubRecordConfigurations.ContainsKey(recordType))
                    SubRecordConfigurations[recordType] = value;
                else
                    SubRecordConfigurations.Add(recordType, value);
            }
        }

        public ChoManifoldRecordConfiguration() : base(typeof(object))
        {
            FileHeaderConfiguration = new ChoManifoldFileHeaderConfiguration(Culture);
        }

        public override void MapRecordFields<T>()
        {
            throw new NotSupportedException();
        }

        public override void MapRecordFields(Type recordType)
        {
            throw new NotSupportedException();
        }

        public override void Validate(object state)
        {
            base.Validate(state);

            if (RecordSelector == null)
                throw new ChoRecordConfigurationException("Missing record selector.");

            //if (SubRecordConfigurations.Count == 0)
            //    throw new ChoRecordConfigurationException("Atleast one record type must be registered.");

            foreach (Type t in SubRecordConfigurations.Keys)
            {
                if (SubRecordConfigurations[t] == null)
                    throw new ChoRecordConfigurationException($"Missing record configuration for '{t.Name}' type.");
            }

        }
    }
}
