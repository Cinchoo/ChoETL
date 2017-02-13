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

namespace ChoCSVSqlDbImportSample
{
    class Program
    {
        static void Main(string[] args)
        {
            BcpDataFile();

            //LoadDataFile();
        }
        static void BcpDataFile()
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
