using ChoETL;
using System;
using System.Data;
using System.Text;
using System.Linq;

namespace ChoParquetReaderTest
{
    class Program
    {
        static void Test1()
        {
            StringBuilder csv = new StringBuilder();
            using (var r = new ChoParquetReader(@"C:\Projects\GitHub\ChoETL\src\Test\ChoParquetWriterTest\bin\Debug\netcoreapp2.1\test1.parquet")
                .ParquetOptions(o => o.TreatByteArrayAsString = true))
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .UseNestedKeyFormat(false)
                    )
                    w.Write(r);
            }

            Console.WriteLine(csv.ToString());
        }
        static void DataTableTest()
        {
            StringBuilder csv = new StringBuilder();
            using (var r = new ChoParquetReader(@"C:\Projects\GitHub\ChoETL\src\Test\ChoParquetWriterTest\bin\Debug\netcoreapp2.1\test1.parquet")
                .ParquetOptions(o => o.TreatByteArrayAsString = true))
            {
                var dt = r.AsDataTable();
            }

            Console.WriteLine(csv.ToString());
        }

        static void ByteArrayTest()
        {
            StringBuilder csv = new StringBuilder();
            using (var r = new ChoParquetReader(@"C:\Projects\GitHub\ChoETL\src\Test\ChoParquetWriterTest\bin\Debug\netcoreapp2.1\ByteArrayTest.parquet")
                .ParquetOptions(o => o.TreatByteArrayAsString = true)
                )
            {
                var dt = r.AsDataTable("x");
                Console.WriteLine(ChoJSONWriter.ToText(dt));
                return;
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .UseNestedKeyFormat(false)
                    )
                    w.Write(r);
            }

            Console.WriteLine(csv.ToString());
        }

        static void ReadParquet52()
        {
            using (var r = new ChoParquetReader("myData52.parquet"))
            {
                foreach (var rec in r.Take(1))
                    Console.WriteLine(rec.Dump());
            }
        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            ReadParquet52();
        }
    }
}
