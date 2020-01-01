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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

            Assert.AreEqual(expected, actual);
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
1,TOm,10000,IT";
            string actual = null;

            Dictionary<int, Manager> ht = new Dictionary<int, Manager>();
            ht.Add(1, new Manager { Name = "TOm", Salary = 10000, Department = "IT" });
            Dictionary<int, Employee> ht1 = new Dictionary<int, Employee>();
            ht1.Add(1, new Employee { Name = "TOm" });

            StringBuilder sb = new StringBuilder();
            using (var w = new ChoCSVWriter(sb)
                .WithFirstLineHeader()
                )
            {
                w.Write(ht);
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
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
            Assert.AreEqual(expected, actual);
        }

        [Test]
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

        [Test]
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

        static void DataReaderTest()
        {
            string csv = @"Id, Name, Address2
1, Tom,
2, Mark,";

            var dr = ChoCSVReader.LoadText(csv).WithFirstLineHeader().AsDataReader();
            
            using (var sw = new StreamWriter(File.OpenWrite("test.csv")))
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

            Console.WriteLine(File.OpenText("test.csv").ReadToEnd());
        }

        static void Pivot()
        {
            string csv = @"Name, Address, Age
""Foo"", ""Foo's address"", 24
""Bar"", ""Bar's address"", 19";

            StringBuilder sb = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                //.WithFirstLineHeader()
                )
            {
                Console.WriteLine(ChoCSVWriter.ToTextAll(r.Transpose(false)));
            }
        }

        static void LargeXmlToCSV()
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            XmlReaderSettings settings = new XmlReaderSettings();

            // SET THE RESOLVER
            settings.XmlResolver = new XmlUrlResolver();

            settings.ValidationType = ValidationType.DTD;
            settings.DtdProcessing = DtdProcessing.Parse;
            settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);
            settings.IgnoreWhitespace = true;

            Console.WriteLine(DateTime.Now.ToString());

            using (var r = new ChoXmlReader(XmlReader.Create(@"C:\Users\nraj39\Downloads\Loan\dblp.xml",
                settings)))
            {
                using (FileStream fs = File.Open(@"C:\Users\nraj39\Downloads\Loan\dblp.csv", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
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

        static void LargeJSON2CSV()
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            using (var r = new ChoJSONReader(@"C:\Users\nraj39\Downloads\Loan\rows.json"))
            {
                //var x = ((IDictionary<string, object>)r.FirstOrDefault()).Flatten().ToDictionary(c => c.Key, c => c.Value);
                //Console.WriteLine(x.Dump());
                using (var w = new ChoCSVWriter(@"C:\Users\nraj39\Downloads\Loan\rows.csv")
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(r.FirstOrDefault());
                }
            }
        }

        static void JSON2CSVTest1()
        {
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

            Console.WriteLine(ChoCSVWriter.ToTextAll(ChoJSONReader.LoadText(json,
                new ChoJSONRecordConfiguration()), 
                new ChoCSVRecordConfiguration().Configure(c => c.WithFirstLineHeader())));
        }

        static void JSON2CSVTest2()
        {
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
                .WithJSONPath("$..getUsers[*]")
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.MaxScanRows = 2)
                    .Configure(c => c.ThrowAndStopOnMissingField = false)
                    )
                {
                    w.Write(r);
                }
            }

            Console.WriteLine(csv.ToString());
        }

        static void JSON2CSVTest3()
        {
            dynamic[] x;
            dynamic[] x1;

            using (var r = new ChoJSONReader(@"C:\Users\nraj39\Downloads\Loan\test1.json")
                 .WithJSONPath("$..data.getUsers[*]"))
            {
                using (var r1 = new ChoJSONReader(@"C:\Users\nraj39\Downloads\Loan\test1.json")
                    .WithJSONPath("$..errors[*]")
                    )
                {
                    x = r.ToArray();
                    x1 = r1.ToArray();

                    var r3 = x1.ZipOrDefault(x, (i, j) =>
                    {
                        if (j != null)
                        {
                            j.Merge(i);
                            return j;
                        }
                        else
                            return i;
                    }).ToArray();

                    using (var w = new ChoCSVWriter(@"C:\Users\nraj39\Downloads\Loan\test1.csv")
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

        static void Main(string[] args)
        {
            JSON2CSVTest3();
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
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

                actual=reader.ReadToEnd();
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
}
