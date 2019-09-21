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
using System.IO;
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

    public class PlaceObj : IChoNotifyRecordFieldWrite
    {
        public string Place { get; set; }
        public int SkuNumber { get; set; }

        public bool AfterRecordFieldWrite(object target, long index, string propName, object value)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordFieldWrite(object target, long index, string propName, ref object value)
        {
            //if (propName == nameof(SkuNumber))
            //    value = String.Format("SKU_{0}", value.ToNString());

            return true;
        }

        public bool RecordFieldWriteError(object target, long index, string propName, object value, Exception ex)
        {
            throw new NotImplementedException();
        }
    }

    class Program
    {
        public enum EmpType {  FullTime, Contract }

        static void Main(string[] args)
        {
            DataTableTest();
            return;

            string codfis = "Example1";
            var codfisValue = new
            { // codfis is the name of the variable as you can see
                Cognome = "vcgm",
                Nome = "vnm",
                Sesso = "ss",
                LuogoDiNascita = "ldn",
                Provincia = "pr",
                DataDiNascita = "ddn"
            };
            var jsonCF = new Dictionary<string, object>();
            jsonCF.Add(codfis, codfisValue);


            using (StreamWriter file = File.CreateText("CodFisCalcolati.json"))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(file, jsonCF);
            }
            return;

            CustomLabel();
            return;

            string[] tt = new string[] { "1", "", "3", "" };

            var c = tt.Select((t, i) => String.IsNullOrWhiteSpace(t) ? (int?)i + 1 : null).Where(t => t != null).ToArray();
            Console.WriteLine(String.Join(",", c));
            return;
        }

        static void CustomLabel()
        {
            string codfis = "Example1";
            var jsonCF = new
            {
                codfis = new
                { // codfis is the name of the variable as you can see
                    Cognome = "vcgm",
                    Nome = "vnm",
                    Sesso = "ss",
                    LuogoDiNascita = "ldn",
                    Provincia = "pr",
                    DataDiNascita = "ddn"
                }
            };

            var jsonCF1 = new Dictionary<string, object>();
            jsonCF1.Add(codfis, new
            { // codfis is the name of the variable as you can see
                Cognome = "vcgm",
                Nome = "vnm",
                Sesso = "ss",
                LuogoDiNascita = "ldn",
                Provincia = "pr",
                DataDiNascita = "ddn"
            });
            jsonCF1.Add(codfis+"1", new
            { // codfis is the name of the variable as you can see
                Cognome = "vcgm",
                Nome = "vnm",
                Sesso = "ss",
                LuogoDiNascita = "ldn",
                Provincia = "pr",
                DataDiNascita = "ddn"
            });

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter(json)
                .SupportMultipleContent(true)
                //.Configure(c => c.SingleElement = true)
                )
            {
                w.Write(jsonCF1);
            }

            Console.WriteLine(json.ToString());
        }

        public class ArmorPOCO
        {
            public int Armor { get; set; }
            public int Strenght { get; set; }
        }

        static void DictionaryTest()
        {
            StringBuilder msg = new StringBuilder();

            Dictionary<int, ArmorPOCO> dict = new Dictionary<int, ArmorPOCO>();
            dict.Add(1, new ArmorPOCO { Armor = 1, Strenght = 1 });
            dict.Add(2, new ArmorPOCO { Armor = 2, Strenght = 2 });

            List<ArmorPOCO> list = new List<ArmorPOCO>();
            list.Add(new ArmorPOCO { Armor = 1, Strenght = 1 });
            list.Add(new ArmorPOCO { Armor = 2, Strenght = 2 });

            using (var w = new ChoJSONWriter<ArmorPOCO>(msg)
                )
            {
                w.Write(list);
            }

            Console.WriteLine(msg.ToString());
        }

        static void CSVToJSON1()
        {
string csv = @"""""|""Rep Employee Name""|""Ship To Customer Number""|""""|""Ship To Customer Name""|""Patient Last Name""|""Patient First Name""|""Patient Location""|""""|""""|""""|""""|""Serial Number""|""Product Description -Used""
"""" | ""CHRISTMAN, AMY"" | ""580788"" | ""4543"" | ""dfgfdgfdgdfgdfsgfdgdfg"" | """" | """" | """" | ""6025"" | ""5/13/2002 12:45:00 PM"" | ""5/13/2002 2:59:00 PM"" | ""7/2/2002 10:15:44 AM"" | """" | ""VAC""
""34534534634"" | ""NAGORNY, WILLIAM"" | ""3453"" | ""363463"" | ""345435435"" | """" | """" | """" | ""6079"" | ""5/15/2002 7:39:51 AM"" | ""3/20/2002 11:00:00 AM"" | ""9/25/2002 8:18:32 AM"" | """" | ""VAC""
""34634643634"" | ""MOORE, NICHOLAS (NICHO"" | ""654287"" | ""98188"" | ""asdfdsfdfasasdf"" | """" | """" | """" | ""6007"" | ""5/31/2002 2:45:16 PM"" | ""5/31/2002 3:51:00 PM"" | ""9/10/2002 10:51:55 AM"" | """" | ""VAC""";

StringBuilder json = new StringBuilder();   
using (var p = ChoCSVReader.LoadText(csv)
    .WithDelimiter("|")
    .WithFirstLineHeader()
    .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true)
    .Configure(c => c.QuoteAllFields = true)
    .Configure(c => c.NullValue = "")
    )
{
    using (var w = new ChoJSONWriter(json)
        //.Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any)
        .Configure(c => c.NullValueHandling = ChoNullValueHandling.Ignore)
        )
    {
        w.Write(p);
    }
}

