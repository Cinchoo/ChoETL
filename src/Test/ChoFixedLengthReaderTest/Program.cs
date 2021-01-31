using ChoETL;
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

        //[Test]
        public static void AABillingTest()
        {
            List<object> expected = new List<object>
            { new AABillingHeaderRecord{ BusinessDate = new DateTime(2017,5,4), Description = "Sample file" },
            new AABillingDetailRecord{ ClientID=1234567890, ClientName = "Chubb" },
            new AABillingTrailerRecord{ RecordCount=10 }
            };
            List<object> actual = new List<object>();

            using (var p = new ChoFixedLengthReader("AABilling.txt")
                .WithRecordSelector(0, 1, null, typeof(AABillingDetailRecord), typeof(AABillingTrailerRecord), typeof(AABillingHeaderRecord))
                //.WithCustomRecordSelector((l) =>
                //{
                //	Tuple<long, string> kvp = l as Tuple<long, string>;
                //	if (kvp.Item2.StartsWith("H"))
                //		return typeof(AABillingHeaderRecord);
                //	else if (kvp.Item2.StartsWith("T"))
                //		return typeof(AABillingTrailerRecord);
                //	else
                //		return typeof(AABillingDetailRecord);
                //})
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);// Console.WriteLine(ChoUtility.Dump(rec));
            }
        }

        public class AccountBalance
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public List<string> lastTwelveMonths { get; set; }
        }

        //[Test]
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

        //[Test]
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

        //[Test]
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

        static void Sample1Test()
        {
            string fix = @"---------------------------A---------------------------

AARON THIAGO LOPES                       3099234 100-11

AARON PAPA DA SILVA                      8610822 160-26

ABNER MENEZEZ SOUZA                      1494778 500-35

EDSON EDUARD MOZART                      1286664 500-34";

            using (var r = ChoFixedLengthReader.LoadText(fix)
                .Configure(c => c.Comment = "--")
                .WithField("Name", 0, 41)
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }
        static void Main(string[] args)
        {
            Sample1Test();
            return;

            AABillingTest();
            return;
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

        //[Test]
        public static void QuickLoad()
        {
            Stopwatch[] sw = new Stopwatch[5];
            for (int i = 0; i < 5; i++)
            {
                sw[i] = Stopwatch.StartNew();
                using (var r = new ChoFixedLengthReader(FileNameAccountsTXT)
                    //.WithFirstLineHeader()
                    //.Configure(c => c.MaxScanRows = 2)
                    )
                {
                    //r.RecordLoadError += (o, e) =>
                    //{
                    //    Console.WriteLine(e.Exception.Message);
                    //    e.Handled = true;
                    //};
                    foreach (dynamic rec in r)
                    {
                        //Console.WriteLine("{0}", rec.Dump());
                    }
                }
                sw[i].Stop();
                Console.WriteLine(sw[i].Elapsed.TotalSeconds);
            }
            Assert.Warn("I am not sure what to test. Maximum amount of elapsed time?");
            Assert.Less(sw.Average(x => x.Elapsed.TotalSeconds), 1);
        }

        //[Test]
        public static void QuickDataTableTest()
        {
            DataTable expected = new DataTable();
            object[] tmpHeader = new object[] { "Account", "LastName", "FirstName", "Balance", "CreditLimit", "AccountCreated", "Rating" };
            object[] allHeader = new object[tmpHeader.Length * 97];
            object[] tmpRow = new object[] { 101, "Reeves", "Keanu", 9315.45, "10000.00", new DateTime(1998, 1, 17).ToString("d"), "A" };
            object[] allRow = new object[tmpRow.Length * 97];
            for (int i = 1; i <= 679; i++)
                expected.Columns.Add("Column" + i.ToString());
            for (int i = 0; i < 97; i++)
            {
                tmpHeader.CopyTo(allHeader, i * 7);
                tmpRow.CopyTo(allRow, i * 7);
            }

            expected.Rows.Add(allHeader);
            expected.Rows.Add(allRow);
            expected.Rows.Add(allRow);
            expected.Rows.Add(allRow);

            var actual = new ChoFixedLengthReader(FileNameAccountsTXT).AsDataTable();

            DataTableAssert.AreEqual(expected, actual);
        }
        //[Test]
        public static void POCODataTableTest()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add("Account", typeof(int));
            expected.Columns.Add("LastName", typeof(string));
            expected.Columns.Add("FirstName", typeof(string));
            expected.Columns.Add("Balance", typeof(double));
            expected.Columns.Add("CreditLimit", typeof(double));
            expected.Columns.Add("AccountCreated", typeof(DateTime));
            expected.Columns.Add("Rating", typeof(string));
            expected.Rows.Add(101, "Reeves", "Keanu", 9315.45, 10000, new DateTime(1998, 1, 17), "A");
            DataTable actual = null;

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<CreditBalanceRecord>(reader))
            {
                writer.WriteLine("Account LastName        FirstName       Balance     CreditLimit   AccountCreated  Rating ");
                writer.WriteLine("101     Reeves          Keanu           9315.45     10000.00      1/17/1998       A      ");
                writer.Flush();
                stream.Position = 0;

                actual = parser.AsDataTable();
            }
            DataTableAssert.AreEqual(expected, actual);
        }

        //[Test]
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

        //[Test]
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

        //[Test]
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

        //[Test]
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

        //[Test]
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
                writer.WriteLine("Id      Name      ");
                writer.WriteLine("1       Carl      ");
                writer.WriteLine("2       Mark      ");
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

        //[Test]
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

        //[Test]
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

        //[Test]
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

        //[Test]
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

        //[Test]
        public static void QuickTest()
        {
            object row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecWithCurrency>(reader).WithFirstLineHeader())
            {
                writer.WriteLine("Id      Name      ");
                writer.WriteLine("1       Carl      ");
                writer.WriteLine("2       Mark      ");
                writer.Flush();
                stream.Position = 0;

                Assert.Throws< ChoRecordConfigurationException >(() => { row = parser.Read(); });
            }
        }

        //[Test]
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

        //[Test]
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

        //[Test]
        public static void FallbackValueUsedViaCodeFirstApproach()
        {
            EmployeeRecSimpleFallback row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecSimpleFallback>(reader))
            {
                writer.WriteLine("001Carl 08/12/2016$100,000                      0F");
                writer.WriteLine("002MarkS13/01/2010$500,000                      1C");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    Console.WriteLine(row.ToStringEx());
                }
            }
            Assert.Fail("Not sure, how to parse the correct position for Salary");
        }

        //[Test]
        public static void FallbackValueUsedViaConfigFirstApproach()
        {
            List<object> expected = new List<object> {
                new ChoDynamicObject { { "Id", (int)1},{"Name","Carl" },{ "JoinedDate", new DateTime(2016, 8, 12) },{ "Salary", new ChoCurrency(100000) },{ "IsActive", false },{ "Status", 'F' }  },
                new ChoDynamicObject { { "Id", (int)2},{"Name","MarkS" },{ "JoinedDate", new DateTime(2010, 1, 1) },{ "Salary", new ChoCurrency(500000) },{ "IsActive", true },{ "Status", 'C' }  }
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
            using (var parser = new ChoFixedLengthReader(reader, config))
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

        //[Test]
        public static void DefaultValueUsedViaCodeFirstApproach()
        {
            EmployeeRecSimple row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecSimple>(reader))
            {
                writer.WriteLine("001Carl 08/12/2016$100,000                      0F");
                writer.WriteLine("002MarkS01/01/2010$500,000                      1C");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    Console.WriteLine(row.ToStringEx());
                }
            }
            Assert.Fail("Not sure, how to write a correct list of expected values, because none of the POCO-Classes EmplyeeRecXXX is suitable.");
        }

        //[Test]
        public static void DefaultValueUsedViaConfigFirstApproach()
        {
            List<object> expected = new List<object> {
                new ChoDynamicObject{ { "Id", (int)1 },{ "Name", "Carl" },{ "JoinedDate", new DateTime(2016, 8, 12) },{ "Salary", new ChoCurrency(100000) },{ "IsActive", false },{ "Status", 'F' } },
                new ChoDynamicObject{ { "Id", (int)2 },{ "Name", "MarkS" },{ "JoinedDate", new DateTime(2010,10, 10) },{ "Salary", new ChoCurrency(500000) },{ "IsActive", true },{ "Status", 'C' } }
            };
            List<object> actual = new List<object>();

            ChoFixedLengthRecordConfiguration config = new ChoFixedLengthRecordConfiguration();
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Id", 0, 3) { FieldType = typeof(int) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Name", 3, 5) { FieldType = typeof(string) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("JoinedDate", 8, 10) { FieldType = typeof(DateTime), DefaultValue = "10/10/2010" });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Salary", 18, 10) { FieldType = typeof(ChoCurrency) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("IsActive", 28, 1) { FieldType = typeof(bool) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Status", 29, 1) { FieldType = typeof(char) });
            config.ErrorMode = ChoErrorMode.ReportAndContinue;

            dynamic row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader(reader, config))
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
    }

    public partial class EmployeeRecSimple
    {
        public int Id { get; set; }
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
        public int Id { get; set; }
        public string Name { get; set; }
        [DefaultValue("1/1/2001")]
        [ChoFallbackValue("13/1/2011")]
        public DateTime JoinedDate { get; set; }
        [DefaultValue("50000")]
        public ChoCurrency Salary { get; set; }
        public bool IsActive { get; set; }
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
