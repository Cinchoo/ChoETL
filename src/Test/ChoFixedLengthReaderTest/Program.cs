using ChoETL;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTestHelper;

namespace ChoFixedLengthReaderTest
{
    [ChoFixedLengthFileHeader]
    public class CreditBalanceRecordMetaData : IChoNotifyRecordRead
    {
        public bool AfterRecordFieldLoad(object target, long index, string propName, object value)
        {
            throw new NotImplementedException();
        }

        public bool AfterRecordLoad(object target, long index, object source, ref bool skip)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordLoad(object target, long index, ref object source)
        {
            throw new NotImplementedException();
        }

        public bool BeginLoad(object source)
        {
            throw new NotImplementedException();
        }

        public bool DoWhile(long index, object source)
        {
            throw new NotImplementedException();
        }

        public void EndLoad(object source)
        {
            throw new NotImplementedException();
        }

        public bool RecordFieldLoadError(object target, long index, string propName, object value, Exception ex)
        {
            throw new NotImplementedException();
        }

        public bool RecordLoadError(object target, long index, object source, Exception ex)
        {
            throw new NotImplementedException();
        }

        public bool SkipUntil(long index, object source)
        {
            return true;
        }
    }

    [MetadataType(typeof(CreditBalanceRecordMetaData))]
    [ChoFixedLengthFileHeader]
    [ChoFixedLengthRecordObject(ColumnCountStrict = true)]
    public class CreditBalanceRecord
    {
        [ChoFixedLengthRecordField(0, 8)]
        public int Account { get; set; }
        [ChoFixedLengthRecordField(8, 16)]
        public string LastName { get; set; }
        [ChoFixedLengthRecordField(24, 16)]
        public string FirstName { get; set; }
        [ChoFixedLengthRecordField(40, 12)]
        public double Balance { get; set; }
        [ChoFixedLengthRecordField(52, 14)]
        public double CreditLimit { get; set; }
        [ChoFixedLengthRecordField(66, 16)]
        public DateTime AccountCreated { get; set; }
        [ChoFixedLengthRecordField(82, 7)]
        public string Rating { get; set; }

        public override bool Equals(object obj)
        {
            var record = obj as CreditBalanceRecord;
            return record != null &&
                   Account == record.Account &&
                   LastName == record.LastName &&
                   FirstName == record.FirstName &&
                   Balance == record.Balance &&
                   CreditLimit == record.CreditLimit &&
                   AccountCreated == record.AccountCreated &&
                   Rating == record.Rating;
        }

