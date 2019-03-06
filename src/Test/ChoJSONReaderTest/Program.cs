using ChoETL;
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
using System.Xml.Serialization;

namespace ChoJSONReaderTest 
{
    public class Filter
    {
        public string filterName { get; set; }
        public string filterformattedValue { get; set; }
        public string filterValue { get; set; }
        public string view { get; set; }
    }

    public class Book
    {
        public string Category { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public double Price { get; set; }
    }

    public class DataMapper : IChoKeyValueType
    {
        public DataMapper()
        {
            SubDataMappers = new List<DataMapper>();
        }

        public string Name { get; set; }

        public DataMapperProperty DataMapperProperty { get; set; }

        public List<DataMapper> SubDataMappers { get; set; }

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

    class Program
    {
        public class FamilyMember
        {
            public int Age { get; set; }
            public string Name { get; set; }
        }

        public class Family
        {
            public int Id { get; set; }
            public ArrayList Daughters { get; set; }
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

        public static void Test()
        {
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
        private static string EmpJSON = @"    
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

        private static string Stores = @"{
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
  ]}            ";

        public class IRCUBE
        {
            public string CurveDefinitionId { get; set; }
            public string CurveFamilyId { get; set; }
            public string CurveName { get; set; }
            public string MarketDataSet { get; set; }
            public string Referenced { get; set; }

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
        }

        private static void GetKeyTest()
        {
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
                    Console.WriteLine(rec.Dump());
            }
        }

        static void Sample18()
        {
            using (var csv = new ChoCSVWriter("dev.csv").WithFirstLineHeader())
            {
                using (var json = new ChoJSONReader("sample18.json"))
                {
                    //var result = json.Select(a => a.data.sensors).ToArray();
                    csv.Write(json.Select(i => new
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

                        SensorsId = i.data.sensors.Length > 0 ? i.data.sensors[0].id : 0,
                        SensortAncestry = i.data.sensors.Length > 0 ? i.data.sensors[0].ancestry : null,
                        SensorName = i.data.sensors.Length > 0 ? i.data.sensors[0].name : null,
                        SensorDescription = i.data.sensors.Length > 0 ? i.data.sensors[0].description : null,
                        SensorUnit = i.data.sensors.Length > 0 ? i.data.sensors[0].unit : 0,
                        SensorCreatedAt = i.data.sensors.Length > 0 ? i.data.sensors[0].created_at : DateTime.MinValue,
                        SensorUpdated_at = i.data.sensors.Length > 0 ? i.data.sensors[0].updated_at : DateTime.MinValue,
                        SensorMeasurement_id = i.data.sensors.Length > 0 ? i.data.sensors[0].measurement_id : 0,
                        SensorUuid = i.data.sensors.Length > 0 ? i.data.sensors[0].uuid : null,
                        SensorValue = i.data.sensors.Length > 0 ? i.data.sensors[0].value : 0,
                        SensorRawValue = i.data.sensors.Length > 0 ? i.data.sensors[0].raw_value : 0,
                        SensorPrevValue = i.data.sensors.Length > 0 ? i.data.sensors[0].prev_value : 0,
                        SensorPrevRawValue = i.data.sensors.Length > 0 ? i.data.sensors[0].prev_raw_value : 0,

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
        }

        public class JSObject
        {
            [JsonProperty("name")]
            public string name { get; set; }
            [JsonProperty("width")]
            public int width { get; set; }
            [JsonProperty("height")]
            public int height { get; set; }
        }
        static void ArrayTest()
        {
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

            using (var p = ChoJSONReader< JSObject>.LoadText(json))
            {
                foreach (var rec in p.Where(r => r.name.Contains("4")))
                    Console.WriteLine(rec.Dump());
            }
        }

        static void JSONToXmlTest()
        {
            string json = @" {
  'Email': 'james@example.com',
  'Active': true,
  'CreatedDate': '2013-01-20T00:00:00Z',
  'Roles': [
    'User',
    'Admin'
  ]
 }";
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
                Console.WriteLine(ChoXmlWriter.ToText(p.First()));
            }


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
            [ChoTypeConverter(typeof(ChoEnumConverter), Parameters = "Description")]
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
            public bool Cotd { get; set; }

            [JsonProperty("cotd_at")]
            public object CotdAt { get; set; }

            [JsonProperty("original_sound")]
            public bool OriginalSound { get; set; }

            [JsonProperty("has_sound")]
            public bool HasSound { get; set; }

            [JsonProperty("recoub_to")]
            public int RecoubTo { get; set; }

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
        }

        public static void Sample25Test()
        {
            //using (var p = new ChoJSONReader("sample26.json"))
            //{
            //    foreach (var rec in p)
            //        Console.WriteLine(rec.Dump());
            //}

            //ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Description;
            var o = ChoJSONReader.Deserialize<CoubBig>("sample25.json", new ChoJSONRecordConfiguration() { SupportMultipleContent = true });
            Console.WriteLine(o.Dump());
        }

        static void LoadTest1()
        {
            var x = new ChoJSONReader(@"C:\Users\nraj39\Downloads\ratings.json").First();
            Console.WriteLine(x.Dump());
        }

        public class Event
        {
            //[ChoJSONRecordField(FieldName = "event_id")]
            //[JsonProperty("event_id")]
            public int EventId { get; set; }
            //[JsonProperty("event_name")]
            public string EventName { get; set; }
            //[JsonProperty("start_date")]
            public DateTime StartDate { get; set; }
            //[JsonProperty("end_date")]
            public DateTime EndDate { get; set; }
            //[ChoJSONRecordField(JSONPath = "$..guests[*]")]
            //[ChoJSONPath("$..guests[*]")]
            //[ChoUseJSONSerialization()]
            public List<Guest> Guests { get; set; }
        }

        public class Guest
        {
            //[JsonProperty("guest_id")]
            public string GuestId { get; set; }
            //[JsonProperty("first_name")]
            public string FirstName { get; set; }
            //[JsonProperty("last_name")]
            public string LastName { get; set; }
            //[JsonProperty("telephone")]
            public string Email { get; set; }
        }

        static void Test3()
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

            using (var p = ChoJSONReader<Event>.LoadText(json)
                .WithField(m => m.EventId, fieldName: "event_id")
                .WithField(m => m.Guests, jsonPath: "$..guests")
                .WithFieldForType<Guest>(m => m.GuestId)
                )
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void Sample29_1()
        {
            using (var r = new ChoJSONReader("sample29.json")
                .WithJSONPath("$.._data")
                .WithField("RollId", jsonPath: "$..Id.RollId", fieldType: typeof(string))
                .WithField("MType", jsonPath: "$..Data.MType", fieldType: typeof(string))
                )

            {
                foreach (var rec in r
                    )
                {
                    Console.WriteLine((string)rec.RollId);
                    Console.WriteLine((string)rec.MType);
                }
            }
        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;

            JSON2CSV();
        }

        static void JSON2CSV()
        {
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
                .WithJSONPath("$")
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    //.Configure(c => c.UseNestedKeyFormat = false)
                    //.Configure(c => c.NestedColumnSeparator = '/')
                    //.Configure(c => c.ThrowAndStopOnMissingField = false)
                    )
                {
                    w.Write(p);
                }
            }

            Console.WriteLine(csv.ToString());
        }

