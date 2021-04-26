using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChoETL;
using NUnit.Framework;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Collections;
using System.Dynamic;
using System.Xml.XPath;
using Newtonsoft.Json;
using System.Data.SqlClient;
using UnitTestHelper;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Net;

namespace ChoXmlReaderTest
{
    public class Applicant
    {
        [XmlAttribute(AttributeName = "social_security_number")]
        public string SSN { get; set; }
    }

    public class JobApplication
    {
        [ChoXmlAttributeRecordField(FieldName = "job_type")]
        public string JobType { get; set; }

        [ChoXmlNodeRecordField(XPath = "/JobApplicationStates/JobApplicationState/Applicants/Applicant")]
        public dynamic[] JobApplicant { get; set; }

        public override bool Equals(object obj)
        {
            var application = obj as JobApplication;
            return application != null &&
                   JobType == application.JobType &&
                   new ArrayEqualityComparer<dynamic>().Equals(JobApplicant, application.JobApplicant);
        }

        public override int GetHashCode()
        {
            var hashCode = -63495930;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(JobType);
            hashCode = hashCode * -1521134295 + new ArrayEqualityComparer<dynamic>().GetHashCode(JobApplicant);
            return hashCode;
        }
        //[ChoXmlNodeRecordField( XPath = "/JobApplicationStates/JobApplicationState/Applicants/Applicant")]
        //public Applicant[] JobApplicant { get; set; }
    }

    public class FamilyMember
    {
        [XmlAttribute("id")]
        public int Id { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
    }
    public class Item
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int? Number { get; set; }
        public DateTime? Created { get; set; }

        public override bool Equals(object obj)
        {
            var item = obj as Item;
            return item != null &&
                   ItemId == item.ItemId &&
                   ItemName == item.ItemName &&
                   EqualityComparer<int?>.Default.Equals(Number, item.Number) &&
                   EqualityComparer<DateTime?>.Default.Equals(Created, item.Created);
        }

