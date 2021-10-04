using ChoETL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoManifoldReaderTest
{
    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {
        static void Main(string[] args)
        {
            MasterDetailTest();
        }

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }

        public static string FileNameMasterDetailTXT => "MasterDetail.txt";

        //[Test]
        public static void MasterDetailTest()
        {
            List<object> expected = new List<object>
            {
                new KeyValuePair<Recipe,RecipeLineItem>(new Recipe { Id = 1704, Name = "Birthday cake"}, new RecipeLineItem { Index = 1, Amount = 25}),
                new KeyValuePair<Recipe,RecipeLineItem>(new Recipe { Id = 1704, Name = "Birthday cake"}, new RecipeLineItem { Index = 2, Amount = 25}),
                new KeyValuePair<Recipe,RecipeLineItem>(new Recipe { Id = 1704, Name = "Birthday cake"}, new RecipeLineItem { Index = 3}),
                new KeyValuePair<Recipe,RecipeLineItem>(new Recipe { Id = 1804, Name = "Wedding cake"}, new RecipeLineItem { Index = 1, Amount = 25}),
                new KeyValuePair<Recipe,RecipeLineItem>(new Recipe { Id = 1804, Name = "Wedding cake"}, new RecipeLineItem { Index = 2, Amount = 25}),
                new KeyValuePair<Recipe,RecipeLineItem>(new Recipe { Id = 1804, Name = "Wedding cake"}, new RecipeLineItem { Index = 3, Amount = 50}),
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoManifoldReader(FileNameMasterDetailTXT)
                .WithCustomRecordSelector((v) =>
                {
                    string l = v as string;
                    if (l.SplitNTrim(';')[0].CastTo<int>() > 1700)
                        return typeof(Recipe);
                    else
                        return typeof(RecipeLineItem);
                })
                )
            {
                foreach (var r in parser.ToMasterDetail<Recipe, RecipeLineItem>())
                    actual.Add(r);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [ChoCSVRecordObject(";")]
        public class Recipe
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                var recipe = obj as Recipe;
                return recipe != null &&
                       Id == recipe.Id &&
                       Name == recipe.Name;
            }

            public override int GetHashCode()
            {
                var hashCode = -1919740922;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                return hashCode;
            }
        }

        [ChoCSVRecordObject(";")]
        public class RecipeLineItem
        {
            public int Index { get; set; }
            public int Amount { get; set; }

            public override bool Equals(object obj)
            {
                var item = obj as RecipeLineItem;
                return item != null &&
                       Index == item.Index &&
                       Amount == item.Amount;
            }

            public override int GetHashCode()
            {
                var hashCode = -1525848980;
                hashCode = hashCode * -1521134295 + Index.GetHashCode();
                hashCode = hashCode * -1521134295 + Amount.GetHashCode();
                return hashCode;
            }
        }

        //[Test]
        public static void QuickTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {{ "Id","1"},{"Name","Carl"},{"Salary","1000"}},
                new ChoDynamicObject {{ "Id","2"},{"Name","Mark"},{"Salary","2000"}},
                new ChoDynamicObject {{ "Id","3"},{"Name","Tom" },{"Salary","3000"}}
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoManifoldReader(reader))
            {
                parser.Configuration.RecordTypeSelector = (l) => typeof(ExpandoObject);
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
                    actual.Add(rec);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void QuickPOCOTest()
        {
            List<object> expected = new List<object>
            {
                new EmployeeRecWithCurrency { Id = 1, Name = "Carl", Salary = new ChoCurrency(1000)},
                new EmployeeRecWithCurrency { Id = 2, Name = "Mark", Salary = new ChoCurrency(2000)},
                new EmployeeRecWithCurrency { Id = 3, Name = "Tom", Salary = new ChoCurrency(3000)}
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoManifoldReader(reader))
            {
                parser.Configuration.RecordTypeSelector = (l) => typeof(EmployeeRecWithCurrency);
                //parser.Configuration[typeof(ExpandoObject)] = new ChoCSVRecordConfiguration();

                writer.WriteLine("1,Carl,1000");
                writer.WriteLine("2,Mark,2000");
                writer.WriteLine("3,Tom,3000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    actual.Add(rec);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        ////[Test]
        public static void LoadTest()
        {
            List<object> expected = new List<object>
            {
                new Orders{ OrderID = 10248, CustomerID = "VINET", EmployeeID = 5 , OrderDate = new DateTime(1996,7,4), RequiredDate = "01081996",ShippedDate = "16071996",ShipVia = 3, Freight = (decimal)32.38},
                new Orders{ OrderID = 10249, CustomerID = "TOMSP", EmployeeID = 6 , OrderDate = new DateTime(1996,7,5), RequiredDate = "16081996",ShippedDate = "10071996",ShipVia = 1, Freight = (decimal)11.61},
                new Customer { CustomerID ="ALFKI", CompanyName = "Alfreds Futterkiste", ContactName = "Maria Anders", ContactTitle = "Sales Representative", Address = "Obere Str. 57", City = "Berlin", Country = "Germany"},
                new Customer { CustomerID ="ANATR", CompanyName = "Ana Trujillo Emparedados y helados", ContactName = "Ana Trujillo", ContactTitle = "Owner", Address = "Avda. de la Constitución 2222", City = "México D.F.;", Country = "Mexico"},
                new Orders{ OrderID = 10250, CustomerID = "HANAR", EmployeeID = 4 , OrderDate = new DateTime(1996,7,8), RequiredDate = "05081996",ShippedDate = "12071996",ShipVia = 2, Freight = (decimal)65.83},
                new SampleType { Field1 ="10111314", Field2 ="012", Field3 =345},
                new SampleType { Field1 ="11101314", Field2 ="123", Field3 =456},
                new Orders{ OrderID = 10251, CustomerID = "VICTE", EmployeeID = 3 , OrderDate = new DateTime(1996,7,8), RequiredDate = "05081996",ShippedDate = "15071996",ShipVia = 1, Freight = (decimal)41.34},
                new SampleType { Field1 ="11121314", Field2 ="901", Field3 =234},
                new SampleType { Field1 ="10101314", Field2 ="234", Field3 =567},
                new Customer { CustomerID ="ANTON", CompanyName = "Antonio Moreno Taquería", ContactName = "Antonio Moreno", ContactTitle = "Owner", Address = "Mataderos  2312", City = "México D.F.", Country = "Mexico"},
                new Customer { CustomerID ="BERGS", CompanyName = "Berglunds snabbköp", ContactName = "Christina Berglund", ContactTitle = "Order Administrator", Address = "Berguvsvägen  8", City = "Luleå", Country = "Sweden"}
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoManifoldReader(reader).WithFirstLineHeader())
            {
                parser.WithCustomRecordSelector((value) =>
                {
                    string recordLine = value as string;
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
                    actual.Add(rec);
//                    Console.WriteLine(rec.ToStringEx());
                }
            }

            CollectionAssert.AreEqual(expected, actual);
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

        public override bool Equals(object obj)
        {
            var type = obj as SampleType;
            return type != null &&
                   Field1 == type.Field1 &&
                   Field2 == type.Field2 &&
                   Field3 == type.Field3;
        }

        public override int GetHashCode()
        {
            var hashCode = 1343835333;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Field1);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Field2);
            hashCode = hashCode * -1521134295 + Field3.GetHashCode();
            return hashCode;
        }

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

        public override bool Equals(object obj)
        {
            var orders = obj as Orders;
            return orders != null &&
                   OrderID == orders.OrderID &&
                   CustomerID == orders.CustomerID &&
                   EmployeeID == orders.EmployeeID &&
                   OrderDate == orders.OrderDate &&
                   RequiredDate == orders.RequiredDate &&
                   ShippedDate == orders.ShippedDate &&
                   ShipVia == orders.ShipVia &&
                   Freight == orders.Freight;
        }

        public override int GetHashCode()
        {
            var hashCode = 84649274;
            hashCode = hashCode * -1521134295 + OrderID.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CustomerID);
            hashCode = hashCode * -1521134295 + EmployeeID.GetHashCode();
            hashCode = hashCode * -1521134295 + OrderDate.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(RequiredDate);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ShippedDate);
            hashCode = hashCode * -1521134295 + ShipVia.GetHashCode();
            hashCode = hashCode * -1521134295 + Freight.GetHashCode();
            return hashCode;
        }

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

        public override bool Equals(object obj)
        {
            var customer = obj as Customer;
            return customer != null &&
                   CustomerID == customer.CustomerID &&
                   CompanyName == customer.CompanyName &&
                   ContactName == customer.ContactName &&
                   ContactTitle == customer.ContactTitle &&
                   Address == customer.Address &&
                   City == customer.City &&
                   Country == customer.Country;
        }

        public override int GetHashCode()
        {
            var hashCode = 667798484;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CustomerID);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CompanyName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ContactName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ContactTitle);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Address);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(City);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Country);
            return hashCode;
        }

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

        public override bool Equals(object obj)
        {
            var currency = obj as EmployeeRecWithCurrency;
            return currency != null &&
                   EqualityComparer<int?>.Default.Equals(Id, currency.Id) &&
                   Name == currency.Name &&
                   Salary.Equals(currency.Salary);
        }

        public override int GetHashCode()
        {
            var hashCode = -1858601383;
            hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Id);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<ChoCurrency>.Default.GetHashCode(Salary);
            return hashCode;
        }
    }

}
