using ChoETL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoFixedLengthReaderTest
{
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

    class Program
    {
        public static void AABillingTest()
        {
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
                    Console.WriteLine(ChoUtility.Dump(rec));
            }
        }

        public class AccountBalance
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public List<string> lastTwelveMonths { get; set; }
        }

        static void NestedObjectTest()
        {
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
                    w.Write(p);
                }

                //foreach (var rec in p)
                //    Console.WriteLine(rec.Dump());
            }

            Console.WriteLine(sb.ToString());
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
        }

        static void Test1()
        {
            string txt = @"Filip    Malýn        Male  1218-02-1994
Božena   Němcová      Female1804-02-1820
Jan      Žižka        Male  0719-09-1360
Che      Guevara      Male  2714-06-1928
AntoinedeSaint-ExupéryMale  1529-06-1900";

            foreach (var rec in ChoFixedLengthReader<Person>.LoadText(txt))
                Console.WriteLine(rec.Dump());

        }

        public class Emp1
        {
            [ChoFixedLengthRecordField(0, 5)]
            public String ID { get; set; }

            [ChoFixedLengthRecordField(5, 10)]
            public String Name1 { get; set; }
        }

        static void Test2()
        {
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
                Console.WriteLine(rec.Dump());

        }

        static void Main(string[] args)
        {
            Test2();
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

        static void QuickLoad()
        {
            using (var r = new ChoFixedLengthReader("accounts.txt").WithFirstLineHeader()
                .Configure(c => c.MaxScanRows = 2)
                )
            {
                //r.RecordLoadError += (o, e) =>
                //{
                //    Console.WriteLine(e.Exception.Message);
                //    e.Handled = true;
                //};
                foreach (dynamic rec in r)
                {
                    Console.WriteLine("{0}", rec.Dump());
                }
            }

        }

        static void QuickDataTableTest()
        {
            var dt = new ChoFixedLengthReader("accounts.txt").AsDataTable();
        }
        static void POCODataTableTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<CreditBalanceRecord>(reader))
            {
                writer.WriteLine("Account LastName        FirstName       Balance     CreditLimit   AccountCreated  Rating ");
                writer.WriteLine("101     Reeves          Keanu           9315.45     10000.00      1/17/1998       A      ");
                writer.Flush();
                stream.Position = 0;

                var dt = parser.AsDataTable();
            }
        }

        static void DynamicApproach()
        {
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
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }

        static void LoadTextTest()
        {
            string txt = "Id      Name      \r\n1       Carl      \r\n2       Mark      ";

            foreach (var e in ChoFixedLengthReader.LoadText(txt).WithFirstLineHeader())
                Console.WriteLine(e.ToStringEx());
        }

        static void CodeFirstApproach()
        {
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
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }

        static void QuickDynamicLoadTest()
        {
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
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }

        static void QuickDynamicLoadTestUsingIterator()
        {
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
                    Console.WriteLine(e.ToStringEx());
                }
            }
        }

        static void MultiLineTest()
        {
            object row = null;
            using (var parser = new ChoFixedLengthReader("Emp.txt").WithFirstLineHeader().WithField("Id", 0, 8).WithField("Name", 8, 10))
            {
                parser.Configuration.MayContainEOLInData = true;
                while ((row = parser.Read()) != null)
                {
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }

        static void ConfigFirstApproachReadAsDynamicRecords()
        {
            ChoFixedLengthRecordConfiguration config = new ChoFixedLengthRecordConfiguration();
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Id", 0, 8) { FieldType = typeof(int) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Name", 8, 10) { FieldType = typeof(string) });

            ExpandoObject row = null;
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
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }

        static void ConfigFirstApproachReadAsTypedRecords()
        {
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
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }

        static void CodeFirstWithDeclarativeApproachRead()
        {
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
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }

        static void QuickTest()
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

                while ((row = parser.Read()) != null)
                {
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }

        static void CodeFirstWithDeclarativeApproach()
        {
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
                    Console.WriteLine(row.ToStringEx());
                }
            }

            //using (var stream = new MemoryStream())
            //using (var reader = new StreamReader(stream))
            //using (var writer = new StreamWriter(stream))
            //{
            //    writer.WriteLine("Account LastName        FirstName       Balance     CreditLimit   AccountCreated  Rating ");
            //    writer.WriteLine("101     Reeves          Keanu           9315.45     10000.00      1/17/1998       A      ");
            //    writer.Flush();
            //    stream.Position = 0;

            //    foreach (var item in new ChoFixedLengthReader<CreditBalanceRecord>(reader))
            //    {
            //        Console.WriteLine(item.ToStringEx());
            //    }
            //}
        }

        static void FallbackValueUsedViaCodeFirstApproach()
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
        }

        static void FallbackValueUsedViaConfigFirstApproach()
        {
            ChoFixedLengthRecordConfiguration config = new ChoFixedLengthRecordConfiguration();
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Id", 0, 3) { FieldType = typeof(int) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Name", 3, 5) { FieldType = typeof(string) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("JoinedDate", 8, 10) { FieldType = typeof(DateTime), FallbackValue = "1/1/2010" });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Salary", 18, 10) { FieldType = typeof(ChoCurrency) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("IsActive", 28, 1) { FieldType = typeof(bool) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Status", 29, 1) { FieldType = typeof(char) });
            config.ErrorMode = ChoErrorMode.ReportAndContinue;

            ExpandoObject row = null;
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
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }

        static void DefaultValueUsedViaCodeFirstApproach()
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
        }

        static void DefaultValueUsedViaConfigFirstApproach()
        {
            ChoFixedLengthRecordConfiguration config = new ChoFixedLengthRecordConfiguration();
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Id", 0, 3) { FieldType = typeof(int) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Name", 3, 5) { FieldType = typeof(string) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("JoinedDate", 8, 10) { FieldType = typeof(DateTime), DefaultValue = "10/10/2010" });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Salary", 18, 10) { FieldType = typeof(ChoCurrency) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("IsActive", 28, 1) { FieldType = typeof(bool) });
            config.FixedLengthRecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Status", 29, 1) { FieldType = typeof(char) });
            config.ErrorMode = ChoErrorMode.ReportAndContinue;

            ExpandoObject row = null;
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
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }
    }

    public partial class EmployeeRecSimple
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public partial class EmployeeRec
    {
        [ChoFixedLengthRecordField(0, 8)]
        public int Id { get; set; }
        [ChoFixedLengthRecordField(8, 10)]
        public string Name { get; set; }
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
