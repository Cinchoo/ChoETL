using ChoETL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoFixedLengthReaderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigFirstApproachReadAsTypedRecords();
        }

        static void CodeFirstWithDeclarativeApproachReadRecords()
        {
            EmployeeRec row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRec>(reader))
            {
                writer.WriteLine("001Carl 08/12/2016$100,000  0F");
                writer.WriteLine("002MarkS01/01/2010$500,000  1C");
                writer.Flush();
                stream.Position = 0;

                while ((row = parser.Read()) != null)
                {
                    Console.WriteLine(row.ToStringEx());
                }
            }
        }

        static void CodeFirstApproachReadRecords()
        {
            //Override the width of necessary simple types
            ChoETLDataTypeSize.Global.SetSize(typeof(int), 3);
            ChoETLDataTypeSize.Global.SetSize(typeof(string), 5);

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

        static void ConfigFirstApproachReadAsDynamicRecords()
        {
            ChoFixedLengthRecordConfiguration config = new ChoFixedLengthRecordConfiguration();
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Id", 0, 3) { FieldType = typeof(int) });
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Name", 3, 5) { FieldType = typeof(string) });
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("JoinedDate", 8, 10) { FieldType = typeof(DateTime) });
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Salary", 18, 10) { FieldType = typeof(ChoCurrency) });
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("IsActive", 28, 1) { FieldType = typeof(bool) });
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Status", 29, 1) { FieldType = typeof(char) });

            ExpandoObject row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader(reader, config))
            {
                writer.WriteLine("001Carl 08/12/2016100,000   0F");
                writer.WriteLine("002MarkS01/01/2010500,000   1C");
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
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Id", 0, 3) { FieldType = typeof(int) });
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Name", 3, 5) { FieldType = typeof(string) });
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("JoinedDate", 8, 10) { FieldType = typeof(DateTime) });
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Salary", 18, 10) { FieldType = typeof(ChoCurrency) });
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("IsActive", 28, 1) { FieldType = typeof(bool) });
            config.RecordFieldConfigurations.Add(new ChoFixedLengthRecordFieldConfiguration("Status", 29, 1) { FieldType = typeof(char) });

            EmployeeRecSimple row = null;
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoFixedLengthReader<EmployeeRecSimple>(reader, config))
            {
                writer.WriteLine("001Carl 08/12/2016100,000   0F");
                writer.WriteLine("002MarkS01/01/2010500,000   1C");
                writer.WriteLine("003Tom                      1C");
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
        [DefaultValue("1/1/2001")]
        public DateTime JoinedDate { get; set; }
        [DefaultValue("50000")]
        public ChoCurrency Salary { get; set; }
        public bool IsActive { get; set; }
        public char Status { get; set; }
    }

    [ChoFixedLengthRecordObject(30)]
    public partial class EmployeeRec
    {
        [ChoFixedLengthRecordField(0, 3)]
        public int Id { get; set; }
        [ChoFixedLengthRecordField(3, 5)]
        public string Name { get; set; }
        [ChoFixedLengthRecordField(8, 10)]
        public DateTime JoinedDate { get; set; }
        [ChoFixedLengthRecordField(18, 10)]
        public ChoCurrency Salary { get; set; }
        [ChoFixedLengthRecordField(28, 1)]
        public bool IsActive { get; set; }
        [ChoFixedLengthRecordField(29, 1)]
        public char Status { get; set; }
    }
}
