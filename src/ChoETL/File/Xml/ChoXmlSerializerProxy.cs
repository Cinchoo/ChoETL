using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ChoETL
{
    public static class ChoXmlSerializerProxy
    {
        internal static readonly ConcurrentDictionary<Type, ChoXmlRecordConfiguration> XmlRecordFieldConfiguration = new ConcurrentDictionary<Type, ChoXmlRecordConfiguration>();

        public static ChoXmlRecordConfiguration AddRecordConfiguration(ChoXmlRecordConfiguration cf)
        {
            Type recordType = cf.RecordTypeInternal;
            if (XmlRecordFieldConfiguration.ContainsKey(recordType))
            {
                XmlRecordFieldConfiguration.TryGetValue(recordType, out cf);
                return cf;
            }
            XmlRecordFieldConfiguration.AddOrUpdate(recordType, cf);
            return cf;
        }

        public static ChoXmlRecordConfiguration GetRecordConfiguration<T>()
        {
            return GetRecordConfiguration(typeof(T));
        }

        public static ChoXmlRecordConfiguration GetRecordConfiguration(Type recordType)
        {
            ChoXmlRecordConfiguration cf = null;
            XmlRecordFieldConfiguration.TryGetValue(recordType, out cf);
            return cf;
        }
    }

    public class ChoXmlSerializerProxy<TInstanceType> : IXmlSerializable, IChoXmlSerializerProxy<TInstanceType>
        where TInstanceType : class
    {
        private TInstanceType _value;
        public TInstanceType Value => _value;

        object IChoXmlSerializerProxy.Value => _value as object;

        public XmlSchema GetSchema()
        {
            return null;
        }

        public virtual void ReadXml(XmlReader reader)
        {
            ChoXmlRecordConfiguration cf = ChoXmlSerializerProxy.GetRecordConfiguration<TInstanceType>();
            if (cf == null)
            {
                cf = new ChoXmlRecordConfiguration<TInstanceType>();
                cf.WithXPath("//");
            }
            var r = Activator.CreateInstance(typeof(ChoXmlReader<>).MakeGenericType(new[] { typeof(TInstanceType) }), new object[] { reader, cf })
                as IChoReader;

            this._value = r.FirstOrDefaultEx<TInstanceType>();
        }
        public virtual void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}
