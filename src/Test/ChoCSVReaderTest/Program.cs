using ChoETL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;

namespace ChoCSVReaderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            LoadTextTest();
        }

        static void LoadTextTest()
        {
            string txt = "Id, Name\r\n1, Mark";
            foreach (var e in ChoCSVReader.LoadText(txt).WithFirstLineHeader())
                Console.WriteLine(e.ToStringEx());
        }

        static void QuickTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRec>(reader).WithDelimiter(",").WithFirstLineHeader().WithFields().WithField("Salary").WithField("Name", typeof(string)))
            {
                writer.WriteLine("Id,Name,Salary");
                writer.WriteLine("1,Carl,1000");
                writer.WriteLine("2,Mark,2000");
                writer.WriteLine("3,Tom,3000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void QuickDynamicTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader).WithDelimiter(",").WithFirstLineHeader().WithField("Id", typeof(int)).WithField("Name", typeof(string)))
            {
                writer.WriteLine("Id,Name,Salary");
                writer.WriteLine("1,Carl,1000");
                writer.WriteLine("2,Mark,2000");
                writer.WriteLine("3,Tom,3000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void DateTimeTest()
        {
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MMM dd, yyyy";

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("JoinedDate", 4) { FieldType = typeof(DateTime) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Active", 5) { FieldType = typeof(bool) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine(@"1,Carl,12345679,""Jan 01, 2011"",0");
                writer.WriteLine(@"2,Mark,50000,""Sep 23, 1995"",1");
                writer.WriteLine(@"3,Tom,150000,""Apr 10, 1999"",1");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    Console.WriteLine(row.ToStringEx());
            }
        }

        static void UsingLinqTest()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.Culture = new System.Globalization.CultureInfo("se-SE");
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("JoinedDate", 4) { FieldType = typeof(DateTime) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("EmployeeNo", 5) { FieldType = typeof(int) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine(@"1,Carl,12.345679 kr,2017-10-10,  (5)    ");
                writer.WriteLine("2,Markl,50000 kr,2001-10-01,  6    ");
                writer.WriteLine("3,Toml,150000 kr,1996-01-25,  9    ");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    Console.WriteLine(row.ToStringEx());
            }
        }

        static void BoolTest()
        {
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.ZeroOrOne;

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("JoinedDate", 4) { FieldType = typeof(DateTime) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Active", 5) { FieldType = typeof(bool) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine(@"1,Carl,12345679,01/10/2016,0");
                writer.WriteLine("2,Mark,50000,10/01/1995,1");
                writer.WriteLine("3,Tom,150000,01/01/1940,1");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    Console.WriteLine(row.ToStringEx());
            }
        }

        public enum EmployeeType
        {
            [Description("Full Time Employee")]
            Permanent = 0,
            [Description("Temporary Employee")]
            Temporary = 1,
            [Description("Contract Employee")]
            Contract = 2
        }
        static void EnumTest()
        {
            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Description;

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("JoinedDate", 4) { FieldType = typeof(DateTime) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("EmployeeType", 5) { FieldType = typeof(EmployeeType) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine(@"1,Carl,12345679,01/10/2016,Full Time Employee");
                writer.WriteLine("2,Mark,50000,10/01/1995,Temporary Employee");
                writer.WriteLine("3,Tom,150000,01/01/1940,Contract Employee");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    Console.WriteLine(row.ToStringEx());
            }
        }

        static void UsingFormatSpecs()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.Culture = new System.Globalization.CultureInfo("se-SE");
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("JoinedDate", 4) { FieldType = typeof(DateTime) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("EmployeeNo", 5) { FieldType = typeof(int) });

            ChoTypeConverterFormatSpec.Instance.IntNumberStyle = NumberStyles.AllowParentheses;

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine(@"1,Carl,12.345679 kr,2017-10-10,  (5)    ");
                writer.WriteLine("2,Markl,50000 kr,2001-10-01,  6    ");
                writer.WriteLine("3,Toml,150000 kr,1996-01-25,  9    ");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    Console.WriteLine(row.ToStringEx());
            }
        }

        static void ValidationOverridePOCOTest()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            var idConfig = new ChoCSVRecordFieldConfiguration("Id", 1);
            idConfig.Validators = new ValidationAttribute[] { new RequiredAttribute() };
            config.CSVRecordFieldConfigurations.Add(idConfig);
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRecWithCurrency>(reader, config))
            {
                writer.WriteLine(",Carl,$100000");
                writer.WriteLine("2,Mark,$50000");
                writer.WriteLine("3,Tom,1000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        public class EmployeeRecWithCurrency
        {
            public int? Id { get; set; }
            public string Name { get; set; }
            public ChoCurrency Salary { get; set; }
        }

        static void CurrencyTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRecWithCurrency>(reader))
            {
                writer.WriteLine("1,Carl,$100000");
                writer.WriteLine("2,Mark,$50000");
                writer.WriteLine("3,Tom,1000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void CurrencyDynamicTest()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine("1,Carl,$100000");
                writer.WriteLine("2,Mark,$50000");
                writer.WriteLine("3,Tom,1000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void AsDataReaderTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRec>(reader))
            {
                writer.WriteLine("1,Carl");
                writer.WriteLine("2,Mark");
                writer.WriteLine("3,Tom");

                writer.Flush();
                stream.Position = 0;

                IDataReader dr = parser.AsDataReader();
                while (dr.Read())
                {
                    Console.WriteLine("Id: {0}, Name: {1}", dr[0], dr[1]);
                }
            }
        }

        static void AsDataTableTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRec>(reader))
            {
                writer.WriteLine("1,Carl");
                writer.WriteLine("2,Mark");
                writer.WriteLine("3,Tom");

                writer.Flush();
                stream.Position = 0;

                DataTable dt = parser.AsDataTable();
                foreach (DataRow dr in dt.Rows)
                {
                    Console.WriteLine("Id: {0}, Name: {1}", dr[0], dr[1]);
                }
            }
        }


        private static void OldTest()
        {
            //var t = ChoTypeDescriptor.GetPropetyAttributes<ChoTypeConverterAttribute>(ChoTypeDescriptor.GetProperty<ChoTypeConverterAttribute>(typeof(EmployeeRecMeta), "Name")).ToArray();
            //return;

            //ChoMetadataObjectCache.Default.Attach(typeof(EmployeeRec), new EmployeeRecMeta());
            //string v = @"4,'123\r\n4,abc'";
            //foreach (var ss in v.SplitNTrim(",", ChoStringSplitOptions.None, '\''))
            //    Console.WriteLine(ss + "-");
            //return;

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            //config.AutoDiscoverColumns = false;
            config.FileHeaderConfiguration.HasHeaderRecord = true;
            //config.CSVFileHeaderConfiguration.FillChar = '$';
            config.ThrowAndStopOnMissingField = false;
            //config.HasExcelSeparator = true;
            config.ColumnCountStrict = false;
            //config.MapRecordFields<EmployeeRec>();
            ChoCSVRecordFieldConfiguration idConfig = new ChoCSVRecordFieldConfiguration("Id", 1);
            idConfig.AddConverter(new IntConverter());
            config.CSVRecordFieldConfigurations.Add(idConfig);
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name1", 2));

            dynamic rec = new ExpandoObject();
            rec.Id = 1;
            rec.Name = "Raj";

            //using (var wr = new ChoCSVWriter("EmpOut.csv", config))
            //{
            //    wr.Write(new List<ExpandoObject>() { rec });
            //}

            //List<EmployeeRec> recs = new List<EmployeeRec>();
            //recs.Add(new EmployeeRec() { Id = 1, Name = "Raj" });
            //recs.Add(new EmployeeRec() { Id = 2, Name = "Mark" });

            //using (var stream = new MemoryStream())
            //using (var reader = new StreamReader(stream))
            //using (var writer = new StreamWriter(stream))
            //using (var parser = new ChoCSVWriter<EmployeeRec>(writer, config))
            //{
            //    parser.Write(recs);
            //    writer.Flush();
            //    stream.Position = 0;

            //    Console.WriteLine(reader.ReadToEnd());
            //}
            //return;

            //string txt = "Id, Name\r\n1, Mark";
            //foreach (var e in ChoCSVReader.LoadText(txt))
            //    Console.WriteLine(e.ToStringEx());
            //return;
            //dynamic row;
            //using (var stream = new MemoryStream())
            //using (var reader = new StreamReader(stream))
            //using (var writer = new StreamWriter(stream))
            //using (var parser = new ChoCSVReader(reader, config))
            //{
            //    //writer.WriteLine("Id,Name");
            //    writer.WriteLine("1,Carl");
            //    writer.WriteLine("2,Mark");
            //    writer.Flush();
            //    stream.Position = 0;

            //    while ((row = parser.Read()) != null)
            //    {
            //        Console.WriteLine(row.Id);
            //    }
            //}
            //return;

            //DataTable dt = new ChoCSVReader<EmployeeRec>("Emp.csv").AsDataTable();
            //var z = dt.Rows.Count;
            //return;

            foreach (var item in new ChoCSVReader<EmployeeRec>("Emp.csv"))
                Console.WriteLine(item.ToStringEx());
            return;

            //var reader = new ChoCSVReader<EmployeeRec>("Emp.csv");
            //var rec = (object)null;

            //while ((rec = reader.Read()) != null)
            //    Console.WriteLine(rec.ToStringEx());

            //var config = new ChoCSVRecordConfiguration(typeof(EmployeeRec));
            //var e = new ChoCSVReader("Emp.csv", config);
            //dynamic i;
            //while ((i = e.Read()) != null)
            //    Console.WriteLine(i.Id);

            ChoETLFramework.Initialize();
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRec>(reader))
            {
                writer.WriteLine("Id,Name");
                writer.WriteLine("1,Carl");
                writer.WriteLine("2,Mark");
                writer.Flush();
                stream.Position = 0;
                //var dr = parser.AsDataReader();
                //while (dr.Read())
                //{
                //    Console.WriteLine(dr[0]);
                //}
                object row1 = null;

                //parser.Configuration.ColumnCountStrict = true;
                while ((row1 = parser.Read()) != null)
                {
                    Console.WriteLine(row1.ToStringEx());
                }
            }
        }
    }
    public class IntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class NameFormatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return String.Format("{0}zzzz".FormatString(value));
        }
    }

    public class Name1Formatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return String.Format("{0}@@@@".FormatString(value));
        }
    }

    [ChoCSVFileHeader()]
    [ChoCSVRecordObject(Encoding = "Encoding.UTF32", ErrorMode = ChoErrorMode.ReportAndContinue,
    IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any, ThrowAndStopOnMissingField = false,
        ObjectValidationMode = ChoObjectValidationMode.MemberLevel)]
    public class EmployeeRecMeta : IChoNotifyRecordRead //, IChoValidatable
    {
        [ChoCSVRecordField(1, FieldName = "id", ErrorMode = ChoErrorMode.ReportAndContinue)]
        [ChoTypeConverter(typeof(IntConverter))]
        [Range(1, 1, ErrorMessage = "Id must be > 0.")]
        //[ChoFallbackValue(1)]
        public int Id { get; set; }
        [ChoCSVRecordField(2, FieldName = "Name")]
        [StringLength(1)]
        [DefaultValue("ZZZ")]
        [ChoFallbackValue("XXX")]
        [ChoTypeConverter(typeof(NameFormatter))]
        [ChoTypeConverter(typeof(Name1Formatter))]
        public string Name { get; set; }

        public bool AfterRecordFieldLoad(object target, int index, string propName, object value)
        {
            throw new NotImplementedException();
        }

        public bool AfterRecordLoad(object target, int index, object source)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordFieldLoad(object target, int index, string propName, ref object value)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordLoad(object target, int index, ref object source)
        {
            throw new NotImplementedException();
        }

        public bool BeginLoad(object source)
        {
            throw new NotImplementedException();
        }

        public void EndLoad(object source)
        {
            throw new NotImplementedException();
        }

        public bool RecordFieldLoadError(object target, int index, string propName, object value, Exception ex)
        {
            return true;
        }

        public bool RecordLoadError(object target, int index, object source, Exception ex)
        {
            throw new NotImplementedException();
        }

        public bool TryValidate(object target, ICollection<ValidationResult> validationResults)
        {
            return true;
        }

        public bool TryValidateFor(object target, string memberName, ICollection<ValidationResult> validationResults)
        {
            return true;
        }

        public void Validate(object target)
        {
        }

        public void ValidateFor(object target, string memberName)
        {
        }
    }

    //[MetadataType(typeof(EmployeeRecMeta))]
    //[ChoCSVFileHeader()]
    [ChoCSVRecordObject(Encoding = "Encoding.UTF32", ErrorMode = ChoErrorMode.IgnoreAndContinue,
    IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any, ThrowAndStopOnMissingField = false)]
    public partial class EmployeeRec //: IChoNotifyRecordRead, IChoValidatable
    {
        //[ChoCSVRecordField(1, FieldName = "id")]
        //[ChoTypeConverter(typeof(IntConverter))]
        //[Range(1, int.MaxValue, ErrorMessage = "Id must be > 0.")]
        //[ChoFallbackValue(1)]
        public int Id { get; set; }

        //[ChoCSVRecordField(2, FieldName = "Name")]
        //[Required]
        //[DefaultValue("ZZZ")]
        //[ChoFallbackValue("XXX")]
        public string Name { get; set; }

        public int Salary { get; set; }
        //[ChoCSVRecordField(3, FieldName = "Address")]
        //public string Address { get; set; }

        public bool AfterRecordFieldLoad(object target, int index, string propName, object value)
        {
            throw new NotImplementedException();
        }

        public bool AfterRecordLoad(object target, int index, object source)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordFieldLoad(object target, int index, string propName, ref object value)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordLoad(object target, int index, ref object source)
        {
            throw new NotImplementedException();
        }

        public bool BeginLoad(object source)
        {
            throw new NotImplementedException();
        }

        public void EndLoad(object source)
        {
            throw new NotImplementedException();
        }

        public bool RecordFieldLoadError(object target, int index, string propName, object value, Exception ex)
        {
            throw new NotImplementedException();
        }

        public bool RecordLoadError(object target, int index, object source, Exception ex)
        {
            throw new NotImplementedException();
        }

        public bool TryValidate(object target, ICollection<ValidationResult> validationResults)
        {
            return true;
        }

        public bool TryValidateFor(object target, string memberName, ICollection<ValidationResult> validationResults)
        {
            return true;
        }

        public void Validate(object target)
        {
        }

        public void ValidateFor(object target, string memberName)
        {
        }
    }

}
