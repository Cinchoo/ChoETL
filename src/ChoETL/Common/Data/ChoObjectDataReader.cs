using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public abstract class ChoObjectDataReader : IDataReader
    {
        const string shemaTableSchema = @"<?xml version=""1.0"" standalone=""yes""?>
<xs:schema id=""NewDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
  <xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:MainDataTable=""SchemaTable"" msdata:Locale="""">
    <xs:complexType>
      <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
        <xs:element name=""SchemaTable"" msdata:Locale="""" msdata:MinimumCapacity=""1"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""ColumnName"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""ColumnOrdinal"" msdata:ReadOnly=""true"" type=""xs:int"" default=""0"" minOccurs=""0"" />
              <xs:element name=""ColumnSize"" msdata:ReadOnly=""true"" type=""xs:int"" minOccurs=""0"" />
              <xs:element name=""NumericPrecision"" msdata:ReadOnly=""true"" type=""xs:short"" minOccurs=""0"" />
              <xs:element name=""NumericScale"" msdata:ReadOnly=""true"" type=""xs:short"" minOccurs=""0"" />
              <xs:element name=""IsUnique"" msdata:ReadOnly=""true"" type=""xs:boolean"" minOccurs=""0"" />
              <xs:element name=""IsKey"" msdata:ReadOnly=""true"" type=""xs:boolean"" minOccurs=""0"" />
              <xs:element name=""BaseServerName"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""BaseCatalogName"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""BaseColumnName"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""BaseSchemaName"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""BaseTableName"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""DataType"" msdata:DataType=""System.Type, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""AllowDBNull"" msdata:ReadOnly=""true"" type=""xs:boolean"" minOccurs=""0"" />
              <xs:element name=""ProviderType"" msdata:ReadOnly=""true"" type=""xs:int"" minOccurs=""0"" />
              <xs:element name=""IsAliased"" msdata:ReadOnly=""true"" type=""xs:boolean"" minOccurs=""0"" />
              <xs:element name=""IsExpression"" msdata:ReadOnly=""true"" type=""xs:boolean"" minOccurs=""0"" />
              <xs:element name=""IsIdentity"" msdata:ReadOnly=""true"" type=""xs:boolean"" minOccurs=""0"" />
              <xs:element name=""IsAutoIncrement"" msdata:ReadOnly=""true"" type=""xs:boolean"" minOccurs=""0"" />
              <xs:element name=""IsRowVersion"" msdata:ReadOnly=""true"" type=""xs:boolean"" minOccurs=""0"" />
              <xs:element name=""IsHidden"" msdata:ReadOnly=""true"" type=""xs:boolean"" minOccurs=""0"" />
              <xs:element name=""IsLong"" msdata:ReadOnly=""true"" type=""xs:boolean"" default=""false"" minOccurs=""0"" />
              <xs:element name=""IsReadOnly"" msdata:ReadOnly=""true"" type=""xs:boolean"" minOccurs=""0"" />
              <xs:element name=""ProviderSpecificDataType"" msdata:DataType=""System.Type, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""DataTypeName"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""XmlSchemaCollectionDatabase"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""XmlSchemaCollectionOwningSchema"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""XmlSchemaCollectionName"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""UdtAssemblyQualifiedName"" msdata:ReadOnly=""true"" type=""xs:string"" minOccurs=""0"" />
              <xs:element name=""NonVersionedProviderType"" msdata:ReadOnly=""true"" type=""xs:int"" minOccurs=""0"" />
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
  </xs:element>
</xs:schema>";
        protected bool Closed;
        protected List<ChoObjectDataReaderProperty> Fields;
        protected KeyValuePair<string, Type>[] _dynamicMembersInfo;

        protected ChoObjectDataReader()
        {
        }

        protected ChoObjectDataReader(Type elementType, KeyValuePair<string, Type>[] dynamicMembersInfo = null)
        {
            _dynamicMembersInfo = dynamicMembersInfo;

            SetFields(elementType, _dynamicMembersInfo);
            Closed = false;
        }

        #region IDataReader Members

        public abstract object GetValue(int i);
        public abstract bool Read();

        #endregion

        #region Implementation of IDataRecord

        public int FieldCount
        {
            get { return Fields != null ? Fields.Count : 0; }
        }

        public virtual int GetOrdinal(string name)
        {
            if (Fields != null)
            {
                for (int i = 0; i < Fields.Count; i++)
                {
                    if (String.Compare(Fields[i].GetName(), name, true) == 0)
                    {
                        return i;
                    }
                }

                throw new IndexOutOfRangeException("name");
            }
            else
                return 0;
        }

        object IDataRecord.this[int i]
        {
            get { return GetValue(i); }
        }

        public virtual bool GetBoolean(int i)
        {
            return (Boolean)GetValue(i);
        }

        public virtual byte GetByte(int i)
        {
            return (Byte)GetValue(i);
        }

        public virtual char GetChar(int i)
        {
            return (Char)GetValue(i);
        }

        public virtual DateTime GetDateTime(int i)
        {
            return (DateTime)GetValue(i);
        }

        public virtual decimal GetDecimal(int i)
        {
            return (Decimal)GetValue(i);
        }

        public virtual double GetDouble(int i)
        {
            return (Double)GetValue(i);
        }

        public virtual Type GetFieldType(int i)
        {
            return Fields[i].GetPropertyType();
        }

        public virtual float GetFloat(int i)
        {
            return (float)GetValue(i);
        }

        public virtual Guid GetGuid(int i)
        {
            return (Guid)GetValue(i);
        }

        public virtual short GetInt16(int i)
        {
            return (Int16)GetValue(i);
        }

        public virtual int GetInt32(int i)
        {
            return (Int32)GetValue(i);
        }

        public virtual long GetInt64(int i)
        {
            return (Int64)GetValue(i);
        }

        public virtual string GetString(int i)
        {
            return (string)GetValue(i);
        }

        public virtual bool IsDBNull(int i)
        {
            return GetValue(i) == null;
        }

        object IDataRecord.this[string name]
        {
            get { return GetValue(GetOrdinal(name)); }
        }


        public virtual string GetDataTypeName(int i)
        {
            return GetFieldType(i).Name;
        }


        public virtual string GetName(int i)
        {
            if (i < 0 || i >= Fields.Count)
            {
                throw new IndexOutOfRangeException("name");
            }
            return Fields[i].GetName();
        }

        public virtual int GetValues(object[] values)
        {
            int i = 0;
            for (; i < Fields.Count; i++)
            {
                if (values.Length <= i)
                {
                    return i;
                }
                values[i] = GetValue(i);
            }
            return i;
        }

        public virtual IDataReader GetData(int i)
        {
            // need to think about this one
            throw new NotImplementedException();
        }

        public virtual long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            // need to keep track of the bytes got for each record - more work than i want to do right now
            // http://msdn.microsoft.com/en-us/library/system.data.idatarecord.getbytes.aspx
            throw new NotImplementedException();
        }

        public virtual long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            // need to keep track of the bytes got for each record - more work than i want to do right now
            // http://msdn.microsoft.com/en-us/library/system.data.idatarecord.getchars.aspx
            throw new NotImplementedException();
        }

        #endregion

        #region Implementation of IDataReader

        public virtual void Close()
        {
            Closed = true;
        }


        public virtual DataTable GetSchemaTable()
        {
            DataSet s = new DataSet();
            s.Locale = System.Globalization.CultureInfo.CurrentCulture;
            s.ReadXmlSchema(new System.IO.StringReader(shemaTableSchema));
            DataTable t = s.Tables[0];

            if (Fields != null)
            {
                for (int i = 0; i < Fields.Count; i++)
                {
                    DataRow row = t.NewRow();
                    row["ColumnName"] = Fields[i].GetName();
                    row["ColumnOrdinal"] = i;

                    Type type = this.GetFieldType(i);
                    if (type.IsGenericType
                      && type.GetGenericTypeDefinition() == typeof(System.Nullable<int>).GetGenericTypeDefinition())
                    {
                        type = type.GetGenericArguments()[0];
                    }
                    row["DataType"] = Fields[i].GetPropertyType();
                    //row["DataTypeName"] = this.GetDataTypeName(i);
                    row["ColumnSize"] = -1;
                    row["AllowDBNull"] = Fields[i].AllowDBNull();
                    t.Rows.Add(row);
                }
            }

            return t;
            //var dt = new DataTable();
            //foreach (ChoObjectDataReaderProperty field in Fields)
            //{
            //    dt.Columns.Add(new DataColumn(field.GetName(), field.GetPropertyType()));
            //}
            //return dt;
        }

        public virtual bool NextResult()
        {
            return false;
        }


        public virtual int Depth
        {
            get { return 1; }
        }

        public virtual bool IsClosed
        {
            get { return Closed; }
        }

        public virtual int RecordsAffected
        {
            get
            {
                // assuming select only?
                return -1;
            }
        }

        #endregion

        #region Implementation of IDisposable

        public virtual void Dispose()
        {
            Fields = null;
        }

        #endregion

        protected void SetFields(Type elementType, KeyValuePair<string, Type>[] membersInfo = null)
        {
            Dictionary<string, ChoObjectDataReaderProperty> prop = new Dictionary<string, ChoObjectDataReaderProperty>();
            Fields = new List<ChoObjectDataReaderProperty>();

            if (membersInfo != null)
            {
                foreach (KeyValuePair<string, Type> kvp in membersInfo)
                {
                    if (!prop.ContainsKey(kvp.Key))
                        prop.Add(kvp.Key, new ChoObjectDataReaderProperty(kvp.Key, kvp.Value));
                }
            }
            else
            {
                foreach (MemberInfo info in ChoType.GetMembers(elementType))
                {
                    if (!prop.ContainsKey(info.Name))
                        prop.Add(info.Name, new ChoObjectDataReaderProperty(info));
                }
            }

            Fields = new List<ChoObjectDataReaderProperty>(prop.Values.ToArray());
        }

        protected class ChoObjectDataReaderProperty
        {
            public readonly MemberInfo MemberInfo;
            public readonly Type ProperyType;
            public readonly string MemberName;
            public readonly bool IsNullable;

            public ChoObjectDataReaderProperty(MemberInfo info)
            {
                MemberInfo = info;
            }

            public ChoObjectDataReaderProperty(string memberName, Type memberType)
            {
                ChoGuard.ArgumentNotNullOrEmpty(memberName, "MemberName");
                //ChoGuard.ArgumentNotNullOrEmpty(memberType, "MemberType");

                MemberName = memberName;
                ProperyType = memberType == null ? typeof(string) : memberType.GetUnderlyingType();
                IsNullable = true; // memberType == null ? true : memberType.IsNullableType() || memberType == typeof(string) || !memberType.IsValueType;
                ChoDataTableColumnTypeAttribute dtColumnType = ChoType.GetAttribute<ChoDataTableColumnTypeAttribute>(ProperyType);
                if (dtColumnType != null && dtColumnType.Type != null)
                    ProperyType = dtColumnType.Type;
            }

            public Type GetPropertyType()
            {
                if (MemberInfo != null)
                    return ChoType.GetMemberType(MemberInfo).GetUnderlyingType();
                else
                    return ProperyType;
            }

            public bool AllowDBNull()
            {
                if (MemberInfo != null)
                {
                    var t = ChoType.GetMemberType(MemberInfo);
                    return t.IsNullableType() || t == typeof(string);
                }
                else
                    return IsNullable;
            }

            public object GetValue(object target)
            {
                if (MemberInfo != null)
                    return ChoType.GetMemberValue(target, MemberInfo);
                else if (target is IDictionary<string, object>) ///ExpandoObject || target is ChoDynamicObject)
                {
                    IDictionary<string, object> dict = target as IDictionary<string, object>;

                    if (dict.ContainsKey(MemberName))
                        return ((IDictionary<string, object>)target)[MemberName];
                    else
                    {
                        if (target is ChoDynamicObject && ((ChoDynamicObject)target).DynamicObjectName == MemberName)
                        {
                            return ((ChoDynamicObject)target).GetText();
                        }
                        return null;
                        //throw new ApplicationException("Can't find '{0}' member in dynamic object.".FormatString(MemberName));
                    }
                }
                else
                    return ChoType.GetMemberValue(target, MemberName); // ChoConvert.ConvertTo(ChoType.GetMemberValue(target, MemberName), ProperyType);
            }

            public string GetName()
            {
                if (MemberInfo != null)
                    return MemberInfo.Name;
                else
                    return MemberName;
            }
        }

    }
}