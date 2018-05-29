using ChoETL;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChoJSONWriterTest
{
public class ToTextConverter : IChoValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value.ToNString();
    }
}

    public class SomeOuterObject
    {
        public string stringValue { get; set; }
        public IPAddress ipValue { get; set; }
    }
    public enum Gender
    {
        [Description("M")]
        Male,
        [Description("F")]
        Female
    }

    public class Person
    {
        public int Age { get; set; }
        //[ChoTypeConverter(typeof(ChoEnumConverter), Parameters = "Name")]
        public Gender Gender { get; set; }
    }

    public class MyDate
    {
        public int year { get; set; }
        public int month { get; set; }
        public int day { get; set; }
    }

    public class Lad
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public MyDate dateOfBirth { get; set; }
    }

    class Program
    {
        public enum EmpType {  FullTime, Contract }

        static void Main(string[] args)
        {
            EnumTest();
        }

        static void ComplexObjTest()
        {
            StringBuilder sb = new StringBuilder();
            var obj = new Lad
            {
                firstName = "Markoff",
                lastName = "Chaney",
                dateOfBirth = new MyDate
                {
                    year = 1901,
                    month = 4,
                    day = 30
                }
            };
            using (var jr = new ChoJSONWriter<Lad>(sb)
                )
            {
                jr.Write(obj);
            }

            Console.WriteLine(sb.ToString());

        }

        static void EnumTest()
        {
            StringBuilder sb = new StringBuilder();
            using (var jr = new ChoJSONWriter<Person>(sb)
                )
            {
                jr.Write(new Person { Age = 1, Gender = Gender.Female });
            }

            Console.WriteLine(sb.ToString());

        }

        static void EnumLoadTest()
        {
            string json = @"[
 {
  ""Age"": 1,
  ""Gender"": ""M""
 }
]";

            using (var p = ChoJSONReader<Person>.LoadText(json))
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void IPAddressTest()
        {
            using (var jr = new ChoJSONWriter<SomeOuterObject>("ipaddr.json")
                .WithField("stringValue")
                .WithField("ipValue", valueConverter: (o) => o.ToString())
                )
            {
                var x1 = new SomeOuterObject { stringValue = "X1", ipValue = IPAddress.Parse("12.23.21.23") };
                jr.Write(x1);
            }

        }

        static void NestedJSONFile()
        {
            var dataMapperModels = new List<DataMapper>();
            var model = new DataMapper
            {
                Name = "performanceLevels",
                SubDataMappers = new List<DataMapper>()
            {
                new DataMapper()
                {
                    Name = "performanceLevel_1",
                    SubDataMappers = new List<DataMapper>()
                    {
                        new DataMapper()
                        {
                            Name = "title",
                            DataMapperProperty = new DataMapperProperty()
                                    {
                                        Source = "column",
                                        SourceColumn = "title-column",
                                        DataType = "string",
                                        Default = "N/A",
                                        SourceTable = null,
                                        Value = null
                                    }
                        },
                        new DataMapper()
                        {
                            Name = "version1",
                            DataMapperProperty = new DataMapperProperty()
                                    {
                                        Source = "column",
                                        SourceColumn = "version-column",
                                        DataType = "int",
                                        Default = "1",
                                        SourceTable = null,
                                        Value = null
                                    }
                        },
                        new DataMapper()
                        {
                            Name = "threeLevels",
                            SubDataMappers = new List<DataMapper>()
                            {
                                new DataMapper()
                                {
                                    Name = "version",
                                    DataMapperProperty = new DataMapperProperty()
                                            {
                                                Source = "column",
                                                SourceColumn = "version-column",
                                                DataType = "int",
                                                Default = "1",
                                                SourceTable = null,
                                                Value = null
                                            }
                                }
                            }
                        }
                    }
                },
                new DataMapper()
                {
                    Name = "performanceLevel_2",
                    SubDataMappers = new List<DataMapper>()
                    {
                        new DataMapper()
                        {
                            Name = "title",
                            DataMapperProperty = new DataMapperProperty()
                                    {
                                        Source = "column",
                                        SourceColumn = "title-column",
                                        DataType = "string",
                                        Default = "N/A",
                                        SourceTable = null,
                                        Value = null
                                    }
                        },
                        new DataMapper()
                        {
                            Name = "version",
                            DataMapperProperty = new DataMapperProperty()
                                    {
                                        Source = "column",
                                        SourceColumn = "version-column",
                                        DataType = "int",
                                        Default = "1",
                                        SourceTable = null,
                                        Value = null
                                    }
                        }
                    }
                }
            }
            };

            dataMapperModels.Add(model);
            using (var w = new ChoJSONWriter("nested.json")
            )
                w.Write(dataMapperModels);

        }

        static void Sample7()
        {
            using (var jr = new ChoJSONReader("sample7.json").WithJSONPath("$.fathers")
                .WithField("id")
                .WithField("married", fieldType: typeof(bool))
                .WithField("name")
                .WithField("sons")
                .WithField("daughters", fieldType: typeof(Dictionary<string, object>[]))
                )
            {
                using (var w = new ChoJSONWriter("sample7out.json"))
                {
                    w.Write(jr);
                }
                /*
                foreach (var item in jr)
                {
                    var x = item.id;
                    Console.WriteLine(x.GetType());

                    Console.WriteLine(item.id);
                    Console.WriteLine(item.married);
                    Console.WriteLine(item.name);
                    foreach (dynamic son in item.sons)
                    {
                        var x1 = son.address;
                        //Console.WriteLine(ChoUtility.ToStringEx(son.address.street));
                    }
                    foreach (var daughter in item.daughters)
                        Console.WriteLine(ChoUtility.ToStringEx(daughter));
                }
                */
            }
        }

        private static void ConvertAllDataWithNativetype()
        {
            using (var jw = new ChoJSONWriter("sample.json"))
            {
                using (var cr = new ChoCSVReader("sample.csv")
                    .WithFirstLineHeader()
                    .WithField("firstName")
                    .WithField("lastName")
                    .WithField("salary", fieldType: typeof(double))
                    )
                {
                    //foreach (var x in cr)
                    //    Console.WriteLine(ChoUtility.ToStringEx(x));
                    jw.Write(cr);
                }
            }
        }
        public static void SaveDict()
        {
            Dictionary<int, string> list = new Dictionary<int, string>();
            list.Add(1, "1/1/2012");
            list.Add(2, null);
            //Hashtable list = new Hashtable();
            //list.Add(1, "33");
            //list.Add(2, null);

            using (var w = new ChoJSONWriter("emp.json")
                )
                w.Write(list);
        }
        public static void SaveStringList()
        {
            //List<EmpType?> list = new List<EmpType?>();
            //list.Add(EmpType.Contract);
            //list.Add(null);

            //List<int?> list = new List<int?>();
            //list.Add(1);
            //list.Add(null);

            //int[] list = new int[] { 11, 21 };
            ArrayList list = new ArrayList();
            list.Add(1);
            list.Add("asas");
            list.Add(null);

            using (var w = new ChoJSONWriter("emp.json")
                .WithField("Value")
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

                using (var parser = new ChoJSONWriter("customers.json").Configure(c => c.UseJSONSerialization = true))
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
                using (var parser = new ChoJSONWriter("customers.json"))
                    parser.Write(comm.ExecuteReader());
            }
        }

        static void POCOTest()
        {
            List<EmployeeRecSimple1> objs = new List<EmployeeRecSimple1>();
            EmployeeRecSimple1 rec1 = new EmployeeRecSimple1();
            rec1.Id = 1;
            rec1.Name = "Mark";
            objs.Add(rec1);

            objs.Add(null);

            using (var w = new ChoJSONWriter<EmployeeRecSimple1>("emp.json")
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure(e => e.NullValueHandling = ChoNullValueHandling.Empty)
                )
            {
                w.Write(objs);

                //w.Write(ChoEnumerable.AsEnumerable(() =>
                //{
                //    return new { Address = new string[] { "NJ", "NY" }, Name = "Raj", Zip = "08837" };
                //}));
                //w.Write(new { Name = "Raj", Zip = "08837", Address = new { City = "New York", State = "NY" } });
            }

        }

        static void DynamicTest()
        {
            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";
            rec1.Date = DateTime.Now;
            rec1.Active = true;
            rec1.Salary = new ChoCurrency(10.01);
            rec1.EmpType = EmpType.FullTime;
            rec1.Array = new int[] { 1, 2, 4 };
            rec1.Dict = new Dictionary<int, string>() { { 1, "xx" } };
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Jason";
            rec2.Date = DateTime.Now;
            rec2.Salary = new ChoCurrency(10.01);
            rec2.Active = false;
            rec2.EmpType = EmpType.Contract;
            rec2.Array = new int[] { 1, 2, 4 };
            rec2.Dict = new string[] { "11", "12", "14" };

            objs.Add(rec2);
            objs.Add(null);

            using (var w = new ChoJSONWriter("emp.json")
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure( c => c.NullValueHandling = ChoNullValueHandling.Empty)
                )
            {
                w.Write(objs);

                //w.Write(ChoEnumerable.AsEnumerable(() =>
                //{
                //    return new { Address = new string[] { "NJ", "NY" }, Name = "Raj", Zip = "08837" };
                //}));
                //w.Write(new { Name = "Raj", Zip = "08837", Address = new { City = "New York", State = "NY" } });
            }

        }
    }
    public class DataMapper : IChoKeyValueType
    {
        public DataMapper()
        {
            SubDataMappers = new List<DataMapper>();
        }

        public string Name { get; set; }

        public DataMapperProperty DataMapperProperty { get; set; }

        public List<DataMapper> SubDataMappers { get; set; }

        public object Value
        {
            get
            {
                if (SubDataMappers.IsNullOrEmpty())
                    return (object)DataMapperProperty;
                else
                {
                    ChoDynamicObject obj = new ChoDynamicObject();
                    foreach (var item in SubDataMappers)
                        obj.AddOrUpdate(item.Name, item.Value);
                    return obj;
                }
            }
            set
            {
                if (value is DataMapperProperty)
                    DataMapperProperty = value as DataMapperProperty;
                else if (value is IEnumerable)
                {

                }
            }
        }

        public object Key
        {
            get
            {
                return Name;
            }
            set
            {
                Name = value.ToNString();
            }
        }
    }

    public class DataMapperProperty
    {
        [JsonProperty(PropertyName = "data-type", NullValueHandling = NullValueHandling.Ignore)]
        public string DataType { get; set; }

        [JsonProperty(PropertyName = "source", NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; set; }

        [JsonProperty(PropertyName = "source-column", NullValueHandling = NullValueHandling.Ignore)]
        public string SourceColumn { get; set; }

        [JsonProperty(PropertyName = "source-table", NullValueHandling = NullValueHandling.Ignore)]
        public string SourceTable { get; set; }

        [JsonProperty(PropertyName = "default", NullValueHandling = NullValueHandling.Ignore)]
        public string Default { get; set; }

        [JsonProperty(PropertyName = "value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }

        //public static explicit operator DataMapperProperty(JToken v)
        //{
        //    return new DataMapperProperty()
        //    {
        //        DataType = v.Value<string>("data-type"),
        //        Source = v.Value<string>("source"),
        //        SourceColumn = v.Value<string>("source-column"),
        //        SourceTable = v.Value<string>("source-table"),
        //        Default = v.Value<string>("default"),
        //        Value = v.Value<string>("value")
        //    };
        //}
    }

    public partial class EmployeeRecSimple1
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public List<EmployeeRecSimple1> SubEmployeeRecSimple1;
    }
}
