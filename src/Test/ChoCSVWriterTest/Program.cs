using ChoETL;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;
using System.Xml;
using System.Xml.Schema;
using Newtonsoft.Json;
using System.Data.Common;

namespace ChoCSVWriterTest
{
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

        public class CustomType
        {
            [ChoCSVRecordField(1, FieldName = "date_start")]
            public DateTime DateStart { get; set; }
            [ChoCSVRecordField(2, FieldName = "date_end")]
            public DateTime DateEnd { get; set; }
            [ChoCSVRecordField(3, FieldName = "current_year")]
            public int CurrentYear { get; set; }
        }
        [Test]
        public static void CSVWithQuotes()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            string expected = @"""date_start"",""date_end"",""current_year""
""" + new DateTime(2023, 01, 01).ToString("d", CultureInfo.GetCultureInfo("en-CA")) + @""",""" + new DateTime(2023, 01, 01).AddDays(2).ToString("d", CultureInfo.GetCultureInfo("en-CA")) + @""",""" + new DateTime(2023, 01, 01).Year.ToString("d", CultureInfo.GetCultureInfo("en-CA")) + @"""";

            StringBuilder msg = new StringBuilder();
            using (var writer = new ChoCSVWriter<CustomType>(msg).WithFirstLineHeader()
                .Configure(c => c.FileHeaderConfiguration.QuoteAllHeaders = true)
                .Configure(c => c.QuoteAllFields = true)
                .Configure(c => c.Culture = new CultureInfo("en-CA"))
                )
            {
                var x1 = new CustomType { DateStart = new DateTime(2023, 01, 01), DateEnd = new DateTime(2023, 01, 01).AddDays(2), CurrentYear = new DateTime(2023, 01, 01).Year };
                writer.Write(x1);
            }

            Assert.AreEqual(expected, msg.ToString());

        }

        [Test]
        public static void IntArrayTest()
        {
            List<string> expectedList = new List<string>();
            List<string> actualList = new List<string>();

            dynamic address = new ChoDynamicObject();
            address.Street = "10 River Rd";
            address.City = "Princeton";

            dynamic state = new ChoDynamicObject();
            state.State = "NJ";
            state.Zip = "09930";

            address.State = state;

            using (var w = new ChoCSVWriter(FileNameIntArrayTestTestCSV)
                .Setup(s => s.RecordFieldWriteError += (object o, ChoRecordFieldWriteErrorEventArgs e) => actualList.Add(e.Exception.ToString()))
                .Configure((Action<ChoCSVRecordConfiguration>)(c => { c.NestedKeySeparator = '/'; c.WithFirstLineHeader(); }))
                )
            {
                //w.Write(new KeyValuePair<int, string>(1, "MM"));
                //w.Write(new KeyValuePair<int, string>(1, "MM"));
                w.Write(new { id = "1s->", address = address });
            }

            Assert.Multiple(() => { Assert.AreEqual(expectedList, actualList); FileAssert.AreEqual(FileNameIntArrayTestExpectedCSV, FileNameIntArrayTestTestCSV); });
        }

        public class TRoot
        {
            public string Client { get; set; }
            public List<TDeal> Deals { get; set; }
        }

        public class TDeal
        {
            public string DealName { get; set; }
            public List<TInterval> TShape { get; set; }
        }

        public class TInterval
        {
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public string Volume { get; set; }
        }

        [Test]
        public static void NestedObjects()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat

            string expected = @"ClientName,DealNo,StartDate,EndDate,Volume
ABC,59045599," + new DateTime(2023, 01, 01).ToString("d") + "," + new DateTime(2023, 01, 01).AddDays(2).ToString("d") + @",100
ABC,59045599," + new DateTime(2023, 01, 01).ToString("d") + "," + new DateTime(2023, 01, 01).AddDays(2).ToString("d") + ",200";

            string actual = null;
            ChoETLFrxBootstrap.IsSandboxEnvironment = true;
            StringBuilder sb = new StringBuilder();
            TRoot root = new TRoot() { Client = "ABC", Deals = new List<TDeal>() };
            root.Deals.Add(new TDeal
            {
                DealName = "59045599",
                TShape = new List<TInterval>()
            {
                new TInterval { StartDate = new DateTime(2023, 01, 01).ToString("d"), EndDate = new DateTime(2023, 01, 01).AddDays(2).ToString("d"), Volume = "100" },
                new TInterval { StartDate = new DateTime(2023, 01, 01).ToString("d"), EndDate = new DateTime(2023, 01, 01).AddDays(2).ToString("d"), Volume = "200" }
            }
            });

            using (var w = new ChoCSVWriter(sb).WithFirstLineHeader())
            {
                w.Write(root.Deals.SelectMany(d => d.TShape.Select(s => new { ClientName = root.Client, DealNo = d.DealName, StartDate = s.StartDate, EndDate = s.EndDate, Volume = s.Volume })));
            }
            actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class Employee
        {
            public string Name { get; set; }
        }

        public class Manager : Employee
        {
            public double Salary { get; set; }
            public string Department { get; set; }
        }

        public class ManagerMetaData
        {
            public string Name { get; set; }
            public double Salary { get; set; }
            [ChoIgnoreMember]
            public string Department { get; set; }
        }

        [Test]
        public static void InheritanceTest()
        {
            string expected = @"Name,Salary
E1,100000
E2,110000";

            StringBuilder csv = new StringBuilder();
            //Assert.Fail("Test does not return. A worker-thread dies and main-thread stays in Wait at line w.Write(o1).");
            using (var w = new ChoCSVWriter<Employee>(csv).WithFirstLineHeader()
                .MapRecordFields<ManagerMetaData>()
                )
            {
                var o1 = new Manager { Name = "E1", Department = "History", Salary = 100000 };
                var o2 = new Manager { Name = "E2", Department = "Math", Salary = 110000 };
                w.Write(o1);
                w.Write(o2);
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        public class SitePostal
        {
            [Required(ErrorMessage = "State is required")]
            [RegularExpression("^[A-Z][A-Z]$", ErrorMessage = "Incorrect zip code.")]
            [DisplayName("STATE")]
            public string State { get; set; }
            [Required]
            [RegularExpression("^[0-9][0-9]*$")]
            //[ChoIgnoreMember]
            [DisplayName("ZIP")]
            public string Zip { get; set; }
        }
        public class SiteAddress
        {
            [Required]
            [StringLength(10)]
            //[ChoCSVRecordField(3)]
            [DisplayName("STREET")]
            public string Street { get; set; }
            [Required]
            [RegularExpression("^[a-zA-Z][a-zA-Z ]*$")]
            [DisplayName("CITY")]
            public string City { get; set; }
            [ChoValidateObject]
            public SitePostal SitePostal { get; set; }
        }
        public class Site
        {
            [Required(ErrorMessage = "SiteID can't be null")]
            [DisplayName("SITE_ID")]
            //[ChoCSVRecordField(1, FormatText = "000")]
            public int SiteID { get; set; }
            [Required]
            [DisplayName("HOUSE")]
            public int House { get; set; }
            //[ChoValidateObject]
            public SiteAddress SiteAddress { get; set; }
            //[ChoCSVRecordField(2)]
            [DisplayName("APARTMENT")]
            public int? Apartment { get; set; }
        }

        [Test]
        public static void Sample3()
        {
            string expected = @"SITE_ID,HOUSE,STREET,CITY,STATE,ZIP,APARTMENT
44,545395,PORT ROYAL,CORPUS CHRISTI,TX,,2
44,608646,TEXAS AVE,ODESSA,TX,79762,
44,487460,EVERHART R,CORPUS CHRISTI,TX,78413,
44,275543,EDWARD GAR,SAN MARCOS,TX,78666,4
44,136811,MAGNOLIA A,SAN ANTONIO,TX1,,1";
            string actual = null;
            using (var p = new ChoCSVReader<Site>(FileNameSample3CSV)
                //.ClearFields()
                //            .WithField(m => m.SiteID)
                //            .WithField(m => m.SiteAddress.City)
                .WithFirstLineHeader(true)
                .Configure(c => c.ObjectValidationMode = ChoObjectValidationMode.ObjectLevel)
                )
            {
                var recs = p.ToArray();
                StringBuilder msg = new StringBuilder();
                using (var w = new ChoCSVWriter<Site>(new StringWriter(msg))
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(recs);
                }
                actual = msg.ToString();
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ListIntTest()
        {
            string expected = @"Value
2
3";
            string actual = null;
            StringBuilder sb = new StringBuilder();

            ChoActivator.Factory = (t, args) =>
            {
                if (t == typeof(ChoScalarObject<ChoCurrency>))
                    return new ChoScalarObject<ChoCurrency>((ChoCurrency)args[0]);

                return null;
            };

            using (var w = new ChoCSVWriter(new StringWriter(sb))
                .WithFirstLineHeader()
                )
            {
                w.Write(2);
                w.Write(3);

                //List<string> l = new List<string>();
                //l.Add("2");
                //l.Add("Tom1");
                //l.Add("Mark1");

                //w.Write(l);
            }

            actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ListStringTest()
        {
            string expected = @"Value
2
Tom
Mark";
            string actual = null;
            StringBuilder sb = new StringBuilder();

            ChoActivator.Factory = (t, args) =>
            {
                if (t == typeof(ChoScalarObject<ChoCurrency>))
                    return new ChoScalarObject<ChoCurrency>((ChoCurrency)args[0]);

                return null;
            };

            using (var w = new ChoCSVWriter(new StringWriter(sb))
                .WithFirstLineHeader()
                )
            {
                w.Write("2");
                w.Write("Tom");
                w.Write("Mark");
            }

            actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ListCurrencyTest_Struct()
        {
            string expected = @"Value
$1.00
$2.00";
            string actual = null;
            StringBuilder sb = new StringBuilder();

            ChoActivator.Factory = (t, args) =>
            {
                if (t == typeof(ChoScalarObject<ChoCurrency>))
                    return new ChoScalarObject<ChoCurrency>((ChoCurrency)args[0]);

                return null;
            };

            using (var w = new ChoCSVWriter(new StringWriter(sb))
                .WithFirstLineHeader()
                )
            {
                w.Write(new ChoCurrency(1));
                w.Write(new ChoCurrency(2));
            }

            actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DictionaryTest()
        {
            string expected = @"Key,Value
1,Tom
2,Mark";
            string actual = null;
            StringBuilder sb = new StringBuilder();

            using (var w = new ChoCSVWriter(new StringWriter(sb))
                .WithFirstLineHeader()
                )
            {
                Dictionary<int, string> l = new Dictionary<int, string>();
                l.Add(1, "Tom");
                l.Add(2, "Mark");

                w.Write(l);
            }

            actual = sb.ToString();
            Console.WriteLine(actual);

            //Assert.AreEqual(expected, actual);
        }

        public class Test
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public DateTime CreatedDate { get; set; }
            public string DueDate { get; set; }
            public string ReferenceNo { get; set; }
            public string Parent { get; set; }
        }
        [Test]
        public static void ListPOCOTest()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            List<Test> list = new List<Test>();

            list.Add(new Test { Id = 1, Name = "Tom", CreatedDate = new DateTime(1234, 5, 6, 7, 8, 9) });
            list.Add(new Test { Id = 2, Name = "Mark" });

            using (var sw = new StreamWriter(FileNameListPOCOTestTestCSV))
            using (var w = new ChoCSVWriter<Test>(sw)
                .WithFirstLineHeader()
                )
            {
                w.Write(list);
            }
            FileAssert.AreEqual(FileNameListPOCOTestExpectedCSV, FileNameListPOCOTestTestCSV);
        }

        [Test]
        public static void WriteSpecificColumns()
        {
            string expected = @"ID, House
1,New York";
            string actual = null;

            StringBuilder csv = new StringBuilder();

            Site site = new Site { SiteID = 1, House = 12, Apartment = 100, SiteAddress = new SiteAddress { City = "New York", Street = "101 Main St." } };

            using (var w = new ChoCSVWriter<Site>(new StringWriter(csv))
                .WithFirstLineHeader()
                .ClearFields()
                .WithField(r => r.SiteID)
                .WithField(r => r.SiteAddress.City)
                .Setup(s => s.FileHeaderWrite += (o, e) =>
                {
                    e.HeaderText = "ID, House";
                })
                )
            {
                w.Write(site);
            }

            actual = csv.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void TestListOfInt()
        {
            string expected = @"Id
1
2";
            string actual = null;

            StringBuilder sb = new StringBuilder();
            using (var w = new ChoCSVWriter(sb)
                .WithFirstLineHeader()
                .WithField("Value", fieldName: "Id")
                )
            {
                w.Write((int?)null);
                w.Write(1);
                w.Write(2);
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void TestListOfInt1()
        {
            string expected = @"Id
1
2
2";
            string actual = null;

            StringBuilder sb = new StringBuilder();
            using (var w = new ChoCSVWriter(sb)
                .WithFirstLineHeader()
                .WithField("Value", fieldName: "Id")
                )
            {
                w.Write(1);
                w.Write(2);
                w.Write(2);
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void TestHashtable()
        {
            string expected = @"Id,Name
2,Tom
1,Raj
2,Tom
1,Raj";
            string actual = null;
            Hashtable ht = new Hashtable();
            ht.Add(1, "Raj");
            ht.Add(2, "Tom");

            StringBuilder sb = new StringBuilder();
            using (var w = new ChoCSVWriter(sb)
                .WithFirstLineHeader()
                .WithField("Key", fieldName: "Id")
                .WithField("Value", fieldName: "Name")
                )
            {
                w.Write(ht);
                w.Write(ht);
                w.Write((Hashtable)null);
                Assert.Throws<ChoWriterException>(() => w.Write(1));
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void TestDictionary()
        {
            string expected = @"Key,Name,Salary,Department
1,Tom,10000,IT";
            string actual = null;

            Dictionary<int, Manager> ht = new Dictionary<int, Manager>();
            ht.Add(1, new Manager { Name = "Tom", Salary = 10000, Department = "IT" });
            ht.Add(2, new Manager { Name = "Tom", Salary = 10000, Department = "IT" });
            Dictionary<int, Employee> ht1 = new Dictionary<int, Employee>();

            StringBuilder sb = new StringBuilder();
            using (var w = new ChoCSVWriter(sb)
                .WithFirstLineHeader()
                )
            {
                w.Write(ht);
                w.Write(ht1);
            }
            actual = sb.ToString();
            Console.WriteLine(actual);
            //Assert.AreEqual(expected, actual);
        }


        [Test]
        public static void AnonymousTypeTest()
        {
            string expected = @"Id,Name
1,Tom";
            StringBuilder sb = new StringBuilder();
            using (var w = new ChoCSVWriter(sb)
                .WithFirstLineHeader()
                )
            {
                w.Write(new { Id = 1, Name = "Tom" });
            }
            string actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        public class Person
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Emp : Person
        {
            public string Role { get; set; }
            public string City { get; set; }
        }

        [Test]
        public static void HierTest()
        {
            string expected = @"Id,Name,Role,City
1,Tom,Developer,Edison
2,Mark,Analyst,Princeton";

            var e1 = new Emp { Id = 1, Name = "Tom", City = "Edison", Role = "Developer" };
            var e2 = new Emp { Id = 2, Name = "Mark", City = "Princeton", Role = "Analyst" };

            var csv = ChoCSVWriter<Emp>.ToTextAll(new Emp[] { e1, e2 },
                new ChoCSVRecordConfiguration().Configure(c => c.FileHeaderConfiguration.HasHeaderRecord = true));
            Assert.AreEqual(expected, csv);
        }


        [Test]
        public static void SelectiveFieldPOCOTest()
        {
            string expected = @"Id,Name
10,Mark
200,Lou";
            string actual = null;

            List<EmployeeRecWithCurrency> objs = new List<EmployeeRecWithCurrency>();
            EmployeeRecWithCurrency rec1 = new EmployeeRecWithCurrency();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.Salary = new ChoCurrency(100000);
            objs.Add(rec1);

            EmployeeRecWithCurrency rec2 = new EmployeeRecWithCurrency();
            rec2.Id = 200;
            rec2.Name = "Lou";
            rec2.Salary = new ChoCurrency(150000);
            objs.Add(rec2);

            StringBuilder msg = new StringBuilder();

            string[] f = new string[] { "Id", "Name" };

            using (var w = new ChoCSVWriter(msg)
                .WithFields(f)
                .WithFirstLineHeader()
                )
            {
                w.Write(objs);
            }

            actual = msg.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class EmployeeRecSimple
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
        [ChoCSVFileHeader()]
        [ChoCSVRecordObject(",")]
        public partial class EmployeeRecSimple1
        {
            [ChoCSVRecordField(1)] public int Id { get; set; }
            [ChoCSVRecordField(2)] public string Name { get; set; }
        }

        [Test]
        public static void QuotesIssue()
        {
            string expected = @"Id,Name
20,John Smith
21,""Jack in ,Da Box""";
            string actual = null;

