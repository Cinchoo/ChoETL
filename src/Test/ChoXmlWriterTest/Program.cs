using ChoETL;
using NUnit.Framework;
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

    public class Emp
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Choice
    {
        public string[] Options { get; set; }
        public Emp Emp { get; set; }
        public List<int> Ids { get; set; }
        public Emp[] EmpArr { get; set; }
        //public Dictionary<int, Emp> EmpDict { get; set; }
    }

    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {

        public static void JSON2XML()
        {
            string json = @"{
  ""header"": ""myheader"",
  ""transaction"": {
    ""date"": ""2019-09-24"",
    ""items"": [
      {
        ""number"": ""123"",
        ""unit"": ""EA"",
        ""qty"": 6
      },
      {
        ""number"": ""456"",
        ""unit"": ""CS"",
        ""qty"": 4
      }
    ]
  }
}";
            using (var r = ChoJSONReader.LoadText(json))
            {
                var x = r.FirstOrDefault();
                //Console.WriteLine(x.Dump());
                Console.WriteLine(ChoXmlWriter.ToText(x, new ChoXmlRecordConfiguration().Configure(c =>
                {
                    c.RootName = "Root1";
                    //c.DoNotEmitXmlNamespace = true;
                    //c.TurnOffXmlFormatting = true;
                })));
            }
        }

        public static void JSON2SoapXML()
        {
            string json = @"{
  ""Name"": ""00141169"",
  ""CurrencyCode"": ""EUR"",
  ""Date"": ""2020-04-03"",
}";

            StringBuilder xml = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                var x = r.FirstOrDefault();
                //Console.WriteLine(x.Dump());

                using (var w = new ChoXmlWriter(xml)
                    .Configure(c => c.NamespaceManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/"))
                    .Configure(c => c.NamespaceManager.AddNamespace("tmp", "http://tempuri.org/"))
                    .Configure(c => c.RootName = "soap:Envelope")
                    .Configure(c => c.NodeName = "soap:Body")
                    .Configure(c => c.DefaultNamespacePrefix = "tmp")
                    )
                {
                    w.Write(new { listdata  = x });
                }
            }

            Console.WriteLine(xml.ToString());
        }

        static void Main(string[] args)
        { 
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;

            JSON2SoapXML();
            return;

            JSON2XmlDateTimeTest();
            return;
            CustomStringArrayTest();

            using (var file = new FileStream("t.json", FileMode.Append))
            {
                using (var w = new ChoJSONWriter(file))
                {

                }
            }
        }

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }

        [Test]
        public static void JSON2XmlDateTimeTest()
        {
            string actual;
            string json = @"{
 ""start"": ""2019-10-24T10:37:27.590Z"",
 ""end"": ""2019-10-24T11:00:00.000Z"",
 ""requests/duration"": {
   ""avg"": 3819.55
 }
}";

            //using (var r = new ChoJSONReader(new StringBuilder(json))
            //    )
            //{
            //    Console.WriteLine(r.First().Dump());
            //}

            actual= ChoJSONWriter.ToText(ChoJSONReader.LoadText(json,
                new ChoJSONRecordConfiguration().Configure(c => c.JsonSerializerSettings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    DateParseHandling = Newtonsoft.Json.DateParseHandling.None
                })).FirstOrDefault()
                );

            Assert.AreEqual(json, actual);
        }
        [Test]
        public static void CustomStringArrayTest()
        {
            string expected = @"<Root>
  <Days>
    <Monday />
    <Tuesday />
    <Wed />
  </Days>
</Root>";
            string actual = null;

            List<string> s = new List<string>();
            s.Add("Monday");
            s.Add("Tuesday");
            s.Add("Wed");

            StringBuilder sb = new StringBuilder();
            using (var w = new ChoXmlWriter(sb)
                .Configure(c => c.IgnoreNodeName = true)
                .WithField("Days", customSerializer: (v) =>
                {
                    StringBuilder sb1 = new StringBuilder();
                    sb1.AppendLine("<Days>");
                    foreach (var r in (IList)v)
                        sb1.AppendLine("<{0} />".FormatString(r).Indent(2, " "));
                    sb1.Append("</Days>");

                    return sb1.ToString();
                })
                )
                w.Write(new { Days = s });
            actual= sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void CustomMemberSerialization()
        {
            string expected = @"<Choices>
  <Choice>
    <Emp>
    <Id>0</Id>
    <Name>Raj</Name>
  </Emp>
    <Ids>
    <int>1</int>
    <int>2</int>
    <int>3</int>
  </Ids>
    <EmpArrs>
      <Emp>
    <Id>1</Id>
    <Name>Tom</Name>
  </Emp>
  <Emp>
    <Id>2</Id>
    <Name>Mark</Name>
  </Emp>
  </EmpArrs>
    <Options>op 1,op 2</Options>
  </Choice>
  <Choice>
    <Emp>
    <Id>0</Id>
    <Name>Raj</Name>
  </Emp>
    <Ids>
    <int>1</int>
    <int>2</int>
    <int>3</int>
  </Ids>
    <EmpArrs>
      <Emp>
    <Id>1</Id>
    <Name>Tom</Name>
  </Emp>
  <Emp>
    <Id>2</Id>
    <Name>Mark</Name>
  </Emp>
  </EmpArrs>
    <Options>op 1,op 2</Options>
  </Choice>
</Choices>";
            string actual = null;

            var sb = new StringBuilder();
            using (var p = new ChoXmlWriter<Choice>(sb)
                .WithField("Options", valueConverter: o => String.Join(",", o as string[]))
                )
            {
                List<Choice> l = new List<Choice>
                {
                    new Choice
                {
                    Options = new[] { "op 1", "op 2" },
                    EmpArr = new Emp[] { new Emp { Id = 1, Name = "Tom" }, new Emp { Id = 2, Name = "Mark" }, null },
                    Emp = new Emp {  Id = 0, Name = "Raj"},
                    //EmpDict = new Dictionary<int, Emp> { { 1, new Emp { Id = 11, Name = "Tom1" } } },
                    Ids = new List<int> { 1, 2, 3}
                },
                    new Choice
                {
                    Options = new[] { "op 1", "op 2" },
                    EmpArr = new Emp[] { new Emp { Id = 1, Name = "Tom" }, new Emp { Id = 2, Name = "Mark" }, null },
                    Emp = new Emp {  Id = 0, Name = "Raj"},
                    //EmpDict = new Dictionary<int, Emp> { { 1, new Emp { Id = 11, Name = "Tom1" } } },
                    Ids = new List<int> { 1, 2, 3}
                }
                };
                p.Write(l);
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
            //Console.WriteLine(ChoXmlWriter.ToText<Choice>(new Choice { Options = new[] { "op 1", "op 2" } }));
        }

        [Test]
        public static void CustomSerialization()
        {
            string expected = @"<Root>
  <XElement id=""dd"">
    <address>dd</address>
  </XElement>
  <XElement id=""dd"">
    <address>dd</address>
  </XElement>
</Root>";
            string actual = null;

            dynamic address = new ChoDynamicObject();
            address.Street = "10 River Rd";
            address.City = "Princeton";

            dynamic state = new ChoDynamicObject();
            state.State = "NJ";
            state.Zip = "09930";

            address.State = state;

            StringBuilder sb = new StringBuilder();
            using (var w = new ChoXmlWriter(sb)
                .WithXmlAttributeField("id")
                .WithXmlElementField("address")
                .Setup(s => s.RecordFieldWriteError += (o, e) => Console.WriteLine(e.Exception.ToString()))
                .Setup(s => s.RecordFieldSerialize += (o, e) =>
                {
                    e.Source = "dd";
                    //e.Source = "<{0}>DD</{0}>".FormatString(e.PropertyName);
                    //e.Handled = true;
                })
                )
            {
                //w.Write(new KeyValuePair<int, string>(1, "MM"));
                //w.Write(new KeyValuePair<int, string>(1, "MM"));
                w.Write(new { id = "2s->", address = address });
                w.Write(new { id = "1s->", address = address });
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void KVPTest()
        {
            StringBuilder msg = new StringBuilder();
            using (var xr = new ChoXmlWriter(msg)
                .Configure(c => c.NamespaceManager = null)
                .Configure(c => c.NullValueHandling = ChoNullValueHandling.Empty)
                //.Configure(c => c.RootName = "KVP")
                //.Configure(c => c.NodeName = "KeyValue")
                )
            {
                xr.Write((KeyValuePair<string, int>?)null);
                //xr.Write(new KeyValuePair<string, int>("X1", 1));
                xr.Write(1);
                xr.Write(2);
                //xr.Write(new KeyValuePair<string, int>("X2", 2));
            }
            Console.WriteLine(msg.ToString());

            Assert.Fail("Not sure, whats expected");
        }

        [Test]
        public static void Sample7Test()
        {

            using (var jr = new ChoJSONReader(FileNameSample7JSON).WithJSONPath("$.fathers")
                .WithField("id")
                .WithField("married", fieldType: typeof(bool))
                .WithField("name")
                .WithField("sons")
                )
            {
                using (var w = new ChoXmlWriter(FileNameSample7ActualXML))
                {
                    w.Write(jr);
                }
            }

            FileAssert.AreEqual(FileNameSample7ExpectedXML, FileNameSample7ActualXML);
        }

        [Test]
        public static void SaveStringList()
        {
            string expected = @"<Root>
  <XElement>1</XElement>
  <XElement>asas</XElement>
  <XElement/>
</Root>";
            string actual = null;

            //List<string> list = new List<string>();
            //list.Add("1/1/2012");
            //list.Add(null);
            ArrayList list = new ArrayList();
            list.Add(1);
            list.Add("asas");
            list.Add(null);

            StringBuilder msg = new StringBuilder();
            using (var w = new ChoXmlWriter(msg)
                )
                w.Write(list);

            actual =msg.ToString();
        }

        [Test]
        public static void SaveDict()
        {
            string expected = @"<DictionaryEntries>
  <DictionaryEntry>
    <Key>2</Key>
    <Value />
  </DictionaryEntry>
  <DictionaryEntry>
    <Key>1</Key>
    <Value>33</Value>
  </DictionaryEntry>
</DictionaryEntries>";
            string actual = null;

            //Dictionary<int, string> list = new Dictionary<int, string>();
            //list.Add(1, "1/1/2012");
            //list.Add(2, null);
            Hashtable list = new Hashtable();
            list.Add(1, "33");
            list.Add(2, null);

            StringBuilder msg = new StringBuilder();
            using (var w = new ChoXmlWriter(msg)
                )
                w.Write(list);
            actual =msg.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DataTableTest()
        {
            Assert.Fail("Make database testable.");

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
        
        [Test]
        public static void DataReaderTest()
        {
            Assert.Fail("Make database testable.");

            string connectionstring = @"Data Source=(localdb)\v11.0;Initial Catalog=TestDb;Integrated Security=True";
            using (var conn = new SqlConnection(connectionstring))
            {
                conn.Open();
                var comm = new SqlCommand("SELECT * FROM Customers", conn);
                using (var parser = new ChoXmlWriter("customers.xml").WithXPath("Customers/Customer"))
                    parser.Write(comm.ExecuteReader());
            }
        }


        [Test]
        public static void ConfigFirstTest()
        {
            string expected = @"<Employees>
  <Employee>
    <Id>1</Id>
    <Name>Mark</Name>
  </Employee>
  <Employee>
    <Id>2</Id>
    <Name />
  </Employee>
</Employees>";
            string actual = null;

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

                actual = reader.ReadToEnd();
            }

            Assert.AreEqual(expected, actual);
        }
        public static string FileNameQuickPOCOTestActualXML => "QuickPOCOTestActual.xml";
        public static string FileNameQuickPOCOTestExpectedXML => "QuickPOCOTestExpected.xml";
        public static string FileNameSample7JSON => "sample7.json";
        public static string FileNameSample7ExpectedXML => "sample7Expected.xml";
        public static string FileNameSample7ActualXML => "sample7Actual.xml";
        [Test]
        public static void QuickPOCOTest()
        {
            List<EmployeeRecSimple> objs = new List<EmployeeRecSimple>();

            EmployeeRecSimple rec1 = new EmployeeRecSimple();
            rec1.Id = null;
            rec1.Name = "Mark";
            rec1.Depends = new List<string>() { "AA", "BB" };
            rec1.Courses = new Dictionary<int, string>() { { 1, "AA" }, { 2, "BB" } };
            objs.Add(rec1);

            EmployeeRecSimple rec2 = new EmployeeRecSimple();
            rec2.Id = "2";
            rec2.Name = null;
            objs.Add(rec2);
            objs.Add(null);

            using (var parser = new ChoXmlWriter<EmployeeRecSimple>(FileNameQuickPOCOTestActualXML).WithXPath("Employees/Employee")
                .Configure(e => e.NullValueHandling = ChoNullValueHandling.Default)
                )
            {
                parser.Write(objs);
            }

            FileAssert.AreEqual(FileNameQuickPOCOTestExpectedXML, FileNameQuickPOCOTestActualXML);
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

        [Test]
        public static void QuickDynamicTest()
        {
            string expected = @"<Employees>
  <Employee>
    <Id>1</Id>
    <Name>Mark</Name>
    <IsActive>true</IsActive>
    <Message><![CDATA[Test]]></Message>
    <Array>
    <anyType xmlns:q1=""http://www.w3.org/2001/XMLSchema"" p3:type=""q1:int"" xmlns:p3=""http://www.w3.org/2001/XMLSchema-instance"">1</anyType>
    <anyType xmlns:q2=""http://www.w3.org/2001/XMLSchema"" p3:type=""q2:string"" xmlns:p3=""http://www.w3.org/2001/XMLSchema-instance"">abc</anyType>
  </Array>
    <Lint>
    <int>1</int>
    <int>2</int>
  </Lint>
    <Dict>
    <item>
      <key>
        <int>1</int>
      </key>
      <value>
        <string>abc</string>
      </value>
    </item>
  </Dict>
  </Employee>
  <Employee>
    <Id>2</Id>
    <Name>Jason</Name>
    <IsActive>true</IsActive>
    <Message><![CDATA[Test]]></Message>
  </Employee>
</Employees>";
            string actual = null;

            ArrayList al = new ArrayList();
            al.Add(1);
            al.Add("abc");

            List<int> lint = new List<int>() { 1, 2 };

            Hashtable ht = new Hashtable();
            ht.Add(1, "abc");

            ChoSerializableDictionary<int, string> dict = new ChoSerializableDictionary<int, string>();
            dict.Add(1, "abc");

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";
            rec1.IsActive = true;
            rec1.Message = new ChoCDATA("Test");
            rec1.Array = al;
            rec1.Lint = lint;
            //rec1.HT = ht;
            rec1.Dict = dict;
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Jason";
            rec2.IsActive = true;
            rec2.Message = new ChoCDATA("Test");
            objs.Add(rec2);

            StringBuilder sb = new StringBuilder();
            using (var parser = new ChoXmlWriter(sb).WithXPath("Employees/Employee"))
            {
                parser.Write(objs);
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);

            //var a = ChoXmlReader.LoadText(sb.ToString()).ToArray();
            //var config = new ChoXmlRecordConfiguration();
            ////config.Configure(c => c.RootName = "Root");
            //Console.WriteLine(ChoXmlWriter.ToText(a.First(), config));
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
                get { return Courses?.Select(kvp => new ChoKeyValuePair<int, string>(kvp)).ToList();  }
                set { Courses = value != null ? value.ToDictionary(v => v.Key, v => v.Value) : new Dictionary<int, string>(); }
            }
            [ChoIgnoreMember]
            public Dictionary<int, string> Courses { get; set; }
        }
    }
}
