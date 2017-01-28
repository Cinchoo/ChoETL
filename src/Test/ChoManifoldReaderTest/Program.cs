using ChoETL;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoManifoldReaderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            LoadTest();
        }

        static void QuickTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoManifoldReader(reader))
            {
                parser.Configuration.RecordSelector = (l) => typeof(ExpandoObject);
                parser.Configuration[typeof(ExpandoObject)] = new ChoCSVRecordConfiguration();

                writer.WriteLine("Id,Name,Salary");
                writer.WriteLine("1,Carl,1000");
                writer.WriteLine("2,Mark,2000");
                writer.WriteLine("3,Tom,3000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void QuickPOCOTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoManifoldReader(reader))
            {
                parser.Configuration.RecordSelector = (l) => typeof(EmployeeRecWithCurrency);
                //parser.Configuration[typeof(ExpandoObject)] = new ChoCSVRecordConfiguration();

                writer.WriteLine("1,Carl,1000");
                writer.WriteLine("2,Mark,2000");
                writer.WriteLine("3,Tom,3000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void LoadTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoManifoldReader(reader).WithFirstLineHeader())
            {
                parser.WithRecordSelector((recordLine) =>
                {
                    if (recordLine.Length == 0)
                        return null;

                    if (Char.IsLetter(recordLine[0]))
                        return typeof(Customer);
                    else if (recordLine.Length == 14)
                        return typeof(SampleType);
                    else
                        return typeof(Orders); 
                });
                //parser.Configuration[typeof(ExpandoObject)] = new ChoCSVRecordConfiguration();

                writer.WriteLine("Header");
                writer.WriteLine("10248|VINET|5|04071996|01081996|16071996|3|32.38  ");
                writer.WriteLine("10249|TOMSP|6|05071996|16081996|10071996|1|11.61");
                writer.WriteLine("ALFKI;Alfreds Futterkiste;Maria Anders;Sales Representative;Obere Str. 57;Berlin;Germany");
                writer.WriteLine("ANATR;Ana Trujillo Emparedados y helados;Ana Trujillo;Owner;Avda. de la Constitución 2222;México D.F.;Mexico");
                writer.WriteLine("10250|HANAR|4|08071996|05081996|12071996|2|65.83");
                writer.WriteLine("10111314012345");
                writer.WriteLine("11101314123456");
                writer.WriteLine("10251|VICTE|3|08071996|05081996|15071996|1|41.34");
                writer.WriteLine("11121314901234");
                writer.WriteLine("10101314234567");
                writer.WriteLine("ANTON;Antonio Moreno Taquería;Antonio Moreno;Owner;Mataderos  2312;México D.F.;Mexico");
                writer.WriteLine("BERGS;Berglunds snabbköp;Christina Berglund;Order Administrator;Berguvsvägen  8;Luleå;Sweden");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }
    }

    [ChoFixedLengthRecordObject]
    public class SampleType
    {
        [ChoFixedLengthRecordField(0, 8)]
        public string Field1 { get; set; }

        [ChoFixedLengthRecordField(8, 3)]
        public string Field2 { get; set; }

        [ChoFixedLengthRecordField(11, 3)]
        public int Field3 { get; set; }

        public override string ToString()
        {
            return "SampleType: " + Field2 + " - " + Field3;
        }
    }
    [ChoCSVRecordObject("|")]
    public class Orders
    {
        public int OrderID { get; set; }

        public string CustomerID { get; set; }

        public int EmployeeID { get; set; }
        [ChoTypeConverter(typeof(ChoDateTimeConverter), Parameters = "ddMMyyyy")]
        public DateTime OrderDate { get; set; }

        public string RequiredDate { get; set; }

        public string ShippedDate { get; set; }

        public int ShipVia { get; set; }

        public decimal Freight { get; set; }

        public override string ToString()
        {
            return "Orders: " + OrderID + " - " + CustomerID + " - " + Freight;
        }
    }

    [ChoCSVRecordObject(";")]
    public class Customer
    {
        public string CustomerID { get; set; }
        public string CompanyName { get; set; }
        public string ContactName { get; set; }
        public string ContactTitle { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }

        public override string ToString()
        {
            return "Customer: " + CustomerID + " - " + CompanyName + ", " + ContactName;
        }
    }
    [ChoCSVRecordObject]
    public class EmployeeRecWithCurrency
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public ChoCurrency Salary { get; set; }
    }

}