        public static void LargeJSON()
        {
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
                {
                    Console.WriteLine($"Name: {rec.name}, Id: {rec.id}");
                }
            }
        }

        public class ArmorPOCO
        {
            public int Armor { get; set; }
            public int Strenght { get; set; }
        }


        static void Sample28_1()
        {
            foreach (var rec in new ChoJSONReader<Dictionary<string, ArmorPOCO>>("sample28.json")
                )
            {
                Console.WriteLine(rec.Dump());
            }
        }

        public class Result
        {
            public int Id { get; set; }
            public int SportId { get; set; }
        }

        static void Sample27_1()
        {
            foreach (var rec in new ChoJSONReader<Result>("sample27.json")
                .WithJSONPath("results[*]")
                )
            {
                Console.WriteLine(rec.Dump());
            }
        }

        public class MyType
        {
            public string EnrityList { get; set; }
            public string KeyName { get; set; }
            public string Value { get; set; }
        }
        static void Sample26_2()
        {
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
                .WithJSONPath("$[*]")
                )
            {
                Console.WriteLine(rec.Dump());
            }
        }

        static void Sample26_1()
        {
            foreach (var rec in new ChoJSONReader("sample26.json")
                .WithJSONPath("..stationmeasurements")
                .WithField("regio", jsonPath: "regio", fieldType: typeof(string))
                .WithField("temperatureMin", jsonPath: "dayhistory.temperatureMin", fieldType: typeof(double))
                .WithField("temperatureMax", jsonPath: "dayhistory.temperatureMax", fieldType: typeof(double))
                .Where(r => r.regio == "Hoogeveen")
                )
                Console.WriteLine(rec.Dump());
        }

        public struct LevelBonus
        {
            public int Power;
            public int Mana;
            public int Strenght;
            public int Armor;
            public int Health;
        }