            List<EmployeeRecSimple1> objs = new List<EmployeeRecSimple1>()
            {
                new EmployeeRecSimple1() { Id = 20, Name = "John Smith" },
                new EmployeeRecSimple1() { Id = 21, Name = "Jack in ,Da Box" }
            };
            actual = ChoCSVWriter<EmployeeRecSimple1>.ToTextAll(objs);
            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void QuotesIssue1()
        {
            string expected = @"20,John Smith
21,Jack in ""Da Box";

            string actual = null;

            List<EmployeeRecSimple> objs = new List<EmployeeRecSimple>()
            {
                new EmployeeRecSimple() { Id = 20, Name = "John Smith" },
                new EmployeeRecSimple() { Id = 21, Name = @"Jack in ""Da Box" }
            };
            actual = ChoCSVWriter<EmployeeRecSimple>.ToTextAll(objs);
            Assert.AreEqual(expected, actual);

            string expected1 = @"[
  {
    ""Id"": 20,
    ""Name"": ""John Smith""
  },
  {
    ""Id"": 21,
    ""Name"": ""Jack in \""Da Box""
  }
]";
            using (var r = ChoCSVReader<EmployeeRecSimple>.LoadText(expected))
            {
                var recs = r.ToArray();
                recs.Print();

                actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected1, actual);
            }
        }

        [Test]
        public static void ReadNWrite()
        {
            string expected = @"1,Tom,NY
2,Mark,NY";
            string csv = @"Id, Name
1, Tom
2, Mark
";

            StringBuilder csvOut = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoCSVWriter(csvOut))
                    w.Write(r.Select(r1 => new
                    {
                        r1.Id,
                        r1.Name,
                        City = "NY"
                    }));
            }
            string actual = csvOut.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Issue45()
        {
            string expected = @"DateOfBirth,Email,FirstName,FundDeposits,LastName,PhoneNumber,Source,State
10/11/2016,test@gmail.com,Raj,100,Mark,609-333-2222,IVR,NJ
10/11/2016,test@gmail.com,Raj,100,Mark,609-333-2222,IVR,NJ
10/11/2016,test@gmail.com,Raj,100,Mark,609-333-2222,IVR,NJ
10/11/2016,test@gmail.com,Raj,100,Mark,609-333-2222,IVR,NJ
10/11/2016,test@gmail.com,Raj,100,Mark,609-333-2222,IVR,NJ
10/11/2016,test@gmail.com,Raj,100,Mark,609-333-2222,IVR,NJ
10/11/2016,test@gmail.com,Raj,100,Mark,609-333-2222,IVR,NJ
10/11/2016,test@gmail.com,Raj,100,Mark,609-333-2222,IVR,NJ
10/11/2016,test@gmail.com,Raj,100,Mark,609-333-2222,IVR,NJ
10/11/2016,test@gmail.com,Raj,100,Mark,609-333-2222,IVR,NJ";
            string actual = null;

            var bucket = new List<ExpandoObject>();

            var x1 = Enumerable.Range(1, 10).Select(x =>
            {
                dynamic record = new ExpandoObject();
                record.DateOfBirth = "10/11/2016";
                record.Email = "test@gmail.com";
                record.FirstName = "Raj";
                record.FundDeposits = 100;
                record.LastName = "Mark";
                record.PhoneNumber = "609-333-2222";
                record.Source = "IVR";
                record.State = "NJ";
                bucket.Add(record);
                return record;
            }).ToArray();


            StringBuilder msg = new StringBuilder();
            using (var parser = new ChoCSVWriter(msg).WithFirstLineHeader())
            {
                parser.Write(bucket.ToList());
            }
            actual = msg.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ValidateSchema()
        {
            string expected = @"Id,Name,Address
1,Mark,1 Main St.";
            string actual = null;

            StringBuilder csv = new StringBuilder();

            var x = new
            {
                Id = 1,
                Name = "Mark",
                Address = "1 Main St.",
            };

            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader()
                .WithField("Id")
                .WithField("Name")
                .WithField("Address")
                .Configure(c => c.ColumnCountStrict = true)
                .Configure(c => c.ColumnOrderStrict = true)
                .Configure(c => c.ErrorMode = ChoErrorMode.ThrowAndStop)
                )
            {
                w.Write(x);
            }

            actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }


        public class StudentInfo
        {
            [ChoDictionaryKey("K1,K2,K3")]
            public Dictionary<string, string> Grades { get; set; }
            [Range(0, 1)]
            [DisplayName("Cre")]
            public Course[] Courses { get; set; }
            //public Course[] Courses { get; set; }
            public string Id { get; set; }
            [DisplayName("Std")]
            public Student Student { get; set; }
            [Range(0, 3)]
            [DisplayName("Sub")]
            public string[] Subjects { get; set; }
            public Teacher Teacher { get; set; }
        }

        public class Student
        {
            [DisplayName("StdId")]
            public string Id { get; set; }
            [DisplayName("StdName")]
            public string Name { get; set; }
        }

        public class Teacher
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public class Course
        {
            [DisplayName("CreId")]
            public string CourseId { get; set; }
            [DisplayName("CreName")]
            public string CourseName { get; set; }
        }

        [Test]
        public static void ComplexObjToCSV()
        {
            string expected = @"CreId_0,CreName_0,CreId_1,CreName_1,SId,StdId,StdName,Sub_0,Sub_1,Sub_2,Sub_3,Teacher.Id,Teacher.Name,K1,K2
c0,Math0,c1,Math1,100,1,Mark,Math,Physics,,,2,Tom,A,B";
            string actual = null;

            var si = new StudentInfo
            {
                Id = "100",
                Student = new Student { Id = "1", Name = "Mark" },
                Teacher = new Teacher { Id = "2", Name = "Tom" },
                Courses = new Course[]
                {
                    new Course { CourseId = "c0", CourseName = "Math0" },
                    new Course { CourseId = "c1", CourseName = "Math1" }
                },
                Subjects = new string[]
                {
                    "Math",
                    "Physics"
                },
                Grades = new Dictionary<string, string>
                {
                    { "K0", "0" },
                    { "K1", "A" },
                    { "K2", "B" }
                }
            };

            StringBuilder sb = new StringBuilder();
            using (var w = new ChoCSVWriter<StudentInfo>(sb)
                .WithFirstLineHeader()
                //.WithField(c => c.Courses, defaultValue: null)
                .WithField(c => c.Id, fieldName: "SId")
                //.WithField(c => c.Courses.FirstOrDefault().CourseId, fieldName: "CId")
                //.Index(c => c.Courses, 1, 1)
                .DictionaryKeys(c => c.Grades, "K1", "K2")
                )
            {
                w.Write(si);
            }

            actual = sb.ToString();
            expected.Print();
            actual.Print();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DataReaderTest()
        {
            string csv = @"Id, Name, Address2
1, Tom,
2, Mark,";

            var dr = ChoCSVReader.LoadText(csv).WithFirstLineHeader().AsDataReader();

            using (var sw = new StreamWriter(File.OpenWrite(FileNameDataReaderTestTestCSV)))
            {
                using (var csvWriter = new ChoCSVWriter(sw)
                    .WithFirstLineHeader()
                    //.Configure(c => c.UseNestedKeyFormat = false)
                    )
                {
                    csvWriter.WriteDataReader(dr);
                    sw.Flush();
                }
            }

            FileAssert.AreEqual(FileNameDataReaderTestExpectedCSV, FileNameDataReaderTestTestCSV);
        }

        [Test]
        public static void Pivot()
        {
            string expected = @"Name,Foo,Bar
Address,Foo's address,Bar's address
Age,24,19";
            string actual = null;

            string csv = @"Name, Address, Age
""Foo"", ""Foo's address"", 24
""Bar"", ""Bar's address"", 19";

            StringBuilder sb = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                //.WithFirstLineHeader()
                .MayHaveQuotedFields()
                )
            {
                actual = ChoCSVWriter.ToTextAll(r.Transpose(false));
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void LargeXmlToCSV()
        {
            return;
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            XmlReaderSettings settings = new XmlReaderSettings();

            // SET THE RESOLVER
            settings.XmlResolver = new XmlUrlResolver();

            settings.ValidationType = ValidationType.DTD;
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            settings.IgnoreWhitespace = true;

            Console.WriteLine(DateTime.Now.ToString());

            using (var r = new ChoXmlReader(XmlReader.Create(@"dblp.xml",
                settings)))
            {
                using (FileStream fs = File.Open(@"dblp.csv", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                using (BufferedStream bs = new BufferedStream(fs))
                using (var w = new ChoCSVWriter(bs)
                    .WithFirstLineHeader())
                {
                    w.NotifyAfter(1000);
                    w.Write(r);
                }
            }
            Console.WriteLine(DateTime.Now.ToString());
        }

        private static void ValidationCallBack(object sender, ValidationEventArgs e)
        {
        }

        [Test]
        public static void JSON2CSVTest1()
        {
            string expected = @"data_getUsers_0_userProfileDetail_userStatus_name,data_getUsers_0_userProfileDetail_userStatusDate,data_getUsers_0_userProfileDetail_lastAttestationDate,data_getUsers_0_userInformation_Id,data_getUsers_0_userInformation_lastName,data_getUsers_0_userInformation_suffix,data_getUsers_0_userInformation_gender,data_getUsers_0_userInformation_birthDate,data_getUsers_0_userInformation_ssn,data_getUsers_0_userInformation_ethnicity,data_getUsers_0_userInformation_languagesSpoken,data_getUsers_0_userInformation_personalEmail,data_getUsers_0_userInformation_otherNames,data_getUsers_0_userInformation_userType_name,data_getUsers_0_userInformation_primaryuserState,data_getUsers_0_userInformation_otheruserState_0,data_getUsers_0_userInformation_practiceSetting,data_getUsers_0_userInformation_primaryEmail
Expired,4/4/2017 3:48:25 AM,2/1/2019 3:50:42 AM,13610875,************,,FEMALE,12/31/1969 7:01:00 PM,000000000,INVALID_REFERENCE_VALUE,,,,APN,CO,CO,INPATIENT_ONLY,*****@*****.com";
            string actual = null;

            string json = @"{
  ""data"": {
    ""getUsers"": [
      {
        ""userProfileDetail"": {
          ""userStatus"": {
            ""name"": ""Expired""
          },
          ""userStatusDate"": ""2017-04-04T07:48:25+00:00"",
          ""lastAttestationDate"": ""2019-02-01T03:50:42.6049634-05:00""
        },
        ""userInformation"": {
          ""Id"": 13610875,
          ""lastName"": ""************"",
          ""suffix"": null,
          ""gender"": ""FEMALE"",
          ""birthDate"": ""1970-01-01T00:01:00+00:00"",
          ""ssn"": ""000000000"",
          ""ethnicity"": ""INVALID_REFERENCE_VALUE"",
          ""languagesSpoken"": null,
          ""personalEmail"": null,
          ""otherNames"": null,
          ""userType"": {
            ""name"": ""APN""
          },
          ""primaryuserState"": ""CO"",
          ""otheruserState"": [
            ""CO""
          ],
          ""practiceSetting"": ""INPATIENT_ONLY"",
          ""primaryEmail"": ""*****@*****.com""
        }
      }
    ]
  }
}";
            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";

            actual = ChoCSVWriter.ToTextAll(ChoJSONReader.LoadText(json,
                new ChoJSONRecordConfiguration()),
                new ChoCSVRecordConfiguration().Configure(c => c.WithFirstLineHeader()));

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void JSON2CSVTest2()
        {
            string expected = @"personalInformation_userId,personalInformation_firstName,personalInformation_languagesSpoken_0_name,personalInformation_languagesSpoken_1_name,personalInformation_languagesSpoken_2_name,personalInformation_state
13610642,***,,,,CA|IL
13611014,**,Afrikaans,Albanian,American Sign Language,WA|TX|GA|MN|NV
13611071,***,Albanian,Hindi,Telugu,OK|AK|WA|MA|GA|MN
13611074,********,,,,AZ
13611082,******,Estonian,Faroese,English,AK|CA|GA|IL|NC|NV|TX|OK|OR|MA|MN|MS|WA|WV|CO
13611227,**,Latvian,English,Fiji,CO|GA|IL|MN|MS|MA|NC|NV|OK|OR|WA|WV";
            string actual = null;

            string json = @"{
    ""data"": {
        ""getUsers"": [
            {
                ""personalInformation"": {
                    ""userId"": 13610642,
                    ""firstName"": ""***"",
                    ""languagesSpoken"": null,
                    ""state"": [
                        ""CA"",
                        ""IL""
                    ]
                }
            },
            {
                ""personalInformation"": {
                    ""userId"": 13611014,
                    ""firstName"": ""**"",
                    ""languagesSpoken"": [
                        {
                            ""name"": ""Afrikaans""
                        },
                        {
                            ""name"": ""Albanian""
                        },
                        {
                            ""name"": ""American Sign Language""
                        }
                    ],
                    ""state"": [
                        ""WA"",
                        ""TX"",
                        ""GA"",
                        ""MN"",
                        ""NV""
                    ]
                }
            },
            {
                ""personalInformation"": {
                    ""userId"": 13611071,
                    ""firstName"": ""***"",
                    ""languagesSpoken"": [
                        {
                            ""name"": ""Albanian""
                        },
                        {
                            ""name"": ""Hindi""
                        },
                        {
                            ""name"": ""Telugu""
                        },
                        {
                            ""name"": ""Malayalam""
                        },
                        {
                            ""name"": ""Tamil""
                        }
                    ],
                    ""state"": [
                        ""OK"",
                        ""AK"",
                        ""WA"",
                        ""MA"",
                        ""GA"",
                        ""MN""
                    ],
                }
            },
            {
                ""personalInformation"": {
                    ""userId"": 13611074,
                    ""firstName"": ""********"",
                    ""languagesSpoken"": null,
                    ""state"": [
                        ""AZ""
                    ]
                }
            },
            {
                ""personalInformation"": {
                    ""userId"": 13611082,
                    ""firstName"": ""******"",
                    ""languagesSpoken"": [
                        {
                            ""name"": ""Estonian""
                        },
                        {
                            ""name"": ""Faroese""
                        },
                        {
                            ""name"": ""English""
                        },
                        {
                            ""name"": ""Hindi""
                        }
                    ],
                    ""state"": [
                        ""AK"",
                        ""CA"",
                        ""GA"",
                        ""IL"",
                        ""NC"",
                        ""NV"",
                        ""TX"",
                        ""OK"",
                        ""OR"",
                        ""MA"",
                        ""MN"",
                        ""MS"",
                        ""WA"",
                        ""WV"",
                        ""CO""
                    ]
                }
            },
            {
                ""personalInformation"": {
                    ""userId"": 13611227,
                    ""firstName"": ""**"",
                    ""languagesSpoken"": [
                        {
                            ""name"": ""Latvian""
                        },
                        {
                            ""name"": ""English""
                        },
                        {
                            ""name"": ""Fiji""
                        },
                        {
                            ""name"": ""Hindi""
                        },
                        {
                            ""name"": ""Japanese""
                        },
                        {
                            ""name"": ""Sanskrit""
                        },
                        {
                            ""name"": ""Zhuang""
                        }
                    ],
                    ""state"": [
                        ""CO"",
                        ""GA"",
                        ""IL"",
                        ""MN"",
                        ""MS"",
                        ""MA"",
                        ""NC"",
                        ""NV"",
                        ""OK"",
                        ""OR"",
                        ""WA"",
                        ""WV""
                    ]
                }
            }
        ]
    }
}";
            StringBuilder csv = new StringBuilder();

            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..getUsers[*]", true)
                .Configure(c => c.JsonSerializerSettings = new JsonSerializerSettings
                {
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    //DateParseHandling = DateParseHandling.DateTimeOffset
                })
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.MaxScanRows = 2)
                    .Configure(c => c.ThrowAndStopOnMissingField = false)
                    )
                {
                    w.Write(r.Select(r1 =>
                    {
                        r1.personalInformation.state = String.Join("|", ((IList)r1.personalInformation.state).OfType<string>());
                        return r1;
                    }
                        ));
                }
            }

            actual = csv.ToString();

            Assert.AreEqual(expected, actual);
        }


        [Test]
        public static void JSON2CSVTest3()
        {
            string expected = @"UserProfileDetail_UserStatus_name,UserProfileDetail_UserStatusDate,UserProfileDetail_EnrollId,UserProfileDetail_lastDate,UserInformation_Id,UserInformation_firstName,UserInformation_middleName,UserInformation_lastName,UserInformation_otherNames,UserInformation_UserType_name,UserInformation_primaryState,UserInformation_otherState_0,UserInformation_otherState_1,UserInformation_UserLicense_0_licenseState,UserInformation_UserLicense_0_licenseNumber,UserInformation_UserLicense_0_licenseStatus,UserInformation_Setting,UserInformation_primaryEmail,UserInformation_modifiedAt,UserInformation_createdAt,message,extensions_code
User One,10/31/2018 2:12:42 AM,am**********************************,7/22/2019 3:05:39 AM,1111122,*****,,*****,,CP,MA,MA,BA,MA,000000000,,ADMINISTRATIVE,*****@*****.com,,,GraphQL.ExecutionError: 13614711 - NO__DATA,212
User Two,10/31/2019 2:12:42 AM,am**********************************,7/22/2019 3:05:39 AM,443333,*****,Jhon,*****,,AP,AK,MP,CLT,KL,000000220,Valid,ADMINISTRATIVE,*****@*****.com,,,GraphQL.ExecutionError: 13614712 - NO__DATA,212
,,,,,,,,,,,,,,,,,,,,""GraphQL.ExecutionError: Cannot return null for non-null type. Field: PrivilegeFlag, Type: Boolean!.
   at GraphQL.Execution.ExecutionStrategy.ValidateNodeResult(ExecutionContext context, ExecutionNode node)
   at GraphQL.Execution.ExecutionStrategy.ExecuteNodeAsync(ExecutionContext context, ExecutionNode node)"",ID: 1454790
,,,,,,,,,,,,,,,,,,,,""GraphQL.ExecutionError: Cannot return null for non-null type. Field: admittingArrangementFlag, Type: Boolean!.
   at GraphQL.Execution.ExecutionStrategy.ValidateNodeResult(ExecutionContext context, ExecutionNode node)
   at GraphQL.Execution.ExecutionStrategy.ExecuteNodeAsync(ExecutionContext context, ExecutionNode node)"",ID: 13614790";

            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";

            StringBuilder csv = new StringBuilder();
            using (var r = new ChoJSONReader(@"test1.json")
                 .WithJSONPath("$..data.getUsers[*]", true))
            {
                using (var r1 = new ChoJSONReader(@"test1.json")
                    .WithJSONPath("$..errors[*]", true)
                    )
                {
                    var r3 = r1.ZipOrDefault(r, (i, j) =>
                    {
                        if (j != null)
                        {
                            j.Merge(i);
                            return j;
                        }
                        else
                            return i;
                    });

                    using (var w = new ChoCSVWriter(csv)
                        .WithFirstLineHeader()
                        .Configure(c => c.MaxScanRows = 10)
                        .Configure(c => c.ThrowAndStopOnMissingField = false)
                        )
                    {
                        w.Write(r3);
                    }
                }
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void JSON2CSVTest4()
        {
            string expected = @"Id,firstName,name,primaryState,otherState_0,otherState_1,otherState_2,otherState_3,otherState_4,createdAt
1111122,*****1,CP,MA,MA,BA,,,,
3333,*****3,CPP,MPA,KL,TN,DL,AP,RJ,";
            string actual = null;

            string json = @"{
    ""getUsers"": [
        {
            ""UserInformation"": {
                ""Id"": 1111122,
                ""firstName"": ""*****1"",
                ""UserType"": {
                    ""name"": ""CP""
                },
                ""primaryState"": ""MA"",
                ""otherState"": [
                    ""MA"",
                    ""BA""
                ],
                ""createdAt"": null
            }
        },
        {
            ""UserInformation"": {
                ""Id"": 3333,
                ""firstName"": ""*****3"",
                ""UserType"": {
                    ""name"": ""CPP""
                },
                ""primaryState"": ""MPA"",
                ""otherState"": [
                    ""KL"",
                    ""TN"",
                    ""DL"",
                    ""AP"",
                    ""RJ""
                ],
                ""createdAt"": null
            }
        }
    ]
}";
            StringBuilder csv = new StringBuilder();

            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..getUsers[*]", true)
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.MaxScanRows = 2)
                    .Configure(c => c.ThrowAndStopOnMissingField = false)
                    .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                    )
                {
                    w.Write(r);
                }
            }

            actual = csv.ToString();

            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSVTest5()
        {
            string expected = @"Name,Description,AccountNumber
Xytrex Co.,Industrial Cleaning Supply Company,ABC15797531
""Watson and Powell, Inc."",Law firm. New York Headquarters,ABC24689753";
            string actual = null;

            string json = @"[
   {
      ""Name"" : ""Xytrex Co."",
      ""Description"" : ""Industrial Cleaning Supply Company"",
      ""AccountNumber"" : ""ABC15797531""
   },
   {
      ""Name"" : ""Watson and Powell, Inc."",
      ""Description"" : ""Law firm. New York Headquarters"",
      ""AccountNumber"" : ""ABC24689753""     
   }
]";
            StringBuilder csv = new StringBuilder();

            using (var r = ChoJSONReader.LoadText(json)
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(r);
                }
            }

            actual = csv.ToString();

            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSVTest6()
        {

            string json = @"{
  ""Id"": ""123456"",
  ""Request"": [
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
            StringBuilder csv = new StringBuilder();

            using (var r = ChoJSONReader.LoadText(json)
                   )
            {
                using (var w = new ChoCSVWriter(csv).WithFirstLineHeader())
                    w.Write(r);
            }
            var actual = csv.ToString();
            var expected = @"Id,Request_0_firstName,Request_0_lastName,Response_0_SId
123456,A,B,123";

            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSVTest7()
        {

            string json = @"{
  ""Id"": ""123456"",
  ""Request"": [
    {
      ""firstName"": ""A"",
      ""lastName"": ""B"",
    },
    {
      ""firstName"": ""A1"",
      ""lastName"": ""B1"",
    }
	],
  ""Response"": [
    {
      ""SId"": ""123""
    },
    {
      ""SId"": ""1234""
    }
  ]
}";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json)
                   .Configure(c => c.FlattenNode = true).Configure(c => c.FlattenByNodeName = "Request")
                   )
            {
                using (var w = new ChoCSVWriter(csv).WithFirstLineHeader())
                    w.Write(r);
            }
            var actual = csv.ToString();
            var expected = @"Id,Response_0_SId,Response_1_SId,RequestfirstName,RequestlastName
123456,123,1234,A,B
123456,123,1234,A1,B1";

            Assert.AreEqual(expected, actual);
        }
        public class Client
        {
            public int Indice { get; set; }
            public string Name { get; set; }
            public string Surname { get; set; }
            public string Company { get; set; }
            public string Tel1 { get; set; }
            public string Tel2 { get; set; }
        }

        public class CallClient
        {
            public int Indice { get; set; }
            public string CallDateTime { get; set; }
            public string Status { get; set; }
        }

        public class ResponsePollClient
        {
            public int Indice { get; set; }
            public string Question1 { get; set; }
            public string Question2 { get; set; }
            public string Question3 { get; set; }
            public string StatusPoll { get; set; }
        }

        public class DataClient
        {
            public Client client { get; set; }
            public CallClient callClient { get; set; }
            public ResponsePollClient pollClient { get; set; }
        }
        [Test]
        public static void WriteComplexObjs()
        {
            string expected = @"client.Indice,client.Name,client.Surname,client.Company,client.Tel1,client.Tel2,callClient.CallDateTime,callClient.Status,pollClient.Question1,pollClient.Question2,pollClient.Question3,pollClient.StatusPoll
1,Name,Surname,ABC Company,555-555-5555,610-333-1234,1/1/2023 12:00:00 AM,Approved,Question1,Question2,Question3,StatusPoll";

            var rec = new DataClient
            {
                client = new Client
                {
                    Indice = 1,
                    Company = "ABC Company",
                    Name = "Name",
                    Surname = "Surname",
                    Tel1 = "555-555-5555",
                    Tel2 = "610-333-1234"
                },
                callClient = new CallClient
                {
                    Indice = 1,
                    CallDateTime = new DateTime(2023, 01, 01).ToString(),
                    Status = "Approved"
                },
                pollClient = new ResponsePollClient
                {
                    Indice = 1,
                    Question1 = "Question1",
                    Question2 = "Question2",
                    Question3 = "Question3",
                    StatusPoll = "StatusPoll"
                }
            };

            StringBuilder csv = new StringBuilder();

            var cf = new ChoCSVRecordConfiguration<DataClient>()
                .WithFirstLineHeader()
                .Ignore(f => f.callClient.Indice)
                .Ignore(f => f.pollClient.Indice)
                ;

            using (var w = new ChoCSVWriter<DataClient>(csv, cf)
                )
            {
                w.Write(rec);
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void WriteComplexObjs_1()
        {
            string expected = @"client.Indice,client.Name,client.Surname,client.Company,client.Tel1,client.Tel2,callClient.CallDateTime,callClient.Status,pollClient.Question1,pollClient.Question2,pollClient.Question3,pollClient.StatusPoll
1,Name,Surname,ABC Company,555-555-5555,610-333-1234,1/1/2023 12:00:00 AM,Approved,Question1,Question2,Question3,StatusPoll";

            var rec = new DataClient
            {
                client = new Client
                {
                    Indice = 1,
                    Company = "ABC Company",
                    Name = "Name",
                    Surname = "Surname",
                    Tel1 = "555-555-5555",
                    Tel2 = "610-333-1234"
                },
                callClient = new CallClient
                {
                    Indice = 1,
                    CallDateTime = new DateTime(2023, 01, 01).ToString(),
                    Status = "Approved"
                },
                pollClient = new ResponsePollClient
                {
                    Indice = 1,
                    Question1 = "Question1",
                    Question2 = "Question2",
                    Question3 = "Question3",
                    StatusPoll = "StatusPoll"
                }
            };

            StringBuilder csv = new StringBuilder();

            var cf = new ChoCSVRecordConfiguration<DataClient>()
                .WithFirstLineHeader()
                .Ignore(f => f.callClient.Indice)
                .Ignore(f => f.pollClient.Indice)
                .Map(f => f.client.Indice)
                .Map(f => f.client.Name)
                .Map(f => f.client.Tel1)
                .Map(f => f.callClient.CallDateTime)
                .Map(f => f.callClient.Status)
                .Map(f => f.pollClient.Question1)
                .Map(f => f.pollClient.Question2)
                .Map(f => f.pollClient.Question2)
                .Map(f => f.pollClient.StatusPoll)
                ;

            using (var w = new ChoCSVWriter<DataClient>(csv, cf)
                //.WithFirstLineHeader()
                //.ClearFields()
                //.WithField(f => f.client.Indice)
                //.WithField(f => f.client.Name)
                //.WithField(f => f.client.Tel1)
                //.WithField(f => f.callClient.CallDateTime)
                //.WithField(f => f.callClient.Status)
                //.WithField(f => f.pollClient.Question1)
                //.WithField(f => f.pollClient.Question2)
                //.WithField(f => f.pollClient.Question2)
                //.WithField(f => f.pollClient.StatusPoll)
                )
            {
                w.Write(rec);
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void WriteComplexObjs_2()
        {
            string expected = @"client.Indice,client.Name,client.Surname,client.Company,client.Tel1,client.Tel2,callClient.CallDateTime,callClient.Status,pollClient.Question1,pollClient.Question2,pollClient.Question3,pollClient.StatusPoll
1,Name,Surname,ABC Company,555-555-5555,610-333-1234,1/1/2023 12:00:00 AM,Approved,Question1,Question2,Question3,StatusPoll";

            var rec = new DataClient
            {
                client = new Client
                {
                    Indice = 1,
                    Company = "ABC Company",
                    Name = "Name",
                    Surname = "Surname",
                    Tel1 = "555-555-5555",
                    Tel2 = "610-333-1234"
                },
                callClient = new CallClient
                {
                    Indice = 1,
                    CallDateTime = new DateTime(2023, 01, 01).ToString(),
                    Status = "Approved"
                },
                pollClient = new ResponsePollClient
                {
                    Indice = 1,
                    Question1 = "Question1",
                    Question2 = "Question2",
                    Question3 = "Question3",
                    StatusPoll = "StatusPoll"
                }
            };

            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter<DataClient>(csv)
                .WithFirstLineHeader()
                .IgnoreField(f => f.callClient.Indice)
                .IgnoreField(f => f.pollClient.Indice)
                )
            {
                w.Write(rec);
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void WriteComplexObjs_31()
        {
            string expected = @"client.Indice,client.Name,client.Surname,client.Company,client.Tel1,client.Tel2,callClient.CallDateTime,callClient.Status,pollClient.Question1,pollClient.Question2,pollClient.Question3,pollClient.StatusPoll
1,Name,Surname,ABC Company,555-555-5555,610-333-1234,1/1/2023 12:00:00 AM,Approved,Question1,Question2,Question3,StatusPoll";

            var rec = new DataClient
            {
                client = new Client
                {
                    Indice = 1,
                    Company = "ABC Company",
                    Name = "Name",
                    Surname = "Surname",
                    Tel1 = "555-555-5555",
                    Tel2 = "610-333-1234"
                },
                callClient = new CallClient
                {
                    Indice = 1,
                    CallDateTime = new DateTime(2023, 01, 01).ToString(),
                    Status = "Approved"
                },
                pollClient = new ResponsePollClient
                {
                    Indice = 1,
                    Question1 = "Question1",
                    Question2 = "Question2",
                    Question3 = "Question3",
                    StatusPoll = "StatusPoll"
                }
            };

            StringBuilder csv = new StringBuilder();

            //var cf = new ChoCSVRecordConfiguration<DataClient>()
            //    .WithFirstLineHeader()
            //    .Ignore(f => f.callClient.Indice)
            //    .Ignore(f => f.pollClient.Indice)
            //    .Map(f => f.client.Indice)
            //    .Map(f => f.client.Name)
            //    .Map(f => f.client.Tel1)
            //    .Map(f => f.callClient.CallDateTime)
            //    .Map(f => f.callClient.Status)
            //    .Map(f => f.pollClient.Question1)
            //    .Map(f => f.pollClient.Question2)
            //    .Map(f => f.pollClient.Question2)
            //    .Map(f => f.pollClient.StatusPoll)
            //    ;

            using (var w = new ChoCSVWriter<DataClient>(csv)
                .WithFirstLineHeader()
                //.ClearFields()
                .IgnoreField(f => f.callClient.Indice)
                .IgnoreField(f => f.pollClient.Indice)
                .WithField(f => f.client.Indice)
                .WithField(f => f.client.Name)
                .WithField(f => f.client.Tel1)
                .WithField(f => f.callClient.CallDateTime)
                .WithField(f => f.callClient.Status)
                .WithField(f => f.pollClient.Question1)
                .WithField(f => f.pollClient.Question2)
                .WithField(f => f.pollClient.Question2)
                .WithField(f => f.pollClient.StatusPoll)

                )
            {
                w.Write(rec);
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class StudentInfo1
        {
            public string Id { get; set; }
            public string Name { get; set; }
            [Range(0, 2)]
            public Course1[] Courses { get; set; }

            public StudentInfo1()
            {
                Courses = new Course1[2];
            }
        }
        public class Course1
        {
            //[DisplayName("CreId")]
            public string CourseId { get; set; }
            //[DisplayName("CreName")]
            public string CourseName { get; set; }
        }

        [Test]
        public static void ArrayWriteTest()
        {
            string expected = @"Id,Name,Courses.CourseId_0,Courses.CourseName_0,Courses.CourseId_1,Courses.CourseName_1,Courses.CourseId_2,Courses.CourseName_2
1,Tom,C11,Math,C12,Biology,,";
            var rec = new StudentInfo1
            {
                Id = "1",
                Name = "Tom",
                Courses = new Course1[]
                {
                    new Course1
                    {
                        CourseId = "C11",
                        CourseName = "Math"
                    },
                    new Course1
                    {
                        CourseId = "C12",
                        CourseName = "Biology"
                    }
                }
            };

            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter<StudentInfo1>(csv)
                .WithFirstLineHeader()
                //.ClearFields()
                .WithField(o => o.Id)
                //.WithField(o => o.Name)
                //.WithField(o => o.Courses.FirstOrDefault().CourseId, fieldName: "CreId")
                //.WithFieldForType<Course1>(o => o.CourseId, fieldName: "CreId")
                .Index(o => o.Courses, 0, -1)
                .NestedKeySeparator('.')
                .WithMaxScanRows(2)
                )
            {
                w.Write(rec);
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        class UserFavourites
        {
            [ChoCSVRecordField]
            public int Id { get; set; }
            [ChoCSVRecordField]
            public string Title { get; set; }

            public override string ToString()
            {
                return $"{Id}, {Title}";
            }
        }
        class UserAndValues : IChoArrayItemFieldNameOverrideable
        {
            [ChoCSVRecordField]
            public int UserID { get; set; }
            [ChoCSVRecordField]
            public string FirstName { get; set; }
            [ChoCSVRecordField]
            public string LastName { get; set; }
            [ChoTypeConverter(typeof(MyListConverter))]
            [ChoCSVRecordField(QuoteField = false)]
            //[Range(1, 2)]
            public List<UserFavourites> Favourites { get; set; }
            [ChoCSVRecordField]
            public UserFavourites SelectedUserFavourites { get; set; }

            public string GetFieldName(string declaringMemberName, string memberName, string separator, int index)
            {
                return $"{declaringMemberName}{index}{memberName}";
            }
        }

        public class MyListConverter : IChoValueConverter, IChoHeaderConverter, IChoValueSelector, IChoCollectionConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                var list = value as ICollection<UserFavourites>;
                return list.Select(f => new object[] { f.Id, f.Title }).Unfold().ToArray();
                return string.Join(",",
                    list.Select(f => f.ToString()));
            }

            public string GetHeader(string name, string fieldName, object parameter, CultureInfo culture)
            {
                return "x, y, z, a";
            }

            public object ExtractValue(string name, string fieldName, object value, CultureInfo culture)
            {
                IDictionary<string, object> dict = value as IDictionary<string, object>;
                List<UserFavourites> list = new List<UserFavourites>();

                list.Add(new UserFavourites
                {
                    Id = dict["x"].CastTo<int>(),
                    Title = dict["y"].CastTo<string>()
                });
                list.Add(new UserFavourites
                {
                    Id = dict["z"].CastTo<int>(),
                    Title = dict["a"].CastTo<string>()
                });
                return list;
            }
        }
        [Test]
        public static void NestedClass2CSVTest()
        {
            string expected = @"UserID,FirstName,LastName,x, y, z, a,Id,Title
1,Tom,Smith,11,Matrix,12,Matrix 2,100,Matrix 100";

            var rec1 = new UserAndValues
            {
                UserID = 1,
                FirstName = "Tom",
                LastName = "Smith",
                Favourites = new List<UserFavourites>
                {
                    new UserFavourites
                    {
                        Id = 11,
                        Title = "Matrix"
                    },
                    new UserFavourites
                    {
                        Id = 12,
                        Title = "Matrix 2"
                    }
                },
                SelectedUserFavourites = new UserFavourites
                {
                    Id = 100,
                    Title = "Matrix 100"
                }
            };

            //var c = new ChoCSVRecordConfiguration<UserAndValues>()
            //    .WithFirstLineHeader()
            //    .Map(f => f.UserID)
            //    .Map(f => f.FirstName)
            //    .Map(f => f.LastName)
            //    .Map(f => f.SelectedUserFavourites.Title)
            //    ;

            StringBuilder csv = new StringBuilder();
            using (var w = new ChoCSVWriter<UserAndValues>(csv/*, c*/)
                .WithFirstLineHeader()
                //.ClearFields()
                //.WithField(f => f.UserID)
                //.WithField(f => f.FirstName)
                //.WithField(f => f.LastName)
                //.WithField(f => f.SelectedUserFavourites.Title)
                //.WithField(f => f.Favourites, headerSelector: () => "x, y, z, a", quoteField: false)
                )
            {
                w.Write(rec1);
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
            return;

            var csv1 = @"UserID,FirstName,LastName,x, y, z, a
1,Tom,Smith,11,Matrix,12,Matrix 2";

            using (var r = ChoCSVReader<UserAndValues>.LoadText(csv1)
                .WithFirstLineHeader()
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }

        }

        public class Model
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public class Foo : Model
        {
            public string Description { get; set; }
            //[DisplayName("Parent")]
            public Foo ParentFoo { get; set; }
        }
        [Test]
        public static void NestedClassRefTest()
        {
            string expected = @"Id,Name,Description,ParentFoo.Id,ParentFoo.Name,ParentFoo.Description
1,Tom,Employee,2,Mark,Contractor";
            var f1 = new Foo
            {
                Id = 1,
                Name = "Tom",
                Description = "Employee",
                ParentFoo = new Foo
                {
                    Id = 2,
                    Name = "Mark",
                    Description = "Contractor",
                    ParentFoo = new Foo
                    {
                        Id = 3,
                        Name = "Kevin",
                        Description = "Employee",
                    }
                }
            };

            StringBuilder csv = new StringBuilder();
            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader()
                .UseNestedKeyFormat()
                .Configure(c => c.AutoIncrementDuplicateColumnNames = true)
                )
            {
                w.Write(f1);
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class EmpGuid
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public EmpGuid()
            {
                Id = Guid.Parse("ca6f8387-bcf0-45ce-85d8-d609dd5a96bd");
                Name = Id.ToString().Substring(5);
            }
        }
        [Test]
        public static void GuidWriteTest()
        {
            string expected = @"Id,Name
ca6f8387-bcf0-45ce-85d8-d609dd5a96bd,387-bcf0-45ce-85d8-d609dd5a96bd";

            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader())
            {
                w.Write(new EmpGuid());
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void ValueListTest()
        {
            string expected = @"X
1
2
3";
            List<int> list = new List<int>()
            {
                1,
                2,
                3
            };

            StringBuilder csv = new StringBuilder();
            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader()
                .WithField("Value", fieldName: "X")
                )
            {
                w.Write(1);
                w.Write(2);
                w.Write(3);

                //w.Write(list as IEnumerable<int>);
            }
            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void JSON2CSV()
        {
            string json = @"[
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
1234,PA|VA
1235,CA|DE|MD";

            var csvData = new StringBuilder();
            using (var jsonReader = ChoJSONReader.LoadText(json))
            {
                using (var csvWriter = new ChoCSVWriter(csvData)
                    .WithFirstLineHeader()
                    //.WithDelimiter("|")
                    //.QuoteAllFields()
                    .Configure(c => c.UseNestedKeyFormat = false)
                    .Configure(c => c.ArrayValueSeparator = '|')
                    //.Configure(c => c.NestedColumnSeparator = '|')
                    )
                {
                    csvWriter.Write(jsonReader);
                }
            }

            Console.WriteLine(csvData.ToString());
            var actual = csvData.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class ClassAttendance
        {
            //[ChoDictionaryKey("addcol1, addcol2, addcol3")]
            public IDictionary<string, object> AdditionalDetails { get; set; }
            public bool Confirmed { get; set; }
            public string DigitalDelivery { get; set; }
        }
        [Test]
        public static void IgnoreDictionaryFieldPrefixTest()
        {
            string expected = @"addcol1,addcol2,Confirmed,DigitalDelivery
one,two,True,DD1";

            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                .Configure(c => c.UseNestedKeyFormat = true)
                .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                .ThrowAndStopOnMissingField(false)
                )
            {
                var x = new ClassAttendance
                {
                    Confirmed = true,
                    DigitalDelivery = "DD1",
                    AdditionalDetails = new ChoDynamicObject(new Dictionary<string, object>
                    {
                        ["addcol1"] = "one",
                        ["addcol2"] = "two",
                    })
                }.ToDynamicObject().ConvertToFlattenObject('/', null, null, true);
                w.Write(x);
                //w.Write(new ClassAttendance
                //{
                //    Confirmed = true,
                //    DigitalDelivery = "DD1",
                //    AdditionalDetails = new ChoDynamicObject(new Dictionary<string, object>
                //    {
                //        ["addcol1"] = "one",
                //        ["addcol3"] = "three",
                //    })
                //});
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void CreateCSVFile()
        {
            string expected = @"val1,val2,val3,val4
1,2,3,4
1,2,3,4
1,2,3,4
1,2,3,4
1,2,3,4";
            List<dynamic> objs = new List<dynamic>();

            for (int i = 0; i < 5; i++)
            {
                dynamic rec = new ExpandoObject();
                rec.val1 = '1';
                rec.val2 = '2';
                rec.val3 = '3';
                rec.val4 = '4';
                objs.Add(rec);
            }

            StringBuilder csv = new StringBuilder();
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration
            {
                Encoding = Encoding.Default
            };

            using (var parser = new ChoCSVWriter(csv, config).WithFirstLineHeader())
            {
                parser.Write(objs);
            }
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class AddressObject
        {
            [DisplayName("City")]
            public string City { get; set; }
            public string Road { get; set; }
            public int RoadNumber { get; set; }
        }

        public class LocationRow
        {
            public Guid RowId { get; set; }
            public AddressObject Address { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
        }
        [Test]
        public static void WriteEmptyColumnWhenNestedObjectIsNull()
        {
            string expected = @"RowId,City,Road,RoadNumber,Latitude,Longitude
ca6f8387-bcf0-45ce-85d8-d609dd5a96bd,,,0,10,1";
            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter<LocationRow>(csv)
                .WithFirstLineHeader()
                .Configure(c => c.UseNestedKeyFormat = false)
                )
            {
                w.Write(new LocationRow
                {
                    RowId = Guid.Parse("ca6f8387-bcf0-45ce-85d8-d609dd5a96bd"),
                    Latitude = 10,
                    Longitude = 1
                });
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class ResultData
        {
            [JsonProperty(PropertyName = "Id")]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "Results")]
            public IEnumerable<Result> Results { get; set; }
        }

        public class Result
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
        [Test]
        public static void ComplexType2CSV()
        {
            string expected = @"Id,Key1,Key2,Key3
1,Value1,Value2,Value3
2,Value1,Value2,Value3";

            StringBuilder csv = new StringBuilder();

            var rec1 = new ResultData
            {
                Id = "1",
                Results = new List<Result>
                {
                    new Result
                    {
                        Name = "Key1",
                        Value = "Value1"
                    },
                    new Result
                    {
                        Name = "Key2",
                        Value = "Value2"
                    },
                    new Result
                    {
                        Name = "Key3",
                        Value = "Value3"
                    },
                }
            };
            var rec2 = new ResultData
            {
                Id = "2",
                Results = new List<Result>
                {
                    new Result
                    {
                        Name = "Key1",
                        Value = "Value1"
                    },
                    new Result
                    {
                        Name = "Key2",
                        Value = "Value2"
                    },
                    new Result
                    {
                        Name = "Key3",
                        Value = "Value3"
                    },
                }
            };

            var recs = new List<ResultData>
            {
                rec1, rec2
            };

            //var dict = rec1.Results.ToDictionary(kvp => kvp.Name, kvp => kvp.Value);
            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader()
                .UseNestedKeyFormat()
                .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                )
            {
                w.Configuration.Encoding = Encoding.GetEncoding("windows-1254");
                w.Write(recs.Select(r => new
                {
                    r.Id,
                    Results = r.Results.ToDictionary(kvp => kvp.Name, kvp => kvp.Value)
                }));
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        class MyClass
        {
            public long A { get; set; }
            public long B { get; set; }
            public long C { get; set; }
            public string Data { get; set; }
        }
        [Test]
        public static void TestIssue134()
        {
            string expected = @"A,B,C,Data
1,2,3,{ ""key"": ""value""}";
            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter<MyClass>(csv)
                .WithFirstLineHeader()
                //.Configure(c => c.QuoteChar = '`')
                )
            {
                w.Write(new MyClass
                {
                    A = 1,
                    B = 2,
                    C = 3,
                    Data = @"{ ""key"": ""value""}"
                });
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void TestIssue134_1()
        {
            string expected = @"A,B,C,Data
1,2,3,{ """"key"""": """"value""""}";
            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter<MyClass>(csv)
                .WithFirstLineHeader()
                .Configure(c => c.EscapeUsingDoubleQuoteChar = true)
                )
            {
                w.Write(new MyClass
                {
                    A = 1,
                    B = 2,
                    C = 3,
                    Data = @"{ ""key"": ""value""}"
                });
            }

            Console.WriteLine(csv.ToString());
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);

            string expected1 = @"[
  {
    ""A"": ""1"",
    ""B"": ""2"",
    ""C"": ""3"",
    ""Data"": ""{ \""\""key\""\"": \""\""value\""\""}""
  }
]";
            using (var r = ChoCSVReader.LoadText(expected)
                .WithFirstLineHeader())
            {
                var actual1 = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected1, actual1);
            }
        }

        public class Resource
        {
            [ChoCSVRecordField]
            public int Id { get; set; }
            [ChoCSVRecordField]
            public string Name { get; set; }
            [ChoCSVRecordField(Size = 10, FieldValueJustification = ChoFieldValueJustification.Right, FillChar = '^', QuoteField = true)]
            public string Zip { get; set; }
        }

        [Test]
        public static void SizeAndAlignTest()
        {
            string expected = @"Id,Name,Zip
1,Mark,""^^^10010""";
            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter<Resource>(csv)
                .WithFirstLineHeader()
                )
            {
                w.Write(new Resource
                {
                    Id = 1,
                    Name = "Mark",
                    Zip = "10010"
                });
            }

            Console.WriteLine(csv.ToString());

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ExternalSortTest()
        {
            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";
            string expected = @"Id,Name,City
3,Lou,FL
2,Mark,NJ
5,Raj,DC
4,Smith,PA
1,Tom,NY";

            StringBuilder csvOut = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                       .WithFirstLineHeader()
                   )
            {
                using (var w = new ChoCSVWriter(csvOut)
                       .WithFirstLineHeader()
                       )
                {
                    w.Write(r.ExternalSort(new ChoLamdaComparer<dynamic>((e1, e2) => String.Compare(e1.Name, e2.Name))));
                }
            }

            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
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
            string expected = @"Id,Name,Address,Age
20,John Smith,PO BOX 12165,25
21,Bob Kevin,123 NEW LIVERPOOL RD ""APT 12"",30
22,Jack Robert,PO BOX 123,40";

            List<EmpRec> employees = new List<EmpRec>()
{
    new EmpRec() { Id = 20, Name = "John Smith",  Address = "PO BOX 12165", Age = "25" },
    new EmpRec() { Id = 21, Name = "Bob Kevin", Address = "123 NEW LIVERPOOL RD \"APT 12\"", Age = "30" },
    new EmpRec() { Id = 22, Name = "Jack Robert", Address = "PO BOX 123", Age = "40" }
};
            StringBuilder csvOut = new StringBuilder();
            using (var w = new ChoCSVWriter<EmpRec>(csvOut)
                .WithFirstLineHeader()
                .Configure(c => c.EscapeQuoteAndDelimiter = false)
                //.Configure(c => c.QuoteChar = '`')
                .WithField(f => f.Address)
                )
            {
                w.Write(employees);
            }

            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void QuoteValueTest_1()
        {
            string expected = @"Id,Name,Address,Age
20,John Smith,PO BOX 12165,25
21,Bob Kevin,""123 NEW LIVERPOOL RD, """"APT 12"""""",30
22,Jack Robert,PO BOX 123,40";

            List<EmpRec> employees = new List<EmpRec>()
{
    new EmpRec() { Id = 20, Name = "John Smith",  Address = "PO BOX 12165", Age = "25" },
    new EmpRec() { Id = 21, Name = "Bob Kevin", Address = "123 NEW LIVERPOOL RD, \"APT 12\"", Age = "30" },
    new EmpRec() { Id = 22, Name = "Jack Robert", Address = "PO BOX 123", Age = "40" }
};
            StringBuilder csvOut = new StringBuilder();
            using (var w = new ChoCSVWriter<EmpRec>(csvOut)
                .WithFirstLineHeader()
                .Configure(c => c.EscapeQuoteAndDelimiter = false)
                //.Configure(c => c.QuoteChar = '`')
                .WithField(f => f.Address)
                )
            {
                w.Write(employees);
            }

            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);

            string expected1 = @"[
  {
    ""Id"": ""20"",
    ""Name"": ""John Smith"",
    ""Address"": ""PO BOX 12165"",
    ""Age"": ""25""
  },
  {
    ""Id"": ""21"",
    ""Name"": ""Bob Kevin"",
    ""Address"": ""123 NEW LIVERPOOL RD, \""APT 12\"""",
    ""Age"": ""30""
  },
  {
    ""Id"": ""22"",
    ""Name"": ""Jack Robert"",
    ""Address"": ""PO BOX 123"",
    ""Age"": ""40""
  }
]";
            using (var r = ChoCSVReader.LoadText(expected)
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                )
            {
                var actual1 = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected1, actual1);
            }
        }
        [Test]
        public static void FindCSVDiff()
        {
            string csv1 = @"id,name
1,Tom
2,Mark
3,Angie";

            string csv2 = @"id,name
1,Tom
2,Mark
4,Lu";
            var input1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader().ToArray();
            var input2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader().ToArray();

            string expected = @"id,name
3,Angie
4,Lu";

            StringBuilder csvOut = new StringBuilder();
            using (var output = new ChoCSVWriter(csvOut).WithFirstLineHeader())
            {
                output.Write(input1.OfType<ChoDynamicObject>().Except(input2.OfType<ChoDynamicObject>(), ChoDynamicObjectEqualityComparer.Default));
                output.Write(input2.OfType<ChoDynamicObject>().Except(input1.OfType<ChoDynamicObject>(), ChoDynamicObjectEqualityComparer.Default));
            }

            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void CSVDiffWithStatus_1()
        {
            string csv1 = @"ID,name
1,Danny
2,Fred
3,Sam";

            string csv2 = @"ID,name
1,Danny
3,Pamela
4,Fernando";
            string expected = @"ID,name,Status
1,Danny,NOCHANGE
2,Fred,DELETED
3,Pamela,CHANGED
4,Fernando,NEW";

            var r1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader().ToArray();
            var r2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader().ToArray();

            StringBuilder csvOut = new StringBuilder();
            using (var w = new ChoCSVWriter(csvOut).WithFirstLineHeader())
            {
                var newItems = r2.OfType<ChoDynamicObject>().Except(r1.OfType<ChoDynamicObject>(), new ChoDynamicObjectEqualityComparer(new string[] { "ID" }))
                    .Select(r =>
                    {
                        var dict = r.AsDictionary();
                        dict["Status"] = "NEW";
                        return new ChoDynamicObject(dict);
                    }).ToArray();

                var deletedItems = r1.OfType<ChoDynamicObject>().Except(r2.OfType<ChoDynamicObject>(), new ChoDynamicObjectEqualityComparer(new string[] { "ID" }))
                    .Select(r =>
                    {
                        var dict = r.AsDictionary();
                        dict["Status"] = "DELETED";
                        return new ChoDynamicObject(dict);
                    }).ToArray();

                var changedItems = r2.OfType<ChoDynamicObject>().Except(r1.OfType<ChoDynamicObject>(), ChoDynamicObjectEqualityComparer.Default)
                    .Except(newItems.OfType<ChoDynamicObject>(), new ChoDynamicObjectEqualityComparer(new string[] { "ID" }))
                    .Select(r =>
                    {
                        var dict = r.AsDictionary();
                        dict["Status"] = "CHANGED";
                        return new ChoDynamicObject(dict);
                    }).ToArray();

                var noChangeItems = r1.OfType<ChoDynamicObject>().Intersect(r2.OfType<ChoDynamicObject>(), ChoDynamicObjectEqualityComparer.Default)
                    .Select(r =>
                    {
                        var dict = r.AsDictionary();
                        dict["Status"] = "NOCHANGE";
                        return new ChoDynamicObject(dict);
                    }).ToArray();

                var finalResult = Enumerable.Concat(newItems, deletedItems).Concat(changedItems).Concat(noChangeItems).OfType<dynamic>().OrderBy(r => r.ID);
                w.Write(finalResult);
            }

            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void CSVDiffWithStatus_2()
        {
            string csv1 = @"ID,name
1,Danny
2,Fred
3,Sam";

            string csv2 = @"ID,name
1,Danny
3,Pamela
4,Fernando";
            string expected = @"ID,name,Status
1,Danny,NOCHANGE
2,Fred,DELETED
3,Sam,CHANGED
4,Fernando,NEW";

            var r1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader().WithMaxScanRows(1).GetEnumerator();
            var r2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader().WithMaxScanRows(1).GetEnumerator();

            StringBuilder csvOut = new StringBuilder();
            using (var w = new ChoCSVWriter(csvOut).WithFirstLineHeader())
            {
                var b1 = r1.MoveNext();
                var b2 = r2.MoveNext();
                dynamic rec = null;

                while (true)
                {
                    if (!b1 && !b2)
                        break;
                    else if (b1 && b2)
                    {
                        var rec1 = r1.Current;
                        var rec2 = r2.Current;

                        if (rec1.ID == rec2.ID)
                        {
                            rec = rec1;
                            rec.Status = ChoDynamicObjectEqualityComparer.Default.Equals(rec1, rec2) ? "NOCHANGE" : "CHANGED";
                            b1 = r1.MoveNext();
                            b2 = r2.MoveNext();
                        }
                        else if (rec1.ID < rec2.ID)
                        {
                            rec = rec1;
                            rec.Status = "DELETED";
                            b1 = r1.MoveNext();
                        }
                        else
                        {
                            rec = rec2;
                            rec.Status = "NEW";
                            b2 = r2.MoveNext();
                        }
                    }
                    else if (b1)
                    {
                        rec = r1.Current;
                        rec.Status = "DELETED";
                        b1 = r1.MoveNext();
                    }
                    else if (b2)
                    {
                        rec = r2.Current;
                        rec.Status = "NEW";
                        b2 = r2.MoveNext();
                    }
                    else
                        break;

                    w.Write(rec);
                }
            }

            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        public class User
        {
            [Key]
            public int ID { get; set; }
            public string Name { get; set; }
        }

//        [Test]
//        public static void UpsertToDbFrom2CSVs()
//        {
//            string csv1 = @"ID,name
//1,Danny
//2,Fred
//3,Sam";

//            string csv2 = @"ID,name
//1,Danny
//3,Pamela
//4,Fernando";
//            string expected = @"ID,name,Status
//1,Danny,Unchanged
//2,Fred,Deleted
//3,Pamela,Changed
//4,Fernando,New";

//            //ChoTypeComparerCache.Instance.ScanAndLoad();

//            var r1 = ChoCSVReader<User>.LoadText(csv1).WithFirstLineHeader();
//            var r2 = ChoCSVReader<User>.LoadText(csv2).WithFirstLineHeader();

//            StringBuilder csvOut = new StringBuilder();
//            using (var w = new ChoCSVWriter(csvOut).WithFirstLineHeader())
//            {
//                foreach (var t in r1.Compare(r2, "ID", "name"))
//                {
//                    dynamic v1 = t.MasterRecord as dynamic;
//                    dynamic v2 = t.DetailRecord as dynamic;
//                    if (t.Status == CompareStatus.Unchanged || t.Status == CompareStatus.Deleted)
//                    {
//                        v1.Status = t.Status.ToString();
//                        w.Write(v1);
//                    }
//                    else
//                    {
//                        v2.Status = t.Status.ToString();
//                        w.Write(v2);
//                    }
//                }
//            }

//            Console.WriteLine(csvOut.ToString());

//            var actual = csvOut.ToString();
//            Assert.AreEqual(expected, actual);
//        }
        [Test]
        public static void CSVDiffWithStatus()
        {
            string csv1 = @"ID,name
1,Danny
2,Fred
3,Sam";

            string csv2 = @"ID,name
1,Danny
3,Pamela
4,Fernando";
            string expected = @"ID,name,Status
1,Danny,Unchanged
2,Fred,Deleted
3,Pamela,Changed
4,Fernando,New";

            //ChoTypeComparerCache.Instance.ScanAndLoad();

            var r1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader().WithMaxScanRows(1).OfType<ChoDynamicObject>();
            var r2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader().WithMaxScanRows(1).OfType<ChoDynamicObject>();

            StringBuilder csvOut = new StringBuilder();
            using (var w = new ChoCSVWriter(csvOut).WithFirstLineHeader())
            {
                foreach (var t in r1.Compare(r2, "ID", "name"))
                {
                    dynamic v1 = t.MasterRecord as dynamic;
                    dynamic v2 = t.DetailRecord as dynamic;
                    if (t.Status == CompareStatus.Unchanged || t.Status == CompareStatus.Deleted)
                    {
                        v1.Status = t.Status.ToString();
                        w.Write(v1);
                    }
                    else
                    {
                        v2.Status = t.Status.ToString();
                        w.Write(v2);
                    }
                }
            }

            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class ScientificNotationdecimal
        {
            [ChoTypeConverterParams(Parameters = NumberStyles.Number | NumberStyles.AllowExponent)]
            public double a { get; set; }
            [ChoTypeConverterParams(Parameters = NumberStyles.Number | NumberStyles.AllowExponent)]
            public double b { get; set; }
            public long RN { get; set; }
            public DateTime TimeStamp { get; set; }
        }
        [Test]
        public static void ScientificNotationdecimals()
        {
            string expected = @"a,b,RN,TimeStamp
11,21,1,1/1/2023";
            StringBuilder csvOut = new StringBuilder();
            using (var w = new ChoCSVWriter(csvOut)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                .ErrorMode(ChoErrorMode.ThrowAndStop)
                )
            {
                w.WithField("a");
                w.WithField("b");
                w.WithField("RN", () => w.RecordNumber);
                w.WithField("TimeStamp", () => new DateTime(2023, 1, 1));

                w.Write(new { a = 11, b = 21 });
            }
            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void InvalidRecordWriteTest()
        {
            string expected = null;
            StringBuilder csvOut = new StringBuilder();

            Assert.Catch<ChoWriterException>(() =>
            {
                using (var w = new ChoCSVWriter(csvOut)
                    .WithFirstLineHeader()
                    .ThrowAndStopOnMissingField(false)
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                    )
                {
                    w.WithField("a");
                    w.WithField("b");
                    w.WithField("RN", () => w.RecordNumber);
                    w.WithField("TimeStamp", () => DateTime.Now);

                    w.Write(1);

                    w.Write(new { a = 11, b = 21 });
                }
                Console.WriteLine(csvOut.ToString());

                var actual = csvOut.ToString();
                Assert.AreEqual(expected, actual);
            });
        }
        [Test]
        public static void ScientificNotationdecimals1()
        {
            string expected = @"a,b,RN,TimeStamp
10,20,1,1/1/2023";
            StringBuilder csvOut = new StringBuilder();
            using (var w = new ChoCSVWriter<ScientificNotationdecimal>(csvOut)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                .ErrorMode(ChoErrorMode.ThrowAndStop)
                )
            {
                //w.WithField(f => f.a);
                //w.WithField(f => f.b);
                w.WithField(f => f.RN, () => w.RecordNumber);
                w.WithField(f => f.TimeStamp, () => new DateTime(2023, 1, 1));

                w.Write(new ScientificNotationdecimal { a = 10, b = 20 });
            }
            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void ScientificNotationdecimals2()
        {
            string expected = @"a,b,RN,TimeStamp
10,20,1,1/1/2023";
            StringBuilder csvOut = new StringBuilder();
            using (var w = new ChoCSVWriter<ScientificNotationdecimal>(csvOut)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                w.WithField(f => f.a);
                w.WithField(f => f.b);
                w.WithField(f => f.RN, () => w.RecordNumber);
                w.WithField(f => f.TimeStamp, () => new DateTime(2023, 1, 1));

                w.Write(new ScientificNotationdecimal { a = 10, b = 20 });
            }
            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class EmployeeRecX
        {
            [ChoCSVRecordField(1, FieldName = "First")]
            public string First { get; set; }
            [ChoCSVRecordField(2, FieldName = "Last")]
            public string Last { get; set; }
            [ChoCSVRecordField(3, FieldName = "Id")]
            public int Id { get; set; }
            [ChoCSVRecordField(4, FieldName = "Name")]
            public string Name { get; set; }
        }
        [Test]
        public static void CSVWriteUsingPOCO()
        {
            string expected = @"First,Last,Id,Name
,,10,Mark
,,11,Top";
            StringBuilder csvOut = new StringBuilder();
            using (var writer = new ChoCSVWriter<EmployeeRecX>(csvOut)
                .WithFirstLineHeader())
            {
                List<EmployeeRecX> objs = new List<EmployeeRecX>();

                EmployeeRecX rec1 = new EmployeeRecX();
                rec1.Id = 10;
                rec1.Name = "Mark";
                objs.Add(rec1);
                //writer.Write(rec1);

                EmployeeRecX rec2 = new EmployeeRecX();
                rec2.Id = 11;
                rec2.Name = "Top";

                //writer.Write(rec2);
                objs.Add(rec2);

                writer.Write(new[] { rec1, rec2 });
            }

            Console.WriteLine(csvOut.ToString());

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        public class Channel
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("alias")]
            public string Alias { get; set; }

            [JsonProperty("value")]
            public double Value { get; set; }

            [JsonProperty("status")]
            public int Status { get; set; }

            [JsonProperty("valid")]
            public bool Valid { get; set; }

            [JsonProperty("description")]
            public string Description { get; set; }
        }

        public class Datum
        {
            [JsonProperty("datetime")]
            public string Datetime { get; set; }

            [JsonProperty("channels")]
            [Range(0, 3)]
            public Channel[] Channels { get; set; }
        }

        public class StationDetailsCall
        {
            [JsonProperty("stationId")]
            public int StationId { get; set; }

            [JsonProperty("data")]
            [Range(0, 2)]
            public Datum[] Data { get; set; }
        }
        [Test]
        public static void NestedClassSerialization()
        {
            string expected = @"StationId,Data.Datetime_0,Data.Channels_0.Id_0,Data.Channels_0.Name_0,Data.Channels_0.Alias_0,Data.Channels_0.Value_0,Data.Channels_0.Status_0,Data.Channels_0.Valid_0,Data.Channels_0.Description_0,Data.Channels_0.Id_1,Data.Channels_0.Name_1,Data.Channels_0.Alias_1,Data.Channels_0.Value_1,Data.Channels_0.Status_1,Data.Channels_0.Valid_1,Data.Channels_0.Description_1,Data.Channels_0.Id_2,Data.Channels_0.Name_2,Data.Channels_0.Alias_2,Data.Channels_0.Value_2,Data.Channels_0.Status_2,Data.Channels_0.Valid_2,Data.Channels_0.Description_2,Data.Channels_0.Id_3,Data.Channels_0.Name_3,Data.Channels_0.Alias_3,Data.Channels_0.Value_3,Data.Channels_0.Status_3,Data.Channels_0.Valid_3,Data.Channels_0.Description_3,Data.Datetime_1,Data.Channels_1.Id_0,Data.Channels_1.Name_0,Data.Channels_1.Alias_0,Data.Channels_1.Value_0,Data.Channels_1.Status_0,Data.Channels_1.Valid_0,Data.Channels_1.Description_0,Data.Channels_1.Id_1,Data.Channels_1.Name_1,Data.Channels_1.Alias_1,Data.Channels_1.Value_1,Data.Channels_1.Status_1,Data.Channels_1.Valid_1,Data.Channels_1.Description_1,Data.Channels_1.Id_2,Data.Channels_1.Name_2,Data.Channels_1.Alias_2,Data.Channels_1.Value_2,Data.Channels_1.Status_2,Data.Channels_1.Valid_2,Data.Channels_1.Description_2,Data.Channels_1.Id_3,Data.Channels_1.Name_3,Data.Channels_1.Alias_3,Data.Channels_1.Value_3,Data.Channels_1.Status_3,Data.Channels_1.Valid_3,Data.Channels_1.Description_3,Data.Datetime_2,Data.Channels_2.Id_0,Data.Channels_2.Name_0,Data.Channels_2.Alias_0,Data.Channels_2.Value_0,Data.Channels_2.Status_0,Data.Channels_2.Valid_0,Data.Channels_2.Description_0,Data.Channels_2.Id_1,Data.Channels_2.Name_1,Data.Channels_2.Alias_1,Data.Channels_2.Value_1,Data.Channels_2.Status_1,Data.Channels_2.Valid_1,Data.Channels_2.Description_1,Data.Channels_2.Id_2,Data.Channels_2.Name_2,Data.Channels_2.Alias_2,Data.Channels_2.Value_2,Data.Channels_2.Status_2,Data.Channels_2.Valid_2,Data.Channels_2.Description_2,Data.Channels_2.Id_3,Data.Channels_2.Name_3,Data.Channels_2.Alias_3,Data.Channels_2.Value_3,Data.Channels_2.Status_3,Data.Channels_2.Valid_3,Data.Channels_2.Description_3
1,01/02/2023 11:00AM,1,name1,,0,0,False,Description1,2,name2,,0,0,False,,,,,,,False,,,,,,,False,,01/02/2023 11:00AM,1,name1,,0,0,False,Description1,2,name2,,0,0,False,,,,,,,False,,,,,,,False,,,1,name1,,0,0,False,Description1,2,name2,,0,0,False,,,,,,,False,,,,,,,False,";

            StationDetailsCall station = new StationDetailsCall();
            station.StationId = 1;
            station.Data = new Datum[] {
             new Datum() { Channels = new Channel[] { new Channel() { Id = 1, Name = "name1", Description = "Description1" }, new Channel() { Id = 2, Name = "name2" } }, Datetime = "01/02/2023 11:00AM" },
             new Datum() { Channels = new Channel[] { new Channel() { Id = 3, Name = "name3", Description = "Description2" }, new Channel() { Id = 4, Name = "name4" } }, Datetime = "01/02/2023 11:00AM" },
            };

            var csv = new StringBuilder();
            using (var w = new ChoCSVWriter<StationDetailsCall>(csv)
                   .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                   .WithFirstLineHeader()
                   .WithMaxScanRows(1)
                  //.UseNestedKeyFormat(true)
                  //.ThrowAndStopOnMissingField(false)
                  )
            {
                w.Write(station);
            }

            csv.Print();
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class ProductCsvModel
        {
            public decimal DirectCosts { get; set; }
            [Range(0, 2)]
            public List<dynamic> Attributes { get; set; }
        }
        [Test]
        public static void DynamicSubMemberstoCSV()
        {
            string expected = @"DirectCosts,Brand,Season,Custom
1.1,Test1,Test2,Test4";
            var records = new List<ProductCsvModel>
                {
                    new ProductCsvModel
                    {
                        DirectCosts = 1.1M,
                        Attributes = new List<dynamic>
                        {
                            new { Brand = "Test1" },
                            new { Season = "Test2" },
                            new { Brand = "Test3" },
                            new { Custom = "Test4" }
                        }
                    }
                };

            ChoETLSettings.ValueNamePrefix = String.Empty;
            var csv = new StringBuilder();
            using (var w = new ChoCSVWriter(csv)
                   .WithFirstLineHeader().UseNestedKeyFormat().ErrorMode(ChoErrorMode.IgnoreAndContinue)
                   .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                  )
            {
                w.Write(records.Select(r => new { r.DirectCosts, Attributes = r.Attributes.ZipToDictionary() }));
            }
            csv.Print();
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Issue186()
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;

            string expected = @"sep=,
Id,FirstName,LastName,HeightFeet,HeightInches,Position,WeightInPounds,Team.Id,Team.Abbreviation,Team.City,Team.Conference,Team.Division,Team.FullName,Team.Name
1,,,,,,,2,,,,,,abc";
            List<PlayerModel> players = new List<PlayerModel>();
            players.Add(new PlayerModel
            {
                Id = 1,
                Team = new TeamModel
                {
                    Id = 2,
                    Name = "abc"
                }
            });

            var csv = new StringBuilder();
            using (var parser = new ChoCSVWriter<PlayerModel>(csv))
            {
                parser.Write(players);
            }
            csv.Print();
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [ChoCSVFileHeader]
        [ChoCSVRecordObject(HasExcelSeparator = true)]
        public class PlayerModel
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("first_name")]
            public string FirstName { get; set; }

            [JsonProperty("last_name")]
            public string LastName { get; set; }

            [JsonProperty("height_feet")]
            public int? HeightFeet { get; set; }

            [JsonProperty("height_inches")]
            public int? HeightInches { get; set; }

            [JsonProperty("position")]
            public string Position { get; set; }

            [JsonProperty("weight_pounds")]
            public int? WeightInPounds { get; set; }

            [JsonProperty("team")]
            //[ChoCSVRecordField(FieldName = "MyField")]
            public TeamModel Team { get; set; }
        }

        [ChoCSVFileHeader]
        [ChoCSVRecordObject(HasExcelSeparator = true)]
        public class TeamModel
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("abbreviation")]
            public string Abbreviation { get; set; }

            [JsonProperty("city")]
            public string City { get; set; }

            [JsonProperty("conference")]
            public string Conference { get; set; }

            [JsonProperty("division")]
            public string Division { get; set; }

            [JsonProperty("full_name")]
            public string FullName { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }

        public class SCImportCompany
        {
            [ChoCSVRecordField(FieldName = "sf__Id")]
            public string SF_Id { get; set; }

            [ChoCSVRecordField(FieldName = "sf__Created__Error", AltFieldNames = "sf__Error, sf__Created")]
            public string SF_Created_Error { get; set; }

            [ChoCSVRecordField(FieldName = "BillingCity")]
            public string City { get; set; }
        }
        [Test]
        public static void Issue265()
        {
            string csv1 = @"""sf__Id""|""sf__Created""|""BillingCity""
""100""|""Created""|""New York""";

            using (var r = ChoCSVReader<SCImportCompany>.LoadText(csv1)
                .WithFirstLineHeader()
                .WithDelimiter("|")
                .MayHaveQuotedFields()
                //.ThrowAndStopOnMissingField(false)
                )
                r.Print();

            string csv2 = @"""sf__Id""|""sf__Error""|""BillingCity""
""200""|""Error""|""Kansas""
";

            using (var r = ChoCSVReader<SCImportCompany>.LoadText(csv2)
                .WithFirstLineHeader()
                .WithDelimiter("|")
                .MayHaveQuotedFields()
                //.ThrowAndStopOnMissingField(false)
                )
                r.Print();

            var rec = new SCImportCompany
            {
                SF_Id = "100",
                SF_Created_Error = "Created / Error",
                City = "New York"
            };
            var rec1 = new SCImportCompany
            {
                SF_Id = "100",
                SF_Created_Error = "Created / Error",
            };

            string expected = @"""sf__Id""|""sf__Created__Error""|""BillingCity""
""100""|""Created / Error""|""New York""
""100""|""Created / Error""|";
            StringBuilder csv = new StringBuilder();
            using (var w = new ChoCSVWriter<SCImportCompany>(csv)
                .WithFirstLineHeader()
                .WithDelimiter("|")
                //.QuoteAllFields()
                .ConfigureHeader(h => h.QuoteAllHeaders = true)
                .Setup(s => s.BeforeRecordFieldWrite += (o, e) =>
                {
                    if (e.Source != null)
                        e.Source = $"\"{e.Source}\"";
                })
                )
            {
                w.Write(rec);
                w.Write(rec1);
            }

            csv.Print();
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Issue283()
        {
            string json = @"[
  {
    ""Description"": ""Old cottage"",
    ""Id"": ""a775a0e8-9e22-4157-bfe4-b13a564fb18d"",
    ""Name"": ""Carrowduff"",
    ""Occupancy"": {
      ""ActivityType"": null,
      ""BedroomNumber"": 0,
      ""OccupancyNumber"": 0,
      ""StoreyNumber"": 0
    },
    ""PropertyDetails"": {
      ""Address"": null,
      ""FloorPlan"": null,
      ""FloorPlan1"": null,
      ""Front"": null,
      ""Rear"": null,
      ""SurveyDate"": ""\/Date(1681120543033+0100)\/"",
      ""Surveyor"": null,
      ""UPRN"": null
    },
    ""PropertyTypeDetails"": {
      ""Archetype"": ""Very Old Bungalow"",
      ""BungalowType"": 0,
      ""Development"": 0,
      ""SurveyNotes"": null,
      ""Type"": 0
    },
    ""SurveyOptions"": {
      ""AdaptedForDisability"": false,
      ""BuildYear"": 1940,
      ""DisabledAdaptionDetails"": null,
      ""HMO"": true,
      ""LevelAccessToFrontDoor"": false,
      ""SurveyType"": {
        ""CommonBlock"": false,
        ""CommonServices"": false,
        ""External"": false,
        ""Grounds"": false,
        ""Internal"": false,
        ""MRS"": false,
        ""OtherGrounds"": false
      }
    },
    ""Elements"": [
      {
        ""Adequate"": true,
        ""Attribute"": ""Juliette "",
        ""Condition"": 3,
        ""Count"": 0,
        ""ElementName"": ""Front-Door"",
        ""InstallationAgeInYears"": 10,
        ""Notes"": ""big red door"",
        ""Photo"": null,
        ""RemYears"": 12,
        ""Repair"": {
          ""Description"": null,
          ""Measure"": 0,
          ""Photo"": null,
          ""Quantity"": 0,
          ""Reason"": {
            ""Investigation"": 0,
            ""Reason"": 0
          }
        },
        ""RepairNecessary"": false
      },
      {
        ""Adequate"": true,
        ""Attribute"": ""Timber"",
        ""Condition"": 4,
        ""Count"": 0,
        ""ElementName"": ""Rear-Door"",
        ""InstallationAgeInYears"": 25,
        ""Notes"": ""other door"",
        ""Photo"": null,
        ""RemYears"": 8,
        ""Repair"": {
          ""Description"": null,
          ""Measure"": 0,
          ""Photo"": null,
          ""Quantity"": 0,
          ""Reason"": {
            ""Investigation"": 0,
            ""Reason"": 0
          }
        },
        ""RepairNecessary"": false
      },
      {
        ""Adequate"": true,
        ""Attribute"": null,
        ""Condition"": 0,
        ""Count"": 0,
        ""ElementName"": ""Other-External-Door"",
        ""InstallationAgeInYears"": 0,
        ""Notes"": null,
        ""Photo"": null,
        ""RemYears"": 0,
        ""Repair"": {
          ""Description"": null,
          ""Measure"": 0,
          ""Photo"": null,
          ""Quantity"": 0,
          ""Reason"": {
            ""Investigation"": 0,
            ""Reason"": 0
          }
        },
        ""RepairNecessary"": false
      },
      {
        ""Adequate"": true,
        ""Attribute"": null,
        ""Condition"": 0,
        ""Count"": 0,
        ""ElementName"": ""Other-External-Door-2"",
        ""InstallationAgeInYears"": 0,
        ""Notes"": null,
        ""Photo"": null,
        ""RemYears"": 0,
        ""Repair"": {
          ""Description"": null,
          ""Measure"": 0,
          ""Photo"": null,
          ""Quantity"": 0,
          ""Reason"": {
            ""Investigation"": 0,
            ""Reason"": 0
          }
        },
        ""RepairNecessary"": false
      },
      {
        ""Adequate"": true,
        ""Attribute"": ""d\/g powder coated"",
        ""Condition"": 1,
        ""Count"": 0,
        ""ElementName"": ""Windows"",
        ""InstallationAgeInYears"": 10,
        ""Notes"": null,
        ""Photo"": null,
        ""RemYears"": 0,
        ""Repair"": {
          ""Description"": null,
          ""Measure"": 0,
          ""Photo"": null,
          ""Quantity"": 0,
          ""Reason"": {
            ""Investigation"": 0,
            ""Reason"": 0
          }
        },
        ""RepairNecessary"": false
      },
      {
        ""Adequate"": true,
        ""Attribute"": null,
        ""Condition"": 0,
        ""Count"": 0,
        ""ElementName"": ""Secondary-Window"",
        ""InstallationAgeInYears"": 0,
        ""Notes"": null,
        ""Photo"": null,
        ""RemYears"": 0,
        ""Repair"": {
          ""Description"": null,
          ""Measure"": 0,
          ""Photo"": null,
          ""Quantity"": 0,
          ""Reason"": {
            ""Investigation"": 0,
            ""Reason"": 0
          }
        },
        ""RepairNecessary"": false
      },
      {
        ""Adequate"": true,
        ""Attribute"": null,
        ""Condition"": 2,
        ""Count"": 0,
        ""ElementName"": ""Secondary-Roof-Covering"",
        ""InstallationAgeInYears"": 20,
        ""Notes"": null,
        ""Photo"": null,
        ""RemYears"": 0,
        ""Repair"": {
          ""Description"": null,
          ""Measure"": 0,
          ""Photo"": null,
          ""Quantity"": 0,
          ""Reason"": {
            ""Investigation"": 0,
            ""Reason"": 0
          }
        },
        ""RepairNecessary"": false
      }
    ]
  }
]";
            string expected = @"Description,Id,Name,Occupancy/ActivityType,Occupancy/BedroomNumber,Occupancy/OccupancyNumber,Occupancy/StoreyNumber,PropertyDetails/Address,PropertyDetails/FloorPlan,PropertyDetails/FloorPlan1,PropertyDetails/Front,PropertyDetails/Rear,PropertyDetails/SurveyDate,PropertyDetails/Surveyor,PropertyDetails/UPRN,PropertyTypeDetails/Archetype,PropertyTypeDetails/BungalowType,PropertyTypeDetails/Development,PropertyTypeDetails/SurveyNotes,PropertyTypeDetails/Type,SurveyOptions/AdaptedForDisability,SurveyOptions/BuildYear,SurveyOptions/DisabledAdaptionDetails,SurveyOptions/HMO,SurveyOptions/LevelAccessToFrontDoor,SurveyOptions/SurveyType/CommonBlock,SurveyOptions/SurveyType/CommonServices,SurveyOptions/SurveyType/External,SurveyOptions/SurveyType/Grounds,SurveyOptions/SurveyType/Internal,SurveyOptions/SurveyType/MRS,SurveyOptions/SurveyType/OtherGrounds,Elements/Value0/Adequate,Elements/Value0/Attribute,Elements/Value0/Condition,Elements/Value0/Count,Elements/Value0/ElementName,Elements/Value0/InstallationAgeInYears,Elements/Value0/Notes,Elements/Value0/Photo,Elements/Value0/RemYears,Elements/Value0/Repair/Description,Elements/Value0/Repair/Measure,Elements/Value0/Repair/Photo,Elements/Value0/Repair/Quantity,Elements/Value0/Repair/Reason/Investigation,Elements/Value0/Repair/Reason/ReasonValue,Elements/Value0/RepairNecessary,Elements/Value1/Adequate,Elements/Value1/Attribute,Elements/Value1/Condition,Elements/Value1/Count,Elements/Value1/ElementName,Elements/Value1/InstallationAgeInYears,Elements/Value1/Notes,Elements/Value1/Photo,Elements/Value1/RemYears,Elements/Value1/Repair/Description,Elements/Value1/Repair/Measure,Elements/Value1/Repair/Photo,Elements/Value1/Repair/Quantity,Elements/Value1/Repair/Reason/Investigation,Elements/Value1/Repair/Reason/ReasonValue,Elements/Value1/RepairNecessary,Elements/Value2/Adequate,Elements/Value2/Attribute,Elements/Value2/Condition,Elements/Value2/Count,Elements/Value2/ElementName,Elements/Value2/InstallationAgeInYears,Elements/Value2/Notes,Elements/Value2/Photo,Elements/Value2/RemYears,Elements/Value2/Repair/Description,Elements/Value2/Repair/Measure,Elements/Value2/Repair/Photo,Elements/Value2/Repair/Quantity,Elements/Value2/Repair/Reason/Investigation,Elements/Value2/Repair/Reason/ReasonValue,Elements/Value2/RepairNecessary,Elements/Value3/Adequate,Elements/Value3/Attribute,Elements/Value3/Condition,Elements/Value3/Count,Elements/Value3/ElementName,Elements/Value3/InstallationAgeInYears,Elements/Value3/Notes,Elements/Value3/Photo,Elements/Value3/RemYears,Elements/Value3/Repair/Description,Elements/Value3/Repair/Measure,Elements/Value3/Repair/Photo,Elements/Value3/Repair/Quantity,Elements/Value3/Repair/Reason/Investigation,Elements/Value3/Repair/Reason/ReasonValue,Elements/Value3/RepairNecessary,Elements/Value4/Adequate,Elements/Value4/Attribute,Elements/Value4/Condition,Elements/Value4/Count,Elements/Value4/ElementName,Elements/Value4/InstallationAgeInYears,Elements/Value4/Notes,Elements/Value4/Photo,Elements/Value4/RemYears,Elements/Value4/Repair/Description,Elements/Value4/Repair/Measure,Elements/Value4/Repair/Photo,Elements/Value4/Repair/Quantity,Elements/Value4/Repair/Reason/Investigation,Elements/Value4/Repair/Reason/ReasonValue,Elements/Value4/RepairNecessary,Elements/Value5/Adequate,Elements/Value5/Attribute,Elements/Value5/Condition,Elements/Value5/Count,Elements/Value5/ElementName,Elements/Value5/InstallationAgeInYears,Elements/Value5/Notes,Elements/Value5/Photo,Elements/Value5/RemYears,Elements/Value5/Repair/Description,Elements/Value5/Repair/Measure,Elements/Value5/Repair/Photo,Elements/Value5/Repair/Quantity,Elements/Value5/Repair/Reason/Investigation,Elements/Value5/Repair/Reason/ReasonValue,Elements/Value5/RepairNecessary,Elements/Value6/Adequate,Elements/Value6/Attribute,Elements/Value6/Condition,Elements/Value6/Count,Elements/Value6/ElementName,Elements/Value6/InstallationAgeInYears,Elements/Value6/Notes,Elements/Value6/Photo,Elements/Value6/RemYears,Elements/Value6/Repair/Description,Elements/Value6/Repair/Measure,Elements/Value6/Repair/Photo,Elements/Value6/Repair/Quantity,Elements/Value6/Repair/Reason/Investigation,Elements/Value6/Repair/Reason/ReasonValue,Elements/Value6/RepairNecessary
Old cottage,a775a0e8-9e22-4157-bfe4-b13a564fb18d,Carrowduff,,0,0,0,,,,,,4/10/2023,,,Very Old Bungalow,0,0,,0,False,1940,,True,False,False,False,False,False,False,False,False,True,""Juliette "",3,0,Front-Door,10,big red door,,12,,0,,0,0,0,False,True,Timber,4,0,Rear-Door,25,other door,,8,,0,,0,0,0,False,True,,0,0,Other-External-Door,0,,,0,,0,,0,0,0,False,True,,0,0,Other-External-Door-2,0,,,0,,0,,0,0,0,False,True,d/g powder coated,1,0,Windows,10,,,0,,0,,0,0,0,False,True,,0,0,Secondary-Window,0,,,0,,0,,0,0,0,False,True,,2,0,Secondary-Roof-Covering,20,,,0,,0,,0,0,0,False";
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            //ChoETLSettings.ValueNamePrefix = "";

            using (var r = ChoJSONReader<Survey>.LoadText(json)
                .UseJsonSerialization())
            {
                //ChoUtility.Print(r.First().Flatten('/'));
                //return;
                using (var w = new ChoCSVWriter("Issue283.csv")
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(r.Select(r1 => new ChoDynamicObject(r1.FlattenToDictionary('/'))));
                }
                //r.Take(1).Print();
            }

            var actual = File.ReadAllText("Issue283.csv");
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void ReadJsonFile()
        {
            using (var r = new ChoJSONReader<Survey>(@"Issue283.json")
                .UseJsonSerialization())
            {
                r.Select(r1 => new ChoDynamicObject(r1.FlattenToDictionary('/'))).Print();
            }
        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;

            Issue283();
            return;

            //AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) => { Console.WriteLine("FirstChanceException: " + eventArgs.Exception.ToString()); };
            Issue265();
            return;

            DynamicSubMemberstoCSV();

            //TestDictionary();
            return;

            for (int i = 0; i < 100; i++)
                CreateCSVFile();
            return;

            ChoDynamicObjectSettings.DictionaryType = DictionaryType.Ordered;
            JSON2CSVTest2();
            return;

            //DataReaderTest();
            //return;
            //ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;

            //Pivot();
            //return;
            //ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            //DataReaderTest();
            //return;
            //CurrencyDynamicTest();
            //return;
            ComplexObjToCSV();
            return;

            ValidateSchema();
            return;

            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            ReadNWrite();
            return;

            WriteSpecificColumns();
            return;
            //DictionaryTest();
            //return;

            //ListTest();
            //return;
            //int z = 44;
            //Console.WriteLine(String.Format("{0:000}", z));
            //return;

            Sample3();
            return;

            InheritanceTest();
            return;
            NestedObjects();
            return;

            CSVWithQuotes();
            return;

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.FileHeaderConfiguration.HasHeaderRecord = true;
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldName = " Id ", QuoteField = true });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1.1;
            rec1.Name = "Mark";
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Tom";
            objs.Add(rec2);

            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = "~";

            using (var parser = new ChoCSVWriter("Emp.csv", config)
                .WithField("Id", fieldType: typeof(double), valueConverter: (v) => ((double)v).ToString(nfi))
                .WithField("Name")
                .Setup(w => w.FileHeaderWrite += (o, e) =>
                {
                    e.HeaderText = "C1, C2";
                }
                ))
            {
                parser.Write(objs);
            }

            //ToTextTest();
        }

        [Test]
        public static void SaveStringList()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            string expected = @"Value2
01/01/2012
01/01/" + DateTime.Now.Year;

            string actual = null;
            List<string> list = new List<string>();
            list.Add("1/1/2012");
            list.Add("1/1");

            StringBuilder csv = new StringBuilder();
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MM/dd/yyyy";
            using (var w = new ChoCSVWriter(csv).WithFirstLineHeader()
                .WithField("Value", fieldName: "Value2", valueConverter: (v => v.CastTo<DateTime>(new DateTime(2020, 10, 11))))
                )
                w.Write(list);

            actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void QuickDynamicTest()
        {
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "dd MM yyyy";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;

            string expected = @"Id,Name,JoinedDate,IsActive,Salary
10,Mark,02 02 2001,Yes,""$100,000.00""
200,Tom,23 10 1990,No,""$150,000.00""";
            string actual = null;

            List<dynamic> objs = new List<dynamic>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 10;
            rec1.Name = @"Mark";
            rec1.JoinedDate = new DateTime(2001, 2, 2);
            rec1.IsActive = true;
            rec1.Salary = new ChoCurrency(100000);
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 200;
            rec2.Name = "Tom";
            rec2.JoinedDate = new DateTime(1990, 10, 23);
            rec2.IsActive = false;
            rec2.Salary = new ChoCurrency(150000);
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            {
                //using (var writer = new StreamWriter(stream))
                using (var parser = new ChoCSVWriter(stream).WithFirstLineHeader())
                {
                    parser.Write(objs);

                    //writer.Flush();

                }
                stream.Position = 0;
                actual = reader.ReadToEnd();
            }

            Console.WriteLine(actual);
            //Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DateTimeDynamicTest()
        {
            string expected = @"Id,Name,JoinedDate,IsActive,Salary
""10"",""Mark"",""Feb 02, 2001"",""True"",""$100,000.00""
""200"",""Lou"",""Oct 23, 1990"",""False"",""$150,000.00""";
            string actual = null;
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MMM dd, yyyy";

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.JoinedDate = new DateTime(2001, 2, 2);
            rec1.IsActive = true;
            rec1.Salary = new ChoCurrency(100000);
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 200;
            rec2.Name = "Lou";
            rec2.JoinedDate = new DateTime(1990, 10, 23);
            rec2.IsActive = false;
            rec2.Salary = new ChoCurrency(150000);
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter(writer).WithFirstLineHeader().QuoteAllFields())
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void BoolTest()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.BooleanFormat

            string expected = @"Id,Name,JoinedDate,IsActive,Salary,Status
""10"",""Mark"",""02/02/2001"",""Y"",""$100,000.00"",""Permanent""
""200"",""Lou"",""10/23/1990"",""N"",""$150,000.00"",""Contract""";
            string actual = null;

            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YOrN;

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.JoinedDate = new DateTime(2001, 2, 2);
            rec1.IsActive = true;
            rec1.Salary = new ChoCurrency(100000);
            rec1.Status = EmployeeType.Permanent;
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 200;
            rec2.Name = "Lou";
            rec2.JoinedDate = new DateTime(1990, 10, 23);
            rec2.IsActive = false;
            rec2.Salary = new ChoCurrency(150000);
            rec2.Status = EmployeeType.Contract;
            objs.Add(rec2);

            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MM/dd/yyyy";

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter(writer).WithFirstLineHeader().QuoteAllFields())
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }

            Assert.AreEqual(expected, actual);
            // TODO: I am not sure, what should be done with code 
            // ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YOrN;
            // at the beginning of the test. Is the expected output "true" or "Y"
        }

        public enum EmployeeType
        {
            [Description("Full Time Employee")]
            Permanent = 0,
            [Description("Temporary Employee")]
            Temporary = 1,
            [Description("Contract Employee")]
            Contract = 2
        }

        [Test]
        public static void EnumTest()
        {
            string expected = @"Id,Name,JoinedDate,IsActive,Salary,Status
""10"",""Mark"",""2/2/2001"",""True"",""$100,000.00"",""Full Time Employee""
""200"",""Lou"",""10/23/1990"",""False"",""$150,000.00"",""Contract Employee""";
            string actual = null;
            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Description;

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.JoinedDate = new DateTime(2001, 2, 2);
            rec1.IsActive = true;
            rec1.Salary = new ChoCurrency(100000);
            rec1.Status = EmployeeType.Permanent;
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 200;
            rec2.Name = "Lou";
            rec2.JoinedDate = new DateTime(1990, 10, 23);
            rec2.IsActive = false;
            rec2.Salary = new ChoCurrency(150000);
            rec2.Status = EmployeeType.Contract;
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter(writer).WithFirstLineHeader().QuoteAllFields())
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }

        public class EmployeeRecWithCurrency
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public ChoCurrency Salary { get; set; }
        }

        [Test]
        public static void CurrencyPOCOTest()
        {
            string expected = @"Id,Name,Salary
""10"",""Mark"",""$100,000.00""
""200"",""Lou"",""$150,000.00""";
            string actual = null;
            List<EmployeeRecWithCurrency> objs = new List<EmployeeRecWithCurrency>();
            EmployeeRecWithCurrency rec1 = new EmployeeRecWithCurrency();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.Salary = new ChoCurrency(100000);
            objs.Add(rec1);

            EmployeeRecWithCurrency rec2 = new EmployeeRecWithCurrency();
            rec2.Id = 200;
            rec2.Name = "Lou";
            rec2.Salary = new ChoCurrency(150000);
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter<EmployeeRecWithCurrency>(writer).WithFirstLineHeader().QuoteAllFields())
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void CurrencyDynamicTest()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            // TODO: Also check ChoTypeConverterFormatSpec.Instance.BooleanFormat I'm not sure it True and False are the correct expected values

            string expected = @"Id,Name,JoinedDate,IsActive,Salary
""10"",""Mark"",""2/2/2001"",""True"",""$100,000.000""
""200"",""Lou"",""10/23/1990"",""False"",""$150,000.000""";
            string actual = null;
            ChoTypeConverterFormatSpec.Instance.CurrencyFormat = "C3";

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.JoinedDate = new DateTime(2001, 2, 2);
            rec1.IsActive = true;
            rec1.Salary = new ChoCurrency(100000);
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 200;
            rec2.Name = "Lou";
            rec2.JoinedDate = new DateTime(1990, 10, 23);
            rec2.IsActive = false;
            rec2.Salary = new ChoCurrency(150000);
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            {
                using (var parser = new ChoCSVWriter(writer).WithFirstLineHeader().QuoteAllFields())
                {
                    parser.Write(objs);

                    writer.Flush();
                    stream.Position = 0;

                    actual = reader.ReadToEnd();
                }
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void FormatSpecDynamicTest()
        {
            string expected = @"Id,Name,JoinedDate,IsActive,Salary
10,Mark,2/2/2001,Y,""$100,000.00""
200,Lou,10/23/1990,N,""$150,000.00""";
            string actual = null;
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "d";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YOrN;
            ChoTypeConverterFormatSpec.Instance.CurrencyFormat = "C2";

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.JoinedDate = new DateTime(2001, 2, 2);
            rec1.IsActive = true;
            rec1.Salary = new ChoCurrency(100000);
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 200;
            rec2.Name = "Lou";
            rec2.JoinedDate = new DateTime(1990, 10, 23);
            rec2.IsActive = false;
            rec2.Salary = new ChoCurrency(150000);
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter(writer).WithFirstLineHeader())
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void FormatSpecTest()
        {
            string expected = @"Shape,Id,Name,JoinedDate,Salary,IsActive,Status
Circle,10,Mark,2/2/2001,$0.00,Y,
Circle,200,Lou,10/23/1990,$0.00,N,";

            string actual = null;
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "d";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YOrN;
            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;

            List<EmployeeRec> objs = new List<EmployeeRec>();
            EmployeeRec rec1 = new EmployeeRec();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.JoinedDate = new DateTime(2001, 2, 2);
            rec1.IsActive = true;
            objs.Add(rec1);

            EmployeeRec rec2 = new EmployeeRec();
            rec2.Id = 200;
            rec2.Name = "Lou";
            rec2.JoinedDate = new DateTime(1990, 10, 23);
            rec2.IsActive = false;
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter<EmployeeRec>(writer))
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void WriteDataTableTest()
        {
            return;
            string expected = @"Id,Name
1,Lou
50,Jason
200,Mike";
            string actual = null;

            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            //ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;
            //            string connString = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            string connString = @"Data Source=(localdb)\MSSQLLocalDb;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;AttachDBFileName=" + Environment.CurrentDirectory + @"\WriteData.mdf";

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.FileHeaderConfiguration.HasHeaderRecord = true;

            SqlConnection conn = new SqlConnection(connString);
            conn.Open();
            SqlCommand cmd = new SqlCommand("SELECT * FROM Members", conn);
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter(writer, config))
            {
                parser.Write(dt);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void WriteDataReaderTest()
        {
            return;
            string expected = @"Id,Name
1,Lou
50,Jason
200,Mike";
            string actual = null;

            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            //ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;
            //            string connString = @"Data Source=(localdb)\MSSQLLocalDb;Initial Catalog=TestDb;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            string connString = @"Data Source=(localdb)\MSSQLLocalDb;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;AttachDBFileName=" + Environment.CurrentDirectory + @"\WriteData.mdf";

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.FileHeaderConfiguration.HasHeaderRecord = true;

            SqlConnection conn = new SqlConnection(connString);
            conn.Open();
            SqlCommand cmd = new SqlCommand("SELECT * FROM Members", conn);
            IDataReader dr = cmd.ExecuteReader();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter(writer, config))
            {
                parser.Write(dr);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ToTextTest()
        {
            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;

            string expectedArrayAndList = @"Shape,Id,Name,JoinedDate,Salary,IsActive,Status
Circle,10,Mark,1/1/0001 12:00:00 AM,$0.00,No,
Circle,200,Lou,1/1/0001 12:00:00 AM,$0.00,No,";

            string actualArray = null;
            string actualList = null;
            string expectedOnce = @"Shape,Id,Name,JoinedDate,Salary,IsActive,Status
Circle,10,Mark,1/1/0001 12:00:00 AM,$0.00,No,";
            string actualOnce = null;

            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            //ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;

            List<EmployeeRec> objs = new List<EmployeeRec>();
            EmployeeRec rec1 = new EmployeeRec();
            rec1.Id = 10;
            rec1.Name = "Mark";
            objs.Add(rec1);

            EmployeeRec rec2 = new EmployeeRec();
            rec2.Id = 200;
            rec2.Name = "Lou";
            objs.Add(rec2);

            //config.HasExcelSeparator = false;
            actualArray = ChoCSVWriter.ToTextAll(objs.ToArray());

            actualList = ChoCSVWriter.ToTextAll(objs);


            actualOnce = ChoCSVWriter.ToText(objs[0]);

            Assert.Multiple(() =>
            {
                Assert.Throws<ChoParserException>(() => ChoCSVWriter.ToText(objs));
                Assert.AreEqual(expectedArrayAndList, actualArray);
                Assert.AreEqual(expectedArrayAndList, actualList);
                Assert.AreEqual(expectedOnce, actualOnce);
            });

            // TODO: 2 subsequent calls of ChoCSVWriter.ToText(objs[0]) delivers different results. Check required
        }

        [Test]
        public static void CodeFirstWithDeclarativeApproachWriteRecords()
        {
            string expected = @"Shape,Id,Name,JoinedDate,Salary,IsActive,Status
Circle,10,Mark,1/1/0001 12:00:00 AM,$0.00,False,
Circle,200,Lou,1/1/0001 12:00:00 AM,$0.00,False,";

            string actual = null;
            List<string> expectedPropertyNames = new List<string> { "Shape", "Id", "Name", "JoinedDate", "Salary", "IsActive", "Status", "Shape", "Id", "Name", "JoinedDate", "Salary", "IsActive", "Status" };
            List<string> actualPropertyNames = new List<string>();
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";
            //ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            //ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;

            List<EmployeeRec> objs = new List<EmployeeRec>();
            EmployeeRec rec1 = new EmployeeRec();
            rec1.Id = 10;
            rec1.Name = "Mark";
            objs.Add(rec1);

            EmployeeRec rec2 = new EmployeeRec();
            rec2.Id = 200;
            rec2.Name = "Lou";
            objs.Add(rec2);

            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;

            StringBuilder sb = new StringBuilder();
            using (var w = new ChoCSVWriter<EmployeeRec>(sb)
                .WithFirstLineHeader()
            )
            {
                w.Write(objs);
            }
            actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void CodeFirstWithDeclarativeApproachWriteRecordsToFile()
        {
            string expected = @"Shape,Id,Name,JoinedDate,Salary,IsActive,Status
Circle,10,Mark,1/1/0001,$0.00,True,
Circle,200,Lou,1/1/0001,$0.00,False,";

            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            // TODO: Check if export for IsActive should be false true or 0 1 on default value ChoTypeConverterFormatSpec.Instance.BooleanFormat Any

            List<EmployeeRec> objs = new List<EmployeeRec>();
            EmployeeRec rec1 = new EmployeeRec();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.IsActive = true;
            objs.Add(rec1);

            EmployeeRec rec2 = new EmployeeRec();
            rec2.Id = 200;
            rec2.Name = "Lou";
            rec2.IsActive = false;
            objs.Add(rec2);

            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;

            using (var tx = File.OpenWrite(FileNameCodeFirstWithDeclarativeApproachWriteRecordsToFileTestCSV))
            {
                using (var parser = new ChoCSVWriter<EmployeeRec>(tx))
                {
                    parser.Write(objs);
                }
            }

            string actual = File.ReadAllText(FileNameCodeFirstWithDeclarativeApproachWriteRecordsToFileTestCSV);
            Assert.AreEqual(expected, actual);

            //FileAssert.AreEqual(FileNameCodeFirstWithDeclarativeApproachWriteRecordsToFileExpectedCSV, FileNameCodeFirstWithDeclarativeApproachWriteRecordsToFileTestCSV);
        }

        [Test]
        public static void ConfigFirstApproachWriteDynamicRecordsToFile()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.FileHeaderConfiguration.HasHeaderRecord = true;
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { Validators = new ValidationAttribute[] { new System.ComponentModel.DataAnnotations.RangeAttribute(3, 100) } });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.ObjectValidationMode = ChoObjectValidationMode.Off;
            config.Encoding = new UTF8Encoding(false);

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Tom";
            objs.Add(rec2);

            using (var parser = new ChoCSVWriter(FileNameConfigFirstApproachWriteDynamicRecordsToFileTestCSV, config))
            {
                parser.Write(objs);
            }

            FileAssert.AreEqual(FileNameConfigFirstApproachWriteDynamicRecordsToFileExpectedCSV, FileNameConfigFirstApproachWriteDynamicRecordsToFileTestCSV);
        }

        [Test]
        public static void ConfigFirstApproachWriteRecordsToFile()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));

            List<EmployeeRecSimple> objs = new List<EmployeeRecSimple>();
            EmployeeRecSimple rec1 = new EmployeeRecSimple();
            rec1.Id = 1;
            rec1.Name = "Mark";
            objs.Add(rec1);

            EmployeeRecSimple rec2 = new EmployeeRecSimple();
            rec2.Id = 2;
            rec2.Name = "Jason";
            objs.Add(rec2);

            using (var parser = new ChoCSVWriter<EmployeeRecSimple>(FileNameConfigFirstApproachWriteRecordsToFileTestCSV, config))
            {
                parser.Write(objs);
            }

            FileAssert.AreEqual(FileNameConfigFirstApproachWriteRecordsToFileExpectedCSV, FileNameConfigFirstApproachWriteRecordsToFileTestCSV);
        }

        public static string FileNameDataReaderTestTestCSV => "DataReaderTestTest.csv";
        public static string FileNameDataReaderTestExpectedCSV => "DataReaderTestExpected.csv";
        public static string FileNameCodeFirstApproachWriteRecordsToFileTestCSV => "CodeFirstApproachWriteRecordsToFileTest.csv";
        public static string FileNameCodeFirstApproachWriteRecordsToFileExpectedCSV => "CodeFirstApproachWriteRecordsToFileExpected.csv";
        public static string FileNameCodeFirstWithDeclarativeApproachWriteRecordsToFileTestCSV => "CodeFirstWithDeclarativeApproachWriteRecordsToFileTest.csv";
        public static string FileNameCodeFirstWithDeclarativeApproachWriteRecordsToFileExpectedCSV => "CodeFirstWithDeclarativeApproachWriteRecordsToFileExpected.csv";
        public static string FileNameConfigFirstApproachWriteDynamicRecordsToFileTestCSV => "ConfigFirstApproachWriteDynamicRecordsToFileTest.csv";
        public static string FileNameConfigFirstApproachWriteDynamicRecordsToFileExpectedCSV => "ConfigFirstApproachWriteDynamicRecordsToFileExpected.csv";
        public static string FileNameConfigFirstApproachWriteRecordsToFileTestCSV => "ConfigFirstApproachWriteRecordsToFileTest.csv";
        public static string FileNameConfigFirstApproachWriteRecordsToFileExpectedCSV => "ConfigFirstApproachWriteRecordsToFileExpected.csv";
        public static string FileNameDataFirstApproachWriteListOfRecordsToFileTestCSV => "DataFirstApproachWriteListOfRecordsToFileTest.csv";
        public static string FileNameDataFirstApproachWriteListOfRecordsToFileExpectedCSV => "DataFirstApproachWriteListOfRecordsToFileExpected.csv";
        public static string FileNameDataFirstApproachWriteSingleRecordToFileTestCSV => "DataFirstApproachWriteSingleRecordToFileTest.csv";
        public static string FileNameDataFirstApproachWriteSingleRecordToFileExpectedCSV => "DataFirstApproachWriteSingleRecordToFileExpected.csv";
        public static string FileNameIntArrayTestTestCSV => "IntArrayTestTest.csv";
        public static string FileNameIntArrayTestExpectedCSV => "IntArrayTestExpected.csv";
        public static string FileNameListPOCOTestTestCSV => "ListPOCOTestTest.csv";
        public static string FileNameListPOCOTestExpectedCSV => "ListPOCOTestExpected.csv";
        public static string FileNameSample3CSV => "Sample3.csv";
        public static string FileNameXTestCSV => "XTest.csv";
        public static string FileNameXExpectedCSV => "XExpected.csv";
        public static string FileNameSaveStringListTestCSV => "SaveStringListTest.csv";
        //

        [Test]
        public static void CodeFirstApproachWriteRecordsToFile()
        {
            List<EmployeeRecSimple> objs = new List<EmployeeRecSimple>();
            EmployeeRecSimple rec1 = new EmployeeRecSimple();
            rec1.Id = 1;
            rec1.Name = "Mark";
            objs.Add(rec1);

            EmployeeRecSimple rec2 = new EmployeeRecSimple();
            rec2.Id = 2;
            rec2.Name = "Jason";
            objs.Add(rec2);

            using (var parser = new ChoCSVWriter<EmployeeRecSimple>(FileNameCodeFirstApproachWriteRecordsToFileTestCSV))
            {
                parser.Write(objs);
            }

            FileAssert.AreEqual(FileNameCodeFirstApproachWriteRecordsToFileExpectedCSV, FileNameCodeFirstApproachWriteRecordsToFileTestCSV);
        }

        [Test]
        public static void DataFirstApproachWriteSingleRecordToFile()
        {
            using (var parser = new ChoCSVWriter(FileNameDataFirstApproachWriteSingleRecordToFileTestCSV))
            {
                dynamic rec1 = new ExpandoObject();
                rec1.Id = 1;
                rec1.Name = "Mark";
                parser.Write(rec1);

                dynamic rec2 = new ExpandoObject();
                rec2.Id = 2;
                rec2.Name = "Jason";
                parser.Write(rec2);
            }
            FileAssert.AreEqual(FileNameDataFirstApproachWriteSingleRecordToFileExpectedCSV, FileNameDataFirstApproachWriteSingleRecordToFileTestCSV);
        }

        [Test]
        public static void DataFirstApproachWriteSingleRecord()
        {
            string expected = @"1,Mark
2,Jason";
            string actual = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter(writer))
            {
                dynamic rec1 = new ExpandoObject();
                rec1.Id = 1;
                rec1.Name = "Mark";

                parser.Write(rec1);

                dynamic rec2 = new ExpandoObject();
                rec2.Id = 2;
                rec2.Name = "Jason";
                parser.Write(rec2);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DataFirstApproachWriteListOfRecordsToFile()
        {
            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Jason";
            objs.Add(rec2);

            using (var parser = new ChoCSVWriter(FileNameDataFirstApproachWriteListOfRecordsToFileTestCSV))
            {
                parser.Write(objs);
            }
            FileAssert.AreEqual(FileNameDataFirstApproachWriteListOfRecordsToFileExpectedCSV, FileNameDataFirstApproachWriteListOfRecordsToFileTestCSV);
        }

        [Test]
        public static void DataFirstApproachWriteListOfRecords()
        {
            string expected = @"1,Mark
2,Jason";
            string actual = null;
            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Jason";
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter(writer))
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }
    }

    public partial class EmployeeRecSimple
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public enum Shape
    {
        [Description("Circle Shape")]
        Circle,
        [Description("Rectangle Shape")]
        Rectangle,
        [Description("Square Shape")]
        Square
    }

    [ChoCSVFileHeader]
    [ChoCSVRecordObject]
    public class EmployeeRec
    {
        public Shape Shape { get; set; }

        //[ChoCSVRecordField(1, FieldName = "NewId")]
        //[Required]
        //[ChoFallbackValue(100)]
        //[Range(100, 10000)]
        public int? Id
        {
            get;
            set;
        }
        //[ChoCSVRecordField(2)]
        //[DefaultValue("XXXX")]
        public string Name
        {
            get;
            set;
        }
        [DefaultValue("1/1/2001")]
        public DateTime JoinedDate { get; set; }
        [DefaultValue("50000")]
        public ChoCurrency Salary { get; set; }
        public bool IsActive { get; set; }
        public char Status { get; set; }
    }



    public class Survey
    {
        public string Description { get; set; }
        public Guid Id { get; set; }
        public string Name { get; set; }
        public Occupancy Occupancy { get; set; }
        public Propertydetails PropertyDetails { get; set; }
        public Propertytypedetails PropertyTypeDetails { get; set; }
        public Surveyoptions SurveyOptions { get; set; }
        public Element[] Elements { get; set; }
    }

    public class Occupancy
    {
        public string ActivityType { get; set; }
        public int BedroomNumber { get; set; }
        public int OccupancyNumber { get; set; }
        public int StoreyNumber { get; set; }
    }

    public class Propertydetails
    {
        public string Address { get; set; }
        public string FloorPlan { get; set; }
        public string FloorPlan1 { get; set; }
        public string Front { get; set; }
        public string Rear { get; set; }
        public DateTime SurveyDate { get; set; }
        public string Surveyor { get; set; }
        public string UPRN { get; set; }
    }

    public class Propertytypedetails
    {
        public string Archetype { get; set; }
        public int BungalowType { get; set; }
        public int Development { get; set; }
        public string SurveyNotes { get; set; }
        public int Type { get; set; }
    }

    public class Surveyoptions
    {
        public bool AdaptedForDisability { get; set; }
        public int BuildYear { get; set; }
        public string DisabledAdaptionDetails { get; set; }
        public bool HMO { get; set; }
        public bool LevelAccessToFrontDoor { get; set; }
        public Surveytype SurveyType { get; set; }
    }

    public class Surveytype
    {
        public bool CommonBlock { get; set; }
        public bool CommonServices { get; set; }
        public bool External { get; set; }
        public bool Grounds { get; set; }
        public bool Internal { get; set; }
        public bool MRS { get; set; }
        public bool OtherGrounds { get; set; }
    }

    public class Element
    {
        public bool Adequate { get; set; }
        public string Attribute { get; set; }
        public int Condition { get; set; }
        public int Count { get; set; }
        public string ElementName { get; set; }
        public int InstallationAgeInYears { get; set; }
        public string Notes { get; set; }
        public object Photo { get; set; }
        public int RemYears { get; set; }
        public Repair Repair { get; set; }
        public bool RepairNecessary { get; set; }
    }

    public class Repair
    {
        public object Description { get; set; }
        public int Measure { get; set; }
        public object Photo { get; set; }
        public int Quantity { get; set; }
        public Reason Reason { get; set; }
    }

    public class Reason
    {
        public int Investigation { get; set; }
        [JsonProperty("Reason")]
        public int ReasonValue { get; set; }
    }

}
