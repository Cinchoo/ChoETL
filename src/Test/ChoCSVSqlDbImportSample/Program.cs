using ChoETL;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.Migrations;

namespace ChoCSVSqlDbImportSample
{
    class Program
    {
        static void Main(string[] args)
        {
            ChoETLFramework.Initialize();
            SortByCity();
        }

        static void SortByCity()
        {
            foreach (var e in new ChoCSVReader<Address>("Test.txt").WithDelimiter("\t").ExternalSort(new AddressCityComparer(), 10, 10))
                Console.WriteLine(e.ToStringEx());
        }

        public class AddressCityComparer : IComparer<Address>
        {
            public int Compare(Address x, Address y)
            {
                return String.Compare(x.City, y.City);
            }
        }
        [Serializable]
        public class Address
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
        public class NerdDinners : DbContext
        {
            public NerdDinners(string connString) : base(connString)
            {

            }

            public DbSet<Address> Addresses { get; set; }
        }
        static void LoadTabFile()
        {
            string connectionstring = @"Data Source=(localdb)\v11.0;Initial Catalog=NerdDinnersDb;Integrated Security=True";

            using (var db = new NerdDinners(connectionstring))
            {
                db.Database.CreateIfNotExists();
                db.Database.ExecuteSqlCommand("TRUNCATE TABLE Addresses");

                foreach (var e in new ChoCSVReader<Address>("Test.txt").WithDelimiter("\t"))
                    db.Addresses.AddOrUpdate(e);

                int recordsAffected = db.SaveChanges();
                Console.WriteLine($"Total inserted: {recordsAffected}");
            }
        }
    }
}
