namespace ChoETL
{
    #region NameSpaces

    using System;
    using System.ComponentModel;
    using System.Xml.Serialization;

    #endregion NameSpaces

    [Serializable]
    //[ChoTypeConverter(typeof(ChoCDATAToStringConverter))]
    [ChoDataTableColumnType(typeof(string))]
    public class ChoCDATA : IXmlSerializable, IFormattable, IConvertible
    {
        #region Instance Data Members (Private)

        private string _text;

        #endregion Instance Data Members (Private)

        #region Constructors

        public ChoCDATA()
        {
        }

        public ChoCDATA(string text)
        {
            if (text != null && text.StartsWith("<![CDATA["))
                text = text.Substring(9, text.Length - 9 - 3);
            CheckValue(text);
            this._text = text;
        }

        #endregion

        #region Properties

        public string Value
        {
            get { return _text; }
            set 
            {
                if (_text == value) return;
                _text = value;
            }
        }

        #endregion

        #region IXmlSerializable Members

        void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteCData(this._text);
        }

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(System.Xml.XmlReader reader)
        {
            _text = reader.ReadElementContentAsString();
        }

        #endregion

        public override string ToString()
        {
            return String.Format("<![CDATA[{0}]]>", _text);
        }

        public override int GetHashCode()
        {
            return _text == null ? 0 : _text.GetHashCode();
        }

        #region IFormattable Members

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }

        #endregion

        #region IEquatable<ChoCDATA> Members

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(obj, null))
                return false;
            if (!(obj is ChoCDATA))
                return false;

            return ((ChoCDATA)obj).Value == Value;
        }

        #endregion

        public void CheckValue(string text)
        {
            if (text == null) return;

            if (text.Contains("<![CDATA["))
                throw new ApplicationException("Nested CDATA value not allowed.");
        }

        public static implicit operator string(ChoCDATA d)
        {
            return d.Value;
        }

        public static implicit operator ChoCDATA(string normalString)
        {
            return new ChoCDATA(normalString);
        }

        public TypeCode GetTypeCode()
        {
            throw new NotImplementedException();
        }

        public bool ToBoolean(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public byte ToByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public char ToChar(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public decimal ToDecimal(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public double ToDouble(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public short ToInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public int ToInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public long ToInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public float ToSingle(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public string ToString(IFormatProvider provider)
        {
            return Value;
        }

        public object ToType(Type conversionType, IFormatProvider provider)
        {
            if (conversionType == typeof(string))
                return Value;

            throw new NotImplementedException();
        }

        public ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public uint ToUInt32(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }

        public ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotImplementedException();
        }
    }
}
