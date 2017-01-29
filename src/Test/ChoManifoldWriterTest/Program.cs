using ChoETL;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoManifoldWriterTest
{
    class Program
    {
        static void Main(string[] args)
        {
            QuickTest();
        }

        public static void QuickTest()
        {
            List<object> objs = new List<object>();
            SampleType s = new SampleType() { Field1 = "Raj", Field2 = "Mark", Field3 = 1212 };
            objs.Add(s);

            var o = new Orders { CustomerID = "123124", OrderID = 1, EmployeeID = 65657657, OrderDate = DateTime.Today, RequiredDate = "DateText" };
            objs.Add(o);

            dynamic rec1 = new ExpandoObject();
            rec1.Id = 10;
            rec1.Name = "Mark";
            rec1.JoinedDate = new DateTime(2001, 2, 2);
            rec1.IsActive = true;
            rec1.Salary = new ChoCurrency(100);
            objs.Add(rec1);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoManifoldWriter(writer))
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                Console.WriteLine(reader.ReadToEnd());
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
