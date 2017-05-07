using ChoETL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Migrations;
using System.Data.SqlClient;
using System.ComponentModel;

namespace ChoCSVSqlDbImportSample
{
    class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            ChoETLFramework.Initialize();
            POCOSortUsingSqlite();

            //LoadDataFile();
        }

        public class Series
        {
            [DefaultValue("XX")]
            public string Series_reference { get; set; }
            public string Period { get; set; }
            public string Data_value { get; set; }
            public string Suppressed { get; set; }
            public string Status { get; set; }
            public string Units { get; set; }
            public string Magnitude { get; set; }
            public string Subject { get; set; }
            public string Group { get; set; }
            public string Series_title_1 { get; set; }
            public string Series_title_2 { get; set; }
            public string Series_title_3 { get; set; }
            public string Series_title_4 { get; set; }
            public string Series_title_5 { get; set; }
        }

        public class Address
        {
            [ChoCSVRecordField(1)]
            public int Id1
            {
                get;
                set;
            }

            [ChoCSVRecordField(2)]
            public string Street
            {
                get;
                set;
            }
            [ChoCSVRecordField(4)]
            public string City
            {
                get;
                set;
            }
        }
        public static void POCOSortUsingSqlite()
        {
            //using (var dr = new ChoCSVReader(@"Test.txt").WithDelimiter("\t").
            //    WithFields("Id", "Street","Filler1", "City").NotifyAfter(10000))
            //{
            //    dr.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
            //    {
            //        Console.WriteLine();
            //        Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
            //    };
            //    using (var dw = new ChoCSVWriter<Address>(Console.Out))
            //        dw.Write(dr.AsEnumerable().CastEnumerable<Address>().StageOnSqlServer().GroupBy(x => x.City).Select(y => y.FirstOrDefault()));
            //}
            using (var dr = new ChoCSVReader<Address>(@"Test.txt").WithDelimiter("\t").NotifyAfter(10000))
            {
                dr.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
                {
                    Console.WriteLine();
                    Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
                };
                using (var dw = new ChoCSVWriter<Address>(Console.Out))
                    dw.Write(dr.AsEnumerable().StageOnSQLite().OrderByDescending(x => x.City));
            }
        }

        public static void SortUsingSqlite()
        {
            using (var dr = new ChoCSVReader(@"Test.txt").WithDelimiter("\t").NotifyAfter(10000))
            {
                dr.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
                {
                    Console.WriteLine();
                    Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
                };
                using (var dw = new ChoCSVWriter(Console.Out))
                    dw.Write(dr.AsEnumerable().StageOnSQLite("ORDER BY Column4"));
            }

        }

        static void BcpDataFile()
        {
            string connectionstring = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True";
            //using (var db = new NerdDinners(connectionstring))
            //{
            //    //db.Database.CreateIfNotExists();
            //    db.Database.ExecuteSqlCommand("TRUNCATE TABLE Series");
            //}
            DateTime st = DateTime.Now;
            Console.WriteLine("Starting..." + st);

            using (var dr = new ChoCSVReader(@"C:\Personal\LabCorpTest\bin\Debug\20170202_CUST_CIF.IN").NotifyAfter(10000))
            {
                dr.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
                {
                    Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
                };
                dr.Loop();
                //foreach (var item in dr.Take(100))
                //{
                //    Console.WriteLine(item.ToStringEx());
                //}
            }

            //using (SqlBulkCopy bcp = new SqlBulkCopy(connectionstring))
            //{
            //    using (var r = new ChoCSVReader<Series>(@"C:\Users\raj\Desktop\Building consents by territorial authority and selected wards (Monthly).csv").WithFirstLineHeader().NotifyAfter(10000))
            //    {
            //        r.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
            //        {
            //            Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
            //        };
            //        using (var dr = r.AsDataReader())
            //        {
            //            bcp.DestinationTableName = "dbo.Series";
            //            bcp.EnableStreaming = true;

            //            bcp.BatchSize = 10000;
            //            bcp.BulkCopyTimeout = 0;
            //            //bcp.NotifyAfter = 10000;
            //            //bcp.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs e)
            //            //{
            //            //    Console.WriteLine(e.RowsCopied.ToString("#,##0") + " rows copied.");
            //            //};
            //            bcp.WriteToServer(dr);
            //        }
            //    }
            //}
            Console.WriteLine("Completed."+ (DateTime.Now - st));
            Console.ReadLine();
        }

        static void BcpDataFile1()
        {
            string connectionstring = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True";
            using (var db = new NerdDinners (connectionstring))
            {
                db.Database.CreateIfNotExists();
                db.Database.ExecuteSqlCommand("TRUNCATE TABLE Customers");
            }

            using (SqlBulkCopy bcp = new SqlBulkCopy(connectionstring))
            {
                using (var dr = new ChoCSVReader<Customer>("Test.txt").WithDelimiter("\t").AsDataReader())
                {
                    bcp.DestinationTableName = "dbo.Customers";
                    bcp.EnableStreaming = true;

                    bcp.NotifyAfter = 10;
                    bcp.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs e)
                    {
                        Console.WriteLine(e.RowsCopied.ToString("#,##0") + " rows copied.");
                    };
                    bcp.WriteToServer(dr);
                }
            }
        }

        static void SortByCity()
        {
            foreach (var e in new ChoCSVReader<Customer>("Test.txt").WithDelimiter("\t").ExternalSort(new AddressCityComparer(), 10, 10))
                Console.WriteLine(e.ToStringEx());
        }

        public class AddressCityComparer : IComparer<Customer>
        {
            public int Compare(Customer x, Customer y)
            {
                return String.Compare(x.City, y.City);
            }
        }
        [Serializable]
        public class Customer
        {
            [ChoCSVRecordField(1)]
            [Key]
            public int Id { get; set; }
            [ChoCSVRecordField(2)]
            public string Street { get; set; }
            [ChoCSVRecordField(4)]
            public string City { get; set; }
            [ChoCSVRecordField(6)]
            public string Zip { get; set; }
        }
        public class NerdDinners  : DbContext
        {
            public NerdDinners (string connString) : base(connString)
            {

            }

            public DbSet<Customer> Customers { get; set; }
        }
        static void LoadDataFile()
        {
            string connectionstring = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True";

            using (var db = new NerdDinners (connectionstring))
            {
                db.Database.CreateIfNotExists();
                db.Database.ExecuteSqlCommand("TRUNCATE TABLE Customers");

                foreach (var e in new ChoCSVReader<Customer>("Test.txt").WithDelimiter("\t"))
                    db.Customers.AddOrUpdate(e);

                int recordsAffected = db.SaveChanges();
                Console.WriteLine($"Total inserted: {recordsAffected}");
            }
        }
    }
}
