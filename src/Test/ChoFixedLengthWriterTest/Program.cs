using ChoETL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace ChoFixedLengthWriterTest
{
    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {
        static void Main(string[] args)
        {
			ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;

            POCOTest();
        }

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }

        [Test]
        public static void TrimTest()
		{
            string expected = @"0000000000Tom 23423432432 432432423";
            string actual = null;

            using (var ms = new MemoryStream())
            using (var tw = new StreamWriter(ms))
            using (var w = new ChoFixedLengthWriter<EmployeeRec>(tw))
			{
				w.Write(new EmployeeRec { Id = 90000000000000, Name = "Tom 23423432432 432432423423432423423423432432432423423432" });

                tw.Flush();
                ms.Position = 0;
                actual = ms.ReadToEnd();
            }

            Assert.AreEqual(expected, actual);
		}

		public class EmployeeRec
		{
			[ChoFixedLengthRecordField(1, 10)]
			public long Id { get; set; }
			[ChoFixedLengthRecordField(11, 25)]
			public string Name { get; set; }
		}

        [Test]
        public static void SaveStringList()
        {
            string expected = @"Value
20120
" + DateTime.Now.Year.ToString() + "0";
            string actual = null;

            List<string> list = new List<string>();
            list.Add("1/1/2012");
            list.Add("1/1");

            using (var w = new ChoFixedLengthWriter(FileNameSaveStringListTXT).WithFirstLineHeader()
                .WithField("Value", 1, 5, valueConverter: v => v.CastTo<DateTime>().ToString("yyyyMMddhhmmss"))
                )
                w.Write(list);

            actual = new StreamReader(FileNameSaveStringListTXT).ReadToEnd();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ToTextTest()
        {
            string expected = @"0000000010Mark                     
0000000200Lou                      ";
            string actual = null;

            //ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "G";
            //ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            ////ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;

            List<EmployeeRecSimple> objs = new List<EmployeeRecSimple>();
            EmployeeRecSimple rec1 = new EmployeeRecSimple();
            rec1.Id = 10;
            rec1.Name = "Mark";
            objs.Add(rec1);

            EmployeeRecSimple rec2 = new EmployeeRecSimple();
            rec2.Id = 200;
            rec2.Name = "Lou";
            objs.Add(rec2);

            actual = ChoFixedLengthWriter.ToTextAll< EmployeeRecSimple>(objs, new ChoFixedLengthRecordConfiguration()
                .ConfigureTypeConverterFormatSpec(ts => ts.DateTimeFormat = "G")
                .ConfigureTypeConverterFormatSpec(ts => ts.BooleanFormat = ChoBooleanFormatSpec.YesOrNo)
                );
            Assert.AreEqual(expected, actual);
        }

        public static string FileNamePOCOTestTXT => "POCOTest.txt";
        public static string FileNameQuickWriteTestTXT => "QuickWriteTest.txt";
        public static string FileNameQuickWriteTest2TXT => "QuickWriteTest2.txt";
        public static string FileNameSaveStringListTXT => "SaveStringList.txt";

        [Test]
        public static void POCOTest()
        {
            string expected = @"Id      Name      
00000001Mark      
00000002Jason     ";
            string actual = null;

            List<EmployeeRecSimple> objs = new List<EmployeeRecSimple>();

            EmployeeRecSimple rec1 = new EmployeeRecSimple();
            rec1.Id = 1;
            rec1.Name = "Mark";
            objs.Add(rec1);

            EmployeeRecSimple rec2 = new EmployeeRecSimple();
            rec2.Id = 2;
            rec2.Name = "Jason";
            objs.Add(rec2);

            ChoFixedLengthRecordConfiguration configuration = new ChoFixedLengthRecordConfiguration();

            configuration.Encoding = System.Text.Encoding.GetEncoding(1252);

            using (var parser = new ChoFixedLengthWriter<EmployeeRecSimple>(FileNamePOCOTestTXT, configuration).
                WithFirstLineHeader().
                WithField("Id", 0, 8).
                WithField("Name", 5, 10))
            {
                parser.Write(objs);
            }

            actual = new StreamReader(FileNamePOCOTestTXT).ReadToEnd();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void QuickWriteTest2()
        {
            string expected = @"Id      Name      
00000001Mark      
00000002Jason     ";
            string actual = null;

            using (var parser = new ChoFixedLengthWriter(FileNameQuickWriteTest2TXT).
                WithFirstLineHeader().
                WithField("Id", 0, 8).
                WithField("Name", 5, 10))
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

            actual = new StreamReader(FileNameQuickWriteTest2TXT).ReadToEnd();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void QuickWriteTest()
        {
            string expected = @"Id      Name      
00000001Mark      
00000002Jason     ";
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

            using (var parser = new ChoFixedLengthWriter(FileNameQuickWriteTestTXT).
                WithFirstLineHeader().
                WithField("Id", 0, 8).
                WithField("Name", 5, 10))
            {
                parser.Write(objs);
            }

            actual = new StreamReader(FileNameQuickWriteTestTXT).ReadToEnd();
            Assert.AreEqual(expected, actual);

        }

        [Test]
        public static void QuickDynamicTest()
        {
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.BooleanFormat
            string expected = @"Id Name      JoinedDateASalary              
010MARK      02/02/2001Y$100,000.00         
000LOU       0000000000N                    ";
            string actual = null;

            //ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MM/dd/yyyy";
            //ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YOrN;

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.JoinedDate = new DateTime(2001, 2, 2);
            rec1.IsActive = true;
            rec1.Salary = new ChoCurrency(100000);
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2000;
            rec2.Name = "Lou";
            rec2.JoinedDate = null; // new DateTime(1990, 10, 23);
            rec2.IsActive = false;
            rec2.Salary = null; // new ChoCurrency(150000);
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthWriter(writer).
                WithFirstLineHeader().
                WithField("Id", 0, 3, null, null, '0', ChoFieldValueJustification.Right, true).
                WithField("Name", 3, 10).
                WithField("JoinedDate", 13, 10, fieldType: typeof(DateTime), fillChar: '0').
                WithField("IsActive", 23, 1, fieldName: "A").
                WithField("Salary", 24, 20)
                .TypeConverterFormatSpec(ts => ts.DateTimeFormat = "MM/dd/yyyy")
                .TypeConverterFormatSpec(ts => ts.BooleanFormat = ChoBooleanFormatSpec.YOrN)
                )

            {
                parser.Configuration["Name"].AddConverter(new ChoUpperCaseConverter());
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                actual=reader.ReadToEnd();
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
            string expected = @"Id   Name                Salary    
00010Mark                $100,000.0
00200Lou                 $150,000.0";
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
            using (var parser = new ChoFixedLengthWriter<EmployeeRecWithCurrency>(writer).WithFirstLineHeader().
                WithField("Id", 0, 5).
                WithField("Name", 5, 20).
                WithField("Salary", 25, 10))
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                actual = reader.ReadToEnd();
            }

            Assert.AreEqual(expected, actual);
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
            // TODO: Check missing usage of ChoTypeConverterFormatSpec.Instance.DateTimeFormat
            string expected = @"Id   Name                JoinedDateISalary    Status    
00010Mark                2/2/2001  T$100,000.0Permanent 
00200Lou                 10/23/1990F$150,000.0Contract  ";
            string actual = null;

            //ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Name;

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
            using (var parser = new ChoFixedLengthWriter(writer).WithFirstLineHeader().
                WithField("Id", 0, 5).
                WithField("Name", 5, 20).
                WithField("JoinedDate", 25, 10).
                WithField("IsActive", 35, 1).
                WithField("Salary", 36, 10).
                WithField("Status", 46, 10)
                .TypeConverterFormatSpec(ts => ts.EnumFormat = ChoEnumFormatSpec.Name)
                )

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
            string expected = @"Id   Name                JoinedDateISalary    Status    
00010Mark                2/2/2001  Y$100,000.00         
00020Lou                 10/23/1990N$150,000.02         ";
            string actual = null;
            
            //ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.YOrN;

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
            rec2.Id = 20;
            rec2.Name = "Lou";
            rec2.JoinedDate = new DateTime(1990, 10, 23);
            rec2.IsActive = false;
            rec2.Salary = new ChoCurrency(150000);
            rec2.Status = EmployeeType.Contract;
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthWriter(writer).WithFirstLineHeader().WithField("Id", 0, 5).
                WithField("Name", 5, 20).
                WithField("JoinedDate", 25, 10).
                WithField("IsActive", 35, 1).
                WithField("Salary", 36, 10).
                WithField("Status", 46, 10)
                .TypeConverterFormatSpec(ts => ts.BooleanFormat = ChoBooleanFormatSpec.YOrN)
                )
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
            string expected = @"Name                JoinedDateISalary    
Mark                Feb 02, 20T$100,000.0
Lou                 Oct 23, 19F$150,000.0";

            string actual = null;
           
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
            using (var parser = new ChoFixedLengthWriter(writer).WithFirstLineHeader()
                .WithField("Name", 5, 20)
                .WithField("JoinedDate", 25, 10)
                .WithField("IsActive", 35, 1)
                .WithField("Salary", 36, 10)
                .TypeConverterFormatSpec(ts => ts.DateTimeFormat = "MMM dd, yyyy")
                )
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
}
