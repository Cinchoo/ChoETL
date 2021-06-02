using ChoETL;
using NUnit.Framework;
using Newtonsoft.Json;
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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Linq;
using static ChoJSONWriterTest.Program;

namespace ChoJSONWriterTest
{
    public class ToTextConverter : IChoValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToNString();
        }
    }

    public class Emp
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Choice
    {
        public string[] Options { get; set; }
        public Emp Emp { get; set; }
        public List<int> Ids { get; set; }
        public Emp[] EmpArr { get; set; }
        //public Dictionary<int, Emp> EmpDict { get; set; }
    }


    public class SomeOuterObject
    {
        public string stringValue { get; set; }
        public IPAddress ipValue { get; set; }
    }
    public enum Gender
    {
        [Description("M")]
        Male,
        [Description("F")]
        Female
    }

    public class Person
    {
        public int Age { get; set; }
        //[ChoTypeConverter(typeof(ChoEnumConverter), Parameters = "Name")]
        public Gender Gender { get; set; }

        public override bool Equals(object obj)
        {
            var person = obj as Person;
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

    public class MyDate
    {
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
    }

    public class Lad
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public MyDate dateOfBirth { get; set; }
    }

    public class PlaceObj : IChoNotifyRecordFieldWrite
    {
        public string Place { get; set; }
        public int SkuNumber { get; set; }

        public bool AfterRecordFieldWrite(object target, long index, string propName, object value)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordFieldWrite(object target, long index, string propName, ref object value)
        {
            //if (propName == nameof(SkuNumber))
            //    value = String.Format("SKU_{0}", value.ToNString());

            return true;
        }

        public bool RecordFieldWriteError(object target, long index, string propName, ref object value, Exception ex)
        {
            throw new NotImplementedException();
        }
    }

    public class ValueObject<T>
    {
        public T Value { get; }
        public ValueObject(T value) => Value = value;
    }

    public class CustomerId : ValueObject<Guid>
    {
        public CustomerId(Guid value) : base(value) { }
    }

    public class EmailAddress : ValueObject<string>
    {
        public EmailAddress(string value) : base(value) { }
    }

    public class CustomerInfo
    {
        public CustomerId Id { get; }
        public EmailAddress Email { get; }

        public CustomerInfo(CustomerId id, EmailAddress email)
        {
            Id = id;
            Email = email;
        }
    }
    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {
        public static void InheritanceTest()
        {
            var customerId = new CustomerId(Guid.NewGuid());
            var emailAddress = new EmailAddress("some@email.com");

            var customer = new CustomerInfo(customerId, emailAddress);

            StringBuilder msg = new StringBuilder();

            using (var w = new ChoJSONWriter<CustomerInfo>(msg)
                )
            {
                w.Write(customer);
            }

            Console.WriteLine(msg.ToString());
        }

        public enum EmpType { FullTime, Contract }

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


        static void InterfaceTest()
        {
            string json = @"{
    ""$type"": ""ChoJSONReaderTest.Program+Person1, ChoJSONReaderTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",
    ""Profession"": {
        ""$type"": ""ChoJSONReaderTest.Program+Programming, ChoJSONReaderTest, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"",
        ""JobTitle"": ""Software Developer"",
        ""FavoriteLanguage"": ""C#""
    }
}";
            StringBuilder jsonOut = new StringBuilder();

            using (var w = new ChoJSONWriter(jsonOut)
                .UseJsonSerialization()
                .JsonSerializationSettings(s => s.TypeNameHandling = TypeNameHandling.All)
            )
            {
                w.Write(new Person1() { Profession = new Writing() });
            }

            Console.WriteLine(jsonOut.ToString());
        }

        public static void CSVWithSpaceHeader2JSON()
        {
            string csv = @"Id, First Name
1, Tom
2, Mark";

            StringBuilder json = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader())
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.FirstName);
                using (var w = new ChoJSONWriter(json)
                    .ErrorMode(ChoErrorMode.ThrowAndStop)
                    )
                {
                    w.Write(r);
                }
            }

            Console.WriteLine(json.ToString());
        }

        public static void CSV2JSONNoIndentation()
        {
            string csv = @"Id, First Name
                1, Tom
                2, Mark";

            StringBuilder json = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                )
            {
                using (var w = new ChoJSONWriter(json)
                    //.Configure(c => c.Formatting = Formatting.None)
                    //.SupportMultipleContent()
                    //.SingleElement()
                    )
                {
                    w.Write(r.Take(2));
                }
            }

            Console.WriteLine(json.ToString());
        }

        static void ComplexObjSerializationTest()
        {
            var sb = new StringBuilder();
            using (var p = new ChoJSONWriter<Choice>(sb)
                .WithField("Options", valueConverter: o => String.Join(",", o as string[]))
                .Formatting()
                .Configure(c => c.NullValueHandling = ChoNullValueHandling.Ignore)
                )
            {
                List<Choice> l = new List<Choice>
                {
                    new Choice
                {
                    Options = new[] { "op 1", "op 2" },
                    EmpArr = new Emp[] { new Emp { Id = 1, Name = "Tom" }, new Emp { Id = 2, Name = "Mark" }, null },
                    //Emp = new Emp {  Id = 0, Name = "Raj"},
                    //EmpDict = new Dictionary<int, Emp> { { 1, new Emp { Id = 11, Name = "Tom1" } } },
                    Ids = new List<int> { 1, 2, 3}
                },
                //    new Choice
                //{
                //    Options = new[] { "op 1", "op 2" },
                //    EmpArr = new Emp[] { new Emp { Id = 1, Name = "Tom" }, new Emp { Id = 2, Name = "Mark" }, null },
                //    //Emp = new Emp {  Id = 0, Name = "Raj"},
                //    //EmpDict = new Dictionary<int, Emp> { { 1, new Emp { Id = 11, Name = "Tom1" } } },
                //    Ids = new List<int> { 1, 2, 3}
                //}
                };
                p.Write(l);
            }

            Console.WriteLine(sb.ToString());

        }

        static void IgnoreNullNodeTest()
        {
            string json = @"[
  {
    ""Id"": 1,
    ""Name"": ""Mark""
  },
  {
    ""Id"": 2,
    ""Name"": null
  }
]
";
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
        }

        static void SerializeDynamicObject()
        {
            dynamic obj = new ExpandoObject();
            obj.Email = "james@example.com";
            obj.Active = true;
            obj.Roles = new List<string>()
            {
                "DEV",
                "OPS"
            };

            string json = ChoJSONWriter.Serialize(obj);

            Console.WriteLine(json);
        }

        static void SerializeAnonymousObject()
        {
            string json = ChoJSONWriter.Serialize(new
            {
                Email = "james@example.com",
                Active = true,
                Roles = new List<string>()
                {
                    "DEV",
                    "OPS"
                }
            });

            Console.WriteLine(json);
        }


        public class Account
        {
            public string Email { get; set; }
            public bool Active { get; set; }
            public DateTime CreatedDate { get; set; }
            public IList<string> Roles { get; set; }

            public override string ToString()
            {
                return Email;
            }
        }
        static void SerializeObject()
        {
            //            string json = @"{
            //  'Email': 'james@example.com',
            //  'Active': true,
            //  'CreatedDate': '2013-01-20T00:00:00Z',
            //  'Roles': [
            //    'User',
            //    'Admin'
            //  ]
            //}";

            string json = ChoJSONWriter.Serialize(new Account
            {
                Email = "james@example.com",
                Active = true,
                Roles = new List<string>()
                    {
                        "DEV",
                        "OPS"
                    }

            });
            //string json = ChoJSONWriter.SerializeAll<Account>(new Account[] {
            //    new Account
            //    {
            //    Email = "james@example.com",
            //    Active = true,
            //    Roles = new List<string>()
            //    {
            //        "DEV",
            //        "OPS"
            //    }

            //    }
            //}
            //);


            Console.WriteLine(json);
        }

        static void SerializeCollection()
        {
            string json = ChoJSONWriter.SerializeAll(new int[] { 1, 2, 3 },
                new ChoJSONRecordConfiguration().Configure(c => c.Formatting = Formatting.None)); // new string[] { "Starcraft", "Halo", "Legend of Zelda" });

            Console.WriteLine(json);
        }

        static void SerializeDictionary()
        {
            var acc1 = new Account
            {
                Email = "james@example.com",
                Active = true,
                Roles = new List<string>()
                    {
                        "DEV",
                        "OPS"
                    }

            };
            var acc2 = new Account
            {
                Email = "rob@example.com",
                Active = true,
                Roles = new List<string>()
                    {
                        "DEV",
                        "OPS"
                    }

            };

            string json = ChoJSONWriter.SerializeAll(new Dictionary<Account, Account>[] {
            new Dictionary<Account, Account>()
            {
                [acc1] = acc1,
                [acc2] = acc2
            }
            }, new ChoJSONRecordConfiguration()
            );

            Console.WriteLine(json);
        }

        static void SerializeScalar()
        {
            string json = ChoJSONWriter.Serialize(1,
                new ChoJSONRecordConfiguration()
                //.Configure(c => c.Formatting = Formatting.None)
                //.Configure(c => c.RootName = "Root")
                //.Configure(c => c.NodeName = "Node")
                ); // new string[] { "Starcraft", "Halo", "Legend of Zelda" });

            Console.WriteLine(json);
        }

        public class Record
        {
            public User User { get; set; }
            public User Sister { get; set; }
            public User Mother { get; set; }
        }

        public class User
        {
            public string Name { get; set; }
            public int Id { get; set; }
        }

        static void SerializeComplexType()
        {
            var rec = new Record
            {
                User = new User
                {
                    Id = 1,
                    Name = "Tom"
                },
                Sister = new User
                {
                    Id = 2,
                    Name = "Betsy"
                },
                Mother = new User
                {
                    Id = 3,
                    Name = "Sisly"
                }
            };

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<Record>(json)
                .WithField(f => f.User, m => m.CustomSerializer((o) => JsonConvert.SerializeObject(o.ToDictionary().Select(kvp => new KeyValuePair<string, object>($"User_{kvp.Key}", kvp.Value)).ToDictionary())))
                )
            {
                w.Write(rec);
            }

            Console.WriteLine(json.ToString());
        }

        static void BSONTest()
        {
            string json = @"[
  {
    ""Id"": 1,
    ""Name"": ""Mark""
  },
  {
    ""Id"": 2,
    ""Name"": null
  }
]
";
            StringBuilder xml = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
            }
            Console.WriteLine(xml.ToString());

        }

        static void XmlWithXData2JSON()
        {
            string json  = @"{
      ""d1"": ""test"",
      ""d2"": ""test@1234"",
      ""xmltestData"": ""<![CDATA[<Invoices><key>we</key></Invoices>]]>"",
      ""user"": ""demo"",
      ""pass"": ""653""
    }";

            StringBuilder xml = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoXmlWriter(xml)
                    .WithField("xmlTestData")
                    )
                    w.Write(r);
            }

            Console.WriteLine(xml.ToString());
        }

        static void WriteCommentTest()
        {
            var rec = new Record
            {
                User = new User
                {
                    Id = 1,
                    Name = "Tom"
                },
                Sister = new User
                {
                    Id = 2,
                    Name = "Betsy"
                },
                Mother = new User
                {
                    Id = 3,
                    Name = "Sisly"
                }
            };

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<Record>(json)
                //.WithField(f => f.User, m => m.CustomSerializer((o) => JsonConvert.SerializeObject(o.ToDictionary().Select(kvp => new KeyValuePair<string, object>($"User_{kvp.Key}", kvp.Value)).ToDictionary())))
                .WithFieldForType<User>(f => f.Name, valueConverter: o => o.ToNString() + "M")
                )
            {
                w.Write(rec);
            }

            Console.WriteLine(json.ToString());

        }

        public class Employee
        {
            public string Name { get; set; }
            public Employee Manager { get; set; }

            public bool ShouldSerializeManager()
            {
                // don't serialize the Manager property if an employee is their own manager
                return (Manager != this);
            }
        }
        public static void ConditionalPropertySerialize()
        {
            Employee joe = new Employee();
            joe.Name = "Joe Employee";
            Employee mike = new Employee();
            mike.Name = "Mike Manager";

            joe.Manager = mike;

            // mike is his own manager
            // ShouldSerialize will skip this property
            mike.Manager = mike;

            string json = ChoJSONWriter.SerializeAll(new[] { joe, mike }, new ChoJSONRecordConfiguration().Configure(c => c.UseJSONSerialization = true));

            Console.WriteLine(json);
        }

        static void SerializeDateTimeTest()
        {
            IList<DateTime> dateList = new List<DateTime>
            {
                new DateTime(2009, 12, 7, 23, 10, 0, DateTimeKind.Utc),
                new DateTime(2010, 1, 1, 9, 0, 0, DateTimeKind.Utc),
                new DateTime(2010, 2, 10, 10, 0, 0, DateTimeKind.Utc)
            };

            string json = ChoJSONWriter.SerializeAll(dateList, new JsonSerializerSettings
            {
                DateFormatString = "d MMMM, yyyy",
                Formatting = Formatting.Indented
            });

            Console.WriteLine(json);
        }

        public class Account1
        {
            //[ChoIgnoreMember]
            //[JsonIgnore]
            public string Email { get; set; }
            public bool Active { get; set; }
            public DateTime CreatedDate { get; set; }
            public IList<string> Roles { get; set; }
        }

        static void ExcludePropertyTest()
        {
            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<Account1>(json)
                .IgnoreField(f => f.Email)
                )
            {
                w.Write(new Account1
                {
                    Email = "james@example.com",
                    Active = true,
                    Roles = new List<string>()
                    {
                        "DEV",
                        "OPS"
                    }

                });
            }

                //string json = ChoJSONWriter.Serialize(new Account1
                //{
                //    Email = "james@example.com",
                //    Active = true,
                //    Roles = new List<string>()
                //    {
                //        "DEV",
                //        "OPS"
                //    }

                //}, new ChoJSONRecordConfiguration<Account>().Ignore(f => f.Email));

            Console.WriteLine(json);
        }

        static void Xml2JSON()
        {
            string xml = @"<Employees xmlns=""http://company.com/schemas"">
                <Employee>
                    <FirstName>name1</FirstName>
                    <LastName>surname1</LastName>
                </Employee>
                <Employee>
                    <FirstName>name2</FirstName>
                    <LastName>surname2</LastName>
                </Employee>
                <Employee>
                    <FirstName>name3</FirstName>
                    <LastName>surname3</LastName>
                </Employee>
            </Employees>";

            StringBuilder json = new StringBuilder();
            using (var r = ChoXmlReader.LoadText(xml))
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }

            Console.WriteLine(json.ToString());
        }

        [ChoJSONRecordObject(ObjectValidationMode = ChoObjectValidationMode.MemberLevel, ErrorMode = ChoErrorMode.ReportAndContinue)]
        public class Emp1
        {
            [DisplayName("Id")]
            public int ID { get; set; }
            public string Name { get; set; }

            public Address1 Address { get; set; }
        }

        public class Address1 : IChoNotifyRecordFieldWrite, IChoNotifyRecordFieldRead
        {
            [DisplayName("street")]
            [StringLength(maximumLength: 5)]
            //[ChoIgnoreMember]
            public string Street { get; set; }
            public string City { get; set; }

            public bool AfterRecordFieldLoad(object target, long index, string propName, object value)
            {
                throw new NotImplementedException();
            }

            public bool AfterRecordFieldWrite(object target, long index, string propName, object value)
            {
                throw new NotImplementedException();
            }

            public bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value)
            {
                if (propName == "City")
                {
                    value = "Edison";
                    return true;
                }
                else
                    return false;

            }

            public bool BeforeRecordFieldWrite(object target, long index, string propName, ref object value)
            {
                throw new NotImplementedException();
            }

            public bool RecordFieldLoadError(object target, long index, string propName, ref object value, Exception ex)
            {
                value = "new value";
                return true;
            }

            public bool RecordFieldWriteError(object target, long index, string propName, ref object value, Exception ex)
            {
                return false;
            }
        }


        static void POCOWriteTest()
        {
            StringBuilder json = new StringBuilder();
            Emp1 e1 = new Emp1
            {
                ID = 1,
                Name = "Tom",

                Address = new Address1
                {
                    Street = "1 f Street",
                    City = "NYC"
                }
            };

            using (var w = new ChoJSONWriter<Emp1>(json)
                )
            {
                w.Write(e1);
            }

            Console.WriteLine(json.ToString());

        }

        static void POCOReadTest()
        {
            string json = @"[
  {
    ""Id"": 1,
    ""Name"": ""Tom"",
    ""Address"": {
      ""street"": ""1 Main Street"",
      ""City"": ""NYC""
    }
  }
]";

            using (var r = ChoJSONReader<Emp1>.LoadText(json)
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
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
            public int Data1 { get; set; }
        }
        static void ConditionalSelectionsOfNodes()
        {
            StringBuilder json = new StringBuilder();

            using (var w = new ChoJSONWriter<CTest>(json)
                .RegisterNodeConverterForType<CTest>(o => (((CTest)o).Id > 0 ? o : null))
                .RegisterNodeConverterForType<List<Class2>>(o =>
                {
                    dynamic x = o as dynamic;
                    x.serializer.Serialize(x.writer, ((List<Class2>)x.value).Where(c => c.Id != 0).ToArray());
                    return null;
                })
            )
            {
                w.Write(new CTest
                {
                    Id = 1,
                    Name = "Tom",
                    Details = new List<Class2>
                    {
                        new Class2
                        {
                            Id = 0,
                            Data1 = 1
                        },
                        new Class2
                        {
                            Id = 10,
                            Data1 = 2
                        }

                    }
                });
                w.Write(new CTest
                {
                    Id = 20,
                    Name = "Tom",
                    Details = new List<Class2>
                    {
                        new Class2
                        {
                            Id = 0,
                            Data1 = 1
                        },
                        new Class2
                        {
                            Id = 10,
                            Data1 = 2
                        }

                    }
                });
            }
            Console.WriteLine(json.ToString());
        }

        public class TestObj
        {
            [JsonProperty("full_name")]
            public string Name { get; set; }


            public City City { get; set; } = new City();
        }

        public class City
        {
            [JsonProperty("city_name")]
            public string Name { get; set; }
            [JsonProperty("zip")]
            public string ZIP { get; set; }
            [JsonProperty("country")]
            public Country Country { get; set; }
        }

        public class Country
        {
            public string Name { get; set; }
        }

        static void FlattenJson()
        {
            StringBuilder json = new StringBuilder();

            using (var w = new ChoJSONWriter<TestObj>(json)
                .MapRecordFields<City>()
                .ClearFields()
                .WithField(f => f.Name, fieldName: "full_name")
                .WithField(f => f.City.Name, fieldName: "city_name")
                .WithField(f => f.City.ZIP, fieldName: "zip")
                .WithField(f => f.City.Country.Name, fieldName: "country")
                )
            {
                w.Write(new TestObj
                {
                    Name = "Tom",
                    City =
                    {
                        Name = "NYC",
                        ZIP = "100010",
                        Country = new Country
                        {
                            Name = "USA"
                        }
                    }
                });
            }

            Console.WriteLine(json.ToString());
        }

        public class InputModel
        {
            public string Id { get; set; }
            [DisplayFormat(DataFormatString = "yyyy-MM-dd hh:mm")]
            public DateTime? ArrivalDate { get; set; }
            public string origin { get; set; }
        }

        static void CustomDateTimeFormatTest()
        {
            StringBuilder json = new StringBuilder();

            using (var w = new ChoJSONWriter<InputModel>(json)
                )
            {
                w.Write(new InputModel
                {
                    Id = "1",
                    ArrivalDate = DateTime.Now,
                    origin = "NYC"
                });
            }

            Console.WriteLine(json.ToString());

        }


        public class DbRowObject
        {
            [ChoArrayIndex(0)]
            public string Item1 { get; set; }
        }

        public class Data
        {
            [ChoSourceType(typeof(string[]))]
            [ChoTypeConverter(typeof(ChoArrayToObjectConverter))]
            public DbRowObject[] DbRows { get; set; }
        }

        public class DbObject
        {
            [ChoJSONPath("database_id")]
            public int DbId { get; set; }
            [ChoJSONPath("row_count")]
            public int RowCount { get; set; }
            [ChoJSONPath("data.rows[*]")]
            public Data Data { get; set; }
        }

        static void SerializeInnerObjectsToArray()
        {
            string json1 = @"{
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

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<DbObject>(json)
                //.WithField(f => f.DbRows, jsonPath: "data.rows[*]")
                //.WithField(f => f.DbRows, m => m.Configure(c => c.AddConverter(ChoArrayToObjectConverter.Instance)))
                )
            {
                w.Write(new DbObject
                {
                    DbId = 9,
                    RowCount = 2,
                    Data = new Data
                    {
                        DbRows = new DbRowObject[]
                        {
                            new DbRowObject
                            {
                                Item1 = "242376_dpi65990"
                            }
                        }
                    }
                });
            }

            Console.WriteLine(json.ToString());
        }

        public class Location
        {
            public string Name { get; set; }
            public LocationList Locations { get; set; }
        }

        // Note: LocationList is simply a subclass of a List<T>
        // which then adds an IsExpanded property for use by the UI.
        public class LocationList : List<Location>
        {
            [JsonProperty("IsExpanded")]
            public bool IsExpanded { get; set; }
        }

        public class RootViewModel
        {
            [JsonProperty("RootLocations")]
            public LocationList RootLocations { get; set; }
        }

        static void SerializeNestedObjectOfList()
        {
            StringBuilder json = new StringBuilder();

            var c = new LocationListJsonConverter();
            ChoTypeConverter.Global.Add(typeof(LocationList), c);

            ChoJSONRecordConfiguration config = new ChoJSONRecordConfiguration();
            config.JsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            config.JsonSerializerSettings.Formatting = Formatting.Indented;

            using (var w = new ChoJSONWriter<RootViewModel>(json, config)
                //.Configure(c => c.RootName = "Locations")
                .SupportMultipleContent()
                .UseJsonSerialization()
                .RegisterNodeConverterForType<LocationList>(s =>
                {
                    dynamic input = s as dynamic;
                    var locationList = input.value as LocationList;

                    JObject jLocationList = new JObject();

                    if (locationList.IsExpanded)
                        jLocationList.Add("IsExpanded", true);
                    else
                        jLocationList.Add("IsExpanded", false);

                    if (locationList.Count > 0)
                    {
                        var jLocations = new JArray();

                        foreach (var location in locationList)
                        {
                            var v = JObject.FromObject(location, config.JsonSerializer);
                            if (v != null)
                            jLocations.Add(v);
                        }

                        jLocationList.Add("Items", jLocations);

                    }

                    return jLocationList;
                })
                //.WithField(f => f.RootLocations, m => m.CustomSerializer(s =>
                //{
                //}))
                //.WithField(f => f.DbRows, jsonPath: "data.rows[*]")
                //.WithField(f => f.DbRows, m => m.Configure(c => c.AddConverter(ChoArrayToObjectConverter.Instance)))
                )
            {
                var l1 = new LocationList
                {
                    IsExpanded = false,
                };
                l1.Add(new Location
                {
                    Name = "First Floor"
                });

                var l = new LocationList
                {
                    IsExpanded = true,
                };

                l.Add(new Location
                {
                    Name = "Main Residence",
                    Locations = l1
                });

                w.Write(new RootViewModel
                {
                    RootLocations = l
                });
            }

            Console.WriteLine(json.ToString());
        }

        public class Order
        {
            public string orderNo { get; set; }
            public string customerNo { get; set; }
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

        static void ObjectMemberToArrayTest()
        {
            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<Order>(json))
            {
                w.Write(new Order
                {
                    orderNo = "1",
                    customerNo = "10",
                    items = new OrderItem[]
                    {
                        new OrderItem
                        {
                            itemId = 1,
                            price = 100.1m,
                            quantity = 5
                        }
                    }
                });
            }

            Console.WriteLine(json.ToString());
        }

        static void AppendFile()
        {
            using (var sw = new StreamWriter("append.json", true))
            {
                using (var w = new ChoJSONWriter<Order>(sw))
                {
                    w.Write(new Order
                    {
                        orderNo = "1",
                        customerNo = "10",
                        items = new OrderItem[]
                        {
                        new OrderItem
                        {
                            itemId = 1,
                            price = 100.1m,
                            quantity = 5
                        }
                        }
                    });
                }
            }

            Console.WriteLine(File.ReadAllText("append.json"));
        }

        public class SensitiveInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string AccountNumber { get; set; }

            public Dictionary<string, string> SensitiveDict { get; set; }
        }

        static void SensitiveInfoSerialization()
        {
            StringBuilder json = new StringBuilder();

            using (var w = new ChoJSONWriter<SensitiveInfo>(json)
                .WithField(f => f.Id)
                .WithField(f => f.Name)
                .WithField(f => f.AccountNumber, valueConverter: o => String.Join("", ((string)o).Reverse()))
                .WithField(f => f.SensitiveDict, valueConverter: o =>
                {
                    var dict = o as Dictionary<string, string>;
                    dict.Remove("1");
                    return dict;
                })
                )
            {
                w.Write(new SensitiveInfo
                {
                    Id = "1",
                    Name = "Tom",
                    AccountNumber = "12345",
                    SensitiveDict = new Dictionary<string, string>
                    {
                        { "1", "11" },
                        { "2", "22" }
                    }
                });
            }

            Console.WriteLine(json.ToString());
        }

        public class DriverInformationX
        {
            [ChoIgnoreMember]
            public string DriverID { get; set; }
            public string Branch { get; set; }
        }

        public class PersonX
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public DriverInformationX Info { get; set; }

            [ChoIgnoreMember]
            public string FullName
            {
                get { return FirstName + " " + LastName; }
            }
        }

        static void IgnoreNestedProperty()
        {
            PersonX person = new PersonX
            {
                FirstName = "Dennis",
                LastName = "Deepwater-Diver",

                Info = new DriverInformationX
                {
                    DriverID = "N022323 2323",
                    Branch = "NYC"
                }
            };

            Console.WriteLine(ChoJSONWriter.Serialize<PersonX>(person));

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<PersonX>(json))
            {
                w.Write(person);
            }

            Console.WriteLine(json.ToString());
        }

        static void CSV2JsonIssue143()
        {
            string csv = @"Id, First Name
1, Tom
2, ";

            StringBuilder json = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithMaxScanRows(1)
                .WithFirstLineHeader())
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                //return;
                using (var w = new ChoJSONWriter(json)
                    .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.None)
                    //.Configure(c => c.DefaultArrayHandling = false)
                    //.Configure(c => c.NullValue = "")
                    .WithMaxScanNodes(1)
                    )
                {
                    w.Write(r);
                }
            }

            Console.WriteLine(json.ToString());

        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            CSV2JsonIssue143();

            return;

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<bool>(json))
                w.Write(true);
            Console.WriteLine(json.ToString());
            return;

            CSV2JSONNoIndentation();
            return;

            TimespanTest();
            return;

            string codfis = "Example1";
            var codfisValue = new
            { // codfis is the name of the variable as you can see
                Cognome = "vcgm",
                Nome = "vnm",
                Sesso = "ss",
                LuogoDiNascita = "ldn",
                Provincia = "pr",
                DataDiNascita = "ddn"
            };
            var jsonCF = new Dictionary<string, object>();
            jsonCF.Add(codfis, codfisValue);


            using (StreamWriter file = File.CreateText("CodFisCalcolati.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, jsonCF);
            }
            return;

            CustomLabel();
            return;

            string[] tt = new string[] { "1", "", "3", "" };

            var c = tt.Select((t, i) => String.IsNullOrWhiteSpace(t) ? (int?)i + 1 : null).Where(t => t != null).ToArray();
            Console.WriteLine(String.Join(",", c));
            return;
        }

        public class Project { public TimeSpan AverageScanTime { get; set; } }
        public static void TimespanTest()
        {
            var newP = new Project() { AverageScanTime = TimeSpan.FromHours(5) };

            Console.WriteLine(ChoJSONWriter.ToText(newP));
        }


        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }

        //[Test]
        public static void CustomLabel()
        {
            string expected = @"{
  ""Example1"": {
    ""Cognome"": ""vcgm"",
    ""Nome"": ""vnm"",
    ""Sesso"": ""ss"",
    ""LuogoDiNascita"": ""ldn"",
    ""Provincia"": ""pr"",
    ""DataDiNascita"": ""ddn""
  },
  ""Example11"": {
    ""Cognome"": ""vcgm"",
    ""Nome"": ""vnm"",
    ""Sesso"": ""ss"",
    ""LuogoDiNascita"": ""ldn"",
    ""Provincia"": ""pr"",
    ""DataDiNascita"": ""ddn""
  }
}";
            string actual = null;

            string codfis = "Example1";
            var jsonCF = new
            {
                codfis = new
                { // codfis is the name of the variable as you can see
                    Cognome = "vcgm",
                    Nome = "vnm",
                    Sesso = "ss",
                    LuogoDiNascita = "ldn",
                    Provincia = "pr",
                    DataDiNascita = "ddn"
                }
            };

            var jsonCF1 = new Dictionary<string, object>();
            jsonCF1.Add(codfis, new
            { // codfis is the name of the variable as you can see
                Cognome = "vcgm",
                Nome = "vnm",
                Sesso = "ss",
                LuogoDiNascita = "ldn",
                Provincia = "pr",
                DataDiNascita = "ddn"
            });
            jsonCF1.Add(codfis + "1", new
            { // codfis is the name of the variable as you can see
                Cognome = "vcgm",
                Nome = "vnm",
                Sesso = "ss",
                LuogoDiNascita = "ldn",
                Provincia = "pr",
                DataDiNascita = "ddn"
            });

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter(json)
                .SupportMultipleContent(true)
                //.Configure(c => c.SingleElement = true)
                )
            {
                w.Write(jsonCF1);
            }

            actual = json.ToString();

            Assert.AreEqual(expected, actual);
        }

        public class ArmorPOCO
        {
            public int Armor { get; set; }
            public int Strenght { get; set; }
        }

        //[Test]
        public static void DictionaryTest()
        {
            string expected = @"[
 {
   ""1"": {
     ""Armor"": 1,
     ""Strenght"": 1
   },
   ""2"": {
     ""Armor"": 2,
     ""Strenght"": 2
   }
 }
]";
            string actual = null;

            StringBuilder msg = new StringBuilder();

            Dictionary<int, ArmorPOCO> dict = new Dictionary<int, ArmorPOCO>();
            dict.Add(1, new ArmorPOCO { Armor = 1, Strenght = 1 });
            dict.Add(2, new ArmorPOCO { Armor = 2, Strenght = 2 });

            using (var w = new ChoJSONWriter<Dictionary<int, ArmorPOCO>>(msg)
                )
            {
                w.Write(dict);
            }

            actual = msg.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void CSVToJSON1()
        {
            string expected = @"[
 {
  ""Rep Employee Name"": ""CHRISTMAN, AMY"",
  ""Ship To Customer Number"": ""580788"",
  ""_Column2"": ""4543"",
  ""Ship To Customer Name"": ""dfgfdgfdgdfgdfsgfdgdfg"",
  ""_Column3"": ""6025"",
  ""_Column4"": ""5/13/2002 12:45:00 PM"",
  ""_Column5"": ""5/13/2002 2:59:00 PM"",
  ""_Column6"": ""7/2/2002 10:15:44 AM"",
  ""Product Description -Used"": ""VAC""
 },
 {
  ""_Column1"": ""34534534634"",
  ""Rep Employee Name"": ""NAGORNY, WILLIAM"",
  ""Ship To Customer Number"": ""3453"",
  ""_Column2"": ""363463"",
  ""Ship To Customer Name"": ""345435435"",
  ""_Column3"": ""6079"",
  ""_Column4"": ""5/15/2002 7:39:51 AM"",
  ""_Column5"": ""3/20/2002 11:00:00 AM"",
  ""_Column6"": ""9/25/2002 8:18:32 AM"",
  ""Product Description -Used"": ""VAC""
 },
 {
  ""_Column1"": ""34634643634"",
  ""Rep Employee Name"": ""MOORE, NICHOLAS (NICHO"",
  ""Ship To Customer Number"": ""654287"",
  ""_Column2"": ""98188"",
  ""Ship To Customer Name"": ""asdfdsfdfasasdf"",
  ""_Column3"": ""6007"",
  ""_Column4"": ""5/31/2002 2:45:16 PM"",
  ""_Column5"": ""5/31/2002 3:51:00 PM"",
  ""_Column6"": ""9/10/2002 10:51:55 AM"",
  ""Product Description -Used"": ""VAC""
 }
]";
            string actual = null;

            string csv = @"""""|""Rep Employee Name""|""Ship To Customer Number""|""""|""Ship To Customer Name""|""Patient Last Name""|""Patient First Name""|""Patient Location""|""""|""""|""""|""""|""Serial Number""|""Product Description -Used""
"""" | ""CHRISTMAN, AMY"" | ""580788"" | ""4543"" | ""dfgfdgfdgdfgdfsgfdgdfg"" | """" | """" | """" | ""6025"" | ""5/13/2002 12:45:00 PM"" | ""5/13/2002 2:59:00 PM"" | ""7/2/2002 10:15:44 AM"" | """" | ""VAC""
""34534534634"" | ""NAGORNY, WILLIAM"" | ""3453"" | ""363463"" | ""345435435"" | """" | """" | """" | ""6079"" | ""5/15/2002 7:39:51 AM"" | ""3/20/2002 11:00:00 AM"" | ""9/25/2002 8:18:32 AM"" | """" | ""VAC""
""34634643634"" | ""MOORE, NICHOLAS (NICHO"" | ""654287"" | ""98188"" | ""asdfdsfdfasasdf"" | """" | """" | """" | ""6007"" | ""5/31/2002 2:45:16 PM"" | ""5/31/2002 3:51:00 PM"" | ""9/10/2002 10:51:55 AM"" | """" | ""VAC""";

            StringBuilder json = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithDelimiter("|")
                .WithFirstLineHeader()
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true)
                .Configure(c => c.QuoteAllFields = true)
                .Configure(c => c.NullValue = "")
                )
            {
                using (var w = new ChoJSONWriter(json)
                    //.Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any)
                    .Configure(c => c.NullValueHandling = ChoNullValueHandling.Ignore)
                    )
                {
                    w.Write(p);
                }
            }

            actual = json.ToString();

            Assert.AreEqual(expected, actual);
        }

        public class data
        {
            public int Id { get; set; }
            public int SSN { get; set; }
            public string Message { get; set; }

        }
        //[Test]
        public static void ListTest()
        {
            string expected = @"[
 {
  ""Id"": 1,
  ""SSN"": 2,
  ""Message"": ""A Message""
 }
]";
            string actual = null;

            List<data> _data = new List<data>();
            _data.Add(new data()
            {
                Id = 1,
                SSN = 2,
                Message = "A Message"
            });

            actual = ChoJSONWriter.ToTextAll<data>(_data);

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void CustomFormat2()
        {
            string expected = @"[
 {
  ""Place"": ""1"",
  ""SkuNumber"": 100
 }
]";
            string actual = null;

            StringBuilder sb = new StringBuilder();

            using (var w = new ChoJSONWriter<PlaceObj>(sb)
            //.WithField(m => m.SkuNumber, valueConverter: (o) => String.Format("SKU_{0}", o.ToNString()))
            )
            {
                PlaceObj o1 = new PlaceObj();
                o1.Place = "1";
                o1.SkuNumber = 100;

                w.Write(o1);
            }

            actual = sb.ToString();
        }

        //[Test]
        public static void CustomFormat1()
        {
            string expected = @"[
 {
  ""Place"": 1,
  ""SkuNumber"": ""SKU_100""
 }
]";
            string actual = null;

            StringBuilder sb = new StringBuilder();

            using (var w = new ChoJSONWriter(sb)
                .WithField("Place")
                .WithField("SkuNumber", valueConverter: (o) => String.Format("SKU_{0}", o.ToNString()))
                )
            {
                dynamic o1 = new ExpandoObject();
                o1.Place = 1;
                o1.SkuNumber = 100;

                w.Write(o1);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        #region Sample50

        public class Rootobject
        {
            public Home home { get; set; }
            public Away away { get; set; }
        }

        public class Home
        {
            [ChoUseJSONSerialization]
            public _0_15 _0_15 { get; set; }
            public _15_30 _15_30 { get; set; }
            public _30_45 _30_45 { get; set; }
            public _45_60 _45_60 { get; set; }
            public _60_75 _60_75 { get; set; }
            public _75_90 _75_90 { get; set; }
        }

        public class _0_15
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class _15_30
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class _30_45
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class _45_60
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class _60_75
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class _75_90
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class Away
        {
            public _0_151 _0_15 { get; set; }
            public _15_301 _15_30 { get; set; }
            public _30_451 _30_45 { get; set; }
            public _45_601 _45_60 { get; set; }
            public _60_751 _60_75 { get; set; }
            public _75_901 _75_90 { get; set; }
        }

        public class _0_151
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }

        public class _15_301
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }

        public class _30_451
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }

        public class _45_601
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }

        public class _60_751
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }

        public class _75_901
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }
        //[Test]
        public static void Sample50()
        {
            string expected = @"{
 ""home"": {
   ""_0_15"": {
     ""goals"": 7,
     ""percentage"": 14
   },
   ""_15_30"": {
     ""goals"": 6,
     ""percentage"": 12
   },
   ""_30_45"": null,
   ""_45_60"": {
     ""goals"": 4,
     ""percentage"": 8
   },
   ""_60_75"": null,
   ""_75_90"": null
 },
 ""away"": {
   ""_0_15"": null,
   ""_15_30"": {
     ""goals"": 7,
     ""percentage"": 15.56
   },
   ""_30_45"": null,
   ""_45_60"": null,
   ""_60_75"": {
     ""goals"": 13,
     ""percentage"": 28.89
   },
   ""_75_90"": {
     ""goals"": 7,
     ""percentage"": 15.56
   }
 }
}";
            string actual = null;

            string json = @"
{
    ""home"": {
        ""_0_15"": {
            ""goals"": 7,
            ""percentage"": 14
        },
        ""_15_30"": {
            ""goals"": 6,
            ""percentage"": 12
        },
        ""30_45"": {
            ""goals"": 11,
            ""percentage"": 22
        },
        ""_45_60"": {
            ""goals"": 4,
            ""percentage"": 8
        },
        ""60_75"": {
            ""goals"": 8,
            ""percentage"": 16
        },
        ""75_90"": {
            ""goals"": 14,
            ""percentage"": 28
        }
    },
    ""away"": {
        ""0_15"": {
            ""goals"": 7,
            ""percentage"": 15.56
        },
        ""_15_30"": {
            ""goals"": 7,
            ""percentage"": 15.56
        },
        ""30_45"": {
            ""goals"": 5,
            ""percentage"": 11.11
        },
        ""45_60"": {
            ""goals"": 6,
            ""percentage"": 13.33
        },
        ""_60_75"": {
            ""goals"": 13,
            ""percentage"": 28.89
        },
        ""_75_90"": {
            ""goals"": 7,
            ""percentage"": 15.56
        }
    }
}";

            using (var p = ChoJSONReader<Rootobject>.LoadText(json).Configure(c => c.SupportMultipleContent = true))
            {
                //foreach (var rec in p)
                //	Console.WriteLine(rec.Dump());
                actual = ChoJSONWriter<Rootobject>.ToText(p.First(), new ChoJSONRecordConfiguration().Configure(c => c.SupportMultipleContent = true));
            }

            Assert.AreEqual(expected, actual);
        }

        #endregion Sample50

        #region Nested2NestedObjectTest

        public class Customer
        {
            public long Id { get; set; }

            public string UserName { get; set; }

            public AddressModel Address { get; set; }

            public override bool Equals(object obj)
            {
                var customer = obj as Customer;
                return customer != null &&
                       Id == customer.Id &&
                       UserName == customer.UserName &&
                       EqualityComparer<AddressModel>.Default.Equals(Address, customer.Address);
            }

            public override int GetHashCode()
            {
                var hashCode = 1849171668;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(UserName);
                hashCode = hashCode * -1521134295 + EqualityComparer<AddressModel>.Default.GetHashCode(Address);
                return hashCode;
            }
        }

        public class AddressModel
        {
            public string Address { get; set; }

            public string Address2 { get; set; }

            public string City { get; set; }

            public override bool Equals(object obj)
            {
                var model = obj as AddressModel;
                return model != null &&
                       Address == model.Address &&
                       Address2 == model.Address2 &&
                       City == model.City;
            }

            public override int GetHashCode()
            {
                var hashCode = -1389774896;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Address);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Address2);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(City);
                return hashCode;
            }
        }

        //[Test]
        public static void Nested2NestedObjectTest()
        {
            Customer expected = new Customer { Id = 123, UserName = "fflintstone", Address = new AddressModel { Address = "345 Cave Stone Road", Address2 = "", City = "Bedrock" } };
            object actual = null;

            string json = @"{ 
""id"": 123,   ""userName"": ""fflintstone"",   ""Address"": {
""address"": ""345 Cave Stone Road"",
""address2"": """",
""city"": ""Bedrock"",
""state"": ""AZ"",
""zip"": """"   } }";

            using (var p = ChoJSONReader<Customer>.LoadText(json).WithFlatToNestedObjectSupport(false)
                //.WithField(r => r.Address.Address, fieldName: "Address")
                )
            {
                actual = p.First();
            }

            Assert.AreEqual(expected, actual);
        }

        #endregion Nested2NestedObjectTest

        //[Test]
        public static void NestedObjectTest()
        {
            Customer expected = new Customer { Id = 123, UserName = "fflintstone", Address = new AddressModel { Address = "345 Cave Stone Road", Address2 = "", City = "Bedrock" } };
            object actual = null;

            string json = @"{
    ""id"": 123,
    ""userName"": ""fflintstone"",
    ""address"": ""345 Cave Stone Road"",
    ""address2"": """",
    ""city"": ""Bedrock"",
    ""state"": ""AZ"",
    ""zip"": """",   
}";

            using (var p = ChoJSONReader<Customer>.LoadText(json).WithFlatToNestedObjectSupport(true)
                //.WithField(r => r.Address.Address, fieldName: "Address")
                )
            {
                actual = p.First();
            }

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void CombineJSONTest()
        {
            string expected = @"[
 {
  ""deliveryDay"": ""2018-06-19T14:00:00+02:00"",
  ""currencyCode"": ""TRY"",
  ""offerType"": ""HOURLY"",
  ""regionCode"": ""TR1"",
  ""offerDetails"": {
    ""startPeriod"": ""1"",
    ""duration"": ""1"",
    ""offerPrices"": [
      {
        ""price"": ""0"",
        ""amount"": ""5""
      },
      {
        ""price"": ""2000"",
        ""amount"": ""5""
      }
    ]
  }
 }
]";
            string actual = null;

            string json = @"[
  {
    ""deliveryDay"": ""2018-06-19T15:00:00.000+0300"",
    ""currencyCode"": ""TRY"",
    ""offerType"": ""HOURLY"",
    ""regionCode"": ""TR1"",
    ""offerDetails"": [
         {
           ""startPeriod"": ""1"",
            ""duration"": ""1"",
            ""offerPrices"": [
                {
                  ""price"": ""0"",
                   ""amount"": ""5""
                 }
               ]
             }
           ]
          },
  {
   ""deliveryDay"": ""2018-06-19T15:00:00.000+0300"",
   ""currencyCode"": ""TRY"",
   ""offerType"": ""HOURLY"",
   ""regionCode"": ""TR1"",
   ""offerDetails"": [
         {
           ""startPeriod"": ""1"",
           ""duration"": ""1"",
           ""offerPrices"": [
                {
                  ""price"": ""2000"",
                   ""amount"": ""5""
               }
              ]
            }
          ]
        }
       ]";


            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json))
            {
                var list = p.GroupBy(r => r.deliveryDay).Select(r => new
                {
                    deliveryDay = r.Key,
                    r.First().currencyCode,
                    r.First().offerType,
                    r.First().regionCode,
                    offerDetails = new
                    {
                        ((Array)r.First().offerDetails).OfType<dynamic>().First().startPeriod,
                        ((Array)r.First().offerDetails).OfType<dynamic>().First().duration,
                        offerPrices = r.Select(r1 => ((Array)r1.offerDetails[0].offerPrices).OfType<object>().First()).ToArray()
                    }
                }).ToArray();

                actual = ChoJSONWriter.ToTextAll(list);

                Assert.AreEqual(expected, actual);
                //foreach (var rec in )
                //	Console.WriteLine(rec.Dump());
            }
        }

        //[Test]
        public static void ComplexObjTest()
        {
            string expected = @"[
 {
  ""firstName"": ""Markoff"",
  ""lastName"": ""Chaney"",
  ""dateOfBirth"": {
    ""year"": 1901,
    ""month"": 4,
    ""day"": 30
  }
 }
]";
            string actual = null;

            StringBuilder sb = new StringBuilder();
            var obj = new Lad
            {
                firstName = "Markoff",
                lastName = "Chaney",
                dateOfBirth = new MyDate
                {
                    year = 1901,
                    month = 4,
                    day = 30
                }
            };
            using (var jr = new ChoJSONWriter<Lad>(sb)
                )
            {
                jr.Write(obj);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void EnumTest()
        {
            string expected = @"[
 {
  ""Age"": 1,
  ""Gender"": ""1""
 }
]";
            string actual = null;

            StringBuilder sb = new StringBuilder();
            using (var jr = new ChoJSONWriter<Person>(sb)
                )
            {
                jr.Write(new Person { Age = 1, Gender = Gender.Female });
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void EnumLoadTest()
        {
            List<object> expected = new List<object>
            {
                new Person { Age = 1, Gender = Gender.Female}
            };
            List<object> actual = new List<object>();

            string json = @"[
 {
  ""Age"": 1,
  ""Gender"": ""F""
 }
]";

            using (var p = ChoJSONReader<Person>.LoadText(json))
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void IPAddressTest()
        {
            using (var jr = new ChoJSONWriter<SomeOuterObject>(FileNameIPAddressTestActualJSON)
                .WithField("stringValue")
                .WithField("ipValue", valueConverter: (o) => o.ToString())
                )
            {
                var x1 = new SomeOuterObject { stringValue = "X1", ipValue = IPAddress.Parse("12.23.21.23") };
                jr.Write(x1);
            }

            FileAssert.AreEqual(FileNameIPAddressTestExpectedJSON, FileNameIPAddressTestActualJSON);

        }

        //[Test]
        public static void NestedJSONFile()
        {
            var dataMapperModels = new List<DataMapper>();
            var model = new DataMapper
            {
                Name = "performanceLevels",
                SubDataMappers = new List<DataMapper>()
            {
                new DataMapper()
                {
                    Name = "performanceLevel_1",
                    SubDataMappers = new List<DataMapper>()
                    {
                        new DataMapper()
                        {
                            Name = "title",
                            DataMapperProperty = new DataMapperProperty()
                                    {
                                        Source = "column",
                                        SourceColumn = "title-column",
                                        DataType = "string",
                                        Default = "N/A",
                                        SourceTable = null,
                                        Value = null
                                    }
                        },
                        new DataMapper()
                        {
                            Name = "version1",
                            DataMapperProperty = new DataMapperProperty()
                                    {
                                        Source = "column",
                                        SourceColumn = "version-column",
                                        DataType = "int",
                                        Default = "1",
                                        SourceTable = null,
                                        Value = null
                                    }
                        },
                        new DataMapper()
                        {
                            Name = "threeLevels",
                            SubDataMappers = new List<DataMapper>()
                            {
                                new DataMapper()
                                {
                                    Name = "version",
                                    DataMapperProperty = new DataMapperProperty()
                                            {
                                                Source = "column",
                                                SourceColumn = "version-column",
                                                DataType = "int",
                                                Default = "1",
                                                SourceTable = null,
                                                Value = null
                                            }
                                }
                            }
                        }
                    }
                },
                new DataMapper()
                {
                    Name = "performanceLevel_2",
                    SubDataMappers = new List<DataMapper>()
                    {
                        new DataMapper()
                        {
                            Name = "title",
                            DataMapperProperty = new DataMapperProperty()
                                    {
                                        Source = "column",
                                        SourceColumn = "title-column",
                                        DataType = "string",
                                        Default = "N/A",
                                        SourceTable = null,
                                        Value = null
                                    }
                        },
                        new DataMapper()
                        {
                            Name = "version",
                            DataMapperProperty = new DataMapperProperty()
                                    {
                                        Source = "column",
                                        SourceColumn = "version-column",
                                        DataType = "int",
                                        Default = "1",
                                        SourceTable = null,
                                        Value = null
                                    }
                        }
                    }
                }
            }
            };

            dataMapperModels.Add(model);
            using (var w = new ChoJSONWriter(FileNameNestedJSONFileActualJSON)
            )
                w.Write(dataMapperModels);

            FileAssert.AreEqual(FileNameNestedJSONFileExpectedJSON, FileNameNestedJSONFileActualJSON);
        }

        public static string FileNameSample7JSON => "sample7.json";
        public static string FileNameSample7WriteActualJSON => "sample7WriteActual.json";
        public static string FileNameSample7WriteExpectedJSON => "sample7WriteExpected.json";
        public static string FileNameSaveDictActualJSON => "SaveDictActual.json";
        public static string FileNameSaveDictExpectedJSON => "SaveDictExpected.json";
        public static string FileNameSaveStringListActualJSON => "SaveStringListActual.json";
        public static string FileNameSaveStringListExpectedJSON => "SaveStringListExpected.json";

        //[Test]
        public static void Sample7Read()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {{"id",0}, { "married",true}, { "name","John Lee"}, {"sons", new object[] { new ChoDynamicObject {{"age", (long)15},{ "name", "Ronald"}, { "address", new ChoDynamicObject { { "street", "abc street" },{ "city", "edison"},{ "state","NJ" } } } } } }, { "daughters", new Dictionary<string, object>[] { new Dictionary<string, object> { {"age",(long)7},{"name","Amy"}},
                new Dictionary<string, object> { {"age",(long)29},{"name","Carol"}},
                new Dictionary<string, object> { {"age",(long)14},{"name","Barbara"}} } } },

                            new ChoDynamicObject {{"id",1}, { "married",false}, { "name","Kenneth Gonzalez"}, {"sons", new object[] { } }, { "daughters", new Dictionary<string, object>[] { } } },

                            new ChoDynamicObject {{"id",2}, { "married",false}, { "name","Larry Lee"}, {"sons", new object[] { new ChoDynamicObject {{"age", (long)4},{ "name", "Anthony"} }, new ChoDynamicObject { { "age", (long)2 }, { "name", "Donald" } } } }, { "daughters", new Dictionary<string, object>[] { new Dictionary<string, object> { {"age",(long)7},{"name","Elizabeth"}},
                new Dictionary<string, object> { {"age",(long)15},{"name","Betty"}} } } }
            };
            List<object> actual = null;

            using (var jr = new ChoJSONReader(FileNameSample7JSON).WithJSONPath("$.fathers")
                .WithField("id")
                .WithField("married", fieldType: typeof(bool))
                .WithField("name")
                .WithField("sons")
                .WithField("daughters", fieldType: typeof(Dictionary<string, object>[]))
                )
            {
                actual = jr.ToList();

                /*
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
                */
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        //[Test]
        public static void Sample7Write()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {{"id",0}, { "married",true}, { "name","John Lee"}, {"sons", new object[] { new ChoDynamicObject {{"age", (long)15},{ "name", "Ronald"}, { "address", new ChoDynamicObject { { "street", "abc street" },{ "city", "edison"},{ "state","NJ" } } } } } }, { "daughters", new Dictionary<string, object>[] { new Dictionary<string, object> { {"age",(long)7},{"name","Amy"}},
                new Dictionary<string, object> { {"age",(long)29},{"name","Carol"}},
                new Dictionary<string, object> { {"age",(long)14},{"name","Barbara"}} } } },

                            new ChoDynamicObject {{"id",1}, { "married",false}, { "name","Kenneth Gonzalez"}, {"sons", new object[] { } }, { "daughters", new Dictionary<string, object>[] { } } },

                            new ChoDynamicObject {{"id",2}, { "married",false}, { "name","Larry Lee"}, {"sons", new object[] { new ChoDynamicObject {{"age", (long)4},{ "name", "Anthony"} }, new ChoDynamicObject { { "age", (long)2 }, { "name", "Donald" } } } }, { "daughters", new Dictionary<string, object>[] { new Dictionary<string, object> { {"age",(long)7},{"name","Elizabeth"}},
                new Dictionary<string, object> { {"age",(long)15},{"name","Betty"}} } } }
            };
            using (var w = new ChoJSONWriter(FileNameSample7WriteActualJSON))
            {
                w.Write(expected);
            }

            FileAssert.AreEqual(FileNameSample7WriteExpectedJSON, FileNameSample7WriteActualJSON);
        }
        public static string FileNameSampleCSV => "sample.csv";
        public static string FileNameSampleActualJSON => "sampleActual.json";
        public static string FileNameSampleExpectedJSON => "sampleExpected.json";
        public static string FileNameDynamicTestActualJSON => "DynamicTestActual.json";
        public static string FileNameDynamicTestExpectedJSON => "DynamicTestExpected.json";
        public static string FileNameIPAddressTestActualJSON => "IPAddressTestActual.json";
        public static string FileNameIPAddressTestExpectedJSON => "IPAddressTestExpected.json";
        public static string FileNameNestedJSONFileActualJSON => "NestedJSONFileActual.json";
        public static string FileNameNestedJSONFileExpectedJSON => "NestedJSONFileExpected.json";
        public static string FileNamePOCOTestActualJSON => "POCOTestActual.json";
        public static string FileNamePOCOTestExpectedJSON => "POCOTestExpected.json";


        //[Test]
        public static void ConvertAllDataWithNativetype()
        {
            using (var jw = new ChoJSONWriter(FileNameSampleActualJSON))
            {
                using (var cr = new ChoCSVReader(FileNameSampleCSV)
                    .WithFirstLineHeader()
                    .WithField("firstName")
                    .WithField("lastName")
                    .WithField("salary", fieldType: typeof(double))
                    )
                {
                    //foreach (var x in cr)
                    //    Console.WriteLine(ChoUtility.ToStringEx(x));
                    jw.Write(cr);
                }
            }

            FileAssert.AreEqual(FileNameSampleExpectedJSON, FileNameSampleActualJSON);
        }
        //[Test]
        public static void SaveDict()
        {
            Dictionary<int, string> list = new Dictionary<int, string>();
            list.Add(1, "1/1/2012");
            list.Add(2, null);
            //Hashtable list = new Hashtable();
            //list.Add(1, "33");
            //list.Add(2, null);

            using (var w = new ChoJSONWriter(FileNameSample7WriteActualJSON)
                )
                w.Write(list);

            FileAssert.AreEqual(FileNameSaveDictExpectedJSON, FileNameSaveDictActualJSON);
        }
        //[Test]
        public static void SaveStringList()
        {
            //List<EmpType?> list = new List<EmpType?>();
            //list.Add(EmpType.Contract);
            //list.Add(null);

            //List<int?> list = new List<int?>();
            //list.Add(1);
            //list.Add(null);

            //int[] list = new int[] { 11, 21 };
            ArrayList list = new ArrayList();
            list.Add(1);
            list.Add("asas");
            list.Add(null);

            using (var w = new ChoJSONWriter(FileNameSaveStringListActualJSON)
                .WithField("Value")
                )
                w.Write(list);

            FileAssert.AreEqual(FileNameSaveStringListExpectedJSON, FileNameSaveStringListActualJSON);
        }

        //[Test]
        public static void DataTableTest()
        {
            StringBuilder sb = new StringBuilder();
            string connectionstring = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Northwind;Integrated Security=True";
            using (var conn = new SqlConnection(connectionstring))
            {
                conn.Open();
                var comm = new SqlCommand("SELECT TOP 2 * FROM Customers", conn);
                SqlDataAdapter adap = new SqlDataAdapter(comm);

                DataTable dt = new DataTable("Customer");
                adap.Fill(dt);

                using (var parser = new ChoJSONWriter(sb)
                    .Configure(c => c.IgnoreRootName = true)
                    )
                    parser.Write(dt);
            }

            Console.WriteLine(sb.ToString());

            //Assert.Fail("Make database testable");
        }

        //[Test]
        public static void DataReaderTest()
        {
            //string connectionstring = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True";
            string connectionstring = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Northwind;Integrated Security=True";
            StringBuilder sb = new StringBuilder();
            using (var conn = new SqlConnection(connectionstring))
            {
                conn.Open();
                var comm = new SqlCommand("SELECT top 2 * FROM Customers", conn);
                using (var parser = new ChoJSONWriter(sb))
                    parser.Write(comm.ExecuteReader());
            }

            Console.WriteLine(sb.ToString());

            Assert.Fail("Make database testable");
        }

        //[Test]
        public static void POCOTest()
        {
            List<EmployeeRecSimple1> objs = new List<EmployeeRecSimple1>();
            EmployeeRecSimple1 rec1 = new EmployeeRecSimple1();
            rec1.Id = 1;
            rec1.Name = "Mark";
            objs.Add(rec1);

            objs.Add(null);

            using (var w = new ChoJSONWriter<EmployeeRecSimple1>(FileNamePOCOTestActualJSON)
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure(e => e.NullValueHandling = ChoNullValueHandling.Empty)
                )
            {
                w.Write(objs);

                //w.Write(ChoEnumerable.AsEnumerable(() =>
                //{
                //    return new { Address = new string[] { "NJ", "NY" }, Name = "Raj", Zip = "08837" };
                //}));
                //w.Write(new { Name = "Raj", Zip = "08837", Address = new { City = "New York", State = "NY" } });
            }

            FileAssert.AreEqual(FileNamePOCOTestExpectedJSON, FileNamePOCOTestActualJSON);
        }

        //[Test]
        public static void DynamicTest()
        {
            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";
            rec1.Date = new DateTime(2019, 12, 3, 17, 34, 23, 421, DateTimeKind.Utc);
            rec1.Active = true;
            rec1.Salary = new ChoCurrency(10.01);
            rec1.EmpType = EmpType.FullTime;
            rec1.Array = new int[] { 1, 2, 4 };
            rec1.Dict = new Dictionary<int, string>() { { 1, "xx" } };
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Jason";
            rec2.Date = new DateTime(2019, 12, 3, 17, 34, 23, 421, DateTimeKind.Utc);
            rec2.Salary = new ChoCurrency(10.01);
            rec2.Active = false;
            rec2.EmpType = EmpType.Contract;
            rec2.Array = new int[] { 1, 2, 4 };
            rec2.Dict = new string[] { "11", "12", "14" };

            objs.Add(rec2);
            objs.Add(null);

            using (var w = new ChoJSONWriter(FileNameDynamicTestActualJSON)
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure(c => c.NullValueHandling = ChoNullValueHandling.Empty)
                )
            {
                w.Write(objs);

                //w.Write(ChoEnumerable.AsEnumerable(() =>
                //{
                //    return new { Address = new string[] { "NJ", "NY" }, Name = "Raj", Zip = "08837" };
                //}));
                //w.Write(new { Name = "Raj", Zip = "08837", Address = new { City = "New York", State = "NY" } });
            }

            FileAssert.AreEqual(FileNameDynamicTestExpectedJSON, FileNameDynamicTestActualJSON);
        }
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

        public object Value
        {
            get
            {
                if (SubDataMappers.IsNullOrEmpty())
                    return (object)DataMapperProperty;
                else
                {
                    ChoDynamicObject obj = new ChoDynamicObject();
                    foreach (var item in SubDataMappers)
                        obj.AddOrUpdate(item.Name, item.Value);
                    return obj;
                }
            }
            set
            {
                if (value is DataMapperProperty)
                    DataMapperProperty = value as DataMapperProperty;
                else if (value is IEnumerable)
                {

                }
            }
        }

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

    public partial class EmployeeRecSimple1
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<EmployeeRecSimple1> SubEmployeeRecSimple1;
    }

    public class LocationListJsonConverter : IChoValueConverter
    {
        public JsonSerializer Serializer { get; set; }
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var locationList = value as LocationList;

            JObject jLocationList = new JObject();

            if (locationList.IsExpanded)
                jLocationList.Add("IsExpanded", true);

            if (locationList.Count > 0)
            {
                var jLocations = new JArray();

                foreach (var location in locationList)
                {
                    jLocations.Add(JObject.FromObject(location, new JsonSerializer()));
                }

                jLocationList.Add("Locations", jLocations);

            }

            return jLocationList.ToString();

        }

        protected virtual object Deserialize(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null && value.GetType().IsCollectionType())
            {
                if (targetType != typeof(object) && !targetType.IsSimple() && !typeof(ICollection).IsAssignableFrom(targetType))
                {
                    IList coll = value as IList;
                    var itemType = value.GetType().GetItemType();
                    if (itemType.IsSimple())
                    {
                        value = ChoActivator.CreateInstance(targetType);
                        foreach (var p in ChoTypeDescriptor.GetProperties<ChoArrayIndexAttribute>(targetType).Select(pd => new { pd, a = ChoTypeDescriptor.GetPropetyAttribute<ChoArrayIndexAttribute>(pd) })
                            .GroupBy(g => g.a.Position).Select(g => g.First()).Where(g => g.a.Position >= 0).OrderBy(g => g.a.Position))
                        {
                            if (p.a.Position < coll.Count)
                            {
                                ChoType.SetPropertyValue(value, p.pd.Name, coll[p.a.Position]);
                            }
                        }
                    }
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var locationList = value as LocationList;

            JObject jLocationList = new JObject();

            if (locationList.IsExpanded)
                jLocationList.Add("IsExpanded", true);

            if (locationList.Count > 0)
            {
                var jLocations = new JArray();

                foreach (var location in locationList)
                {
                    jLocations.Add(JObject.FromObject(location, Serializer == null ? new JsonSerializer() : Serializer));
                }

                jLocationList.Add("Locations", jLocations);

            }

            return jLocationList;
        }

        protected virtual object Serialize(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            IList result = ChoActivator.CreateInstance(typeof(IList<>).MakeGenericType(targetType)) as IList;

            if (value != null && !value.GetType().IsCollectionType())
            {
                if (targetType == typeof(object) || targetType.IsSimple())
                {
                    foreach (var p in ChoTypeDescriptor.GetProperties(value.GetType()).Where(pd => ChoTypeDescriptor.GetPropetyAttribute<ChoIgnoreMemberAttribute>(pd) == null))
                    {
                        result.Add(ChoConvert.ConvertTo(ChoType.GetPropertyValue(value, p.Name), targetType, culture));
                    }
                }
            }

            return result.OfType<object>().ToArray();
        }
    }
}


