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
    public class ChoManifoldRecordConfiguration : ChoFileRecordConfiguration
    {
        public readonly dynamic Context = new ChoDynamicObject();

        [DataMember]
        public bool IgnoreIfNoRecordParserExists
        {
            get;
            set;
        }
        [DataMember]
        public ChoManifoldFileHeaderConfiguration FileHeaderConfiguration
        {
            get;
            set;
        }
        internal IChoNotifyFileRead NotifyFileReadObject;
        internal IChoNotifyRecordRead NotifyRecordReadObject;
        internal IChoNotifyRecordFieldRead NotifyRecordFieldReadObject;
        public Type NotifyRecordReadType
        {
            get;
            set;
        }
        internal IChoNotifyFileWrite NotifyFileWriteObject;
        internal IChoNotifyRecordWrite NotifyRecordWriteObject;
        internal IChoNotifyRecordFieldWrite NotifyRecordFieldWriteObject;
        public Type NotifyRecordWriteType
        {
            get;
            set;
        }
        [DataMember]
        public ChoManifoldRecordTypeConfiguration RecordTypeConfiguration
        {
            get;
            set;
        }

        public override IEnumerable<ChoRecordFieldConfiguration> RecordFieldConfigurations
        {
            get
            {
                yield break;
            }
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
            RecordTypeConfiguration = new ChoManifoldRecordTypeConfiguration();

            RecordSelector = new Func<object, Type>((value) =>
            {
                string line = value as string;
                if (line.IsNullOrEmpty()) return RecordTypeConfiguration.DefaultRecordType;

                if (RecordTypeCodeExtractor != null)
                {
                    string rt = RecordTypeCodeExtractor(line);
                    if (RecordTypeConfiguration.Contains(rt))
                        return RecordTypeConfiguration[rt];
                }
                else
                {
                    if (RecordTypeConfiguration.StartIndex >= 0 && RecordTypeConfiguration.Size == 0)
                        return RecordTypeConfiguration.DefaultRecordType;
                    if (RecordTypeConfiguration.StartIndex + RecordTypeConfiguration.Size > line.Length)
                        return RecordTypeConfiguration.DefaultRecordType;

                    string rtc = line.Substring(RecordTypeConfiguration.StartIndex, RecordTypeConfiguration.Size);
                    if (RecordTypeConfiguration.Contains(rtc))
                        return RecordTypeConfiguration[rtc];
                }

                return RecordTypeConfiguration.DefaultRecordType;
            });
        }

        public ChoManifoldRecordConfiguration MapRecordFields<T>()
        {
            throw new NotSupportedException();
        }

        public ChoManifoldRecordConfiguration MapRecordFields(params Type[] recordTypes)
        {
            throw new NotSupportedException();
        }

        public override void Validate(object state)
        {
            base.Validate(state);

            if (NotifyRecordReadType != null)
            {
                NotifyFileReadObject = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyFileRead>(NotifyRecordReadType);
                NotifyRecordReadObject = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordRead>(NotifyRecordReadType);
                NotifyRecordFieldReadObject = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordFieldRead>(NotifyRecordReadType);
            }
            if (NotifyRecordWriteType != null)
                NotifyRecordWriteObject = ChoMetadataObjectCache.CreateMetadataObject<IChoNotifyRecordWrite>(NotifyRecordWriteType);

            //if (SubRecordConfigurations.Count == 0)
            //    throw new ChoRecordConfigurationException("Atleast one record type must be registered.");

            foreach (Type t in SubRecordConfigurations.Keys)
            {
                if (SubRecordConfigurations[t] == null)
                    throw new ChoRecordConfigurationException($"Missing record configuration for '{t.Name}' type.");
            }

            if (RecordTypeConfiguration != null)
            {
                if (RecordSelector == null && RecordTypeCodeExtractor == null)
                {
                    if (RecordTypeConfiguration.StartIndex < 0)
                        throw new ChoRecordConfigurationException("RecordTypeConfiguration start index must be >= 0.");
                    else
                    {
                        if (RecordTypeConfiguration.Size <= 0)
                            throw new ChoRecordConfigurationException("RecordTypeConfiguration size must be > 0.");
                    }
                }
            }
        }

        public ChoManifoldRecordConfiguration Configure(Action<ChoManifoldRecordConfiguration> action)
        {
            if (action != null)
                action(this);

            return this;
        }
    }
}
