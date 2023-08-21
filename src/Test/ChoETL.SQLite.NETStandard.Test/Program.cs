using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace ChoETL.SQLite.NETStandard.Test
{
    class Program
    {
        public class Emp
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string City { get; set; }
        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            CSVToDataTableUsingLiteParser();
        }

        static void CSVToDataTableUsingLiteParser()
        {
            string filePath = @"C:\Projects\GitHub\ChoETL\data\XBTUSD.csv";
            ChoCSVLiteReader parser = new ChoCSVLiteReader();
           
            var items = parser.ReadFile<Trade>(filePath, mapper: (lineNo, cols, rec) =>
             {
                 rec.Id = cols[0];
                 rec.Price = cols[1].CastTo<double>();
                 rec.Quantity = cols[2].CastTo<double>();
             }).Take(10000);
            var dt = items.AsDataTable();
            dt.Print();
        }

        public class USDBitCoin
        {
            public int Id { get; set; }
            public double Price { get; set; }
            public double Qty { get; set; }
        }

        static void StageLargeFile()
        {
            int c = 0;
            var parser = new ChoCSVLiteReader();
            Stopwatch w2 = Stopwatch.StartNew();
            using (var r = new StreamReader(@"..\..\..\..\..\..\data\XBTUSD.csv"))
            {
                foreach (var rec in parser.Read(r, ',').Take(1000000))
                {
                    //rec.Print();
                    c++;
                    if (c % 100000 == 0)
                        $"Rows loaded: {c}".Print();

                }
            }
            w2.Stop();
            $"StreamReader: {w2.Elapsed}".Print();


            //ChoTypeDescriptor.DoNotUseTypeConverterForTypes = true;
            for (int i = 2; i < 3; i++)
            {
                var watch = Stopwatch.StartNew();
                List<Trade> trades = null;

                using (var r = new ChoCSVReader(@"..\..\..\..\..\..\data\XBTUSD.csv")
                    .Configure(c => c.NotifyAfter = 100000)
                    .Setup(s => s.RowsLoaded += (o, e) =>
                    {
                        $"Rows Loaded: {e.RowsLoaded} <-- {DateTime.Now}".Print();
                    })
                    //.Configure(c => c.LiteParsing = true)
                    )
                {
                    //r.Take(1).Print();
                    //return;
                    var trades1 = r.Take(1000000).ToList(); //.Count().Print();
                    //return;
                    //r.Take(1000000).StageOnSQLite(new ChoETLSqliteSettings()
                    //    .Configure(c => c.ConnectionString = "DataSource=local.db;Version=3;Synchronous=OFF;Journal Mode=OFF")
                    //    .Configure(c => c.NotifyAfter = 500000)
                    //    .Configure(c => c.BatchSize = 500000)
                    //    .Configure(c => c.RowsUploaded += (o, e) =>
                    //    {
                    //        Console.WriteLine($"Rows uploaded: {e.RowsUploaded}");
                    //    }));
                    //trades = r.Take(1000000).Select(r => new Trade { Id = r.Column1, Price = r.Column2, Quantity = r.Column3 }).ToList();
                }
                watch.Stop();
                watch.Elapsed.Print();
                return;
                watch = Stopwatch.StartNew();

                trades.StageOnSQLite(new ChoETLSqliteSettings()
                    .Configure(c => c.ConnectionString = "DataSource=local.db;Version=3;Synchronous=OFF;Journal Mode=OFF")
                    .Configure(c => c.NotifyAfter = 500000)
                    .Configure(c => c.BatchSize = 500000)
                    .Configure(c => c.RowsUploaded += (o, e) =>
                    {
                        Console.WriteLine($"Rows uploaded: {e.RowsUploaded}");
                    }));

                watch.Stop();
                watch.Elapsed.Print();
            }
        }
        static void StageJSONFile()
        {
            string json = @"
    [
        {
            ""Id"": 1,
            ""Name"": ""Polo"",
            ""City"": ""New York""
        },
        {
            ""Id"": 2,
            ""Name"": ""328"",
            ""City"": ""Edison""
        }
    ]";
            ChoETLFrxBootstrap.TraceLevel = TraceLevel.Error;
            using (var r = ChoJSONReader<Emp>.LoadText(json)
                )
            {
                r.StageOnSQLite().Where(e => e.Id == 2).Print();
            }

        }

        static void StageCSVFile()
        {
            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";

            using (var r = ChoCSVReader<Emp>.LoadText(csv)
                .WithFirstLineHeader())
            {
                r.StageOnSQLite();
            }

        }
    }
    public class Trade
    {
        public string Id { get; set; }
        public double Price { get; set; }
        public double Quantity { get; set; }
    }
}
