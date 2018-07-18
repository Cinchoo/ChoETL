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
            using (var writer = new ChoCSVWriter<CustomType>("CSVWithQuotes.csv").WithFirstLineHeader()
                .Configure(c => c.QuoteAllFields = true)
                .Configure(c => c.Culture = new CultureInfo("en-CA"))
                )
            {
                var x1 = new CustomType { DateStart = DateTime.Today, DateEnd = DateTime.Today.AddDays(2), CurrentYear = DateTime.Today.Year };
                writer.Write(x1);
            }

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

            using (var w = new ChoCSVWriter("nestedObjects.csv").WithFirstLineHeader())
            {
                w.Write(root.Deals.SelectMany(d => d.TShape.Select(s => new { ClientName = root.Client, DealNo = d.DealName, StartDate = s.StartDate, EndDate = s.EndDate, Volume = s.Volume })));
            }

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


        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            ListTest();
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
            using (var parser = new ChoCSVWriter(writer).WithFirstLineHeader().QuoteAllFields())
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                Console.WriteLine(reader.ReadToEnd());
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
