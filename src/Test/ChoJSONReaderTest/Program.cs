using ChoETL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
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
                                                           Street = "432 Main Avenue"
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


        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            Sample32();
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
