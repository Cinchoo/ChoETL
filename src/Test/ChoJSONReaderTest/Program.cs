using ChoETL;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using UnitTestHelper;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Windows.Data;
using System.Net;
using System.IO.MemoryMappedFiles;
using Newtonsoft.Json.Serialization;
using System.Linq.Expressions;
using System.Xml.Schema;
using KellermanSoftware.CompareNetObjects;
using static ChoJSONReaderTest.Program;
using System.Web;

namespace ChoJSONReaderTest
{
    public class Filter
    {
        public string filterName { get; set; }
        public string filterformattedValue { get; set; }
        public string filterValue { get; set; }
        public string view { get; set; }

        public override bool Equals(object obj)
        {
            var filter = obj as Filter;
            return filter != null &&
                   filterName == filter.filterName &&
                   filterformattedValue == filter.filterformattedValue &&
                   filterValue == filter.filterValue &&
                   view == filter.view;
        }

        public override int GetHashCode()
        {
            var hashCode = 2100303978;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(filterName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(filterformattedValue);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(filterValue);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(view);
            return hashCode;
        }
    }

    public class Book
    {
        public string Category { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public double Price { get; set; }

        public override bool Equals(object obj)
        {
            var book = obj as Book;
            return book != null &&
                   Category == book.Category &&
                   Title == book.Title &&
                   Author == book.Author &&
                   Price == book.Price;
        }

        public override int GetHashCode()
        {
            var hashCode = 1343742386;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Category);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Title);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Author);
            hashCode = hashCode * -1521134295 + Price.GetHashCode();
            return hashCode;
        }
    }

    public class DataMapper : IChoKeyValueType
    {
        public DataMapper()
        {
            SubDataMappers = new List<DataMapper>();
        }

        [IgnoreDataMember]
        public object Key
        {
            get
            {
                return Name;
            }
            set
            {
                Name = value.ToNString();
            }
        }

        [ChoIgnoreMember]
        [IgnoreDataMember]
        public object Value
        {
            get
            {
                if (SubDataMappers.IsNullOrEmpty())
                    return (object)DataMapperProperty;
                else
                {
                    ExpandoObject obj = new ExpandoObject();
                    foreach (var item in SubDataMappers)
                        obj.AddOrUpdate(item.Name, item.Value);
                    return obj;
                }
            }
            set
            {
                if (value is IDictionary<string, object>)
                {
                    IDictionary<string, object> dict = value as IDictionary<string, object>;
                    if (!dict.Where(kvp => kvp.Key == "data-type").Any())
                    {
                        List<DataMapper> dm = new List<DataMapper>();
                        foreach (var kvp in (IDictionary<string, object>)value)
                        {
                            dm.Add(new DataMapper { Key = kvp.Key, Value = kvp.Value });
                        }
                        SubDataMappers = dm;
                    }
                    else
                    {
                        DataMapperProperty = ((IDictionary<string, object>)value).ToJSONObject<DataMapperProperty>();
                        //DataMapperProperty dm = new DataMapperProperty();
                        //foreach (var kvp in (IDictionary<string, object>)value)
                        //{
                        //    if (kvp.Key == "data-type")
                        //        dm.DataType = kvp.Value.ToNString();
                        //    else if (kvp.Key == "source")
                        //        dm.Source = kvp.Value.ToNString();
                        //    else if (kvp.Key == "source-column")
                        //        dm.SourceColumn = kvp.Value.ToNString();
                        //    else if (kvp.Key == "source-table")
                        //        dm.SourceTable = kvp.Value.ToNString();
                        //    else if (kvp.Key == "default")
                        //        dm.Default = kvp.Value.ToNString();
                        //    else if (kvp.Key == "value")
                        //        dm.Value = kvp.Value.ToNString();
                        //}

                        //DataMapperProperty = dm;
                    }
                }
            }
        }

        public string Name { get; set; }

        public DataMapperProperty DataMapperProperty { get; set; }

        public List<DataMapper> SubDataMappers { get; set; }

        public override bool Equals(object obj)
        {
            var mapper = obj as DataMapper;
            return mapper != null &&
                   Name == mapper.Name &&
                   EqualityComparer<DataMapperProperty>.Default.Equals(DataMapperProperty, mapper.DataMapperProperty) &&
                   new ListEqualityComparer<DataMapper>().Equals(SubDataMappers, mapper.SubDataMappers);
        }

        public override int GetHashCode()
        {
            var hashCode = 874566192;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<DataMapperProperty>.Default.GetHashCode(DataMapperProperty);
            hashCode = hashCode * -1521134295 + new ListEqualityComparer<DataMapper>().GetHashCode(SubDataMappers);
            return hashCode;
        }
    }

    public class DataMapperProperty
    {
        [JsonProperty(PropertyName = "data-type", NullValueHandling = NullValueHandling.Ignore)]
        public string DataType { get; set; }

        [JsonProperty(PropertyName = "source", NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; set; }

        [JsonProperty(PropertyName = "source-column", NullValueHandling = NullValueHandling.Ignore)]
        public string SourceColumn { get; set; }

        [JsonProperty(PropertyName = "source-table", NullValueHandling = NullValueHandling.Ignore)]
        public string SourceTable { get; set; }

        [JsonProperty(PropertyName = "default", NullValueHandling = NullValueHandling.Ignore)]
        public string Default { get; set; }

        [JsonProperty(PropertyName = "value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }

        public override bool Equals(object obj)
        {
            var property = obj as DataMapperProperty;
            return property != null &&
                   DataType == property.DataType &&
                   Source == property.Source &&
                   SourceColumn == property.SourceColumn &&
                   SourceTable == property.SourceTable &&
                   Default == property.Default &&
                   Value == property.Value;
        }

        public override int GetHashCode()
        {
            var hashCode = 43592883;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DataType);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Source);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceColumn);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(SourceTable);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Default);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
            return hashCode;
        }

        //public static explicit operator DataMapperProperty(JToken v)
        //{
        //    return new DataMapperProperty()
        //    {
        //        DataType = v.Value<string>("data-type"),
        //        Source = v.Value<string>("source"),
        //        SourceColumn = v.Value<string>("source-column"),
        //        SourceTable = v.Value<string>("source-table"),
        //        Default = v.Value<string>("default"),
        //        Value = v.Value<string>("value")
        //    };
        //}
    }
    public enum ChoHL7Version
    {
        v2_1,
        v2_2,
        v2_3
    }

    public class MenuItem
    {
        public string Value { get; set; }
        public string OnClick { get; set; }
    }

    [Serializable]
    public class MyObjectType
    {
        [ChoJSONRecordField(JSONPath = "$.id")]
        public string Id { get; set; }
        [ChoJSONRecordField(JSONPath = "$.value")]
        [ChoDefaultValue("FileMenu")]
        public string Value1 { get; set; }

        [XmlElement]
        [ChoJSONRecordField(JSONPath = "$.popup.menuitem")]
        public MenuItem[] MenuItems { get; set; }
    }

    public class Message
    {
        public string Base
        {
            get;
            set;
        }
        public Dictionary<string, string> Rates
        {
            get;
            set;
        }
    }
    public class Product
    {
        public string Name { get; set; }
        public double Price { get; set; }
    }

    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    public class Program
    {
        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();

            ChoActivator.Factory = (t, args) =>
            {
                if (t == typeof(Person3))
                    return new Employee3();
                else if (t == typeof(IDictionary))
                    return new Dictionary<string, object>();
                else if (t == typeof(GeographyPoint3))
                    return new GeographyPoint(0, 0);
                else if (t == typeof(StaticCar))
                    return StaticCar.Instance;
                else if (t == typeof(D3Point))
                    return new D3Point(0, 0, 0);
                else
                    return null;
            };

            //ChoActivator.Factory = (t, args) =>
            //{
            //    if (t == typeof(GeographyPoint))
            //        return new GeographyPoint(0, 0);
            //    else
            //        return null;
            //};
            //ChoActivator.Factory = (t, args) =>
            //{
            //    if (t == typeof(GeographyPoint))
            //        return new GeographyPoint(0, 0);
            //    else
            //        return null;
            //};
        }

        public class FamilyMember
        {
            public int Age { get; set; }
            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                var member = obj as FamilyMember;
                return member != null &&
                       Age == member.Age &&
                       Name == member.Name;
            }

            public override int GetHashCode()
            {
                var hashCode = 356282786;
                hashCode = hashCode * -1521134295 + Age.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                return hashCode;
            }
        }

        public class Family
        {
            public int Id { get; set; }
            public List<FamilyMember> Daughters { get; set; }

            public override bool Equals(object obj)
            {
                var family = obj as Family;
                return family != null &&
                       Id == family.Id &&
                    new ListEqualityComparer<FamilyMember>().Equals(Daughters, family.Daughters);
            }

            public override int GetHashCode()
            {
                var hashCode = 635865446;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                //hashCode = hashCode * -1521134295 + new ArrayListEqualityComparer().GetHashCode(Daughters);
                return hashCode;
            }
        }

        public class Customer
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            // need to flatten these lists
            public List<CreditCard> CreditCards { get; set; }
            public List<Address> Addresses { get; set; }
        }

        public class CreditCard
        {
            public string Name { get; set; }
        }

        public class Address
        {
            public string Street { get; set; }
        }

        [Test]
        public static void ConvertNestedJson2CSV()
        {
            string json = @"{
""GpsLocation"": {
        ""Equipment"": [
            {
                ""EquipmentId"": ""EQ00001"",
                ""InquiryValue"": [
                    ""IV00001""
                ],
                ""Timestamp"": ""2020-02-01 01:01:01.01"",
            },
            {
                ""EquipmentId"": ""EQ00002"",
                ""InquiryValue"": [
                    ""IV00002""
                ],
                ""Timestamp"": ""2020-01-01 01:01:01.01""
            }
        ]
    }
}";
            string expected = @"EquipmentId,InquiryValue,Timestamp
EQ00001,IV00001,2/1/2020 1:01:01 AM
EQ00002,IV00002,1/1/2020 1:01:01 AM";

            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";

            StringBuilder csv = new StringBuilder();

            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$.GpsLocation.Equipment")
                .WithField("EquipmentId")
                .WithField("InquiryValue", jsonPath: "InquiryValue[0]", fieldType: typeof(string))
                .WithField("Timestamp", fieldType: typeof(DateTime))
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader())

                    w.Write(r);
            }
            var actual = csv.ToString();
            Assert.AreEqual(actual, expected);
        }

        [Test]
        public static void Test()
        {
            Assert.Ignore("Where is the testcase for ChoJSONReader ?");

            List<Customer> allCustomers = GetAllCustomers();
            var result = allCustomers
   .Select(customer => new[]
   {
      customer.FirstName,
      customer.LastName
   }
   .Concat(customer.CreditCards.Select(cc => cc.Name))
   .Concat(customer.Addresses.Select(address => address.Street)));

            foreach (var c in result)
                Console.WriteLine(ChoUtility.ToStringEx(c.ToList().ToExpandoObject()));
            return;
            //  Customer has CreditCards list and Addresses list

            // how to flatten Customer, CreditCards list, and Addresses list into one flattened record/list?

            var flatenned = from c in allCustomers
                            select
                                c.FirstName + ", " +
                                c.LastName + ", " +
                                String.Join(", ", c.CreditCards.Select(c2 => c2.Name).ToArray()) + ", " +
                                String.Join(", ", c.Addresses.Select(a => a.Street).ToArray());

            flatenned.ToList().ForEach(Console.WriteLine);
        }

        private static List<Customer> GetAllCustomers()
        {
            return new List<Customer>
                   {
                       new Customer
                           {
                               FirstName = "Joe",
                               LastName = "Blow",
                               CreditCards = new List<CreditCard>
                                                 {
                                                     new CreditCard
                                                         {
                                                             Name = "Visa"
                                                         },
                                                     new CreditCard
                                                         {
                                                             Name = "Master Card"
                                                         }
                                                 },
                               Addresses = new List<Address>
                                               {
                                                   new Address
                                                       {
                                                           Street = "38 Oak Street"
                                                       },
                                                   new Address
                                                       {
                                                           Street = "432 Cross Avenue"
                                                       }
                                               }
                           },
                       new Customer
                           {
                               FirstName = "Sally",
                               LastName = "Cupcake",
                               CreditCards = new List<CreditCard>
                                                 {
                                                     new CreditCard
                                                         {
                                                             Name = "Discover"
                                                         },
                                                     new CreditCard
                                                         {
                                                             Name = "Master Card"
                                                         }
                                                 },
                               Addresses = new List<Address>
                                               {
                                                   new Address
                                                       {
                                                           Street = "29 Maple Grove"
                                                       },
                                                   new Address
                                                       {
                                                           Street = "887 Nut Street"
                                                       }
                                               }
                           }
                   };
        }
        private static string EmpJSON => @"    
        [
          {
            ""Id"": 1,
            ""Name"": ""Raj"",
            ""Courses"": [ ""Math"", ""Tamil""],
            ""Dict"": {""key1"":""value1"",""key2"":""value2""}
          },
          {
            ""Id"": 2,
            ""Name"": ""Tom"",
          }
        ]
        ";

        private static string Stores => @"{
  'Stores': [
    'Lambton Quay',
    'Willis Street'
  ],
  'Manufacturers': [
    {
      'Name': 'Acme Co',
      'Products': [
        {
          'Name': 'Anvil',
          'Price': 50
        }
      ]
    },
    {
      'Name': 'Contoso',
      'Products': [
        {
          'Name': 'Elbow Grease',
          'Price': 99.95
        },
        {
          'Name': 'Headlight Fluid',
          'Price': 4
        }
      ]
    }
  ]}";

        public class IRCUBE
        {
            public string CurveDefinitionId { get; set; }
            public string CurveFamilyId { get; set; }
            public string CurveName { get; set; }
            public string MarketDataSet { get; set; }
            public string Referenced { get; set; }

            public override bool Equals(object obj)
            {
                var iRCUBE = obj as IRCUBE;
                return iRCUBE != null &&
                       CurveDefinitionId == iRCUBE.CurveDefinitionId &&
                       CurveFamilyId == iRCUBE.CurveFamilyId &&
                       CurveName == iRCUBE.CurveName &&
                       MarketDataSet == iRCUBE.MarketDataSet &&
                       Referenced == iRCUBE.Referenced;
            }

            public override int GetHashCode()
            {
                var hashCode = 1056701833;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CurveDefinitionId);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CurveFamilyId);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CurveName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(MarketDataSet);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Referenced);
                return hashCode;
            }

            public override string ToString()
            {
                StringBuilder msg = new StringBuilder();

                msg.AppendLine("CurveDefinitionId: " + CurveDefinitionId);
                msg.AppendLine("CurveFamilyId: " + CurveFamilyId);
                msg.AppendLine("CurveName: " + CurveName);
                msg.AppendLine("MarketDataSet: " + MarketDataSet);
                msg.AppendLine("Referenced: " + Referenced);

                return msg.ToString();
            }
        }

        public class Error
        {
            //[ChoJSONRecordField()]
            public string Status { get; set; }
            //[ChoJSONRecordField()]
            public List<ErrorMessage> ErrorMessages { get; set; }

            public override bool Equals(object obj)
            {
                var error = obj as Error;
                return error != null &&
                       Status == error.Status &&
                       new ListEqualityComparer<ErrorMessage>().Equals(ErrorMessages, error.ErrorMessages);
            }

            public override int GetHashCode()
            {
                var hashCode = 639856040;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Status);
                hashCode = hashCode * -1521134295 + new ListEqualityComparer<ErrorMessage>().GetHashCode(ErrorMessages);
                return hashCode;
            }
        }

        public class ErrorMessage : IChoKeyValueType
        {
            public int ErrorCode { get; set; }
            public string ErrorMsg { get; set; }
            [ChoIgnoreMember]
            public object Key
            {
                get { throw new NotImplementedException(); }
                set { ErrorCode = value.CastTo<int>(); }
            }
            [ChoIgnoreMember]
            public object Value { get { throw new NotImplementedException(); } set { ErrorMsg = (string)value; } }

            public override bool Equals(object obj)
            {
                var message = obj as ErrorMessage;
                return message != null &&
                       ErrorCode == message.ErrorCode &&
                       ErrorMsg == message.ErrorMsg;
            }

            public override int GetHashCode()
            {
                var hashCode = -1737096492;
                hashCode = hashCode * -1521134295 + ErrorCode.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ErrorMsg);
                return hashCode;
            }
        }

        [Test]
        public static void GetKeyTest()
        {
            List<object> expected = new List<object>
            {
                new Error{ Status = "Error", ErrorMessages = new List<ErrorMessage>{new ErrorMessage {  ErrorCode= 1001, ErrorMsg = "Schema validation Error"}, new ErrorMessage {  ErrorCode= 1002, ErrorMsg = "Schema validation Error"} }}
            };
            List<object> actual = new List<object>();

            string json = @"{
               ""status"":""Error"",
               ""errorMessages"":{
                    ""1001"":""Schema validation Error"",
                    ""1002"":""Schema validation Error""
               }
            }";

            using (var p = new ChoJSONReader<Error>(new StringReader(json))
                //.WithField("Status")
                //.WithField("ErrorMessages")
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample18()
        {
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";

            StringBuilder csv = new StringBuilder();
            using (var w = new ChoCSVWriter(csv).WithFirstLineHeader())
            {
                using (var json = new ChoJSONReader(FileNameSample18JSON))
                {
                    //var result = json.Select(a => a.data.sensors).ToArray();
                    w.Write(json.Select(i => new
                    {
                        // Info about device
                        Id = i.id,
                        DeviceUuid = i.uuid,
                        DeviceName = i.name,
                        DeviceDescription = i.description,
                        DeviceState = i.state,
                        UserTags = i.user_tags,
                        LastReading = i.last_reading_at,
                        AddedAt = i.added_at,
                        Updated = i.updated_at,
                        MacAddress = i.mac_address,

                        //Info about owner
                        OwnerID = i.owner.id,
                        OwnerUuid = i.owner.uuid,
                        OwnerUserName = i.owner.username,
                        OwnerAvatar = i.owner.avatar,
                        OwnerUrl = i.owner.url,
                        OwnerJoinDate = i.owner.joined_at,
                        OwnerCity = i.owner.location.city,
                        OwnerCountry = i.owner.location.country,
                        OwnerCountryCode = i.owner.location.country_code,
                        DeviceIds = i.owner.device_ids,

                        //Info about data
                        DataRecorded_At = i.data.recorded_at,
                        DataAdded_At = i.data.added_at,
                        DataLocation = i.data.location.ip,
                        DataExposure = i.data.location.exposure,
                        DataElevation = i.data.location.elevation,
                        DataLatitude = i.data.location.latitude,
                        DataLongitude = i.data.location.longitude,
                        DataGeoLocation = i.data.location.geohash,
                        DataCity = i.data.location.city,
                        DataCountryCode = i.data.location.country_code,
                        DataCountry = i.data.location.country,
                        //SensorBattery

                        SensorsId = i.data.sensors.Count > 0 ? i.data.sensors[0].id : 0,
                        SensortAncestry = i.data.sensors.Count > 0 ? i.data.sensors[0].ancestry : null,
                        SensorName = i.data.sensors.Count > 0 ? i.data.sensors[0].name : null,
                        SensorDescription = i.data.sensors.Count > 0 ? i.data.sensors[0].description : null,
                        SensorUnit = i.data.sensors.Count > 0 ? i.data.sensors[0].unit : 0,
                        SensorCreatedAt = i.data.sensors.Count > 0 ? i.data.sensors[0].created_at : DateTime.MinValue,
                        SensorUpdated_at = i.data.sensors.Count > 0 ? i.data.sensors[0].updated_at : DateTime.MinValue,
                        SensorMeasurement_id = i.data.sensors.Count > 0 ? i.data.sensors[0].measurement_id : 0,
                        SensorUuid = i.data.sensors.Count > 0 ? i.data.sensors[0].uuid : null,
                        SensorValue = i.data.sensors.Count > 0 ? i.data.sensors[0].value : 0,
                        SensorRawValue = i.data.sensors.Count > 0 ? i.data.sensors[0].raw_value : 0,
                        SensorPrevValue = i.data.sensors.Count > 0 ? i.data.sensors[0].prev_value : 0,
                        SensorPrevRawValue = i.data.sensors.Count > 0 ? i.data.sensors[0].prev_raw_value : 0,

                        ////SensorHumidity
                        //SensorsHumidityId = i.data.sensors[1].id,
                        //SensortHumidityAncestry = i.data.sensors[1].ancestry,
                        //SensorHumidityName = i.data.sensors[1].name,
                        //SensorHumidityDescription = i.data.sensors[1].description,
                        //SensorHumidityUnit = i.data.sensors[1].unit,
                        //SensorHumidityCreatedAt = i.data.sensors[1].created_at,
                        //SensorumidityUpdated_at = i.data.sensors[1].updated_at,
                        //SensorHumidityMeasurement_id = i.data.sensors[1].measurement_id,
                        //SensorHumidityUuid = i.data.sensors[1].uuid,
                        //SensorHumidityValue = i.data.sensors[1].value,
                        //SensorHumidityRawValue = i.data.sensors[1].raw_value,
                        //SensorHumidityPrevValue = i.data.sensors[1].prev_value,
                        //SensorHumidityPrevRawValue = i.data.sensors[1].prev_raw_value,

                        ////Temperature
                        //SensorsTemperatureId = i.data.sensors[2].id,
                        //SensortTemperatureAncestry = i.data.sensors[2].ancestry,
                        //SensorTemperatureName = i.data.sensors[2].name,
                        //SensorTemperatureDescription = i.data.sensors[2].description,
                        //SensorTemperatureUnit = i.data.sensors[2].unit,
                        //SensorTemperatureCreatedAt = i.data.sensors[2].created_at,
                        //SensorTemperatureUpdated_at = i.data.sensors[2].updated_at,
                        //SensorTemperatureMeasurement_id = i.data.sensors[2].measurement_id,
                        //SensorTemperatureyUuid = i.data.sensors[2].uuid,
                        //SensorTemperatureValue = i.data.sensors[2].value,
                        //SensorTemperatureRawValue = i.data.sensors[2].raw_value,
                        //SensorTemperaturePrevValue = i.data.sensors[2].prev_value,
                        //SensorTemperaturePrevRawValue = i.data.sensors[2].prev_raw_value,

                        ////No2 gas sensor

                        //SensorsNo2Id = i.data.sensors[3].id,
                        //SensortNo2Ancestry = i.data.sensors[3].ancestry,
                        //SensorNo2Name = i.data.sensors[3].name,
                        //SensorNo2Description = i.data.sensors[3].description,
                        //SensorNo2Unit = i.data.sensors[3].unit,
                        //SensorNo2CreatedAt = i.data.sensors[3].created_at,
                        //SensorMo2Updated_at = i.data.sensors[3].updated_at,
                        //SensorNo2Measurement_id = i.data.sensors[3].measurement_id,
                        //SensorNo2Uuid = i.data.sensors[3].uuid,
                        //SensorNo2Value = i.data.sensors[3].value,
                        //SensorNo2RawValue = i.data.sensors[3].raw_value,
                        //SensorNo2PrevValue = i.data.sensors[3].prev_value,
                        //SensorNo2PrevRawValue = i.data.sensors[3].prev_raw_value,


                        ////CO2 gas sensor 
                        //SensorsCo2Id = i.data.sensors[4].id,
                        //SensortCo2Ancestry = i.data.sensors[4].ancestry,
                        //SensorCo2Name = i.data.sensors[4].name,
                        //SensorCo2Description = i.data.sensors[4].description,
                        //SensorCo2Unit = i.data.sensors[4].unit,
                        //SensorCo2CreatedAt = i.data.sensors[4].created_at,
                        //SensorCo2Updated_at = i.data.sensors[4].updated_at,
                        //SensorCo2Measurement_id = i.data.sensors[4].measurement_id,
                        //SensorCo2Uuid = i.data.sensors[4].uuid,
                        //SensorCo2Value = i.data.sensors[4].value,
                        //SensorCo2RawValue = i.data.sensors[4].raw_value,
                        //SensorCo2PrevValue = i.data.sensors[4].prev_value,
                        //SensorCo2PrevRawValue = i.data.sensors[4].prev_raw_value,


                        ////Network sensor


                        //SensorsNetworkId = i.data.sensors[5].id,
                        //SensortNetworkAncestry = i.data.sensors[5].ancestry,
                        //SensorNetworkName = i.data.sensors[5].name,
                        //SensorNetworkDescription = i.data.sensors[5].description,
                        //SensorNetworkUnit = i.data.sensors[5].unit,
                        //SensorNetworkCreatedAt = i.data.sensors[5].created_at,
                        //SensorNetworkUpdated_at = i.data.sensors[5].updated_at,
                        //SensorNetworkMeasurement_id = i.data.sensors[5].measurement_id,
                        //SensorNetworkUuid = i.data.sensors[5].uuid,
                        //SensorNetworkValue = i.data.sensors[5].value,
                        //SensorNetworkRawValue = i.data.sensors[5].raw_value,
                        //SensorNetworkPrevValue = i.data.sensors[5].prev_value,
                        //SensorNetworkPrevRawValue = i.data.sensors[5]?.prev_raw_value,




                        ////decibel sensor  db

                        //SensorsDBId = i.data.sensors[6].id,
                        //SensorDBAncestry = i.data.sensors[6].ancestry,
                        //SensorDBName = i.data.sensors[6].name,
                        //SensorDBDescription = i.data.sensors[6].description,
                        //SensorDBUnit = i.data.sensors[6].unit,
                        //SensorDBCreatedAt = i.data.sensors[6].created_at,
                        //SensorDBUpdated_at = i.data.sensors[6].updated_at,
                        //SensorDBMeasurement_id = i.data.sensors[6].measurement_id,
                        //SensorDBUuid = i.data.sensors[6].uuid,
                        //SensorDBValue = i.data.sensors[6].value,
                        //SensorDBRawValue = i.data.sensors[6].raw_value,
                        //SensorDBPrevValue = i.data.sensors[6].prev_value,
                        //SensorDBPrevRawValue = i.data.sensors[6].prev_raw_value,

                        //// LDR Analog Light Sensor

                        //LightSensorsId = i.data.sensors[7].id,
                        //LightSensortAncestry = i.data.sensors[7].ancestry,
                        //LightSensorName = i.data.sensors[7].name,
                        //LightSensorDescription = i.data.sensors[7].description,
                        //LightSensorUnit = i.data.sensors[7].unit,
                        //LightSensorCreatedAt = i.data.sensors[7].created_at,
                        //LightSensorUpdated_at = i.data.sensors[7].updated_at,
                        //LightSensorMeasurement_id = i.data.sensors[7].measurement_id,
                        //LightSensorUuid = i.data.sensors[7].uuid,
                        //LightSensorValue = i.data.sensors[7].value,
                        //LightSensorRawValue = i.data.sensors[7].raw_value,
                        //LightSensorPrevValue = i.data.sensors[7].prev_value,
                        //LightSensorPrevRawValue = i.data.sensors[7].prev_raw_value,

                        ////solar panel 
                        //SolarPaneltSensorsId = i.data.sensors[8].id,
                        //SolarPanelSensortAncestry = i.data.sensors[8].ancestry,
                        //SolarPanelName = i.data.sensors[8].name,
                        //SolarPanelSensorDescription = i.data.sensors[8].description,
                        //SolarPanelSensorUnit = i.data.sensors[8].unit,
                        //SolarPanelSensorCreatedAt = i.data.sensors[8].created_at,
                        //SolarPanelSensorUpdated_at = i.data.sensors[8].updated_at,
                        //SolarPanelSensorMeasurement_id = i.data.sensors[8].measurement_id,
                        //SolarPanelSensorUuid = i.data.sensors[8].uuid,
                        //SolarPanelSensorValue = i.data.sensors[8].value,
                        //SolarPanelSensorRawValue = i.data.sensors[8].raw_value,
                        //SolarPanelSensorPrevValue = i.data.sensors[8].prev_value,
                        //SolarPanelSensorPrevRawValue = i.data.sensors[8].prev_raw_value,



                        KitId = i.kit != null ? i.kit.id : null,
                        KitUuid = i.kit != null ? i.kit.uuid : null,
                        KitSlug = i.kit != null ? i.kit.slug : null,
                        KitName = i.kit != null ? i.kit.name : null,
                        KitDescription = i.kit != null ? i.kit.description : null,
                        KitCreatedAt = i.kit != null ? i.kit.created_at : null,
                        KitUpdatedAt = i.kit != null ? i.kit.updated_at : null,

                        x = i.data.ContainsKey("Date") ? i.Date : null

                    }));

                }
            }

            var actual = csv.ToString();
            var expected = File.ReadAllText(FileNameSample18ExpectedCSV);

            Assert.AreEqual(expected, actual);
        }

        public class JSObject
        {
            [JsonProperty("name")]
            public string name { get; set; }
            [JsonProperty("width")]
            public int width { get; set; }
            [JsonProperty("height")]
            public int height { get; set; }

            public override bool Equals(object obj)
            {
                var @object = obj as JSObject;
                return @object != null &&
                       name == @object.name &&
                       width == @object.width &&
                       height == @object.height;
            }

            public override int GetHashCode()
            {
                var hashCode = -1072973697;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
                hashCode = hashCode * -1521134295 + width.GetHashCode();
                hashCode = hashCode * -1521134295 + height.GetHashCode();
                return hashCode;
            }
        }
        [Test]
        public static void ArrayTest()
        {
            List<object> expected = new List<object>
            {
                new JSObject{ name="164.jpg", height = 211, width = 300}
            };
            List<object> actual = new List<object>();

            string json = @"[{""name"":""1.jpg"",""height"":300,""width"":211}, 
{ ""width"":211,""height"":300,""name"":""157.jpg""}, 
{ ""width"":211,""height"":300,""name"":""158.jpg""}, 
{ ""height"":211,""name"":""159.jpg"",""width"":300}, 
{ ""name"":""160.jpg"",""height"":211,""width"":300}, 
{ ""width"":300,""height"":211,""name"":""161.jpg""}, 
{ ""width"":300,""height"":211,""name"":""162.jpg""}, 
{ ""name"":""163.jpg"",""height"":211,""width"":300}, 
{ ""width"":300,""height"":211,""name"":""164.jpg""}, 
{ ""height"":211,""name"":""165.jpg"",""width"":300}, 
{ ""height"":211,""name"":""166.jpg"",""width"":300}
            ";

            using (var p = ChoJSONReader<JSObject>.LoadText(json))
            {
                foreach (var rec in p.Where(r => r.name.Contains("4")))
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void JSONToXmlTest()
        {
            string expected = @"<dynamic>
  <Email>james@example.com</Email>
  <Roles>
    <role>User</role>
    <role>Admin</role>
  </Roles>
</dynamic>";
            string actual = null;

            string json = @" {
  'Email': 'james@example.com',
  'Active': true,
  'CreatedDate': '2013-01-20T00:00:00Z',
  'Roles': [
    'User',
    'Admin'
  ]
 }";
            //ChoDynamicObjectSettings.XmlArrayQualifier = (key, obj) =>
            //{
            //    if (key == "features")
            //        return true;
            //    return null;
            //};

            using (var p = ChoJSONReader.LoadText(json)
                .WithField("Email")
                .WithField("Roles", customSerializer: ((o) =>
                {
                    return ((JArray)o).Select(i =>
                    {
                        var x = new ChoDynamicObject("role"); x.SetText(i.ToString());
                        return x;
                    }).ToArray();
                }))
                .Configure(c => c.SupportMultipleContent = true)
                )
            {
                actual = ChoXmlWriter.ToText(p.First(), new ChoXmlRecordConfiguration()
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.NodeName = "dynamic")
                    .Configure(c => c.DoNotEmitXmlNamespace = true)
                    .Configure(c => c.UseXmlArray = true)
                    .Configure(c => c.TurnOffPluralization = true)
                    );
            }
            Assert.AreEqual(expected, actual);

        }

        public enum CoubType
        {
            [Description("Coub::Simple")]
            Simple = 1,
            [Description("Coub::Temp")]
            Temp = 2,
            [Description("Coub::Recoub")]
            Recoub = 3
        }
        public partial class CoubBig
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("type")]
            [ChoTypeConverter(typeof(ChoEnumDescriptionConverter))]
            public CoubType Type { get; set; }

            [JsonProperty("permalink")]
            public string Permalink { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("channel_id")]
            public int ChannelId { get; set; }

            [JsonProperty("created_at")]
            public DateTimeOffset CreatedAt { get; set; }

            [JsonProperty("updated_at")]
            public DateTimeOffset UpdatedAt { get; set; }

            [JsonProperty("is_done")]
            public bool IsDone { get; set; }

            [JsonProperty("views_count")]
            public int ViewsCount { get; set; }

            [JsonProperty("cotd")]
            public bool? Cotd { get; set; }

            [JsonProperty("cotd_at")]
            public object CotdAt { get; set; }

            [JsonProperty("original_sound")]
            public bool OriginalSound { get; set; }

            [JsonProperty("has_sound")]
            public bool HasSound { get; set; }

            [JsonProperty("recoub_to")]
            public int? RecoubTo { get; set; }

            [JsonProperty("age_restricted")]
            public bool AgeRestricted { get; set; }

            [JsonProperty("allow_reuse")]
            public bool AllowReuse { get; set; }

            [JsonProperty("banned")]
            public bool Banned { get; set; }

            [JsonProperty("percent_done")]
            public long PercentDone { get; set; }

            [JsonProperty("recoubs_count")]
            public long RecoubsCount { get; set; }

            [JsonProperty("likes_count")]
            public long LikesCount { get; set; }

            [JsonProperty("raw_video_id")]
            public long RawVideoId { get; set; }

            [JsonProperty("raw_video_thumbnail_url")]
            public Uri RawVideoThumbnailUrl { get; set; }

            [JsonProperty("raw_video_title")]
            public string RawVideoTitle { get; set; }

            [JsonProperty("video_block_banned")]
            public bool VideoBlockBanned { get; set; }

            [JsonProperty("duration")]
            public float Duration { get; set; }

            public override bool Equals(object obj)
            {
                var big = obj as CoubBig;
                return big != null &&
                       Id == big.Id &&
                       Type == big.Type &&
                       Permalink == big.Permalink &&
                       Title == big.Title &&
                       ChannelId == big.ChannelId &&
                       CreatedAt.Equals(big.CreatedAt) &&
                       UpdatedAt.Equals(big.UpdatedAt) &&
                       IsDone == big.IsDone &&
                       ViewsCount == big.ViewsCount &&
                       Cotd == big.Cotd &&
                       EqualityComparer<object>.Default.Equals(CotdAt, big.CotdAt) &&
                       OriginalSound == big.OriginalSound &&
                       HasSound == big.HasSound &&
                       RecoubTo == big.RecoubTo &&
                       AgeRestricted == big.AgeRestricted &&
                       AllowReuse == big.AllowReuse &&
                       Banned == big.Banned &&
                       PercentDone == big.PercentDone &&
                       RecoubsCount == big.RecoubsCount &&
                       LikesCount == big.LikesCount &&
                       RawVideoId == big.RawVideoId &&
                       EqualityComparer<Uri>.Default.Equals(RawVideoThumbnailUrl, big.RawVideoThumbnailUrl) &&
                       RawVideoTitle == big.RawVideoTitle &&
                       VideoBlockBanned == big.VideoBlockBanned &&
                       Duration == big.Duration;
            }

            public override int GetHashCode()
            {
                var hashCode = 1327889996;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + Type.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Permalink);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Title);
                hashCode = hashCode * -1521134295 + ChannelId.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<DateTimeOffset>.Default.GetHashCode(CreatedAt);
                hashCode = hashCode * -1521134295 + EqualityComparer<DateTimeOffset>.Default.GetHashCode(UpdatedAt);
                hashCode = hashCode * -1521134295 + IsDone.GetHashCode();
                hashCode = hashCode * -1521134295 + ViewsCount.GetHashCode();
                hashCode = hashCode * -1521134295 + Cotd.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(CotdAt);
                hashCode = hashCode * -1521134295 + OriginalSound.GetHashCode();
                hashCode = hashCode * -1521134295 + HasSound.GetHashCode();
                hashCode = hashCode * -1521134295 + RecoubTo.GetHashCode();
                hashCode = hashCode * -1521134295 + AgeRestricted.GetHashCode();
                hashCode = hashCode * -1521134295 + AllowReuse.GetHashCode();
                hashCode = hashCode * -1521134295 + Banned.GetHashCode();
                hashCode = hashCode * -1521134295 + PercentDone.GetHashCode();
                hashCode = hashCode * -1521134295 + RecoubsCount.GetHashCode();
                hashCode = hashCode * -1521134295 + LikesCount.GetHashCode();
                hashCode = hashCode * -1521134295 + RawVideoId.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<Uri>.Default.GetHashCode(RawVideoThumbnailUrl);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RawVideoTitle);
                hashCode = hashCode * -1521134295 + VideoBlockBanned.GetHashCode();
                hashCode = hashCode * -1521134295 + Duration.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void Sample25Test()
        {
            //using (var p = new ChoJSONReader("sample26.json"))
            //{
            //    foreach (var rec in p)
            //        Console.WriteLine(rec.Dump());
            //}

            //ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Description;
            List<object> expected = new List<object>
            {
                new CoubBig{ Id = 4951721, Type = CoubType.Simple, Permalink = "2hzea", Title ="Dustin Hoffman's favorite band.",
                    ChannelId = 53881, CreatedAt = new DateTimeOffset(new DateTime(2014,7,16,12,49,31,DateTimeKind.Utc)),
                    UpdatedAt =new DateTimeOffset(new DateTime(2018,9,29,14,17,00,DateTimeKind.Utc)),
                    IsDone = true, ViewsCount = 25747, OriginalSound = false, HasSound = false, AgeRestricted = false,
                    PercentDone = 100, RecoubsCount = 151, LikesCount = 450, RawVideoId = 110290, RawVideoTitle = "Alfa Romeo Spider 1600 Duetto - The graduate", Duration = (float)9.56, RawVideoThumbnailUrl = new Uri("http://s1.storage.akamai.coub.com/get/b75/p/raw_video/cw_image/77c50c2aa78/dd9863648e6d929ed30ed/coub_media_1470510743_8ljhm_att-url-download.jpg")
                }
            };
            List<CoubBig> actual = null;
            var config = new ChoJSONRecordConfiguration() { SupportMultipleContent = true };
            config.JsonSerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
            var o = ChoJSONReader.Deserialize<CoubBig>(FileNameSample25JSON, config);
            actual = o.ToList();

            CollectionAssert.AreEqual(expected, actual);
        }

        public class Event
        {
            //[ChoJSONRecordField(FieldName = "event_id")]
            [JsonProperty("event_id")]
            public int EventId { get; set; }
            [JsonProperty("event_name")]
            public string EventName { get; set; }
            [JsonProperty("start_date")]
            public DateTime? StartDate { get; set; }
            [JsonProperty("end_date")]
            public DateTime? EndDate { get; set; }
            //[ChoJSONRecordField(JSONPath = "$..guests[*]")]
            //[ChoJSONPath("$..guests[*]")]
            //[ChoUseJSONSerialization()]
            public List<Guest> Guests { get; set; }

            public override bool Equals(object obj)
            {
                var @event = obj as Event;
                return @event != null &&
                       EventId == @event.EventId &&
                       EventName == @event.EventName &&
                       StartDate == @event.StartDate &&
                       EndDate == @event.EndDate &&
                       new ListEqualityComparer<Guest>().Equals(Guests, @event.Guests);
            }

            public override int GetHashCode()
            {
                var hashCode = -496979945;
                hashCode = hashCode * -1521134295 + EventId.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EventName);
                hashCode = hashCode * -1521134295 + StartDate.GetHashCode();
                hashCode = hashCode * -1521134295 + EndDate.GetHashCode();
                hashCode = hashCode * -1521134295 + new ListEqualityComparer<Guest>().GetHashCode(Guests);
                return hashCode;
            }
        }

        public class Guest
        {
            [JsonProperty("guest_id")]
            public string GuestId { get; set; }
            [JsonProperty("first_name")]
            public string FirstName { get; set; }
            [ChoJSONRecordField(FieldName = "last_name")]
            public string LastName { get; set; }
            //[JsonProperty("telephone")]
            public string Email { get; set; }

            public override bool Equals(object obj)
            {
                var guest = obj as Guest;
                return guest != null &&
                       GuestId == guest.GuestId &&
                       FirstName == guest.FirstName &&
                       LastName == guest.LastName &&
                       Email == guest.Email;
            }

            public override int GetHashCode()
            {
                var hashCode = 7598289;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(GuestId);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FirstName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LastName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Email);
                return hashCode;
            }
        }

        [Test]
        public static void Test3()
        {
            string json = @"{
    ""event_id"": 123,
    ""event_name"": ""event1"",
    ""start_date"": ""2018-11-30"",
    ""end_date"": ""2018-12-04"",
    ""participants"": {
        ""guests"": [
            {
                ""guest_id"": 143,
                ""first_name"": ""John"",
                ""last_name"": ""Smith"",               
            },
            {
                ""guest_id"": 189,
                ""first_name"": ""Bob"",
                ""last_name"": ""Duke"",    
            }
        ]
    }
}";
            string expected = @"[
  {
    ""event_id"": 123,
    ""event_name"": ""event1"",
    ""start_date"": ""2018-11-30T00:00:00"",
    ""end_date"": ""2018-12-04T00:00:00"",
    ""Guests"": [
      {
        ""guest_id"": ""143"",
        ""first_name"": ""John"",
        ""LastName"": ""Smith"",
        ""Email"": null
      },
      {
        ""guest_id"": ""189"",
        ""first_name"": ""Bob"",
        ""LastName"": ""Duke"",
        ""Email"": null
      }
    ]
  }
]";
            var config = new ChoJSONRecordConfiguration<Event>()
                //.Map(p => p.Guests.FirstOrDefault().GuestId, fieldName: "guest_id")
                //.Map(p => p.Guests.FirstOrDefault().FirstName, fieldName: "first_name")
                //.Map(p => p.Guests.FirstOrDefault().LastName, fieldName: "last_name")
                .MapForType<Guest>(p => p.GuestId, fieldName: "guest_id")
                .MapForType<Guest>(p => p.FirstName, fieldName: "first_name")
                .MapForType<Guest>(p => p.LastName, fieldName: "last_name")
                .Map(p => p.EventId, fieldName: "event_id")
                .Map(p => p.Guests, "$..guests[*]")
                ;

            using (var p = ChoJSONReader<Event>.LoadText(json)
                .WithField(m => m.EventId, fieldName: "event_id")
                .WithField(m => m.Guests, jsonPath: "$..guests[*]")
                //.WithFieldForType<Guest>(m => m.GuestId, fieldName: "guest_id")
                //.WithFieldForType<Guest>(m => m.FirstName, fieldName: "first_name")
                //.WithFieldForType<Guest>(m => m.LastName, fieldName: "last_name")
                )
            {
                //foreach (var rec in p)
                //{
                //    actual.Add(rec);
                //    Console.WriteLine(rec.Dump());
                //}

                var actual = JsonConvert.SerializeObject(p, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

            //CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample29_1()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {{ "RollId", ".key.7157" }, { "MType", ".TestMType" } },
                new ChoDynamicObject {{ "RollId", ".key.11261" }, { "MType", null } },
                new ChoDynamicObject {{ "RollId", ".key.7914" }, { "MType", null } }
            };
            List<object> actual = new List<object>();
            using (var r = new ChoJSONReader(FileNameSample29JSON)
                .WithJSONPath("$.._data")
                .WithField("RollId", jsonPath: "$..Id.RollId", fieldType: typeof(string))
                .WithField("MType", jsonPath: "$..Data.MType", fieldType: typeof(string))
                )

            {
                foreach (var rec in r
                    )
                {
                    actual.Add(rec);
                    Console.WriteLine((string)rec.RollId);
                    Console.WriteLine((string)rec.MType);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        public class Facility
        {
            [ChoJSONRecordField]
            public int? Id { get; set; }
            [ChoJSONRecordField]
            public string Name { get; set; }
            [ChoIgnoreMember] //Ignore Uuid
            public string Uuid { get; set; }
            [ChoJSONRecordField]
            public string CreatedAt { get; set; }
            [ChoJSONRecordField]
            public string UpdatedAt { get; set; }
            [ChoJSONRecordField]
            public bool Active { get; set; }
        }
        [Test]
        public static void Issue42()
        {
            string expected = @"Id,Name,CreatedAt,UpdatedAt,Active
39205,Sample1,2019-03-06T14:25:32Z,2019-03-06T14:25:31Z,true
35907,Sample2,2019-02-21T09:33:25Z,2019-02-21T09:33:25Z,true";
            string actual = null;

            string json = @"{
    ""facilities"": [
        {
            ""id"": 39205,
            ""name"": ""Sample1"",
            ""uuid"": ""ac2f3464-c425-4063-86ad-163521b1d610"",
            ""createdAt"": ""2019-03-06T14:25:32Z"",
            ""updatedAt"": ""2019-03-06T14:29:31Z"",
            ""active"": true
        },
        {
            ""id"": 35907,
            ""name"": ""Sample2"",
            ""uuid"": ""d371debb-f030-4c1e-b198-5eb562ceac0f"",
            ""createdAt"": ""2019-02-21T09:33:25Z"",
            ""updatedAt"": ""2019-02-21T09:33:25Z"",
            ""active"": true
        }
    ]
}
";
            StringBuilder csv = new StringBuilder();
            using (var p = ChoJSONReader<Facility>.LoadText(json)
                .WithJSONPath("$..facilities")
                )
            {
                foreach (var rec in p)
                {
                    rec.Print();
                    break;
                }
                return;
                using (var w = new ChoCSVWriter<Facility>(csv)
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(p);
                }
            }

            Console.WriteLine(csv.ToString());
            //actual = Encoding.ASCII.GetString(memStream.ToArray());

            //Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample31Test()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add("PLAYER_ID");
            expected.Columns.Add("PLAYER_NAME");
            expected.Columns.Add("TEAM_ID");
            expected.Columns.Add("TEAM_ABBREVIATION");
            expected.Columns.Add("AGE");
            expected.Columns.Add("GP");
            expected.Columns.Add("W");
            expected.Columns.Add("L");
            expected.Columns.Add("W_PCT");
            expected.Columns.Add("MIN");
            expected.Columns.Add("FGM");
            expected.Columns.Add("FGA");
            expected.Columns.Add("FG_PCT");
            expected.Columns.Add("FG3M");
            expected.Columns.Add("FG3A");
            expected.Columns.Add("FG3_PCT");
            expected.Columns.Add("FTM");
            expected.Columns.Add("FTA");
            expected.Columns.Add("FT_PCT");
            expected.Columns.Add("OREB");
            expected.Columns.Add("DREB");
            expected.Columns.Add("REB");
            expected.Columns.Add("AST");
            expected.Columns.Add("TOV");
            expected.Columns.Add("STL");
            expected.Columns.Add("BLK");
            expected.Columns.Add("BLKA");
            expected.Columns.Add("PF");
            expected.Columns.Add("PFD");
            expected.Columns.Add("PTS");
            expected.Columns.Add("PLUS_MINUS");
            expected.Columns.Add("NBA_FANTASY_PTS");
            expected.Columns.Add("DD2");
            expected.Columns.Add("TD3");
            expected.Columns.Add("GP_RANK");
            expected.Columns.Add("W_RANK");
            expected.Columns.Add("L_RANK");
            expected.Columns.Add("W_PCT_RANK");
            expected.Columns.Add("MIN_RANK");
            expected.Columns.Add("FGM_RANK");
            expected.Columns.Add("FGA_RANK");
            expected.Columns.Add("FG_PCT_RANK");
            expected.Columns.Add("FG3M_RANK");
            expected.Columns.Add("FG3A_RANK");
            expected.Columns.Add("FG3_PCT_RANK");
            expected.Columns.Add("FTM_RANK");
            expected.Columns.Add("FTA_RANK");
            expected.Columns.Add("FT_PCT_RANK");
            expected.Columns.Add("OREB_RANK");
            expected.Columns.Add("DREB_RANK");
            expected.Columns.Add("REB_RANK");
            expected.Columns.Add("AST_RANK");
            expected.Columns.Add("TOV_RANK");
            expected.Columns.Add("STL_RANK");
            expected.Columns.Add("BLK_RANK");
            expected.Columns.Add("BLKA_RANK");
            expected.Columns.Add("PF_RANK");
            expected.Columns.Add("PFD_RANK");
            expected.Columns.Add("PTS_RANK");
            expected.Columns.Add("PLUS_MINUS_RANK");
            expected.Columns.Add("NBA_FANTASY_PTS_RANK");
            expected.Columns.Add("DD2_RANK");
            expected.Columns.Add("TD3_RANK");
            expected.Columns.Add("CFID");
            expected.Columns.Add("CFPARAMS");
            expected.Rows.Add(203932, "Aaron Gordon", 1610612753, "ORL", 23.0, 15, 9, 6, 0.6, 34.8, 6.5, 14.7, 0.439, 1.7, 4.7, 0.366, 2.4, 3.1, 0.783, 1.7, 5.9, 7.6, 3.7, 2.2, 0.7, 0.7, 0.7, 1.9, 3.5, 17.1, 4.4, 33.6, 2, 0, 1, 57, 152, 139, 23, 55, 41, 223, 80, 87, 140, 81, 78, 188, 60, 38, 44, 67, 46, 165, 72, 77, 193, 42, 55, 53, 57, 67, 11, 5, "203932,1610612753");
            expected.Rows.Add(203932, "Aaron Gordon", 1610612753, "ORL", 23.0, 15, 9, 6, 0.6, 34.8, 6.5, 14.7, 0.439, 1.7, 4.7, 0.366, 2.4, 3.1, 0.783, 1.7, 5.9, 7.6, 3.7, 2.2, 0.7, 0.7, 0.7, 1.9, 3.5, 17.1, 4.4, 33.6, 2, 0, 1, 57, 152, 139, 23, 55, 41, 223, 80, 87, 140, 81, 78, 188, 60, 38, 44, 67, 46, 165, 72, 77, 193, 42, 55, 53, 57, 67, 11, 5, "203932,1610612753");

            var actual = new ChoJSONReader(FileNameSample31JSON).WithJSONPath("$..headers[*]", true).Transpose().AsDataTable();
            new ChoJSONReader(FileNameSample31JSON).WithJSONPath("$..rowSet[*]", true).Select(r => ((Array)r.Value).ToDictionary()).Fill(actual);

            DataTableAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void TestData2()
        {
            string expected = @"Column1,Column2,Column3,Column4,Column5,Column6,Column7,Column8,Column9,Column10
A,B,C,D,E,F,G,H,I,J
K,L,M,N,O,P,Q,R,S,T";
            string actual = null;

            string json = @"
{
  ""inputFile"": [
    [""Column1"", ""Column2"", ""Column3"", ""Column4"", ""Column5"", ""Column6"", ""Column7"", ""Column8"", ""Column9"", ""Column10""],
    [ ""A"", ""B"", ""C"", ""D"", ""E"", ""F"", ""G"", ""H"", ""I"", ""J"" ],
    [ ""K"", ""L"", ""M"", ""N"", ""O"", ""P"", ""Q"", ""R"", ""S"", ""T"" ]
  ]
}";

            StringBuilder msg = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
                .WithJSONPath("$.inputFile[*]", true)
                )
            {
                using (var w = new ChoCSVWriter(msg))
                {
                    w.Write(p);
                }
                actual = msg.ToString();
            }

            Assert.AreEqual(expected, actual);
        }

        public class Car
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Brand Brand { get; set; }

            public override bool Equals(object obj)
            {
                var car = obj as Car;
                return car != null &&
                       Id == car.Id &&
                       Name == car.Name &&
                       EqualityComparer<Brand>.Default.Equals(Brand, car.Brand);
            }

            public override int GetHashCode()
            {
                var hashCode = 1934537100;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<Brand>.Default.GetHashCode(Brand);
                return hashCode;
            }
        }
        public class Brand
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                var brand = obj as Brand;
                return brand != null &&
                       Id == brand.Id &&
                       Name == brand.Name;
            }

            public override int GetHashCode()
            {
                var hashCode = -1919740922;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                return hashCode;
            }
        }

        [Test]
        public static void ChildLoad()
        {
            List<object> expected = new List<object> {
                new Car { Id = 1, Name = "Polo", Brand = new Brand { Id = 1, Name = "xxx" } },
                new Car { Id = 2, Name = "328", Brand = new Brand { Id = 1, Name = "xxx" } }
            };
            List<object> actual = new List<object>();

            string carJson = @"
    [
        {
            ""Id"": 1,
            ""Name"": ""Polo"",
            ""Brand"": ""Volkswagen""
        },
        {
            ""Id"": 2,
            ""Name"": ""328"",
            ""Brand"": ""BMW""
        }
    ]";
            using (var p = ChoJSONReader<Car>.LoadText(carJson)
                .WithField(r => r.Brand, valueConverter: o => new Brand() { Id = 1, Name = "xxx" })
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample32Test()
        {
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";

            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader()
                )
            {
                using (var r = new ChoJSONReader(FileNameSample32JSON)
                    .WithJSONPath("$..Individuals[*]", true)
                    )
                {
                    w.Write(r.SelectMany(r1 => ((dynamic[])r1.Events).Select(r2 => new { r1.Id, r2.RecordId, r2.RecordType, r2.EventDate })));
                }
            }

            var actual = csv.ToString();
            var expected = File.ReadAllText(FileNameSample32ExpectedCSV);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void FlattenByTest()
        {
            string json = @"
{
""Count"": 185,
""Message"": ""Results returned successfully"",
""SearchCriteria"": ""Make ID:474 | ModelYear:2016"",
""Results"": [{
        ""Make_ID"": 474,
        ""Make_Name"": ""Honda"",
        ""Model_ID"": 1861,
        ""Model_Name"": ""i10"",
        ""owners"": [{
                ""name"": ""Balaji"",
                ""address"": [{
                        ""city"": ""kcp"",
                        ""pincode"": ""12345""
                    }
                ]
            }, {
                ""name"": ""Rajesh"",
                ""address"": [{
                        ""city"": ""chennai"",
                        ""pincode"": ""12346""
                    }
                ]
            }
        ]
    }, {
        ""Make_ID"": 475,
        ""Make_Name"": ""Honda"",
        ""Model_ID"": 1862,
        ""Model_Name"": ""i20"",
        ""owners"": [{
                ""name"": ""Vijay"",
                ""address"": [{
                        ""city"": ""madurai"",
                        ""pincode"": ""12347""
                    }
                ]
            }, {
                ""name"": ""Andrej"",
                ""address"": [{
                        ""city"": ""Berlin"",
                        ""pincode"": ""12348""
                    }
                ]
            }
        ]
    }
]}";
            var expected = @"[
  {
    ""Make_ID"": 474,
    ""Model_ID"": 1861,
    ""name"": ""Balaji"",
    ""city"": ""kcp"",
    ""pincode"": ""12345""
  },
  {
    ""Make_ID"": 474,
    ""Model_ID"": 1861,
    ""name"": ""Rajesh"",
    ""city"": ""chennai"",
    ""pincode"": ""12346""
  },
  {
    ""Make_ID"": 475,
    ""Model_ID"": 1862,
    ""name"": ""Vijay"",
    ""city"": ""madurai"",
    ""pincode"": ""12347""
  },
  {
    ""Make_ID"": 475,
    ""Model_ID"": 1862,
    ""name"": ""Andrej"",
    ""city"": ""Berlin"",
    ""pincode"": ""12348""
  }
]";
            //ChoDynamicObjectSettings.JsonArrayQualifier = (key, obj) =>
            //{
            //    if (key == "address")
            //        return false;

            //    return null;
            //};

            string actual = null;
            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..Results[*]", true)
                .WithField("Make_ID", jsonPath: "$..Make_ID", isArray: false)
                .WithField("Model_ID", jsonPath: "$..Model_ID", isArray: false)
                .WithField("owners", jsonPath: "$..owners[*]")
                //.WithField("owners", jsonPath: "$..owners[0].address[0]", isArray: false)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());

                actual = JsonConvert.SerializeObject(r.FlattenBy("owners", "address").ToArray(), Newtonsoft.Json.Formatting.Indented);

                //foreach (var rec in r.FlattenBy("owners", "address"))
                //    Console.WriteLine(rec.Dump());

                //foreach (IDictionary<string, object> rec in r)
                //{
                //    foreach (var child in rec.FlattenBy("owners", "address"))
                //    {
                //        Console.WriteLine(child.Dump());
                //    }
                //    //foreach (IDictionary<string, object> owner in (IEnumerable)rec["owners"])
                //    //    foreach (IDictionary<string, object> address in (IEnumerable)owner["address"])
                //    //    {
                //    //        dynamic x = new ChoDynamicObject();
                //    //        x.Merge(rec);
                //    //        x.Merge(owner);
                //    //        x.Merge(address);

                //    //        x.Remove("owners");
                //    //        x.Remove("address");
                //    //        Console.WriteLine(x.Dump());
                //    //    }
                //}
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ManualFlattenByTest()
        {
            string json = @"
{
""Count"": 185,
""Message"": ""Results returned successfully"",
""SearchCriteria"": ""Make ID:474 | ModelYear:2016"",
""Results"": [{
        ""Make_ID"": 474,
        ""Make_Name"": ""Honda"",
        ""Model_ID"": 1861,
        ""Model_Name"": ""i10"",
        ""owners"": [{
                ""name"": ""Balaji"",
                ""address"": [{
                        ""city"": ""kcp"",
                        ""pincode"": ""12345""
                    }
                ]
            }, {
                ""name"": ""Rajesh"",
                ""address"": [{
                        ""city"": ""chennai"",
                        ""pincode"": ""12346""
                    }
                ]
            }
        ]
    }, {
        ""Make_ID"": 475,
        ""Make_Name"": ""Honda"",
        ""Model_ID"": 1862,
        ""Model_Name"": ""i20"",
        ""owners"": [{
                ""name"": ""Vijay"",
                ""address"": [{
                        ""city"": ""madurai"",
                        ""pincode"": ""12347""
                    }
                ]
            }, {
                ""name"": ""Andrej"",
                ""address"": [{
                        ""city"": ""Berlin"",
                        ""pincode"": ""12348""
                    }
                ]
            }
        ]
    }
]}";
            var expected = @"[
  {
    ""Make_ID"": 474,
    ""Model_ID"": 1861,
    ""name"": ""Balaji"",
    ""city"": ""kcp"",
    ""pincode"": ""12345""
  },
  {
    ""Make_ID"": 474,
    ""Model_ID"": 1861,
    ""name"": ""Rajesh"",
    ""city"": ""chennai"",
    ""pincode"": ""12346""
  },
  {
    ""Make_ID"": 475,
    ""Model_ID"": 1862,
    ""name"": ""Vijay"",
    ""city"": ""madurai"",
    ""pincode"": ""12347""
  },
  {
    ""Make_ID"": 475,
    ""Model_ID"": 1862,
    ""name"": ""Andrej"",
    ""city"": ""Berlin"",
    ""pincode"": ""12348""
  }
]";
            string actual = null;
            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..Results[*]", true)
                .WithField("Make_ID", jsonPath: "$..Make_ID", isArray: false)
                .WithField("Model_ID", jsonPath: "$..Model_ID", isArray: false)
                .WithField("owners", jsonPath: "$..owners[*]")
                //.WithField("owners", jsonPath: "$..owners[0].address[0]", isArray: false)
                )
            {

                List<object> output = new List<object>();
                foreach (IDictionary<string, object> rec in r)
                {
                    foreach (IDictionary<string, object> owner in (IEnumerable)rec["owners"])
                    {
                        foreach (IDictionary<string, object> address in (IEnumerable)owner["address"])
                        {
                            dynamic x = new ChoDynamicObject();
                            x.Merge(rec);
                            x.Merge(owner);
                            x.Merge(address);

                            x.Remove("owners");
                            x.Remove("address");

                            output.Add(x);
                        }
                    }
                }
                actual = JsonConvert.SerializeObject(output.ToArray(), Newtonsoft.Json.Formatting.Indented);
            }
            Assert.AreEqual(expected, actual);
        }
        public enum GenderEnumWithDesc
        {
            [Description("M")]
            Male,
            [Description("F")]
            Female
        }

        public class PersonWithEnum
        {
            public int Age { get; set; }
            //[ChoTypeConverter(typeof(ChoEnumConverter), Parameters = "Name")]
            public GenderEnumWithDesc Gender { get; set; }

            public override bool Equals(object obj)
            {
                var person = obj as PersonWithEnum;
                return person != null &&
                       Age == person.Age &&
                       Gender == person.Gender;
            }

            public override int GetHashCode()
            {
                var hashCode = -1400370628;
                hashCode = hashCode * -1521134295 + Age.GetHashCode();
                hashCode = hashCode * -1521134295 + Gender.GetHashCode();
                return hashCode;
            }
        }
        [Test]
        public static void EnumLoadTest()
        {
            List<object> expected = new List<object>
            {
                new PersonWithEnum { Age = 1, Gender = GenderEnumWithDesc.Female}
            };
            List<object> actual = new List<object>();

            string json = @"[
 {
  ""Age"": 1,
  ""Gender"": ""F""
 }
]";
            //ChoTypeConverter.Global.Add(typeof(Enum), new ChoEnumConverter());
            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Description;

            using (var p = ChoJSONReader<PersonWithEnum>.LoadText(json))
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        public class PersonWithEnumDeclarative
        {
            public int Age { get; set; }
            [ChoTypeConverter(typeof(ChoEnumDescriptionConverter))]
            public GenderEnumWithDesc Gender { get; set; }

            public override bool Equals(object obj)
            {
                var person = obj as PersonWithEnumDeclarative;
                return person != null &&
                       Age == person.Age &&
                       Gender == person.Gender;
            }

            public override int GetHashCode()
            {
                var hashCode = -1400370628;
                hashCode = hashCode * -1521134295 + Age.GetHashCode();
                hashCode = hashCode * -1521134295 + Gender.GetHashCode();
                return hashCode;
            }
        }
        [Test]
        public static void EnumDeclarativeLoadTest()
        {
            List<object> expected = new List<object>
            {
                new PersonWithEnumDeclarative { Age = 1, Gender = GenderEnumWithDesc.Female}
            };
            List<object> actual = new List<object>();

            string json = @"[
 {
  ""Age"": 1,
  ""Gender"": ""F""
 }
]";

            using (var p = ChoJSONReader<PersonWithEnumDeclarative>.LoadText(json))
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void ArrayItemsTest()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add("Column_0");
            expected.Columns.Add("Column_1");
            expected.Columns.Add("Column_2");
            expected.Columns.Add("Column_3");
            expected.Columns.Add("Column_4");
            expected.Columns.Add("Column_5");
            expected.Columns.Add("Column_6");
            expected.Columns.Add("Column_7");
            expected.Columns.Add("Column_8");
            expected.Columns.Add("Column_9");
            expected.Columns.Add("Column_10");
            expected.Columns.Add("Column_11");
            expected.Columns.Add("Column_12");
            expected.Columns.Add("Column_13");
            expected.Columns.Add("Column_14");
            expected.Columns.Add("Column_15");
            expected.Columns.Add("Column_16");
            expected.Columns.Add("Column_17");
            expected.Columns.Add("Column_18");
            expected.Columns.Add("Column_19");
            expected.Columns.Add("Column_20");
            expected.Columns.Add("Column_21");
            expected.Columns.Add("Column_22");
            expected.Columns.Add("Column_23");
            expected.Columns.Add("Column_24");
            expected.Columns.Add("Column_25");
            expected.Columns.Add("Column_26");
            expected.Columns.Add("Column_27");
            expected.Columns.Add("Column_28");
            expected.Rows.Add("Test123", "TestHub", "TestVersion", "TestMKT", "TestCAP", "TestRegion", "TestAssembly",
                "TestProduct", "Testgroup", "Testsample", 1806, 1807, 1808, 1809, 1810, 1811, 1812, 1901, 1902,
                1903, 1904, 1905, 1906, 1907, 1908, 1909, 1910, 1911, 1912);
            expected.Rows.Add("Sample12", "Sample879", "201806.1.0", "Sample098", "TSA CBU", "B8", "B8",
                63, "63EM", "EM 42 T", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            expected.Rows.Add("Sample121233", "Sample233879", "2012323806.1.0", "Sampl233e098", "TSA CBU", "B8", "B8",
                "B3", "B3ULUE", "UL 42 R", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            string json = @"[
[
    ""Test123"",
    ""TestHub"",
    ""TestVersion"",
    ""TestMKT"",
    ""TestCAP"",
    ""TestRegion"",
    ""TestAssembly"",
    ""TestProduct"",
    ""Testgroup"",
    ""Testsample"",
    ""1806"",
    ""1807"",
    ""1808"",
    ""1809"",
    ""1810"",
    ""1811"",
    ""1812"",
    ""1901"",
    ""1902"",
    ""1903"",
    ""1904"",
    ""1905"",
    ""1906"",
    ""1907"",
    ""1908"",
    ""1909"",
    ""1910"",
    ""1911"",
    ""1912""
],
[
    ""Sample12"",
    ""Sample879"",
    ""201806.1.0"",
    ""Sample098"",
    ""TSA CBU"",
    ""B8"",
    ""B8"",
    ""63"",
    ""63EM"",
    ""EM 42 T"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0""
],
[
    ""Sample121233"",
    ""Sample233879"",
    ""2012323806.1.0"",
    ""Sampl233e098"",
    ""TSA CBU"",
    ""B8"",
    ""B8"",
    ""B3"",
    ""B3ULUE"",
    ""UL 42 R"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0""
]
]";
            using (var r = ChoJSONReader.LoadText(json)
                )
            {
                var dt = r.Select(rec => ((object[])rec.Value).ToDictionary(valueNamePrefix: "Column_")).AsDataTable();

                DataTableAssert.AreEqual(expected, dt);
            }
        }

        [Test]
        public static void Sample33Test()
        {
            //StringBuilder csvErrors = new StringBuilder();
            //using (var errors = new ChoJSONReader("sample33.json")
            //        .WithJSONPath("$..errors[*]")
            //        .WithField("errors_message", jsonPath: "$.message", isArray: false)
            //        .WithField("errors_extensions_code", jsonPath: "$.extensions.code", isArray: false)
            //        .WithField("errors_locations", jsonPath: "$.locations[*]", isArray: false)
            //        .WithField("errors_path", jsonPath: "$.path[*]")
            //           )
            //{
            //    var arrError = errors.ToArray();
            //    int errorCount = arrError.Length;

            //    using (var w = new ChoCSVWriter(csvErrors)
            //        .WithFirstLineHeader()
            //        .Configure(c => c.MaxScanRows = errorCount)
            //        .Configure(c => c.ThrowAndStopOnMissingField = false)
            //        )
            //    {
            //        w.FileHeaderArrange += (o, e) =>
            //        {
            //            var first = e.Fields.First();
            //            e.Fields.RemoveAt(0);
            //            e.Fields.Add(first);
            //        };
            //        w.Write(arrError);
            //    }
            //}
            //Console.WriteLine(csvErrors.ToString());
            //return;

            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";
            StringBuilder csv = new StringBuilder();
            using (var r = new ChoJSONReader(FileNameSample33JSON)
                .WithJSONPath("$..getUsers[*]", true)
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .WithMaxScanRows(10)
                    .Configure(c => c.ThrowAndStopOnMissingField = false)
                    )
                    w.Write(r);
            }

            using (var sw = new StreamWriter(FileNameSample33TestCSV))
                sw.Write(csv.ToString());
            var actual = csv.ToString();
            var expected = File.ReadAllText(FileNameSample33ExpectedCSV);

            Assert.AreEqual(expected, actual);
        }
        interface Vehicle1 { }

        public class Car1 : Vehicle1
        {
            public string make { get; set; }
            public int numberOfDoors { get; set; }

            public override bool Equals(object obj)
            {
                var car = obj as Car1;
                return car != null &&
                       make == car.make &&
                       numberOfDoors == car.numberOfDoors;
            }

            public override int GetHashCode()
            {
                var hashCode = -1617715551;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(make);
                hashCode = hashCode * -1521134295 + numberOfDoors.GetHashCode();
                return hashCode;
            }
        }

        public class Bicycle1 : Vehicle1
        {
            public int frontGears { get; set; }
            public int backGears { get; set; }

            public override bool Equals(object obj)
            {
                var bicycle = obj as Bicycle1;
                return bicycle != null &&
                       frontGears == bicycle.frontGears &&
                       backGears == bicycle.backGears;
            }

            public override int GetHashCode()
            {
                var hashCode = -539181454;
                hashCode = hashCode * -1521134295 + frontGears.GetHashCode();
                hashCode = hashCode * -1521134295 + backGears.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void PolyTypeTest()
        {
            List<object> expected = new List<object>
            {
                new Car1{ make = "Smart", numberOfDoors = 2 },
                new Car1{ make = "Lexus", numberOfDoors = 4 },
                new Bicycle1{ frontGears = 3, backGears = 6 }
            };
            List<object> actual = new List<object>();

            string json = @"[
  {
    ""Car"": {
      ""make"": ""Smart"",
      ""numberOfDoors"": 2
    }
  },
  {
    ""Car"": {
      ""make"": ""Lexus"",
      ""numberOfDoors"": 4
    }
  },
  {
    ""Bicycle"" : {
      ""frontGears"": 3,
      ""backGears"": 6
    }
  }
]";
            using (var r = ChoJSONReader<Vehicle1>.LoadText(json)
                .WithCustomRecordSelector(o =>
                {
                    var o1 = o.CastTo<Tuple<long, JObject>>().Item2;
                    var type = o1.GetNameAt(0) as string;
                    if (type == "Bicycle")
                        return typeof(Bicycle1);
                    else
                        return typeof(Car1);
                })
                .WithCustomNodeSelector(o =>
                {
                    return ((JObject)o).GetValueAt(0) as JObject;
                })
                )
            {
                foreach (var rec in r)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample34Test()
        {
            string expected = @"UserProfileDetail_UserStatus_name,UserProfileDetail_UserStatusDate,UserProfileDetail_EnrollId,UserProfileDetail_lastDate,UserInformation_id,UserInformation_firstName,UserInformation_middleName,UserInformation_lastName,UserInformation_otherNames,UserInformation_primaryState,UserInformation_otherState,UserInformation_UserLicense_licenseState,UserInformation_UserLicense_licenseNumber,UserInformation_UserLicense_licenseStatus,UserInformation_UserLicense_aaaaaaaaaaaaaaaaa,UserInformation_Setting,UserInformation_primaryEmail,UserInformation_modifiedAt,UserInformation_createdAt
User One,10/31/2018,am**********************************,7/22/2019,1111122,*****,,*****,,MA,MA|BA|DL|RJ,MA|MA2,0|22,,only one|only one2,ADMINISTRATIVE,*****@*****.com,,
User Two,10/31/2019,am**********************************,7/22/2019,443333,*****,Jhon,*****,,AK,MP|CLT,KL,220,Valid,,ADMINISTRATIVE,*****@*****.com,,";

            StringBuilder csv = new StringBuilder();

            using (var r = new ChoJSONReader("sample34.json")
                .WithJSONPath("$..data.getUsers[*]", true)
                )
            {
                var arrPractitioners = r.ToArray();
                int practitionersCount = arrPractitioners.Length;

                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.ThrowAndStopOnMissingField = false)
                    .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                    )
                {
                    w.Write(arrPractitioners.Select(r1 => new
                    {
                        UserProfileDetail_UserStatus_name = r1.UserProfileDetail.UserStatus.name,
                        UserProfileDetail_UserStatusDate = r1.UserProfileDetail.UserStatusDate,
                        UserProfileDetail_EnrollId = r1.UserProfileDetail.EnrollId,
                        UserProfileDetail_lastDate = r1.UserProfileDetail.lastDate,
                        UserInformation_id = r1.UserInformation.id,
                        UserInformation_firstName = r1.UserInformation.firstName,
                        UserInformation_middleName = r1.UserInformation.middleName,
                        UserInformation_lastName = r1.UserInformation.lastName,
                        UserInformation_otherNames = r1.UserInformation.otherNames,
                        UserInformation_primaryState = r1.UserInformation.primaryState,
                        UserInformation_otherState = r1.UserInformation.otherState != null ? string.Join("|", r1.UserInformation.otherState) : null,
                        UserInformation_UserLicense_licenseState = r1.UserInformation.UserLicense != null ? string.Join("|", ((List<object>)r1.UserInformation.UserLicense).Cast<dynamic>().Select(r2 => r2.licenseState).ToArray()) : null,
                        UserInformation_UserLicense_licenseNumber = r1.UserInformation.UserLicense != null ? string.Join("|", ((List<object>)r1.UserInformation.UserLicense).Cast<dynamic>().Select(r2 => Int32.Parse(r2.licenseNumber)).ToArray()) : null,
                        UserInformation_UserLicense_licenseStatus = r1.UserInformation.UserLicense != null ? string.Join("|", ((List<object>)r1.UserInformation.UserLicense).Cast<dynamic>().Select(r2 => r2.licenseStatus).ToArray()) : null,
                        UserInformation_UserLicense_aaaaaaaaaaaaaaaaa = r1.UserInformation.UserLicense != null ? string.Join("|", ((List<object>)r1.UserInformation.UserLicense).Cast<dynamic>().Select(r2 => r2.aaaaaaaaaaaaaaaaa).ToArray()) : null,
                        UserInformation_Setting = r1.UserInformation.Setting,
                        UserInformation_primaryEmail = r1.UserInformation.primaryEmail,
                        UserInformation_modifiedAt = r1.UserInformation.modifiedAt,
                        UserInformation_createdAt = r1.UserInformation.createdAt,
                    }));
                }
                //using (var w = new ChoCSVWriter(csv)
                //    .WithFirstLineHeader()
                //    .WithMaxScanRows(2)
                //    .ThrowAndStopOnMissingField(false)
                //    )
                //    w.Write(r);
            }

            string actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void JSON2XmlArray()
        {
            string json = @"{
  ""header"": ""myheader"",
  ""transaction"": {
    ""date"": ""2019-09-24"",
    ""items"": [
      {
        ""number"": ""123"",
        ""unit"": ""EA"",
        ""qty"": 6
      },
      {
        ""number"": ""456"",
        ""unit"": ""CS"",
        ""qty"": 4
      }
    ]
  }
}";
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <header>myheader</header>
  <transaction date=""2019-09-24"">
    <item number=""123"" unit=""EA"" qty=""6"" />
    <item number=""456"" unit=""CS"" qty=""4"" />
  </transaction>
</Root>";

            StringBuilder xml = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                r.AfterRecordLoad += (o, e) =>
                {
                    var rec = e.Record as dynamic;

                    rec.transaction.SetAsAttribute("date");
                    var items = ((IList)rec.transaction.items).Cast<dynamic>()
                        .Select(i =>
                        {
                            i.SetAsAttribute("number");
                            i.SetAsAttribute("unit");
                            i.SetAsAttribute("qty");
                            return i;
                        }).ToArray();
                };

                using (var w = new ChoXmlWriter(xml)
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.NodeName = "Root")
                    )
                {
                    w.Write(r);
                }
            }

            string actual = xml.ToString();
            Console.WriteLine(xml.ToString());

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void JSON2XmlArray_1()
        {
            string json = @"{
  ""header"": ""myheader"",
  ""transaction"": {
    ""date"": ""2019-09-24"",
    ""items"": [
      {
        ""number"": ""123"",
        ""unit"": ""EA"",
        ""qty"": 6
      },
      {
        ""number"": ""456"",
        ""unit"": ""CS"",
        ""qty"": 4
      }
    ]
  }
}";
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <header>myheader</header>
  <transaction date=""2019-09-24"">
    <items number=""123"" unit=""EA"" qty=""6"" />
    <items number=""456"" unit=""CS"" qty=""4"" />
  </transaction>
</Root>";

            StringBuilder xml = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                r.AfterRecordLoad += (o, e) =>
                {
                    var rec = e.Record as dynamic;

                    rec.transaction.SetAsAttribute("date");
                    var items = ((IList)rec.transaction.items).Cast<dynamic>()
                        .Select(i =>
                        {
                            i.SetAsAttribute("number");
                            i.SetAsAttribute("unit");
                            i.SetAsAttribute("qty");
                            return i;
                        }).ToArray();
                };

                using (var w = new ChoXmlWriter(xml)
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.NodeName = "Root")
                    .Configure(c => c.KeepOriginalNodeName = true)
                    )
                {
                    w.Write(r);
                }
            }

            string actual = xml.ToString();
            Console.WriteLine(xml.ToString());

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void BuildDynamicDataTableFromJSON()
        {
            string expected = @"[
  {
    ""Make_ID"": 474,
    ""Model_ID"": 1861,
    ""name"": ""Balaji"",
    ""city"": ""kcp""
  },
  {
    ""Make_ID"": 474,
    ""Model_ID"": 1861,
    ""name"": ""Rajesh"",
    ""city"": ""chennai""
  },
  {
    ""Make_ID"": 475,
    ""Model_ID"": 1862,
    ""name"": ""Vijay"",
    ""city"": ""madurai""
  },
  {
    ""Make_ID"": 475,
    ""Model_ID"": 1862,
    ""name"": ""Andrej"",
    ""city"": ""Berlin""
  }
]";

            StringBuilder csv = new StringBuilder();
            using (var r = new ChoJSONReader("sample35.json")
                .WithJSONPath("$..Results")
                )
            {
                var r1 = r.FlattenBy("owners", "address").ToArray();
                var dt = r1.AsDataTable(selectedFields: new string[] { "Make_ID", "Model_ID", "name", "city" });
                var actual = dt.DumpAsJson();
                Console.WriteLine(dt.DumpAsJson());

                Assert.AreEqual(expected, actual);

                return;
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .WithFields("Make_ID", "Model_ID", "name", "city")
                    )
                    w.Write(r1);
            }

            Console.WriteLine(csv.ToString());
        }

        [Test]
        public static void JSONDataTable()
        {
            string expected = @"[
  {
    ""header"": ""myheader"",
    ""transaction_date"": ""2019-09-24"",
    ""transaction_items_0_number"": ""123"",
    ""transaction_items_0_unit"": ""EA"",
    ""transaction_items_0_qty"": 6,
    ""transaction_items_1_number"": ""456"",
    ""transaction_items_1_unit"": ""CS"",
    ""transaction_items_1_qty"": 4
  }
]";

            string json = @"{
              ""header"": ""myheader"",
              ""transaction"": {
                ""date"": ""2019-09-24"",
                ""items"": [
                  {
                    ""number"": ""123"",
                    ""unit"": ""EA"",
                    ""qty"": 6
                  },
                  {
                    ""number"": ""456"",
                    ""unit"": ""CS"",
                    ""qty"": 4
                  }
                ]
              }
            }";

            string actual = null;
            using (var r = ChoJSONReader.LoadText(json))
            {
                var dt = r.Select(f => f.Flatten()).AsDataTable();
                actual = dt.DumpAsJson();
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void JSON2CSV1()
        {
            var expected = @"product,display_en,description_summary_en,description_action_en,description_full_en,fulfillment_instructions_en,fulfillment_instructions_es,image,format,sku,attributes_key1,attributes_key2,pricing_trial,pricing_interval,pricing_intervalLength,pricing_quantityBehavior,pricing_quantityDefault,pricing_price_USD,pricing_price_EUR,pricing_quantityDiscounts_10,pricing_discountReason_en,pricing_discountDuration
my-sku-123,String,String,String,String,String,String,https://d8y8nchqlnmka.cloudfront.net/NVaGM-nhSpQ/-FooqIP-R84/photio-imac-hero.png,digital,string,value1,value2,2,month,1,allow,1,14.95,10.99,25,The Reason,1";

            StringBuilder csv = new StringBuilder();

            using (var r = new ChoJSONReader("sample36.json")
                .WithJSONPath("$..products[*]", true)
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.IgnoreDictionaryFieldPrefix = false)
                    )
                {
                    w.Write(r);
                }
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class Person
        {
            public string Name { get; set; }
            public JToken Items { get; set; }

            [JsonIgnore]
            public string ItemsValue => Items.ToString();
        }

        [Test]
        public static void DeserializeAsJToken()
        {
            string json = @"{
  ""name"" : ""tim"",
  ""items"" : {
    ""car"" : ""Mercedes"",
    ""house"" : ""2 Bedroom""
  }
}";
            using (var r = ChoJSONReader<Person>.LoadText(json))
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }
        static void SetupTest()
        {
            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 10;
            rec1.Name = "Mark";
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 200;
            rec2.Name = "Lou";
            objs.Add(rec2);

            StringBuilder csv = new StringBuilder();
            using (var parser = new ChoCSVWriter(csv)
              .WithFirstLineHeader()
                .Setup(r => r.BeforeRecordWrite += (o, e) =>
                {
                })
              )
            {
                parser.Write(objs);
            }

            Console.WriteLine(csv.ToString());
        }
        [Test]

        public static void LargeJsonTest()
        {
            //JObject o1 = JObject.Parse(File.ReadAllText(@"citylots.json"));
            var expected = @"[
  {
    ""FCC-IRCUBE"": [
      {
        ""curveDefinitionId"": ""FCC"",
        ""curveFamilyId"": ""EUR/EURCURVE"",
        ""curveName"": ""EURCURVE"",
        ""marketDataSet"": ""FCC-IRCUBE"",
        ""referenced"": false
      }
    ]
  }
]";

            using (var r = new ChoJSONReader("sample14.json") //sf_city_lots citylots
                .WithJSONPath("$.irCurves.EUR")
                )
            {
                //Console.WriteLine(r.Count());
                //foreach (var rec in r.Take(10))
                //    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(r.ToArray(), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Person1
        {
            public IProfession Profession { get; set; }
        }

        public interface IProfession
        {
            string JobTitle { get; }
        }

        public class Programming : IProfession
        {
            public string JobTitle => "Software Developer";
            public string FavoriteLanguage { get; set; }
        }

        public class Writing : IProfession
        {
            public string JobTitle => "Copywriter";
            public string FavoriteWord { get; set; }
        }

        [Test]
        public static void InterfaceTest()
        {
            string json = @"{
    ""$type"": ""ChoJSONReaderTest.Program+Person1, ChoJSONReaderTest"",
    ""Profession"": {
        ""$type"": ""ChoJSONReaderTest.Program+Programming, ChoJSONReaderTest"",
        ""JobTitle"": ""Software Developer"",
        ""FavoriteLanguage"": ""C#""
    }
}";

            Person1 expected = new Person1
            {
                Profession = new Programming
                {
                    FavoriteLanguage = "C#"
                }
            };
            Person1 actual = null;
            foreach (var rec in ChoJSONReader<Person1>.LoadText(json)
                .Configure(c => c.UseJSONSerialization = true)
                .Configure(c => c.JsonSerializerSettings.TypeNameHandling = TypeNameHandling.Objects)
                .UseDefaultContractResolver()
                .Configure(c => c.UnknownType = typeof(Programming))
                )
            {
                actual = rec;
                break;
            }
            Assert.AreEqual(actual.GetType(), expected.GetType());
        }

        [Test]
        public static void DateTimeAsStringTest()
        {
            string json = @"[
  {
    ""rno"": 1,
    ""Name"": ""XYZ"",
    ""Created Date"": ""2014-04-30T14:39:12.2397769Z""
  },
  {
    ""rno"": 2,
    ""Name"": ""ABC"",
    ""Created Date"": """"
  }
]";
            List<string> expected = new List<string>()
            {
                "2014-04-30T14:39:12.2397769Z",
                "",
            };
            List<string> actual = new List<string>();
            using (var r = ChoJSONReader.LoadText(json)
                .JsonSerializationSettings(j => j.DateParseHandling = DateParseHandling.None)
                )
            {
                foreach (var rec in r)
                    actual.Add(rec["Created Date"]);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class ImdbJsonPerson
        {
            public string Url { get; set; }
        }

        public class ImdbJsonMovie
        {
            public string Url { get; set; }
            public string Name { get; set; }
            public string Image { get; set; }
            public List<string> Genre { get; set; }
            public List<ImdbJsonPerson> Actor { get; set; }
            public List<ImdbJsonPerson> Director { get; set; }
            //public string[] Creator { get; set; }
        }

        [Test]
        public static void ArrayOrSingleNodeTest()
        {
            ImdbJsonMovie expected = null;
            ImdbJsonMovie actual = null;
            using (var r = new ChoJSONReader<ImdbJsonMovie>("sample37.json"))
            {
                actual = r.FirstOrDefault();

                expected = JsonConvert.DeserializeObject<ImdbJsonMovie>(JsonConvert.SerializeObject(actual));
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
            }
            CompareLogic compareLogic = new CompareLogic();
            ComparisonResult result = compareLogic.Compare(expected, actual);
            Assert.AreEqual(true, result.AreEqual);
        }

        [Test]
        public static void Json2CSV1()
        {
            string expected = @"attr1,attr2,attr3
val1,val2,val3";

            string json = @"{
                'attr1': 'val1',
                'attr2': 'val2',
                'attr3': 'val3'                      
            }";

            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    )
                    w.Write(r);
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Json2CSV2()
        {
            string json = @" {id: 1, name: ""Tom"", friends: [""Dick"", ""Harry""]}";

            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json)
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .NestedKeySeparator('/')
                    )
                    w.Write(r);
            }

            Console.WriteLine(csv.ToString());
        }

        public class FileInfo
        {
            public List<string> filenames { get; set; }
            public int? cluster_number { get; set; }
            public List<string> Top_Terms { get; set; }
        }

        [Test]
        public static void Sample38Test()
        {
            string expected = @"filename,cluster_number,top_terms
a.txt,0,would
b.txt,0,get
c.txt,0,like
a.txt,1,would
b.txt,1,get
c.txt,1,like";

            StringBuilder csv = new StringBuilder();
            using (var r = new ChoJSONReader<FileInfo>("sample38.json")
                .WithJSONPath("$..^", flattenIfJArrayWhenReading: false)
                .WithField(f => f.filenames, jsonPath: "Value[*].filenames")
                .WithField(f => f.cluster_number, jsonPath: "Value[*].cluster_number")
                .WithField(f => f.Top_Terms, jsonPath: "Value[*].Top_Terms")
                .Setup(s => s.SkipUntil += (o, e) =>
                {
                    var node = e.Source as JObject;
                })
                //.WithField("filenames", jsonPath: "Value[*].filenames", fieldType: typeof(string[]))
                //.WithField("cluster_number", jsonPath: "Value[*].cluster_number", fieldType: typeof(int))
                //.WithField("Top_Terms", jsonPath: "Value[*].Top_Terms", fieldType: typeof(string[]))
                )
            {
                //var x = r/*.Select(r1 => r1.Value)*/.ToArray();

                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    //.WithField(f => f.filenames)
                    //.WithField(f => f.cluster_number)
                    //.WithField(f => f.Top_Terms)
                    //.Index(f => f.filenames, 0, 3)
                    //.Index(f => f.Top_Terms, 0, 3)
                    )
                    w.Write(r.SelectMany(r1 => r1.filenames.Select((f, i) => new
                    {
                        filename = f,
                        cluster_number = r1.cluster_number,
                        top_terms = r1.Top_Terms[i]
                    })
                    ));
                //    //foreach (var rec in r) //.Select(r2 => ((dynamic[])r2).SelectMany(r1 => ((IList<string>)r1.filenames))))
                //    //    Console.WriteLine(rec.Dump());
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DefaultValueTest()
        {
            string json = @"
    [
        {
            ""Id"": 1,
            ""Name"": ""Polo"",
            ""Brand"": ""Volkswagen""
        },
        {
            ""Id"": 2,
            ""Name"": ""328"",
            ""Brand"": ""BMW""
        }
    ]";

            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Polo"",
    ""Name1"": ""Test""
  },
  {
    ""Id"": 2,
    ""Name"": ""328"",
    ""Name1"": ""Test""
  }
]";

            using (var r = ChoJSONReader.LoadText(json)
                .WithField("Id")
                .WithField("Name")
                .WithField("Name1", defaultValue: "Test")
                .Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Null)
                )
            {

                var actual = JsonConvert.SerializeObject(r.ToArray(), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void FallbackValueTest()
        {
            string json = @"
    [
        {
            ""Id"": ""1"",
            ""Name"": ""Polo"",
            ""Brand"": ""Volkswagen""
        },
        {
            ""Id"": ""2x"",
            ""Name"": ""328"",
            ""Brand"": ""BMW""
        }
    ]";

            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Polo"",
    ""Name1"": ""Test""
  },
  {
    ""Id"": 100,
    ""Name"": ""328"",
    ""Name1"": ""Test""
  }
]";
            using (var r = ChoJSONReader.LoadText(json)
                .WithField("Id", fieldType: typeof(int), fallbackValue: 100)
                .WithField("Name")
                .WithField("Name1", defaultValue: "Test")
                .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Any)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                var actual = JsonConvert.SerializeObject(r.ToArray(), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class EmpWithCurrency
        {
            public int Id { get; set; }
            public ChoCurrency Salary { get; set; }
        }

        [Test]
        public static void CurrencyTest()
        {
            string json = @"
    [
        {
            ""Id"": ""1"",
            ""Name"": ""Polo"",
            ""Salary"": ""$2000""
        },
        {
            ""Id"": ""2"",
            ""Name"": ""328"",
            ""Salary"": ""$10,000""
        }
    ]";

            string expected = @"[
  {
    ""Id"": 1,
    ""Salary"": 2000.0
  },
  {
    ""Id"": 2,
    ""Salary"": 10000.0
  }
]";
            using (var r = ChoJSONReader.LoadText(json)
                .WithField("Id", fieldType: typeof(int))
                .WithField("Salary", fieldType: typeof(ChoCurrency))
                )
            {
                var actual = JsonConvert.SerializeObject(r.ToArray(), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }

        [Test]
        public static void CurrencyTestWithPOCO()
        {
            string json = @"
    [
        {
            ""Id"": ""1"",
            ""Name"": ""Polo"",
            ""Salary"": ""$2000""
        },
        {
            ""Id"": ""2"",
            ""Name"": ""328"",
            ""Salary"": ""$10,000""
        }
    ]";

            string expected = @"[
  {
    ""Id"": 1,
    ""Salary"": 2000.0
  },
  {
    ""Id"": 2,
    ""Salary"": 10000.0
  }
]";

            using (var r = ChoJSONReader<EmpWithCurrency>.LoadText(json)
                //.WithField("Id")
                //.WithField("Salary", fieldType: typeof(decimal))
                .WithMaxScanNodes(1)
                )
            {
                var actual = JsonConvert.SerializeObject(r.ToArray(), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }
        [Test]
        public static void CurrencyDynamicTest()
        {
            string json = @"
    [
        {
            ""Id"": ""1"",
            ""Name"": ""Polo"",
            ""Salary"": ""$2000""
        },
        {
            ""Id"": ""2"",
            ""Name"": ""328"",
            ""Salary"": ""$10,000""
        }
    ]";

            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Polo"",
    ""Salary"": 2000.0
  },
  {
    ""Id"": 2,
    ""Name"": ""328"",
    ""Salary"": 10000.0
  }
]";
            //ChoTypeConverterFormatSpec.Instance.TreatCurrencyAsDecimal = false;
            using (var r = ChoJSONReader.LoadText(json)
                //.WithField("Id")
                //.WithField("Salary", fieldType: typeof(decimal))
                .WithMaxScanNodes(2)
                )
            {
                var actual = JsonConvert.SerializeObject(r.ToArray(), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }

        public class Balance
        {
            public float amount { get; set; }
            public float value { get; set; }
        }

        [Test]
        public static void LoadDictValuesTest()
        {
            string json = @"{
  ""AE"": {
    ""amount"": ""0.00000000"",
    ""value"": ""0.00000000""
  },
  ""AR"": {
    ""amount"": ""0.00000000"",
    ""value"": ""0.00000000""
  },
  ""BC"": {
    ""amount"": ""0.09670332"",
    ""value"": ""3.74814004""
  }
}";


            string expected = @"[
  {
    ""amount"": 0.0,
    ""value"": 0.0
  },
  {
    ""amount"": 0.0,
    ""value"": 0.0
  },
  {
    ""amount"": 0.09670332,
    ""value"": 3.74814
  }
]";
            using (var r = ChoJSONReader<Balance>.LoadText(json)
                .WithJSONPath("$..^")
                )
            {
                var actual = JsonConvert.SerializeObject(r.ToArray(), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void LoadDictKeysTest()
        {
            string json = @"{
  ""1"": {
    ""amount"": ""0.00000000"",
    ""value"": ""0.00000000""
  },
  ""2"": {
    ""amount"": ""0.00000000"",
    ""value"": ""0.00000000""
  },
  ""3"": {
    ""amount"": ""0.09670332"",
    ""value"": ""3.74814004""
  }
}";


            string expected = @"[
  1,
  2,
  3
]";
            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..~")
                .WithField("Id", fieldName: "Value", fieldType: typeof(int))
                )
            {
                var actual = JsonConvert.SerializeObject(r.Select(rec => rec.Id).ToArray(), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void LoadDictTest()
        {
            string json = @"{
  ""AE"": {
    ""amount"": ""0.00000000"",
    ""value"": ""0.00000000""
  },
  ""AR"": {
    ""amount"": ""0.00000000"",
    ""value"": ""0.00000000""
  },
  ""BC"": {
    ""amount"": ""0.09670332"",
    ""value"": ""3.74814004""
  }
}";
            string expected = @"[
  {
    ""AE"": {
      ""amount"": 0.0,
      ""value"": 0.0
    },
    ""AR"": {
      ""amount"": 0.0,
      ""value"": 0.0
    },
    ""BC"": {
      ""amount"": 0.09670332,
      ""value"": 3.74814
    }
  }
]";

            using (var r = ChoJSONReader<IDictionary<string, Balance>>.LoadText(json)
                .UseJsonSerialization()
                //.WithJSONPath("$", true)
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public enum Gender { Male, Female }
        public class Employee
        {
            public int Age { get; set; }
            public Gender Gender { get; set; }
        }
        [Test]
        public static void EnumTest()
        {
            string json = @"{ ""Age"": 35, ""Gender"": ""Male"" }";

            string expected = @"[
  {
    ""Age"": 35,
    ""Gender"": 0
  }
]";
            using (var r = ChoJSONReader<Employee>.LoadText(json))
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void DynamicEnumTest()
        {
            string json = @"{ ""Age"": 35, ""Gender"": ""Male"" }";

            string expected = @"[
  {
    ""Age"": 35,
    ""Gender"": 0
  }
]";
            using (var r = ChoJSONReader.LoadText(json)
                .WithField("Age")
                .WithField("Gender", fieldType: typeof(Gender))
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void JSON2XmlTest()
        {
            string json = @"[
  {
    ""Id"": 1,
    ""Name"": ""Mark""
  },
  {
    ""Id"": 2,
    ""Name"": ""Tom""
  }
]
";
            string expected = @"<Emps xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <Emp>
    <Id>1</Id>
    <Name>Mark</Name>
  </Emp>
  <Emp>
    <Id>2</Id>
    <Name>Tom</Name>
  </Emp>
</Emps>";
            StringBuilder xml = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoXmlWriter(xml)
                    .WithRootName("Emps")
                    .WithNodeName("Emp")
                    )
                    w.Write(r);
            }
            Console.WriteLine(xml.ToString());
            var actual = xml.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class UserInfo
        {
            [ChoJSONRecordField(JSONPath = "$.name")]
            public string name { get; set; }
            [ChoJSONRecordField(JSONPath = "$.teamname")]
            public string teamname { get; set; }
            [ChoJSONRecordField(JSONPath = "$.email")]
            public string email { get; set; }
            [ChoJSONRecordField(JSONPath = "$.players")]
            public string[] players { get; set; }
        }
        [Test]
        public static void ReadSelectNodeTest()
        {
            string json = @"
{
    ""user"": {
        ""name"": ""asdf"",
        ""teamname"": ""b"",
        ""email"": ""c"",
        ""players"": [""1"", ""2""]
    }
}";
            string expected = @"[
  {
    ""name"": ""asdf"",
    ""teamname"": ""b"",
    ""email"": ""c"",
    ""players"": [
      ""1"",
      ""2""
    ]
  }
]";
            using (var r = ChoJSONReader<UserInfo>.LoadText(json)
                .WithJSONPath("$.user")
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Image
        {
            public string Src { get; set; }
        }
        [Test]
        public static void Sample39Test()
        {
            //var tokens = JObject.Load(new JsonTextReader(new StreamReader(ChoPath.GetFullPath("sample39.json")))).SelectTokens("$..queryresult.pods[*].subpods[*].img")
            //    .Select(t => JsonConvert.DeserializeObject<Image>(t.ToString()));
            //foreach (var rec in tokens)
            //    Console.WriteLine(rec.Dump());

            //return;
            string expected = @"[
  {
    ""src"": ""https://www5a.myWebsite.com/Calculate/MSP/MSP362523b9dc107h895b640000214gh7dcb3c6e027?MSPStoreType=image/gif&s=54"",
    ""alt"": ""3 + 3"",
    ""title"": ""3 + 3"",
    ""width"": 30,
    ""height"": 18,
    ""type"": ""Default"",
    ""themes"": ""1,2,3,4,5,6,7,8,9,10,11,12"",
    ""colorinvertable"": true
  },
  {
    ""src"": ""https://www5a.myWebsite.com/Calculate/MSP/MSP362623b9dc107h895b6400005d6ad57aedd6f604?MSPStoreType=image/gif&s=54"",
    ""alt"": ""6"",
    ""title"": ""6"",
    ""width"": 8,
    ""height"": 18,
    ""type"": ""Default"",
    ""themes"": ""1,2,3,4,5,6,7,8,9,10,11,12"",
    ""colorinvertable"": true
  },
  {
    ""src"": ""https://www5a.myWebsite.com/Calculate/MSP/MSP362723b9dc107h895b6400001971f69egfe5ac57?MSPStoreType=image/gif&s=54"",
    ""alt"": ""Number line"",
    ""title"": """",
    ""width"": 330,
    ""height"": 54,
    ""type"": ""1DMathPlot_2"",
    ""themes"": ""1,2,3,4,5,6,7,8,9,10,11,12"",
    ""colorinvertable"": true
  },
  {
    ""src"": ""https://www5a.myWebsite.com/Calculate/MSP/MSP362823b9dc107h895b6400004b2i72a35885f8hf?MSPStoreType=image/gif&s=54"",
    ""alt"": ""six"",
    ""title"": ""six"",
    ""width"": 18,
    ""height"": 18,
    ""type"": ""Default"",
    ""themes"": ""1,2,3,4,5,6,7,8,9,10,11,12"",
    ""colorinvertable"": true
  },
  {
    ""src"": ""https://www5a.myWebsite.com/Calculate/MSP/MSP362923b9dc107h895b64000055i4die93dg45fa5?MSPStoreType=image/gif&s=54"",
    ""alt"": ""| + | | = | \n3 | | 3 | | 6"",
    ""title"": ""| + | | = | \n3 | | 3 | | 6"",
    ""width"": 130,
    ""height"": 56,
    ""type"": ""Default"",
    ""themes"": ""1,2,3,4,5,6,7,8,9,10,11,12"",
    ""colorinvertable"": true
  },
  {
    ""src"": ""https://www5a.myWebsite.com/Calculate/MSP/MSP363023b9dc107h895b6400002hcc1ibbc04h11de?MSPStoreType=image/gif&s=54"",
    ""alt"": ""age 6: 3.2 seconds | age 8: 1.8 seconds | age 10: 1.2 seconds | \nage 18: 0.83 seconds\n(ignoring concentration, repetition, variations in education, etc.)"",
    ""title"": ""age 6: 3.2 seconds | age 8: 1.8 seconds | age 10: 1.2 seconds | \nage 18: 0.83 seconds\n(ignoring concentration, repetition, variations in education, etc.)"",
    ""width"": 449,
    ""height"": 64,
    ""type"": ""Grid"",
    ""themes"": ""1,2,3,4,5,6,7,8,9,10,11,12"",
    ""colorinvertable"": true
  }
]";
            using (var r = new ChoJSONReader("sample39.json")
                .WithJSONPath("$..queryresult.pods[*].subpods[*].img", true)
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public abstract class Person2
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        public class Employee2 : Person2
        {
            public string Department { get; set; }
            public string JobTitle { get; set; }
        }

        public class Artist2 : Person2
        {
            public string Skill { get; set; }
        }
        [Test]
        public static void DeserializeDifferentObjects()
        {
            string json = @"[
  {
    ""Department"": ""Department1"",
    ""JobTitle"": ""JobTitle1"",
    ""FirstName"": ""FirstName1"",
    ""LastName"": ""LastName1""
  },
  {
    ""Department"": ""Department2"",
    ""JobTitle"": ""JobTitle2"",
    ""FirstName"": ""FirstName2"",
    ""LastName"": ""LastName2""
  },
  {
    ""Skill"": ""Painter"",
    ""FirstName"": ""FirstName3"",
    ""LastName"": ""LastName3""
  }
]";
            string expected = @"[
  {
    ""Department"": ""Department1"",
    ""JobTitle"": ""JobTitle1"",
    ""FirstName"": ""FirstName1"",
    ""LastName"": ""LastName1""
  },
  {
    ""Department"": ""Department2"",
    ""JobTitle"": ""JobTitle2"",
    ""FirstName"": ""FirstName2"",
    ""LastName"": ""LastName2""
  },
  {
    ""Skill"": ""Painter"",
    ""FirstName"": ""FirstName3"",
    ""LastName"": ""LastName3""
  }
]";
            using (var r = ChoJSONReader<Person2>.LoadText(json)
                .WithCustomRecordSelector(o =>
                {
                    var pair = (Tuple<long, JObject>)o;
                    var obj = pair.Item2;

                    if (obj.ContainsKey("Skill"))
                        return typeof(Artist2);

                    return typeof(Employee2);
                })
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Account
        {
            public string Email { get; set; }
            public bool Active { get; set; }
            public DateTime CreatedDate { get; set; }
            public IList<string> Roles { get; set; }
        }
        [Test]
        public static void DeserializeObject()
        {
            string json = @"{
  'Email': 'james@example.com',
  'Active': true,
  'CreatedDate': '2013-01-20T00:00:00Z',
  'Roles': [
    'User',
    'Admin'
  ]
}";

            Account account = ChoJSONReader.DeserializeText<Account>(json).FirstOrDefault();

            Assert.AreEqual(account.Email, "james@example.com");
        }

        [Test]
        public static void DeserializeCollection()
        {
            string json = @"['Starcraft','Halo','Legend of Zelda']";

            List<string> videogames = ChoJSONReader.DeserializeText<string>(json).ToList();

            string expected = @"[
  ""Starcraft"",
  ""Halo"",
  ""Legend of Zelda""
]";
            Console.WriteLine(string.Join(", ", videogames.ToArray()));
            var actual = JsonConvert.SerializeObject(videogames, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DeserializeDictionary()
        {
            string json = @"{
  'href': '/account/login.aspx',
  'target': '_blank'
}";

            Dictionary<string, string> htmlAttributes = ChoJSONReader.DeserializeText<Dictionary<string, string>>(json).FirstOrDefault();

            Assert.AreEqual(htmlAttributes["href"], "/account/login.aspx");
            Assert.AreEqual(htmlAttributes["target"], "_blank");
        }

        public class Movie
        {
            public string Title { get; set; }
            public int Year { get; set; }
        }

        [Test]
        public static void DeserializeFromFile()
        {
            Movie movie1 = ChoJSONReader.Deserialize<Movie>("movie.json").FirstOrDefault();
            Assert.AreEqual(movie1.Title, "They Shall Not Grow Old");
            Assert.AreEqual(movie1.Year, 2018);
        }

        public class Person3
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DateTime BirthDate { get; set; }
        }

        public class Employee3 : Person3
        {
            public string Department { get; set; }
            public string JobTitle { get; set; }
        }

        [Test]
        public static void CustomCreationTest()
        {
            string json = @"{
  'Department': 'Furniture',
  'JobTitle': 'Carpenter',
  'FirstName': 'John',
  'LastName': 'Joinery',
  'BirthDate': '1983-02-02T00:00:00'
}";

            //var dt = ChoJSONReader.LoadText(json).AsDataTable();
            Person3 person = ChoJSONReader.DeserializeText<Person3>(json).FirstOrDefault();
            Assert.AreEqual(person.GetType(), typeof(Employee3));
            Assert.AreEqual(person.FirstName, "John");
        }
        [Test]
        public static void ToDataTable()
        {
            string json = @"[
{""id"":""10"",""name"":""User"",""add"":false,""edit"":true,""authorize"":true,""view"":true},
{ ""id"":""11"",""name"":""Group"",""add"":true,""edit"":false,""authorize"":false,""view"":true},
{ ""id"":""12"",""name"":""Permission"",""add"":true,""edit"":true,""authorize"":true,""view"":true}
]";
            string expected = @"[
  {
    ""id"": ""10"",
    ""name"": ""User"",
    ""add"": false,
    ""edit"": true,
    ""authorize"": true,
    ""view"": true
  },
  {
    ""id"": ""11"",
    ""name"": ""Group"",
    ""add"": true,
    ""edit"": false,
    ""authorize"": false,
    ""view"": true
  },
  {
    ""id"": ""12"",
    ""name"": ""Permission"",
    ""add"": true,
    ""edit"": true,
    ""authorize"": true,
    ""view"": true
  }
]";
            using (var r = ChoJSONReader.LoadText(json))
            {
                var dt = r.AsDataTable();
                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
            //using (var r = new ChoJSONReader("sample7.json"))
            //{
            //    var dt = r.AsDataTable(); //.Select(f => f.Flatten()).AsDataTable();
            //}
        }

        [Test]
        public static void ToDataTableComplexJson()
        {
            string expected = @"[
  {
    ""fathers_0_id"": 0,
    ""fathers_0_married"": true,
    ""fathers_0_name"": ""John Lee"",
    ""fathers_0_sons_0_age"": 15,
    ""fathers_0_sons_0_name"": ""Ronald"",
    ""fathers_0_sons_0_address_street"": ""abc street"",
    ""fathers_0_sons_0_address_city"": ""edison"",
    ""fathers_0_sons_0_address_state"": ""NJ"",
    ""fathers_0_daughters_0_age"": 7,
    ""fathers_0_daughters_0_name"": ""Amy"",
    ""fathers_0_daughters_1_age"": 29,
    ""fathers_0_daughters_1_name"": ""Carol"",
    ""fathers_0_daughters_2_age"": 14,
    ""fathers_0_daughters_2_name"": ""Barbara"",
    ""fathers_1_id"": 1,
    ""fathers_1_married"": false,
    ""fathers_1_name"": ""Kenneth Gonzalez"",
    ""fathers_2_id"": 2,
    ""fathers_2_married"": false,
    ""fathers_2_name"": ""Larry Lee"",
    ""fathers_2_sons_0_age"": 4,
    ""fathers_2_sons_0_name"": ""Anthony"",
    ""fathers_2_sons_1_age"": 2,
    ""fathers_2_sons_1_name"": ""Donald"",
    ""fathers_2_daughters_0_age"": 7,
    ""fathers_2_daughters_0_name"": ""Elizabeth"",
    ""fathers_2_daughters_1_age"": 15,
    ""fathers_2_daughters_1_name"": ""Betty""
  }
]";
            using (var r = new ChoJSONReader("sample7.json"))
            {
                var dt = r.AsDataTable(); //.Select(f => f.Flatten()).AsDataTable();
                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }


        public class Employee4
        {
            [ChoJSONRecordField]
            public string Department { get; set; }
            [ChoJSONRecordField]
            public string JobTitle { get; set; }
            //[DisplayFormat(DataFormatString = "dd-MM-yyyy")]
            [ChoJSONRecordField(FormatText = "dd-MM-yyyy")]
            public DateTime BirthDate { get; set; }
        }
        [Test]
        public static void CustomDateTimeFormatTest()
        {
            string json = @"{
  'BirthDate': '30-12-2003',
  'Department': 'Furniture',
  'JobTitle': 'Carpenter',
  'FirstName': 'John',
  'LastName': 'Joinery',
}";
            string expected = @"[
  {
    ""BirthDate"": ""2003-12-30T00:00:00"",
    ""Department"": ""Furniture"",
    ""JobTitle"": ""Carpenter""
  }
]";
            using (var r = ChoJSONReader.LoadText(json)
                .WithField("BirthDate", fieldType: typeof(DateTime), formatText: "dd-MM-yyyy")
                .WithField("Department")
                .WithField("JobTitle")
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
            return;

            using (var r = ChoJSONReader<Employee4>.LoadText(json))
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }

        }
        [Test]
        public static void CustomDateTimeFormatPOCOTest()
        {
            string json = @"{
  'BirthDate': '30-12-2003',
  'Department': 'Furniture',
  'JobTitle': 'Carpenter',
  'FirstName': 'John',
  'LastName': 'Joinery',
}";
            string expected = @"[
  {
    ""Department"": ""Furniture"",
    ""JobTitle"": ""Carpenter"",
    ""BirthDate"": ""2003-12-30T00:00:00""
  }
]";
            using (var r = ChoJSONReader<Employee4>.LoadText(json))
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }
        [Test]
        public static void Sample41Test()
        {
            StringBuilder csv = new StringBuilder();
            string expected = @"startTime,endTime,id,iCalUId,isAllDay,isCancelled,isOrganizer,isOnlineMeeting,onlineMeetingProvider,type,location,locationType,organizer,recurrence,attendees
3/15/2019,3/15/2019,AAMkAGViNDU7zAAAAA7zAAAZb2ckAAA=,040000008200E641B4C,False,False,True,False,unknown,singleInstance,,default,Megan Bowen,,1";

            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader()
                )
            {
                using (var r = new ChoJSONReader(@"sample41.json")
                    .WithField("startTime", jsonPath: "$.start.dateTime", isArray: false)
                    .WithField("endTime", jsonPath: "$.end.dateTime", isArray: false)
                    .WithField("id")
                    .WithField("iCalUId")
                    .WithField("isAllDay")
                    .WithField("isCancelled")
                    .WithField("isOrganizer")
                    .WithField("isOnlineMeeting")
                    .WithField("onlineMeetingProvider")
                    .WithField("type")
                    .WithField("location", jsonPath: "$.location.displayname")
                    .WithField("locationType", jsonPath: "$.location.locationType", isArray: false)
                    .WithField("organizer", jsonPath: "$.organizer.emailAddress.name", isArray: false)
                    .WithField("recurrence", jsonPath: "$.recurrence.pattern.type")
                    .WithField("attendees", jsonPath: "$.attendees[*]", valueConverter: o => ((IList)o).Count)
                )
                {
                    w.Write(r);
                }
            }

            string actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class fooString : IChoNotifyRecordFieldRead
        {
            public string time { get; set; }
            public List<double[]> data1m { get; set; }

            public bool AfterRecordFieldLoad(object target, long index, string propName, object value)
            {
                return true;
            }

            public bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value)
            {
                if (propName == nameof(data1m))
                {
                    ((fooString)target).data1m = JsonConvert.DeserializeObject<List<double[]>>(value.FirstOrDefaultEx().ToString());
                    return false;
                }
                return true;
            }

            public bool RecordFieldLoadError(object target, long index, string propName, ref object value, Exception ex)
            {
                return true;
            }
        }
        [Test]
        public static void TestJArrayAsTextInElement()
        {
            string json = @"{
  ""time"": 20200526, 
  ""data1m"": ""[[1590451620,204.73,204.81,204.73,204.81,1.00720100],[1590451680,204.66,204.66,204.58,204.58,1.00000000],[1590452280,204.65,204.83,204.65,204.83,13.74186800],[1590452820,203.75,203.75,203.75,203.75,0.50000000],[1590452880,203.47,203.47,203,203,1.60000000],[1590453000,203.06,203.06,203.06,203.06,4.00000000]]""
}";
            string expected = @"[
  {
    ""time"": ""20200526"",
    ""data1m"": [
      [
        1590451620.0,
        204.73,
        204.81,
        204.73,
        204.81,
        1.007201
      ],
      [
        1590451680.0,
        204.66,
        204.66,
        204.58,
        204.58,
        1.0
      ],
      [
        1590452280.0,
        204.65,
        204.83,
        204.65,
        204.83,
        13.741868
      ],
      [
        1590452820.0,
        203.75,
        203.75,
        203.75,
        203.75,
        0.5
      ],
      [
        1590452880.0,
        203.47,
        203.47,
        203.0,
        203.0,
        1.6
      ],
      [
        1590453000.0,
        203.06,
        203.06,
        203.06,
        203.06,
        4.0
      ]
    ]
  }
]";
            using (var r = ChoJSONReader<fooString>.LoadText(json)
                //.WithField("time")
                //.WithField("data1m", valueConverter: o => JsonConvert.DeserializeObject<List<double[]>>(o as string))
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Packaging
        {
            public string Qty { get; set; }
        }
        public class Company1
        {
            public string Ref { get; set; }
            public double GW { get; set; }
            public List<Packaging> Packaging { get; set; }
        }
        [Test]
        public static void CustomResolverTest()
        {
            string expected = @"[
  {
    ""Ref"": ""ABC123456"",
    ""GW"": 123.45,
    ""Packaging"": [
      {
        ""Qty"": ""5M""
      },
      {
        ""Qty"": ""7M""
      }
    ]
  }
]";
            using (var r = new ChoJSONReader<Company1>("sample16.json")
                .WithField(f => f.Ref)
                .WithFieldForType<Packaging>(f => f.Qty/*, fieldName: "qty"*/, valueConverter: o => o.ToNString() + "M", customSerializer: o =>
                {
                    JsonReader reader = o as JsonReader;
                    JsonSerializer serializer = new JsonSerializer();
                    if (reader.TokenType == JsonToken.Null)
                    {
                        return string.Empty;
                    }
                    else if (reader.TokenType == JsonToken.Integer)
                    {
                        return serializer.Deserialize(reader, typeof(int));
                    }
                    return 0;
                })
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void Json2CSVContainsNewLine()
        {
            string json = @"{
""comment"": ""this is a test line \n \n but still value for this record""
}";
            string expected = @"comment
this is a test line   but still value for this record";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .WithField("comment", valueConverter: o => ((string)o).Replace("\n", ""))
                    )
                {
                    w.Write(r);
                }
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void FlattenJSONTest()
        {
            string json = @"{
  ""Library"": null,
  ""LibraryNumber"": 5,
  ""Author"": {
    ""Year"": null,
    ""Address"": null,
    ""LastName"": null,
    ""AllBooks"": [],
    ""WonNYBestSeller"": false,
    ""NumberofBooks"": 0,
    ""SelfPublished"": false,
    ""AuthorFunFacts"": {
      ""NumberofPets"": 0,
      ""NumberofCars"": 0,
      ""PlaceofBirth"": null,
      ""WrittenChildrenBook"": false
    },
    ""Biography"": null,
    ""Age"": 32,
    ""NumberBestSellers"": 0,
    ""NumberMovies"": 1
  },
  ""AuthorLuckyNumber"": 24,
  ""SearchKeys"": [
    ""Alice"",
    ""AuthorX""
  ],
  ""Website"": null,
  ""BookPublications"": {
    ""FirstPublication"": 1,
    ""LastPublication"": 1
  },
  ""FavoriteQuote"": ""FavoriteQuote1"",
  ""NumberPoems"": 1,
}";
            string expected = @"[
  {
    ""Library"": null,
    ""LibraryNumber"": 5,
    ""Year"": null,
    ""Address"": null,
    ""LastName"": null,
    ""WonNYBestSeller"": false,
    ""NumberofBooks"": 0,
    ""SelfPublished"": false,
    ""NumberofPets"": 0,
    ""NumberofCars"": 0,
    ""PlaceofBirth"": null,
    ""WrittenChildrenBook"": false,
    ""Biography"": null,
    ""Age"": 32,
    ""NumberBestSellers"": 0,
    ""NumberMovies"": 1,
    ""AuthorLuckyNumber"": 24,
    ""SearchKeys/0"": ""Alice"",
    ""SearchKeys/1"": ""AuthorX"",
    ""Website"": null,
    ""FirstPublication"": 1,
    ""LastPublication"": 1,
    ""FavoriteQuote"": ""FavoriteQuote1"",
    ""NumberPoems"": 1
  }
]";
            List<object> output = new List<object>();
            using (var r = ChoJSONReader.LoadText(json))
            {
                foreach (var rec in r)
                    output.Add(rec.ConvertToFlattenObject('/', null, null, true));
            }

            string actual = JsonConvert.SerializeObject(output, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void DictTest5()
        {
            string json = @"{
""Id"": 1,
""Name"": ""Tom""
}";
            using (var r = ChoJSONReader<IDictionary>.LoadText(json))
            {
                var rec = r.FirstOrDefault();
                Assert.AreEqual(rec["Id"], 1);
                Assert.AreEqual(rec["Name"], "Tom");
            }
        }

        [Test]
        public static void ArrayTest1()
        {
            string json = @"[1, 2]";

            string expected = @"[
  1,
  2
]";
            using (var r = ChoJSONReader<int>.LoadText(json))
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void JSON2XmlArrayTest()
        {
            string json = @"{
   ""userName"":[
      ""user1"",
      ""user2""
   ],
   ""referenceNumber"":""098784866589157763"",
   ""responseCode"":""00"",
   ""responseDesc"":""Success.""
}";
            //ChoDynamicObjectSettings.XmlArrayQualifier = (x, y) => true;
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <XElement>
    <userNames>
      <userName>user1</userName>
      <userName>user2</userName>
    </userNames>
    <referenceNumber>098784866589157763</referenceNumber>
    <responseCode>00</responseCode>
    <responseDesc>Success.</responseDesc>
  </XElement>
</Root>";
            StringBuilder xml = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoXmlWriter(xml)
                    .Configure(c => c.XmlArrayQualifier = (x, y) => true)
                    //.WithField("userName", m => m.Configure(c => c.IsArray = false))
                    //.WithField("referenceNumber")
                    )
                    w.Write(r);
            }

            Console.WriteLine(xml.ToString());
            var actual = xml.ToString();
            Assert.AreEqual(expected, actual);
        }

        public abstract class Enumeration : IComparable
        {
            public string Name { get; private set; }

            public int Id { get; private set; }

            protected Enumeration(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public override string ToString() => Name;

            public static IEnumerable<T> GetAll<T>() where T : Enumeration
            {
                var fields = typeof(T).GetFields(BindingFlags.Public |
                                                 BindingFlags.Static |
                                                 BindingFlags.DeclaredOnly);

                return fields.Select(f => f.GetValue(null)).Cast<T>();
            }

            public override bool Equals(object obj)
            {
                var otherValue = obj as Enumeration;

                if (otherValue == null)
                    return false;

                var typeMatches = GetType().Equals(obj.GetType());
                var valueMatches = Id.Equals(otherValue.Id);

                return typeMatches && valueMatches;
            }

            public int CompareTo(object other) => Id.CompareTo(((Enumeration)other).Id);

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            // Other utility methods ...
        }
        public class Dto
        {
            public string Name { get; set; }
            [ChoTypeConverter(typeof(CardTypeConverter))]
            public CardType CardType { get; set; }
        }

        public class StatusTypeConvertor : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var name = value as string;
                if (name == "Active")
                    return StatusType.Active;
                else
                    return StatusType.Inactive;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class CardTypeConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var name = value as string;
                if (name == "Amex")
                    return CardType.Amex;
                if (name == "MasterCard")
                    return CardType.MasterCard;
                else
                    return CardType.Visa;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        public class CardType : Enumeration
        {
            public static readonly CardType Amex = new CardType(1, "Amex");
            public static readonly CardType Visa = new CardType(2, "Visa");
            public static readonly CardType MasterCard = new CardType(3, "MasterCard");

            public CardType(int id, string name)
                : base(id, name)
            {
            }
            public static explicit operator CardType(string name)
            {
                if (name == "Amex")
                    return Amex;
                if (name == "MasterCard")
                    return MasterCard;
                else
                    return Visa;
            }
        }
        public class StatusType : Enumeration
        {
            public static readonly StatusType Active = new StatusType(1, "Active");
            public static readonly StatusType Inactive = new StatusType(2, "Inactive");

            public StatusType(int id, string name) : base(id, name)
            {

            }
            public static explicit operator StatusType(string name)
            {
                if (name == "Active")
                    return Active;
                else
                    return Inactive;
            }
        }
        [Test]
        public static void DeserializeEnumClass()
        {
            string json = @"[
{
    ""Name"": ""Tom"",
    ""CardType"": ""Amex""
}
]";
            string expected = @"{
  ""Name"": ""Tom"",
  ""CardType"": {
    ""Name"": ""Amex"",
    ""Id"": 1
  }
}";
            var x = ChoJSONReader.DeserializeText<Dto>(json).FirstOrDefault();

            var actual = JsonConvert.SerializeObject(x, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine(x.Dump());
            Assert.AreEqual(expected, actual);
        }

        [JsonConverter(typeof(ChoJSONPathConverter))]
        public class MoreDataObj
        {
            [ChoJSONPath("MoreData1.Field3")]
            public int InnerField3 { get; set; }
            public int Field3 { get; set; }
        }
        public class Sample
        {
            public int Field1 { get; set; }
            public int Field2 { get; set; }
            public MoreDataObj MoreData { get; set; }
            public string Field5 { get; set; }
            [ChoJSONPath("MoreData.MoreData1.Field4")]
            public string Field4 { get; set; }
        }
        [Test]
        public static void JSONPathInInnerObjectTest()
        {
            string json = @"{
  ""Field1"": 1234,
  ""Field2"": 5678,
  ""MoreData"": {
    ""Field3"": 9012,
    ""MoreData1"": {
      ""Field3"": 19012,
      ""Field4"": 13456
    }
  },
  ""Field5"": ""Test""
}";
            string expected = @"{
  ""Field1"": 1234,
  ""Field2"": 5678,
  ""MoreData"": {
    ""MoreData1"": {
      ""Field3"": 19012
    },
    ""Field3"": 9012
  },
  ""Field5"": ""Test"",
  ""Field4"": ""13456""
}";
            var rec = ChoJSONReader.DeserializeText<Sample>(json).FirstOrDefault();
            var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
            Console.WriteLine(rec.Dump());
            Assert.AreEqual(expected, actual);
        }

        public abstract class Instrument
        {
            [JsonProperty("ticker")]
            public string Ticker { get; set; }
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("market")]
            public string Market { get; set; }
            [JsonProperty("locale")]
            public string Locale { get; set; }
            [JsonProperty("currency")]
            public string Currency { get; set; }
            [JsonProperty("active")]
            public bool Active { get; set; }
            [JsonProperty("primaryExch")]
            public string PrimaryExch { get; set; }
            public DateTimeOffset Updated { get; set; }
        }
        [JsonConverter(typeof(ChoJsonPathJsonConverter))]
        public class Stock : Instrument
        {
            [JsonProperty("type")]
            public string Type { get; set; }
            [ChoJSONPath("$.codes.cik")]
            public string CIK { get; set; }
            [ChoJSONPath("$.codes.figiuid")]
            public string FIGIUID { get; set; }
            [ChoJSONPath("$.codes.scfigi")]
            public string SCFIGI { get; set; }
            [ChoJSONPath("$.codes.cfigi")]
            public string CFIGI { get; set; }
            [ChoJSONPath("$.codes.figi")]
            public string FIGI { get; set; }
        }

        [JsonConverter(typeof(ChoJsonPathJsonConverter))]
        public class ForeignExchange : Instrument
        {
            [ChoJSONPath("$.attrs.base")]
            public string BaseCurrency { get; set; }
        }

        [Test]
        public static void DesrializeMultipleTypes()
        {
            string json = @"[
  {
    ""ticker"": ""AAPL"",
    ""name"": ""Apple Inc."",
    ""market"": ""STOCKS"",
    ""locale"": ""US"",
    ""currency"": ""USD"",
    ""active"": true,
    ""primaryExch"": ""NGS"",
    ""type"": ""cs"",
    ""codes"": {
      ""cik"": ""0000320193"",
      ""figiuid"": ""EQ0010169500001000"",
      ""scfigi"": ""BBG001S5N8V8"",
      ""cfigi"": ""BBG000B9XRY4"",
      ""figi"": ""BBG000B9Y5X2""
    },
    ""updated"": ""2019-01-15T05:21:28.437Z"",
    ""url"": ""https://api.polygon.io/v2/reference/tickers/AAPL""
  },
  {
    ""ticker"": ""$AEDAUD"",
    ""name"": ""United Arab Emirates dirham - Australian dollar"",
    ""market"": ""FX"",
    ""locale"": ""G"",
    ""currency"": ""AUD"",
    ""active"": true,
    ""primaryExch"": ""FX"",
    ""updated"": ""2019-01-25T00:00:00.000Z"",
    ""attrs"": {
      ""currencyName"": ""Australian dollar,"",
      ""currency"": ""AUD,"",
      ""baseName"": ""United Arab Emirates dirham,"",
      ""base"": ""AED""
    },
    ""url"": ""https://api.polygon.io/v2/tickers/$AEDAUD""
  },
]";
            string expected = @"[
  {
    ""type"": ""cs"",
    ""CIK"": ""0000320193"",
    ""FIGIUID"": ""EQ0010169500001000"",
    ""SCFIGI"": ""BBG001S5N8V8"",
    ""CFIGI"": ""BBG000B9XRY4"",
    ""FIGI"": ""BBG000B9Y5X2"",
    ""ticker"": ""AAPL"",
    ""name"": ""Apple Inc."",
    ""market"": ""STOCKS"",
    ""locale"": ""US"",
    ""currency"": ""USD"",
    ""active"": true,
    ""primaryExch"": ""NGS"",
    ""Updated"": ""0001-01-01T00:00:00+00:00""
  },
  {
    ""BaseCurrency"": ""AED"",
    ""ticker"": ""$AEDAUD"",
    ""name"": ""United Arab Emirates dirham - Australian dollar"",
    ""market"": ""FX"",
    ""locale"": ""G"",
    ""currency"": ""AUD"",
    ""active"": true,
    ""primaryExch"": ""FX"",
    ""Updated"": ""0001-01-01T00:00:00+00:00""
  }
]";
            using (var r = ChoJSONReader<Instrument>.LoadText(json)
                .WithCustomRecordSelector(o =>
                {
                    Tuple<long, JObject> j = o as Tuple<long, JObject>;
                    if (j.Item2.SelectToken("ticker").ToString().StartsWith("$"))
                        return typeof(ForeignExchange);
                    else
                        return typeof(Stock);
                })
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class CTest
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public List<Class2> Details { get; set; }
        }

        public class Class2
        {
            public int Id { get; set; }
            public double Data1 { get; set; }
        }

        [Test]
        public static void CustomNodeSelectorTest()
        {
            string json = @"[
{
  ""id"":5,
  ""name"":""test"",
  ""details"":[
    {
      ""id"":12,
      ""data1"":0.25
    },
    {
      ""id"":0,
      ""data1"":0.0
    },
  ]
},
{
  ""id"":0,
  ""name"":""test"",
  ""details"":[
    {
      ""id"":12,
      ""data1"":0.25
    },
    {
      ""id"":0,
      ""data1"":0.0
    },
  ]
}
]";
            string expected = @"[
  {
    ""Id"": 5,
    ""Name"": ""test"",
    ""Details"": [
      {
        ""Id"": 12,
        ""Data1"": 0.25
      },
      {
        ""Id"": 0,
        ""Data1"": 0.0
      }
    ]
  }
]";
            using (var r = ChoJSONReader<CTest>.LoadText(json)
            .WithCustomNodeSelector(o => o["id"].CastTo<int>() > 0 ? o : null)
            )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void ConditionalSelectionsOfNodes()
        {
            string json = @"[
{
  ""id"":5,
  ""name"":""test"",
  ""details"":[
    {
      ""id"":12,
      ""data1"":0.25
    },
    {
      ""id"":0,
      ""data1"":0.0
    },
  ]
},
{
  ""id"":0,
  ""name"":""test"",
  ""details"":[
    {
      ""id"":12,
      ""data1"":0.25
    },
    {
      ""id"":0,
      ""data1"":0.0
    },
  ]
}
]";
            string expected = @"[
  {
    ""Id"": 5,
    ""Name"": ""test"",
    ""Details"": [
      {
        ""Id"": 12,
        ""Data1"": 0.25
      },
      {
        ""Id"": 0,
        ""Data1"": 0.0
      }
    ]
  },
  {
    ""Id"": 0,
    ""Name"": ""test"",
    ""Details"": [
      {
        ""Id"": 12,
        ""Data1"": 0.25
      },
      {
        ""Id"": 0,
        ""Data1"": 0.0
      }
    ]
  }
]";
            using (var r = ChoJSONReader<CTest>.LoadText(json)
                .RegisterNodeConverterForType<List<Class2>>(o =>
                {
                    var value = o as JToken[];
                    var list = new List<Class2>();
                    foreach (var item in value.OfType<JArray>())
                    {
                        list.AddRange(item.ToObject<Class2[]>());
                    }

                    return list;
                })
            //.WithCustomNodeSelector(o => o["id"].CastTo<int>() > 0 ? o : null)
            )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void ConditionalSelectionsOfNodes_1()
        {
            string json = @"[
{
  ""id"":5,
  ""name"":""test"",
  ""details"":[
    {
      ""id"":12,
      ""data1"":0.25
    },
    {
      ""id"":0,
      ""data1"":0.0
    },
  ]
},
{
  ""id"":0,
  ""name"":""test"",
  ""details"":[
    {
      ""id"":12,
      ""data1"":0.25
    },
    {
      ""id"":0,
      ""data1"":0.0
    },
  ]
}
]";
            string expected = @"[
  {
    ""Id"": 5,
    ""Name"": ""test"",
    ""Details"": [
      {
        ""Id"": 12,
        ""Data1"": 0.25
      },
      {
        ""Id"": 0,
        ""Data1"": 0.0
      }
    ]
  },
  {
    ""Id"": 0,
    ""Name"": ""test"",
    ""Details"": [
      {
        ""Id"": 12,
        ""Data1"": 0.25
      },
      {
        ""Id"": 0,
        ""Data1"": 0.0
      }
    ]
  }
]";
            using (var r = ChoJSONReader<CTest>.LoadText(json)
                .RegisterNodeConverterForType<CTest>(o =>
                {
                    var value = o as JToken;
                    return value.ToObject<CTest>();
                })
            //.WithCustomNodeSelector(o => o["id"].CastTo<int>() > 0 ? o : null)
            )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void JSON2DataTable2()
        {
            string json = @"{
  ""data"": {
    ""utime"": ""2020-07-22 16:02:39.628"",
    ""record"": [
      {
        ""samt"": 0.0,
        ""itms"": [
          {
            ""num"": 1.0,
            ""itm_det"": {
              ""samt"": 0.0,
              ""csamt"": 0.5,
              ""rt"": 18.0,
              ""txval"": 15000.0,
              ""camt"": 0.0,
              ""iamt"": 2700.0
            }
          }
        ],
        ""val"": 20000.01,
        ""txval"": 15000.0,
        ""camt"": 0.0,
        ""inum"": ""Manjusha-GSTR1"",
        ""iamt"": 2700.0,
        ""csamt"": 0.5,
        ""inv_typ"": ""R"",
        ""pos"": ""12"",
        ""idt"": ""16-07-2017"",
        ""rchrg"": ""N"",
        ""chksum"": ""23bd7b0296c66900d9b89a7af16facf08bd68a9aa7e0ddb7c7f9aa8d5dd1431e"",
        ""ctin"": ""27GSPMH0781G1ZK"",
        ""cfs"": ""Y""
      }
    ],
    ""ttl_record"": 8,
    ""fp"": ""062018"",
    ""gstin"": ""33AIYPV3847J1ZC""
  },
  ""meta"": {
    ""form"": ""6a"",
    ""level"": ""L2"",
    ""fp"": ""062018"",
    ""section"": ""b2b"",
    ""gstin"": ""33AIYPV3847J1ZC"",
    ""flush"": ""false""
  }
}";
            string expected = @"[
  {
    ""data_utime"": ""2020-07-22 16:02:39.628"",
    ""data_record_0_samt"": 0.0,
    ""data_record_0_itms_0_num"": 1.0,
    ""data_record_0_itms_0_itm_det_samt"": 0.0,
    ""data_record_0_itms_0_itm_det_csamt"": 0.5,
    ""data_record_0_itms_0_itm_det_rt"": 18.0,
    ""data_record_0_itms_0_itm_det_txval"": 15000.0,
    ""data_record_0_itms_0_itm_det_camt"": 0.0,
    ""data_record_0_itms_0_itm_det_iamt"": 2700.0,
    ""data_record_0_val"": 20000.01,
    ""data_record_0_txval"": 15000.0,
    ""data_record_0_camt"": 0.0,
    ""data_record_0_inum"": ""Manjusha-GSTR1"",
    ""data_record_0_iamt"": 2700.0,
    ""data_record_0_csamt"": 0.5,
    ""data_record_0_inv_typ"": ""R"",
    ""data_record_0_pos"": ""12"",
    ""data_record_0_idt"": ""16-07-2017"",
    ""data_record_0_rchrg"": ""N"",
    ""data_record_0_chksum"": ""23bd7b0296c66900d9b89a7af16facf08bd68a9aa7e0ddb7c7f9aa8d5dd1431e"",
    ""data_record_0_ctin"": ""27GSPMH0781G1ZK"",
    ""data_record_0_cfs"": ""Y"",
    ""data_ttl_record"": 8,
    ""data_fp"": ""062018"",
    ""data_gstin"": ""33AIYPV3847J1ZC"",
    ""meta_form"": ""6a"",
    ""meta_level"": ""L2"",
    ""meta_fp"": ""062018"",
    ""meta_section"": ""b2b"",
    ""meta_gstin"": ""33AIYPV3847J1ZC"",
    ""meta_flush"": ""false""
  }
]";
            var dt = ChoJSONReader.LoadText(json).AsDataTable();

            var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        public struct GeographyPoint
        {
            public int Latitude { get; }
            public int Longitude { get; }

            [JsonConstructor]
            public GeographyPoint(int lat, int lon)
            {
                Latitude = lat;
                Longitude = lon;
            }
        }

        public class LookUpData
        {
            [DataMember]
            public string Id { get; set; }
            [DataMember]
            [ChoCustomSerializer(typeof(GeographyPointConverter))]
            public GeographyPoint Location { get; set; }
        }
        public class GeographyPointConverter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return new GeographyPoint((int)((JObject)value)["Latitude"], (int)((JObject)value)["Longitude"]);
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        [Test]
        public static void CustomObjectInstance_1()
        {
            string json = @"{
    ""Id"": ""123"",
    ""Location"": {
      ""Latitude"": 1,
      ""Longitude"": -1,
      }
  }";

            string expected = @"[
  {
    ""Id"": ""123"",
    ""Location"": {
      ""Latitude"": 1,
      ""Longitude"": -1
    }
  }
]";
            using (var r = ChoJSONReader<LookUpData>.LoadText(json)
                //.WithField(f => f.Id)
                //.WithField(f => f.Location, customSerializer: o => o)
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class LookUpData2
        {
            [DataMember]
            public string Id { get; set; }
            [DataMember]
            public GeographyPoint Location { get; set; }
        }
        [Test]
        public static void CustomObjectInstance_2()
        {
            string json = @"{
    ""Id"": ""123"",
    ""Location"": {
      ""Latitude"": 1,
      ""Longitude"": -1,
      }
  }";

            string expected = @"[
  {
    ""Id"": ""123"",
    ""Location"": {
      ""Latitude"": 1,
      ""Longitude"": -1
    }
  }
]";
            using (var r = ChoJSONReader<LookUpData2>.LoadText(json)
                .WithField(f => f.Id)
                .WithField(f => f.Location, customSerializer: o => new GeographyPoint((int)((JObject)o)["Latitude"], (int)((JObject)o)["Longitude"]))
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class LookUpData3
        {
            [DataMember]
            public string Id { get; set; }
            [DataMember]
            public GeographyPoint3 Location { get; set; }
        }
        public struct GeographyPoint3
        {
            public int Latitude { get; set; }
            public int Longitude { get; set; }

            public GeographyPoint3(int lat, int lon)
            {
                Latitude = lat;
                Longitude = lon;
            }
        }
        [Test]
        public static void CustomObjectInstance_3()
        {
            string json = @"{
    ""Id"": ""123"",
    ""Location"": {
      ""Latitude"": 1,
      ""Longitude"": -1,
      }
  }";

            string expected = @"[
  {
    ""Id"": ""123"",
    ""Location"": {
      ""Latitude"": 1,
      ""Longitude"": -1
    }
  }
]";
            using (var r = ChoJSONReader<LookUpData3>.LoadText(json)
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void ProgramaticSetup()
        {
            string json = @"{
   ""getUsers"":[
      {
         ""UserInformation"":{
            ""Id"":1111122,
            ""firstName"":""*****1"",
            ""UserType"":{
               ""name"":""CP""
            },
            ""primaryState"":""MA"",
            ""otherState"":[
               ""MA"",
               ""BA""
            ],
            ""createdAt"":""2019-04-21 08:57:53""
         }
      },
      {
         ""UserInformation"":{
            ""Id"":3333,
            ""firstName"":""*****3"",
            ""UserType"":{
               ""name"":""CPP""
            },
            ""primaryState"":""MPA"",
            ""otherState"":[
               ""KL"",
               ""TN""
            ],
            ""createdAt"":null
         }
      }
   ]
}";
            string expected = @"Id,FirstName,UserType,primaryState,otherState_0,otherState_1,createdAt
1111122,*****1,CP,MA,MA,BA,2019-04-21 08:57:53
3333,*****3,CPP,MPA,KL,TN,";

            StringBuilder csv = new StringBuilder();

            var config = new ChoJSONRecordConfiguration();
            config.JSONPath = "$..getUsers[*].UserInformation";
            config.AllowComplexJSONPath = true;

            var idFieldConfig = new ChoJSONRecordFieldConfiguration("Id");

            config.JSONRecordFieldConfigurations.Add(idFieldConfig);
            config.JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration("FirstName"));
            var userTypeRC = new ChoJSONRecordFieldConfiguration("UserType", "$.UserType.name");
            userTypeRC.IsArray = false;
            config.JSONRecordFieldConfigurations.Add(userTypeRC);
            config.JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration("primaryState"));
            config.JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration("otherState", "$.otherState[*]") { FieldType = typeof(string[]) });
            config.JSONRecordFieldConfigurations.Add(new ChoJSONRecordFieldConfiguration("createdAt"));

            using (var r = ChoJSONReader.LoadText(json, config))
            {
                using (var w = new ChoCSVWriter(csv).WithFirstLineHeader()
                    .UseNestedKeyFormat(true)
                    )
                    w.Write(r);
            }

            Console.WriteLine(csv.ToString());
            string actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class InsertIntoDbEntity
        {
            public string tableName { get; set; }
            public InsertIntoDbColumnsValuesEntity columnsValues { get; set; }

            public InsertIntoDbEntity()
            {
                columnsValues = new InsertIntoDbColumnsValuesEntity();
            }
        }

        public class InsertIntoDbColumnsValuesEntity
        {
            public Dictionary<string, string> columnsValues { get; set; }
        }

        [Test]
        public static void JSONTest1()
        {
            string json = @"{
    ""tableName"": ""ApiTestTbl"",
    ""columnsValues"": {
        ""test1"": ""value1"",
        ""column2"": ""value2""
    }
}";
            string expected = @"[
  {
    ""tableName"": ""ApiTestTbl"",
    ""columnsValues"": {
      ""columnsValues"": {
        ""test1"": ""value1"",
        ""column2"": ""value2""
      }
    }
  }
]";
            var config = new ChoJSONRecordConfiguration<InsertIntoDbEntity>();
            config.Map(f => f.tableName);
            config.Map(f => f.columnsValues.columnsValues, "$..columnsValues");

            using (var r = ChoJSONReader<InsertIntoDbEntity>.LoadText(json, config)
                //.WithField(f => f.columnsValues.columnsValues, jsonPath: "$..columnsValues")
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void JSON2CSV2()
        {
            string json = @"{
""GpsLocation"": {
        ""Equipment"": [
            {
                ""EquipmentId"": ""EQ00001"",
                ""InquiryValue"": [
                    ""IV00001""
                ],
                ""Timestamp"": ""2020-02-01 01:01:01.01"",
            },
            {
                ""EquipmentId"": ""EQ00002"",
                ""InquiryValue"": [
                    ""IV00002""
                ],
                ""Timestamp"": ""2020-01-01 01:01:01.01""
            }
        ]
    }
}";
            string expected = @"EquipmentId,InquiryValue,Timestamp
EQ00001,IV00001,2/1/2020
EQ00002,IV00002,1/1/2020";
            StringBuilder csv = new StringBuilder();

            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$.GpsLocation.Equipment")
                .WithField("EquipmentId")
                .WithField("InquiryValue", jsonPath: "InquiryValue[0]", isArray: false)
                .WithField("Timestamp", fieldType: typeof(DateTime))
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader())
                    w.Write(r);
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class OrderSystem
        {
            [JsonProperty("created")]
            public string Created { get; set; }

            [JsonProperty("by")]
            public string By { get; set; }
        }

        public class OrderLocation
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("country")]
            public string Country { get; set; }
        }

        public class OrderArticle
        {
            [JsonProperty("Size")]
            public int Size { get; set; }

            [JsonProperty("ProductName")]
            public string ProductName { get; set; }

            [JsonProperty("ProductId")]
            public string ProductId { get; set; }
        }

        public class Order
        {
            [JsonProperty("OrderID")]
            public int OrderID { get; set; }

            [JsonProperty("OrderName")]
            public string OrderName { get; set; }

            [JsonProperty("OrderArticles")]
            public List<OrderArticle> OrderArticles { get; set; }

            [JsonProperty("ProcessedId")]
            public int ProcessedId { get; set; }

            [JsonProperty("Date")]
            public string Date { get; set; }
        }

        public class OrderRoot
        {
            [JsonProperty("system")]
            public OrderSystem System { get; set; }

            [JsonProperty("location")]
            public OrderLocation Location { get; set; }

            [JsonProperty("order")]
            public List<Order> Orders { get; set; }
        }
        [Test]
        public static void Sample55Test1()
        {
            string expected = @"Created;By;Id;OrderID;OrderName;Size;ProductName;ProductId
2021-08-01T13:33:37.123Z;web;100;22;Soda;33;Coke;999
2021-08-01T13:33:37.123Z;web;100;22;Soda;66;Fanta;888
2021-08-01T13:33:37.123Z;web;100;22;Soda;50;Pepsi;444
2021-08-01T13:33:37.123Z;web;100;23;Beverage;44;Coke;999";

            StringBuilder csv = new StringBuilder();
            using (var r = new ChoJSONReader<OrderRoot>("sample55.json")
                .UseJsonSerialization()
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithDelimiter(";")
                    .WithFirstLineHeader())
                {
                    w.Write(r.SelectMany(root =>
                        root.Orders
                        .SelectMany(order => order.OrderArticles
                        .Select(orderarticle => new
                        {
                            root.System.Created,
                            root.System.By,
                            root.Location.Id,
                            order.OrderID,
                            order.OrderName,
                            orderarticle.Size,
                            orderarticle.ProductName,
                            orderarticle.ProductId,
                        })
                            )
                        )
                    );
                }
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Sample55Test2()
        {
            string expected = @"created;by;id;country;OrderID;OrderName;Size;ProductName;ProductId
8/1/2021 1:33:37 PM;web;100;DE;22;Soda;33;Coke;999
8/1/2021 1:33:37 PM;web;100;DE;22;Soda;66;Fanta;888
8/1/2021 1:33:37 PM;web;100;DE;22;Soda;50;Pepsi;444
8/1/2021 1:33:37 PM;web;100;DE;23;Beverage;44;Coke;999";

            StringBuilder csv = new StringBuilder();
            using (var r = new ChoJSONReader("sample55.json")
                .WithField("created", jsonPath: "$..system.created", isArray: false, fieldType: typeof(string))
                .WithField("by", jsonPath: "$..system.by", isArray: false)
                .WithField("id", jsonPath: "$..location.id", isArray: false)
                .WithField("country", jsonPath: "$..location.country", isArray: false)
                .WithField("OrderID")
                .WithField("OrderName")
                .WithField("Size")
                .WithField("ProductName")
                .WithField("ProductId")
                .Configure(c => c.FlattenNode = true)
                .Configure(c => c.UseNestedKeyFormat = false)
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithDelimiter(";")
                    .WithFirstLineHeader())
                {
                    w.Write(r);
                }
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Sample55Test()
        {
            string expected = @"created;by;id;OrderID;OrderName;Size;ProductName;ProductId
2021-08-01T01:33:37.123Z;web;100;22;Soda;33;Coke;999
2021-08-01T01:33:37.123Z;web;100;22;Soda;66;Fanta;888
2021-08-01T01:33:37.123Z;web;100;22;Soda;50;Pepsi;444
2021-08-01T01:33:37.123Z;web;100;23;Beverage;44;Coke;999";
            StringBuilder csv = new StringBuilder();
            using (var r = new ChoJSONReader("sample55.json"))
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithDelimiter(";")
                    .WithFirstLineHeader())
                {
                    w.Write(r.SelectMany(root =>
                        ((Array)root.order).Cast<dynamic>()
                        .SelectMany(order => ((IList)order.OrderArticles).Cast<dynamic>()
                        .Select(orderarticle => new
                        {
                            created = ((DateTime)root.system.created).ToString("yyyy-MM-ddThh:mm:ss.fffZ"),
                            root.system.by,
                            root.location.id,
                            order.OrderID,
                            order.OrderName,
                            orderarticle.Size,
                            orderarticle.ProductName,
                            orderarticle.ProductId,
                        })
                            )
                        )
                    );
                }
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Sample42Test()
        {
            string expected = @"Comment|File_Version|File_FileTransfer_CreateDate|Data_0_DataID|Data_0_Process_StartDate|Data_0_Process_Detail_Type|Data_0_Process_Detail_Result_Success|Data_0_Process_Detail_Result_ExamineBy|Data_0_Process_Detail_Execution_0_Description|Data_0_Process_Detail_Execution_0_ExcecuteBy
this is json file.|1.0|10-08-2020|A50000|01-07-2020|C|True|Desmond||Alice
this is json file.|1.0|10-08-2020|A50000|01-07-2020|D|False|Desmond|description here|Alice
this is json file.|1.0|10-08-2020|A50000|02-07-2020|G|True|Dan||Alice
this is json file.|1.0|10-08-2020|A50000|02-07-2020|H|True|Dan|description here|Alice
this is json file.|1.0|10-08-2020|A50000|02-07-2020|J|False|Dan|description here|Alice";

            StringBuilder csv = new StringBuilder();
            using (var r = new ChoJSONReader("sample42.json"))
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithDelimiter("|")
                    .WithFirstLineHeader())
                {
                    w.Write(r.SelectMany(r1 =>
                        ((Array)r1.Data).Cast<dynamic>()
                        .SelectMany(d => ((IList)d.Process).Cast<dynamic>()
                        .SelectMany(p => ((IList)p.Detail).Cast<dynamic>()
                        .Select(d1 => new
                        {
                            r1.Comment,
                            r1.File,
                            Data_0_DataID = d.DataID,
                            Data_0_Process_StartDate = p.StartDate,
                            Data_0_Process_Detail = d1
                        })
                                )
                            )
                        )
                    );
                }
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        public class CreateRequest
        {
            public long code { get; set; }
            public string message { get; set; }
            [JsonProperty("class")]
            public string class1 { get; set; }
            public string key { get; set; }
            public Fields fields { get; set; }
        }
        public class Fields
        {
            [JsonProperty("ref")]
            public string refe { get; set; }
            public string org_id { get; set; }
        }
        [Test]
        public static void DesrializeSelectiveNode()
        {
            string json = @"{
    ""objects"": {
        ""UserRequest::567"": {
            ""code"": 1,
            ""message"": ""created"",
            ""class"": ""UserRequest"",
            ""key"": ""567"",
            ""fields"": {
                ""ref"": ""R-000567"",
                ""org_id"": ""4""
            }
        }
    }
}";
            string expected = @"[
  {
    ""code"": 1,
    ""message"": ""created"",
    ""class"": ""UserRequest"",
    ""key"": ""567"",
    ""fields"": {
      ""ref"": ""R-000567"",
      ""org_id"": ""4""
    }
  }
]";
            using (var r = ChoJSONReader<CreateRequest>.LoadText(json)
                .WithJSONPath("$.objects.*", true)
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public interface IShape
        {

        }
        public class Polygon
        {
            [JsonProperty("coordinates")]
            public double[][][] Coords { get; set; }
        }

        public class Point
        {
            [JsonProperty("coordinates")]
            public double[] Coords { get; set; }
        }

        [Test]
        public static void DesrializeMoreThanOneType1()
        {
            string polyJson = @"{
  ""geom"": {
    ""coordinates"": [
      [
        [
          0.07666021585464479,
          51.49331798632394
        ],
        [
          0.07707864046096803,
          51.49337476490021
        ],
        [
          0.07717519998550416,
          51.49315433003204
        ],
        [
          0.07676750421524049,
          51.49309087131179
        ],
        [
          0.07666021585464479,
          51.49331798632394
        ]
      ]
    ],
    ""type"": ""Polygon""
  }
}
";
            string expected = @"{
  ""coordinates"": [
    [
      [
        0.076660215854644789,
        51.493317986323937
      ],
      [
        0.077078640460968031,
        51.49337476490021
      ],
      [
        0.077175199985504164,
        51.493154330032041
      ],
      [
        0.076767504215240492,
        51.493090871311793
      ],
      [
        0.076660215854644789,
        51.493317986323937
      ]
    ]
  ]
}";
            var config = new ChoJSONRecordConfiguration()
                .Configure(c => c.JSONPath = "$.geom")
                .Configure(c => c.SupportsMultiRecordTypes = true)
                .Configure(c => c.RecordTypeSelector = o =>
                {
                    var tuple = o as Tuple<long, JObject>;
                    var jObj = tuple.Item2;

                    if (jObj["type"].ToString() == "Polygon")
                        return typeof(Polygon);
                    else
                        return typeof(Point);
                })
                ;

            object rec = ChoJSONReader.DeserializeText(polyJson, null, config).FirstOrDefault();
            Console.WriteLine(rec.Dump());

            var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DesrializeMoreThanOneType2()
        {
            string expected = @"{
  ""coordinates"": [
    -0.00203667,
    51.51020028
  ]
}";

            string pointJson = @"{
    ""geom"": {
        ""coordinates"": [
            -0.00203667,
            51.51020028
        ],
        ""type"": ""Point""
    }
}";
            var config = new ChoJSONRecordConfiguration()
                .Configure(c => c.JSONPath = "$.geom")
                .Configure(c => c.SupportsMultiRecordTypes = true)
                .Configure(c => c.RecordTypeSelector = o =>
                {
                    var tuple = o as Tuple<long, JObject>;
                    var jObj = tuple.Item2;

                    if (jObj["type"].ToString() == "Polygon")
                        return typeof(Polygon);
                    else
                        return typeof(Point);
                })
                ;

            var rec = ChoJSONReader.DeserializeText(pointJson, null, config).FirstOrDefault();
            var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }
        public class Geom
        {
            //[JsonProperty("coordinates")]
            public double[] Coords { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }
        }
        [Test]
        public static void DesrializeMoreThanOneType3()
        {
            string polyJson = @"{
  ""geom"": {
    ""coordinates"": [
      [
        [
          0.07666021585464479,
          51.49331798632394
        ],
        [
          0.07707864046096803,
          51.49337476490021
        ],
        [
          0.07717519998550416,
          51.49315433003204
        ],
        [
          0.07676750421524049,
          51.49309087131179
        ],
        [
          0.07666021585464479,
          51.49331798632394
        ]
      ]
    ],
    ""type"": ""Polygon""
  }
}
";

            string expected = @"{
  ""Coords"": [
    0.076660215854644789,
    51.493317986323937,
    0.077078640460968031,
    51.49337476490021,
    0.077175199985504164,
    51.493154330032041,
    0.076767504215240492,
    51.493090871311793,
    0.076660215854644789,
    51.493317986323937
  ],
  ""type"": ""Polygon""
}";
            var config = new ChoJSONRecordConfiguration<Geom>()
                .Configure(c => c.JSONPath = "$.geom")
                .Map(f => f.Coords, m => m.CustomSerializer(o =>
                {
                    var jObj = o as JObject;
                    var type = jObj["type"].ToString();

                    if (type == "Polygon")
                        return jObj["coordinates"].ToObject<double[][][]>().SelectMany(x1 => x1.SelectMany(x2 => x2)).ToArray();
                    else
                        return jObj["coordinates"].ToObject<double[]>();
                })
                    )
                ;
            object rec = ChoJSONReader.DeserializeText<Geom>(polyJson, null, config).FirstOrDefault();
            Console.WriteLine(rec.Dump());
            var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DesrializeMoreThanOneType4()
        {
            string polyJson = @"{
  ""geom"": {
    ""coordinates"": [
      [
        [
          0.07666021585464479,
          51.49331798632394
        ],
        [
          0.07707864046096803,
          51.49337476490021
        ],
        [
          0.07717519998550416,
          51.49315433003204
        ],
        [
          0.07676750421524049,
          51.49309087131179
        ],
        [
          0.07666021585464479,
          51.49331798632394
        ]
      ]
    ],
    ""type"": ""Polygon""
  }
}
";

            string expected = @"{
  ""Coords"": [
    -0.00203667,
    51.51020028
  ],
  ""type"": ""Point""
}";
            var config = new ChoJSONRecordConfiguration<Geom>()
                .Configure(c => c.JSONPath = "$.geom")
                .Map(f => f.Coords, m => m.CustomSerializer(o =>
                {
                    var jObj = o as JObject;
                    var type = jObj["type"].ToString();

                    if (type == "Polygon")
                        return jObj["coordinates"].ToObject<double[][][]>().SelectMany(x1 => x1.SelectMany(x2 => x2)).ToArray();
                    else
                        return jObj["coordinates"].ToObject<double[]>();
                })
                    )
                ;


            string pointJson = @"{
    ""geom"": {
        ""coordinates"": [
            -0.00203667,
            51.51020028
        ],
        ""type"": ""Point""
    }
}";

            var rec = ChoJSONReader.DeserializeText<Geom>(pointJson, null, config).FirstOrDefault();
            var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }
        public class StaticCar
        {
            public static StaticCar Instance = new StaticCar();

            public int Id { get; set; }
            public string Name { get; set; }
            public string Brand { get; set; }

            private StaticCar()
            {

            }
        }
        [Test]
        public static void StaticClassSerialization()
        {
            string carJson = @"
    [
        {
            ""Id"": 1,
            ""Name"": ""Polo"",
            ""Brand"": ""Volkswagen""
        },
        {
            ""Id"": 2,
            ""Name"": ""328"",
            ""Brand"": ""BMW""
        }
    ]";
            string expected1 = @"{
  ""Id"": 1,
  ""Name"": ""Polo"",
  ""Brand"": ""Volkswagen""
}";
            string expected2 = @"{
  ""Id"": 2,
  ""Name"": ""328"",
  ""Brand"": ""BMW""
}";
            int index = 0;
            foreach (var c in ChoJSONReader.DeserializeText(carJson).Select(o => o.ConvertToObject(typeof(StaticCar))))
            {
                var actual = JsonConvert.SerializeObject(c, Newtonsoft.Json.Formatting.Indented);
                if (index == 0)
                    Assert.AreEqual(expected1, actual);
                else
                    Assert.AreEqual(expected2, actual);

                index++;
            }
        }

        public class Item1
        {
            [ChoJSONPath("$.Value.[0]")]
            public string Key { get; set; }
            [ChoJSONPath("$.Value.[1][0]")]
            public string Value { get; set; }
        }
        [Test]
        public static void DeseializeArrayToObjects()
        {
            string json = @"[
  [
    ""NameA"",
    [
      ""AAA""
    ]
  ],
  [
    ""NameB"",
    [
      ""BBB""
    ]
  ],
  [
    ""NameC"",
    [
      ""CCC""
    ]
  ]
]";
            string expected = @"[
  {
    ""Key"": ""NameA"",
    ""Value"": ""AAA""
  },
  {
    ""Key"": ""NameB"",
    ""Value"": ""BBB""
  },
  {
    ""Key"": ""NameC"",
    ""Value"": ""CCC""
  }
]";
            var recs = ChoJSONReader.DeserializeText<Item1>(json).ToArray();
            foreach (var rec in recs)
                Console.WriteLine(rec.Dump());

            var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
            return;

            using (var r = ChoJSONReader<Item1>.LoadText(json)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                using (var w = new ChoJSONWriter<Item1>(Console.Out))
                    w.Write(r);
            }
        }

        public class DbRowObject
        {
            [ChoArrayIndex(0)]
            public string Item1 { get; set; }
            [ChoArrayIndex(1)]
            public string Item2 { get; set; }
            [ChoArrayIndex(2)]
            public string Item3 { get; set; }
            [ChoArrayIndex(3)]
            public int Item4 { get; set; }
            [ChoArrayIndex(4)]
            public int Item5 { get; set; }
        }

        public class DbObject
        {
            [ChoJSONPath("data.rows[*]")]
            [ChoTypeConverter(typeof(ChoArrayToObjectConverter))]
            public DbRowObject[] DbRows { get; set; }
            [ChoJSONPath("database_id")]
            public int DbId { get; set; }
            [ChoJSONPath("row_count")]
            public int RowCount { get; set; }
        }
        [Test]
        public static void DeserializeInnerArrayToObjects1()
        {
            string json = @"{
""database_id"": 9,
""row_count"": 2,
""data"": {
    ""rows"": [
        [
            ""242376_dpi65990"",
            ""ppo"",
            ""2020-08-01T00:00:00.000Z"",
            8,
            8
        ],
        [
            ""700328_dpi66355"",
            ""ppo"",
            ""2020-08-01T00:00:00.000Z"",
            9,
            6
        ]
    ]
  }
}";
            string expected = @"[
  {
    ""DbRows"": [
      {
        ""Item1"": ""242376_dpi65990"",
        ""Item2"": ""ppo"",
        ""Item3"": ""8/1/2020 12:00:00 AM"",
        ""Item4"": 8,
        ""Item5"": 8
      },
      {
        ""Item1"": ""700328_dpi66355"",
        ""Item2"": ""ppo"",
        ""Item3"": ""8/1/2020 12:00:00 AM"",
        ""Item4"": 9,
        ""Item5"": 6
      }
    ],
    ""DbId"": 9,
    ""RowCount"": 2
  }
]";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader<DbObject>.LoadText(json)
                //.WithField(f => f.DbRows, m => m.Configure(c  => c.UseJSONSerialization = true))
                //.WithField(f => f.DbRows, m => m.Configure(c => c.AddConverter(ChoArrayToObjectConverter.Instance)).Configure(c => c.JSONPath = "data.rows[*]"))
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                //return;
                //using (var w = new ChoJSONWriter<DbObject>(jsonOut)
                //    //.WithField(f => f.DbRows, m => m.Configure(c => c.AddConverter(ChoArrayToObjectConverter.Instance)))
                //    )
                //    w.Write(r);
            }

        }

        public class DbObject2
        {
            [ChoJSONPath("data.rows[*]")]
            public DbRowObject[] DbRows { get; set; }
            [ChoJSONPath("database_id")]
            public int DbId { get; set; }
            [ChoJSONPath("row_count")]
            public int RowCount { get; set; }
        }
        [Test]
        public static void DeserializeInnerArrayToObjects2()
        {
            string json = @"{
""database_id"": 9,
""row_count"": 2,
""data"": {
    ""rows"": [
        [
            ""242376_dpi65990"",
            ""ppo"",
            ""2020-08-01T00:00:00.000Z"",
            8,
            8
        ],
        [
            ""700328_dpi66355"",
            ""ppo"",
            ""2020-08-01T00:00:00.000Z"",
            9,
            6
        ]
    ]
  }
}";
            string expected = @"[
  {
    ""DbRows"": [
      {
        ""Item1"": ""242376_dpi65990"",
        ""Item2"": ""ppo"",
        ""Item3"": ""8/1/2020 12:00:00 AM"",
        ""Item4"": 8,
        ""Item5"": 8
      },
      {
        ""Item1"": ""700328_dpi66355"",
        ""Item2"": ""ppo"",
        ""Item3"": ""8/1/2020 12:00:00 AM"",
        ""Item4"": 9,
        ""Item5"": 6
      }
    ],
    ""DbId"": 9,
    ""RowCount"": 2
  }
]";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader<DbObject2>.LoadText(json)
                //.WithField(f => f.DbRows, m => m.Configure(c  => c.UseJSONSerialization = true))
                .WithField(f => f.DbRows, m => m.Configure(c => c.AddConverter(ChoArrayToObjectConverter.Instance)).Configure(c => c.JSONPath = "data.rows[*]"))
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                //return;
                //using (var w = new ChoJSONWriter<DbObject>(jsonOut)
                //    //.WithField(f => f.DbRows, m => m.Configure(c => c.AddConverter(ChoArrayToObjectConverter.Instance)))
                //    )
                //    w.Write(r);
            }

        }
        [Test]
        public static void DeserializeInnerArrayToObjects3()
        {
            string json = @"{
""database_id"": 9,
""row_count"": 2,
""data"": {
    ""rows"": [
        [
            ""242376_dpi65990"",
            ""ppo"",
            ""2020-08-01T00:00:00.000Z"",
            8,
            8
        ],
        [
            ""700328_dpi66355"",
            ""ppo"",
            ""2020-08-01T00:00:00.000Z"",
            9,
            6
        ]
    ]
  }
}";
            string expected = @"[
  {
    ""DbRows"": [
      [
        ""242376_dpi65990"",
        ""ppo"",
        ""8/1/2020 12:00:00 AM"",
        8,
        8
      ],
      [
        ""700328_dpi66355"",
        ""ppo"",
        ""8/1/2020 12:00:00 AM"",
        9,
        6
      ]
    ],
    ""DbId"": 9,
    ""RowCount"": 2
  }
]";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader<DbObject>.LoadText(json)
                )
            {
                using (var w = new ChoJSONWriter<DbObject>(jsonOut)
                    //.WithField(f => f.DbRows, m => m.Configure(c => c.AddConverter(ChoArrayToObjectConverter.Instance)))
                    )
                    w.Write(r);
            }
            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);

        }
        [Test]
        public static void DeserializeInnerArrayToObjects4()
        {
            string json = @"{
""database_id"": 9,
""row_count"": 2,
""data"": {
    ""rows"": [
        [
            ""242376_dpi65990"",
            ""ppo"",
            ""2020-08-01T00:00:00.000Z"",
            8,
            8
        ],
        [
            ""700328_dpi66355"",
            ""ppo"",
            ""2020-08-01T00:00:00.000Z"",
            9,
            6
        ]
    ]
  }
}";
            string expected = @"[
  {
    ""DbRows"": [
      [
        ""242376_dpi65990"",
        ""ppo"",
        ""8/1/2020 12:00:00 AM"",
        8,
        8
      ],
      [
        ""700328_dpi66355"",
        ""ppo"",
        ""8/1/2020 12:00:00 AM"",
        9,
        6
      ]
    ],
    ""DbId"": 9,
    ""RowCount"": 2
  }
]";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader<DbObject2>.LoadText(json)
                .WithField(f => f.DbRows, m => m.Configure(c => c.AddConverter(ChoArrayToObjectConverter.Instance)).Configure(c => c.SourceType = typeof(object[])))
                )
            {
                using (var w = new ChoJSONWriter<DbObject2>(jsonOut)
                    .WithField(f => f.DbRows, m => m.Configure(c => c.AddConverter(ChoArrayToObjectConverter.Instance)).Configure(c => c.SourceType = typeof(object[])))
                    )
                    w.Write(r);
            }
            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);

        }
        public class Data
        {
            public int Sts { get; set; }
            public int TMtd { get; set; }
            public int SId { get; set; }
            public int T { get; set; }
            public int CCSr { get; set; }
            public int TId { get; set; }
            public int UId { get; set; }
            public int NPro { get; set; }
            [ChoJSONPath("^P*")]
            public List<Element> PValues { get; set; }
        }

        public class Element
        {
            public string SKUId { get; set; }
            public int Q { get; set; }
        }
        [Test]
        public static void DesrializeSomeMembersToCollection()
        {
            string json = @"{
  ""Sts"": 1,
  ""TMtd"": 2,
  ""SId"": 215,
  ""T"": 1599453168,
  ""CCSr"": 98972,
  ""TId"": 492,
  ""UId"": 1687,
  ""NPro"": 3,
  ""P1"": {
    ""SKUId"": ""006920180209601"",
    ""Q"": 1
  },
  ""P2"": {
    ""SKUId"": ""006954767430522"",
    ""Q"": 1
  },
  ""P3"": {
    ""SKUId"": ""006954767410623"",
    ""Q"": 1
  }
}";
            string expected = @"[
  {
    ""Sts"": 1,
    ""TMtd"": 2,
    ""SId"": 215,
    ""T"": 1599453168,
    ""CCSr"": 98972,
    ""TId"": 492,
    ""UId"": 1687,
    ""NPro"": 3,
    ""PValues"": [
      {
        ""SKUId"": ""006920180209601"",
        ""Q"": 1
      },
      {
        ""SKUId"": ""006954767430522"",
        ""Q"": 1
      },
      {
        ""SKUId"": ""006954767410623"",
        ""Q"": 1
      }
    ]
  }
]";
            using (var r = ChoJSONReader<Data>.LoadText(json)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }


        public class Data1
        {
            public int Sts { get; set; }
            public int TMtd { get; set; }
            public int SId { get; set; }
            public int T { get; set; }
            public int CCSr { get; set; }
            public int TId { get; set; }
            public int UId { get; set; }
            public int NPro { get; set; }
            public List<Element> P1 { get; set; }
        }
        [Test]
        public static void DesrializeListOrObject()
        {
            string json = @"{
  ""P1"":[],
  ""Sts"": 1,
  ""TMtd"": 2,
  ""SId"": 215,
  ""T"": 1599453168,
  ""CCSr"": 98972,
  ""TId"": 492,
  ""UId"": 1687,
  ""NPro"": 3,
}";
            string expected = @"[
  {
    ""Sts"": 1,
    ""TMtd"": 2,
    ""SId"": 215,
    ""T"": 1599453168,
    ""CCSr"": 98972,
    ""TId"": 492,
    ""UId"": 1687,
    ""NPro"": 3,
    ""P1"": []
  }
]";
            using (var r = ChoJSONReader<Data1>.LoadText(json)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Location1
        {
            public Location1()
            {
                Locations = new LocationList();
            }
            public string Name { get; set; }
            public LocationList Locations { get; set; }
        }

        // Note: LocationList is simply a subclass of a List<T>
        // which then adds an IsExpanded property for use by the UI.
        public class LocationList : List<Location1>
        {
            [JsonProperty("IsExpanded")]
            public bool IsExpanded { get; set; }
        }

        public class RootViewModel
        {
            public RootViewModel()
            {
                RootLocations = new LocationList();
            }

            public string Test { get; set; }
            [JsonProperty("RootLocations")]
            public LocationList RootLocations { get; set; }
        }
        [Test]
        public static void DeserializeNestedObjectOfList()
        {
            string json = @"{
    ""Test"": ""x"",
  ""RootLocations"": {
    ""IsExpanded"": true,
    ""Locations"": [
      {
        ""Name"": ""Main Residence"",
        ""Locations"": [{
          ""IsExpanded"": false,
          ""Locations"": [
            {
              ""Name"": ""First Floor"",
              ""Locations"": null
            }
          ]
        }]
      }
    ]
  }
}";
            string expected = @"[
  {
    ""Test"": ""x"",
    ""RootLocations"": [
      {
        ""Name"": ""Main Residence"",
        ""Locations"": [
          {
            ""Name"": null,
            ""Locations"": [
              {
                ""Name"": ""First Floor"",
                ""Locations"": null
              }
            ]
          }
        ]
      }
    ]
  }
]";
            ChoJSONRecordConfiguration config = new ChoJSONRecordConfiguration();
            config.JsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            using (var r = ChoJSONReader<RootViewModel>.LoadText(json, config)
                .Configure(c => c.MapRecordFieldsForType<LocationList>())
                .Configure(c => c.MapRecordFieldsForType<Location1>())
                              .RegisterNodeConverterForType<LocationList>(s =>
                              {
                                  dynamic input = s as dynamic;
                                  /*
                                  var serializer = input.serializer;
                                  IDictionary<string, object> dict = input as IDictionary<string, object>;

                                  var locationList = new LocationList();

                                  JObject jLocationList = null;
                                  if (!dict.ContainsKey("value"))
                                  {
                                      JsonReader reader = input.reader as JsonReader;

                                      if (reader.TokenType == JsonToken.StartArray)
                                      {
                                          var jarr = JArray.Load(input.reader) as JArray;
                                          jLocationList = jarr.FirstOrDefault() as JObject;
                                      }
                                  }
                                  else
                                      jLocationList = ((JToken[])input.value)[0] as JObject;
                                  */
                                  var locationList = new LocationList();
                                  JObject jLocationList = ((JToken[])input)[0] as JObject;
                                  if (jLocationList != null)
                                  {
                                      try
                                      {
                                          locationList.IsExpanded = (bool)(jLocationList["IsExpanded"] ?? false);
                                          var jLocations = jLocationList["Locations"] as JArray;
                                          if (jLocations != null)
                                          {
                                              locationList.AddRange(jLocations.ToObject<List<Location1>>(/*serializer*/));

                                              //foreach (var jLocation in jLocations)
                                              //{
                                              //    var location = jLocation.ToObject<Location>();// serializer.Deserialize<Location>(new JTokenReader(jLocation));
                                              //    locationList.Add(location);
                                              //}
                                          }
                                      }
                                      catch { return null; }
                                  }

                                  return locationList;
                              }))
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());

                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }
        [Test]
        public static void BigIntTest()
        {
            string json = @"[{ ""column_one"": ""value"", ""column_two"": 200, ""column_three"": 3000000000, ""column_four"": ""value_2"" }]";

            string expected = @"column_one,column_two,column_three,column_four
value,200,3000000000,value_2";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json)
                .Setup(s => s.BeforeRecordFieldLoad += (o, e) =>
                {
                    if (e.PropertyName == "column_three")
                    {
                        e.Source = (long)((JValue)e.Source).Value;
                    }
                })
                )
            {
                using (var w = new ChoCSVWriter(csv).WithFirstLineHeader())
                {
                    w.Write(r);
                }
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void DiscoverHeaderTest()
        {
            string json = @"[
{
""column_a"": 1,
""column_b"": 2,
""column_c"": 3
},
{
""column_a"": 11,
""column_x"": ""not present in first item"",
""column_c"": 33
}
]";
            string expected = @"column_a,column_b,column_c,column_x
1,2,3,
11,,33,not present in first item";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json)
                .UseJsonSerialization()
                )
            {
                using (var w = new ChoCSVWriter(csv).WithFirstLineHeader()
                    .ThrowAndStopOnMissingField(false)
                    .WithMaxScanRows(2))
                {
                    w.Write(r);
                }
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSV3_1()
        {
            string expected = @"File Name,page,text,words,confidence
file.json,1,The quick brown fox jumps,The,0.958
file.json,1,The quick brown fox jumps,quick,0.57
file.json,1,The quick brown fox jumps,brown,0.799
file.json,1,The quick brown fox jumps,fox,0.442
file.json,1,The quick brown fox jumps,jumps,0.878
file.json,1,over,over,0.37
file.json,1,the lazy dog!,the,0.909
file.json,1,the lazy dog!,lazy,0.853
file.json,1,the lazy dog!,dog!,0.41";

            StringBuilder csv = new StringBuilder();
            using (var p = new ChoJSONReader("sample43.json")
                .WithJSONPath("$..readResults")
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithField("FileName", fieldName: "File Name")
                    .WithField("page")
                    .WithField("text")
                    .WithField("words")
                    .WithField("confidence")
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(p
                        .SelectMany(r1 => ((dynamic[])r1.lines).SelectMany(r2 => ((IList)r2.words).OfType<dynamic>().Select(r3 => new
                        {
                            FileName = "file.json",
                            r1.page,
                            r2.text,
                            words = r3.text,
                            r3.confidence
                        }))));
                }
            }

            Console.WriteLine(csv.ToString());
            string actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSV3_2()
        {
            string expected = @"page,text,words,confidence
1,The quick brown fox jumps,""The,quick,brown,fox,jumps"",0.7294
1,over,over,0.37
1,the lazy dog!,""the,lazy,dog!"",0.724";

            StringBuilder csv = new StringBuilder();

            using (var p = new ChoJSONReader("sample43.json")
                .WithJSONPath("$..readResults")
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(p
                        .SelectMany(r1 => ((dynamic[])r1.lines)
                        .Select(r2 => new
                        {
                            r1.page,
                            r2.text,
                            words = String.Join(",", ((IList)r2.words).OfType<dynamic>().Select(s1 => s1.text)),
                            confidence = ((IList)r2.words).OfType<dynamic>().Select(s1 => (double)s1.confidence).Average()
                        })));
                }
            }

            Console.WriteLine(csv.ToString());
            string actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSV4()
        {
            string expected = @"Field Name,Page,Practice Name,Owner FullName,Owner Email
file1.json,1,Some Practice Name,Bob Lee,bob@gmail.com";

            StringBuilder csv = new StringBuilder();
            using (var p = new ChoJSONReader("sample44.json")
                .WithJSONPath("$..readResults")
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithField("FileName", fieldName: "Field Name")
                    .WithField("Page", fieldName: "Page")
                    .WithField("PracticeName", fieldName: "Practice Name")
                    .WithField("OwnerFullName", fieldName: "Owner FullName")
                    .WithField("OwnerEmail", fieldName: "Owner Email")
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(p
                        .Select(r1 =>
                        {
                            var lines = (dynamic[])r1.lines;
                            return new
                            {
                                FileName = "file1.json",
                                Page = r1.page,
                                PracticeName = lines[2].text,
                                OwnerFullName = lines[4].text,
                                OwnerEmail = lines[6].text,
                            };
                        }
                ));
                }
            }

            Console.WriteLine(csv.ToString());
            string actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void ReadSpacedHeaderCSV()
        {
            string csv = @"Field Name,Page,Practice Name,Owner FullName,Owner Email
file1.json,1,Some Practice Name,Bob Lee,bob@gmail.com";

            string expected = @"[
  {
    ""FileName"": ""file1.json"",
    ""Page"": ""1"",
    ""PracticeName"": ""Some Practice Name"",
    ""OwnerFullName"": ""Bob Lee"",
    ""OwnerEmail"": ""bob@gmail.com""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                    .WithField("FileName", fieldName: "Field Name")
                    .WithField("Page", fieldName: "Page")
                    .WithField("PracticeName", fieldName: "Practice Name")
                    .WithField("OwnerFullName", fieldName: "Owner FullName")
                    .WithField("OwnerEmail", fieldName: "Owner Email")
                .WithFirstLineHeader())
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void JArray2CSV()
        {
            string json = @"[
{
""column_a"": 1,
""column_b"": 2,
""column_c"": 3
},
{
""column_a"": 11,
""column_b"": 21,
""column_c"": 31
}
]";
            string expected = @"column_a,column_b,column_c
1,2,3
11,21,31";

            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadJTokens(JArray.Parse(json)))
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    )
                    w.Write(r);
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class Sample45
        {
            public IDictionary<string, object> TransactionsDict { get; set; }
            [JsonProperty("success")]
            public long Success { get; set; }
            [JsonProperty("method")]
            public string Method { get; set; }
            //[ChoTypeConverter(typeof(TransactionKeyConverter))]
            //[ChoSourceType(typeof(string))]
            public List<string> TransactionsKeys { get; set; }
            [ChoTypeConverter(typeof(TransactionConverter))]
            public List<Transaction> Transactions { get; set; }
        }

        public class Transaction
        {
            [JsonProperty("buy_amount")]
            public decimal? BuyAmount { get; set; }
            [JsonProperty("buy_currency")]
            public string BuyCurrency { get; set; }
        }

        public class TransactionKeyConverter : IChoValueConverter, IChoCollectionConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return ((IEnumerable)value).Cast<string>().Where(i => i.IsNumber()).CastEnumerable<int>().ToList();
                throw new NotImplementedException();
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class TransactionConverter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var itemType = targetType.GetUnderlyingType().GetItemType();
                if (value is JObject jobj)
                    return jobj.ToObject(itemType);

                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public static void Sample45Test()
        {
            string expected = @"[
  {
    ""TransactionsDict"": {
      ""success"": 1,
      ""method"": ""getTrades"",
      ""5183"": {
        ""buy_amount"": ""0.00455636"",
        ""buy_currency"": ""BTC""
      },
      ""6962"": {
        ""buy_amount"": ""52.44700000"",
        ""buy_currency"": ""IOT""
      },
      ""6963"": {
        ""buy_amount"": ""383.54300000"",
        ""buy_currency"": ""TNT""
      },
      ""6964"": {
        ""buy_amount"": ""3412.50000000"",
        ""buy_currency"": ""FUN""
      },
      ""6965"": {
        ""buy_amount"": ""539.45000000"",
        ""buy_currency"": ""XLM""
      }
    },
    ""success"": 1,
    ""method"": ""getTrades"",
    ""TransactionsKeys"": [
      ""success"",
      ""method"",
      ""5183"",
      ""6962"",
      ""6963"",
      ""6964"",
      ""6965""
    ],
    ""Transactions"": [
      {
        ""buy_amount"": null,
        ""buy_currency"": null
      },
      {
        ""buy_amount"": null,
        ""buy_currency"": null
      },
      {
        ""buy_amount"": 0.00455636,
        ""buy_currency"": ""BTC""
      },
      {
        ""buy_amount"": 52.44700000,
        ""buy_currency"": ""IOT""
      },
      {
        ""buy_amount"": 383.54300000,
        ""buy_currency"": ""TNT""
      },
      {
        ""buy_amount"": 3412.50000000,
        ""buy_currency"": ""FUN""
      },
      {
        ""buy_amount"": 539.45000000,
        ""buy_currency"": ""XLM""
      }
    ]
  }
]";
            using (var r = new ChoJSONReader<Sample45>("sample45.json")
                .WithField(f => f.TransactionsDict, jsonPath: "$.*")
                .WithField(f => f.TransactionsKeys, jsonPath: "~*")
                .WithField(f => f.Transactions, jsonPath: "$^*", itemConverter: o =>
                {
                    var JObject = o as JObject;
                    if (JObject != null && JObject.Properties().Count() == 1 && JObject.ContainsKey("Value"))
                    {
                        return null;
                    }
                    else
                        return JObject.ToObject<Transaction>();
                })
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Sample46Test()
        {
            string expected = @"DigiKeyPartNumber,Packaging,Part_Status,Type,Protocol,Number_of_Drivers_Receivers,Duplex,Receiver_Hysteresis,Data_Rate,Voltage_Supply,Operating_Temperature,Mounting_Type,Package_Case,Supplier_Device_Package,Base_Part_Number,Capacitance,Tolerance,Voltage_Rated,ESR_Equivalent_Series_Resistance_,Lifetime_Temp_,Polarization,Ratings,Applications,Ripple_Current_Low_Frequency,Impedance,Lead_Spacing,Size_Dimension,Height_Seated_Max_,Surface_Mount_Land_Size
296-19853-1-ND,Tape & Reel (TR),Active,Transceiver,RS232,2/2,Full,300mV,250kbps,3V ~ 5.5V,-40°C ~ 85°C,Surface Mount,""16-SSOP (0.209"""", 5.30mm Width)"",16-SSOP,MAX323,,,,,,,,,,,,,,
495-77678-ND,Bulk,Active,,,,,,,,-40°C ~ 85°C,Chassis Mount,""Radial, Can - Screw Terminals"",,,47000µF,±20%,40V,12mOhm @ 100Hz,12000 Hrs @ 85°C,Polar,-,General Purpose,13A @ 100Hz,10 mOhms,0.874"" (22.20mm),2.032"" Dia (51.60mm),3.177"" (80.70mm),-";

            StringBuilder csv = new StringBuilder();
            using (var r = new ChoJSONReader("sample46.json")
                .UseJsonSerialization()
            )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .ThrowAndStopOnMissingField(false)
                    .WithMaxScanRows(2)
                    )
                    w.Write(r);
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class TO_JsonPunches
        {
            public string EmployeeID { get; set; }
            public string MatchedDateTime { get; set; }
        }
        [Test]
        public static void Sample46ATest()
        {
            string json = @"
[
    {
        ""RequestID"": 12345,
        ""Status"": 100,
        ""ResponseMessage"": ""API Call Successful"",
        ""ResponseData"": [
            {
                ""EmployeeID"": ""1824"",
                ""MatchedDateTime"": [
                    ""20 Oct 2020 06:41:45 AM""
                ]
            },
            {
                ""EmployeeID"": ""1214"",
                ""MatchedDateTime"": [
                    ""20 Oct 2020 06:05:03 AM""
                ]
            }
        ]
    }
]";

            //var results = ChoJSONReader<TO_JsonPunches>.LoadText(json).WithJSONPath("$.ResponseData").ToArray();
            string expected = @"[
  {
    ""EmployeeID"": ""1824"",
    ""MatchedDateTime"": ""20 Oct 2020 06:41:45 AM""
  },
  {
    ""EmployeeID"": ""1214"",
    ""MatchedDateTime"": ""20 Oct 2020 06:05:03 AM""
  }
]";
            using (var r = ChoJSONReader<TO_JsonPunches>.LoadText(json)
                .WithJSONPath("$..ResponseData[*]", true)
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Order1
        {
            public string orderNo { get; set; }
            public string customerNo { get; set; }
            [ChoJSONPath("items[*]")]
            [ChoSourceType(typeof(object[]))]
            [ChoTypeConverter(typeof(ChoArrayToObjectConverter))]
            public OrderItem[] items { get; set; }
        }

        public class OrderItem
        {
            [ChoArrayIndex(0)]
            public int itemId { get; set; }
            [ChoArrayIndex(1)]
            public decimal price { get; set; }
            [ChoArrayIndex(2)]
            public decimal quantity { get; set; }
        }
        [Test]
        public static void Sample47Test()
        {
            string expected = @"[
  {
    ""orderNo"": ""AO1234"",
    ""customerNo"": ""C0129999"",
    ""items"": [
      {
        ""itemId"": 255,
        ""price"": 1.65,
        ""quantity"": 20951.6
      },
      {
        ""itemId"": 266,
        ""price"": 1.8,
        ""quantity"": 20000.0
      },
      {
        ""itemId"": 277,
        ""price"": 1.9,
        ""quantity"": 0.5
      }
    ]
  }
]";
            using (var r = new ChoJSONReader<Order1>("sample47.json")
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class StringifiedModel
        {
            public string Id { get; set; }
            public Dictionary<string, string> Foo { get; set; }
        }
        [Test]
        public static void DesrializeStringifiedText()
        {
            string expected = @"[
  {
    ""Id"": ""abcd1234"",
    ""Foo"": {
      ""field1"": ""abc"",
      ""field2"": ""def""
    }
  }
]";
            using (var r = new ChoJSONReader<StringifiedModel>("sample48.json")
                .WithField(f => f.Id)
                .WithField(f => f.Foo, valueConverter: o => JsonConvert.DeserializeObject((o as string).Replace(@"\""", @""""), typeof(Dictionary<string, string>)))
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void DesrializeStringifiedText1()
        {
            string expected = @"[
  {
    ""Id"": ""abcd1234"",
    ""Foo"": {
      ""field1"": ""abc"",
      ""field2"": ""def""
    }
  }
]";
            using (var r = new ChoJSONReader<StringifiedModel>("sample48.json")
                .Setup(s => s.BeforeRecordFieldLoad += (o, e) =>
                {
                    if (e.PropertyName == nameof(StringifiedModel.Foo))
                    {
                        var txt = e.Source.ToString().Replace(@"\""", @"""");
                        txt = txt.Substring(1, txt.Length - 2);
                        e.Source = JObject.Parse(txt);
                    }
                })
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void JSON2CSVViceVersa()
        {
            string json = @"
{
  ""str1"": ""aaa"",
  ""num1"": 1,
  ""boolean1"": true,
  ""object1"": {
    ""str2"": ""bbb"",
    ""num2"": 2,
    ""boolean2"": false
  },
  ""list1"": [
    ""ccc"",
    ""ddd"",
    ""eee""
  ],
  ""list2"": [
    1,
    2,
    3
  ],
  ""list3"": [
    true,
    false,
    true
  ]
}";
            string expected = @"str1,num1,boolean1,object1.str2,object1.num2,object1.boolean2,list1_0,list1_1,list1_2,list2_0,list2_1,list2_2,list3_0,list3_1,list3_2
aaa,1,True,bbb,2,False,ccc,ddd,eee,1,2,3,True,False,True";
            StringBuilder csv = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
                .UseJsonSerialization()
                )
            {
                using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader()
                .NestedKeySeparator('.')
                .ArrayIndexSeparator('_')
                )
                {
                    w.Write(p);
                }
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);

            string expected1 = @"[
  {
    ""str1"": ""aaa"",
    ""num1"": 1,
    ""boolean1"": true,
    ""object1"": {
      ""str2"": ""bbb"",
      ""num2"": ""2"",
      ""boolean2"": ""False""
    },
    ""list1"": [
      ""ccc"",
      ""ddd"",
      ""eee""
    ],
    ""list2"": [
      1,
      2,
      3
    ],
    ""list3"": [
      true,
      false,
      true
    ]
  }
]";
            StringBuilder json1 = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv.ToString())
                .WithFirstLineHeader()
                .NestedKeySeparator('.')
                .ArrayIndexSeparator('_')
                .WithMaxScanRows(2)
                )
            {
                using (var w = new ChoJSONWriter(json1)
                    )
                {
                    var recs = r.ToArray();
                    w.Write(recs);
                }
            }
            Console.WriteLine(json1.ToString());
            var actual1 = json1.ToString();
            Assert.AreEqual(expected1, actual1);
        }

        public static void CreateLargeJSONFile()
        {
            string json = @"{
  ""type"": ""Feature"",
  ""id"": 0,
  ""properties"": { ""ID_0"": 136 },
  ""geometry"": {
  ""type"": ""Polygon"",
  ""coordinates"": [
      [
        [ 102.911849975585938, 1.763612031936702 ],
        [ 102.911430358886832, 1.763888001442069 ]
      ]
    ]
  }
}";
            bool first = true;
            using (var w = new StreamWriter("large.json"))
            {
                w.WriteLine("{");
                w.WriteLine(@"""features"":");
                w.WriteLine("[");

                for (int i = 0; i < 1000000; i++)
                {
                    if (first)
                    {
                        first = false;
                        w.Write(json);
                    }
                    else
                    {
                        w.WriteLine(",");
                        w.Write(json);
                    }
                }

                w.WriteLine("");
                w.WriteLine("]");

                w.WriteLine("}");
            }
        }

        static void ReadLargeFile()
        {
            string json = @"
{
  ""type"": ""FeatureCollection"",
  ""name"": ""MYS_adm2"",
  ""crs"": {
    ""type"": ""name"",
    ""properties"": { ""name"": ""urn:ogc:def:crs:OGC:1.3:CRS84"" }
  },
  ""features"": [
    {
      ""type"": ""Feature"",
      ""id"": 0,
      ""properties"": { ""ID_0"": 136 },
      ""geometry"": {
        ""type"": ""Polygon"",
        ""coordinates"": [
          [
            [ 102.911849975585938, 1.763612031936702 ],
            [ 102.911430358886832, 1.763888001442069 ]
          ]
        ]
      }
    },
    {
      ""type"": ""Feature"",
      ""id"": 1,
      ""properties"": { ""ID_0"": 136 },
      ""geometry"": {
        ""type"": ""MultiPolygon"",
        ""coordinates"": [
          [
            [
              [ 103.556556701660156, 1.455448031425533 ],
              [ 103.555900573730582, 1.455950021743831 ]
            ]
          ]
        ]
      }
    }
  ]
}";
            using (var r = new ChoJSONReader("large.json")
                .WithJSONPath("$.features")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }

        }
        [Test]
        public static void Sample49Test()
        {
            string expected = @"[
  {
    ""Abstände"": ""Eingangslager"",
    ""Eingangslager"": 0,
    ""Zone A"": 20,
    ""Zone B"": 32,
    ""Zone C"": 44,
    ""Zone D"": 45,
    ""Zone E"": 50,
    ""Zone F"": 45,
    ""Zone G"": 35,
    ""Zone H"": 10,
    ""Ausgangslager"": 40
  },
  {
    ""Abstände"": ""Zone A"",
    ""Eingangslager"": 20,
    ""Zone A"": 0,
    ""Zone B"": 12,
    ""Zone C"": 24,
    ""Zone D"": 35,
    ""Zone E"": 40,
    ""Zone F"": 30,
    ""Zone G"": 24,
    ""Zone H"": 12,
    ""Ausgangslager"": 30
  },
  {
    ""Abstände"": ""Zone B"",
    ""Eingangslager"": 32,
    ""Zone A"": 12,
    ""Zone B"": 0,
    ""Zone C"": 12,
    ""Zone D"": 22,
    ""Zone E"": 30,
    ""Zone F"": 24,
    ""Zone G"": 30,
    ""Zone H"": 17,
    ""Ausgangslager"": 45
  }
]";
            using (var r = new ChoJSONReader("sample49.json")
                .WithJSONPath("$.LocationDistance.Body")
                )
            {
                string actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Zone_A);
            }
        }
        [Test]
        public static void DeserializeTest()
        {
            string json = @"
{
  ""type"": ""FeatureCollection"",
  ""name"": ""MYS_adm2"",
  ""crs"": {
    ""type"": ""name"",
    ""properties"": { ""name"": ""urn:ogc:def:crs:OGC:1.3:CRS84"" }
  },
  ""features"": [
    {
      ""type"": ""Feature"",
      ""id"": 0,
      ""properties"": { ""ID_0"": 136 },
      ""geometry"": {
        ""type"": ""Polygon"",
        ""coordinates"": [
          [
            [ 102.911849975585938, 1.763612031936702 ],
            [ 102.911430358886832, 1.763888001442069 ]
          ]
        ]
      }
    },
    {
      ""type"": ""Feature"",
      ""id"": 1,
      ""properties"": { ""ID_0"": 136 },
      ""geometry"": {
        ""type"": ""MultiPolygon"",
        ""coordinates"": [
          [
            [
              [ 103.556556701660156, 1.455448031425533 ],
              [ 103.555900573730582, 1.455950021743831 ]
            ]
          ]
        ]
      }
    }
  ]
}";
            string expected = @"[
  {
    ""type"": ""Feature"",
    ""id"": 0,
    ""properties"": {
      ""ID_0"": 136
    },
    ""geometry"": {
      ""type"": ""Polygon"",
      ""coordinates"": [
        [
          [
            102.91184997558594,
            1.7636120319367019
          ],
          [
            102.91143035888683,
            1.763888001442069
          ]
        ]
      ]
    }
  },
  {
    ""type"": ""Feature"",
    ""id"": 1,
    ""properties"": {
      ""ID_0"": 136
    },
    ""geometry"": {
      ""type"": ""MultiPolygon"",
      ""coordinates"": [
        [
          [
            [
              103.55655670166016,
              1.4554480314255329
            ],
            [
              103.55590057373058,
              1.455950021743831
            ]
          ]
        ]
      ]
    }
  }
]";
            var recs = ChoJSONReader.DeserializeText(json, "$.features").ToArray();

            var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void ExpandJSON_1()
        {
            string json = @"
{
""person/account/id"":""01"",
""person/account/user_name"":""admin"",
""person/account/last_name"":""John"",
""person/account/first_name"":""Doe"",
""person/account/email"":""jdoe@emaail.com"",
""person/account/access"":[""admin"", ""regulator"", ""superuser""],
""person/address/address1"":""123 Street"",
""person/address/address2"":"""",
""person/address/city"":""Detroit"",
""person/address/state"":""ST"",
""posts/post_id[0]"":""1"",
""posts/post_date_publication[0]"":""2020-10-27"",
""posts/post_content[0]"":""test 1 post."",
""posts/post_id[1]"":""2"",
""posts/post_date_publication[1]"":""2020-10-27"",
""posts/post_content[1]"":""test 2 post.""
}";
            //ChoETLSettings.ArrayBracketNotation = ChoArrayBracketNotation.Square;

            string expected = @"[
  {
    ""person"": {
      ""account"": {
        ""id"": ""01"",
        ""user_name"": ""admin"",
        ""last_name"": ""John"",
        ""first_name"": ""Doe"",
        ""email"": ""jdoe@emaail.com"",
        ""access"": [
          ""admin"",
          ""regulator"",
          ""superuser""
        ]
      },
      ""address"": {
        ""address1"": ""123 Street"",
        ""address2"": """",
        ""city"": ""Detroit"",
        ""state"": ""ST""
      }
    },
    ""posts"": {
      ""post_id"": [
        ""1"",
        ""2""
      ],
      ""post_date_publication"": [
        ""2020-10-27"",
        ""2020-10-27""
      ],
      ""post_content"": [
        ""test 1 post."",
        ""test 2 post.""
      ]
    }
  }
]";
            StringBuilder outJson = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoJSONWriter(outJson))
                    w.Write(r.OfType<ChoDynamicObject>().Select(r1 =>
                    {
                        var rec = r1.ConvertToNestedObject('/', '[', ']');
                        return new
                        {
                            person = rec.person,
                            posts = rec.posts, //.ExpandArrayToObjects((Func<int, string>)(i => $"I{i}"))
                        };
                    }));
            }

            Console.WriteLine(outJson.ToString());
            var actual = outJson.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void ExpandJSON_2()
        {
            string json = @"
{
""person/account/id"":""01"",
""person/account/user_name"":""admin"",
""person/account/last_name"":""John"",
""person/account/first_name"":""Doe"",
""person/account/email"":""jdoe@emaail.com"",
""person/account/access"":[""admin"", ""regulator"", ""superuser""],
""person/address/address1"":""123 Street"",
""person/address/address2"":"""",
""person/address/city"":""Detroit"",
""person/address/state"":""ST"",
""posts/post_id[0]"":""1"",
""posts/post_date_publication[0]"":""2020-10-27"",
""posts/post_content[0]"":""test 1 post."",
""posts/post_id[1]"":""2"",
""posts/post_date_publication[1]"":""2020-10-27"",
""posts/post_content[1]"":""test 2 post.""
}";
            //ChoETLSettings.ArrayBracketNotation = ChoArrayBracketNotation.Square;

            string expected = @"[
  {
    ""person"": {
      ""account"": {
        ""id"": ""01"",
        ""user_name"": ""admin"",
        ""last_name"": ""John"",
        ""first_name"": ""Doe"",
        ""email"": ""jdoe@emaail.com"",
        ""access"": [
          ""admin"",
          ""regulator"",
          ""superuser""
        ]
      },
      ""address"": {
        ""address1"": ""123 Street"",
        ""address2"": """",
        ""city"": ""Detroit"",
        ""state"": ""ST""
      }
    },
    ""posts"": {
      ""I0"": {
        ""post_id"": ""1"",
        ""post_date_publication"": ""2020-10-27"",
        ""post_content"": ""test 1 post.""
      },
      ""I1"": {
        ""post_id"": ""2"",
        ""post_date_publication"": ""2020-10-27"",
        ""post_content"": ""test 2 post.""
      }
    }
  }
]";
            StringBuilder outJson = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoJSONWriter(outJson))
                    w.Write(r.OfType<ChoDynamicObject>().Select(r1 =>
                    {
                        var rec = r1.ConvertToNestedObject('/', '[', ']');
                        return new
                        {
                            person = rec.person,
                            posts = rec.posts.ExpandArrayToObjects((Func<int, string>)(i => $"I{i}"))
                        };
                    }));
            }

            Console.WriteLine(outJson.ToString());
            var actual = outJson.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSV_Issue120()
        {
            string json = @"{
	""name"": ""Fruits"",
	""date"": ""2020-05-26"",
	""counts"": 2,
	""reports"": {
		""Fruit days (4344) 05-26-2020"": {
			""name"": ""Orange, Fresh"",
			""reportName"": ""Oranges 4344 05-26-2020"",
			""fruitNumber"": ""4344"",
			""date"": ""05-26-2020"",
			""status"": ""on sale"",
			""fresh"": true
		},
		""Fruit days (2821) 05-28-2020"": {
			""name"": ""Apple, Fresh"",
			""reportName"": ""Apples 2821 05-26-2020"",
			""fruitNumber"": ""2821"",
			""date"": ""05-28-2020"",
			""status"": ""on sale"",
			""fresh"": true
		},
	}
}";
            string expected = @"""name"",""date"",""counts"",""reports__|"",""reports__|_name"",""reports__|_reportName"",""reports__|_fruitNumber"",""reports__|_date"",""reports__|_status"",""reports__|_fresh""
""Fruits"",""2020-05-26"",""2"",""Fruit days (4344) 05-26-2020"",""Orange, Fresh"",""Oranges 4344 05-26-2020"",""4344"",""05-26-2020"",""on sale"",""True""
"""","""","""",""Fruit days (2821) 05-28-2020"",""Apple, Fresh"",""Apples 2821 05-26-2020"",""2821"",""05-28-2020"",""on sale"",""True""";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                var recs = r
                        .SelectMany(r1 => ((IDictionary<string, object>)r1.reports)
                        .Select((r2, i) =>
                        {
                            string name = null;
                            string date = null;
                            long? counts = null;
                            if (i == 0)
                            {
                                name = r1.name;
                                date = r1.date;
                                counts = r1.counts;
                            }
                            else
                            {
                            }

                            dynamic ret = new ChoDynamicObject();
                            ret.name = name;
                            ret.date = date;
                            ret.counts = counts;
                            ret["reports__|"] = r2.Key;

                            foreach (var kvp in (IDictionary<string, object>)r2.Value)
                            {
                                ret[$"reports__|_{kvp.Key}"] = kvp.Value;
                            }
                            return ret;
                        }
                        ));

                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .QuoteAllFields()
                    .ConfigureHeader(c => c.QuoteAllHeaders = true)
                    )
                    w.Write(recs);
            }

            Console.WriteLine(csv.ToString());
            string actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class ResultData
        {
            [JsonProperty(PropertyName = "Id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "Results")]
            public IEnumerable<Result1> Results { get; set; }
        }

        public class Result1
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
        [Test]
        public static void JSON2CSV5()
        {
            string json = @"{
  ""Id"": ""839c0a09-f2d0-4f29-9cce-bc022d3511b5"",
  ""Results"": [
    {
      ""Name"": ""ABC"",
      ""Value"": ""5""
    },
    {
      ""Name"": ""CDE"",
      ""Value"": ""2""
    }
  ]
}";
            string expected = @"Id,ABC,CDE
839c0a09-f2d0-4f29-9cce-bc022d3511b5,5,2";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json)
                .UseJsonSerialization()
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                    )
                {
                    w.Write(r.Select(r1 => new
                    {
                        r1.Id,
                        Results = ((IList)r1.Results).OfType<IDictionary<string, object>>().ToDictionary(kvp => kvp.Values.First(), kvp => kvp.Values.Skip(1).FirstOrDefault())
                    }));
                }
            }

            Console.WriteLine(csv.ToString());
            string actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSV6()
        {
            string json = @"{
  ""Id"": ""839c0a09-f2d0-4f29-9cce-bc022d3511b5"",
  ""Results"": [
    {
      ""Name"": ""ABC"",
      ""Value"": ""5""
    },
    {
      ""Name"": ""CDE"",
      ""Value"": ""2""
    }
  ]
}";
            string expected = @"Id,ABC,CDE
839c0a09-f2d0-4f29-9cce-bc022d3511b5,5,2";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader<ResultData>.LoadText(json)
                .UseJsonSerialization()
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                    )
                {
                    w.Write(r.Select(r1 => new
                    {
                        r1.Id,
                        Results = r1.Results.ToDictionary(kvp => kvp.Name, kvp => kvp.Value)
                    }));
                }
            }

            Console.WriteLine(csv.ToString());
            string actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class ResultDataX
        {
            [JsonProperty(PropertyName = "Id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "Results")]
            public IEnumerable<ResultX> Results { get; set; }
        }

        public class ResultX : IResultX
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public interface IResultX
        {

        }
        [Test]
        public static void InterfaceTest1()
        {
            string json = @"{
  ""Id"": ""839c0a09-f2d0-4f29-9cce-bc022d3511b5"",
  ""Results"": [
    {
      ""Name"": ""ABC"",
      ""Value"": ""5""
    },
    {
      ""Name"": ""CDE"",
      ""Value"": ""2""
    }
  ]
}";
            string expected = @"[
  {
    ""Id"": ""839c0a09-f2d0-4f29-9cce-bc022d3511b5"",
    ""Results"": [
      {
        ""Name"": ""ABC"",
        ""Value"": ""5""
      },
      {
        ""Name"": ""CDE"",
        ""Value"": ""2""
      }
    ]
  }
]";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader<ResultDataX>.LoadText(json)
                .WithField(f => f.Results, itemConverter: o => o)
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CustomeDictKeyTypeTest()
        {
            string json = @"
{
  ""7:00AM"": 1,
  ""8:00AM"": 2,
  ""9:00AM"": 3,
}";
            string expected = @"[
  {
    ""07:00AM"": 1,
    ""08:00AM"": 2,
    ""09:00AM"": 3
  }
]";
            using (var r = ChoJSONReader<Dictionary<DateTime, int>>.LoadText(json)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r.Select(rec => rec.ToDictionary(kvp => kvp.Key.ToString("HH:mmtt"), kvp => kvp.Value)), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }
        [Test]
        public static void DeserializeAnonymousType()
        {
            string json = @"
{
""Data"":[
    {
        ""Customer"":""C1"",
        ""ID"":""11111"",
        ""Desc"":""Row 1"",
        ""Price"":""123456""
    },
    {
        ""Customer"":""C2"",
        ""ID"":""22222"",
        ""Desc"":""Row 2"",
        ""Price"":""789012""
    },
    {
        ""Customer"":""C3"",
        ""ID"":""33333"",
        ""Desc"":""Row 3"",
        ""Price"":""345678""
    }
],
""Success"":true
}";
            string expected = @"[
  {
    ""Data"": [
      {
        ""Customer"": ""C1"",
        ""ID"": ""11111"",
        ""Desc"": ""Row 1"",
        ""Price"": ""123456""
      },
      {
        ""Customer"": ""C2"",
        ""ID"": ""22222"",
        ""Desc"": ""Row 2"",
        ""Price"": ""789012""
      },
      {
        ""Customer"": ""C3"",
        ""ID"": ""33333"",
        ""Desc"": ""Row 3"",
        ""Price"": ""345678""
      }
    ],
    ""Success"": true
  }
]";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoJSONWriter(jsonOut))
                {
                    foreach (var rec in r)
                        w.Write(new { Data = ((IEnumerable)rec.data), Success = rec.Success });
                }
            }
            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void JSON2CSV7()
        {
            string json = @"
{
  ""email"": ""email@email.com"",
  ""financial_status"": ""paid"",
  ""name"": ""#CCC94440"",
  ""line_items"": [
    {
      ""title"": ""item0"",
      ""quantity"": 3
    },
    {
      ""title"": ""item1"",
      ""quantity"": 2
    }
  ],
  ""shipping_lines"": [
    {
      ""title"": ""Free Shipping"",
      ""price"": ""1.00""
    }
  ]
}
";
            string expected = @"email,financial_status,name,title,quantity,price
email@email.com,paid,#CCC94440,item0,3,1.00
email@email.com,paid,#CCC94440,item1,2,1.00";

            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter(csv).WithFirstLineHeader())
                    w.Write(r.SelectMany(r1 => ((dynamic[])r1.line_Items)
                    .Select(r2 => new
                    {
                        r1.email,
                        r1.financial_status,
                        r1.name,
                        r2.title,
                        r2.quantity,
                        price = ((dynamic[])r1.shipping_lines)[0].price
                    }
                    )));
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSV10()
        {
            string json = @"
[
  {
    ""id"": 1,
    ""name"": ""Mike"",
    ""features"": {
      ""colors"": [
        ""blue""
      ],
      ""sizes"": [
        ""big""
      ]
    }
  },
  {
    ""id"": 1,
    ""name"": ""Jose"",
    ""features"": {
      ""colors"": [
        ""blue"",
        ""red""
      ],
      ""sizes"": [
        ""big"",
        ""small""
      ]
    }
  }
]";
            string expected = @"id,name,features_colors_0,features_colors_1,features_sizes_0,features_sizes_1
1,Mike,blue,,big,
1,Jose,blue,red,big,small";

            StringBuilder csv = new StringBuilder();

            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .WithMaxScanRows(2)
                    .ThrowAndStopOnMissingField(false)
                    )
                {
                    w.Write(r);
                }
            }
            string actual = csv.ToString();

            Assert.AreEqual(expected, actual);
        }

        [ChoJSONPath("$.error")]
        public class Error2
        {
            public long id { get; set; }
            public string code { get; set; }
            public string message { get; set; }
            public long? qty { get; set; }

        }
        [Test]
        public static void StringToLongTest()
        {
            string json = @"{
  ""error"": [
    {
      ""id"": ""15006"",
      ""code"": ""Error CODE"",
      ""message"": ""Error Message"",
      ""qty"": """"
    }
  ]
}";

            string expected = @"{
  ""id"": 15006,
  ""code"": ""Error CODE"",
  ""message"": ""Error Message"",
  ""qty"": null
}";
            var err = ChoJSONReader.DeserializeText<Error2>(json).FirstOrDefault();

            Console.WriteLine(err.Dump());
            string actual = JsonConvert.SerializeObject(err, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSV8()
        {
            string expected = @"billing_profile_definition,billing_profile_id,contact_id,create_timestamp,external_id,id,invoice_email_template_id,invoice_template_id,max_subscribers,modify_timestamp,passreset_email_template_id,profile_package_id,status,subscriber_email_template_id,terminate_timestamp,type,vat_rate
id,6,8,2021-02-15 16:31:49,125125,6,15,1,,2021-02-19 09:30:38,14,,active,13,,sipaccount,21
id,6,11,2021-02-19 14:34:00,125124,8,13,1,,2021-02-19 15:34:00,13,,active,13,,sipaccount,21";

            var sampleJson = File.ReadAllText(@"klant.json");

            StringBuilder csv = new StringBuilder();
            using (var custData = ChoJSONReader.LoadText(sampleJson)
                .WithJSONPath("$.._embedded.ngcp:customers")
                .WithFields(new string[] {
                    "billing_profile_definition",
                    "billing_profile_id",
                    "billing_profiles",
                    "contact_id",
                    "create_timestamp",
                    "external_id",
                    "id",
                    "invoice_email_template_id",
                    "invoice_template_id",
                    "max_subscribers",
                    "modify_timestamp",
                    "passreset_email_template_id",
                    "profile_package_id",
                    "status",
                    "subscriber_email_template_id",
                    "terminate_timestamp",
                    "type",
                    "vat_rate"
                })
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.MaxScanRows = 1)
                    .Configure(c => c.ThrowAndStopOnMissingField = false)
                    )
                {
                    w.Write(custData);
                }
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        public static void DumpDict()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("1", "2");
            dict.Add("2", new Error2 { id = 1 });

            Console.WriteLine(dict.Dump());
        }

        class ClassToDeserialize : IChoItemConvertable
        {
            public string Foo { get; set; }
            [ChoJSONPath("$.Bar[*]")]
            public List<IBarX> Bar { get; set; }

            public object ItemConvert(string propName, object value)
            {
                var jobj = value as JObject;
                if (jobj.ContainsKey("Type"))
                    return jobj.ToObject<ItemX>();
                else
                    return jobj.ToObject<PersonX>();
            }
        }
        interface IBarX
        {
            string Type { get; }
        }

        class PersonX : IBarX
        {
            public string Type => "Person";
            public string Name { get; set; }
            public int Age { get; set; }
            public string Country { get; set; }
        }

        class ItemX : IBarX
        {
            public string Type { get; set; }
            public int Year { get; set; }
        }

        [Test]
        public static void ConditionalDeserializationOfItems()
        {
            string json = @"{
  ""Foo"": ""Whatever"",
  ""Bar"": [
    {
      ""Name"": ""Enrico"",
      ""Age"": 33,
      ""Country"": ""Italy""
    },
    {
      ""Type"": ""Video"",
      ""Year"": 2004
    },
    {
      ""Name"": ""Sam"",
      ""Age"": 18,
      ""Country"": ""USA""
    },
    {
      ""Type"": ""Book"",
      ""Year"": 1980
    }
  ]
}";
            string expected = @"[
  {
    ""Foo"": ""Whatever"",
    ""Bar"": [
      {
        ""Type"": ""Person"",
        ""Name"": ""Enrico"",
        ""Age"": 33,
        ""Country"": ""Italy""
      },
      {
        ""Type"": ""Video"",
        ""Year"": 2004
      },
      {
        ""Type"": ""Person"",
        ""Name"": ""Sam"",
        ""Age"": 18,
        ""Country"": ""USA""
      },
      {
        ""Type"": ""Book"",
        ""Year"": 1980
      }
    ]
  }
]";
            using (var r = ChoJSONReader<ClassToDeserialize>.LoadText(json)
                .WithField(f => f.Foo)
                .WithField(f => f.Bar, jsonPath: "$.Bar[*]")
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void JSON2CSV9()
        {
            string json = @"
[
  {
    ""id"": 1,
    ""name"": ""Mike"",
    ""features"": {
      ""colors"": [
        ""blue""
      ],
      ""sizes"": [
        ""big""
      ]
    }
  },
  {
    ""id"": 1,
    ""name"": ""Jose"",
    ""features"": {
      ""colors"": [
        ""blue"",
        ""red""
      ],
      ""sizes"": [
        ""big"",
        ""small""
      ]
    }
  }
]";
            string expected = @"id,name,features_colors_0,features_colors_1,features_sizes_0,features_sizes_1
1,Mike,blue,,big,
1,Jose,blue,red,big,small";
            StringBuilder csv = new StringBuilder();

            using (var r = ChoJSONReader.LoadText(json)
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .WithMaxScanRows(2)
                    .ThrowAndStopOnMissingField(false)
                    )
                {
                    w.Write(r);
                }
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class fooRoot1
        {
            [ChoJSONPath("Value[*]")]
            [ChoSourceType(typeof(object[]))]
            [ChoTypeConverter(typeof(ChoArrayToObjectsConverter))]
            public foo1[] Value { get; set; }
        }

        [ChoSourceType(typeof(object[]))]
        [ChoTypeConverter(typeof(ChoArrayToObjectConverter))]
        public class foo1
        {
            [ChoArrayIndex(0)]
            public long prop1 { get; set; }
            [ChoArrayIndex(1)]
            public double prop2 { get; set; }
        }
        [Test]
        public static void ArrayToObjects1()
        {
            string json = @"
[
  {
    ""Value"": [
      [
        1618170480000,
        ""59594.60000000"",
        ""59625.00000000"",
        ""59557.13000000"",
        ""59595.05000000"",
        ""32.64148000"",
        1618170539999,
        ""1945185.17004597"",
        1209,
        ""14.78751100"",
        ""881221.83660417"",
        ""0""
      ],
      [
        1618170540000,
        ""59595.05000000"",
        ""59669.81000000"",
        ""59564.22000000"",
        ""59630.16000000"",
        ""27.45082600"",
        1618170599999,
        ""1636424.61486602"",
        1066,
        ""10.24907000"",
        ""610941.51532090"",
        ""0""
      ]
    ]
  }
]";
            string expected = @"[
  {
    ""Value"": [
      [
        1618170480000,
        59594.6
      ],
      [
        1618170540000,
        59595.05
      ]
    ]
  }
]";
            StringBuilder jsonOutput = new StringBuilder();
            using (var r = ChoJSONReader<fooRoot1>.LoadText(json)
                //.UseJsonSerialization()
                //.WithJSONPath("$", true)
                )
            {
                var x = r.ToArray();
                foreach (var rec in x)
                    Console.WriteLine(ChoUtility.Dump(rec));

                using (var w = new ChoJSONWriter<fooRoot1>(jsonOutput)
                    )
                {
                    w.Write(x);
                }
            }

            Console.WriteLine(jsonOutput.ToString());
            var actual = jsonOutput.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void ArrayToObjects()
        {
            string json = @"
[
    [
        1618170480000,
        ""59594.60000000"",
        ""59625.00000000"",
        ""59557.13000000"",
        ""59595.05000000"",
        ""32.64148000"",
        1618170539999,
        ""1945185.17004597"",
        1209,
        ""14.78751100"",
        ""881221.83660417"",
        ""0""
    ],
    [
        1618170540000,
    ]
]";

            string expected = @"[
  [
    1618170480000,
    59594.6
  ],
  [
    1618170540000,
    0.0
  ]
]";
            StringBuilder jsonOutput = new StringBuilder();
            using (var r = ChoJSONReader<foo1>.LoadText(json)
                //.UseJsonSerialization()
                //.WithJSONPath("$", true)
                )
            {
                var x = r.ToArray();
                foreach (var rec in x)
                    Console.WriteLine(ChoUtility.Dump(rec));

                using (var w = new ChoJSONWriter<foo1>(jsonOutput)
                    )
                {
                    w.Write(x);
                }
            }

            Console.WriteLine(jsonOutput.ToString());
            var actual = jsonOutput.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Issue141()
        {
            string expected = @"Latitude,Longitude,Floor ID,Sink,Handdryer,Urinal
10.00,11.00,FL01,Test,Test,10
10.00,11.00,FL01,5,Test,Test";
            StringBuilder csv = new StringBuilder();

            using (var r = new ChoJSONReader("sample141.json")
                .IgnoreField("Elevator")
                .UseJsonSerialization()
                )
            {
                var r1 = r.FlattenBy("Floor", "Toilet");
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .ThrowAndStopOnMissingField(false)
                    .WithMaxScanRows(2)
                    )
                    w.Write(r1);
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);

            csv.Clear();

            string expected1 = @"Latitude,Longitude,Floor ID,Floor Level,Floor Name,Desk Type,Foot Rest,Separator Type
10.00,11.00,FL02,Level 2,Floor2,20,30,Test";
            using (var r = new ChoJSONReader("sample141.json")
                      .IgnoreField("Elevator")
                    .UseJsonSerialization()
                      )
            {
                var r1 = r.FlattenBy("Floor", "Cubicles");
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .ThrowAndStopOnMissingField(false)
                    .WithMaxScanRows(2)
                    )
                    w.Write(r1);
            }

            Console.WriteLine(csv.ToString());
            var actual1 = csv.ToString();
            Assert.AreEqual(expected1, actual1);

            csv.Clear();

            string expected2 = @"Number,Led Light_Number
Elevator1,
Elevator2,123";
            using (var r = new ChoJSONReader("sample141.json")
                .WithJSONPath("$..Elevator")
                .UseJsonSerialization()
                  )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .ThrowAndStopOnMissingField(false)
                    .WithMaxScanRows(2)
                    )
                    w.Write(r);
            }

            Console.WriteLine(csv.ToString());
            var actual2 = csv.ToString();
            Assert.AreEqual(expected2, actual2);
        }
        [Test]
        public static void Issue141x()
        {
            string expected = @"Latitude,Longitude,Floor ID,Sink,Handdryer,Urinal,Floor Level,Floor Name,Desk Type,Foot Rest,Separator Type,Number,Led Light_Number
10.00,11.00,FL01,Test,Test,10,,,,,,,
10.00,11.00,FL01,5,Test,Test,,,,,,,
10.00,11.00,FL02,,,,Level 2,Floor2,20,30,Test,,
,,,,,,,,,,,Elevator1,
,,,,,,,,,,,Elevator2,123";

            string jsonFilePath = "sample141.json";
            List<dynamic> objs = new List<dynamic>();
            using (var r = new ChoJSONReader(jsonFilePath)
                .IgnoreField("Elevator")
                .UseJsonSerialization()
                )
            {
                var r1 = r.FlattenBy("Floor", "Toilet");
                objs.AddRange(r1.ToArray());
            }
            using (var r = new ChoJSONReader(jsonFilePath)
                .IgnoreField("Elevator")
                .UseJsonSerialization()
                )
            {
                var r1 = r.FlattenBy("Floor", "Cubicles");
                objs.AddRange(r1.ToArray());
            }

            using (var r = new ChoJSONReader(jsonFilePath)
                .WithJSONPath("$..Elevator")
                .UseJsonSerialization()
                )
            {
                objs.AddRange(r.ToArray());
            }

            StringBuilder csv = new StringBuilder();
            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                .WithMaxScanRows(10)
                )
            {

                w.Write(objs);
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public enum ResponseStatus { ok, fail };

        public class JsonJsonStatusReport
        {
            public ResponseStatus ResponseStatus { get; set; }
            public int NumberOfPages { get; set; }
            public JsonJsonStatusReportData[] Data { get; set; }
        }

        public class JsonJsonStatusReportData
        {
            public string Name { get; set; }
            public string BookingDate { get; set; }
        }
        [Test]
        public static void CustomDateTimeReadTest()
        {
            string json = @"
{
  ""response"": ""ok"",
  ""numberofpages"": 4,
  ""data"": [
    {
      ""name"": ""user1"",
      ""bookingdate"": ""24/05/2019""
    },
    {
      ""name"": ""user2"",
      ""bookingdate"": ""24/05/2019""
    },
    {
      ""name"": ""user3"",
      ""bookingdate"": ""4/03/2020""
    },
    {
      ""name"": ""user4"",
      ""bookingdate"": ""00:00""
    }
  ]
}";
            string expected = @"[
  {
    ""ResponseStatus"": 0,
    ""NumberOfPages"": 4,
    ""Data"": [
      {
        ""Name"": ""user1"",
        ""BookingDate"": ""24/05/2019""
      },
      {
        ""Name"": ""user2"",
        ""BookingDate"": ""24/05/2019""
      },
      {
        ""Name"": ""user3"",
        ""BookingDate"": ""4/03/2020""
      },
      {
        ""Name"": ""user4"",
        ""BookingDate"": ""00:00""
      }
    ]
  }
]";
            using (var r = ChoJSONReader<JsonJsonStatusReport>.LoadText(json)
                .UseJsonSerialization()
                .WithFieldForType<JsonJsonStatusReportData>(f => f.BookingDate, formatText: "dd/MM/yyyy")
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }
        [Test]
        public static void Json2Xml50()
        {
            string expected = @"<InvoicesDoc xmlns=""http://www.aade.gr/myDATA/invoice/v1.0"" xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:icls=""https://www.aade.gr/myDATA/incomeClassificaton/v1.0"" xmlns:ecls=""https://www.aade.gr/myDATA/expensesClassificaton/v1.0"" xmlns:schemaLocation=""http://www.aade.gr/myDATA/invoice/v1.0/InvoicesDoc-v0.6.xsd"">
  <invoice>
    <issuer>
      <vatNumber>888888888</vatNumber>
      <country>GR</country>
      <branch>1</branch>
    </issuer>
    <counterpart>
      <vatNumber>999999999</vatNumber>
      <country>GR</country>
      <branch>0</branch>
      <address>
        <postalCode>12345</postalCode>
        <city>TEST</city>
      </address>
    </counterpart>
    <invoiceHeader>
      <series>A</series>
      <aa>101</aa>
      <issueDate>2021-04-27</issueDate>
      <invoiceType>1.1</invoiceType>
      <currency>EUR</currency>
    </invoiceHeader>
    <paymentMethods>
      <paymentMethodDetails>
        <type>3</type>
        <amount>1760.00</amount>
        <paymentMethodInfo>Payment Method Info...</paymentMethodInfo>
      </paymentMethodDetails>
    </paymentMethods>
    <invoiceDetail>
      <lineNumber>1</lineNumber>
      <netValue>1000.00</netValue>
      <vatCategory>1</vatCategory>
      <vatAmount>240.00</vatAmount>
      <discountOption>true</discountOption>
      <incomeClassification>
        <icls:classificationType>E3_561_001</icls:classificationType>
        <icls:classificationCategory>category1_2</icls:classificationCategory>
        <icls:amount>1000.00</icls:amount>
      </incomeClassification>
    </invoiceDetail>
    <invoiceDetail>
      <lineNumber>2</lineNumber>
      <netValue>500.00</netValue>
      <vatCategory>1</vatCategory>
      <vatAmount>120.00</vatAmount>
      <discountOption>true</discountOption>
      <incomeClassification>
        <icls:classificationType>E3_561_001</icls:classificationType>
        <icls:classificationCategory>category1_3</icls:classificationCategory>
        <icls:amount>500.00</icls:amount>
      </incomeClassification>
    </invoiceDetail>
    <taxesTotals>
      <taxes>
        <taxType>1</taxType>
        <taxCategory>2</taxCategory>
        <underlyingValue>500.00</underlyingValue>
        <taxAmount>100.00</taxAmount>
      </taxes>
    </taxesTotals>
    <invoiceSummary>
      <totalNetValue>1500.00</totalNetValue>
      <totalVatAmount>360.00</totalVatAmount>
      <totalWithheldAmount>100.00</totalWithheldAmount>
      <totalFeesAmount>0.00</totalFeesAmount>
      <totalStampDutyAmount>0.00</totalStampDutyAmount>
      <totalOtherTaxesAmount>0.00</totalOtherTaxesAmount>
      <totalDeductionsAmount>0.00</totalDeductionsAmount>
      <totalGrossValue>1760.00</totalGrossValue>
      <incomeClassification>
        <icls:classificationType>E3_561_001</icls:classificationType>
        <icls:classificationCategory>category1_2</icls:classificationCategory>
        <icls:amount>1000.00</icls:amount>
      </incomeClassification>
      <incomeClassification>
        <icls:classificationType>E3_561_001</icls:classificationType>
        <icls:classificationCategory>category1_3</icls:classificationCategory>
        <icls:amount>500.00</icls:amount>
      </incomeClassification>
    </invoiceSummary>
  </invoice>
</InvoicesDoc>";
            StringBuilder xml = new StringBuilder();
            using (var r = new ChoJSONReader("sample50.json"))
            {
                using (var w = new ChoXmlWriter(xml)
                    .WithRootName("InvoicesDoc")
                    .IgnoreNodeName()
                    .WithXmlNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
                    .WithXmlNamespace("", "http://www.aade.gr/myDATA/invoice/v1.0")
                    .WithXmlNamespace("icls", "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")
                    .WithXmlNamespace("ecls", "https://www.aade.gr/myDATA/expensesClassificaton/v1.0")
                    .WithXmlNamespace("schemaLocation", "http://www.aade.gr/myDATA/invoice/v1.0/InvoicesDoc-v0.6.xsd")
                    )
                    w.Write(r.Select(r1 =>
                    {
                        foreach (var id in r1.invoice.invoiceDetails)
                            id.incomeClassification.AddNamespace("icls", "https://www.aade.gr/myDATA/incomeClassificaton/v1.0", true);
                        foreach (var ic in r1.invoice.invoiceSummary.incomeClassification)
                            ic.AddNamespace("icls", "https://www.aade.gr/myDATA/incomeClassificaton/v1.0", true);

                        return r1;
                    }));

                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
            }

            Console.WriteLine(xml.ToString());
            var actual = xml.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Json2Xml50_1()
        {
            string expected = @"<InvoicesDoc xmlns=""http://www.aade.gr/myDATA/invoice/v1.0"" xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:icls=""https://www.aade.gr/myDATA/incomeClassificaton/v1.0"" xmlns:ecls=""https://www.aade.gr/myDATA/expensesClassificaton/v1.0"" xmlns:schemaLocation=""http://www.aade.gr/myDATA/invoice/v1.0/InvoicesDoc-v0.6.xsd"">
  <invoice>
    <issuer>
      <vatNumber>888888888</vatNumber>
      <country>GR</country>
      <branch>1</branch>
    </issuer>
    <counterpart>
      <vatNumber>999999999</vatNumber>
      <country>GR</country>
      <branch>0</branch>
      <address>
        <postalCode>12345</postalCode>
        <city>TEST</city>
      </address>
    </counterpart>
    <invoiceHeader>
      <series>A</series>
      <aa>101</aa>
      <issueDate>2021-04-27</issueDate>
      <invoiceType>1.1</invoiceType>
      <currency>EUR</currency>
    </invoiceHeader>
    <paymentMethods>
      <paymentMethodDetails>
        <type>3</type>
        <amount>1760.00</amount>
        <paymentMethodInfo>Payment Method Info...</paymentMethodInfo>
      </paymentMethodDetails>
    </paymentMethods>
    <invoiceDetails>
      <invoiceDetail>
        <lineNumber>1</lineNumber>
        <netValue>1000.00</netValue>
        <vatCategory>1</vatCategory>
        <vatAmount>240.00</vatAmount>
        <discountOption>true</discountOption>
        <incomeClassification>
          <icls:classificationType>E3_561_001</icls:classificationType>
          <icls:classificationCategory>category1_2</icls:classificationCategory>
          <icls:amount>1000.00</icls:amount>
        </incomeClassification>
      </invoiceDetail>
      <invoiceDetail>
        <lineNumber>2</lineNumber>
        <netValue>500.00</netValue>
        <vatCategory>1</vatCategory>
        <vatAmount>120.00</vatAmount>
        <discountOption>true</discountOption>
        <incomeClassification>
          <icls:classificationType>E3_561_001</icls:classificationType>
          <icls:classificationCategory>category1_3</icls:classificationCategory>
          <icls:amount>500.00</icls:amount>
        </incomeClassification>
      </invoiceDetail>
    </invoiceDetails>
    <taxesTotals>
      <taxes>
        <taxType>1</taxType>
        <taxCategory>2</taxCategory>
        <underlyingValue>500.00</underlyingValue>
        <taxAmount>100.00</taxAmount>
      </taxes>
    </taxesTotals>
    <invoiceSummary>
      <totalNetValue>1500.00</totalNetValue>
      <totalVatAmount>360.00</totalVatAmount>
      <totalWithheldAmount>100.00</totalWithheldAmount>
      <totalFeesAmount>0.00</totalFeesAmount>
      <totalStampDutyAmount>0.00</totalStampDutyAmount>
      <totalOtherTaxesAmount>0.00</totalOtherTaxesAmount>
      <totalDeductionsAmount>0.00</totalDeductionsAmount>
      <totalGrossValue>1760.00</totalGrossValue>
      <incomeClassifications>
        <incomeClassification>
          <icls:classificationType>E3_561_001</icls:classificationType>
          <icls:classificationCategory>category1_2</icls:classificationCategory>
          <icls:amount>1000.00</icls:amount>
        </incomeClassification>
        <incomeClassification>
          <icls:classificationType>E3_561_001</icls:classificationType>
          <icls:classificationCategory>category1_3</icls:classificationCategory>
          <icls:amount>500.00</icls:amount>
        </incomeClassification>
      </incomeClassifications>
    </invoiceSummary>
  </invoice>
</InvoicesDoc>";
            StringBuilder xml = new StringBuilder();
            using (var r = new ChoJSONReader("sample50.json"))
            {
                using (var w = new ChoXmlWriter(xml)
                    .WithRootName("InvoicesDoc")
                    .IgnoreNodeName()
                    .WithXmlNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
                    .WithXmlNamespace("", "http://www.aade.gr/myDATA/invoice/v1.0")
                    .WithXmlNamespace("icls", "https://www.aade.gr/myDATA/incomeClassificaton/v1.0")
                    .WithXmlNamespace("ecls", "https://www.aade.gr/myDATA/expensesClassificaton/v1.0")
                    .WithXmlNamespace("schemaLocation", "http://www.aade.gr/myDATA/invoice/v1.0/InvoicesDoc-v0.6.xsd")
                    .Configure(c => c.XmlArrayQualifier = (key, obj) =>
                    {
                        if (key == "invoiceDetail"
                            || key == "invoiceDetails")
                            return true;
                        return null;
                    })
                    )
                    w.Write(r.Select(r1 =>
                    {
                        foreach (var id in r1.invoice.invoiceDetails)
                            id.incomeClassification.AddNamespace("icls", "https://www.aade.gr/myDATA/incomeClassificaton/v1.0", true);
                        foreach (var ic in r1.invoice.invoiceSummary.incomeClassification)
                            ic.AddNamespace("icls", "https://www.aade.gr/myDATA/incomeClassificaton/v1.0", true);

                        return r1;
                    }));

                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
            }

            Console.WriteLine(xml.ToString());
            var actual = xml.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class Customer2
        {
            [ChoJSONPath("$..CustomerID.value")]
            public string CustomerID { get; set; }
            [ChoJSONPath("$..CustomerCurrencyID.value")]
            public string CustomerCurrencyID { get; set; }
        }
        [Test]
        public static void JSONTest2()
        {
            string json = @"
{
    ""CustomerID"": {
        ""value"": ""EXAMPLE""
    },
    ""CustomerCurrencyID"": {
        ""value"": ""USD""
    }
}";
            string expected = @"{
  ""CustomerID"": ""EXAMPLE"",
  ""CustomerCurrencyID"": ""USD""
}";
            var rec = ChoJSONReader.DeserializeText<Customer2>(json).FirstOrDefault();
            Console.WriteLine(rec.Dump());

            var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        public class PropertyModel
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("isConfigProperty")]
            public bool IsConfigProperty { get; set; }
            [JsonProperty("displayProperty")]
            public bool DisplayProperty { get; set; }
            [JsonProperty("default")]
            public string Default { get; set; }
        }
        [Test]
        public static void SerializeZeroToNullIssue()
        {
            string json = @"
{
  ""name"":""Some name"",
  ""isConfigProperty"":true,
  ""displayProperty"":false,
  ""default"":""0""
}";
            string expected = @"[
  {
    ""name"": ""Some name"",
    ""isConfigProperty"": true,
    ""displayProperty"": false,
    ""default"": ""0""
  }
]";
            using (var r = ChoJSONReader<PropertyModel>.LoadText(json)
                .UseJsonSerialization()
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Metadata
        {
            public string id { get; set; }
            public string uri { get; set; }
            public string type { get; set; }
        }

        public class Result51
        {
            public Metadata metadata { get; set; }
            public string ID { get; set; }
            public string Value1 { get; set; }
            public string Value2 { get; set; }
            public string Value3 { get; set; }
        }

        public static void ReadJsonOneItemAtATime()
        {
            string expected = null;
            using (var r = new ChoJSONReader<Result51>("sample53.json")
                .WithJSONPath("$..d.results")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }
        [Test]
        public static void Sample53Test()
        {
            string expected = @"[
  {
    ""__metadata/uri"": ""myuri.com"",
    ""__metadata/type"": ""String"",
    ""jobNumber"": ""123456789"",
    ""numberVacancy"": ""1"",
    ""some_obj/__metadata/uri"": ""myuri.com"",
    ""some_obj/__metadata/type"": ""String"",
    ""some_obj/code"": ""000012356"",
    ""anothernested/results/0/__metadata/uri"": ""myuri.com"",
    ""anothernested/results/0/__metadata/type"": ""String"",
    ""anothernested/results/0/picklistLabels/results/0/__metadata/uri"": ""myuri.com"",
    ""anothernested/results/0/picklistLabels/results/0/__metadata/type"": ""String"",
    ""anothernested/results/0/picklistLabels/results/0/label"": ""Casual"",
    ""anothernested/results/0/picklistLabels/results/1/__metadata/uri"": ""myuri.com"",
    ""anothernested/results/0/picklistLabels/results/1/__metadata/type"": ""String"",
    ""anothernested/results/0/picklistLabels/results/1/label"": ""Casual""
  },
  {
    ""__metadata/uri"": ""myuri.com"",
    ""__metadata/type"": ""String"",
    ""jobNumber"": ""987654321"",
    ""numberVacancy"": ""1"",
    ""some_obj/__metadata/uri"": ""myuri.com"",
    ""some_obj/__metadata/type"": ""String"",
    ""some_obj/code"": ""000012356"",
    ""anothernested/results/0/__metadata/uri"": ""myuri.com"",
    ""anothernested/results/0/__metadata/type"": ""String"",
    ""anothernested/results/0/picklistLabels/results/0/__metadata/uri"": ""myuri.com"",
    ""anothernested/results/0/picklistLabels/results/0/__metadata/type"": ""String"",
    ""anothernested/results/0/picklistLabels/results/0/label"": ""Casual"",
    ""anothernested/results/0/picklistLabels/results/1/__metadata/uri"": ""myuri.com"",
    ""anothernested/results/0/picklistLabels/results/1/__metadata/type"": ""String"",
    ""anothernested/results/0/picklistLabels/results/1/label"": ""Casual""
  }
]";
            using (var r = new ChoJSONReader("sample53.json")
                .WithJSONPath("$..d.results")
                .Configure(c => c.NestedKeySeparator = '/')
                )
            {
                var dt = r.AsDataTable();
                Console.WriteLine(dt.Dump());

                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Xml2JSONWithSingleOrArrayNode()
        {
            string xml = @"
<export_person>
<person>
<fname>James</fname>
<lname>Williams</lname>
 <dept_details>
     <name>Engineering</name>
     <address>117, street</address>
  </dept_details>
</person>
</export_person>";

            string expected = @"[
  {
    ""fname"": ""James"",
    ""lname"": ""Williams"",
    ""dept_details"": {
      ""name"": ""Engineering"",
      ""address"": ""117, street""
    }
  }
]";
            StringBuilder json = new StringBuilder();
            using (var r = ChoXmlReader.LoadText(xml)
                //.Configure(c => c.UseXmlArray = false)
                //.Configure(c => c.TurnOffPluralization = true)
                )
            {
                using (var w = new ChoJSONWriter(json)
                    .Configure(c => c.DefaultArrayHandling = false)
                    )
                {
                    w.Write(r);
                }

            }

            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class RECORD_PHOTO
        {
            public string ROWW { get; set; }
            public string ALBUMID { get; set; }
            public string LINK { get; set; }
        }
        [Test]
        public static void SortAndDatatable()
        {
            string json = @"{
  ""RECORDS"": [
    {
      ""ROWW"": ""279166"",
      ""ALBUMID"": ""3"",
      ""LINK"": ""https://...1""
    },
    {
      ""ROWW"": ""279165"",
      ""ALBUMID"": ""1"",
      ""LINK"": ""https://...2""
    },
    {
      ""ROWW"": ""279164"",
      ""ALBUMID"": ""2"",
      ""LINK"": ""https://...3""
    }]
}";
            string expected = @"[
  {
    ""ROWW"": ""279164"",
    ""ALBUMID"": ""2"",
    ""LINK"": ""https://...3""
  },
  {
    ""ROWW"": ""279165"",
    ""ALBUMID"": ""1"",
    ""LINK"": ""https://...2""
  },
  {
    ""ROWW"": ""279166"",
    ""ALBUMID"": ""3"",
    ""LINK"": ""https://...1""
  }
]";
            using (var r = ChoJSONReader<RECORD_PHOTO>.LoadText(json)
                .WithJSONPath("$..RECORDS")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                var dt = r.OrderBy(rec => rec.ROWW).AsDataTable();
                Console.WriteLine(dt.Dump());
                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Questions
        {
            public Class1[] Property1 { get; set; }
        }

        public class Class1
        {
            public object Answer { get; set; }
            public int QuestionId { get; set; }
            public string Title { get; set; }
            public string AnswerType { get; set; }
        }
        [Test]
        public static void MultipleValues()
        {
            string json = @"[
  {
    ""Answer"": true,
    ""QuestionId"": 55,
    ""Title"": ""Are you Married?"",
    ""AnswerType"": ""Boolean""
  },
  {
    ""Answer"": {
      ""Id"": ""1"",
      ""Description"": ""Female"",
      ""Reference"": ""F"",
      ""ArchiveDate"": null,
      ""ParentId"": null,
      ""OptionType"": {
        ""Id"": 40,
        ""Type"": ""dropdown""
      }
    },
    ""QuestionId"": 778,
    ""Title"": ""Gender"",
    ""AnswerType"": ""Option""
  }
]";
            string expected = @"[
  {
    ""Answer"": true,
    ""QuestionId"": 55,
    ""Title"": ""Are you Married?"",
    ""AnswerType"": ""Boolean""
  },
  {
    ""Answer"": {
      ""Id"": ""1"",
      ""Description"": ""Female"",
      ""Reference"": ""F"",
      ""ArchiveDate"": null,
      ""ParentId"": null,
      ""OptionType"": {
        ""Id"": 40,
        ""Type"": ""dropdown""
      }
    },
    ""QuestionId"": 778,
    ""Title"": ""Gender"",
    ""AnswerType"": ""Option""
  }
]";
            using (var r = ChoJSONReader<Class1>.LoadText(json)
                )
            {
                var recs = r.ToArray();
                foreach (var rec in recs)
                {
                    Console.WriteLine(rec.Answer.GetType());

                    Console.WriteLine(rec.Dump());
                }

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        public class PlayersDTO
        {
            [ChoJSONPath("$..player")]
            [ChoTypeConverter(typeof(ChoCustomExprConverter), Parameters = @"'a => String.Join(ChoETL.ChoUtility.CastEnumerable<string>(a), ""|"")'")]
            public string Players { get; set; }
            public string Team { get; set; }
        }
        [Test]
        public static void JSONIssue147()
        {
            string expected = @"[
  {
    ""Extra"": ""extra"",
    ""Players"": ""a,b"",
    ""Substitute"": null,
    ""Team"": ""Title""
  },
  {
    ""Extra"": null,
    ""Players"": ""a,b,c"",
    ""Substitute"": ""d,e"",
    ""Team"": ""Title""
  }
]";
            using (var r = new ChoJSONReader("issue147.json")
                .WithField("Extra")
                .WithField("Players", valueConverter: o => String.Join(",", (IEnumerable<object>)o), jsonPath: "$..Players[*].player")
                .WithField("Substitute", valueConverter: o => o != null ? String.Join(",", (IEnumerable<object>)o) : null, jsonPath: "$..Substitute[*].player")
                .WithField("Team")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                var dt = r.AsDataTable();
                Console.WriteLine(dt.Dump());

                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
                return;
                foreach (var rec in r)
                {
                    Console.WriteLine(rec.Dump());
                }
            }
        }

        public class KeyValueConverter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is JProperty)
                {
                    var jo = value as JProperty;
                    return jo != null ? jo.Value : null;
                }
                else if (value is JObject)
                {
                    JObject jo = value as JObject;
                    var prop = jo.First.ToObject<JProperty>();
                    return prop.Value.ToObject(targetType);
                }
                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

        }
        public class Root
        {
            public string key { get; set; }
            public MyModel value { get; set; }
        }
        public class MyModel
        {
            [JsonProperty("session_id")]
            [ChoTypeConverter(typeof(KeyValueConverter))]
            public string SessionId { get; set; }
            [ChoTypeConverter(typeof(KeyValueConverter))]
            [JsonProperty("title_id_type")]
            public string TitleIdType { get; set; }
            [ChoTypeConverter(typeof(KeyValueConverter))]
            [JsonProperty("event_step")]
            public int EventStep { get; set; }
            [JsonProperty("country")]
            [ChoTypeConverter(typeof(KeyValueConverter))]
            public string Country { get; set; }
            [ChoTypeConverter(typeof(KeyValueConverter))]
            [JsonProperty("event_params")]
            public Dictionary<string, string> EventParams { get; set; }
            [ChoTypeConverter(typeof(KeyValueConverter))]
            [JsonProperty("device_id_map")]
            public Dictionary<string, string> DeviceIdMap { get; set; }
            [ChoTypeConverter(typeof(KeyValueConverter))]
            [JsonProperty("experiment_id_list")]
            public string[] ExperimentIdList { get; set; }
            [JsonProperty("experiment_id_list1")]
            [ChoTypeConverter(typeof(KeyValueConverter))]
            public int[] ExperimentIdList1 { get; set; }
        }
        public class Root1
        {
            public string key { get; set; }
            public MyModel1 value { get; set; }
        }
        public class MyModel1
        {
            [JsonProperty("session_id")]
            public string SessionId { get; set; }
            [JsonProperty("title_id_type")]
            public string TitleIdType { get; set; }
            [JsonProperty("event_step")]
            public int EventStep { get; set; }
            [JsonProperty("country")]
            public string Country { get; set; }
            [JsonProperty("event_params")]
            public Dictionary<string, string> EventParams { get; set; }
            [JsonProperty("device_id_map")]
            public Dictionary<string, string> DeviceIdMap { get; set; }
            [JsonProperty("experiment_id_list")]
            public string[] ExperimentIdList { get; set; }
            [JsonProperty("experiment_id_list1")]
            public int[] ExperimentIdList1 { get; set; }
        }
        [Test]
        public static void ReadJsonWithTypeIn()
        {
            string json = @"
[
  {
    ""key"": null,
    ""value"": {
      ""session_id"": { ""string"": ""pFEe0KByL/Df:3170:5"" },
      ""title_id_type"": { ""string"": ""server"" },
      ""event_name"": { ""string"": ""achievement"" },
      ""event_type"": { ""string"": ""server"" },
      ""event_step"": { ""int"": 8 },
      ""country"": { ""string"": ""US"" },
      ""event_params"": {
        ""map"": {
          ""cdur"": ""416"",
          ""gdur"": ""416"",
          ""sdur"": ""416"",
          ""tdur"": ""0"",
          ""type"": ""challenge"",
          ""percent"": ""100"",
          ""status"": ""expired""
        }
      },
      ""device_id_map"": { ""map"": {} },
      ""experiment_id_list"": { ""array"": [""a"", ""b""] },
      ""experiment_id_list1"": { ""array"": [1, 2] },
    }
  }
]";
            string expected = @"[
  {
    ""key"": null,
    ""value"": {
      ""session_id"": ""pFEe0KByL/Df:3170:5"",
      ""title_id_type"": ""server"",
      ""event_step"": 8,
      ""country"": ""US"",
      ""event_params"": {
        ""cdur"": ""416"",
        ""gdur"": ""416"",
        ""sdur"": ""416"",
        ""tdur"": ""0"",
        ""type"": ""challenge"",
        ""percent"": ""100"",
        ""status"": ""expired""
      },
      ""device_id_map"": {},
      ""experiment_id_list"": [
        ""a"",
        ""b""
      ],
      ""experiment_id_list1"": [
        1,
        2
      ]
    }
  }
]";
            using (var r = ChoJSONReader<Root1>.LoadText(json)
                .Setup(s => s.BeforeRecordFieldLoad += (o, e) =>
                {
                    JObject jo = e.Source as JObject;
                    if (jo != null)
                    {
                        if (e.PropertyName != "value")
                        {
                            var prop = jo.First.ToObject<JProperty>();
                            e.Source = prop.Value;
                        }
                    }
                })
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        static void ReadLargeFile1()
        {
            WebClient wc = new WebClient();
            var sr = wc.OpenRead("https://margincalculator.angelbroking.com/OpenAPI_File/files/OpenAPIScripMaster.json");

            using (var r = new ChoJSONReader(new StreamReader(sr))
                //.DetectEncodingFromByteOrderMarks(true)
                )
            {
                var items = r.Where(rec1 => ((string)rec1.symbol).StartsWith("BANKNIFTY")).ToArray();
                foreach (var rec in items)
                {
                    Console.WriteLine(rec.Dump());
                }

                Console.WriteLine(items.Length);
            }
        }


        public class CardLegalities
        {
            [JsonProperty("lang")]
            public string Lang { get; set; }

            [JsonProperty("set")]
            public string Set { get; set; }

            [JsonProperty("set_name")]
            public string SetName { get; set; }

            [JsonProperty("collector_number")]
            public string CollectorNumber { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            /// <summary>
            /// An object describing the legality of this card in different formats
            /// </summary>
            [JsonProperty("legalities")]
            public Legalities Legalities { get; set; }
        }

        public class Legalities
        {
            [JsonProperty("standard1")]
            public string Standard { get; set; }

            [JsonProperty("future")]
            public string Future { get; set; }

            [JsonProperty("historic")]
            public string Historic { get; set; }

            [JsonProperty("gladiator")]
            public string Gladiator { get; set; }

            [JsonProperty("pioneer")]
            public string Pioneer { get; set; }

            [JsonProperty("modern")]
            public string Modern { get; set; }

            [JsonProperty("legacy")]
            public string Legacy { get; set; }

            [JsonProperty("pauper")]
            public string Pauper { get; set; }

            [JsonProperty("vintage")]
            public string Vintage { get; set; }

            [JsonProperty("penny")]
            public string Penny { get; set; }

            [JsonProperty("commander")]
            public string Commander { get; set; }

            [JsonProperty("brawl")]
            public string Brawl { get; set; }

            [JsonProperty("duel")]
            public string Duel { get; set; }

            [JsonProperty("oldschool")]
            public string Oldschool { get; set; }

            [JsonProperty("premodern")]
            public string Premodern { get; set; }
        }
        [Test]
        public static void Issue148a()
        {
            string expected = @"[
  {
    ""Lang"": ""en"",
    ""Set"": ""lea"",
    ""SetName"": ""Limited Edition Alpha"",
    ""CollectorNumber"": ""142"",
    ""Name"": ""Dwarven Demolition Team"",
    ""Legalities.Standard"": ""not_legal"",
    ""Legalities.Future"": ""not_legal"",
    ""Legalities.Historic"": ""not_legal"",
    ""Legalities.Gladiator"": ""not_legal"",
    ""Legalities.Pioneer"": ""not_legal"",
    ""Legalities.Modern"": ""legal"",
    ""Legalities.Legacy"": ""legal"",
    ""Legalities.Pauper"": ""not_legal"",
    ""Legalities.Vintage"": ""legal"",
    ""Legalities.Penny"": ""not_legal"",
    ""Legalities.Commander"": ""legal"",
    ""Legalities.Brawl"": ""not_legal"",
    ""Legalities.Duel"": ""legal"",
    ""Legalities.Oldschool"": ""legal"",
    ""Legalities.Premodern"": ""not_legal""
  }
]";
            //ChoETLSettings.NestedKeySeparator = '.';
            using (var r = new ChoJSONReader<CardLegalities>("issue148a.json")
                .WithFieldForType<Legalities>(f => f.Standard, fieldName: "standard")
                .Configure(c => c.NestedKeySeparator = '.')
                )
            {
                var dt = r.AsDataTable();
                Console.WriteLine(dt.Dump());

                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class RECORD_PHOTO1
        {
            public string ROWW { get; set; }
            public string ALBUMID { get; set; }
            public string LINK { get; set; }
        }

        [ChoMetadataRefType(typeof(RECORD_PHOTO1))]
        public class RECORD_PHOTO1_META_DATA
        {
            public string ROWW { get; set; }
            public string ALBUMID { get; set; }
            [JsonIgnore]
            public string LINK { get; set; }
        }

        [Test]
        public static void ExcludePropertyUsingMetaType()
        {
            string json = @"{
  ""RECORDS"": [
    {
      ""ROWW"": ""279166"",
      ""ALBUMID"": ""3"",
      ""LINK"": ""https://...1""
    },
    {
      ""ROWW"": ""279165"",
      ""ALBUMID"": ""1"",
      ""LINK"": ""https://...2""
    },
    {
      ""ROWW"": ""279164"",
      ""ALBUMID"": ""2"",
      ""LINK"": ""https://...3""
    }]
}";
            string expected = @"[
  {
    ""ROWW"": ""279164"",
    ""ALBUMID"": ""2"",
    ""LINK"": null
  },
  {
    ""ROWW"": ""279165"",
    ""ALBUMID"": ""1"",
    ""LINK"": null
  },
  {
    ""ROWW"": ""279166"",
    ""ALBUMID"": ""3"",
    ""LINK"": null
  }
]";
            using (var r = ChoJSONReader<RECORD_PHOTO1>.LoadText(json)
                .WithJSONPath("$..RECORDS")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                var dt = r.OrderBy(rec => rec.ROWW).AsDataTable();
                Console.WriteLine(dt.Dump());

                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class RECORD_PHOTO2
        {
            public string ROWW { get; set; }
            public string ALBUMID { get; set; }
            public string LINK { get; set; }
        }
        [Test]
        public static void MapFieldNameDynamically()
        {
            string json = @"{
  ""RECORDS"": [
    {
      ""ROW"": ""279166"",
      ""ALBUMID"": ""3"",
      ""LINK"": ""https://...1""
    },
    {
      ""ROW"": ""279165"",
      ""ALBUMID"": ""1"",
      ""LINK"": ""https://...2""
    },
    {
      ""ROW"": ""279164"",
      ""ALBUMID"": ""2"",
      ""LINK"": ""https://...3""
    }]
}";
            string expected = @"[
  {
    ""ROWW"": ""279164"",
    ""ALBUMID"": ""2"",
    ""LINK"": ""https://...3""
  },
  {
    ""ROWW"": ""279165"",
    ""ALBUMID"": ""1"",
    ""LINK"": ""https://...2""
  },
  {
    ""ROWW"": ""279166"",
    ""ALBUMID"": ""3"",
    ""LINK"": ""https://...1""
  }
]";

            var cfg = new ChoJSONRecordConfiguration<RECORD_PHOTO2>();
            cfg.Map(r => r.ROWW, fieldName: "ROW");
            using (var r = ChoJSONReader<RECORD_PHOTO1>.LoadText(json, cfg)
                .WithJSONPath("$..RECORDS")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                var dt = r.OrderBy(rec => rec.ROWW).AsDataTable();
                Console.WriteLine(dt.Dump());

                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Issue148()
        {
            string expected = @"ItemInternalId|SyncJobId|ExternalJobIdentifier|Status|ErrorMessage|Operation|Entity|RecordCount|Processed|Errored|Queued|SyncType|FilelogId|Inserted|Updated|InsertedBy|UpdatedBy|EditCount
16c1b4cb-dbd5-4fcb-96e2-0fb6f3ac49db|363|7500n000007X8CsAAK|UploadComplete|InvalidBatch : InvalidBatch : Field name not found : MMC_MaretingEmailAddress__c|upsert|ExportSCCompany|0|0|0|0|S|168960|7/19/2021|7/19/2021|BBAKER-XPSTOWER-MMCGROUP\bbaker|BBAKER-XPSTOWER-MMCGROUP\bbaker|1
a3cc4aaf-d4a3-4838-8205-7f2de6a8bad0|372||UploadComplete|||ExportSCMergedCompany|0|0|0|0|S|169181|7/21/2021|7/21/2021|BBAKER-XPSTOWER-MMCGROUP\bbaker|BBAKER-XPSTOWER-MMCGROUP\bbaker|1";
            StringBuilder csv = new StringBuilder();

            var csvRecordConfiguration = new ChoCSVRecordConfiguration
            {
                Delimiter = "|",
                AutoDiscoverColumns = true,
                AutoDiscoverFieldTypes = true,
                ThrowAndStopOnMissingField = false,
                IgnoredFields = { "@odata.etag" },
                FileHeaderConfiguration = new ChoCSVFileHeaderConfiguration
                {
                    IgnoreColumnsWithEmptyHeader = true,
                    HasHeaderRecord = true
                }
            };
            var jsonRecordConfiguration = new ChoJSONRecordConfiguration
            {
                UseJSONSerialization = true,
            };

            using (var r = new ChoJSONReader("Issue148.json", jsonRecordConfiguration)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(ChoUtility.Dump(rec));
                //return;
                using (var w = new ChoCSVWriter(csv, csvRecordConfiguration)
                    .WithFirstLineHeader()
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                    )
                {
                    w.Write(r);
                }
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public struct D3Point //: IEquatable<D3Point>
        {
            public static readonly D3Point Null = new D3Point(0, 0, 0);

            public double X { get; private set; }
            public double Y { get; private set; }
            public double Z { get; private set; }

            public D3Point(double coordinateX, double coordinateY, double coordinateZ)
            {
                X = coordinateX;
                Y = coordinateY;
                Z = coordinateZ;
            }
        }
        [Test]
        public static void StructDeserialization()
        {
            string json = @"
{
  ""X"": 1262.6051066219518,
  ""Y"": -25972.229375190014,
  ""Z"": -299.99999999999994
}";
            string expected = @"[
  {
    ""X"": 1262.6051066219518,
    ""Y"": -25972.229375190014,
    ""Z"": -299.99999999999994
  }
]";
                //.RegisterNodeConverterForType<List<Class2>>(o =>
                // {
                //     var value = o as JToken[];
                //     var list = new List<Class2>();
                //     foreach (var item in value.OfType<JArray>())
                //     {
                //         list.AddRange(item.ToObject<Class2[]>());
                //     }

                //     return list;
                // })

            using (var r = ChoJSONReader<D3Point>.LoadText(json)
                .RegisterNodeConverterForType<D3Point>(o =>
                {
                    dynamic jo = o as JObject;

                    D3Point rec = new D3Point((double)jo.X, (double)jo.Y, (double)jo.Z);

                    return rec;
                })
                )
            {
                var recs = r.ToArray();
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void FlattenJSON1()
        {
            string expected = @"student,subjectssubjectname,subjectsmarkstype,subjectsmarksgrade
Bob,English,essay,A
Bob,English,vocabulary,B
Bob,English,spoken,C
Bob,French,essay,B
Bob,French,vocabulary,A
Bob,French,spoken,B
Mark,Dutch,essay,C
Mark,Dutch,vocabulary,B
Mark,Dutch,spoken,A
Mark,Mandrian,essay,C
Mark,Mandrian,vocabulary,C
Mark,Mandrian,spoken,C";
            StringBuilder csv = new StringBuilder();

            using (var r = new ChoJSONReader("sample54.json")
                .Configure(c => c.FlattenNode = true)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                //return;
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                    )
                {
                    w.Write(r);
                }
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class ExtDataObj
        {
            public string SessionId { get; set; }
            [JsonExtensionData]
            public Dictionary<string, JToken> Others { get; set; }
        }
        [Test]
        public static void JsonExtensionDataTest()
        {
            string json = @"
{
    ""sessionId"": ""sesh1"",
    ""instrumentId"": ""DEMO"",
    ""clientId"": ""a100"",
    ""assignedToType"": ""Client"",
    ""completeDate"": ""2/3/19"",
    ""answerStyle"": ""byValue"",
    ""Q1"": ""This is a long test text message of the testering."",
    ""Q2"": ""2"",
    ""Q3"": ""2|3|1"",
    ""Q4"": 9
}
";

            string expected = @"{
  ""SessionId"": ""sesh1"",
  ""instrumentId"": ""DEMO"",
  ""clientId"": ""a100"",
  ""assignedToType"": ""Client"",
  ""completeDate"": ""2/3/19"",
  ""answerStyle"": ""byValue"",
  ""Q1"": ""This is a long test text message of the testering."",
  ""Q2"": ""2"",
  ""Q3"": ""2|3|1"",
  ""Q4"": 9
}";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader<ExtDataObj>.LoadText(json)
                .UseJsonSerialization()
                )
            {
                using (var w = new ChoJSONWriter<ExtDataObj>(jsonOut)
                    .UseJsonSerialization()
                    .SupportMultipleContent()
                    .SingleElement()
                    )
                    w.Write(r);

                //r.Print();
                //return;
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
            }
            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JsonArrayToSingleCSVColumn()
        {
            string json = @"
[
  {
    ""id"": 1234,
    ""states"": [
      ""PA"",
      ""VA""
    ]
  },
  {
    ""id"": 1235,
    ""states"": [
      ""CA"",
      ""DE"",
      ""MD""
    ]
    }
]";
            string expected = @"id,states
1234,PA-VA
1235,CA-DE-MD";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.UseNestedKeyFormat = false)
                    .Configure(c => c.ArrayValueSeparator = '-')
                    )
                {
                    w.Write(r);
                }
            }
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        public class Rootobject
        {
            public string formatVersion { get; set; }
            [ChoJSONPath("$..matrix[*][*]")]
            public List<IMatrix> matrix { get; set; }
            public Summary summary { get; set; }
        }

        public interface IMatrix
        {

        }

        public class Summary
        {
            public int successfulRoutes { get; set; }
            public int totalRoutes { get; set; }
        }

        public class Matrix1 : IMatrix
        {
            public int statusCode { get; set; }
            public Response response { get; set; }
            //public Detailederror detailedError { get; set; }
        }

        public class Matrix2 : IMatrix
        {
            public int statusCode { get; set; }
            public string response { get; set; }
            public Detailederror detailedError { get; set; }
        }

        public class Response
        {
            public RouteSummary routeSummary { get; set; }
        }

        public class RouteSummary
        {
            public int lengthInMeters { get; set; }
        }

        public class Detailederror
        {
            public string message { get; set; }
            public string code { get; set; }
        }
        [Test]
        public static void DeserializeChildrenToConcreteClasses()
        {
            string expected = @"[
  {
    ""formatVersion"": ""0.0.1"",
    ""matrix"": [
      {
        ""statusCode"": 200,
        ""response"": {
          ""routeSummary"": {
            ""lengthInMeters"": 95028
          }
        }
      },
      {
        ""statusCode"": 200,
        ""response"": {
          ""routeSummary"": {
            ""lengthInMeters"": 97955
          }
        }
      },
      {
        ""statusCode"": 200,
        ""response"": {
          ""routeSummary"": {
            ""lengthInMeters"": 105077
          }
        }
      },
      {
        ""statusCode"": 200,
        ""response"": {
          ""routeSummary"": {
            ""lengthInMeters"": 108004
          }
        }
      },
      {
        ""statusCode"": 200,
        ""response"": {
          ""routeSummary"": {
            ""lengthInMeters"": 103661
          }
        }
      },
      {
        ""statusCode"": 200,
        ""response"": {
          ""routeSummary"": {
            ""lengthInMeters"": 106588
          }
        }
      },
      {
        ""statusCode"": 400,
        ""response"": ""Engine error while executing route request: MAP_MATCHING_FAILURE: Origin (0, 0)"",
        ""detailedError"": {
          ""message"": ""Engine error while executing route request: MAP_MATCHING_FAILURE: Origin (0, 0)"",
          ""code"": ""MAP_MATCHING_FAILURE""
        }
      },
      {
        ""statusCode"": 400,
        ""response"": ""Engine error while executing route request: MAP_MATCHING_FAILURE: Origin (0, 0)"",
        ""detailedError"": {
          ""message"": ""Engine error while executing route request: MAP_MATCHING_FAILURE: Origin (0, 0)"",
          ""code"": ""MAP_MATCHING_FAILURE""
        }
      }
    ],
    ""summary"": {
      ""successfulRoutes"": 6,
      ""totalRoutes"": 8
    }
  }
]";
            using (var r = new ChoJSONReader<Rootobject>("sample56.json")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                //.WithField(f => f.formatVersion)
                .WithField(f => f.matrix, itemTypeSelector: o =>
               {
                   dynamic dobj = o as dynamic;
                   switch ((int)dobj.statusCode)
                   {
                       case 200:
                           return typeof(Matrix1);
                       default:
                           return typeof(Matrix2);
                   }
               })
                //.WithField(f => f.summary)
                //.UseJsonSerialization()
                )
            {
                //r.Print();
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void DeserializeToConcreteClasses()
        {
            string expected = @"[
  {
    ""statusCode"": 200,
    ""response"": {
      ""routeSummary"": {
        ""lengthInMeters"": 95028
      }
    }
  },
  {
    ""statusCode"": 200,
    ""response"": {
      ""routeSummary"": {
        ""lengthInMeters"": 97955
      }
    }
  },
  {
    ""statusCode"": 400,
    ""response"": ""Engine error while executing route request: MAP_MATCHING_FAILURE: Origin (0, 0)"",
    ""detailedError"": {
      ""message"": ""Engine error while executing route request: MAP_MATCHING_FAILURE: Origin (0, 0)"",
      ""code"": ""MAP_MATCHING_FAILURE""
    }
  },
  {
    ""statusCode"": 400,
    ""response"": ""Engine error while executing route request: MAP_MATCHING_FAILURE: Origin (0, 0)"",
    ""detailedError"": {
      ""message"": ""Engine error while executing route request: MAP_MATCHING_FAILURE: Origin (0, 0)"",
      ""code"": ""MAP_MATCHING_FAILURE""
    }
  }
]";
            using (var r = new ChoJSONReader<IMatrix>("sample57.json")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Configure(c => c.SupportsMultiRecordTypes = true)
                .Configure(c => c.RecordTypeSelector = o =>
                {
                    dynamic dobj = o as dynamic;
                    switch ((int)dobj.Item2.statusCode)
                    {
                        case 200:
                            return typeof(Matrix1);
                        default:
                            return typeof(Matrix2);
                    }
                })
                )
            {
                //r.Print();
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public interface IGalleryItem
        {
        }

        public class GalleryItem : IGalleryItem
        {
            public string id { get; set; }
            public string title { get; set; }
            public bool is_album { get; set; }
        }

        public class GalleryAlbum : GalleryItem
        {
            public int images_count { get; set; }
            public List<GalleryImage> images { get; set; }
        }

        public class GalleryImage
        {
            public string id { get; set; }
            public string link { get; set; }
        }
        [Test]
        public static void DeserializeToConcreteClasses1()
        {
            string expected = @"[
  {
    ""id"": ""OUHDm"",
    ""title"": ""My most recent drawing. Spent over 100 hours."",
    ""is_album"": false
  },
  {
    ""images_count"": 3,
    ""images"": [
      {
        ""id"": ""24nLu"",
        ""link"": ""http://i.imgur.com/24nLu.jpg""
      },
      {
        ""id"": ""Ziz25"",
        ""link"": ""http://i.imgur.com/Ziz25.jpg""
      },
      {
        ""id"": ""9tzW6"",
        ""link"": ""http://i.imgur.com/9tzW6.jpg""
      }
    ],
    ""id"": ""lDRB2"",
    ""title"": ""Imgur Office"",
    ""is_album"": true
  }
]";
            using (var r = new ChoJSONReader<IGalleryItem>("Issue151.json")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Configure(c => c.RecordTypeSelector = o =>
                {
                    dynamic dobj = o as dynamic;
                    switch ((bool)dobj.Item2.is_album)
                    {
                        case true:
                            return typeof(GalleryAlbum);
                        default:
                            return typeof(GalleryItem);
                    }
                })
                .Configure(c => c.SupportsMultiRecordTypes = true)
                )
            {
                //foreach (var rec in r)
                //    Console.Write(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [ChoKnownTypeDiscriminator("is_album")]
        [ChoKnownType(typeof(GalleryAlbumX), "true")]
        [ChoKnownType(typeof(GalleryItemX), "false")]
        public interface IGalleryItemX
        {
        }

        public class GalleryItemX : IGalleryItemX
        {
            public string id { get; set; }
            public string title { get; set; }
            public bool is_album { get; set; }
        }

        public class GalleryAlbumX : GalleryItemX
        {
            public int images_count { get; set; }
            public List<GalleryImage> images { get; set; }
        }

        public class GalleryImageX
        {
            public string id { get; set; }
            public string link { get; set; }
        }

        public class RootAlblumX
        {
            public string id { get; set; }
            //[JsonConverter(typeof(ChoKnownTypeConverter<IGalleryItemX>))]
            public List<IGalleryItemX> albums { get; set; }
        }
        [Test]
        public static void DeserializeToConcreteClasses2()
        {
            string expected = @"[
  {
    ""id"": ""123-22"",
    ""albums"": [
      {
        ""id"": ""OUHDm"",
        ""title"": ""My most recent drawing. Spent over 100 hours."",
        ""is_album"": false
      },
      {
        ""images_count"": 3,
        ""images"": [
          {
            ""id"": ""24nLu"",
            ""link"": ""http://i.imgur.com/24nLu.jpg""
          },
          {
            ""id"": ""Ziz25"",
            ""link"": ""http://i.imgur.com/Ziz25.jpg""
          },
          {
            ""id"": ""9tzW6"",
            ""link"": ""http://i.imgur.com/9tzW6.jpg""
          }
        ],
        ""id"": ""lDRB2"",
        ""title"": ""Imgur Office"",
        ""is_album"": true
      }
    ]
  }
]";
            using (var r = new ChoJSONReader<RootAlblumX>("Issue152.json")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                //.UseJsonSerialization()
                //.JsonSerializationSettings(s => s.Converters.Add(Activator.CreateInstance<ChoKnownTypeConverter<IGalleryItemX>>()))
                )
            {
                //foreach (var rec in r)
                //    Console.Write(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void DeserializeToConcreteClasses4()
        {
            string expected = @"[
  {
    ""id"": ""OUHDm"",
    ""title"": ""My most recent drawing. Spent over 100 hours."",
    ""is_album"": false
  },
  {
    ""images_count"": 3,
    ""images"": [
      {
        ""id"": ""24nLu"",
        ""link"": ""http://i.imgur.com/24nLu.jpg""
      },
      {
        ""id"": ""Ziz25"",
        ""link"": ""http://i.imgur.com/Ziz25.jpg""
      },
      {
        ""id"": ""9tzW6"",
        ""link"": ""http://i.imgur.com/9tzW6.jpg""
      }
    ],
    ""id"": ""lDRB2"",
    ""title"": ""Imgur Office"",
    ""is_album"": true
  }
]";
            using (var r = new ChoJSONReader<IGalleryItemX>("Issue151.json")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                //.Configure(c => c.KnownTypeDiscriminator = "is_album")
                .Configure(c => c.SupportsMultiRecordTypes = true)
                )
            {
                //foreach (var rec in r)
                //    Console.Write(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void DeserializeToConcreteClasses3()
        {
            string expected = @"[
  {
    ""id"": ""OUHDm"",
    ""title"": ""My most recent drawing. Spent over 100 hours."",
    ""is_album"": false
  },
  {
    ""images_count"": 3,
    ""images"": [
      {
        ""id"": ""24nLu"",
        ""link"": ""http://i.imgur.com/24nLu.jpg""
      },
      {
        ""id"": ""Ziz25"",
        ""link"": ""http://i.imgur.com/Ziz25.jpg""
      },
      {
        ""id"": ""9tzW6"",
        ""link"": ""http://i.imgur.com/9tzW6.jpg""
      }
    ],
    ""id"": ""lDRB2"",
    ""title"": ""Imgur Office"",
    ""is_album"": true
  }
]";
            using (var r = new ChoJSONReader<IGalleryItemX>("Issue151.json")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Configure(c => c.KnownTypeDiscriminator = "is_album")
                .Configure(c => c.KnownTypes.Clear())
                .Configure(c => c.KnownTypes.Add("true", typeof(GalleryAlbumX)))
                .Configure(c => c.KnownTypes.Add("false", typeof(GalleryItemX)))
                .Configure(c => c.SupportsMultiRecordTypes = true)
                )
            {
                //foreach (var rec in r)
                //    Console.Write(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void LoadDuplicateKeys()
        {
            string json = @"
{
    ""Quotes"": {
        ""Quote"": {
            ""Text"": ""Hi""
        },
        ""Quote"": {
            ""Text"": ""Hello""
        }
    }
}";
            string expected = @"[
  {
    ""Quotes"": {
      ""Quote"": {
        ""Text"": ""Hello""
      }
    }
  }
]";
            using (var r = ChoJSONReader.LoadText(json)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void FlattenJSON()
        {
            string json = @"
{
    ""First Name"" : ""Steve"",
    ""Last Name"" : ""Williams"",
    ""Age"" : 20,
    ""Employement Details"" : {
        ""Organization"" : ""Google"",
        ""SalaryReceived"" : 25000,
        ""Designation"" : ""Senior Engineer""
    }
}";
            string expected = @"[
  {
    ""First Name"": ""Steve"",
    ""Last Name"": ""Williams"",
    ""Age"": 20,
    ""Organization"": ""Google"",
    ""SalaryReceived"": 25000,
    ""Designation"": ""Senior Engineer""
  }
]";

            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json)
                )
            {
                using (var w = new ChoJSONWriter(jsonOut))
                    w.Write(r.FlattenBy("Employement Details"));
            }
            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class ProductRoot
        {
            public ProductX Product { get; set; }
        }
        public partial class ProductX //: IChoNotifyRecordFieldRead
        {
            public string SKU { get; set; }

            public StandardProductID StandardProductID { get; set; }

            public Condition Condition { get; set; }

            public bool AfterRecordFieldLoad(object target, long index, string propName, object value)
            {
                throw new NotImplementedException();
            }

            public bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value)
            {
                throw new NotImplementedException();
            }

            public bool RecordFieldLoadError(object target, long index, string propName, ref object value, Exception ex)
            {
                throw new NotImplementedException();
            }
        }
        public class StandardProductID
        {
            public string Type { get; set; }

            public string Value { get; set; }
        }
        public class Condition
        {
            public string ConditionType { get; set; }
        }
        [Test]
        public static void TokenReplacementTest()
        {
            string json = @"
{
  ""Product"": {
    ""SKU"": ""{mappingKey1}"",
    ""StandardProductID"": {
      ""Type"": ""{mappingKey2}"",
      ""Value"": ""{mappingKey3}""
    },
    ""Condition"": {
      ""ConditionType"": ""{mappingKey4}""
    }
  }
}";

            string expected = @"{
  ""Product"": {
    ""SKU"": ""{mappingKey1}"",
    ""StandardProductID"": {
      ""Type"": ""{mappingKey2}"",
      ""Value"": ""{mappingKey3}""
    },
    ""Condition"": {
      ""ConditionType"": ""{mappingKey4}""
    }
  }
}";
            StringBuilder jsonOut = new StringBuilder();

            Dictionary<string, string> dict = new Dictionary<string, string> { { "mappingKey1", "mappingValue1" }, { "mappingKey2", "mappingValue2" }, { "mappingKey3", "mappingValue3" }, { "mappingKey4", "mappingValue4" } };
            Dictionary<string, string> dictOut = new Dictionary<string, string>
            {
                { "mappingValue1", "mappingKey1" },
                { "mappingValue2", "mappingKey2" },
                { "mappingValue3", "mappingKey3" },
                { "mappingValue4", "mappingKey4" }
            };
            using (var r = ChoJSONReader<ProductRoot>.LoadText(json)
                .UseJsonSerialization()
                .UseDefaultContractResolver()
                //.WithField(f => f.SKU, valueConverter: o =>
                //{
                //    var v = o as string;
                //    if (v.StartsWith("{") && v.EndsWith("}"))
                //        v = v.Substring(1, v.Length - 2);
                //    if (dict.ContainsKey(v))
                //        return dict[v];
                //    else
                //        return o;
                //})
                .Setup(s => s.BeforeRecordFieldLoad += (o, e) =>
                {
                    var v = e.Source.ToNString();
                    if (v.StartsWith("{") && v.EndsWith("}"))
                    {
                        v = v.Substring(1, v.Length - 2);
                        if (dict.ContainsKey(v))
                            e.Source = dict[v];
                    }
                })
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                //r.Print();

                using (var w = new ChoJSONWriter<ProductRoot>(jsonOut)
                    .SupportMultipleContent()
                    .SingleElement()
                    .UseJsonSerialization()
                    .UseDefaultContractResolver()
                    .Setup(s => s.BeforeRecordFieldWrite += (o, e) =>
                    {
                        var v = e.Source.ToString();
                        if (dictOut.ContainsKey(v))
                            e.Source = $"{{{dictOut[v]}}}";
                    })
                    )
                {
                    w.Write(r);
                }
            }
            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Json2Xml51()
        {
            string json = @"
[
    {
        ""firstName"": ""John"",
        ""lastName"": ""Smith"",
        ""age"": 25,
        ""address"": {
            ""streetAddress"": ""21 2nd Street"",
            ""city"": ""New York"",
            ""state"": ""NY"",
            ""postalCode"": ""10021""
        },
        ""phoneNumber"": [
            {
                ""type"": ""home"",
                ""number"": ""212 555-1234""
            },
            {
                ""type"": ""fax"",
                ""number"": ""646 555-4567""
            }
        ]
    },
    {
        ""firstName"": ""Tom"",
        ""lastName"": ""Mark"",
        ""age"": 50,
        ""address"": {
            ""streetAddress"": ""10 Main Street"",
            ""city"": ""Edison"",
            ""state"": ""NJ"",
            ""postalCode"": ""08837""
        },
        ""phoneNumber"": [
            {
                ""type"": ""home"",
                ""number"": ""732 555-1234""
            },
            {
                ""type"": ""fax"",
                ""number"": ""609 555-4567""
            }
        ]
    }
]
";
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <XElement>
    <firstName>John</firstName>
    <lastName>Smith</lastName>
    <age>25</age>
    <address>
      <streetAddress>21 2nd Street</streetAddress>
      <city>New York</city>
      <state>NY</state>
      <postalCode>10021</postalCode>
    </address>
    <phoneNumber>
      <type>home</type>
      <number>212 555-1234</number>
    </phoneNumber>
    <phoneNumber>
      <type>fax</type>
      <number>646 555-4567</number>
    </phoneNumber>
  </XElement>
  <XElement>
    <firstName>Tom</firstName>
    <lastName>Mark</lastName>
    <age>50</age>
    <address>
      <streetAddress>10 Main Street</streetAddress>
      <city>Edison</city>
      <state>NJ</state>
      <postalCode>08837</postalCode>
    </address>
    <phoneNumber>
      <type>home</type>
      <number>732 555-1234</number>
    </phoneNumber>
    <phoneNumber>
      <type>fax</type>
      <number>609 555-4567</number>
    </phoneNumber>
  </XElement>
</Root>";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoXmlWriter(jsonOut)
                    .UseXmlSerialization()
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                       )
                {
                    w.Write(r);
                }
            }

            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }


        public class Aggregates
        {
            [JsonProperty("ticker")]
            public string Ticker { get; set; }

            [JsonProperty("queryCount")]
            public int QueryCount { get; set; }

            [JsonProperty("resultsCount")]
            public int ResultsCount { get; set; }

            [JsonProperty("adjusted")]
            public bool Adjusted { get; set; }

            [JsonProperty("results")]
            public List<IQuote> Aggregatelist { get; set; }

            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("request_id")]
            public string RequestId { get; set; }

            [JsonProperty("count")]
            public int Count { get; set; }
        }

        public class Aggregate
        {
            /// <summary>
            /// The trading volume of the symbol in the given time period.
            /// </summary>
            [JsonProperty("v")]
            public object Volume { get; set; }

            /// <summary>
            /// The volume weighted average price.
            /// </summary>
            [JsonProperty("vw")]
            public double VolumeWeightedw { get; set; }

            /// <summary>
            /// The open price for the symbol in the given time period.
            /// </summary>
            [JsonProperty("o")]
            public double Open { get; set; }

            /// <summary>
            /// The close price for the symbol in the given time period.
            /// </summary>
            [JsonProperty("c")]
            public double Close { get; set; }

            /// <summary>
            /// The highest price for the symbol in the given time period.
            /// </summary>
            [JsonProperty("h")]
            public double High { get; set; }

            /// <summary>
            /// The lowest price for the symbol in the given time period.
            /// </summary>
            [JsonProperty("l")]
            public double Low { get; set; }

            /// <summary>
            /// The Unix Msec timestamp for the start of the aggregate window.
            /// </summary>
            [JsonProperty("t")]
            public double StartTime { get; set; }

            /// <summary>
            /// The number of transactions in the aggregate window.
            /// </summary>
            [JsonProperty("n")]
            public int TransactionsNum { get; set; }
        }
        [ChoMetadataRefType(typeof(Quote))]
        public class QuoteRef
        {
            [JsonProperty("t")]
            public DateTime Date { get; set; }
            [JsonProperty("o")]
            public decimal Open { get; set; }
            [JsonProperty("h")]
            public decimal High { get; set; }
            [JsonProperty("l")]
            public decimal Low { get; set; }
            [JsonProperty("c")]
            public decimal Close { get; set; }
            [JsonProperty("v")]
            public decimal Volume { get; set; }
        }
        public class Quote : IQuote
        {
            public DateTime Date { get; set; }
            public decimal Open { get; set; }
            public decimal High { get; set; }
            public decimal Low { get; set; }
            public decimal Close { get; set; }
            public decimal Volume { get; set; }
        }
        public interface IQuote
        {
            DateTime Date { get; }
            decimal Open { get; }
            decimal High { get; }
            decimal Low { get; }
            decimal Close { get; }
            decimal Volume { get; }
        }
        [Test]
        public static void MapToDifferentType()
        {
            string expected = @"[
  {
    ""ticker"": ""AAPL"",
    ""queryCount"": 2,
    ""resultsCount"": 2,
    ""adjusted"": true,
    ""results"": [
      {
        ""t"": 1234,
        ""o"": 74.06,
        ""h"": 75.15,
        ""l"": 73.7975,
        ""c"": 75.0875,
        ""v"": 135647456.0
      },
      {
        ""t"": 1234,
        ""o"": 74.2875,
        ""h"": 75.145,
        ""l"": 74.125,
        ""c"": 74.3575,
        ""v"": 146535512.0
      }
    ],
    ""status"": ""OK"",
    ""request_id"": ""6a7e466379af0a71039d60cc78e72282"",
    ""count"": 0
  }
]";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = new ChoJSONReader<Aggregates>("sample58.json")
                //.UseJsonSerialization()
                .WithField(f => f.Aggregatelist, fieldTypeSelector: o => typeof(List<Quote>))
                .WithFieldForType<Quote>(f => f.Date, valueConverter: o =>
                {
                    var dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    return dtDateTime.AddMilliseconds(o.CastTo<long>()).ToLocalTime();
                })
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Null)
                )
            {
                var x = r.ToArray();
                x.Print();

                using (var w = new ChoJSONWriter(jsonOut)
                        .WithFieldForType<Quote>(f => f.Date, valueConverter: o =>
                        {
                            return 1234;
                        })
                    )
                    w.Write(x);
            }

            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class GraphItem
        {
            [JsonProperty("id")]
            public int Id { get; set; }
            [JsonExtensionData]
            public Dictionary<string, JToken> Fields { get; set; }
        }
        [Test]
        public static void ConvertAdditionalFieldToPOCO()
        {
            string json = @"[
  {
    ""id"": ""1001"",
    ""fields"": {
      ""Column"": ""true"",
      ""Column2"": ""value2"",
      ""Column3"": ""65""
    },
     ""name"": ""Tom""
 },
  {
    ""id"": ""1002"",
    ""fields"": {
      ""Column"": ""true"",
      ""Column2"": ""value2"",
      ""Column3"": ""65""
    }
  }
]
";
            string expected = @"[
  {
    ""id"": 1001,
    ""fields"": {
      ""Column"": ""true"",
      ""Column2"": ""value2"",
      ""Column3"": ""65""
    },
    ""name"": ""Tom""
  },
  {
    ""id"": 1002,
    ""fields"": {
      ""Column"": ""true"",
      ""Column2"": ""value2"",
      ""Column3"": ""65""
    }
  }
]";
            using (var r = ChoJSONReader<GraphItem>.LoadText(json)
                .UseJsonSerialization()
                )
            {
                //r.Print();

                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }
        [ChoTurnOffImplicitOps(false)]
        public struct SofT<T>
        {
            //[JsonProperty]
            public T TValue { get; set; }

            public static implicit operator SofT<T>(string jtoken)
            {
                return new SofT<T>()
                {
                    TValue = (T)Convert.ChangeType(jtoken, typeof(T))
                };
            }

            public static implicit operator string(SofT<T> soft)
            {
                return soft.TValue?.ToString() ?? "";
            }
        }


        [JsonObject(MemberSerialization.OptIn)]
        public class Something
        {
            [JsonProperty]
            public SofT<int> TestStructInt { get; set; }

            [JsonProperty]
            public SofT<decimal> TestStructDecimal { get; set; }
        }
        [Test]
        public static void ImplicitValueCoversion()
        {
            var json = @"{
  ""TestStructInt"": ""12"",
  ""TestStructDecimal"": ""3.45""
}";
            string expected = @"[
  {
    ""TestStructInt"": ""12"",
    ""TestStructDecimal"": ""3.45""
  }
]";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader<Something>.LoadText(json)
                //    .UseJsonSerialization()
                //.UseDefaultContractResolver()
                )
            {
                using (var w = new ChoJSONWriter<Something>(jsonOut)
                    //.UseJsonSerialization()
                    //.UseDefaultContractResolver()
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                    )
                    w.Write(r);

                //r.Print();
            }
            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
            return;
            var modelDeserialised = JsonConvert.DeserializeObject<Something>(json);
            var modelReserialised = JsonConvert.SerializeObject(modelDeserialised);
            Console.WriteLine(modelReserialised);
        }
        [Test]
        public static void OutputEntireArrayToColumn()
        {
            string json = @"{
  ""FirstName"": ""something"",
  ""SomeProperties"": [
    { ""lala"": ""a"" },
    { ""lala"": ""b"" },
  ]
}";

            string expected = @"FirstName,SomeProperties
something,""[{""""lala"""": """"a""""},{""""lala"""": """"b""""}]""";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json)
                .WithField("FirstName")
                .WithField("SomeProperties", customSerializer: o => o.ToNString().Replace(Environment.NewLine, String.Empty).Replace("  ", String.Empty))
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(r);
                }

            }
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class Settings
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("content")]
            public ContentStructure Content { get; set; }
        }


        public struct ContentStructure
        {
            public Content ContentClass;
            public string ContentString;

            public static implicit operator ContentStructure(Content content) => new ContentStructure { ContentClass = content };
            public static implicit operator ContentStructure(string @string) => new ContentStructure { ContentString = @string };
            public static implicit operator Content(ContentStructure contentStruct) => contentStruct.ContentClass;
        }


        public class Content
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("duration")]
            public long Duration { get; set; }
        }
        [Test]
        public static void DeserializeToContentStruct()
        {
            Assert.Ignore();

            string json = @"{
    ""id"": ""any_id"",
    ""type"": ""any_type"",
    ""content"": {
        ""id"": ""any_id"",
        ""duration"": 1000
    }
}";

            string expected = @"FirstName,SomeProperties
something,""[{""""lala"""": """"a""""},{""""lala"""": """"b""""}]""";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader<Settings>.LoadText(json)
                .UseJsonSerialization()
                .UseDefaultContractResolver()
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                var recs = r.ToArray();
                //r.Print();
                //return;
                using (var w = new ChoJSONWriter<Settings>(jsonOut)
                    .UseJsonSerialization()
                    .UseDefaultContractResolver()
                    )
                    w.Write(recs);
            }
            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class Menu

        {
            public string Header { get; set; }
            public MenuItem[] Items { get; set; }
        }
        public class MenuItem
        {
            public string Name { get; set; }
            public string Label { get; set; }
        }
        [Test]
        public static void LoadKeyValueToItem()
        {
            string json = @"{
  ""menu"": {
    ""header"": ""SVG Viewer"",
    ""items"": [
      { ""0"": ""Open"" },
      {
        ""1"": ""OpenNew"",
        ""label"": ""Open New""
      },
      null,
      {
        ""2"": ""ZoomIn"",
        ""label"": ""Zoom In""
      },
      {
        ""3"": ""ZoomOut"",
        ""label"": ""Zoom Out""
      },
      {
        ""4"": ""OriginalView"",
        ""label"": ""Original View""
      },
      null,
      { ""5"": ""Quality"" },
      { ""6"": ""Pause"" },
      { ""7"": ""Mute"" },
      null,
      {
        ""8"": ""Find"",
        ""label"": ""Find...""
      },
      {
        ""9"": ""FindAgain"",
        ""label"": ""Find Again""
      },
      { ""10"": ""Copy"" },
      {
        ""11"": ""CopyAgain"",
        ""label"": ""Copy Again""
      },
      {
        ""12"": ""CopySVG"",
        ""label"": ""Copy SVG""
      },
      {
        ""13"": ""ViewSVG"",
        ""label"": ""View SVG""
      },
      {
        ""14"": ""ViewSource"",
        ""label"": ""View Source""
      },
      {
        ""15"": ""SaveAs"",
        ""label"": ""Save As""
      },
      null,
      { ""16"": ""Help"" },
      {
        ""17"": ""About"",
        ""label"": ""About Adobe CVG Viewer...""
      }
    ]
  }
}";
            string expected = @"[
  {
    ""Header"": ""SVG Viewer"",
    ""Items"": [
      {
        ""Name"": ""Open"",
        ""Label"": null
      },
      {
        ""Name"": ""OpenNew"",
        ""Label"": ""Open New""
      },
      {
        ""Name"": ""ZoomIn"",
        ""Label"": ""Zoom In""
      },
      {
        ""Name"": ""ZoomOut"",
        ""Label"": ""Zoom Out""
      },
      {
        ""Name"": ""OriginalView"",
        ""Label"": ""Original View""
      },
      {
        ""Name"": ""Quality"",
        ""Label"": null
      },
      {
        ""Name"": ""Pause"",
        ""Label"": null
      },
      {
        ""Name"": ""Mute"",
        ""Label"": null
      },
      {
        ""Name"": ""Find"",
        ""Label"": ""Find...""
      },
      {
        ""Name"": ""FindAgain"",
        ""Label"": ""Find Again""
      },
      {
        ""Name"": ""Copy"",
        ""Label"": null
      },
      {
        ""Name"": ""CopyAgain"",
        ""Label"": ""Copy Again""
      },
      {
        ""Name"": ""CopySVG"",
        ""Label"": ""Copy SVG""
      },
      {
        ""Name"": ""ViewSVG"",
        ""Label"": ""View SVG""
      },
      {
        ""Name"": ""ViewSource"",
        ""Label"": ""View Source""
      },
      {
        ""Name"": ""SaveAs"",
        ""Label"": ""Save As""
      },
      {
        ""Name"": ""Help"",
        ""Label"": null
      },
      {
        ""Name"": ""About"",
        ""Label"": ""About Adobe CVG Viewer...""
      }
    ]
  }
]";
            using (var r = ChoJSONReader<Menu>.LoadText(json)
                .WithJSONPath("$..menu")
                .Setup(s => s.BeforeRecordFieldLoad += (o, e) =>
                {
                    if (e.PropertyName == "Items")
                    {
                        JArray src = e.Source.GetValueAt<JArray>(0) as JArray;
                        JArray ret = new JArray();
                        if (src != null)
                        {
                            foreach (var jo in src.OfType<JObject>().Select(o1 => o1.ToObject<Dictionary<string, string>>()))
                            {
                                ret.Add(JToken.FromObject(new { Name = jo.GetValueAt<string>(0), Label = jo.GetValueAt<string>(1) }));
                            }
                        }
                        e.Source = new JToken[] { ret };
                    }
                })
                //.WithFieldForType<MenuItem>(f => f.Name, customSerializer: o =>
                //{
                //    JObject obj = o as JObject;
                //    if (obj != null)
                //    {
                //        IDictionary<string, object> dict = obj.ToObject<IDictionary<string, object>>();
                //        if (dict != null && dict.Count > 0)
                //            return dict.First().Value;
                //    }
                //    return "";
                //}
                //)
                )
            {
                //r.Print();

                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class House
        {
            [XmlElement]
            public MyObject[] Objects { get; set; }
        }

        public class MyObject
        {
            public string Name { get; set; }
        }
        [Test]
        public static void JSONToXml()
        {
            string expected = @"[
  {
    ""Objects"": [
      {
        ""$type"": ""ChoJSONReaderTest.Program+MyObject, ChoJSONReaderTest"",
        ""Name"": ""Name1""
      },
      {
        ""$type"": ""ChoJSONReaderTest.Program+MyObject, ChoJSONReaderTest"",
        ""Name"": ""Name2""
      }
    ]
  }
]";

            var house1 = new House
            {
                Objects = new MyObject[]
                 {
                  new MyObject() { Name = "Name1" },
                  new MyObject() { Name = "Name2" }
                 }
            };

            var jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Objects
            };
            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<House>(json)
                .JsonSerializationSettings(s => s.TypeNameHandling = TypeNameHandling.Objects)
                )
            {
                w.Write(house1);
            }

            using (var r = ChoJSONReader<House>.LoadText(json.ToString())
                .JsonSerializationSettings(s => s.TypeNameHandling = TypeNameHandling.Objects)
                )
            {
                using (var w = new ChoXmlWriter<House>(Console.Out)
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                    .IgnoreRootName()
                    .UseXmlSerialization()
                    )
                    w.Write(r);

                //r.Print();
            }
            Console.WriteLine(json.ToString());

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Json2Xml3()
        {
            string json = @"{
  ""$type"": ""Program+House, z3n3gd53"",
  ""Objects"": [
    {
      ""$type"": ""Program+MyObject, z3n3gd53"",
      ""Name"": ""Name1""
    },
    {
      ""$type"": ""Program+MyObject, z3n3gd53"",
      ""Name"": ""Name2""
    }
  ]
}";
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:json=""http://james.newtonking.com/projects/json"">
  <XElement xsi:type=""Program+House, z3n3gd53"">
    <Objects>
      <Object xsi:type=""Program+MyObject, z3n3gd53"">
        <Name>Name1</Name>
      </Object>
      <Object xsi:type=""Program+MyObject, z3n3gd53"">
        <Name>Name2</Name>
      </Object>
    </Objects>
  </XElement>
</Root>";
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json)
                //.UseJsonSerialization()
                )
            {
                //r.PrintAsJson();
                //return;
                using (var w = new ChoXmlWriter(jsonOut)
                    .Configure(c => c.UseXmlArray = true)
                    .WithXmlNamespace("xsi", ChoXmlSettings.XmlSchemaInstanceNamespace)
                    .WithXmlNamespace("json", ChoXmlSettings.JSONSchemaNamespace)
                    .Configure(c => c.OmitXsiNamespace = false)
                    //.Configure(c => c.UseJsonNamespaceForObjectType = true)
                    )
                    w.Write(r);
            }
            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Issue163()
        {
            string expected = @"[
  {
    ""Id"": ""1"",
    ""name"": ""name"",
    ""nestedobject"": {
      ""id"": ""2"",
      ""name"": ""objName""
    },
    ""nestedarray"": [
      {
        ""name"": ""namelist10"",
        ""city"": ""citylist10""
      },
      {
        ""name"": ""namelist11""
      }
    ]
  },
  {
    ""Id"": ""2"",
    ""name"": ""name1"",
    ""nestedobject"": {
      ""id"": ""3"",
      ""name"": ""objName""
    },
    ""nestedarray"": [
      {
        ""name"": ""namelist20"",
        ""city"": ""citylist20""
      },
      {
        ""name"": ""namelist21""
      }
    ]
  }
]";
            string csv = @"Id,name,nestedobject/id,nestedobject/name,nestedarray/0/name, nestedarray/0/city, nestedarray/1/name, nestedarray/200/city
1,name,2,objName,namelist10, citylist10,namelist11, citylist11
2,name1,3,objName,namelist20, citylist20,namelist21, citylist21";

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter(json)
                .Configure(c => c.DefaultArrayHandling = false)
                )
            {
                using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                    .Configure(c => c.NestedKeySeparator = '/')
                )
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        public readonly struct Model
        {
            public string Value { get; }

            [JsonConstructor]
            public Model(string value)
            {
                Value = value;
            }
        }
        [Test]
        public static void StructTypeTest()
        {
            string expected = @"[
  {
    ""Value"": ""Test""
  }
]";
            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<Model>(json)
                .SupportMultipleContent()
                .SingleElement()
                )
            {
                w.Write(new Model("Test"));
            }

            using (var r = ChoJSONReader<Model>.LoadText(json.ToString())
                .UseJsonSerialization()
                )
            {
                //r.Print();
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Issue165_1()
        {
            string expected = @"[
  {
    ""Id"": 1,
    ""nestedobject"": {
      ""id"": 2,
      ""name"": ""objName""
    },
    ""nestedarray"": [
      {
        ""name"": ""namelist10"",
        ""city"": ""citylist10""
      },
      {
        ""name"": ""namelist11""
      }
    ]
  },
  {
    ""Id"": 2,
    ""name"": ""name1"",
    ""nestedobject"": {
      ""id"": 3,
      ""name"": ""obj3Nmae""
    },
    ""nestedarray"": [
      {
        ""name"": ""namelist20"",
        ""city"": ""citylist20""
      }
    ]
  }
]";
            string csv =
                @"Id,name,nestedobject/id,nestedobject/name,nestedarray/0/name, nestedarray/0/city, nestedarray/1/name, nestedarray/200/city
1,,2,objName,namelist10,citylist10,namelist11,citylist11
2,name1,3,obj3Nmae,namelist20,citylist20,,citylist21";

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter(json)
                .Configure(c => c.DefaultArrayHandling = false)
                .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Null)
                )
            {
                using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                    .Configure(c => c.NestedKeySeparator = '/')
                    .WithMaxScanRows(1)
                    )
                    w.Write(r);
            }

            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Issue165()
        {
            string expected = @"[
  {
    ""Id"": 1,
    ""nestedobject"": {
      ""id"": 2,
      ""name"": ""objName""
    },
    ""nestedarray"": [
      {
        ""name"": ""namelist10"",
        ""city"": ""citylist10""
      },
      {
        ""name"": ""namelist11""
      }
    ]
  },
  {
    ""Id"": 2,
    ""nestedobject"": {
      ""id"": 3,
      ""name"": ""obj3Nmae""
    },
    ""nestedarray"": [
      {
        ""name"": ""namelist20"",
        ""city"": ""citylist20""
      }
    ]
  }
]";

            string csv =
                @"Id,name,nestedobject/id,nestedobject/name,nestedarray/0/name, nestedarray/0/city, nestedarray/1/name, nestedarray/200/city
1,,2,objName,namelist10,citylist10,namelist11,citylist11
2,name1,3,obj3Nmae,namelist20,citylist20,,citylist21";

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter(json)
                .Configure(c => c.DefaultArrayHandling = false)
                //.JsonSerializationSettings(s => s.NullValueHandling = NullValueHandling.Ignore)
                )
            {
                using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                    .Configure(c => c.NestedKeySeparator = '/')
                    .WithMaxScanRows(1)
                    .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Any)
                    )
                    w.Write(r);
            }

            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void MaxScanNodesTest()
        {
            string json = @"{
  ""Parts"": [
    {
      ""PartNum"": 1,
      ""PartCount"": 15,
      ""Table"": [
        {
          ""Col1"": ""Some Text"",
          ""Col2"": 0,
        },
        {
          ""Col1"": ""Some Text 2"",
          ""Col2"": 1,
		  ""Col3"":""SOme other value""
        }
      ]
    }
  ]
}";
            string expected = @"[
  {
    ""Col1"": ""Some Text"",
    ""Col2"": 0
  },
  {
    ""Col1"": ""Some Text 2"",
    ""Col2"": 1
  }
]";
            using (var r = ChoJSONReader.LoadText(json)
                       .WithJSONPath("$..Table").WithMaxScanNodes(2).UseJsonSerialization().ErrorMode(ChoErrorMode.IgnoreAndContinue)
                      )
            {
                //using (var w = new ChoCSVWriter(Console.Out)
                //    .WithMaxScanRows(2)
                //    .WithFirstLineHeader()
                //    .ThrowAndStopOnMissingField(false)
                //    )
                //{
                //    w.Write(r);
                //}
                var dt = r.AsDataTable();
                dt.Print();

                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public abstract class Component
        {
            [JsonProperty("name")]
            public string name { get; set; }

            public Component()
            {

            }
            public Component(string name)
            {
                this.name = name;
            }

            public virtual void Add(Component component)
            {
                throw new NotImplementedException();
            }

            public virtual void Remove(Component component)
            {
                throw new NotImplementedException();
            }

            public virtual bool IsComposite()
            {
                return true;
            }
        }

        public class Composite : Component
        {
            [JsonProperty("children")]
            public List<Component> _children { get; set; }

            public Composite()
            {

            }

            public Composite(string name) : base(name)
            {
                this._children = new List<Component>();
            }

            public override void Add(Component component)
            {
                this._children.Add(component);
            }

            public override void Remove(Component component)
            {
                this._children.Remove(component);
            }
        }

        public class Leaf : Component
        {
            public int experience { get; set; }
            public bool achieved { get; set; }

            public Leaf()
            {

            }

            [JsonConstructor]
            public Leaf(string name, int experience) : base(name)
            {
                this.experience = experience;
            }

            public override bool IsComposite()
            {
                return false;
            }
        }
        [Test]
        public static void CompositeSerialization()
        {
            string expected = @"[
  {
    ""children"": [
      {
        ""experience"": 0,
        ""achieved"": false,
        ""name"": ""First level""
      },
      {
        ""experience"": 0,
        ""achieved"": false,
        ""name"": ""Second level""
      },
      {
        ""experience"": 0,
        ""achieved"": false,
        ""name"": ""Third level""
      }
    ],
    ""name"": ""Levels""
  }
]";

            Composite levels = new Composite("Levels");
            Composite firstLevel = new Composite("First level");
            Composite secondLevel = new Composite("Second level");
            Composite thirdLevel = new Composite("Third level");
            Leaf firstAchievement = new Leaf("Mission 1", 1);
            Leaf secondAchviement = new Leaf("Mission 2", 2);
            Leaf thirdAchievement = new Leaf("Mission 3", 3);
            Leaf fourthAchievement = new Leaf("Mission 4", 4);
            Leaf fifthAchievement = new Leaf("Mission 5", 5);
            Leaf sixthAchievement = new Leaf("Mission 6", 6);
            firstLevel.Add(firstAchievement);
            secondLevel.Add(secondAchviement);
            secondLevel.Add(thirdAchievement);
            thirdLevel.Add(fourthAchievement);
            thirdLevel.Add(fifthAchievement);
            thirdLevel.Add(sixthAchievement);
            levels.Add(firstLevel);
            levels.Add(secondLevel);
            levels.Add(thirdLevel);

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<Composite>(json)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .UseJsonSerialization()
                .JsonSerializationSettings(s => s.TypeNameHandling = TypeNameHandling.Objects)
                )
                w.Write(levels);
            json.Print();

            using (var r = ChoJSONReader<Composite>.LoadText(json.ToString())
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .WithField(f => f._children, itemTypeSelector: o =>
                {
                    var jObject = o as JObject;
                    return jObject.ContainsKey("_children") ? typeof(Composite) : typeof(Leaf);
                })

                //.UseJsonSerialization()
                //.JsonSerializationSettings(s => s.TypeNameHandling = TypeNameHandling.Objects)
                )
            {
                //r.Print();

                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class MyClass2
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class MyCollectionClass : IEnumerable
        {
            MyClass2[] m_Items = null;
            int freeIndex = 0;

            public MyCollectionClass()
            {
                // For the sake of simplicity, let's keep them as arrays
                // ideally, it should be link list
                m_Items = new MyClass2[100];
            }

            public void Add(MyClass2 item)
            {
                // Let us only worry about adding the item 
                m_Items[freeIndex] = item;
                freeIndex++;
            }

            // IEnumerable Member
            public IEnumerator GetEnumerator()
            {
                foreach (MyClass2 o in m_Items)
                {
                    // Let's check for end of list (it's bad code since we used arrays)
                    if (o == null)
                    {
                        break;
                    }

                    // Return the current element and then on next function call 
                    // resume from next element rather than starting all over again;
                    yield return o;
                }
            }
        }
        [Test]
        public static void CustomCollectionTest()
        {
            string expected1 = @"[
  {
    ""Id"": 1,
    ""Name"": ""Tom""
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark""
  }
]";
            string expected2 = @"[
  {
    ""Id"": 1,
    ""Name"": ""Tom""
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark""
  }
]";

            MyCollectionClass coll = new MyCollectionClass();
            coll.Add(new MyClass2 { Id = 1, Name = "Tom" });
            coll.Add(new MyClass2 { Id = 2, Name = "Mark" });

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<MyClass2>(json)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                w.Write(coll.OfType<MyClass2>());
            }

            json.Print();
            var actual1 = json.ToString();
            Assert.AreEqual(expected1, actual1);

            MyCollectionClass coll2 = new MyCollectionClass();
            using (var r = ChoJSONReader<MyClass2>.LoadText(json.ToString()))
            {
                foreach (var rec in r)
                    coll2.Add(rec);
            }
            coll2.Print();

            var actual2 = JsonConvert.SerializeObject(coll2, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected2, actual2);
        }
        [Test]
        public static void JSON2XmlWithTextAttributeTest()
        {
            string json = @"{

  ""properties"": {
    ""replace"": [
      {
        ""@name"": ""firstElement"",
        ""#text"": ""11111""
      },
      {
        ""@name"": ""secondElement"",
        ""#text"": ""2222""
      }
    ]
  }
}";
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <XElement>
    <properties>
      <replace name=""firstElement"">
      11111
    </replace>
      <replace name=""secondElement"">
      2222
    </replace>
    </properties>
  </XElement>
</Root>";

            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json.ToString())
                //.WithJSONPath("$..properties.replace")
                )
            {
                using (var w = new ChoXmlWriter(jsonOut).ErrorMode(ChoErrorMode.IgnoreAndContinue)
                    .Configure(c => c.TurnOffXmlFormatting = false)
                    .Configure(c => c.Formatting = System.Xml.Formatting.Indented)
                    )
                    w.Write(r);
            }
            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void LoadSensorData()
        {
            string expected = @"[
  {
    ""Keys"": {
      ""RecipeStepID"": ""START"",
      ""StepResult"": ""NormalEnd""
    }
  },
  {
    ""Keys"": {
      ""RecipeStepID"": ""END"",
      ""StepResult"": ""NormalEnd""
    }
  }
]";
            using (var r = new ChoJSONReader(@"Sensors.json")
                .WithJSONPath("$..ControlJob.RecipeSteps")
                )
            {
                //foreach (var rec in r)
                //{
                //    rec.Print();
                //}
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CSV2JSON()
        {
            Assert.Ignore();

            string expected = null;

            string csvFilePath = @"XBTUSD.csv";

            StringBuilder jsonOut = new StringBuilder();
            using (var r = new ChoCSVReader(csvFilePath)
                .NotifyAfter(1000)
                .Setup(s => s.RowsLoaded += (o, e) => $"Rows loaded: {e.RowsLoaded}".Print())
                )
            {
                using (var w = new ChoJSONWriter(jsonOut))
                    w.Write(r.Take(10));
            }

            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }

        static void Issue170()
        {
            ChoETLFrxBootstrap.MaxArrayItemsToPrint = 1;
            string jsonFilePath = @"largetestdata\largetestdata.json";
            //string jsonFilePath = @"smallsubset.json";
            //ChoJSONExtensions.SplitJsonFile(jsonFilePath, new string[] { "ControlJob", "ProcessJobs", "ProcessRecipes", "RecipeSteps" },
            //                                 (directory, name, i, ext) => Path.Combine(directory, Path.ChangeExtension(name + $"_fragment_{i}", ext)));
            return;

            dynamic keys = null;
            dynamic attributes = null;

            //Capture Keys
            //using (var r = new ChoJSONReader(jsonFilePath).WithJSONPath("$..ControlJob.Keys"))
            //{
            //    keys = r.FirstOrDefault();
            //}

            ////Capture attributes
            //using (var r = new ChoJSONReader(jsonFilePath).WithJSONPath("$..ControlJob.Attributes"))
            //{
            //    attributes = r.FirstOrDefault();
            //}

            int fileCount = 0;
            using (var r = new ChoJSONReader(jsonFilePath)
                .WithJSONPath("$..ControlJob.ProcessJobs.ProcessRecipes.RecipeSteps")
                //.WithJSONPath("$..ControlJob.Attributes.ProcessJobs.ProcessRecipes[0].Keys")
                .NotifyAfter(1)
                .Setup(s => s.RowsLoaded += (o, e) => $"Rows loaded: {e.RowsLoaded} <- {DateTime.Now}".Print())
                //.Configure(c => c.JObjectLoadOptions = ChoJObjectLoadOptions.None )
                //.Configure(c => c.UseImplicitJArrayLoader = true)
                //.Configure(c => c.MaxJArrayItemsLoad = 10)
                .Configure(c => c.CustomJObjectLoader = (sr, s) =>
                {
                    string outFilePath = $@"RecipeSteps_{fileCount++}.json";
                    using (var jo = new ChoJObjectWriter(outFilePath))
                    {
                        jo.Formatting = Newtonsoft.Json.Formatting.Indented;

                        jo.WriteValue(new { x = 1, y = 2, z = new int[] { 1, 2 }, a = (string)null });
                        jo.WriteProperty("Null", null);
                        jo.WriteProperty("Keys", keys);
                        jo.WriteProperty("Attributes", attributes);
                        jo.WriteProperty("RecipeSteps", sr);
                    }
                    return ChoJSONObjects.EmptyJObject;
                })
            )
            {
                //r.Skip(10000).Take(1).Print();
                r.Loop(null, o => o.Print());
                //r.Count().Print();
            }

        }
        [Test]
        public static void Issue171()
        {
            string expected = @"{
  ""Jobs"": {
    ""Keys"": {
      ""JobID"": ""test123"",
      ""DeviceID"": ""TEST01""
    },
    ""Props"": {
      ""FileType"": ""Measurements"",
      ""InstrumentDescriptions"": [
        {
          ""InstrumentID"": ""1723007"",
          ""InstrumentType"": ""Actual1"",
          ""Name"": ""U"",
          ""DataType"": ""Double"",
          ""Units"": ""degC""
        },
        {
          ""InstrumentID"": ""2424009"",
          ""InstrumentType"": ""Actual2"",
          ""Name"": ""VG03"",
          ""DataType"": ""Double"",
          ""Units"": ""Pa""
        }
      ]
    },
    ""Steps"": {
      ""Keys"": {
        ""StepID"": ""START"",
        ""StepResult"": ""NormalEnd""
      },
      ""InstrumentData"": [
        {
          ""Keys"": {
            ""InstrumentID"": ""1723007""
          },
          ""Measurements"": [
            {
              ""DateTime"": ""2021-11-16 21:18:37.000"",
              ""Value"": 540
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.100"",
              ""Value"": 539
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.200"",
              ""Value"": 540
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.300"",
              ""Value"": 540
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.400"",
              ""Value"": 540
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.500"",
              ""Value"": 540
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.600"",
              ""Value"": 540
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.700"",
              ""Value"": 538
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.800"",
              ""Value"": 540
            }
          ]
        },
        {
          ""Keys"": {
            ""InstrumentID"": ""2424009""
          },
          ""Measurements"": [
            {
              ""DateTime"": ""2021-11-16 21:18:37.000"",
              ""Value"": 1333.22
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.100"",
              ""Value"": 1333.22
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.200"",
              ""Value"": 1333.22
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.300"",
              ""Value"": 1333.22
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.400"",
              ""Value"": 1333.22
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.500"",
              ""Value"": 1333.22
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.600"",
              ""Value"": 1333.22
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.700"",
              ""Value"": 1333.22
            },
            {
              ""DateTime"": ""2021-11-16 21:18:37.800"",
              ""Value"": 1333.22
            }
          ]
        }
      ]
    }
  }
}";

            string jsonFilePath = @"smallsubset1.json";

            dynamic keys = null;
            dynamic props = null;

            //Capture Keys
            using (var r = new ChoJSONReader(jsonFilePath).WithJSONPath("$..Job.Keys"))
            {
                keys = r.FirstOrDefault();
            }

            //Capture props
            using (var r = new ChoJSONReader(jsonFilePath).WithJSONPath("$..Job.Props"))
            {
                props = r.FirstOrDefault();
            }

            StringBuilder jsonOut = new StringBuilder();
            //Loop thro ReceipeSteps, write to individual files
            using (var r = new ChoJSONReader(jsonFilePath).WithJSONPath("$..Job.Steps")
                    .NotifyAfter(1)
                    .Setup(s => s.RowsLoaded += (o, e) => $"Rows loaded: {e.RowsLoaded} <- {DateTime.Now}".Print())

                    //Callback used to hook up to loader, stream the nodes to file (this avoids loading to memory)
                    .Configure(c => c.CustomJObjectLoader = (sr, s) =>
                    {
                        using (var topJo = new ChoJObjectWriter(jsonOut))
                        {
                            topJo.Formatting = Newtonsoft.Json.Formatting.Indented;
                            using (var jo = new ChoJObjectWriter("Jobs", topJo))
                            {
                                jo.WriteProperty("Keys", keys);
                                jo.WriteProperty("Props", props);
                                jo.WriteProperty("Steps", sr);
                            }
                        }

                        "".Print();

                        return ChoJSONObjects.EmptyJObject;
                    })
                  )
            {
                r.Loop();
            }

            var actual = jsonOut.ToString();
            Assert.AreEqual(expected, actual);
        }

        static void ReadXBTUSDFile()
        {
            string jsonFilePath = @"C:\Projects\GitHub\ChoETL\data\XBTUSD.json";

            using (var r = new ChoJSONReader(jsonFilePath)
                //.WithJSONPath("$..Column4")
                .NotifyAfter(10)
                .Setup(s => s.RowsLoaded += (o, e) => $"Rows loaded: {e.RowsLoaded}".Print())
                .Configure(c => c.CustomJObjectLoader = (re, s) =>
                {
                    //var x = JObject.Load(re);
                    ////re.Skip();
                    //return x;
                    return JObject.FromObject(new { Id = 1 });
                })
                )
            {
                r.Take(10).Loop(null, o => o.Print());
            }

        }
        public class Order2
        {
            public int Id { get; set; }
            //[ChoJSONPath("ShippingMethod.Code")]
            //public string ShippingMethod { get; set; }
            public JObject ShippingMethod { get; set; }
            [JsonIgnore]
            public string ShippingMethod1
            {
                get
                {
                    return (string)ShippingMethod?["Code"];
                }
            }
        }

        static void CustomMemberLoad()
        {
            string json = @"{
  'Id': 1,
  'ShippingMethod': {
     'Code': 'external_DHLExpressWorldwide',
     'Description': 'DHL ILS Express Worldwide'
  }
}";
            using (var r = ChoJSONReader<Order2>.LoadText(json)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                r.Print();
            }
        }

        static void FlattenByKeys()
        {
            string json = @"{
        ""key1"": ""val1"",
        ""key2"": {
            ""key2-1"": 
            [
                {
                    ""key2-arr1-1"": ""val2-arr1-1(1)"",
                    ""key2-arr1-2"": 
                    [
                        {
                            ""key2-arr1-arr2-1"" : ""val2-arr1-arr2-1(1)(1)"",
                            ""key2-arr1-arr2-2"" : 
                            [
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(1)(1)(1)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(1)(1)(1)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(1)(1)(1)""
                                },
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(1)(1)(2)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(1)(1)(2)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(1)(1)(2)""
                                },
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(1)(1)(3)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(1)(1)(3)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(1)(1)(3)""
                                }
                            ],
                            ""key2-arr1-arr2-3"" : ""val2-arr1-arr2-3(1)(1)""
                        },
                        {
                            ""key2-arr1-arr2-1"" : ""val2-arr1-arr2-1(1)(2)"",
                            ""key2-arr1-arr2-2"" : 
                            [
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(1)(2)(1)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(1)(2)(1)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(1)(2)(1)""
                                },
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(1)(2)(2)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(1)(2)(2)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(1)(2)(2)""
                                },
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(1)(2)(3)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(1)(2)(3)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(1)(2)(3)""
                                }
                            ],
                            ""key2-arr1-arr2-3"" : ""val2-arr1-arr2-3(1)(2)""
                        }
                    ]
                },
                {
                    ""key2-arr1-1"": ""val2-arr1-1(2)"",
                    ""key2-arr1-2"": 
                    [
                        {
                            ""key2-arr1-arr2-1"" : ""val2-arr1-arr2-1(2)(1)"",
                            ""key2-arr1-arr2-2"" : 
                            [
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(2)(1)(1)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(2)(1)(1)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(2)(1)(1)""
                                },
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(2)(1)(2)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(2)(1)(2)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(2)(1)(2)""
                                 },
                                 {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(2)(1)(3)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(2)(1)(3)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(2)(1)(3)""
                                 }
                            ],
                            ""key2-arr1-arr2-3"" : ""val2-arr1-arr2-3(2)(1)""
                        },
                        {
                            ""key2-arr1-arr2-1"" : ""val2-arr1-arr2-1(2)(2)"",
                            ""key2-arr1-arr2-2"" : 
                            [
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(2)(2)(1)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(2)(2)(1)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(2)(2)(1)""
                                },
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(2)(2)(2)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(2)(2)(2)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(2)(2)(2)""
                                },
                                {
                                    ""key2-arr1-arr2-arr3-1"" : ""val2-arr1-arr2-arr3-1(2)(2)(3)"",
                                    ""key2-arr1-arr2-arr3-2"" : ""val2-arr1-arr2-arr3-2(2)(2)(3)"",
                                    ""key2-arr1-arr2-arr3-3"" : ""val2-arr1-arr2-arr3-3(2)(2)(3)""
                                }
                            ],
                           ""key2-arr1-arr2-3"" : ""val2-arr1-arr2-3(2)(2)""
                        }
                    ]
                }
            ],
            ""key2-2"" : ""val2-2""
            },
        ""key3"": ""val3""
        }";


            //key2
            //key2 - 1
            //key2 - arr1 - 2
            //key2 - arr1 - arr2 - 2


            using (var r = ChoJSONReader.LoadText(json)
       .ErrorMode(ChoErrorMode.IgnoreAndContinue)
      )
            {
                var dt = r.OfType<object>().First().Flatten(nestedKeySeparator: '/').ToDictionary(kvp => kvp.Key, kvp => kvp.Value); //.AsDataTable();
                dt.Print();
            }
        }

        static string hierarchyJson = @"{
  ""id"": 3585,
  ""parentId"": 0,
  ""nodes"": [
    {
      ""id"": 3586,
      ""parentId"": 3585,
      ""nodes"": [
        {
          ""id"": 3587,
          ""parentId"": 3586,
          ""nodes"": null
        }
      ]
    },
    {
      ""id"": 3599,
      ""parentId"": 3585,
      ""nodes"": [
        {
          ""id"": 3600,
          ""parentId"": 3599,
          ""nodes"": null
        },
        {
          ""id"": 3601,
          ""parentId"": 3599,
          ""nodes"": null
        },
        {
          ""id"": 3602,
          ""parentId"": 3599,
          ""nodes"": null
        },
        {
          ""id"": 3603,
          ""parentId"": 3599,
          ""nodes"": null
        }
      ]
    },
    {
      ""id"": 3744,
      ""parentId"": 3585,
      ""nodes"": null
    }
  ]
}";

        static void HierachyLoad1()
        {

            using (var r = ChoJSONReader.LoadText(hierarchyJson).WithJSONPath("$..nodes")
                .WithField("id")
                )
            {
                using (var w = new ChoJSONWriter(Console.Out)
                    .SingleElement()
                    .SupportMultipleContent()
                    )
                    w.Write(new { nodes = r.ToArray() });
            }
        }

        static void HierachyLoad()
        {

            using (var r = ChoJSONReader.LoadText(hierarchyJson)
                .WithField("id")
                .WithField("nodes")
                //.UseJsonSerialization()
                .UseDefaultContractResolver()
                .IgnoreField("parentId")
                    .Configure(c => c.TurnOnAutoDiscoverJsonConverters = true)
                )
            {
                r.Print();
                return;

                using (var w = new ChoJSONWriter(Console.Out)
                    .SingleElement()
                    .SupportMultipleContent()
                .UseJsonSerialization()
                .UseDefaultContractResolver()
                .IgnoreField("parentId")
                    .Configure(c => c.TurnOnAutoDiscoverJsonConverters = true)
                    )
                    w.Write(new { nodes = r.ToArray() });
            }
        }

        public static void Issue179()
        {
            typeof(ChoJSONReader).GetAssemblyVersion().Print();

            var json = @"{
		""Message"": ""MsgName"",
		""TimestampLocal"": ""2022-02-02T12:06:18.3400276+11:00"",
		""TimestampGlobal"": ""2022-02-02T00:58:46.1036028Z"",
		""TargetComponentId"": ""compid_a"",
		""AxisData"": [
		  {
			""MaxTemperature"": 33.1,
			""PositionFrom"": 2660.0,
			""PositionTo"": 1311.0,
			""Name"": ""Xaxis"",
			""ComponentId"": ""Xaxis_compid"",
			""MaxCurrent"": 7692.0
		  },
		  {
			""MaxTemperature"": 31.9,
			""PositionFrom"": 2145.0,
			""PositionTo"": 254.0,
			""Name"": ""Zaxis"",
			""ComponentId"": ""Zaxis_compid"",
			""MaxCurrent"": 6566.0
		  },
		  {
			""PositionFrom"": 90.0,
			""PositionTo"": -90.0,
			""Name"": ""Caxis"",
			""ComponentId"": ""Caxis_compid"",
			""MaxCurrent"": 4432.0
		  }
		],
		""ActionId"": ""87990"",
		""ComponentId"": ""compid_b"",
		""Duration"": 0,
		""TransactionId"": ""0b10e099-69b8-4a8e-b704-d087ef0d6915""
		}";

            var conf = new ChoJSONRecordConfiguration
            {
                Culture = System.Globalization.CultureInfo.InvariantCulture,
                FlattenNode = true,
                NestedKeySeparator = null,
                UseNestedKeyFormat = true,
            };

            using
            (
                var r = ChoJSONReader.LoadText(json, conf)
                .ErrorMode(ChoErrorMode.ReportAndContinue)
            )
            {
                using (var w = new ChoCSVWriter(Console.Out)
                    .WithDelimiter(",")
                    .WithFirstLineHeader().UseNestedKeyFormat()
                      )
                {
                    w.Write(r);
                }
            }
        }

        public static void FlattenNodes()
        {
            string json = @"{
  ""data"": ""TestItems"",
  ""value"": [
    {
      ""Id"": 2,
      ""ProductId"": [
        1
      ],
      ""ProductName"": ""Tenant1""
    },
    {
      ""Id"": 3,
      ""ProductId"": [
        2,
        3,
        4
      ],
      ""ProductName"": ""Archlight""
    },
    {
      ""Id"": 4,
      ""ProductId"": [
        5,
        6
      ],
      ""ProductName"": ""Apple""
    },
    {
      ""Id"": 5,
      ""ProductId"": [
        2,
        3,
        4
      ],
      ""ProductName"": ""Samsung""
    }
  ]
}";

            using (var r = ChoJSONReader.LoadText(json)
                .Configure(c => c.NestedKeySeparator = '.')
                .Configure(c => c.FlattenNode = true)
                //.WithMaxScanNodes(5)
                .UseJsonSerialization()
                  )
            {
                //r.Print();
                //return;
                using (var w = new ChoCSVWriter(Console.Out)
                    .WithFirstLineHeader(true)
                    .WithField("data", m => m.Position(1))
                    .WithField("valueId", m => m.Position(2))
                    .WithField("value.productid", m => m.Position(4))
                    .WithField("value.productname", m => m.Position(3))

                    //.WithFields("data","valueId","productid", "valueProductName")
                    )
                    w.Write(r); //.Select(rec => new ChoDynamicObject(rec)).OfType<dynamic>().Select(rec => rec.RenameKeyAt(3, "productid")));
            }
        }

        [JsonConverter(typeof(ChoKnownTypeConverter<MilItem>))]
        [ChoKnownTypeDiscriminator("itemType")]
        [ChoKnownType(typeof(Weapon), "1")]
        [ChoKnownType(typeof(Armur), "2")]
        public abstract class MilItem
        {
            public virtual ItemType itemType => ItemType.NONE; // expression-bodied property
            public string name { get; set; }
            public string description { get; set; }
            public ItemRarity rarity { get; set; }

            public enum ItemType
            {
                NONE,
                WEAPON,
                ARMOUR,
                CONSUMABLE,
            }
            public enum ItemRarity
            {
                COMMON,
                UNCOMMON,
                RARE,
                MYTHIC,
                LEGENDARY,
            }
        }

        public class Weapon : MilItem
        {
            public override ItemType itemType => ItemType.WEAPON; // expression-bodied property
            public int damage { get; set; }
            public int critChance { get; set; }
        }

        public class Armur : MilItem
        {
            public override ItemType itemType => ItemType.ARMOUR; // expression-bodied property
            public int damage { get; set; }
            public int critChance { get; set; }
        }


        static void DeserializeDictWithAbstractValue()
        {
            string json = @"{
  ""excalibur"": {
    ""damage"": 9999,
    ""critChance"": 10,
    ""itemID"": ""excalibur"",
    ""iconLink"": """",
    ""name"": ""Excalibur"",
    ""description"": ""placeholder"",
    ""itemType"": 1,
    ""rarity"": 4,
    ""stackSize"": 1,
    ""canBeSold"": false,
    ""buyPrice"": 0,
    ""sellPrice"": 0
  },
  ""armur"": {
    ""damage"": 9999,
    ""critChance"": 10,
    ""itemID"": ""excalibur"",
    ""iconLink"": """",
    ""name"": ""Excalibur"",
    ""description"": ""placeholder"",
    ""itemType"": 2,
    ""rarity"": 4,
    ""stackSize"": 1,
    ""canBeSold"": false,
    ""buyPrice"": 0,
    ""sellPrice"": 0
  }

}";
            using (var r = ChoJSONReader<Dictionary<string, MilItem>>.LoadText(json)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                //.UseJsonSerialization()
                //.Configure(c => c.TurnOnAutoDiscoverJsonConverters = true)
                //.UseDefaultContractResolver()
                //.JsonSerializationSettings(s => s.Converters.Add(new ChoKnownTypeConverter(typeof(MilItem), "itemType", new Dictionary<string, Type>()
                //{
                //    { "1",  typeof(Weapon) },
                //    { "2",  typeof(Weapon) },
                //})))
                )
            {
                r.Print();
            }

        }

        public static void Issue190_1()
        {
            string json = @"
{
  ""mykey"": 1234,
  ""user"": {
      ""name"": ""asdf"",
      ""teamname"": ""b"",
      ""email"": ""c"",
      ""players"": [""1"", ""2""],
      ""playersex"": [""1"", ""2""]

  }
}";

            var conf = new ChoJSONRecordConfiguration
            {
                FlattenNode = false,
            };
            using (var r = ChoJSONReader.LoadText(json, conf)
                  )
            {
                r.FlattenBy("user", "playersex").ToArray().AsDataTable().Print();

            }
        }
        public static void Issue190()
        {
            string json = @"
{
  ""mykey"": 1234,
  ""user"": {
      ""name"": ""asdf"",
      ""teamname"": ""b"",
      ""email"": ""c"",
      ""players"": [""1"", ""2""]
  }
}";

            var conf = new ChoJSONRecordConfiguration
            {
                FlattenNode = true,
                IgnoreDictionaryFieldPrefix = true,
            };
            using (var r = ChoJSONReader.LoadText(json, conf)
                .WithField("mykey", jsonPath: "$.mykey", fieldName: "mykey", isArray: false)
                .WithField("username", jsonPath: "$.user.name", fieldName: "username", isArray: false)
                .WithField("player", jsonPath: "$.user.players[*]", fieldName: "player")
            )
            {
                //var dt = r.Flatten().AsDataTable();
                //r.Flatten().AsDataTable().Print();
                r.OfType<dynamic>().SelectMany(r1 => ((IList<dynamic>)r1.player).Select(r2 => new
                {
                    myKey = r1.myKey,
                    username = r1.username,
                    player = r2
                })).AsDataTable().Print();

            }
        }

        static void Issue190_2()
        {
            var json = @"{
  ""Message"": ""MessageX"",
  ""TimestampLocal"": ""2022-02-02T02:02:34.2830859+01:00"",
  ""TimestampGlobal"": ""2022-02-02T00:58:58.8800398Z"",
  ""PickerId"": ""1"",
  ""DestinationComponentId"": 0,
  ""SourceComponentId"": 0,
  ""ActionList"": {
    ""ActionId"": [
      4428,
	  4429
    ]
  },
  ""TransactionId"": ""999bd293-b040-4e88-a89f-180cf9307597"",
  ""TransactionType"": ""none""
}";

            var conf = new ChoJSONRecordConfiguration
            {
                Culture = System.Globalization.CultureInfo.InvariantCulture,
                //FlattenNode = true,
                ErrorMode = ChoErrorMode.IgnoreAndContinue,
            };

            using
            (
                var r = ChoJSONReader.LoadText(json, conf)
                    .WithField("TimestampGlobal", jsonPath: "$.TimestampGlobal", fieldType: typeof(DateTime), fieldName: "TimestampGlobal")
                    .WithField("TimestampLocal", jsonPath: "$.TimestampLocal", fieldType: typeof(DateTime), fieldName: "TimestampLocal")
                    .WithField("PickerId", jsonPath: "$.PickerId", fieldType: typeof(string), fieldName: "PickerId")
                    .WithField("DestinationComponentId", jsonPath: "$.DestinationComponentId", fieldType: typeof(int), fieldName: "DestinationComponentId")
                    .WithField("SourceComponentId", jsonPath: "$.SourceComponentId", fieldType: typeof(int), fieldName: "SourceComponentId")
                    //.WithField("ActionListIds", valueConverter: e => e != null ? String.Join(",", (IEnumerable<object>)e) : null, jsonPath: "$.ActionList.ActionId[*]", fieldType: typeof(string), fieldName: "ActionListIds")
                    .WithField("ActionListIds", jsonPath: "$.ActionList.ActionId[*]", fieldName: "ActionListIds")
                    .WithField("TransactionId", jsonPath: "$.TransactionId", fieldType: typeof(Guid), fieldName: "TransactionId")
                    .WithField("TransactionType", jsonPath: "$.TransactionType", fieldType: typeof(string), fieldName: "TransactionType")
            )
            {
                var dt = r.FlattenBy().AsDataTable();
                dt.Print();
            }
        }
        public static void Issue190_3()
        {
            var json = @"{
			""Message"": ""MsgName"",
			""TimestampLocal"": ""2022-02-15T09:44:46.1175766+01:00"",
			""TimestampGlobal"": ""2022-02-15T08:24:53.5248177Z"",
			""MachineId"": ""121212"",
			""PCName"": ""hijkl"",
			""VersionSDMC"": ""1.0.0.0"",
			""ModuleCount"": 5,
			""MachineNumber"": 5,
			""Location"": ""http://localhost:123"",
			""Machine"": ""abcdef"",
			""VersionUI"": ""1.8.2.5 [21-06]"",
			""PackagingUnitConfig"": {
				""Serialnumbers"": [
					{
						""Name"": ""Packaging Sealer"",
						""Value"": ""175150239""
					},
					{
						""Name"": ""Printer"",
						""Value"": ""16606""
					}
				]
			}
		}";


            using
            (
                var r = ChoJSONReader.LoadText(json)
                    .WithField("TimestampGlobal", jsonPath: "$.TimestampGlobal", fieldType: typeof(DateTime), fieldName: "TimestampGlobal")
                    .WithField("TimestampLocal", jsonPath: "$.TimestampLocal", fieldType: typeof(DateTime), fieldName: "TimestampLocal")
                    .WithField("PCName", jsonPath: "$.PCName", fieldType: typeof(string), fieldName: "PCName")
                    .WithField("VersionSDMC", jsonPath: "$.VersionSDMC", fieldType: typeof(string), fieldName: "VersionSDMC")
                    .WithField("ModuleCount", jsonPath: "$.ModuleCount", fieldType: typeof(int), fieldName: "ModuleCount")
                    .WithField("MachineNumber", jsonPath: "$.MachineNumber", fieldType: typeof(int), fieldName: "MachineNumber")
                    .WithField("Location", jsonPath: "$.Location", fieldType: typeof(string), fieldName: "Location")
                    .WithField("Machine", jsonPath: "$.Machine", fieldType: typeof(string), fieldName: "Machine")
                    .WithField("VersionUI", jsonPath: "$.VersionUI", fieldType: typeof(string), fieldName: "VersionUI")
                    .WithField("Serialnumbers", jsonPath: "$.PackagingUnitConfig.Serialnumbers[*]", fieldName: "Serialnumbers", valueConverter: o =>
                    {
                        var list = o as IList;
                        return list.OfType<dynamic>().Select(rec => new
                        {
                            PackagingUnitConfigSerialnumberName = rec.Name,
                            PackagingUnitConfigSerialnumberValue = rec.Value
                        }).ToArray();
                    })
            )
            {
                var dt = r.FlattenBy().AsDataTable();
                dt.Print();
            }
        }

        public static void Issue190_4()
        {
            var json = @"{
    ""Message"": ""RowaDoseConfig"",
    ""TimestampLocal"": ""2022-02-15T09:44:46.1175766+01:00"",
    ""TimestampGlobal"": ""2022-02-15T08:24:53.5248177Z"",
    ""MachineId"": ""10017747"",
    ""PCName"": ""NL5759RD05"",
    ""VersionSDMC"": ""1.0.0.0"",
    ""ModuleCount"": 5,
    ""MachineNumber"": 5,
    ""Location"": ""http://localhost:22223"",
    ""Machine"": ""NL5759RD05"",
    ""VersionUI"": ""1.8.2.5 [21-06]"",
    ""PackagingUnitConfig"": {
        ""Serialnumbers"": [
            {
                ""Name"": ""Packaging Sealer"",
                ""Value"": ""175150239""
            },
            {
                ""Name"": ""Printer"",
                ""Value"": ""16606""
            }
        ],
        ""UnitType"": ""Version1"",
        ""CrossSealTargetTemperatures"": [
            1400,
            1450
        ]
    }
}";

            using
            (
                var r = ChoJSONReader.LoadText(json)
                    .WithField("TimestampGlobal", jsonPath: "$.TimestampGlobal", fieldType: typeof(DateTime), fieldName: "TimestampGlobal")
                    .WithField("TimestampLocal", jsonPath: "$.TimestampLocal", fieldType: typeof(DateTime), fieldName: "TimestampLocal")
                    .WithField("PCName", jsonPath: "$.PCName", fieldType: typeof(string), fieldName: "PCName")
                    .WithField("VersionSDMC", jsonPath: "$.VersionSDMC", fieldType: typeof(string), fieldName: "VersionSDMC")
                    .WithField("ModuleCount", jsonPath: "$.ModuleCount", fieldType: typeof(int), fieldName: "ModuleCount")
                    .WithField("MachineNumber", jsonPath: "$.MachineNumber", fieldType: typeof(int), fieldName: "MachineNumber")
                    .WithField("Location", jsonPath: "$.Location", fieldType: typeof(string), fieldName: "Location")
                    .WithField("Machine", jsonPath: "$.Machine", fieldType: typeof(string), fieldName: "Machine")
                    .WithField("VersionUI", jsonPath: "$.VersionUI", fieldType: typeof(string), fieldName: "VersionUI")
                    .WithField("PackagingUnitConfig", jsonPath: "$.PackagingUnitConfig", fieldName: "PackagingUnitConfig",
                            valueConverter: o =>
                            {
                                var pcs = o as IList;
                                var result1 = pcs.OfType<dynamic>().SelectMany(puc => ((IList)puc.Serialnumbers).OfType<dynamic>()
                                    .Select(sn => new
                                    {
                                        PackagingUnitConfigUnitType = (string)puc.UnitType,
                                        PackagingUnitConfigCrossSealTargetTemperature = (long?)null,
                                        SerialnumberName = (string)sn.Name,
                                        SerialnumberValue = (string)sn.Value,
                                    })).ToArray();
                                var result2 = pcs.OfType<dynamic>().SelectMany(puc => ((IList)puc.CrossSealTargetTemperatures).OfType<long>()
                                    .Select(tt => new
                                    {
                                        PackagingUnitConfigUnitType = (string)puc.UnitType,
                                        PackagingUnitConfigCrossSealTargetTemperature = (long?)tt,
                                        SerialnumberName = String.Empty,
                                        SerialnumberValue = String.Empty,
                                    })).ToArray();

                                return result1.Union(result2).ToArray();
                            })
            )
            {
                var dt = r.FlattenBy().AsDataTable();
                dt.Print();
            }
        }
        public static void Issue182()
        {
            var json1 = @"{
    ""Message"": ""MessageName"",
    ""TimestampLocal"": ""2022-02-16T19:52:32.0585315+01:00"",
    ""TimestampGlobal"": ""2022-02-16T18:52:32.0870918Z"",
    ""FailedDrugsEntries"": [

    ]
}";
            var json = @"{
    ""Message"": ""MessageName"",
    ""TimestampLocal"": ""2022-02-16T19:52:32.0585315+01:00"",
    ""TimestampGlobal"": ""2022-02-16T18:52:32.0870918Z"",
    ""FailedDrugsEntries"": [
        {
            ""Batch"": ""batch-a"",
            ""Bag"": ""4321"",
            ""Drug"": ""00232323"",
            ""Drug_Name"": ""drug-a""
        },
        {
            ""Batch"": ""batch-b"",
            ""Bag"": ""8765"",
            ""Drug"": ""00434343"",
            ""Drug_Name"": ""drug-b""
        }
    ]
}";
            var conf = new ChoJSONRecordConfiguration
            {
                FlattenNode = true,
                ErrorMode = ChoErrorMode.IgnoreAndContinue
            };

            using
            (
                var r = ChoJSONReader.LoadText(json, conf)
                        .WithField("TimestampGlobal", jsonPath: "$.TimestampGlobal", fieldType: typeof(DateTime), fieldName: "TimestampGlobal")
                        .WithField("TimestampLocal", jsonPath: "$.TimestampLocal", fieldType: typeof(DateTime), fieldName: "TimestampLocal")
                        .WithField("FailedDrugsEntriesBatch", jsonPath: "$.FailedDrugsEntriesBatch", fieldType: typeof(string), fieldName: "FailedDrugsEntriesBatch")
                        .WithField("FailedDrugsEntriesBag", jsonPath: "$.FailedDrugsEntriesBag", fieldType: typeof(string), fieldName: "FailedDrugsEntriesBag")
                        .WithField("FailedDrugsEntriesDrug", jsonPath: "$.FailedDrugsEntriesDrug", fieldType: typeof(string), fieldName: "FailedDrugsEntriesDrug")
                        .WithField("FailedDrugsEntriesDrugName", jsonPath: "$.FailedDrugsEntriesDrug_Name", fieldType: typeof(string), fieldName: "FailedDrugsEntriesDrugName")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
            )
            {
                var recs = r.ToArray();
                var dt = recs.AsDataTable();
                dt.Print();
            }
        }
        public static void Issue182_1()
        {
            var json1 = @"{
    ""Message"": ""MessageName"",
    ""TimestampLocal"": ""2022-02-16T19:52:32.0585315+01:00"",
    ""TimestampGlobal"": ""2022-02-16T18:52:32.0870918Z"",
    ""FailedDrugsEntries"": [

    ]
}";
            var json = @"{
    ""Message"": ""MessageName"",
    ""TimestampLocal"": ""2022-02-16T19:52:32.0585315+01:00"",
    ""TimestampGlobal"": ""2022-02-16T18:52:32.0870918Z"",
    ""FailedDrugsEntries"": [
        {
            ""Batch"": ""batch-a"",
            ""Bag"": ""4321"",
            ""Drug"": ""00232323"",
            ""Drug_Name"": ""drug-a""
        },
        {
            ""Batch"": ""batch-b"",
            ""Bag"": ""8765"",
            ""Drug"": ""00434343"",
            ""Drug_Name"": ""drug-b""
        }
    ]
}";
            var conf = new ChoJSONRecordConfiguration
            {
                //FlattenNode = true,
                ErrorMode = ChoErrorMode.IgnoreAndContinue
            };

            using
            (
                var r = ChoJSONReader.LoadText(json, conf)
                        .WithField("TimestampGlobal", jsonPath: "$.TimestampGlobal", fieldType: typeof(DateTime), fieldName: "TimestampGlobal")
                        .WithField("TimestampLocal", jsonPath: "$.TimestampLocal", fieldType: typeof(DateTime), fieldName: "TimestampLocal")
                        .WithField("FailedDrugsEntries", jsonPath: "$.FailedDrugsEntries[*]", valueConverter: o =>
                        {
                            if (o == null)
                            {
                                dynamic rec = new ChoDynamicObject();
                                rec.Add("FailedDrugsEntriesBatch", null);
                                rec.Add("FailedDrugsEntriesBag", null);
                                rec.Add("FailedDrugsEntriesDrug", null);
                                rec.Add("FailedDrugsEntriesDrugName", null);
                                return new List<object> { rec };
                            }
                            else
                            {
                                IList recs = o as IList;
                                return recs.OfType<dynamic>().Select(rec =>
                                {
                                    rec.RenameKey("Batch", "FailedDrugsEntriesBatch");
                                    rec.RenameKey("Bag", "FailedDrugsEntriesBag");
                                    rec.RenameKey("Drug", "FailedDrugsEntriesDrug");
                                    rec.RenameKey("Drug_Name", "FailedDrugsEntriesDrugName");
                                    return rec;
                                }).ToArray();
                            }
                        })
            )
            {
                var recs = r.ToArray();
                var dt = recs.FlattenBy().AsDataTable();
                dt.Print();
            }
        }

        public class Substitution
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        public class Recipient
        {
            private List<Substitution> _substitutions;
            public string Name { get; private set; }
            public string EmailAddress { get; private set; }
            public IReadOnlyCollection<Substitution> Substitutions => _substitutions.AsReadOnly();

            [JsonConstructor]
            public Recipient(string name, string emailAddress, List<Substitution> substitutions)
            {
                EmailAddress = emailAddress;
                Name = name;
                _substitutions = substitutions;
            }
        }

        static void ReadOnlyCollectionSerializationTest()
        {
            var recipient = new Recipient("Foo Bar", "my-email@domain.com", new List<Substitution>
            {
                new Substitution
                {
                    Key = "Firstname", Value = "Foo"
                },
                new Substitution
                {
                    Key = "Lastname", Value = "Bar"
                },
            });

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<Recipient>(json))
            {
                w.Write(recipient);
            }

            using (var r = ChoJSONReader<Recipient>.LoadText(json.ToString())
                .UseJsonSerialization(false)
                )
            {
                r.Print();
            }
        }

        public static void Issue182_2()
        {
            var json = @"{
    ""Message"": ""MessageName"",
    ""TimestampLocal"": ""2022-02-16T19:52:32.0585315+01:00"",
    ""TimestampGlobal"": ""2022-02-16T18:52:32.0870918Z"",
    ""FailedDrugsEntries"": [
        {
            ""Batch"": ""batch-a"",
            ""Bag"": ""4321"",
            ""Drug"": ""00232323"",
            ""Drug_Name"": ""drug-a"",
            ""FailedDrugsEntries"": [
                {
                    ""Batch"": ""batch-a"",
                    ""Bag"": ""4321"",
                    ""Drug"": ""00232323"",
                    ""Drug_Name"": ""drug-a""
                },
                {
                    ""Batch"": ""batch-b"",
                    ""Bag"": ""8765"",
                    ""Drug"": ""00434343"",
                    ""Drug_Name"": ""drug-b""
                }
            ]
        },
        {
            ""Batch"": ""batch-b"",
            ""Bag"": ""8765"",
            ""Drug"": ""00434343"",
            ""Drug_Name"": ""drug-b""
        }
    ]
}";
            var conf = new ChoJSONRecordConfiguration
            {
                FlattenNode = false,
                ErrorMode = ChoErrorMode.IgnoreAndContinue
            };

            using
            (
                var r = ChoJSONReader.LoadText(json, conf)
                //.UseJsonSerialization()
                //.UseDefaultContractResolver()
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
            )
            {
                var recs = r.ToArray();
                var dt = recs.AsDataTable();
                dt.Print();
            }
        }

        static void Issue188_1()
        {
            var json = @"[
{
	    ""Name"": ""S2254SR1"",
	    ""Version"": ""8.1.3.0"",
	    ""Info"": ""WinCorpX.Exe 28.05.2021 12:47:38"",
	    ""Dlls"": {
	        ""dbexpint.dll"": ""7.0.2.113"",
	        ""dbxadapter30.dll"": ""11.0.2902.10471""
		}
},
{
	    ""Name"": ""S2254SR1"",
	    ""Version"": ""8.1.3.0"",
	    ""Info"": ""WinCorpX.Exe 28.05.2021 12:47:38"",
	    ""Dlls"": {
	        ""dbexpint.dll"": ""7.0.2.113"",
	        ""dbxadapter30.dll"": ""11.0.2902.10471""
		}
}
]";

            //ChoETLSettings.KeySeparator = ChoCharEx.Escape;
            using (var r = ChoJSONReader.LoadText(json)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                //using (var w = new ChoCSVWriter(Console.Out)
                //    .WithFirstLineHeader()
                //    .Configure(c => c.AddEOLDelimiterAtEOF = true)
                //    )
                //    w.Write(r);
                //return;
                //r.Print();
                //return;
                //var x = r.FirstOrDefault();
                DataTable x = r.Select(r1 => r1.Flatten()).AsDataTable();
                x.Print();
            }
        }

        public static void Issue191()
        {
            string json = @"{
  ""BrandId"": ""998877665544332211"",
  ""Categories"": [ ""112233445566778899"" ],
  ""Contact"": {
    ""Phone"": [
      {
        ""Value"": ""12346789"",
        ""Description"": { ""vi"": ""Phone"" },
        ""Type"": 1
      },
      {
        ""Value"": ""987654321"",
        ""Description"": { ""vi"": ""Phone"" },
        ""Type"": 1
      }
    ],
	 ""BlaBlaBlaPhone"": [
      {
        ""Value"": ""12346789"",
        ""Description"": { ""vi"": ""Phone"" },
        ""Type"": 1
      }
    ]
  }
}";

            using (var r = ChoJSONReader.LoadText(json)
                         .Configure(c => c.DefaultArrayHandling = false)
                         .Configure(c => c.FlattenNode = true)
                         .Configure(c => c.UseNestedKeyFormat = true)
                         .Configure(c => c.FlattenByNodeName = "Contact.Phone")
                         //.Configure(c => c.FlattenByJsonPath = "$..Contact")
                         //.Configure(c => c.IgnoreArrayIndex = false)
                         .Configure(c => c.NestedKeySeparator = '~')
                         .Configure(c => c.NestedKeySeparator = '.')
                         //.WithField("Phone", jsonPath: "$.['Contact.Phone.Value']", isArray: false)
                         )
            {
                //r.Print();
                //return;
                DataTable dt = r.AsDataTable();
                dt.DumpAsJson().Print();
            }

            //using (var r = ChoJSONReader.LoadText(json)
            //    .ClearFields()
            //       .WithField("BrandId").WithField("Category", jsonPath: "Categories1[0]")
            //      )
            //{
            //    r.AsDataTable().Print();
            //}
        }
        public static void NormalizeJSON()
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;

            string json = @"{
  ""videos"": [
    {
      ""file"": {
        ""S"": ""file1.mp4""
      },
      ""id"": {
        ""S"": ""1""
      },
      ""canvas"": {
        ""S"": ""This is Canvas1""
      }
    },
    {
      ""file"": {
        ""S"": ""main.mp4""
      },
      ""id"": {
        ""S"": ""0""
      },
      ""canvas"": {
        ""S"": ""this is a canvas""
      }
    }
  ]
}";

            using (var r = ChoJSONReader.LoadText(json)
                   .WithJSONPath("$..videos")
                   .WithField("file", jsonPath: "file.S", isArray: false)
                   .WithField("id", jsonPath: "id.S", isArray: false)
                   .WithField("canvas", jsonPath: "canvas.S", isArray: false)

                   )
            {
                using (var w = new ChoJSONWriter(Console.Out)
                    .SupportMultipleContent()
                    .SingleElement()
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                    )
                {
                    w.Write(new { Videos = r.ToArray() });
                }
            }
        }

        static void Issue194()
        {
            string json = @"{
	""MyDictionary"": {
	""1"": ""Value1"",
	""2"": ""Value2""
	},
	""MyArray"": [123, 456]
}";

            ChoETLSettings.ArrayBracketNotation = ChoArrayBracketNotation.Parenthesis;

            using (var r = ChoJSONReader.LoadText(json)
                .Configure(c => c.ArrayIndexSeparator = '.')
                .Configure(c => c.NestedKeySeparator = '.')
                )
            {
                r.AsDataTable().Print();
            }

        }

        public class ResponseX
        {
            public String Name { get; set; }
            public string Interval { get; set; }
            public List<PointX> Points { get; set; } = new List<PointX>();
        }

        [ChoJSONPathConverter]
        public class PointX
        {
            public Double Speed { get; set; }
            [ChoJSONPath("mid.h")]
            public Double High { get; set; }
            [ChoJSONPath("mid.l")]
            public Double Low { get; set; }
        }

        public static void SelectiveNodesAtChildrenTest()
        {
            typeof(ChoJSONReader).GetAssemblyVersion().Print();
            "".Print();

            string json = @"{
  ""name"":""xyz"",
  ""interval"": ""H1"",
  ""points"": [
    {  
      ""speed"": 1431, 
      ""mid"": { ""h"": ""1.07904"", ""l"": ""1.07872"" }
     }
  ]
}";
            using (var r = ChoJSONReader<ResponseX>.LoadText(json)
                //.Configure(c => c.TurnOnAutoDiscoverJsonConverters = true)
                .JsonSerializationSettings(s => s.Converters.Add(ChoJSONPathConverter.Instance))
                .JsonSerializerContext(c => c.StringComparision = StringComparison.InvariantCultureIgnoreCase)
                  )
            {
                r.Print();
            }

        }

        public static void FlattenNestedObjects()
        {
            string json = @"{
   ""Quantity"":0,
   ""QuantityUnit"":""pcs"",
   ""PartNumber"":""12345"",
   ""Parent"":"""",
   ""Children"":[
      {
         ""Quantity"":1,
         ""QuantityUnit"":""pcs"",
         ""PartNumber"":""88774"",
         ""Parent"":""12345"",
         ""Children"":[
            {
               ""Quantity"":1,
               ""QuantityUnit"":""pcs"",
               ""PartNumber"":""42447"",
               ""Parent"":""88774""
            },
            {
               ""Quantity"":0.420,
               ""QuantityUnit"":""kg"",
               ""PartNumber"":""12387"",
               ""Parent"":""88774""
            }
         ]
      }
   ]
}";

            using (var r = ChoJSONReader.LoadText(json)
                         .Configure(c => c.DefaultArrayHandling = false)
                         .Configure(c => c.FlattenNode = true)
                         .Configure(c => c.UseNestedKeyFormat = true)
                         //.Configure(c => c.FlattenByNodeName = "Children.Children")
                         .Configure(c => c.FlattenByJsonPath = "$..Children[*]")
                         //.Configure(c => c.IgnoreArrayIndex = false)
                         .Configure(c => c.NestedKeySeparator = '~')
                         .Configure(c => c.NestedKeySeparator = '.')
                         //.WithField("Phone", jsonPath: "$.['Contact.Phone.Value']", isArray: false)
                         )
            {
                r.Print();
                return;
                DataTable dt = r.AsDataTable();
                dt.DumpAsJson().Print();
            }
        }

        public static void FlattenNodeTest()
        {

            string json = @"{
  ""Id"": ""123456"",
  ""Request"": [
    {
      ""firstName"": ""A"",
      ""lastName"": ""B"",
    },
    {
      ""firstName"": ""A"",
      ""lastName"": ""B"",
    }
	],
  ""Response"": [
    {
      ""SId"": ""123""
    }
  ]
}";
            typeof(ChoJSONReader).GetAssemblyVersion().Print();
            "".Print();

            using (var r = ChoJSONReader.LoadText(json)
                   .Configure(c => c.FlattenNode = true).Configure(c => c.FlattenByNodeName = "Request")
                   )
            {
                using (var w = new ChoCSVWriter(Console.Out).WithFirstLineHeader())
                    w.Write(r);
            }
        }
        public static void Issue209()
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;

            var json = @"
			{
				""A"": 5.5,
				""B"": { ""b_arr"": [ 1.1, 2.2 ] },
				""C"": ""c_val"",
				""D"": { ""d_arr"": [ 3.3, 4.4 ] }
			}";

            dynamic[] r1 = null;
            using (var r = ChoJSONReader.LoadText(json).ErrorMode(ChoErrorMode.IgnoreAndContinue)
                   .Configure(c => c.NestedKeySeparator = '.')
                   .Configure(c => c.FlattenNode = true)
                   .Configure(c => c.FlattenByNodeName = "B.b_arr")
                   .WithField("Field_A", fieldName: "A").WithField("Field_B", fieldName: "B.b_arr").WithField("Field_C", fieldName: "C").WithField("Field_D", fieldName: "D!")
                   )
            {
                r1 = r.ToArray();
            }

            "".Print();

            dynamic[] r2 = null;
            using (var r = ChoJSONReader.LoadText(json).ErrorMode(ChoErrorMode.IgnoreAndContinue)
                   .Configure(c => c.NestedKeySeparator = '.')
                   .Configure(c => c.FlattenNode = true)
                   .Configure(c => c.FlattenByNodeName = "D.d_arr")
                   .WithField("Field_A", fieldName: "A").WithField("Field_B", fieldName: "B!").WithField("Field_C", fieldName: "C").WithField("Field_D", fieldName: "D.d_arr")
                   )
            {
                r2 = r.ToArray();
            }

            r1.Union(r2).AsDataTable().Print();
        }
        public static void Issue206()
        {
            typeof(ChoJSONReader).GetAssemblyVersion().Print();
            "".Print();

            string json = "{\"id\":\"123456789\",\"TimeRanges\":[[0,1],[1,2]]}";
            using (var r = ChoJSONReader.LoadText(json)
                   .Configure(c => c.DefaultArrayHandling = true)
                   .Configure(c => c.FlattenNode = true)
                   .Configure(c => c.UseNestedKeyFormat = true)
                   .Configure(c => c.FlattenByNodeName = "TimeRanges")
                   .Configure(c => c.NestedKeySeparator = '.')
                   .Configure(c => c.NestedKeySeparator = '.')
                   .WithMaxScanNodes(12)
                  )
            {
                //r.Print();
                //return;
                //var dt = r.AsDataTable();
                r.DumpAsJson().Print();
            }

        }

        static void Issue218()
        {
            var json = @"
{
    ""Data"": {
        ""Results"": [
            ""a"",
            ""b"",
            ""c""
        ]
    }
}
";

            dynamic[] r1 = null;

            using
            (
                var r = ChoJSONReader.LoadText(json)

                    .Configure(c => c.NestedKeySeparator = '.')
                    .Configure(c => c.FlattenNode = true)
                    .Configure(c => c.FlattenByNodeName = "Data.Results")
                    .WithField("DataResult", fieldName: "Data.Result")
            )
            {
                r1 = r.ToArray();
            }

            r1.AsDataTable().Print();
        }

        static void Issue226()
        {
            var json = @"{
  ""TimestampGlobal"": ""2022-04-26T14:00:31.6892162Z""
}";

            using
            (
                var r = ChoJSONReader.LoadText(json)
                    .WithField("TimestampGlobal", jsonPath: "$.TimestampGlobal", fieldType: typeof(DateTime), fieldName: "TimestampGlobal", isArray: false)
                    .Configure(c => c.Culture = CultureInfo.InvariantCulture)
                    .Configure(c => c.TypeConverterFormatSpec = new ChoTypeConverterFormatSpec { DateTimeFormat = "o" })
            )
            {
                var dt1 = r.Select(k => k.AsDictionary()["TimestampGlobal"]).Cast<DateTime>().First();
                dt1.Millisecond.Print();

                // Compare with .NET
                var dt2 = DateTime.Parse("2022-04-26T14:00:31.6892162Z");
                dt2.Millisecond.Print();
            }
        }

        static void Issue228()
        {
            string json = @"{
      ""id"": ""4b5260d2-e088-4546-a315-b9c4b274406f"",
      ""type"": ""donut"",
      ""name"":""cake"",
      ""flavours"": [""chocolate"",""blueberry"",""vanilla""],
      ""batters"": {
        ""topping"":""glazed"",
        ""category"":[ ""eggless"",""flavoured""]
      }     
}";

            ChoTypeDescriptor.RegisterTypeConvertersForType<IList, ArrayToStringConverter>();
            ChoTypeDescriptor.RegisterTypeConvertersForType<string, StringToArrayConverter>();

            StringBuilder jsonOut = new StringBuilder();
            Func<object, object> array2StringConverter = o => String.Join(";", ((IList)o).OfType<object>().Select(i => i.ToNString()));
            using (var r = ChoJSONReader.LoadText(json)
                //.UseJsonSerialization()
                .WithJSONPath("$", true)
                //.UseDefaultContractResolver()
                //.WithField("id")
                //.WithField("type")
                //.WithField("name")
                //.WithField("flavours"/*, valueConverter: array2StringConverter*/, propertyConverter: new ArrayToStringConverter())
                //.WithField("batters/topping", jsonPath: "batters.topping", isArray: false)
                //.WithField("batters/category", jsonPath: "batters.category[*]", isArray: true/*, valueConverter: array2StringConverter*/,
                //        propertyConverter: new ArrayToStringConverter())
                .Configure(c => c.ConvertToFlattenObject = true)
                .Configure(c => c.NestedKeySeparator = '/')
                )
            {
                //r.Print();
                //return;

                using (var w = new ChoJSONWriter(jsonOut)
                    .SingleElement()
                    .SupportMultipleContent()
                    //.WithFirstLineHeader()
                    //.UseNestedKeyFormat(false)
                    //.Configure(c => c.RegisterTypeConverterForType<List<object>>(new ListToStringConverters()))
                    )
                    w.Write(r);
            }

            jsonOut.Print();
            return;

            StringBuilder jsonOut1 = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(jsonOut.ToString())
                //.UseJsonSerialization()
                .WithJSONPath("$", true)
                //.UseDefaultContractResolver()
                .Configure(c => c.ConvertToNestedObject = true)
                .Configure(c => c.NestedKeySeparator = '/')
                )
            {
                //r.Print();
                //return;
                using (var w = new ChoJSONWriter(jsonOut1)
                    //.WithFirstLineHeader()
                    //.UseNestedKeyFormat(false)
                    //.Configure(c => c.RegisterTypeConverterForType<List<object>>(new ListToStringConverters()))
                    )
                    w.Write(r);
            }

            jsonOut1.Print();
        }

        static void Issue228_2()
        {
            string json = @"{
      ""id"": ""4b5260d2-e088-4546-a315-b9c4b274406f"",
      ""type"": ""donut"",
      ""name"":""cake"",
      ""flavours"": [""chocolate"",""blueberry"",""vanilla""],
      ""batters"": {
        ""topping"":""glazed"",
        ""category"":[ ""eggless"",""flavoured""]
      }     
}";
            Func<object, object> array2StringConverter = o => String.Join(";", ((IList)o).OfType<object>().Select(i => i.ToNString()));
            StringBuilder jsonOut = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$", true)
                .WithField("id")
                .WithField("type")
                .WithField("name")
                .WithField("flavours", valueConverter: array2StringConverter)
                .WithField("batters/topping", jsonPath: "batters.topping", isArray: false)
                .WithField("batters/category", jsonPath: "batters.category[*]", isArray: true, valueConverter: array2StringConverter)
                )
            {
                using (var w = new ChoJSONWriter(jsonOut)
                    .SingleElement()
                    .SupportMultipleContent()
                    //.WithFirstLineHeader()
                    //.UseNestedKeyFormat(false)
                    //.Configure(c => c.RegisterTypeConverterForType<List<object>>(new ListToStringConverters()))
                    )
                    w.Write(r);
            }

            jsonOut.Print();

            StringBuilder jsonOut1 = new StringBuilder();
            Func<object, object> string2ArrayConverter = o => ((string)o).Split(";").ToArray();
            using (var r = ChoJSONReader.LoadText(jsonOut.ToString())
                //.UseJsonSerialization()
                .WithJSONPath("$", true)
                .WithField("id")
                .WithField("type")
                .WithField("name")
                .WithField("flavours", valueConverter: string2ArrayConverter)
                .WithField("batters/topping", isArray: false)
                .WithField("batters/category", isArray: true, valueConverter: string2ArrayConverter)
                .Configure(c => c.ConvertToNestedObject = true)
                .Configure(c => c.NestedKeySeparator = '/')
                )
            {
                //r.Print();
                //return;
                using (var w = new ChoJSONWriter(jsonOut1)
                    .SingleElement()
                    .SupportMultipleContent()
                    )
                    w.Write(r);
            }

            jsonOut1.Print();
        }

        public class ArrayToStringConverter : IChoValueConverter, IChoCollectionConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is IList && ((IList)value).OfType<object>().All(i => i is string))
                    return String.Join(";", ((IList)value).OfType<object>().Select(i => i.ToNString()));
                else
                    return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value;
            }
        }

        public class StringToArrayConverter : IChoCollectionConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                string val = value as string;
                if (val == null)
                    return value;

                if (!val.Contains(";"))
                    return val;

                return val.Split(";").ToArray();
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value;
            }
        }

        static void Issue234()
        {
            var json = @"{
  ""TimestampGlobal"": ""2022-08-06T19:36:46.1043075+00:00""
}";

            using
            (
                var r = ChoJSONReader.LoadText(json)
                    .WithField("TimestampGlobal", jsonPath: "$.TimestampGlobal", fieldType: typeof(DateTimeOffset), fieldName: "TimestampGlobal")
                    //.Configure(c => c.Culture = CultureInfo.InvariantCulture)
                    //.Configure(c => c.TypeConverterFormatSpec = new ChoTypeConverterFormatSpec { DateTimeFormat = "o" })
                    .JsonSerializationSettings(s => s.DateParseHandling = DateParseHandling.DateTimeOffset)
            )
            {
                //var dt1 = r.Select(k => k.AsDictionary()["TimestampGlobal"]).Cast<DateTimeOffset>().First();
                var dt1 = (DateTimeOffset)r.First().TimestampGlobal;
                dt1.Offset.Print(); // 02:00:00

                // Compare with .NET
                var dt2 = DateTimeOffset.Parse("2022-08-06T19:36:46.1043075+00:00");
                dt2.Offset.Print(); // 00:00:00
            }
        }

        public static void Issue235()
        {
            var json = @"{
		  ""Message"": ""RowaDoseOrderResult"",
		  ""TimestampLocal"": ""2022-02-26T12:00:04.48904+01:00"",
		  ""TimestampGlobal"": ""2022-02-26T09:56:31.6487665Z"",
		  ""OrderIdentifier"": ""135e3f64-1270-4ef1-add6-06e03d037075"",
		  ""Pouches"": [
			{
			  ""PouchIdentifier"": ""2202250205364"",
			  ""Pills"": [
				{
				  ""MedId"": ""01839853"",
				  ""SourceId"": ""91B4C612"",
				  ""SourceType"": ""Canister"",
				  ""RequestedAmount"": 1.0,
				  ""DispensedAmount"": 1.0
				},
				{
				  ""MedId"": ""01849875"",
				  ""SourceId"": ""8D96B152"",
				  ""SourceType"": ""Canister"",
				  ""RequestedAmount"": 2.0,
				  ""DispensedAmount"": 2.0
				},
				{
				  ""MedId"": ""02735229"",
				  ""SourceId"": ""91B53206"",
				  ""SourceType"": ""Canister"",
				  ""RequestedAmount"": 1.0,
				  ""DispensedAmount"": 1.0
				},
				{
				  ""MedId"": ""02069482"",
				  ""SourceId"": ""9045F100"",
				  ""SourceType"": ""Canister"",
				  ""RequestedAmount"": 1.0,
				  ""DispensedAmount"": 1.0
				},
				{
				  ""MedId"": ""02292815"",
				  ""SourceId"": ""907C49B7"",
				  ""SourceType"": ""Canister"",
				  ""RequestedAmount"": 1.0,
				  ""DispensedAmount"": 1.0
				}
			  ]
			}
		  ]
		}";

            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            typeof(ChoCSVReader).GetAssemblyVersion().Print();
            typeof(ChoJSONReader).GetAssemblyVersion().Print();

            using
            (
                var r = ChoJSONReader.LoadText(json)

                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)

                    .Configure(c => c.Culture = CultureInfo.InvariantCulture)
                    .Configure(c => c.FlattenNode = true)
            //.Configure(c => c.FlattenByJsonPath = "$.Pouches[*].Pills[*]")

            .JsonSerializationSettings(s => s.DateParseHandling = DateParseHandling.DateTimeOffset)

            .WithField("TimestampGlobal", jsonPath: "$.TimestampGlobal", typeof(DateTime), fieldName: "TimestampGlobal")
            .WithField("TimestampLocal", jsonPath: "$.TimestampLocal", typeof(DateTimeOffset), fieldName: "TimestampLocal")
            .WithField("OrderIdentifier", jsonPath: "$.OrderIdentifier", typeof(string), fieldName: "OrderIdentifier")
            .WithField("PouchIdentifier", jsonPath: "$.PouchesPouchIdentifier", typeof(string), fieldName: "PouchIdentifier")
            .WithField("PouchPillMedId", jsonPath: "$.PouchesPillsMedId", typeof(string), fieldName: "PouchPillMedId")
            .WithField("PouchPillSourceId", jsonPath: "$.PouchesPillsSourceId", typeof(string), fieldName: "PouchPillSourceId")
            .WithField("PouchPillSourceType", jsonPath: "$.PouchesPillsSourceType", typeof(string), fieldName: "PouchPillSourceType")
            .WithField("PouchPillRequestedAmount", jsonPath: "$.PouchesPillsRequestedAmount", typeof(decimal), fieldName: "PouchPillRequestedAmount")
            .WithField("PouchPillDispensedAmount", jsonPath: "$.PouchesPillsDispensedAmount", typeof(decimal), fieldName: "PouchPillDispensedAmount")
            )
            {
                var dt = r.AsDataTable();
                dt.Print();
            }
        }

        public class DynAddress
        {
            public string Number { get; set; }
            public string Street { get; set; }
            public string City { get; set; }
            public string Country { get; set; }
        }

        public class DynPerson
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public DynAddress PostalAddress { get; set; }
        }
        static void DynamicPropertyMapping()
        {
            var person = new DynPerson
            {
                Name = "Tom",
                Age = 10,
                PostalAddress = new DynAddress
                {
                    Number = "10",
                    Street = "Main St.",
                    City = "New York",
                    Country = "USA",
                },
            };

            Action<Type, MemberInfo, string, JsonProperty> remapJsonProperty = (t, mi, pn, jsonProp) =>
            {
                if (t == typeof(DynAddress))
                {
                    if (pn == nameof(DynAddress.Number))
                        jsonProp.PropertyName = "Num";
                    else if (pn == nameof(DynAddress.Street))
                        jsonProp.PropertyName = "Str";
                }
                else if (t == typeof(DynPerson))
                {
                    if (pn == nameof(DynPerson.Name))
                        jsonProp.PropertyName = "N";
                    else if (pn == nameof(DynPerson.PostalAddress))
                        jsonProp.PropertyName = "PAddress";
                }
            };

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<DynPerson>(json)
                .UseJsonSerialization()
                .UseDefaultContractResolver()
                .SingleElement()
                .SupportMultipleContent()
                .Configure(c => c.RemapJsonProperty = remapJsonProperty)
                )
            {
                w.Write(person);
            }

            json.Print();

            string json1 = @"{
  ""N"": ""Tom"",
  ""Age"": 10,
  ""PAddress"": {
    ""Num"": ""10"",
    ""Str"": ""Main St."",
    ""City"": ""New York"",
    ""Country"": ""USA""
  }
}";
            using (var r = ChoJSONReader<DynPerson>.LoadText(json.ToString())
                .UseJsonSerialization()
                .UseDefaultContractResolver()
                .Configure(c => c.RemapJsonProperty = remapJsonProperty)
                )
            {
                r.Print();
            }
        }

        static void Issue239()
        {
            var json = @"
{
  ""Message"": ""ABC"",
  ""TimestampLocal"": ""2021-06-10T10:15:54.5147158+00:00"",
  ""TimestampGlobal"": ""2021-06-10T10:14:45.2005202Z""
}";

            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            typeof(ChoJSONReader).GetAssemblyVersion().Print();

            using
            (
                var r = ChoJSONReader.LoadText(json)

                    .Configure(c => c.Culture = CultureInfo.InvariantCulture)

                    .JsonSerializationSettings(s => s.DateParseHandling = DateParseHandling.DateTimeOffset)

                    .WithField("Message", jsonPath: "$.Message", typeof(string), fieldName: "Message")
                    .WithField("TimestampLocal", jsonPath: "$.TimestampLocal", typeof(DateTimeOffset?), fieldName: "TimestampLocal")
                    .WithField("TimestampGlobal", jsonPath: "$.TimestampGlobal", typeof(DateTime?), fieldName: "TimestampGlobal")
            )
            {
                r.AsDataTable().Print();
            }
        }

        static void Issue240()
        {
            var json = @"
{
  ""Name"": ""ABC"",
    ""Data"": {
        ""Result"": ""ok"",
        ""Results"": [
            ""x"",
            ""y""
        ]
    }
}";

            using
            (
                var r = ChoJSONReader.LoadText(json)
                    .WithField("Name", jsonPath: "$.Name", typeof(string), fieldName: "Name")
                    .WithField("DataResult", jsonPath: "$.Data.Result", typeof(string), fieldName: "DataResult")
                    .WithField("DataResults", jsonPath: "$.Data.Results[*]", typeof(string[]), fieldName: "DataResults")
            )
            {
                r.Flatten(true).AsDataTable().Print();
            }
        }

        public class RandomNumberRoot
        {
            [ChoJSONPath("$~*")] //json path to capture keys
            public string[] Keys { get; set; }

            [ChoJSONPath("$^*")] //json path to capture values
            public RandomNumberData[] Values { get; set; }
        }

        public class RandomNumberData
        {
            public string id { get; set; }
            public int t { get; set; }
            public int tn { get; set; }
            public string tid { get; set; }
            public string txzone { get; set; }
            public string sth { get; set; }
            public string stv { get; set; }
            public string slu { get; set; }
            public string slht { get; set; }
            public string iu { get; set; }
            public string iti { get; set; }
            public string ide { get; set; }
            public string ish { get; set; }
            public string isv { get; set; }
            public string it { get; set; }
            public string ic { get; set; }
            public string isp { get; set; }
            public string il { get; set; }
            public string idi { get; set; }
            public string ilu { get; set; }
            public int tod_h { get; set; }
            public int tod_v { get; set; }
            public int cur_online { get; set; }
            public string tot_v { get; set; }
            public string tot_h { get; set; }
            public string last_hits { get; set; }
        }

        static void Issue260()
        {
            string json = @"{
  ""random_number1"": {
    ""t"": 100800,
    ""tn"": 1671933005,
    ""tid"": ""279"",
    ""txzone"": ""Asia/Tbilisi"",
    ""sth"": ""450228"",
    ""stv"": ""203986"",
    ""slu"": ""1587888024"",
    ""slht"": ""1656684260"",
    ""iu"": ""test"",
    ""iti"": ""testtest"",
    ""ide"": "".."",
    ""ish"": ""0"",
    ""isv"": ""0"",
    ""it"": ""0"",
    ""ic"": ""23"",
    ""isp"": ""1"",
    ""il"": ""0"",
    ""idi"": ""1587888024"",
    ""ilu"": ""1587888024"",
    ""tod_h"": 0,
    ""tod_v"": 0,
    ""cur_online"": 0,
    ""tot_v"": ""203986"",
    ""tot_h"": ""450228"",
    ""last_hits"": ""1656684260""
  },
  ""random_number2"": {
    ""t"": 3343566,
    ""tn"": 4444456,
    ""tid"": ""279"",
    ""txzone"": ""Asia/Tbilisi"",
    ""sth"": ""453456"",
    ""stv"": ""664"",
    ""slu"": ""1587888024"",
    ""slht"": ""1656684260"",
    ""iu"": ""test"",
    ""iti"": ""testtest"",
    ""ide"": "".."",
    ""ish"": ""0"",
    ""isv"": ""0"",
    ""it"": ""0"",
    ""ic"": ""23"",
    ""isp"": ""1"",
    ""il"": ""0"",
    ""idi"": ""1587888024"",
    ""ilu"": ""1587888024"",
    ""tod_h"": 0,
    ""tod_v"": 0,
    ""cur_online"": 0,
    ""tot_v"": ""203986"",
    ""tot_h"": ""450228"",
    ""last_hits"": ""1656684260""
  },
}";
            using (var r = ChoJSONReader<RandomNumberRoot>.LoadText(json)
                )
            {
                using (var w = new ChoCSVWriter<RandomNumberData>(Console.Out)
                    .WithFirstLineHeader())
                {
                    var recs = r.Select(rec => rec.Keys.Zip(rec.Values, (k, v) =>
                    {
                        v.id = k;
                        return v;
                    })).SelectMany(t => t);

                    w.Write(recs);
                }
            }
        }
        [Test]
        public static void Issue263_1()
        {
            string json = @"[
  {
    ""id"": 104004101,
    ""objs"": {
      ""aList"": [
        {
          ""type"": ""sub"",
          ""value"": 133
        }
      ],
      ""bList"": [
        {
          ""type"": ""add"",
          ""subid"": 10400401
        }
      ]
    }
  },
  {
    ""id"": 104004102,
    ""objs"": {
      ""aList"": [
        {
          ""type"": ""sub"",
          ""value"": 133
        },
        {
          ""type"": ""sub"",
          ""value"": 233
        },
        {
          ""type"": ""sub"",
          ""value"": 433
        }
      ],
      ""bList"": [
        {
          ""type"": ""add"",
          ""subid"": 10400401
        },
        {
          ""type"": ""add"",
          ""subid"": 10400402
        }
      ]
    }
  },
  {
    ""id"": 104004103,
    ""objs"": {
      ""aList"": [
        {
          ""type"": ""sub"",
          ""value"": 333
        }
      ],
      ""bList"": [
        {
          ""type"": ""add"",
          ""subid"": 10400403
        }
      ]
    }
  }
]";
            string expectedCSV = @"id,objs/aList/0/type,objs/aList/0/value,objs/aList/1/type,objs/aList/1/value,objs/aList/2/type,objs/aList/2/value,objs/bList/0/type,objs/bList/0/subid,objs/bList/1/type,objs/bList/1/subid
""104004101"",""sub"",""133"","""","""","""","""",""add"",""10400401"","""",""""
""104004102"",""sub"",""133"",""sub"",""233"",""sub"",""433"",""add"",""10400401"",""add"",""10400402""
""104004103"",""sub"",""333"","""","""","""","""",""add"",""10400403"","""",""""";

            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter(csv).WithFirstLineHeader()
                             .QuoteAllFields()
                             .WithMaxScanRows(3)
                             .ThrowAndStopOnMissingField(false)
                             .NestedKeySeparator('/')
                             .ErrorMode(ChoErrorMode.IgnoreAndContinue))
                {
                    w.Write(r);
                }
            }

            Console.WriteLine("print csv====");
            csv.Print();
            var actualCSV = csv.ToString();
            Assert.AreEqual(expectedCSV, actualCSV);

            Console.WriteLine("print json====");

            StringBuilder jsonOut = new StringBuilder();
            // how to restore the csv to original json ?
            using (var r = ChoCSVReader.LoadText(csv.ToString())
                .NestedKeySeparator('/')
                .WithFirstLineHeader()
                .WithMaxScanRows(3) // convert string to number
                .QuoteAllFields())
            {
                var recs = r.ToArray();

                using (var w = new ChoJSONWriter(jsonOut)
                       .Configure(c => c.ThrowAndStopOnMissingField = false)
                       .Setup(s => s.BeforeRecordFieldWrite += (o, e) =>
                       {
                           if (e.Source is ChoDynamicObject dobj)
                           {
                               e.Source = dobj.IgnoreNullValues();
                           }
                       })
                      )
                {
                    w.Write(recs);
                }
            }
            json.Print();
            var actualJSON = jsonOut.ToString();
            Assert.AreEqual(json, actualJSON);
        }
        [Test]
        public static void Issue263_2()
        {
            string json = @"[
  {
    ""id"": 104004101,
    ""objs"": {
      ""aList"": [
        {
          ""type"": ""sub"",
          ""value"": 133
        }
      ],
      ""bList"": [
        {
          ""type"": ""add"",
          ""subid"": 10400401
        }
      ]
    }
  },
  {
    ""id"": 104004102,
    ""objs"": {
      ""aList"": [
        {
          ""type"": ""sub"",
          ""value"": 133
        },
        {
          ""type"": ""sub"",
          ""value"": 233
        },
        {
          ""type"": ""sub"",
          ""value"": 433
        }
      ],
      ""bList"": [
        {
          ""type"": ""add"",
          ""subid"": 10400401
        },
        {
          ""type"": ""add"",
          ""subid"": 10400402
        }
      ]
    }
  },
  {
    ""id"": 104004103,
    ""objs"": {
      ""aList"": [
        {
          ""type"": ""sub"",
          ""value"": 333
        }
      ],
      ""bList"": [
        {
          ""type"": ""add"",
          ""subid"": 10400403
        }
      ]
    }
  }
]";
            string expectedCSV = @"id,objs/aList/0/type,objs/aList/0/value,objs/aList/1/type,objs/aList/1/value,objs/aList/2/type,objs/aList/2/value,objs/bList/0/type,objs/bList/0/subid,objs/bList/1/type,objs/bList/1/subid
""104004101"",""sub"",""133"","""","""","""","""",""add"",""10400401"","""",""""
""104004102"",""sub"",""133"",""sub"",""233"",""sub"",""433"",""add"",""10400401"",""add"",""10400402""
""104004103"",""sub"",""333"","""","""","""","""",""add"",""10400403"","""",""""";

            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter(csv).WithFirstLineHeader()
                             .QuoteAllFields()
                             .WithMaxScanRows(3)
                             .ThrowAndStopOnMissingField(false)
                             .NestedKeySeparator('/')
                             .ErrorMode(ChoErrorMode.IgnoreAndContinue))
                {
                    w.Write(r);
                }
            }

            Console.WriteLine("print csv====");
            csv.Print();
            var actualCSV = csv.ToString();
            Assert.AreEqual(expectedCSV, actualCSV);

            Console.WriteLine("print json====");

            StringBuilder jsonOut = new StringBuilder();
            // how to restore the csv to original json ?
            using (var r = ChoCSVReader.LoadText(csv.ToString())
                .NestedKeySeparator('/')
                .WithFirstLineHeader()
                .WithMaxScanRows(3) // convert string to number
                .QuoteAllFields())
            {
                var recs = r.ToArray();

                using (var w = new ChoJSONWriter(jsonOut)
                       .Configure(c => c.ThrowAndStopOnMissingField = false)
                       .NullValueHandling(ChoNullValueHandling.Ignore)
                       .Setup(s => s.BeforeRecordFieldWrite += (o, e) =>
                       {
                           if (e.Source is ChoDynamicObject dobj)
                           {
                               e.Source = dobj.IgnoreNullValues();
                           }
                       })
                      )
                {
                    w.Write(recs);
                }
            }
            json.Print();
            var actualJSON = jsonOut.ToString();
            Assert.AreEqual(json, actualJSON);
        }
        [Test]
        public static void Issue263_3()
        {
            string json = @"[
  {
    ""id"": 104004101,
    ""objs"": {
      ""aList"": [
        {
          ""type"": ""sub"",
          ""value"": 133
        }
      ],
      ""bList"": [
        {
          ""type"": ""add"",
          ""subid"": 10400401
        }
      ]
    }
  },
  {
    ""id"": 104004102,
    ""objs"": {
      ""aList"": [
        {
          ""type"": ""sub"",
          ""value"": 133
        },
        {
          ""type"": ""sub"",
          ""value"": 233
        },
        {
          ""type"": ""sub"",
          ""value"": 433
        }
      ],
      ""bList"": [
        {
          ""type"": ""add"",
          ""subid"": 10400401
        },
        {
          ""type"": ""add"",
          ""subid"": 10400402
        }
      ]
    }
  },
  {
    ""id"": 104004103,
    ""objs"": {
      ""aList"": [
        {
          ""type"": ""sub"",
          ""value"": 333
        }
      ],
      ""bList"": [
        {
          ""type"": ""add"",
          ""subid"": 10400403
        }
      ]
    }
  }
]";
            string expectedCSV = @"id,objs/aList/0/type,objs/aList/0/value,objs/aList/1/type,objs/aList/1/value,objs/aList/2/type,objs/aList/2/value,objs/bList/0/type,objs/bList/0/subid,objs/bList/1/type,objs/bList/1/subid
""104004101"",""sub"",""133"","""","""","""","""",""add"",""10400401"","""",""""
""104004102"",""sub"",""133"",""sub"",""233"",""sub"",""433"",""add"",""10400401"",""add"",""10400402""
""104004103"",""sub"",""333"","""","""","""","""",""add"",""10400403"","""",""""";

            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter(csv).WithFirstLineHeader()
                             .QuoteAllFields()
                             .WithMaxScanRows(3)
                             .ThrowAndStopOnMissingField(false)
                             .NestedKeySeparator('/')
                             .ErrorMode(ChoErrorMode.IgnoreAndContinue))
                {
                    w.Write(r);
                }
            }

            Console.WriteLine("print csv====");
            csv.Print();
            var actualCSV = csv.ToString();
            Assert.AreEqual(expectedCSV, actualCSV);

            Console.WriteLine("print json====");

            StringBuilder jsonOut = new StringBuilder();
            // how to restore the csv to original json ?
            using (var r = ChoCSVReader.LoadText(csv.ToString())
                .NestedKeySeparator('/')
                .WithFirstLineHeader()
                .WithMaxScanRows(3) // convert string to number
                .QuoteAllFields())
            {
                var recs = r.ToArray();

                using (var w = new ChoJSONWriter(jsonOut)
                       .Configure(c => c.ThrowAndStopOnMissingField = false)
                       .Setup(s => s.BeforeRecordFieldWrite += (o, e) =>
                       {
                           if (e.Source is ChoDynamicObject dobj)
                           {
                               e.Source = IgnoreNullValues(dobj);
                           }
                       })
                      )
                {
                    w.Write(recs);
                }
            }
            json.Print();
            var actualJSON = jsonOut.ToString();
            Assert.AreEqual(json, actualJSON);
        }
        public static object IgnoreNullValues(object src)
        {
            if (!(src is ChoDynamicObject)) return src;

            ChoDynamicObject dest = new ChoDynamicObject();
            foreach (var kvp in (ChoDynamicObject)src)
            {
                if (kvp.Value is ChoDynamicObject dobj)
                {
                    if (HasAllNullValues(dobj))
                        continue;

                    dest.Add(kvp.Key, kvp.Value);
                }
                else if (kvp.Value is IList list)
                {
                    List<object> output = new List<object>();
                    foreach (var item in list)
                    {
                        if (item is ChoDynamicObject dobj1)
                        {
                            if (HasAllNullValues(dobj1))
                                continue;
                        }

                        output.Add(item);
                    }
                    if (output.Count > 0)
                        dest.Add(kvp.Key, output.ToArray());
                }
                else
                    dest.Add(kvp.Key, kvp.Value);
            }

            return HasAllNullValues(dest) ? null : dest;
        }


        private static bool HasAllNullValues(ChoDynamicObject src)
        {
            foreach (var v in src.Values)
            {
                if (v == null)
                    continue;
                else if (v is IList list)
                {
                    foreach (var item in list.OfType<ChoDynamicObject>())
                    {
                        if (!HasAllNullValues(item))
                            return false;
                    }
                }
                else if (v is ChoDynamicObject v1)
                {
                    if (HasAllNullValues(v1))
                        continue;
                    else
                        return false;
                }
                else
                    return false;
            }

            return true;
        }

        static void Issue263()
        {
            string json = @"{
  ""BATCH_CODE"": [ ""1"", ""2"" ],
  ""WIP_CODE"": []
}";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter(csv)
                    .Configure(c => c.NestedKeySeparator = '/')
                    .Configure(c => c.ArrayIndexSeparator = '_')
                    .WithFirstLineHeader())
                {
                    w.Write(r);
                }
            }

            csv.Print();

            using (var r = ChoCSVReader.LoadText(csv.ToString())
                    .Configure(c => c.NestedKeySeparator = '/')
                    .Configure(c => c.ArrayIndexSeparator = '_')
                .WithFirstLineHeader())
            {
                using (var w = new ChoJSONWriter(Console.Out)
                    .SupportMultipleContent()
                    .SingleElement()
                   // .WithField("BATCH_CODE", isArray: true)
                   //.WithField("WIP_CODE", isArray: true)
                   .Configure(c => c.ThrowAndStopOnMissingField = false)
                   )
                {
                    w.Write(r);
                }
            }
        }

        static void CreateLargeCSVFile()
        {
            string jsonFilePath = @"C:\Projects\GitHub\ChoETL\data\largetestdata\largetestdata.json";
            string csvFilePath = @"C:\Projects\GitHub\ChoETL\data\largetestdata\largetestdata.csv";
            using (var r = new ChoJSONReader(jsonFilePath)
                //.WithJSONPath("$..ControlJob.Attributes.SensorDescriptions")
                .WithJSONPath("$..ControlJob.ProcessJobs.ProcessRecipes.RecipeSteps")
                .NotifyAfter(1)
                .Setup(s => s.RowsLoaded += (o, e) => $"Rows loaded: {e.RowsLoaded} <- {DateTime.Now}".Print())
                )
            {
                using (var w = new ChoCSVWriter(csvFilePath)
                    .WithFirstLineHeader())
                    w.Write(r.First());
            }

        }

        public class MetaResults
        {
            [JsonProperty("meta")]
            public Meta Meta { get; set; }
            [JsonProperty("results")]
            public List<Result2> Results { get; set; }
        }
        public class Meta
        {
            [JsonProperty("request_id")]
            public string RequestId { get; set; }
            [JsonProperty("warnings")]
            public string[] Warnings { get; set; }
        }

        static string nestedJsonData = @"{
  ""meta"": {
    ""warnings"": [  ],
    ""page"": {
      ""current"": 1,
      ""total_pages"": 1,
      ""total_results"": 2,
      ""size"": 10
    },
    ""request_id"": ""6887a53f701a59574a0f3a7012e01aa8""
  },
  ""results"": [
    {
      ""phone"": {
        ""raw"": 3148304280.0
      },
      ""accounts_balance_ach"": {
        ""raw"": 27068128.71
      },
      ""accounts_balance_pending"": {
        ""raw"": ""46809195.64""
      },
      ""email"": {
        ""raw"": ""Brisa34@hotmail.com""
      },
      ""accounts_count"": {
        ""raw"": 6.0
      },
      ""id"": {
        ""raw"": ""c98808a2-d7d6-4444-834d-2fe4f6858f6b""
      },
      ""display_name"": {
        ""raw"": ""The Johnstons""
      },
      ""type"": {
        ""raw"": ""Couple""
      },
      ""advisor_email"": {
        ""raw"": ""Cornelius_Schiller14@hotmail.com""
      },
      ""created_at"": {
        ""raw"": ""2018-10-02T10:42:07+00:00""
      },
      ""source"": {
        ""raw"": ""event""
      },
      ""accounts_balance"": {
        ""raw"": 43629003.47
      },
      ""accounts_donations"": {
        ""raw"": 38012278.75
      },
      ""advisor_name"": {
        ""raw"": ""Cloyd Jakubowski""
      },
      ""_meta"": {
        ""score"": 0.42934617
      }
    },
    {
      ""phone"": {
        ""raw"": 2272918612.0
      },
      ""accounts_balance_ach"": {
        ""raw"": 35721452.35
      },
      ""accounts_balance_pending"": {
        ""raw"": ""35117465.2""
      },
      ""email"": {
        ""raw"": ""Ruby87@yahoo.com""
      },
      ""accounts_count"": {
        ""raw"": 1.0
      },
      ""id"": {
        ""raw"": ""687af11f-0f73-4112-879c-1108303cb07a""
      },
      ""display_name"": {
        ""raw"": ""Kennith Johnston""
      },
      ""type"": {
        ""raw"": ""Individual""
      },
      ""advisor_email"": {
        ""raw"": ""Evangeline_Wisoky92@hotmail.com""
      },
      ""created_at"": {
        ""raw"": ""2018-10-02T16:16:02+00:00""
      },
      ""source"": {
        ""raw"": ""website""
      },
      ""accounts_balance"": {
        ""raw"": 23063874.19
      },
      ""accounts_donations"": {
        ""raw"": 33025175.79
      },
      ""advisor_name"": {
        ""raw"": ""Ernie Mertz""
      },
      ""_meta"": {
        ""score"": 0.39096162
      }
    }
  ]
}";

        public class Result2
        {
            [JsonProperty("phone")]
            public string Phone { get; set; }
            [JsonProperty("accounts_balance_ach")]
            public string AccountsBalanceACH { get; set; }
        }

        [Test]
        public static void DeserializeNestedData_1()
        {
            string expected = @"[
  {
    ""meta"": {
      ""request_id"": ""6887a53f701a59574a0f3a7012e01aa8"",
      ""warnings"": []
    },
    ""results"": [
      {
        ""phone"": ""3148304280"",
        ""accounts_balance_ach"": ""27068128.71""
      },
      {
        ""phone"": ""2272918612"",
        ""accounts_balance_ach"": ""35721452.35""
      }
    ]
  }
]";

            using (var r = ChoJSONReader<MetaResults>.LoadText(nestedJsonData)
                .WithJSONPath("$", true)
                .Setup(s => s.BeforeRecordFieldLoad += (o, e) =>
                {
                    var src = e.Source as JObject;
                    if (src != null && src.ContainsKey("raw"))
                        e.Source = src["raw"];
                })
                )
            {
                var recs = r.ToArray();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void DeserializeNestedData_2()
        {
            string expected = @"[
  {
    ""phone"": ""3148304280"",
    ""accounts_balance_ach"": ""27068128.71""
  },
  {
    ""phone"": ""2272918612"",
    ""accounts_balance_ach"": ""35721452.35""
  }
]";

            using (var r = ChoJSONReader<Result2>.LoadText(nestedJsonData)
                .WithJSONPath("$..results")
                .Setup(s => s.BeforeRecordFieldLoad += (o, e) =>
                {
                    var src = e.Source as JObject;
                    e.Source = src["raw"];
                })
                )
            {
                var recs = r.ToArray();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Result3
        {
            [JsonProperty("phone")]
            [ChoJSONPath("phone.raw")]
            public string Phone { get; set; }
            [JsonProperty("accounts_balance_ach")]
            [ChoJSONPath("accounts_balance_ach.raw")]
            public string AccountsBalanceACH { get; set; }
        }

        [Test]
        public static void DeserializeNestedData_3()
        {
            string expected = @"[
  {
    ""phone"": ""3148304280"",
    ""accounts_balance_ach"": ""27068128.71""
  },
  {
    ""phone"": ""2272918612"",
    ""accounts_balance_ach"": ""35721452.35""
  }
]";

            using (var r = ChoJSONReader<Result3>.LoadText(nestedJsonData)
                .WithJSONPath("$..results")
                )
            {
                var recs = r.ToArray();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class MetaResults4
        {
            [JsonProperty("meta")]
            public Meta4 Meta { get; set; }
            [JsonProperty("results")]
            public List<Result4> Results { get; set; }
        }
        public class Meta4
        {
            [JsonProperty("request_id")]
            public string RequestId { get; set; }
            [JsonProperty("warnings")]
            public string[] Warnings { get; set; }
        }

        [JsonConverter(typeof(ChoJsonPathJsonConverter))]
        public class Result4
        {
            [JsonProperty("phone")]
            [ChoJSONPath("phone.raw")]
            public string Phone { get; set; }
            [JsonProperty("accounts_balance_ach")]
            [ChoJSONPath("accounts_balance_ach.raw")]
            public string AccountsBalanceACH { get; set; }
        }

        [Test]
        public static void DeserializeNestedData_4()
        {
            string expected = @"[
  {
    ""meta"": {
      ""request_id"": ""6887a53f701a59574a0f3a7012e01aa8"",
      ""warnings"": []
    },
    ""results"": [
      {
        ""phone"": ""3148304280"",
        ""accounts_balance_ach"": ""27068128.71""
      },
      {
        ""phone"": ""2272918612"",
        ""accounts_balance_ach"": ""35721452.35""
      }
    ]
  }
]";

            using (var r = ChoJSONReader<MetaResults4>.LoadText(nestedJsonData)
                .WithJSONPath("$", true)
                )
            {
                var recs = r.ToArray();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class MyObject1
        {
            public string Label { get; set; }  // JSON Property Name less "ABC_" ("Column1", "Column2", or "Whatever")
            public string Option { get; set; } // JSON Property Value
        }

        public class MyDto
        {
            [JsonProperty(PropertyName = "Known Column 1")]
            public string KnownColumn1 { get; set; }

            [JsonProperty(PropertyName = "Known Column 2")]
            public string KnownColumn2 { get; set; }

            [JsonProperty(PropertyName = "Known Column 3")]
            public string KnownColumn3 { get; set; }

            public List<MyObject1> ABCs { get; set; } = new List<MyObject1>();
        }

        [Test]
        public static void DeserializeDynamicNameJSONPropertiesToListOfObjects()
        {
            string json = @"[
    {
        ""Known Column 1"": ""val 1"",
        ""Known Column 2"": ""val 2"",
        ""Known Column 3"": ""val 3"",
        ""ABC_Column1"": ""xxx"",
        ""ABC_Column2"": ""yyy"",
        ""ABC_Whatever"": ""zzz""
    },
    {
        ""Known Column 1"": ""val 4"",
        ""Known Column 2"": ""val 5"",
        ""Known Column 3"": ""val 6"",
        ""ABC_Column1"": ""aaa"",
        ""ABC_Column2"": ""bbb"",
        ""ABC_Whatever"": ""ccc""
    }
]";

            string expected = @"[
  {
    ""Known Column 1"": ""val 1"",
    ""Known Column 2"": ""val 2"",
    ""Known Column 3"": ""val 3"",
    ""ABCs"": [
      {
        ""Label"": ""ABC_Column1"",
        ""Option"": ""xxx""
      },
      {
        ""Label"": ""ABC_Column2"",
        ""Option"": ""yyy""
      },
      {
        ""Label"": ""ABC_Whatever"",
        ""Option"": ""zzz""
      }
    ]
  },
  {
    ""Known Column 1"": ""val 4"",
    ""Known Column 2"": ""val 5"",
    ""Known Column 3"": ""val 6"",
    ""ABCs"": [
      {
        ""Label"": ""ABC_Column1"",
        ""Option"": ""aaa""
      },
      {
        ""Label"": ""ABC_Column2"",
        ""Option"": ""bbb""
      },
      {
        ""Label"": ""ABC_Whatever"",
        ""Option"": ""ccc""
      }
    ]
  }
]";
            using (var r = ChoJSONReader<MyDto>.LoadText(json)
                .WithField(f => f.ABCs, customSerializer: o =>
                {
                    List<MyObject1> list = new List<MyObject1>();
                    JObject obj = o as JObject;
                    foreach (var kvp in obj)
                    {
                        if (!kvp.Key.StartsWith("ABC_"))
                            continue;

                        list.Add(new MyObject1 { Label = kvp.Key, Option = kvp.Value.ToNString() });
                    }
                    return list;
                })
                )
            {
                var recs = r.ToArray();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void ReadJsonDateFieldTest()
        {
            string json = @"{
  ""Abbrev"": ""ALEX"",
  ""MeetingDate"": ""\/Date(1679058000000+1100)\/""
}";

            string expected = @"[
  {
    ""Abbrev"": ""ALEX"",
    ""MeetingDate"": ""2023-03-17T09:00:00-04:00""
  }
]";
            using (var r = ChoJSONReader.LoadText(json))
            {
                var recs = r.ToArray();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class PFGetForm
        {
            public string Abbrev { get; set; }
            public DateTime MeetingDate { get; set; }
        }

        [Test]
        public static void ReadJsonDateFieldTest_1()
        {
            string json = @"{
  ""Abbrev"": ""ALEX"",
  ""MeetingDate"": ""\/Date(1679058000000+1100)\/""
}";

            string expected = @"[
  {
    ""Abbrev"": ""ALEX"",
    ""MeetingDate"": ""2023-03-17T09:00:00-04:00""
  }
]";
            using (var r = ChoJSONReader<PFGetForm>.LoadText(json))
            {
                var recs = r.ToArray();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class PFGetForm2
        {
            public string Abbrev { get; set; }
            public string MeetingDate { get; set; }
        }

        [Test]
        public static void ReadJsonDateFieldTest_2()
        {
            string json = @"{
  ""Abbrev"": ""ALEX"",
  ""MeetingDate"": ""\/Date(1679058000000+1100)\/""
}";

            string expected = @"[
  {
    ""Abbrev"": ""ALEX"",
    ""MeetingDate"": ""3/17/2023 9:00:00 AM""
  }
]";
            using (var r = ChoJSONReader<PFGetForm2>.LoadText(json))
            {
                var recs = r.ToArray();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void AsDataTableWithArrayNodeTest()
        {
            string json = @"
	{
		""user"": {
			""name"": ""asdf"",
			""teamname"": ""b"",
			""email"": ""c"",
			""players"": [""1"", ""2""]
		}
	}";

            string expected = @"name,teamname,email,players_0,players_1
asdf,b,c,1,2";

            using (var r = ChoJSONReader<UserInfo>.LoadText(json)
                .WithJSONPath("$.user")
                .Configure(c => c.ArrayValueNamePrefix = String.Empty)
                )
            {
                var dt = r.AsDataTable();
                var actual = dt.ToStringEx();
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void DesrializeComplexJSON_Dynamic()
        {
            string json = @"[{
  ""id"": 1111,
  ""product_id"": [
    2222,
    ""test 1""
  ],
  ""product_qty"": 1.0,
  ""picking_date"": false,
  ""partner_id"": [
    10,
    ""Funeral""
  ],
  ""picking_id"": [
    20,
    ""Testing""
  ],
  ""picking_state"": ""cancel""
}, 
{
  ""id"": 2222,
  ""product_id"": false,
  ""product_qty"": 1.0,
  ""picking_date"": ""2023-08-11 10:10:39"",
  ""partner_id"": false,
  ""picking_id"": false,
  ""picking_state"": ""cancel""
}]";
            string expected = @"[
  [
    2222,
    ""test 1""
  ],
  false
]";
            using (var r = ChoJSONReader.LoadText(json))
            {
                var recs = r.Select(r1 => r1.product_id).ToArray();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        public partial class Product1
        {
            [JsonProperty("id")]
            public long Id { get; set; }

            [JsonProperty("product_id")]
            public List<object> ProductId { get; set; }

            [JsonProperty("product_qty")]
            public long ProductQty { get; set; }

            [JsonProperty("picking_date")]
            public object PickingDate { get; set; }

            [JsonProperty("partner_id")]
            public List<object> PartnerId { get; set; }

            [JsonProperty("picking_id")]
            public List<object> PickingId { get; set; }

            [JsonProperty("picking_state")]
            public string PickingState { get; set; }
        }
        [Test]
        public static void DesrializeComplexJSON_POCO()
        {
            string json = @"[{
  ""id"": 1111,
  ""product_id"": [
    2222,
    ""test 1""
  ],
  ""product_qty"": 1.0,
  ""picking_date"": false,
  ""partner_id"": [
    10,
    ""Funeral""
  ],
  ""picking_id"": [
    20,
    ""Testing""
  ],
  ""picking_state"": ""cancel""
}, 
{
  ""id"": 2222,
  ""product_id"": false,
  ""product_qty"": 1.0,
  ""picking_date"": ""2023-08-11 10:10:39"",
  ""partner_id"": false,
  ""picking_id"": false,
  ""picking_state"": ""cancel""
}]";
            string expected1 = @"[
  [
    2222,
    ""test 1""
  ],
  [
    false
  ]
]";
            string expected2 = @"[
  false,
  ""2023-08-11 10:10:39""
]";
            using (var r = ChoJSONReader<Product1>.LoadText(json))
            {
                var recs = r.ToArray();
                var productIds = recs.SelectMany(r1 => r1.ProductId).ToArray();
                var pickingDates = recs.Select(r1 => r1.PickingDate).ToArray();

                var actual1 = JsonConvert.SerializeObject(productIds, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected1, actual1);

                var actual2 = JsonConvert.SerializeObject(pickingDates, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected2, actual2);
            }
        }
        public class DeviceStates
        {
            [JsonProperty("tenantId")]
            public string TenantId { get; set; }
            [JsonProperty("tenantName")]
            public string TenantName { get; set; }
            [JsonProperty("statisticsPerDay")]
            public Dictionary<string, DayStatistic> StatisticsPerDay { get; set; }
        }

        public class DayStatistic
        {
            [JsonProperty("lora")]
            public Lora Lora { get; set; }
        }

        public class Lora
        {
            [JsonProperty("counters")]
            public Counters Counters { get; set; }
        }

        public class Counters
        {
            [JsonProperty("virtualMsgIn")]
            public int VirtualMsgIn { get; set; }

            [JsonProperty("numberOfSources")]
            public int NumberOfSources { get; set; }

            [JsonProperty("msgIn")]
            public int MsgIn { get; set; }

            [JsonProperty("bytesIn")]
            public int BytesIn { get; set; }
        }

        [Test]
        public static void DeserializeDictionaryProperty()
        {
            string json = @"{
  ""tenantId"": ""62b8c3a9d7b7c57a8d78c5b16fb4"",
  ""tenantName"": ""TEST IOT"",
  ""statisticsPerDay"": {
    ""2023-08-07"": {
      ""lora"": {
        ""counters"": {
          ""virtualMsgIn"": 34,
          ""numberOfSources"": 1,
          ""msgIn"": 34,
          ""bytesIn"": 1428
        }
      }
    },
    ""2023-08-08"": {
      ""lora"": {
        ""counters"": {
          ""virtualMsgIn"": 22,
          ""numberOfSources"": 1,
          ""msgIn"": 22,
          ""bytesIn"": 924
        }
      }
    },
    ""2023-08-05"": {
      ""lora"": {
        ""counters"": {
          ""virtualMsgIn"": 13,
          ""numberOfSources"": 1,
          ""msgIn"": 13,
          ""bytesIn"": 546
        }
      }
    }
  }
}";
            string expected = @"";
            using (var r = ChoJSONReader<DeviceStates>.LoadText(json))
            {
                var recs = r.FirstOrDefault();

                recs.Print();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(json, actual);
            }

        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            Sample32();
            return;

            Issue235();
            return;
            //ChoDynamicObjectSettings.DictionaryType = DictionaryType.Regular;
            //dynamic dict = new ChoDynamicObject();
            //Parallel.For(0, 1000, i =>
            //{
            //    dict.Configuration = 1;
            //});
            //return;

            FlattenNestedObjects();
            return;

            SelectiveNodesAtChildrenTest();
            return;

            DeserializeDictWithAbstractValue();
            return;

            FlattenNodes();
            return;

            Issue179();

            HierachyLoad();
            //DeserializeNestedObjectOfList();
            return;

            ReadJsonOneItemAtATime();
            return;

            JSON2CSV9();
            //JSON2CSV9();
            //DeserializeInnerArrayToObjects();

            //CreateLargeJSONFile();
            //JSON2CSVViceVersa();
        }

        static void SimpleTest()
        {
        }

        public class VarObject
        {

        }

        public class MyResponse : IChoRecordFieldSerializable
        {
            [JsonProperty(PropertyName = "starttime")]
            public string StartTime { get; set; }
            [JsonProperty(PropertyName = "endtime")]
            public string EndTime { get; set; }
            public Dictionary<string, VarObject> VarData { get; set; }

            public bool RecordFieldDeserialize(object record, long index, string propName, ref object value)
            {
                if (propName == nameof(VarData))
                {
                    return true;
                }
                else
                    return false;
            }

            public bool RecordFieldSerialize(object record, long index, string propName, ref object value)
            {
                throw new NotImplementedException();
            }
        }

        static void DictTest2()
        {
            string json = @"[
  {
    ""starttime"": ""...1"",
    ""endtime"": ""...."",
    ""var1"": {},
    ""var2"": {}
  },
  {
    ""starttime"": ""...1"",
    ""endtime"": ""...."",
    ""var1"": {},
    ""var3"": {}
  },
  {
    ""starttime"": ""...1"",
    ""endtime"": ""...."",
    ""var1"": {}
  }
]";
            using (var r = ChoJSONReader<MyResponse>.LoadText(json)
                .WithField(f => f.VarData, customSerializer: o => new Dictionary<string, VarObject> { { "1", new VarObject() } })
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        public class RegistrantInfoResponse
        {
            public string questionid { get; set; }
            public string fieldname { get; set; }
            public string name { get; set; }
            public object response { get; set; }

            public override bool Equals(object obj)
            {
                var response = obj as RegistrantInfoResponse;
                return response != null &&
                       questionid == response.questionid &&
                       fieldname == response.fieldname &&
                       name == response.name;// &&
                                             //EqualityComparer<object>.Default.Equals(this.response, response.response);
            }

            public override int GetHashCode()
            {
                var hashCode = -333780040;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(questionid);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(fieldname);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
                return hashCode;
            }
        }
        public class CompareableDictionary<TKey, TValue> : Dictionary<TKey, TValue>//, IComparer<CompareableDictionary<TKey, TValue>>
        {
            public override bool Equals(object obj)
            {
                var response = obj as CompareableDictionary<TKey, TValue>;
                bool retVal = response != null &&
                    Count == response.Count;
                if (retVal)
                {
                    foreach (var item in this)
                    {
                        if (!response.ContainsKey(item.Key))
                            return false;
                        var responseItem = response[item.Key];
                        if (!item.Value.Equals(responseItem))
                            return false;
                    }
                    return true;
                }
                return retVal;
            }
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
            //public int Compare(CompareableDictionary<TKey, TValue> x, CompareableDictionary<TKey, TValue> y)
            //{
            //    throw new NotImplementedException();
            //}
        }
        public class RegistrantInfo
        {
            public string attendeeid { get; set; }
            public Dictionary<int, RegistrantInfoResponse> responses { get; set; }

            public override bool Equals(object obj)
            {
                var info = obj as RegistrantInfo;
                return info != null &&
                       attendeeid == info.attendeeid &&
                       new DictionaryEqualityComparer<int, RegistrantInfoResponse>().Equals(responses, info.responses);
            }

            public override int GetHashCode()
            {
                var hashCode = 1425576453;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(attendeeid);
                hashCode = hashCode * -1521134295 + new DictionaryEqualityComparer<int, RegistrantInfoResponse>().GetHashCode(responses);
                return hashCode;
            }
        }

        [Test]
        public static void DictTest1()
        {
            List<RegistrantInfo> expected = new List<RegistrantInfo>
            {
                new RegistrantInfo{ attendeeid = "1", responses = new Dictionary<int, RegistrantInfoResponse>() }
            };
            expected[0].responses.Add(1, new RegistrantInfoResponse { questionid = "1", fieldname = "1", name = "question?", response = new object[] { " Identify need", " Evaluate products and services" } });
            expected[0].responses.Add(2, new RegistrantInfoResponse { questionid = "2", fieldname = "2", name = "question2", response = "live" });
            List<object> actual = new List<object>();

            string json = @"
{
  ""attendeeid"": ""1"",
  ""responses"": {
    ""1"": {
      ""questionid"": ""1"",
      ""fieldname"": ""1"",
      ""name"": ""question?"",
      ""pageid"": ""2"",
      ""page"": ""Attendee Information"",
      ""auto_capitalize"": ""0"",
      ""choicekey"": """",
      ""response"": [
        "" Identify need"",
        "" Evaluate products and services""
      ]
    },
    ""2"": {
      ""questionid"": ""2"",
      ""fieldname"": ""2"",
      ""name"": ""question2"",
      ""pageid"": ""2"",
      ""page"": ""Attendee Information"",
      ""auto_capitalize"": ""0"",
      ""choicekey"": ""live"",
      ""response"": ""live""
    }
  }
}";
            foreach (var rec in ChoJSONReader<RegistrantInfo>.LoadText(json))
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }

        public class Item
        {
            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("timestamp")]
            public int Timestamp { get; set; }

            [JsonProperty("event")]
            public string Event { get; set; }

            [JsonProperty("category")]
            public List<string> Categories { get; set; }
        }

        [Test]
        public static void SingleOrArrayItemTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"email","john.doe@sendgrid.com"},{"timestamp", (int)1337966815 }, { "event", "open" }, { "category", new object[] { "newuser", "transactional" } } },
                new ChoDynamicObject{{"email", "jane.doe@sendgrid.com" },{"timestamp", (int)1337966815 }, { "event", "open" }, { "category", new object[] { "olduser" } } }
            };
            List<object> actual = new List<object>();

            string json = @"[
  {
    ""email"": ""john.doe@sendgrid.com"",
    ""timestamp"": 1337966815,
    ""category"": [
      ""newuser"",
      ""transactional""
    ],
    ""event"": ""open""
  },
  {
    ""email"": ""jane.doe@sendgrid.com"",
    ""timestamp"": 1337966815,
    ""category"": ""olduser"",
    ""event"": ""open""
  }
]";
            foreach (var rec in ChoJSONReader<Item>.LoadText(json))
            {
                actual.Add(rec);
            }

            var actualText = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            var expectedText = JsonConvert.SerializeObject(expected, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expectedText, actualText);
            //CollectionAssert.AreEqual(expected, actual);
        }

        public class Mesh : IChoRecordFieldSerializable
        {
            public Triangle[] Triangles { get; set; }

            public override bool Equals(object obj)
            {
                var mesh = obj as Mesh;
                return mesh != null &&
                       new ArrayEqualityComparer<Triangle>().Equals(Triangles, mesh.Triangles);
            }

            public override int GetHashCode()
            {
                return 1511374898 + new ArrayEqualityComparer<Triangle>().GetHashCode(Triangles);
            }

            public bool RecordFieldDeserialize(object record, long index, string propName, ref object source)
            {
                JArray array = ((JToken[])source)[0] as JArray;
                source = new Triangle[] { new Triangle { Indices = array.SelectMany(y => ((int[])y.ToObject<int[]>()).Select(z => z)).ToArray() } };
                return true;
            }

            public bool RecordFieldSerialize(object record, long index, string propName, ref object source)
            {
                throw new NotImplementedException();
            }
        }

        public class Triangle
        {
            public Int32[] Indices { get; set; }

            public override bool Equals(object obj)
            {
                var triangle = obj as Triangle;
                return triangle != null &&
                       new ArrayEqualityComparer<Int32>().Equals(Indices, triangle.Indices);
            }

            public override int GetHashCode()
            {
                return -131595806 + new ArrayEqualityComparer<Int32>().GetHashCode(Indices);
            }
        }
        [Test]
        public static void Sample50()
        {
            List<object> expected = new List<object>
            {
                new Mesh{
                    Triangles = new Triangle[]{new Triangle{ Indices=new int[]{1337,1338,1339}}}
                }
            };
            List<object> actual = new List<object>();

            var json = "{ \"Triangles\": [[1337],[1338],[1339]]}";

            foreach (var rec in ChoJSONReader<Mesh>.LoadText(json)
                //.WithField(o => o.Triangles, customSerializer: o =>
                //{
                //    JArray array = o as JArray;

                //    return new Triangle
                //    {
                //        // converts the json array to an int[]
                //        Indices = array.SelectMany(y => ((int[])y.ToObject<int[]>()).Select(z => z)).ToArray()
                //    };
                //})
                )
            {
                actual.Add(rec);
                //                Console.WriteLine(rec.Dump());
            }

            CollectionAssert.AreEqual(expected, actual);

        }

        [Test]
        public static void RecordSelectTest()
        {
            List<Vehicle> expected = new List<Vehicle>
            {
                new Vehicle{ OwnerType = @"App\Models\User", Owner = new JObject()}
            };
            expected[0].Owner.Add("id", 1);
            expected[0].Owner.Add("username", "testuser");
            expected[0].Owner.Add("email", "test123@mail.com");
            expected[0].Owner.Add("email_verified_at", null);
            expected[0].Owner.Add("created_at", "2019-04-20 10:23:50");
            expected[0].Owner.Add("updated_at", "2019-04-20 10:23:50");
            List<object> actual = new List<object>();

            string json = @"{
    ""id"": 1,
    ""owner_id"": 1,
    ""owner_type"": ""App\\Models\\User"",
    ""created_at"": ""2019-04-21 08:57:53"",
    ""updated_at"": ""2019-04-21 08:57:53"",
    ""owner"": {
        ""id"": 1,
        ""username"": ""testuser"",
        ""email"": ""test123@mail.com"",
        ""email_verified_at"": null,
        ""created_at"": ""2019-04-20 10:23:50"",
        ""updated_at"": ""2019-04-20 10:23:50""
    }
}";

            using (var p = ChoJSONReader<Vehicle>.LoadText(json)
                //.WithField(r => r.Owner, fieldTypeSelector: o => typeof(User))
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        public class Vehicle
        {
            [JsonProperty("owner_type")]
            public string OwnerType { get; set; }

            [JsonProperty("owner")]
            public JObject Owner { get; set; }

            public override bool Equals(object obj)
            {
                var vehicle = obj as Vehicle;
                return vehicle != null &&
                       OwnerType == vehicle.OwnerType &&
                       Enumerable.SequenceEqual(Owner.ToObject<Dictionary<string, object>>(), vehicle.Owner.ToObject<Dictionary<string, object>>());
                //EqualityComparer<JObject>.Default.Equals(Owner, vehicle.Owner);
            }

            public override int GetHashCode()
            {
                var hashCode = -2115913328;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(OwnerType);
                hashCode = hashCode * -1521134295 + EqualityComparer<JObject>.Default.GetHashCode(Owner);
                return hashCode;
            }
        }
        public class User
        {
            public int id { get; set; }
            public int owner_id { get; set; }
            public string created_at { get; set; }
            public string updated_at { get; set; }
            public Owner owner { get; set; }
        }

        public class Owner
        {
            public int id { get; set; }
            public string username { get; set; }
            public string email { get; set; }
            public object email_verified_at { get; set; }
            public string created_at { get; set; }
            public string updated_at { get; set; }
        }

        [Test]
        public static void JSON2CSV()
        {
            string expected = @"name,age,cars_car1,cars_car2,cars_car3,cars_Country_0,cars_Country_1
John,30,Ford,BMW,Fiat,USA,Mexico";
            string actual = null;

            string json = @"
{
  ""name"":""John"",
  ""age"":30,
  ""cars"": {
        ""car1"":""Ford"",
        ""car2"":""BMW"",
        ""car3"":""Fiat"",
        ""Country"": [
            ""USA"",
            ""Mexico""
    ]
  }
 }";

            StringBuilder csv = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
                .WithJSONPath("$", true)
                .Configure(c => c.NestedKeySeparator = '_')
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.UseNestedKeyFormat = true)
                    //.Configure(c => c.NestedColumnSeparator = '/')
                    //.Configure(c => c.ThrowAndStopOnMissingField = false)
                    )
                {
                    w.Write(p);
                }
            }

            actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void LargeJSON()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{ { "name", "foo" }, { "id",1} },
                new ChoDynamicObject{ { "name", "bar" }, { "id",2} },
                new ChoDynamicObject{ { "name", "baz" }, { "id",3} }
            };
            List<object> actual = new List<object>();

            string json = @"[
  {
    ""name"": ""foo"",
    ""id"": 1
  },
  {
    ""name"": ""bar"",
    ""id"": 2
  },
  {
    ""name"": ""baz"",
    ""id"": 3
  }
]";
            using (var p = ChoJSONReader.LoadText(json))
            {
                foreach (var rec in p)
                    actual.Add(rec);
                //                    Console.WriteLine($"Name: {rec.name}, Id: {rec.id}");
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class ArmorPOCO
        {
            public int Armor { get; set; }
            public int Strenght { get; set; }

            public override bool Equals(object obj)
            {
                var pOCO = obj as ArmorPOCO;
                return pOCO != null &&
                       Armor == pOCO.Armor &&
                       Strenght == pOCO.Strenght;
            }

            public override int GetHashCode()
            {
                var hashCode = -987005140;
                hashCode = hashCode * -1521134295 + Armor.GetHashCode();
                hashCode = hashCode * -1521134295 + Strenght.GetHashCode();
                return hashCode;
            }
        }


        [Test]
        public static void Sample28_1()
        {
            List<object> expected = new List<object> {
                new Dictionary<string,ArmorPOCO> {{ "1", new ArmorPOCO { Armor = 1, Strenght = 1 } }, { "0", new ArmorPOCO { Armor = 1, Strenght = 1 } } }
            };
            List<object> actual = new List<object>();

            foreach (var rec in new ChoJSONReader<Dictionary<string, ArmorPOCO>>(FileNameSample28JSON)
                )
            {
                actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        public class Result
        {
            public int Id { get; set; }
            public int SportId { get; set; }

            public override bool Equals(object obj)
            {
                var result = obj as Result;
                return result != null &&
                       Id == result.Id &&
                       SportId == result.SportId;
            }

            public override int GetHashCode()
            {
                var hashCode = 1689353528;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + SportId.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void Sample27_1()
        {
            List<object> expected = new List<object> {
                new Result { Id = 8033583, SportId = 18 },
                new Result { Id = 8033584, SportId = 18 }
            };
            List<object> actual = new List<object>();

            foreach (var rec in new ChoJSONReader<Result>(FileNameSample27JSON)
                .WithJSONPath("results[*]", true)
                )
            {
                actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class MyType
        {
            public string EnrityList { get; set; }
            public string KeyName { get; set; }
            public string Value { get; set; }

            public override bool Equals(object obj)
            {
                var type = obj as MyType;
                return type != null &&
                       EnrityList == type.EnrityList &&
                       KeyName == type.KeyName &&
                       Value == type.Value;
            }

            public override int GetHashCode()
            {
                var hashCode = -1272359899;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EnrityList);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(KeyName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
                return hashCode;
            }
        }
        [Test]
        public static void Sample26_2()
        {
            List<object> expected = new List<object>
            {
                new MyType{ EnrityList = "Attribute", KeyName = "AkeyName", Value = "Avalue"},
                new MyType{ EnrityList = "BusinessKey", KeyName = "AkeyName", Value = "Avalue"}
            };
            List<MyType> actual = new List<MyType>();

            string json = @"[
    {
        ""EnrityList"": ""Attribute"",
        ""KeyName"": ""AkeyName"",
        ""Value"": ""Avalue""
    },
    {
        ""EnrityList"": ""BusinessKey"",
        ""KeyName"": ""AkeyName"",
        ""Value"": ""Avalue""
    }
]";

            foreach (var rec in ChoJSONReader<MyType>.LoadText(json)
                .WithJSONPath("$[*]", true)
                )
            {
                actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample26_1()
        {
            List<object> expected = new List<object>()
            {
                new ChoDynamicObject{{ "regio", "Hoek van Holland"},{ "temperatureMin", 7.3},{ "temperatureMax", 8.8 } }
            };

            List<object> actual = new List<object>();
            foreach (var rec in new ChoJSONReader(FileNameSample26JSON)
                .WithJSONPath("..stationmeasurements")
                .WithField("regio", jsonPath: "regio", fieldType: typeof(string))
                .WithField("temperatureMin", jsonPath: "dayhistory.temperatureMin", fieldType: typeof(double))
                .WithField("temperatureMax", jsonPath: "dayhistory.temperatureMax", fieldType: typeof(double))
                .Where(r => r.regio == "Hoek van Holland")
                )
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }

        public struct LevelBonus
        {
            public int Power { get; set; }
            public int Mana { get; set; }
            public int Strenght { get; set; }
            public int Armor { get; set; }
            public int Health { get; set; }

            public override bool Equals(object obj)
            {
                if (!(obj is LevelBonus))
                {
                    return false;
                }

                var bonus = (LevelBonus)obj;
                return Power == bonus.Power &&
                       Mana == bonus.Mana &&
                       Strenght == bonus.Strenght &&
                       Armor == bonus.Armor &&
                       Health == bonus.Health;
            }

            public override int GetHashCode()
            {
                var hashCode = -351764645;
                hashCode = hashCode * -1521134295 + Power.GetHashCode();
                hashCode = hashCode * -1521134295 + Mana.GetHashCode();
                hashCode = hashCode * -1521134295 + Strenght.GetHashCode();
                hashCode = hashCode * -1521134295 + Armor.GetHashCode();
                hashCode = hashCode * -1521134295 + Health.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void DictTest()
        {
            List<Dictionary<string, LevelBonus>> expected = new List<Dictionary<string, LevelBonus>> { new Dictionary<string, LevelBonus>() };
            expected[0].Add("0", new LevelBonus { Armor = 1, Strenght = 1, Mana = 2, Power = 1, Health = 1 });
            expected[0].Add("1", new LevelBonus { Armor = 1, Strenght = 1 });
            List<object> actual = new List<object>();

            string json = @"{
  ""0"": {
    ""Armor"": 1,
    ""Strenght"": 1,
    ""Mana"": 2,
    ""Power"": 1,
    ""Health"": 1
  },
  ""1"": {
    ""Armor"": 1,
    ""Strenght"": 1
  }
}";

            using (var p = ChoJSONReader<Dictionary<string, LevelBonus>>.LoadText(json)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        // [Test]
        public static void PartialJSONFileTest()
        {
            string json = @"{
    ""property1"": 1,
    ""property2"": 2,
    ""property3"": 3,
    ""property4"": 4,
    ""array"": {
        ""amount"": 9999,
        ""array"": [
}";
            using (var rec = ChoJSONReader.LoadText(json)
                .Configure(c => c.ErrorMode = ChoErrorMode.IgnoreAndContinue)
                )
            {
                Console.WriteLine(rec.Dump());
            }
            Assert.Fail("Not sure, what to test. Throws Newtonsoft-Exception. What should the Parameter ErrorMode do?");
        }

        [Test]
        public static void TestCountToppings()
        {
            List<object> expected = new List<object>
            {
                new { Key = (object)"pepperoni", Count = 2},
                new { Key = (object)"feta cheese", Count = 1},
                new { Key = (object)"sausage", Count = 3},
                new { Key = (object)"beef", Count = 3}
            };
            List<object> actual = new List<object>();

            using (var p = new ChoJSONReader(FileNameSample100JSON)
                .WithJSONPath("$[*].toppings[*]", true)
                )
            {
                foreach (var rec in p.GroupBy(g => ((dynamic)g).Value).Select(g => new { g.Key, Count = g.Count() }))
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        public class ActionMessage
        {
            public string Action { get; set; }
            public DateTime Timestamp { get; set; }
            public string Url { get; set; }
            public string IP { get; set; }

            public override bool Equals(object obj)
            {
                var message = obj as ActionMessage;
                return message != null &&
                       Action == message.Action &&
                       Timestamp == message.Timestamp &&
                       Url == message.Url &&
                       IP == message.IP;
            }

            public override int GetHashCode()
            {
                var hashCode = -659757660;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Action);
                hashCode = hashCode * -1521134295 + Timestamp.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Url);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(IP);
                return hashCode;
            }
        }
        [Test]
        public static void LoadChildren()
        {
            List<object> expected = new List<object>
            {
                new ActionMessage{ Action = "open", Timestamp = new DateTime(2018,09,05,20,46,00), Url = "http://www.google.com", IP = "66.102.6.98"}
            };
            List<object> actual = new List<object>();

            string json = @"{
    ""test1@gmail.com"": [
        {
            ""action"": ""open"",
            ""timestamp"": ""2018-09-05 20:46:00"",
            ""url"": ""http://www.google.com"",
            ""ip"": ""66.102.6.98""
        }
    ]
}";
            using (var p = ChoJSONReader<ActionMessage>.LoadText(json)
                .WithJSONPath("$.^")
                )
            {
                foreach (var rec in p)
                {
                    actual.Add(rec);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        public class Project : IChoRecordFieldSerializable
        {
            public Guid CustID { get; set; }
            public string Title { get; set; }
            public string CalendarID { get; set; }

            public override bool Equals(object obj)
            {
                var project = obj as Project;
                return project != null &&
                       CustID.Equals(project.CustID) &&
                       Title == project.Title &&
                       CalendarID == project.CalendarID;
            }

            public override int GetHashCode()
            {
                var hashCode = -1182825150;
                hashCode = hashCode * -1521134295 + EqualityComparer<Guid>.Default.GetHashCode(CustID);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Title);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CalendarID);
                return hashCode;
            }

            public bool RecordFieldDeserialize(object record, long index, string propName, ref object source)
            {
                return true;
            }

            public bool RecordFieldSerialize(object record, long index, string propName, ref object source)
            {
                return true;
            }
        }

        [Test]
        public static void GuidTest()
        {
            List<object> expected = new List<object>
            {
                new Project { CustID = new Guid("3d49a71b-0913-4eab-8c1d-ec8ddea1fbe3"), CalendarID = "AAMkADE5ZDViNmIyLWU3N2.....pVolcdmAABY3IuJAAA=", Title  = "Timesheet" },
                new Project { CustID = new Guid("3d49a71b-0913-4eab-8c1d-ec8ddea1fbe3"), CalendarID = "AAMkADE5Z.......boA_pVolcdmAABZ8biCAAA=", Title  = "Galaxy" }
            };
            List<object> actual = new List<object>();

            string json = @" [{
        ""CustID"": ""3d49a71b-0913-4eab-8c1d-ec8ddea1fbe3"",
        ""Title"": ""Timesheet"",
        ""CalendarID"": ""AAMkADE5ZDViNmIyLWU3N2.....pVolcdmAABY3IuJAAA="",
        ""PartitionKey"": ""Project"",
        ""RowKey"": ""94a6.....29a4f34"",
        ""Timestamp"": ""2018-09-02T11:24:57.1838388+03:00"",
        ""ETag"": ""W/\""datetime'2018-09-02T08%3A24%3A57.1838388Z'\""""
    }, {
        ""CustID"": ""3d49a71b-0913-4eab-8c1d-ec8ddea1fbe3"",
        ""Title"": ""Galaxy"",
        ""CalendarID"": ""AAMkADE5Z.......boA_pVolcdmAABZ8biCAAA="",
        ""PartitionKey"": ""Project"",
        ""RowKey"": ""f5cc....86a4b"",
        ""Timestamp"": ""2018-09-03T13:02:27.642082+03:00"",
        ""ETag"": ""W/\""datetime'2018-09-03T10%3A02%3A27.642082Z'\""""
    }]";

            using (var p = ChoJSONReader<Project>.LoadText(json)
                .WithField(m => m.CustID)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void ReformatJSON()
        {
            string expected = @"contactName,quantity,description,invoiceNumber
Company,7,Beer No* 45.5 DIN KEG,C6188372
Company,2,Beer Old 49.5 DIN KEG,C6188372";
            string actual = null;

            string json = @"{
    ""contactName"": ""Company"",
    ""lineItems"": [
     {
        ""quantity"": 7.0,
        ""description"": ""Beer No* 45.5 DIN KEG""
     },
     {
        ""quantity"": 2.0,
        ""description"": ""Beer Old 49.5 DIN KEG""
     }
     ],
    ""invoiceNumber"": ""C6188372""
}";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    )
                    w.Write(p
                        .SelectMany(r1 => ((dynamic[])r1.lineItems).Select(r2 => new
                        {
                            r1.contactName,
                            r2.quantity,
                            r2.description,
                            r1.invoiceNumber
                        })));
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Bcp()
        {
            Assert.Ignore("This is not a JSONReader test");

            string connectionString = "*** DB Connection String ***";
            using (var p = new ChoCSVReader<EmployeeRec>("sample17.xml")
                )
            {
                using (SqlBulkCopy bcp = new SqlBulkCopy(connectionString))
                {
                    bcp.DestinationTableName = "dbo.EMPLOYEE";
                    bcp.EnableStreaming = true;
                    bcp.BatchSize = 10000;
                    bcp.BulkCopyTimeout = 0;
                    bcp.NotifyAfter = 10;
                    bcp.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs e)
                    {
                        Console.WriteLine(e.RowsCopied.ToString("#,##0") + " rows copied.");
                    };
                    bcp.WriteToServer(p.AsDataReader());
                }
            }
        }

        [Test]
        public static void XmlTypeTest()
        {
            ChoDynamicObject expected = new ChoDynamicObject {
                { "Source","WEB"},
                { "CodePlan", "5" },
                { "PlanSelection","1" },
                { "PlanAmount","500.01" },
                { "PlanLimitCount", "31" },
                { "PlanLimitAmount","3000.01" },
                { "Visible","false" },
                { "Count",null } };

            string json = @" {
        ""Source"": ""WEB"",
        ""CodePlan"": 5,
        ""PlanSelection"": ""1"",
        ""PlanAmount"": ""500.01"",
        ""PlanLimitCount"": 31,
        ""PlanLimitAmount"": ""3000.01"",
        ""Visible"": false,
        ""Count"": null
     }";

            var r = ChoJSONReader.DeserializeText(json).ToArray();

            string xml = ChoXmlWriter.ToText(r.First(), new ChoXmlRecordConfiguration { EmitDataType = false });
            Console.WriteLine(xml);
            var actual = ChoXmlReader.LoadText(xml).First();

            CollectionAssert.AreEqual(expected, actual);
        }

        public class RootObject2
        {
            public string name { get; set; }
            public Dictionary<string, object> settings { get; set; } = new Dictionary<string, object>();

            public override bool Equals(object obj)
            {
                var @object = obj as RootObject2;
                return @object != null &&
                       name == @object.name &&
                       new DictionaryEqualityComparer<string, object>().Equals(settings, @object.settings);
            }

            public override int GetHashCode()
            {
                var hashCode = 2108768990;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(name);
                hashCode = hashCode * -1521134295 + new DictionaryEqualityComparer<string, object>().GetHashCode(settings);
                return hashCode;
            }
        }
        [Test]
        public static void StringNodeTest()
        {
            List<object> expected = new List<object>
            {
                new RootObject2 { name = "foo", settings = new Dictionary<string, object>{ { "setting1", (bool)true },{ "setting2", (Int64)1 } }}
            };
            List<object> actual = new List<object>();

            string json = @"{
  ""name"": ""foo"",
  ""settings"": {
    ""setting1"": true,
    ""setting2"": 1
  }
}";

            using (var p = ChoJSONReader<RootObject2>.LoadText(json)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
                //                    Console.WriteLine(rec.Dump());
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        public class Error1
        {
            public int Code { get; set; }
            public Error1Message[] Msg { get; set; }

            public override bool Equals(object obj)
            {
                var error = obj as Error1;
                return error != null &&
                       Code == error.Code &&
                       new ArrayEqualityComparer<Error1Message>().Equals(Msg, error.Msg);
            }

            public override int GetHashCode()
            {
                var hashCode = 731883478;
                hashCode = hashCode * -1521134295 + Code.GetHashCode();
                hashCode = hashCode * -1521134295 + new ArrayEqualityComparer<Error1Message>().GetHashCode(Msg);
                return hashCode;
            }
        }
        public class Error1Message
        {
            public string Message { get; set; }
            public int? Code { get; set; }

            public override bool Equals(object obj)
            {
                var message = obj as Error1Message;
                return message != null &&
                       Message == message.Message &&
                       EqualityComparer<int?>.Default.Equals(Code, message.Code);
            }

            public override int GetHashCode()
            {
                var hashCode = -1798610120;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Message);
                hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Code);
                return hashCode;
            }
        }
        [Test]
        public static void VarySchemas()
        {
            List<object> expected = new List<object>
            {
                new Error1 { Code = 10, Msg = new Error1Message[] {
                    new Error1Message { Message = "A single string" },
                    new Error1Message { Message = "An object with a message" },
                    new Error1Message { Message = "An object with a message and a code", Code = 5 },
                    new Error1Message { Code = 5 }
                } }
            };
            List<object> actual = new List<object>();

            string json = @"{
    ""Code"": 10,
    ""Msg"": [
        ""A single string"",
        { ""Message"": ""An object with a message"" },
        { ""Message"": ""An object with a message and a code"", ""Code"": 5 },
        { ""Code"": 5 }
    ]
}";

            using (var p = ChoJSONReader<Error1>.LoadText(json)
                .WithField(m => m.Msg, customSerializer: (o) =>
                {
                    List<Error1Message> msg = new List<Error1Message>();
                    foreach (var e in (JArray)((JToken[])o)[0])
                    {
                        if (e is JValue)
                            msg.Add(new Error1Message { Message = ((JValue)e).Value as string });
                        else if (e is JObject)
                            msg.Add(((JObject)e).ToObject<Error1Message>());
                    }
                    return msg.ToArray();
                }
                )
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSONToDataset()
        {
            DataTable expected1 = new DataTable();
            expected1.Columns.Add("ViewId");
            expected1.Columns.Add("IsAddAllowed", typeof(bool));
            expected1.Columns.Add("IsEditAllowed", typeof(bool));
            expected1.Columns.Add("IsDeleteAllowed", typeof(bool));
            expected1.Rows.Add("B27A68AD-7C21-4DDB-8A1D-8932459CF53B", true, true, true);

            DataTable expected2 = new DataTable();
            expected2.Columns.Add("ViewId");
            expected2.Columns.Add("IsAddAllowed", typeof(bool));
            expected2.Columns.Add("IsEditAllowed", typeof(bool));
            expected2.Columns.Add("IsDeleteAllowed", typeof(bool));
            expected2.Rows.Add("Z27A68AD-7C21-4DDB-8A1D-8932459CF53B", true, true, true);

            string json = @"{
""Data"": [
    {
       ""Code"": ""DEMO"",
       ""Name"": ""DEMO"",
       ""UserId"": ""B27A68AD-7C21-4DDB-8A1D-8932459CF53B"",
       ""RoleDetails"": [{
            ""ViewId"": ""B27A68AD-7C21-4DDB-8A1D-8932459CF53B"",
            ""IsAddAllowed"": true,
            ""IsEditAllowed"": true,
            ""IsDeleteAllowed"": true
        }],
        ""RoleDetails1"":[ {
            ""ViewId"": ""Z27A68AD-7C21-4DDB-8A1D-8932459CF53B"",
            ""IsAddAllowed"": true,
            ""IsEditAllowed"": true,
            ""IsDeleteAllowed"": true
          }]

    }
]
}";
            var dt1 = ChoJSONReader.DeserializeText(json, "$.Data[0].RoleDetails").AsDataTable();
            var dt2 = ChoJSONReader.DeserializeText(json, "$.Data[0].RoleDetails1").AsDataTable();
            //DataSet ds = new DataSet();
            //ds.Tables.Add(dt1);
            //ds.Tables.Add(dt2);
            DataTableAssert.AreEqual(expected1, dt1);
            DataTableAssert.AreEqual(expected2, dt2);
        }

        [Test]
        public static void Sample24Test()
        {
            //var rec = ChoJSONReader.Deserialize("sample24.json");
            //Console.WriteLine(rec.Dump());

            //DataTable dt = new ChoJSONReader("sample24.json").Select(i => i.Flatten(new Func<string, string>(cn =>
            //{
            //    if (cn == "Scenario")
            //        return "1Scenario1";
            //    return cn;
            //}), null)).AsDataTable();

            DataTable expected = new DataTable();
            expected.Columns.Add("Solution_Value_PreserveMvDurConv", typeof(bool));
            expected.Columns.Add("Solution_Value_SaveCpInstrumentResults", typeof(bool));
            expected.Columns.Add("Solution_Value_SimID");
            expected.Columns.Add("Solution_Value_SimPdCount", typeof(long));
            expected.Columns.Add("Solution_Value_SimForecast", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpSimComponents", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_PackedHexDateValue", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_Year", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_Month", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_MonthNum", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_Day", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_RawDay", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_DaysLeftInMonth", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_DaysInThisYear", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_DayOfYear", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_DateValue", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_PackedIntDateValue", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_IsEndOfMonth", typeof(bool));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_IsLastDayOfFebruary", typeof(bool));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_DaysInThisMonth", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_IsEmpty", typeof(bool));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentStartDate_IsValid", typeof(bool));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpRecentMonthCount", typeof(long));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpReportMissingXferRates", typeof(bool));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpCalculateMissingXferRates", typeof(bool));
            expected.Columns.Add("Solution_Value_FtpPreferences_FtpInProcessAllowanceDays", typeof(long));
            expected.Columns.Add("Solution_Value_WgtAvgLifeCalcSetting", typeof(long));
            expected.Columns.Add("Solution_Value_FairValueFrequency", typeof(long));
            expected.Columns.Add("Solution_Value_MarketValueFrequency", typeof(long));

            expected.Columns.Add("Solution_Value_DurConvexFrequency", typeof(long));
            expected.Columns.Add("Solution_Value_ActivateFTP", typeof(bool));
            expected.Columns.Add("Solution_Value_ActivateWAL", typeof(bool));
            expected.Columns.Add("Solution_Value_ActivateYld", typeof(bool));
            expected.Columns.Add("Solution_Value_ShockBPS", typeof(long));
            expected.Columns.Add("Solution_Value_ShockDecimal", typeof(double));
            expected.Columns.Add("Solution_Value_SimSecurities", typeof(long));
            expected.Columns.Add("Solution_Value_SimOverrideOAS", typeof(long));
            expected.Columns.Add("Solution_Value_SimSkipBalance", typeof(long));

            expected.Columns.Add("Solution_Value_SimResults", typeof(long));
            expected.Columns.Add("Solution_Value_SimSkipFairValue", typeof(long));
            expected.Columns.Add("Solution_Value_SimBuildFwdCP", typeof(long));
            expected.Columns.Add("Solution_Value_SimRunGapSwitches", typeof(long));
            expected.Columns.Add("Solution_Value_IncludeGapInSim", typeof(long));
            expected.Columns.Add("Solution_Value_CalculateFairValue", typeof(bool));

            expected.Columns.Add("Solution_Name");
            expected.Columns.Add("Solution_Context");

            expected.Columns.Add("Scenario");

            expected.Rows.Add(false, false, "Hira", 13, 0, 0, 0, 0, 0, -24000, 0, 0, 31, 366, -30, -90, 0, false, false, 30, true, false, 0, true, false, 0,
                0, 0, 0, 0, true, false, false, 0, 0.0, 1, 0, 1, 1, 1, 0, 0, 0, false, "Solution Set 1", "SingleSim",
                "Shock: Parallel Up200Bps, 1115, ALM_Base:RS_U200_FLAT, ,");

            DataTable actual = new ChoJSONReader(FileNameSample24JSON,
                new ChoJSONRecordConfiguration()
                .Configure(c => c.MaxScanRows = 1)).AsDataTable();

            DataTableAssert.AreEqual(expected, actual);
        }
        public class StockQuotes
        {
            [ChoJSONRecordField(JSONPath = "$..timestamp[*]")]
            public int[] Timestamps { get; set; }
            [ChoJSONRecordField(JSONPath = "$..indicators.quote[0].open[*]")]
            public double[] Opens { get; set; }
        }

        public class StockQuote1 : IChoConvertible
        {
            [ChoFieldMap("Timestamps")]
            public DateTime Timestamp { get; set; }
            [ChoFieldMap("Opens")]
            public double Open { get; set; }

            public bool Convert(string propName, object propValue, CultureInfo culture, out object convPropValue)
            {
                convPropValue = null;
                if (propName == nameof(Timestamp))
                {
                    convPropValue = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)propValue).ToUniversalTime();
                    return true;
                }

                return false;
            }

            public bool ConvertBack(string propName, object propValue, Type targetType, CultureInfo culture, out object convPropValue)
            {
                throw new NotImplementedException();
            }

            public override bool Equals(object obj)
            {
                var quote = obj as StockQuote1;
                return quote != null &&
                       Timestamp == quote.Timestamp &&
                       Open == quote.Open;
            }

            public override int GetHashCode()
            {
                var hashCode = 1629026340;
                hashCode = hashCode * -1521134295 + Timestamp.GetHashCode();
                hashCode = hashCode * -1521134295 + Open.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void MSFTQuooteToCSV()
        {
            DateTime dt19700101 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            List<object> expected = new List<object>
            {
/*                new StockQuotes {
                    Timestamps = new int[]{ 743866200, 743952600, 744039000, 744298200, 744384600, 744471000, 744557400, 744643800, 744903000, 744989400, 745075800, 745162200, 745248600, 745507800, 745594200, 745680600, 745767000, 745853400, 746112600, 746199000, 746285400, 746371800, 746458200, 746717400, 746803800, 746890200, 746976600, 747063000, 747408600, 747495000, 747581400, 747667800, 747927000, 748013400, 748099800, 748186200, 748272600, 748531800, 748618200, 748704600, 748791000, 748877400, 749136600, 749223000, 749309400, 749395800, 749482200, 749741400, 749827800, 749914200, 750000600, 750087000, 750346200, 750432600, 750519000, 750605400, 750691800, 750951000, 751037400, 751123800, 751210200, 751296600, 751555800, 751642200, 751728600, 751815000, 751901400, 752164200, 752250600, 752337000, 752423400, 752509800, 752769000, 752855400, 752941800, 753028200, 753114600, 753373800, 753460200, 753546600, 753633000, 753719400, 753978600, 754065000, 754151400, 754324200, 754583400, 754669800, 754756200, 754842600, 754929000, 755188200, 755274600, 755361000, 755447400, 755533800, 755793000, 755879400, 755965800, 756052200, 756138600, 756397800, 756484200, 756570600, 756657000, 757002600, 757089000, 757175400, 757261800, 757348200, 757607400, 757693800, 757780200, 757866600, 757953000, 758212200, 758298600, 758385000, 758471400, 758557800, 758817000, 758903400, 758989800, 759076200, 759162600, 759421800, 759508200, 759594600, 759681000, 759767400, 760026600, 760113000, 760199400, 760285800, 760372200, 760631400, 760717800, 760804200, 760890600, 760977000, 761236200, 761322600, 761409000, 761495400, 761581800, 761927400, 762013800, 762100200, 762186600, 762445800, 762532200, 762618600, 762705000, 762791400, 763050600, 763137000, 763223400, 763309800, 763396200, 763655400, 763741800, 763828200, 763914600, 764001000, 764260200, 764346600, 764433000, 764519400, 764605800, 764865000, 764951400, 765037800, 765124200, 765466200, 765552600, 765639000, 765725400, 765811800, 766071000, 766157400, 766243800, 766330200, 766416600, 766675800, 766762200, 766848600, 766935000, 767021400, 767280600, 767367000, 767539800, 767626200, 767885400, 767971800, 768058200, 768144600, 768231000, 768490200, 768576600, 768663000, 768749400, 768835800, 769095000, 769181400, 769267800, 769354200, 769440600, 769699800, 769786200, 769872600, 769959000, 770045400, 770391000, 770477400, 770563800, 770650200, 770909400, 770995800, 771082200, 771168600, 771255000, 771514200, 771600600, 771687000, 771773400, 771859800, 772119000, 772205400, 772291800, 772378200, 772464600, 772723800, 772810200, 772896600, 772983000, 773069400, 773415000, 773501400, 773587800, 773674200, 773933400, 774019800, 774106200, 774192600, 774279000, 774538200, 774624600, 774711000, 774797400, 774883800, 775143000, 775229400, 775315800, 775402200, 775488600, 775747800, 775834200, 775920600, 776007000, 776093400, 776352600, 776439000, 776525400, 776611800, 776698200, 776957400, 777043800, 777130200, 777216600, 777303000, 777562200, 777648600, 777735000, 777821400, 777907800, 778167000, 778253400, 778339800, 778426200, 778512600, 778858200, 778944600, 779031000, 779117400, 779376600, 779463000, 779549400, 779635800, 779722200, 779981400, 780067800, 780154200, 780240600, 780327000, 780586200, 780672600, 780759000, 780845400, 780931800, 781191000, 781277400, 781363800, 781450200, 781536600, 781795800, 781882200, 781968600, 782055000, 782141400, 782400600, 782487000, 782573400, 782659800, 782746200, 783005400, 783091800, 783178200, 783264600, 783351000, 783613800, 783700200, 783786600, 783873000, 783959400, 784218600, 784305000, 784391400, 784477800, 784564200, 784823400, 784909800, 784996200, 785082600, 785169000, 785428200, 785514600, 785601000, 785773800, 786033000, 786119400, 786205800, 786292200, 786378600, 786637800, 786724200, 786810600, 786897000, 786983400, 787242600, 787329000, 787415400, 787501800, 787588200, 787847400, 787933800, 788020200, 788106600, 788193000, 788538600, 788625000, 788711400, 788797800, 789143400, 789229800, 789316200, 789402600, 789661800, 789748200, 789834600, 789921000, 790007400, 790266600, 790353000, 790439400, 790525800, 790612200, 790871400, 790957800, 791044200, 791130600, 791217000, 791476200, 791562600, 791649000, 791735400, 791821800, 792081000, 792167400, 792253800, 792340200, 792426600, 792685800, 792772200, 792858600, 792945000, 793031400, 793377000, 793463400, 793549800, 793636200, 793895400, 793981800, 794068200, 794154600, 794241000, 794500200, 794586600, 794673000, 794759400, 794845800, 795105000, 795191400, 795277800, 795364200, 795450600, 795709800, 795796200, 795882600, 795969000, 796055400, 796314600, 796401000, 796487400, 796573800, 796660200, 796915800, 797002200, 797088600, 797175000, 797261400, 797520600, 797607000, 797693400, 797779800, 798125400, 798211800, 798298200, 798384600, 798471000, 798730200, 798816600, 798903000, 798989400, 799075800, 799335000, 799421400, 799507800, 799594200, 799680600, 799939800, 800026200, 800112600, 800199000, 800285400, 800544600, 800631000, 800717400, 800803800, 800890200, 801149400, 801235800, 801322200, 801408600, 801495000, 801840600, 801927000, 802013400, 802099800, 802359000, 802445400, 802531800, 802618200, 802704600, 802963800, 803050200, 803136600, 803223000, 803309400, 803568600, 803655000, 803741400, 803827800, 803914200, 804173400, 804259800, 804346200, 804432600, 804519000, 804778200, 804951000, 805037400, 805123800, 805383000, 805469400, 805555800, 805642200, 805728600, 805987800, 806074200, 806160600, 806247000, 806333400, 806592600, 806679000, 806765400, 806851800, 806938200, 807197400, 807283800, 807370200, 807456600, 807543000, 807802200, 807888600, 807975000, 808061400, 808147800, 808407000, 808493400, 808579800, 808666200, 808752600, 809011800, 809098200, 809184600, 809271000, 809357400, 809616600, 809703000, 809789400, 809875800, 809962200, 810307800, 810394200, 810480600, 810567000, 810826200, 810912600, 810999000, 811085400, 811171800, 811431000, 811517400, 811603800, 811690200, 811776600, 812035800, 812122200, 812208600, 812295000, 812381400, 812640600, 812727000, 812813400, 812899800, 812986200, 813245400, 813331800, 813418200, 813504600, 813591000, 813850200, 813936600, 814023000, 814109400, 814195800, 814455000, 814541400, 814627800, 814714200, 814800600, 815063400, 815149800, 815236200, 815322600, 815409000, 815668200, 815754600, 815841000, 815927400, 816013800, 816273000, 816359400, 816445800, 816532200, 816618600, 816877800, 816964200, 817050600, 817223400, 817482600, 817569000, 817655400, 817741800, 817828200, 818087400, 818173800, 818260200, 818346600, 818433000, 818692200, 818778600, 818865000, 818951400, 819037800, 819297000, 819383400, 819469800, 819556200, 819642600, 819988200, 820074600, 820161000, 820247400, 820593000, 820679400, 820765800, 820852200, 821111400, 821197800, 821284200, 821370600, 821457000, 821716200, 821802600, 821889000, 821975400, 822061800, 822321000, 822407400, 822493800, 822580200, 822666600, 822925800, 823012200, 823098600, 823185000, 823271400, 823530600, 823617000, 823703400, 823789800, 823876200, 824135400, 824221800, 824308200, 824394600, 824481000, 824826600, 824913000, 824999400, 825085800, 825345000, 825431400, 825517800, 825604200, 825690600, 825949800, 826036200, 826122600, 826209000, 826295400, 826554600, 826641000, 826727400, 826813800, 826900200, 827159400, 827245800, 827332200, 827418600, 827505000, 827764200, 827850600, 827937000, 828023400, 828109800, 828369000, 828455400, 828541800, 828628200, 828970200, 829056600, 829143000, 829229400, 829315800, 829575000, 829661400, 829747800, 829834200, 829920600, 830179800, 830266200, 830352600, 830439000, 830525400, 830784600, 830871000, 830957400, 831043800, 831130200, 831389400, 831475800, 831562200, 831648600, 831735000, 831994200, 832080600, 832167000, 832253400, 832339800, 832599000, 832685400, 832771800, 832858200, 832944600, 833290200, 833376600, 833463000, 833549400, 833808600, 833895000, 833981400, 834067800, 834154200, 834413400, 834499800, 834586200, 834672600, 834759000, 835018200, 835104600, 835191000, 835277400, 835363800, 835623000, 835709400, 835795800, 835882200, 835968600, 836227800, 836314200, 836400600, 836573400, 836832600, 836919000, 837005400, 837091800, 837178200, 837437400, 837523800, 837610200, 837696600, 837783000, 838042200, 838128600, 838215000, 838301400, 838387800, 838647000, 838733400, 838819800, 838906200, 838992600, 839251800, 839338200, 839424600, 839511000, 839597400, 839856600, 839943000, 840029400, 840115800, 840202200, 840461400, 840547800, 840634200, 840720600, 840807000, 841066200, 841152600, 841239000, 841325400, 841411800, 841757400, 841843800, 841930200, 842016600, 842275800, 842362200, 842448600, 842535000, 842621400, 842880600, 842967000, 843053400, 843139800, 843226200, 843485400, 843571800, 843658200, 843744600, 843831000, 844090200, 844176600, 844263000, 844349400, 844435800, 844695000, 844781400, 844867800, 844954200, 845040600, 845299800, 845386200, 845472600, 845559000, 845645400, 845904600, 845991000, 846077400, 846163800, 846250200, 846513000, 846599400, 846685800, 846772200, 846858600, 847117800, 847204200, 847290600, 847377000, 847463400, 847722600, 847809000, 847895400, 847981800, 848068200, 848327400, 848413800, 848500200, 848586600, 848673000, 848932200, 849018600, 849105000, 849277800, 849537000, 849623400, 849709800, 849796200, 849882600, 850141800, 850228200, 850314600, 850401000, 850487400, 850746600, 850833000, 850919400, 851005800, 851092200, 851351400, 851437800, 851610600, 851697000, 851956200, 852042600, 852215400, 852301800, 852561000, 852647400, 852733800, 852820200, 852906600, 853165800, 853252200, 853338600, 853425000, 853511400, 853770600, 853857000, 853943400, 854029800, 854116200, 854375400, 854461800, 854548200, 854634600, 854721000, 854980200, 855066600, 855153000, 855239400, 855325800, 855585000, 855671400, 855757800, 855844200, 855930600, 856276200, 856362600, 856449000, 856535400, 856794600, 856881000, 856967400, 857053800, 857140200, 857399400, 857485800, 857572200, 857658600, 857745000, 858004200, 858090600, 858177000, 858263400, 858349800, 858609000, 858695400, 858781800, 858868200, 858954600, 859213800, 859300200, 859386600, 859473000, 859818600, 859905000, 859991400, 860077800, 860164200, 860419800, 860506200, 860592600, 860679000, 860765400, 861024600, 861111000, 861197400, 861283800, 861370200, 861629400, 861715800, 861802200, 861888600, 861975000, 862234200, 862320600, 862407000, 862493400, 862579800, 862839000, 862925400, 863011800, 863098200, 863184600, 863443800, 863530200, 863616600, 863703000, 863789400, 864048600, 864135000, 864221400, 864307800, 864394200, 864739800, 864826200, 864912600, 864999000, 865258200, 865344600, 865431000, 865517400, 865603800, 865863000, 865949400, 866035800, 866122200, 866208600, 866467800, 866554200, 866640600, 866727000, 866813400, 867072600, 867159000, 867245400, 867331800, 867418200, 867677400, 867763800, 867850200, 867936600, 868282200, 868368600, 868455000, 868541400, 868627800, 868887000, 868973400, 869059800, 869146200, 869232600, 869491800, 869578200, 869664600, 869751000, 869837400, 870096600, 870183000, 870269400, 870355800, 870442200, 870701400, 870787800, 870874200, 870960600, 871047000, 871306200, 871392600, 871479000, 871565400, 871651800, 871911000, 871997400, 872083800, 872170200, 872256600, 872515800, 872602200, 872688600, 872775000, 872861400, 873207000, 873293400, 873379800, 873466200, 873725400, 873811800, 873898200, 873984600, 874071000, 874330200, 874416600, 874503000, 874589400, 874675800, 874935000, 875021400, 875107800, 875194200, 875280600, 875539800, 875626200, 875712600, 875799000, 875885400, 876144600, 876231000, 876317400, 876403800, 876490200, 876749400, 876835800, 876922200, 877008600, 877095000, 877354200, 877440600, 877527000, 877613400, 877699800, 877962600, 878049000, 878135400, 878221800, 878308200, 878567400, 878653800, 878740200, 878826600, 878913000, 879172200, 879258600, 879345000, 879431400, 879517800, 879777000, 879863400, 879949800, 880036200, 880122600, 880381800, 880468200, 880554600, 880727400, 880986600, 881073000, 881159400, 881245800, 881332200, 881591400, 881677800, 881764200, 881850600, 881937000, 882196200, 882282600, 882369000, 882455400, 882541800, 882801000, 882887400, 882973800, 883146600, 883405800, 883492200, 883578600, 883751400, 884010600, 884097000, 884183400, 884269800, 884356200, 884615400, 884701800, 884788200, 884874600, 884961000, 885306600, 885393000, 885479400, 885565800, 885825000, 885911400, 885997800, 886084200, 886170600, 886429800, 886516200, 886602600, 886689000, 886775400, 887034600, 887121000, 887207400, 887293800, 887380200, 887725800, 887812200, 887898600, 887985000, 888244200, 888330600, 888417000, 888503400, 888589800, 888849000, 888935400, 889021800, 889108200, 889194600, 889453800, 889540200, 889626600, 889713000, 889799400, 890058600, 890145000, 890231400, 890317800, 890404200, 890663400, 890749800, 890836200, 890922600, 891009000, 891268200, 891354600, 891441000, 891527400, 891613800, 891869400, 891955800, 892042200, 892128600, 892474200, 892560600, 892647000, 892733400, 892819800, 893079000, 893165400, 893251800, 893338200, 893424600, 893683800, 893770200, 893856600, 893943000, 894029400, 894288600, 894375000, 894461400, 894547800, 894634200, 894893400, 894979800, 895066200, 895152600, 895239000, 895498200, 895584600, 895671000, 895757400, 895843800, 896189400, 896275800, 896362200, 896448600, 896707800, 896794200, 896880600, 896967000, 897053400, 897312600, 897399000, 897485400, 897571800, 897658200, 897917400, 898003800, 898090200, 898176600, 898263000, 898522200, 898608600, 898695000, 898781400, 898867800, 899127000, 899213400, 899299800, 899386200, 899731800, 899818200, 899904600, 899991000, 900077400, 900336600, 900423000, 900509400, 900595800, 900682200, 900941400, 901027800, 901114200, 901200600, 901287000, 901546200, 901632600, 901719000, 901805400, 901891800, 902151000, 902237400, 902323800, 902410200, 902496600, 902755800, 902842200, 902928600, 903015000, 903101400, 903360600, 903447000, 903533400, 903619800, 903706200, 903965400, 904051800, 904138200, 904224600, 904311000, 904570200, 904656600, 904743000, 904829400, 904915800, 905261400, 905347800, 905434200, 905520600, 905779800, 905866200, 905952600, 906039000, 906125400, 906384600, 906471000, 906557400, 906643800, 906730200, 906989400, 907075800, 907162200, 907248600, 907335000, 907594200, 907680600, 907767000, 907853400, 907939800, 908199000, 908285400, 908371800, 908458200, 908544600, 908803800, 908890200, 908976600, 909063000, 909149400, 909412200, 909498600, 909585000, 909671400, 909757800, 910017000, 910103400, 910189800, 910276200, 910362600, 910621800, 910708200, 910794600, 910881000, 910967400, 911226600, 911313000, 911399400, 911485800, 911572200, 911831400, 911917800, 912004200, 912177000, 912436200, 912522600, 912609000, 912695400, 912781800, 913041000, 913127400, 913213800, 913300200, 913386600, 913645800, 913732200, 913818600, 913905000, 913991400, 914250600, 914337000, 914423400, 914509800, 914855400, 914941800, 915028200, 915114600, 915460200, 915546600, 915633000, 915719400, 915805800, 916065000, 916151400, 916237800, 916324200, 916410600, 916756200, 916842600, 916929000, 917015400, 917274600, 917361000, 917447400, 917533800, 917620200, 917879400, 917965800, 918052200, 918138600, 918225000, 918484200, 918570600, 918657000, 918743400, 918829800, 919175400, 919261800, 919348200, 919434600, 919693800, 919780200, 919866600, 919953000, 920039400, 920298600, 920385000, 920471400, 920557800, 920644200, 920903400, 920989800, 921076200, 921162600, 921249000, 921508200, 921594600, 921681000, 921767400, 921853800, 922113000, 922199400, 922285800, 922372200, 922458600, 922717800, 922804200, 922890600, 922977000, 923319000, 923405400, 923491800, 923578200, 923664600, 923923800, 924010200, 924096600, 924183000, 924269400, 924528600, 924615000, 924701400, 924787800, 924874200, 925133400, 925219800, 925306200, 925392600, 925479000, 925738200, 925824600, 925911000, 925997400, 926083800, 926343000, 926429400, 926515800, 926602200, 926688600, 926947800, 927034200, 927120600, 927207000, 927293400, 927552600, 927639000, 927725400, 927811800, 927898200, 928243800, 928330200, 928416600, 928503000, 928762200, 928848600, 928935000, 929021400, 929107800, 929367000, 929453400, 929539800, 929626200, 929712600, 929971800, 930058200, 930144600, 930231000, 930317400, 930576600, 930663000, 930749400, 930835800, 930922200, 931267800, 931354200, 931440600, 931527000, 931786200, 931872600, 931959000, 932045400, 932131800, 932391000, 932477400, 932563800, 932650200, 932736600, 932995800, 933082200, 933168600, 933255000, 933341400, 933600600, 933687000, 933773400, 933859800, 933946200, 934205400, 934291800, 934378200, 934464600, 934551000, 934810200, 934896600, 934983000, 935069400, 935155800, 935415000, 935501400, 935587800, 935674200, 935760600, 936019800, 936106200, 936192600, 936279000, 936365400, 936711000, 936797400, 936883800, 936970200, 937229400, 937315800, 937402200, 937488600, 937575000, 937834200, 937920600, 938007000, 938093400, 938179800, 938439000, 938525400, 938611800, 938698200, 938784600, 939043800, 939130200, 939216600, 939303000, 939389400, 939648600, 939735000, 939821400, 939907800, 939994200, 940253400, 940339800, 940426200, 940512600, 940599000, 940858200, 940944600, 941031000, 941117400, 941203800, 941466600, 941553000, 941639400, 941725800, 941812200, 942071400, 942157800, 942244200, 942330600, 942417000, 942676200, 942762600, 942849000, 942935400, 943021800, 943281000, 943367400, 943453800, 943626600, 943885800, 943972200, 944058600, 944145000, 944231400, 944490600, 944577000, 944663400, 944749800, 944836200, 945095400, 945181800, 945268200, 945354600, 945441000, 945700200, 945786600, 945873000, 945959400, 946305000, 946391400, 946477800, 946564200, 946650600, 946909800, 946996200, 947082600, 947169000, 947255400, 947514600, 947601000, 947687400, 947773800, 947860200, 948205800, 948292200, 948378600, 948465000, 948724200, 948810600, 948897000, 948983400, 949069800, 949329000, 949415400, 949501800, 949588200, 949674600, 949933800, 950020200, 950106600, 950193000, 950279400, 950538600, 950625000, 950711400, 950797800, 950884200, 951229800, 951316200, 951402600, 951489000, 951748200, 951834600, 951921000, 952007400, 952093800, 952353000, 952439400, 952525800, 952612200, 952698600, 952957800, 953044200, 953130600, 953217000, 953303400, 953562600, 953649000, 953735400, 953821800, 953908200, 954167400, 954253800, 954340200, 954426600, 954513000, 954768600, 954855000, 954941400, 955027800, 955114200, 955373400, 955459800, 955546200, 955632600, 955719000, 955978200, 956064600, 956151000, 956237400, 956583000, 956669400, 956755800, 956842200, 956928600, 957187800, 957274200, 957360600, 957447000, 957533400, 957792600, 957879000, 957965400, 958051800, 958138200, 958397400, 958483800, 958570200, 958656600, 958743000, 959002200, 959088600, 959175000, 959261400, 959347800, 959693400, 959779800, 959866200, 959952600, 960211800, 960298200, 960384600, 960471000, 960557400, 960816600, 960903000, 960989400, 961075800, 961162200, 961421400, 961507800, 961594200, 961680600, 961767000, 962026200, 962112600, 962199000, 962285400, 962371800, 962631000, 962803800, 962890200, 962976600, 963235800, 963322200, 963408600, 963495000, 963581400, 963840600, 963927000, 964013400, 964099800, 964186200, 964445400, 964531800, 964618200, 964704600, 964791000, 965050200, 965136600, 965223000, 965309400, 965395800, 965655000, 965741400, 965827800, 965914200, 966000600, 966259800, 966346200, 966432600, 966519000, 966605400, 966864600, 966951000, 967037400, 967123800, 967210200, 967469400, 967555800, 967642200, 967728600, 967815000, 968160600, 968247000, 968333400, 968419800, 968679000, 968765400, 968851800, 968938200, 969024600, 969283800, 969370200, 969456600, 969543000, 969629400, 969888600, 969975000, 970061400, 970147800, 970234200, 970493400, 970579800, 970666200, 970752600, 970839000, 971098200, 971184600, 971271000, 971357400, 971443800, 971703000, 971789400, 971875800, 971962200, 972048600, 972307800, 972394200, 972480600, 972567000, 972653400, 972916200, 973002600, 973089000, 973175400, 973261800, 973521000, 973607400, 973693800, 973780200, 973866600, 974125800, 974212200, 974298600, 974385000, 974471400, 974730600, 974817000, 974903400, 975076200, 975335400, 975421800, 975508200, 975594600, 975681000, 975940200, 976026600, 976113000, 976199400, 976285800, 976545000, 976631400, 976717800, 976804200, 976890600, 977149800, 977236200, 977322600, 977409000, 977495400, 977841000, 977927400, 978013800, 978100200, 978445800, 978532200, 978618600, 978705000, 978964200, 979050600, 979137000, 979223400, 979309800, 979655400, 979741800, 979828200, 979914600, 980173800, 980260200, 980346600, 980433000, 980519400, 980778600, 980865000, 980951400, 981037800, 981124200, 981383400, 981469800, 981556200, 981642600, 981729000, 981988200, 982074600, 982161000, 982247400, 982333800, 982679400, 982765800, 982852200, 982938600, 983197800, 983284200, 983370600, 983457000, 983543400, 983802600, 983889000, 983975400, 984061800, 984148200, 984407400, 984493800, 984580200, 984666600, 984753000, 985012200, 985098600, 985185000, 985271400, 985357800, 985617000, 985703400, 985789800, 985876200, 985962600, 986218200, 986304600, 986391000, 986477400, 986563800, 986823000, 986909400, 986995800, 987082200, 987427800, 987514200, 987600600, 987687000, 987773400, 988032600, 988119000, 988205400, 988291800, 988378200, 988637400, 988723800, 988810200, 988896600, 988983000, 989242200, 989328600, 989415000, 989501400, 989587800, 989847000, 989933400, 990019800, 990106200, 990192600, 990451800, 990538200, 990624600, 990711000, 990797400, 991143000, 991229400, 991315800, 991402200, 991661400, 991747800, 991834200, 991920600, 992007000, 992266200, 992352600, 992439000, 992525400, 992611800, 992871000, 992957400, 993043800, 993130200, 993216600, 993475800, 993562200, 993648600, 993735000, 993821400, 994080600, 994167000, 994339800, 994426200, 994685400, 994771800, 994858200, 994944600, 995031000, 995290200, 995376600, 995463000, 995549400, 995635800, 995895000, 995981400, 996067800, 996154200, 996240600, 996499800, 996586200, 996672600, 996759000, 996845400, 997104600, 997191000, 997277400, 997363800, 997450200, 997709400, 997795800, 997882200, 997968600, 998055000, 998314200, 998400600, 998487000, 998573400, 998659800, 998919000, 999005400, 999091800, 999178200, 999264600, 999610200, 999696600, 999783000, 999869400, 1000128600, 1000733400, 1000819800, 1000906200, 1000992600, 1001079000, 1001338200, 1001424600, 1001511000, 1001597400, 1001683800, 1001943000, 1002029400, 1002115800, 1002202200, 1002288600, 1002547800, 1002634200, 1002720600, 1002807000, 1002893400, 1003152600, 1003239000, 1003325400, 1003411800, 1003498200, 1003757400, 1003843800, 1003930200, 1004016600, 1004103000, 1004365800, 1004452200, 1004538600, 1004625000, 1004711400, 1004970600, 1005057000, 1005143400, 1005229800, 1005316200, 1005575400, 1005661800, 1005748200, 1005834600, 1005921000, 1006180200, 1006266600, 1006353000, 1006525800, 1006785000, 1006871400, 1006957800, 1007044200, 1007130600, 1007389800, 1007476200, 1007562600, 1007649000, 1007735400, 1007994600, 1008081000, 1008167400, 1008253800, 1008340200, 1008599400, 1008685800, 1008772200, 1008858600, 1008945000, 1009204200, 1009377000, 1009463400, 1009549800, 1009809000, 1009981800, 1010068200, 1010154600, 1010413800, 1010500200, 1010586600, 1010673000, 1010759400, 1011018600, 1011105000, 1011191400, 1011277800, 1011364200, 1011709800, 1011796200, 1011882600, 1011969000, 1012228200, 1012314600, 1012401000, 1012487400, 1012573800, 1012833000, 1012919400, 1013005800, 1013092200, 1013178600, 1013437800, 1013524200, 1013610600, 1013697000, 1013783400, 1014129000, 1014215400, 1014301800, 1014388200, 1014647400, 1014733800, 1014820200, 1014906600, 1014993000, 1015252200, 1015338600, 1015425000, 1015511400, 1015597800, 1015857000, 1015943400, 1016029800, 1016116200, 1016202600, 1016461800, 1016548200, 1016634600, 1016721000, 1016807400, 1017066600, 1017153000, 1017239400, 1017325800, 1017671400, 1017757800, 1017844200, 1017930600, 1018017000, 1018272600, 1018359000, 1018445400, 1018531800, 1018618200, 1018877400, 1018963800, 1019050200, 1019136600, 1019223000, 1019482200, 1019568600, 1019655000, 1019741400, 1019827800, 1020087000, 1020173400, 1020259800, 1020346200, 1020432600, 1020691800, 1020778200, 1020864600, 1020951000, 1021037400, 1021296600, 1021383000, 1021469400, 1021555800, 1021642200, 1021901400, 1021987800, 1022074200, 1022160600, 1022247000, 1022592600, 1022679000, 1022765400, 1022851800, 1023111000, 1023197400, 1023283800, 1023370200, 1023456600, 1023715800, 1023802200, 1023888600, 1023975000, 1024061400, 1024320600, 1024407000, 1024493400, 1024579800, 1024666200, 1024925400, 1025011800, 1025098200, 1025184600, 1025271000, 1025530200, 1025616600, 1025703000, 1025875800, 1026135000, 1026221400, 1026307800, 1026394200, 1026480600, 1026739800, 1026826200, 1026912600, 1026999000, 1027085400, 1027344600, 1027431000, 1027517400, 1027603800, 1027690200, 1027949400, 1028035800, 1028122200, 1028208600, 1028295000, 1028554200, 1028640600, 1028727000, 1028813400, 1028899800, 1029159000, 1029245400, 1029331800, 1029418200, 1029504600, 1029763800, 1029850200, 1029936600, 1030023000, 1030109400, 1030368600, 1030455000, 1030541400, 1030627800, 1030714200, 1031059800, 1031146200, 1031232600, 1031319000, 1031578200, 1031664600, 1031751000, 1031837400, 1031923800, 1032183000, 1032269400, 1032355800, 1032442200, 1032528600, 1032787800, 1032874200, 1032960600, 1033047000, 1033133400, 1033392600, 1033479000, 1033565400, 1033651800, 1033738200, 1033997400, 1034083800, 1034170200, 1034256600, 1034343000, 1034602200, 1034688600, 1034775000, 1034861400, 1034947800, 1035207000, 1035293400, 1035379800, 1035466200, 1035552600, 1035815400, 1035901800, 1035988200, 1036074600, 1036161000, 1036420200, 1036506600, 1036593000, 1036679400, 1036765800, 1037025000, 1037111400, 1037197800, 1037284200, 1037370600, 1037629800, 1037716200, 1037802600, 1037889000, 1037975400, 1038234600, 1038321000, 1038407400, 1038580200, 1038839400, 1038925800, 1039012200, 1039098600, 1039185000, 1039444200, 1039530600, 1039617000, 1039703400, 1039789800, 1040049000, 1040135400, 1040221800, 1040308200, 1040394600, 1040653800, 1040740200, 1040913000, 1040999400, 1041258600, 1041345000, 1041517800, 1041604200, 1041863400, 1041949800, 1042036200, 1042122600, 1042209000, 1042468200, 1042554600, 1042641000, 1042727400, 1042813800, 1043159400, 1043245800, 1043332200, 1043418600, 1043677800, 1043764200, 1043850600, 1043937000, 1044023400, 1044282600, 1044369000, 1044455400, 1044541800, 1044628200, 1044887400, 1044973800, 1045060200, 1045146600, 1045233000, 1045578600, 1045665000, 1045751400, 1045837800, 1046097000, 1046183400, 1046269800, 1046356200, 1046442600, 1046701800, 1046788200, 1046874600, 1046961000, 1047047400, 1047306600, 1047393000, 1047479400, 1047565800, 1047652200, 1047911400, 1047997800, 1048084200, 1048170600, 1048257000, 1048516200, 1048602600, 1048689000, 1048775400, 1048861800, 1049121000, 1049207400, 1049293800, 1049380200, 1049466600, 1049722200, 1049808600, 1049895000, 1049981400, 1050067800, 1050327000, 1050413400, 1050499800, 1050586200, 1050931800, 1051018200, 1051104600, 1051191000, 1051277400, 1051536600, 1051623000, 1051709400, 1051795800, 1051882200, 1052141400, 1052227800, 1052314200, 1052400600, 1052487000, 1052746200, 1052832600, 1052919000, 1053005400, 1053091800, 1053351000, 1053437400, 1053523800, 1053610200, 1053696600, 1054042200, 1054128600, 1054215000, 1054301400, 1054560600, 1054647000, 1054733400, 1054819800, 1054906200, 1055165400, 1055251800, 1055338200, 1055424600, 1055511000, 1055770200, 1055856600, 1055943000, 1056029400, 1056115800, 1056375000, 1056461400, 1056547800, 1056634200, 1056720600, 1056979800, 1057066200, 1057152600, 1057239000, 1057584600, 1057671000, 1057757400, 1057843800, 1057930200, 1058189400, 1058275800, 1058362200, 1058448600, 1058535000, 1058794200, 1058880600, 1058967000, 1059053400, 1059139800, 1059399000, 1059485400, 1059571800, 1059658200, 1059744600, 1060003800, 1060090200, 1060176600, 1060263000, 1060349400, 1060608600, 1060695000, 1060781400, 1060867800, 1060954200, 1061213400, 1061299800, 1061386200, 1061472600, 1061559000, 1061818200, 1061904600, 1061991000, 1062077400, 1062163800, 1062509400, 1062595800, 1062682200, 1062768600, 1063027800, 1063114200, 1063200600, 1063287000, 1063373400, 1063632600, 1063719000, 1063805400, 1063891800, 1063978200, 1064237400, 1064323800, 1064410200, 1064496600, 1064583000, 1064842200, 1064928600, 1065015000, 1065101400, 1065187800, 1065447000, 1065533400, 1065619800, 1065706200, 1065792600, 1066051800, 1066138200, 1066224600, 1066311000, 1066397400, 1066656600, 1066743000, 1066829400, 1066915800, 1067002200, 1067265000, 1067351400, 1067437800, 1067524200, 1067610600, 1067869800, 1067956200, 1068042600, 1068129000, 1068215400, 1068474600, 1068561000, 1068647400, 1068733800, 1068820200, 1069079400, 1069165800, 1069252200, 1069338600, 1069425000, 1069684200, 1069770600, 1069857000, 1070029800, 1070289000, 1070375400, 1070461800, 1070548200, 1070634600, 1070893800, 1070980200, 1071066600, 1071153000, 1071239400, 1071498600, 1071585000, 1071671400, 1071757800, 1071844200, 1072103400, 1072189800, 1072276200, 1072449000, 1072708200, 1072794600, 1072881000, 1073053800, 1073313000, 1073399400, 1073485800, 1073572200, 1073658600, 1073917800, 1074004200, 1074090600, 1074177000, 1074263400, 1074609000, 1074695400, 1074781800, 1074868200, 1075127400, 1075213800, 1075300200, 1075386600, 1075473000, 1075732200, 1075818600, 1075905000, 1075991400, 1076077800, 1076337000, 1076423400, 1076509800, 1076596200, 1076682600, 1077028200, 1077114600, 1077201000, 1077287400, 1077546600, 1077633000, 1077719400, 1077805800, 1077892200, 1078151400, 1078237800, 1078324200, 1078410600, 1078497000, 1078756200, 1078842600, 1078929000, 1079015400, 1079101800, 1079361000, 1079447400, 1079533800, 1079620200, 1079706600, 1079965800, 1080052200, 1080138600, 1080225000, 1080311400, 1080570600, 1080657000, 1080743400, 1080829800, 1080916200, 1081171800, 1081258200, 1081344600, 1081431000, 1081776600, 1081863000, 1081949400, 1082035800, 1082122200, 1082381400, 1082467800, 1082554200, 1082640600, 1082727000, 1082986200, 1083072600, 1083159000, 1083245400, 1083331800, 1083591000, 1083677400, 1083763800, 1083850200, 1083936600, 1084195800, 1084282200, 1084368600, 1084455000, 1084541400, 1084800600, 1084887000, 1084973400, 1085059800, 1085146200, 1085405400, 1085491800, 1085578200, 1085664600, 1085751000, 1086096600, 1086183000, 1086269400, 1086355800, 1086615000, 1086701400, 1086787800, 1086874200, 1087219800, 1087306200, 1087392600, 1087479000, 1087565400, 1087824600, 1087911000, 1087997400, 1088083800, 1088170200, 1088429400, 1088515800, 1088602200, 1088688600, 1088775000, 1089120600, 1089207000, 1089293400, 1089379800, 1089639000, 1089725400, 1089811800, 1089898200, 1089984600, 1090243800, 1090330200, 1090416600, 1090503000, 1090589400, 1090848600, 1090935000, 1091021400, 1091107800, 1091194200, 1091453400, 1091539800, 1091626200, 1091712600, 1091799000, 1092058200, 1092144600, 1092231000, 1092317400, 1092403800, 1092663000, 1092749400, 1092835800, 1092922200, 1093008600, 1093267800, 1093354200, 1093440600, 1093527000, 1093613400, 1093872600, 1093959000, 1094045400, 1094131800, 1094218200, 1094563800, 1094650200, 1094736600, 1094823000, 1095082200, 1095168600, 1095255000, 1095341400, 1095427800, 1095687000, 1095773400, 1095859800, 1095946200, 1096032600, 1096291800, 1096378200, 1096464600, 1096551000, 1096637400, 1096896600, 1096983000, 1097069400, 1097155800, 1097242200, 1097501400, 1097587800, 1097674200, 1097760600, 1097847000, 1098106200, 1098192600, 1098279000, 1098365400, 1098451800, 1098711000, 1098797400, 1098883800, 1098970200, 1099056600, 1099319400, 1099405800, 1099492200, 1099578600, 1099665000, 1099924200, 1100010600, 1100097000, 1100183400, 1100269800, 1100529000, 1100615400, 1100701800, 1100788200, 1100874600, 1101133800, 1101220200, 1101306600, 1101479400, 1101738600, 1101825000, 1101911400, 1101997800, 1102084200, 1102343400, 1102429800, 1102516200, 1102602600, 1102689000, 1102948200, 1103034600, 1103121000, 1103207400, 1103293800, 1103553000, 1103639400, 1103725800, 1103812200, 1104157800, 1104244200, 1104330600, 1104417000, 1104503400, 1104762600, 1104849000, 1104935400, 1105021800, 1105108200, 1105367400, 1105453800, 1105540200, 1105626600, 1105713000, 1106058600, 1106145000, 1106231400, 1106317800, 1106577000, 1106663400, 1106749800, 1106836200, 1106922600, 1107181800, 1107268200, 1107354600, 1107441000, 1107527400, 1107786600, 1107873000, 1107959400, 1108045800, 1108132200, 1108391400, 1108477800, 1108564200, 1108650600, 1108737000, 1109082600, 1109169000, 1109255400, 1109341800, 1109601000, 1109687400, 1109773800, 1109860200, 1109946600, 1110205800, 1110292200, 1110378600, 1110465000, 1110551400, 1110810600, 1110897000, 1110983400, 1111069800, 1111156200, 1111415400, 1111501800, 1111588200, 1111674600, 1112020200, 1112106600, 1112193000, 1112279400, 1112365800, 1112621400, 1112707800, 1112794200, 1112880600, 1112967000, 1113226200, 1113312600, 1113399000, 1113485400, 1113571800, 1113831000, 1113917400, 1114003800, 1114090200, 1114176600, 1114435800, 1114522200, 1114608600, 1114695000, 1114781400, 1115040600, 1115127000, 1115213400, 1115299800, 1115386200, 1115645400, 1115731800, 1115818200, 1115904600, 1115991000, 1116250200, 1116336600, 1116423000, 1116509400, 1116595800, 1116855000, 1116941400, 1117027800, 1117114200, 1117200600, 1117546200, 1117632600, 1117719000, 1117805400, 1118064600, 1118151000, 1118237400, 1118323800, 1118410200, 1118669400, 1118755800, 1118842200, 1118928600, 1119015000, 1119274200, 1119360600, 1119447000, 1119533400, 1119619800, 1119879000, 1119965400, 1120051800, 1120138200, 1120224600, 1120570200, 1120656600, 1120743000, 1120829400, 1121088600, 1121175000, 1121261400, 1121347800, 1121434200, 1121693400, 1121779800, 1121866200, 1121952600, 1122039000, 1122298200, 1122384600, 1122471000, 1122557400, 1122643800, 1122903000, 1122989400, 1123075800, 1123162200, 1123248600, 1123507800, 1123594200, 1123680600, 1123767000, 1123853400, 1124112600, 1124199000, 1124285400, 1124371800, 1124458200, 1124717400, 1124803800, 1124890200, 1124976600, 1125063000, 1125322200, 1125408600, 1125495000, 1125581400, 1125667800, 1126013400, 1126099800, 1126186200, 1126272600, 1126531800, 1126618200, 1126704600, 1126791000, 1126877400, 1127136600, 1127223000, 1127309400, 1127395800, 1127482200, 1127741400, 1127827800, 1127914200, 1128000600, 1128087000, 1128346200, 1128432600, 1128519000, 1128605400, 1128691800, 1128951000, 1129037400, 1129123800, 1129210200, 1129296600, 1129555800, 1129642200, 1129728600, 1129815000, 1129901400, 1130160600, 1130247000, 1130333400, 1130419800, 1130506200, 1130769000, 1130855400, 1130941800, 1131028200, 1131114600, 1131373800, 1131460200, 1131546600, 1131633000, 1131719400, 1131978600, 1132065000, 1132151400, 1132237800, 1132324200, 1132583400, 1132669800, 1132756200, 1132929000, 1133188200, 1133274600, 1133361000, 1133447400, 1133533800, 1133793000, 1133879400, 1133965800, 1134052200, 1134138600, 1134397800, 1134484200, 1134570600, 1134657000, 1134743400, 1135002600, 1135089000, 1135175400, 1135261800, 1135348200, 1135693800, 1135780200, 1135866600, 1135953000, 1136298600, 1136385000, 1136471400, 1136557800, 1136817000, 1136903400, 1136989800, 1137076200, 1137162600, 1137508200, 1137594600, 1137681000, 1137767400, 1138026600, 1138113000, 1138199400, 1138285800, 1138372200, 1138631400, 1138717800, 1138804200, 1138890600, 1138977000, 1139236200, 1139322600, 1139409000, 1139495400, 1139581800, 1139841000, 1139927400, 1140013800, 1140100200, 1140186600, 1140532200, 1140618600, 1140705000, 1140791400, 1141050600, 1141137000, 1141223400, 1141309800, 1141396200, 1141655400, 1141741800, 1141828200, 1141914600, 1142001000, 1142260200, 1142346600, 1142433000, 1142519400, 1142605800, 1142865000, 1142951400, 1143037800, 1143124200, 1143210600, 1143469800, 1143556200, 1143642600, 1143729000, 1143815400, 1144071000, 1144157400, 1144243800, 1144330200, 1144416600, 1144675800, 1144762200, 1144848600, 1144935000, 1145280600, 1145367000, 1145453400, 1145539800, 1145626200, 1145885400, 1145971800, 1146058200, 1146144600, 1146231000, 1146490200, 1146576600, 1146663000, 1146749400, 1146835800, 1147095000, 1147181400, 1147267800, 1147354200, 1147440600, 1147699800, 1147786200, 1147872600, 1147959000, 1148045400, 1148304600, 1148391000, 1148477400, 1148563800, 1148650200, 1148995800, 1149082200, 1149168600, 1149255000, 1149514200, 1149600600, 1149687000, 1149773400, 1149859800, 1150119000, 1150205400, 1150291800, 1150378200, 1150464600, 1150723800, 1150810200, 1150896600, 1150983000, 1151069400, 1151328600, 1151415000, 1151501400, 1151587800, 1151674200, 1151933400, 1152106200, 1152192600, 1152279000, 1152538200, 1152624600, 1152711000, 1152797400, 1152883800, 1153143000, 1153229400, 1153315800, 1153402200, 1153488600, 1153747800, 1153834200, 1153920600, 1154007000, 1154093400, 1154352600, 1154439000, 1154525400, 1154611800, 1154698200, 1154957400, 1155043800, 1155130200, 1155216600, 1155303000, 1155562200, 1155648600, 1155735000, 1155821400, 1155907800, 1156167000, 1156253400, 1156339800, 1156426200, 1156512600, 1156771800, 1156858200, 1156944600, 1157031000, 1157117400, 1157463000, 1157549400, 1157635800, 1157722200, 1157981400, 1158067800, 1158154200, 1158240600, 1158327000, 1158586200, 1158672600, 1158759000, 1158845400, 1158931800, 1159191000, 1159277400, 1159363800, 1159450200, 1159536600, 1159795800, 1159882200, 1159968600, 1160055000, 1160141400, 1160400600, 1160487000, 1160573400, 1160659800, 1160746200, 1161005400, 1161091800, 1161178200, 1161264600, 1161351000, 1161610200, 1161696600, 1161783000, 1161869400, 1161955800, 1162218600, 1162305000, 1162391400, 1162477800, 1162564200, 1162823400, 1162909800, 1162996200, 1163082600, 1163169000, 1163428200, 1163514600, 1163601000, 1163687400, 1163773800, 1164033000, 1164119400, 1164205800, 1164378600, 1164637800, 1164724200, 1164810600, 1164897000, 1164983400, 1165242600, 1165329000, 1165415400, 1165501800, 1165588200, 1165847400, 1165933800, 1166020200, 1166106600, 1166193000, 1166452200, 1166538600, 1166625000, 1166711400, 1166797800, 1167143400, 1167229800, 1167316200, 1167402600, 1167834600, 1167921000, 1168007400, 1168266600, 1168353000, 1168439400, 1168525800, 1168612200, 1168957800, 1169044200, 1169130600, 1169217000, 1169476200, 1169562600, 1169649000, 1169735400, 1169821800, 1170081000, 1170167400, 1170253800, 1170340200, 1170426600, 1170685800, 1170772200, 1170858600, 1170945000, 1171031400, 1171290600, 1171377000, 1171463400, 1171549800, 1171636200, 1171981800, 1172068200, 1172154600, 1172241000, 1172500200, 1172586600, 1172673000, 1172759400, 1172845800, 1173105000, 1173191400, 1173277800, 1173364200, 1173450600, 1173706200, 1173792600, 1173879000, 1173965400, 1174051800, 1174311000, 1174397400, 1174483800, 1174570200, 1174656600, 1174915800, 1175002200, 1175088600, 1175175000, 1175261400, 1175520600, 1175607000, 1175693400, 1175779800, 1176125400, 1176211800, 1176298200, 1176384600, 1176471000, 1176730200, 1176816600, 1176903000, 1176989400, 1177075800, 1177335000, 1177421400, 1177507800, 1177594200, 1177680600, 1177939800, 1178026200, 1178112600, 1178199000, 1178285400, 1178544600, 1178631000, 1178717400, 1178803800, 1178890200, 1179149400, 1179235800, 1179322200, 1179408600, 1179495000, 1179754200, 1179840600, 1179927000, 1180013400, 1180099800, 1180445400, 1180531800, 1180618200, 1180704600, 1180963800, 1181050200, 1181136600, 1181223000, 1181309400, 1181568600, 1181655000, 1181741400, 1181827800, 1181914200, 1182173400, 1182259800, 1182346200, 1182432600, 1182519000, 1182778200, 1182864600, 1182951000, 1183037400, 1183123800, 1183383000, 1183469400, 1183642200, 1183728600, 1183987800, 1184074200, 1184160600, 1184247000, 1184333400, 1184592600, 1184679000, 1184765400, 1184851800, 1184938200, 1185197400, 1185283800, 1185370200, 1185456600, 1185543000, 1185802200, 1185888600, 1185975000, 1186061400, 1186147800, 1186407000, 1186493400, 1186579800, 1186666200, 1186752600, 1187011800, 1187098200, 1187184600, 1187271000, 1187357400, 1187616600, 1187703000, 1187789400, 1187875800, 1187962200, 1188221400, 1188307800, 1188394200, 1188480600, 1188567000, 1188912600, 1188999000, 1189085400, 1189171800, 1189431000, 1189517400, 1189603800, 1189690200, 1189776600, 1190035800, 1190122200, 1190208600, 1190295000, 1190381400, 1190640600, 1190727000, 1190813400, 1190899800, 1190986200, 1191245400, 1191331800, 1191418200, 1191504600, 1191591000, 1191850200, 1191936600, 1192023000, 1192109400, 1192195800, 1192455000, 1192541400, 1192627800, 1192714200, 1192800600, 1193059800, 1193146200, 1193232600, 1193319000, 1193405400, 1193664600, 1193751000, 1193837400, 1193923800, 1194010200, 1194273000, 1194359400, 1194445800, 1194532200, 1194618600, 1194877800, 1194964200, 1195050600, 1195137000, 1195223400, 1195482600, 1195569000, 1195655400, 1195828200, 1196087400, 1196173800, 1196260200, 1196346600, 1196433000, 1196692200, 1196778600, 1196865000, 1196951400, 1197037800, 1197297000, 1197383400, 1197469800, 1197556200, 1197642600, 1197901800, 1197988200, 1198074600, 1198161000, 1198247400, 1198506600, 1198679400, 1198765800, 1198852200, 1199111400, 1199284200, 1199370600, 1199457000, 1199716200, 1199802600, 1199889000, 1199975400, 1200061800, 1200321000, 1200407400, 1200493800, 1200580200, 1200666600, 1201012200, 1201098600, 1201185000, 1201271400, 1201530600, 1201617000, 1201703400, 1201789800, 1201876200, 1202135400, 1202221800, 1202308200, 1202394600, 1202481000, 1202740200, 1202826600, 1202913000, 1202999400, 1203085800, 1203431400, 1203517800, 1203604200, 1203690600, 1203949800, 1204036200, 1204122600, 1204209000, 1204295400, 1204554600, 1204641000, 1204727400, 1204813800, 1204900200, 1205155800, 1205242200, 1205328600, 1205415000, 1205501400, 1205760600, 1205847000, 1205933400, 1206019800, 1206365400, 1206451800, 1206538200, 1206624600, 1206711000, 1206970200, 1207056600, 1207143000, 1207229400, 1207315800, 1207575000, 1207661400, 1207747800, 1207834200, 1207920600, 1208179800, 1208266200, 1208352600, 1208439000, 1208525400, 1208784600, 1208871000, 1208957400, 1209043800, 1209130200, 1209389400, 1209475800, 1209562200, 1209648600, 1209735000, 1209994200, 1210080600, 1210167000, 1210253400, 1210339800, 1210599000, 1210685400, 1210771800, 1210858200, 1210944600, 1211203800, 1211290200, 1211376600, 1211463000, 1211549400, 1211895000, 1211981400, 1212067800, 1212154200, 1212413400, 1212499800, 1212586200, 1212672600, 1212759000, 1213018200, 1213104600, 1213191000, 1213277400, 1213363800, 1213623000, 1213709400, 1213795800, 1213882200, 1213968600, 1214227800, 1214314200, 1214400600, 1214487000, 1214573400, 1214832600, 1214919000, 1215005400, 1215091800, 1215437400, 1215523800, 1215610200, 1215696600, 1215783000, 1216042200, 1216128600, 1216215000, 1216301400, 1216387800, 1216647000, 1216733400, 1216819800, 1216906200, 1216992600, 1217251800, 1217338200, 1217424600, 1217511000, 1217597400, 1217856600, 1217943000, 1218029400, 1218115800, 1218202200, 1218461400, 1218547800, 1218634200, 1218720600, 1218807000, 1219066200, 1219152600, 1219239000, 1219325400, 1219411800, 1219671000, 1219757400, 1219843800, 1219930200, 1220016600, 1220362200, 1220448600, 1220535000, 1220621400, 1220880600, 1220967000, 1221053400, 1221139800, 1221226200, 1221485400, 1221571800, 1221658200, 1221744600, 1221831000, 1222090200, 1222176600, 1222263000, 1222349400, 1222435800, 1222695000, 1222781400, 1222867800, 1222954200, 1223040600, 1223299800, 1223386200, 1223472600, 1223559000, 1223645400, 1223904600, 1223991000, 1224077400, 1224163800, 1224250200, 1224509400, 1224595800, 1224682200, 1224768600, 1224855000, 1225114200, 1225200600, 1225287000, 1225373400, 1225459800, 1225722600, 1225809000, 1225895400, 1225981800, 1226068200, 1226327400, 1226413800, 1226500200, 1226586600, 1226673000, 1226932200, 1227018600, 1227105000, 1227191400, 1227277800, 1227537000, 1227623400, 1227709800, 1227882600, 1228141800, 1228228200, 1228314600, 1228401000, 1228487400, 1228746600, 1228833000, 1228919400, 1229005800, 1229092200, 1229351400, 1229437800, 1229524200, 1229610600, 1229697000, 1229956200, 1230042600, 1230129000, 1230301800, 1230561000, 1230647400, 1230733800, 1230906600, 1231165800, 1231252200, 1231338600, 1231425000, 1231511400, 1231770600, 1231857000, 1231943400, 1232029800, 1232116200, 1232461800, 1232548200, 1232634600, 1232721000, 1232980200, 1233066600, 1233153000, 1233239400, 1233325800, 1233585000, 1233671400, 1233757800, 1233844200, 1233930600, 1234189800, 1234276200, 1234362600, 1234449000, 1234535400, 1234881000, 1234967400, 1235053800, 1235140200, 1235399400, 1235485800, 1235572200, 1235658600, 1235745000, 1236004200, 1236090600, 1236177000, 1236263400, 1236349800, 1236605400, 1236691800, 1236778200, 1236864600, 1236951000, 1237210200, 1237296600, 1237383000, 1237469400, 1237555800, 1237815000, 1237901400, 1237987800, 1238074200, 1238160600, 1238419800, 1238506200, 1238592600, 1238679000, 1238765400, 1239024600, 1239111000, 1239197400, 1239283800, 1239629400, 1239715800, 1239802200, 1239888600, 1239975000, 1240234200, 1240320600, 1240407000, 1240493400, 1240579800, 1240839000, 1240925400, 1241011800, 1241098200, 1241184600, 1241443800, 1241530200, 1241616600, 1241703000, 1241789400, 1242048600, 1242135000, 1242221400, 1242307800, 1242394200, 1242653400, 1242739800, 1242826200, 1242912600, 1242999000, 1243344600, 1243431000, 1243517400, 1243603800, 1243863000, 1243949400, 1244035800, 1244122200, 1244208600, 1244467800, 1244554200, 1244640600, 1244727000, 1244813400, 1245072600, 1245159000, 1245245400, 1245331800, 1245418200, 1245677400, 1245763800, 1245850200, 1245936600, 1246023000, 1246282200, 1246368600, 1246455000, 1246541400, 1246887000, 1246973400, 1247059800, 1247146200, 1247232600, 1247491800, 1247578200, 1247664600, 1247751000, 1247837400, 1248096600, 1248183000, 1248269400, 1248355800, 1248442200, 1248701400, 1248787800, 1248874200, 1248960600, 1249047000, 1249306200, 1249392600, 1249479000, 1249565400, 1249651800, 1249911000, 1249997400, 1250083800, 1250170200, 1250256600, 1250515800, 1250602200, 1250688600, 1250775000, 1250861400, 1251120600, 1251207000, 1251293400, 1251379800, 1251466200, 1251725400, 1251811800, 1251898200, 1251984600, 1252071000, 1252416600, 1252503000, 1252589400, 1252675800, 1252935000, 1253021400, 1253107800, 1253194200, 1253280600, 1253539800, 1253626200, 1253712600, 1253799000, 1253885400, 1254144600, 1254231000, 1254317400, 1254403800, 1254490200, 1254749400, 1254835800, 1254922200, 1255008600, 1255095000, 1255354200, 1255440600, 1255527000, 1255613400, 1255699800, 1255959000, 1256045400, 1256131800, 1256218200, 1256304600, 1256563800, 1256650200, 1256736600, 1256823000, 1256909400, 1257172200, 1257258600, 1257345000, 1257431400, 1257517800, 1257777000, 1257863400, 1257949800, 1258036200, 1258122600, 1258381800, 1258468200, 1258554600, 1258641000, 1258727400, 1258986600, 1259073000, 1259159400, 1259332200, 1259591400, 1259677800, 1259764200, 1259850600, 1259937000, 1260196200, 1260282600, 1260369000, 1260455400, 1260541800, 1260801000, 1260887400, 1260973800, 1261060200, 1261146600, 1261405800, 1261492200, 1261578600, 1261665000, 1262010600, 1262097000, 1262183400, 1262269800, 1262615400, 1262701800, 1262788200, 1262874600, 1262961000, 1263220200, 1263306600, 1263393000, 1263479400, 1263565800, 1263911400, 1263997800, 1264084200, 1264170600, 1264429800, 1264516200, 1264602600, 1264689000, 1264775400, 1265034600, 1265121000, 1265207400, 1265293800, 1265380200, 1265639400, 1265725800, 1265812200, 1265898600, 1265985000, 1266330600, 1266417000, 1266503400, 1266589800, 1266849000, 1266935400, 1267021800, 1267108200, 1267194600, 1267453800, 1267540200, 1267626600, 1267713000, 1267799400, 1268058600, 1268145000, 1268231400, 1268317800, 1268404200, 1268659800, 1268746200, 1268832600, 1268919000, 1269005400, 1269264600, 1269351000, 1269437400, 1269523800, 1269610200, 1269869400, 1269955800, 1270042200, 1270128600, 1270474200, 1270560600, 1270647000, 1270733400, 1270819800, 1271079000, 1271165400, 1271251800, 1271338200, 1271424600, 1271683800, 1271770200, 1271856600, 1271943000, 1272029400, 1272288600, 1272375000, 1272461400, 1272547800, 1272634200, 1272893400, 1272979800, 1273066200, 1273152600, 1273239000, 1273498200, 1273584600, 1273671000, 1273757400, 1273843800, 1274103000, 1274189400, 1274275800, 1274362200, 1274448600, 1274707800, 1274794200, 1274880600, 1274967000, 1275053400, 1275399000, 1275485400, 1275571800, 1275658200, 1275917400, 1276003800, 1276090200, 1276176600, 1276263000, 1276522200, 1276608600, 1276695000, 1276781400, 1276867800, 1277127000, 1277213400, 1277299800, 1277386200, 1277472600, 1277731800, 1277818200, 1277904600, 1277991000, 1278077400, 1278423000, 1278509400, 1278595800, 1278682200, 1278941400, 1279027800, 1279114200, 1279200600, 1279287000, 1279546200, 1279632600, 1279719000, 1279805400, 1279891800, 1280151000, 1280237400, 1280323800, 1280410200, 1280496600, 1280755800, 1280842200, 1280928600, 1281015000, 1281101400, 1281360600, 1281447000, 1281533400, 1281619800, 1281706200, 1281965400, 1282051800, 1282138200, 1282224600, 1282311000, 1282570200, 1282656600, 1282743000, 1282829400, 1282915800, 1283175000, 1283261400, 1283347800, 1283434200, 1283520600, 1283866200, 1283952600, 1284039000, 1284125400, 1284384600, 1284471000, 1284557400, 1284643800, 1284730200, 1284989400, 1285075800, 1285162200, 1285248600, 1285335000, 1285594200, 1285680600, 1285767000, 1285853400, 1285939800, 1286199000, 1286285400, 1286371800, 1286458200, 1286544600, 1286803800, 1286890200, 1286976600, 1287063000, 1287149400, 1287408600, 1287495000, 1287581400, 1287667800, 1287754200, 1288013400, 1288099800, 1288186200, 1288272600, 1288359000, 1288618200, 1288704600, 1288791000, 1288877400, 1288963800, 1289226600, 1289313000, 1289399400, 1289485800, 1289572200, 1289831400, 1289917800, 1290004200, 1290090600, 1290177000, 1290436200, 1290522600, 1290609000, 1290781800, 1291041000, 1291127400, 1291213800, 1291300200, 1291386600, 1291645800, 1291732200, 1291818600, 1291905000, 1291991400, 1292250600, 1292337000, 1292423400, 1292509800, 1292596200, 1292855400, 1292941800, 1293028200, 1293114600, 1293460200, 1293546600, 1293633000, 1293719400, 1293805800, 1294065000, 1294151400, 1294237800, 1294324200, 1294410600, 1294669800, 1294756200, 1294842600, 1294929000, 1295015400, 1295361000, 1295447400, 1295533800, 1295620200, 1295879400, 1295965800, 1296052200, 1296138600, 1296225000, 1296484200, 1296570600, 1296657000, 1296743400, 1296829800, 1297089000, 1297175400, 1297261800, 1297348200, 1297434600, 1297693800, 1297780200, 1297866600, 1297953000, 1298039400, 1298385000, 1298471400, 1298557800, 1298644200, 1298903400, 1298989800, 1299076200, 1299162600, 1299249000, 1299508200, 1299594600, 1299681000, 1299767400, 1299853800, 1300109400, 1300195800, 1300282200, 1300368600, 1300455000, 1300714200, 1300800600, 1300887000, 1300973400, 1301059800, 1301319000, 1301405400, 1301491800, 1301578200, 1301664600, 1301923800, 1302010200, 1302096600, 1302183000, 1302269400, 1302528600, 1302615000, 1302701400, 1302787800, 1302874200, 1303133400, 1303219800, 1303306200, 1303392600, 1303738200, 1303824600, 1303911000, 1303997400, 1304083800, 1304343000, 1304429400, 1304515800, 1304602200, 1304688600, 1304947800, 1305034200, 1305120600, 1305207000, 1305293400, 1305552600, 1305639000, 1305725400, 1305811800, 1305898200, 1306157400, 1306243800, 1306330200, 1306416600, 1306503000, 1306848600, 1306935000, 1307021400, 1307107800, 1307367000, 1307453400, 1307539800, 1307626200, 1307712600, 1307971800, 1308058200, 1308144600, 1308231000, 1308317400, 1308576600, 1308663000, 1308749400, 1308835800, 1308922200, 1309181400, 1309267800, 1309354200, 1309440600, 1309527000, 1309872600, 1309959000, 1310045400, 1310131800, 1310391000, 1310477400, 1310563800, 1310650200, 1310736600, 1310995800, 1311082200, 1311168600, 1311255000, 1311341400, 1311600600, 1311687000, 1311773400, 1311859800, 1311946200, 1312205400, 1312291800, 1312378200, 1312464600, 1312551000, 1312810200, 1312896600, 1312983000, 1313069400, 1313155800, 1313415000, 1313501400, 1313587800, 1313674200, 1313760600, 1314019800, 1314106200, 1314192600, 1314279000, 1314365400, 1314624600, 1314711000, 1314797400, 1314883800, 1314970200, 1315315800, 1315402200, 1315488600, 1315575000, 1315834200, 1315920600, 1316007000, 1316093400, 1316179800, 1316439000, 1316525400, 1316611800, 1316698200, 1316784600, 1317043800, 1317130200, 1317216600, 1317303000, 1317389400, 1317648600, 1317735000, 1317821400, 1317907800, 1317994200, 1318253400, 1318339800, 1318426200, 1318512600, 1318599000, 1318858200, 1318944600, 1319031000, 1319117400, 1319203800, 1319463000, 1319549400, 1319635800, 1319722200, 1319808600, 1320067800, 1320154200, 1320240600, 1320327000, 1320413400, 1320676200, 1320762600, 1320849000, 1320935400, 1321021800, 1321281000, 1321367400, 1321453800, 1321540200, 1321626600, 1321885800, 1321972200, 1322058600, 1322231400, 1322490600, 1322577000, 1322663400, 1322749800, 1322836200, 1323095400, 1323181800, 1323268200, 1323354600, 1323441000, 1323700200, 1323786600, 1323873000, 1323959400, 1324045800, 1324305000, 1324391400, 1324477800, 1324564200, 1324650600, 1324996200, 1325082600, 1325169000, 1325255400, 1325601000, 1325687400, 1325773800, 1325860200, 1326119400, 1326205800, 1326292200, 1326378600, 1326465000, 1326810600, 1326897000, 1326983400, 1327069800, 1327329000, 1327415400, 1327501800, 1327588200, 1327674600, 1327933800, 1328020200, 1328106600, 1328193000, 1328279400, 1328538600, 1328625000, 1328711400, 1328797800, 1328884200, 1329143400, 1329229800, 1329316200, 1329402600, 1329489000, 1329834600, 1329921000, 1330007400, 1330093800, 1330353000, 1330439400, 1330525800, 1330612200, 1330698600, 1330957800, 1331044200, 1331130600, 1331217000, 1331303400, 1331559000, 1331645400, 1331731800, 1331818200, 1331904600, 1332163800, 1332250200, 1332336600, 1332423000, 1332509400, 1332768600, 1332855000, 1332941400, 1333027800, 1333114200, 1333373400, 1333459800, 1333546200, 1333632600, 1333978200, 1334064600, 1334151000, 1334237400, 1334323800, 1334583000, 1334669400, 1334755800, 1334842200, 1334928600, 1335187800, 1335274200, 1335360600, 1335447000, 1335533400, 1335792600, 1335879000, 1335965400, 1336051800, 1336138200, 1336397400, 1336483800, 1336570200, 1336656600, 1336743000, 1337002200, 1337088600, 1337175000, 1337261400, 1337347800, 1337607000, 1337693400, 1337779800, 1337866200, 1337952600, 1338298200, 1338384600, 1338471000, 1338557400, 1338816600, 1338903000, 1338989400, 1339075800, 1339162200, 1339421400, 1339507800, 1339594200, 1339680600, 1339767000, 1340026200, 1340112600, 1340199000, 1340285400, 1340371800, 1340631000, 1340717400, 1340803800, 1340890200, 1340976600, 1341235800, 1341322200, 1341495000, 1341581400, 1341840600, 1341927000, 1342013400, 1342099800, 1342186200, 1342445400, 1342531800, 1342618200, 1342704600, 1342791000, 1343050200, 1343136600, 1343223000, 1343309400, 1343395800, 1343655000, 1343741400, 1343827800, 1343914200, 1344000600, 1344259800, 1344346200, 1344432600, 1344519000, 1344605400, 1344864600, 1344951000, 1345037400, 1345123800, 1345210200, 1345469400, 1345555800, 1345642200, 1345728600, 1345815000, 1346074200, 1346160600, 1346247000, 1346333400, 1346419800, 1346765400, 1346851800, 1346938200, 1347024600, 1347283800, 1347370200, 1347456600, 1347543000, 1347629400, 1347888600, 1347975000, 1348061400, 1348147800, 1348234200, 1348493400, 1348579800, 1348666200, 1348752600, 1348839000, 1349098200, 1349184600, 1349271000, 1349357400, 1349443800, 1349703000, 1349789400, 1349875800, 1349962200, 1350048600, 1350307800, 1350394200, 1350480600, 1350567000, 1350653400, 1350912600, 1350999000, 1351085400, 1351171800, 1351258200, 1351690200, 1351776600, 1351863000, 1352125800, 1352212200, 1352298600, 1352385000, 1352471400, 1352730600, 1352817000, 1352903400, 1352989800, 1353076200, 1353335400, 1353421800, 1353508200, 1353681000, 1353940200, 1354026600, 1354113000, 1354199400, 1354285800, 1354545000, 1354631400, 1354717800, 1354804200, 1354890600, 1355149800, 1355236200, 1355322600, 1355409000, 1355495400, 1355754600, 1355841000, 1355927400, 1356013800, 1356100200, 1356359400, 1356532200, 1356618600, 1356705000, 1356964200, 1357137000, 1357223400, 1357309800, 1357569000, 1357655400, 1357741800, 1357828200, 1357914600, 1358173800, 1358260200, 1358346600, 1358433000, 1358519400, 1358865000, 1358951400, 1359037800, 1359124200, 1359383400, 1359469800, 1359556200, 1359642600, 1359729000, 1359988200, 1360074600, 1360161000, 1360247400, 1360333800, 1360593000, 1360679400, 1360765800, 1360852200, 1360938600, 1361284200, 1361370600, 1361457000, 1361543400, 1361802600, 1361889000, 1361975400, 1362061800, 1362148200, 1362407400, 1362493800, 1362580200, 1362666600, 1362753000, 1363008600, 1363095000, 1363181400, 1363267800, 1363354200, 1363613400, 1363699800, 1363786200, 1363872600, 1363959000, 1364218200, 1364304600, 1364391000, 1364477400, 1364823000, 1364909400, 1364995800, 1365082200, 1365168600, 1365427800, 1365514200, 1365600600, 1365687000, 1365773400, 1366032600, 1366119000, 1366205400, 1366291800, 1366378200, 1366637400, 1366723800, 1366810200, 1366896600, 1366983000, 1367242200, 1367328600, 1367415000, 1367501400, 1367587800, 1367847000, 1367933400, 1368019800, 1368106200, 1368192600, 1368451800, 1368538200, 1368624600, 1368711000, 1368797400, 1369056600, 1369143000, 1369229400, 1369315800, 1369402200, 1369747800, 1369834200, 1369920600, 1370007000, 1370266200, 1370352600, 1370439000, 1370525400, 1370611800, 1370871000, 1370957400, 1371043800, 1371130200, 1371216600, 1371475800, 1371562200, 1371648600, 1371735000, 1371821400, 1372080600, 1372167000, 1372253400, 1372339800, 1372426200, 1372685400, 1372771800, 1372858200, 1373031000, 1373290200, 1373376600, 1373463000, 1373549400, 1373635800, 1373895000, 1373981400, 1374067800, 1374154200, 1374240600, 1374499800, 1374586200, 1374672600, 1374759000, 1374845400, 1375104600, 1375191000, 1375277400, 1375363800, 1375450200, 1375709400, 1375795800, 1375882200, 1375968600, 1376055000, 1376314200, 1376400600, 1376487000, 1376573400, 1376659800, 1376919000, 1377005400, 1377091800, 1377178200, 1377264600, 1377523800, 1377610200, 1377696600, 1377783000, 1377869400, 1378215000, 1378301400, 1378387800, 1378474200, 1378733400, 1378819800, 1378906200, 1378992600, 1379079000, 1379338200, 1379424600, 1379511000, 1379597400, 1379683800, 1379943000, 1380029400, 1380115800, 1380202200, 1380288600, 1380547800, 1380634200, 1380720600, 1380807000, 1380893400, 1381152600, 1381239000, 1381325400, 1381411800, 1381498200, 1381757400, 1381843800, 1381930200, 1382016600, 1382103000, 1382362200, 1382448600, 1382535000, 1382621400, 1382707800, 1382967000, 1383053400, 1383139800, 1383226200, 1383312600, 1383575400, 1383661800, 1383748200, 1383834600, 1383921000, 1384180200, 1384266600, 1384353000, 1384439400, 1384525800, 1384785000, 1384871400, 1384957800, 1385044200, 1385130600, 1385389800, 1385476200, 1385562600, 1385735400, 1385994600, 1386081000, 1386167400, 1386253800, 1386340200, 1386599400, 1386685800, 1386772200, 1386858600, 1386945000, 1387204200, 1387290600, 1387377000, 1387463400, 1387549800, 1387809000, 1387895400, 1388068200, 1388154600, 1388413800, 1388500200, 1388673000, 1388759400, 1389018600, 1389105000, 1389191400, 1389277800, 1389364200, 1389623400, 1389709800, 1389796200, 1389882600, 1389969000, 1390314600, 1390401000, 1390487400, 1390573800, 1390833000, 1390919400, 1391005800, 1391092200, 1391178600, 1391437800, 1391524200, 1391610600, 1391697000, 1391783400, 1392042600, 1392129000, 1392215400, 1392301800, 1392388200, 1392733800, 1392820200, 1392906600, 1392993000, 1393252200, 1393338600, 1393425000, 1393511400, 1393597800, 1393857000, 1393943400, 1394029800, 1394116200, 1394202600, 1394458200, 1394544600, 1394631000, 1394717400, 1394803800, 1395063000, 1395149400, 1395235800, 1395322200, 1395408600, 1395667800, 1395754200, 1395840600, 1395927000, 1396013400, 1396272600, 1396359000, 1396445400, 1396531800, 1396618200, 1396877400, 1396963800, 1397050200, 1397136600, 1397223000, 1397482200, 1397568600, 1397655000, 1397741400, 1398087000, 1398173400, 1398259800, 1398346200, 1398432600, 1398691800, 1398778200, 1398864600, 1398951000, 1399037400, 1399296600, 1399383000, 1399469400, 1399555800, 1399642200, 1399901400, 1399987800, 1400074200, 1400160600, 1400247000, 1400506200, 1400592600, 1400679000, 1400765400, 1400851800, 1401197400, 1401283800, 1401370200, 1401456600, 1401715800, 1401802200, 1401888600, 1401975000, 1402061400, 1402320600, 1402407000, 1402493400, 1402579800, 1402666200, 1402925400, 1403011800, 1403098200, 1403184600, 1403271000, 1403530200, 1403616600, 1403703000, 1403789400, 1403875800, 1404135000, 1404221400, 1404307800, 1404394200, 1404739800, 1404826200, 1404912600, 1404999000, 1405085400, 1405344600, 1405431000, 1405517400, 1405603800, 1405690200, 1405949400, 1406035800, 1406122200, 1406208600, 1406295000, 1406554200, 1406640600, 1406727000, 1406813400, 1406899800, 1407159000, 1407245400, 1407331800, 1407418200, 1407504600, 1407763800, 1407850200, 1407936600, 1408023000, 1408109400, 1408368600, 1408455000, 1408541400, 1408627800, 1408714200, 1408973400, 1409059800, 1409146200, 1409232600, 1409319000, 1409664600, 1409751000, 1409837400, 1409923800, 1410183000, 1410269400, 1410355800, 1410442200, 1410528600, 1410787800, 1410874200, 1410960600, 1411047000, 1411133400, 1411392600, 1411479000, 1411565400, 1411651800, 1411738200, 1411997400, 1412083800, 1412170200, 1412256600, 1412343000, 1412602200, 1412688600, 1412775000, 1412861400, 1412947800, 1413207000, 1413293400, 1413379800, 1413466200, 1413552600, 1413811800, 1413898200, 1413984600, 1414071000, 1414157400, 1414416600, 1414503000, 1414589400, 1414675800, 1414762200, 1415025000, 1415111400, 1415197800, 1415284200, 1415370600, 1415629800, 1415716200, 1415802600, 1415889000, 1415975400, 1416234600, 1416321000, 1416407400, 1416493800, 1416580200, 1416839400, 1416925800, 1417012200, 1417185000, 1417444200, 1417530600, 1417617000, 1417703400, 1417789800, 1418049000, 1418135400, 1418221800, 1418308200, 1418394600, 1418653800, 1418740200, 1418826600, 1418913000, 1418999400, 1419258600, 1419345000, 1419431400, 1419604200, 1419863400, 1419949800, 1420036200, 1420209000, 1420468200, 1420554600, 1420641000, 1420727400, 1420813800, 1421073000, 1421159400, 1421245800, 1421332200, 1421418600, 1421764200, 1421850600, 1421937000, 1422023400, 1422282600, 1422369000, 1422455400, 1422541800, 1422628200, 1422887400, 1422973800, 1423060200, 1423146600, 1423233000, 1423492200, 1423578600, 1423665000, 1423751400, 1423837800, 1424183400, 1424269800, 1424356200, 1424442600, 1424701800, 1424788200, 1424874600, 1424961000, 1425047400, 1425306600, 1425393000, 1425479400, 1425565800, 1425652200, 1425907800, 1425994200, 1426080600, 1426167000, 1426253400, 1426512600, 1426599000, 1426685400, 1426771800, 1426858200, 1427117400, 1427203800, 1427290200, 1427376600, 1427463000, 1427722200, 1427808600, 1427895000, 1427981400, 1428327000, 1428413400, 1428499800, 1428586200, 1428672600, 1428931800, 1429018200, 1429104600, 1429191000, 1429277400, 1429536600, 1429623000, 1429709400, 1429795800, 1429882200, 1430141400, 1430227800, 1430314200, 1430400600, 1430487000, 1430746200, 1430832600, 1430919000, 1431005400, 1431091800, 1431351000, 1431437400, 1431523800, 1431610200, 1431696600, 1431955800, 1432042200, 1432128600, 1432215000, 1432301400, 1432647000, 1432733400, 1432819800, 1432906200, 1433165400, 1433251800, 1433338200, 1433424600, 1433511000, 1433770200, 1433856600, 1433943000, 1434029400, 1434115800, 1434375000, 1434461400, 1434547800, 1434634200, 1434720600, 1434979800, 1435066200, 1435152600, 1435239000, 1435325400, 1435584600, 1435671000, 1435757400, 1435843800, 1436189400, 1436275800, 1436362200, 1436448600, 1436535000, 1436794200, 1436880600, 1436967000, 1437053400, 1437139800, 1437399000, 1437485400, 1437571800, 1437658200, 1437744600, 1438003800, 1438090200, 1438176600, 1438263000, 1438349400, 1438608600, 1438695000, 1438781400, 1438867800, 1438954200, 1439213400, 1439299800, 1439386200, 1439472600, 1439559000, 1439818200, 1439904600, 1439991000, 1440077400, 1440163800, 1440423000, 1440509400, 1440595800, 1440682200, 1440768600, 1441027800, 1441114200, 1441200600, 1441287000, 1441373400, 1441719000, 1441805400, 1441891800, 1441978200, 1442237400, 1442323800, 1442410200, 1442496600, 1442583000, 1442842200, 1442928600, 1443015000, 1443101400, 1443187800, 1443447000, 1443533400, 1443619800, 1443706200, 1443792600, 1444051800, 1444138200, 1444224600, 1444311000, 1444397400, 1444656600, 1444743000, 1444829400, 1444915800, 1445002200, 1445261400, 1445347800, 1445434200, 1445520600, 1445607000, 1445866200, 1445952600, 1446039000, 1446125400, 1446211800, 1446474600, 1446561000, 1446647400, 1446733800, 1446820200, 1447079400, 1447165800, 1447252200, 1447338600, 1447425000, 1447684200, 1447770600, 1447857000, 1447943400, 1448029800, 1448289000, 1448375400, 1448461800, 1448634600, 1448893800, 1448980200, 1449066600, 1449153000, 1449239400, 1449498600, 1449585000, 1449671400, 1449757800, 1449844200, 1450103400, 1450189800, 1450276200, 1450362600, 1450449000, 1450708200, 1450794600, 1450881000, 1450967400, 1451313000, 1451399400, 1451485800, 1451572200, 1451917800, 1452004200, 1452090600, 1452177000, 1452263400, 1452522600, 1452609000, 1452695400, 1452781800, 1452868200, 1453213800, 1453300200, 1453386600, 1453473000, 1453732200, 1453818600, 1453905000, 1453991400, 1454077800, 1454337000, 1454423400, 1454509800, 1454596200, 1454682600, 1454941800, 1455028200, 1455114600, 1455201000, 1455287400, 1455633000, 1455719400, 1455805800, 1455892200, 1456151400, 1456237800, 1456324200, 1456410600, 1456497000, 1456756200, 1456842600, 1456929000, 1457015400, 1457101800, 1457361000, 1457447400, 1457533800, 1457620200, 1457706600, 1457962200, 1458048600, 1458135000, 1458221400, 1458307800, 1458567000, 1458653400, 1458739800, 1458826200, 1459171800, 1459258200, 1459344600, 1459431000, 1459517400, 1459776600, 1459863000, 1459949400, 1460035800, 1460122200, 1460381400, 1460467800, 1460554200, 1460640600, 1460727000, 1460986200, 1461072600, 1461159000, 1461245400, 1461331800, 1461591000, 1461677400, 1461763800, 1461850200, 1461936600, 1462195800, 1462282200, 1462368600, 1462455000, 1462541400, 1462800600, 1462887000, 1462973400, 1463059800, 1463146200, 1463405400, 1463491800, 1463578200, 1463664600, 1463751000, 1464010200, 1464096600, 1464183000, 1464269400, 1464355800, 1464701400, 1464787800, 1464874200, 1464960600, 1465219800, 1465306200, 1465392600, 1465479000, 1465565400, 1465824600, 1465911000, 1465997400, 1466083800, 1466170200, 1466429400, 1466515800, 1466602200, 1466688600, 1466775000, 1467034200, 1467120600, 1467207000, 1467293400, 1467379800, 1467725400, 1467811800, 1467898200, 1467984600, 1468243800, 1468330200, 1468416600, 1468503000, 1468589400, 1468848600, 1468935000, 1469021400, 1469107800, 1469194200, 1469453400, 1469539800, 1469626200, 1469712600, 1469799000, 1470058200, 1470144600, 1470231000, 1470317400, 1470403800, 1470663000, 1470749400, 1470835800, 1470922200, 1471008600, 1471267800, 1471354200, 1471440600, 1471527000, 1471613400, 1471872600, 1471959000, 1472045400, 1472131800, 1472218200, 1472477400, 1472563800, 1472650200, 1472736600, 1472823000, 1473168600, 1473255000, 1473341400, 1473427800, 1473687000, 1473773400, 1473859800, 1473946200, 1474032600, 1474291800, 1474378200, 1474464600, 1474551000, 1474637400, 1474896600, 1474983000, 1475069400, 1475155800, 1475242200, 1475501400, 1475587800, 1475674200, 1475760600, 1475847000, 1476106200, 1476192600, 1476279000, 1476365400, 1476451800, 1476711000, 1476797400, 1476883800, 1476970200, 1477056600, 1477315800, 1477402200, 1477488600, 1477575000, 1477661400, 1477920600, 1478007000, 1478093400, 1478179800, 1478266200, 1478529000, 1478615400, 1478701800, 1478788200, 1478874600, 1479133800, 1479220200, 1479306600, 1479393000, 1479479400, 1479738600, 1479825000, 1479911400, 1480084200, 1480343400, 1480429800, 1480516200, 1480602600, 1480689000, 1480948200, 1481034600, 1481121000, 1481207400, 1481293800, 1481553000, 1481639400, 1481725800, 1481812200, 1481898600, 1482157800, 1482244200, 1482330600, 1482417000, 1482503400, 1482849000, 1482935400, 1483021800, 1483108200, 1483453800, 1483540200, 1483626600, 1483713000, 1483972200, 1484058600, 1484145000, 1484231400, 1484317800, 1484663400, 1484749800, 1484836200, 1484922600, 1485181800, 1485268200, 1485354600, 1485441000, 1485527400, 1485786600, 1485873000, 1485959400, 1486045800, 1486132200, 1486391400, 1486477800, 1486564200, 1486650600, 1486737000, 1486996200, 1487082600, 1487169000, 1487255400, 1487341800, 1487687400, 1487773800, 1487860200, 1487946600, 1488205800, 1488292200, 1488378600, 1488465000, 1488551400, 1488810600, 1488897000, 1488983400, 1489069800, 1489156200, 1489411800, 1489498200, 1489584600, 1489671000, 1489757400, 1490016600, 1490103000, 1490189400, 1490275800, 1490362200, 1490621400, 1490707800, 1490794200, 1490880600, 1490967000, 1491226200, 1491312600, 1491399000, 1491485400, 1491571800, 1491831000, 1491917400, 1492003800, 1492090200, 1492435800, 1492522200, 1492608600, 1492695000, 1492781400, 1493040600, 1493127000, 1493213400, 1493299800, 1493386200, 1493645400, 1493731800, 1493818200, 1493904600, 1493991000, 1494250200, 1494336600, 1494423000, 1494509400, 1494595800, 1494855000, 1494941400, 1495027800, 1495114200, 1495200600, 1495459800, 1495546200, 1495632600, 1495719000, 1495805400, 1496151000, 1496237400, 1496323800, 1496410200, 1496669400, 1496755800, 1496842200, 1496928600, 1497015000, 1497274200, 1497360600, 1497447000, 1497533400, 1497619800, 1497879000, 1497965400, 1498051800, 1498138200, 1498224600, 1498483800, 1498570200, 1498656600, 1498743000, 1498829400, 1499088600, 1499261400, 1499347800, 1499434200, 1499693400, 1499779800, 1499866200, 1499952600, 1500039000, 1500298200, 1500384600, 1500471000, 1500557400, 1500643800, 1500903000, 1500989400, 1501075800, 1501162200, 1501248600, 1501507800, 1501594200, 1501680600, 1501767000, 1501853400, 1502112600, 1502199000, 1502285400, 1502371800, 1502458200, 1502717400, 1502803800, 1502890200, 1502976600, 1503063000, 1503322200, 1503408600, 1503495000, 1503581400, 1503667800, 1503927000, 1504013400, 1504099800, 1504186200, 1504272600, 1504618200, 1504704600, 1504791000, 1504877400, 1505136600, 1505223000, 1505309400, 1505395800, 1505482200, 1505741400, 1505827800, 1505914200, 1506000600, 1506087000, 1506346200, 1506432600, 1506519000, 1506605400, 1506691800, 1506951000, 1507037400, 1507123800, 1507210200, 1507296600, 1507555800, 1507642200, 1507728600, 1507815000, 1507901400, 1508160600, 1508247000, 1508333400, 1508419800, 1508506200, 1508765400, 1508851800, 1508938200, 1509024600, 1509111000, 1509370200, 1509456600, 1509543000, 1509629400, 1509715800, 1509978600, 1510065000, 1510151400, 1510237800, 1510324200, 1510583400, 1510669800, 1510756200, 1510842600, 1510929000, 1511188200, 1511274600, 1511361000, 1511533800, 1511793000, 1511879400, 1511965800, 1512052200, 1512138600, 1512397800, 1512484200, 1512570600, 1512657000, 1512743400, 1513002600, 1513089000, 1513175400, 1513261800, 1513348200, 1513607400, 1513693800, 1513780200, 1513866600, 1513953000, 1514298600, 1514385000, 1514471400, 1514557800, 1514903400, 1514989800, 1515076200, 1515162600, 1515421800, 1515508200, 1515594600, 1515681000, 1515767400, 1516113000, 1516199400, 1516285800, 1516372200, 1516631400, 1516717800, 1516804200, 1516890600, 1516977000, 1517236200, 1517322600, 1517409000, 1517495400, 1517581800, 1517841000, 1517927400, 1518013800, 1518100200, 1518186600, 1518445800, 1518532200, 1518618600, 1518705000, 1518791400, 1519137000, 1519223400, 1519309800, 1519396200, 1519655400, 1519741800, 1519828200, 1519914600, 1520001000, 1520260200, 1520346600, 1520433000, 1520519400, 1520605800, 1520861400, 1520947800, 1521034200, 1521120600, 1521207000, 1521466200, 1521552600, 1521639000, 1521725400, 1521811800, 1522071000, 1522157400, 1522243800, 1522330200, 1522675800, 1522762200, 1522848600, 1522935000, 1523021400, 1523280600, 1523367000, 1523453400, 1523539800, 1523626200, 1523885400, 1523971800, 1524058200, 1524144600, 1524231000, 1524490200, 1524576600, 1524663000, 1524749400, 1524835800, 1525095000, 1525181400, 1525267800, 1525354200, 1525440600, 1525699800, 1525786200, 1525872600, 1525959000, 1526045400, 1526304600, 1526391000, 1526477400, 1526563800, 1526650200, 1526909400, 1526995800, 1527082200, 1527168600, 1527255000, 1527600600, 1527687000, 1527773400, 1527859800, 1528119000, 1528205400, 1528291800, 1528378200, 1528464600, 1528723800, 1528810200, 1528896600, 1528983000, 1529069400, 1529328600, 1529415000, 1529501400, 1529587800, 1529674200, 1529933400, 1530019800, 1530106200, 1530192600, 1530279000, 1530538200, 1530624600, 1530797400, 1530883800, 1531143000, 1531229400, 1531315800, 1531402200, 1531488600, 1531747800, 1531834200, 1531920600, 1532007000, 1532093400, 1532352600, 1532439000, 1532525400, 1532611800, 1532721602 },
                    Opens = new double[]{ 2.375, 2.4375, 2.28125, 2.3359375, 2.28125, 2.2890625, 2.296875, 2.3359375, 2.2890625, 2.28125, 2.2265625, 2.265625, 2.3125, 2.3671875, 2.4375, 2.4296875, 2.4140625, 2.296875, 2.359375, 2.390625, 2.34375, 2.296875, 2.2578125, 2.265625, 2.3046875, 2.3515625, 2.4140625, 2.390625, 2.3671875, 2.3125, 2.34375, 2.40625, 2.453125, 2.375, 2.3671875, 2.3984375, 2.359375, 2.3828125, 2.359375, 2.375, 2.4140625, 2.453125, 2.5546875, 2.5703125, 2.6171875, 2.5859375, 2.578125, 2.578125, 2.5859375, 2.6015625, 2.65625, 2.6328125, 2.640625, 2.6328125, 2.6171875, 2.6171875, 2.5703125, 2.53125, 2.5625, 2.515625, 2.4609375, 2.4765625, 2.515625, 2.515625, 2.484375, 2.4921875, 2.484375, 2.5078125, 2.5390625, 2.5, 2.4609375, 2.3828125, 2.46875, 2.46875, 2.5, 2.5390625, 2.5546875, 2.5625, 2.53125, 2.5859375, 2.5234375, 2.4921875, 2.4921875, 2.4296875, 2.421875, 2.46875, 2.46875, 2.453125, 2.515625, 2.5625, 2.6171875, 2.65625, 2.65625, 2.59375, 2.609375, 2.5390625, 2.546875, 2.53125, 2.5, 2.5, 2.515625, 2.53125, 2.5625, 2.5703125, 2.53125, 2.5234375, 2.515625, 2.5859375, 2.5546875, 2.53125, 2.515625, 2.5078125, 2.5078125, 2.58203125, 2.6328125, 2.65625, 2.65625, 2.65625, 2.6484375, 2.671875, 2.6640625, 2.6640625, 2.6171875, 2.6015625, 2.6953125, 2.6875, 2.6640625, 2.65625, 2.625, 2.640625, 2.6640625, 2.65625, 2.6484375, 2.6328125, 2.625, 2.4921875, 2.515625, 2.453125, 2.4921875, 2.4765625, 2.46875, 2.4921875, 2.5078125, 2.46875, 2.453125, 2.5, 2.4921875, 2.5, 2.5078125, 2.5390625, 2.59375, 2.5546875, 2.59375, 2.53125, 2.5390625, 2.5390625, 2.5390625, 2.546875, 2.53125, 2.578125, 2.625, 2.65625, 2.6484375, 2.640625, 2.65625, 2.5703125, 2.6640625, 2.6796875, 2.71875, 2.734375, 2.703125, 2.5859375, 2.609375, 2.578125, 2.6640625, 2.734375, 2.7890625, 2.8046875, 2.71875, 2.7109375, 2.6484375, 2.640625, 2.609375, 2.6484375, 2.71875, 2.765625, 2.796875, 2.875, 2.875, 2.953125, 2.9765625, 2.875, 2.8984375, 2.9765625, 2.9296875, 2.9765625, 2.9140625, 2.8671875, 2.90625, 2.9375, 2.921875, 3.046875, 3.0078125, 2.96875, 2.9765625, 3.0390625, 3.0625, 3.0625, 3.203125, 3.1875, 3.296875, 3.234375, 3.265625, 3.34375, 3.328125, 3.28125, 3.2890625, 3.375, 3.3515625, 3.2421875, 3.265625, 3.2890625, 3.3203125, 3.3515625, 3.390625, 3.34375, 3.28125, 3.2578125, 3.296875, 3.21875, 3.125, 3.109375, 3.25, 3.2109375, 3.234375, 3.2265625, 3.1484375, 3.03515625, 3.03125, 3.1171875, 3.1015625, 3.015625, 3.015625, 3.0859375, 3.046875, 3.21875, 3.1640625, 3.125, 3.0390625, 3.03125, 3.1640625, 3.1953125, 3.1484375, 3.09375, 3.1171875, 3.2265625, 3.359375, 3.359375, 3.34375, 3.2890625, 3.2734375, 3.3125, 3.34375, 3.3671875, 3.4609375, 3.46875, 3.4296875, 3.4609375, 3.4921875, 3.4921875, 3.4375, 3.4140625, 3.46875, 3.4921875, 3.5234375, 3.5859375, 3.5625, 3.6484375, 3.5625, 3.53125, 3.4921875, 3.515625, 3.5625, 3.5078125, 3.5703125, 3.5234375, 3.609375, 3.6015625, 3.625, 3.5703125, 3.5546875, 3.51953125, 3.578125, 3.5546875, 3.5, 3.46875, 3.5390625, 3.5546875, 3.53125, 3.5078125, 3.5, 3.4296875, 3.46875, 3.4140625, 3.3984375, 3.4453125, 3.4921875, 3.5390625, 3.578125, 3.5, 3.453125, 3.5390625, 3.640625, 3.703125, 3.71875, 3.671875, 3.7421875, 3.8203125, 3.8515625, 3.890625, 3.921875, 3.921875, 3.9375, 3.9140625, 3.84375, 3.8671875, 4.015625, 3.96875, 3.890625, 3.921875, 4.03125, 4.046875, 4.0390625, 4.0078125, 4.03125, 3.953125, 3.8515625, 3.859375, 3.90625, 3.9765625, 4.015625, 3.9140625, 3.921875, 3.9609375, 3.953125, 3.96875, 3.9453125, 3.90625, 3.9453125, 3.9609375, 3.90625, 3.9609375, 3.9765625, 3.9609375, 3.9140625, 3.765625, 3.84375, 3.7890625, 3.8125, 3.796875, 3.8046875, 3.8671875, 3.84375, 3.765625, 3.8046875, 3.7421875, 3.8046875, 3.7890625, 3.8203125, 3.828125, 3.875, 3.953125, 4.046875, 4.0546875, 4.03125, 3.9296875, 3.8125, 3.9375, 3.8515625, 3.8046875, 3.7578125, 3.7578125, 3.6953125, 3.7109375, 3.671875, 3.71875, 3.7578125, 3.796875, 3.828125, 3.8828125, 3.875, 3.875, 3.8828125, 3.7578125, 3.8046875, 3.796875, 3.78125, 3.734375, 3.828125, 3.8359375, 3.8359375, 3.8671875, 3.9453125, 3.953125, 3.9765625, 3.953125, 4.0234375, 4.109375, 4.28125, 4.2421875, 4.2890625, 4.3359375, 4.484375, 4.375, 4.40625, 4.3828125, 4.453125, 4.40625, 4.4609375, 4.578125, 4.546875, 4.5546875, 4.6015625, 4.53125, 4.484375, 4.4375, 4.390625, 4.3671875, 4.4140625, 4.390625, 4.34375, 4.4765625, 4.5078125, 4.484375, 4.71875, 4.8359375, 4.7890625, 4.78125, 4.7265625, 4.7109375, 4.8515625, 4.984375, 4.9765625, 4.9296875, 5.1171875, 5.1328125, 5.015625, 5.09375, 5.0859375, 4.9921875, 5.0, 5.0078125, 4.9453125, 5.0859375, 5.0390625, 5.09375, 5.328125, 5.3515625, 5.265625, 5.3359375, 5.46875, 5.5703125, 5.4921875, 5.57421875, 5.5, 5.203125, 5.3125, 5.234375, 5.1640625, 5.3046875, 5.1796875, 5.2578125, 5.234375, 5.2890625, 5.2578125, 5.2109375, 5.25, 5.3125, 5.453125, 5.6484375, 5.7421875, 5.6640625, 5.671875, 5.6875, 5.59375, 5.421875, 5.5, 5.6015625, 5.6796875, 5.7265625, 5.6640625, 5.8046875, 6.0, 6.1875, 6.046875, 6.2578125, 6.140625, 6.734375, 6.6875, 6.0625, 5.953125, 5.875, 5.7890625, 5.96875, 6.1015625, 6.03125, 6.0, 5.8203125, 5.671875, 5.6875, 5.5, 5.7109375, 5.90625, 5.8828125, 6.109375, 6.0625, 5.921875, 6.0546875, 6.203125, 6.1484375, 6.1796875, 6.23046875, 6.09375, 5.9453125, 6.25, 6.1328125, 6.0234375, 5.8984375, 5.6328125, 5.8046875, 5.8359375, 5.734375, 5.6796875, 5.953125, 5.890625, 5.859375, 5.9609375, 6.109375, 6.0078125, 6.0078125, 5.921875, 5.828125, 5.75, 5.84375, 5.703125, 5.6171875, 5.65625, 5.6484375, 5.5, 5.5625, 5.7265625, 5.65625, 5.5078125, 5.5078125, 5.375, 5.46875, 5.3046875, 5.078125, 5.3671875, 5.4375, 5.53125, 5.359375, 5.453125, 6.140625, 5.953125, 6.03125, 5.9375, 6.0234375, 6.1015625, 5.984375, 6.09375, 6.203125, 6.4375, 6.2578125, 6.1640625, 6.25, 6.0625, 6.03125, 5.8671875, 6.03125, 6.15625, 6.0390625, 5.9921875, 5.8984375, 5.65625, 5.625, 5.5234375, 5.3671875, 5.5234375, 5.4765625, 5.5390625, 5.4375, 5.7421875, 5.59375, 5.4453125, 5.3828125, 5.4921875, 5.3828125, 5.6953125, 5.7890625, 5.90625, 5.8125, 5.6953125, 5.765625, 5.5703125, 5.5234375, 5.4609375, 5.6953125, 5.484375, 5.6328125, 5.640625, 5.6484375, 5.515625, 5.453125, 5.4921875, 5.5703125, 5.453125, 5.390625, 5.40625, 5.375, 5.03125, 5.21875, 5.40625, 5.3671875, 5.2265625, 5.296875, 5.34375, 5.640625, 5.7265625, 5.7109375, 5.6171875, 5.7109375, 5.5703125, 5.671875, 5.6796875, 5.671875, 5.765625, 5.875, 5.796875, 6.03125, 6.0, 6.0390625, 6.171875, 6.234375, 6.109375, 6.2578125, 6.15625, 6.171875, 6.078125, 6.109375, 6.25, 6.4140625, 6.4375, 6.296875, 6.296875, 6.1875, 6.1484375, 6.0, 6.0, 6.140625, 6.0703125, 5.96875, 5.9453125, 6.015625, 6.139643669128418, 6.3203125, 6.25, 6.3984375, 6.6484375, 6.640625, 6.515625, 6.3203125, 6.34375, 6.2109375, 6.453125, 6.3671875, 6.3984375, 6.446287631988525, 6.421875, 6.5078125, 6.5390625, 6.40625, 6.5, 6.34375, 6.34375, 6.3203125, 6.3125, 6.515625, 6.578125, 6.625, 6.9765625, 6.90625, 7.046875, 7.0625, 6.96875, 7.046875, 7.0859375, 7.0390625, 7.0546875, 7.1953125, 7.03125, 6.9375, 6.9765625, 7.0546875, 7.140625, 7.1484375, 7.1875, 7.453125, 7.4375, 7.25, 7.3671875, 7.3359375, 7.296875, 7.1875, 7.3046875, 7.40625, 7.421875, 7.3359375, 7.328125, 7.41015625, 7.421875, 7.4140625, 7.375, 7.609375, 7.3515625, 7.5625, 7.546875, 7.6796875, 7.8203125, 7.796875, 7.6953125, 7.78125, 7.65625, 7.625, 7.6484375, 7.75, 7.765625, 7.6171875, 7.53125, 7.53125, 7.515625, 7.6484375, 7.6015625, 7.5078125, 7.40625, 7.5546875, 7.5, 7.390625, 7.19921875, 7.0234375, 6.890625, 7.359375, 7.328125, 7.359375, 7.563475131988525, 7.5, 6.84375, 7.25, 7.46875, 7.453125, 7.3671875, 7.4375, 7.3671875, 7.6328125, 7.71875, 7.6015625, 7.7734375, 7.7578125, 7.796875, 7.765625, 7.8125, 7.7421875, 7.796875, 7.8203125, 7.7421875, 7.734375, 7.65625, 7.7578125, 7.8046875, 7.7109375, 7.6953125, 7.8125, 7.828125, 7.765625, 7.625, 7.7109375, 7.671875, 7.625, 7.65625, 7.796875, 7.74609375, 7.84375, 8.1015625, 8.1953125, 8.3046875, 8.375, 8.53125, 8.5625, 8.6015625, 8.59375, 8.59375, 8.484375, 8.28125, 8.34375, 8.234375, 8.3046875, 8.421875, 8.4140625, 8.5234375, 8.609375, 8.5, 8.359375, 8.4140625, 8.5859375, 8.671875, 8.671875, 8.640625, 8.375, 8.421875, 8.3203125, 8.2890625, 8.4296875, 8.5390625, 8.54296875, 8.5546875, 8.4609375, 8.5234375, 8.578125, 8.578125, 8.65625, 8.8359375, 9.046875, 8.9609375, 8.9609375, 9.0, 8.9765625, 9.078125, 9.421875, 9.3359375, 9.40625, 9.75, 9.65625, 9.421875, 9.4453125, 9.625, 9.6328125, 9.71875, 9.84375, 9.8984375, 9.6953125, 9.5859375, 9.3125, 9.796875, 10.546875, 10.125, 10.625, 10.140625, 10.03125, 9.578125, 10.171875, 10.5, 10.6875, 10.515625, 10.515625, 10.65625, 10.6875, 10.671875, 10.5, 10.390625, 10.28125, 10.578125, 10.5625, 10.625, 10.46875, 10.25, 10.640625, 10.53125, 10.703125, 10.625, 10.703125, 10.875, 11.328125, 11.890625, 12.265625, 11.890625, 12.0625, 12.296875, 12.09375, 12.28125, 12.734375, 12.828125, 12.796875, 12.890625, 12.28125, 12.296875, 12.59375, 12.28125, 12.296875, 12.5625, 12.421875, 12.203125, 12.171875, 12.109375, 11.859375, 11.78125, 12.5, 12.4375, 12.53125, 12.015625, 12.125, 12.40625, 12.4375, 12.59375, 12.296875, 12.140625, 12.453125, 12.21875, 12.28125, 12.46875, 12.3125, 12.578125, 12.21875, 12.078125, 12.0625, 11.640625, 11.328125, 11.34375, 11.890625, 11.640625, 11.265625, 11.609375, 11.375, 11.71875, 11.953125, 12.078125, 12.390625, 12.21875, 11.9375, 11.84375, 12.234375, 12.078125, 12.3125, 13.0, 13.375, 13.453125, 13.828125, 14.578125, 14.1875, 14.21875, 14.671875, 14.828125, 15.25, 15.25, 14.953125, 14.921875, 14.546875, 14.40625, 14.75, 14.6875, 14.796875, 14.796875, 14.5, 14.546875, 14.515625, 14.484375, 15.078125, 15.125, 15.171875, 15.34375, 15.90625, 15.90625, 14.6875, 15.625, 15.421875, 15.078125, 15.03320026397705, 15.171875, 15.609375, 15.640625, 15.6015625, 15.84375, 15.84375, 16.203125, 16.4375, 16.65625, 16.046875, 16.171875, 16.234375, 16.1875, 16.609375, 16.265625, 16.140625, 15.984375, 15.8046875, 15.703125, 16.2578125, 16.328125, 16.171875, 16.5625, 16.3125, 16.28125, 16.3515625, 17.2578125, 17.796875, 18.8125, 18.125, 17.609375, 17.109375, 18.15625, 17.296875, 17.3125, 17.40625, 17.09375, 17.59375, 17.75, 17.671875, 17.53125, 17.78125, 17.9140625, 18.0390625, 17.8515625, 17.5, 17.421875, 17.265625, 17.0, 16.9375, 16.625, 16.84375, 17.375, 17.703125, 16.90625, 17.234375, 17.0, 16.90625, 16.6875, 16.5078125, 16.640625, 17.203125, 17.0, 17.3828125, 17.21875, 17.40625, 17.359375, 16.875, 17.109375, 16.953125, 16.46875, 17.078125, 16.78125, 16.53125, 16.875, 16.6953125, 16.953125, 16.609375, 16.703125, 16.6875, 16.8046875, 16.5625, 16.71875, 16.8671875, 16.984375, 16.875, 17.078125, 17.3125, 17.2890625, 17.125, 17.140625, 16.96875, 17.015625, 16.640625, 16.625, 17.015625, 17.296875, 16.5, 17.109375, 16.859375, 16.53125, 16.71875, 16.1875, 16.375, 16.453125, 16.75, 16.8125, 16.65625, 16.2578125, 16.453125, 16.3046875, 16.265625, 16.2578125, 16.453125, 16.8125, 16.8671875, 16.703125, 16.90625, 17.2265625, 17.140625, 16.984375, 17.375, 17.65625, 17.7421875, 17.9453125, 17.7109375, 18.09375, 17.765625, 17.9375, 18.125, 17.78125, 17.546875, 17.21875, 17.0546875, 16.984375, 17.484375, 16.8125, 16.09375, 16.140625, 15.8828125, 15.46875, 14.859375, 15.296875, 15.8046875, 16.375, 16.203125, 16.40625, 16.21875, 16.234375, 16.078125, 16.2578125, 15.578125, 16.1875, 16.515625, 16.296875, 16.546875, 16.765625, 17.15625, 16.9453125, 17.34375, 17.484375, 17.796875, 18.28125, 18.6171875, 18.578125, 18.96875, 19.390625, 19.484375, 19.765625, 19.421875, 19.84375, 19.65625, 19.921875, 19.7578125, 19.828125, 19.8125, 19.328125, 19.4375, 19.4375, 20.234375, 20.84375, 20.859375, 21.375, 21.390625, 21.46875, 20.65625, 20.609375, 19.8125, 20.078125, 20.625, 20.296875, 20.5, 20.625, 20.609375, 20.609375, 20.421875, 20.015625, 20.375, 20.53125, 20.296875, 21.046875, 22.625, 22.125, 22.34375, 22.015625, 22.15625, 22.453125, 22.671875, 23.0, 23.15625, 22.328125, 21.6875, 21.984375, 22.203125, 22.171875, 22.3125, 22.59375, 22.875, 23.09375, 23.671875, 24.0, 24.3125, 23.375, 22.375, 23.03125, 22.515625, 22.875, 22.546875, 22.296875, 21.90625, 21.875, 21.546875, 20.46875, 21.53125, 20.8125, 22.171875, 21.46875, 22.53125, 21.125, 21.453125, 21.53125, 21.453125, 21.65625, 21.328125, 20.71875, 21.4375, 21.59375, 20.96875, 21.015625, 21.46875, 21.171875, 21.46875, 21.40625, 21.328125, 21.65625, 21.609375, 21.21875, 21.171875, 21.578125, 22.484375, 22.703125, 23.21875, 23.6875, 23.953125, 25.421875, 26.46875, 25.5625, 26.375, 26.3125, 27.28125, 27.203125, 26.71875, 26.953125, 27.03125, 27.5, 27.78125, 28.53125, 29.5625, 29.0, 29.515625, 29.375, 29.546875, 29.21875, 28.0, 29.078125, 28.4375, 28.28125, 29.0, 28.3125, 27.984375, 28.234375, 27.28125, 27.15625, 26.03125, 25.9375, 26.859375, 26.453125, 25.71875, 26.078125, 26.03125, 26.171875, 25.875, 26.96875, 28.0625, 27.53125, 27.78125, 27.578125, 27.84375, 27.8125, 27.640625, 27.0625, 26.21875, 23.8125, 25.453125, 24.6875, 24.921875, 25.0, 25.5625, 25.0, 25.28125, 26.234375, 26.46875, 26.96875, 26.328125, 26.4375, 25.625, 27.109375, 27.40625, 28.28125, 27.28125, 28.25, 27.953125, 28.078125, 27.015625, 25.84375, 25.625, 25.625, 24.34375, 22.640625, 23.296875, 24.8125, 24.875, 23.96875, 25.03125, 26.5, 26.03125, 25.8125, 26.21875, 26.5, 27.25, 26.703125, 26.9375, 26.21875, 26.359375, 26.8125, 26.59375, 26.375, 26.453125, 26.328125, 26.515625, 27.296875, 27.53125, 28.359375, 27.5625, 27.109375, 27.828125, 27.25, 27.390625, 27.265625, 28.1875, 28.125, 29.671875, 30.671875, 31.28125, 32.34375, 30.0625, 32.296875, 31.71875, 31.234375, 31.921875, 33.21875, 33.03125, 33.28125, 32.75, 33.1875, 32.328125, 33.125, 33.546875, 33.75, 34.65625, 35.125, 35.09375, 35.90625, 35.75, 35.640625, 35.21875, 34.796875, 34.90232467651367, 35.46875, 37.375, 37.4375, 38.046875, 37.71875, 37.03125, 34.0, 36.3125, 35.734375, 37.84375, 41.734375, 40.4375, 38.90625, 40.421875, 41.375, 43.125, 42.9375, 43.6875, 43.859375, 43.125, 41.59375, 42.03125, 40.0625, 40.640625, 41.234375, 39.96875, 40.6875, 40.421875, 39.9375, 38.234375, 37.734375, 36.859375, 37.0625, 38.234375, 39.109375, 38.09375, 38.09375, 37.390625, 37.96875, 37.25, 37.78125, 38.6875, 38.875, 39.96875, 40.578125, 40.25, 40.65625, 40.140625, 41.375, 42.265625, 41.609375, 43.46875, 43.234375, 43.171875, 41.75, 43.265625, 44.734375, 45.0625, 46.6875, 47.25, 45.625, 47.15625, 47.59375, 47.4375, 46.625, 47.125, 45.8125, 46.5625, 45.40625, 43.65625, 44.53125, 43.4375, 41.125, 41.0625, 42.5, 42.5, 43.59375, 44.375, 42.65625, 41.1875, 41.375, 40.71875, 40.25, 39.5625, 40.34375, 39.8125, 39.9375, 40.375, 40.375, 40.5625, 39.4375, 38.6875, 39.90625, 39.9375, 39.78125, 39.25, 38.9375, 38.375, 38.59375, 39.125, 39.375, 40.3125, 39.03125, 39.1875, 38.46875, 39.96875, 39.9375, 40.0625, 40.9375, 40.0, 39.375, 39.03125, 39.53125, 40.34375, 41.15625, 42.4375, 44.21875, 42.96875, 42.875, 42.6875, 42.75, 43.34375, 43.875, 44.9375, 45.4375, 46.125, 45.03125, 45.90625, 46.1875, 46.59375, 46.5625, 46.875, 47.5, 47.75, 50.0, 48.21875, 46.8125, 47.1875, 45.78125, 44.4375, 44.40625, 44.59375, 44.34375, 43.8125, 42.84375, 42.9375, 42.5625, 42.6875, 43.03125, 42.8125, 41.78125, 42.0, 41.96875, 41.46875, 42.53125, 42.71875, 42.21875, 42.28125, 42.0, 42.15625, 43.53125, 46.8125, 47.6875, 47.53125, 46.4375, 45.90625, 46.15625, 45.75, 46.875, 47.4375, 46.84375, 46.21875, 47.53125, 47.25, 46.84375, 47.75, 46.4375, 47.1875, 48.0, 48.28125, 47.375, 48.4375, 45.09375, 46.0, 45.625, 45.78125, 45.0, 45.09375, 45.25, 46.375, 46.15625, 46.84375, 46.75, 47.3125, 47.0, 46.0, 45.4375, 44.75, 43.59375, 44.125, 45.78125, 45.28125, 46.78125, 46.0, 47.1875, 45.75, 45.0, 45.71875, 46.625, 46.375, 46.46875, 46.15625, 45.90625, 42.40625, 44.875, 44.0625, 44.125, 44.875, 44.125, 43.46875, 43.21875, 42.46875, 42.21875, 44.8125, 44.625, 44.78125, 45.8125, 45.0625, 44.875, 45.53125, 46.53125, 47.90625, 47.625, 47.375, 46.5625, 46.0, 46.6875, 46.804649353027344, 48.09375, 49.28125, 54.625, 58.3125, 57.40625, 56.1875, 58.148399353027344, 58.625, 59.21875, 59.375, 58.46875, 58.9375, 58.75, 58.6875, 56.78125, 55.5625, 56.09375, 54.3125, 56.71875, 55.75, 54.25, 52.1875, 53.59375, 55.90625, 55.25, 53.53125, 53.5, 51.898399353027344, 50.5, 51.21875, 49.9453010559082, 49.0625, 48.8125, 49.25, 51.21875, 51.03125, 52.1875, 53.40625, 53.21875, 54.71875, 51.9453010559082, 52.4375, 50.617149353027344, 49.875, 49.625, 49.25, 50.0, 47.5625, 46.75, 47.125, 47.34375, 45.125, 45.875, 44.8125, 45.90625, 47.375, 48.0, 48.0625, 46.90625, 47.65625, 49.78125, 48.8125, 49.3125, 47.28125, 47.96875, 47.625, 49.375, 48.375, 51.40625, 53.40625, 56.3125, 53.8828010559082, 51.8125, 52.59375, 53.09375, 53.0, 47.21875, 45.78125, 44.125, 43.9375, 43.5, 44.3125, 42.5625, 41.0625, 40.4375, 39.5625, 37.125, 38.25, 40.71875, 39.3125, 33.625, 34.375, 35.0, 33.71875, 35.375, 36.4375, 36.40625, 35.1875, 35.15625, 35.125, 35.46875, 35.09375, 33.875, 33.3125, 34.21875, 34.5, 34.78125, 34.4375, 34.03125, 32.6875, 32.5625, 31.9375, 31.5625, 32.28125, 31.03125, 31.21875, 31.82029914855957, 32.1875, 33.0, 33.0078010559082, 34.09375, 34.625, 35.78125, 34.8125, 34.5, 33.375, 34.90625, 35.40625, 36.3125, 36.28125, 36.9375, 38.5, 40.6875, 39.96875, 38.75, 39.625, 39.5, 39.125, 38.53125, 39.84375, 39.9375, 39.4375, 40.6328010559082, 40.34375, 39.40625, 39.125, 39.40625, 39.75, 39.125, 38.8125, 38.03125, 36.71875, 37.375, 36.03125, 35.40625, 34.53125, 33.9375, 35.46875, 35.09375, 34.96875, 34.28125, 34.09375, 34.71875, 35.09375, 35.03125, 36.875, 36.90625, 36.21875, 36.15625, 36.0, 35.9375, 35.5625, 35.5625, 35.34375, 35.40625, 35.40625, 35.3125, 35.34375, 35.09375, 35.59375, 35.34375, 35.4375, 35.0, 35.0, 35.03125, 35.0, 35.1875, 34.5625, 34.375, 33.78125, 34.34375, 32.71875, 32.125, 31.8125, 32.4375, 32.0, 30.5625, 31.625, 30.46875, 31.71875, 30.40625, 30.5, 30.25, 29.78125, 28.1875, 27.75, 27.90625, 27.8125, 26.96875, 27.0, 28.15625, 26.9375, 26.75, 25.9375, 24.8125, 29.21875, 30.65625, 32.3125, 31.3125, 30.96875, 30.5, 32.34375, 33.75, 34.5, 34.25, 35.1875, 34.625, 34.34375, 34.875, 35.5625, 34.25, 34.96875, 33.34375, 34.0, 34.53125, 34.71875, 34.71875, 34.0625, 33.6875, 33.03125, 34.5, 35.71875, 34.6875, 33.40625, 31.0, 29.03125, 28.625, 29.59375, 30.0, 26.71875, 27.3125, 27.75, 28.90625, 30.25, 28.96875, 25.523399353027344, 24.5, 23.71875, 21.40625, 20.375, 22.375, 23.4375, 23.0625, 22.5625, 21.96875, 22.0625, 21.59375, 23.90625, 24.25, 24.46875, 25.0, 25.5, 26.5, 27.4375, 26.6875, 26.8125, 26.84375, 30.0, 30.375, 29.875, 30.5, 31.375, 30.5, 31.78125, 32.25, 31.5, 30.40625, 31.25, 30.375, 31.03125, 31.0, 31.875, 30.65625, 29.40625, 29.8125, 28.8125, 29.5, 28.5, 28.6875, 27.625, 28.15625, 27.21875, 28.8125, 29.6875, 29.78125, 29.28125, 28.75, 28.625, 29.3125, 29.9375, 30.15625, 28.96875, 27.34375, 26.09375, 26.25, 27.65625, 26.25, 27.25, 27.28125, 26.125, 25.28125, 27.46875, 28.5625, 28.03125, 28.6875, 27.6875, 27.875, 27.40625, 27.65625, 26.6875, 26.875, 28.1875, 28.28499984741211, 28.975000381469727, 30.325000762939453, 29.780000686645508, 30.700000762939453, 30.260000228881836, 31.69499969482422, 32.904998779296875, 35.150001525878906, 34.05500030517578, 34.099998474121094, 33.78499984741211, 35.03499984741211, 34.76499938964844, 34.26499938964844, 33.83000183105469, 35.5, 34.625, 34.0, 35.415000915527344, 35.875, 35.619998931884766, 35.564998626708984, 34.97999954223633, 34.564998626708984, 34.369998931884766, 33.849998474121094, 34.54999923706055, 33.845001220703125, 34.025001525878906, 34.724998474121094, 35.19499969482422, 34.970001220703125, 35.83000183105469, 35.400001525878906, 34.779998779296875, 34.744998931884766, 34.79999923706055, 35.275001525878906, 35.380001068115234, 36.44499969482422, 36.060001373291016, 36.849998474121094, 36.42499923706055, 35.5099983215332, 36.025001525878906, 35.11000061035156, 33.755001068115234, 33.974998474121094, 34.10499954223633, 33.56999969482422, 34.57500076293945, 35.0, 34.54999923706055, 33.90999984741211, 34.93000030517578, 35.775001525878906, 36.29999923706055, 36.025001525878906, 35.150001525878906, 35.11000061035156, 34.150001525878906, 33.099998474121094, 32.95000076293945, 32.10499954223633, 35.349998474121094, 35.70000076293945, 35.724998474121094, 35.33000183105469, 35.29999923706055, 35.61000061035156, 34.01499938964844, 34.619998931884766, 33.5, 33.130001068115234, 33.560001373291016, 33.025001525878906, 32.82500076293945, 33.005001068115234, 33.400001525878906, 33.60499954223633, 33.650001525878906, 33.26499938964844, 33.02000045776367, 33.255001068115234, 32.4900016784668, 32.3849983215332, 32.619998931884766, 32.875, 32.35499954223633, 31.420000076293945, 31.889999389648438, 30.829999923706055, 31.350000381469727, 30.565000534057617, 30.334999084472656, 29.799999237060547, 30.950000762939453, 31.170000076293945, 30.524999618530273, 29.520000457763672, 28.424999237060547, 28.594999313354492, 28.09000015258789, 28.280000686645508, 28.05500030517578, 27.459999084472656, 27.010000228881836, 26.704999923706055, 27.229999542236328, 26.174999237060547, 23.959999084472656, 25.325000762939453, 26.135000228881836, 25.7549991607666, 25.049999237060547, 24.809999465942383, 25.469999313354492, 25.815000534057617, 26.239999771118164, 28.459999084472656, 28.079999923706055, 28.399999618530273, 28.75, 26.799999237060547, 27.8799991607666, 27.850000381469727, 27.950000762939453, 28.934999465942383, 29.559999465942383, 28.170000076293945, 28.700000762939453, 28.950000762939453, 30.235000610351562, 30.25, 30.30500030517578, 31.15999984741211, 31.049999237060547, 29.459999084472656, 29.649999618530273, 30.040000915527344, 30.96500015258789, 30.93000030517578, 31.350000381469727, 32.11000061035156, 32.22999954223633, 32.16999816894531, 32.349998474121094, 33.400001525878906, 34.1150016784668, 33.04999923706055, 33.18000030517578, 33.125, 33.224998474121094, 32.18000030517578, 32.189998626708984, 32.494998931884766, 32.39500045776367, 31.584999084472656, 31.559999465942383, 32.33000183105469, 31.915000915527344, 32.5, 33.244998931884766, 33.9900016784668, 34.125, 33.7599983215332, 33.83000183105469, 33.58000183105469, 33.564998626708984, 33.0099983215332, 33.584999084472656, 34.459999084472656, 34.310001373291016, 34.57500076293945, 34.005001068115234, 33.86000061035156, 33.709999084472656, 33.9900016784668, 34.1150016784668, 33.91999816894531, 33.32500076293945, 33.55500030517578, 34.625, 34.875, 34.345001220703125, 34.86000061035156, 34.2400016784668, 34.7599983215332, 34.150001525878906, 34.33000183105469, 34.42499923706055, 34.26499938964844, 33.54999923706055, 33.30500030517578, 32.025001525878906, 32.04999923706055, 32.095001220703125, 32.1150016784668, 31.950000762939453, 31.225000381469727, 31.530000686645508, 32.07500076293945, 31.200000762939453, 30.399999618530273, 30.80500030517578, 30.149999618530273, 30.059999465942383, 30.0049991607666, 30.2450008392334, 30.1299991607666, 31.0, 30.850000381469727, 29.954999923706055, 29.700000762939453, 29.860000610351562, 29.024999618530273, 28.969999313354492, 29.549999237060547, 29.53499984741211, 29.389999389648438, 29.524999618530273, 30.6200008392334, 31.5, 31.454999923706055, 31.834999084472656, 31.815000534057617, 31.7549991607666, 31.260000228881836, 31.040000915527344, 31.06999969482422, 30.5, 31.3700008392334, 31.09000015258789, 30.700000762939453, 30.104999542236328, 30.524999618530273, 30.239999771118164, 29.549999237060547, 29.399999618530273, 29.975000381469727, 29.915000915527344, 29.450000762939453, 28.719999313354492, 27.989999771118164, 28.44499969482422, 27.149999618530273, 28.665000915527344, 27.559999465942383, 27.94499969482422, 27.575000762939453, 28.0, 28.200000762939453, 28.975000381469727, 28.395000457763672, 28.719999313354492, 28.260000228881836, 27.850000381469727, 26.924999237060547, 26.450000762939453, 27.03499984741211, 25.735000610351562, 26.049999237060547, 26.079999923706055, 26.31999969482422, 25.6299991607666, 24.719999313354492, 24.59000015258789, 25.635000228881836, 27.200000762939453, 26.264999389648438, 25.225000381469727, 27.200000762939453, 27.2450008392334, 27.354999542236328, 28.104999542236328, 27.75, 26.975000381469727, 25.934999465942383, 26.850000381469727, 27.06999969482422, 26.795000076293945, 25.825000762939453, 25.81999969482422, 26.450000762939453, 25.4950008392334, 24.75, 25.21500015258789, 25.75, 24.94499969482422, 25.825000762939453, 26.6200008392334, 26.290000915527344, 27.420000076293945, 26.575000762939453, 27.829999923706055, 27.764999389648438, 27.735000610351562, 27.184999465942383, 26.700000762939453, 26.045000076293945, 27.350000381469727, 25.524999618530273, 27.299999237060547, 27.274999618530273, 27.059999465942383, 26.190000534057617, 25.6200008392334, 26.545000076293945, 27.204999923706055, 26.655000686645508, 26.844999313354492, 26.020000457763672, 26.684999465942383, 25.69499969482422, 25.655000686645508, 26.260000228881836, 26.030000686645508, 24.934999465942383, 24.475000381469727, 23.2450008392334, 20.875, 22.725000381469727, 21.8700008392334, 23.424999237060547, 23.774999618530273, 23.80500030517578, 23.790000915527344, 22.7549991607666, 22.145000457763672, 22.450000762939453, 23.5, 23.55500030517578, 24.075000762939453, 23.80500030517578, 23.9950008392334, 23.625, 24.979999542236328, 24.71500015258789, 25.020000457763672, 25.690000534057617, 25.780000686645508, 26.424999237060547, 26.329999923706055, 26.25, 26.165000915527344, 25.239999771118164, 24.44499969482422, 25.06999969482422, 24.260000228881836, 23.725000381469727, 23.75, 23.875, 23.625, 24.270000457763672, 25.114999771118164, 24.075000762939453, 23.450000762939453, 23.799999237060547, 24.334999084472656, 23.360000610351562, 23.34000015258789, 23.854999542236328, 23.299999237060547, 22.415000915527344, 23.200000762939453, 23.55500030517578, 23.020000457763672, 22.434999465942383, 22.15999984741211, 23.049999237060547, 22.649999618530273, 22.69499969482422, 21.905000686645508, 22.375, 22.114999771118164, 22.0, 23.68000030517578, 24.125, 25.6299991607666, 25.19499969482422, 26.139999389648438, 26.299999237060547, 25.985000610351562, 25.71500015258789, 25.71500015258789, 26.68000030517578, 25.625, 26.700000762939453, 26.075000762939453, 26.100000381469727, 26.594999313354492, 26.21500015258789, 28.375, 27.889999389648438, 28.4950008392334, 28.100000381469727, 28.0, 27.53499984741211, 27.049999237060547, 27.1200008392334, 27.9950008392334, 28.299999237060547, 28.475000381469727, 27.774999618530273, 27.5049991607666, 28.489999771118164, 28.725000381469727, 29.030000686645508, 28.81999969482422, 28.799999237060547, 29.21500015258789, 29.325000762939453, 28.639999389648438, 28.065000534057617, 28.475000381469727, 27.434999465942383, 27.5, 26.780000686645508, 26.924999237060547, 27.5, 26.84000015258789, 26.5, 27.209999084472656, 26.920000076293945, 26.625, 26.7450008392334, 26.524999618530273, 26.770000457763672, 27.014999389648438, 26.639999389648438, 26.5049991607666, 26.3700008392334, 26.149999618530273, 26.795000076293945, 27.010000228881836, 27.459999084472656, 27.684999465942383, 27.360000610351562, 27.549999237060547, 28.260000228881836, 28.165000915527344, 28.5, 28.15999984741211, 26.469999313354492, 25.934999465942383, 25.795000076293945, 25.975000381469727, 26.014999389648438, 24.65999984741211, 24.844999313354492, 24.364999771118164, 25.079999923706055, 23.725000381469727, 23.96500015258789, 23.899999618530273, 23.915000915527344, 23.43000030517578, 23.940000534057617, 23.399999618530273, 23.649999618530273, 23.274999618530273, 23.204999923706055, 23.625, 24.6200008392334, 24.81999969482422, 24.770000457763672, 24.290000915527344, 24.440000534057617, 23.540000915527344, 24.06999969482422, 23.899999618530273, 23.739999771118164, 24.020000457763672, 23.579999923706055, 23.06999969482422, 23.170000076293945, 22.950000762939453, 23.309999465942383, 23.059999465942383, 22.809999465942383, 23.719999313354492, 24.68000030517578, 24.520000457763672, 25.889999389648438, 25.979999542236328, 26.020000457763672, 26.75, 25.549999237060547, 25.600000381469727, 25.459999084472656, 24.940000534057617, 24.670000076293945, 24.25, 24.459999084472656, 25.100000381469727, 25.989999771118164, 25.760000228881836, 26.229999542236328, 25.309999465942383, 25.610000610351562, 24.709999084472656, 24.889999389648438, 24.270000457763672, 24.68000030517578, 25.600000381469727, 24.770000457763672, 25.610000610351562, 25.059999465942383, 25.75, 25.479999542236328, 25.329999923706055, 25.389999389648438, 25.950000762939453, 25.729999542236328, 25.540000915527344, 25.649999618530273, 26.25, 25.860000610351562, 26.110000610351562, 25.75, 25.899999618530273, 26.149999618530273, 26.040000915527344, 26.079999923706055, 25.850000381469727, 25.8799991607666, 25.399999618530273, 24.860000610351562, 24.6299991607666, 24.200000762939453, 24.200000762939453, 24.25, 24.780000686645508, 24.469999313354492, 24.729999542236328, 24.979999542236328, 24.75, 24.989999771118164, 24.469999313354492, 24.440000534057617, 23.719999313354492, 23.940000534057617, 24.670000076293945, 24.969999313354492, 25.200000762939453, 24.799999237060547, 25.639999389648438, 25.979999542236328, 26.09000015258789, 26.34000015258789, 26.139999389648438, 25.649999618530273, 25.639999389648438, 25.389999389648438, 25.950000762939453, 25.940000534057617, 25.59000015258789, 26.5, 26.690000534057617, 27.020000457763672, 27.260000228881836, 27.559999465942383, 27.25, 26.950000762939453, 27.6299991607666, 27.469999313354492, 27.559999465942383, 27.139999389648438, 27.110000610351562, 26.8700008392334, 26.280000686645508, 26.420000076293945, 26.780000686645508, 26.280000686645508, 26.940000534057617, 26.8799991607666, 26.459999084472656, 26.600000381469727, 26.329999923706055, 26.149999618530273, 26.309999465942383, 25.540000915527344, 25.719999313354492, 25.8799991607666, 25.610000610351562, 25.709999084472656, 25.790000915527344, 25.65999984741211, 25.610000610351562, 25.559999465942383, 25.850000381469727, 26.299999237060547, 26.649999618530273, 26.780000686645508, 26.309999465942383, 26.309999465942383, 26.510000228881836, 26.5, 26.459999084472656, 26.700000762939453, 27.420000076293945, 28.100000381469727, 28.229999542236328, 28.389999389648438, 28.649999618530273, 28.030000686645508, 27.65999984741211, 27.479999542236328, 28.3700008392334, 28.40999984741211, 28.760000228881836, 28.489999771118164, 29.760000228881836, 29.389999389648438, 29.1200008392334, 29.610000610351562, 28.469999313354492, 28.270000457763672, 28.40999984741211, 28.59000015258789, 28.030000686645508, 28.450000762939453, 29.15999984741211, 29.149999618530273, 29.010000228881836, 29.360000610351562, 29.219999313354492, 28.90999984741211, 28.979999542236328, 28.65999984741211, 29.200000762939453, 28.90999984741211, 29.280000686645508, 28.950000762939453, 29.350000381469727, 29.030000686645508, 28.719999313354492, 27.270000457763672, 26.90999984741211, 27.09000015258789, 27.15999984741211, 27.010000228881836, 26.3700008392334, 26.350000381469727, 26.59000015258789, 26.149999618530273, 26.260000228881836, 26.3799991607666, 26.1200008392334, 26.010000228881836, 25.850000381469727, 25.860000610351562, 25.700000762939453, 25.389999389648438, 25.329999923706055, 25.290000915527344, 25.170000076293945, 25.329999923706055, 25.329999923706055, 25.8700008392334, 25.610000610351562, 25.5, 25.899999618530273, 25.950000762939453, 25.81999969482422, 25.719999313354492, 25.959999084472656, 26.1200008392334, 26.440000534057617, 26.450000762939453, 26.59000015258789, 26.690000534057617, 27.049999237060547, 26.829999923706055, 27.040000915527344, 27.100000381469727, 27.489999771118164, 27.15999984741211, 27.170000076293945, 27.139999389648438, 27.049999237060547, 27.209999084472656, 27.40999984741211, 27.420000076293945, 27.579999923706055, 27.729999542236328, 28.190000534057617, 28.170000076293945, 28.389999389648438, 28.030000686645508, 27.670000076293945, 27.549999237060547, 27.520000457763672, 27.549999237060547, 27.709999084472656, 27.979999542236328, 28.1299991607666, 28.360000610351562, 28.280000686645508, 28.489999771118164, 28.639999389648438, 28.299999237060547, 27.809999465942383, 27.84000015258789, 27.610000610351562, 27.399999618530273, 27.219999313354492, 27.059999465942383, 27.030000686645508, 27.190000534057617, 26.8700008392334, 26.969999313354492, 27.09000015258789, 26.979999542236328, 26.719999313354492, 26.899999618530273, 26.920000076293945, 26.65999984741211, 26.729999542236328, 26.610000610351562, 26.899999618530273, 26.59000015258789, 26.469999313354492, 26.6299991607666, 26.610000610351562, 26.350000381469727, 26.329999923706055, 26.229999542236328, 26.309999465942383, 25.799999237060547, 25.649999618530273, 25.18000030517578, 25.3799991607666, 25.299999237060547, 25.260000228881836, 25.25, 24.959999084472656, 24.770000457763672, 24.479999542236328, 24.649999618530273, 24.3799991607666, 24.600000381469727, 25.110000610351562, 25.25, 25.209999084472656, 25.200000762939453, 24.950000762939453, 25.479999542236328, 25.809999465942383, 25.770000457763672, 25.739999771118164, 25.81999969482422, 25.479999542236328, 25.68000030517578, 25.389999389648438, 25.530000686645508, 25.329999923706055, 25.079999923706055, 25.649999618530273, 25.360000610351562, 25.510000228881836, 27.399999618530273, 27.450000762939453, 27.15999984741211, 27.010000228881836, 26.510000228881836, 26.59000015258789, 26.190000534057617, 26.350000381469727, 26.31999969482422, 26.15999984741211, 26.030000686645508, 25.6299991607666, 26.09000015258789, 25.8700008392334, 25.81999969482422, 26.0, 25.469999313354492, 25.700000762939453, 26.030000686645508, 25.75, 25.969999313354492, 26.049999237060547, 25.709999084472656, 25.989999771118164, 26.15999984741211, 26.139999389648438, 26.1299991607666, 26.1200008392334, 26.049999237060547, 26.030000686645508, 26.020000457763672, 26.280000686645508, 26.399999618530273, 26.3799991607666, 26.549999237060547, 26.989999771118164, 27.34000015258789, 27.309999465942383, 27.770000457763672, 28.219999313354492, 28.149999618530273, 28.200000762939453, 28.479999542236328, 28.479999542236328, 28.600000381469727, 28.18000030517578, 28.56999969482422, 28.700000762939453, 28.6200008392334, 28.31999969482422, 27.670000076293945, 27.8799991607666, 27.780000686645508, 27.670000076293945, 27.90999984741211, 27.399999618530273, 28.049999237060547, 28.18000030517578, 27.6200008392334, 28.0, 29.889999389648438, 29.06999969482422, 28.3799991607666, 28.360000610351562, 28.700000762939453, 28.34000015258789, 28.780000686645508, 28.450000762939453, 28.270000457763672, 28.3799991607666, 28.010000228881836, 28.15999984741211, 27.3799991607666, 27.260000228881836, 27.299999237060547, 27.389999389648438, 27.229999542236328, 27.010000228881836, 27.030000686645508, 27.219999313354492, 26.93000030517578, 27.350000381469727, 27.1299991607666, 27.270000457763672, 27.399999618530273, 27.209999084472656, 27.459999084472656, 27.5, 27.299999237060547, 27.290000915527344, 27.229999542236328, 27.399999618530273, 27.459999084472656, 27.290000915527344, 27.299999237060547, 27.299999237060547, 27.34000015258789, 27.530000686645508, 27.3700008392334, 27.360000610351562, 27.219999313354492, 27.389999389648438, 27.440000534057617, 27.450000762939453, 27.280000686645508, 27.190000534057617, 27.389999389648438, 27.170000076293945, 27.209999084472656, 27.260000228881836, 27.59000015258789, 27.81999969482422, 28.440000534057617, 28.149999618530273, 28.389999389648438, 28.540000915527344, 28.100000381469727, 28.200000762939453, 27.829999923706055, 28.190000534057617, 28.040000915527344, 27.969999313354492, 28.06999969482422, 28.530000686645508, 28.219999313354492, 28.809999465942383, 28.299999237060547, 27.670000076293945, 27.709999084472656, 27.860000610351562, 28.110000610351562, 28.1200008392334, 28.15999984741211, 28.260000228881836, 28.649999618530273, 28.3799991607666, 29.209999084472656, 29.18000030517578, 29.43000030517578, 29.920000076293945, 29.889999389648438, 30.15999984741211, 27.34000015258789, 27.329999923706055, 27.25, 27.1299991607666, 27.030000686645508, 26.75, 26.520000457763672, 26.6200008392334, 26.559999465942383, 26.639999389648438, 26.75, 26.950000762939453, 27.270000457763672, 27.15999984741211, 27.100000381469727, 27.260000228881836, 27.010000228881836, 27.1299991607666, 27.079999923706055, 27.100000381469727, 27.049999237060547, 27.219999313354492, 27.149999618530273, 27.0, 27.010000228881836, 27.0, 26.84000015258789, 26.8700008392334, 27.010000228881836, 26.850000381469727, 26.850000381469727, 26.889999389648438, 26.75, 26.799999237060547, 26.8700008392334, 26.84000015258789, 26.850000381469727, 26.81999969482422, 26.600000381469727, 26.690000534057617, 26.770000457763672, 26.68000030517578, 26.399999618530273, 26.030000686645508, 26.209999084472656, 25.84000015258789, 25.950000762939453, 25.760000228881836, 25.760000228881836, 26.06999969482422, 25.950000762939453, 26.540000915527344, 26.350000381469727, 26.25, 26.420000076293945, 26.3700008392334, 26.170000076293945, 26.270000457763672, 26.190000534057617, 26.25, 26.100000381469727, 26.030000686645508, 25.93000030517578, 26.0, 25.8700008392334, 25.709999084472656, 25.639999389648438, 25.25, 25.239999771118164, 25.18000030517578, 25.329999923706055, 25.219999313354492, 25.190000534057617, 25.190000534057617, 25.299999237060547, 25.209999084472656, 25.170000076293945, 25.399999618530273, 25.389999389648438, 25.43000030517578, 25.450000762939453, 25.079999923706055, 25.100000381469727, 24.81999969482422, 24.639999389648438, 24.530000686645508, 24.350000381469727, 24.190000534057617, 23.989999771118164, 24.239999771118164, 24.399999618530273, 24.139999389648438, 24.040000915527344, 24.25, 24.239999771118164, 24.110000610351562, 24.219999313354492, 24.469999313354492, 24.65999984741211, 25.06999969482422, 25.030000686645508, 24.920000076293945, 25.229999542236328, 25.010000228881836, 24.579999923706055, 24.450000762939453, 24.709999084472656, 24.65999984741211, 24.479999542236328, 25.049999237060547, 25.06999969482422, 24.950000762939453, 24.65999984741211, 24.81999969482422, 24.8799991607666, 25.229999542236328, 25.1299991607666, 25.34000015258789, 25.200000762939453, 25.329999923706055, 25.229999542236328, 25.040000915527344, 24.889999389648438, 24.84000015258789, 25.030000686645508, 25.229999542236328, 25.309999465942383, 25.5, 25.75, 25.8799991607666, 25.739999771118164, 25.799999237060547, 25.68000030517578, 25.75, 25.829999923706055, 25.989999771118164, 25.729999542236328, 25.709999084472656, 25.700000762939453, 25.3799991607666, 25.329999923706055, 25.549999237060547, 25.399999618530273, 25.489999771118164, 25.360000610351562, 25.309999465942383, 25.399999618530273, 25.219999313354492, 25.270000457763672, 24.979999542236328, 25.079999923706055, 25.110000610351562, 25.170000076293945, 25.219999313354492, 25.06999969482422, 25.09000015258789, 25.219999313354492, 25.059999465942383, 24.850000381469727, 24.65999984741211, 24.969999313354492, 24.579999923706055, 24.639999389648438, 25.149999618530273, 25.239999771118164, 25.530000686645508, 25.790000915527344, 26.040000915527344, 25.709999084472656, 25.790000915527344, 26.0, 26.299999237060547, 25.989999771118164, 25.690000534057617, 25.719999313354492, 25.610000610351562, 25.75, 25.780000686645508, 25.809999465942383, 25.899999618530273, 26.760000228881836, 27.15999984741211, 27.290000915527344, 27.799999237060547, 27.219999313354492, 27.40999984741211, 26.979999542236328, 27.079999923706055, 26.979999542236328, 27.030000686645508, 26.81999969482422, 26.889999389648438, 26.850000381469727, 26.790000915527344, 26.84000015258789, 26.84000015258789, 26.899999618530273, 27.059999465942383, 26.809999465942383, 27.059999465942383, 27.170000076293945, 27.3799991607666, 27.209999084472656, 27.059999465942383, 26.940000534057617, 26.799999237060547, 26.6200008392334, 26.6200008392334, 26.540000915527344, 26.520000457763672, 26.3700008392334, 26.34000015258789, 26.09000015258789, 26.06999969482422, 25.799999237060547, 25.489999771118164, 25.309999465942383, 25.399999618530273, 25.3700008392334, 25.389999389648438, 25.610000610351562, 25.90999984741211, 25.709999084472656, 25.360000610351562, 25.040000915527344, 24.65999984741211, 24.770000457763672, 24.670000076293945, 24.510000228881836, 24.489999771118164, 24.309999465942383, 24.709999084472656, 24.68000030517578, 24.489999771118164, 24.559999465942383, 25.049999237060547, 24.90999984741211, 24.889999389648438, 24.950000762939453, 24.969999313354492, 25.219999313354492, 25.100000381469727, 25.610000610351562, 25.610000610351562, 25.93000030517578, 26.600000381469727, 26.530000686645508, 26.719999313354492, 26.940000534057617, 26.979999542236328, 26.940000534057617, 27.149999618530273, 27.360000610351562, 27.329999923706055, 27.479999542236328, 27.850000381469727, 28.1200008392334, 28.06999969482422, 28.059999465942383, 27.920000076293945, 27.799999237060547, 27.790000915527344, 27.790000915527344, 27.68000030517578, 27.729999542236328, 27.81999969482422, 27.93000030517578, 27.899999618530273, 27.670000076293945, 27.709999084472656, 27.709999084472656, 27.700000762939453, 27.290000915527344, 27.0, 27.079999923706055, 26.8799991607666, 26.81999969482422, 26.760000228881836, 26.8700008392334, 26.709999084472656, 26.520000457763672, 26.68000030517578, 26.510000228881836, 26.40999984741211, 26.149999618530273, 26.25, 26.770000457763672, 26.959999084472656, 26.889999389648438, 26.93000030517578, 26.649999618530273, 27.010000228881836, 27.25, 27.030000686645508, 26.899999618530273, 26.739999771118164, 26.8700008392334, 27.010000228881836, 26.40999984741211, 26.34000015258789, 26.40999984741211, 26.559999465942383, 27.229999542236328, 27.81999969482422, 27.90999984741211, 27.959999084472656, 27.969999313354492, 27.479999542236328, 27.510000228881836, 26.950000762939453, 27.010000228881836, 26.959999084472656, 26.6200008392334, 26.6299991607666, 26.40999984741211, 26.600000381469727, 26.850000381469727, 26.670000076293945, 26.719999313354492, 26.530000686645508, 26.729999542236328, 26.59000015258789, 26.75, 26.950000762939453, 26.979999542236328, 27.020000457763672, 26.809999465942383, 26.920000076293945, 26.899999618530273, 26.989999771118164, 27.270000457763672, 27.059999465942383, 27.18000030517578, 27.040000915527344, 27.200000762939453, 27.34000015258789, 27.350000381469727, 27.700000762939453, 27.739999771118164, 27.079999923706055, 27.079999923706055, 26.709999084472656, 27.010000228881836, 27.010000228881836, 26.950000762939453, 27.030000686645508, 27.299999237060547, 27.670000076293945, 27.600000381469727, 27.8799991607666, 27.65999984741211, 27.610000610351562, 27.229999542236328, 27.290000915527344, 27.100000381469727, 27.079999923706055, 27.030000686645508, 26.940000534057617, 27.110000610351562, 27.049999237060547, 27.049999237060547, 27.06999969482422, 27.09000015258789, 27.079999923706055, 26.969999313354492, 24.229999542236328, 24.31999969482422, 24.489999771118164, 23.989999771118164, 23.350000381469727, 23.65999984741211, 23.850000381469727, 23.75, 23.670000076293945, 23.709999084472656, 23.139999389648438, 23.100000381469727, 23.15999984741211, 22.889999389648438, 22.84000015258789, 22.790000915527344, 22.479999542236328, 23.110000610351562, 22.989999771118164, 23.56999969482422, 23.770000457763672, 23.549999237060547, 23.260000228881836, 22.739999771118164, 22.8700008392334, 22.719999313354492, 22.549999237060547, 22.149999618530273, 22.030000686645508, 22.149999618530273, 21.959999084472656, 21.729999542236328, 21.59000015258789, 22.010000228881836, 21.969999313354492, 22.139999389648438, 22.540000915527344, 22.610000610351562, 23.059999465942383, 22.850000381469727, 22.649999618530273, 22.889999389648438, 22.959999084472656, 23.31999969482422, 23.540000915527344, 23.530000686645508, 23.479999542236328, 23.450000762939453, 23.389999389648438, 23.43000030517578, 23.3700008392334, 22.790000915527344, 22.3700008392334, 22.280000686645508, 22.290000915527344, 22.59000015258789, 22.81999969482422, 23.440000534057617, 24.079999923706055, 24.010000228881836, 24.0, 24.1200008392334, 24.579999923706055, 24.079999923706055, 24.06999969482422, 24.020000457763672, 24.1200008392334, 24.190000534057617, 24.399999618530273, 24.280000686645508, 24.389999389648438, 24.489999771118164, 24.3700008392334, 24.43000030517578, 24.520000457763672, 24.549999237060547, 24.610000610351562, 24.700000762939453, 25.049999237060547, 25.65999984741211, 26.010000228881836, 25.649999618530273, 25.81999969482422, 25.709999084472656, 25.84000015258789, 25.920000076293945, 25.850000381469727, 25.8700008392334, 25.889999389648438, 25.690000534057617, 25.510000228881836, 25.479999542236328, 25.530000686645508, 25.43000030517578, 25.899999618530273, 25.81999969482422, 25.989999771118164, 26.579999923706055, 26.739999771118164, 26.739999771118164, 27.010000228881836, 27.239999771118164, 26.829999923706055, 26.809999465942383, 26.90999984741211, 27.18000030517578, 27.469999313354492, 27.350000381469727, 27.31999969482422, 27.3700008392334, 27.389999389648438, 27.920000076293945, 27.760000228881836, 27.799999237060547, 27.690000534057617, 27.459999084472656, 27.579999923706055, 28.34000015258789, 28.479999542236328, 28.239999771118164, 28.5, 28.350000381469727, 28.479999542236328, 28.299999237060547, 28.43000030517578, 28.280000686645508, 28.329999923706055, 28.489999771118164, 28.350000381469727, 28.65999984741211, 28.780000686645508, 28.709999084472656, 28.850000381469727, 28.770000457763672, 28.860000610351562, 28.780000686645508, 29.110000610351562, 29.170000076293945, 29.190000534057617, 29.280000686645508, 29.1299991607666, 29.139999389648438, 29.309999465942383, 29.520000457763672, 29.90999984741211, 29.969999313354492, 29.65999984741211, 29.690000534057617, 29.34000015258789, 29.440000534057617, 29.420000076293945, 29.229999542236328, 29.229999542236328, 29.360000610351562, 29.100000381469727, 28.959999084472656, 28.81999969482422, 29.190000534057617, 29.559999465942383, 29.600000381469727, 29.540000915527344, 30.139999389648438, 30.190000534057617, 29.709999084472656, 29.989999771118164, 30.1299991607666, 29.829999923706055, 29.530000686645508, 29.989999771118164, 29.860000610351562, 29.860000610351562, 29.90999984741211, 29.700000762939453, 29.6299991607666, 29.649999618530273, 30.0, 29.799999237060547, 29.760000228881836, 30.649999618530273, 31.260000228881836, 31.260000228881836, 31.149999618530273, 30.729999542236328, 31.059999465942383, 30.6299991607666, 30.780000686645508, 31.079999923706055, 31.219999313354492, 30.649999618530273, 30.56999969482422, 30.40999984741211, 30.84000015258789, 30.81999969482422, 29.969999313354492, 29.59000015258789, 29.639999389648438, 29.239999771118164, 29.350000381469727, 28.889999389648438, 29.040000915527344, 29.170000076293945, 29.579999923706055, 28.90999984741211, 28.6299991607666, 28.75, 29.309999465942383, 29.219999313354492, 28.959999084472656, 28.709999084472656, 27.950000762939453, 27.81999969482422, 28.020000457763672, 27.489999771118164, 27.799999237060547, 27.760000228881836, 27.719999313354492, 27.420000076293945, 27.18000030517578, 27.25, 26.81999969482422, 27.31999969482422, 27.350000381469727, 27.34000015258789, 27.93000030517578, 27.899999618530273, 28.520000457763672, 28.219999313354492, 27.940000534057617, 28.040000915527344, 27.579999923706055, 27.84000015258789, 27.75, 27.889999389648438, 27.860000610351562, 28.010000228881836, 28.31999969482422, 28.579999923706055, 28.5, 28.299999237060547, 28.059999465942383, 28.43000030517578, 28.600000381469727, 28.6299991607666, 28.610000610351562, 28.34000015258789, 28.979999542236328, 28.959999084472656, 28.790000915527344, 28.860000610351562, 29.09000015258789, 30.170000076293945, 30.1299991607666, 29.940000534057617, 30.389999389648438, 30.600000381469727, 30.68000030517578, 30.520000457763672, 30.68000030517578, 30.700000762939453, 30.68000030517578, 30.56999969482422, 30.84000015258789, 30.899999618530273, 31.0, 31.030000686645508, 30.969999313354492, 30.729999542236328, 30.899999618530273, 30.84000015258789, 30.540000915527344, 30.280000686645508, 30.489999771118164, 30.549999237060547, 31.1200008392334, 30.790000915527344, 30.420000076293945, 30.6200008392334, 30.3700008392334, 30.020000457763672, 29.579999923706055, 29.940000534057617, 29.959999084472656, 29.969999313354492, 30.350000381469727, 30.860000610351562, 30.690000534057617, 30.479999542236328, 30.440000534057617, 29.979999542236328, 30.0, 29.469999313354492, 29.549999237060547, 29.360000610351562, 29.860000610351562, 29.8700008392334, 29.670000076293945, 29.790000915527344, 30.049999237060547, 29.90999984741211, 29.860000610351562, 29.700000762939453, 29.239999771118164, 29.559999465942383, 29.940000534057617, 29.760000228881836, 30.020000457763672, 30.510000228881836, 31.049999237060547, 31.149999618530273, 31.360000610351562, 31.010000228881836, 30.989999771118164, 30.239999771118164, 29.93000030517578, 29.40999984741211, 29.709999084472656, 28.950000762939453, 29.190000534057617, 29.450000762939453, 29.049999237060547, 29.329999923706055, 29.719999313354492, 29.639999389648438, 28.899999618530273, 28.940000534057617, 28.770000457763672, 28.239999771118164, 27.8799991607666, 28.09000015258789, 28.18000030517578, 28.100000381469727, 28.270000457763672, 28.280000686645508, 28.209999084472656, 28.610000610351562, 28.299999237060547, 28.1299991607666, 28.420000076293945, 28.700000762939453, 28.5, 28.649999618530273, 28.559999465942383, 28.6200008392334, 28.670000076293945, 28.6299991607666, 28.809999465942383, 29.1200008392334, 28.979999542236328, 28.790000915527344, 28.700000762939453, 28.8700008392334, 28.479999542236328, 28.690000534057617, 28.809999465942383, 29.139999389648438, 29.68000030517578, 29.700000762939453, 29.489999771118164, 29.459999084472656, 29.700000762939453, 29.709999084472656, 29.559999465942383, 29.889999389648438, 29.65999984741211, 30.030000686645508, 30.040000915527344, 30.299999237060547, 30.030000686645508, 30.100000381469727, 30.239999771118164, 30.75, 31.219999313354492, 31.09000015258789, 30.1200008392334, 30.709999084472656, 30.850000381469727, 31.56999969482422, 36.0099983215332, 34.849998474121094, 34.369998931884766, 35.52000045776367, 36.529998779296875, 37.220001220703125, 36.7599983215332, 36.59000015258789, 36.040000915527344, 35.599998474121094, 34.18000030517578, 33.31999969482422, 33.540000915527344, 34.619998931884766, 33.7599983215332, 33.86000061035156, 33.959999084472656, 34.22999954223633, 34.400001525878906, 34.36000061035156, 34.09000015258789, 33.27000045776367, 33.380001068115234, 33.58000183105469, 33.91999816894531, 33.5, 32.7400016784668, 33.13999938964844, 34.2599983215332, 34.61000061035156, 34.63999938964844, 34.72999954223633, 34.61000061035156, 34.47999954223633, 35.04999923706055, 35.029998779296875, 34.63999938964844, 34.689998626708984, 35.290000915527344, 35.900001525878906, 36.130001068115234, 36.40999984741211, 36.349998474121094, 36.099998474121094, 35.900001525878906, 35.790000915527344, 35.220001220703125, 35.189998626708984, 34.54999923706055, 34.709999084472656, 33.36000061035156, 34.349998474121094, 34.13999938964844, 34.459999084472656, 34.029998779296875, 33.41999816894531, 33.540000915527344, 33.15999984741211, 31.540000915527344, 31.479999542236328, 32.349998474121094, 34.900001525878906, 33.02000045776367, 32.849998474121094, 32.560001373291016, 31.90999984741211, 31.059999465942383, 30.489999771118164, 29.90999984741211, 29.280000686645508, 28.34000015258789, 28.290000915527344, 28.520000457763672, 28.43000030517578, 28.6200008392334, 28.8799991607666, 28.309999465942383, 28.799999237060547, 28.149999618530273, 28.6200008392334, 28.239999771118164, 27.649999618530273, 27.739999771118164, 28.190000534057617, 28.020000457763672, 27.690000534057617, 27.239999771118164, 27.020000457763672, 27.75, 28.059999465942383, 27.34000015258789, 27.829999923706055, 28.399999618530273, 29.43000030517578, 28.540000915527344, 28.719999313354492, 27.299999237060547, 28.670000076293945, 29.3799991607666, 28.739999771118164, 29.329999923706055, 29.329999923706055, 29.030000686645508, 28.479999542236328, 28.229999542236328, 27.8799991607666, 28.829999923706055, 29.56999969482422, 29.0, 29.1299991607666, 29.549999237060547, 28.940000534057617, 28.719999313354492, 28.829999923706055, 28.8700008392334, 28.239999771118164, 28.1200008392334, 28.56999969482422, 29.1200008392334, 30.010000228881836, 30.190000534057617, 30.639999389648438, 30.450000762939453, 31.6299991607666, 30.049999237060547, 29.799999237060547, 28.799999237060547, 28.739999771118164, 28.5, 29.59000015258789, 29.93000030517578, 29.0, 29.690000534057617, 29.280000686645508, 29.209999084472656, 29.399999618530273, 30.0, 29.889999389648438, 29.979999542236328, 30.469999313354492, 29.8700008392334, 29.299999237060547, 28.809999465942383, 28.280000686645508, 28.260000228881836, 28.110000610351562, 28.559999465942383, 28.209999084472656, 28.3799991607666, 28.239999771118164, 27.90999984741211, 27.280000686645508, 27.719999313354492, 27.989999771118164, 27.649999618530273, 27.360000610351562, 27.850000381469727, 27.399999618530273, 28.6299991607666, 29.020000457763672, 29.040000915527344, 28.549999237060547, 28.549999237060547, 28.899999618530273, 28.299999237060547, 27.920000076293945, 27.809999465942383, 28.030000686645508, 27.68000030517578, 27.729999542236328, 27.270000457763672, 26.899999618530273, 25.969999313354492, 26.110000610351562, 25.93000030517578, 25.790000915527344, 25.200000762939453, 25.15999984741211, 25.479999542236328, 24.93000030517578, 26.1299991607666, 27.579999923706055, 26.360000610351562, 25.65999984741211, 25.540000915527344, 25.860000610351562, 26.09000015258789, 25.639999389648438, 26.059999465942383, 25.520000457763672, 26.200000762939453, 25.850000381469727, 25.920000076293945, 25.389999389648438, 25.639999389648438, 26.729999542236328, 26.899999618530273, 27.350000381469727, 27.860000610351562, 27.780000686645508, 28.06999969482422, 27.81999969482422, 27.979999542236328, 27.780000686645508, 27.540000915527344, 27.540000915527344, 27.100000381469727, 27.229999542236328, 27.610000610351562, 27.579999923706055, 27.34000015258789, 27.610000610351562, 27.68000030517578, 27.670000076293945, 27.0, 26.739999771118164, 26.030000686645508, 26.209999084472656, 26.200000762939453, 26.520000457763672, 26.100000381469727, 27.139999389648438, 26.920000076293945, 26.09000015258789, 25.729999542236328, 24.799999237060547, 26.3700008392334, 26.219999313354492, 25.65999984741211, 25.579999923706055, 25.81999969482422, 26.170000076293945, 26.940000534057617, 25.770000457763672, 26.3799991607666, 26.18000030517578, 26.3700008392334, 25.6299991607666, 24.979999542236328, 22.899999618530273, 23.770000457763672, 21.790000915527344, 22.860000610351562, 25.639999389648438, 23.780000686645508, 22.940000534057617, 23.56999969482422, 24.200000762939453, 24.299999237060547, 23.040000915527344, 21.549999237060547, 21.059999465942383, 21.670000076293945, 21.639999389648438, 23.1299991607666, 23.690000534057617, 22.530000686645508, 22.479999542236328, 23.1299991607666, 23.329999923706055, 21.8700008392334, 21.31999969482422, 21.850000381469727, 21.290000915527344, 20.889999389648438, 20.149999618530273, 20.559999465942383, 19.739999771118164, 19.5, 19.6299991607666, 18.1200008392334, 18.020000457763672, 19.889999389648438, 20.860000610351562, 19.729999542236328, 20.229999542236328, 19.8799991607666, 18.989999771118164, 18.649999618530273, 19.399999618530273, 18.90999984741211, 20.350000381469727, 20.6200008392334, 20.81999969482422, 20.110000610351562, 19.149999618530273, 19.34000015258789, 19.209999084472656, 19.809999465942383, 19.860000610351562, 19.420000076293945, 19.239999771118164, 19.280000686645508, 19.260000228881836, 19.200000762939453, 19.149999618530273, 19.010000228881836, 19.309999465942383, 19.530000686645508, 20.200000762939453, 20.75, 20.190000534057617, 19.6299991607666, 20.170000076293945, 19.709999084472656, 19.520000457763672, 19.530000686645508, 19.06999969482422, 19.6299991607666, 19.459999084472656, 18.8700008392334, 18.049999237060547, 16.969999313354492, 17.290000915527344, 17.780000686645508, 17.799999237060547, 17.780000686645508, 17.739999771118164, 17.030000686645508, 17.850000381469727, 18.540000915527344, 18.510000228881836, 19.15999984741211, 19.639999389648438, 19.25, 18.940000534057617, 18.969999313354492, 19.270000457763672, 18.489999771118164, 18.219999313354492, 18.299999237060547, 17.770000457763672, 18.020000457763672, 17.030000686645508, 17.010000228881836, 17.049999237060547, 16.290000915527344, 15.960000038146973, 16.030000686645508, 16.1200008392334, 15.859999656677246, 15.350000381469727, 15.199999809265137, 15.369999885559082, 16.6299991607666, 17.010000228881836, 16.979999542236328, 16.81999969482422, 16.31999969482422, 17.030000686645508, 17.3700008392334, 17.31999969482422, 17.3700008392334, 18.040000915527344, 17.979999542236328, 18.170000076293945, 18.540000915527344, 17.739999771118164, 17.829999923706055, 18.229999542236328, 19.5, 19.1299991607666, 18.540000915527344, 18.760000228881836, 18.969999313354492, 19.450000762939453, 19.690000534057617, 19.440000534057617, 19.209999084472656, 19.15999984741211, 19.690000534057617, 18.889999389648438, 18.59000015258789, 18.760000228881836, 18.920000076293945, 19.81999969482422, 20.549999237060547, 20.25, 20.110000610351562, 20.600000381469727, 20.190000534057617, 20.3700008392334, 20.139999389648438, 20.059999465942383, 19.959999084472656, 19.459999084472656, 19.200000762939453, 19.510000228881836, 19.920000076293945, 19.829999923706055, 20.1299991607666, 20.360000610351562, 20.510000228881836, 20.40999984741211, 20.139999389648438, 19.93000030517578, 19.540000915527344, 20.25, 20.31999969482422, 20.559999465942383, 21.0, 21.360000610351562, 21.309999465942383, 21.770000457763672, 21.959999084472656, 21.979999542236328, 22.059999465942383, 22.170000076293945, 22.59000015258789, 22.899999618530273, 23.229999542236328, 23.469999313354492, 23.5, 23.6200008392334, 24.040000915527344, 23.950000762939453, 23.3799991607666, 23.450000762939453, 23.43000030517578, 23.56999969482422, 23.600000381469727, 23.969999313354492, 24.049999237060547, 23.760000228881836, 23.209999084472656, 23.079999923706055, 22.309999465942383, 22.649999618530273, 22.190000534057617, 22.420000076293945, 23.200000762939453, 23.75, 23.93000030517578, 24.399999618530273, 24.440000534057617, 24.690000534057617, 24.700000762939453, 24.93000030517578, 23.610000610351562, 23.440000534057617, 22.989999771118164, 23.729999542236328, 24.200000762939453, 23.770000457763672, 23.81999969482422, 23.68000030517578, 23.84000015258789, 23.93000030517578, 23.75, 23.459999084472656, 23.31999969482422, 23.1299991607666, 23.6299991607666, 23.6200008392334, 23.31999969482422, 23.290000915527344, 23.25, 23.600000381469727, 23.93000030517578, 24.40999984741211, 24.600000381469727, 24.59000015258789, 24.40999984741211, 25.06999969482422, 24.56999969482422, 24.350000381469727, 23.81999969482422, 23.90999984741211, 24.09000015258789, 24.6200008392334, 24.739999771118164, 24.799999237060547, 24.93000030517578, 24.649999618530273, 24.969999313354492, 25.25, 25.059999465942383, 25.459999084472656, 25.110000610351562, 25.399999618530273, 25.920000076293945, 25.920000076293945, 25.690000534057617, 25.600000381469727, 25.90999984741211, 25.760000228881836, 25.40999984741211, 24.459999084472656, 24.979999542236328, 24.68000030517578, 24.989999771118164, 25.440000534057617, 25.56999969482422, 25.65999984741211, 25.59000015258789, 26.139999389648438, 25.899999618530273, 26.450000762939453, 26.489999771118164, 26.420000076293945, 26.459999084472656, 26.559999465942383, 29.200000762939453, 28.1299991607666, 28.729999542236328, 28.239999771118164, 28.06999969482422, 28.030000686645508, 27.700000762939453, 27.639999389648438, 27.690000534057617, 28.520000457763672, 28.3799991607666, 28.6200008392334, 28.899999618530273, 29.079999923706055, 29.010000228881836, 29.469999313354492, 29.610000610351562, 29.5, 30.0, 29.989999771118164, 29.65999984741211, 29.84000015258789, 29.950000762939453, 29.809999465942383, 29.110000610351562, 29.149999618530273, 29.520000457763672, 29.899999618530273, 29.84000015258789, 30.049999237060547, 29.780000686645508, 29.520000457763672, 29.469999313354492, 29.709999084472656, 29.969999313354492, 29.90999984741211, 29.889999389648438, 30.06999969482422, 29.950000762939453, 29.84000015258789, 30.399999618530273, 30.600000381469727, 30.709999084472656, 30.8799991607666, 31.0, 31.350000381469727, 31.149999618530273, 30.979999542236328, 30.6200008392334, 30.850000381469727, 30.8799991607666, 30.6299991607666, 30.280000686645508, 30.709999084472656, 30.149999618530273, 30.260000228881836, 30.309999465942383, 31.079999923706055, 30.75, 30.809999465942383, 30.610000610351562, 30.0, 29.239999771118164, 29.200000762939453, 29.350000381469727, 29.84000015258789, 29.899999618530273, 28.389999389648438, 28.3700008392334, 28.260000228881836, 28.3799991607666, 28.0, 28.010000228881836, 27.969999313354492, 28.030000686645508, 27.93000030517578, 27.809999465942383, 28.1299991607666, 28.530000686645508, 28.59000015258789, 28.790000915527344, 28.84000015258789, 28.68000030517578, 28.520000457763672, 28.270000457763672, 28.649999618530273, 28.770000457763672, 29.079999923706055, 28.510000228881836, 28.459999084472656, 28.65999984741211, 28.520000457763672, 28.559999465942383, 28.860000610351562, 28.889999389648438, 29.31999969482422, 29.18000030517578, 29.420000076293945, 29.5, 29.6299991607666, 29.760000228881836, 29.5, 29.59000015258789, 29.719999313354492, 29.829999923706055, 30.09000015258789, 29.709999084472656, 29.6299991607666, 29.639999389648438, 29.350000381469727, 29.1299991607666, 29.149999618530273, 29.15999984741211, 29.31999969482422, 29.950000762939453, 30.25, 30.149999618530273, 30.790000915527344, 30.81999969482422, 30.790000915527344, 30.770000457763672, 31.219999313354492, 31.329999923706055, 31.040000915527344, 31.1200008392334, 31.0, 30.950000762939453, 30.920000076293945, 30.93000030517578, 31.06999969482422, 30.670000076293945, 30.520000457763672, 29.770000457763672, 29.59000015258789, 28.93000030517578, 29.010000228881836, 28.68000030517578, 28.979999542236328, 29.260000228881836, 29.200000762939453, 29.1200008392334, 28.8700008392334, 28.520000457763672, 27.649999618530273, 26.6299991607666, 26.850000381469727, 25.649999618530273, 26.229999542236328, 25.729999542236328, 25.84000015258789, 25.530000686645508, 26.059999465942383, 26.549999237060547, 26.100000381469727, 25.81999969482422, 25.25, 25.219999313354492, 25.1299991607666, 25.040000915527344, 25.860000610351562, 25.75, 26.469999313354492, 26.559999465942383, 26.3700008392334, 26.780000686645508, 26.15999984741211, 25.780000686645508, 25.459999084472656, 25.049999237060547, 24.510000228881836, 24.1299991607666, 23.299999237060547, 23.09000015258789, 23.360000610351562, 23.700000762939453, 23.81999969482422, 24.600000381469727, 24.329999923706055, 24.43000030517578, 25.139999389648438, 25.5, 25.5, 25.510000228881836, 24.959999084472656, 24.860000610351562, 25.600000381469727, 25.510000228881836, 25.84000015258789, 25.860000610351562, 26.139999389648438, 26.06999969482422, 26.1299991607666, 25.75, 25.989999771118164, 26.200000762939453, 26.149999618530273, 25.489999771118164, 25.18000030517578, 25.549999237060547, 25.329999923706055, 24.68000030517578, 24.420000076293945, 24.350000381469727, 24.360000610351562, 24.709999084472656, 24.68000030517578, 24.6200008392334, 24.309999465942383, 24.440000534057617, 24.09000015258789, 24.0, 24.09000015258789, 23.8799991607666, 23.739999771118164, 23.600000381469727, 23.670000076293945, 23.8799991607666, 24.239999771118164, 24.100000381469727, 24.06999969482422, 24.190000534057617, 23.979999542236328, 24.200000762939453, 25.040000915527344, 25.100000381469727, 25.059999465942383, 25.399999618530273, 25.280000686645508, 25.420000076293945, 24.889999389648438, 24.510000228881836, 24.639999389648438, 24.850000381469727, 24.799999237060547, 24.6299991607666, 24.610000610351562, 24.770000457763672, 23.959999084472656, 24.059999465942383, 24.31999969482422, 24.6200008392334, 24.6200008392334, 24.739999771118164, 24.649999618530273, 25.020000457763672, 25.290000915527344, 25.360000610351562, 25.59000015258789, 25.270000457763672, 25.260000228881836, 25.399999618530273, 25.520000457763672, 25.239999771118164, 25.1200008392334, 25.790000915527344, 26.209999084472656, 27.149999618530273, 26.8799991607666, 27.059999465942383, 27.459999084472656, 27.40999984741211, 27.170000076293945, 26.68000030517578, 26.809999465942383, 27.010000228881836, 26.68000030517578, 26.469999313354492, 26.329999923706055, 26.040000915527344, 25.899999618530273, 25.709999084472656, 25.799999237060547, 25.649999618530273, 25.56999969482422, 25.200000762939453, 25.209999084472656, 25.190000534057617, 25.049999237060547, 25.56999969482422, 26.239999771118164, 26.809999465942383, 26.93000030517578, 27.079999923706055, 26.829999923706055, 27.280000686645508, 27.190000534057617, 27.270000457763672, 27.309999465942383, 27.530000686645508, 27.760000228881836, 27.920000076293945, 27.950000762939453, 27.850000381469727, 28.010000228881836, 27.969999313354492, 28.1200008392334, 27.969999313354492, 27.940000534057617, 27.920000076293945, 27.799999237060547, 28.049999237060547, 27.940000534057617, 27.899999618530273, 28.040000915527344, 28.639999389648438, 28.260000228881836, 28.200000762939453, 28.1200008392334, 28.329999923706055, 28.079999923706055, 28.15999984741211, 28.459999084472656, 28.5, 28.399999618530273, 28.020000457763672, 28.139999389648438, 28.510000228881836, 28.75, 28.899999618530273, 27.770000457763672, 27.799999237060547, 27.93000030517578, 27.969999313354492, 27.700000762939453, 27.799999237060547, 28.100000381469727, 28.190000534057617, 27.93000030517578, 27.760000228881836, 27.209999084472656, 27.040000915527344, 27.049999237060547, 26.969999313354492, 27.1299991607666, 26.780000686645508, 26.530000686645508, 26.639999389648438, 26.90999984741211, 26.690000534057617, 26.600000381469727, 26.110000610351562, 26.260000228881836, 26.219999313354492, 26.1299991607666, 25.770000457763672, 25.809999465942383, 25.6200008392334, 25.40999984741211, 25.489999771118164, 25.079999923706055, 25.219999313354492, 25.059999465942383, 25.059999465942383, 25.18000030517578, 25.299999237060547, 25.229999542236328, 25.600000381469727, 25.93000030517578, 25.65999984741211, 25.34000015258789, 25.600000381469727, 25.600000381469727, 25.530000686645508, 25.450000762939453, 25.81999969482422, 25.979999542236328, 26.190000534057617, 26.170000076293945, 26.190000534057617, 25.829999923706055, 25.649999618530273, 25.420000076293945, 25.459999084472656, 25.100000381469727, 25.0, 25.540000915527344, 25.790000915527344, 25.559999465942383, 25.739999771118164, 26.299999237060547, 26.459999084472656, 26.549999237060547, 25.940000534057617, 25.600000381469727, 25.850000381469727, 26.049999237060547, 26.06999969482422, 25.799999237060547, 25.3799991607666, 25.649999618530273, 25.350000381469727, 25.280000686645508, 24.959999084472656, 24.399999618530273, 24.530000686645508, 24.850000381469727, 24.719999313354492, 24.209999084472656, 24.200000762939453, 24.170000076293945, 24.350000381469727, 24.68000030517578, 24.959999084472656, 24.989999771118164, 24.489999771118164, 24.049999237060547, 23.889999389648438, 24.09000015258789, 23.899999618530273, 24.010000228881836, 24.020000457763672, 23.790000915527344, 24.299999237060547, 24.0, 23.75, 24.219999313354492, 24.170000076293945, 24.520000457763672, 24.600000381469727, 24.440000534057617, 24.510000228881836, 24.229999542236328, 25.299999237060547, 25.709999084472656, 25.739999771118164, 25.93000030517578, 26.100000381469727, 25.969999313354492, 26.489999771118164, 26.540000915527344, 26.6200008392334, 26.549999237060547, 26.600000381469727, 26.6200008392334, 26.469999313354492, 26.6299991607666, 26.809999465942383, 27.280000686645508, 27.040000915527344, 26.860000610351562, 27.260000228881836, 27.81999969482422, 27.8799991607666, 27.290000915527344, 27.520000457763672, 27.510000228881836, 26.979999542236328, 26.829999923706055, 26.530000686645508, 25.969999313354492, 25.020000457763672, 24.709999084472656, 24.950000762939453, 24.5, 25.1299991607666, 25.239999771118164, 25.219999313354492, 25.25, 24.56999969482422, 24.40999984741211, 24.420000076293945, 24.030000686645508, 24.649999618530273, 25.079999923706055, 24.510000228881836, 25.530000686645508, 25.729999542236328, 26.290000915527344, 26.459999084472656, 25.780000686645508, 25.200000762939453, 25.690000534057617, 26.0, 26.0, 25.440000534057617, 25.920000076293945, 26.170000076293945, 26.729999542236328, 27.049999237060547, 26.799999237060547, 27.309999465942383, 27.049999237060547, 25.299999237060547, 24.899999618530273, 25.190000534057617, 25.65999984741211, 25.93000030517578, 25.979999542236328, 25.200000762939453, 24.719999313354492, 24.299999237060547, 25.420000076293945, 25.899999618530273, 26.34000015258789, 26.579999923706055, 26.860000610351562, 27.18000030517578, 26.760000228881836, 27.309999465942383, 27.110000610351562, 26.940000534057617, 27.3700008392334, 27.260000228881836, 27.149999618530273, 27.059999465942383, 27.079999923706055, 27.030000686645508, 27.1299991607666, 27.139999389648438, 26.760000228881836, 26.190000534057617, 26.100000381469727, 26.239999771118164, 26.3799991607666, 26.209999084472656, 27.010000228881836, 26.59000015258789, 26.469999313354492, 26.579999923706055, 26.8799991607666, 26.559999465942383, 26.469999313354492, 26.010000228881836, 25.479999542236328, 25.239999771118164, 24.889999389648438, 24.610000610351562, 24.3799991607666, 24.940000534057617, 24.81999969482422, 25.3700008392334, 25.559999465942383, 25.59000015258789, 25.780000686645508, 25.809999465942383, 25.670000076293945, 25.479999542236328, 25.520000457763672, 25.40999984741211, 25.75, 25.719999313354492, 25.719999313354492, 25.670000076293945, 26.020000457763672, 25.860000610351562, 26.010000228881836, 25.81999969482422, 25.90999984741211, 25.959999084472656, 26.110000610351562, 25.950000762939453, 26.0, 26.549999237060547, 26.81999969482422, 27.3799991607666, 27.530000686645508, 28.049999237060547, 27.93000030517578, 27.43000030517578, 27.8700008392334, 27.93000030517578, 28.399999618530273, 28.309999465942383, 28.15999984741211, 28.81999969482422, 29.549999237060547, 29.469999313354492, 29.06999969482422, 29.610000610351562, 29.450000762939453, 28.969999313354492, 29.65999984741211, 29.790000915527344, 29.899999618530273, 30.139999389648438, 30.040000915527344, 30.149999618530273, 30.260000228881836, 30.68000030517578, 30.639999389648438, 30.6299991607666, 30.329999923706055, 30.329999923706055, 30.309999465942383, 31.200000762939453, 31.18000030517578, 31.450000762939453, 31.200000762939453, 31.479999542236328, 31.239999771118164, 31.40999984741211, 31.889999389648438, 31.93000030517578, 32.310001373291016, 32.0099983215332, 31.540000915527344, 31.670000076293945, 32.040000915527344, 32.099998474121094, 31.969999313354492, 32.2400016784668, 32.529998779296875, 32.790000915527344, 32.90999984741211, 32.540000915527344, 32.099998474121094, 31.959999084472656, 31.809999465942383, 32.099998474121094, 32.189998626708984, 32.650001525878906, 32.52000045776367, 32.060001373291016, 32.400001525878906, 32.220001220703125, 32.15999984741211, 31.65999984741211, 31.149999618530273, 31.219999313354492, 31.059999465942383, 30.43000030517578, 30.479999542236328, 30.889999389648438, 30.989999771118164, 31.270000457763672, 31.280000686645508, 31.1299991607666, 32.150001525878906, 32.310001373291016, 32.209999084472656, 31.920000076293945, 32.119998931884766, 32.119998931884766, 31.979999542236328, 32.04999923706055, 31.850000381469727, 31.8799991607666, 31.450000762939453, 30.700000762939453, 30.479999542236328, 30.190000534057617, 30.860000610351562, 30.690000534057617, 30.81999969482422, 30.639999389648438, 30.309999465942383, 29.989999771118164, 29.790000915527344, 29.100000381469727, 29.690000534057617, 29.350000381469727, 29.15999984741211, 29.200000762939453, 29.3799991607666, 29.350000381469727, 29.299999237060547, 28.760000228881836, 28.6200008392334, 28.510000228881836, 28.8799991607666, 29.639999389648438, 29.209999084472656, 29.729999542236328, 29.100000381469727, 29.219999313354492, 29.329999923706055, 29.59000015258789, 29.989999771118164, 30.190000534057617, 30.93000030517578, 30.959999084472656, 30.299999237060547, 30.299999237060547, 30.0, 30.190000534057617, 29.979999542236328, 30.450000762939453, 30.6200008392334, 30.229999542236328, 30.59000015258789, 30.610000610351562, 30.1200008392334, 30.079999923706055, 29.709999084472656, 29.149999618530273, 28.760000228881836, 29.479999542236328, 29.639999389648438, 29.600000381469727, 30.510000228881836, 31.0, 29.56999969482422, 29.239999771118164, 29.239999771118164, 29.229999542236328, 29.479999542236328, 29.75, 29.479999542236328, 29.59000015258789, 29.209999084472656, 29.530000686645508, 30.0, 30.06999969482422, 30.209999084472656, 30.389999389648438, 30.5, 30.350000381469727, 30.299999237060547, 30.110000610351562, 30.360000610351562, 30.920000076293945, 30.81999969482422, 30.760000228881836, 30.59000015258789, 30.389999389648438, 30.25, 30.93000030517578, 30.700000762939453, 30.649999618530273, 30.530000686645508, 30.600000381469727, 30.450000762939453, 30.219999313354492, 30.5, 31.040000915527344, 30.829999923706055, 30.690000534057617, 30.940000534057617, 30.889999389648438, 31.010000228881836, 31.190000534057617, 31.100000381469727, 31.09000015258789, 30.950000762939453, 31.43000030517578, 31.0, 30.950000762939453, 30.280000686645508, 30.170000076293945, 30.18000030517578, 29.809999465942383, 29.68000030517578, 29.75, 29.969999313354492, 30.229999542236328, 29.639999389648438, 29.68000030517578, 29.149999618530273, 29.219999313354492, 28.969999313354492, 29.3700008392334, 29.450000762939453, 29.299999237060547, 29.649999618530273, 29.049999237060547, 28.729999542236328, 27.770000457763672, 28.15999984741211, 28.190000534057617, 27.860000610351562, 28.549999237060547, 28.84000015258789, 29.59000015258789, 29.6200008392334, 29.81999969482422, 29.530000686645508, 29.1200008392334, 28.8799991607666, 28.940000534057617, 27.020000457763672, 27.239999771118164, 26.8799991607666, 26.670000076293945, 26.799999237060547, 26.760000228881836, 26.709999084472656, 27.229999542236328, 27.540000915527344, 27.360000610351562, 27.010000228881836, 27.110000610351562, 27.049999237060547, 26.780000686645508, 26.5, 26.3799991607666, 26.809999465942383, 26.81999969482422, 26.559999465942383, 27.049999237060547, 27.530000686645508, 27.31999969482422, 27.110000610351562, 26.790000915527344, 27.25, 27.690000534057617, 27.360000610351562, 27.450000762939453, 27.200000762939453, 27.030000686645508, 26.889999389648438, 26.709999084472656, 26.59000015258789, 27.25, 27.6299991607666, 27.270000457763672, 26.770000457763672, 26.75, 26.719999313354492, 26.649999618530273, 26.489999771118164, 26.899999618530273, 26.829999923706055, 27.149999618530273, 27.190000534057617, 27.100000381469727, 27.299999237060547, 27.200000762939453, 27.700000762939453, 27.579999923706055, 28.010000228881836, 27.81999969482422, 28.010000228881836, 27.790000915527344, 27.670000076293945, 27.8700008392334, 27.6200008392334, 27.3799991607666, 27.350000381469727, 27.350000381469727, 27.649999618530273, 27.8799991607666, 27.93000030517578, 27.920000076293945, 28.040000915527344, 27.8799991607666, 28.1299991607666, 27.739999771118164, 27.68000030517578, 27.969999313354492, 27.3799991607666, 27.420000076293945, 27.8799991607666, 27.719999313354492, 27.850000381469727, 28.290000915527344, 28.209999084472656, 28.110000610351562, 28.25, 27.940000534057617, 27.84000015258789, 27.8700008392334, 28.0, 28.030000686645508, 27.8799991607666, 28.1200008392334, 28.34000015258789, 28.110000610351562, 28.190000534057617, 28.299999237060547, 28.239999771118164, 28.139999389648438, 28.31999969482422, 28.639999389648438, 28.59000015258789, 28.75, 28.389999389648438, 28.219999313354492, 28.729999542236328, 28.729999542236328, 29.56999969482422, 29.100000381469727, 28.850000381469727, 28.649999618530273, 28.899999618530273, 28.850000381469727, 28.950000762939453, 29.6200008392334, 30.299999237060547, 30.700000762939453, 30.6200008392334, 31.709999084472656, 31.899999618530273, 31.799999237060547, 32.560001373291016, 32.93000030517578, 32.630001068115234, 33.22999954223633, 33.41999816894531, 33.650001525878906, 33.06999969482422, 32.849998474121094, 32.66999816894531, 32.61000061035156, 32.86000061035156, 33.45000076293945, 33.63999938964844, 34.130001068115234, 34.72999954223633, 35.099998474121094, 34.790000915527344, 34.22999954223633, 33.91999816894531, 34.41999816894531, 34.7400016784668, 34.849998474121094, 34.81999969482422, 34.91999816894531, 35.619998931884766, 34.599998474121094, 34.84000015258789, 35.25, 35.5099983215332, 35.04999923706055, 35.13999938964844, 34.9900016784668, 34.54999923706055, 34.689998626708984, 34.970001220703125, 34.959999084472656, 34.2599983215332, 33.65999984741211, 32.939998626708984, 34.08000183105469, 34.119998931884766, 34.52000045776367, 34.380001068115234, 34.75, 34.40999984741211, 33.65999984741211, 34.09000015258789, 34.349998474121094, 34.58000183105469, 34.34000015258789, 35.0, 35.58000183105469, 35.65999984741211, 36.0099983215332, 36.34000015258789, 35.720001220703125, 32.400001525878906, 31.700000762939453, 31.90999984741211, 32.040000915527344, 31.6200008392334, 31.260000228881836, 31.469999313354492, 31.780000686645508, 31.969999313354492, 32.060001373291016, 31.690000534057617, 31.899999618530273, 31.549999237060547, 31.540000915527344, 32.2400016784668, 32.77000045776367, 32.459999084472656, 32.5099983215332, 32.13999938964844, 32.0, 31.790000915527344, 31.760000228881836, 31.440000534057617, 31.610000610351562, 32.189998626708984, 35.16999816894531, 34.400001525878906, 33.52000045776367, 33.38999938964844, 32.93000030517578, 33.369998931884766, 31.75, 31.389999389648438, 31.100000381469727, 31.309999465942383, 31.219999313354492, 31.899999618530273, 32.56999969482422, 32.720001220703125, 32.77000045776367, 33.380001068115234, 33.41999816894531, 32.9900016784668, 33.47999954223633, 33.40999984741211, 32.540000915527344, 32.869998931884766, 32.4900016784668, 32.63999938964844, 32.880001068115234, 33.0, 33.349998474121094, 33.36000061035156, 33.880001068115234, 33.689998626708984, 33.599998474121094, 33.310001373291016, 33.06999969482422, 33.310001373291016, 33.68000030517578, 33.900001525878906, 34.66999816894531, 34.599998474121094, 34.45000076293945, 34.81999969482422, 34.97999954223633, 35.02000045776367, 34.349998474121094, 33.81999969482422, 35.880001068115234, 35.61000061035156, 35.630001068115234, 35.529998779296875, 35.65999984741211, 35.66999816894531, 35.59000015258789, 35.790000915527344, 37.2400016784668, 37.959999084472656, 37.66999816894531, 37.689998626708984, 37.380001068115234, 36.97999954223633, 37.869998931884766, 37.95000076293945, 37.349998474121094, 36.849998474121094, 36.91999816894531, 37.27000045776367, 37.529998779296875, 37.93000030517578, 37.56999969482422, 37.56999969482422, 37.81999969482422, 38.09000015258789, 38.13999938964844, 38.209999084472656, 38.849998474121094, 38.41999816894531, 38.560001373291016, 38.61000061035156, 38.060001373291016, 37.63999938964844, 37.41999816894531, 36.72999954223633, 36.939998626708984, 36.36000061035156, 36.5099983215332, 36.20000076293945, 36.810001373291016, 36.720001220703125, 37.20000076293945, 37.58000183105469, 37.220001220703125, 37.400001525878906, 37.349998474121094, 37.20000076293945, 36.849998474121094, 36.33000183105469, 36.0, 35.880001068115234, 35.900001525878906, 35.9900016784668, 34.72999954223633, 35.900001525878906, 36.689998626708984, 36.83000183105469, 36.81999969482422, 36.2599983215332, 36.09000015258789, 37.45000076293945, 36.869998931884766, 36.119998931884766, 35.97999954223633, 36.790000915527344, 36.95000076293945, 37.7400016784668, 36.970001220703125, 36.290000915527344, 35.79999923706055, 36.31999969482422, 36.630001068115234, 36.880001068115234, 37.349998474121094, 37.33000183105469, 37.38999938964844, 37.630001068115234, 37.220001220703125, 37.56999969482422, 37.939998626708984, 37.689998626708984, 37.61000061035156, 37.58000183105469, 37.45000076293945, 37.97999954223633, 37.91999816894531, 38.20000076293945, 38.25, 38.13999938964844, 38.279998779296875, 37.9900016784668, 37.869998931884766, 37.79999923706055, 38.41999816894531, 37.650001525878906, 37.900001525878906, 38.2599983215332, 39.470001220703125, 39.25, 40.720001220703125, 40.34000015258789, 40.65999984741211, 40.47999954223633, 39.7400016784668, 39.790000915527344, 40.43000030517578, 41.150001525878906, 41.439998626708984, 41.290000915527344, 41.25, 39.959999084472656, 39.75, 39.93000030517578, 40.439998626708984, 39.0, 39.11000061035156, 39.34000015258789, 40.060001373291016, 40.0099983215332, 40.130001068115234, 39.959999084472656, 39.9900016784668, 39.7400016784668, 40.290000915527344, 40.13999938964844, 41.099998474121094, 40.400001525878906, 40.2400016784668, 40.310001373291016, 39.52000045776367, 39.290000915527344, 39.220001220703125, 39.34000015258789, 39.540000915527344, 39.7400016784668, 39.91999816894531, 40.29999923706055, 40.09000015258789, 39.66999816894531, 39.61000061035156, 39.68000030517578, 39.79999923706055, 40.290000915527344, 40.369998931884766, 40.2599983215332, 40.13999938964844, 40.150001525878906, 40.45000076293945, 40.95000076293945, 40.599998474121094, 40.209999084472656, 40.59000015258789, 41.47999954223633, 41.38999938964844, 41.029998779296875, 40.93000030517578, 40.810001373291016, 41.099998474121094, 41.040000915527344, 41.290000915527344, 41.61000061035156, 41.56999969482422, 41.45000076293945, 41.72999954223633, 41.83000183105469, 41.70000076293945, 41.93000030517578, 41.61000061035156, 42.16999816894531, 41.86000061035156, 41.72999954223633, 41.90999984741211, 41.75, 41.869998931884766, 41.97999954223633, 41.369998931884766, 41.70000076293945, 42.220001220703125, 42.33000183105469, 42.5099983215332, 45.45000076293945, 44.650001525878906, 44.560001373291016, 45.0, 45.45000076293945, 44.93000030517578, 44.29999923706055, 44.36000061035156, 43.90999984741211, 44.06999969482422, 43.380001068115234, 43.209999084472656, 42.970001220703125, 43.310001373291016, 42.7400016784668, 42.84000015258789, 43.22999954223633, 43.2599983215332, 43.040000915527344, 43.68000030517578, 44.08000183105469, 44.58000183105469, 44.939998626708984, 44.970001220703125, 45.34000015258789, 44.84000015258789, 45.349998474121094, 45.400001525878906, 45.310001373291016, 44.900001525878906, 44.75, 45.09000015258789, 45.43000030517578, 44.529998779296875, 44.7400016784668, 45.11000061035156, 46.02000045776367, 46.470001220703125, 46.81999969482422, 46.7400016784668, 46.90999984741211, 46.540000915527344, 46.38999938964844, 46.2599983215332, 46.59000015258789, 46.810001373291016, 47.29999923706055, 46.849998474121094, 46.630001068115234, 46.880001068115234, 45.93000030517578, 45.97999954223633, 46.369998931884766, 46.27000045776367, 45.83000183105469, 45.97999954223633, 46.119998931884766, 45.86000061035156, 45.47999954223633, 46.5, 45.599998474121094, 43.81999969482422, 43.869998931884766, 43.0, 42.529998779296875, 43.20000076293945, 43.060001373291016, 44.36000061035156, 45.0, 44.619998931884766, 46.83000183105469, 45.709999084472656, 45.86000061035156, 46.439998626708984, 46.31999969482422, 46.939998626708984, 46.88999938964844, 47.29999923706055, 47.79999923706055, 47.86000061035156, 48.91999816894531, 48.650001525878906, 48.849998474121094, 48.560001373291016, 48.810001373291016, 49.7400016784668, 49.40999984741211, 49.130001068115234, 48.65999984741211, 48.0, 49.02000045776367, 47.9900016784668, 47.65999984741211, 47.4900016784668, 47.95000076293945, 47.880001068115234, 48.84000015258789, 48.439998626708984, 48.38999938964844, 48.81999969482422, 48.2599983215332, 47.11000061035156, 47.58000183105469, 47.08000183105469, 46.779998779296875, 47.20000076293945, 45.900001525878906, 45.04999923706055, 46.58000183105469, 47.630001068115234, 47.779998779296875, 48.369998931884766, 48.63999938964844, 48.40999984741211, 47.70000076293945, 47.439998626708984, 46.72999954223633, 46.65999984741211, 46.369998931884766, 46.380001068115234, 45.97999954223633, 46.75, 47.61000061035156, 47.41999816894531, 46.970001220703125, 45.959999084472656, 46.220001220703125, 45.310001373291016, 46.29999923706055, 45.939998626708984, 46.380001068115234, 47.36000061035156, 47.0, 42.95000076293945, 42.7400016784668, 40.93000030517578, 41.54999923706055, 40.59000015258789, 41.630001068115234, 41.939998626708984, 42.220001220703125, 42.68000030517578, 42.2400016784668, 42.7400016784668, 42.650001525878906, 42.65999984741211, 43.380001068115234, 43.970001220703125, 43.630001068115234, 43.18000030517578, 43.5099983215332, 43.70000076293945, 44.150001525878906, 43.95000076293945, 43.9900016784668, 44.130001068115234, 43.66999816894531, 43.560001373291016, 43.0099983215332, 43.06999969482422, 43.0, 42.189998626708984, 42.349998474121094, 42.310001373291016, 41.33000183105469, 40.70000076293945, 41.470001220703125, 41.369998931884766, 41.43000030517578, 42.2599983215332, 42.560001373291016, 42.880001068115234, 42.779998779296875, 42.91999816894531, 41.220001220703125, 41.119998931884766, 41.099998474121094, 40.779998779296875, 40.599998474121094, 40.65999984741211, 40.34000015258789, 41.61000061035156, 41.459999084472656, 41.25, 41.630001068115234, 41.400001525878906, 41.79999923706055, 41.7599983215332, 41.95000076293945, 41.66999816894531, 41.72999954223633, 43.0, 42.66999816894531, 42.88999938964844, 45.65999984741211, 47.22999954223633, 47.779998779296875, 48.720001220703125, 48.70000076293945, 48.58000183105469, 48.369998931884766, 47.81999969482422, 47.56999969482422, 46.27000045776367, 47.54999923706055, 47.54999923706055, 46.849998474121094, 48.189998626708984, 48.029998779296875, 48.869998931884766, 47.97999954223633, 47.560001373291016, 47.38999938964844, 47.279998779296875, 47.29999923706055, 46.83000183105469, 46.81999969482422, 47.5, 47.43000030517578, 47.060001373291016, 46.93000030517578, 47.369998931884766, 46.790000915527344, 46.310001373291016, 46.29999923706055, 45.7599983215332, 45.790000915527344, 46.65999984741211, 46.220001220703125, 45.45000076293945, 45.349998474121094, 45.72999954223633, 46.220001220703125, 46.790000915527344, 46.33000183105469, 46.130001068115234, 45.66999816894531, 46.029998779296875, 45.650001525878906, 45.040000915527344, 44.709999084472656, 44.459999084472656, 44.47999954223633, 43.959999084472656, 44.34000015258789, 44.439998626708984, 44.75, 45.0099983215332, 44.97999954223633, 45.45000076293945, 45.68000030517578, 46.0099983215332, 46.54999923706055, 46.650001525878906, 46.779998779296875, 45.439998626708984, 45.27000045776367, 45.90999984741211, 45.939998626708984, 45.58000183105469, 45.400001525878906, 46.2599983215332, 47.290000915527344, 46.97999954223633, 46.75, 47.97999954223633, 47.709999084472656, 46.38999938964844, 46.95000076293945, 46.81999969482422, 46.189998626708984, 47.060001373291016, 46.529998779296875, 46.810001373291016, 46.84000015258789, 46.779998779296875, 46.06999969482422, 45.29999923706055, 40.45000076293945, 42.56999969482422, 42.0099983215332, 43.22999954223633, 43.400001525878906, 43.560001373291016, 42.16999816894531, 42.36000061035156, 43.40999984741211, 42.810001373291016, 43.29999923706055, 44.209999084472656, 43.119998931884766, 43.13999938964844, 43.43000030517578, 43.189998626708984, 43.970001220703125, 44.290000915527344, 43.5, 43.619998931884766, 43.380001068115234, 43.93000030517578, 43.45000076293945, 44.47999954223633, 43.83000183105469, 43.369998931884766, 43.880001068115234, 44.75, 44.27000045776367, 45.75, 46.33000183105469, 47.099998474121094, 46.560001373291016, 47.45000076293945, 46.97999954223633, 46.560001373291016, 46.650001525878906, 47.0099983215332, 47.02000045776367, 47.41999816894531, 47.439998626708984, 47.91999816894531, 47.529998779296875, 52.29999923706055, 52.529998779296875, 53.9900016784668, 53.540000915527344, 53.540000915527344, 53.31999969482422, 52.849998474121094, 52.93000030517578, 54.18000030517578, 54.4900016784668, 54.09000015258789, 54.54999923706055, 54.06999969482422, 53.70000076293945, 53.47999954223633, 53.06999969482422, 53.08000183105469, 53.16999816894531, 53.0, 53.9900016784668, 54.25, 54.25, 53.91999816894531, 54.09000015258789, 53.79999923706055, 54.540000915527344, 54.40999984741211, 55.31999969482422, 55.4900016784668, 54.119998931884766, 55.790000915527344, 55.470001220703125, 55.369998931884766, 55.38999938964844, 54.709999084472656, 54.33000183105469, 55.65999984741211, 55.540000915527344, 56.36000061035156, 55.77000045776367, 54.880001068115234, 54.9900016784668, 55.70000076293945, 55.86000061035156, 55.349998474121094, 56.290000915527344, 56.470001220703125, 56.040000915527344, 54.31999969482422, 54.93000030517578, 54.31999969482422, 52.70000076293945, 52.369998931884766, 52.5099983215332, 52.7599983215332, 53.79999923706055, 52.0, 51.310001373291016, 51.47999954223633, 49.97999954223633, 51.0, 51.40999984741211, 51.939998626708984, 51.790000915527344, 52.0099983215332, 51.86000061035156, 54.72999954223633, 54.880001068115234, 54.16999816894531, 53.25, 52.099998474121094, 51.939998626708984, 49.54999923706055, 49.02000045776367, 49.88999938964844, 48.68000030517578, 50.25, 50.900001525878906, 51.4900016784668, 52.33000183105469, 51.970001220703125, 52.279998779296875, 52.34000015258789, 50.689998626708984, 51.72999954223633, 52.599998474121094, 51.349998474121094, 50.970001220703125, 52.40999984741211, 52.970001220703125, 52.400001525878906, 51.560001373291016, 50.79999923706055, 51.88999938964844, 52.93000030517578, 53.0, 52.709999084472656, 52.75, 53.45000076293945, 54.209999084472656, 54.91999816894531, 53.25, 53.61000061035156, 54.11000061035156, 53.84000015258789, 54.209999084472656, 53.65999984741211, 54.93000030517578, 54.95000076293945, 55.04999923706055, 55.43000030517578, 55.189998626708984, 54.36000061035156, 54.869998931884766, 54.66999816894531, 54.4900016784668, 54.369998931884766, 55.119998931884766, 55.220001220703125, 55.29999923706055, 55.4900016784668, 56.630001068115234, 56.290000915527344, 55.79999923706055, 51.90999984741211, 51.779998779296875, 52.2599983215332, 51.47999954223633, 50.619998931884766, 49.349998474121094, 50.0, 50.34000015258789, 49.84000015258789, 49.869998931884766, 49.91999816894531, 50.4900016784668, 50.33000183105469, 51.130001068115234, 51.20000076293945, 51.439998626708984, 50.79999923706055, 51.720001220703125, 50.47999954223633, 50.470001220703125, 50.47999954223633, 50.599998474121094, 50.70000076293945, 51.91999816894531, 51.93000030517578, 51.91999816894531, 52.2599983215332, 52.439998626708984, 52.63999938964844, 52.380001068115234, 51.9900016784668, 52.2400016784668, 52.02000045776367, 52.0, 51.04999923706055, 49.58000183105469, 49.900001525878906, 49.779998779296875, 49.52000045776367, 50.40999984741211, 50.63999938964844, 50.20000076293945, 51.08000183105469, 51.279998779296875, 49.810001373291016, 49.099998474121094, 48.91999816894531, 49.90999984741211, 50.720001220703125, 51.130001068115234, 50.83000183105469, 50.779998779296875, 51.41999816894531, 51.72999954223633, 52.5, 52.939998626708984, 53.560001373291016, 53.84000015258789, 53.95000076293945, 53.70000076293945, 53.709999084472656, 56.150001525878906, 55.97999954223633, 56.08000183105469, 56.470001220703125, 56.52000045776367, 56.61000061035156, 56.0, 56.2599983215332, 56.599998474121094, 56.849998474121094, 56.68000030517578, 56.79999923706055, 57.650001525878906, 58.060001373291016, 58.16999816894531, 58.15999984741211, 58.029998779296875, 58.029998779296875, 58.0099983215332, 57.61000061035156, 57.540000915527344, 57.41999816894531, 57.43000030517578, 57.599998474121094, 57.900001525878906, 57.79999923706055, 57.880001068115234, 58.279998779296875, 58.18000030517578, 57.97999954223633, 57.650001525878906, 57.0099983215332, 57.66999816894531, 57.779998779296875, 57.470001220703125, 57.630001068115234, 56.790000915527344, 56.0, 56.5, 56.38999938964844, 56.150001525878906, 57.630001068115234, 57.27000045776367, 57.349998474121094, 57.5099983215332, 57.91999816894531, 57.869998931884766, 57.08000183105469, 56.93000030517578, 57.880001068115234, 57.810001373291016, 57.56999969482422, 57.40999984741211, 57.27000045776367, 57.290000915527344, 57.7400016784668, 57.849998474121094, 57.90999984741211, 57.88999938964844, 57.11000061035156, 56.70000076293945, 57.119998931884766, 57.36000061035156, 57.529998779296875, 57.470001220703125, 57.5, 60.279998779296875, 59.939998626708984, 60.849998474121094, 60.810001373291016, 60.61000061035156, 60.0099983215332, 60.15999984741211, 59.970001220703125, 59.81999969482422, 59.529998779296875, 58.650001525878906, 59.779998779296875, 60.54999923706055, 60.0, 60.47999954223633, 58.22999954223633, 59.02000045776367, 58.33000183105469, 58.939998626708984, 60.40999984741211, 60.779998779296875, 60.5, 60.97999954223633, 61.0099983215332, 60.29999923706055, 60.34000015258789, 60.650001525878906, 60.86000061035156, 60.11000061035156, 59.08000183105469, 59.70000076293945, 60.43000030517578, 60.0099983215332, 61.29999923706055, 61.18000030517578, 61.81999969482422, 62.5, 63.0, 62.70000076293945, 62.95000076293945, 62.560001373291016, 63.689998626708984, 63.43000030517578, 63.84000015258789, 63.45000076293945, 63.209999084472656, 63.400001525878906, 62.86000061035156, 62.959999084472656, 62.790000915527344, 62.47999954223633, 62.189998626708984, 62.29999923706055, 62.7599983215332, 62.72999954223633, 62.61000061035156, 63.060001373291016, 62.619998931884766, 62.68000030517578, 62.66999816894531, 62.2400016784668, 62.66999816894531, 62.70000076293945, 63.20000076293945, 63.95000076293945, 64.12000274658203, 65.38999938964844, 65.69000244140625, 64.86000061035156, 64.36000061035156, 63.25, 63.5, 63.5, 63.7400016784668, 63.56999969482422, 63.52000045776367, 64.25, 64.23999786376953, 64.41000366210938, 64.5, 64.73999786376953, 64.47000122070312, 64.61000061035156, 64.33000183105469, 64.41999816894531, 64.52999877929688, 64.54000091552734, 64.08000183105469, 64.12999725341797, 64.69000244140625, 63.9900016784668, 63.970001220703125, 64.19000244140625, 64.26000213623047, 65.19000244140625, 65.11000061035156, 65.01000213623047, 64.52999877929688, 64.55000305175781, 64.75, 64.91000366210938, 64.91000366210938, 65.19000244140625, 64.12000274658203, 64.94000244140625, 65.36000061035156, 64.62999725341797, 64.95999908447266, 65.12000274658203, 65.41999816894531, 65.6500015258789, 65.80999755859375, 65.38999938964844, 66.30000305175781, 65.5999984741211, 65.8499984741211, 65.61000061035156, 65.5999984741211, 65.41999816894531, 65.29000091552734, 65.04000091552734, 65.33000183105469, 65.6500015258789, 65.45999908447266, 65.66999816894531, 67.4800033569336, 67.9000015258789, 68.08000183105469, 68.1500015258789, 68.91000366210938, 68.68000030517578, 69.70999908447266, 69.37999725341797, 69.02999877929688, 68.9000015258789, 68.97000122070312, 68.86000061035156, 68.98999786376953, 68.36000061035156, 68.61000061035156, 68.13999938964844, 68.2300033569336, 68.88999938964844, 67.4000015258789, 67.5, 67.88999938964844, 68.72000122070312, 68.87000274658203, 68.97000122070312, 69.80000305175781, 69.79000091552734, 70.52999877929688, 70.23999786376953, 70.44000244140625, 71.97000122070312, 72.30000305175781, 72.63999938964844, 72.51000213623047, 72.04000091552734, 69.25, 70.0199966430664, 70.91000366210938, 69.2699966430664, 69.7300033569336, 70.5, 70.81999969482422, 70.20999908447266, 70.54000091552734, 70.08999633789062, 71.4000015258789, 70.11000061035156, 69.20999908447266, 69.37999725341797, 68.77999877929688, 69.33000183105469, 68.26000213623047, 68.2699966430664, 68.69999694824219, 69.45999908447266, 70.0, 70.69000244140625, 71.5, 72.23999786376953, 72.80000305175781, 73.08999633789062, 73.5, 74.18000030517578, 73.44999694824219, 73.52999877929688, 73.80000305175781, 74.33999633789062, 73.76000213623047, 72.66999816894531, 73.30000305175781, 73.0999984741211, 72.55000305175781, 72.19000244140625, 72.4000015258789, 72.80000305175781, 72.08999633789062, 72.25, 71.9000015258789, 71.61000061035156, 73.05999755859375, 73.58999633789062, 73.33999633789062, 73.58000183105469, 72.2699966430664, 72.47000122070312, 72.3499984741211, 72.95999908447266, 72.73999786376953, 72.86000061035156, 73.05999755859375, 72.25, 73.01000213623047, 74.02999877929688, 74.70999908447266, 73.33999633789062, 73.73999786376953, 73.68000030517578, 74.33000183105469, 74.30999755859375, 74.76000213623047, 74.93000030517578, 75.0, 74.83000183105469, 75.2300033569336, 75.20999908447266, 75.3499984741211, 75.11000061035156, 73.98999786376953, 74.08999633789062, 73.66999816894531, 73.55000305175781, 73.54000091552734, 73.94000244140625, 74.70999908447266, 74.66999816894531, 74.08999633789062, 75.22000122070312, 75.66999816894531, 75.97000122070312, 76.33000183105469, 76.36000061035156, 76.48999786376953, 77.58999633789062, 77.41999816894531, 77.47000122070312, 77.66999816894531, 77.56999969482422, 78.31999969482422, 78.98999786376953, 78.9000015258789, 78.58000183105469, 79.19999694824219, 84.37000274658203, 83.69999694824219, 84.36000061035156, 83.68000030517578, 83.3499984741211, 84.08000183105469, 84.19999694824219, 84.7699966430664, 84.13999938964844, 84.11000061035156, 83.79000091552734, 83.66000366210938, 83.5, 83.47000122070312, 83.0999984741211, 83.12000274658203, 82.4000015258789, 82.73999786376953, 83.83000183105469, 83.01000213623047, 83.30999755859375, 84.06999969482422, 84.70999908447266, 83.51000213623047, 83.5999984741211, 84.41999816894531, 81.33999633789062, 81.55000305175781, 82.54000091552734, 83.62999725341797, 84.29000091552734, 85.30999755859375, 85.73999786376953, 85.43000030517578, 85.26000213623047, 87.12000274658203, 86.3499984741211, 86.19999694824219, 86.05000305175781, 85.4000015258789, 85.30999755859375, 85.6500015258789, 85.9000015258789, 85.62999725341797, 86.12999725341797, 86.05999755859375, 86.58999633789062, 87.66000366210938, 88.19999694824219, 88.6500015258789, 87.86000061035156, 88.12999725341797, 88.66999816894531, 90.0999984741211, 89.08000183105469, 89.80000305175781, 90.13999938964844, 90.0, 91.9000015258789, 92.55000305175781, 92.47000122070312, 93.12000274658203, 95.13999938964844, 93.30000305175781, 93.75, 94.79000091552734, 93.63999938964844, 90.55999755859375, 86.88999938964844, 90.48999786376953, 89.70999908447266, 86.30000305175781, 88.73999786376953, 88.93000030517578, 88.51000213623047, 91.20999908447266, 92.44999694824219, 91.4800033569336, 92.9800033569336, 92.05000305175781, 93.5999984741211, 94.4000015258789, 95.73999786376953, 94.83999633789062, 93.98999786376953, 91.58000183105469, 92.33999633789062, 94.33999633789062, 93.16000366210938, 94.2699966430664, 95.29000091552734, 96.5, 97.0, 95.12000274658203, 93.52999877929688, 94.68000030517578, 93.73999786376953, 93.05000305175781, 92.93000030517578, 91.2699966430664, 89.5, 90.61000061035156, 94.94000244140625, 89.81999969482422, 90.18000030517578, 90.47000122070312, 89.58000183105469, 87.8499984741211, 92.44000244140625, 91.48999786376953, 91.04000091552734, 92.38999938964844, 92.01000213623047, 92.43000030517578, 94.05000305175781, 94.06999969482422, 95.0, 96.22000122070312, 96.44000244140625, 95.91000366210938, 95.73999786376953, 96.23999786376953, 93.30000305175781, 93.55000305175781, 97.5999984741211, 96.33000183105469, 93.20999908447266, 94.98999786376953, 92.95999908447266, 93.31999969482422, 95.16999816894531, 95.8499984741211, 96.01000213623047, 97.45999908447266, 97.80000305175781, 97.91999816894531, 97.23999786376953, 97.36000061035156, 96.76000213623047, 96.01000213623047, 97.0, 97.68000030517578, 96.70999908447266, 98.7300033569336, 98.30000305175781, 97.83999633789062, 98.30999755859375, 99.29000091552734, 99.27999877929688, 101.26000213623047, 102.0, 102.4800033569336, 102.6500015258789, 101.08999633789062, 101.01000213623047, 101.0999984741211, 101.72000122070312, 101.6500015258789, 101.51000213623047, 100.01000213623047, 99.6500015258789, 101.37000274658203, 102.08000183105469, 100.41000366210938, 100.0, 98.81999969482422, 99.58000183105469, 97.37999725341797, 98.93000030517578, 98.0999984741211, 100.4800033569336, 99.5, 99.88999938964844, 101.6500015258789, 102.0, 101.1500015258789, 102.7699966430664, 104.37000274658203, 105.4000015258789, 104.61000061035156, 105.94000244140625, 104.93000030517578, 108.08000183105469, 106.30000305175781, 108.56999969482422, 107.95999908447266, 110.73999786376953, 110.18000030517578 }
                },
*/
                new StockQuote1 { Timestamp = dt19700101.AddSeconds((int)743866200).ToUniversalTime() , Open =     2.375 } ,
                new StockQuote1 { Timestamp = dt19700101.AddSeconds((int)743952600).ToUniversalTime() , Open =     2.4375 } ,
                new StockQuote1 { Timestamp = dt19700101.AddSeconds((int)744039000).ToUniversalTime() , Open =     2.28125 } ,
                new StockQuote1 { Timestamp = dt19700101.AddSeconds((int)744298200).ToUniversalTime() , Open =     2.3359375 } ,
                new StockQuote1 { Timestamp = dt19700101.AddSeconds((int)744384600).ToUniversalTime() , Open =     2.28125 }
            };
            List<object> actual = new List<object>();

            using (var p = new ChoJSONReader<StockQuotes>(FileNameSample23JSON)
                )
            {
                foreach (var rec in p.ExpandToObjects<StockQuote1>().Take(5))
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);

            //using (var p = new ChoJSONReader("sample23.json")
            //    .WithField("timestamp", jsonPath: "$..timestamp[*]")
            //    .WithField("open", jsonPath: "$..indicators.quote[0].open[*]")
            //    )
            //{
            //    foreach (var rec in p.ExpandToObjects((pn, v) =>
            //    {
            //        if (pn == "timestamp")
            //            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)v).ToUniversalTime();
            //        else
            //            return v;
            //    })) //.SelectMany(r => ((IList)r.timestamps).OfType<dynamic>().Select((r1, index) => new StockQuote1 { Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(r1).ToUniversalTime(), Open = r.open[index]  })))
            //        Console.WriteLine(rec.Dump());
            //}
        }


        [Test]
        public static void JsonToXmlSoap()
        {
            string expectedXML = @"<soap:Envelope xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ns2=""http://ws.cwt.ru/"">
  <soap:Body>
    <ns2:processPayment>
      <Status>SUCCESS</Status>
      <StatusCode>000</StatusCode>
      <StatusMessage>SUCCESS</StatusMessage>
      <ns2:Payments>
        <ns2:Payment>
          <ns2:InPaymentParameters>
            <ns2:entry>
              <key>FEE_AMOUNT</key>
              <value>100</value>
            </ns2:entry>
            <ns2:entry>
              <key>SOURCE_AMOUNT</key>
              <value>80000</value>
            </ns2:entry>
            <ns2:entry>
              <key>SOURCE_ACCOUNT_NUMBER</key>
              <value>888117823</value>
            </ns2:entry>
            <ns2:entry>
              <key>DESTINATION_BANK_CB_ID</key>
              <value>BANK OF AMERICA</value>
            </ns2:entry>
            <ns2:entry>
              <key>DESCRIPTION</key>
              <value>chama</value>
            </ns2:entry>
            <ns2:entry>
              <key>merchantId</key>
              <value>1321</value>
            </ns2:entry>
            <ns2:entry>
              <key>MERCHANT_ACQUIRER_CONTRACT_ID</key>
              <value>1</value>
            </ns2:entry>
            <ns2:entry>
              <key>DESTINATION_ACCOUNT</key>
              <value>01116132194100</value>
            </ns2:entry>
            <ns2:entry>
              <key>MERCHANT_ID</key>
              <value>admin</value>
            </ns2:entry>
            <ns2:entry>
              <key>REMOTE_TRANSACTION_ID</key>
              <value>000000086814933</value>
            </ns2:entry>
            <ns2:entry>
              <key>DESTINATION_CONNECTION_ID</key>
              <value>243</value>
            </ns2:entry>
            <ns2:entry>
              <key>FINANCE_OPERATION_TYPE</key>
              <value>WITHDRAWAL</value>
            </ns2:entry>
            <ns2:entry>
              <key>ISO8583_CARD_ACCEPTOR_ID</key>
              <value>000000000105817</value>
            </ns2:entry>
            <ns2:entry>
              <key>SOURCE_CONNECTION_ID</key>
              <value>243</value>
            </ns2:entry>
            <ns2:entry>
              <key>MESSAGE_ID</key>
              <value>139096</value>
            </ns2:entry>
            <ns2:entry>
              <key>ISO8583_CARD_ACCEPTOR_TERMINAL_ID</key>
              <value>POS00002</value>
            </ns2:entry>
            <ns2:entry>
              <key>OPERATION_ID</key>
              <value>756033604</value>
            </ns2:entry>
            <ns2:entry>
              <key>OPERATION_STATUS_MESSAGE</key>
            </ns2:entry>
            <ns2:entry>
              <key>OPERATION_STATUS</key>
              <value>SUCCESS</value>
            </ns2:entry>
            <ns2:entry>
              <key>SERVICE_NAME</key>
            </ns2:entry>
            <ns2:entry>
              <key>AUTHORIZATION_PASS</key>
              <value>admin</value>
            </ns2:entry>
            <ns2:entry>
              <key>AUTHORIZATION_LOGIN</key>
              <value>admin</value>
            </ns2:entry>
            <ns2:entry>
              <key>ISO8583_APPROVAL_CODE</key>
              <value>122127</value>
            </ns2:entry>
            <ns2:entry>
              <key>SOURCE_CARD_PAN</key>
            </ns2:entry>
            <ns2:entry>
              <key>SERVICE_TYPE</key>
              <value>FINANCE</value>
            </ns2:entry>
            <ns2:entry>
              <key>TRANSACTION_ID</key>
              <value>184139327</value>
            </ns2:entry>
          </ns2:InPaymentParameters>
          <OutPaymentParameters />
          <ServiceFields />
        </ns2:Payment>
      </ns2:Payments>
    </ns2:processPayment>
  </soap:Body>
</soap:Envelope>";

            string expectedJSON = @"{
  ""soap:Envelope"": {
    ""@xmlns:soap"": ""http://schemas.xmlsoap.org/soap/envelope/"",
    ""soap:Body"": {
      ""ns2:processPayment"": {
        ""@xmlns:ns2"": ""http://ws.cwt.ru/"",
        ""Status"": ""SUCCESS"",
        ""StatusCode"": ""000"",
        ""StatusMessage"": ""SUCCESS"",
        ""Payments"": {
          ""Payment"": {
            ""InPaymentParameters"": {
              ""entry"": [
                {
                  ""key"": ""FEE_AMOUNT"",
                  ""value"": ""100""
                },
                {
                  ""key"": ""SOURCE_AMOUNT"",
                  ""value"": ""80000""
                },
                {
                  ""key"": ""SOURCE_ACCOUNT_NUMBER"",
                  ""value"": ""888117823""
                },
                {
                  ""key"": ""DESTINATION_BANK_CB_ID"",
                  ""value"": ""BANK OF AMERICA""
                },
                {
                  ""key"": ""DESCRIPTION"",
                  ""value"": ""chama""
                },
                {
                  ""key"": ""merchantId"",
                  ""value"": ""1321""
                },
                {
                  ""key"": ""MERCHANT_ACQUIRER_CONTRACT_ID"",
                  ""value"": ""1""
                },
                {
                  ""key"": ""DESTINATION_ACCOUNT"",
                  ""value"": ""01116132194100""
                },
                {
                  ""key"": ""MERCHANT_ID"",
                  ""value"": ""admin""
                },
                {
                  ""key"": ""REMOTE_TRANSACTION_ID"",
                  ""value"": ""000000086814933""
                },
                {
                  ""key"": ""DESTINATION_CONNECTION_ID"",
                  ""value"": ""243""
                },
                {
                  ""key"": ""FINANCE_OPERATION_TYPE"",
                  ""value"": ""WITHDRAWAL""
                },
                {
                  ""key"": ""ISO8583_CARD_ACCEPTOR_ID"",
                  ""value"": ""000000000105817""
                },
                {
                  ""key"": ""SOURCE_CONNECTION_ID"",
                  ""value"": ""243""
                },
                {
                  ""key"": ""MESSAGE_ID"",
                  ""value"": ""139096""
                },
                {
                  ""key"": ""ISO8583_CARD_ACCEPTOR_TERMINAL_ID"",
                  ""value"": ""POS00002""
                },
                {
                  ""key"": ""OPERATION_ID"",
                  ""value"": ""756033604""
                },
                {
                  ""key"": ""OPERATION_STATUS_MESSAGE""
                },
                {
                  ""key"": ""OPERATION_STATUS"",
                  ""value"": ""SUCCESS""
                },
                {
                  ""key"": ""SERVICE_NAME""
                },
                {
                  ""key"": ""AUTHORIZATION_PASS"",
                  ""value"": ""admin""
                },
                {
                  ""key"": ""AUTHORIZATION_LOGIN"",
                  ""value"": ""admin""
                },
                {
                  ""key"": ""ISO8583_APPROVAL_CODE"",
                  ""value"": ""122127""
                },
                {
                  ""key"": ""SOURCE_CARD_PAN""
                },
                {
                  ""key"": ""SERVICE_TYPE"",
                  ""value"": ""FINANCE""
                },
                {
                  ""key"": ""TRANSACTION_ID"",
                  ""value"": ""184139327""
                }
              ]
            },
            ""OutPaymentParameters"": null,
            ""ServiceFields"": null
          }
        }
      }
    }
  }
}";
            string actualXML = null;
            string actualJSON = null;
            using (var p = new ChoJSONReader(FileNameSample22JSON))
            {
                var e = p.First();

                actualXML = ChoXmlWriter.Serialize(e, new ChoXmlRecordConfiguration()
                    .Configure(c => c.WithXmlNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/"))
                    .Configure(c => c.WithXmlNamespace("ns2", "http://ws.cwt.ru/"))
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    );
                //Console.WriteLine(xml);

                actualJSON = ChoJSONWriter.Serialize(e, new ChoJSONRecordConfiguration()
                    .Configure(c => c.KeepNSPrefix = true)
                    .Configure(c => c.EnableXmlAttributePrefix = true)
                    .Configure(c => c.DefaultArrayHandling = false)
                    );
                //Console.WriteLine(json);
            }
            Assert.Multiple(() => { Assert.AreEqual(expectedXML, actualXML); Assert.AreEqual(expectedJSON, actualJSON); });
        }

        public class Plan
        {
            public string Source { get; set; }
            public int CodePlan { get; set; }
            public string PlanSelection { get; set; }
            public string PlanAmount { get; set; }
            public int PlanLimitCount { get; set; }
            public string PlanLimitAmount { get; set; }
            public bool Visible { get; set; }
            public int? Count { get; set; }
        }

        [Test]
        public static void JSON2XmlAndViceVersa()
        {
            string json = @"{
  ""Source"": ""WEB"",
  ""CodePlan"": 5,
  ""PlanSelection"": ""1"",
  ""PlanAmount"": ""500.01"",
  ""PlanLimitCount"": 31,
  ""PlanLimitAmount"": ""3000.01"",
  ""Visible"": false,
  ""Count"": null
}";

            var plan = ChoJSONReader.DeserializeText<Plan>(json).FirstOrDefault();
            Console.WriteLine(plan.Dump());
            var xml = ChoXmlWriter.Serialize(plan);
            Console.WriteLine(xml);
            plan = ChoXmlReader.DeserializeText<Plan>(xml).FirstOrDefault();
            Console.WriteLine(plan.Dump());
            var json1 = ChoJSONWriter.Serialize(plan);
            Console.WriteLine(json1);

            Assert.AreEqual(json, json1);
        }

        class Row
        {
            public string Foo { get; set; }
            public string Bar { get; set; }

            public override bool Equals(object obj)
            {
                var row = obj as Row;
                return row != null &&
                       Foo == row.Foo &&
                       Bar == row.Bar;
            }

            public override int GetHashCode()
            {
                var hashCode = -504981047;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Foo);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Bar);
                return hashCode;
            }
        }

        [Test]
        public static void Test2()
        {
            List<object> expected = new List<object>
            {
                new Row { Bar="Some other value", Foo = "Some value"},
                new Row { Bar="Ipsum", Foo = "Lorem"}
            };
            List<object> actual = new List<object>();

            string json = @"{
   ""root"":[
      {
         ""row"":[
            {
               ""name"":""Foo"",
               ""value"":""Some value""
            },
            {
               ""name"":""Bar"",
               ""value"":""Some other value""
            }
         ]
      },
      {
         ""row"":[
            {
               ""name"":""Foo"",
               ""value"":""Lorem""
            },
            {
               ""name"":""Bar"",
               ""value"":""Ipsum""
            }
         ]
      },
   ]
}";
            using (var p = ChoJSONReader<Row>.LoadText(json)
                .WithJSONPath("$..root")
                .WithField(x => x.Foo, customSerializer: (o) =>
                {
                    return ((JArray)((JObject)o)["row"]).Where(x => (string)x["name"] == "Foo").First()["value"];
                })
                .WithField(x => x.Bar, customSerializer: (o) =>
                {
                    return ((JArray)((JObject)o)["row"]).Where(x => (string)x["name"] == "Bar").First()["value"];
                }
                ))
            {
                foreach (var rec in p)
                    actual.Add(rec);
                //                    Console.WriteLine(rec.Dump());
            }

            CollectionAssert.AreEqual(expected, actual);
            //using (var p = ChoJSONReader.LoadText(json)
            //    .WithJSONPath("$..root")
            //    )
            //{
            //    foreach (var rec in p.Select(r => new Row
            //    {
            //        Foo = ((Array)r.Row).OfType<dynamic>().Where(x => x.name == "Foo").First().value,
            //        Bar = ((Array)r.Row).OfType<dynamic>().Where(x => x.name == "Bar").First().value,
            //    }))
            //        Console.WriteLine(rec.Dump());
            //}
        }

        public class Quotes
        {
            public double USDAED { get; set; }
            public double USDAFN { get; set; }
            public double USDALL { get; set; }
            public double USDAMD { get; set; }
            public double USDANG { get; set; }
            public double USDAOA { get; set; }
            public double USDARS { get; set; }
            public double USDAUD { get; set; }
            public double USDAWG { get; set; }
            public double USDAZN { get; set; }
            public double USDBAM { get; set; }
            public int USDBBD { get; set; }
            public double USDBDT { get; set; }
            public double USDBGN { get; set; }
            public double USDBHD { get; set; }
            public double USDBIF { get; set; }
            public int USDBMD { get; set; }
            public double USDBND { get; set; }
            public double USDBOB { get; set; }
            public double USDBRL { get; set; }
            public int USDBSD { get; set; }
            public double USDBTC { get; set; }
            public double USDBTN { get; set; }
            public double USDBWP { get; set; }
            public double USDBYN { get; set; }
            public int USDBYR { get; set; }
            public double USDBZD { get; set; }
            public double USDCAD { get; set; }
            public double USDCDF { get; set; }
            public double USDCHF { get; set; }
            public double USDCLF { get; set; }
            public double USDCLP { get; set; }
            public double USDCNY { get; set; }
            public double USDCOP { get; set; }
            public double USDCRC { get; set; }
            public int USDCUC { get; set; }
            public double USDCUP { get; set; }
            public double USDCVE { get; set; }
            public double USDCZK { get; set; }
            public double USDDJF { get; set; }
            public double USDDKK { get; set; }
            public double USDDOP { get; set; }
            public double USDDZD { get; set; }
            public double USDEGP { get; set; }
            public double USDERN { get; set; }
            public double USDETB { get; set; }
            public double USDEUR { get; set; }
            public double USDFJD { get; set; }
            public double USDFKP { get; set; }
            public double USDGBP { get; set; }
            public double USDGEL { get; set; }
            public double USDGGP { get; set; }
            public double USDGHS { get; set; }
            public double USDGIP { get; set; }
            public double USDGMD { get; set; }
            public double USDGNF { get; set; }
            public double USDGTQ { get; set; }
            public double USDGYD { get; set; }
            public double USDHKD { get; set; }
            public double USDHNL { get; set; }
            public double USDHRK { get; set; }
            public double USDHTG { get; set; }
            public double USDHUF { get; set; }
            public int USDIDR { get; set; }
            public double USDILS { get; set; }
            public double USDIMP { get; set; }
            public double USDINR { get; set; }
            public int USDIQD { get; set; }
            public double USDIRR { get; set; }
            public double USDISK { get; set; }
            public double USDJEP { get; set; }
            public double USDJMD { get; set; }
            public double USDJOD { get; set; }
            public double USDJPY { get; set; }
            public double USDKES { get; set; }
            public double USDKGS { get; set; }
            public double USDKHR { get; set; }
            public double USDKMF { get; set; }
            public double USDKPW { get; set; }
            public double USDKRW { get; set; }
            public double USDKWD { get; set; }
            public double USDKYD { get; set; }
            public double USDKZT { get; set; }
            public double USDLAK { get; set; }
            public double USDLBP { get; set; }
            public double USDLKR { get; set; }
            public double USDLRD { get; set; }
            public double USDLSL { get; set; }
            public double USDLTL { get; set; }
            public double USDLVL { get; set; }
            public double USDLYD { get; set; }
            public double USDMAD { get; set; }
            public double USDMDL { get; set; }
            public double USDMGA { get; set; }
            public double USDMKD { get; set; }
            public double USDMMK { get; set; }
            public double USDMNT { get; set; }
            public double USDMOP { get; set; }
            public double USDMRO { get; set; }
            public double USDMUR { get; set; }
            public double USDMVR { get; set; }
            public double USDMWK { get; set; }
            public double USDMXN { get; set; }
            public double USDMYR { get; set; }
            public double USDMZN { get; set; }
            public double USDNAD { get; set; }
            public double USDNGN { get; set; }
            public double USDNIO { get; set; }
            public double USDNOK { get; set; }
            public double USDNPR { get; set; }
            public double USDNZD { get; set; }
            public double USDOMR { get; set; }
            public int USDPAB { get; set; }
            public double USDPEN { get; set; }
            public double USDPGK { get; set; }
            public double USDPHP { get; set; }
            public double USDPKR { get; set; }
            public double USDPLN { get; set; }
            public double USDPYG { get; set; }
            public double USDQAR { get; set; }
            public double USDRON { get; set; }
            public double USDRSD { get; set; }
            public double USDRUB { get; set; }
            public double USDRWF { get; set; }
            public double USDSAR { get; set; }
            public double USDSBD { get; set; }
            public double USDSCR { get; set; }
            public double USDSDG { get; set; }
            public double USDSEK { get; set; }
            public double USDSGD { get; set; }
            public double USDSHP { get; set; }
            public double USDSLL { get; set; }
            public double USDSOS { get; set; }
            public double USDSRD { get; set; }
            public double USDSTD { get; set; }
            public double USDSVC { get; set; }
            public double USDSYP { get; set; }
            public double USDSZL { get; set; }
            public double USDTHB { get; set; }
            public double USDTJS { get; set; }
            public double USDTMT { get; set; }
            public double USDTND { get; set; }
            public double USDTOP { get; set; }
            public double USDTRY { get; set; }
            public double USDTTD { get; set; }
            public double USDTWD { get; set; }
            public double USDTZS { get; set; }
            public double USDUAH { get; set; }
            public double USDUGX { get; set; }
            public int USDUSD { get; set; }
            public double USDUYU { get; set; }
            public double USDUZS { get; set; }
            public double USDVEF { get; set; }
            public int USDVND { get; set; }
            public double USDVUV { get; set; }
            public double USDWST { get; set; }
            public double USDXAF { get; set; }
            public double USDXAG { get; set; }
            public double USDXAU { get; set; }
            public double USDXCD { get; set; }
            public double USDXDR { get; set; }
            public double USDXOF { get; set; }
            public double USDXPF { get; set; }
            public double USDYER { get; set; }
            public double USDZAR { get; set; }
            public double USDZMK { get; set; }
            public double USDZMW { get; set; }
            public double USDZWL { get; set; }

            public override bool Equals(object obj)
            {
                var quotes = obj as Quotes;
                return quotes != null &&
                       USDAED == quotes.USDAED &&
                       USDAFN == quotes.USDAFN &&
                       USDALL == quotes.USDALL &&
                       USDAMD == quotes.USDAMD &&
                       USDANG == quotes.USDANG &&
                       USDAOA == quotes.USDAOA &&
                       USDARS == quotes.USDARS &&
                       USDAUD == quotes.USDAUD &&
                       USDAWG == quotes.USDAWG &&
                       USDAZN == quotes.USDAZN &&
                       USDBAM == quotes.USDBAM &&
                       USDBBD == quotes.USDBBD &&
                       USDBDT == quotes.USDBDT &&
                       USDBGN == quotes.USDBGN &&
                       USDBHD == quotes.USDBHD &&
                       USDBIF == quotes.USDBIF &&
                       USDBMD == quotes.USDBMD &&
                       USDBND == quotes.USDBND &&
                       USDBOB == quotes.USDBOB &&
                       USDBRL == quotes.USDBRL &&
                       USDBSD == quotes.USDBSD &&
                       USDBTC == quotes.USDBTC &&
                       USDBTN == quotes.USDBTN &&
                       USDBWP == quotes.USDBWP &&
                       USDBYN == quotes.USDBYN &&
                       USDBYR == quotes.USDBYR &&
                       USDBZD == quotes.USDBZD &&
                       USDCAD == quotes.USDCAD &&
                       USDCDF == quotes.USDCDF &&
                       USDCHF == quotes.USDCHF &&
                       USDCLF == quotes.USDCLF &&
                       USDCLP == quotes.USDCLP &&
                       USDCNY == quotes.USDCNY &&
                       USDCOP == quotes.USDCOP &&
                       USDCRC == quotes.USDCRC &&
                       USDCUC == quotes.USDCUC &&
                       USDCUP == quotes.USDCUP &&
                       USDCVE == quotes.USDCVE &&
                       USDCZK == quotes.USDCZK &&
                       USDDJF == quotes.USDDJF &&
                       USDDKK == quotes.USDDKK &&
                       USDDOP == quotes.USDDOP &&
                       USDDZD == quotes.USDDZD &&
                       USDEGP == quotes.USDEGP &&
                       USDERN == quotes.USDERN &&
                       USDETB == quotes.USDETB &&
                       USDEUR == quotes.USDEUR &&
                       USDFJD == quotes.USDFJD &&
                       USDFKP == quotes.USDFKP &&
                       USDGBP == quotes.USDGBP &&
                       USDGEL == quotes.USDGEL &&
                       USDGGP == quotes.USDGGP &&
                       USDGHS == quotes.USDGHS &&
                       USDGIP == quotes.USDGIP &&
                       USDGMD == quotes.USDGMD &&
                       USDGNF == quotes.USDGNF &&
                       USDGTQ == quotes.USDGTQ &&
                       USDGYD == quotes.USDGYD &&
                       USDHKD == quotes.USDHKD &&
                       USDHNL == quotes.USDHNL &&
                       USDHRK == quotes.USDHRK &&
                       USDHTG == quotes.USDHTG &&
                       USDHUF == quotes.USDHUF &&
                       USDIDR == quotes.USDIDR &&
                       USDILS == quotes.USDILS &&
                       USDIMP == quotes.USDIMP &&
                       USDINR == quotes.USDINR &&
                       USDIQD == quotes.USDIQD &&
                       USDIRR == quotes.USDIRR &&
                       USDISK == quotes.USDISK &&
                       USDJEP == quotes.USDJEP &&
                       USDJMD == quotes.USDJMD &&
                       USDJOD == quotes.USDJOD &&
                       USDJPY == quotes.USDJPY &&
                       USDKES == quotes.USDKES &&
                       USDKGS == quotes.USDKGS &&
                       USDKHR == quotes.USDKHR &&
                       USDKMF == quotes.USDKMF &&
                       USDKPW == quotes.USDKPW &&
                       USDKRW == quotes.USDKRW &&
                       USDKWD == quotes.USDKWD &&
                       USDKYD == quotes.USDKYD &&
                       USDKZT == quotes.USDKZT &&
                       USDLAK == quotes.USDLAK &&
                       USDLBP == quotes.USDLBP &&
                       USDLKR == quotes.USDLKR &&
                       USDLRD == quotes.USDLRD &&
                       USDLSL == quotes.USDLSL &&
                       USDLTL == quotes.USDLTL &&
                       USDLVL == quotes.USDLVL &&
                       USDLYD == quotes.USDLYD &&
                       USDMAD == quotes.USDMAD &&
                       USDMDL == quotes.USDMDL &&
                       USDMGA == quotes.USDMGA &&
                       USDMKD == quotes.USDMKD &&
                       USDMMK == quotes.USDMMK &&
                       USDMNT == quotes.USDMNT &&
                       USDMOP == quotes.USDMOP &&
                       USDMRO == quotes.USDMRO &&
                       USDMUR == quotes.USDMUR &&
                       USDMVR == quotes.USDMVR &&
                       USDMWK == quotes.USDMWK &&
                       USDMXN == quotes.USDMXN &&
                       USDMYR == quotes.USDMYR &&
                       USDMZN == quotes.USDMZN &&
                       USDNAD == quotes.USDNAD &&
                       USDNGN == quotes.USDNGN &&
                       USDNIO == quotes.USDNIO &&
                       USDNOK == quotes.USDNOK &&
                       USDNPR == quotes.USDNPR &&
                       USDNZD == quotes.USDNZD &&
                       USDOMR == quotes.USDOMR &&
                       USDPAB == quotes.USDPAB &&
                       USDPEN == quotes.USDPEN &&
                       USDPGK == quotes.USDPGK &&
                       USDPHP == quotes.USDPHP &&
                       USDPKR == quotes.USDPKR &&
                       USDPLN == quotes.USDPLN &&
                       USDPYG == quotes.USDPYG &&
                       USDQAR == quotes.USDQAR &&
                       USDRON == quotes.USDRON &&
                       USDRSD == quotes.USDRSD &&
                       USDRUB == quotes.USDRUB &&
                       USDRWF == quotes.USDRWF &&
                       USDSAR == quotes.USDSAR &&
                       USDSBD == quotes.USDSBD &&
                       USDSCR == quotes.USDSCR &&
                       USDSDG == quotes.USDSDG &&
                       USDSEK == quotes.USDSEK &&
                       USDSGD == quotes.USDSGD &&
                       USDSHP == quotes.USDSHP &&
                       USDSLL == quotes.USDSLL &&
                       USDSOS == quotes.USDSOS &&
                       USDSRD == quotes.USDSRD &&
                       USDSTD == quotes.USDSTD &&
                       USDSVC == quotes.USDSVC &&
                       USDSYP == quotes.USDSYP &&
                       USDSZL == quotes.USDSZL &&
                       USDTHB == quotes.USDTHB &&
                       USDTJS == quotes.USDTJS &&
                       USDTMT == quotes.USDTMT &&
                       USDTND == quotes.USDTND &&
                       USDTOP == quotes.USDTOP &&
                       USDTRY == quotes.USDTRY &&
                       USDTTD == quotes.USDTTD &&
                       USDTWD == quotes.USDTWD &&
                       USDTZS == quotes.USDTZS &&
                       USDUAH == quotes.USDUAH &&
                       USDUGX == quotes.USDUGX &&
                       USDUSD == quotes.USDUSD &&
                       USDUYU == quotes.USDUYU &&
                       USDUZS == quotes.USDUZS &&
                       USDVEF == quotes.USDVEF &&
                       USDVND == quotes.USDVND &&
                       USDVUV == quotes.USDVUV &&
                       USDWST == quotes.USDWST &&
                       USDXAF == quotes.USDXAF &&
                       USDXAG == quotes.USDXAG &&
                       USDXAU == quotes.USDXAU &&
                       USDXCD == quotes.USDXCD &&
                       USDXDR == quotes.USDXDR &&
                       USDXOF == quotes.USDXOF &&
                       USDXPF == quotes.USDXPF &&
                       USDYER == quotes.USDYER &&
                       USDZAR == quotes.USDZAR &&
                       USDZMK == quotes.USDZMK &&
                       USDZMW == quotes.USDZMW &&
                       USDZWL == quotes.USDZWL;
            }

            public override int GetHashCode()
            {
                var hashCode = 1183535231;
                hashCode = hashCode * -1521134295 + USDAED.GetHashCode();
                hashCode = hashCode * -1521134295 + USDAFN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDALL.GetHashCode();
                hashCode = hashCode * -1521134295 + USDAMD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDANG.GetHashCode();
                hashCode = hashCode * -1521134295 + USDAOA.GetHashCode();
                hashCode = hashCode * -1521134295 + USDARS.GetHashCode();
                hashCode = hashCode * -1521134295 + USDAUD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDAWG.GetHashCode();
                hashCode = hashCode * -1521134295 + USDAZN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBAM.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBBD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBDT.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBGN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBHD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBIF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBMD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBND.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBOB.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBRL.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBSD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBTC.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBTN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBWP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBYN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBYR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDBZD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCAD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCDF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCHF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCLF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCLP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCNY.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCOP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCRC.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCUC.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCUP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCVE.GetHashCode();
                hashCode = hashCode * -1521134295 + USDCZK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDDJF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDDKK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDDOP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDDZD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDEGP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDERN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDETB.GetHashCode();
                hashCode = hashCode * -1521134295 + USDEUR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDFJD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDFKP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDGBP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDGEL.GetHashCode();
                hashCode = hashCode * -1521134295 + USDGGP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDGHS.GetHashCode();
                hashCode = hashCode * -1521134295 + USDGIP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDGMD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDGNF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDGTQ.GetHashCode();
                hashCode = hashCode * -1521134295 + USDGYD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDHKD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDHNL.GetHashCode();
                hashCode = hashCode * -1521134295 + USDHRK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDHTG.GetHashCode();
                hashCode = hashCode * -1521134295 + USDHUF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDIDR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDILS.GetHashCode();
                hashCode = hashCode * -1521134295 + USDIMP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDINR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDIQD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDIRR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDISK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDJEP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDJMD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDJOD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDJPY.GetHashCode();
                hashCode = hashCode * -1521134295 + USDKES.GetHashCode();
                hashCode = hashCode * -1521134295 + USDKGS.GetHashCode();
                hashCode = hashCode * -1521134295 + USDKHR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDKMF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDKPW.GetHashCode();
                hashCode = hashCode * -1521134295 + USDKRW.GetHashCode();
                hashCode = hashCode * -1521134295 + USDKWD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDKYD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDKZT.GetHashCode();
                hashCode = hashCode * -1521134295 + USDLAK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDLBP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDLKR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDLRD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDLSL.GetHashCode();
                hashCode = hashCode * -1521134295 + USDLTL.GetHashCode();
                hashCode = hashCode * -1521134295 + USDLVL.GetHashCode();
                hashCode = hashCode * -1521134295 + USDLYD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMAD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMDL.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMGA.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMKD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMMK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMNT.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMOP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMRO.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMUR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMVR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMWK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMXN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMYR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDMZN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDNAD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDNGN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDNIO.GetHashCode();
                hashCode = hashCode * -1521134295 + USDNOK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDNPR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDNZD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDOMR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDPAB.GetHashCode();
                hashCode = hashCode * -1521134295 + USDPEN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDPGK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDPHP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDPKR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDPLN.GetHashCode();
                hashCode = hashCode * -1521134295 + USDPYG.GetHashCode();
                hashCode = hashCode * -1521134295 + USDQAR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDRON.GetHashCode();
                hashCode = hashCode * -1521134295 + USDRSD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDRUB.GetHashCode();
                hashCode = hashCode * -1521134295 + USDRWF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSAR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSBD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSCR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSDG.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSEK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSGD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSHP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSLL.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSOS.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSRD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSTD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSVC.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSYP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDSZL.GetHashCode();
                hashCode = hashCode * -1521134295 + USDTHB.GetHashCode();
                hashCode = hashCode * -1521134295 + USDTJS.GetHashCode();
                hashCode = hashCode * -1521134295 + USDTMT.GetHashCode();
                hashCode = hashCode * -1521134295 + USDTND.GetHashCode();
                hashCode = hashCode * -1521134295 + USDTOP.GetHashCode();
                hashCode = hashCode * -1521134295 + USDTRY.GetHashCode();
                hashCode = hashCode * -1521134295 + USDTTD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDTWD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDTZS.GetHashCode();
                hashCode = hashCode * -1521134295 + USDUAH.GetHashCode();
                hashCode = hashCode * -1521134295 + USDUGX.GetHashCode();
                hashCode = hashCode * -1521134295 + USDUSD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDUYU.GetHashCode();
                hashCode = hashCode * -1521134295 + USDUZS.GetHashCode();
                hashCode = hashCode * -1521134295 + USDVEF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDVND.GetHashCode();
                hashCode = hashCode * -1521134295 + USDVUV.GetHashCode();
                hashCode = hashCode * -1521134295 + USDWST.GetHashCode();
                hashCode = hashCode * -1521134295 + USDXAF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDXAG.GetHashCode();
                hashCode = hashCode * -1521134295 + USDXAU.GetHashCode();
                hashCode = hashCode * -1521134295 + USDXCD.GetHashCode();
                hashCode = hashCode * -1521134295 + USDXDR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDXOF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDXPF.GetHashCode();
                hashCode = hashCode * -1521134295 + USDYER.GetHashCode();
                hashCode = hashCode * -1521134295 + USDZAR.GetHashCode();
                hashCode = hashCode * -1521134295 + USDZMK.GetHashCode();
                hashCode = hashCode * -1521134295 + USDZMW.GetHashCode();
                hashCode = hashCode * -1521134295 + USDZWL.GetHashCode();
                return hashCode;
            }
        }

        public class RootObject1
        {
            public bool success { get; set; }
            public string terms { get; set; }
            public string privacy { get; set; }
            public int timestamp { get; set; }
            public string source { get; set; }
            public Quotes quotes { get; set; }

            public override bool Equals(object obj)
            {
                var @object = obj as RootObject1;
                return @object != null &&
                       success == @object.success &&
                       terms == @object.terms &&
                       privacy == @object.privacy &&
                       timestamp == @object.timestamp &&
                       source == @object.source &&
                       EqualityComparer<Quotes>.Default.Equals(quotes, @object.quotes);
            }

            public override int GetHashCode()
            {
                var hashCode = 1649883298;
                hashCode = hashCode * -1521134295 + success.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(terms);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(privacy);
                hashCode = hashCode * -1521134295 + timestamp.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(source);
                hashCode = hashCode * -1521134295 + EqualityComparer<Quotes>.Default.GetHashCode(quotes);
                return hashCode;
            }
        }
        [Test]
        public static void GetUSDEURTest()
        {

            List<RootObject1> expected = new List<RootObject1>{
                new RootObject1 { success = true, terms = @"https://currencylayer.com/terms", privacy = @"https://currencylayer.com/privacy", source = "USD", timestamp = 1531859047, quotes = new Quotes
                {   USDAED = 3.672696, USDAFN = 72.349998, USDALL = 107.800003, USDAMD = 479.869995, USDANG = 1.840135, USDAOA = 253.248993, USDARS = 27.519729, USDAUD = 1.352971, USDAWG = 1.78, USDAZN = 1.699498, USDBAM = 1.679202, USDBBD = 2, USDBDT = 83.989998, USDBGN = 1.670597, USDBHD = 0.378397, USDBIF = 1750.97998, USDBMD = 1, USDBND = 1.351802, USDBOB = 6.860055, USDBRL = 3.843706, USDBSD = 1, USDBTC = 0.000137, USDBTN = 68.599998, USDBWP = 10.240898, USDBYN = 1.969683,
                    USDBYR = 19600, USDBZD = 1.997801, USDCAD = 1.31929, USDCDF = 1565.49956, USDCHF = 0.99945, USDCLF = 0.023794, USDCLP = 653.749567, USDCNY = 6.701695, USDCOP = 2868.5,
                    USDCRC = 563.900024, USDCUC = 1, USDCUP = 26.5, USDCVE = 94.498825, USDCZK = 22.168301, USDDJF = 177.499098, USDDKK = 6.38995, USDDOP = 49.799999, USDDZD = 117.593002,
                    USDEGP = 17.849784, USDERN = 14.990155, USDETB = 27.321099, USDEUR = 0.857054, USDFJD = 2.092501, USDFKP = 0.761982, USDGBP = 0.76252, USDGEL = 2.438802, USDGGP = 0.762528, USDGHS = 4.791006,
                    USDGIP = 0.762196, USDGMD = 47.450001, USDGNF = 9017.00034, USDGTQ = 7.490504, USDGYD = 207.479996, USDHKD = 7.84865, USDHNL = 23.924054, USDHRK = 6.333199, USDHTG = 67.389999, USDHUF = 277.440002,
                    USDIDR = 14365, USDILS = 3.627503, USDIMP = 0.762528, USDINR = 68.442497, USDIQD = 1184, USDIRR = 43420.000305, USDISK = 106.501894, USDJEP = 0.762528, USDJMD = 130.050003, USDJOD = 0.7095,
                    USDJPY = 112.845001, USDKES = 100.349998, USDKGS = 68.138397, USDKHR = 4045.000145, USDKMF = 419.279999, USDKPW = 899.999631, USDKRW = 1125.630005, USDKWD = 0.302796,
                    USDKYD = 0.8199, USDKZT = 343.459991, USDLAK = 8400.000146, USDLBP = 1505.0002, USDLKR = 159.800003, USDLRD = 161.300003, USDLSL = 13.259829, USDLTL = 3.048701, USDLVL = 0.62055, USDLYD = 1.368501,
                    USDMAD = 9.470197, USDMDL = 16.605042, USDMGA = 3270.000263, USDMKD = 52.490002, USDMMK = 1426.999522, USDMNT = 2448.000064, USDMOP = 8.0839, USDMRO = 355.000118, USDMUR = 34.25024,
                    USDMVR = 15.570144, USDMWK = 713.440002, USDMXN = 18.906898, USDMYR = 4.0445, USDMZN = 58.000073, USDNAD = 13.256987, USDNGN = 358.000202, USDNIO = 31.309999, USDNOK = 8.145701, USDNPR = 109.099998,
                    USDNZD = 1.47401, USDOMR = 0.384499, USDPAB = 1, USDPEN = 3.269497, USDPGK = 3.247497, USDPHP = 53.320382, USDPKR = 127.599998, USDPLN = 3.684599, USDPYG = 5715.299805, USDQAR = 3.640299, USDRON = 3.983297,
                    USDRSD = 100.425903, USDRUB = 62.539001, USDRWF = 852.700012, USDSAR = 3.750096, USDSBD = 7.871501, USDSCR = 13.430341, USDSDG = 17.955205, USDSEK = 8.83087, USDSGD = 1.36469, USDSHP = 0.762198, USDSLL = 8199.999824, USDSOS = 571.000137,
                    USDSRD = 7.419946, USDSTD = 21010.199219, USDSVC = 8.749948, USDSYP = 514.97998, USDSZL = 13.249501, USDTHB = 33.29099, USDTJS = 9.419501, USDTMT = 3.41, USDTND = 2.634601, USDTOP = 2.326904,
                    USDTRY = 4.8014, USDTTD = 6.707497, USDTWD = 30.52799, USDTZS = 2272.999512, USDUAH = 26.237499, USDUGX = 3683.999877, USDUSD = 1, USDUYU = 31.264999, USDUZS = 7765.000337, USDVEF = 119699.999987, USDVND = 23049, USDVUV = 109.900002, USDWST = 2.611902, USDXAF = 561.880005,
                    USDXAG = 0.064146, USDXAU = 0.000814, USDXCD = 2.70389, USDXDR = 0.710792, USDXOF = 561.880005, USDXPF = 102.304294, USDYER = 249.850006, USDZAR = 13.263897, USDZMK = 9001.198945, USDZMW = 10.029719, USDZWL = 322.355011
                    }
                }
            };
            var x = ChoJSONReader.Deserialize<RootObject1>(FileNameSample21JSON);
            //, 
            //    new ChoJSONRecordConfiguration()
            //    .Configure(c => c.JSONPath = "$..quotes.USDEUR")
            //    ).First().Value;
            var actual = x.ToList();
            Assert.AreEqual(expected, actual);
        }

        public class Detail
        {
            public string Name { get; set; }
            public string Job { get; set; }

            public override bool Equals(object obj)
            {
                var detail = obj as Detail;
                return detail != null &&
                       Name == detail.Name &&
                       Job == detail.Job;
            }

            public override int GetHashCode()
            {
                var hashCode = 1660590706;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Job);
                return hashCode;
            }
        }

        [Test]
        public static void Test1()
        {
            Detail expected = new Detail { Name = "John", Job = "Receptionist" };
            object actual = null;
            string json = @"{ user: [{
     serialNo: 1,
     details: [{ name: ""John"",
             job: ""Receptionist"" }]
 },
 {
     serialNo: 2,
     details: [{
             name: ""Alan"",
             job: ""Salesman""
          }]
  }
]}";
            using (var p = ChoJSONReader.LoadText(json)
                .Configure(c => c.SupportMultipleContent = true)
                )
            {
                dynamic x = p.FirstOrDefault();
                dynamic y = x.user[0].details[0];
                var z = y.ConvertToObject<Detail>();

                actual = z;
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void CountNodes()
        {
            int expected = 2;
            int? actual = null;

            string json = @"[
{
    ""type"": ""envelope"",
    ""quantity"": 1,
    ""length"": 6,
    ""width"": 1,
    ""height"": 4
},
{
    ""type"": ""box"",
    ""quantity"": 2,
    ""length"": 9,
    ""width"": 9,
    ""height"": 9
}
]";

            using (var p = ChoJSONReader.LoadText(json).WithJSONPath("$.*"))
            {
                actual = p.Count();
            }
            Assert.AreEqual(expected, actual);
        }

        public class AttendanceStatistics
        {
            [JsonProperty(PropertyName = "registrantCount")]
            public int RegistrantCount { get; set; }

            [JsonProperty(PropertyName = "percentageAttendance")]
            public float PercentageAttendance { get; set; }

            [JsonProperty(PropertyName = "averageInterestRating")]
            public float AverageInterestRating { get; set; }

            [JsonProperty(PropertyName = "averageAttentiveness")]
            public float AverageAttentiveness { get; set; }

            [JsonProperty(PropertyName = "averageAttendanceTimeSeconds")]
            public float AverageAttendanceTimeSeconds { get; set; }

            public override bool Equals(object obj)
            {
                var statistics = obj as AttendanceStatistics;
                return statistics != null &&
                       RegistrantCount == statistics.RegistrantCount &&
                       PercentageAttendance == statistics.PercentageAttendance &&
                       AverageInterestRating == statistics.AverageInterestRating &&
                       AverageAttentiveness == statistics.AverageAttentiveness &&
                       AverageAttendanceTimeSeconds == statistics.AverageAttendanceTimeSeconds;
            }

            public override int GetHashCode()
            {
                var hashCode = -1322614220;
                hashCode = hashCode * -1521134295 + RegistrantCount.GetHashCode();
                hashCode = hashCode * -1521134295 + PercentageAttendance.GetHashCode();
                hashCode = hashCode * -1521134295 + AverageInterestRating.GetHashCode();
                hashCode = hashCode * -1521134295 + AverageAttentiveness.GetHashCode();
                hashCode = hashCode * -1521134295 + AverageAttendanceTimeSeconds.GetHashCode();
                return hashCode;
            }
        }

        public class PollsAndSurveysStatistics
        {
            [JsonProperty(PropertyName = "pollCount")]
            public int PollCount { get; set; }

            [JsonProperty(PropertyName = "surveyCount")]
            public float SurveyCount { get; set; }

            [JsonProperty(PropertyName = "questionsAsked")]
            public int QuestionsAsked { get; set; }

            [JsonProperty(PropertyName = "percentagePollsCompleted")]
            public float PercentagePollsCompleted { get; set; }

            [JsonProperty(PropertyName = "percentageSurveysCompleted")]
            public float PercentageSurveysCompleted { get; set; }

            public override bool Equals(object obj)
            {
                var statistics = obj as PollsAndSurveysStatistics;
                return statistics != null &&
                       PollCount == statistics.PollCount &&
                       SurveyCount == statistics.SurveyCount &&
                       QuestionsAsked == statistics.QuestionsAsked &&
                       PercentagePollsCompleted == statistics.PercentagePollsCompleted &&
                       PercentageSurveysCompleted == statistics.PercentageSurveysCompleted;
            }

            public override int GetHashCode()
            {
                var hashCode = 1218383926;
                hashCode = hashCode * -1521134295 + PollCount.GetHashCode();
                hashCode = hashCode * -1521134295 + SurveyCount.GetHashCode();
                hashCode = hashCode * -1521134295 + QuestionsAsked.GetHashCode();
                hashCode = hashCode * -1521134295 + PercentagePollsCompleted.GetHashCode();
                hashCode = hashCode * -1521134295 + PercentageSurveysCompleted.GetHashCode();
                return hashCode;
            }
        }

        public class SessionPerformanceStats
        {
            [JsonProperty(PropertyName = "attendance")]
            public AttendanceStatistics Attendance { get; set; }

            [JsonProperty(PropertyName = "pollsAndSurveys")]
            public PollsAndSurveysStatistics PollsAndSurveys { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public Gender Gender { get; set; }

            public override bool Equals(object obj)
            {
                var stats = obj as SessionPerformanceStats;
                return stats != null &&
                       EqualityComparer<AttendanceStatistics>.Default.Equals(Attendance, stats.Attendance) &&
                       EqualityComparer<PollsAndSurveysStatistics>.Default.Equals(PollsAndSurveys, stats.PollsAndSurveys) &&
                       Gender == stats.Gender;
            }

            public override int GetHashCode()
            {
                var hashCode = 1359950135;
                hashCode = hashCode * -1521134295 + EqualityComparer<AttendanceStatistics>.Default.GetHashCode(Attendance);
                hashCode = hashCode * -1521134295 + EqualityComparer<PollsAndSurveysStatistics>.Default.GetHashCode(PollsAndSurveys);
                hashCode = hashCode * -1521134295 + Gender.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void Sample100()
        {
            List<object> expected = new List<object>
            {
                new SessionPerformanceStats{ Gender= Gender.Female,Attendance=new AttendanceStatistics{
                        AverageAttendanceTimeSeconds =253,
                        AverageInterestRating = 0,
                        AverageAttentiveness = 0,
                        PercentageAttendance = 100,
                        RegistrantCount = 1}, PollsAndSurveys = new PollsAndSurveysStatistics{
                        QuestionsAsked = 1,
                        SurveyCount = 0,
                        PercentageSurveysCompleted = 0,
                        PercentagePollsCompleted = 100, PollCount = 2
                    } },
                new SessionPerformanceStats{ Gender= Gender.Male,Attendance=new AttendanceStatistics{
                        AverageAttendanceTimeSeconds =83,
                        AverageInterestRating = 0,
                        AverageAttentiveness = 0,
                        PercentageAttendance = 100,
                        RegistrantCount = 1}, PollsAndSurveys = new PollsAndSurveysStatistics{
                        QuestionsAsked = 2,
                        SurveyCount = 0,
                        PercentageSurveysCompleted = 0,
                        PercentagePollsCompleted = 0,
                        PollCount = 0
                    } }
            };
            List<object> actual = new List<object>();

            string json = @"{
    ""5234592"":{
    ""pollsAndSurveys"":{
        ""questionsAsked"":1,
        ""surveyCount"":0,
        ""percentageSurveysCompleted"":0,
        ""percentagePollsCompleted"":100,
        ""pollCount"":2},
    ""attendance"":{
        ""averageAttendanceTimeSeconds"":253,
        ""averageInterestRating"":0,
        ""averageAttentiveness"":0,
        ""registrantCount"":1,
        ""percentageAttendance"":100},
    ""gender"":""1""
    },
    ""5235291"":{
    ""pollsAndSurveys"":{
        ""questionsAsked"":2,
        ""surveyCount"":0,
        ""percentageSurveysCompleted"":0,
        ""percentagePollsCompleted"":0,
        ""pollCount"":0},
    ""attendance"":{
        ""averageAttendanceTimeSeconds"":83,
        ""averageInterestRating"":0,
        ""averageAttentiveness"":0,
        ""registrantCount"":1,
        ""percentageAttendance"":100}
    }
}";
            using (var p = ChoJSONReader<SessionPerformanceStats>.LoadText(json)
                .WithJSONPath("$.^")
                .Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Null)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class Attributes
        {
            public int id { get; set; }
            public string name { get; set; }
            public string list_type { get; set; }
            public List<string> attribute_list { get; set; }
        }
        [Test]
        public static void ListOfStringTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"id",1047},{"name","City"},{"attribute_list",new object[] { "RWC", "HMO", "SJ", "Ensenada" } }, {"list_type",1} }
            };
            List<object> actual = new List<object>();

            string json = @"{
    ""id"":1047,
    ""name"":""City"",
    ""attribute_list"":[""RWC"",""HMO"",""SJ"",""Ensenada""],
    ""list_type"":1
}";

            using (var p = ChoJSONReader.LoadText(json)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void ArrayToDataTableTest()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add("Column_0");
            expected.Columns.Add("Column_1");
            expected.Rows.Add("Test123", "TestHub");
            expected.Rows.Add("Sample12", "Sample879");
            expected.Rows.Add("Sample121233", "Sample233879");
            DataTable actual = null;

            string json = @"[
[
    ""Test123"",
    ""TestHub"",
    ""TestVersion"",
    ""TestMKT"",
    ""TestCAP"",
    ""TestRegion"",
    ""TestAssembly"",
    ""TestProduct"",
    ""Testgroup"",
    ""Testsample"",
    ""1806"",
    ""1807"",
    ""1808"",
    ""1809"",
    ""1810"",
    ""1811"",
    ""1812"",
    ""1901"",
    ""1902"",
    ""1903"",
    ""1904"",
    ""1905"",
    ""1906"",
    ""1907"",
    ""1908"",
    ""1909"",
    ""1910"",
    ""1911"",
    ""1912""
],
[
    ""Sample12"",
    ""Sample879"",
    ""201806.1.0"",
    ""Sample098"",
    ""TSA CBU"",
    ""B8"",
    ""B8"",
    ""63"",
    ""63EM"",
    ""EM 42 T"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0""
],
[
    ""Sample121233"",
    ""Sample233879"",
    ""2012323806.1.0"",
    ""Sampl233e098"",
    ""TSA CBU"",
    ""B8"",
    ""B8"",
    ""B3"",
    ""B3ULUE"",
    ""UL 42 R"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0"",
    ""0""
]
]";
            using (var p = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..[*]", true)
                )
            {
                //var dt = p.Select(r => ((object[])r.Value).ToDictionary()).AsDataTable();
                actual = new DataTable();
                actual.Columns.Add("Column_0");
                actual.Columns.Add("Column_1");

                var dict = p.Select(
                    r => ((object[])r.Value)
                    .ToDictionary()).ToArray();
                Console.WriteLine(JsonConvert.SerializeObject(dict, Newtonsoft.Json.Formatting.Indented));
                dict.Fill(actual);
            }

            var actualText = JsonConvert.SerializeObject(actual);
            var expectedText = JsonConvert.SerializeObject(expected);
            //Console.WriteLine(expectedText);
            //Console.WriteLine(actualText);
            //DataTableAssert.AreEqual(expected, actual);
            Assert.AreEqual(expectedText, actualText);
        }
        [Test]
        public static void DataTableTest()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add("Val1");
            expected.Columns.Add("Val2");
            expected.Columns.Add("Val3");
            expected.Columns.Add("links_0_rel");
            expected.Columns.Add("links_0_uri");
            expected.Columns.Add("links_1_rel");
            expected.Columns.Add("links_1_uri");
            expected.Rows.Add("1234", "foo1", "bar1", "self", "/blah/1234", "pricing_data", "/blah/1234/pricing_data");
            expected.Rows.Add("5678", "foo2", "bar2", "self", "/blah/5678", "pricing_data", "/blah/5678/pricing_data");
            DataTable actual = null;

            string json = @"{
    ""data"": [
        {
            ""Val1"": ""1234"",
            ""Val2"": ""foo1"",
            ""Val3"": ""bar1"",
            ""links"": [
                {
                    ""rel"": ""self"",
                    ""uri"": ""/blah/1234""
                },
                {
                    ""rel"": ""pricing_data"",
                    ""uri"": ""/blah/1234/pricing_data""
                }
            ]
        },
        {
            ""Val1"": ""5678"",
            ""Val2"": ""foo2"",
            ""Val3"": ""bar2"",
            ""links"": [
                {
                    ""rel"": ""self"",
                    ""uri"": ""/blah/5678""
                },
                {
                    ""rel"": ""pricing_data"",
                    ""uri"": ""/blah/5678/pricing_data""
                }
            ]
        }
    ],
    ""meta"": {
        ""pagination"": {
            ""total"": 2,
            ""count"": 2,
            ""per_page"": 25,
            ""current_page"": 1,
            ""total_pages"": 1,
            ""links"": []
        }
    }
}";

            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..data[*]", true)
                //.WithField("Val1")
                //.WithField("Val2")
                //.WithField("Val3")
                )
            {
                //foreach (var rec in r)
                //	Console.WriteLine(rec.Flatten().Dump());
                actual = r.ToArray().Select(i => i.Flatten()).AsDataTable();
            }
            DataTableAssert.AreEqual(expected, actual);
        }

        public class Alert
        {
            public string alert { get; set; }
            public string riskcode { get; set; }

            public override bool Equals(object obj)
            {
                var alert = obj as Alert;
                return alert != null &&
                       this.alert == alert.alert &&
                       riskcode == alert.riskcode;
            }

            public override int GetHashCode()
            {
                var hashCode = -731292344;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(alert);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(riskcode);
                return hashCode;
            }
        }
        [Test]
        public static void SelectiveFieldsTest()
        {
            List<object> expected = new List<object>
            {
                new Alert { alert = "X-Content-Type-Options Header Missing", riskcode = "1"},
                new Alert { alert = "X-Content-Type-Options Header Missing", riskcode = "1"}
            };
            List<object> actual = new List<object>();

            string json = @"{
    ""@version"": ""2.7.0"",
    ""@generated"": ""Wed, 30 May 2018 17:23:14"",
    ""site"": {
        ""@name"": ""http://google.com"",
        ""@host"": ""google.com"",
        ""@port"": ""80"",
        ""@ssl"": ""false"",
        ""alerts"": [
            {
                ""alert"": ""X-Content-Type-Options Header Missing"",
                ""name"": ""X-Content-Type-Options Header Missing"",
                ""riskcode"": ""1"",
                ""confidence"": ""2"",
                ""riskdesc"": ""Low (Medium)"",
                ""desc"": ""<p>The Anti-MIME-Sniffing header X-Content-Type-Options was not set to 'nosniff'. This allows older versions of Internet Explorer and Chrome to perform MIME-sniffing on the response body, potentially causing the response body to be interpreted and displayed as a content type other than the declared content type. Current (early 2014) and legacy versions of Firefox will use the declared content type (if one is set), rather than performing MIME-sniffing.</p>"",
                ""instances"": [
                    {
                        ""uri"": ""http://google.com"",
                        ""method"": ""GET"",
                        ""param"": ""X-Content-Type-Options""
                    }
                ],          
                ""wascid"": ""15"",
                ""sourceid"": ""3""
            },
            {
                ""alert"": ""X-Content-Type-Options Header Missing"",
                ""name"": ""X-Content-Type-Options Header Missing"",
                ""riskcode"": ""1"",
                ""confidence"": ""2"",
                ""riskdesc"": ""Low (Medium)"",
                ""desc"": ""<p>The Anti-MIME-Sniffing header X-Content-Type-Options was not set to 'nosniff'. This allows older versions of Internet Explorer and Chrome to perform MIME-sniffing on the response body, potentially causing the response body to be interpreted and displayed as a content type other than the declared content type. Current (early 2014) and legacy versions of Firefox will use the declared content type (if one is set), rather than performing MIME-sniffing.</p>"",
                ""instances"": [
                    {
                        ""uri"": ""http://google.com"",
                        ""method"": ""GET"",
                        ""param"": ""X-Content-Type-Options""
                    }
                ],          
                ""wascid"": ""15"",
                ""sourceid"": ""3""
            }
        ]
    }
}";
            using (var p = ChoJSONReader<Alert>.LoadText(json).WithJSONPath("$..alerts[*]", true))
            {
                foreach (var rec in p)
                    actual.Add(rec);
                //                    Console.WriteLine(rec.Dump());
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void NSTest()
        {
            string expected = @"<ns3:Test_Service xmlns:ns3=""http://www.CCKS.org/XRT/Form"">
  <ns3:fname>mark</ns3:fname>
  <ns3:lname>joye</ns3:lname>
  <ns3:CarCompany>saab</ns3:CarCompany>
  <ns3:CarNumber>9741</ns3:CarNumber>
  <ns3:IsInsured>true</ns3:IsInsured>
  <ns3:safty>ABS</ns3:safty>
  <ns3:safty>AirBags</ns3:safty>
  <ns3:safty>childdoorlock</ns3:safty>
  <ns3:CarDescription>test Car</ns3:CarDescription>
  <ns3:collections>
    <ns3:XYZ>1</ns3:XYZ>
    <ns3:PQR>11</ns3:PQR>
    <ns3:contactdetails>
      <ns3:contname>DOM</ns3:contname>
      <ns3:contnumber>8787</ns3:contnumber>
    </ns3:contactdetails>
    <ns3:contactdetails>
      <ns3:contname>COM</ns3:contname>
      <ns3:contnumber>4564</ns3:contnumber>
      <ns3:addtionaldetails>
        <ns3:description>54657667</ns3:description>
      </ns3:addtionaldetails>
    </ns3:contactdetails>
    <ns3:contactdetails>
      <ns3:contname>gf</ns3:contname>
      <ns3:contnumber>123</ns3:contnumber>
      <ns3:addtionaldetails>
        <ns3:description>123</ns3:description>
      </ns3:addtionaldetails>
    </ns3:contactdetails>
  </ns3:collections>
</ns3:Test_Service>";
            string actual = null;

            string json = @"{
  ""ns3:Test_Service"": {
    ""@xmlns:ns3"": ""http://www.CCKS.org/XRT/Form"",
    ""ns3:fname"": ""mark"",
    ""ns3:lname"": ""joye"",
    ""ns3:CarCompany"": ""saab"",
    ""ns3:CarNumber"": ""9741"",
    ""ns3:IsInsured"": ""true"",
    ""ns3:safty"": [ ""ABS"", ""AirBags"", ""childdoorlock"" ],
    ""ns3:CarDescription"": ""test Car"",
    ""ns3:collections"": [
      {
        ""ns3:XYZ"": ""1"",
        ""ns3:PQR"": ""11"",
        ""ns3:contactdetails"": [
          {
            ""ns3:contname"": ""DOM"",
            ""ns3:contnumber"": ""8787""
          },
          {
            ""ns3:contname"": ""COM"",
            ""ns3:contnumber"": ""4564"",
            ""ns3:addtionaldetails"": [ { ""ns3:description"": ""54657667"" } ]
          },
          {
            ""ns3:contname"": ""gf"",
            ""ns3:contnumber"": ""123"",
            ""ns3:addtionaldetails"": [ { ""ns3:description"": ""123"" } ]
          }
        ]
      }
    ]
  }
}";
            ////string json = @"{""Test_Service"" : {""fname"":""mark"",""lname"":""joye"",""CarCompany"":""saab"",""CarNumber"":""9741"",""IsInsured"":""true"",""safty"":[""ABS"",""AirBags"",""childdoorlock""],""CarDescription"":""test Car"",""collections"":[{""XYZ"":""1"",""PQR"":""11"",""contactdetails"":[{""contname"":""DOM"",""contnumber"":""8787""},{""contname"":""COM"",""contnumber"":""4564"",""addtionaldetails"":[{""description"":""54657667""}]},{""contname"":""gf"",""contnumber"":""123"",""addtionaldetails"":[{""description"":""123""}]}]}]}}";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json).Configure(c => c.SupportMultipleContent = true))
            {
                //Console.WriteLine(p.First().Dump());
                //return;
                using (var w = new ChoXmlWriter(sb)
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    .WithXmlNamespace("ns3", "http://www.CCKS.org/XRT/Form")
                    .Configure(c => c.DoNotEmitXmlNamespace = true)
                    .Configure(c => c.KeepOriginalNodeName = true)
                    )
                {
                    w.Write(p);
                }
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }


        public class BookingInfo //: IChoNotifyRecordFieldRead
        {
            [ChoJSONRecordField(FieldName = "travel_class")]
            public string TravelClass { get; set; }
            [ChoJSONRecordField(FieldName = "booking_code")]
            public string BookingCode { get; set; }

            public bool AfterRecordFieldLoad(object target, long index, string propName, object value)
            {
                throw new NotImplementedException();
            }

            public bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value)
            {
                throw new NotImplementedException();
            }

            public override bool Equals(object obj)
            {
                var info = obj as BookingInfo;
                return info != null &&
                       TravelClass == info.TravelClass &&
                       BookingCode == info.BookingCode;
            }

            public override int GetHashCode()
            {
                var hashCode = -1895515840;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TravelClass);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BookingCode);
                return hashCode;
            }

            public bool RecordFieldLoadError(object target, long index, string propName, ref object value, Exception ex)
            {
                throw new NotImplementedException();
            }
        }

        public class FlightInfo //: IChoNotifyRecordRead
        {
            [ChoJSONRecordField(JSONPath = "$.departs_at")]
            public DateTime DepartAt { get; set; }
            [ChoJSONRecordField(JSONPath = "$.arrives_at")]
            public DateTime ArriveAt { get; set; }

            [ChoJSONRecordField(JSONPath = "$.origin.airport")]
            public string Origin { get; set; }

            [ChoJSONRecordField(JSONPath = "$.booking_info")]
            public BookingInfo[] BookingInfo { get; set; }

            public bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value)
            {
                throw new NotImplementedException();
            }

            public bool AfterRecordFieldLoad(object target, long index, string propName, object value)
            {
                throw new NotImplementedException();
            }

            public bool RecordFieldLoadError(object target, long index, string propName, object value, Exception ex)
            {
                throw new NotImplementedException();
            }

            public bool BeginLoad(object source)
            {
                throw new NotImplementedException();
            }

            public void EndLoad(object source)
            {
                throw new NotImplementedException();
            }

            public bool SkipUntil(long index, object source)
            {
                throw new NotImplementedException();
            }

            public bool DoWhile(long index, object source)
            {
                throw new NotImplementedException();
            }

            public bool BeforeRecordLoad(object target, long index, ref object source)
            {
                throw new NotImplementedException();
            }

            public bool AfterRecordLoad(object target, long index, object source, ref bool skip)
            {
                throw new NotImplementedException();
            }

            public bool RecordLoadError(object target, long index, object source, Exception ex)
            {
                throw new NotImplementedException();
            }

            public override bool Equals(object obj)
            {
                var info = obj as FlightInfo;
                return info != null &&
                       DepartAt == info.DepartAt &&
                       ArriveAt == info.ArriveAt &&
                       Origin == info.Origin &&
                       Enumerable.SequenceEqual(BookingInfo, info.BookingInfo);
            }

            public override int GetHashCode()
            {
                var hashCode = -926953148;
                hashCode = hashCode * -1521134295 + DepartAt.GetHashCode();
                hashCode = hashCode * -1521134295 + ArriveAt.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Origin);
                hashCode = hashCode * -1521134295 + EqualityComparer<BookingInfo[]>.Default.GetHashCode(BookingInfo);
                return hashCode;
            }
        }

        [Test]
        public static void BookingInfoTest()
        {
            FlightInfo expected = new FlightInfo
            {
                DepartAt = new DateTime(2018, 6, 3, 6, 25, 0, DateTimeKind.Utc),
                ArriveAt = new DateTime(2018, 6, 3, 7, 25, 0, DateTimeKind.Utc),
                Origin = "PEN",
                BookingInfo = new BookingInfo[] { new BookingInfo { BookingCode = "Q", TravelClass = "ECONOMY" } }
            };

            string json = @"{
  ""currency"": ""MYR"",
  ""results"": [
    {
      ""itineraries"": [
        {
          ""outbound"": {
            ""flights"": [
              {
                ""departs_at"": ""2018-06-03T06:25"",
                ""arrives_at"": ""2018-06-03T07:25"",
                ""origin"": {
                  ""airport"": ""PEN""

                },
                ""destination"": {
                  ""airport"": ""KUL"",
                  ""terminal"": ""M""
                },
                ""marketing_airline"": ""OD"",
                ""operating_airline"": ""OD"",
                ""flight_number"": ""2105"",
                ""aircraft"": ""738"",
                ""booking_info"": {
                  ""travel_class"": ""ECONOMY"",
                  ""booking_code"": ""Q"",
                  ""seats_remaining"": 9
                }
              }
            ]
          },
          ""inbound"": {
            ""flights"": [
              {
                ""departs_at"": ""2018-06-04T14:10"",
                ""arrives_at"": ""2018-06-04T15:10"",
                ""origin"": {
                  ""airport"": ""KUL"",
                  ""terminal"": ""M""
                },
                ""destination"": {
                  ""airport"": ""PEN""
                },
                ""marketing_airline"": ""OD"",
                ""operating_airline"": ""OD"",
                ""flight_number"": ""2108"",
                ""aircraft"": ""739"",
                ""booking_info"": {
                  ""travel_class"": ""ECONOMY"",
                  ""booking_code"": ""O"",
                  ""seats_remaining"": 5
                }
              }
            ]
          }
        }
      ],
      ""fare"": {
        ""total_price"": ""360.00"",
        ""price_per_adult"": {
          ""total_fare"": ""360.00"",
          ""tax"": ""104.00""
        },
        ""restrictions"": {
          ""refundable"": false,
          ""change_penalties"": true
        }
      }
    }
  ]
}";
            var actual = ChoJSONReader<FlightInfo>.LoadText(json).Configure(c => c.SupportMultipleContent = false)
                .WithJSONPath("$..results[0].itineraries[0].outbound.flights[0]", true)
                .FirstOrDefault();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void JsonToString()
        {
            string expected = @"wide, outstretched, width,breadth, town, street,earth, country, greatness. wife, the mistress of the house, wide agricultural tract, waste land the land which is not suitabie for cultivation.";
            string actual = null;

            string json = @"[
    {
        ""eng_trans"": ""wide, outstretched,""
    },
    {
        ""eng_trans"": ""width,breadth, town, street,earth, country, greatness.""
    },
    {
        ""eng_trans"": ""wife, the mistress of the house,""
    },
    {
        ""eng_trans"": ""wide agricultural tract,""
    },
    {
        ""eng_trans"": ""waste land the land which is not suitabie for cultivation.""
    }]";

            actual = String.Join(" ", ChoJSONReader.LoadText(json).Select(r1 => r1.eng_trans));

            //var x = ChoJSONReader.LoadText(json, null, new ChoJSONRecordConfiguration().Configure(c => c.JSONPath = "$.eng_trans")).Select(r => r.Value).ToArray();
            //Console.WriteLine(x.Dump());

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Colors2DataTable()
        {
            //Assert.Fail("I am not sure, how the output should be");

            DataTable expected = new DataTable();
            expected.Columns.Add("color");
            expected.Columns.Add("category");
            expected.Rows.Add("black", "hue");
            expected.Rows.Add("white", "value");
            expected.Rows.Add("red", "hue");
            expected.Rows.Add("blue", "hue");
            expected.Rows.Add("yellow", "hue");
            expected.Rows.Add("green", "hue");

            DataTable actual = null;

            using (var p = new ChoJSONReader(FileNameColorsJSON)
                .WithJSONPath("$.colors")
                .WithField("color")
                .WithField("category")
                )
            {
                //                var tmp = p.ToList();
                actual = p.AsDataTable();
                //foreach (var rec in p)
                //	Console.WriteLine(rec.Dump());
            }

            DataTableAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample43()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject
                {
                    { "property1",1},
                    { "property2",2 },
                    { "someArray", new object[] { 2 } }
                }
            };
            List<object> actual = new List<object>();

            string json = @"{
    ""property1"": 1,
    ""property2"": 2,
    ""someArray"": [
        {
            ""item1"": 1,
            ""item2"": 2
        },
        {
            ""item1"": 5
        }
    ]
}";
            using (var p = ChoJSONReader.LoadText(json)
                .WithField("property1", jsonPath: "$.property1", isArray: false)
                .WithField("property2", jsonPath: "$.property2", isArray: false)
                .WithField("someArray", jsonPath: @"$.someArray[?(@.item2)].item2", isArray: true)
            )
            {
                foreach (var rec in p)
                    actual.Add(rec);// Console.WriteLine(rec.Dump());
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        public partial class MyNode
        {
            public long Param1 { get; set; }
            public string Param2 { get; set; }
            public object Param3 { get; set; }

            public override bool Equals(object obj)
            {
                var node = obj as MyNode;
                return node != null &&
                       Param1 == node.Param1 &&
                       Param2 == node.Param2;
                //&& EqualityComparer<object>.Default.Equals(Param3, node.Param3);
            }

            public override int GetHashCode()
            {
                var hashCode = -1460526020;
                hashCode = hashCode * -1521134295 + Param1.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Param2);
                hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(Param3);
                return hashCode;
            }
        }

        [Test]
        public static void Sample42()
        {
            List<object> expected = new List<object>
            {
                new MyNode{ Param1 = 1, Param2 = "myValue2a", Param3 = new ChoDynamicObject{{"myParam3param",0}}},
                new MyNode{ Param1 = 1, Param2 = "myValue2b", Param3 = new ChoDynamicObject{{"myItemA","abc"},{"myItemB","dev" },{"myItemC","0" } }},
                new MyNode{ Param1 = 1, Param2 = "myValue2c", Param3 = new ChoDynamicObject{{"myItemA","ghi"},{"myItemB","jkl" },{"myItemC","0" } }}
            };
            List<MyNode> actual = null;

            string json = @"{
""myNodes"": [
    {
        ""param1"": 1,
        ""param2"": ""myValue2a"",
        ""param3"": {
            ""myParam3param"": 0
        }
    },
    {
        ""param1"": 1,
        ""param2"": ""myValue2b"",
        ""param3"": [
        {
            ""myItemA"": ""abc"",
            ""myItemB"": ""def"",
            ""myItemC"": ""0""
        }]
    },
    {
        ""param1"": 1,
        ""param2"": ""myValue2c"",
        ""param3"": [
        {
            ""myItemA"": ""ghi"",
            ""myItemB"": ""jkl"",
            ""myItemC"": ""0""
        }]
    }]
}";
            using (var p = ChoJSONReader<MyNode>.LoadText(json)
                .WithJSONPath("$..myNodes")
                )
            {
                actual = p.ToList();
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample41()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{ { "entity_id", "1" },{"CustomerName","Test1" },{"AccountNumber","ACC17-001" },{"CustomerType","Direct Sale" } },
                new ChoDynamicObject{ { "entity_id", "2" },{"CustomerName","Test2" },{"AccountNumber","ACC17-002" },{"CustomerType","Direct Sale" } },
                new ChoDynamicObject{ { "entity_id", "3" },{"CustomerName","Test3" },{"AccountNumber","ACC17-003" },{"CustomerType","Direct Sale" } },
                new ChoDynamicObject{ { "entity_id", "4" },{"CustomerName","Test4" },{"AccountNumber","ACC17-004" },{"CustomerType","Direct Sale" } },
                new ChoDynamicObject{ { "entity_id", "5" },{"CustomerName","Test5" },{"AccountNumber","ACC17-005" },{"CustomerType","Invoice" } },
                new ChoDynamicObject{ { "entity_id", "6" },{"CustomerName","Test6" },{"AccountNumber","ACC17-006" },{"CustomerType", "Invoice" } }
            };
            IEnumerable<object> actual = null;

            string json = @"[
[{""Key"":""entity_id"",""Value"":""1""},{""Key"":""CustomerName"",""Value"":""Test1""},{""Key"":""AccountNumber"",""Value"":""ACC17-001""},{""Key"":""CustomerType"",""Value"":""Direct Sale""}],
[{""Key"":""entity_id"",""Value"":""2""},{""Key"":""CustomerName"",""Value"":""Test2""},{""Key"":""AccountNumber"",""Value"":""ACC17-002""},{""Key"":""CustomerType"",""Value"":""Direct Sale""}],
[{""Key"":""entity_id"",""Value"":""3""},{""Key"":""CustomerName"",""Value"":""Test3""},{""Key"":""AccountNumber"",""Value"":""ACC17-003""},{""Key"":""CustomerType"",""Value"":""Direct Sale""}],
[{""Key"":""entity_id"",""Value"":""4""},{""Key"":""CustomerName"",""Value"":""Test4""},{""Key"":""AccountNumber"",""Value"":""ACC17-004""},{""Key"":""CustomerType"",""Value"":""Direct Sale""}],
[{""Key"":""entity_id"",""Value"":""5""},{""Key"":""CustomerName"",""Value"":""Test5""},{""Key"":""AccountNumber"",""Value"":""ACC17-005""},{""Key"":""CustomerType"",""Value"":""Invoice""}],
[{""Key"":""entity_id"",""Value"":""6""},{""Key"":""CustomerName"",""Value"":""Test6""},{""Key"":""AccountNumber"",""Value"":""ACC17-006""},{""Key"":""CustomerType"",""Value"":""Invoice""}]
]";

            using (var p = ChoJSONReader.LoadText(json))
            {
                actual = p.Select(r1 => (dynamic[])r1.Value).Select(r2 =>
                    ChoDynamicObject.FromKeyValuePairs(r2.Select(kvp => new KeyValuePair<string, object>(kvp.Key.ToString(), kvp.Value)))
                ).ToList();

                //                Console.WriteLine(ChoJSONWriter.ToTextAll(result));
            }

            CollectionAssert.AreEqual(expected, actual);
            //using (var p = ChoJSONReader.LoadText(json))
            //{
            //	var result = p.Select(r1 => (dynamic[])r1.Value).Select(r2 => 
            //	{
            //		ChoDynamicObject obj = new ChoDynamicObject();
            //		foreach (dynamic r3 in r2)
            //			obj.Add(r3.Key.ToString(), r3.Value);
            //		return obj;
            //	});

            //	Console.WriteLine(ChoJSONWriter.ToTextAll(result));
            //}
        }

        [Test]
        public static void Sample40()
        {
            object[] expected = new object[] { new ChoDynamicObject {
                { "Excited", new string[]{ "https://github.com/vedantroy/image-test/raw/master/Happy/excited1.gif", "https://github.com/vedantroy/image-test/raw/master/Happy/excited2.gif", "https://github.com/vedantroy/image-test/raw/master/Happy/excited3.gif" }},
                { "Sad", new string[]{ "https://github.com/vedantroy/image-test/raw/master/Sad/sad1.gif", "https://github.com/vedantroy/image-test/raw/master/Sad/sad2.gif", "https://github.com/vedantroy/image-test/raw/master/Sad/sad3.gif","https://github.com/vedantroy/image-test/raw/master/Sad/sad4.gif" }}
            } };
            object[] actual = null;

            string json = @"{
    ""Excited"":[""https://github.com/vedantroy/image-test/raw/master/Happy/excited1.gif"",
            ""https://github.com/vedantroy/image-test/raw/master/Happy/excited2.gif"",
                ""https://github.com/vedantroy/image-test/raw/master/Happy/excited3.gif""],

    ""Sad"":[""https://github.com/vedantroy/image-test/raw/master/Sad/sad1.gif"",
            ""https://github.com/vedantroy/image-test/raw/master/Sad/sad2.gif"",
            ""https://github.com/vedantroy/image-test/raw/master/Sad/sad3.gif"",
            ""https://github.com/vedantroy/image-test/raw/master/Sad/sad4.gif""]
    }";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
            )
            {
                actual = p.ToArray();
                //Console.WriteLine(ChoJSONWriter<RootObject>.ToTextAll(p));
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class RootObject
        {
            public string Id { get; set; }
            public List<CustomField> Custom_fields { get; set; }
        }
        public class CustomField
        {
            public string Definition { get; set; }
            public object Value { get; set; }
        }

        [Test]
        public static void Sample39()
        {
            string expected = @"[
  {
    ""Id"": ""12345"",
    ""Custom_fields"": [
      {
        ""Definition"": ""field1"",
        ""Value"": ""stringvalue""
      },
      {
        ""Definition"": ""field2"",
        ""Value"": [
          ""arrayvalue1"",
          ""arrayvalue2""
        ]
      },
      {
        ""Definition"": ""field3"",
        ""Value"": {
          ""type"": ""user"",
          ""id"": ""1245""
        }
      }
    ]
  }
]";
            string actual = null;

            string json = @"
                {
                    ""id"": ""12345"",
                    ""custom_fields"": [
                        {
                        ""definition"": ""field1"",
                        ""value"": ""stringvalue""

                        },      
                        {
                        ""definition"": ""field2"",
                        ""value"": [ ""arrayvalue1"", ""arrayvalue2"" ]
                },
                        {
                        ""definition"": ""field3"",
                        ""value"": {
                            ""type"": ""user"",
                            ""id"": ""1245""
                        }
                        }
                    ]
                }";


            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader<RootObject>.LoadText(json)
            //.WithField(m => m.Custom_fields, itemConverter: v => v)
            )
            {
                //var x = p.ToArray();
                actual = ChoJSONWriter<RootObject>.ToTextAll(p);
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample38()
        {
            var testJson = @"{""entry1"": {
                       ""49208118"": [
                          {
                             ""description"": ""just a description""
                          },
                          {
                             ""description"": ""another description"" 
                          }
                       ],
                       ""29439559"": [
                          {
                             ""description"": ""just a description""
                          },
                          {
                             ""description"": ""another description"" 
                          }
                       ]
                     }
}";

            string expected = @"description
just a description
another description
just a description
another description";

            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(testJson)
                .WithJSONPath("$..description", true)
                .WithField("description", fieldName: "Value")
                )
            {
                //var x = r/*.Select(r1 => r1.Value)*/.ToArray();

                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    )
                    w.Write(r);
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class sp
        {
            public string mKey { get; set; }
            public string productType { get; set; }
            public string key { get; set; }

            public override bool Equals(object obj)
            {
                var sp = obj as sp;
                return sp != null &&
                       mKey == sp.mKey &&
                       productType == sp.productType &&
                       key == sp.key;
            }

            public override int GetHashCode()
            {
                var hashCode = 601410887;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(mKey);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(productType);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(key);
                return hashCode;
            }
        }

        [Test]
        public static void Sample37()
        {
            List<object> expected = new List<object>
            {
                new sp{ mKey = "SB_MARKET:924.136028459", key = "924.136028459", productType = "BOOK1"},
                new sp{ mKey = "SB_MARKET:924.136028459", key = "924.136028500", productType = "BOOK2"}
            };
            List<object> actual = new List<object>();

            string json = @"{  
       ""success"":1,
       ""results"":[
          {  
             ""Markets"":{  
                ""924.136028459"":{  
                   ""productType"":""BOOK1"",
                   ""key"":""SB_MARKET:924.136028459""

                },
                ""924.136028500"":{  
                   ""productType"":""BOOK2"",
                   ""key"":""SB_MARKET:924.136028459""
                }
             }
          }
       ]
    }
            ";

            ChoETLSettings.KeySeparator = '_';
            foreach (var rec in ChoJSONReader.LoadText(json).WithJSONPath("$..results.Markets")
                .Select(r => (IDictionary<string, object>)r).SelectMany(r1 => r1.Keys.Select(k => new { key = k, value = ((dynamic)((IDictionary<string, object>)r1)[k]) })
                .Select(k1 => new sp { mKey = k1.value.key, key = k1.key, productType = k1.value.productType })))
                actual.Add(rec);// Console.WriteLine(rec.Dump());
            //Console.WriteLine($"ProductType: {rec.productType}, Key: {rec.key}");

            //StringBuilder sb = new StringBuilder();
            //using (var p = ChoJSONReader.LoadText(json)
            //	.WithJSONPath("$..Markets.*")
            //	)
            //{
            //	foreach (var rec in p)
            //		Console.WriteLine(rec.Dump());
            //}
            //Console.WriteLine(sb.ToString());

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample36()
        {
            string expected = @"<vessel xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <row>
    <_mmsi>538006090</_mmsi>
    <_imo>9700665</_imo>
    <lat_>60.87363</lat_>
    <_lon>-13.02203</_lon>
  </row>
  <row>
    <_mmsi>527555481</_mmsi>
    <_imo>0</_imo>
    <lat_>4.57883</lat_>
    <_lon>3.76899</_lon>
  </row>
</vessel>";

            string json = @"{
               ""paging"": {

                  ""limit"": 100,
                  ""total"": 1394,
                  ""next"": ""Mg==""
               },
               ""data"": [
                  {
                     ""mmsi"": 538006090,
                     ""imo"": 9700665,
                     ""last_known_position"": {
                        ""timestamp"": ""2017-12-18T20:24:27+00:00"",
                        ""geometry"": {
                           ""type"": ""Point"",
                           ""coordinates"": [
                              60.87363,
                              -13.02203
                           ]
                }
            }
                  },
                  {
                     ""mmsi"": 527555481,
                     ""imo"": null,
                     ""last_known_position"": {
                        ""timestamp"": ""2017-12-18T20:24:27+00:00"",
                        ""geometry"": {
                           ""type"": ""Point"",
                           ""coordinates"": [
                              4.57883,
                              3.76899
                           ]
                        }
                     }
                  }
               ]
            }
            ";
            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..data")
                )
            {
                using (var w = new ChoXmlWriter(sb)
                    .Configure(c => c.RootName = "vessel")
                    .Configure(c => c.NodeName = "row")
                    //.Configure(c => c.NullValueHandling = ChoNullValueHandling.Ignore)
                    )
                {
                    w.Write(p.Select(r => new { _mmsi = r.mmsi, _imo = r.imo == null ? "0" : r.imo, lat_ = r.last_known_position.geometry.coordinates[0], _lon = r.last_known_position.geometry.coordinates[1] }));
                }
            }
            var actual = sb.ToString();

            //var x = ChoXmlReader.LoadText(sb.ToString()).ToArray();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample35()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"SelFoodId","2"},{"SelQuantity","5"}},
                new ChoDynamicObject{{"SelFoodId","7"},{"SelQuantity","3"}},
                new ChoDynamicObject{{"SelFoodId","9"},{"SelQuantity","7"}}
            };
            List<object> actual = new List<object>();
            string json = @"[
{""SelFoodId"":""2"",""SelQuantity"":""5""},
{ ""SelFoodId"":""7"",""SelQuantity"":""3""},
{ ""SelFoodId"":""9"",""SelQuantity"":""7""}]
";
            foreach (var rec in ChoJSONReader.LoadText(json))
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }

        public class Issue
        {
            [ChoJSONRecordField(JSONPath = "$..id")]
            public int? id { get; set; }
            [ChoJSONRecordField(JSONPath = "$..project.id")]
            public int project_id { get; set; }
            [ChoJSONRecordField(JSONPath = "$..project.name")]
            public string project_name { get; set; }

            public override bool Equals(object obj)
            {
                var issue = obj as Issue;
                return issue != null &&
                       EqualityComparer<int?>.Default.Equals(id, issue.id) &&
                       project_id == issue.project_id &&
                       project_name == issue.project_name;
            }

            public override int GetHashCode()
            {
                var hashCode = -1472824570;
                hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(id);
                hashCode = hashCode * -1521134295 + project_id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(project_name);
                return hashCode;
            }
        }

        [Test]
        public static void Sample33()
        {
            Issue expected = new Issue { id = 1, project_id = 1, project_name = "name of project" };

            string json = @"{
               ""issue"" : 
               {
                  ""id"": 1,
                  ""project"":
                  {
                     ""id"":1,
                     ""name"":""name of project""
                  }
               }
            }";
            var issue = ChoJSONReader<Issue>.LoadText(json).First();

            Assert.AreEqual(expected, issue);
        }

        [Test]
        public static void Sample32()
        {
            string expected = @"SRNO,STK_IDN,CERTIMG
2814,1001101259,6262941723
2815,1001101269,6262941726
2816,1001101279,6262941729";
            string actual = null;

            string json = @"{
""HDRDTL"":[""SRNO"",""STK_IDN"",""CERTIMG""],
""PKTDTL"":[
{""SRNO"":""2814"",""STK_IDN"":""1001101259"",""CERTIMG"":""6262941723""},
{""SRNO"":""2815"",""STK_IDN"":""1001101269"",""CERTIMG"":""6262941726""},
{""SRNO"":""2816"",""STK_IDN"":""1001101279"",""CERTIMG"":""6262941729""}
],
""IMGTTL"":
[""CERTIMG"",""ARRIMG""],
""IMGDTL"":{""CERTIMG"":""CRd6z2uq3gvx7kk"",""ARRIMG"":""ASd6z2uq3gvx7kk""}
}";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json).WithJSONPath("$..PKTDTL")
                )
            {
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    )
                    w.Write(p);
            }

            actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample31()
        {
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <XElement>
    <mercedes>
      <model>GLS 350 d 4MATIC</model>
      <code>MB-GLS</code>
      <year>2015</year>
    </mercedes>
    <bmw>
      <model>BMW 420d Cabrio</model>
      <code>BM-420D</code>
      <year>2017</year>
    </bmw>
    <audi>
      <model>A5 Sportback 2.0 TDI quattro</model>
      <code>AU-A5</code>
      <year>2018</year>
    </audi>
    <tesla>
      <model>Model S</model>
      <code>TS-MOS</code>
      <year>2016</year>
    </tesla>
    <test_drive>0</test_drive>
    <path>D:\Mizz\cars\</path>
  </XElement>
</Root>";
            string actual = null;

            string json = @"{
    ""mercedes"": {

        ""model"" : ""GLS 350 d 4MATIC"",
        ""code"" : ""MB-GLS"",
        ""year"": 2015

    },
    ""bmw"": {
        ""model"" : ""BMW 420d Cabrio"",
        ""code"" : ""BM-420D"",
        ""year"": 2017
    },
    ""audi"": {
        ""model"" : ""A5 Sportback 2.0 TDI quattro"",
        ""code"" : ""AU-A5"",
        ""year"": 2018
    },
    ""tesla"": {
        ""model"" : ""Model S"",
        ""code"" : ""TS-MOS"",
        ""year"": 2016
    },
  ""test_drive"": 0,
  ""path"": ""D:\\Mizz\\cars\\""
}";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
                )
            {
                //foreach (ChoDynamicObject rec in p)
                //	Console.WriteLine(ChoUtility.Dump(rec.Keys));

                using (var w = new ChoXmlWriter(sb)
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Sample30()
        {
            string expected = @"<Emps>
  <Emp>
    <items>
      <item>
        <title>Overlay HD/CC</title>
        <guid>1</guid>
        <description>This example shows tooltip overlays for captions and quality.</description>
        <image>http://content.jwplatform.com/thumbs/3XnJSIm4-640.jpg</image>
        <source file=""http://content.jwplatform.com/videos/3XnJSIm4-kNspJqnJ.mp4"" label=""360p"" />
        <sources>
          <source file=""http://content.jwplatform.com/videos/3XnJSIm4-DZ7jSYgM.mp4"" label=""720p"" />
          <source file=""http://content.jwplatform.com/videos/3XnJSIm4-kNspJqnJ.mp4"" label=""360p"" />
          <source file=""http://content.jwplatform.com/videos/3XnJSIm4-injeKYZS.mp4"" label=""180p"" />
        </sources>
        <tracks>
          <track file=""http://content.jwplatform.com/captions/2UEDrDhv.txt"" label=""English"" />
          <track file=""http://content.jwplatform.com/captions/6aaGiPcs.txt"" label=""Japanese"" />
          <track file=""http://content.jwplatform.com/captions/2nxzdRca.txt"" label=""Russian"" />
          <track file=""http://content.jwplatform.com/captions/BMjSl0KC.txt"" label=""Spanish"" />
        </tracks>
      </item>
    </items>
  </Emp>
  <Emp>
    <items />
  </Emp>
</Emps>";
            string actual = null;

            string json = @"{""Emp"": [
 {
                ""items"": [
                  {
      ""title"": ""Overlay HD/CC"",
                    ""guid"": ""1"",
                    ""description"": ""This example shows tooltip overlays for captions and quality."",
                    ""image"": ""http://content.jwplatform.com/thumbs/3XnJSIm4-640.jpg"",
                    ""source"": {
        ""@file"": ""http://content.jwplatform.com/videos/3XnJSIm4-kNspJqnJ.mp4"",
                      ""@label"": ""360p""
      },
      ""sources"": [
        {
          ""@file"": ""http://content.jwplatform.com/videos/3XnJSIm4-DZ7jSYgM.mp4"",
          ""@label"": ""720p""
        },
        {
          ""@file"": ""http://content.jwplatform.com/videos/3XnJSIm4-kNspJqnJ.mp4"",
          ""@label"": ""360p""
        },
        {
          ""@file"": ""http://content.jwplatform.com/videos/3XnJSIm4-injeKYZS.mp4"",
          ""@label"": ""180p""
        }
      ],
      ""tracks"": [
        {
          ""@file"": ""http://content.jwplatform.com/captions/2UEDrDhv.txt"",
          ""@label"": ""English""
        },
        {
          ""@file"": ""http://content.jwplatform.com/captions/6aaGiPcs.txt"",
          ""@label"": ""Japanese""
        },
        {
          ""@file"": ""http://content.jwplatform.com/captions/2nxzdRca.txt"",
          ""@label"": ""Russian""
        },
        {
          ""@file"": ""http://content.jwplatform.com/captions/BMjSl0KC.txt"",
          ""@label"": ""Spanish""
        }
      ]
    }
  ]
 },
  {
  ""items"": null
 }
]
}
";
            //ChoDynamicObjectSettings.XmlArrayQualifier = (key, obj) =>
            //{
            //    //if (key == "sources"
            //    //    || key == "tracks")
            //    //    return true;
            //    return null;
            //};

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json).Configure(c => c.SupportMultipleContent = true)
                )
            {
                //                dynamic rec = p.First();
                //                var x = rec.Emp[0];

                using (var w = new ChoXmlWriter(sb)
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    .Configure(c => c.DoNotEmitXmlNamespace = true)
                    .Configure(c => c.UseXmlArray = true)
                    .Configure(c => c.XmlArrayQualifier = (key, obj) => null)
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample29()
        {
            string expected = @"<RSS xmlns:jwplayer=""http://support.jwplayer.com/customer/portal/articles/1403635-media-format-reference#feeds"" version=""2.0"">
  <Channel>
    <item>
      <title>Overlay HD/CC</title>
      <guid>1</guid>
      <description>This example shows tooltip overlays for captions and quality.</description>
      <jwplayer:image>http://content.jwplatform.com/thumbs/3XnJSIm4-640.jpg</jwplayer:image>
      <jwplayer:sources>
        <jwplayer:source file=""http://content.jwplatform.com/videos/3XnJSIm4-DZ7jSYgM.mp4"" label=""720p"" />
        <jwplayer:source file=""http://content.jwplatform.com/videos/3XnJSIm4-kNspJqnJ.mp4"" label=""360p"" />
        <jwplayer:source file=""http://content.jwplatform.com/videos/3XnJSIm4-injeKYZS.mp4"" label=""180p"" />
      </jwplayer:sources>
      <jwplayer:tracks>
        <jwplayer:track file=""http://content.jwplatform.com/captions/2UEDrDhv.txt"" label=""English"" />
        <jwplayer:track file=""http://content.jwplatform.com/captions/6aaGiPcs.txt"" label=""Japanese"" />
        <jwplayer:track file=""http://content.jwplatform.com/captions/2nxzdRca.txt"" label=""Russian"" />
        <jwplayer:track file=""http://content.jwplatform.com/captions/BMjSl0KC.txt"" label=""Spanish"" />
      </jwplayer:tracks>
    </item>
  </Channel>
</RSS>";
            string actual = null;

            string json = @"{
  ""RSS"": {
    ""Channel"": {
      ""item"": [
        {
          ""title"": ""Overlay HD/CC"",
          ""guid"": ""1"",
          ""description"": ""This example shows tooltip overlays for captions and quality."",
          ""jwplayer:image"": ""http://content.jwplatform.com/thumbs/3XnJSIm4-640.jpg"",
          ""jwplayer:source"": [
            {
              ""@file"": ""http://content.jwplatform.com/videos/3XnJSIm4-DZ7jSYgM.mp4"",
              ""@label"": ""720p""
            },
            {
              ""@file"": ""http://content.jwplatform.com/videos/3XnJSIm4-kNspJqnJ.mp4"",
              ""@label"": ""360p""
            },
            {
              ""@file"": ""http://content.jwplatform.com/videos/3XnJSIm4-injeKYZS.mp4"",
              ""@label"": ""180p""
            }
          ],
          ""jwplayer:track"": [
            {
              ""@file"": ""http://content.jwplatform.com/captions/2UEDrDhv.txt"",
              ""@label"": ""English""
            },
            {
              ""@file"": ""http://content.jwplatform.com/captions/6aaGiPcs.txt"",
              ""@label"": ""Japanese""
            },
            {
              ""@file"": ""http://content.jwplatform.com/captions/2nxzdRca.txt"",
              ""@label"": ""Russian""
            },
            {
              ""@file"": ""http://content.jwplatform.com/captions/BMjSl0KC.txt"",
              ""@label"": ""Spanish""
            }
          ]
        }
      ]
    },
    ""@xmlns:jwplayer"": ""http://support.jwplayer.com/customer/portal/articles/1403635-media-format-reference#feeds"",
    ""@version"": ""2.0""
  }
}";
            //ChoDynamicObjectSettings.XmlArrayQualifier = (key, obj) =>
            //{
            //    if (key == "jwplayer:source"
            //        || key == "jwplayer:track")
            //        return true;
            //    return null;
            //};
            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
                )
            {
                using (var w = new ChoXmlWriter(sb)
                    .WithXmlNamespace("jwplayer", "http://support.jwplayer.com/customer/portal/articles/1403635-media-format-reference#feeds")
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    .Configure(c => c.DoNotEmitXmlNamespace = true)
                    .Configure(c => c.XmlArrayQualifier = (key, obj) =>
                    {
                        if (key == "jwplayer:source"
                            || key == "jwplayer:track")
                            return true;
                        return null;
                    })
                    )
                    w.Write(p);
            }

            actual = sb.ToString();
            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample28()
        {
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <XElement name=""desired_gross_margin"" type=""int"" value=""50"" />
  <XElement name=""desired_adjusted_gross_margin"" type=""int"" value=""50"" />
  <XElement name=""target_electricity_tariff_unit_charge"" type=""decimal"" value=""0"" />
  <XElement name=""target_electricity_tariff_standing_charge"" type=""decimal"" value=""0"" />
  <XElement name=""target_gas_tariff_unit_charge"" type=""decimal"" value=""0"" />
  <XElement name=""target_gas_tariff_standing_charge"" type=""decimal"" value=""0"" />
  <XElement name=""planned_go_live_date"" type=""DateTime"" value=""10/10/2016"" />
  <XElement name=""assumed_fuel_ratio"" type=""int"" value=""0"" />
  <XElement name=""weather_variable"" type=""string"">
    <value>
      <year_one>Cold</year_one>
      <year_two>Average</year_two>
      <year_three>Warm</year_three>
    </value>
  </XElement>
</Root>";
            string actual = null;
            string json = @"
[
    { ""value"":50,""name"":""desired_gross_margin"",""type"":""int""},
    { ""value"":50,""name"":""desired_adjusted_gross_margin"",""type"":""int""},
    { ""value"":0,""name"":""target_electricity_tariff_unit_charge"",""type"":""decimal""},
    { ""value"":0,""name"":""target_electricity_tariff_standing_charge"",""type"":""decimal""},
    { ""value"":0,""name"":""target_gas_tariff_unit_charge"",""type"":""decimal""},
    { ""value"":0,""name"":""target_gas_tariff_standing_charge"",""type"":""decimal""},
    { ""value"":""10/10/2016"",""name"":""planned_go_live_date"",""type"":""DateTime""},
    { ""value"":""0"",""name"":""assumed_fuel_ratio"",""type"":""int""},
    {
                ""value"":{
                    ""year_one"":""Cold"",
        ""year_two"":""Average"",
        ""year_three"":""Warm""
           
        },
    ""name"":""weather_variable"",""type"":""string""}
]";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
                )
            {
                //foreach (var rec in p)
                //	Console.WriteLine(rec.Dump());

                using (var w = new ChoXmlWriter(sb)
                    .WithField("name", isXmlAttribute: true)
                    .WithField("type", isXmlAttribute: true)
                    .WithField("value", isAnyXmlNode: true)
                    //.Configure(c => c.IgnoreRootName = true)
                    //.Configure(c => c.IgnoreNodeName = true)
                    //.WithDefaultXmlNamespace("x1", "http://unknwn")
                    )
                {
                    w.Write(p);
                }
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Sample27()
        {
            string expected = @"<cars xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <car>
    <features>
      <feature>
        <code>1</code>
      </feature>
      <feature>
        <code>2</code>
      </feature>
    </features>
  </car>
  <car>
    <features>
      <feature>
        <code>3</code>
      </feature>
      <feature>
        <code>2</code>
      </feature>
    </features>
  </car>
</cars>";
            string actual = null;

            string json = @"
            [
                {
                    ""car"": {
                        ""features"": [{
                            ""code"": ""1""
                        }, {
                            ""code"": ""2""
                        }]
                    }
                },
                {
                    ""car"": {
                        ""features"": [{
                            ""code"": ""3""
                        }, {
                            ""code"": ""2""
                        }]
                    }
                }
            ]";

            //ChoDynamicObjectSettings.XmlArrayQualifier = (key, obj) =>
            //{
            //    if (key == "features")
            //        return true;
            //    return null;
            //};

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoXmlWriter(sb)
                    .Configure(c => c.RootName = "cars")
                    //.Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    .Configure(c => c.UseXmlArray = true)
                    .Configure(c => c.XmlArrayQualifier = (key, obj) =>
                    {
                        if (key == "features")
                            return true;
                        return null;
                    })
                    )
                {
                    w.Write(p);
                }
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample26()
        {
            string expected = @"<x1:Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:x1=""http://unknwn"">
  <x1:XElement>
    <x1:item>
      <x1:name>item #1</x1:name>
      <x1:code>itm-123</x1:code>
      <x1:image url=""http://www.foo.com/bar.jpg"" />
    </x1:item>
  </x1:XElement>
</x1:Root>";
            string actual = null;

            string json = @"
{
  'item': {
    'name': 'item #1',
    'code': 'itm-123',
    'image': {
      '@url': 'http://www.foo.com/bar.jpg'
    }
  }
}";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json))
            {
                //foreach (var rec in p)
                //	Console.WriteLine(rec.Dump());
                //Console.WriteLine(ChoXmlWriter.ToTextAll(p, new ChoXmlRecordConfiguration().Configure(c => c.IgnoreRootName = true).Configure(c => c.IgnoreNodeName = true)));

                using (var w = new ChoXmlWriter(sb)
                    //.Configure(c => c.IgnoreRootName = true)
                    //.Configure(c => c.IgnoreNodeName = true)
                    .WithDefaultXmlNamespace("x1", "http://unknwn")
                    )
                {
                    w.Write(p);
                }
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample25()
        {
            string expected = @"Value
2017-02-11
2017-02-12";
            string actual = null;

            string json = @"
{
 ""2017-02-11"":
  {
  ""Table1"": [
    {
      ""code"": ""code day1.1.1"",
      ""no"": ""no day1.1.1""
    }
  ],
  ""Table2"": [
    {
      ""code"": ""code day1.2.1"",
      ""no"": ""no day1.2.1""
    },
    {
      ""code"": ""code day1.2.2"",
      ""no"": ""no day1.2.2""
    }
  ]
 },
 ""2017-02-12"":
  {
  ""Table1"": [
    {
      ""code"": ""code day2.1.1"",
      ""no"": ""no day2.1.1""
    },
    {
      ""code"": ""code day2.1.2"",
      ""no"": ""no day2.1.2""
    }
  ],
  ""Table2"": [
    {
      ""code"": ""code day2.2.1"",
      ""no"": ""no day2.2.1""
    }
  ]
 }
}";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json))
            {
                //foreach (var rec in p)
                //	Console.WriteLine(rec.Dump());
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(p.SelectMany(r => (string[])r.KeysArray));
                    //var x = p.Select(r => r.Keys);
                    //Console.WriteLine(x.Dump());
                    //foreach (var rec in p)
                    //	w.Write(ChoUtility.Transpose(rec));
                }
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample24()
        {
            string expected = @"[
  {
    ""ref"": ""ABC123456"",
    ""pickcompname"": ""ABC Company"",
    ""gw"": 123.45,
    ""packaging"": [
      {
        ""qty"": 5,
        ""unit"": ""C""
      },
      {
        ""qty"": 7,
        ""unit"": ""L""
      }
    ]
  }
]";
            string actual = null;

            StringBuilder sb = new StringBuilder();
            using (var p = new ChoJSONReader(FileNameSample16JSON))
            {
                using (var w = new ChoJSONWriter(sb)
                    )
                {
                    w.Write(p);
                }
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample23()
        {
            string expected = @"<Root>
  <XElement>
    <firstName>John</firstName>
    <lastName>Smith</lastName>
    <age>25</age>
    <address>
      <streetAddress>21 2nd Street</streetAddress>
      <city>New York</city>
      <state>NY</state>
      <postalCode>10021</postalCode>
    </address>
    <phoneNumber>
      <type>home</type>
      <number>212 555-1234</number>
    </phoneNumber>
    <phoneNumber>
      <type>fax</type>
      <number>646 555-4567</number>
    </phoneNumber>
  </XElement>
  <XElement>
    <firstName>Tom</firstName>
    <lastName>Mark</lastName>
    <age>50</age>
    <address>
      <streetAddress>10 Main Street</streetAddress>
      <city>Edison</city>
      <state>NJ</state>
      <postalCode>08837</postalCode>
    </address>
    <phoneNumber>
      <type>home</type>
      <number>732 555-1234</number>
    </phoneNumber>
    <phoneNumber>
      <type>fax</type>
      <number>609 555-4567</number>
    </phoneNumber>
  </XElement>
</Root>";
            string actual = null;

            string json = @"[
    {
        ""firstName"": ""John"",
        ""lastName"": ""Smith"",
        ""age"": 25,
        ""address"": {
            ""streetAddress"": ""21 2nd Street"",
            ""city"": ""New York"",
            ""state"": ""NY"",
            ""postalCode"": ""10021""
        },
        ""phoneNumber"": [
            {
                ""type"": ""home"",
                ""number"": ""212 555-1234""
            },
            {
                ""type"": ""fax"",
                ""number"": ""646 555-4567""
            }
        ]
    },
    {
        ""firstName"": ""Tom"",
        ""lastName"": ""Mark"",
        ""age"": 50,
        ""address"": {
            ""streetAddress"": ""10 Main Street"",
            ""city"": ""Edison"",
            ""state"": ""NJ"",
            ""postalCode"": ""08837""
        },
        ""phoneNumber"": [
            {
                ""type"": ""home"",
                ""number"": ""732 555-1234""
            },
            {
                ""type"": ""fax"",
                ""number"": ""609 555-4567""
            }
        ]
    }
]";
            //ChoDynamicObjectSettings.XmlArrayQualifier = (key, obj) =>
            //{
            //    if (key == "address"
            //    || key == "phoneNumber")
            //        return false;

            //    return null;
            //};

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
                )
            {
                //using (var w = new ChoCSVWriter(sb)
                //	.WithFirstLineHeader()
                //	)
                //	w.Write(p);
                using (var w = new ChoXmlWriter(sb)
                    .Configure(c => c.DoNotEmitXmlNamespace = true)
                    .Configure(c => c.XmlArrayQualifier = (key, obj) =>
                    {
                        if (key == "address"
                        || key == "phoneNumber")
                            return false;

                        return null;
                    })
                    )
                    w.Write(p);
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample22()
        {
            string expected = @"item_1,item_2,item_3,item_4_0,item_4_1,item_4_2,item_4_3,item_4_4,item_5_sub_item_1,item_5_sub_item_2_0,item_5_sub_item_2_1
value_11,value_12,value_13,sub_value_14,sub_value_15,,,,sub_item_value_11,sub_item_value_12,sub_item_value_13
value_21,value_22,,sub_value_24,sub_value_25,sub_value_15,sub_value_15,sub_value_15,sub_item_value_21,sub_item_value_22,sub_item_value_23
value_21,value_22,,sub_value_24,sub_value_25,sub_value_15,sub_value_15,sub_value_15,sub_item_value_21,sub_item_value_22,sub_item_value_23
value_21,value_22,,sub_value_24,sub_value_25,,,,sub_item_value_21,sub_item_value_22,sub_item_value_23
value_21,value_22,,sub_value_24,sub_value_25,,,,sub_item_value_21,sub_item_value_22,sub_item_value_23
value_21,value_22,,sub_value_24,sub_value_25,,,,sub_item_value_21,sub_item_value_22,sub_item_value_23
value_21,value_22,,sub_value_24,sub_value_25,,,,sub_item_value_21,sub_item_value_22,sub_item_value_23";
            string actual = null;

            string json = @"
{
        ""node"":[
            {
                ""item_1"":""value_11"",
                ""item_2"":""value_12"",
                ""item_3"":""value_13"",
                ""item_4"":[""sub_value_14"", ""sub_value_15""],
                ""item_5"":{
                    ""sub_item_1"":""sub_item_value_11"",
                    ""sub_item_2"":[""sub_item_value_12"", ""sub_item_value_13""]
                }
            },
            {
                ""item_1"":""value_21"",
                ""item_2"":""value_22"",
                ""item_4"":[""sub_value_24"", ""sub_value_25"", ""sub_value_15"", ""sub_value_15"", ""sub_value_15""],
                ""item_5"":{
                    ""sub_item_1"":""sub_item_value_21"",
                    ""sub_item_2"":[""sub_item_value_22"", ""sub_item_value_23""]
                }
            },
            {
                ""item_1"":""value_21"",
                ""item_2"":""value_22"",
                ""item_4"":[""sub_value_24"", ""sub_value_25"", ""sub_value_15"", ""sub_value_15"", ""sub_value_15""],
                ""item_5"":{
                    ""sub_item_1"":""sub_item_value_21"",
                    ""sub_item_2"":[""sub_item_value_22"", ""sub_item_value_23""]
                }
            },
            {
                ""item_1"":""value_21"",
                ""item_2"":""value_22"",
                ""item_4"":[""sub_value_24"", ""sub_value_25""],
                ""item_5"":{
                    ""sub_item_1"":""sub_item_value_21"",
                    ""sub_item_2"":[""sub_item_value_22"", ""sub_item_value_23""]
                }
            },
            {
                ""item_1"":""value_21"",
                ""item_2"":""value_22"",
                ""item_4"":[""sub_value_24"", ""sub_value_25""],
                ""item_5"":{
                    ""sub_item_1"":""sub_item_value_21"",
                    ""sub_item_2"":[""sub_item_value_22"", ""sub_item_value_23""]
                }
            },
            {
                ""item_1"":""value_21"",
                ""item_2"":""value_22"",
                ""item_4"":[""sub_value_24"", ""sub_value_25""],
                ""item_5"":{
                    ""sub_item_1"":""sub_item_value_21"",
                    ""sub_item_2"":[""sub_item_value_22"", ""sub_item_value_23""]
                }
            },
            {
                ""item_1"":""value_21"",
                ""item_2"":""value_22"",
                ""item_4"":[""sub_value_24"", ""sub_value_25""],
                ""item_5"":{
                    ""sub_item_1"":""sub_item_value_21"",
                    ""sub_item_2"":[""sub_item_value_22"", ""sub_item_value_23""]
                }
            }
        ]
    }";
            StringBuilder csv = new StringBuilder();
            using (var p = new ChoJSONReader(new StringReader(json))
                .WithJSONPath("$..node")
                )
            {
                using (var w = new ChoCSVWriter(new StringWriter(csv))
                    .WithFirstLineHeader()
                    .Configure(c => c.MaxScanRows = 2)
                    .Configure(c => c.ThrowAndStopOnMissingField = false)
                    )
                {
                    w.Write(p);
                }
            }

            actual = csv.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample21()
        {
            string expected = @"[
  {
    ""Name"": ""Xytrex Co."",
    ""Description"": ""Industrial Cleaning Supply Company"",
    ""Account Number"": ""ABC15797531""
  },
  {
    ""Name"": ""Watson and Powell Inc."",
    ""Description"": ""Law firm. New York Headquarters"",
    ""Account Number"": ""ABC24689753""
  }
]";
            string actual = null;

            string csv = @"Name,Description,Account Number
Xytrex Co.,Industrial Cleaning Supply Company,ABC15797531
Watson and Powell Inc.,Law firm. New York Headquarters,ABC24689753";

            StringBuilder json = new StringBuilder();
            using (var p = new ChoCSVReader(new StringReader(csv))
                    .WithFirstLineHeader()
                )
            {
                using (var w = new ChoJSONWriter(json))
                {
                    w.Write(p);
                }
            }

            actual = json.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample20()
        {
            string expected = @"Account_Number
ABC15797531
ABC24689753";
            string actual = null;

            string json = @"
            [
               {
                  ""Name"" : ""Xytrex Co."",
                  ""Description"" : ""Industrial Cleaning Supply Company"",
                  ""Account Number"" : ""ABC15797531""
               },
               {
                  ""Name"" : ""Watson and Powell Inc."",
                  ""Description"" : ""Law firm. New York Headquarters"",
                  ""Account Number"" : ""ABC24689753""     
               }
            ]";
            StringBuilder csv = new StringBuilder();
            using (var p = new ChoJSONReader(new StringReader(json)))
            {
                using (var w = new ChoCSVWriter(new StringWriter(csv))
                    .WithFirstLineHeader()
                    .WithField("Account Number", fieldName: "Account_Number")
                    )
                {
                    w.Write(p);
                }
            }

            actual = csv.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample19()
        {
            var expected = new[]
            {
                new {pickcompname = "ABC Company", qty = 5},
                new {pickcompname = "ABC Company", qty = 7}
            };
            /*            dynamic expectedDyn1 = new ExpandoObject();
                        expectedDyn1.pickcompname = "ABC Company";
                        expectedDyn1.qty = 5;
                        dynamic expectedDyn2 = new ExpandoObject();
                        expectedDyn2.pickcompname = "ABC Company";
                        expectedDyn2.qty = 7;
                        IEnumerable<object> expected = new object[] { expectedDyn1, expectedDyn2 };*/
            var actual = expected;

            //            expectedDyn.dasdf  
            //            object[] = new object();

            using (var p = new ChoJSONReader(FileNameSample19JSON))
            {
                actual = p.SelectMany(p1 => ((dynamic[])p1.packaing).Select(p2 => new { pickcompname = (string)p1.pickcompname, qty = (int)p2.qty })).ToArray();
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class MarketData
        {
            [ChoJSONRecordField(JSONPath = @"['Meta Data']")]
            public MetaData MetaData { get; set; }

            [ChoJSONRecordField(JSONPath = @"$..['Stock Quotes'][*]")]
            public List<StockQuote> StockQuotes { get; set; }

            public override bool Equals(object obj)
            {
                var data = obj as MarketData;
                return data != null &&
                       EqualityComparer<MetaData>.Default.Equals(MetaData, data.MetaData) &&
                       new ListEqualityComparer<StockQuote>().Equals(StockQuotes, data.StockQuotes);
            }

            public override int GetHashCode()
            {
                var hashCode = 1506236156;
                hashCode = hashCode * -1521134295 + EqualityComparer<MetaData>.Default.GetHashCode(MetaData);
                hashCode = hashCode * -1521134295 + new ListEqualityComparer<StockQuote>().GetHashCode(StockQuotes);
                return hashCode;
            }
        }

        public class MetaData
        {
            [JsonProperty(PropertyName = "1. Information")]
            //[ChoJSONRecordField(JSONPath = @"['Meta Data']['1. Information']")]
            public string Information { get; set; }
            [JsonProperty(PropertyName = "2. Notes")]
            public string Notes { get; set; }
            [JsonProperty(PropertyName = "3. Time Zone")]
            public string TimeZone { get; set; }

            public override bool Equals(object obj)
            {
                var data = obj as MetaData;
                return data != null &&
                       Information == data.Information &&
                       Notes == data.Notes &&
                       TimeZone == data.TimeZone;
            }

            public override int GetHashCode()
            {
                var hashCode = -900408703;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Information);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Notes);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TimeZone);
                return hashCode;
            }
        }

        public class StockQuote
        {
            [JsonProperty(PropertyName = "1. symbol")]
            public string Symbol { get; set; }
            [JsonProperty(PropertyName = "2. price")]
            public double Price { get; set; }
            [JsonProperty(PropertyName = "3. volume")]
            public int Volumne { get; set; }
            [JsonProperty(PropertyName = "4. timestamp")]
            public DateTime Timestamp { get; set; }

            public override bool Equals(object obj)
            {
                var quote = obj as StockQuote;
                return quote != null &&
                       Symbol == quote.Symbol &&
                       Price == quote.Price &&
                       Volumne == quote.Volumne &&
                       Timestamp == quote.Timestamp;
            }

            public override int GetHashCode()
            {
                var hashCode = -1276399733;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Symbol);
                hashCode = hashCode * -1521134295 + Price.GetHashCode();
                hashCode = hashCode * -1521134295 + Volumne.GetHashCode();
                hashCode = hashCode * -1521134295 + Timestamp.GetHashCode();
                return hashCode;
            }
        }

        public static string FileNameSample1TestCSV => "Sample1Test.csv";
        public static string FileNameSample1ExpectedCSV => "Sample1Expected.csv";
        public static string FileNameSample1JSON => "sample1.json";
        public static string FileNameSample2JSON => "sample2.json";
        public static string FileNameSample2TestCSV => "Sample2Test.csv";
        public static string FileNameSample2ExpectedCSV => "Sample2Expected.csv";
        public static string FileNameSample3JSON => "sample3.json";
        public static string FileNameSample3TestXML => "sample3Test.xml";
        public static string FileNameSample3ExpectedXML => "sample3Expected.xml";
        public static string FileNameSample4JSON => "sample4.json";
        public static string FileNameSample4TestCSV => "sample4Test.csv";
        public static string FileNameSample4ExpectedCSV => "sample4Expected.csv";
        public static string FileNameSample5JSON => "sample5.json";
        public static string FileNameSample6JSON => "sample6.json";
        public static string FileNameSample7JSON => "sample7.json";
        public static string FileNameSample8JSON => "sample8.json";
        public static string FileNameSample9JSON => "sample9.json";
        public static string FileNameSample10JSON => "sample10.json";
        public static string FileNameSample12JSON => "sample12.json";
        public static string FileNameSample14JSON => "sample14.json";
        public static string FileNameSample15JSON => "sample15.json";
        public static string FileNameSample16JSON => "sample16.json";
        public static string FileNameSample16TestCSV => "sample16Test.csv";
        public static string FileNameSample16ExpectedCSV => "sample16Expected.csv";
        public static string FileNameSample17JSON => "sample17.json";
        public static string FileNameSample18JSON => "sample18.json";
        public static string FileNameSample18TestCSV => "sample18Test.csv";
        public static string FileNameSample18ExpectedCSV => "sample18Expected.csv";
        public static string FileNameSample19JSON => "sample19.json";
        public static string FileNameSample21JSON => "sample21.json";
        public static string FileNameSample22JSON => "sample22.json";
        public static string FileNameSample23JSON => "sample23.json";
        public static string FileNameSample24JSON => "sample24.json";
        public static string FileNameSample25JSON => "sample25.json";
        public static string FileNameSample26JSON => "sample26.json";
        public static string FileNameSample27JSON => "sample27.json";
        public static string FileNameSample28JSON => "sample28.json";
        public static string FileNameSample29JSON => "sample29.json";
        public static string FileNameSample31JSON => "sample31.json";
        public static string FileNameSample32JSON => "sample32.json";
        public static string FileNameSample32TestCSV => "sample32Test.csv";
        public static string FileNameSample32ExpectedCSV => "sample32Expected.csv";
        public static string FileNameSample33JSON => "sample33.json";
        public static string FileNameSample33TestCSV => "sample33Test.csv";
        public static string FileNameSample33ExpectedCSV => "sample33Expected.csv";
        public static string FileNameSample100JSON => "sample100.json";
        public static string FileNameColorsJSON => "colors.json";
        public static string FileNameEmpJSON => "Emp.json";

        [Test]
        public static void Sample17()
        {
            List<object> expected = new List<object>
            {
                new MarketData{
                    MetaData = new MetaData{
                        Information = "Batch Stock Market Quotes",
                        Notes = "IEX Real-Time Price provided for free by IEX (https://iextrading.com/developer/).",
                        TimeZone = "US/Eastern" },
                    StockQuotes = new List<StockQuote>{
                        new StockQuote { Symbol = "MSFT", Price = 96.18, Volumne = 20087326, Timestamp = new DateTime(2018,3,9,13,53,7) },
                        new StockQuote { Symbol = "AMD", Price = 11.68, Volumne = 63025764, Timestamp = new DateTime(2018,3,9,13,53,8) },
                        new StockQuote { Symbol = "NVDA", Price = 243.96, Volumne = 8649187, Timestamp = new DateTime(2018,3,9,13,52,51) }
                    }
                }
            };
            List<object> actual = new List<object>();

            using (var p = new ChoJSONReader<MarketData>(FileNameSample17JSON)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample16()
        {
            using (var p = new ChoJSONReader(FileNameSample16JSON)
                .WithField("Ref", jsonPath: "$..ref", fieldType: typeof(string))
                .WithField("pickcompname", jsonPath: "$..pickcompname", fieldType: typeof(string))
                .WithField("gw", jsonPath: "$..gw", fieldType: typeof(double))
                .WithField("qty1", jsonPath: "$..packaging[0].qty", fieldType: typeof(int))
                .WithField("unit1", jsonPath: "$..packaging[0].unit", fieldType: typeof(string))
                .WithField("qty2", jsonPath: "$..packaging[1].qty", fieldType: typeof(int))
                .WithField("unit2", jsonPath: "$..packaging[1].unit", fieldType: typeof(string))
                )
            {
                using (var c = new ChoCSVWriter(FileNameSample16TestCSV).WithFirstLineHeader())
                    c.Write(p);
            }

            FileAssert.AreEqual(FileNameSample16ExpectedCSV, FileNameSample16TestCSV);
        }

        [Test]
        public static void Sample15()
        {
            Dictionary<string, string>[] expected = new Dictionary<string, string>[]
            {
                new Dictionary<string, string>(){ {"eventNumber", "40262-1" }, { "startDate", "Tuesday, December 12, 2017" }, { "eventType", "Corporate" } },
                new Dictionary<string, string>(){ {"eventNumber", "14361-1" }, { "startDate", "Monday, October 23, 2017" }, { "eventType", "School" } },
                new Dictionary<string, string>(){ {"eventNumber", "5014-1" }, { "startDate", "Friday, October 13, 2017" }, { "eventType", "Birthday" } }
            };
            IEnumerable actual = null;

            using (var p = new ChoJSONReader(FileNameSample15JSON)
                .WithField("header", jsonPath: "$..header[*]", fieldType: typeof(string[]))
                .WithField("results", jsonPath: "$..results[*]", fieldType: typeof(List<string[]>))
                )
            {
                var rec = p.FirstOrDefault();
                string[] header = rec.header;
                List<string[]> results = rec.results;

                actual = results.Select(a => header.Zip(a, (k, v) => new { Key = k, Value = v }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)).ToArray();
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample14()
        {
            List<object> expected = new List<object> {
                new ChoDynamicObject {
                    { "USD",  new object[] { new ChoDynamicObject { { "curveDefinitionId", "FCC" }, { "curveFamilyId", "value" }, { "curveName", "value" }, { "marketDataSet", "value" }, { "referenced", false } }, new ChoDynamicObject { { "curveDefinitionId", "FCC" }, { "curveFamilyId", "value" }, { "curveName", "value" }, { "marketDataSet", "value" }, { "referenced", false } } } },
                    { "EUR", new object[] { new ChoDynamicObject { { "curveDefinitionId", "FCC" }, { "curveFamilyId", "EUR/EURCURVE" }, { "curveName", "EURCURVE" }, { "marketDataSet", "FCC-IRCUBE" }, { "referenced", false } } } },
                    { "GBP" , new object[]{new ChoDynamicObject[] { new ChoDynamicObject { { "curveDefinitionId", "FCC" }, { "curveFamilyId", "value" }, { "curveName", "value" }, { "marketDataSet", "value" }, { "referenced", false } } } } }
                }
            };
            List<object> actual = new List<object>();

            using (var p = new ChoJSONReader(FileNameSample14JSON)
                .WithField("USD", jsonPath: "$..USD.FCC-IRCUBE[*]")
                .WithField("EUR", jsonPath: "$..EUR.FCC-IRCUBE[*]")
                .WithField("GBP", jsonPath: "$..GBP.FCC-IRCUBE")
            )
            {
                foreach (dynamic rec in p)
                {
                    actual.Add(rec);

                    Console.WriteLine("USD:");
                    Console.WriteLine();
                    foreach (var curr in rec.USD)
                    {
                        Console.WriteLine(curr.ToString());
                    }
                    Console.WriteLine();

                    foreach (var curr in rec.EUR)
                    {
                        Console.WriteLine(curr.ToString());
                    }
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample14b()
        {
            List<object> expected = new List<object> {
                new ChoDynamicObject {
                    { "USD",  new IRCUBE[] { new IRCUBE { CurveDefinitionId= "FCC" , CurveFamilyId =  "value" , CurveName =  "value" , MarketDataSet = "value" ,  Referenced = "False" } , new IRCUBE { CurveDefinitionId= "FCC" , CurveFamilyId="value" , CurveName="value" , MarketDataSet= "value" , Referenced= "False" } } },
                    { "EUR", new IRCUBE[] { new IRCUBE { CurveDefinitionId="FCC" , CurveFamilyId= "EUR/EURCURVE" , CurveName= "EURCURVE" , MarketDataSet = "FCC-IRCUBE" , Referenced = "False" } } },
                    { "GBP" , new IRCUBE[]{ new IRCUBE { CurveDefinitionId="FCC" , CurveFamilyId="value" , CurveName="value" , MarketDataSet ="value" , Referenced = "False" } } }
                }
            };
            List<object> actual = new List<object>();


            using (var p = new ChoJSONReader(FileNameSample14JSON)
            .WithField("USD", jsonPath: "$..USD.FCC-IRCUBE[*]", fieldType: typeof(IRCUBE[]))
            .WithField("EUR", jsonPath: "$..EUR.FCC-IRCUBE[*]", fieldType: typeof(IRCUBE[]))
            .WithField("GBP", jsonPath: "$..GBP.FCC-IRCUBE", fieldType: typeof(IRCUBE[]))
                )
            {
                foreach (dynamic rec in p)
                {
                    actual.Add(rec);

                    Console.WriteLine("USD:");
                    Console.WriteLine();
                    foreach (var curr in rec.USD)
                    {
                        Console.WriteLine(curr.ToString());
                    }
                    Console.WriteLine();
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample13()
        {
            List<object> expected = new List<object> {
                "Open"
            };
            List<object> actual = new List<object>();

            using (var p = new ChoJSONReader(FileNameSample3JSON)
                //.WithField("details_attributes", jsonPath: "$..details_attributes", fieldType: typeof(ChoDynamicObject))
                )
            {
                foreach (dynamic rec in p)
                    actual.Add(rec.menu.popup.menuitem[1].value);
                //                    Console.WriteLine(rec.menu.popup.menuitem[1].value);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample12()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"name","Data1"},{"id",1 }, { "last", "0.00000045" } },
                new ChoDynamicObject{{"name","Data2"},{"id",2 }, { "last", "0.02351880" } }
            };
            List<object> actual = new List<object>();

            using (var jr = new ChoJSONReader(FileNameSample12JSON)
                )
            {
                foreach (var x1 in jr)
                {
                    foreach (var z1 in x1)
                    {
                        dynamic newObj = new ChoDynamicObject();
                        newObj.name = z1.Key;

                        foreach (var kvp in (ChoDynamicObject)z1.Value)
                            ((ChoDynamicObject)newObj).AddOrUpdate(kvp.Key, kvp.Value);

                        actual.Add(newObj);
                        //                        Console.WriteLine(ChoUtility.DumpAsJson(newObj));
                    }
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample11()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"TotalCount",4}, {"Url",new string[] { "file1.jpg", "file2.jpg", "file3.jpg", "file4.jpg" } } }
            };
            List<object> actual = new List<object>();

            string j1 = @"{
    ""images"":{
         ""totalCount"":4,
         ""0"":{
                ""url"":""file1.jpg""
         },
         ""1"":{
                ""url"":""file2.jpg""
         },
         ""2"":{
                ""url"":""file3.jpg""
        },
        ""3"":{
                ""url"":""file4.jpg""
        }
        }
    }";
            using (var jr = ChoJSONReader.LoadText(j1)
                .WithField("TotalCount", jsonPath: "$..totalCount", fieldType: typeof(int))
                .WithField("Url", jsonPath: "$..url", fieldType: typeof(string[]))
                )
            {
                foreach (var x in jr)
                    actual.Add(x);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample10()
        {
            List<object> expected = new List<object>
            {
                new Filter { filterName = "Is Active", filterValue = "True", view = "Demo/UsersbyFunction", filterformattedValue = "True" },
                new Filter { filterName = "Sbg", filterValue = "PMT", view = "Demo/UsersbyFunction", filterformattedValue = "PMT" },
                new Filter { filterName = "Sbg", filterValue = "SPS", view = "Demo/UsersbyFunction", filterformattedValue = "SPS" },
                new Filter { filterName = "Sbg", filterValue = "CORP", view = "Demo/UsersbyFunction", filterformattedValue = "CORP" },
                new Filter { filterName = "Sbg", filterValue = "PMT", view = "Demo/UsersbyFunction", filterformattedValue = "PMT" },
                new Filter { filterName = "Sbg", filterValue = "SPS", view = "Demo/UsersbyFunction", filterformattedValue = "SPS" }
            };

            List<object> actual = new List<object>();

            using (var jr = new ChoJSONReader<Filter>(FileNameSample10JSON)
                .WithJSONPath("$..[*][*]", true)
                )
            {
                foreach (var x in jr)
                {
                    actual.Add(x);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample9()
        {
            List<object> expected = new List<object>
            {
                new Book { Author = "Nigel Rees", Category = "Reference", Price = 8.88, Title = "Sayings of the Century"},
                new Book { Author = "Evelyn Waugh", Category = "Fiction", Price = 12.66, Title = "Sword of Honour"}
            };
            List<object> actual = new List<object>();

            using (var jr = new ChoJSONReader<Book>(FileNameSample9JSON).WithJSONPath("$..book")
            )
            {
                foreach (var x in jr)
                {
                    actual.Add(x);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample9_1()
        {
            List<object> expected = new List<object>
            {
                new Book { Author = "Nigel Rees", Category = "Reference", Price = 8.88, Title = "Sayings of the Century"},
                new Book { Author = "Evelyn Waugh", Category = "Fiction", Price = 12.66, Title = "Sword of Honour"},
                new Book { Author = "Nigel Rees", Category = "Reference", Price = 8.88, Title = "Sayings of the Century"},
                new Book { Author = "Evelyn Waugh", Category = "Fiction", Price = 12.66, Title = "Sword of Honour"},
            };
            List<object> actual = new List<object>();

            using (var jr = new ChoJSONReader<Book>(FileNameSample9JSON).WithJSONPath("$..book[*]", true)
            )
            {
                foreach (var x in jr)
                {
                    actual.Add(x);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void SerializeToKeyValuePair1()
        {
            List<object> expected = new List<object>
            {
                new DataMapper{
                    Name = "performanceLevels", SubDataMappers = new List<DataMapper>{
                        new DataMapper{ Name = "performanceLevel_1", SubDataMappers = new List<DataMapper>{
                            new DataMapper { Name = "title", DataMapperProperty =
                            new DataMapperProperty { DataType = "string", Source = "column", SourceColumn = "title-column", Default = "N/A" }},
                            new DataMapper { Name = "version1", DataMapperProperty =
                            new DataMapperProperty {DataType = "int", Source = "column", SourceColumn = "version-column", Default = "1"}},
                            new DataMapper { Name = "threeLevels", SubDataMappers = new List<DataMapper>
                            {
                                new DataMapper{Name = "version", DataMapperProperty = new DataMapperProperty { DataType = "int", Source = "column", SourceColumn = "version-column", Default = "1"}}
                            } } } },
                        new DataMapper{ Name = "performanceLevel_2", SubDataMappers = new List<DataMapper> { new DataMapper { Name = "title", DataMapperProperty = new DataMapperProperty { DataType = "string", Source = "column", SourceColumn = "title-column", Default = "N/A" } },
                        new DataMapper{ Name = "version", DataMapperProperty = new DataMapperProperty{DataType = "int", Source = "column", SourceColumn = "version-column", Default = "1" } } }
                        }
                    }
                }
            };
            List<object> actual = new List<object>();

            using (var jr = new ChoJSONReader<DataMapper>(FileNameSample8JSON))
            {
                foreach (var x in jr)
                {
                    actual.Add(x);
                }
            }

            actual.Print();
            expected.Print();
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample7()
        {
            List<object> expected = new List<object>
            {
                new Family{ Id = 0, Daughters = new List<FamilyMember>{ new FamilyMember { Name = "Amy", Age = 7}, new FamilyMember { Name = "Carol", Age =  29},new FamilyMember { Name = "Barbara", Age =  14} } },
                new Family{ Id = 1, Daughters = new List<FamilyMember>{ } },
                new Family{ Id = 2, Daughters = new List<FamilyMember>{ new FamilyMember { Name = "Elizabeth", Age =  7}, new FamilyMember { Name = "Betty", Age = 15 } } }
            };
            List<object> actual = new List<object>();

            using (var jr = new ChoJSONReader<Family>(FileNameSample7JSON).WithJSONPath("$.fathers"))
            {
                foreach (var x in jr)
                {
                    actual.Add(x);
                    /*                    Console.WriteLine(x.Id);
                                        foreach (var fm in x.Daughters)
                                            Console.WriteLine(fm);
                     */
                }
            }
            CollectionAssert.AreEqual(expected, actual);
            return;

            using (var jr = new ChoJSONReader("sample7.json").WithJSONPath("$.fathers")
                .WithField("id")
                .WithField("married")
                .WithField("name")
                .WithField("sons")
                .WithField("daughters", fieldType: typeof(Dictionary<string, object>[]))
                )
            {
                foreach (var item in jr)
                {
                    var x = item.id;
                    Console.WriteLine(x.GetType());

                    Console.WriteLine(item.id);
                    Console.WriteLine(item.married);
                    Console.WriteLine(item.name);
                    foreach (dynamic son in item.sons)
                    {
                        var x1 = son.address;
                        //Console.WriteLine(ChoUtility.ToStringEx(son.address.street));
                    }
                    foreach (var daughter in item.daughters)
                        Console.WriteLine(ChoUtility.ToStringEx(daughter));
                }
            }
        }

        [Test]
        public static void IgnoreItems()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {{"ProductId", "17213812"}, { "User", "Regular Guest"} },
                new ChoDynamicObject {{"ProductId", "17813832" } }
            };
            List<object> actual = new List<object>();

            using (var jr = new ChoJSONReader(FileNameSample6JSON)
                .WithField("ProductId", jsonPath: "$.productId", fieldType: typeof(string))
                .WithField("User", jsonPath: "$.returnPolicies.user", fieldType: typeof(string))
                .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Null)
                )
            {
                foreach (var item in jr)
                    actual.Add(item);// Console.WriteLine(item.ProductId + " " + item.User);
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void KVPTest()
        {
            List<object> expected = new List<object> { "OBJ1", "OBJ2" };
            List<object> actual = new List<object>();

            using (var jr = new ChoJSONReader<Dictionary<string, string>>(FileNameSample5JSON).Configure(c => c.UseJSONSerialization = true))
            {
                foreach (var dict1 in jr.Select(dict => dict.Select(kvp => new { kvp.Key, kvp.Value })).SelectMany(x => x))
                {
                    actual.Add(dict1.Key);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample4()
        {
            using (var jr = new ChoJSONReader<JObject>(FileNameSample4JSON).Configure(c => c.UseJSONSerialization = true))
            {
                using (var xw = new ChoCSVWriter(FileNameSample4TestCSV).WithFirstLineHeader())
                {
                    foreach (JObject jItem in jr)
                    {
                        dynamic item = jItem;
                        var identifiers = ChoEnumerable.AsEnumerable<JObject>(jItem).Select(e => ((IList<JToken>)((dynamic)e).identifiers).Select(i =>
                           new
                           {
                               identityText = i["identityText"].ToString(),
                               identityTypeCode = i["identityTypeCode"].ToString()
                           })).SelectMany(x => x).ToArray();

                        var members = ChoEnumerable.AsEnumerable<JObject>(jItem).Select(e => ((IList<JToken>)((dynamic)e).members).Select(m => ((IList<JToken>)((dynamic)m).identifiers).Select(i =>
                           new
                           {
                               dob = m["dob"].ToString(),
                               firstName = m["firstName"].ToString(),
                               gender = m["gender"].ToString(),
                               identityText = i["identityText"].ToString(),
                               identityTypeCode = i["identityTypeCode"].ToString(),
                               lastname = m["lastName"].ToString(),
                               memberId = m["memberId"].ToString(),
                               optOutIndicator = m["optOutIndicator"].ToString(),
                               relationship = m["relationship"].ToString()

                           }))).SelectMany(x => x).SelectMany(y => y).ToArray();

                        var comb = members.ZipEx(identifiers, (m, i) =>
                        {
                            if (i == null)
                                return new
                                {
                                    ccId = ChoUtility.ToStringEx(item.ccId),
                                    hId = ChoUtility.ToStringEx(item.hId),
                                    identifiers_identityText = String.Empty,
                                    identifiers_identityTypeCode = String.Empty,
                                    members_dob = m.dob,
                                    members_firstName = m.firstName,
                                    members_gender = m.gender,
                                    members_identifiers_identityText = m.identityText,
                                    members_identityTypeCode = m.identityTypeCode,
                                    members_lastname = m.lastname,
                                    members_memberid = m.memberId,
                                    member_optOutIndicator = m.optOutIndicator,
                                    member_relationship = m.relationship,
                                    SubscriberFirstName = ChoUtility.ToStringEx(item.subscriberFirstame),
                                    SubscriberLastName = ChoUtility.ToStringEx(item.subscriberLastName),

                                };
                            else
                                return new
                                {
                                    ccId = ChoUtility.ToStringEx(item.ccId),
                                    hId = ChoUtility.ToStringEx(item.hId),
                                    identifiers_identityText = i.identityText,
                                    identifiers_identityTypeCode = i.identityTypeCode,
                                    members_dob = m.dob,
                                    members_firstName = m.firstName,
                                    members_gender = m.gender,
                                    members_identifiers_identityText = m.identityText,
                                    members_identityTypeCode = m.identityTypeCode,
                                    members_lastname = m.lastname,
                                    members_memberid = m.memberId,
                                    member_optOutIndicator = m.optOutIndicator,
                                    member_relationship = m.relationship,
                                    SubscriberFirstName = ChoUtility.ToStringEx(item.subscriberFirstame),
                                    SubscriberLastName = ChoUtility.ToStringEx(item.subscriberLastName),
                                };

                        }).ToArray();
                        xw.Write(comb);
                    }
                }
            }

            //foreach (var e in jr.Select(i => new[] { i.ccId.ToString(), i.hId.ToString() }
            //.Concat(((IList<JToken>)i.identifiers).Select(jt => jt["identityText"].ToString()))
            //.Concat(((IList<JToken>)i.members).Select(jt => jt["dob"].ToString()))
            //)
            //)
            //    xw.Write(e.ToList().ToExpandoObject());

            //foreach (var e in jr.Select(i => new { i.ccId, i.hId, identityText = ((IList<JToken>)i.identifiers).Select(x => x["identityText"]) }))
            //{

            //}

            FileAssert.AreEqual(FileNameSample4ExpectedCSV, FileNameSample4TestCSV);
        }

        [Test]
        public static void Sample3()
        {
            using (var jr = new ChoJSONReader<MyObjectType>(FileNameSample3JSON).WithJSONPath("$.menu")
                )
            {
                jr.AfterRecordFieldLoad += (o, e) =>
                {
                };
                using (var xw = new ChoXmlWriter<MyObjectType>(FileNameSample3TestXML).Configure(c => c.UseXmlSerialization = true))
                    xw.Write(jr);
            }

            FileAssert.AreEqual(FileNameSample3ExpectedXML, FileNameSample3TestXML);
        }

        [Test]
        public static void Sample2()
        {
            using (var csv = new ChoCSVWriter(FileNameSample2TestCSV) { TraceSwitch = ChoETLFramework.TraceSwitchOff }.WithFirstLineHeader())
            {
                csv.Write(new ChoJSONReader(FileNameSample2JSON) { TraceSwitch = ChoETLFramework.TraceSwitchOff }
                .WithField("Base")
                .WithField("Rates", fieldType: typeof(IDictionary<string, object>))
                .Select(m => ((IDictionary<string, object>)m.Rates).Select(r => new { Base = m.Base, Key = r.Key, Value = r.Value })).SelectMany(m => m)
                );
            }

            FileAssert.AreEqual(FileNameSample2ExpectedCSV, FileNameSample2TestCSV);
        }

        //[Test]
        public static void Sample1()
        {
            using (var csv = new ChoCSVWriter(FileNameSample1TestCSV) { TraceSwitch = ChoETLFramework.TraceSwitchOff }.WithFirstLineHeader())
            {
                csv.Write(new ChoJSONReader(FileNameSample1JSON) { TraceSwitch = ChoETLFramework.TraceSwitchOff }.Select(e => Flatten(e)));
            }
            FileAssert.AreEqual(FileNameSample1ExpectedCSV, FileNameSample1TestCSV);
        }
        private static object[] Flatten(dynamic e)
        {
            List<object> list = new List<object>();
            list.Add(new { F1 = e.F1, F2 = e.F2, E1 = String.Empty, E2 = String.Empty, D1 = String.Empty, D2 = String.Empty });
            foreach (var se in e.F3)
            {
                if (se["E3"] != null)
                {
                    foreach (var de in se.E3)
                        list.Add(new { F1 = e.F1, F2 = e.F2, E1 = se.E1, E2 = se.E2, D1 = de.D1, D2 = de.D2 });
                }
                else
                    list.Add(new { F1 = e.F1, F2 = e.F2, E1 = se.E1, E2 = se.E2, D1 = String.Empty, D2 = String.Empty });
            }
            return list.ToArray();
        }

        private const string FileNameJsonToXmlJsonInput = "companies.json";
        private const string FileNameJsonToXmlExpectedXml = "companies.xml";
        private const string FileNameJsonToXmlActualXml = "companiesactual.xml";
        [Test]
        public static void JsonToXml()
        {
            //using (var r1 = new ChoJSONReader("companies.json"))
            //{
            //    using (var w1 = new ChoXmlWriter(Console.Out)
            //        .WithNodeName("Company")
            //        )
            //    {
            //        w1.Write(r1);
            //    }
            //}
            //return;

            StringBuilder xml = new StringBuilder();
            using (var w = new ChoXmlWriter(xml)
                .WithXPath("Companies/Company")
                )
            {
                w.Write(new ChoJSONReader(FileNameJsonToXmlJsonInput, new ChoJSONRecordConfiguration()
                    /*.Configure(c => c.MaxScanRows = 1)*/));
            }

            var actual = xml.ToString();
            var expected = File.ReadAllText(FileNameJsonToXmlExpectedXml);
            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual);
        }

        private const string FileNameJsonToCSVJsonInput = "companies.json";
        private const string FileNameJsonToCSVExpectedCSV = "companies.csv";
        private const string FileNameJsonToCSVActualCSV = "companies1.csv";

        [Test]
        public static void JsonToCSV()
        {
            //using (var r1 = ChoCSVReader.LoadText(csv).
            //    WithFirstLineHeader())
            //{
            //    using (var w1 = new ChoJSONWriter(Console.Out))
            //    {
            //        w1.Write(r1);
            //    }
            //}
            //Assert.Fail("File companies.json not found");

            StringBuilder outCSV = new StringBuilder();
            using (var csv = new ChoCSVWriter(FileNameJsonToCSVActualCSV)
                .WithFirstLineHeader())
            {
                csv.Write(new ChoJSONReader<Company>(FileNameJsonToCSVJsonInput)
                    .NotifyAfter(10000).Take(10).
                    SelectMany(c => c.Products.Touch().
                    Select(p => new { c.name, c.Permalink, prod_name = p.name, prod_permalink = p.Permalink })));
            }
            //Assert.Fail("Write appropriate test: maybe the following");
            FileAssert.AreEqual(FileNameJsonToCSVExpectedCSV, FileNameJsonToCSVActualCSV);
        }

        //[Test]
        public static void LoadTest()
        {
            Assert.Fail("File companies.json not found");
            using (var p = new ChoJSONReader<Company>("companies.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.NotifyAfter(10000))
            {
                p.Configuration.ColumnCountStrict = true;
                foreach (var e in p)
                    Console.WriteLine("overview: " + e.name);
            }

            Assert.Fail("Write appropriate test: maybe the following");
            //CollectionAssert.AreEqual(expected, actual);
        }

        public class Product
        {
            [ChoJSONRecordField]
            public string name { get; set; }
            [ChoJSONRecordField]
            public string Permalink { get; set; }

            public override bool Equals(object obj)
            {
                var rec = obj as Product;
                return rec != null &&
                       name == rec.name &&
                       Permalink == rec.Permalink;
            }

            public override int GetHashCode()
            {
                return new { name, Permalink }.GetHashCode();
            }
        }

        public class Company
        {
            [ChoJSONRecordField]
            public string name { get; set; }
            [ChoJSONRecordField]
            public string Permalink { get; set; }
            [ChoJSONRecordField(JSONPath = "$.products")]
            public Product[] Products { get; set; }
        }
        [Test]
        public static void QuickLoad()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"Id",1},{"Name","Raj"}},
                new ChoDynamicObject{{"Id",2},{"Name","Tom"}}
            };
            List<object> actual = new List<object>();

            foreach (dynamic e in new ChoJSONReader(FileNameEmpJSON))
                actual.Add(e);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void POCOTest()
        {
            List<EmployeeRec> expected = new List<EmployeeRec>
            {
                new EmployeeRec{ Id = 1, Name = "Raj", Dict = new Dictionary<string, string>() , Courses = new string[]{"Math","Tamil" } },
                new EmployeeRec{ Id = 2, Name = "Tom" }
            };
            expected[0].Dict.Add("key1", "value1");
            expected[0].Dict.Add("key2", "value2");

            List<object> actual = new List<object>();

            using (var parser = ChoJSONReader<EmployeeRec>.LoadText(EmpJSON))
            {
                actual.AddRange(parser.ToArray());
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void StorePOCOTest()
        {
            List<object> expected = new List<object>
            {
                new StoreRec { Name = "Acme Co", Products = new ProductRec[]{ new ProductRec { Name = "Anvil", Price = "50" } } },
                new StoreRec { Name = "Contoso", Products = new ProductRec[]{ new ProductRec { Name = "Elbow Grease", Price = "99.95" }, new ProductRec { Name = "Headlight Fluid", Price = "4" } } }
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONReader<StoreRec>(reader).WithJSONPath("$.Manufacturers"))
            {
                writer.WriteLine(Stores);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    actual.Add(rec);
                }

                CollectionAssert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void StorePOCONodeLoadTest()
        {
            List<object> expected = new List<object>
            {
                new StoreRec { Name = "Acme Co", Products = new ProductRec[]{ new ProductRec { Name = "Anvil", Price = "50" } } },
                new StoreRec { Name = "Contoso", Products = new ProductRec[]{ new ProductRec { Name = "Elbow Grease", Price = "99.95" }, new ProductRec { Name = "Headlight Fluid", Price = "4" } } }
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var jparser = new JsonTextReader(reader))
            {
                writer.WriteLine(Stores);

                writer.Flush();
                stream.Position = 0;

                var config = new ChoJSONRecordConfiguration() { UseJSONSerialization = true };
                object rec;
                using (var parser = new ChoJSONReader<StoreRec>(JObject.Load(jparser).SelectTokens("$.Manufacturers"), config))
                {
                    while ((rec = parser.Read()) != null)
                    {
                        actual.Add(rec);
                    }
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void QuickLoadTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"Name","Acme Co" }, { "Products", new Product[] { new Product {  name = "Anvil"} }} },
                new ChoDynamicObject{{"Name","Contoso" }, { "Products", new Product[] { new Product { name = "Elbow Grease" }, new Product { name = "Headlight Fluid" } } } }
            };
            List<object> actual = new List<object>();

            using (var parser = ChoJSONReader.LoadText(Stores).
                WithJSONPath("$.Manufacturers").WithField("Name", fieldType: typeof(string)).
                WithField("Products", fieldType: typeof(Product[])))
            {
                actual.AddRange(parser.ToArray());
            }
            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void QuickLoadSerializationTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"Name","Acme Co"},{ "Products", new object[] { new ChoDynamicObject { { "Name", "Anvil" }, { "Price", 50 } } } } },
                new ChoDynamicObject{{"Name","Contoso"},{ "Products", new object[] { new ChoDynamicObject { { "Name", "Elbow Grease" }, { "Price", 99.95 } }, new ChoDynamicObject { { "Name", "Headlight Fluid" }, { "Price", 4 } } } } }
            };
            List<object> actual = new List<object>();

            var config = new ChoJSONRecordConfiguration() { UseJSONSerialization = false };

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONReader(reader, config).WithJSONPath("$.Manufacturers"))
            {
                writer.WriteLine(Stores);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    actual.Add(rec);
                    Console.WriteLine(rec.ToStringEx());
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        public class EmployeeRec
        {
            public int Id
            {
                get;
                set;
            }
            public string Name
            {
                get;
                set;
            }
            public string[] Courses
            {
                get;
                set;
            }
            public Dictionary<string, string> Dict
            {
                get;
                set;
            }

            public override bool Equals(object obj)
            {
                var rec = obj as EmployeeRec;
                return rec != null &&
                       Id == rec.Id &&
                       Name == rec.Name &&
                       new ArrayEqualityComparer<string>().Equals(Courses, rec.Courses) &&
                       new DictionaryEqualityComparer<string, string>().Equals(Dict, rec.Dict);
            }

            public override int GetHashCode()
            {
                var hashCode = 2017795256;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + new ArrayEqualityComparer<string>().GetHashCode(Courses);
                hashCode = hashCode * -1521134295 + new DictionaryEqualityComparer<string, string>().GetHashCode(Dict);
                return hashCode;
            }

            public override string ToString()
            {
                return "{0}. {1}. Course Count: {2}. Dict Count: {3}".FormatString(Id, Name, Courses == null ? 0 : Courses.Length, Dict == null ? 0 : Dict.Count);
            }
        }
        public class ProductRec
        {
            public string Name
            {
                get;
                set;
            }
            public string Price
            {
                get;
                set;
            }

            public override bool Equals(object obj)
            {
                var rec = obj as ProductRec;
                return rec != null &&
                       Name == rec.Name &&
                       Price == rec.Price;
            }

            public override int GetHashCode()
            {
                var hashCode = -44027456;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Price);
                return hashCode;
            }
        }
        public class StoreRec
        {
            public string Name
            {
                get;
                set;
            }
            public ProductRec[] Products
            {
                get;
                set;
            }

            public override bool Equals(object obj)
            {
                var rec = obj as StoreRec;
                return rec != null &&
                       Name == rec.Name &&
                       new ArrayEqualityComparer<ProductRec>().Equals(Products, rec.Products);
            }

            public override int GetHashCode()
            {
                var hashCode = -347228509;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + new ArrayEqualityComparer<ProductRec>().GetHashCode(Products);
                return hashCode;
            }

            public override string ToString()
            {
                return "{0}. {1}.".FormatString(Name, Products == null ? 0 : Products.Length);
            }
        }
    }
}