Console.WriteLine(json.ToString());
        }

        public class data
        {
            public int Id { get; set; }
            public int SSN { get; set; }
            public string Message { get; set; }

        }
        static void ListTest()
        {
            List<data> _data = new List<data>();
            _data.Add(new data()
            {
                Id = 1,
                SSN = 2,
                Message = "A Message"
            });

            Console.WriteLine(ChoJSONWriter.ToTextAll<data>(_data));
        }

        static void CustomFormat2()
        {
            StringBuilder sb = new StringBuilder();

            using (var w = new ChoJSONWriter<PlaceObj>(sb)
                //.WithField(m => m.SkuNumber, valueConverter: (o) => String.Format("SKU_{0}", o.ToNString()))
            )
            {
                PlaceObj o1 = new PlaceObj();
                o1.Place = "1";
                o1.SkuNumber = 100;

                w.Write(o1);
            }

            Console.WriteLine(sb.ToString());
        }

        static void CustomFormat1()
        {
            StringBuilder sb = new StringBuilder();

            using (var w = new ChoJSONWriter(sb)
                .WithField("Place")
                .WithField("SkuNumber", valueConverter: (o) => String.Format("SKU_{0}", o.ToNString()))
                )
            {
                dynamic o1 = new ExpandoObject();
                o1.Place = 1;
                o1.SkuNumber = 100;

                w.Write(o1);
            }

            Console.WriteLine(sb.ToString());
        }

        #region Sample50

        public class Rootobject
        {
            public Home home { get; set; }
            public Away away { get; set; }
        }

        public class Home
        {
            [ChoUseJSONSerialization]
            public _0_15 _0_15 { get; set; }
            public _15_30 _15_30 { get; set; }
            public _30_45 _30_45 { get; set; }
            public _45_60 _45_60 { get; set; }
            public _60_75 _60_75 { get; set; }
            public _75_90 _75_90 { get; set; }
        }

        public class _0_15
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class _15_30
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class _30_45
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class _45_60
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class _60_75
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class _75_90
        {
            public int goals { get; set; }
            public int percentage { get; set; }
        }

        public class Away
        {
            public _0_151 _0_15 { get; set; }
            public _15_301 _15_30 { get; set; }
            public _30_451 _30_45 { get; set; }
            public _45_601 _45_60 { get; set; }
            public _60_751 _60_75 { get; set; }
            public _75_901 _75_90 { get; set; }
        }

        public class _0_151
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }

        public class _15_301
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }

        public class _30_451
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }

        public class _45_601
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }

        public class _60_751
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }

        public class _75_901
        {
            public int goals { get; set; }
            public float percentage { get; set; }
        }
        static void Sample50()
        {
            string json = @"
{
    ""home"": {
        ""0_15"": {
            ""goals"": 7,
            ""percentage"": 14
        },
        ""15_30"": {
            ""goals"": 6,
            ""percentage"": 12
        },
        ""30_45"": {
            ""goals"": 11,
            ""percentage"": 22
        },
        ""45_60"": {
            ""goals"": 4,
            ""percentage"": 8
        },
        ""60_75"": {
            ""goals"": 8,
            ""percentage"": 16
        },
        ""75_90"": {
            ""goals"": 14,
            ""percentage"": 28
        }
    },
    ""away"": {
        ""0_15"": {
            ""goals"": 7,
            ""percentage"": 15.56
        },
        ""15_30"": {
            ""goals"": 7,
            ""percentage"": 15.56
        },
        ""30_45"": {
            ""goals"": 5,
            ""percentage"": 11.11
        },
        ""45_60"": {
            ""goals"": 6,
            ""percentage"": 13.33
        },
        ""60_75"": {
            ""goals"": 13,
            ""percentage"": 28.89
        },
        ""75_90"": {
            ""goals"": 7,
            ""percentage"": 15.56
        }
    }
}";

            using (var p = ChoJSONReader<Rootobject>.LoadText(json).Configure(c => c.SupportMultipleContent = true))
            {
                //foreach (var rec in p)
                //	Console.WriteLine(rec.Dump());
                Console.WriteLine(ChoJSONWriter<Rootobject>.ToText(p.First(), new ChoJSONRecordConfiguration().Configure(c => c.SupportMultipleContent = true)));
            }
        }

        #endregion Sample50

        #region Nested2NestedObjectTest

        public class Customer
        {
            public long Id { get; set; }

            public string UserName { get; set; }

            public AddressModel Address { get; set; }
        }

        public class AddressModel
        {
            public string Address { get; set; }

            public string Address2 { get; set; }

            public string City { get; set; }
        }

        static void Nested2NestedObjectTest()
        {
            string json = @"{ 
""id"": 123,   ""userName"": ""fflintstone"",   ""Address"": {
""address"": ""345 Cave Stone Road"",
""address2"": """",
""city"": ""Bedrock"",
""state"": ""AZ"",
""zip"": """"   } }";

            using (var p = ChoJSONReader<Customer>.LoadText(json).WithFlatToNestedObjectSupport(false)
                //.WithField(r => r.Address.Address, fieldName: "Address")
                )
            {
                var x = p.First();
                Console.WriteLine(x.Dump());
            }
        }

        #endregion Nested2NestedObjectTest

        static void NestedObjectTest()
        {
            string json = @"{
    ""id"": 123,
    ""userName"": ""fflintstone"",
    ""address"": ""345 Cave Stone Road"",
    ""address2"": """",
    ""city"": ""Bedrock"",
    ""state"": ""AZ"",
    ""zip"": """",   
}";

            using (var p = ChoJSONReader<Customer>.LoadText(json).WithFlatToNestedObjectSupport(true)
                //.WithField(r => r.Address.Address, fieldName: "Address")
                )
            {
                var x = p.First();
                Console.WriteLine(x.Dump());
            }
        }

        static void CombineJSONTest()
        {
            string json = @"[
  {
    ""deliveryDay"": ""2018-06-19T15:00:00.000+0300"",
    ""currencyCode"": ""TRY"",
    ""offerType"": ""HOURLY"",
    ""regionCode"": ""TR1"",
    ""offerDetails"": [
         {
           ""startPeriod"": ""1"",
            ""duration"": ""1"",
            ""offerPrices"": [
                {
                  ""price"": ""0"",
                   ""amount"": ""5""
                 }
               ]
             }
           ]
          },
  {
   ""deliveryDay"": ""2018-06-19T15:00:00.000+0300"",
   ""currencyCode"": ""TRY"",
   ""offerType"": ""HOURLY"",
   ""regionCode"": ""TR1"",
   ""offerDetails"": [
         {
           ""startPeriod"": ""1"",
           ""duration"": ""1"",
           ""offerPrices"": [
                {
                  ""price"": ""2000"",
                   ""amount"": ""5""
               }
              ]
            }
          ]
        }
       ]";


            StringBuilder sb = new StringBuilder();
            using (var p = ChoJSONReader.LoadText(json))
            {
                var list = p.GroupBy(r => r.deliveryDay).Select(r => new
                {
                    deliveryDay = r.Key,
                    r.First().currencyCode,
                    r.First().offerType,
                    r.First().regionCode,
                    offerDetails = new
                    {
                        ((Array)r.First().offerDetails).OfType<dynamic>().First().startPeriod,
                        ((Array)r.First().offerDetails).OfType<dynamic>().First().duration,
                        offerPrices = r.Select(r1 => ((Array)r1.offerDetails[0].offerPrices).OfType<object>().First()).ToArray()
                    }
                }).ToArray();

                Console.WriteLine(ChoJSONWriter.ToTextAll(list));
                //foreach (var rec in )
                //	Console.WriteLine(rec.Dump());
            }
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
            StringBuilder sb = new StringBuilder();
            string connectionstring = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Northwind;Integrated Security=True";
            using (var conn = new SqlConnection(connectionstring))
            {
                conn.Open();
                var comm = new SqlCommand("SELECT TOP 2 * FROM Customers", conn);
                SqlDataAdapter adap = new SqlDataAdapter(comm);

                DataTable dt = new DataTable("Customer");
                adap.Fill(dt);

                using (var parser = new ChoJSONWriter(sb)
                    .Configure(c => c.IgnoreRootName = true)
                    )
                    parser.Write(dt);
            }

            Console.WriteLine(sb.ToString());
        }

        static void DataReaderTest()
        {
            //string connectionstring = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True";
            string connectionstring = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Northwind;Integrated Security=True";
            StringBuilder sb = new StringBuilder();
            using (var conn = new SqlConnection(connectionstring))
            {
                conn.Open();
                var comm = new SqlCommand("SELECT top 2 * FROM Customers", conn);
                using (var parser = new ChoJSONWriter(sb))
                    parser.Write(comm.ExecuteReader());
            }

            Console.WriteLine(sb.ToString());
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


