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
        //[Test]
        public static void CSVWithQuotes()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            string expected = @"""date_start"",""date_end"",""current_year""
""" + DateTime.Today.ToString("d",CultureInfo.GetCultureInfo("en-CA")) + @""",""" + DateTime.Today.AddDays(2).ToString("d",CultureInfo.GetCultureInfo("en-CA")) + @""",""" + DateTime.Today.Year.ToString("d",CultureInfo.GetCultureInfo("en-CA")) + @"""";

            StringBuilder msg = new StringBuilder();
            using (var writer = new ChoCSVWriter<CustomType>(msg).WithFirstLineHeader()
                .Configure(c => c.FileHeaderConfiguration.QuoteAllHeaders = true)
                .Configure(c => c.QuoteAllFields = true)
                .Configure(c => c.Culture = new CultureInfo("en-CA"))
                )
            {
                var x1 = new CustomType { DateStart = DateTime.Today, DateEnd = DateTime.Today.AddDays(2), CurrentYear = DateTime.Today.Year };
                writer.Write(x1);
            }

            Assert.AreEqual(expected, msg.ToString());

        }

        //[Test]
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
                .Setup(s => s.RecordFieldWriteError += (o, e) => actualList.Add(e.Exception.ToString()))
                .Configure(c => { c.NestedColumnSeparator = '/'; c.WithFirstLineHeader(); })
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

        //[Test]
        public static void NestedObjects()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat

            string expected = @"ClientName,DealNo,StartDate,EndDate,Volume
ABC,59045599," + DateTime.Today.ToString("d") + "," + DateTime.Today.AddDays(2).ToString("d") + @",100
ABC,59045599," + DateTime.Today.ToString("d") + "," + DateTime.Today.AddDays(2).ToString("d") + ",200";
            string actual = null;
            ChoETLFrxBootstrap.IsSandboxEnvironment = true;
            StringBuilder sb = new StringBuilder();
            TRoot root = new TRoot() { Client = "ABC", Deals = new List<TDeal>() };
            root.Deals.Add(new TDeal
            {
                DealName = "59045599",
                TShape = new List<TInterval>()
            {
                new TInterval { StartDate = DateTime.Today.ToString(), EndDate = DateTime.Today.AddDays(2).ToString(), Volume = "100" },
                new TInterval { StartDate = DateTime.Today.ToString(), EndDate = DateTime.Today.AddDays(2).ToString(), Volume = "200" }
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

        //[Test]
        public static void InheritanceTest()
        {
            Assert.Fail("Test does not return. A worker-thread dies and main-thread stays in Wait at line w.Write(o1).");
            using (var w = new ChoCSVWriter<Employee>("Inheritance.csv").WithFirstLineHeader()
                .MapRecordFields<ManagerMetaData>()
                )
            {
                var o1 = new Manager { Name = "E1", Department = "History", Salary = 100000 };
                var o2 = new Manager { Name = "E2", Department = "Math", Salary = 110000 };
                w.Write(o1);
                w.Write(o2);
            }
            Assert.Fail("Write a useful assertion");
        }
        public class SitePostal
        {
            [Required(ErrorMessage = "State is required")]
            [RegularExpression("^[A-Z][A-Z]$", ErrorMessage = "Incorrect zip code.")]
            public string State { get; set; }
            [Required]
            [RegularExpression("^[0-9][0-9]*$")]
            [ChoIgnoreMember]
            public string Zip { get; set; }
        }
        public class SiteAddress
        {
            [Required]
            [StringLength(10)]
            //[ChoCSVRecordField(3)]
            public string Street { get; set; }
            [Required]
            [RegularExpression("^[a-zA-Z][a-zA-Z ]*$")]
            public string City { get; set; }
            [ChoValidateObject]
            public SitePostal SitePostal { get; set; }
        }
        public class Site
        {
            [Required(ErrorMessage = "SiteID can't be null")]
            //[ChoCSVRecordField(1, FormatText = "000")]
            public int SiteID { get; set; }
            [Required]
            public int House { get; set; }
            //[ChoValidateObject]
            public SiteAddress SiteAddress { get; set; }
            //[ChoCSVRecordField(2)]
            public int Apartment { get; set; }
        }

        //[Test]
        public static void Sample3()
        {
            string expected = @"SiteID,House,Street,City,State,Apartment
44,545395,PORT ROYAL,CORPUS CHRISTI,2
44,608646,TEXAS AVE,ODESSA,
44,487460,EVERHART RD,CORPUS CHRISTI,
44,275543,EDWARD GARY,SAN MARCOS,4
44,136811,MAGNOLIA AVE,SAN ANTONIO,1";
            string actual = null;
            using (var p = new ChoCSVReader<Site>(FileNameSample3CSV)
                //.ClearFields()
                //            .WithField(m => m.SiteID)
                //            .WithField(m => m.SiteAddress.City)
                .WithFirstLineHeader(true)
                .Configure(c => c.ObjectValidationMode = ChoObjectValidationMode.ObjectLevel)
                )
            {
                StringBuilder msg = new StringBuilder();
                using (var w = new ChoCSVWriter<Site>(new StringWriter(msg))
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(p);
                }
                actual = msg.ToString();
            }
            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void ListTest()
        {
            string expected = @"Value
$1.00";
            string actual = null;
            StringBuilder sb = new StringBuilder();

            using (var w = new ChoCSVWriter(new StringWriter(sb))
                .WithFirstLineHeader()
                )
            {
                List<ChoCurrency> l1 = new List<ChoCurrency>();
                l1.Add(new ChoCurrency(1));
                //l1.Add(2);
                //l1.Add(3);

                w.Write(l1);

                //List<string> l = new List<string>();
                //l.Add("2");
                //l.Add("Tom1");
                //l.Add("Mark1");

                //w.Write(l);
            }

            actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }

        //[Test]
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
        //[Test]
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

        //[Test]
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

        //[Test]
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

        //[Test]
        public static void TestListOfInt1()
        {
            string expected = @"Id
1
2
2";
            string actual = null;

            List<int?> l = new List<int?>();
            l.Add(1);
            l.Add(null);
            l.Add(2);
            StringBuilder sb = new StringBuilder();
            using (var w = new ChoCSVWriter(sb)
                .WithFirstLineHeader()
                .WithField("Value", fieldName: "Id")
                )
            {
                w.Write(l);
                w.Write(2);
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
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

        //[Test]
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


        //[Test]
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

        //[Test]
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


        //[Test]
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

        //[Test]
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

        //[Test]
        public static void QuotesIssue1()
        {
            string expected = @"20,John Smith
21,""Jack in """"""""Da Box""";
            string actual = null;

            List<EmployeeRecSimple> objs = new List<EmployeeRecSimple>()
            {
                new EmployeeRecSimple() { Id = 20, Name = "John Smith" },
                new EmployeeRecSimple() { Id = 21, Name = @"Jack in ""Da Box" }
            };
            actual = ChoCSVWriter<EmployeeRecSimple>.ToTextAll(objs);
            Assert.AreEqual(expected, actual);
        }

        //[Test]
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

        //[Test]
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

        //[Test]
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
            [Range(1, 3)]
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

        //[Test]
        public static void ComplexObjToCSV()
        {
            string expected = @"K2,CreId_1,CreName_1,Id,SId,StdName,Sub_1,Sub_2,Sub_3,Teacher.Id,Teacher.Name,K01
B,c1,Math1,100,1,Mark,Physics,,,100,Tom,";
            string actual = null;

            var si = new StudentInfo
            {
                Id = "100",
                Student = new Student { Id = "1", Name = "Mark" },
                Teacher = new Teacher { Id = "100", Name = "Tom" },
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
                //.WithField(c => c.Courses, defaultValue: null)
                .WithField(c => c.Student.Id, fieldName: "SId")
                //.WithField(c => c.Courses.FirstOrDefault().CourseId, fieldName: "CId")
                .WithFirstLineHeader()
                //.Index(c => c.Courses, 1, 1)
                .DictionaryKeys(c => c.Grades, "K01", "K2")
                )
            {
                w.Write(si);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
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
                    csvWriter.Write(dr);
                    sw.Flush();
                }
            }

            FileAssert.AreEqual(FileNameDataReaderTestExpectedCSV, FileNameDataReaderTestTestCSV);
        }

        //[Test]
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
                )
            {
                actual = ChoCSVWriter.ToTextAll(r.Transpose(false));
            }

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void LargeXmlToCSV()
        {
            Assert.Fail(@"Cannot find file C:\Users\nraj39\Downloads\Loan\dblp.xml");

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

        //[Test]
        public static void LargeJSON2CSV()
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            using (var r = new ChoJSONReader(@"rows.json"))
            {
                //var x = ((IDictionary<string, object>)r.FirstOrDefault()).Flatten().ToDictionary(c => c.Key, c => c.Value);
                //Console.WriteLine(x.Dump());
                using (var w = new ChoCSVWriter(@"rows.csv")
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(r.FirstOrDefault());
                }
            }
        }

        //[Test]
        public static void JSON2CSVTest1()
        {
            string expected = @"data_getUsers_0_userProfileDetail_userStatus_name,data_getUsers_0_userProfileDetail_userStatusDate,data_getUsers_0_userProfileDetail_lastAttestationDate,data_getUsers_0_userInformation_Id,data_getUsers_0_userInformation_lastName,data_getUsers_0_userInformation_suffix,data_getUsers_0_userInformation_gender,data_getUsers_0_userInformation_birthDate,data_getUsers_0_userInformation_ssn,data_getUsers_0_userInformation_ethnicity,data_getUsers_0_userInformation_languagesSpoken,data_getUsers_0_userInformation_personalEmail,data_getUsers_0_userInformation_otherNames,data_getUsers_0_userInformation_userType_name,data_getUsers_0_userInformation_primaryuserState,data_getUsers_0_userInformation_otheruserState_0,data_getUsers_0_userInformation_practiceSetting,data_getUsers_0_userInformation_primaryEmail
Expired,4/4/2017 9:48:25 AM,2/1/2019 9:50:42 AM,13610875,************,,FEMALE,1/1/1970 1:01:00 AM,000000000,INVALID_REFERENCE_VALUE,,,,APN,CO,CO,INPATIENT_ONLY,*****@*****.com";
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

            actual = ChoCSVWriter.ToTextAll(ChoJSONReader.LoadText(json,
                new ChoJSONRecordConfiguration()), 
                new ChoCSVRecordConfiguration().Configure(c => c.WithFirstLineHeader()));

            Assert.AreEqual(expected, actual);
        }

        //[Test]
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
                .WithJSONPath("$..getUsers[*]")
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

        //[Test]
        public static void JSON2CSVTest3()
        {
            Assert.Fail(@"Cannot find file C:\Users\nraj39\Downloads\Loan\test1.json");

            using (var r = new ChoJSONReader(@"test1.json")
                 .WithJSONPath("$..data.getUsers[*]"))
            {
                using (var r1 = new ChoJSONReader(@"test1.json")
                    .WithJSONPath("$..errors[*]")
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

                    using (var w = new ChoCSVWriter(@"test1.csv")
                        .WithFirstLineHeader()
                        .Configure(c => c.MaxScanRows = 10)
                        .Configure(c => c.ThrowAndStopOnMissingField = false)
                        )
                    {
                        w.Write(r3);
                    }
                }
            }
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

        static void WriteComplexObjs()
        {
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
                    CallDateTime = DateTime.Today.ToString(),
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
                //.Map(f => f.client.Indice)
                //.Map(f => f.client.Name)
                //.Map(f => f.client.Tel1)
                //.Map(f => f.callClient.CallDateTime)
                //.Map(f => f.callClient.Status)
                //.Map(f => f.pollClient.Question1)
                //.Map(f => f.pollClient.Question2)
                //.Map(f => f.pollClient.Question2)
                //.Map(f => f.pollClient.StatusPoll)
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


        static void ArrayWriteTest()
        {
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
                .WithMaxScanRows(2)
                )
            {
                w.Write(rec);
            }

            Console.WriteLine(csv.ToString());

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

            public string GetFieldName(string declaringMemberName, string memberName, char separator, int index)
            {
                return $"{declaringMemberName}{index}{memberName}";
            }
        }

        public class MyListConverter : IChoValueConverter, IChoHeaderConverter, IChoValueSelector
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
        public static void NestedClass2CSVTest()
        {
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

        static void NestedClassRefTest()
        {
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
                )
            {
                w.Write(f1);
            }

            Console.WriteLine(csv.ToString());
        }

        public class EmpGuid
        {
            public Guid Id { get; set; }
            public string Name { get; set; }

            public EmpGuid()
            {
                Id = Guid.NewGuid();
                Name = Id.ToString().Substring(5);
            }
        }

        static void GuidWriteTest()
        {
            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader())
            {
                w.Write(new EmpGuid());
            }

            Console.WriteLine(csv.ToString());
        }

        static void ValueListTest()
        {
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
                w.Write(list);
            }
            Console.WriteLine(csv.ToString());
        }


        static void JSON2CSV()
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
        }

        public class ClassAttendance 
        {
            //[ChoDictionaryKey("addcol1, addcol2, addcol3")]
            public IDictionary<string, object> AdditionalDetails { get; set; }
            public bool Confirmed { get; set; }
            public string DigitalDelivery { get; set; }
        }

        static void IgnoreDictionaryFieldPrefixTest()
        {
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
                }.ToDynamicObject().ConvertToFlattenObject('/', true);
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
        }

        public static void CreateCSVFile()
        {
            List<dynamic> objs = new List<dynamic>();

            for (int i = 0; i < 100; i++)
            {
                dynamic rec = new ExpandoObject();
                rec.val1 = '1';
                rec.val2 = '2';
                rec.val3 = '3';
                rec.val4 = '4';
                objs.Add(rec);
            }

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration
            {
                Encoding = Encoding.Default
            };

            using (var parser = new ChoCSVWriter(@"t.txt", config).WithFirstLineHeader())
            {
                parser.Write(objs);
            }
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

        static void WriteEmptyColumnWhenNestedObjectIsNull()
        {
            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter<LocationRow>(csv)
                .WithFirstLineHeader()
                .Configure(c => c.UseNestedKeyFormat = false)
                )
            {
                w.Write(new LocationRow
                {
                    RowId = Guid.NewGuid(),
                    Latitude = 10,
                    Longitude = 1
                });
            }

            Console.WriteLine(csv.ToString());
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

        static void ComplexType2CSV()
        {
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
        }

        class MyClass
        {
            public long A { get; set; }
            public long B { get; set; }
            public long C { get; set; }
            public string Data { get; set; }
        }

        static void TestIssue134()
        {
            StringBuilder csv = new StringBuilder();

            using (var w = new ChoCSVWriter<MyClass>(csv)
                .WithFirstLineHeader()
                .Configure(c => c.QuoteChar = '`')
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

        static void SizeAndAlignTest()
        {
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
        }


        public static void ExternalSortTest()
        {
            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";

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
        }

        public class EmpRec
        { 
            public int Id { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string Age { get; set; }
        }

        static void QuoteValueTest()
        {
            List<EmpRec> employees = new List<EmpRec>()
{
    new EmpRec() { Id = 20, Name = "John Smith",  Address = "PO BOX 12165", Age = "25" },
    new EmpRec() { Id = 21, Name = "Bob Kevin", Address = "123 NEW LIVERPOOL RD \"APT 12\"", Age = "30" },
    new EmpRec() { Id = 22, Name = "Jack Robert", Address = "PO BOX 123", Age = "40" }
};
            using (var w = new ChoCSVWriter<EmpRec>(Console.Out)
                .WithFirstLineHeader()
                .Configure(c => c.EscapeQuoteAndDelimiter = false)
                //.Configure(c => c.QuoteChar = '`')
                .WithField(f => f.Address)
                )
            {
                w.Write(employees);
            }
        }

        static void FindCSVDiff()
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

            using (var output = new ChoCSVWriter(Console.Out).WithFirstLineHeader())
            {
                output.Write(input1.OfType<ChoDynamicObject>().Except(input2.OfType<ChoDynamicObject>(), ChoDynamicObjectEqualityComparer.Default));
                output.Write(input2.OfType<ChoDynamicObject>().Except(input1.OfType<ChoDynamicObject>(), ChoDynamicObjectEqualityComparer.Default));
            }
        }

        public static void TimespanIssue()
        {
            string csv = @" A; B; C; D; E; F; G; H
a;b;;2021-05-06;e;11:00;3;9";

            //ChoType.IsTypeSimple = t => t == typeof(TimeSpan) ? (bool?)true : null;

            using (var r = ChoCSVReader<MyClass1>.LoadText(csv)
                .WithFirstLineHeader()
                .WithDelimiter(";")
                //.WithField(f => f.f, valueConverter: o => TimeSpan.Parse(o.ToNString()))
                )
                r.Print();
        }

        public class MyClass1
        {
            public string a { get; set; }

            public string b { get; set; }

            public string c { get; set; }

            public DateTime d { get; set; }

            public string e { get; set; }

            public TimeSpan f { get; set; }

            public string g { get; set; }

            public string h { get; set; }
        }

        static void CSVDiffWithStatus_1()
        {
            string csv1 = @"ID,name
1,Danny
2,Fred
3,Sam";

            string csv2 = @"ID,name
1,Danny
3,Pamela
4,Fernando";

            var r1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader().ToArray();
            var r2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader().ToArray();

            using (var w = new ChoCSVWriter(Console.Out).WithFirstLineHeader())
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

            Console.WriteLine();
        }

        static void CSVDiffWithStatus_2()
        {
            string csv1 = @"ID,name
1,Danny
2,Fred
3,Sam";

            string csv2 = @"ID,name
1,Danny
3,Pamela
4,Fernando";

            var r1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader().WithMaxScanRows(1).GetEnumerator();
            var r2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader().WithMaxScanRows(1).GetEnumerator();

            using (var w = new ChoCSVWriter(Console.Out).WithFirstLineHeader())
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
        }

        static void CSVDiffWithStatus()
        {
            string csv1 = @"ID,name
1,Danny
2,Fred
3,Sam";

            string csv2 = @"ID,name
1,Danny
3,Pamela
4,Fernando";

            //ChoTypeComparerCache.Instance.ScanAndLoad();

            var r1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader().WithMaxScanRows(1).OfType<ChoDynamicObject>();
            var r2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader().WithMaxScanRows(1).OfType<ChoDynamicObject>();

            using (var w = new ChoCSVWriter(Console.Out).WithFirstLineHeader())
            {
                foreach (var t in r1.Compare(r2, "ID", "name" ))
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

        static void ScientificNotationdecimals()
        {
            using (var w = new ChoCSVWriter(Console.Out)
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
        }

        static void ScientificNotationdecimals1()
        {
            using (var w = new ChoCSVWriter<ScientificNotationdecimal>(Console.Out)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                w.WithField(f => f.RN, () => w.RecordNumber);
                w.WithField(f => f.TimeStamp, () => DateTime.Now);

                w.Write(new ScientificNotationdecimal { a = 1, b = 2 });
            }
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
        public static void TestChoETL()
        {
            using (var writer = new ChoCSVWriter<EmployeeRecX>(Console.Out)
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

            "".Print();
        }
        static void Main(string[] args)
        {
            //AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) => { Console.WriteLine("FirstChanceException: " + eventArgs.Exception.ToString()); };
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            TestChoETL();
            //TestDictionary();
            return;

            for (int i = 0; i < 100; i++)
                CreateCSVFile();
            return;

            ChoDynamicObjectSettings.UseOrderedDictionary = false;
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

        //[Test]
        public static void SaveStringList()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            string expected = @"Value2
1/1/2012
1/1/" + DateTime.Now.Year;
            string actual = null;
            List<string> list = new List<string>();
            list.Add("1/1/2012");
            list.Add("1/1");

            using (var w = new ChoCSVWriter(FileNameSaveStringListTestCSV).WithFirstLineHeader()
                .WithField("Value", fieldName: "Value2", valueConverter: (v => v.CastTo<DateTime>(new DateTime(2020,10,11))))
                )
                w.Write(list);

            actual = new StreamReader(FileNameSaveStringListTestCSV).ReadToEnd();
            Assert.AreEqual(expected, actual);
        }

        //[Test]
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

        //[Test]
        public static void DateTimeDynamicTest()
        {
            string expected = @"Id,Name,JoinedDate,IsActive,Salary
""10"",""Mark"",""Feb 02, 2001"",""True"",""$100,000.00""
""200"",""Lou"",""Oct 23, 1990"",""False"",""$150,000.00""";
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

        //[Test]
        public static void BoolTest()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.BooleanFormat

            string expected = @"Id,Name,JoinedDate,IsActive,Salary,Status
""10"",""Mark"",""2/2/2001"",""Y"",""$100,000.00"",""Permanent""
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

        //[Test]
        public static void EnumTest()
        {
            string expected = @"Id,Name,JoinedDate,IsActive,Salary,Status
""10"",""Mark"",""2/2/2001 12:00:00 AM"",""True"",""$100,000.00"",""Permanent""
""200"",""Lou"",""10/23/1990 12:00:00 AM"",""False"",""$150,000.00"",""Contract""";
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

        //[Test]
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

        //[Test]
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

        //[Test]
        public static void FormatSpecDynamicTest()
        {
            string expected = @"Id,Name,JoinedDate,IsActive,Salary
10,Mark,2/2/2001,Y,""$100,000.00""
200,Lou,10/23/1990,N,""$150,000.00""";
            string actual = null;
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "d";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YOrN;

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.JoinedDate = new DateTime(2001, 2, 2);
            rec1.IsActive = true;
            rec1.Salary = 100000;
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 200;
            rec2.Name = "Lou";
            rec2.JoinedDate = new DateTime(1990, 10, 23);
            rec2.IsActive = false;
            rec2.Salary = 150000;
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

        //[Test]
        public static void FormatSpecTest()
        {
            string expected = @"Shape,Id,Name,JoinedDate,Salary,IsActive,Status
Circle,10,Mark,2/2/2001,$0.00,Y," + "\0" + @"
Circle,200,Lou,10/23/1990,$0.00,N," + "\0";
            string actual = null;
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "d";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YOrN;

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

        //[Test]
        public static void WriteDataTableTest()
        {
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

        //[Test]
        public static void WriteDataReaderTest()
        {
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

        //[Test]
        public static void ToTextTest()
        {
            
            string expectedArrayAndList = @"Shape,Id,Name,JoinedDate,Salary,IsActive,Status
Circle,10,Mark,1/1/0001 12:00:00 AM,$0.00,No," + "\0" + @"
Circle,200,Lou,1/1/0001 12:00:00 AM,$0.00,No," + "\0";
            string actualArray = null;
            string actualList = null;
            string expectedOnce = @"Shape,Id,Name,JoinedDate,Salary,IsActive,Status
Circle,10,Mark,1/1/0001 12:00:00 AM,$0.00,No," + "\0";
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

            Assert.Multiple(() => {
                Assert.Throws<ChoReaderException>(() => ChoCSVWriter.ToText(objs));
                Assert.AreEqual(expectedArrayAndList, actualArray);
                Assert.AreEqual(expectedArrayAndList, actualList);
                Assert.AreEqual(expectedOnce, actualOnce);
            });

            // TODO: 2 subsequent calls of ChoCSVWriter.ToText(objs[0]) delivers different results. Check required
        }

        //[Test]
        public static void CodeFirstWithDeclarativeApproachWriteRecords()
        {
            string expected = @"Shape,Id,Name,JoinedDate,Salary,IsActive,Status
Circle,10,Mark,1/1/0001 12:00:00 AM,$0.00,false," + "\0" + @"
Circle,200,Lou,1/1/0001 12:00:00 AM,$0.00,false," + "\0";
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

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter<EmployeeRec>(writer))
            {
                parser.BeforeRecordFieldWrite += (o, e) =>
                {
                    actualPropertyNames.Add(e.PropertyName);
                };
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }

            Assert.Multiple(() => { CollectionAssert.AreEqual(expectedPropertyNames, actualPropertyNames); Assert.AreEqual(expected, actual); });
            // TODO: Check if export for IsActive should be false or 0
        }

        //[Test]
        public static void CodeFirstWithDeclarativeApproachWriteRecordsToFile()
        {
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

            using (var tx = File.OpenWrite(FileNameCodeFirstWithDeclarativeApproachWriteRecordsToFileTestCSV))
            {
                using (var parser = new ChoCSVWriter<EmployeeRec>(tx))
                {
                    parser.Write(objs);
                }
            }

            FileAssert.AreEqual(FileNameCodeFirstWithDeclarativeApproachWriteRecordsToFileExpectedCSV, FileNameCodeFirstWithDeclarativeApproachWriteRecordsToFileTestCSV);
        }

        //[Test]
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

        //[Test]
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

        //[Test]
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

        //[Test]
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

        //[Test]
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

                actual=reader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }

        //[Test]
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

        //[Test]
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
}
