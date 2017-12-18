using ChoETL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChoXmlWriterTest
{
    class Program
    {
        static void Main(string[] args)
        {
            sample7Test();
        }

        static void sample7Test()
        {

            //using (var jr = new ChoJSONReader("sample7.json").WithJSONPath("$.fathers")
            //    .WithField("id")
            //    .WithField("married", fieldType: typeof(bool))
            //    .WithField("name")
            //    .WithField("sons")
            //    //.WithField("daughters", fieldType: typeof(Dictionary<string, object>[]))
            //    )
            //{
            //    using (var w = new ChoXmlWriter("sample7.xml"))
            //    {
            //        w.Write(jr);
            //    }
            //    /*
            //    foreach (var item in jr)
            //    {
            //        var x = item.id;
            //        Console.WriteLine(x.GetType());

            //        Console.WriteLine(item.id);
            //        Console.WriteLine(item.married);
            //        Console.WriteLine(item.name);
            //        foreach (dynamic son in item.sons)
            //        {
            //            var x1 = son.address;
            //            //Console.WriteLine(ChoUtility.ToStringEx(son.address.street));
            //        }
            //        foreach (var daughter in item.daughters)
            //            Console.WriteLine(ChoUtility.ToStringEx(daughter));
            //    }
            //    */
            //}

            using (var xr = new ChoXmlReader("sample7.xml")
                .WithField("id")
                .WithField("married")
                .WithField("sons")
                )
            {
                using (var xr1 = new ChoJSONWriter("sample7out.json"))
                    xr1.Write(xr);
            }
        }

        public static void SaveStringList()
        {
            //List<string> list = new List<string>();
            //list.Add("1/1/2012");
            //list.Add(null);
            ArrayList list = new ArrayList();
            list.Add(1);
            list.Add("asas");
            list.Add(null);

            using (var w = new ChoXmlWriter("emp.xml")
                .WithField("Value")
                )
                w.Write(list);
        }
        public static void SaveDict()
        {
            //Dictionary<int, string> list = new Dictionary<int, string>();
            //list.Add(1, "1/1/2012");
            //list.Add(2, null);
            Hashtable list = new Hashtable();
            list.Add(1, "33");
            list.Add(2, null);

            using (var w = new ChoXmlWriter("emp.xml")
                )
                w.Write(list);
        }

        static void DataTableTest()
        {
            string connectionstring = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True";
            using (var conn = new SqlConnection(connectionstring))
            {
                conn.Open();
                var comm = new SqlCommand("SELECT * FROM Customers", conn);
                SqlDataAdapter adap = new SqlDataAdapter(comm);

                DataTable dt = new DataTable("Customer");
                adap.Fill(dt);

                using (var parser = new ChoXmlWriter("customers.xml").WithXPath("Customers/Customer").Configure(c => c.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration("CustId") { IsXmlAttribute = true })))
                    parser.Write(dt);
            }
        }

        static void DataReaderTest()
        {
            string connectionstring = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True";
            using (var conn = new SqlConnection(connectionstring))
            {
                conn.Open();
                var comm = new SqlCommand("SELECT * FROM Customers", conn);
                using (var parser = new ChoXmlWriter("customers.xml").WithXPath("Customers/Customer"))
                    parser.Write(comm.ExecuteReader());
            }
        }


        static void ConfigFirstTest()
        {
            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";
            rec1.IsActive = true;
            rec1.Message = new ChoCDATA("Test");
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = null;
            rec2.IsActive = true;
            rec2.Message = new ChoCDATA("Test");
            objs.Add(rec2);

            ChoXmlRecordConfiguration config = new ChoXmlRecordConfiguration();
            config.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration("Id"));
            config.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration("Name"));

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlWriter(writer, config).WithXPath("Employees/Employee"))
            {
                parser.Write(objs);

                writer.Flush();
                stream.Position = 0;

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        static void QuickPOCOTest()
        {
            List<EmployeeRecSimple> objs = new List<EmployeeRecSimple>();

            EmployeeRecSimple rec1 = new EmployeeRecSimple();
            rec1.Id = null;
            rec1.Name = "Mark";
            rec1.Depends = new List<string>() { "AA", "BB" };
            rec1.Courses = new Dictionary<int, string>() { { 1, "AA" }, { 2, "BB" } };
            objs.Add(rec1);

            //EmployeeRecSimple rec2 = new EmployeeRecSimple();
            //rec2.Id = "2";
            //rec2.Name = null;
            //objs.Add(rec2);
            objs.Add(null);

            using (var parser = new ChoXmlWriter<EmployeeRecSimple>("Emp.xml").WithXPath("Employees/Employee")
                .Configure(e => e.NullValueHandling = ChoNullValueHandling.Default)
                )
            {
                parser.Write(objs);
            }
            //        using (var reader = new ChoXmlReader("emp.xml").WithXPath("Employees/Employee")
            //.WithField("Id")
            //.WithField("Name")
            //.WithField("Depends", isArray: false, fieldType: typeof(List<string>))
            //.WithField("KVP", isArray: false, fieldType: typeof(List<ChoKeyValuePair<int, string>>))
            //)
            //        {
            //            foreach (var i in reader)
            //                Console.WriteLine(ChoUtility.ToStringEx(i));
            //        }

            //using (var reader = new ChoXmlReader<EmployeeRecSimple>("emp.xml").WithXPath("Employees/Employee"))
            //{
            //    foreach (var i in reader)
            //        Console.WriteLine(ChoUtility.ToStringEx(i));
            //}
        }

        static void QuickDynamicTest()
        {
            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";
            rec1.IsActive = true;
            rec1.Message = new ChoCDATA("Test");
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Jason";
            rec2.IsActive = true;
            rec2.Message = new ChoCDATA("Test");
            objs.Add(rec2);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlWriter(writer).WithXPath("Employees/Employee"))
            {
                parser.Write(objs);
                parser.Close();

                writer.Flush();
                stream.Position = 0;

                Console.WriteLine(reader.ReadToEnd());
            }
        }

        public partial class EmployeeRecSimple1
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public partial class EmployeeRecSimple
        {
            [ChoXmlAttributeRecordField]
            public string Id { get; set; }
            [ChoXmlElementRecordField]
            public string Name { get; set; }
            [ChoXmlElementRecordField]
            public List<string> Depends { get; set; }

            [ChoXmlElementRecordField]
            public List<ChoKeyValuePair<int, string>> KVP
            {
                get { return Courses.Select(kvp => new ChoKeyValuePair<int, string>(kvp)).ToList();  }
                set { Courses = value != null ? value.ToDictionary(v => v.Key, v => v.Value) : new Dictionary<int, string>(); }
            }
            [ChoIgnoreMember]
            public Dictionary<int, string> Courses { get; set; }
        }
    }
}
