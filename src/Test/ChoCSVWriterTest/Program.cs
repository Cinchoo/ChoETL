using ChoETL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoCSVWriterTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigFirstApproachWriteDynamicRecordsToFile();
        }

        static void WriteDataTableTest()
        {
            ChoTypeConverterFormatSpec.Instance.Value.DateTimeFormat = "G";
            ChoTypeConverterFormatSpec.Instance.Value.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            //ChoTypeConverterFormatSpec.Instance.Value.EnumFormat = ChoEnumFormatSpec.Name;
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
            ChoTypeConverterFormatSpec.Instance.Value.DateTimeFormat = "G";
            ChoTypeConverterFormatSpec.Instance.Value.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            //ChoTypeConverterFormatSpec.Instance.Value.EnumFormat = ChoEnumFormatSpec.Name;
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
            ChoTypeConverterFormatSpec.Instance.Value.DateTimeFormat = "G";
            ChoTypeConverterFormatSpec.Instance.Value.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            //ChoTypeConverterFormatSpec.Instance.Value.EnumFormat = ChoEnumFormatSpec.Name;

            List<EmployeeRec> objs = new List<EmployeeRec>();
            EmployeeRec rec1 = new EmployeeRec();
            //rec1.Id = 10;
            rec1.Name = "Mark";
            objs.Add(rec1);

            EmployeeRec rec2 = new EmployeeRec();
            rec2.Id = 200;
            rec2.Name = "Lou";
            objs.Add(rec2);

            Console.WriteLine(ChoCSVWriter.ToText(objs));
        }

        static void CodeFirstWithDeclarativeApproachWriteRecords()
        {
            ChoTypeConverterFormatSpec.Instance.Value.DateTimeFormat = "G";
            ChoTypeConverterFormatSpec.Instance.Value.BooleanFormat = ChoBooleanFormatSpec.YesOrNo;
            //ChoTypeConverterFormatSpec.Instance.Value.EnumFormat = ChoEnumFormatSpec.Name;

            List<EmployeeRec> objs = new List<EmployeeRec>();
            EmployeeRec rec1 = new EmployeeRec();
            //rec1.Id = 10;
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
            config.RecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { Validators = new ValidationAttribute[] { new RangeAttribute(3, 100) } });
            config.RecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.ObjectValidationMode = ChoObjectValidationMode.ObjectLevel;

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
            config.RecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1));
            config.RecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));

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
    [ChoCSVRecordObject(HasExcelSeparator = true)]
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