        static void DictTest()
        {
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
                    Console.WriteLine(rec.Dump());
            }
        }

        static void PartialJSONFileTest()
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
        }

        static void TestCountToppings()
        {
            using (var p = new ChoJSONReader("sample100.json")
                .WithJSONPath("$[*].toppings[*]")
                )
            {
                foreach (var rec in p.GroupBy(g => ((dynamic)g).Value).Select(g => new { g.Key, Count = g.Count() }))
                    Console.WriteLine(rec.Dump());
            }
        }

        public class ActionMessage
        {
            public string Action { get; set; }
            public DateTime Timestamp { get; set; }
            public string Url { get; set; }
            public string IP { get; set; }

        }
        static void LoadChildren()
        {
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
    .WithJSONPath("$.*")
    )
{
    foreach (var rec in p)
    {
        Console.WriteLine("action: " + rec.Action);
        Console.WriteLine("timestamp: " + rec.Timestamp);
        Console.WriteLine("url: " + rec.Url);
        Console.WriteLine("ip: " + rec.IP);
    }
}
        }

        public class Project : IChoRecordFieldSerializable
        {
            public Guid CustID { get; set; }
            public string Title { get; set; }
            public string CalendarID { get; set; }

            public bool RecordFieldDeserialize(object record, long index, string propName, ref object source)
            {
                return true;
            }

            public bool RecordFieldSerialize(object record, long index, string propName, ref object source)
            {
                return true;
            }
        }

        static void GuidTest()
        {
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
                    Console.WriteLine(rec.Dump());
            }
        }

        static void ReformatJSON()
        {
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
            Console.WriteLine(sb.ToString());
        }

        static void Bcp()
        {
            string connectionString = "*** DB Connection String ***";
            using (var p = new ChoCSVReader<EmployeeRec>("sample17.xml")
                .WithFirstLineHeader()
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

        static void XmlTypeTest()
        {
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

            var r = ChoJSONReader.DeserializeText(json);

            string xml = ChoXmlWriter.ToText(r.First(), new ChoXmlRecordConfiguration { EmitDataType = false });
            Console.WriteLine(xml);
            var r1 = ChoXmlReader.LoadText(xml).First();
            Console.WriteLine(r1.Dump());
        }

        public class RootObject2
        {
            public string name { get; set; }
            public Dictionary<string, object> settings { get; set; }
        }
        static void StringNodeTest()
        {
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
                    Console.WriteLine(rec.Dump());
            }
        }

        public class Error1
        {
            public int Code { get; set; }
            public Error1Message[] Msg { get; set; }
        }
        public class Error1Message
        {
            public string Message { get; set; }
            public int? Code { get; set; }
        }
        static void VarySchemas()
        {
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
                    foreach (var e in (JArray)o)
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
                    Console.WriteLine(rec.Dump());
            }
        }
        static void JSONToDataset()
        {
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
            DataSet ds = new DataSet();
            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
        }

        static void Sample24Test()
        {
            //var rec = ChoJSONReader.Deserialize("sample24.json");
            //Console.WriteLine(rec.Dump());

            //DataTable dt = new ChoJSONReader("sample24.json").Select(i => i.Flatten(new Func<string, string>(cn =>
            //{
            //    if (cn == "Scenario")
            //        return "1Scenario1";
            //    return cn;
            //}), null)).AsDataTable();

            DataTable dt = new ChoJSONReader("sample24.json").AsDataTable();
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
        }

        static void MSFTQuooteToCSV()
        {
            using (var p = new ChoJSONReader<StockQuotes>("sample23.json")
                )
            {
                foreach (var rec in p.ExpandToObjects<StockQuote1>())
                    Console.WriteLine(rec.Dump());
            }

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


        static void JsonToXmlSoap()
        {
            using (var p = new ChoJSONReader("sample22.json"))
            {
                var e = p.First();

                var xml = ChoXmlWriter.Serialize(e);
                Console.WriteLine(xml);

                var json = ChoJSONWriter.Serialize(e);
                Console.WriteLine(json);
            }
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

        static void JSON2XmlAndViceVersa()
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

            var plan = ChoJSONReader.DeserializeText<Plan>(json);
            Console.WriteLine(plan.Dump());
            var xml = ChoXmlWriter.Serialize(plan);
            Console.WriteLine(xml);
            plan = ChoXmlReader.DeserializeText<Plan>(xml);
            Console.WriteLine(plan.Dump());
            var json1 = ChoJSONWriter.Serialize(plan);
            Console.WriteLine(json1);
        }

        class Row
        {
            public string Foo { get; set; }
            public string Bar { get; set; }
        }

        static void Test2()
        {
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
                    Console.WriteLine(rec.Dump());
            }

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
        }

        public class RootObject1
        {
            public bool success { get; set; }
            public string terms { get; set; }
            public string privacy { get; set; }
            public int timestamp { get; set; }
            public string source { get; set; }
            public Quotes quotes { get; set; }
        }
        static void GetUSDEURTest()
        {
            var x = ChoJSONReader.Deserialize<RootObject1>("sample21.json");
            //, 
            //    new ChoJSONRecordConfiguration()
            //    .Configure(c => c.JSONPath = "$..quotes.USDEUR")
            //    ).First().Value;

            Console.WriteLine(x.Dump());
        }

        public class Detail
        {
            public string Name { get; set; }
            public string Job { get; set; }
        }

        static void Test1()
        {
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
            }
        }

        static void CountNodes()
        {
            string json = @"{
""package1"": {
    ""type"": ""envelope"",
    ""quantity"": 1,
    ""length"": 6,
    ""width"": 1,
    ""height"": 4
},
""package2"": {
    ""type"": ""box"",
    ""quantity"": 2,
    ""length"": 9,
    ""width"": 9,
    ""height"": 9
}
}";

            using (var p = ChoJSONReader.LoadText(json).WithJSONPath("$.*"))
            {
                Console.WriteLine(p.Count());
                //foreach (var rec in p)
                //    Console.WriteLine(rec.Dump());
            }
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
        }

        public enum Gender { Male, Female }
        public class SessionPerformanceStats
        {
            [JsonProperty(PropertyName = "attendance")]
            public AttendanceStatistics Attendance { get; set; }

            [JsonProperty(PropertyName = "pollsAndSurveys")]
            public PollsAndSurveysStatistics PollsAndSurveys { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public Gender Gender { get; set; }
        }

        static void Sample100()
        {
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
                .WithJSONPath("$.*")
                )
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }

        public class Attributes
        {
            public int id { get; set; }
            public string name { get; set; }
            public string list_type { get; set; }
            public List<string> attribute_list { get; set; }
        }
        static void ListOfStringTest()
        {
            string json = @"{
    ""id"":1047,
    ""name"":""City"",
    ""attribute_list"":[""RWC"",""HMO"",""SJ"",""Ensenada""],
    ""list_type"":1
}";

            using (var p = ChoJSONReader.LoadText(json))
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void ArrayToDataTableTest()
        {
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
            using (var p = ChoJSONReader.LoadText(json))
            {
                //var dt = p.Select(r => ((object[])r.Value).ToDictionary()).AsDataTable();
                DataTable dt = new DataTable();
                dt.Columns.Add("Column_1");
                dt.Columns.Add("C2");

                p.Select(r => ((object[])r.Value).ToDictionary()).Fill(dt);
            }
        }
        static void DataTableTest()
        {
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
                .WithJSONPath("$..data[*]")
                //.WithField("Val1")
                //.WithField("Val2")
                //.WithField("Val3")
                )
            {
                //foreach (var rec in r)
                //	Console.WriteLine(rec.Flatten().Dump());
                var dt = r.ToArray().Select(i => i.Flatten()).AsDataTable();
            }
        }

        public class Alert
        {
            public string alert { get; set; }
            public string riskcode { get; set; }
        }
        static void SelectiveFieldsTest()
        {
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
            using (var p = ChoJSONReader<Alert>.LoadText(json).WithJSONPath("$..alerts[*]"))
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }
        static void NSTest()
        {
            string json = @"{""ns3:Test_Service"" : {""@xmlns:ns3"":""http://www.CCKS.org/XRT/Form"",""ns3:fname"":""mark"",""ns3:lname"":""joye"",""ns3:CarCompany"":""saab"",""ns3:CarNumber"":""9741"",""ns3:IsInsured"":""true"",""ns3:safty"":[""ABS"",""AirBags"",""childdoorlock""],""ns3:CarDescription"":""test Car"",""ns3:collections"":[{""ns3:XYZ"":""1"",""ns3:PQR"":""11"",""ns3:contactdetails"":[{""ns3:contname"":""DOM"",""ns3:contnumber"":""8787""},{""ns3:contname"":""COM"",""ns3:contnumber"":""4564"",""ns3:addtionaldetails"":[{""ns3:description"":""54657667""}]},{""ns3:contname"":""gf"",""ns3:contnumber"":""123"",""ns3:addtionaldetails"":[{""ns3:description"":""123""}]}]}]}}";
            ////string json = @"{""Test_Service"" : {""fname"":""mark"",""lname"":""joye"",""CarCompany"":""saab"",""CarNumber"":""9741"",""IsInsured"":""true"",""safty"":[""ABS"",""AirBags"",""childdoorlock""],""CarDescription"":""test Car"",""collections"":[{""XYZ"":""1"",""PQR"":""11"",""contactdetails"":[{""contname"":""DOM"",""contnumber"":""8787""},{""contname"":""COM"",""contnumber"":""4564"",""addtionaldetails"":[{""description"":""54657667""}]},{""contname"":""gf"",""contnumber"":""123"",""addtionaldetails"":[{""description"":""123""}]}]}]}}";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json).Configure(c => c.SupportMultipleContent = true))
            {
                //Console.WriteLine(p.First().Dump());
                //return;
                using (var w = new ChoXmlWriter(sb)
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    )
                {
                    w.Write(p);
                }
            }

            Console.WriteLine(sb.ToString());
        }


        public class BookingInfo : IChoNotifyRecordFieldRead
        {
            [ChoJSONRecordField(JSONPath = "$.travel_class")]
            public string TravelClass { get; set; }
            [ChoJSONRecordField(JSONPath = "$.booking_code")]
            public string BookingCode { get; set; }

            public bool AfterRecordFieldLoad(object target, long index, string propName, object value)
            {
                throw new NotImplementedException();
            }

            public bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value)
            {
                throw new NotImplementedException();
            }

            public bool RecordFieldLoadError(object target, long index, string propName, object value, Exception ex)
            {
                throw new NotImplementedException();
            }
        }

        public class FlightInfo : IChoNotifyRecordRead
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
        }

        static void BookingInfoTest()
        {
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
            var x = ChoJSONReader<FlightInfo>.LoadText(json).Configure(c => c.SupportMultipleContent = false)
                .WithJSONPath("$.results[0].itineraries[0].outbound.flights")
                .FirstOrDefault();
            Console.WriteLine(x.Dump());
        }

        static void JsonToString()
        {
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

            Console.WriteLine(String.Join(" ", ChoJSONReader.LoadText(json).Select(r1 => r1.eng_trans)));

            //var x = ChoJSONReader.LoadText(json, null, new ChoJSONRecordConfiguration().Configure(c => c.JSONPath = "$.eng_trans")).Select(r => r.Value).ToArray();
            //Console.WriteLine(x.Dump());

        }

        static void Colors2DataTable()
        {
            using (var p = new ChoJSONReader("colors.json")
                .WithJSONPath("$.colors")
                //.WithField("color")
                //.WithField("category")
                )
            {
                var dt = p.AsDataTable();
                //foreach (var rec in p)
                //	Console.WriteLine(rec.Dump());
            }
        }

        static void Sample43()
        {
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
                .WithField("property1", jsonPath: "$.property1")
                .WithField("property2", jsonPath: "$.property2")
                .WithField("someArray", jsonPath: @"$.someArray[*][?(@.item2)]", isArray: true)
            )
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }

        public partial class MyNode
        {
            public long Param1 { get; set; }
            public string Param2 { get; set; }
            public object Param3 { get; set; }
        }

        static void Sample42()
        {
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
                Console.WriteLine(ChoJSONWriter.ToTextAll(p));
            }

        }

        static void Sample41()
        {
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
                var result = p.Select(r1 => (dynamic[])r1.Value).Select(r2 =>
                    ChoDynamicObject.FromKeyValuePairs(r2.Select(kvp => new KeyValuePair<string, object>(kvp.Key.ToString(), kvp.Value)))
                );

                Console.WriteLine(ChoJSONWriter.ToTextAll(result));
            }

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

        static void Sample40()
        {
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
                var x = p.ToArray();
                //Console.WriteLine(ChoJSONWriter<RootObject>.ToTextAll(p));
            }

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

        static void Sample39()
        {
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
            //using (var p = ChoJSONReader.LoadText(json)
            //	)
            //{
            //	var x = p.ToArray();
            //}
            //return;

            using (var p = ChoJSONReader<RootObject>.LoadText(json)
                        .WithField(m => m.Custom_fields, itemConverter: v => v)
            )
            {
                //var x = p.ToArray();
                Console.WriteLine(ChoJSONWriter<RootObject>.ToTextAll(p));
            }
        }

        static void Sample38()
        {
            var testJson = @"{'entry1': {
                       '49208118': [
                          {
                             'description': 'just a description'
                          },
                          {
                             'description': 'another description' 
                          }
                       ],
                       '29439559': [
                          {
                             'description': 'just a description'
                          },
                          {
                             'description': 'another description' 
                          }
                       ]
                     }
                }";
        }

        public class sp
        {
            public string mKey { get; set; }
            public string productType { get; set; }
            public string key { get; set; }
        }

        static void Sample37()
        {
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

            foreach (var rec in ChoJSONReader.LoadText(json).WithJSONPath("$..Markets")
                .Select(r => (IDictionary<string, object>)r).SelectMany(r1 => r1.Keys.Select(k => new { key = k, value = ((dynamic)((IDictionary<string, object>)r1)[k]) })
                .Select(k1 => new sp { mKey = k1.key, key = k1.value.productType, productType = k1.value.productType })))
                Console.WriteLine(rec.Dump());
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
        }

        static void Sample36()
        {
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
            Console.WriteLine(sb.ToString());

            var x = ChoXmlReader.LoadText(sb.ToString()).ToArray();
        }

        static void Sample35()
        {
            string json = @"[
{""SelFoodId"":""2"",""SelQuantity"":""5""},
{ ""SelFoodId"":""7"",""SelQuantity"":""3""},
{ ""SelFoodId"":""9"",""SelQuantity"":""7""}]
";
            foreach (var rec in ChoJSONReader.LoadText(json))
                Console.WriteLine(rec.Dump());
        }

        static void Sample34()
        {
            string json = @"{
    ""build"": 44396,
    ""files"": [
        ""00005DC8F14C92FFA13E7FDF1C9C35E4684F8B7A"", 
        [
            [""file1.zip"", 462485959, 462485959, 2, 0, 883, true, 266716, 1734, 992, 558, 0],
            [""file1.doc"", 521042, 521042, 2, 0, 883, true, 266716, 1734, 992, 558, 0]
        ], 
        ""0001194B90612DFB5E8D363249719FB62E221430"", 
        [
            [""file2.iso"", 501163544, 501163544, 2, 0, 956, true, 194777, 2573, 0, 0, 0]
        ], 
        ""0002B5245B0897BEA7D7F426E104B6D24FF368DE"", 
        [
            [""file3.mp4"", 284564707, 284564707, 2, 0, 543, true, 205165, 1387, 853, 480, 0]
        ]
    ]
}";
            foreach (var rec in ChoJSONReader.LoadText(json).WithJSONPath("$..files").Select(e => new { key = e.Key, fileName = ((object[])((object[])e.Value).Cast<object>().First())[0], fileSize = ((object[])((object[])e.Value).Cast<object>().First())[1] }))
                Console.WriteLine(rec.Dump());
        }

        public class Issue
        {
            [ChoJSONRecordField(JSONPath = "$..id")]
            public int? id { get; set; }
            [ChoJSONRecordField(JSONPath = "$..project.id")]
            public int project_id { get; set; }
            [ChoJSONRecordField(JSONPath = "$..project.name")]
            public string project_name { get; set; }
        }

        static void Sample33()
        {
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
            Console.WriteLine(issue.Dump());
        }

        static void Sample32()
        {
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

            Console.WriteLine(sb.ToString());
        }

        static void Sample31()
        {
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

            Console.WriteLine(sb.ToString());
        }
        static void Sample30()
        {
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
            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json).Configure(c => c.SupportMultipleContent = true)
                )
            {
                dynamic rec = p.First();
                var x = rec.Emp[0];

                //using (var w = new ChoXmlWriter(sb)
                //    .Configure(c => c.IgnoreRootName = true)
                //    .Configure(c => c.IgnoreNodeName = false)
                //    )
                //    w.Write(p);
            }

            Console.WriteLine(sb.ToString());
        }

        static void Sample29()
        {
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

            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
                )
            {
                using (var w = new ChoXmlWriter(sb)
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    )
                    w.Write(p);
            }

            Console.WriteLine(sb.ToString());
        }

        static void Sample28()
        {
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
            Console.WriteLine(sb.ToString());
        }
        static void Sample27()
        {
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
            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoXmlWriter(sb)
                    .Configure(c => c.RootName = "cars")
                    //.Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    )
                {
                    w.Write(p);
                }
            }
            Console.WriteLine(sb.ToString());
        }

        static void Sample26()
        {
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
            Console.WriteLine(sb.ToString());
        }

        static void Sample25()
        {
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
            Console.WriteLine(sb.ToString());

        }

        static void Sample24()
        {
            StringBuilder sb = new StringBuilder();
            using (var p = new ChoJSONReader("sample16.json"))
            {
                using (var w = new ChoJSONWriter(sb)
                    )
                {
                    w.Write(p);
                }
            }
            Console.WriteLine(sb.ToString());
        }

        static void Sample23()
        {
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
            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json)
                )
            {
                //using (var w = new ChoCSVWriter(sb)
                //	.WithFirstLineHeader()
                //	)
                //	w.Write(p);
                using (var w = new ChoXmlWriter(sb)
        )
                    w.Write(p);
            }
            Console.WriteLine(sb.ToString());
        }
        static void Sample22()
        {
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

            Console.WriteLine(csv.ToString());
        }

        static void Sample21()
        {
            string json = @"Name,Description,Account Number
    Xytrex Co.,Industrial Cleaning Supply Company,ABC15797531
    Watson and Powell Inc.,Law firm. New York Headquarters,ABC24689753";

            StringBuilder csv = new StringBuilder();
            using (var p = new ChoCSVReader(new StringReader(json))
                    .WithFirstLineHeader()
                )
            {
                using (var w = new ChoJSONWriter(new StringWriter(csv)))
                {
                    w.Write(p);
                }
            }

            Console.WriteLine(csv.ToString());
        }

        static void Sample20()
        {
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
                    .WithField("Account_Number", fieldName: "Account Number")
                    )
                {
                    w.Write(p);
                }
            }

            Console.WriteLine(csv.ToString());
        }

        static void Sample19()
        {
            using (var p = new ChoJSONReader("sample19.json"))
            {
                var z1 = p.SelectMany(p1 => ((dynamic[])p1.packaing).Select(p2 => new { pickcompname = p1.pickcompname, qty = p2.qty })).ToArray();
                return;

                foreach (var rec in p)
                {
                    var z = rec.packaing;
                }
            }
        }

        public class MarketData
        {
            [ChoJSONRecordField(JSONPath = @"['Meta Data']")]
            public MetaData MetaData { get; set; }

            [ChoJSONRecordField(JSONPath = @"$..['Stock Quotes'][*]")]
            public List<StockQuote> StockQuotes { get; set; }
        }

        public class MetaData
        {
            [JsonProperty(PropertyName = "1. Information")]
            [ChoJSONRecordField(JSONPath = @"['Meta Data']['1. Information']")]
            public string Information { get; set; }
            [JsonProperty(PropertyName = "2. Notes")]
            public string Notes { get; set; }
            [JsonProperty(PropertyName = "3. Time Zone")]
            public string TimeZone { get; set; }
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
        }

        static void Sample17()
        {
            using (var p = new ChoJSONReader<MarketData>("sample17.json")
                )
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void Sample16()
        {
            using (var p = new ChoJSONReader("sample16.json")
                .WithField("Ref", jsonPath: "$..ref", fieldType: typeof(string))
                .WithField("pickcompname", jsonPath: "$..pickcompname", fieldType: typeof(string))
                .WithField("gw", jsonPath: "$..gw", fieldType: typeof(double))
                .WithField("qty1", jsonPath: "$..packaing[0].qty", fieldType: typeof(int))
                .WithField("unit1", jsonPath: "$..packaing[0].unit", fieldType: typeof(string))
                .WithField("qty2", jsonPath: "$..packaing[1].qty", fieldType: typeof(int))
                .WithField("unit2", jsonPath: "$..packaing[1].unit", fieldType: typeof(string))
                )
            {
                using (var c = new ChoCSVWriter("sample16.csv").WithFirstLineHeader())
                    c.Write(p);
            }
        }

        static void Sample15()
        {
            using (var p = new ChoJSONReader("sample15.json")
                .WithField("header", jsonPath: "$..header[*]", fieldType: typeof(string[]))
                .WithField("results", jsonPath: "$..results[*]", fieldType: typeof(List<string[]>))
                )
            {
                var rec = p.FirstOrDefault();
                string[] header = rec.header;
                List<string[]> results = rec.results;

                var z = results.Select(a => header.Zip(a, (k, v) => new { Key = k, Value = v }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)).ToArray();
            }
        }

        static void Sample14()
        {
            using (var p = new ChoJSONReader("sample14.json")
                .WithField("USD", jsonPath: "$..USD.FCC-IRCUBE[*]")
                .WithField("EUR", jsonPath: "$..EUR.FCC-IRCUBE[*]")
                .WithField("GBP", jsonPath: "$..GBP.FCC-IRCUBE")
            )
            {
                foreach (dynamic rec in p)
                {
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
            return;

            using (var p = new ChoJSONReader("sample14.json")
            .WithField("USD", jsonPath: "$..USD.FCC-IRCUBE[*]", fieldType: typeof(IRCUBE[]))
            .WithField("EUR", jsonPath: "$..EUR.FCC-IRCUBE[*]", fieldType: typeof(IRCUBE[]))
            .WithField("GBP", jsonPath: "$..GBP.FCC-IRCUBE", fieldType: typeof(IRCUBE[]))
                )
            {
                foreach (dynamic rec in p)
                {
                    Console.WriteLine("USD:");
                    Console.WriteLine();
                    foreach (var curr in rec.USD)
                    {
                        Console.WriteLine(curr.ToString());
                    }
                    Console.WriteLine();
                }
            }
        }

        static void Sample13()
        {
            using (var p = new ChoJSONReader("sample3.json")
                //.WithField("details_attributes", jsonPath: "$..details_attributes", fieldType: typeof(ChoDynamicObject))
                )
            {
                foreach (dynamic rec in p)
                    Console.WriteLine(rec.menu.popup.menuitem[1].value);
            }
        }

        static void Sample12()
        {
            using (var jr = new ChoJSONReader("sample12.json")
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

                        Console.WriteLine(ChoUtility.DumpAsJson(newObj));
                    }
                }
            }
        }

        static void Sample11()
        {
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
                {
                    Console.WriteLine($"TotalCount: {x.TotalCount}");

                    foreach (var url in x.Url)
                    {
                        Console.WriteLine($"Url: {url}");
                    }
                }
            }
        }

        static void Sample10()
        {
            using (var jr = new ChoJSONReader<Filter>("sample10.json")
                )
            {
                foreach (var x in jr)
                {
                    Console.WriteLine($"FilterName: {x.filterName}");
                    Console.WriteLine($"FilterformattedValue: {x.filterformattedValue}");
                    Console.WriteLine($"filterValue : {x.filterValue}");
                    Console.WriteLine($"View: {x.view}");
                }
            }
            return;
            using (var jr = new ChoJSONReader("sample10.json")
                )
            {
                foreach (var x in jr)
                {
                    Console.WriteLine($"FilterName: {x.filterName}");
                }
            }
        }

        static void Sample9()
        {
            using (var jr = new ChoJSONReader<Book>("sample9.json").WithJSONPath("$..book")
            )
            {
                foreach (var x in jr)
                {
                    Console.WriteLine($"Category: {x.Category}");
                    Console.WriteLine($"Title: {x.Title}");
                    Console.WriteLine($"Author: {x.Author}");
                    Console.WriteLine($"Price: {x.Price}");
                }
            }
            return;
            using (var jr = new ChoJSONReader("sample9.json").WithJSONPath("$..book")
                )
            {
                foreach (var x in jr)
                {
                    Console.WriteLine($"Category: {x.category}");
                    Console.WriteLine($"Title: {x.title}");
                    Console.WriteLine($"Author: {x.author}");
                    Console.WriteLine($"Price: {x.price}");
                }
            }
        }

        static void Sample8()
        {
            using (var jr = new ChoJSONReader<DataMapper>("sample8.json"))
            {
                foreach (var x in jr)
                {
                    Console.WriteLine(ChoUtility.DumpAsJson(x));
                }
            }
        }

        static void Sample7()
        {
            using (var jr = new ChoJSONReader<Family>("sample7.json").WithJSONPath("$.fathers"))
            {
                foreach (var x in jr)
                {
                    Console.WriteLine(x.Id);
                    foreach (var fm in x.Daughters)
                        Console.WriteLine(fm);
                }
            }
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

        static void IgnoreItems()
        {
            using (var jr = new ChoJSONReader("sample6.json")
                .WithField("ProductId", jsonPath: "$.productId")
                .WithField("User", jsonPath: "$.returnPolicies.user")
                )
            {
                foreach (var item in jr)
                    Console.WriteLine(item.ProductId + " " + item.User);
            }
        }
        public static void KVPTest()
        {
            using (var jr = new ChoJSONReader<Dictionary<string, string>>("sample5.json").Configure(c => c.UseJSONSerialization = true))
            {
                foreach (var dict1 in jr.Select(dict => dict.Select(kvp => new { kvp.Key, kvp.Value })).SelectMany(x => x))
                {
                    Console.WriteLine(dict1.Key);
                }
            }
        }

        static void Sample4()
        {
            using (var jr = new ChoJSONReader("sample4.json").Configure(c => c.UseJSONSerialization = true))
            {
                using (var xw = new ChoCSVWriter("sample4.csv").WithFirstLineHeader())
                {
                    foreach (JObject jItem in jr)
                    {
                        dynamic item = jItem;
                        var identifiers = ChoEnumerable.AsEnumerable<JObject>(jItem).Select(e => ((IList<JToken>)((dynamic)e).identifiers).Select(i =>
                           new
                           {
                               identityText = i["identityText"].ToString(),
                               identityTypeCode = i["identityTypeCode"].ToString()
                           })).SelectMany(x => x);

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

                           }))).SelectMany(x => x).SelectMany(y => y);

                        var comb = members.ZipEx(identifiers, (m, i) =>
                        {
                            if (i == null)
                                return new
                                {
                                    item.ccId,
                                    item.hId,
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
                                    SubscriberFirstName = item.subscriberFirstame,
                                    SubscriberLastName = item.subscriberLastName,

                                };
                            else
                                return new
                                {
                                    item.ccId,
                                    item.hId,
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
                                    SubscriberFirstName = item.subscriberFirstame,
                                    SubscriberLastName = item.subscriberLastName,
                                };

                        });
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
        }

        static void Sample3()
        {
            using (var jr = new ChoJSONReader<MyObjectType>("sample3.json").WithJSONPath("$.menu")
                )
            {
                jr.AfterRecordFieldLoad += (o, e) =>
                {
                };
                using (var xw = new ChoXmlWriter<MyObjectType>("sample3.xml").Configure(c => c.UseXmlSerialization = true))
                    xw.Write(jr);
            }
        }

        static void Sample2()
        {
            //using (var csv = new ChoCSVWriter("sample2.csv") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.WithFirstLineHeader())
            //{
            //    csv.Write(new ChoJSONReader("sample2.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }
            //    .WithField("Base")
            //    .WithField("Rates", fieldType: typeof(Dictionary<string, object>))
            //    .Select(m => ((Dictionary<string, object>)m.Rates).Select(r => new { Base = m.Base, Key = r.Key, Value = r.Value })).SelectMany(m => m)
            //    );
            //}
        }

        static void Sample1()
        {
            using (var csv = new ChoCSVWriter("sample1.csv") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.WithFirstLineHeader())
            {
                csv.Write(new ChoJSONReader("sample1.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.Select(e => Flatten(e)));
            }
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
        static void JsonToXml()
        {
            using (var csv = new ChoXmlWriter("companies.xml") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.WithXPath("companies/company"))
            {
                csv.Write(new ChoJSONReader<Company>("companies.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.NotifyAfter(10000).Take(10).
                    SelectMany(c => c.Products.Touch().
                    Select(p => new { c.name, c.Permalink, prod_name = p.name, prod_permalink = p.Permalink })));
            }
        }

        static void JsonToCSV()
        {
            using (var csv = new ChoCSVWriter("companies.csv") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.WithFirstLineHeader())
            {
                csv.Write(new ChoJSONReader<Company>("companies.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.NotifyAfter(10000).Take(10).
                    SelectMany(c => c.Products.Touch().
                    Select(p => new { c.name, c.Permalink, prod_name = p.name, prod_permalink = p.Permalink })));
            }
        }

        static void LoadTest()
        {
            using (var p = new ChoJSONReader<Company>("companies.json") { TraceSwitch = ChoETLFramework.TraceSwitchOff }.NotifyAfter(10000))
            {
                p.Configuration.ColumnCountStrict = true;
                foreach (var e in p)
                    Console.WriteLine("overview: " + e.name);
            }

            //Console.WriteLine("Id: " + e.name);
        }

        public class Product
        {
            [ChoJSONRecordField]
            public string name { get; set; }
            [ChoJSONRecordField]
            public string Permalink { get; set; }
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
        static void QuickLoad()
        {
            foreach (dynamic e in new ChoJSONReader("Emp.json"))
                Console.WriteLine("Id: " + e.Id + " Name: " + e.Name);
        }

        static void POCOTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONReader<EmployeeRec>(reader))
            {
                writer.WriteLine(EmpJSON);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }
        static void StorePOCOTest()
        {
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
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }
        static void StorePOCONodeLoadTest()
        {
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
                        Console.WriteLine(rec.ToStringEx());
                    }
                }
            }
        }
        static void QuickLoadTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoJSONReader(reader).WithJSONPath("$.Manufacturers").WithField("Name", fieldType: typeof(string)).WithField("Products", fieldType: typeof(Product[])))
            {
                writer.WriteLine(Stores);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }
        static void QuickLoadSerializationTest()
        {
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
                    Console.WriteLine(rec.ToStringEx());
                }
            }
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
            public override string ToString()
            {
                return "{0}. {1}.".FormatString(Name, Products == null ? 0 : Products.Length);
            }
        }
    }
}
