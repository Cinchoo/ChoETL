using ChoETL;
using NUnit.Framework;
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
using System.IO;

namespace ChoCSVSqlDbImportSample
{
    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            ChoETLFramework.Initialize();
            //POCOSortUsingSqlite();

            POCOSortUsingSqlServer();
        }

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }

        static public string FileNameTestTXT => "Test.txt";
        static public string FileNamePOCOSortUsingSqlServerTestTXT => "POCOSortUsingSqlServerTest.txt";
        static public string FileNamePOCOSortUsingSqlServerExpectedTXT => "POCOSortUsingSqlServerExpected.txt";
        static public string FileNamePOCOSortUsingSqlServerUsingBcpTestTXT => "POCOSortUsingSqlServerUsingBcpTest.txt";
        static public string FileNamePOCOSortUsingSqlServerUsingBcpExpectedTXT => "POCOSortUsingSqlServerUsingBcpExpected.txt";
        static public string FileNameSortUsingSqlServerTestTXT => "SortUsingSqlServerTest.txt";
        static public string FileNameSortUsingSqlServerExpectedTXT => "SortUsingSqlServerExpected.txt";
        
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
        //public static void POCOSortUsingSqlite()
        //{
        //    using (var dr = new ChoCSVReader<Address>(FileNameTestTXT).WithDelimiter("\t").NotifyAfter(10000))
        //    {
        //        dr.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
        //        {
        //            Console.WriteLine();
        //            Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
        //        };
        //        using (var dw = new ChoCSVWriter<Address>(Console.Out))
        //            dw.Write(dr.AsEnumerable().StageOnSQLite().OrderByDescending(x => x.City));
        //    }
        //}

        //public static void SortUsingSqlite()
        //{
        //    using (var dr = new ChoCSVReader(FileNameTestTXT).WithDelimiter("\t").NotifyAfter(10000))
        //    {
        //        dr.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
        //        {
        //            Console.WriteLine();
        //            Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
        //        };
        //        using (var dw = new ChoCSVWriter(Console.Out))
        //            dw.Write(dr.AsEnumerable().StageOnSQLite("ORDER BY Column4"));
        //    }

        //}
        //[Test]
        public static void POCOSortUsingSqlServer()
        {
            var dbFilePath = Path.GetFullPath(@"..\..\..\..\..\data\db\localdb.mdf");
            dbFilePath.Print();

            ChoETLSqlServerSettings settings = new ChoETLSqlServerSettings();
            settings.ConnectionString =  $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={dbFilePath};Integrated Security=True;Connect Timeout=30";

            long? rowsLoaded = null;
            using (var dr = new ChoCSVReader<Address>(FileNameTestTXT).WithDelimiter("\t").NotifyAfter(10000))
            {
                dr.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
                {
                    rowsLoaded = e.RowsLoaded;
                    Console.WriteLine();
                    Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
                };
                using (var sw = new StreamWriter(FileNamePOCOSortUsingSqlServerTestTXT))
                using (var dw = new ChoCSVWriter<Address>(sw))
                    dw.Write(dr.AsEnumerable().StageOnSqlServer(settings).OrderByDescending(x => x.City).OrderBy(x=> x.Id1));
            }

            Assert.Multiple(() => 
            { 
                Assert.AreEqual(rowsLoaded, 101, "Rows loaded");
                
                var expected = File.ReadAllText(FileNamePOCOSortUsingSqlServerExpectedTXT);
                var actual = File.ReadAllText(FileNamePOCOSortUsingSqlServerTestTXT);

                FileAssert.AreEqual(FileNamePOCOSortUsingSqlServerExpectedTXT, FileNamePOCOSortUsingSqlServerTestTXT); 
            });
        }

        //[Test]
        public static void SortUsingSqlServer()
        {
            long? rowsLoaded = null;
            using (var dr = new ChoCSVReader(FileNameTestTXT).WithDelimiter("\t").NotifyAfter(10000))
            {
                dr.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
                {
                    rowsLoaded = e.RowsLoaded;
                    Console.WriteLine();
                    Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
                };
                using (var sw = new StreamWriter(FileNameSortUsingSqlServerTestTXT))
                using (var dw = new ChoCSVWriter(sw))
                    dw.Write(dr.AsEnumerable().StageOnSqlServer("ORDER BY Column4"));
            }

            Assert.Multiple(() => { Assert.AreEqual(rowsLoaded, 101, "Rows loaded"); FileAssert.AreEqual(FileNameSortUsingSqlServerExpectedTXT, FileNameSortUsingSqlServerTestTXT); });
        }

        //[Test]
        public static void POCOSortUsingSqlServerUsingBcp()
        {
            long? rowsLoaded = null;
            using (var dr = new ChoCSVReader<Address>(FileNameTestTXT).WithDelimiter("\t").NotifyAfter(10000))
            {
                dr.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
                {
                    rowsLoaded = e.RowsLoaded;
                    Console.WriteLine();
                    Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
                };
                using (var sw = new StreamWriter(FileNamePOCOSortUsingSqlServerUsingBcpTestTXT))
                using (var dw = new ChoCSVWriter<Address>(sw))
                    dw.Write(dr.AsEnumerable().StageOnSqlServerUsingBcp().OrderByDescending(x => x.City).OrderBy(x => x.Id1));
            }

            Assert.Multiple(() => { Assert.AreEqual(rowsLoaded, 101, "Rows loaded"); FileAssert.AreEqual(FileNamePOCOSortUsingSqlServerUsingBcpExpectedTXT, FileNamePOCOSortUsingSqlServerUsingBcpTestTXT); });
        }

        //public static void SortUsingSqlServerUsingBcp()
        //{
        //    using (var dr = new ChoCSVReader(FileNameTestTXT).WithDelimiter("\t").NotifyAfter(10000))
        //    {
        //        dr.RowsLoaded += delegate (object sender, ChoRowsLoadedEventArgs e)
        //        {
        //            Console.WriteLine();
        //            Console.WriteLine(e.RowsLoaded.ToString("#,##0") + " rows loaded.");
        //        };
        //        using (var dw = new ChoCSVWriter(Console.Out))
        //            dw.Write(dr.AsEnumerable().StageOnSqlServerUsingBcp().AsTypedEnumerable<dynamic>().OrderByDescending());
        //    }
        //}
        //[Test]
        public static void BcpDataFile()
        {
            string connectionstring = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True";
            //using (var db = new NerdDinners(connectionstring))
            //{
            //    //db.Database.CreateIfNotExists();
            //    db.Database.ExecuteSqlCommand("TRUNCATE TABLE Series");
            //}
            DateTime st = DateTime.Now;
            Console.WriteLine("Starting..." + st);
            using (var dr = new ChoCSVReader(@"20170202_CUST_CIF.IN").NotifyAfter(10000))
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
            //    using (var r = new ChoCSVReader<Series>(@"Building consents by territorial authority and selected wards (Monthly).csv").WithFirstLineHeader().NotifyAfter(10000))
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
            Assert.Fail("Provide correct test. Avoid Console.ReadLine() to prevent hanging.");
            Console.ReadLine();
        }

        //[Test]
        public static void BcpDataFile1()
        {
            long? rowsCopied = null;
            long sQLRowsCopiedEventFireCount = 0;

            string connectionstring = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TestDb;Integrated Security=True";
            using (var db = new NerdDinners (connectionstring))
            {
                db.Database.CreateIfNotExists();
                db.Database.ExecuteSqlCommand("TRUNCATE TABLE Customers");
            }

            using (SqlBulkCopy bcp = new SqlBulkCopy(connectionstring))
            {
                using (var dr = new ChoCSVReader<Customer>(FileNameTestTXT).WithDelimiter("\t").AsDataReader())
                {
                    bcp.DestinationTableName = "dbo.Customers";
                    bcp.EnableStreaming = true;

                    bcp.NotifyAfter = 10;
                    bcp.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs e)
                    {
                        sQLRowsCopiedEventFireCount++;
                        rowsCopied = e.RowsCopied;
                        Console.WriteLine(e.RowsCopied.ToString("#,##0") + " rows copied.");
                    };
                    
                    bcp.WriteToServer(dr);
                }
            }

            Assert.Multiple(() => { Assert.AreEqual(10, sQLRowsCopiedEventFireCount); Assert.AreEqual(100, rowsCopied); });
        }

        //[Test]
        public static void SortByCity()
        {
            List<Customer> expected = new List<Customer> {
new Customer { Id = 35, Street = "Pascalstr 951", City = "Berlin", Zip = "14111"},
new Customer { Id = 34, Street = "94, rue Descartes", City = "Bordeaux", Zip = "33000"},
new Customer { Id = 1, Street = "1970 Napa Ct.", City = "Bothell", Zip = "98011"},
new Customer { Id = 2, Street = "9833 Mt. Dias Blv.", City = "Bothell", Zip = "98011"},
new Customer { Id = 3, Street = "7484 Roundtree Drive", City = "Bothell", Zip = "98011"},
new Customer { Id = 4, Street = "9539 Glenside Dr", City = "Bothell", Zip = "98011"},
new Customer { Id = 5, Street = "1226 Shoe St.", City = "Bothell", Zip = "98011"},
new Customer { Id = 6, Street = "1399 Firestone Drive", City = "Bothell", Zip = "98011"},
new Customer { Id = 7, Street = "5672 Hale Dr.", City = "Bothell", Zip = "98011"},
new Customer { Id = 8, Street = "6387 Scenic Avenue", City = "Bothell", Zip = "98011"},
new Customer { Id = 9, Street = "8713 Yosemite Ct.", City = "Bothell", Zip = "98011"},
new Customer { Id = 10, Street = "250 Race Court", City = "Bothell", Zip = "98011"},
new Customer { Id = 11, Street = "1318 Lasalle Street", City = "Bothell", Zip = "98011"},
new Customer { Id = 12, Street = "5415 San Gabriel Dr.", City = "Bothell", Zip = "98011"},
new Customer { Id = 13, Street = "9265 La Paz", City = "Bothell", Zip = "98011"},
new Customer { Id = 14, Street = "8157 W. Book", City = "Bothell", Zip = "98011"},
new Customer { Id = 15, Street = "4912 La Vuelta", City = "Bothell", Zip = "98011"},
new Customer { Id = 16, Street = "40 Ellis St.", City = "Bothell", Zip = "98011"},
new Customer { Id = 17, Street = "6696 Anchor Drive", City = "Bothell", Zip = "98011"},
new Customer { Id = 18, Street = "1873 Lion Circle", City = "Bothell", Zip = "98011"},
new Customer { Id = 19, Street = "3148 Rose Street", City = "Bothell", Zip = "98011"},
new Customer { Id = 20, Street = "6872 Thornwood Dr.", City = "Bothell", Zip = "98011"},
new Customer { Id = 21, Street = "5747 Shirley Drive", City = "Bothell", Zip = "98011"},
new Customer { Id = 40, Street = "1902 Santa Cruz", City = "Bothell", Zip = "98011"},
new Customer { Id = 33, Street = "10203 Acorn Avenue", City = "Calgary", Zip = "T2P 2G8"},
new Customer { Id = 37, Street = "Downshire Way", City = "Cambridge", Zip = "BA5 3HX"},
new Customer { Id = 39, Street = "3997 Via De Luna", City = "Cambridge", Zip = "02139"},
new Customer { Id = 86, Street = "390 Ridgewood Ct.", City = "Carnation", Zip = "98014"},
new Customer { Id = 87, Street = "1411 Ranch Drive", City = "Carnation", Zip = "98014"},
new Customer { Id = 88, Street = "9666 Northridge Ct.", City = "Carnation", Zip = "98014"},
new Customer { Id = 89, Street = "3074 Arbor Drive", City = "Carnation", Zip = "98014"},
new Customer { Id = 90, Street = "9752 Jeanne Circle", City = "Carnation", Zip = "98014"},
new Customer { Id = 25, Street = "9178 Jumping St.", City = "Dallas", Zip = "75201"},
new Customer { Id = 38, Street = "8154 Via Mexico", City = "Detroit", Zip = "48226"},
new Customer { Id = 24, Street = "80 Sunview Terrace", City = "Duluth", Zip = "55802"},
new Customer { Id = 76, Street = "2598 La Vista Circle", City = "Duvall", Zip = "98019"},
new Customer { Id = 77, Street = "9693 Mellowood Street", City = "Duvall", Zip = "98019"},
new Customer { Id = 78, Street = "1825 Corte Del Prado", City = "Duvall", Zip = "98019"},
new Customer { Id = 79, Street = "5086 Nottingham Place", City = "Duvall", Zip = "98019"},
new Customer { Id = 80, Street = "3977 Central Avenue", City = "Duvall", Zip = "98019"},
new Customer { Id = 81, Street = "8209 Green View Court", City = "Duvall", Zip = "98019"},
new Customer { Id = 82, Street = "8463 Vista Avenue", City = "Duvall", Zip = "98019"},
new Customer { Id = 83, Street = "5379 Treasure Island Way", City = "Duvall", Zip = "98019"},
new Customer { Id = 84, Street = "3421 Bouncing Road", City = "Duvall", Zip = "98019"},
new Customer { Id = 85, Street = "991 Vista Verde", City = "Duvall", Zip = "98019"},
new Customer { Id = 41, Street = "793 Crawford Street", City = "Kenmore", Zip = "98028"},
new Customer { Id = 42, Street = "463 H Stagecoach Rd.", City = "Kenmore", Zip = "98028"},
new Customer { Id = 43, Street = "5203 Virginia Lane", City = "Kenmore", Zip = "98028"},
new Customer { Id = 44, Street = "4095 Cooper Dr.", City = "Kenmore", Zip = "98028"},
new Customer { Id = 45, Street = "6697 Ridge Park Drive", City = "Kenmore", Zip = "98028"},
new Customer { Id = 46, Street = "5669 Ironwood Way", City = "Kenmore", Zip = "98028"},
new Customer { Id = 47, Street = "8192 Seagull Court", City = "Kenmore", Zip = "98028"},
new Customer { Id = 48, Street = "5553 Cash Avenue", City = "Kenmore", Zip = "98028"},
new Customer { Id = 49, Street = "7048 Laurel", City = "Kenmore", Zip = "98028"},
new Customer { Id = 50, Street = "25 95th Ave NE", City = "Kenmore", Zip = "98028"},
new Customer { Id = 36, Street = "34 Waterloo Road", City = "Melbourne", Zip = "3000"},
new Customer { Id = 29, Street = "8291 Crossbow Way", City = "Memphis", Zip = "38103"},
new Customer { Id = 61, Street = "7726 Driftwood Drive", City = "Monroe", Zip = "98272"},
new Customer { Id = 62, Street = "3841 Silver Oaks Place", City = "Monroe", Zip = "98272"},
new Customer { Id = 63, Street = "9652 Los Angeles", City = "Monroe", Zip = "98272"},
new Customer { Id = 64, Street = "4566 La Jolla", City = "Monroe", Zip = "98272"},
new Customer { Id = 65, Street = "1356 Grove Way", City = "Monroe", Zip = "98272"},
new Customer { Id = 66, Street = "4775 Kentucky Dr.", City = "Monroe", Zip = "98272"},
new Customer { Id = 67, Street = "4734 Sycamore Court", City = "Monroe", Zip = "98272"},
new Customer { Id = 68, Street = "896 Southdale", City = "Monroe", Zip = "98272"},
new Customer { Id = 69, Street = "2275 Valley Blvd.", City = "Monroe", Zip = "98272"},
new Customer { Id = 70, Street = "1792 Belmont Rd.", City = "Monroe", Zip = "98272"},
new Customer { Id = 71, Street = "5734 Ashford Court", City = "Monroe", Zip = "98272"},
new Customer { Id = 72, Street = "5030 Blue Ridge Dr.", City = "Monroe", Zip = "98272"},
new Customer { Id = 73, Street = "158 Walnut Ave", City = "Monroe", Zip = "98272"},
new Customer { Id = 74, Street = "8310 Ridge Circle", City = "Monroe", Zip = "98272"},
new Customer { Id = 75, Street = "3747 W. Landing Avenue", City = "Monroe", Zip = "98272"},
new Customer { Id = 32, Street = "26910 Indela Road", City = "Montreal", Zip = "H1Y 2H5"},
new Customer { Id = 27, Street = "2487 Riverside Drive", City = "Nevada", Zip = "84407"},
new Customer { Id = 30, Street = "9707 Coldwater Drive", City = "Orlando", Zip = "32804"},
new Customer { Id = 31, Street = "9100 Sheppard Avenue North", City = "Ottawa", Zip = "K4B 1T7"},
new Customer { Id = 28, Street = "9228 Via Del Sol", City = "Phoenix", Zip = "85004"},
new Customer { Id = 22, Street = "636 Vine Hill Way", City = "Portland", Zip = "97205"},
new Customer { Id = 26, Street = "5725 Glaze Drive", City = "San Francisco", Zip = "94109"},
new Customer { Id = 23, Street = "6657 Sand Pointe Lane", City = "Seattle", Zip = "98104"},
new Customer { Id = 91, Street = "7166 Brock Lane", City = "Seattle", Zip = "98104"},
new Customer { Id = 92, Street = "7126 Ending Ct.", City = "Seattle", Zip = "98104"},
new Customer { Id = 93, Street = "4598 Manila Avenue", City = "Seattle", Zip = "98104"},
new Customer { Id = 94, Street = "5666 Hazelnut Lane", City = "Seattle", Zip = "98104"},
new Customer { Id = 95, Street = "1220 Bradford Way", City = "Seattle", Zip = "98104"},
new Customer { Id = 96, Street = "5375 Clearland Circle", City = "Seattle", Zip = "98104"},
new Customer { Id = 97, Street = "2639 Anchor Court", City = "Seattle", Zip = "98104"},
new Customer { Id = 98, Street = "502 Alexander Pl.", City = "Seattle", Zip = "98104"},
new Customer { Id = 99, Street = "5802 Ampersand Drive", City = "Seattle", Zip = "98104"},
new Customer { Id = 100, Street = "5125 Cotton Ct.", City = "Seattle", Zip = "98104"},
new Customer { Id = 101, Street = "3243 Buckingham Dr.", City = "Seattle", Zip = "98104"},
new Customer { Id = 51, Street = "3280 Pheasant Circle", City = "Snohomish", Zip = "98296"},
new Customer { Id = 52, Street = "4231 Spar Court", City = "Snohomish", Zip = "98296"},
new Customer { Id = 53, Street = "1285 Greenbrier Street", City = "Snohomish", Zip = "98296"},
new Customer { Id = 54, Street = "5724 Victory Lane", City = "Snohomish", Zip = "98296"},
new Customer { Id = 55, Street = "591 Merriewood Drive", City = "Snohomish", Zip = "98296"},
new Customer { Id = 56, Street = "3114 Notre Dame Ave.", City = "Snohomish", Zip = "98296"},
new Customer { Id = 57, Street = "7230 Vine Maple Street", City = "Snohomish", Zip = "98296"},
new Customer { Id = 58, Street = "2601 Cambridge Drive", City = "Snohomish", Zip = "98296"},
new Customer { Id = 59, Street = "2115 Passing", City = "Snohomish", Zip = "98296"},
new Customer { Id = 60, Street = "4852 Chaparral Court", City = "Snohomish", Zip = "98296"}
            };
            List<object> actual = new List<object>();

            foreach (var e in new ChoCSVReader<Customer>(FileNameTestTXT).WithDelimiter("\t").ExternalSort(new AddressCityIdComparer(), 10, 10))
                actual.Add(e);
            //Console.WriteLine(e.ToStringEx());

            CollectionAssert.AreEqual(expected, actual);
        }

        public class AddressCityIdComparer : IComparer<Customer>
        {
            public int Compare(Customer x, Customer y)
            {
                var tmp = String.Compare(x.City, y.City);
                if (tmp != 0)
                    return tmp;
                return x.Id.CompareTo(y.Id);
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

            public override bool Equals(object obj)
            {
                var customer = obj as Customer;
                return customer != null &&
                       Id == customer.Id &&
                       Street == customer.Street &&
                       City == customer.City &&
                       Zip == customer.Zip;
            }

            public override int GetHashCode()
            {
                var hashCode = 719180356;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Street);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(City);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Zip);
                return hashCode;
            }

            /*
            public override bool Equals(object obj)
            {
                Customer compareObject = obj as Customer;
                if (compareObject != null)
                    return this.Id.Equals(compareObject.Id) &&
                        this.Street.Equals(compareObject.Street) &&
                        this.City.Equals(compareObject.City) &&
                        this.Zip.Equals(compareObject.Zip);
                return base.Equals(obj);
            }
            public override int GetHashCode()
            {

                return base.GetHashCode();
            }
            */
        }
        public class NerdDinners  : DbContext
        {
            public NerdDinners (string connString) : base(connString)
            {

            }

            public DbSet<Customer> Customers { get; set; }
        }

        //[Test]
        public static void LoadDataFile()
        {
            int recordsAffected;
            string connectionstring = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=TestDb;Integrated Security=True";

            using (var db = new NerdDinners(connectionstring))
            {
                db.Database.CreateIfNotExists();
                db.Database.ExecuteSqlCommand("TRUNCATE TABLE Customers");

                foreach (var e in new ChoCSVReader<Customer>(FileNameTestTXT).WithDelimiter("\t"))
                    db.Customers.AddOrUpdate(e);

                recordsAffected = db.SaveChanges();
                Console.WriteLine($"Total inserted: {recordsAffected}");
            }

            Assert.AreEqual(101, recordsAffected);
        }
    }
}