        public override int GetHashCode()
        {
            var hashCode = -985661466;
            hashCode = hashCode * -1521134295 + Account.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LastName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FirstName);
            hashCode = hashCode * -1521134295 + Balance.GetHashCode();
            hashCode = hashCode * -1521134295 + CreditLimit.GetHashCode();
            hashCode = hashCode * -1521134295 + AccountCreated.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Rating);
            return hashCode;
        }
    }

    public class CreditBalanceRecordEx
    {
        public int Account { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public double Balance { get; set; }
        public double CreditLimit { get; set; }
        public DateTime AccountCreated { get; set; }
        public string Rating { get; set; }
    }

    public class EmployeeRecWithCurrency
    {
        [ChoFixedLengthRecordField(0, 8)]
        public int? Id { get; set; }
        [ChoFixedLengthRecordField(8, 10)]
        public string Name { get; set; }
        [ChoIgnoreMember]
        [ChoFixedLengthRecordField(18, 28)]
        public ChoCurrency Salary { get; set; }

        public override bool Equals(object obj)
        {
            var currency = obj as EmployeeRecWithCurrency;
            return currency != null &&
                   EqualityComparer<int?>.Default.Equals(Id, currency.Id) &&
                   Name == currency.Name &&
                   Salary.Equals(currency.Salary);
        }

        public override int GetHashCode()
        {
            var hashCode = -1858601383;
            hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<ChoCurrency>.Default.GetHashCode(Salary);
            return hashCode;
        }
    }

    //[ChoFixedLengthRecordObject(recordLength: 25)]
    public abstract class AABillingRecord
    {

    }

    [ChoRecordTypeCode("H")]
    public class AABillingHeaderRecord : AABillingRecord //, IChoNotifyRecordRead
    {
        [ChoFixedLengthRecordField(1, 8)]
        [ChoTypeConverter(typeof(ChoDateTimeConverter), Parameters = "yyyyMMdd")]
        public DateTime BusinessDate { get; set; }
        [ChoFixedLengthRecordField(9, 16)]
        public string Description { get; set; }

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

        public bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value)
        {
            if (propName == "BusinessDate")
                value = DateTime.ParseExact(value.ToNString(), "yyyyMMdd", null);

            return true;
        }

        public bool AfterRecordFieldLoad(object target, long index, string propName, object value)
        {
            throw new NotImplementedException();
        }

        public bool RecordFieldLoadError(object target, long index, string propName, object value, Exception ex)
        {
            throw new NotImplementedException();
        }
    }

    public class AABillingDetailRecord : AABillingRecord
    {
        [ChoFixedLengthRecordField(0, 10)]
        public int ClientID { get; set; }
        [ChoFixedLengthRecordField(10, 15)]
        public string ClientName { get; set; }
    }

    [ChoRecordTypeCode("T")]
    public class AABillingTrailerRecord : AABillingRecord
    {
        [ChoFixedLengthRecordField(1, 24)]
        public int RecordCount { get; set; }
    }

    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {
        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }

        [Test]
        public static void AABillingTest()
        {
            string data = @"H20170504Sample file     
1234567890Chubb          
T10                      ";

            string expected = @"{
  ""$type"": ""System.Object[], mscorlib"",
  ""$values"": [
    {
      ""$type"": ""ChoFixedLengthReaderTest.AABillingHeaderRecord, ChoFixedLengthReaderTest"",
      ""BusinessDate"": ""2017-05-04T00:00:00"",
      ""Description"": ""Sample file""
    },
    {
      ""$type"": ""ChoFixedLengthReaderTest.Program+AABillingDetailRecord, ChoFixedLengthReaderTest"",
      ""ClientID"": 1234567890,
      ""ClientName"": ""Chubb""
    },
    {
      ""$type"": ""ChoFixedLengthReaderTest.AABillingTrailerRecord, ChoFixedLengthReaderTest"",
      ""RecordCount"": 10
    }
  ]
}";
            using (var p = ChoFixedLengthReader.LoadText(data)
                .WithRecordSelector(0, 1, typeof(AABillingDetailRecord), typeof(AABillingTrailerRecord), typeof(AABillingHeaderRecord))
                )
            {
                var recs = p.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented, new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All
                });
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void AABillingTest1()
        {
            string data = @"H20170504Sample file     
1234567890Chubb          
T10                      ";

            string expected = @"{
  ""$type"": ""System.Object[], mscorlib"",
  ""$values"": [
    {
      ""$type"": ""ChoFixedLengthReaderTest.AABillingHeaderRecord, ChoFixedLengthReaderTest"",
      ""BusinessDate"": ""2017-05-04T00:00:00"",
      ""Description"": ""Sample file""
    },
    {
      ""$type"": ""ChoFixedLengthReaderTest.Program+AABillingDetailRecord, ChoFixedLengthReaderTest"",
      ""ClientID"": 1234567890,
      ""ClientName"": ""Chubb""
    },
    {
      ""$type"": ""ChoFixedLengthReaderTest.AABillingTrailerRecord, ChoFixedLengthReaderTest"",
      ""RecordCount"": 10
    }
  ]
}";
            using (var p = ChoFixedLengthReader.LoadText(data)
                .WithCustomRecordSelector((l) =>
                {
                    Tuple<long, string> kvp = l as Tuple<long, string>;
                    if (kvp.Item2.StartsWith("H"))
                        return typeof(AABillingHeaderRecord);
                    else if (kvp.Item2.StartsWith("T"))
                        return typeof(AABillingTrailerRecord);
                    else
                        return typeof(AABillingDetailRecord);
                })
                )
            {
                var recs = p.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented, new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.All
                });
                Assert.AreEqual(expected, actual);
            }
        }

        public class AccountBalance
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public List<string> lastTwelveMonths { get; set; }
        }

        [Test]
        public static void NestedObjectTest()
        {
            string expected = @"ID   Name Mon,T
00001Anne 1,2  
00002John 1,2  
00003Brit 1,2  ";
            string actual = null;

            string csv = @"AccountId, Name, Jan, Feb, Mar, Dec
1, Anne, 1000.00, 400.00, 500.00,200.00
2, John, 900.00, 500.00, 500.00,1200.00
3, Brit, 600.00, 600.00, 500.00,2200.00";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader<AccountBalance>.LoadText(csv)
                .WithFirstLineHeader(true)
                .WithField(m => m.lastTwelveMonths, valueSelector: v =>
                {
                    List<string> list = new List<string>();
                    //list.Add(v.Column5);
                    list.Add(v.Jan);
                    list.Add(v.Feb);
                    list.Add(v.Mar);
                    list.Add(v.Dec);
                    return list;
                })
                )
            {
                var x = p.ToArray();

                using (var w = new ChoFixedLengthWriter<AccountBalance>(sb)
                    .WithFirstLineHeader()
                    .WithField("ID", 1, 5)
                    .WithField("Name", 6, 5)
                    .WithField(f => f.lastTwelveMonths, 10, 5, fieldName: "Mon,Tue", valueSelector: v =>
                    {
                        return "1,2";
                    })
                    )
                {
                    w.Write(x);
                }

                //foreach (var rec in p)
                //    Console.WriteLine(rec.Dump());
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }
        public class AABillingDetailRecord : AABillingRecord
        {
            [ChoFixedLengthRecordField(0, 10)]
            public int ClientID { get; set; }
            [ChoFixedLengthRecordField(10, 15)]
            public string ClientName { get; set; }
        }

        public class Person
        {
            [ChoFixedLengthRecordField(0, 9)]
            public String Name { get; set; }

            [ChoFixedLengthRecordField(9, 13)]
            public String Surname { get; set; }

            [ChoFixedLengthRecordField(22, 6)]
            public String Gender { get; set; }

            [ChoFixedLengthRecordField(28, 2)]
            public Int32 OrderNum { get; set; }

            [ChoFixedLengthRecordField(30, 10, FormatText = "dd-MM-yyyy")]
            public DateTime BirthDate { get; set; }

            public override bool Equals(object obj)
            {
                var person = obj as Person;
                return person != null &&
                       Name == person.Name &&
                       Surname == person.Surname &&
                       Gender == person.Gender &&
                       OrderNum == person.OrderNum &&
                       BirthDate == person.BirthDate;
            }

            public override int GetHashCode()
            {
                var hashCode = -459681997;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Surname);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Gender);
                hashCode = hashCode * -1521134295 + OrderNum.GetHashCode();
                hashCode = hashCode * -1521134295 + BirthDate.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void Test1()
        {
            List<object> expected = new List<object> {
                new Person{ Name = "Filip", Surname = "Malýn", Gender = "Male", OrderNum = 12, BirthDate = new DateTime(1994,2,18) },
                new Person{ Name = "Božena", Surname = "Němcová", Gender = "Female", OrderNum = 18, BirthDate = new DateTime(1820,2,4)},
                new Person{ Name = "Jan", Surname = "Žižka", Gender = "Male", OrderNum = 7, BirthDate = new DateTime(1360,9,19)},
                new Person{ Name = "Che", Surname = "Guevara", Gender = "Male", OrderNum = 27, BirthDate = new DateTime(1928,6,14)},
                new Person{ Name = "Antoinede", Surname = "Saint-Exupéry", Gender = "Male", OrderNum = 15, BirthDate = new DateTime(1900,6,29)}
            };
            List<object> actual = new List<object>();

            string txt = @"Filip    Malýn        Male  1218-02-1994
Božena   Němcová      Female1804-02-1820
Jan      Žižka        Male  0719-09-1360
Che      Guevara      Male  2714-06-1928
AntoinedeSaint-ExupéryMale  1529-06-1900";

            foreach (var rec in ChoFixedLengthReader<Person>.LoadText(txt))
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }

        public class Emp1
        {
            [ChoFixedLengthRecordField(0, 5)]
            public String ID { get; set; }

            [ChoFixedLengthRecordField(5, 10)]
            public String Name1 { get; set; }

            public override bool Equals(object obj)
            {
                var emp = obj as Emp1;
                return emp != null &&
                       ID == emp.ID &&
                       Name1 == emp.Name1;
            }

            public override int GetHashCode()
            {
                var hashCode = 2108289525;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ID);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name1);
                return hashCode;
            }
        }

        [Test]
        public static void Test2()
        {
            List<object> expected = new List<object>
            {
                new Emp1{ ID = "1", Name1 = "Mark" },
                new Emp1{ ID = "2", Name1 = "Tom" }
            };
            List<object> actual = new List<object>();

            string txt = @"ID   Name      
1    Mark      
2    Tom       ";

            foreach (var rec in ChoFixedLengthReader<Emp1>.LoadText(txt)
                //.WithRecordLength(15)
                //.WithField("ID", startIndex: 0, size: 5)
                //.WithField("Name1", startIndex: 5, size: 10)
                .WithFirstLineHeader(true)
                //.WithHeaderLineAt(2, false)
                )
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void Sample1Test()
        {
            string fix = @"---------------------------A---------------------------

AARON THIAGO LOPES                       3099234 100-11

AARON PAPA DA SILVA                      8610822 160-26

ABNER MENEZEZ SOUZA                      1494778 500-35

EDSON EDUARD MOZART                      1286664 500-34";

            string expected = @"[
  {
    ""Name"": ""AARON THIAGO LOPES""
  },
  {
    ""Name"": ""AARON PAPA DA SILVA""
  },
  {
    ""Name"": ""ABNER MENEZEZ SOUZA""
  },
  {
    ""Name"": ""EDSON EDUARD MOZART""
  }
]";
            using (var r = ChoFixedLengthReader.LoadText(fix)
                .Configure(c => c.Comment = "--")
                .WithField("Name", 0, 41)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class EmpRec
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string Age { get; set; }
        }
        [Test]
        public static void QuoteValueTest()
        {
            List<EmpRec> employees = new List<EmpRec>()
                {
                    new EmpRec() { Id = 20, Name = " John Smith",  Address = "PO BOX 12165", Age = "25" },
                    new EmpRec() { Id = 21, Name = "Bob Kevin", Address = "123 NEW LIVERPOOL RD \"APT 12\"", Age = "30" },
                    new EmpRec() { Id = 22, Name = "Jack Robert", Address = "PO BOX 123", Age = "40" }
                };

            string expected = @"Id        Name                     Address                                           Age       
0000000020 John Smith              PO BOX 12165                                      25        
0000000021Bob Kevin                123 NEW LIVERPOOL RD ""APT 12""                     30        
0000000022Jack Robert              PO BOX 123                                        40        ";
            StringBuilder data = new StringBuilder();
            using (var w = new ChoFixedLengthWriter<EmpRec>(data)
                .WithFirstLineHeader()
                .Configure(c => c.EscapeQuoteAndDelimiter = false)
                //.Configure(c => c.QuoteChar = '`')
                .WithField(f => f.Id, 1, 10)
                .WithField(f => f.Name, 11, 25)
                .WithField(f => f.Address, 36, 50)
                .WithField(f => f.Age, 86, 10)
                )
            {
                w.Write(employees);
            }

            var actual = data.ToString();
            Assert.AreEqual(expected, actual);
        }

        internal class TestData
        {
            [ChoFixedLengthRecordField(0, 1)]
            public bool TestBool1 { get; set; }
            [ChoFixedLengthRecordField(1, 1)]
            public bool TestBool2 { get; set; }
            [ChoFixedLengthRecordField(2, 1)]
            public bool TestBool3 { get; set; }
            [ChoFixedLengthRecordField(3, 1)]
            public bool TestBool4 { get; set; }
            [ChoFixedLengthRecordField(4, 1)]
            public string TestString { get; set; }
        }
        [Test]
        public static void Issue219()
        {
            string csv = @"0101a
1111b
1011c
0000d
1000e";
            //ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.ZeroOrOne;

            string expected = @"[
  {
    ""TestBool1"": false,
    ""TestBool2"": true,
    ""TestBool3"": false,
    ""TestBool4"": true,
    ""TestString"": ""a""
  },
  {
    ""TestBool1"": true,
    ""TestBool2"": true,
    ""TestBool3"": true,
    ""TestBool4"": true,
    ""TestString"": ""b""
  },
  {
    ""TestBool1"": true,
    ""TestBool2"": false,
    ""TestBool3"": true,
    ""TestBool4"": true,
    ""TestString"": ""c""
  },
  {
    ""TestBool1"": false,
    ""TestBool2"": false,
    ""TestBool3"": false,
    ""TestBool4"": false,
    ""TestString"": ""d""
  },
  {
    ""TestBool1"": true,
    ""TestBool2"": false,
    ""TestBool3"": false,
    ""TestBool4"": false,
    ""TestString"": ""e""
  }
]";
            using (var r = ChoFixedLengthReader<TestData>.LoadText(csv)
                .TypeConverterFormatSpec(fs => fs.BooleanFormat = ChoBooleanFormatSpec.ZeroOrOne)
                )
            {
                //Console.WriteLine("TestBool1: " + e.TestBool1 + " TestBool2: " + e.TestBool2 + " TestBool3: " + e.TestBool3 + " TestBool4: " + e.TestBool4 + " TestString: " + e.TestString);

                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void Discussion275()
        {
            var csv = @"01010000001002699000PRESUNTO FATIADO KG         
01010000002004199000BACON KG                                              ";

            string expected = @"[
  {
    ""Code"": ""01010000001002699000"",
    ""Name"": ""RESUNTO FATIADO KG"",
    ""Price"": null
  },
  {
    ""Code"": ""01010000002004199000"",
    ""Name"": ""ACON KG"",
    ""Price"": null
  }
]";
            using (var r = ChoFixedLengthReader<Product>.LoadText(csv)
                .WithField("Code", 0, 20)
                .WithField("Name", 21, 25)
                .WithField("Price", 46, 3)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Setup(s => s.BeforeRecordLoad += (o, e) =>
                {
                    var line = e.Source as string;
                    e.Source = line.Substring(0, 48);
                })
                //.Configure(c => c.AllowVariableRecordLength = true)
                )
            {
                var recs = r.ToArray();
                recs.Print();
                Assert.AreEqual(recs.Length, 2);

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        public class Product
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string Price { get; set; }
        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            //QuickLoad();
            //return;
            //foreach (dynamic rec in new ChoFixedLengthReader("emp.txt").WithFirstLineHeader()
            //    .Configure(c => c.FileHeaderConfiguration.IgnoreCase = false)
            //    .Configure(c => c.FileHeaderConfiguration.TrimOption = ChoFieldValueTrimOption.None)
            //    .Configure(c => c.ThrowAndStopOnMissingField = true)
            //    //.Configure(c => c.ColumnOrderStrict = false)
            //    )
            //{
            //    Console.WriteLine(rec.id);
            //    //Console.WriteLine(rec[" id "]);
            //}
            //return;
            //foreach (var rec in new ChoFixedLengthReader<EmployeeRec>("emp.txt")
            //    .WithFirstLineHeader()
            //    //.Configure(c => c.FileHeaderConfiguration.IgnoreCase = false)
            //    //.Configure(c => c.ThrowAndStopOnMissingField = true)
            //    //.Setup(r => r.BeforeRecordLoad += (o, e) =>
            //    //{
            //    //    if (e.Source != null)
            //    //    {
            //    //        e.Skip = ((string)e.Source).StartsWith("#");
            //    //    }
            //    //})
            //    )
            //{
            //    Console.WriteLine(rec.Id);
            //}

            //return;
            foreach (dynamic rec in new ChoFixedLengthReader("emp.txt").WithFirstLineHeader()
                .Configure(c => c.MayContainEOLInData = true)
                .Configure(c => c.FileHeaderConfiguration.IgnoreCase = true)
                .Configure(c => c.FileHeaderConfiguration.TrimOption = ChoFieldValueTrimOption.None))
            {
                Console.WriteLine(rec.id);
                //Console.WriteLine("{0}", rec[" id     "]);
                //Console.WriteLine(rec[0]);
            }
            return;

            //Override the width of necessary simple types
            //ChoFixedLengthFieldDefaultSizeConfiguation.Instance.SetSize(typeof(int), 3);
            //ChoFixedLengthFieldDefaultSizeConfiguation.Instance.SetSize(typeof(string), 5);

            QuickLoad();
        }
        [Test]
        public static void QuickLoad1()
        {
            string expected = @"[
  {
    ""id"": ""1"",
    ""Name"": ""\""Carl's\""""
  },
  {
    ""id"": ""2"",
    ""Name"": ""Mark""
  }
]";
            using (var r = new ChoFixedLengthReader("emp.txt").WithFirstLineHeader()
                .Configure(c => c.MayContainEOLInData = true)
                .Configure(c => c.FileHeaderConfiguration.IgnoreCase = true)
                .Configure(c => c.FileHeaderConfiguration.TrimOption = ChoFieldValueTrimOption.None))
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void QuickLoad()
        {
            string expected = @"[
  {
    ""Account"": 101,
    ""LastName"": ""Reeves"",
    ""FirstName"": ""Keanu"",
    ""Balance"": 9315.45,
    ""CreditLimit"": 10000.0,
    ""AccountCreated"": ""1998-01-17T00:00:00"",
    ""Rating"": ""A""
  },
  {
    ""Account"": 101,
    ""LastName"": ""Reeves"",
    ""FirstName"": ""Keanu"",
    ""Balance"": 9315.45,
    ""CreditLimit"": 10000.0,
    ""AccountCreated"": ""1998-01-17T00:00:00"",
    ""Rating"": ""A""
  },
  {
    ""Account"": 101,
    ""LastName"": ""Reeves"",
    ""FirstName"": ""Keanu"",
    ""Balance"": 9315.45,
    ""CreditLimit"": 10000.0,
    ""AccountCreated"": ""1998-01-17T00:00:00"",
    ""Rating"": ""A""
  }
]";
            using (var r = new ChoFixedLengthReader(FileNameAccountsTXT)
                .WithFirstLineHeader()
                .Configure(c => c.MaxScanRows = 2)
                )
            {
                //r.RecordLoadError += (o, e) =>
                //{
                //    Console.WriteLine(e.Exception.Message);
                //    e.Handled = true;
                //};

                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void QuickDataTableTest()
        {
            string expected = @"[
  {
    ""Account"": ""101"",
    ""LastName"": ""Reeves"",
    ""FirstName"": ""Keanu"",
    ""Balance"": ""9315.45"",
    ""CreditLimit"": ""10000.00"",
    ""AccountCreated"": ""1/17/1998"",
    ""Rating"": ""A""
  },
  {
    ""Account"": ""101"",
    ""LastName"": ""Reeves"",
    ""FirstName"": ""Keanu"",
    ""Balance"": ""9315.45"",
    ""CreditLimit"": ""10000.00"",
    ""AccountCreated"": ""1/17/1998"",
    ""Rating"": ""A""
  },
  {
    ""Account"": ""101"",
    ""LastName"": ""Reeves"",
    ""FirstName"": ""Keanu"",
    ""Balance"": ""9315.45"",
    ""CreditLimit"": ""10000.00"",
    ""AccountCreated"": ""1/17/1998"",
    ""Rating"": ""A""
  }
]";
            var dt = new ChoFixedLengthReader(FileNameAccountsTXT, new ChoFixedLengthRecordConfiguration()
            { 
                
            }.Configure(c => c.WithFirstLineHeader())).AsDataTable();
            var actual = JsonConvert.SerializeObject(dt, Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void POCODataTableTest()
        {
            string expected = @"[
  {
    ""Account"": 101,
    ""LastName"": ""Reeves"",
    ""FirstName"": ""Keanu"",
    ""Balance"": 9315.45,
    ""CreditLimit"": 10000.0,
    ""AccountCreated"": ""1998-01-17T00:00:00"",
    ""Rating"": ""A""
  }
]";
            DataTable dt = null;

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<CreditBalanceRecord>(reader))
            {
                writer.WriteLine("Account LastName        FirstName       Balance     CreditLimit   AccountCreated  Rating ");
                writer.WriteLine("101     Reeves          Keanu           9315.45     10000.00      1/17/1998       A      ");
                writer.Flush();
                stream.Position = 0;

                dt = parser.AsDataTable();
            }
            var actual = JsonConvert.SerializeObject(dt, Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DynamicApproach()
        {
            List<object> expected = new List<object> {
                new ChoDynamicObject{{"Account",(int)101},{"LastName","Reeves"},{"FirstName","Keanu"},{"Balance",(double)9315.45},{"CreditLimit",(double)10000},{"AccountCreated",new DateTime(1998, 1, 17) }, { "Rating","A"} },
                new ChoDynamicObject{{"Account",(int)102},{"LastName","Tom"},{"FirstName","Mark"},{"Balance",(double)9315.45},{"CreditLimit",(double)15000},{"AccountCreated",new DateTime(2000, 12, 17) }, { "Rating","A"} }
            };
            List<object> actual = new List<object>();

            object row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader(reader)
                .WithFirstLineHeader()
                .WithField("Account", 0, 8, fieldType: typeof(int))
                .WithField("LastName", 8, 16)
                .WithField("FirstName", 24, 16)
                .WithField("Balance", 40, 12, fieldType: typeof(double))
                .WithField("CreditLimit", 52, 14, fieldType: typeof(double))
                .WithField("AccountCreated", 66, 16, fieldType: typeof(DateTime))
                .WithField("Rating", 82, 7))
            {
                writer.WriteLine("Account LastName        FirstName       Balance     CreditLimit   AccountCreated  Rating ");
                writer.WriteLine("101     Reeves          Keanu           9315.45     10000.00      1/17/1998       A      ");
                writer.WriteLine("102     Tom             Mark            9315.45     15000.00      12/17/2000      A      ");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    actual.Add(row);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void LoadTextTest()
        {
            List<object> expected = new List<object> {
                new ChoDynamicObject  {{"Id","1"},{"Name","Carl"}},
                new ChoDynamicObject  {{"Id","2"},{"Name","Mark"}}
            };
            List<object> actual = new List<object>();

            string txt = "Id      Name      \r\n1       Carl      \r\n2       Mark      ";

            foreach (var e in ChoFixedLengthReader.LoadText(txt).WithFirstLineHeader())
                actual.Add(e);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void CodeFirstApproach()
        {
            List<EmployeeRecSimple> expected = new List<EmployeeRecSimple> {
               new EmployeeRecSimple{ Id = 1, Name = "Carl"},
               new EmployeeRecSimple{ Id = 2, Name = "Mark"}
            };
            List<object> actual = new List<object>();

            EmployeeRecSimple row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecSimple>(reader).WithFirstLineHeader())
            {
                writer.WriteLine("Id        Name                     ");
                writer.WriteLine("1         Carl                     ");
                writer.WriteLine("2         Mark                     ");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    actual.Add(row);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void QuickDynamicLoadTest()
        {
            List<object> expected = new List<object> {
                new ChoDynamicObject{{"Id","1"},{"Name","Carl"} },
                new ChoDynamicObject{{"Id","2"},{"Name","Mark"} }
            };
            List<object> actual = new List<object>();

            object row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader(reader).WithFirstLineHeader())
            {
                writer.WriteLine("Id      Name      ");
                writer.WriteLine("1       Carl      ");
                writer.WriteLine("2       Mark      ");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    actual.Add(row);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void QuickDynamicLoadTestUsingIterator()
        {
            List<object> expected = new List<object> {
                new ChoDynamicObject{{"Id","1"},{"Name","Carl"} },
                new ChoDynamicObject{{"Id","2"},{"Name","Mark"} }
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("Id        Name                     ");
                writer.WriteLine("1         Carl                     ");
                writer.WriteLine("2         Mark                     ");
                writer.Flush();
                stream.Position = 0;

                foreach (var e in new ChoFixedLengthReader(reader).WithFirstLineHeader())
                {
                    actual.Add(e);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }
        public static string FileNameEmpTXT => "Emp.txt";
        public static string FileNameAccountsTXT => "Accounts.txt";

        [Test]
        public static void MultiLineTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{ { "Id", "1" },{ "Name", @"""Carl's""" } },
                new ChoDynamicObject{ { "Id", "2" },{ "Name", "Mark" } }
            };
            List<object> actual = new List<object>();
            object row = null;
            using (var parser = new ChoFixedLengthReader(FileNameEmpTXT).WithFirstLineHeader().WithField("Id", 0, 8).WithField("Name", 8, 10))
            {
                parser.Configuration.MayContainEOLInData = true;
                while ((row = parser.Read()) != null)
                {
                    actual.Add(row);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void ConfigFirstApproachReadAsDynamicRecords()
        {
            List<object> expected = new List<object>{
                new ChoDynamicObject { { "Id", (int)1 }, { "Name", "Carl" } },
                new ChoDynamicObject {{ "Id", (int)2 }, {"Name","Mark" } }
            };
            List<object> actual = new List<object>();

            ChoFixedLengthRecordConfiguration config = new ChoFixedLengthRecordConfiguration();
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Id", 0, 8) { FieldType = typeof(int) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Name", 8, 10) { FieldType = typeof(string) });

            dynamic row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader(reader, config).WithFirstLineHeader())
            {
                writer.WriteLine("Id        Name                     ");
                writer.WriteLine("1         Carl                     ");
                writer.WriteLine("2         Mark                     ");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    actual.Add(row);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void ConfigFirstApproachReadAsTypedRecords()
        {
            List<object> expected = new List<object>{
                new EmployeeRecSimple { Id=1 , Name="Carl"  },
                new EmployeeRecSimple { Id=2 , Name="Mark"  }
            };
            List<object> actual = new List<object>();

            ChoFixedLengthRecordConfiguration config = new ChoFixedLengthRecordConfiguration();
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Id", 0, 8) { FieldType = typeof(int) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Name", 8, 10) { FieldType = typeof(string) });

            EmployeeRecSimple row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecSimple>(reader, config).WithFirstLineHeader())
            {
                writer.WriteLine("Id        Name                     ");
                writer.WriteLine("1         Carl                     ");
                writer.WriteLine("2         Mark                     ");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    actual.Add(row);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void CodeFirstWithDeclarativeApproachRead()
        {
            List<EmployeeRec> expected = new List<EmployeeRec> {
                new EmployeeRec{Id = 1, Name = "Carl" },
                new EmployeeRec { Id = 2, Name = "Mark"}
            };
            List<object> actual = new List<object>();

            EmployeeRec row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRec>(reader).WithFirstLineHeader())
            {
                writer.WriteLine("Id        Name                     ");
                writer.WriteLine("1         Carl                     ");
                writer.WriteLine("2         Mark                     ");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    actual.Add(row);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void QuickTest()
        {
            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Carl""
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark""
  }
]";

            object row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecSimple>(reader)
                .WithFirstLineHeader()
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                )
            {
                writer.WriteLine("Id        Name                     ");
                writer.WriteLine("1         Carl                     ");
                writer.WriteLine("2         Mark                     ");
                writer.Flush();
                stream.Position = 0;

                //Assert.Throws<ChoRecordConfigurationException>(() => { row = parser.Read(); });
                var actual = JsonConvert.SerializeObject(parser, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void IgnoreMemberTest()
        {
            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Carl"",
    ""Salary"": 0.0
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark"",
    ""Salary"": 0.0
  }
]";

            object row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecWithCurrency>(reader)
                .WithFirstLineHeader()
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                )
            {
                writer.WriteLine("Id        Name                     ");
                writer.WriteLine("1         Carl                     ");
                writer.WriteLine("2         Mark                     ");
                writer.Flush();
                stream.Position = 0;

                //Assert.Throws<ChoRecordConfigurationException>(() => { row = parser.Read(); });
                var actual = JsonConvert.SerializeObject(parser, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class EmployeeRecWithCurrency1
        {
            [ChoFixedLengthRecordField(0, 8)]
            public int? Id { get; set; }
            [ChoFixedLengthRecordField(8, 10)]
            public string Name { get; set; }
            [ChoFixedLengthRecordField(18, 28)]
            public ChoCurrency Salary { get; set; }

            public override bool Equals(object obj)
            {
                var currency = obj as EmployeeRecWithCurrency;
                return currency != null &&
                       EqualityComparer<int?>.Default.Equals(Id, currency.Id) &&
                       Name == currency.Name &&
                       Salary.Equals(currency.Salary);
            }

            public override int GetHashCode()
            {
                var hashCode = -1858601383;
                hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Id);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<ChoCurrency>.Default.GetHashCode(Salary);
                return hashCode;
            }
        }
        [Test]
        public static void MissingHeaderTest()
        {
            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Carl"",
    ""Salary"": 0.0
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark"",
    ""Salary"": 0.0
  }
]";

            object row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecWithCurrency1>(reader)
                .WithFirstLineHeader()
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                )
            {
                writer.WriteLine("Id        Name                     ");
                writer.WriteLine("1         Carl                     ");
                writer.WriteLine("2         Mark                     ");
                writer.Flush();
                stream.Position = 0;

                //Assert.Throws<ChoRecordConfigurationException>(() => { row = parser.Read(); });
                Assert.Catch<ChoParserException>(() =>
                {
                    var actual = JsonConvert.SerializeObject(parser, Formatting.Indented);
                    Assert.AreEqual(expected, actual);
                });
            }
        }
        [Test]
        public static void CodeFirstWithDeclarativeApproach()
        {
            List<CreditBalanceRecord> expected = new List<CreditBalanceRecord> {
                new CreditBalanceRecord {  Account = 101, LastName = "Reeves", FirstName = "Keanu", Balance = 9315.45, CreditLimit = 10000, AccountCreated = new DateTime(1998,1,17), Rating = "A"}
            };
            List<object> actual = new List<object>();

            object row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<CreditBalanceRecord>(reader))
            {
                writer.WriteLine("Account LastName        FirstName       Balance     CreditLimit   AccountCreated  Rating ");
                writer.WriteLine("101     Reeves          Keanu           9315.45     10000.00      1/17/1998       A      ");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    actual.Add(row);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void CodeFirstWithDeclarativeApproach2()
        {
            List<CreditBalanceRecord> expected = new List<CreditBalanceRecord> {
                new CreditBalanceRecord {  Account = 101, LastName = "Reeves", FirstName = "Keanu", Balance = 9315.45, CreditLimit = 10000, AccountCreated = new DateTime(1998,1,17), Rating = "A"}
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("Account LastName        FirstName       Balance     CreditLimit   AccountCreated  Rating ");
                writer.WriteLine("101     Reeves          Keanu           9315.45     10000.00      1/17/1998       A      ");
                writer.Flush();
                stream.Position = 0;

                foreach (var item in new ChoFixedLengthReader<CreditBalanceRecord>(reader))
                {
                    actual.Add(item);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void FallbackValueUsedViaCodeFirstApproach()
        {
            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Carl"",
    ""JoinedDate"": ""2016-12-08T00:00:00"",
    ""Salary"": 100000.0,
    ""IsActive"": false,
    ""Status"": ""F""
  },
  {
    ""Id"": 2,
    ""Name"": ""MarkS"",
    ""JoinedDate"": ""2011-01-13T00:00:00"",
    ""Salary"": 500000.0,
    ""IsActive"": true,
    ""Status"": ""C""
  }
]";

            EmployeeRecSimpleFallback row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecSimpleFallback>(reader)
                .TypeConverterFormatSpec(fs => fs.DateTimeFormat = "dd/MM/yyyy")
                .TypeConverterFormatSpec(fs => fs.BooleanFormat = ChoBooleanFormatSpec.ZeroOrOne)
                )
            {
                writer.WriteLine("001Carl 08/12/2016$100,000                      0F");
                writer.WriteLine("002MarkS13/13/2010$500,000                      1C");
                writer.Flush();
                stream.Position = 0;

                //while ((row = parser.Read()) != null)
                //{
                //    Console.WriteLine(row.ToStringEx());
                //}

                var actual = JsonConvert.SerializeObject(parser, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void FallbackValueUsedViaConfigFirstApproach()
        {
            List<object> expected = new List<object> {
                new ChoDynamicObject { { "Id", (int)1},{"Name","Carl" },{ "JoinedDate", new DateTime(2016, 12, 8) },{ "Salary", new ChoCurrency(100000) },{ "IsActive", false },{ "Status", 'F' }  },
                new ChoDynamicObject { { "Id", (int)2},{"Name","MarkS" },{ "JoinedDate", new DateTime(2010, 1, 13) },{ "Salary", new ChoCurrency(500000) },{ "IsActive", true },{ "Status", 'C' }  }
            };
            List<object> actual = new List<object>();

            ChoFixedLengthRecordConfiguration config = new ChoFixedLengthRecordConfiguration();
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Id", 0, 3) { FieldType = typeof(int) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Name", 3, 5) { FieldType = typeof(string) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("JoinedDate", 8, 10) { FieldType = typeof(DateTime), FallbackValue = "1/1/2010" });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Salary", 18, 10) { FieldType = typeof(ChoCurrency) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("IsActive", 28, 1) { FieldType = typeof(bool) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Status", 29, 1) { FieldType = typeof(char) });
            config.ErrorMode = ChoErrorMode.ReportAndContinue;

            dynamic row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader(reader, config)
                .TypeConverterFormatSpec(fs => fs.DateTimeFormat = "dd/MM/yyyy")
                .TypeConverterFormatSpec(fs => fs.BooleanFormat = ChoBooleanFormatSpec.ZeroOrOne)
                )
            {
                writer.WriteLine("001Carl 08/12/2016100,000   0F");
                writer.WriteLine("002MarkS13/01/2010500,000   1C");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    actual.Add(row);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void DefaultValueUsedViaCodeFirstApproach()
        {
            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Carl""
  },
  {
    ""Id"": 2,
    ""Name"": ""XX""
  }
]";

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecSimple>(reader)
                .WithFirstLineHeader()
                )
            {
                writer.WriteLine("Id        Name                     ");
                writer.WriteLine("1         Carl                     ");
                writer.WriteLine("2                                  ");
                writer.Flush();
                stream.Position = 0;

                var recs = parser.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);

            }
        }

        [Test]
        public static void DefaultValueUsedViaConfigFirstApproach()
        {
            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Carl"",
    ""JoinedDate"": ""2016-12-08T00:00:00"",
    ""Salary"": 100000.0,
    ""IsActive"": false,
    ""Status"": ""F""
  },
  {
    ""Id"": 2,
    ""Name"": ""XX"",
    ""JoinedDate"": ""2010-01-13T00:00:00"",
    ""Salary"": 500000.0,
    ""IsActive"": true,
    ""Status"": ""C""
  }
]";

            ChoFixedLengthRecordConfiguration config = new ChoFixedLengthRecordConfiguration();
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Id", 0, 3) { FieldType = typeof(int) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Name", 3, 5) { FieldType = typeof(string), DefaultValue = "XX" });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("JoinedDate", 8, 10) { FieldType = typeof(DateTime), DefaultValue = "10/10/2010" });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Salary", 18, 10) { FieldType = typeof(ChoCurrency) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("IsActive", 28, 1) { FieldType = typeof(bool) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Status", 29, 1) { FieldType = typeof(char) });
            config.ErrorMode = ChoErrorMode.ReportAndContinue;

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader(reader, config)
                .TypeConverterFormatSpec(fs => fs.DateTimeFormat = "dd/MM/yyyy")
                .TypeConverterFormatSpec(fs => fs.BooleanFormat = ChoBooleanFormatSpec.ZeroOrOne)
                )
            {
                writer.WriteLine("001Carl 08/12/2016100,000   0F");
                writer.WriteLine("002     13/01/2010500,000   1C");
                writer.Flush();
                stream.Position = 0;

                var recs = parser.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }
    }

    public partial class EmployeeRecSimple
    {
        public int Id { get; set; }
        [DefaultValue("XX")]
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            var simple = obj as EmployeeRecSimple;
            return simple != null &&
                   Id == simple.Id &&
                   Name == simple.Name;
        }

        public override int GetHashCode()
        {
            var hashCode = -1919740922;
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
    }

    public partial class EmployeeRec
    {
        [ChoFixedLengthRecordField(0, 8)]
        public int Id { get; set; }
        [ChoFixedLengthRecordField(8, 10)]
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            var rec = obj as EmployeeRec;
            return rec != null &&
                   Id == rec.Id &&
                   Name == rec.Name;
        }

        public override int GetHashCode()
        {
            var hashCode = -1919740922;
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }
    }

    //public partial class EmployeeRecSimple
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //    [DefaultValue("1/1/2001")]
    //    public DateTime JoinedDate { get; set; }
    //    [DefaultValue("50000")]
    //    public ChoCurrency Salary { get; set; }
    //    public bool IsActive { get; set; }
    //    public char Status { get; set; }
    //}

    [ChoFixedLengthRecordObject(ErrorMode = ChoErrorMode.ReportAndContinue)]
    public partial class EmployeeRecSimpleFallback
    {
        [ChoFixedLengthRecordField(0, 3)]
        public int Id { get; set; }
        [ChoFixedLengthRecordField(3, 5)]
        public string Name { get; set; }
        [DefaultValue("1/1/2001")]
        [ChoFallbackValue("01/13/2011")]
        [ChoFixedLengthRecordField(8, 10)]
        public DateTime JoinedDate { get; set; }
        [DefaultValue("50000")]
        [ChoFixedLengthRecordField(18, 30)]
        public ChoCurrency Salary { get; set; }
        [ChoFixedLengthRecordField(48, 1)]
        public bool IsActive { get; set; }
        [ChoFixedLengthRecordField(49, 1)]
        public char Status { get; set; }
    }

    //[ChoFixedLengthRecordObject()]
    //public partial class EmployeeRec
    //{
    //    [ChoFixedLengthRecordField(0, 3)]
    //    public int Id { get; set; }
    //    [ChoFixedLengthRecordField(3, 5)]
    //    public string Name { get; set; }
    //    [ChoFixedLengthRecordField(8, 10)]
    //    public DateTime JoinedDate { get; set; }
    //    [ChoFixedLengthRecordField(18, 10)]
    //    public ChoCurrency Salary { get; set; }
    //    [ChoFixedLengthRecordField(28, 1)]
    //    public bool IsActive { get; set; }
    //    [ChoFixedLengthRecordField(29, 1)]
    //    public char Status { get; set; }
    //}
}