        public override int GetHashCode()
        {
            var hashCode = 1225882131;
            hashCode = hashCode * -1521134295 + ItemId.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ItemName);
            hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Number);
            hashCode = hashCode * -1521134295 + EqualityComparer<DateTime?>.Default.GetHashCode(Created);
            return hashCode;
        }
    }
    public class SelectedIds
    {
        [XmlElement]
        public int[] Id;

        public override bool Equals(object obj)
        {
            var ids = obj as SelectedIds;
            return ids != null &&
                   new ArrayEqualityComparer<int>().Equals(Id, ids.Id);
        }

        public override int GetHashCode()
        {
            return 2108858624 + new ArrayEqualityComparer<int>().GetHashCode(Id);
        }
    }

    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }

        public override bool Equals(object obj)
        {
            var person = obj as Person;
            return person != null &&
                   FirstName == person.FirstName &&
                   LastName == person.LastName &&
                   DateOfBirth == person.DateOfBirth &&
                   Address == person.Address;
        }

        public override int GetHashCode()
        {
            var hashCode = 330419756;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FirstName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LastName);
            hashCode = hashCode * -1521134295 + DateOfBirth.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Address);
            return hashCode;
        }
    }

    public class RoomInfo
    {
        public int RoomNumber { get; set; }

        public override bool Equals(object obj)
        {
            var info = obj as RoomInfo;
            return info != null &&
                   RoomNumber == info.RoomNumber;
        }

        public override int GetHashCode()
        {
            return 929369503 + RoomNumber.GetHashCode();
        }
    }

    public class Table
    {
        public string Color { get; set; }

        public override bool Equals(object obj)
        {
            var table = obj as Table;
            return table != null &&
                   Color == table.Color;
        }

        public override int GetHashCode()
        {
            return -1200350280 + EqualityComparer<string>.Default.GetHashCode(Color);
        }
    }

    public class Emp
    {
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
    }

    public class MyMyfields
    {
        [ChoXmlNodeRecordField(XPath = "my:Admin")]
        public MyAdmin Admin { get; set; }
        [ChoXmlNodeRecordField(XPath = "my:Request_Status")]
        public string Request_Status { get; set; }
        [ChoXmlNodeRecordField(XPath = "my:Request_Type")]
        public string Request_Type { get; set; }

        public override bool Equals(object obj)
        {
            var myfields = obj as MyMyfields;
            return myfields != null &&
                   EqualityComparer<MyAdmin>.Default.Equals(Admin, myfields.Admin) &&
                   Request_Status == myfields.Request_Status &&
                   Request_Type == myfields.Request_Type;
        }

        public override int GetHashCode()
        {
            var hashCode = 1828740556;
            hashCode = hashCode * -1521134295 + EqualityComparer<MyAdmin>.Default.GetHashCode(Admin);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Request_Status);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Request_Type);
            return hashCode;
        }
    }

    public class MyAdmin
    {
        public MyRouting_Order Routing_Order { get; set; }

        public override bool Equals(object obj)
        {
            var admin = obj as MyAdmin;
            return admin != null &&
                   EqualityComparer<MyRouting_Order>.Default.Equals(Routing_Order, admin.Routing_Order);
        }

        public override int GetHashCode()
        {
            return -300207606 + EqualityComparer<MyRouting_Order>.Default.GetHashCode(Routing_Order);
        }
    }

    public class MyRouting_Order
    {
        [XmlElement("Approver-1_Order")]
        public string Approver1_Order { get; set; }
        [XmlElement("Approver-2_Order")]
        public string Approver2_Order { get; set; }
        [XmlElement("Approver-3_Order")]
        public string Approver3_Order { get; set; }

        public override bool Equals(object obj)
        {
            var order = obj as MyRouting_Order;
            return order != null &&
                   Approver1_Order == order.Approver1_Order &&
                   Approver2_Order == order.Approver2_Order &&
                   Approver3_Order == order.Approver3_Order;
        }

        public override int GetHashCode()
        {
            var hashCode = 2046693555;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Approver1_Order);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Approver2_Order);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Approver3_Order);
            return hashCode;
        }
    }

    public class EmployeeRec1
    {
        [ChoXmlNodeRecordField(XPath = "@Id")]
        public int Id { get; set; }
        [ChoXmlNodeRecordField(XPath = "Name")]
        public string Name { get; set; }
        [ChoXmlNodeRecordField(XPath = "/Address")]
        public AddressRec Address { get; set; }

        public override bool Equals(object obj)
        {
            var rec = obj as EmployeeRec1;
            return rec != null &&
                   Id == rec.Id &&
                   Name == rec.Name &&
                   EqualityComparer<AddressRec>.Default.Equals(Address, rec.Address);
        }

        public override int GetHashCode()
        {
            var hashCode = 1983353833;
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<AddressRec>.Default.GetHashCode(Address);
            return hashCode;
        }
    }

    public partial class AddressRec
    {
        [XmlElement(ElementName = "AddressLine")]
        public AddressLineRec[] AddressLines { get; set; }
        [XmlElement(ElementName = "Country")]
        public string Country { get; set; }

        public override bool Equals(object obj)
        {
            var rec = obj as AddressRec;
            return rec != null &&
                   new ArrayEqualityComparer<AddressLineRec>().Equals(AddressLines, rec.AddressLines) &&
                   Country == rec.Country;
        }

        public override int GetHashCode()
        {
            var hashCode = -299856257;
            hashCode = hashCode * -1521134295 + new ArrayEqualityComparer<AddressLineRec>().GetHashCode(AddressLines);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Country);
            return hashCode;
        }
    }

    public partial class AddressLineRec
    {
        [XmlAttribute(AttributeName = "Id")]
        public int Id { get; set; }
        [XmlText]
        public string AddressLine { get; set; }

        public override bool Equals(object obj)
        {
            var rec = obj as AddressLineRec;
            return rec != null &&
                   Id == rec.Id &&
                   AddressLine == rec.AddressLine;
        }

        public override int GetHashCode()
        {
            var hashCode = -633335143;
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AddressLine);
            return hashCode;
        }
    }

    public class SyncInvoice
    {
        [ChoXmlNodeRecordField(XPath = "@languageCode")]
        public string LanguageCode { get; set; }
        [ChoXmlNodeRecordField(XPath = "/x:ApplicationArea")]
        public ApplicationArea ApplicationArea { get; set; }

        public override bool Equals(object obj)
        {
            var invoice = obj as SyncInvoice;
            return invoice != null &&
                   LanguageCode == invoice.LanguageCode &&
                   EqualityComparer<ApplicationArea>.Default.Equals(ApplicationArea, invoice.ApplicationArea);
        }

        public override int GetHashCode()
        {
            var hashCode = -1552420970;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LanguageCode);
            hashCode = hashCode * -1521134295 + EqualityComparer<ApplicationArea>.Default.GetHashCode(ApplicationArea);
            return hashCode;
        }
    }

    public class ApplicationArea
    {
        public Sender Sender { get; set; }
        public DateTime CreationDateTime { get; set; }
        public string BODID { get; set; }

        public override bool Equals(object obj)
        {
            var area = obj as ApplicationArea;
            return area != null &&
                   EqualityComparer<Sender>.Default.Equals(Sender, area.Sender) &&
                   CreationDateTime == area.CreationDateTime &&
                   BODID == area.BODID;
        }

        public override int GetHashCode()
        {
            var hashCode = 553382520;
            hashCode = hashCode * -1521134295 + EqualityComparer<Sender>.Default.GetHashCode(Sender);
            hashCode = hashCode * -1521134295 + CreationDateTime.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BODID);
            return hashCode;
        }
    }

    public class Sender
    {
        public string LogicalID { get; set; }
        public string ComponentID { get; set; }
        public string ConfirmationCode { get; set; }

        public override bool Equals(object obj)
        {
            var sender = obj as Sender;
            return sender != null &&
                   LogicalID == sender.LogicalID &&
                   ComponentID == sender.ComponentID &&
                   ConfirmationCode == sender.ConfirmationCode;
        }

        public override int GetHashCode()
        {
            var hashCode = -148620913;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LogicalID);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ComponentID);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ConfirmationCode);
            return hashCode;
        }
    }

    public class Car : IChoNotifyRecordFieldConfigurable, IChoNotifyRecordConfigurable
    {
        public string StockNumber { get; set; }
        public string Make { get; set; }
        public XmlNode[] Models { get; set; }

        public void RecondConfigure(ChoRecordConfiguration configuration)
        {
        }

        public void RecondFieldConfigure(ChoRecordFieldConfiguration fieldConfiguration)
        {
            if (fieldConfiguration.Name == nameof(Models))
            {
                ((ChoXmlRecordFieldConfiguration)fieldConfiguration).XPath = "//model";
            }
        }
    }

    public class EmpWithCurrency
    {
        public int Id { get; set; }
        public ChoCurrency Salary { get; set; }
    }

    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    public class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;

            GenerateXmlFromDatatable();
        }

        static void GenerateXmlFromDatatable()
        {
            using (var r = new ChoXmlReader("sample92.xml")
                .WithMaxScanNodes(2)
                .WithXPath("Order")
                )
            {
                var dt = r.AsDataTable();
                StringBuilder xml = new StringBuilder();

                using (var w = new ChoXmlWriter(xml))
                {
                    w.Write(dt);
                }

                Console.WriteLine(xml.ToString());
            }
        }

        static void TransformXml()
        {
            string xml = @"<root>
       <lastname>Mark</lastname>
       <firstname>Tom</firstname>
       <student>
          <id>1234</id>
          <ssn>123-21-2234</ssn>
       </student>
    </root>";

            StringBuilder outXml = new StringBuilder();
            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("/"))
            {
                using (var w = new ChoXmlWriter(outXml)
                    .IgnoreRootName()
                    .Configure(c => c.NodeName = "root"))
                {
                    w.Write(r.Select(r1 => new
                    {
                        r1.lastName,
                        r1.firstName,
                        studentId = r1.student.id,
                        studentSsn = r1.student.ssn
                    }));
                }
            }
            Console.WriteLine(outXml.ToString());
        }

        [XmlRoot("bagInfo", Namespace = "http://www.kadaster.nl/schemas/lvbag/extract-deelbestand-mutaties-lvc/v20200601")]
        public class bagInfo
        {
            [XmlElement("Gebied-Registratief", Namespace = "http://www.kadaster.nl/schemas/lvbag/extract-selecties/v20200601")]
            public GebiedRegistratief GebiedRegistratief { get; set; }
        }

        public class GebiedRegistratief
        {
        }

        static void ReadAllNS()
        {
            IDictionary<string, string> ns = null;
            using (var r = new ChoXmlReader<bagInfo>("sample95.xml")
            )
            {
                var rec = r.FirstOrDefault();
                ns = r.Configuration.GetXmlNamespacesInScope();
            }
            using (var r = new ChoXmlReader<bagInfo>("sample95.xml")
                .UseXmlSerialization()
                .WithXmlNamespaces(ns)
                )
            {
                foreach (var rec in r)
                {
                    ns = r.Configuration.GetXmlNamespacesInScope();
                    Console.WriteLine(r.Configuration.GetXmlNamespacesInScope().Dump());
                    Console.WriteLine(rec.Dump());
                }
            }

            StringBuilder xml = new StringBuilder();
            using (var w = new ChoXmlWriter<bagInfo>(xml)
                .WithXmlNamespaces(ns)
                .WithDefaultNamespacePrefix("mlm")
                .UseXmlSerialization()
                .WithRootName("bagMutaties")
                .Configure(c => c.OmitXmlDeclaration = false)
                .Configure(c => c.Encoding = Encoding.UTF8)
                .Configure(c => c.XmlVersion = "1.2")
                )
            {
                w.Write(new bagInfo
                {
                });
            }

            Console.WriteLine(xml.ToString());
        }

        public class Root
        {
            [ChoXmlNodeRecordField(XPath = "//property1")]
            public string[] Properties { get; set; }
            [ChoXmlNodeRecordField(XPath = "//amount/*")]
            public double[] Amounts { get; set; }
        }

        static void Test100()
        {
            string xml = @"<root>
 <property1>a</property1>
 <property1>b</property1>
 <property1>c</property1>
 <amount>
  <EUR type=""integer"">1000</EUR>
  <USD type=""integer"">1100</USD>
 </amount>
</root>";

            using (var r = ChoXmlReader<Root>.LoadText(xml)
                .WithXPath("/")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }

        }

        [ChoXmlRecordObject(XPath = "/")]
        public class ABCX
        {
            [ChoXmlNodeRecordField(XPath = "/Header/Date")]
            public string Header { get; set; }
            [ChoXmlNodeRecordField(XPath = "/Document")]
            public string[] Document { get; set; }
        }

        static void ElementsToArray()
        {
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ABC>
  <Header>
    <Date>2020-03-20T09:08:29Z</Date>
    <Code>A101</Code>    
  </Header>
  <Document>
    <AAA>Test Data 123</AAA>
    <BBB>Test Date 456</BBB>
  </Document>
</ABC>";

            using (var r = ChoXmlReader<ABCX>.LoadText(xml)
                //.WithXPath("/")
                //.WithField("Date", xPath: "/Header/Date")
                //.WithField("Code", xPath: "/Header/Code")
                //.WithField("Document", xPath: "/Document", fieldType: typeof(string[]))
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }


        public class Item
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; }
            public int? Number { get; set; }
        }

        static void MemoryTest()
        {
            string xml = @"<?xml version=""1.0""?>
    <Item Number = ""100"" ItemName = ""TestName1"" ItemId = ""1"" />";

            XDocument doc = XDocument.Parse(xml);

            var i = 0;
            while (i < 1000000)
            {
                var entity = ChoXmlReader<Item>.LoadXElement(doc.Root);
                Console.WriteLine(entity.Dump());
                i++;
            }
        }

        static void Soap2JSONTest()
        {
            string soap = @"<SOAP-ENV:Envelope
    xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/""
    xmlns:SOAP-ENC=""http://schemas.xmlsoap.org/soap/encoding/""
    xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
    xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
    <SOAP-ENV:Header>
        <reportname>ReportName</reportname>
        <reportstartdate>2020-Jun-1</reportstartdate>
        <reportenddate>2020-Jun-1</reportenddate>
    </SOAP-ENV:Header>
    <SOAP-ENV:Body>
        <reportresponse>
            <row>
                <rowid>1</rowid>
                <value1>1</value1>
                <value2>1</value2>
                <value3>1</value3>
            </row>
        </reportresponse>
    </SOAP-ENV:Body>
</SOAP-ENV:Envelope>";


            StringBuilder json = new StringBuilder();
            using (var r = ChoXmlReader.LoadText(soap)
                .WithXmlNamespace("SOAP-ENV", "http://schemas.xmlsoap.org/soap/envelope/")
                .WithXPath("//SOAP-ENV:Envelope")
                )
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r.Select(r1 =>
                    {
                        var item = r1.Body.reportresponse.row;
                        r1.Body.reportresponse["row"] = new object[] { item };
                        return r1;
                    }));
            }

            Console.WriteLine(json.ToString());
        }

        static void LoadXmlFragmentTest()
        {
            string xml = @"
  <Emp>
    <Id>10</Id>
    <Salary>$2000</Salary>
  </Emp>
  <Emp>
    <Id>20</Id>
    <Salary>$10,000</Salary>
  </Emp>
";

            using (var r = ChoXmlReader.LoadxmlFragment(xml)
                .WithMaxScanNodes(10)
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }

        }

        static void CurrencyDynamicTest()
        {
            string xml = @"<Emps>
  <Emp>
    <Id>10</Id>
    <Salary>$2000</Salary>
  </Emp>
  <Emp>
    <Id>20</Id>
    <Salary>$10,000</Salary>
  </Emp>
</Emps>
";

            using (var r = ChoXmlReader.LoadText(xml)
                .WithMaxScanNodes(10)
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void CurrencyTest()
        {
            string xml = @"<Emps>
  <Emp>
    <Id>10</Id>
    <Salary>$2000</Salary>
  </Emp>
  <Emp>
    <Id>20</Id>
    <Salary>$10,000</Salary>
  </Emp>
</Emps>
";

            using (var r = ChoXmlReader<EmpWithCurrency>.LoadText(xml)
                .WithMaxScanNodes(10)
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void XNameWithSpaceTest()
        {
            string xml = @"<Emps>
  <Emp>
    <Id>10</Id>
    <Name>Tom</Name>
  </Emp>
  <Emp>
    <Id>20</Id>
  </Emp>
</Emps>
";

            using (var r = ChoXmlReader.LoadText(xml)
                .WithFields("Id", "First Name")
                .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Any)
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void DefaultValueTest()
        {
            string xml = @"<Emps>
  <Emp>
    <Id>10</Id>
    <Name>Tom</Name>
  </Emp>
  <Emp>
    <Id>20</Id>
  </Emp>
</Emps>
";

            using (var r = ChoXmlReader.LoadText(xml)
                .WithField("Id", fieldType: typeof(int))
                .WithField("Name", fieldType: typeof(string), defaultValue: "Markx")
                .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Any)
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void FallbacktValueTest()
        {
            string xml = @"<Emps>
  <Emp>
    <Id>10</Id>
    <Name>Tom</Name>
  </Emp>
  <Emp>
    <Id>2x</Id>
    <Name>Mark</Name>
  </Emp>
</Emps>
";

            using (var r = ChoXmlReader.LoadText(xml)
                .WithField("Id", fieldType: typeof(int), fallbackValue: 200)
                .WithField("Name", fieldType: typeof(string), defaultValue: "Markx")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void HugeXml2Json()
        {
            StringBuilder json = new StringBuilder();
            using (var r = new ChoXmlReader("sample94.xml")
                .WithXPath("/root/hugeArray/item")
                )
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }

            Console.WriteLine(json.ToString());
        }

        static void Xml2JSON2()
        {
            string xml = @"<map version=""1.2"" tiledversion=""1.3.1"" orientation=""orthogonal"" renderorder=""right-down"" compressionlevel=""0"" width=""80"" height=""50"" tilewidth=""16"" tileheight=""16"" infinite=""0"" nextlayerid=""2"" nextobjectid=""1"">
 <tileset firstgid=""1"" name=""TilesetSA"" tilewidth=""16"" tileheight=""16"" tilecount=""4000"" columns=""80"">
  <image source=""../../TilesetSA.png"" width=""1280"" height=""800""/>
 </tileset>
 <layer id=""1"" name=""Walls"" width=""80"" height=""50"">
  <data encoding=""csv"">
3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,81,81,81,81,81,81,0,0,0,0,
0,0,0,0,0,0,0,0,0,0,0,0,0,0,
0,0,0,3,3,3,3,3,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,
</data>
 </layer>
</map>";

            StringBuilder json = new StringBuilder();
            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("/")
                )
            {
                var dt = r.AsDataTable();

                //Console.WriteLine(r.First().layer.data.GetText());
                using (var w = new ChoJSONWriter(json))
                {
                    w.Write(r);
                }
            }
            Console.WriteLine(json.ToString());
        }

        static void ToKVPTest()
        {
            string xml = @"<test-run>
 <test-suite>
   <test-case id=""1234"" name=""ABC"" result=""Passed"">
   </test-case>
 </test-suite>
</test-run>";
            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("//test-case")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void ExtractTextFromXml()
        {
            string xml = @"<PolicyResponseMessage xmlns=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CourtPolicyResponseMessage-4.0"" xmlns:j=""http://niem.gov/niem/domains/jxdm/4.0"" 
xmlns:nc=""http://niem.gov/niem/niem-core/2.0"" xmlns:mark=""urn:mark:ecf:extensions:Common"" xmlns:ecf=""urn:oasis:names:tc:legalxml-courtfiling:schema:xsd:CommonTypes-4.0"">
      <RuntimePolicyParameters>
        <CourtCodelist>
          <ECFElementName>nc:CaseCategoryText</ECFElementName>
          <EffectiveDate>
            <nc:Date>2012-10-10</nc:Date>
          </EffectiveDate>
          <CourtCodelistURI>
            <nc:IdentificationID>https://Test1.com</nc:IdentificationID>
          </CourtCodelistURI>
        </CourtCodelist>
        <CourtCodelist>
          <ECFElementName>mark:CaseTypeText</ECFElementName>
          <EffectiveDate>
            <nc:Date>2012-10-10</nc:Date>
          </EffectiveDate>
          <CourtCodelistURI>
            <nc:IdentificationID>https://Test2.com</nc:IdentificationID>
          </CourtCodelistURI>
        </CourtCodelist>
      </RuntimePolicyParameters>
    </PolicyResponseMessage>";

            using (var r = ChoXmlReader.LoadText(xml)
                .WithXmlNamespace("nc", "http://niem.gov/niem/niem-core/2.0")
                .WithXPath("//nc:IdentificationID/text()")
                )
            {
                Console.WriteLine(r.Select(kvp => kvp.Value).ToArray().Dump());
            }
        }

        static void FilterNodesTest()
        {
            string xml = @"<ABC>
  <NAMEDETAILS></NAMEDETAILS>
  <PRODUCT>
    <PRODUCTDETAILS>
      <ProductName>
         <name>Car</name>
         <name>lorry</name>
         <name>Car</name>
      </ProductName>
    </PRODUCTDETAILS>
    <PRODUCTDETAILS>
      <ProductName>
         <name>van</name>
         <name>cycle</name>
         <name>bus</name>
      </ProductName>
    </PRODUCTDETAILS>
    <PRODUCTDETAILS>
      <ProductName>
         <name>car</name>
         <name>cycle</name>
         <name>bus</name>
      </ProductName>
    </PRODUCTDETAILS>
  </PRODUCT>    
</ABC>";

            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("//PRODUCTDETAILS")
                )
            {
                foreach (var rec in r)
                {
                    if (((IList)rec.ProductName).Contains("Car"))
                        Console.WriteLine(rec.Dump());
                }
            }
        }

        static void Sample93Test()
        {
            StringBuilder json = new StringBuilder();
            using (var r = new ChoXmlReader("sample93.xml")
                .Configure(c => c.DefaultNamespacePrefix = null)
                .WithXPath("//os:featureMember")
                )
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }

            Console.WriteLine(json.ToString());
        }

        static void LoadSoapXmlTest()
        {
            string xml = @"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tmp=""http://tempuri.org/"">
  <soap:Body>
    <tmp:listdata>
      <tmp:Name>00141169</tmp:Name>
      <tmp:CurrencyCode>EUR</tmp:CurrencyCode>
      <tmp:Date>2020-04-03</tmp:Date>
    </tmp:listdata>
  </soap:Body>
</soap:Envelope>";

            using (var r = ChoXmlReader.LoadText(xml)
                             .Configure(c => c.NamespaceManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/"))
                    .Configure(c => c.NamespaceManager.AddNamespace("tmp", "http://tempuri.org/"))
                    .Configure(c => c.RootName = "soap:Envelope")
                    .Configure(c => c.NodeName = "Body")
                    .Configure(c => c.DefaultNamespacePrefix = "tmp")
       )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void TestSample91()
        {
            StringBuilder csv = new StringBuilder();

            using (var r = new ChoXmlReader("sample91.xml")
                .WithXPath("//StudentRequest")
                .WithField("StudentFirstName")
                .WithField("Email", xPath: @"/VariableData/Variable[@name='Email']/text()")
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .UseNestedKeyFormat(false)
                    )
                    w.Write(r);
            }

            Console.WriteLine(csv.ToString());
        }

        static void TestSample92()
        {
            StringBuilder csv = new StringBuilder();

            using (var r = new ChoXmlReader("sample92.xml")
                .WithXPath("//Order")
                )
            {
                //using (var w = new ChoJSONWriter(csv))
                //    w.Write(r);
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .UseNestedKeyFormat(true)
                    .Configure(c => c.IgnoreRootNodeName = true)
                    .Setup(s => s.FileHeaderArrange += (o, e) =>
                    {
                        e.Fields = e.Fields.Select(f =>
                        {
                            if (f == "Customer_#text")
                                return "Customer_ID";
                            else
                                return f;
                        }).ToList();
                    })
                    )
                    w.Write(r);
            }

            Console.WriteLine(csv.ToString());
        }

        static void PartialLoadTest()
        {
            using (var r = new ChoXmlReader<Car>("sample49.xml")
                .WithXPath("/Car")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void XmlRead1()
        {
            string csv = @"<Flusso>
  <Affidamento IdAffidamento=""2325"">
   <Pratica IdPratica=""0010193043084620""></Pratica>  
   <Pratica IdPratica=""0010193043084611""></Pratica>  
   </Affidamento>
  <Affidamento IdAffidamento=""2325"">  
    <Pratica IdPratica=""0010193043084621""></Pratica>
   </Affidamento>
</Flusso>
";

            using (var r = ChoXmlReader.LoadText(csv)
                .WithField("IdAffidamento")
                .WithField("Pratica", isArray: true)
                )
            {
                foreach (var e in r)
                {
                    Console.WriteLine(e.IdAffidamento);
                    foreach (var Pratica in e.Pratica)
                        Console.WriteLine(Pratica.IdPratica);
                }
            }
        }

        static void Xml2JSONWithTabs()
        {
            string xml = @"<Request>
 <HEADER>
    <uniqueID>2019111855545921230</uniqueID>
 </HEADER>
 <DETAIL>
<cmnmGrp>
  <coNm>IS XYZ INC.</coNm>
  <embossedNm>ANNA ST       UART</embossedNm>
  <cMNm>ST      UART/ANNA K</cMNm>
  <cmfirstNm>ANNA</cmfirstNm>
  <cmmiddleNm>K</cmmiddleNm>
  <cm2NdLastNm>ST       UART</cm2NdLastNm>
</cmnmGrp>
</DETAIL>
</Request>";


            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath(@"/")
                )
            {
                Console.WriteLine(ChoJSONWriter.ToTextAll(r));
            }

        }

        [XmlRoot("ButikOmbud")]
        public abstract class AssortmentViewModel
        {
            public string Typ { get; set; }
            public string Nr { get; set; }
        }

        [XmlRoot("StoreAssortmentView")]
        public class StoreAssortmentViewModel : AssortmentViewModel
        {

        }
        [XmlRoot("AgentAssortmentView")]
        public class AgentAssortmentViewModel : AssortmentViewModel
        {

        }

        static void XmlTypeTest()
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<ButikerOmbud xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <Info>
        <Meddelande>blah blah</Meddelande>
    </Info>
    <ButikOmbud xsi:type=""StoreAssortmentViewModel"">
        <Typ>Butik</Typ><Nr>2515</Nr>
    </ButikOmbud>
    <ButikOmbud xsi:type=""StoreAssortmentViewModel"">
        <Typ>Butik</Typ><Nr>2516</Nr>
    </ButikOmbud>
    <ButikOmbud xsi:type=""AgentAssortmentViewModel"">
        <Typ>Ombud</Typ><Nr>011703-91A</Nr>
    </ButikOmbud>
    <ButikOmbud xsi:type=""AgentAssortmentViewModel"">
        <Typ>Ombud</Typ><Nr>011703-92B</Nr>
    </ButikOmbud>
</ButikerOmbud>";

            string xml1 = @"<ButikerOmbud xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <StoreAssortmentView>
    <Typ>Butik</Typ>
    <Nr>2515</Nr>
  </StoreAssortmentView>
  <StoreAssortmentView>
    <Typ>Butik</Typ>
    <Nr>2516</Nr>
  </StoreAssortmentView>
  <AgentAssortmentView>
    <Typ>Ombud</Typ>
    <Nr>011703-91A</Nr>
  </AgentAssortmentView>
  <AgentAssortmentView>
    <Typ>Ombud</Typ>
    <Nr>011703-92B</Nr>
  </AgentAssortmentView>
</ButikerOmbud>";

            StringBuilder output = new StringBuilder();
            using (var w = new ChoXmlWriter<AssortmentViewModel>(output)
                    .Configure(c => c.UseXmlSerialization = true)
                    .WithRootName("ButikerOmbud")
                //.Configure(c => c.XmlSerializer = new XmlSerializer(typeof(AssortmentViewModel), new Type[] { typeof(StoreAssortmentViewModel), typeof(AgentAssortmentViewModel) }))
                )
            {
                foreach (var rec in ChoXmlReader<AssortmentViewModel>.LoadText(xml)
                    .WithXPath("/ButikOmbud")
                    .WithXmlNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
                    .Configure(c => c.DefaultNamespacePrefix = null)
                    .Configure(c => c.IncludeSchemaInstanceNodes = true)
                    .Configure(c => c.UseXmlSerialization = true)
                    //.Configure(c => c.XmlSerializer = new XmlSerializer(typeof(AssortmentViewModel), new Type[] { typeof(StoreAssortmentViewModel), typeof(AgentAssortmentViewModel) }))
                    //.WithXmlNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
                    //.WithMaxScanNodes(1)
                    )
                {
                    Console.WriteLine(rec.Dump());
                    w.Write(rec);
                }
            }

            Console.WriteLine(output);
        }

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }
        public void Test1()
        {
            HttpWebRequest request = HttpWebRequest.Create("https://www.wired.com/feed/") as HttpWebRequest;

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            using (Stream responseStream = response.GetResponseStream())
                foreach (var item in new ChoXmlReader(responseStream))
                {
                    Console.WriteLine(item.ToStringEx());
                }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        //[Test]
        public static void Xml2CSV2()
        {
            string expected = @"CentreName,Country,CustomerId,DOB,Email,ExpiryDate
Corporate Office,Austria,379,25/02/1991,farah@gmail.com,3/1/2020 8:01:00 AM
Corporate Office,Egypt,988915,01/03/1986,hesh.a.metwally@gmail.com,7/1/2020 11:38:00 AM";
            string actual = null;

            StringBuilder sb = new StringBuilder();

            using (var r = new ChoXmlReader(FileNameSample22XML)
                .WithXPath("b:MarketingAllCardholderData")
                .WithXmlNamespace("a", "schemas.datacontract.org/2004/07/ExternalClient.Responses")
                .WithXmlNamespace("b", "schemas.datacontract.org/2004/07/ExternalClient.Data.Classes")
                )
            {
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    .Configure(c => c.UseNestedKeyFormat = false)
                    )
                    w.Write(r);
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Xml2CSV1()
        {
            string expected = @"ARandomRoot-ARandomLOne-Id,ARandomRoot-ARandomLOne-OtherId,ARandomRoot-AnotherRandomLOne-ARandomLTwo-ARandomLTree-NumberOfElements,ARandomRoot-AnotherRandomLOne-ARandomLTwo-ARandomLTree-ARandomLFour-RandomDataOne,ARandomRoot-AnotherRandomLOne-ARandomLTwo-ARandomLTree-ARandomLFour-RandomDataTwo
12,34,2,R1,10.12
12,34,2,R2,9.8";
            string actual = null;

            string xml = @"<ARandomRoot>
  <ARandomLOne>
    <Id>12</Id>
    <OtherId>34</OtherId>    
  </ARandomLOne>
  <AnotherRandomLOne>
    <ARandomLTwo>
      <ARandomLTree>
        <NumberOfElements>2</NumberOfElements>
        <ARandomLFour>
          <RandomDataOne>R1</RandomDataOne>
          <RandomDataTwo>10.12</RandomDataTwo>          
        </ARandomLFour>
        <ARandomLFour>
          <RandomDataOne>R2</RandomDataOne>
          <RandomDataTwo>9.8</RandomDataTwo>          
        </ARandomLFour>
      </ARandomLTree>
    </ARandomLTwo>
  </AnotherRandomLOne>
</ARandomRoot>";

            StringBuilder csv = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml)
                .WithXPath("/")
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.NestedColumnSeparator = '-')
                    )
                    w.Write(p.SelectMany(r =>
                        ((dynamic[])r.AnotherRandomLOne.ARandomLTwo.ARandomLTree.ARandomLFours).Select(r1 => new
                        {
                            ARandomRoot = new
                            {
                                ARandomLOne = r.ARandomLOne,
                                AnotherRandomLOne = new
                                {
                                    ARandomLTwo = new
                                    {
                                        ARandomLTree = new
                                        {
                                            NumberOfElements = r.AnotherRandomLOne.ARandomLTwo.ARandomLTree.NumberOfElements,
                                            ARandomLFour = r1
                                        }
                                    }
                                }
                            }
                        })
                    ));
            }
            actual = csv.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample50Test()
        {
            string expected = @"targetMarketAttributes/targetMarket,targetMarketAttributes/alternateItemIdentificationList/alternateItemIdentification/0/agency,targetMarketAttributes/alternateItemIdentificationList/alternateItemIdentification/0/id,targetMarketAttributes/alternateItemIdentificationList/alternateItemIdentification/1/agency,targetMarketAttributes/alternateItemIdentificationList/alternateItemIdentification/1/id,targetMarketAttributes/shortDescriptionList/shortDescription/lang,targetMarketAttributes/shortDescriptionList/shortDescription/#text,targetMarketAttributes/productDescriptionList/productDescription/lang,targetMarketAttributes/productDescriptionList/productDescription/#text,targetMarketAttributes/additionalDescriptionList/additionalDescription/lang,targetMarketAttributes/additionalDescriptionList/additionalDescription/#text,targetMarketAttributes/isDispatchUnitList/isDispatchUnit,targetMarketAttributes/isInvoiceUnitList/isInvoiceUnit,targetMarketAttributes/isOrderableUnitList/isOrderableUnit,targetMarketAttributes/packagingMarkedReturnable,targetMarketAttributes/minimumTradeItemLifespanFromProductionList/minimumTradeItemLifespanFromProduction,targetMarketAttributes/nonGTINPalletHi,targetMarketAttributes/nonGTINPalletTi,targetMarketAttributes/numberOfItemsPerPallet,targetMarketAttributes/hasBatchNumber,targetMarketAttributes/productMarkedRecyclable,targetMarketAttributes/depth/uom,targetMarketAttributes/depth/#text,targetMarketAttributes/height/uom,targetMarketAttributes/height/#text,targetMarketAttributes/width/uom,targetMarketAttributes/width/#text,targetMarketAttributes/grossWeight/uom,targetMarketAttributes/grossWeight/#text,targetMarketAttributes/netWeight/uom,targetMarketAttributes/netWeight/#text,targetMarketAttributes/totalUnitsPerCase,targetMarketAttributes/preDefinedFlex/alternateClassificationList/alternateClassification/0/code,targetMarketAttributes/preDefinedFlex/alternateClassificationList/alternateClassification/0/scheme,targetMarketAttributes/preDefinedFlex/alternateClassificationList/alternateClassification/1/code,targetMarketAttributes/preDefinedFlex/alternateClassificationList/alternateClassification/1/scheme,targetMarketAttributes/preDefinedFlex/alternateClassificationList/alternateClassification/2/code,targetMarketAttributes/preDefinedFlex/alternateClassificationList/alternateClassification/2/scheme,targetMarketAttributes/preDefinedFlex/alternateClassificationList/alternateClassification/3/code,targetMarketAttributes/preDefinedFlex/alternateClassificationList/alternateClassification/3/scheme,targetMarketAttributes/preDefinedFlex/alternateClassificationList/alternateClassification/4/code,targetMarketAttributes/preDefinedFlex/alternateClassificationList/alternateClassification/4/scheme,targetMarketAttributes/preDefinedFlex/brandOwnerAdditionalTradeItemIdentificationList/brandOwnerAdditionalTradeItemIdentification/brandOwnerAdditionalIdType,targetMarketAttributes/preDefinedFlex/brandOwnerAdditionalTradeItemIdentificationList/brandOwnerAdditionalTradeItemIdentification/brandOwnerAdditionalIdValue,targetMarketAttributes/preDefinedFlex/consumerSalesConditionList/consumerSalesCondition,targetMarketAttributes/preDefinedFlex/countryOfOriginList/countryOfOrigin,targetMarketAttributes/preDefinedFlex/dataCarrierList/dataCarrierTypeCode,targetMarketAttributes/preDefinedFlex/donationIdentificationNumberMarked,targetMarketAttributes/preDefinedFlex/doesTradeItemContainLatex,targetMarketAttributes/preDefinedFlex/exemptFromFDAPreMarketAuthorization,targetMarketAttributes/preDefinedFlex/fDA510KPremarketAuthorization,targetMarketAttributes/preDefinedFlex/fDAMedicalDeviceListingList/fDAMedicalDeviceListing,targetMarketAttributes/preDefinedFlex/gs1TradeItemIdentificationKey/code,targetMarketAttributes/preDefinedFlex/gs1TradeItemIdentificationKey/value,targetMarketAttributes/preDefinedFlex/isTradeItemManagedByManufactureDate,targetMarketAttributes/preDefinedFlex/manufacturerList/manufacturer/gln,targetMarketAttributes/preDefinedFlex/manufacturerDeclaredReusabilityType,targetMarketAttributes/preDefinedFlex/mRICompatibilityCode,targetMarketAttributes/preDefinedFlex/serialNumberLocationCodeList/serialNumberLocationCode,targetMarketAttributes/preDefinedFlex/tradeChannelList/tradeChannel,targetMarketAttributes/preDefinedFlex/tradeItemContactInfoList/tradeItemContactInfo/availableTime/lang,targetMarketAttributes/preDefinedFlex/tradeItemContactInfoList/tradeItemContactInfo/availableTime/#text,targetMarketAttributes/preDefinedFlex/tradeItemContactInfoList/tradeItemContactInfo/contactInfoGLN,targetMarketAttributes/preDefinedFlex/tradeItemContactInfoList/tradeItemContactInfo/contactType,targetMarketAttributes/preDefinedFlex/tradeItemContactInfoList/tradeItemContactInfo/targetMarketCommunicationChannel/communicationChannelList/communicationChannel/communicationChannelCode,targetMarketAttributes/preDefinedFlex/uDIDDeviceCount
US,Example,31321,Example,1,en,Example,en,Example,en,Example,No,No,No,No,1825,0,0,0,Yes,No,in,12,in,8,in,12,lb,0.3213,lb,0.3213,1,Example,Example,Example,Example,Example,Example,Example,Example,Example,Example,Example,Example,FALSE,US,Example,No,No,No,Example,Example,Example,14,true,0100000000000,SINGLE_USE,UNSPECIFIED,NOT_MARKED,Example,en,2019-02-08T00:00:00,0000000000002,ABC,TELEPHONE,1";
            string actual = null;

            StringBuilder msg = new StringBuilder();
            using (var p = new ChoXmlReader(FileNameSample50XML)
                .WithXPath("//targetMarketAttributes")
                .WithMaxScanNodes(10)
                )
            {
                using (var w = new ChoCSVWriter(msg)
                    .WithFirstLineHeader()
                    .Configure(c => c.UseNestedKeyFormat = true)
                    .Configure(c => c.NestedColumnSeparator = '/')
                    .Configure(c => c.ThrowAndStopOnMissingField = false)
                    )
                    w.Write(p);
            }

            actual = msg.ToString();

            Assert.AreEqual(expected, actual);
        }

        public class SoapBody
        {
            [ChoXmlNodeRecordField(XPath = "/x:UserName")]
            public string UserName { get; set; }
            [ChoXmlNodeRecordField(XPath = "/x:Password")]
            public string Password { get; set; }

            public override bool Equals(object obj)
            {
                var body = obj as SoapBody;
                return body != null &&
                       UserName == body.UserName &&
                       Password == body.Password;
            }

            public override int GetHashCode()
            {
                var hashCode = 1155857689;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(UserName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Password);
                return hashCode;
            }
        }

        //[Test]
        public static void SoapMsgTest()
        {
            List<object> expected = new List<object>
            {
                new SoapBody{ UserName = "daniel@xxx.com", Password = "123456"}
            };
            List<object> actual = new List<object>();

            string soap = @"<s:Envelope xmlns:s=""http://schemas.xmlsoap.org/soap/envelope/"">
<s:Body>
<Exit xmlns=""http://tempuri.org/"">
<UserName>daniel@xxx.com</UserName>
<Password>123456</Password>
<parkingLotId>21</parkingLotId>
<gateNumber>EX 41</gateNumber>
<ticketNumber>123123123123123123123</ticketNumber>
<plateNumber>12211221</plateNumber>
<paymentSum>10.0</paymentSum>
<ExitDateTime>2018-12-23T09:56:10</ExitDateTime>
<referenceId>987462187346238746263</referenceId>
</Exit>
</s:Body>
</s:Envelope>";

            foreach (var rec in ChoXmlReader<SoapBody>.LoadText(soap)
                .WithXPath("//Exit")
                .WithXmlNamespace("x", "http://tempuri.org/")
                )
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void DefaultNSTest1()
        {
            List<object> expected = new List<object>
            {
                new SyncInvoice { LanguageCode = "GB", ApplicationArea = new ApplicationArea { BODID = "05a1ef3c-67c0-4d38-83ea-b4691a3c4fe0", CreationDateTime = new DateTime(2018,12,13,18,19,03,DateTimeKind.Utc), Sender = new Sender {
                ComponentID = "M3BE", ConfirmationCode = "OnError", LogicalID = "lid://infor.m3be.m3be "} } }
            };
            List<object> actual = new List<object>();

            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<SyncInvoice xmlns=""http://schema.infor.com/InforOAGIS/2"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""SyncInvoice.xsd"" releaseID=""9.2"" versionID=""2.13.0"" systemEnvironmentCode=""Production"" languageCode=""GB"">
<ApplicationArea>
    <Sender>
        <LogicalID>lid://infor.m3be.m3be </LogicalID>
        <ComponentID>M3BE</ComponentID>
        <ConfirmationCode>OnError</ConfirmationCode>
    </Sender>
    <CreationDateTime>2018-12-13T18:19:03.000Z</CreationDateTime>
    <BODID>05a1ef3c-67c0-4d38-83ea-b4691a3c4fe0</BODID>
</ApplicationArea>
</SyncInvoice>";


            // SyncInvoice.ApplicationArea is null
            using (var parser = ChoXmlReader<SyncInvoice>.LoadText(xml).WithXPath("SyncInvoice")
                .WithXmlNamespace("x", "http://schema.infor.com/InforOAGIS/2")
                )
            {
                foreach (dynamic rec in parser)
                {
                    actual.Add(rec);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void DefaultNSTest()
        {
            string xml = @"<SyncInvoice xmlns=""http://schema.infor.com/InforOAGIS/2"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""SyncInvoice.xsd"" languageCode=""IT"" />";

            using (var parser = ChoXmlReader<SyncInvoice>.LoadText(xml)
                           .WithXPath("SyncInvoice")
                      )
            {
                foreach (var rec in parser)
                    Console.WriteLine(rec.Dump());
            }
        }

        //[Test]
        public static void TestXml1()
        {
            List<object> expected = new List<object>
            {
                new EmployeeRec1 {
                    Id = 1,
                    Name = "Tom",
                    Address = new AddressRec
                    {
                        AddressLines = new AddressLineRec[]
                        {
                            new AddressLineRec{ Id = 1, AddressLine = "XYZ road"},
                            new AddressLineRec{ Id = 2, AddressLine = "MiceTown"}
                        }
                    }
                },
                new EmployeeRec1 {
                    Id = 2,
                    Name = "Mark",
                    Address = new AddressRec
                    {
                        Country = "United States",
                        AddressLines = new AddressLineRec[]
                        {
                            new AddressLineRec{ Id = 1, AddressLine = "123 street"},
                            new AddressLineRec{ Id = 2, AddressLine = "TigerCity"}
                        }
                    }
                }
            };
            List<object> actual = new List<object>();

            string xml = @"<Employees>
    <Employee Id='1'>
        <Name>Tom</Name>
        <Address>
            <AddressLine Id='1'>XYZ road</AddressLine>
            <AddressLine Id='2'>MiceTown</AddressLine>
        </Address>
    </Employee>
    <Employee Id='2'>
        <Name>Mark</Name>
        <Address>
            <AddressLine Id='1'>123 street</AddressLine>
            <AddressLine Id='2'>TigerCity</AddressLine>
            <Country>United States</Country>
        </Address>
    </Employee>
</Employees>
";

            foreach (var rec in ChoXmlReader<EmployeeRec1>.LoadText(xml))
            {
                actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void XmlToJSON2_1()
        {
            List<object> expected = new List<object>
            {
                new MyMyfields{ Request_Status="Save as Draft", Request_Type = "CAPEX", Admin = new MyAdmin { Routing_Order=new MyRouting_Order{ Approver1_Order= "1", Approver2_Order = "5", Approver3_Order = "4"}}}
            };
            List<object> actual = new List<object>();

            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<?mso-infoPathSolution name=""urn:schemas-microsoft-com:office:infopath:myProject:-myXSD-2017-05-05T14-19-13"" solutionVersion=""1.0.0.2046"" productVersion=""16.0.0.0"" PIVersion=""1.0.0.0"" href=""https://myportal.sharepoint.com/sites/mySite/myProject/Forms/template.xsn""?>
<?mso-application progid=""InfoPath.Document"" versionProgid=""InfoPath.Document.4""?>
<?mso-infoPath-file-attachment-present?>
<my:myFields xmlns:my=""http://schemas.microsoft.com/office/infopath/2003/myXSD/2017-05-05T14:19:13"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xhtml=""http://www.w3.org/1999/xhtml"" xmlns:pc=""http://schemas.microsoft.com/office/infopath/2007/PartnerControls"" xmlns:ma=""http://schemas.microsoft.com/office/2009/metadata/properties/metaAttributes"" xmlns:d=""http://schemas.microsoft.com/office/infopath/2009/WSSList/dataFields"" xmlns:q=""http://schemas.microsoft.com/office/infopath/2009/WSSList/queryFields"" xmlns:dfs=""http://schemas.microsoft.com/office/infopath/2003/dataFormSolution"" xmlns:dms=""http://schemas.microsoft.com/office/2009/documentManagement/types"" xmlns:tns=""http://microsoft.com/webservices/SharePointPortalServer/UserProfileService"" xmlns:s1=""http://microsoft.com/wsdl/types/"" xmlns:http=""http://schemas.xmlsoap.org/wsdl/http/"" xmlns:tm=""http://microsoft.com/wsdl/mime/textMatching/"" xmlns:soap=""http://schemas.xmlsoap.org/wsdl/soap/"" xmlns:soapenc=""http://schemas.xmlsoap.org/soap/encoding/"" xmlns:mime=""http://schemas.xmlsoap.org/wsdl/mime/"" xmlns:soap12=""http://schemas.xmlsoap.org/wsdl/soap12/"" xmlns:wsdl=""http://schemas.xmlsoap.org/wsdl/"" xmlns:xd=""http://schemas.microsoft.com/office/infopath/2003"" xml:lang=""en-US"">
    <my:Admin>
        <my:Routing_Order>
            <my:Approver-1_Order>1</my:Approver-1_Order>
            <my:Approver-2_Order>5</my:Approver-2_Order>
            <my:Approver-3_Order>4</my:Approver-3_Order>
        </my:Routing_Order>
    </my:Admin>
    <my:Request_Status>Save as Draft</my:Request_Status>
    <my:Request_Type>CAPEX</my:Request_Type>
</my:myFields>";


            foreach (var rec in ChoXmlReader<MyMyfields>.LoadText(xml)
                .WithXPath("/my:myFields")
                .WithXmlNamespace("my", "http://schemas.microsoft.com/office/infopath/2003/myXSD/2017-05-05T14:19:13")
                //.IgnoreField("lang")
                )
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }
        //[Test]
        public static void XmlToJSON2_2()
        {
            string expected = @"{
 ""Admin"": {
   ""Routing_Order"": {
     ""Approver1_Order"": ""1"",
     ""Approver2_Order"": ""5"",
     ""Approver3_Order"": ""4""
   }
 },
 ""Request_Status"": ""Save as Draft"",
 ""Request_Type"": ""CAPEX""
}
";
            string actual = null;

            List<object> source = new List<object>
            {
                new MyMyfields{ Request_Status="Save as Draft", Request_Type = "CAPEX", Admin = new MyAdmin { Routing_Order=new MyRouting_Order{ Approver1_Order= "1", Approver2_Order = "5", Approver3_Order = "4"}}}
            };
            StringBuilder sb = new StringBuilder();
            foreach (var rec in source)
            {
                sb.AppendLine(ChoJSONWriter.ToText(rec));
            }

            actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }
        //[Test]
        public static void XmlToJSON3()
        {
            string expected = @"properties_Guid,properties_ProcessType,properties_Description
fizeofnpj-dzeifjzenf-ezfizef,ZMIN,Test 2";
            string actual = null;

            string xml = @"<?xml version='1.0' encoding='UTF-8'?>
<entry>
    <content>
        <m:properties xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"">
            <d:Guid>fizeofnpj-dzeifjzenf-ezfizef</d:Guid>
            <d:ObjectId>6000009251</d:ObjectId>
            <d:ProcessType>ZMIN</d:ProcessType>
            <d:ProcessTypeTxt>Incident</d:ProcessTypeTxt>
            <d:Description>Test 2</d:Description>
            <d:IntroText>Incident (IT Service Management)</d:IntroText>
            <d:CreatedAtDateFormatted>08.05.18</d:CreatedAtDateFormatted>
            <d:ChangedAtDateFormatted>08.05.18</d:ChangedAtDateFormatted>
            <d:PostingDate>2018-05-08T00:00:00</d:PostingDate>
            <d:ChangedAtDate>2018-05-08T00:00:00</d:ChangedAtDate>
            <d:Priority>2</d:Priority>
            <d:PriorityTxt>2: High</d:PriorityTxt>
            <d:PriorityState>None</d:PriorityState>
            <d:Concatstatuser>New</d:Concatstatuser>
            <d:ActionRequired>false</d:ActionRequired>
            <d:StillOpen>true</d:StillOpen>
            <d:Icon />
            <d:SoldToPartyName />
            <d:ServiceTeamName />
            <d:PersonRespName />
            <d:CategoryTxt>Change - Interface - Evolutive Maintenance</d:CategoryTxt>
            <d:ConfigItemTxt />
            <d:SAPComponent>BC-BCS-VAS</d:SAPComponent>
        </m:properties>
    </content>
</entry>";

            var nsManager = new XmlNamespaceManager(new NameTable());
            //register mapping of prefix to namespace uri 
            nsManager.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            nsManager.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");

            StringBuilder csv = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml)
                  .WithXPath("//entry/content/m:properties")
                  .WithXmlNamespaceManager(nsManager)
                  .WithField("Guid", xPath: "d:Guid")
                  .WithField("ProcessType", xPath: "d:ProcessType")
                  .WithField("Description", xPath: "d:Description")
                )
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    )
                    w.Write(p);
            }

            actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void CSVToXmlTest()
        {
            string expected = @"<Employees>
  <Employee>
    <Id>1</Id>
    <Name>Tom</Name>
    <City>NY</City>
  </Employee>
  <Employee>
    <Id>2</Id>
    <Name>Mark</Name>
    <City>NJ</City>
  </Employee>
  <Employee>
    <Id>3</Id>
    <Name>Lou</Name>
    <City>FL</City>
  </Employee>
  <Employee>
    <Id>4</Id>
    <Name>Smith</Name>
    <City>PA</City>
  </Employee>
  <Employee>
    <Id>5</Id>
    <Name>Raj</Name>
    <City>DC</City>
  </Employee>
</Employees>";
            string actual = null;

            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoXmlWriter(sb)
                    .Configure(c => c.RootName = "Employees")
                    .Configure(c => c.NodeName = "Employee")
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void MultipleXmlNS()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"lat",(double)25.0312615000 },{"lon",(double)121.3505846635 } },
                new ChoDynamicObject{{"lat",(double)25.0312520284 },{"lon",(double)121.3505897764 } },
                new ChoDynamicObject{{"lat",(double)25.0312457420 },{"lon",(double)121.3506018464 } },
                new ChoDynamicObject{{"lat",(double)25.0312426407 },{"lon",(double)121.3506035227 } },
            };
            List<object> actual = new List<object>();

            string xml = @"<gpx xmlns=""http://www.topografix.com/GPX/1/1"" xmlns:gpxx=""http://www.garmin.com/xmlschemas/GpxExtensions/v3"" xmlns:gpxtrkx=""http://www.garmin.com/xmlschemas/TrackStatsExtension/v1"" xmlns:wptx1=""http://www.garmin.com/xmlschemas/WaypointExtension/v1"" xmlns:gpxtpx=""http://www.garmin.com/xmlschemas/TrackPointExtension/v1"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" creator=""GPSMAP 64ST TWN"" version=""1.1"" xsi:schemaLocation=""http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd http://www.garmin.com/xmlschemas/GpxExtensions/v3 http://www8.garmin.com/xmlschemas/GpxExtensionsv3.xsd http://www.garmin.com/xmlschemas/TrackStatsExtension/v1 http://www8.garmin.com/xmlschemas/TrackStatsExtension.xsd http://www.garmin.com/xmlschemas/WaypointExtension/v1 http://www8.garmin.com/xmlschemas/WaypointExtensionv1.xsd http://www.garmin.com/xmlschemas/TrackPointExtension/v1 http://www.garmin.com/xmlschemas/TrackPointExtensionv1.xsd"">
  <metadata>
    <link href=""http://www.garmin.com"">
      <text>Garmin International</text>
    </link>
    <time>2018-10-05T09:21:31Z</time>
  </metadata>
  <trk>
    <name>2018-10-05 17:21:26</name>
    <extensions>
      <gpxx:TrackExtension>
        <gpxx:DisplayColor>Cyan</gpxx:DisplayColor>
      </gpxx:TrackExtension>
      <gpxtrkx:TrackStatsExtension>
        <gpxtrkx:Distance>1033</gpxtrkx:Distance>
        <gpxtrkx:TotalElapsedTime>996</gpxtrkx:TotalElapsedTime>
        <gpxtrkx:MovingTime>870</gpxtrkx:MovingTime>
        <gpxtrkx:StoppedTime>86</gpxtrkx:StoppedTime>
        <gpxtrkx:MovingSpeed>1</gpxtrkx:MovingSpeed>
        <gpxtrkx:MaxSpeed>2</gpxtrkx:MaxSpeed>
        <gpxtrkx:MaxElevation>207</gpxtrkx:MaxElevation>
        <gpxtrkx:MinElevation>189</gpxtrkx:MinElevation>
        <gpxtrkx:Ascent>17</gpxtrkx:Ascent>
        <gpxtrkx:Descent>5</gpxtrkx:Descent>
        <gpxtrkx:AvgAscentRate>0</gpxtrkx:AvgAscentRate>
        <gpxtrkx:MaxAscentRate>0</gpxtrkx:MaxAscentRate>
        <gpxtrkx:AvgDescentRate>0</gpxtrkx:AvgDescentRate>
        <gpxtrkx:MaxDescentRate>-0</gpxtrkx:MaxDescentRate>
        </gpxtrkx:TrackStatsExtension>
      </extensions>
      <trkseg>
        <trkpt lat=""25.0312615000"" lon=""121.3505846635"">
        <ele>189.04</ele>
        <time>2018-10-05T09:04:55Z</time>
      </trkpt>
      <trkpt lat=""25.0312520284"" lon=""121.3505897764"">
        <ele>189.04</ele>
        <time>2018-10-05T09:04:57Z</time>
        </trkpt>
      <trkpt lat=""25.0312457420"" lon=""121.3506018464"">
        <ele>196.43</ele>
        <time>2018-10-05T09:04:59Z</time>
      </trkpt>
      <trkpt lat=""25.0312426407"" lon=""121.3506035227"">
        <ele>196.42</ele>
        <time>2018-10-05T09:05:01Z</time>
      </trkpt>
    </trkseg>
  </trk>
</gpx>";

            foreach (var rec in ChoXmlReader.LoadText(xml)
                .WithXPath("./trk/trkseg/trkpt")
                .WithField("lat")
                .WithField("lon")
                )
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void XmlToJSON1_1()
        {
            string expected = @"[
 {
  ""@id"": 1,
  ""Name"": ""Mark"",
  ""Age"": 35,
  ""Gender"": ""Male"",
  ""DateOfBirth"": ""1980-05-30T00:00:00"",
  ""Height"": {
    ""@units"": ""cm"",
    ""#text"": ""30""
  },
  ""Weight"": {
    ""@units"": ""kg"",
    ""#text"": ""10""
  }
 },
 {
  ""@id"": 2,
  ""Name"": ""Tom"",
  ""Age"": 21,
  ""Gender"": ""Female"",
  ""DateOfBirth"": ""2000-01-01T00:00:00"",
  ""Height"": {
    ""@units"": ""cm"",
    ""#text"": ""10""
  },
  ""Weight"": {
    ""@units"": ""kg"",
    ""#text"": ""20""
  }
 }
]";
            string actual = null;

            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<ContrastDoseReport xmlns=""http://www.medrad.com/ContrastDoseReport"" xsi:schemaLocation=""MEDRAD_Injection_Report.XSD"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
    <Patient id=""1"">
        <Name>Mark</Name>
        <Age>35</Age>
        <Gender>Male</Gender>
        <DateOfBirth>05-30-1980</DateOfBirth>
        <Height units=""cm"">30</Height>
        <Weight units=""kg"">10</Weight>
    </Patient>
    <Patient id=""2"">
        <Name>Tom</Name>
        <Age>21</Age>
        <Gender>Female</Gender>
        <DateOfBirth>01-01-2000</DateOfBirth>
        <Height units=""cm"">10</Height>
        <Weight units=""kg"">20</Weight>
    </Patient>
</ContrastDoseReport>";

            actual = ChoJSONWriter.ToTextAll(ChoXmlReader.LoadText(xml),
                new ChoJSONRecordConfiguration().Configure(c => c.EnableXmlAttributePrefix = true));

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void XmlToJSON1_2()
        {
            string expected = @"<ContrastDoseReport>
  <Patient id=""1"">
    <Name>Mark</Name>
    <Age>35</Age>
    <Gender>Male</Gender>
    <DateOfBirth>1980-05-30T00:00:00</DateOfBirth>
    <Height units=""cm"">
    30
  </Height>
    <Weight units=""kg"">
    10
  </Weight>
  </Patient>
  <Patient id=""2"">
    <Name>Tom</Name>
    <Age>21</Age>
    <Gender>Female</Gender>
    <DateOfBirth>2000-01-01T00:00:00</DateOfBirth>
    <Height units=""cm"">
    10
  </Height>
    <Weight units=""kg"">
    20
  </Weight>
  </Patient>
</ContrastDoseReport>";
            string actual = null;

            string json = @"[
 {
  ""@id"": 1,
  ""Name"": ""Mark"",
  ""Age"": 35,
  ""Gender"": ""Male"",
  ""DateOfBirth"": ""1980-05-30T00:00:00"",
  ""Height"": {
    ""@units"": ""cm"",
    ""#text"": ""30""
  },
  ""Weight"": {
    ""@units"": ""kg"",
    ""#text"": ""10""
  }
 },
 {
  ""@id"": 2,
  ""Name"": ""Tom"",
  ""Age"": 21,
  ""Gender"": ""Female"",
  ""DateOfBirth"": ""2000-01-01T00:00:00"",
  ""Height"": {
    ""@units"": ""cm"",
    ""#text"": ""10""
  },
  ""Weight"": {
    ""@units"": ""kg"",
    ""#text"": ""20""
  }
 }
]";

            actual = ChoXmlWriter.ToTextAll(ChoJSONReader.LoadText(json), new ChoXmlRecordConfiguration().Configure(c => c.RootName = "ContrastDoseReport").Configure(c => c.NodeName = "Patient"));

            Assert.AreEqual(expected, actual);
        }

        public class Name
        {
            public string Part { get; set; }
            public string Value
            {
                get; set;
            }

            public override bool Equals(object obj)
            {
                var name = obj as Name;
                return name != null &&
                       Part == name.Part &&
                       Value == name.Value;
            }

            public override int GetHashCode()
            {
                var hashCode = 1556981182;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Part);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
                return hashCode;
            }
        }

        //[Test]
        public static void ComplexTest1()
        {
            List<object> expected = new List<object>
            {
                new Name{ Part = "first", Value = "Foo"},
                new Name{ Part = "last", Value = "Bar"},
                new Name{ Part = "first", Value = "Foo1"},
                new Name{ Part = "last", Value = "Bar1"}
            };
            List<object> actual = new List<object>();

            string xml = @"<SalesLead>
    <Customer>
         <Name part=""first"">Foo</Name>
         <Name part=""last"">Bar</Name>
    </Customer>
    <Customer>
         <Name part=""first"">Foo1</Name>
         <Name part=""last"">Bar1</Name>
    </Customer>
</SalesLead>";

            using (var x = ChoXmlReader.LoadText(xml)
                )
            {
                foreach (var rec in x.OfType<dynamic>().SelectMany(r =>
                    ((object[])r.Names).OfType<dynamic>().Select(r1 =>
                        new Name { Part = r1.part, Value = r1.GetText() }
                    )
                    )
                    )
                    actual.Add(rec);
            }

            Assert.AreEqual(expected, actual);
        }

        public class ManagedObject
        {

        }
        public class ManagedObjectJTS : ManagedObject
        {
            [ChoXmlNodeRecordField(XPath = @"/p[@name=""name""]")]
            public string Name { get; set; }
            [ChoXmlNodeRecordField(XPath = @"/p[@name=""cellBarQualify""]")]
            public int CellBarQualify { get; set; }

            public override bool Equals(object obj)
            {
                var jTS = obj as ManagedObjectJTS;
                return jTS != null &&
                       Name == jTS.Name &&
                       CellBarQualify == jTS.CellBarQualify;
            }

            public override int GetHashCode()
            {
                var hashCode = -295656929;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + CellBarQualify.GetHashCode();
                return hashCode;
            }
        }
        public class ManagedObjectCCF : ManagedObject
        {
            [ChoXmlNodeRecordField(XPath = @"/p[@name=""name""]")]
            public string Name { get; set; }
            [ChoXmlNodeRecordField(XPath = @"/p[@name=""SBTSId""]")]
            public int SBTSId { get; set; }

            public override bool Equals(object obj)
            {
                var cCF = obj as ManagedObjectCCF;
                return cCF != null &&
                       Name == cCF.Name &&
                       SBTSId == cCF.SBTSId;
            }

            public override int GetHashCode()
            {
                var hashCode = 68726846;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + SBTSId.GetHashCode();
                return hashCode;
            }
        }
        public class ManagedObjectPOC : ManagedObject
        {
            [ChoXmlNodeRecordField(XPath = @"/p[@name=""alpha""]")]
            public string Alpha { get; set; }
            [ChoXmlNodeRecordField(XPath = @"/p[@name=""bepPeriod""]")]
            public int BepPeriod { get; set; }

            public override bool Equals(object obj)
            {
                var pOC = obj as ManagedObjectPOC;
                return pOC != null &&
                       Alpha == pOC.Alpha &&
                       BepPeriod == pOC.BepPeriod;
            }

            public override int GetHashCode()
            {
                var hashCode = 1924863892;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Alpha);
                hashCode = hashCode * -1521134295 + BepPeriod.GetHashCode();
                return hashCode;
            }
        }

        //[Test]
        public static void Test71()
        {
            List<object> expected = new List<object>
            {
                new ManagedObjectJTS{ Name = "VM_25261_G1_A", CellBarQualify = 0},
                new ManagedObjectCCF{ Name = "ET_AR_G_0267_GHABATGHAYATI", SBTSId = 10267},
                new ManagedObjectPOC{ Alpha = "0", BepPeriod = 10 }
            };
            List<object> actual = new List<object>();

            using (var p = new ChoXmlReader<ManagedObject>(FileNameSample71XML)
                .WithCustomRecordSelector(ele =>
                {
                    var classValue = ((Tuple<long, XElement>)ele).Item2.Attributes("class").FirstOrDefault().Value;
                    if (classValue == "JTS")
                        return typeof(ManagedObjectJTS);
                    else if (classValue == "CCF")
                        return typeof(ManagedObjectCCF);
                    else if (classValue == "POC")
                        return typeof(ManagedObjectPOC);
                    else
                        throw new NullReferenceException();
                })
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        public class TestPlan
        {
            [ChoXmlNodeRecordField(XPath = @"/ThreadGroup/stringProp[@name=""ThreadGroup.on_sample_error""]")]
            public string NumThreads { get; set; }
            [ChoXmlNodeRecordField(XPath = @"/ThreadGroup/stringProp[@name=""ThreadGroup.ramp_time""]")]
            public int RampTime { get; set; }

            [ChoXmlNodeRecordField(XPath = @"/hashTree/hashTree/HTTPSamplerProxy/stringProp[@name=""HTTPSampler.path""]")]
            public string Path { get; set; }
            [ChoXmlNodeRecordField(XPath = @"/hashTree/hashTree/HTTPSamplerProxy/stringProp[@name=""HTTPSampler.domain""]")]
            public string Domain { get; set; }

            public override bool Equals(object obj)
            {
                var plan = obj as TestPlan;
                return plan != null &&
                       NumThreads == plan.NumThreads &&
                       RampTime == plan.RampTime &&
                       Path == plan.Path &&
                       Domain == plan.Domain;
            }

            public override int GetHashCode()
            {
                var hashCode = -1603925905;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(NumThreads);
                hashCode = hashCode * -1521134295 + RampTime.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Path);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Domain);
                return hashCode;
            }
        }

        //[Test]
        public static void TestPlanTest()
        {
            List<object> expected = new List<object>
            {
                new TestPlan{ Domain = "www.abc.com/abc-service-api", NumThreads="continue", Path = "/v1/test/test?debug=false", RampTime = 1}
            };
            List<object> actual = new List<object>();

            using (var p = new ChoXmlReader<TestPlan>(FileNameSample70XML)
                .WithXPath("/TestPlan/hashTree/hashTree")
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void XmlToJSONKVP()
        {
            string expected = @"<List`1s>
  <ArrayOfAnyType>
    <anyType xmlns:q1=""http://www.w3.org/2001/XMLSchema"" p3:type=""q1:string"" xmlns:p3=""http://www.w3.org/2001/XMLSchema-instance"">81963</anyType>
    <anyType xmlns:q2=""http://www.w3.org/2001/XMLSchema"" p3:type=""q2:string"" xmlns:p3=""http://www.w3.org/2001/XMLSchema-instance"">complete</anyType>
    <anyType xmlns:q3=""http://www.w3.org/2001/XMLSchema"" p3:type=""q3:string"" xmlns:p3=""http://www.w3.org/2001/XMLSchema-instance"">2018-07-30</anyType>
  </ArrayOfAnyType>
  <ArrayOfAnyType>
    <anyType xmlns:q1=""http://www.w3.org/2001/XMLSchema"" p3:type=""q1:string"" xmlns:p3=""http://www.w3.org/2001/XMLSchema-instance"">81194</anyType>
    <anyType xmlns:q2=""http://www.w3.org/2001/XMLSchema"" p3:type=""q2:string"" xmlns:p3=""http://www.w3.org/2001/XMLSchema-instance"">complete</anyType>
    <anyType xmlns:q3=""http://www.w3.org/2001/XMLSchema"" p3:type=""q3:string"" xmlns:p3=""http://www.w3.org/2001/XMLSchema-instance"">2018-07-30</anyType>
  </ArrayOfAnyType>
</List`1s>";
            string actual = null;

            string xml = @"<jobs><job>
           <properties>
              <name>jobid</name>
              <value>81963</value>
           </properties>
           <properties>
              <name>status</name>
              <value>complete</value>
           </properties>
           <properties>
              <name>date</name>
              <value>2018-07-30</value>
           </properties>
        </job>
        <job>
           <properties>
              <name>jobid</name>
              <value>81194</value>
           </properties>
           <properties>
              <name>status</name>
              <value>complete</value>
           </properties>
           <properties>
              <name>date</name>
              <value>2018-07-30</value>
           </properties>
        </job></jobs>";


            using (var p = ChoXmlReader.LoadText(xml))
            {
                //Console.WriteLine(ChoJSONWriter.ToTextAll(p.Select(r => ((IList<dynamic>)r.propertiess).ToDictionary(r1 => r1.name, r1 => r1.value))));
                //Console.WriteLine(ChoJSONWriter.ToTextAll(p.Select(r => ((IList<dynamic>)r.propertiess).Select(r1 => r1.value).ToList())));

                actual = ChoXmlWriter.ToTextAll(p.Select(r => ((IList<dynamic>)r.propertiess).Select(r1 => r1.value).ToList()));

            }

            Assert.AreEqual(expected, actual);
            Assert.Warn("I am not sure, if this is the original XmlToJSONKVP test.");
        }

        //[Test]
        public static void Sample49Test()
        {
            string expected = @"[
 {
  ""StockNumber"": 1020,
  ""Make"": ""Renault"",
  ""Models"": [
   {
     ""modelName"": ""Kwid"",
     ""modelType"": ""Basic"",
     ""price"": ""5 Lakhs"",
     ""preOrderNeeded"": ""No""
   },
   {
     ""modelName"": ""Kwid"",
     ""modelType"": ""Compact Model with all upgrades"",
     ""price"": ""7.25 Lakhs"",
     ""preOrderNeeded"": ""Yes""
   }
  ]
 },
 {
  ""StockNumber"": 1010,
  ""Make"": ""Toyota"",
  ""Models"": null
 }
]";
            string actual = null;

            using (var r = new ChoXmlReader(FileNameSample49XML)
                .WithXPath("/CarCollection/Cars/Car")
                .WithMaxScanNodes(10)
                //.Configure(c => c.MaxScanRows = 10)
                )
            {
                actual = ChoJSONWriter.ToTextAll(r);
            }

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample48Test()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add("Type", typeof(string));
            expected.Columns.Add("Indice", typeof(Int64)).AllowDBNull = false;
            expected.Columns.Add("Limites_Haut");
            expected.Columns.Add("Limites_Bas");
            expected.Columns.Add("Points_Point_0_id");
            expected.Columns.Add("Points_Point_0_X");
            expected.Columns.Add("Points_Point_0_Y");
            expected.Columns.Add("Points_Point_0_#text");
            expected.Columns.Add("Points_Point_1_id");
            expected.Columns.Add("Points_Point_1_X");
            expected.Columns.Add("Points_Point_1_Y");
            expected.Columns.Add("Points_Point_1_#text");
            expected.Columns.Add("Points_Point_2_id");
            expected.Columns.Add("Points_Point_2_X");
            expected.Columns.Add("Points_Point_2_Y");
            expected.Columns.Add("Points_Point_2_#text");
            expected.Rows.Add("Point", 859, "26.5", "43.2", "01", "45", "44", "12", "02", "5", "41", "5", "03", "4", "464", "3");
            expected.Rows.Add("Point", 256, "16.5", "12.2", "05", "6.5", "22", "5", "06", "58", "46.5", "5", "07", "98", "4.5", "6");

            var actual = new ChoXmlReader(FileNameSample48XML)
                .WithXPath("//Contour/Elements/Element")
                .Select(i => i.Flatten())
                .AsDataTable();

            DataTableAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void XmlNSTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject("folder") {{"name","Strategy"}, { "id", "ffc905fd-a78c-4311-b2f6-a188c00ed10a" } , { "type", "strategy" } },
                new ChoDynamicObject("folder") {{"name", "Business" }, { "id", "0d806081-438f-4ae5-86d9-8ff5ee4e9f1a" } , { "type", "business" } },
                new ChoDynamicObject("folder") {{"name", "Application" }, { "id", "3566e95c-c070-46bb-bde3-f6017ae49dc1" } , { "type", "application" } },
                new ChoDynamicObject("folder") {{"name", "Technology & Physical" }, { "id", "4fabc4fa-a882-4843-ae69-170b66df7685" } , { "type", "technology" } },
                new ChoDynamicObject("folder") {{"name", "Motivation" }, { "id", "ce5e0874-1c06-41c1-9b95-eec6558afa89" } , { "type", "motivation" } }
            };
            List<object> actual = new List<object>();

            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
  <archimate:model xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" 
     xmlns:archimate=""http://www.archimatetool.com/archimate"" name=""ACME"" 
     id=""38f940a6-9fc7-4619-9806-fd4d48397af7"" version=""4.0.0"">
    <folder name=""Strategy"" id=""ffc905fd-a78c-4311-b2f6-a188c00ed10a"" type=""strategy""/>
    <folder name=""Business"" id=""0d806081-438f-4ae5-86d9-8ff5ee4e9f1a"" type=""business""/>
    <folder name=""Application"" id=""3566e95c-c070-46bb-bde3-f6017ae49dc1"" type=""application""/>
    <folder name=""Technology &amp; Physical"" id=""4fabc4fa-a882-4843-ae69-170b66df7685"" type=""technology""/>
    <folder name=""Motivation"" id=""ce5e0874-1c06-41c1-9b95-eec6558afa89"" type=""motivation"">
      <element xsi:type=""archimate:Principle"" name=""Secure the Whole"" id=""9546e727-f9f7-402a-a4a2-50519d697d75""/>
    </folder>
 </archimate:model>";

            using (var p = ChoXmlReader.LoadText(xml)
                )
            {
                foreach (var x in p)
                    actual.Add(x);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void XmlToJSONNumberTest()
        {
            string xml = @"<Report xmlns:json=""http://james.newtonking.com/projects/json"">
<ReportItem>
    <Name>MyObjectName</Name>
    <Revenue>99999.45</Revenue>
</ReportItem>
</Report>";

            var x = ChoXmlReader.DeserializeText(xml);

            Console.WriteLine(x.DumpAsJson());
            Assert.Warn("Console.WriteLine(ChoJSONWriter.ToTextAll(x)); works");
        }

        //[Test]
        public static void Sample22Test()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add("Age", typeof(long)).AllowDBNull = false;
            expected.Columns.Add("DateOfBirth");
            expected.Columns.Add("EmailAddress");
            expected.Columns.Add("MobilePhone_CountryCode");
            expected.Columns.Add("MobilePhone_Number");
            expected.Columns.Add("WorkPhone_CountryCode");
            expected.Columns.Add("WorkPhone_Number");
            expected.Rows.Add((long)39, "06:07:1985:00:00", "abc@rentacar3.com", "1", "2049515487", "93", "1921525542");
            expected.Rows.Add((long)29, "06:07:1989:00:00", "abc@rentacar2.com", "1", "2049515949", "93", "1921525125");
            DataTable actual = null;

            string xml = @"<Response>
    <MemberSummary>
      <Age>39</Age>      
      <DateOfBirth>06:07:1985:00:00</DateOfBirth>
      <EmailAddress>abc@rentacar3.com</EmailAddress>      
      <MobilePhone>
        <CountryCode>1</CountryCode>        
        <Number>2049515487</Number>
      </MobilePhone>      
      <WorkPhone>
        <CountryCode>93</CountryCode>        
        <Number>1921525542</Number>
      </WorkPhone>      
    </MemberSummary>

    <MemberSummary>
      <Age>29</Age>      
      <DateOfBirth>06:07:1989:00:00</DateOfBirth>
      <EmailAddress>abc@rentacar2.com</EmailAddress>      
      <MobilePhone>
        <CountryCode>1</CountryCode>        
        <Number>2049515949</Number>
      </MobilePhone>      
      <WorkPhone>
        <CountryCode>93</CountryCode>        
        <Number>1921525125</Number>
      </WorkPhone> 
      <HomePhone>
        <CountryCode>213</CountryCode>       
        <Number>8182879870</Number>
      </HomePhone>      
    </MemberSummary>
</Response>";

            using (var p = ChoXmlReader.LoadText(xml))
            {
                actual = p.Select(e => e.Flatten()).AsDataTable();
            }

            DataTableAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample21Test()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add("Key", typeof(object)).AllowDBNull = false;
            expected.Columns.Add("Value");
            expected.Rows.Add("Key1", "79,0441326460292");
            expected.Rows.Add("Key1", "76,0959542079328");
            expected.Rows.Add("Key1", "74,3061819154758");
            expected.Rows.Add("Key1", "78,687039788779");
            expected.Rows.Add("Key2", "87,7110395931923");

            DataTable actual = null;

            using (var p = new ChoXmlReader(FileNameSample21XML)
                .WithField("Key")
                .WithField("Value", xPath: "/Values/string")
                )
            {
                actual = p.SelectMany(r => ((Array)r.Value).OfType<string>().Select(r1 => new { Key = r.Key, Value = r1 })).AsDataTable();

            }

            DataTableAssert.AreEqual(expected, actual);
        }

        public class Naptan
        {
            [ChoXmlNodeRecordField(XPath = "/z:AtcoCode")]
            public string AtcoCode { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:NaptanCode")]
            public string NaptanCode { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:Place/z:Location/z:Translation/z:Latitude")]
            public double Latitude { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:Place/z:Location/z:Translation/z:Longitude")]
            public double Longitude { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:StopClassification/z:OnStreet/z:Bus/z:TimingStatus")]
            public string TimmingStatus { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:StopClassification/z:OnStreet/z:Bus/z:BusStopType")]
            public string BusStopType { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:Descriptor/z:CommonName")]
            public string CommonName { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:Descriptor/z:Landmark")]
            public string Landmark { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:Descriptor/z:Street")]
            public string Street { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:Descriptor/z:Indicator")]
            public string Indicator { get; set; }

            public override bool Equals(object obj)
            {
                var naptan = obj as Naptan;
                return naptan != null &&
                       AtcoCode == naptan.AtcoCode &&
                       NaptanCode == naptan.NaptanCode &&
                       Latitude == naptan.Latitude &&
                       Longitude == naptan.Longitude &&
                       TimmingStatus == naptan.TimmingStatus &&
                       BusStopType == naptan.BusStopType &&
                       CommonName == naptan.CommonName &&
                       Landmark == naptan.Landmark &&
                       Street == naptan.Street &&
                       Indicator == naptan.Indicator;
            }

            public override int GetHashCode()
            {
                var hashCode = -650170821;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(AtcoCode);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(NaptanCode);
                hashCode = hashCode * -1521134295 + Latitude.GetHashCode();
                hashCode = hashCode * -1521134295 + Longitude.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TimmingStatus);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BusStopType);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CommonName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Landmark);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Street);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Indicator);
                return hashCode;
            }
        }

        //[Test]
        public static void Sample20()
        {
            List<object> expected = new List<object>
            {
                new Naptan{ AtcoCode = "030028280001", NaptanCode = "brkpjmt", CommonName = "Tinkers Corner", Landmark = "adj Forbury Lane", Street = "Holt Lane", Indicator = "opp", Longitude=-1.42979961186, Latitude =51.38882190967, BusStopType = "CUS", TimmingStatus = "OTH" }
            };
            List<object> actual = new List<object>();

            using (var p = new ChoXmlReader<Naptan>(FileNameSample20XML)
                .WithXPath("//NaPTAN/StopPoints/StopPoint")
                .WithXmlNamespace("z", "http://www.naptan.org.uk/")
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void NSTest()
        {
            string xml = @"<ns3:Test_Service xmlns:ns3=""http://www.CCKS.org/XRT/Form"">
  <ns3:fname>mark</ns3:fname>
  <ns3:lname>joye</ns3:lname>
  <ns3:CarCompany>saab</ns3:CarCompany>
  <ns3:CarNumber>9741</ns3:CarNumber>
  <ns3:IsInsured>true</ns3:IsInsured>
  <ns3:safties></ns3:safties>
  <ns3:CarDescription>test Car</ns3:CarDescription>
  <ns3:collections>
    <ns3:collection>
      <ns3:XYZ>1</ns3:XYZ>
      <ns3:PQR>11</ns3:PQR>
      <ns3:contactdetails>
        <ns3:contactdetail>
          <ns3:contname>DOM</ns3:contname>
          <ns3:contnumber>8787</ns3:contnumber>
        </ns3:contactdetail>
        <ns3:contactdetail>
          <ns3:contname>COM</ns3:contname>
          <ns3:contnumber>4564</ns3:contnumber>
          <ns3:addtionaldetails>
            <ns3:addtionaldetail>
              <ns3:description>54657667</ns3:description>
            </ns3:addtionaldetail>
          </ns3:addtionaldetails>
        </ns3:contactdetail>
        <ns3:contactdetail>
          <ns3:contname>gf</ns3:contname>
          <ns3:contnumber>123</ns3:contnumber>
          <ns3:addtionaldetails>
            <ns3:addtionaldetail>
              <ns3:description>123</ns3:description>
            </ns3:addtionaldetail>
          </ns3:addtionaldetails>
        </ns3:contactdetail>
      </ns3:contactdetails>
    </ns3:collection>
  </ns3:collections>
</ns3:Test_Service>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("//")
                .WithXmlNamespace("ns3", "http://www.CCKS.org/XRT/Form")
                )
            {
                using (var w = new ChoJSONWriter(sb)
                    .Configure(c => c.SupportMultipleContent = true)
                    )
                {
                    w.Write(p.Select(e => e.AddNamespace("ns3", "http://www.CCKS.org/XRT/Form")));
                }
            }

            Console.WriteLine(sb.ToString());

            Assert.Fail("Not sure, how to test");
        }

        //[Test]
        public static void CDATATest()
        {
            string expected = @"[
 {
  ""First_Name"": ""Luke"",
  ""Last_Name"": ""Skywalker"",
  ""ID"": {
    ""ID1"": ""1"",
    ""Name"": ""1234""
  }
 }
]";
            string actual = null;

            string ID = null;

            string xml = @"<CUST><First_Name>Luke</First_Name> <Last_Name>Skywalker</Last_Name> <ID ID1=""1""><Name><![CDATA[1234]]></Name></ID> </CUST>";

            using (var p = new ChoXmlReader(new StringReader(xml))
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                //.Configure(c => c.RetainXmlAttributesAsNative = true)
                .WithXPath("/")
                )
            {
                actual = ChoJSONWriter.ToTextAll(p);
            }
            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample46()
        {
            string expected = @"overallResult,test
Passed,ChoETL.ChoDynamicObject
Passed,ChoETL.ChoDynamicObject";
            string actual = null;

            string xml = @"<session
    beginTime=""2018-05-11T10:37:30""
    halSerialNumber=""08J-0735""
    testMode=""Remote""
    userName=""Myname"">
    <appliance overallResult=""Passed"" partNumber=""AN-02-203"" serialNumber=""3"">
        <test_set testState=""Passed"">
            <test
                arcDetect=""0""
                lowerLimitMilliamps=""0.00""
                name=""HiPot 50Hz""
                numTests=""1""
                startConditions=""StartKey""
                targetOutputKilovolts=""1.50""
                testVoltageOutput=""Back""
                timeHoldSeconds=""2.0""
                timeRampDownSeconds=""0.0""
                timeRampUpSeconds=""0.0""
                type=""HiPot50""
                upperLimitMilliamps=""20.00""
            />
            <test_result
                appliedOutputKilovolts=""1.50""
                leakageMilliamps=""0.57""
                testDurationSeconds=""2.00""
                testState=""Passed""
                timeOfTest=""2018-05-11T10:39:29""
            />
        </test_set>
        <test_set testState=""Passed"">
            <test
                lowerLimitMilliamps=""0.00""
                name=""Power Leakage""
                numTests=""1""
                powerFactorLowerLimit=""0.000""
                powerFactorUpperLimit=""1.000""
                powerLowerLimitKVA=""3.00""
                powerUpperLimitKVA=""4.00""
                reversePolarity=""0""
                timeHoldSeconds=""3.0""
                type=""PowerLeakage""
                upperLimitMilliamps=""20.00""
            />
            <test_result
                leakageMilliamps=""0.05""
                powerAV=""3.437""
                powerFactor=""1.000""
                testDurationSeconds=""3.00""
                testState=""Passed""
                timeOfTest=""2018-05-11T10:39:33""
            />
        </test_set>
    </appliance>
</session>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("/")
                )
            {
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    .Configure(c => c.UseNestedKeyFormat = false)
                    )
                {
                    w.Write(p.SelectMany(r => ((dynamic[])r.appliance.test_sets).Select(r1 => new { r.appliance.overallResult, test = r1.test })));
                }
                //using (var csv = new ChoCSVReader(sb)
                //                    .WithFirstLineHeader()
                //    )
                //{
                //    var dt = csv.AsDataTable();
                //}
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample45()
        {
            string expected = @"ObjectName,PrincipalType,DisplayName,RoleDefBindings
New Data,,,
Documents,,,
Documents2,,,
Documents2,User,,
Documents2,Group,,";
            string actual = null;

            string xml = @"<SPSecurableObject>
  <ObjectName>New Data</ObjectName>
  <ChildObjects>
    <SPSecurableObject>
      <ObjectName>Documents</ObjectName>
    </SPSecurableObject>
    <SPSecurableObject>
      <ObjectName>Documents2</ObjectName>
      <RoleAssignments>
        <SPRoleAssignment>
          <PrincipalType>User</PrincipalType>
          <Member>
            <User>
              <DisplayName>John Doe</DisplayName>
            </User>
          </Member>
          <RoleDefBindings>
            <RoleName>Limited Access</RoleName>
          </RoleDefBindings>
        </SPRoleAssignment>
        <SPRoleAssignment>
          <PrincipalType>Group</PrincipalType>
          <Member>
            <Group>
              <GroupName>Group1</GroupName>
            </Group>
          </Member>
          <RoleDefBindings>
            <RoleName>Full Control</RoleName>
          </RoleDefBindings>
        </SPRoleAssignment>
      </RoleAssignments>
    </SPSecurableObject>
</ChildObjects>
</SPSecurableObject>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("/")
                )
            {
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    )
                    w.Write(Flatten(p.First()));
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        static IEnumerable Flatten(dynamic obj)
        {
            yield return new { ObjectName = obj.ObjectName, PrincipalType = (string)null, DisplayName = (string)null, RoleDefBindings = (string)null };

            if (obj.ContainsRoleAssignments)
            {
                foreach (dynamic child in (IList)obj.RoleAssignments)
                {
                    yield return new { ObjectName = obj.ObjectName, PrincipalType = child.PrincipalType, DisplayName = (string)null, RoleDefBindings = (string)null };
                }
            }

            if (obj.ChildObjects == null)
                yield break;
            else
            {
                foreach (dynamic child in (IList)obj.ChildObjects)
                {
                    foreach (object rec in Flatten(child))
                        yield return rec;
                }
            }
        }

        //[Test]
        public static void Sample44()
        {
            string expected = @"{
 {
  ""items"": {
    ""item"": {
      ""title"": ""Overlay HD/CC"",
      ""guid"": ""1"",
      ""description"": ""This example shows tooltip overlays for captions and quality."",
      ""image"": ""http://content.jwplatform.com/thumbs/3XnJSIm4-640.jpg"",
      ""source"": {
        ""file"": ""http://content.jwplatform.com/videos/3XnJSIm4-kNspJqnJ.mp4"",
        ""label"": ""360p""
      },
      ""sources"": [
        {
          ""file"": ""http://content.jwplatform.com/videos/3XnJSIm4-DZ7jSYgM.mp4"",
          ""label"": ""720p""
        },
        {
          ""file"": ""http://content.jwplatform.com/videos/3XnJSIm4-kNspJqnJ.mp4"",
          ""label"": ""360p""
        },
        {
          ""file"": ""http://content.jwplatform.com/videos/3XnJSIm4-injeKYZS.mp4"",
          ""label"": ""180p""
        }
      ],
      ""tracks"": [
        {
          ""file"": ""http://content.jwplatform.com/captions/2UEDrDhv.txt"",
          ""label"": ""English""
        },
        {
          ""file"": ""http://content.jwplatform.com/captions/6aaGiPcs.txt"",
          ""label"": ""Japanese""
        },
        {
          ""file"": ""http://content.jwplatform.com/captions/2nxzdRca.txt"",
          ""label"": ""Russian""
        },
        {
          ""file"": ""http://content.jwplatform.com/captions/BMjSl0KC.txt"",
          ""label"": ""Spanish""
        }
      ]
    }
  }
 },
 {
  ""items"": null
 }
}";
            string actual = null;

            string xml = @"<RSS xmlns:jwplayer=""http://support.jwplayer.com/customer/portal/articles/1403635-media-format-reference#feeds"" version=""2.0"">
  <Channel>
    <items>
      <item>
        <title>Overlay HD/CC</title>
        <guid>1</guid>
        <description>This example shows tooltip overlays for captions and quality.</description>
        <jwplayer:image>http://content.jwplatform.com/thumbs/3XnJSIm4-640.jpg</jwplayer:image>
          <jwplayer:source file=""http://content.jwplatform.com/videos/3XnJSIm4-kNspJqnJ.mp4"" label=""360p"" />
        <jwplayer:sources>
          <jwplayer:source file=""http://content.jwplatform.com/videos/3XnJSIm4-DZ7jSYgM.mp4"" label=""720p"" />
          <jwplayer:source file=""http://content.jwplatform.com/videos/3XnJSIm4-kNspJqnJ.mp4"" label=""360p"" />
          <jwplayer:source file=""http://content.jwplatform.com/videos/3XnJSIm4-injeKYZS.mp4"" label=""180p"" />
        </jwplayer:sources>
        <jwplayer:tracks>
          <jwplayer:track file=""http://content.jwplatform.com/captions/2UEDrDhv.txt"" label=""English"" />
          <jwplayer:track file=""http://content.jwplatform.com/captions/6aaGiPcs.txt"" label=""Japanese"" />
          <jwplayer:track file=""http://content.jwplatform.com/captions/2nxzdRca.txt"" label=""Russian"" />
          <jwplayer:track file=""http://content.jwplatform.com/captions/BMjSl0KC.txt"" label=""Spanish"" />
        </jwplayer:tracks>
      </item>
    </items>
  </Channel>
  <Channel>
  </Channel>

</RSS>";
            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml)
                .Configure(c => c.NullValueHandling = ChoNullValueHandling.Ignore)
                //.Configure(c => c.RetainXmlAttributesAsNative = true)
                )
            {
                using (var w = new ChoJSONWriter(sb)
                    .Configure(c => c.SupportMultipleContent = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    )
                {
                    w.Write(p);
                }

            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample43()
        {
            string expected = @"[
 {
  ""name"": ""slideshow"",
  ""xsl"": ""http://localhost:8080/Xsl-c.xslt"",
  ""category"": [
   {
     ""name"": ""1234"",
     ""xsl"": ""http://localhost:8080/Xsl-b.xslt""
   }
  ]
 },
 {
  ""name"": ""article"",
  ""xsl"": ""http://localhost:8080/Xsl-a.xslt"",
  ""category"": [
   {
     ""name"": ""1234"",
     ""xsl"": ""http://localhost:8080/Xsl-b.xslt""
   },
   {
     ""name"": ""1234"",
     ""xsl"": ""http://localhost:8080/Xsl-b.xslt""
   }
  ]
 }
]";
            string actual = null;

            string xml = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
<XslMapper>
  <type name=""slideshow"" xsl=""http://localhost:8080/Xsl-c.xslt"" >
    <category name=""1234"" xsl=""http://localhost:8080/Xsl-b.xslt""></category>
  </type>
  <type name=""article"" xsl=""http://localhost:8080/Xsl-a.xslt"">
    <category name=""1234"" xsl=""http://localhost:8080/Xsl-b.xslt""></category>
    <category name=""1234"" xsl=""http://localhost:8080/Xsl-b.xslt""></category>
  </type>
</XslMapper>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml)
                .Configure(c => c.NullValueHandling = ChoNullValueHandling.Ignore)
                .Setup(s => s.MembersDiscovered += (o, e) => e.Value.AddOrUpdate("category", typeof(Object[])))
                )
            {
                using (var w = new ChoJSONWriter(sb))
                {
                    w.Write(p);
                }

            }
            Console.WriteLine(sb.ToString());
        }

        //[Test]
        public static void Sample42()
        {
            string expected = @"[
 {
  ""FirstName"": ""Luke"",
  ""Last_Name"": null,
  ""EmpID"": null
 },
 {
  ""FirstName"": ""Luke"",
  ""Last_Name"": null,
  ""EmpID"": null
 }
]";
            // above is the original console output, below is my expectation (neuli1980)
            expected = @"[
 {
  ""FirstName"": ""Luke"",
  ""Last_Name"": ""Skywalker"",
  ""EmpID"": 1234
 },
 {
  ""FirstName"": ""Luke"",
  ""Last_Name"": ""Skywalker"",
  ""EmpID"": 1234
 }
]";
            string actual = null;

            string xml = @"<custs><CUST><First_Name>Luke</First_Name> <Last_Name>Skywalker</Last_Name> <ID><![CDATA[1234]]></ID> </CUST><CUST><First_Name>Luke</First_Name> <Last_Name>Skywalker</Last_Name> <ID><![CDATA[1234]]></ID> </CUST></custs>";
            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader<Emp>.LoadText(xml)/*.WithXPath("/")*/
                .Configure(c => c.EmptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Empty)
                )
            {
                using (var w = new ChoJSONWriter<Emp>(sb)
                    //.Configure(c => c.SupportMultipleContent = true)
                    //.Configure(c => c.RootName = "Emp")
                    //.Configure(c => c.IgnoreNodeName = true)
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample41()
        {
            string expected = @"{
 ""GetItemRequest"": {
  ""ApplicationCrediential"": {
    ""ConsumerKey"": """",
    ""ConsumerSecret"": """"
  }
 }
}";
            string actual = null;

            string xml = @"<GetItemRequest>
    <ApplicationCrediential>
        <ConsumerKey></ConsumerKey>
        <ConsumerSecret></ConsumerSecret>
    </ApplicationCrediential>
</GetItemRequest>
            ";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("/")
                .Configure(c => c.EmptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Empty)
                )
            {
                using (var w = new ChoJSONWriter(sb)
                    .Configure(c => c.SupportMultipleContent = true)
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample40()
        {
            string expected = @"[
 {
  ""FirstName"": ""name1"",
  ""LastName"": ""surname1""
 },
 {
  ""FirstName"": ""name2"",
  ""LastName"": ""surname2""
 },
 {
  ""FirstName"": ""name3"",
  ""LastName"": ""surname3""
 }
]";
            string actual = null;

            string xml = @"<Employees xmlns:x1=""http://company.com/schemas"">
                <Employee>
                    <FirstName>name1</FirstName>
                    <LastName>surname1</LastName>
                </Employee>
                <Employee>
                    <FirstName>name2</FirstName>
                    <LastName>surname2</LastName>
                </Employee>
                <Employee>
                    <FirstName>name3</FirstName>
                    <LastName>surname3</LastName>
                </Employee>
            </Employees>
            ";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml)
                .WithXmlNamespace("x1", "http://company.com/schemas")
                )
            {
                using (var w = new ChoJSONWriter(sb)
                    .Configure(c => c.SupportMultipleContent = false)
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample39()
        {
            string expected = @"forecast_conditions_day_of_week data,forecast_conditions_low data,forecast_conditions_high data,forecast_conditions_icon data,forecast_conditions_condition data
Sun,34,48,/ig/images/weather/mostly_sunny.gif,Partly Sunny
Mon,32,45,/ig/images/weather/sunny.gif,Clear";
            string actual = null;

            string xml = @"<weather>
    <current_conditions>
        <condition data=""Mostly Cloudy"" />
        <temp_f data=""48"" />
        <temp_c data=""9"" />
        <humidity data=""Humidity: 71%"" />
        <icon data=""/ig/images/weather/mostly_cloudy.gif"" />
        <wind_condition data=""Wind: W at 17 mph"" />
    </current_conditions>
    <forecast_conditions>
        <day_of_week data=""Sun"" />
        <low data=""34"" />
        <high data=""48"" />
        <icon data=""/ig/images/weather/mostly_sunny.gif"" />
        <condition data=""Partly Sunny"" />
    </forecast_conditions>
    <forecast_conditions>
        <day_of_week data=""Mon"" />
        <low data=""32"" />
        <high data=""45"" />
        <icon data=""/ig/images/weather/sunny.gif"" />
        <condition data=""Clear"" />
    </forecast_conditions>
</weather>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("/forecast_conditions"))
            {
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample38()
        {
            string expected = @"results_field,results_something,results_name
2,0,alex
0,0,jack
2,1,heath
0,0,blake";
            string actual = null;

            string xml = @"<?xml version=""1.0""?>
            <results>
                <results>
                        <field>2</field>
                        <something>0</something>
                        <name>alex</name>
                </results>
                <results>
                        <field>0</field>
                        <something>0</something>
                        <name>jack</name>
                </results>
                <results>
                        <field>2</field>
                        <something>1</something>
                        <name>heath</name>
                </results>
                <results>
                        <field>0</field>
                        <something>0</something>
                        <name>blake</name>
                </results>
            </results>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml))
            {
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample37()
        {
            string expected = @"[
 {
  ""Products"": [
   {
     ""ProductCode"": ""C1010"",
     ""CategoryName"": ""Coins""
   }
   {
     ""ProductCode"": ""C1012"",
     ""CategoryName"": ""Coins""
   }
   {
     ""ProductCode"": ""C1013"",
     ""CategoryName"": ""Coins""
   }
  ]
 }
]";
            string actual = null;

            string xml = @"<Products>
  <Product ProductCode=""C1010"" CategoryName=""Coins"" />
  <Product ProductCode=""C1012"" CategoryName=""Coins"" />
  <Product ProductCode=""C1013"" CategoryName=""Coins"" />
</Products>";

            using (var p = ChoXmlReader.LoadText(xml).WithXPath("/")
                //.Configure(c => c.RetainXmlAttributesAsNative = false)
                )
            {
                actual = ChoJSONWriter.ToTextAll(p.ToArray());
                //foreach (var rec in p)
                //    Console.WriteLine(rec.Dump());
                //using (var w = new ChoCSVWriter(sb)
                //	.WithFirstLineHeader()
                //	)
                //	w.Write(p);
            }

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample36()
        {
            string expected = @"<DataRows>
  <DataRow>
    <ColumnNames>
      <ColumnName>Value1</ColumnName>
  <ColumnName>Value3</ColumnName>
  <ColumnName>Value4</ColumnName>
  <ColumnName>Value5</ColumnName>
  <ColumnName>Value6</ColumnName></ColumnNames>
  </DataRow>
</DataRows>";
            string actual = null;

            string xml = @"<root>
  <DataRow>
    <ColumnName>Value1</ColumnName>
    <ColumnName>Value3</ColumnName>
    <ColumnName>Value4</ColumnName>
    <ColumnName>Value5</ColumnName>
    <ColumnName>Value6</ColumnName>
  </DataRow>
</root>";

            using (var p = ChoXmlReader.LoadText(xml)
                .Configure(c => c.RetainAsXmlAwareObjects = true)
                )
            {
                actual = ChoXmlWriter.ToTextAll(p.ToArray());
                //foreach (var rec in p)
                //    Console.WriteLine(rec.Dump());
                //using (var w = new ChoCSVWriter(sb)
                //	.WithFirstLineHeader()
                //	)
                //	w.Write(p);
            }

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample35()
        {
            string expected = @"A_TempFZ1_Set,A_TempHZ2_Set,A_TempHZ3_Set
60,195,195";
            string actual = null;

            string xml = @"<VWSRecipeFile>
                <EX_Extrusion User=""ABC"" Version=""1.0"" Description="""" LastChange=""41914.7876341204"">
                    <Values>
                        <C22O01_A_TempFZ1_Set Item=""A_TempFZ1_Set"" Type=""4"" Hex=""42700000"" Value=""60""/>
                        <C13O02_A_TempHZ2_Set Item=""A_TempHZ2_Set"" Type=""4"" Hex=""43430000"" Value=""195""/>
                        <C13O03_A_TempHZ3_Set Item=""A_TempHZ3_Set"" Type=""4"" Hex=""43430000"" Value=""195""/>
                    </Values>
                </EX_Extrusion>
            </VWSRecipeFile>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("/Values/*"))
            {
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    )
                    w.Write(p.ToDictionary(r => r.Item, r => r.Value).ToDynamic());
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample34()
        {
            string expected = @"{
 ""Items"": [
  {
    ""Name"": ""name"",
    ""Detail"": ""detail""
  }
 ]
}";
            string actual = null;

            string xml = @"<Items>
 <Item>
    <Name>name</Name>
    <Detail>detail</Detail>    
  </Item>
</Items>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("/"))
            {
                using (var w = new ChoJSONWriter(sb)
                    .Configure(c => c.SupportMultipleContent = true)
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample33()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"Value",new object[]{
                            new ChoDynamicObject{ { "id", "108013515952807_470186843068804" },{ "created_time", new DateTime(2013,05,14,20,43,28,DateTimeKind.Utc).ToLocalTime() } },
                            new ChoDynamicObject{ {"message", "TEKST" }, { "id", "108013515952807_470178529736302" },{ "created_time", new DateTime(2013,05,14,20,22,07,DateTimeKind.Utc).ToLocalTime() } }
                } } }
/*                new ChoDynamicObject{{"Value",new object[] 
                {
                    new ChoDynamicObject { { "id", "108013515952807" }, {"posts", new ChoDynamicObject {
                        { "data",new object[]
                        {
                        }
                        } } } }
                } }}*/
            };
            List<object> actual = new List<object>();

            string json = @"
{
    ""id"":""108013515952807"",

    ""posts"":
    {
                ""data"":[
                {
            ""id"":""108013515952807_470186843068804"",
                    ""created_time"":""2013-05-14T20:43:28+0000""

        },
        {
            ""message"":""TEKST"",
            ""id"":""108013515952807_470178529736302"",
            ""created_time"":""2013-05-14T20:22:07+0000""
        }
        ]
    }
}";

            using (var p = ChoJSONReader.LoadText(json).WithJSONPath("$..posts.data").Configure(c => c.MaxScanRows = 10))
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample32()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject("book"){{"id",(Int64)1},{"date",new DateTime(2012,2,1)},{"title","XML Developer's Guide" },
                    {"price",(double)44.95 }, {"description","An in-depth look at creating applications\n            with XML." } },
                new ChoDynamicObject("book"){{"id",(Int64)2},{"date",new DateTime(2013,10,16)},{"title","Dolor sit amet" },
                    {"price",(double)5.95 }, {"description",@"Lorem ipsum" } }
            };
            List<object> actual = new List<object>();

            string xml = @"<?xml version=""1.0""?>
<catalog>
    <book id=""1"" date=""2012-02-01"">
        <title>XML Developer's Guide</title>
        <price>44.95</price>
        <description>
            An in-depth look at creating applications
            with XML.
        </description>
    </book>
    <book id=""2"" date=""2013-10-16"">
        <author>Mark Colsberg</author>
        <title>Dolor sit amet</title>
        <price>5.95</price>
        <description>Lorem ipsum</description>
    </book>
</catalog>";

            using (var p = ChoXmlReader.LoadText(xml))
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample31()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject("Column"){{"Name","key1"},{ "DataType", "Boolean" },{ "Column", "True" } },
                new ChoDynamicObject("Column"){ { "Name", "key2" },{ "DataType", "String" },{ "Column",  "Hello World" } },
                new ChoDynamicObject("Column"){ { "Name", "key3" },{ "DataType", "Integer" }, { "Column", "999" } }
            };
            List<object> actual = new List<object>();

            string xml = @"<Columns>
 <Column Name=""key1"" DataType=""Boolean"">True</Column>
 <Column Name=""key2"" DataType=""String"">Hello World</Column>
 <Column Name=""key3"" DataType=""Integer"">999</Column>
</Columns>";

            using (var p = ChoXmlReader.LoadText(xml))
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample30()
        {
            StringBuilder msg = new StringBuilder();
            using (var p = new ChoXmlReader(FileNameSample30XML)
                .WithXPath("/")
                //.WithField("packages", fieldName: "Package")
                .Configure(c => c.EmptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Ignore)
                )
            {
                using (var w = new ChoJSONWriter(msg)
                    .Configure(c => c.SupportMultipleContent = true)
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    )
                {
                    w.Write(p);
                }
                //using (var w = new ChoXmlWriter(new StringWriter(msg))
                //	.Configure(c => c.NullValueHandling = ChoNullValueHandling.Empty)
                //	//.Configure(c => c.SupportMultipleContent = true)
                //	//.WithFirstLineHeader()
                //	.Configure(c => c.RootName = String.Empty)
                //	.Configure(c => c.IgnoreRootName = true)
                //	.Configure(c => c.IgnoreNodeName = true)
                //	)
                //{
                //	w.Write(p);
                //}
            }
            using (var sw = new StreamWriter(FileNameSample30ActualJSON))
                sw.Write(msg.ToString());
            FileAssert.AreEqual(FileNameSample30ExpectedJSON, FileNameSample30ActualJSON);
        }

        //[Test]
        public static void JSONArrayTest()
        {
            string expected = @"""ApplicationCrediential"": {
 ""ConsumerKey"": {
   ""Consumer"": [
     {
       ""isActive"": ""false"",
       ""#text"": ""Tom""
     },
     {
       ""#text"": ""Mark""
     }
   ]
 },
 ""ConsumerSecret"": null
}
";
            string actual = null;

            string xml = @"<GetItemRequest xmlns:json=""http://james.newtonking.com/projects/json"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema"">
    <ApplicationCrediential>
        <ConsumerKey>
            <Consumer json:Array='true' xsi:nil=""true"">
                <Name isActive = 'false'>Tom</Name>
                <Name>Mark</Name>
            </Consumer>
        </ConsumerKey>
        <ConsumerSecret></ConsumerSecret>
    </ApplicationCrediential>
</GetItemRequest>";

            StringBuilder msg = new StringBuilder();
            using (var p = new ChoXmlReader(new StringReader(xml))
                .WithXPath("//ApplicationCrediential")
                .WithField("ConsumerKey")
                .WithField("ConsumerSecret")
            )
            {
                foreach (var rec in p)
                    msg.AppendLine(ChoJSONWriter.ToText(rec));

                //var x = p.First();
                //Console.WriteLine(ChoJSONWriter.ToText(x));
                //Console.WriteLine(ChoXmlWriter.ToText(x));
            }

            actual = msg.ToString();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample22()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {{"dateprodstart",new DateTime(2018,3,19)}}
            };
            List<object> actual = new List<object>();

            string xml = @"<?xml version=""1.0"" encoding=""utf- 8""?>

<ListItems dateprodstart=""20180319"" heureprodstart=""12:08:36"" 
dateprodend=""20180319"" heureprodend=""12:12:45"" version=""1.21"" >

<item>
    <filename>test5</filename>
    <destination>O</destination>
    <test1>EVA00</test1>
    <test2>ko</test2>
</item>

<item>
    <filename>test</filename>
    <destination>O</destination>
    <test1>xxxx</test1>
    <test2>xxxx</test2>
</item>
</ListItems>";

            using (var p = new ChoXmlReader(new StringReader(xml))
                .WithXPath("/ListItems")
                .WithField("dateprodstart", fieldType: typeof(DateTime), formatText: "yyyyMMdd")
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample20Test()
        {
            string expected = @"""GetItemRequest"": {
 ""ApplicationCrediential"": {
   ""ConsumerKey"": null,
   ""ConsumerSecret"": {
     ""nil"": ""true""
   }
 }
}";
            string actual = null;

            string xml = @"<GetItemRequest xmlns:xsi=""http://www.w3.org/2001/XMLSchema"">
    <ApplicationCrediential>
        <ConsumerKey></ConsumerKey>
        <ConsumerSecret xsi:nil=""true""></ConsumerSecret>
    </ApplicationCrediential>
</GetItemRequest>";

            //ChoXmlSettings.XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";
            using (var p = new ChoXmlReader(new StringReader(xml))
                .WithXPath("/")
            )
            {
                var x = p.First();
                actual = ChoJSONWriter.ToText(x);
            }

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample21()
        {

            List<object> expected = new List<object>
            {
                @"{
 ""type"": ""MCS"",
 ""id"": ""id1"",
 ""description"": ""desc1""
}",@"{
 ""type"": ""MCS"",
 ""id"": ""id2"",
 ""description"": ""desc2""
}",@"{
 ""type"": ""MCM"",
 ""id"": ""id3"",
 ""description"": ""desc3""
}",@"{
 ""type"": ""MCM"",
 ""id"": ""id4"",
 ""description"": ""desc4""
}"
            };
            List<object> actual = new List<object>();

            string xml = @"<AdapterCards>
    <cards type=""MCS"">
        <card>
            <id>id1</id>
            <description>desc1</description>
            <mccode>code1</mccode>
        </card>
        <card>
            <id>id2</id>
            <description>desc2</description>
            <mccode>code2</mccode>
        </card>
    </cards>
    <cards type=""MCM"">
        <card>
            <id>id3</id>
            <description>desc3</description>
            <mccode>code3</mccode>
        </card>
        <card>
            <id>id4</id>
            <description>desc4</description>
            <mccode>code4</mccode>
        </card>
    </cards>
    <cards type=""F""/>
    <cards type=""B""/>
</AdapterCards>";


            using (var p = new ChoXmlReader(new StringReader(xml))
            )
            {
                foreach (var rec in p.SelectMany(r1 => r1.cards == null ? Enumerable.Empty<object>() : ((dynamic[])r1.cards).Select(r2 => new { type = r1.type, id = r2.id, description = r2.description })))
                    actual.Add(ChoJSONWriter.ToText(rec));
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample19()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject("WorkUnit") { { "ID", 130 }, { "EmployeeID", 3 }, { "AllocationID", 114 }, { "TaskID", 239 }, { "ProjectID", 26 }, { "ProjectName","LIK Template"} }
            };
            List<object> actual = null;

            using (var p = new ChoXmlReader(FileNameSample19XML)
                //.WithXmlNamespace("tlp", "http://www.timelog.com/XML/Schema/tlp/v4_4")
                )
            {
                actual = p.ToList();
                //        //foreach (var rec in p)
                //        //	Console.WriteLine(rec.Dump());
                //        //return;
                //        using (var w = new ChoCSVWriter(Console.Out)
                //.WithFirstLineHeader()
                //)
                //        {
                //            w.Write(p);
                //        }

                //        Console.WriteLine();
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample18()
        {
            using (var p = new ChoXmlReader(FileNameSample18XML)
                )
            {
                //foreach (dynamic rec in p)
                //{
                //	var z = ((IList<object>)rec.product_lineitems).SelectMany<object, string>(r1 => ((dynamic)r1).price);
                //	foreach (var z1 in z)
                //		Console.WriteLine(z1);
                //}

                using (var w = new ChoCSVWriter(Console.Out)
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(p.SelectMany(r => ((IList<object>)r.product_lineitems).Cast<dynamic>().Select(r1 => new { original_impot_no = r.original_impot_no, price = r1.price })));
                }
                Console.WriteLine();
                return;
                foreach (var rec in p)
                {
                    Console.WriteLine(rec.original_impot_no);
                    //var x = ((IList<object>)rec.product_lineitems).ToArray();

                    var x = ((IList<object>)rec.product_lineitems).Cast<dynamic>().Select(r => new { original_impot_no = rec.original_impot_no, price = r.price }).ToArray();

                    //foreach (dynamic li in (IList<object>)rec.product_lineitems)
                    //{
                    //	Console.WriteLine(li.id);
                    //	Console.WriteLine(li.price);
                    //}

                    Console.WriteLine(rec.GetXml());
                }
            }
        }

        public class Emp
        {
            [ChoXmlElementRecordField(FieldName = "First_Name")]
            public string FirstName { get; set; }
            public string Last_Name { get; set; }
            public EmpID EmpID { get; set; }
        }

        public class EmpID
        {
            public int ID { get; set; }
        }

        //[Test]
        public static void NoEncodeTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"id","&lt;bk101&gt;"}},
                new ChoDynamicObject{{"id","bk102"}},
                new ChoDynamicObject{{"id","bk103"}}
            };
            List<object> actual = new List<object>();

            using (var xr = new ChoXmlReader(FileNameNoEncodeXML)
                .WithField("id", encodeValue: false)
            )
            {
                foreach (dynamic rec in xr)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }


        //[Test]
        public static void Sample17()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {{"HouseNumber",(int)1},{"RoomInfos",new List<RoomInfo> { new RoomInfo {  RoomNumber = 1}, new RoomInfo {  RoomNumber = 2}, new RoomInfo { RoomNumber = 2  } }},
                    { "Furnitures", new Table { Color = "Blue"} } }
            };
            List<object> actual = new List<object>();

            using (var xr = new ChoXmlReader(FileNameSample17XML).WithXPath("//HouseInfo")
                .WithField("HouseNumber", fieldType: typeof(int))
                .WithField("RoomInfos", xPath: "//HouseLog/RoomInfo", fieldType: typeof(List<RoomInfo>))
                .WithField("Furnitures", xPath: "//HouseLog/RoomInfo/Furnitures/Table", fieldType: typeof(Table))
            )
            {
                foreach (dynamic rec in xr)
                    actual.Add(rec);
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        public static string FileNameHtmlTableXML => "HtmlTable.xml";
        public static string FileNameHTMLTableToCSVActualCSV => "HtmlTableToCSVActual.csv";
        public static string FileNameHTMLTableToCSVExpectedCSV => "HtmlTableToCSVExpected.csv";
        public static string FileNameSampleXML => "sample.xml";
        public static string FileNameXmlToCSVSampleActualCSV => "XmlToCSVSampleActual.csv";
        public static string FileNameXmlToCSVSampleExpectedCSV => "XmlToCSVSampleExpected.csv";
        public static string FileNameXmlToCSVSample2ActualCSV => "XmlToCSVSample2Actual.csv";
        public static string FileNameXmlToCSVSample2ExpectedCSV => "XmlToCSVSample2Expected.csv";
        public static string FileNameXmlToCSVSample3ActualCSV => "XmlToCSVSample3Actual.csv";
        public static string FileNameXmlToCSVSample3ExpectedCSV => "XmlToCSVSample3Expected.csv";
        public static string FileNameXmlToCSVSample5ActualCSV => "XmlToCSVSample5Actual.csv";
        public static string FileNameXmlToCSVSample5ExpectedCSV => "XmlToCSVSample5Expected.csv";
        public static string FileNameXmlToCSVSample6ActualCSV => "XmlToCSVSample6Actual.csv";
        public static string FileNameXmlToCSVSample6ExpectedCSV => "XmlToCSVSample6Expected.csv";
        public static string FileNameXmlToCSVSample7ActualCSV => "XmlToCSVSample7Actual.csv";
        public static string FileNameXmlToCSVSample7ExpectedCSV => "XmlToCSVSample7Expected.csv";
        public static string FileNameSample2XML => "sample2.xml";
        public static string FileNameSample3XML => "sample3.xml";
        public static string FileNameSample4XML => "sample4.xml";
        public static string FileNameSample5XML => "sample5.xml";
        public static string FileNameSample6XML => "sample6.xml";
        public static string FileNameSample7XML => "sample7.xml";
        public static string FileNameSample8XML => "sample8.xml";
        public static string FileNameSample8ActualJSON => "sample8Actual.json";
        public static string FileNameSample8ExpectedJSON => "sample8Expected.json";
        public static string FileNameSample9XML => "sample9.xml";
        public static string FileNameSample10XML => "sample10.xml";
        public static string FileNameSample11XML => "sample11.xml";
        public static string FileNameSample12XML => "sample12.xml";
        public static string FileNameSample13XML => "sample13.xml";
        public static string FileNameXmlNullTestActualJSON => "XmlNullTestActual.json";
        public static string FileNameXmlNullTestExpectedJSON => "XmlNullTestExpected.json";
        public static string FileNameSample14XML => "sample14.xml";
        public static string FileNameSample14ActualXML => "sample14Actual.xml";
        public static string FileNameSample14ExpectedXML => "sample14Expected.xml";
        public static string FileNameSample15XML => "sample15.xml";
        public static string FileNameSample16XML => "sample16.xml";
        public static string FileNameSample17XML => "sample17.xml";
        public static string FileNameSample18XML => "sample18.xml";
        public static string FileNameSample19XML => "sample19.xml";
        public static string FileNameSample20XML => "sample20.xml";
        public static string FileNameSample21XML => "sample21.xml";
        public static string FileNameSample22XML => "sample22.xml";
        public static string FileNameSample30XML => "sample30.xml";
        public static string FileNameSample30ActualJSON => "sample30Actual.json";
        public static string FileNameSample30ExpectedJSON => "sample30Expected.json";
        public static string FileNameSample48XML => "sample48.xml";
        public static string FileNameSample49XML => "sample49.xml";
        public static string FileNameSample50XML => "sample50.xml";
        public static string FileNameSample70XML => "sample70.xml";
        public static string FileNameSample71XML => "sample71.xml";

        public static string FileNameXmlToJSONSample4ActualJSON => "XmlToJSONSample4Actual.json";
        public static string FileNameXmlToJSONSample4ExpectedJSON => "XmlToJSONSample4Expected.json";
        public static string FileNameJSONToXmlSample4JSON => "JSONToXmlSample4.json";
        public static string FileNameJSONToXmlSample4ActualXML => "JSONToXmlSample4Actual.xml";
        public static string FileNameJSONToXmlSample4ExpectedXML => "JSONToXmlSample4Expected.xml";
        public static string FileNameNoEncodeXML => "NoEncode.xml";
        public static string FileNamePivot1XML => "Pivot1.xml";


        //[Test]
        public static void HTMLTableToCSV()
        {
            using (var cr = new ChoCSVWriter(FileNameHTMLTableToCSVActualCSV).WithFirstLineHeader())
            {
                using (var xr = new ChoXmlReader(FileNameHtmlTableXML).WithXPath("//tbody/tr")
                    .WithField("Lot", xPath: "td[1]", fieldType: typeof(int))
                    .WithField("Op", xPath: "td[2]", fieldType: typeof(int))
                    .WithField("Status", xPath: "td[3]", fieldType: typeof(string))
                    .WithField("iDispoStatus", xPath: "td[4]", fieldType: typeof(string))
                    .WithField("DispoBy", xPath: "td[5]", fieldType: typeof(string))
                    .WithField("DispoDate", xPath: "td[6]", fieldType: typeof(DateTime))
                    .WithField("TRCount", xPath: "td[7]", fieldType: typeof(int))
                    .WithField("View", xPath: "td[8]/a/@href", fieldType: typeof(string))
                )
                {
                    cr.Write(xr);
                }
            }

            FileAssert.AreEqual(FileNameHTMLTableToCSVExpectedCSV, FileNameHTMLTableToCSVActualCSV);
        }

        //[Test]
        public static void BulkLoad1()
        {
            Assert.Fail(@"Database file C:\USERS\NRAJ39\DOWNLOADS\ADVENTUREWORKS2012_DATA.MDF not attached.");

            string connectionstring = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=C:\USERS\NRAJ39\DOWNLOADS\ADVENTUREWORKS2012_DATA.MDF;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            int houseNo = 0;
            using (var xr = new ChoXmlReader("sample17.xml").WithXPath("/HouseInfo")
                .WithField("HouseNumber", fieldType: typeof(int))
                )
            {
                houseNo = xr.First().HouseNumber;
            }
            using (var xr = new ChoXmlReader("sample17.xml").WithXPath("/HouseInfo/HouseLog/RoomInfo")
                .WithField("HouseNumber", fieldType: typeof(int), valueConverter: (o) => houseNo)
                .WithField("RoomNumber", fieldType: typeof(int))
                .WithField("Timestamp", fieldType: typeof(DateTime))
                .WithField("Color", xPath: "Furnitures/Table/Color", fieldType: typeof(string))
                .WithField("Height", xPath: "Furnitures/Table/Height", fieldType: typeof(string))
                .WithField("Scope", xPath: "ToolCounts/Scope", fieldType: typeof(int))
                .WithField("Code", xPath: "Bathroom/Code", fieldType: typeof(int))
                .WithField("Faucet", xPath: "Bathroom/Faucets", fieldType: typeof(int))
            )
            {
                //foreach (dynamic rec in xr)
                //{
                //    Console.WriteLine(rec.DumpAsJson());
                //}
                //return;
                using (SqlBulkCopy bcp = new SqlBulkCopy(connectionstring))
                {
                    bcp.DestinationTableName = "dbo.HOUSEINFO";
                    bcp.EnableStreaming = true;
                    bcp.BatchSize = 10000;
                    bcp.BulkCopyTimeout = 0;
                    bcp.NotifyAfter = 10;
                    bcp.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs e)
                    {
                        Console.WriteLine(e.RowsCopied.ToString("#,##0") + " rows copied.");
                    };
                    bcp.WriteToServer(xr.AsDataReader());
                }
            }

        }

        //[Test]
        public static void Sample16()
        {
            List<object> expected = new List<object>
            {
                new Person{ FirstName = "John", LastName = "Doe", DateOfBirth = new DateTime(1900,1,12), Address = "100, Example Street"}
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoXmlReader(FileNameSample16XML)
            )
            {
                var dict = parser.ToDictionary(i => (string)i.name, i => (object)i.value, StringComparer.CurrentCultureIgnoreCase);
                var person = dict.ToObject<Person>();

                actual.Add(person);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample12()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"SelectedIdValue",new SelectedIds {  Id = new int[] {108,110,111}} }}
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoXmlReader(FileNameSample12XML)
            .WithField("SelectedIdValue", xPath: "//SelectedIds", fieldType: typeof(SelectedIds))
            )
            {
                foreach (dynamic rec in parser)
                {
                    actual.Add(rec);
                    //                    Console.WriteLine("{0}", rec.GetXml());
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void DynamicXmlTest()
        {
            string expected = @"<Item1 Id=""100"" Name=""Raj"">
  <StartDate @Value=""0001-01-11T00:00:00"" />
  <SelectedIds>
    <Id @Value=""101"" />
    <SelectedId @Value=""102"" />
    <SelectedId @Value=""103"" />
  </SelectedIds>
</Item1>";
            string actual = null;

            ChoDynamicObject src = new ChoDynamicObject("Item1");

            IDictionary<string, object> x = src as IDictionary<string, object>;
            x.Add("@Id", 100);
            x.Add("@Name", "Raj");

            //x.Add("@@Value", "Hello!");
            ChoDynamicObject sd = new ChoDynamicObject();
            ((IDictionary<string, object>)sd).Add("@@Value", "0001-01-11T00:00:00");

            x.Add("StartDate", sd);

            ChoDynamicObject id1 = new ChoDynamicObject("Id");
            ((IDictionary<string, object>)id1).Add("@@Value", 101);

            ChoDynamicObject id2 = new ChoDynamicObject();
            ((IDictionary<string, object>)id2).Add("@@Value", 102);

            ChoDynamicObject id3 = new ChoDynamicObject();
            ((IDictionary<string, object>)id3).Add("@@Value", 103);

            x.Add("SelectedIds", new object[] { id1, id2, id3 });

            actual = src.GetXml();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample15()
        {
            List<object> expected = new List<object>
            {
                "I am not sure, whats expected. Really 2 SOAP-ENV:Header entries?"
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoXmlReader(FileNameSample15XML)
            )
            {
                foreach (dynamic rec in parser)
                {
                    actual.Add(rec);
                    //                    Console.WriteLine(ChoUtility.Dump(rec));
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample14()
        {
            using (var w = new ChoXmlWriter(FileNameSample14ActualXML))
            {
                using (var parser = new ChoXmlReader(FileNameSample14XML)
            )
                {
                    foreach (dynamic rec in parser)
                    {
                        //dynamic x = rec.description;

                        //rec.description = new ChoDynamicObject();
                        //rec.description.val = "100";
                        //rec.description.Value = new FamilyMember();

                        w.Write(rec);
                        Console.WriteLine(ChoUtility.Dump(rec));
                    }
                }
            }

            FileAssert.AreEqual(FileNameSample14ExpectedXML, FileNameSample14ActualXML);
            Assert.Fail("Missing Book with id 101, but source-xml is a valid xml");
        }

        //[Test]
        public static void NullableTest()
        {
            object expected = new Item { Number = 100, ItemName = "TestName1", ItemId = 1 };
            object actual = null;

            string xml = @"<?xml version=""1.0""?>
    <Item Number = ""100"" ItemName = ""TestName1"" ItemId = ""1"" />";

            XDocument doc = XDocument.Parse(xml);

            actual = ChoXmlReader<Item>.LoadXElements(new XElement[] { doc.Root }).FirstOrDefault();

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Pivot1()
        {
            string expected = @"Column1,Column2,Column3
A_TempFZ1_Set,A_TempFZ2_Set,A_TempFZ3_Set
60,196,200";
            string actual = null;

            using (var parser = new ChoXmlReader(FileNamePivot1XML).WithXPath(@"//Values/*")
                .WithField("Item")
                .WithField("Value")
            )
            {

                actual = ChoCSVWriter.ToTextAll(parser.Cast<ChoDynamicObject>().Transpose(false),
                    new ChoCSVRecordConfiguration().Configure(c => c.FileHeaderConfiguration.HasHeaderRecord = true));
            }

            Assert.AreEqual(expected, actual);
            // I am not sure, if that is correct;
        }

        //[Test]
        public static void Sample6()
        {
            List<object> expected = new List<object>
            {
                new JobApplication { JobType = "REQUESTED", JobApplicant = new object[]{
                    new ChoDynamicObject("Applicant") {
                        { "social_security_number", "999999999" },
                        {"type", "PB" },
                        { "date_of_birth", "1972-10-01T00:00:00.0000000" },
                        {"first_name", "Thomas" },
                        { "last_name", "Edison" },
                        { "Addresses", new object[]{
                            new ChoDynamicObject("Address"){ { "city", "Portland" },{ "state_code_id", "MI" },{ "country_code", "USA" },{ "postal_code", "12345" },{ "item_code", "CURRENT" },{ "street_number", "6297" },{ "street", "LAKE ARBOR" } },
                            new ChoDynamicObject("Address"){{"item_code","PREVIOUS"}}
                        } },
                        { "Communications", new object[]{
                            new ChoDynamicObject("Communication"){ { "item_code", "PEMAIL" } , { "com", "edison@gmail.com" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "HOME" } , { "com", "(123)-456-7890" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "OTHER" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "WORK" } , { "com", "(100)-200-3000" } , { "contact_type", "CU" } }
                        } }},
                    new ChoDynamicObject("Applicant") {
                        { "social_security_number", "123456789" },
                        {"type", "CB" },
                        { "date_of_birth", "1976-10-01T00:00:00.0000000" },
                        {"first_name", "Mary" },
                        { "last_name", "Edison" },
                        { "Addresses", new object[]{
                            new ChoDynamicObject("Address"){ { "city", "BarHarBor" },{ "state_code_id", "MI" },{ "country_code", "USA" },{ "postal_code", "12345" },{ "item_code", "CURRENT" },{ "street_number", "6297" },{ "street", "LAKE ARBOR" } },
                            new ChoDynamicObject("Address"){{"item_code","PREVIOUS"}}
                        } },
                        { "Communications", new object[]{
                            new ChoDynamicObject("Communication"){ { "item_code", "PEMAIL" } , { "com", "mary@gmail.com" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "HOME" } , { "com", "(999)-456-7890" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "OTHER" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "WORK" } , { "com", "(300)-200-3000" } , { "contact_type", "CU" } }
                        } }}} },
                new JobApplication { JobType = "RECOMMENDED", JobApplicant = new object[]{
                    new ChoDynamicObject("Applicant") {
                        { "social_security_number", "999999999" },
                        {"type", "PB" },
                        { "date_of_birth", "1972-10-01T00:00:00.0000000" },
                        {"first_name", "Thomas" },
                        { "last_name", "Edison" },
                        { "Addresses", new object[]{
                            new ChoDynamicObject("Address"){ { "city", "Portland" },{ "state_code_id", "MI" },{ "country_code", "USA" },{ "postal_code", "12345" },{ "item_code", "CURRENT" },{ "street_number", "6297" },{ "street", "LAKE ARBOR" } },
                            new ChoDynamicObject("Address"){{"item_code","PREVIOUS"}}
                        } },
                        { "Communications", new object[]{
                            new ChoDynamicObject("Communication"){ { "item_code", "PEMAIL" } , { "com", "edison@gmail.com" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "HOME" } , { "com", "(123)-456-7890" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "OTHER" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "WORK" } , { "com", "(100)-200-3000" } , { "contact_type", "CU" } }
                        } }},
                    new ChoDynamicObject("Applicant") {
                        { "social_security_number", "123456789" },
                        {"type", "CB" },
                        { "date_of_birth", "1976-10-01T00:00:00.0000000" },
                        {"first_name", "Mary" },
                        { "last_name", "Edison" },
                        { "Addresses", new object[]{
                            new ChoDynamicObject("Address"){ { "city", "BarHarBor" },{ "state_code_id", "MI" },{ "country_code", "USA" },{ "postal_code", "12345" },{ "item_code", "CURRENT" },{ "street_number", "6297" },{ "street", "LAKE ARBOR" } },
                            new ChoDynamicObject("Address"){{"item_code","PREVIOUS"}}
                        } },
                        { "Communications", new object[]{
                            new ChoDynamicObject("Communication"){ { "item_code", "PEMAIL" } , { "com", "mary@gmail.com" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "HOME" } , { "com", "(999)-456-7890" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "OTHER" } , { "contact_type", "CU" } },
                            new ChoDynamicObject("Communication"){ { "item_code", "WORK" } , { "com", "(300)-200-3000" } , { "contact_type", "CU" } }
                        } }} }},
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoXmlReader<JobApplication>(FileNameSample6XML)
            )
            {
                foreach (dynamic rec in parser)
                {
                    actual.Add(rec);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void XmlNullTest()
        {
            using (var parser = new ChoXmlReader(FileNameSample13XML)
            )
            {
                //var c = parser.Select(x => (string)x.AustrittDatum).ToArray();
                using (var jw = new ChoJSONWriter(FileNameXmlNullTestActualJSON))
                    //jw.Write(parser.ToArray());
                    jw.Write(new { AustrittDatum = parser.Select(x => x.AustrittDatum).ToArray() });
                //jw.Write(new { AustrittDatum = parser.Select(x => (string)x.AustrittDatum.ToString()).ToArray() });
            }

            FileAssert.AreEqual(FileNameXmlNullTestExpectedJSON, FileNameXmlNullTestActualJSON);
        }

        private static string EmpXml => @"<Employees>
                <Employee Id='1'>
                    <Name isActive = 'true'>Tom</Name>
                </Employee>
                <Employee Id='2'>
                    <Name>Mark</Name>
                </Employee>
            </Employees>
        ";

        //[Test]
        public static void XmlToCSVSample7()
        {
            using (var parser = new ChoXmlReader(FileNameSample7XML).WithXPath("/UpdateDB/Transaction")
                .WithField("Table", xPath: "/Insert/Table")
                .WithField("szCustomerID", xPath: "/Insert/Set/szCustomerID")
                .WithField("szCustomerName", xPath: "/Insert/Set/szCustomerName")
                .WithField("szExternalID", xPath: "/Insert/Set/szExternalID")
                )
            {
                using (var writer = new ChoCSVWriter(FileNameXmlToCSVSample7ActualCSV).WithFirstLineHeader())
                    writer.Write(parser.Where(r => r.Table == "CUSTOMER").Select(r => new { szCustomerID = r.szCustomerID, szCustomerName = r.szCustomerName, szExternalID = r.szExternalID }));
            }

            FileAssert.AreEqual(FileNameXmlToCSVSample7ExpectedCSV, FileNameXmlToCSVSample7ActualCSV);
        }

        //[Test]
        public static void XmlToCSVSample6()
        {
            using (var parser = new ChoXmlReader(FileNameSample6XML).WithXPath("JobApplications")
                .WithField("ID", xPath: "@id")
                .WithField("PB_SSN", xPath: "/JobApplication[@job_type='REQUESTED']/JobApplicationStates/JobApplicationState/Applicants/Applicant[@type='PB']/@social_security_number")
                .WithField("PB_FIRST_NAME", xPath: "/JobApplication[@job_type='REQUESTED']/JobApplicationStates/JobApplicationState/Applicants/Applicant[@type='PB']/@first_name")
                .WithField("PB_CITY", xPath: "/JobApplication[@job_type='REQUESTED']/JobApplicationStates/JobApplicationState/Applicants/Applicant[@type='PB']/Addresses/Address[@item_code='CURRENT']/@city")
                .WithField("PB_STATE", xPath: "/JobApplication[@job_type='REQUESTED']/JobApplicationStates/JobApplicationState/Applicants/Applicant[@type='PB']/Addresses/Address[@item_code='CURRENT']/@state_code_id")
                .WithField("PB_PEMAIL", xPath: "/JobApplication[@job_type='REQUESTED']/JobApplicationStates/JobApplicationState/Applicants/Applicant[@type='PB']/Communications/Communication[@item_code='PEMAIL']/@com")
                .WithField("CB_SSN", xPath: "/JobApplication[@job_type='REQUESTED']/JobApplicationStates/JobApplicationState/Applicants/Applicant[@type='CB']/@social_security_number")
                .WithField("CB_FIRST_NAME", xPath: "/JobApplication[@job_type='REQUESTED']/JobApplicationStates/JobApplicationState/Applicants/Applicant[@type='CB']/@first_name")
                .WithField("CB_CITY", xPath: "/JobApplication[@job_type='REQUESTED']/JobApplicationStates/JobApplicationState/Applicants/Applicant[@type='CB']/Addresses/Address[@item_code='CURRENT']/@city")
                .WithField("CB_STATE", xPath: "/JobApplication[@job_type='REQUESTED']/JobApplicationStates/JobApplicationState/Applicants/Applicant[@type='CB']/Addresses/Address[@item_code='CURRENT']/@state_code_id")
                .WithField("CB_PEMAIL", xPath: "/JobApplication[@job_type='REQUESTED']/JobApplicationStates/JobApplicationState/Applicants/Applicant[@type='CB']/Communications/Communication[@item_code='PEMAIL']/@com")
         )
            {
                using (var writer = new ChoCSVWriter(FileNameXmlToCSVSample6ActualCSV).WithFirstLineHeader())
                    writer.Write(parser);
            }

            FileAssert.AreEqual(FileNameXmlToCSVSample6ExpectedCSV, FileNameXmlToCSVSample6ActualCSV);
        }


        //[Test]
        public static void XmlToCSVSample5()
        {
            using (var parser = new ChoXmlReader(FileNameSample5XML).WithXPath("/PRICE")
            )
            {
                using (var writer = new ChoCSVWriter(FileNameXmlToCSVSample5ActualCSV).WithFirstLineHeader())
                    writer.Write(parser);
            }

            FileAssert.AreEqual(FileNameXmlToCSVSample5ExpectedCSV, FileNameXmlToCSVSample5ActualCSV);
        }

        //[Test]
        public static void Sample8Test()
        {
            using (var parser = new ChoXmlReader(FileNameSample8XML).WithXPath("/root/data")
                .WithField("id", xPath: "@name")
                .WithField("text", xPath: "/value")
            )
            {
                using (var writer = new ChoJSONWriter(FileNameSample8ActualJSON)
                    .Configure(c => c.SupportMultipleContent = true)
                    )
                    writer.Write(new { Texts = parser.ToArray() });
            }

            FileAssert.AreEqual(FileNameSample8ExpectedJSON, FileNameSample8ActualJSON);
        }

        //[Test]
        public static void Sample9Test()
        {
            string expected = @"[
 {
  ""view_id"": ""2adaf1b2"",
  ""view_name"": ""Users by Function"",
  ""view_content_url"": ""ExampleWorkbook/sheets/UsersbyFunction"",
  ""view_total_count"": 95,
  ""view_total_available"": 2
 },
 {
  ""view_id"": ""09ecb39a"",
  ""view_name"": ""Users by Site"",
  ""view_content_url"": ""ExampleWorkbook/sheets/UsersbySite"",
  ""view_total_count"": 95,
  ""view_total_available"": 2
 }
]";
            string actual = null;

            int totalAvailable;
            using (var parser = new ChoXmlReader(FileNameSample9XML, "abc.com/api").WithXPath("/tsResponse/pagination")
                .WithField("totalAvailable", fieldType: typeof(int))
                .WithField("pageNumber", fieldType: typeof(int))
            )
            {
                totalAvailable = parser.FirstOrDefault().totalAvailable;
            }


            StringBuilder sb = new StringBuilder();
            using (var parser = new ChoXmlReader(FileNameSample9XML, "abc.com/api").WithXPath("/tsResponse/views/view")
                .WithField("view_id", xPath: "@id")
                .WithField("view_name", xPath: "@name")
                .WithField("view_content_url", xPath: "@contentUrl")
                .WithField("view_total_count", xPath: "/x:usage/@totalViewCount", fieldType: typeof(int))
            )
            {
                using (var writer = new ChoJSONWriter(sb)
                    )
                {
                    foreach (dynamic rec in parser)
                        writer.Write(new { view_id = rec.view_id, view_name = rec.view_name, view_content_url = rec.view_content_url, view_total_count = rec.view_total_count, view_total_available = totalAvailable });
                    //writer.Write(parser);
                }
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }
        //[Test]
        public static void Sample10Test()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{
                    { "tag1","testt1"},
                    { "tag2","testt2"},
                    { "tag2a","anonym"},
                    { "tag3", "testt3" },
                    { "tag4","testt4"},
                    { "t51","tttt"},
                    { "t52","ttt"},
                    { "t53","ttt"},
                    { "r1",1},
                    { "r2",0} }
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoXmlReader(FileNameSample10XML).WithXPath("/root/body/e1")
                .WithField("tag1", xPath: "en/tag1")
                .WithField("tag2", xPath: "en/tag2/text()")
                .WithField("tag2a", xPath: "en/tag2/@user")
                .WithField("tag3", xPath: "en/tag3")
                .WithField("tag4", xPath: "en/tag4")
                .WithField("t51", xPath: "en/tag5/t51")
                .WithField("t52", xPath: "en/tag5/t52")
                .WithField("t53", xPath: "en/tag5/t53")
                .WithField("r1", xPath: "r1")
                .WithField("r2", xPath: "r2/tr1")
            )
            {
                actual = parser.ToList();
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Sample11Test()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject
                {
                    {"id",1 },
                    {"sons", new object[] {
                        new ChoDynamicObject { { "id", "11" }, { "name", "Tom1" }, { "address", new ChoDynamicObject {{"street","10 River Rd" },{ "city", "Edison" },{ "state", "NJ" } } }, { "workbook", new ChoDynamicObject { { "id", "9fb2948d" } } },{ "owner", new ChoDynamicObject { { "id", "c2abaaa9" } } },{"usage",new ChoDynamicObject{ { "totalViewCount", "95" },{"#text","sdsad" } } } },
                        new ChoDynamicObject { { "id", "12" }, { "name", "Tom2" }, { "address", new ChoDynamicObject {{"street","10 Madison Ave" },{ "city", "New York" },{ "state", "NY" } } }, { "workbook", new ChoDynamicObject { { "id", "9fb2948d" } } },{ "owner", new ChoDynamicObject { { "id", "c2abaaa9" } } },{"usage",new ChoDynamicObject{ { "totalViewCount", "95" },{"#text","sdsad" } } } }
                    } } }
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoXmlReader(FileNameSample11XML).WithXPath("/members/father")
                .WithField("id")
                .WithField("sons")
            )
            {
                actual = parser.ToList();
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        static void xMain1(string[] args)
        {
            return;
            //dynamic p = new ChoPropertyBag();
            //p.Name = "Raj";
            //p.Zip = "10020";

            //foreach (var kvp in ChoExpandoObjectEx.ToExpandoObject(p))
            //    Console.WriteLine(kvp);
            //return;
            //XmlToJSONSample4();
            //    JSONToXmlSample4();

            string json = @"
    {
        ""Row1"":{
            ""x"":123,
            ""y"":21,
            ""z"":22
        },
    }";
            using (var r = ChoJSONReader.LoadText(json)
                .WithField("x", jsonPath: "$.Row1", fieldType: typeof(object))
                .WithField("y", jsonPath: "$.Row1.y", fieldType: typeof(object))
                .WithField("z", jsonPath: "$.Row1", fieldType: typeof(object))
                )
            {
                r.BeforeRecordFieldLoad += (o, e) =>
                {
                    if (e.PropertyName == "x")
                    {
                        //dynamic x = e.PropertyName;
                        //e.Record.x = x.x;
                        e.Skip = true;
                    }
                };
                using (var writer = new ChoXmlWriter("sample.xml"))
                    writer.Write(r);
                return;
            }
        }

        [XmlRoot("Batches")]
        public class Batch
        {
            public string Name;
        }
        [XmlRoot("Levels")]
        public class Level
        {
            public int Id;
            public string Name;
            public int PkgRatio;
        }
        public class VariableData
        {
            public int VariableDataId;
            public string Value;
            public int LevelId;
        }
        public class ProductionOrderFile
        {
            public string ProductionOrderName { get; set; }
            public string ProductCode { get; set; }
            [XmlElement]
            public List<Batch> Batches { get; set; }
            [XmlElement]
            public List<Level> Levels { get; set; }
            [XmlElement]
            public List<VariableData> VariableData { get; set; }
        }

        //[Test]
        public static void JSONToXmlSample4()
        {
            using (var parser = new ChoJSONReader<ProductionOrderFile>(FileNameJSONToXmlSample4JSON).Configure(c => c.UseJSONSerialization = true)
    )
            {
                using (var writer = new ChoXmlWriter<ProductionOrderFile>(FileNameJSONToXmlSample4ActualXML).Configure(c => c.UseXmlSerialization = true))
                    writer.Write(parser);
            }

            FileAssert.AreEqual(FileNameJSONToXmlSample4ExpectedXML, FileNameJSONToXmlSample4ActualXML);
        }

        //[Test]
        public static void XmlToJSONSample4()
        {
            using (var parser = new ChoXmlReader<ProductionOrderFile>(FileNameSample4XML).WithXPath("/").Configure(c => c.UseXmlSerialization = true)
                )
            {
                using (var writer = new ChoJSONWriter(FileNameXmlToJSONSample4ActualJSON).Configure(c => c.UseJSONSerialization = true).Configure(c => c.SupportMultipleContent = false).Configure(c => c.Formatting = Newtonsoft.Json.Formatting.None)
                    )
                    writer.Write(parser);

                //foreach (var x in parser)
                //{
                //    Console.WriteLine(x.ProductionOrderName);
                //    Console.WriteLine("{0}", ((ICollection)x.Batches).Count);
                //    Console.WriteLine("{0}", ((ICollection)x.VariableDatas).Count);
                //}
            }

            FileAssert.AreEqual(FileNameXmlToJSONSample4ExpectedJSON, FileNameXmlToJSONSample4ActualJSON);
            //using (var parser = new ChoXmlReader("sample4.xml").WithXPath("/")
            //    .WithField("ProductionOrderName", xPath: "ProductionOrderName")
            //    .WithField("Batches", xPath: "//Batches/Batch", isCollection: true, fieldType: typeof(Batch))
            //    .WithField("VariableDatas", xPath: "//VariableData", isCollection: true, fieldType: typeof(VariableData))
            //    )
            //{
            //    using (var writer = new ChoJSONWriter("sample3.json"))
            //        writer.Write(parser);

            //    //foreach (var x in parser)
            //    //{
            //    //    Console.WriteLine(x.ProductionOrderName);
            //    //    Console.WriteLine("{0}", ((ICollection)x.Batches).Count);
            //    //    Console.WriteLine("{0}", ((ICollection)x.VariableDatas).Count);
            //    //}
            //}
        }

        //[Test]
        public static void XmlToCSVSample3()
        {
            using (var parser = ChoXmlReader.LoadXElements(XDocument.Load(FileNameSample3XML).XPathSelectElements("//member[name='table']/value/array/data/value"))
                .WithField("id", xPath: "array/data/value[1]")
                .WithField("scanTime", xPath: "array/data/value[2]")
                .WithField("host", xPath: "array/data/value[3]")
                .WithField("vuln", xPath: "array/data/value[4]")
                .WithField("port", xPath: "array/data/value[5]")
                .WithField("protocol", xPath: "array/data/value[6]")
            )
            {
                using (var writer = new ChoCSVWriter(FileNameXmlToCSVSample3ActualCSV).WithFirstLineHeader())
                    writer.Write(parser);

            }

            FileAssert.AreEqual(FileNameXmlToCSVSample3ExpectedCSV, FileNameXmlToCSVSample3ActualCSV);
        }

        //[Test]
        public static void XmlToCSVSample2()
        {
            using (var parser = new ChoXmlReader(FileNameSample2XML)
                .WithField("messageID")
                .WithField("orderNumber")
                .WithField("model")
                .WithField("tls")
                .WithField("status")
                .WithField("timestamp")
                .WithField("message")
                .WithField("attributes", xPath: "attributes/attribute", fieldType: typeof(IList))
                )
            {
                parser.BeforeRecordFieldLoad += (o, e) =>
                {
                    dynamic a = e.Record;
                    IDictionary<string, object> dict = (IDictionary<string, object>)e.Record;

                    if (e.PropertyName == "attributes")
                    {
                        ((IList<object>)e.Source).Cast<XElement>().Select(e1 =>
                        {
                            dict[e1.Attribute("name").Value] = e1.Attribute("value").Value;

                            return e1;
                        }).ToArray();

                        e.Skip = true;
                    }
                };
                using (var writer = new ChoCSVWriter(FileNameXmlToCSVSample2ActualCSV).WithFirstLineHeader())
                    writer.Write(parser);
            }

            FileAssert.AreEqual(FileNameXmlToCSVSample2ExpectedCSV, FileNameXmlToCSVSample2ActualCSV);
        }

        //[Test]
        public static void XmlToCSVSample()
        {
            using (var parser = new ChoXmlReader(FileNameSampleXML).WithXPath("Attributes/Attribute")
                .WithField("Name", xPath: "Name")
                .WithField("Value", xPath: "value")
                )
            {
                using (var writer = new ChoCSVWriter(FileNameXmlToCSVSampleActualCSV))
                    writer.Write(parser.Select(kvp => kvp.Value).ToExpandoObject());
                //Console.WriteLine(ChoCSVWriter.ToText(parser.Select(kvp => kvp.Value).ToExpandoObject()));
            }

            FileAssert.AreEqual(FileNameXmlToCSVSampleExpectedCSV, FileNameXmlToCSVSampleActualCSV);

            // Expected file not checked in because of not existent XPath, maybee a Exception-test

        }

        //[Test]
        public static void ToDataTable()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add("Id", typeof(Int32)).AllowDBNull = false;
            expected.Columns.Add("Name", typeof(string));
            expected.Columns.Add("IsActive", typeof(bool)).AllowDBNull = false;
            expected.Rows.Add(1, "Tom", true);
            expected.Rows.Add(2, "Mark", false);

            DataTable actual = null;

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlReader<EmployeeRec>(reader))
            {
                writer.WriteLine(EmpXml);

                writer.Flush();
                stream.Position = 0;

                actual = parser.AsDataTable();
            }

            DataTableAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void LoadTest()
        {
            Assert.Fail(@"File C:\temp\EPAXMLDownload1.xml not found.");

            DateTime st = DateTime.Now;
            Console.WriteLine("Starting..." + st);
            using (var r = new ChoXmlReader(@"C:\temp\EPAXMLDownload1.xml").NotifyAfter(10000).WithXPath("Document/FacilitySite"))
            {
                //r.Loop();
                foreach (var e in r.Take(10))
                {
                    Console.WriteLine(e.ToStringEx());
                }
            }
            Console.WriteLine("Completed." + (DateTime.Now - st));
            Console.ReadLine();
        }

        //[Test]
        public static void LoadTextTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"name","xxx"},{"author","Tom"},{"title","C++" } },
                new ChoDynamicObject{{"name","yyyy"}, { "author", null }, { "title", null } }
            };
            List<object> actual = new List<object>();

            foreach (var x in ChoXmlReader.LoadText(@"<books><book name=""xxx"" author=""Tom""><title>C++</title></book><book name=""yyyy""></book></books>"))
            {
                actual.Add(x);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void POCOTest()
        {
            List<object> expected = new List<object>
            {
                new EmployeeRec { Id = 1, IsActive = true, Name = "Tom"},
                new EmployeeRec { Id = 2, Name = "Mark"}
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlReader<EmployeeRec>(reader))
            {
                writer.WriteLine(EmpXml);

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
        public static void ConfigFirstDynamicTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {{"Id", (Int64)1}, { "Name", new ChoDynamicObject { { "isActive","true"},{ "#text", "Tom" } } } },
                new ChoDynamicObject {{"Id",(Int64)2}, { "Name", "Mark" } }
            };
            List<object> actual = new List<object>();

            ChoXmlRecordConfiguration config = new ChoXmlRecordConfiguration();
            config.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration("Id"));
            config.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration("Name"));

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlReader(reader, config))
            {
                writer.WriteLine(EmpXml);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    actual.Add(rec);
                }
            }

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void QuickTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {{"Id",1 }, { "Name", new ChoDynamicObject{ {"isActive", "true" },{ "#text", "Tom" } } } },
                new ChoDynamicObject {{"Id",2 }, { "Name", "Mark" } }
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlReader(reader))
            {
                writer.WriteLine(EmpXml);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    actual.Add(rec);
                }
            }
            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void CodeFirstTest()
        {
            List<object> expected = new List<object>
            {
                new EmployeeRecSimple{ Id = 1, Name = "Tom"},
                new EmployeeRecSimple{ Id = 2, Name = "Mark"}
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlReader<EmployeeRecSimple>(reader))
            {
                writer.WriteLine(EmpXml);

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    actual.Add(rec);
                }
            }

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        public static void QuickTestWithXmlNS()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"name","Tanmay Patilx"}},
                new ChoDynamicObject{{"name","Tanmay Patilx1"}},
                new ChoDynamicObject{{"name","Tanmay Patily"}},
                new ChoDynamicObject{{"name","Tanmay Patily1"}}
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlReader(reader).WithXPath("/cont:contacts/cont:contact/cont:contact1").WithXmlNamespace("cont", "www.tutorialspoint.com/profile").WithField("name", "cont:name"))
            {
                writer.WriteLine(@"<cont:contacts xmlns:cont=""www.tutorialspoint.com/profile"">
                <cont:contact >
                    <cont:contact1 >
                       <cont:name>Tanmay Patilx</cont:name>
                       <cont:company>TutorialsPoint</cont:company>
                       <cont:phone> (011) 123 - 4567 </cont:phone>
                    </cont:contact1> 
                    <cont:contact1 >
                       <cont:name>Tanmay Patilx1</cont:name>
                       <cont:company>TutorialsPoint</cont:company>
                       <cont:phone> (011) 123 - 4567 </cont:phone>
                    </cont:contact1> 
                   <cont:name>Tanmay Patil</cont:name>
                   <cont:company>TutorialsPoint</cont:company>
                   <cont:phone> (011) 123 - 4567 </cont:phone>
                </cont:contact> 
                <cont:contact >
                     <cont:contact1 >
                       <cont:name>Tanmay Patily</cont:name>
                       <cont:company>TutorialsPoint</cont:company>
                       <cont:phone> (011) 123 - 4567 </cont:phone>
                    </cont:contact1> 
                      <cont:contact1 >
                       <cont:name>Tanmay Patily1</cont:name>
                       <cont:company>TutorialsPoint</cont:company>
                       <cont:phone> (011) 123 - 4567 </cont:phone>
                    </cont:contact1> 
                 <cont:name>Tanmay Patil1</cont:name>
                   <cont:company>TutorialsPoint1</cont:company>
                   <cont:phone> (011) 123 - 45671 </cont:phone>
                </cont:contact> 
                </cont:contacts>
                ");

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

        public partial class EmployeeRecSimple
        {
            public int Id { get; set; }
            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                var simple = obj as EmployeeRecSimple;
                return simple != null &&
                       Id == simple.Id &&
                       Name == simple.Name;
            }

            public override int GetHashCode()
            {
                var hashCode = -1919740922;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                return hashCode;
            }
        }

        [XmlRoot(ElementName = "Employee")]
        public class EmployeeRec
        {
            [XmlAttribute]
            [ChoXmlNodeRecordField(XPath = "//@Id")]
            [Required]
            public int Id
            {
                get;
                set;
            }
            [ChoXmlNodeRecordField(XPath = "//Name")]
            [DefaultValue("XXXX")]
            public string Name
            {
                get;
                set;
            }
            [ChoXmlNodeRecordField(XPath = "//Name/@isActive")]
            public bool IsActive
            {
                get;
                set;
            }

            public override bool Equals(object obj)
            {
                var rec = obj as EmployeeRec;
                return rec != null &&
                       Id == rec.Id &&
                       Name == rec.Name &&
                       IsActive == rec.IsActive;
            }

            public override int GetHashCode()
            {
                var hashCode = -2060289483;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + IsActive.GetHashCode();
                return hashCode;
            }

            public override string ToString()
            {
                return "{0}. {1}. {2}".FormatString(Id, Name, IsActive);
            }
        }
    }
}
