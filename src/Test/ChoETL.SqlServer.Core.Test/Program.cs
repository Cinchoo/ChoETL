using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace ChoETL.SqlServer.Core.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            StageLargeFile();
        }
        static void StageLargeFile()
        {
            //ChoTypeDescriptor.DoNotUseTypeConverterForTypes = true;
            for (int i = 0; i < 5; i++)
            {
                var watch = Stopwatch.StartNew();
                List<Trade> trades = null;

                //using (MemoryMappedFile memoryMappedFile = MemoryMappedFile.CreateFromFile(@"..\..\..\..\..\..\data\XBTUSD.csv"))
                //using (MemoryMappedViewStream memoryMappedViewStream = memoryMappedFile.CreateViewStream(0, 0, MemoryMappedFileAccess.Read))
                //{
                //    using (var r = new ChoCSVReader<Trade>(memoryMappedViewStream)
                //        .Configure(c => c.NotifyAfter = 100000)
                //        .Setup(s => s.RowsLoaded += (o, e) =>
                //        {
                //            $"Rows Loaded: {e.RowsLoaded} <-- {DateTime.Now}".Print();
                //        })
                //        .Configure(c => c.LiteParsing = true)
                //        )
                //    {
                //        trades = r.Take(100000).ToList(); //.Count().Print();
                //    }
                //}

                using (var r = new ChoCSVReader<Trade>(@"..\..\..\..\..\..\data\XBTUSD.csv")
                .Configure(c => c.NotifyAfter = 100000)
                .Setup(s => s.RowsLoaded += (o, e) =>
                {
                    $"Rows Loaded: {e.RowsLoaded} <-- {DateTime.Now}".Print();
                })
                //.Configure(c => c.LiteParsing = true)
                //.Configure(c => c.BufferSize = 1024 * 1024)
                //.Configure(c => c.TurnOffMemoryMappedFile = true)
                )
                {
                    //r.Take(1).Print();
                    //return;
                    trades = r.Take(1000000).ToList(); //.Count().Print();
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

                //watch = Stopwatch.StartNew();

                //trades.StageOnSqlServer(new ChoETLSqlServerSettings()
                //    //.Configure(c => c.ConnectionString = "DataSource=local.db;Version=3;Synchronous=OFF;Journal Mode=OFF")
                //    .Configure(c => c.NotifyAfter = 500000)
                //    //.Configure(c => c.BatchSize = 500000)
                //    .Configure(c => c.RowsUploaded += (o, e) =>
                //    {
                //        Console.WriteLine($"Rows uploaded: {e.RowsUploaded}");
                //    }));

                //watch.Stop();
                //watch.Elapsed.Print();
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
