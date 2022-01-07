using System;
using System.Collections.Generic;
using System.Diagnostics;
using ChoETL;
using System.Linq;

namespace ChoETL.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = TraceLevel.Off;
            CSV2JSON();
        }
        public class Trade
        {
            public string Id { get; set; }
            public double Price { get; set; }
            public double Quantity { get; set; }
        }

        static void CSV2JSON()
        {
            string filePath = @"C:\Projects\GitHub\ChoETL\data\XBTUSD.csv";
            ChoCSVLiteReader parser = new ChoCSVLiteReader();

            using (var w = new ChoJSONWriter<Trade>(@"C:\Projects\GitHub\ChoETL\data\XBTUSD.json")
                .NotifyAfter(100000)
                .Setup(s => s.RowsWritten += (o, e) => $"Rows written: {e.RowsWritten}.".Print())
                )
            {
                w.Write(parser.ReadFile<Trade>(filePath, mapper: (lineNo, cols, rec) =>
                {
                    rec.Id = cols[0];
                    rec.Price = cols[1].CastTo<double>();
                    rec.Quantity = cols[2].CastTo<double>();
                }));
            }
        }

        static void ToDataTableFromDictionary()
        {
            var data = TestClassGenerator.GetTestEnumerable2(100000).Select(e => e.ToSimpleDictionary());

            for (int i = 0; i < 10; i++)
            {
                Stopwatch w = Stopwatch.StartNew();
                var dt = data.AsDataTable();
                //dt.Print();
                //break;
                w.Stop();
                w.ElapsedMilliseconds.ToString().Print();
            }
        }

        static void ToDataTableFromNullableValueType()
        {
            List<int?> list = new List<int?>
            {
                1,
                null,
                2
            };

            var dt = list.AsDataTable();
            dt.Print();
        }

        static void ToDataTableFromValueType()
        {
            List<string> list = new List<string>
            {
                "Tom",
                "Mark",
            };

            var dt = list.AsDataTable();
            dt.Print();
        }

        static void ToDataTableTest1()
        {
            var data = TestClassGenerator.GetTestEnumerable(100000);

            for (int i = 0; i < 10; i++)
            {
                Stopwatch w = Stopwatch.StartNew();
                var dt = data.AsDataTable();
                //dt.Print();
                //break;
                w.Stop();
                w.ElapsedMilliseconds.ToString().Print();
            }
        }
    }
}
