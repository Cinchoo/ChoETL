using ChoETL;
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
using System.Xml;
using System.Xml.Schema;

namespace ChoCSVWriterTest
{
    class Program
    {
        public class CustomType
        {
            [ChoCSVRecordField(1, FieldName = "date_start")]
            public DateTime DateStart { get; set; }
            [ChoCSVRecordField(2, FieldName = "date_end")]
            public DateTime DateEnd { get; set; }
            [ChoCSVRecordField(3, FieldName = "current_year")]
            public int CurrentYear { get; set; }
        }
        static void CSVWithQuotes()
        {
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

            Console.WriteLine(msg.ToString());
        }

        static void IntArrayTest()
        {
            dynamic address = new ChoDynamicObject();
            address.Street = "10 River Rd";
            address.City = "Princeton";

            dynamic state = new ChoDynamicObject();
            state.State = "NJ";
            state.Zip = "09930";

            address.State = state;

            using (var w = new ChoCSVWriter("intarray.csv")
                .Setup(s => s.RecordFieldWriteError += (o, e) => Console.WriteLine(e.Exception.ToString()))
                .Configure(c => c.NestedColumnSeparator = '/')
                )
            {
                //w.Write(new KeyValuePair<int, string>(1, "MM"));
                //w.Write(new KeyValuePair<int, string>(1, "MM"));
                w.Write(new { id = "1s->", address = address });
            }
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

        private static void NestedObjects()
        {
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
            Console.WriteLine(sb.ToString());
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

        static void InheritanceTest()
        {
            using (var w = new ChoCSVWriter<Employee>("Inheritance.csv").WithFirstLineHeader()
                .MapRecordFields<ManagerMetaData>()
                )
            {
                var o1 = new Manager { Name = "E1", Department = "History", Salary = 100000 };
                var o2 = new Manager { Name = "E2", Department = "Math", Salary = 110000 };
                w.Write(o1);
                w.Write(o2);
            }
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

        static void Sample3()
        {
            using (var p = new ChoCSVReader<Site>("Sample3.csv")
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
                    Console.WriteLine(msg.ToString());
                //foreach (var rec in p)
                    //Console.WriteLine(rec.Dump());
            }
        }

        static void ListTest()
        {
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

            Console.WriteLine(sb.ToString());
        }

        static void DictionaryTest()
        {
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

            Console.WriteLine(sb.ToString());
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
        static void ListPOCOTest()
        {
            List<Test> list = new List<Test>();

            list.Add(new Test { Id = 1, Name = "Tom", CreatedDate = DateTime.Today });
            list.Add(new Test { Id = 2, Name = "Mark" });

            using (var w = new ChoCSVWriter<Test>(Console.Out)
                .WithFirstLineHeader()
                )
            {
                w.Write(list);
            }
        }

        static void WriteSpecificColumns()
        {
            StringBuilder csv = new StringBuilder();

            Site site = new Site { SiteID = 1, House = 12, Apartment = 100, SiteAddress = new SiteAddress { City = "New York", Street = "101 Main St." } };

            using (var w = new ChoCSVWriter<Site>(new StringWriter(csv))
                .WithFirstLineHeader()
                //.ClearFields()
                .WithField(r => r.SiteID)
                //.WithField(r => r.SiteAddress.City)
                .Setup(s => s.FileHeaderWrite += (o, e) =>
                {
                    e.HeaderText = "ID, House";
                })
                )
            {
                w.Write(site);
            }

            Console.WriteLine(csv.ToString());
        }

        static void TestListOfInt()
        {
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
            Console.WriteLine(sb.ToString());
        }

        static void TestListOfInt1()
        {
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
            Console.WriteLine(sb.ToString());
        }

        static void TestHashtable()
        {
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
                w.Write(1);
            }
            Console.WriteLine(sb.ToString());
        }

        static void TestDictionary()
        {
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
            Console.WriteLine(sb.ToString());
        }


        static void AnonymousTypeTest()
        {
            StringBuilder sb = new StringBuilder();
            using (var w = new ChoCSVWriter(sb)
                .WithFirstLineHeader()
                )
            {
                w.Write(new { Id = 1, Name = "Tom" });
            }
            Console.WriteLine(sb.ToString());
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

        static void HierTest()
        {
            var e1 = new Emp { Id = 1, Name = "Tom", City = "Edison", Role = "Developer" };
            var e2 = new Emp { Id = 2, Name = "Mark", City = "Princeton", Role = "Analyst" };

            var csv = ChoCSVWriter<Emp>.ToTextAll(new Emp[] { e1, e2 },
                new ChoCSVRecordConfiguration().Configure(c => c.FileHeaderConfiguration.HasHeaderRecord = true));
            Console.WriteLine(csv);
        }


        static void SelectiveFieldPOCOTest()
        {
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

            Console.WriteLine(msg.ToString());
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

        static void QuotesIssue()
        {
            List<EmployeeRecSimple1> objs = new List<EmployeeRecSimple1>()
            {
                new EmployeeRecSimple1() { Id = 20, Name = "John Smith" },
                new EmployeeRecSimple1() { Id = 21, Name = "Jack in ,Da Box" }
            };
            Console.WriteLine(ChoCSVWriter<EmployeeRecSimple1>.ToTextAll(objs));
        }

        static void QuotesIssue1()
        {

            List<EmployeeRecSimple> objs = new List<EmployeeRecSimple>()
            {
                new EmployeeRecSimple() { Id = 20, Name = "John Smith" },
                new EmployeeRecSimple() { Id = 21, Name = @"Jack in ""Da Box" }
            };
            Console.WriteLine(ChoCSVWriter<EmployeeRecSimple>.ToTextAll(objs));
        }

        static void ReadNWrite()
        {
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
            Console.WriteLine(csvOut.ToString());
        }

        static void Issue45()
        {
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
            Console.WriteLine(msg.ToString());
        }

        static void ValidateSchema()
        {
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

            Console.WriteLine(csv.ToString());
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

        static void ComplexObjToCSV()
        {
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

            Console.WriteLine(sb.ToString());
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

        static void Main(string[] args)
        {
            LargeXmlToCSV();
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

        public static void SaveStringList()
        {
            List<string> list = new List<string>();
            list.Add("1/1/2012");
            list.Add("1/1");

            using (var w = new ChoCSVWriter("List.csv").WithFirstLineHeader()
                .WithField("Value", fieldName: "Value2", valueConverter: (v => v.CastTo<DateTime>()))
                )
                w.Write(list);
        }

        static void QuickDynamicTest()
        {
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

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        static void DateTimeDynamicTest()
        {
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

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        static void BoolTest()
        {
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

                Console.WriteLine(reader.ReadToEnd());
            }
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

        static void EnumTest()
        {
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

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        public class EmployeeRecWithCurrency
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public ChoCurrency Salary { get; set; }
        }

        static void CurrencyPOCOTest()
        {
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

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        static void CurrencyDynamicTest()
        {
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
            {
                using (var parser = new ChoCSVWriter(writer).WithFirstLineHeader().QuoteAllFields())
                {
                    parser.Write(objs);

                    writer.Flush();
                    stream.Position = 0;

                    Console.WriteLine(reader.ReadToEnd());
                }
            }
        }

        static void FormatSpecDynamicTest()
        {
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

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        static void FormatSpecTest()
        {
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

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        static void WriteDataTableTest()
        {
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            //ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;
            string connString = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

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

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        static void WriteDataReaderTest()
        {
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            //ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;
            string connString = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

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

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        static void ToTextTest()
        {
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
            Console.WriteLine(ChoCSVWriter.ToText(objs));
            Console.WriteLine(ChoCSVWriter.ToTextAll(objs.ToArray()));
        }

        static void CodeFirstWithDeclarativeApproachWriteRecords()
        {
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
                    Console.WriteLine(e.PropertyName);
                };
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        static void CodeFirstWithDeclarativeApproachWriteRecordsToFile()
        {
            List<EmployeeRec> objs = new List<EmployeeRec>();
            EmployeeRec rec1 = new EmployeeRec();
            rec1.Id = 10;
            rec1.Name = "Mark";
            objs.Add(rec1);

            EmployeeRec rec2 = new EmployeeRec();
            rec2.Id = 200;
            rec2.Name = "Lou";
            objs.Add(rec2);

            using (var tx = File.OpenWrite("Emp.csv"))
            {
                using (var parser = new ChoCSVWriter<EmployeeRec>(tx))
                {
                    parser.Write(objs);
                }
            }
        }

        static void ConfigFirstApproachWriteDynamicRecordsToFile()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.FileHeaderConfiguration.HasHeaderRecord = true;
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { Validators = new ValidationAttribute[] { new RangeAttribute(3, 100) } });
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

            using (var parser = new ChoCSVWriter("Emp.csv", config))
            {
                parser.Write(objs);
            }
        }

        static void ConfigFirstApproachWriteRecordsToFile()
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

            using (var parser = new ChoCSVWriter<EmployeeRecSimple>("Emp.csv", config))
            {
                parser.Write(objs);
            }
        }

        static void CodeFirstApproachWriteRecordsToFile()
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

            using (var parser = new ChoCSVWriter<EmployeeRecSimple>("Emp.csv"))
            {
                parser.Write(objs);
            }
        }

        static void DataFirstApproachWriteSingleRecordToFile()
        {
            using (var parser = new ChoCSVWriter("Emp.csv"))
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
        }

        static void DataFirstApproachWriteSingleRecord()
        {
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

                Console.WriteLine(reader.ReadToEnd());
            }

        }

        static void DataFirstApproachWriteListOfRecordsToFile()
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

            using (var parser = new ChoCSVWriter("Emp.csv"))
            {
                parser.Write(objs);
            }
        }

        static void DataFirstApproachWriteListOfRecords()
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

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVWriter(writer))
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                Console.WriteLine(reader.ReadToEnd());
            }

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
