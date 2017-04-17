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

    class Program
    {
        static void Main(string[] args)
        {
            //Override the width of necessary simple types
            //ChoFixedLengthFieldDefaultSizeConfiguation.Instance.SetSize(typeof(int), 3);
            //ChoFixedLengthFieldDefaultSizeConfiguation.Instance.SetSize(typeof(string), 5);

            POCODataTableTest();
        }

        static void QuickLoad()
        {
            foreach (var rec in new ChoFixedLengthReader("accounts.txt"))
            {
                Console.WriteLine(rec.ToStringEx());
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
            using (var parser = new ChoFixedLengthReader("Emp.txt").WithFirstLineHeader().WithField("Id", 0, 8).WithField("Name", 8, 10, true))
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
