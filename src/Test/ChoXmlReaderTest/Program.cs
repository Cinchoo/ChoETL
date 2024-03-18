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
using System.Runtime.Serialization;
using System.Linq.Expressions;

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

    //[XmlRoot(ElementName = "Project", Namespace = "http://schemas.microsoft.com/developer/msbuild/2003")]
    public class Project
    {
        [XmlAttribute(AttributeName = "ToolsVersion")]
        public string ToolsVersion1 { get; set; }
        //[XmlElement]
        public List<ImportClass> Import { get; set; }

        //[XmlElement(ElementName = "PropertyGroup")]
        public List<PropertyBlock> PropertyGroup { get; set; }

    }

    public class PropertyBlock
    {
        [XmlAttribute]
        public string Name { get; set; } = string.Empty;

        [XmlAttribute]
        public string Condition { get; set; } = string.Empty;

        [XmlElement]
        public string Platform { get; set; } = string.Empty; // one of AnyCPU, x86, x64

        [XmlElement]
        public string Platforms { get; set; } = string.Empty; // AnyCPU;x86;x64

        [XmlElement]
        public string PlatformTarget { get; set; } = string.Empty; // x86 or x64

        [XmlElement]
        public string OutputType { get; set; } = string.Empty;

        [XmlElement]
        public string TargetFramework { get; set; } = string.Empty; // net5.0-windows7.0, net48

        [XmlElement]
        public string TargetFrameworkVersion { get; set; } = string.Empty; // v4.8

        [XmlElement]
        public string TargetFrameworkProfile { get; set; } = string.Empty; // client

        [XmlElement]
        public string UseWindowsForms { get; set; } = string.Empty;

        [XmlElement]
        public string RuntimeIdentifier { get; set; } = string.Empty;

        [XmlElement]
        public string SelfContained { get; set; } = string.Empty;

        [XmlElement]
        public string PublishReadyToRun { get; set; } = string.Empty;

        [XmlElement]
        public string PublishDir { get; set; } = string.Empty;

        [XmlElement]
        public string IsPackable { get; set; } = string.Empty;

        [XmlElement]
        public string NoWarn { get; set; } = string.Empty;

        [XmlElement]
        public string StartupObject { get; set; } = string.Empty;

        //public string PreBuildEvent { get; set; }= string.Empty;

        //
        // From HsDragon NET Framework 4.8 project file
        //
        [XmlElement]
        public string AppDesignerFolder { get; set; } = string.Empty;

        [XmlElement]
        public string RootNamespace { get; set; } = string.Empty;

        [XmlElement]
        public string AssemblyName { get; set; } = string.Empty;

        [XmlElement]
        public string FileAlignment { get; set; } = string.Empty;

        [XmlElement]
        public string Deterministic { get; set; } = string.Empty;

        [XmlElement]
        public string DebugSymbols { get; set; } = string.Empty; // true

        [XmlElement]
        public string DebugType { get; set; } = string.Empty; // pdbonly

        [XmlElement]
        public string Optimize { get; set; } = string.Empty; // true

        [XmlElement]
        public string OutputPath { get; set; } = string.Empty; // \bin\x86\Debug

        [XmlElement]
        public string DefineConstants { get; set; } = string.Empty; // DEBUG;TRACE

        [XmlElement]
        public string ErrorReport { get; set; } = string.Empty; // prompt

        [XmlElement]
        public string WarningLevel { get; set; } = string.Empty; // 4

        [XmlElement]
        public string RegisterForComInterop { get; set; } = string.Empty; // false

        [XmlElement]
        public string Prefer32Bit { get; set; } = string.Empty; // false

        [XmlElement]
        public string SignAssembly { get; set; } = string.Empty; // false

        [XmlElement]
        public string LangVersioin { get; set; } = string.Empty; // 7.3

        // From HsDragon NET Framework 4.8 project file
        //
        //<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ">
        //<DebugSymbols>true</DebugSymbols>
        //<DebugType>full</DebugType>
        //<Optimize>false</Optimize>
        //<OutputPath>bin\Debug\</OutputPath>
        //<DefineConstants>DEBUG;TRACE</DefineConstants>
        //<ErrorReport>prompt</ErrorReport>
        //<WarningLevel>4</WarningLevel>
        //<RegisterForComInterop>false</RegisterForComInterop>
        //<Prefer32Bit>false</Prefer32Bit>
        //</PropertyGroup>
        //<PropertyGroup>
        //<SignAssembly>false</SignAssembly>
        //</PropertyGroup>
        //
        // <ItemGroup>
        //  <Reference Include="System" />
        //  <Reference Include="System.Core" />
        //  <Reference Include="System.Windows.Forms" />
        //  <Reference Include="System.Xml.Linq" />
        //</ItemGroup>
        //<ItemGroup>
        //  <Compile Include="HsComUDP.cs" />
        //  <Compile Include="Properties\AssemblyInfo.cs" />
        //  <Compile Include="Utils.cs" />
        //</ItemGroup>
        //<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />
    }
    public class ImportClass
    {
        [XmlAttribute]
        public string Project { get; set; } = string.Empty;
    }

    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    public class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            //XmlArray2JSON();

            var x1 = Activator.CreateInstance(typeof(DateTime));

            var configuration = new ChoXmlRecordConfiguration { ErrorMode = ChoErrorMode.ThrowAndStop };
            //throw new Exception("Debugger catches this");
            var x = new ChoXmlReader<MyObject>("XmlFile5.xml", configuration);
            throw new Exception("Uncaught by debugger, shows \"An application error occurred\" in debugger console.");

            return;

            LoadProducts();
            return;

            FlattenKeyValue2DataTable();
            return;

            SOAPXmlToJSON();
            return;

            LoadConfigItems();
            return;
            //Xml2JSON1();
            //LoadXmlUsingConfigAndPOCO();
            //DesrializeUsingProxy();
        }

        [Test]
        public void SOAPMessageToDataTableTest()
        {
            string soapXml = @"<soap:Envelope xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
<soap:Header>
    <correlationId xmlns:ns4=""http://www.abc.de/abc_v4""
                   xmlns:ns3=""http://www.abc.de/abc_v3""
                   xmlns:ns2=""http://www.abc.de/abc_v2""
                   xmlns=""http://www.abc.de/bone_v1"">100</correlationId>
</soap:Header>
<soap:Body>
    <searchResponse xmlns=""http://www.abc.de/abc_v1""
                    xmlns:ns2=""http://www.abc.de/abc_v2""
                    xmlns:ns3=""http://www.abc.de/abc_v3""
                    xmlns:ns4=""http://www.abc.de/abc_v4"">
        <candidate>
            <identifier>
                <type>VAT</type>
                <value>DE123437641</value>
            </identifier>
            <identifier>
                <type>ONR</type>
                <value>19276000</value>
            </identifier>
            <identifier>
                <type>TAX_ID</type>
                <value>5333044444444</value>
            </identifier>
            <registry>
                <type>HRB</type>
                <number>1268</number>
            </registry>
            <hitType>COMPANY</hitType>
            <name>Möbelhaus Peter Neumann - Negativ GmbH - Basisdaten Nachtragstest</name>
            <location>
                <street>Friedlandstr.</street>
                <house>2</house>
                <city>Aachen</city>
                <zip>52064</zip>
                <country>
                    <code>DEU</code>
                    <text>Deutschland</text>
                </country>
            </location>
            <unitType>HEADOFFICE</unitType>
            <status>active</status>
            <similarity>100</similarity>
        </candidate>
        <candidate>
            <identifier>
                <type>VAT</type>
                <value>DE666814999</value>
            </identifier>
            <identifier>
                <type>ONR</type>
                <value>26120001</value>
            </identifier>
            <identifier>
                <type>TAX_ID</type>
                <value>5333044444444</value>
            </identifier>
            <registry>
                <type>HRB</type>
                <number>1234</number>
                <city>Hamburg</city>
            </registry>
            <hitType>COMPANY</hitType>
            <name>Möbelhaus Peter Neumann GmbH</name>
            <location>
                <street>Seewartenstr.</street>
                <house>9</house>
                <city>Hamburg</city>
                <zip>20459</zip>
                <country>
                    <code>DEU</code>
                    <text>Deutschland</text>
                </country>
            </location>
            <unitType>HEADOFFICE</unitType>
            <status>active</status>
            <similarity>100</similarity>
        </candidate>
    </searchResponse>
</soap:Body>
</soap:Envelope>";

            string expected = @"identifiers_0_identifier_type,identifiers_0_identifier_value,identifiers_1_identifier_type,identifiers_1_identifier_value,identifiers_2_identifier_type,identifiers_2_identifier_value,registry_type,registry_number,hitType,name,location_street,location_house,location_city,location_zip,location_country_code,location_country_text,unitType,status,similarity
VAT,DE123437641,ONR,19276000,TAX_ID,5333044444444,HRB,1268,COMPANY,Möbelhaus Peter Neumann - Negativ GmbH - Basisdaten Nachtragstest,Friedlandstr.,2,Aachen,52064,DEU,Deutschland,HEADOFFICE,active,100
VAT,DE666814999,ONR,26120001,TAX_ID,5333044444444,HRB,1234,COMPANY,Möbelhaus Peter Neumann GmbH,Seewartenstr.,9,Hamburg,20459,DEU,Deutschland,HEADOFFICE,active,100";

            using (var r = ChoXmlReader.LoadText(soapXml)
                .WithXPath("//candidate")
                .WithXmlNamespace("", "http://www.abc.de/abc_v1")
                .Configure(c => c.IgnoreRootDictionaryFieldPrefix = true)
                )
            {
                var actual = r.AsDataTable().Dump();
                Assert.AreEqual(expected, actual);
            }

        }
        [Test]
        public static void Issue100()
        {
            string xml = @"<Root xmlns:c=""Ala ma kota"">

<!-- ... -->

        <c:Histogram>
            <Data Width=""10"" Height=""20"" />
        </c:Histogram>

<!-- ... -->

</Root>";

            string expected = @"[
  {
    ""Data"": {
      ""Width"": 10,
      ""Height"": 20
    }
  }
]";
            using (var r = ChoXmlReader<Histogram>.LoadText(xml)
                .WithXPath("c:Histogram")
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }

        [Test]
        public static void Issue100_1()
        {
            string xml = @"<Root xmlns:c=""Ala ma kota"">

<!-- ... -->

        <c:Histogram>
            <c:Data Width=""10"" Height=""20"" />
        </c:Histogram>

<!-- ... -->

</Root>";

            string expected = @"[
  {
    ""Data"": {
      ""Width"": 10,
      ""Height"": 20
    }
  }
]";
            using (var r = ChoXmlReader<Histogram>.LoadText(xml)
                .WithXPath("c:Histogram")
                )
            {
                var actual = JsonConvert.SerializeObject(r, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }

        public class Histogram
        {
            public Data Data { get; set; }
        }

        public class Data
        {
            [XmlAttribute]
            public int Width { get; set; }
            [XmlAttribute]
            public int Height { get; set; }
        }

        [Test]
        public static void XmlArray2JSON()
        {
            string xml = @"<Drivers>
  <Driver>
    <Name>MyName</Name>
  </Driver>
</Drivers>";

            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("//Driver")
                )
            {
                using (var w = new ChoJSONWriter(Console.Out)
                    .Configure(c => c.RootName = "Drivers")
                    )
                {
                    w.Write(r);
                }
            }
        }
        [Test]
        public static void GPOPolicyLoad()
        {
            using (var r = new ChoXmlReader("GPOPolicy.xml")
                .WithXPath("//GPO")
                .WithXmlNamespace("g", "http://www.microsoft.com/GroupPolicy/Settings")
                .WithXmlNamespace("t", "http://www.microsoft.com/GroupPolicy/Types")
                .WithXmlNamespace("s", "http://www.microsoft.com/GroupPolicy/Types/Security")
                .WithXmlNamespace("q1", "http://www.microsoft.com/GroupPolicy/Settings/Security")
                .WithXmlNamespace("q2", "http://www.microsoft.com/GroupPolicy/Settings/Auditing")
                .WithXmlNamespace("q3", "http://www.microsoft.com/GroupPolicy/Settings/Registry")
                .Configure(c => c.UseXmlArray = false)
                //.WithField("Identifier", xPath: "/g:Identifier/t:Identifier")
                //.WithField("Domain", xPath: "/g:Identifier/t:Domain")
                //.WithField("Name", xPath: "g:Name")
                //.ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Configure(c => c.IgnoreNSPrefix = true)
                )
            {
                //r.Print();
                //return;
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                var expected = File.ReadAllText("GPOPolicy.json");

                Assert.AreEqual(expected, actual);

                StringBuilder json = new StringBuilder();
                using (var w = new ChoJSONWriter(json))
                {
                    w.Write(recs);
                }

                string expectedJson = expected;
                var actualJson = json.ToString();
                Assert.AreEqual(expectedJson, actualJson);
            }
        }
        [Test]
        public static void GPOPolicyLoadWithNS()
        {
            using (var r = new ChoXmlReader("GPOPolicy.xml")
                .WithXPath("//GPO")
                .WithXmlNamespace("g", "http://www.microsoft.com/GroupPolicy/Settings")
                .WithXmlNamespace("t", "http://www.microsoft.com/GroupPolicy/Types")
                .WithXmlNamespace("s", "http://www.microsoft.com/GroupPolicy/Types/Security")
                .WithXmlNamespace("q1", "http://www.microsoft.com/GroupPolicy/Settings/Security")
                .WithXmlNamespace("q2", "http://www.microsoft.com/GroupPolicy/Settings/Auditing")
                .WithXmlNamespace("q3", "http://www.microsoft.com/GroupPolicy/Settings/Registry")
                .Configure(c => c.UseXmlArray = false)
                //.WithField("Identifier", xPath: "/g:Identifier/t:Identifier")
                //.WithField("Domain", xPath: "/g:Identifier/t:Domain")
                //.WithField("Name", xPath: "g:Name")
                //.ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Configure(c => c.IgnoreNSPrefix = false)
                )
            {
                //r.Print();
                //return;
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                var expected = File.ReadAllText("GPOPolicyNS.json");

                Assert.AreEqual(expected, actual);

                StringBuilder json = new StringBuilder();
                using (var w = new ChoJSONWriter(json)
                    .Configure(c => c.KeepNSPrefix = true)
                    )
                {
                    w.Write(recs);
                }

                string expectedJson = expected;
                var actualJson = json.ToString();
                Assert.AreEqual(expectedJson, actualJson);
            }
        }

        public class Product
        {
            public int ProductCode { get; set; }
            [ChoXPath("Properties/@no")]
            public int PropertyNo { get; set; }
            [ChoXPath("Properties/ColorProperties/Color")]
            public string Color { get; set; }
        }
        [Test]
        public static void LoadProducts()
        {
            string xml = @"<Root>
 <Products> 
    <Product>
      <ProductCode>1</ProductCode>
      <Properties no=""45"">
        <ColorProperties>
           <Color>Blue</Color>
        </ColorProperties>
      </Properties>
    </Product>
    <Product>
      <ProductCode>2</ProductCode>
      <Properties no=""45"">
        <ColorProperties>
           <Color>Red</Color>
        </ColorProperties>
      </Properties>
    </Product>
     <Product>
      <ProductCode>3</ProductCode>
      <Properties no=""45"">
        <ColorProperties>
           <Color>Yellow</Color>
        </ColorProperties>
      </Properties>
    </Product>
 </Products>
</Root>";

            string expected = @"[
  {
    ""ProductCode"": 1,
    ""PropertyNo"": 45,
    ""Color"": ""Blue""
  },
  {
    ""ProductCode"": 2,
    ""PropertyNo"": 45,
    ""Color"": ""Red""
  },
  {
    ""ProductCode"": 3,
    ""PropertyNo"": 45,
    ""Color"": ""Yellow""
  }
]";
            using (var r = ChoXmlReader<Product>.LoadText(xml)
                   .WithXPath("//Product")
                  )
            {
                var recs = r.ToArray();
                //r.Print();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Record
        {
            [ChoXmlElementRecordField(FieldName = "field_name")]
            public int Field { get; set; }
        }
        [Test]
        public static void Issue195()
        {
            string expected = @"<root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <data>
    <rec>
      <b1a>
        <field_name>1</field_name>
      </b1a>
    </rec>
  </data>
</root>";

            var record = new Record { Field = 1 };
            var sb = new StringBuilder();
            using (var writer = new ChoXmlWriter<Record>(sb).WithXPath("/root/data/rec/b1a"))
            {
                writer.Write(record);
            }
            Console.WriteLine(sb);
            var actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void FlattenKeyValue2DataTable()
        {
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>

<soapenv:Envelope xmlns:fnx1=""http://www"" xmlns:rob=""http://"" xmlns:x=""http://www"" xmlns:soapenv=""http:/"" xmlns:msxsl=""urn:schemas-microsoft-com:xslt"">
<soapenv:Header/>
<soapenv:Body>
    <x:RequestResponseServiceResponse>
        <rob:RobotGeneralDBQueryOut>
            <QueryResult>
                <row>
                    <column>
                        <name>KOD_ZEHUT</name>
                        <value>f</value>
                    </column>
                    <column>
                        <name>MIS_ZEHUT</name>
                        <value></value>
                    </column>
                    <column>
                        <name>SUG_HAFRASHA</name>
                        <value>1</value>
                    </column>
                </row>
                <row>
                    <column>
                        <name>KOD_ZEHUT</name>
                        <value>f</value>
                    </column>
                    <column>
                        <name>MIS_ZEHUT</name>
                        <value>5432</value>
                    </column>
                    <column>
                        <name>SUG_HAFRASHA</name>
                        <value>2</value>
                    </column>
                </row>
            </QueryResult>
        </rob:RobotGeneralDBQueryOut>
        <esb:ESBServiceResponseMetadata xmlns:esb=""http://www"">
            <esb:ResponseStatus>Success</esb:ResponseStatus>
            <esb:ResponseCode>0</esb:ResponseCode>
            <esb:ResponseDescription/>
            <esb:InstanceWFID>702167729</esb:InstanceWFID>
        </esb:ESBServiceResponseMetadata>
    </x:RequestResponseServiceResponse>
</soapenv:Body>
</soapenv:Envelope>";

            string expected = @"[
  {
    ""KOD_ZEHUT"": ""f"",
    ""MIS_ZEHUT"": null,
    ""SUG_HAFRASHA"": ""1""
  },
  {
    ""KOD_ZEHUT"": ""f"",
    ""MIS_ZEHUT"": ""5432"",
    ""SUG_HAFRASHA"": ""2""
  }
]";
            using (var r = ChoXmlReader.LoadText(xml)
                   .WithXPath("//row")
                  .WithField("name", xPath: "column/name")
                  .WithField("value", xPath: "column/value")
                  )
            {
                //r.Select(r1 => ChoUtility.ToDictionary(r1.name as IList, r1.value as IList)).AsDataTable().Print();
                //return;
                var dt = r.ToArray().Pivot().AsDataTable();
                dt.Print();

                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void SOAPXmlToJSON()
        {
            string xml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<SOAP-ENV:Envelope>
   <SOAP-ENV:Body>
      <Results>
         <Summary>
            <Status xsi:type=""xsd:boolean"">true</Status>
            <etc xsi:type=""xsd:string"">etc</etc>
         </Summary>
      </Results>
   </SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

            string expected = @"{
  ""Summary"": {
    ""Status"": {
      ""type"": ""xsd:boolean"",
      ""#text"": ""true""
    },
    ""etc"": {
      ""type"": ""xsd:string"",
      ""#text"": ""etc""
    }
  }
}";

            StringBuilder json = new StringBuilder();
            using (var r = ChoXmlReader.LoadText(xml).WithXPath("//Summary")
                .WithXmlNamespace("SOAP-ENV", "")
                .WithXmlNamespace("xsi", "")
                )
            {
                using (var w = new ChoJSONWriter(json)
                    .SupportMultipleContent()
                    )
                    w.Write(r);
            }

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class ConfigItem
        {
            public string Spoken { get; set; }
            public string Description { get; set; }
            [ChoXPath("/Folders/Folder")]
            [ChoUseXmlProxy]
            public Folder[] Folders { get; set; }
        }

        public class Folder
        {
            public string Network { get; set; }
            public string Location { get; set; }
        }

        [Test]
        public static void LoadConfigItems()
        {
            string xml = @"<ConfigItems>
  <ConfigItem>
    <Spoken> data </Spoken>
    <Description> folders holding system data files </Description>
    <Folders>
      <Folder>
        <Network>Local</Network>
        <Location>C:\users\kkkwj\documents\highspeed\user\data cpu-ufo</Location>
      </Folder>
      <Folder>
        <Network>WAN</Network>
        <Location>C:\users\kkkwj\documents\highspeed\user\data general</Location>
      </Folder>
    </Folders>
  </ConfigItem>
</ConfigItems>";

            string expected = @"[
  {
    ""Spoken"": ""data"",
    ""Description"": ""folders holding system data files"",
    ""Folders_Value0_Network"": ""Local"",
    ""Folders_Value0_Location"": ""C:\\users\\kkkwj\\documents\\highspeed\\user\\data cpu-ufo"",
    ""Folders_Value1_Network"": ""WAN"",
    ""Folders_Value1_Location"": ""C:\\users\\kkkwj\\documents\\highspeed\\user\\data general""
  }
]";
            using (var r = ChoXmlReader<ConfigItem>.LoadText(xml)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                var recs = r.AsDataTable();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }

        [Test]
        public static void DesrializeUsingProxy()
        {
            string expected = @"{
  ""PropertyValue"": ""Value""
}";

            // get the xml value somehow
            var xdoc = XDocument.Parse(@"<Class><Property>Value</Property></Class>");

            var cf = new ChoXmlRecordConfiguration();
            var cf1 = cf.MapRecordFieldsForType<Class>();
            cf1.Map(f => f.PropertyValue, fieldName: "Property");
            ChoXmlSerializerProxy.AddRecordConfiguration(cf1);

            // deserialize the xml into the proxy type
            //XmlSerializer xmlSerializer = new XmlSerializer(typeof(ChoXmlSerializerProxy<Class>), ChoNullNSXmlSerializerFactory.GetXmlOverrides(typeof(TInstanceType)));
            var xmlSerializer = ChoNullNSXmlSerializerFactory.GetXmlSerializer<ChoXmlSerializerProxy<Class>, Class>();
            using (XmlReader reader = xdoc.CreateReader())
            {
                var obj = xmlSerializer.Deserialize(reader);
                var proxy = obj as ChoXmlSerializerProxy<Class>;
                var value = proxy.Value;
                value.Print();

                var actual = JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        //[XmlRoot("Class")]
        public sealed class Class
        {
            //[ChoXmlNodeRecordField(FieldName = "Property")]
            public string PropertyValue { get; set; }
        }


        [Test]
        public static void LoadVSProjectFile()
        {
            IDictionary<string, string> ns = null;

            string xml = "VSProject.xml";
            //using (var r1 = new ChoXmlReader(xml).WithXPath("//")
            //      )
            //{
            //    var rec = r1.FirstOrDefault();
            //    //rec.Print();
            //    ns = r1.Configuration.GetXmlNamespacesInScope();
            //}

            //ns.Print();

            string expected = @"[
  {
    ""ToolsVersion1"": ""15.0"",
    ""Import"": [
      {
        ""Project"": ""$(MSBuildExtensionsPath)\\$(MSBuildToolsVersion)\\Microsoft.Common.props""
      },
      {
        ""Project"": ""$(MSBuildToolsPath)\\Microsoft.CSharp.targets""
      }
    ],
    ""PropertyGroup"": [
      {
        ""Name"": """",
        ""Condition"": """",
        ""Platform"": ""AnyCPU"",
        ""Platforms"": """",
        ""PlatformTarget"": """",
        ""OutputType"": ""Library"",
        ""TargetFramework"": """",
        ""TargetFrameworkVersion"": ""v4.8"",
        ""TargetFrameworkProfile"": """",
        ""UseWindowsForms"": """",
        ""RuntimeIdentifier"": """",
        ""SelfContained"": """",
        ""PublishReadyToRun"": """",
        ""PublishDir"": """",
        ""IsPackable"": """",
        ""NoWarn"": """",
        ""StartupObject"": """",
        ""AppDesignerFolder"": ""Properties"",
        ""RootNamespace"": ""HsDragon"",
        ""AssemblyName"": ""HsDragon"",
        ""FileAlignment"": ""512"",
        ""Deterministic"": ""true"",
        ""DebugSymbols"": """",
        ""DebugType"": """",
        ""Optimize"": """",
        ""OutputPath"": """",
        ""DefineConstants"": """",
        ""ErrorReport"": """",
        ""WarningLevel"": """",
        ""RegisterForComInterop"": """",
        ""Prefer32Bit"": """",
        ""SignAssembly"": """",
        ""LangVersioin"": """"
      },
      {
        ""Name"": """",
        ""Condition"": "" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "",
        ""Platform"": """",
        ""Platforms"": """",
        ""PlatformTarget"": """",
        ""OutputType"": """",
        ""TargetFramework"": """",
        ""TargetFrameworkVersion"": """",
        ""TargetFrameworkProfile"": """",
        ""UseWindowsForms"": """",
        ""RuntimeIdentifier"": """",
        ""SelfContained"": """",
        ""PublishReadyToRun"": """",
        ""PublishDir"": """",
        ""IsPackable"": """",
        ""NoWarn"": """",
        ""StartupObject"": """",
        ""AppDesignerFolder"": """",
        ""RootNamespace"": """",
        ""AssemblyName"": """",
        ""FileAlignment"": """",
        ""Deterministic"": """",
        ""DebugSymbols"": ""true"",
        ""DebugType"": ""full"",
        ""Optimize"": ""false"",
        ""OutputPath"": ""bin\\Debug\\"",
        ""DefineConstants"": ""DEBUG;TRACE"",
        ""ErrorReport"": ""prompt"",
        ""WarningLevel"": ""4"",
        ""RegisterForComInterop"": ""false"",
        ""Prefer32Bit"": ""false"",
        ""SignAssembly"": """",
        ""LangVersioin"": """"
      },
      {
        ""Name"": """",
        ""Condition"": "" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "",
        ""Platform"": """",
        ""Platforms"": """",
        ""PlatformTarget"": """",
        ""OutputType"": """",
        ""TargetFramework"": """",
        ""TargetFrameworkVersion"": """",
        ""TargetFrameworkProfile"": """",
        ""UseWindowsForms"": """",
        ""RuntimeIdentifier"": """",
        ""SelfContained"": """",
        ""PublishReadyToRun"": """",
        ""PublishDir"": """",
        ""IsPackable"": """",
        ""NoWarn"": """",
        ""StartupObject"": """",
        ""AppDesignerFolder"": """",
        ""RootNamespace"": """",
        ""AssemblyName"": """",
        ""FileAlignment"": """",
        ""Deterministic"": """",
        ""DebugSymbols"": """",
        ""DebugType"": ""pdbonly"",
        ""Optimize"": ""true"",
        ""OutputPath"": ""bin\\Release\\"",
        ""DefineConstants"": ""TRACE"",
        ""ErrorReport"": ""prompt"",
        ""WarningLevel"": ""4"",
        ""RegisterForComInterop"": """",
        ""Prefer32Bit"": ""false"",
        ""SignAssembly"": """",
        ""LangVersioin"": """"
      },
      {
        ""Name"": """",
        ""Condition"": """",
        ""Platform"": """",
        ""Platforms"": """",
        ""PlatformTarget"": """",
        ""OutputType"": """",
        ""TargetFramework"": """",
        ""TargetFrameworkVersion"": """",
        ""TargetFrameworkProfile"": """",
        ""UseWindowsForms"": """",
        ""RuntimeIdentifier"": """",
        ""SelfContained"": """",
        ""PublishReadyToRun"": """",
        ""PublishDir"": """",
        ""IsPackable"": """",
        ""NoWarn"": """",
        ""StartupObject"": """",
        ""AppDesignerFolder"": """",
        ""RootNamespace"": """",
        ""AssemblyName"": """",
        ""FileAlignment"": """",
        ""Deterministic"": """",
        ""DebugSymbols"": """",
        ""DebugType"": """",
        ""Optimize"": """",
        ""OutputPath"": """",
        ""DefineConstants"": """",
        ""ErrorReport"": """",
        ""WarningLevel"": """",
        ""RegisterForComInterop"": """",
        ""Prefer32Bit"": """",
        ""SignAssembly"": ""false"",
        ""LangVersioin"": """"
      },
      {
        ""Name"": """",
        ""Condition"": ""'$(Configuration)|$(Platform)' == 'Debug|x86'"",
        ""Platform"": """",
        ""Platforms"": """",
        ""PlatformTarget"": ""x86"",
        ""OutputType"": """",
        ""TargetFramework"": """",
        ""TargetFrameworkVersion"": """",
        ""TargetFrameworkProfile"": """",
        ""UseWindowsForms"": """",
        ""RuntimeIdentifier"": """",
        ""SelfContained"": """",
        ""PublishReadyToRun"": """",
        ""PublishDir"": """",
        ""IsPackable"": """",
        ""NoWarn"": """",
        ""StartupObject"": """",
        ""AppDesignerFolder"": """",
        ""RootNamespace"": """",
        ""AssemblyName"": """",
        ""FileAlignment"": """",
        ""Deterministic"": """",
        ""DebugSymbols"": ""true"",
        ""DebugType"": ""full"",
        ""Optimize"": """",
        ""OutputPath"": ""bin\\x86\\Debug\\"",
        ""DefineConstants"": ""DEBUG;TRACE"",
        ""ErrorReport"": ""prompt"",
        ""WarningLevel"": """",
        ""RegisterForComInterop"": """",
        ""Prefer32Bit"": """",
        ""SignAssembly"": """",
        ""LangVersioin"": """"
      },
      {
        ""Name"": """",
        ""Condition"": ""'$(Configuration)|$(Platform)' == 'Release|x86'"",
        ""Platform"": """",
        ""Platforms"": """",
        ""PlatformTarget"": ""x86"",
        ""OutputType"": """",
        ""TargetFramework"": """",
        ""TargetFrameworkVersion"": """",
        ""TargetFrameworkProfile"": """",
        ""UseWindowsForms"": """",
        ""RuntimeIdentifier"": """",
        ""SelfContained"": """",
        ""PublishReadyToRun"": """",
        ""PublishDir"": """",
        ""IsPackable"": """",
        ""NoWarn"": """",
        ""StartupObject"": """",
        ""AppDesignerFolder"": """",
        ""RootNamespace"": """",
        ""AssemblyName"": """",
        ""FileAlignment"": """",
        ""Deterministic"": """",
        ""DebugSymbols"": """",
        ""DebugType"": ""pdbonly"",
        ""Optimize"": ""true"",
        ""OutputPath"": ""bin\\x86\\Release\\"",
        ""DefineConstants"": ""TRACE"",
        ""ErrorReport"": ""prompt"",
        ""WarningLevel"": """",
        ""RegisterForComInterop"": """",
        ""Prefer32Bit"": """",
        ""SignAssembly"": """",
        ""LangVersioin"": """"
      },
      {
        ""Name"": """",
        ""Condition"": ""'$(Configuration)|$(Platform)' == 'Debug|x64'"",
        ""Platform"": """",
        ""Platforms"": """",
        ""PlatformTarget"": ""x64"",
        ""OutputType"": """",
        ""TargetFramework"": """",
        ""TargetFrameworkVersion"": """",
        ""TargetFrameworkProfile"": """",
        ""UseWindowsForms"": """",
        ""RuntimeIdentifier"": """",
        ""SelfContained"": """",
        ""PublishReadyToRun"": """",
        ""PublishDir"": """",
        ""IsPackable"": """",
        ""NoWarn"": """",
        ""StartupObject"": """",
        ""AppDesignerFolder"": """",
        ""RootNamespace"": """",
        ""AssemblyName"": """",
        ""FileAlignment"": """",
        ""Deterministic"": """",
        ""DebugSymbols"": ""true"",
        ""DebugType"": ""full"",
        ""Optimize"": """",
        ""OutputPath"": ""bin\\x64\\Debug\\"",
        ""DefineConstants"": ""DEBUG;TRACE"",
        ""ErrorReport"": ""prompt"",
        ""WarningLevel"": """",
        ""RegisterForComInterop"": """",
        ""Prefer32Bit"": """",
        ""SignAssembly"": """",
        ""LangVersioin"": """"
      },
      {
        ""Name"": """",
        ""Condition"": ""'$(Configuration)|$(Platform)' == 'Release|x64'"",
        ""Platform"": """",
        ""Platforms"": """",
        ""PlatformTarget"": ""x64"",
        ""OutputType"": """",
        ""TargetFramework"": """",
        ""TargetFrameworkVersion"": """",
        ""TargetFrameworkProfile"": """",
        ""UseWindowsForms"": """",
        ""RuntimeIdentifier"": """",
        ""SelfContained"": """",
        ""PublishReadyToRun"": """",
        ""PublishDir"": """",
        ""IsPackable"": """",
        ""NoWarn"": """",
        ""StartupObject"": """",
        ""AppDesignerFolder"": """",
        ""RootNamespace"": """",
        ""AssemblyName"": """",
        ""FileAlignment"": """",
        ""Deterministic"": """",
        ""DebugSymbols"": """",
        ""DebugType"": ""pdbonly"",
        ""Optimize"": ""true"",
        ""OutputPath"": ""bin\\x64\\Release\\"",
        ""DefineConstants"": ""TRACE"",
        ""ErrorReport"": ""prompt"",
        ""WarningLevel"": """",
        ""RegisterForComInterop"": """",
        ""Prefer32Bit"": """",
        ""SignAssembly"": """",
        ""LangVersioin"": """"
      },
      {
        ""Name"": """",
        ""Condition"": """",
        ""Platform"": """",
        ""Platforms"": """",
        ""PlatformTarget"": """",
        ""OutputType"": """",
        ""TargetFramework"": """",
        ""TargetFrameworkVersion"": """",
        ""TargetFrameworkProfile"": """",
        ""UseWindowsForms"": """",
        ""RuntimeIdentifier"": """",
        ""SelfContained"": """",
        ""PublishReadyToRun"": """",
        ""PublishDir"": """",
        ""IsPackable"": """",
        ""NoWarn"": """",
        ""StartupObject"": """",
        ""AppDesignerFolder"": """",
        ""RootNamespace"": """",
        ""AssemblyName"": """",
        ""FileAlignment"": """",
        ""Deterministic"": """",
        ""DebugSymbols"": """",
        ""DebugType"": """",
        ""Optimize"": """",
        ""OutputPath"": """",
        ""DefineConstants"": """",
        ""ErrorReport"": """",
        ""WarningLevel"": """",
        ""RegisterForComInterop"": """",
        ""Prefer32Bit"": """",
        ""SignAssembly"": """",
        ""LangVersioin"": """"
      }
    ]
  }
]";
            using (var r = new ChoXmlReader<Project>(xml)
                   .WithXPath("//")
                   .WithXmlNamespace("http://schemas.microsoft.com/developer/msbuild/2003")
                   .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                  //.UseXmlSerialization()
                  )
            {
                var recs = r.ToArray();
                //r.Print();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void LoadSelectiveNode()
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-16""?>
<cincinnati xmlns=""http://www.sesame-street.com/abc/def/1"">
  <cincinnatiChild xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">
    <ElementValue xmlns:a=""http://schemas.data.org/2004/07/sesame-street.abc.def.ghi"">
      <a:someField>false</a:someField>
      <a:data xmlns:b=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
        <b:KeyValueThing>
          <b:Key>key1</b:Key>
          <b:Value i:type=""a:ArrayOfPeople"">
            <a:Person>
              <a:firstField>
              </a:firstField>
              <a:dictionary>
                <b:KeyValueThing>
                  <b:Key>ID</b:Key>
                  <b:Value i:type=""c:long"" xmlns:c=""http://www.w3.org/2001/XMLSchema"">000101</b:Value>
                </b:KeyValueThing>
                <b:KeyValueThing>
                  <b:Key>Name</b:Key>
                  <b:Value i:type=""c:string"" xmlns:c=""http://www.w3.org/2001/XMLSchema"">John</b:Value>
                </b:KeyValueThing>
              </a:dictionary>
            </a:Person>
            <a:Person>
              <a:firstField>
              </a:firstField>
              <a:dictionary>
                <b:KeyValueThing>
                  <b:Key>ID</b:Key>
                  <b:Value i:type=""c:long"" xmlns:c=""http://www.w3.org/2001/XMLSchema"">000102</b:Value>
                </b:KeyValueThing>
                <b:KeyValueThing>
                  <b:Key>Name</b:Key>
                  <b:Value i:type=""c:string"" xmlns:c=""http://www.w3.org/2001/XMLSchema"">John</b:Value>
                </b:KeyValueThing>
              </a:dictionary>
            </a:Person>
          </b:Value>
        </b:KeyValueThing>
      </a:data>
    </ElementValue>
  </cincinnatiChild>
</cincinnati>";

            string expected = @"[
  {
    ""Value"": ""000101""
  },
  {
    ""Value"": ""John""
  },
  {
    ""Value"": ""000102""
  },
  {
    ""Value"": ""John""
  }
]";
            using (var r = ChoXmlReader.LoadText(xml).WithXPath("//b:Value/b:Value")
                .WithField("Value", xPath: "text()")
                .WithXmlNamespace("b", "http://schemas.microsoft.com/2003/10/Serialization/Arrays")
                .Configure(c => c.IgnoreNSPrefix = true)
                )
            {
                //r.Print(); //.Where(r1 => r1.Key == "ID").Select(r2 => r2.Value).Print();
                var recs = r.ToArray();
                //r.Print();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void LoadXmlUsingConfigAndPOCO()
        {
            string xml = @"<Employees>
    <Employee Id='1'>
        <Name>Tom</Name>
    </Employee>
    <Employee Id='2'>
        <Name>Mark</Name>
    </Employee>
</Employees>";

            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Tom""
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark""
  }
]";
            using (var r = ChoXmlReader<EmployeeRecX>.LoadText(xml))
            {
                var recs = r.ToArray();
                //r.Print();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        //[ChoXmlRecordObject( IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Null)]
        public class EmployeeRecX
        {
            //[ChoXmlNodeRecordField(XPath = "//@Id")]
            [ChoXPath("@Id")]
            [Required]
            public int? Id
            {
                get;
                set;
            }
            //[ChoXmlNodeRecordField(XPath = "//Name1")]
            [ChoXPath("Name")]
            [DefaultValue("XXXX")]
            public string Name
            {
                get;
                set;
            }

            public override string ToString()
            {
                return "{0}. {1}".FormatString(Id, Name);
            }
        }
        [Test]
        public static void NestedClassTest()
        {
            string xml = @"<Specifier>
  <CollectionBlock>
     <Type>foo</Type>
     <Name>my name</Name>
  </CollectionBlock>
  <ProductBlock>
     <Type>type here</Type>
     <Name>Block One</Name>
     <Description>some text</Description>
  </ProductBlock>
  <ProductBlock>
     <Type>type here</Type>
     <Name>Block Two</Name>
     <Description>some text</Description>
  </ProductBlock>
</Specifier>";

            string expected = @"[
  {
    ""CollectionBlock_Type"": ""foo"",
    ""CollectionBlock_Name"": ""my name"",
    ""CollectionBlock_Description"": null,
    ""ProductBlock_Value0_Type"": ""type here"",
    ""ProductBlock_Value0_Name"": ""Block One"",
    ""ProductBlock_Value0_Description"": ""some text"",
    ""ProductBlock_Value1_Type"": ""type here"",
    ""ProductBlock_Value1_Name"": ""Block Two"",
    ""ProductBlock_Value1_Description"": ""some text""
  }
]";
            using (var r = ChoXmlReader<Specifier>.LoadText(xml).WithXPath("//").ErrorMode(ChoErrorMode.IgnoreAndContinue)
      )
            {
                //r.Print();
                var recs = r.AsDataTable();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        public class Specifier
        {
            public Block CollectionBlock { get; set; }
            [ChoXPath("//ProductBlock")]
            public Block[] ProductBlock { get; set; }
        }

        public class Block
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
        }
        [XmlRoot(ElementName = "Customer")]
        public class Customer
        {
            [XmlAttribute(AttributeName = "FirstName")]
            public string FirstName { get; set; }
            [XmlAttribute(AttributeName = "LastName")]
            public string LastName { get; set; }
        }

        [XmlRoot(ElementName = "Request")]
        public class Request
        {
            [XmlElement(ElementName = "Customer")]
            public Customer Customer { get; set; }
            [XmlElement(ElementName = "SubRequestXml")]
            public ChoCDATA SubRequestXml { get; set; }
            [XmlAttribute(AttributeName = "CustID")]
            public string CustID { get; set; }
            [XmlAttribute(AttributeName = "OrderNumber")]
            public string OrderNumber { get; set; }
        }
        [Test]
        public static void CDATAValueTest()
        {
            string xml = @"<Request CustID=""001"" OrderNumber=""FRDGD"">
    <Customer FirstName=""ABC"" LastName=""XYZ"" ></Customer>
    <SubRequestXml>
        <![CDATA[<BCC><Cake_Order=""Cake_N01""/></BCC>]]>
    </SubRequestXml>
</Request>";

            using (var r = ChoXmlReader<Request>.LoadText(xml).WithXPath("/")
                .UseXmlSerialization())
            {
                string value = r.FirstOrDefault().SubRequestXml.Value;
                value.Print();

                Assert.AreEqual(value, "<BCC><Cake_Order=\"Cake_N01\"/></BCC>");
            }
        }

        [Test]
        public static void Xml2Json1()
        {
            string xml = @"<Message>
  <MessageInfo>
    <Guid>be190914-4b18-4454-96ec-67887dd4d7a7</Guid>
    <SourceId>101</SourceId>
  </MessageInfo>
<LegalEntities>
 <LegalEntity>
 <Roles>
        <Role>
          <LEAssociateTypeId>101</LEAssociateTypeId>
          <LEAssociateTypeId_Value>Client/Counterparty</LEAssociateTypeId_Value>
          <LastUpdatedDate>2021-08-07T23:05:17</LastUpdatedDate>
          <LegalEntityRoleStatusId>3</LegalEntityRoleStatusId>
          <LegalEntityRoleStatusId_Value>Active</LegalEntityRoleStatusId_Value>
        </Role>
        <Role>
          <LEAssociateTypeId>6000</LEAssociateTypeId>
          <LEAssociateTypeId_Value>Account Owner</LEAssociateTypeId_Value>
          <LastUpdatedDate>2021-08-07T21:20:07</LastUpdatedDate>
          <LegalEntityRoleStatusId>3</LegalEntityRoleStatusId>
          <LegalEntityRoleStatusId_Value>Active</LegalEntityRoleStatusId_Value>
        </Role>
        <Role>
          <LEAssociateTypeId>5003</LEAssociateTypeId>
          <LEAssociateTypeId_Value>Investment Manager</LEAssociateTypeId_Value>
          <LastUpdatedDate>2021-08-16T06:12:59</LastUpdatedDate>
          <LegalEntityRoleStatusId>3</LegalEntityRoleStatusId>
          <LegalEntityRoleStatusId_Value>Active</LegalEntityRoleStatusId_Value>
        </Role>
      </Roles>
 </LegalEntity>
 </LegalEntities>
</Message>";

            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("//")
                )
            {
                using (var w = new ChoJSONWriter(Console.Out)
                    .SupportMultipleContent(true)
                    .Configure(c => c.DefaultArrayHandling = false)
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                    .JsonSerializationSettings(s => s.ReferenceLoopHandling = ReferenceLoopHandling.Serialize)
                    )
                {
                    //w.Write(r);
                    w.Write(r.Select(r1 =>
                    {
                        IList roles = r1["LegalEntities.LegalEntity.Roles"];
                        r1["LegalEntities.LegalEntity.Roles"] = roles.OfType<object>().Select(i => new { Role = i }).ToArray();
                        return r1;
                    }));
                }
            }
        }
        [Test]
        public static void SelectiveChildTest()
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
    <PropertyGroup>
        <AutoGenerateBindingRedirects>true</AutoGenerateBindingRedirects>
        <IsWebBootstrapper>false</IsWebBootstrapper>
        <PublishUrl>publish\</PublishUrl>
        <Install>true</Install>
        <UpdateIntervalUnits>Days</UpdateIntervalUnits>
    </PropertyGroup>
    <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|iPhoneSimulator' "">
        <DebugSymbols>true</DebugSymbols>
        <DebugType>full</DebugType>
        <Optimize>false</Optimize>
    </PropertyGroup>
    <ItemGroup>
        <Compile Include=""Launch.cs"" />
        <Compile Include=""Launch.designer.cs"">
          <DependentUpon>Launch.cs</DependentUpon>
        </Compile>
        <Compile Include=""Main.cs"" />
        <None Include=""Info.plist"">
          <SubType>Designer</SubType>
        </None>
    </ItemGroup>
    <ItemGroup>
        <Reference Include=""System"" />
        <Reference Include=""System.Core"" />
    </ItemGroup>
    <ItemGroup>
        <ProjectReference Include=""..\..\Model\Project.Model.csproj"">
          <Project>{0511395F-513C-4F56-BF87-718CA49BB13B}</Project>
          <Name>Project.Model</Name>
        </ProjectReference>
    </ItemGroup>
    <ItemGroup>
        <PackageReference Include=""AutoMapper"">
          <Version>7.0.1</Version>
        </PackageReference>
        <PackageReference Include=""GMImagePicker.Xamarin"">
          <Version>2.5.0</Version>
        </PackageReference>
        <PackageReference Include=""IdentityModel"">
          <Version>4.1.0</Version>
        </PackageReference>
        <PackageReference Include=""Microsoft.AppCenter"">
          <Version>4.3.0</Version>
        </PackageReference>
        <PackageReference Include=""Microsoft.AppCenter.Analytics"">
          <Version>4.3.0</Version>
        </PackageReference>
        <PackageReference Include=""Microsoft.AppCenter.Crashes"">
          <Version>4.3.0</Version>
        </PackageReference>
        <PackageReference Include=""Package.Test"" Version=""1.1.21"" />
    </ItemGroup>
  <ItemGroup />
</Project>";

            using (var r = ChoXmlReader.LoadText(xml).ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .WithXmlNamespace("http://schemas.microsoft.com/developer/msbuild/2003")
               .WithXPath("//PackageReference")
               .WithField("Include")
               .WithField("Version")
               )
            {
                r.Print();
                return;
                using (var w = new ChoJSONWriter(Console.Out)
                    )
                    w.Write(r);
            }
        }

        [Test]
        public static void Issue165()
        {
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <XElement>
    <Id>1</Id>
    <nestedobject>
      <id>2</id>
      <name>objName</name>
    </nestedobject>
    <nestedarray>
      <name>namelist10</name>
      <city>citylist10</city>
    </nestedarray>
    <nestedarray>
      <name>namelist11</name>
      <city>citylist11</city>
    </nestedarray>
  </XElement>
  <XElement>
    <Id>2</Id>
    <name>name1</name>
    <nestedobject>
      <id>3</id>
      <name>obj3Nmae</name>
    </nestedobject>
    <nestedarray>
      <name>namelist20</name>
      <city>citylist20</city>
    </nestedarray>
    <nestedarray>
      <city>citylist21</city>
    </nestedarray>
  </XElement>
</Root>";

            string csv =
                @"Id,name,nestedobject/id,nestedobject/name,nestedarray/0/name, nestedarray/0/city, nestedarray/1/name, nestedarray/1/city
1,,2,objName,namelist10,citylist10,namelist11,citylist11
2,name1,3,obj3Nmae,namelist20,citylist20,,citylist21";

            StringBuilder json = new StringBuilder();
            using (var w = new ChoXmlWriter(json)
                .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Null)
                //.Configure(c => c.NullValueHandling = ChoNullValueHandling.Ignore)
                )
            {
                using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                    .Configure(c => c.NestedKeySeparator = '/')
                    .WithMaxScanRows(1)
                    //.IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Null)
                    )
                    //r.Print();
                    w.Write(r);
            }

            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Issue165_1()
        {
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <XElement>
    <Id>1</Id>
    <nestedobject>
      <id>2</id>
      <name>objName</name>
    </nestedobject>
    <nestedarray>
      <name>namelist10</name>
      <city>citylist10</city>
    </nestedarray>
    <nestedarray>
      <name>namelist11</name>
      <city>citylist11</city>
    </nestedarray>
  </XElement>
  <XElement>
    <Id>2</Id>
    <nestedobject>
      <id>3</id>
      <name>obj3Nmae</name>
    </nestedobject>
    <nestedarray>
      <name>namelist20</name>
      <city>citylist20</city>
    </nestedarray>
    <nestedarray>
      <city>citylist21</city>
    </nestedarray>
  </XElement>
</Root>";

            string csv =
                @"Id,name,nestedobject/id,nestedobject/name,nestedarray/0/name, nestedarray/0/city, nestedarray/1/name, nestedarray/1/city
1,,2,objName,namelist10,citylist10,namelist11,citylist11
2,name1,3,obj3Nmae,namelist20,citylist20,,citylist21";

            StringBuilder json = new StringBuilder();
            using (var w = new ChoXmlWriter(json)
                //.JsonSerializationSettings(s => s.NullValueHandling = NullValueHandling.Ignore)
                )
            {
                using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                    .Configure(c => c.NestedKeySeparator = '/')
                    .WithMaxScanRows(1)
                    .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Any)
                    )
                    w.Write(r);
            }

            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Xml2JsonAttributeAs()
        {
            string xml = @"<recipe>
   <orderedDirections>
      <add what=""flour"" to=""bowl"" amount=""1c""/> 
      <add what=""sugar"" to=""bowl"" amount=""1/2c""/>  
      <stir what=""bowl""/>
      <move from=""bowl"" to=""pot"" amount=""1/2""/>
      <add what=""eggs"" to=""pot""/>
      <stir what=""pot""/>
   </orderedDirections>
</recipe>";

            using (var r = ChoXmlReader.LoadText(xml)
                )
            {
                using (var w = new ChoJSONWriter(Console.Out)
                    .Configure(c => c.TurnOnAutoDiscoverJsonConverters = true)
                    .Configure(c => c.EnableXmlAttributePrefix = false)
                    )
                    w.Write(r);
            }
        }

        [Test]
        public static void SerializeAndDeserializeObjectWithType()
        {
            string xml = @"<SecurityCustomizationData xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">
  <_x003C_CustomizationsForTypeList_x003E_k__BackingField>
    <BaseRepositoryCustomizations i:type=""RepositoryCustomizationsOfAxSecurityRoleNcCATIYq"">
    </BaseRepositoryCustomizations>
    <BaseRepositoryCustomizations i:type=""RepositoryCustomizationsOfAxSecurityDutyNcCATIYq"">
    </BaseRepositoryCustomizations>
    <BaseRepositoryCustomizations i:type=""RepositoryCustomizationsOfAxSecurityPrivilegeNcCATIYq"">
    </BaseRepositoryCustomizations>
  </_x003C_CustomizationsForTypeList_x003E_k__BackingField>
</SecurityCustomizationData>";

            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("//")
                .WithXmlNamespace("i", "http://www.w3.org/2001/XMLSchema-instance")
                )
            {
                using (var w = new ChoXmlWriter(Console.Out)
                .WithXmlNamespace("i", "http://www.w3.org/2001/XMLSchema-instance")
                    )
                    w.Write(r);
            }
        }

        [Test]
        public static void ExtractAllNodes()
        {
            string xml = @"<rss xmlns:atom=""http://www.w3.org/2005/Atom"" xmlns:media=""http://search.yahoo.com/mrss/"" version=""2.0"">
    <channel>
        <item>
            <title>Fire kills four newborn babies at children's hospital in India</title>
            <link>http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344</link>
            <description>Four newborn babies have died after a fire broke out at a children's hospital in India, officials said.</description>
            <pubDate>Tue, 09 Nov 2021 07:51:00 +0000</pubDate>
            <guid>http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344</guid>
            <enclosure url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" length=""0"" type=""image/jpeg"" />
            <media:description type=""html"">A man carries a child out from the Kamla Nehru Children’s Hospital after a fire in the newborn care unit of the hospital killed four infants, in Bhopal, India, Monday, Nov. 8, 2021. There were 40 children in total in the unit, out of which 36 have been rescued, said Medical Education Minister Vishwas Sarang. (AP Photo) </media:description>
            <media:thumbnail url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" width=""70"" height=""70"" />
            <media:content type=""image/jpeg"" url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" />
        </item>
    </channel>
</rss>";

            string expected = @"[
  {
    ""desc"": {
      ""media:type"": ""html"",
      ""#text"": ""A man carries a child out from the Kamla Nehru Children’s Hospital after a fire in the newborn care unit of the hospital killed four infants, in Bhopal, India, Monday, Nov. 8, 2021. There were 40 children in total in the unit, out of which 36 have been rescued, said Medical Education Minister Vishwas Sarang. (AP Photo) ""
    }
  }
]";
            string expected1 = @"[
  {
    ""desc"": {
      ""media:type"": ""html"",
      ""media:xmlns"": ""http://search.yahoo.com/mrss/"",
      ""#text"": ""A man carries a child out from the Kamla Nehru Children’s Hospital after a fire in the newborn care unit of the hospital killed four infants, in Bhopal, India, Monday, Nov. 8, 2021. There were 40 children in total in the unit, out of which 36 have been rescued, said Medical Education Minister Vishwas Sarang. (AP Photo) ""
    }
  }
]";
            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("//item")
                .WithXmlNamespace("media", "http://search.yahoo.com/mrss/")
                .WithField("desc", xPath: "media:description")
                )
            {
                var recs = r.ToArray();

                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void ExtractAllNodes_1()
        {
            string xml = @"<rss xmlns:atom=""http://www.w3.org/2005/Atom"" xmlns:media=""http://search.yahoo.com/mrss/"" version=""2.0"">
    <channel>
        <item>
            <title>Fire kills four newborn babies at children's hospital in India</title>
            <link>http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344</link>
            <description>Four newborn babies have died after a fire broke out at a children's hospital in India, officials said.</description>
            <pubDate>Tue, 09 Nov 2021 07:51:00 +0000</pubDate>
            <guid>http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344</guid>
            <enclosure url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" length=""0"" type=""image/jpeg"" />
            <media:description type=""html"">A man carries a child out from the Kamla Nehru Children’s Hospital after a fire in the newborn care unit of the hospital killed four infants, in Bhopal, India, Monday, Nov. 8, 2021. There were 40 children in total in the unit, out of which 36 have been rescued, said Medical Education Minister Vishwas Sarang. (AP Photo) </media:description>
            <media:thumbnail url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" width=""70"" height=""70"" />
            <media:content type=""image/jpeg"" url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" />
        </item>
    </channel>
</rss>";

            string expected = @"[
  {
    ""desc"": {
      ""media:type"": ""html"",
      ""media:xmlns"": ""http://search.yahoo.com/mrss/"",
      ""#text"": ""A man carries a child out from the Kamla Nehru Children’s Hospital after a fire in the newborn care unit of the hospital killed four infants, in Bhopal, India, Monday, Nov. 8, 2021. There were 40 children in total in the unit, out of which 36 have been rescued, said Medical Education Minister Vishwas Sarang. (AP Photo) ""
    }
  }
]";
            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("//item")
                .WithXmlNamespace("media", "http://search.yahoo.com/mrss/")
                .WithField("desc", xPath: "media:description")
                .Configure(c => c.IncludeAllSchemaNS = true)
                )
            {
                var recs = r.ToArray();

                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void DynamicAutoDiscoverColumnsTest()
        {
            string xml = @"<rss xmlns:atom=""http://www.w3.org/2005/Atom"" xmlns:media=""http://search.yahoo.com/mrss/"" version=""2.0"">
    <channel>
        <item>
            <title>Fire kills four newborn babies at children's hospital in India</title>
            <link>http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344</link>
            <description>Four newborn babies have died after a fire broke out at a children's hospital in India, officials said.</description>
            <pubDate>Tue, 09 Nov 2021 07:51:00 +0000</pubDate>
            <guid>http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344</guid>
            <enclosure url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" length=""0"" type=""image/jpeg"" />
            <media:description type=""html"">A man carries a child out from the Kamla Nehru Children’s Hospital after a fire in the newborn care unit of the hospital killed four infants, in Bhopal, India, Monday, Nov. 8, 2021. There were 40 children in total in the unit, out of which 36 have been rescued, said Medical Education Minister Vishwas Sarang. (AP Photo) </media:description>
            <media:thumbnail url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" width=""70"" height=""70"" />
            <media:content type=""image/jpeg"" url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" />
        </item>
    </channel>
</rss>";

            string expected = @"[
  {
    ""desc"": {
      ""media:type"": ""html"",
      ""media:xmlns"": ""http://search.yahoo.com/mrss/"",
      ""#text"": ""A man carries a child out from the Kamla Nehru Children’s Hospital after a fire in the newborn care unit of the hospital killed four infants, in Bhopal, India, Monday, Nov. 8, 2021. There were 40 children in total in the unit, out of which 36 have been rescued, said Medical Education Minister Vishwas Sarang. (AP Photo) ""
    },
    ""title"": ""Fire kills four newborn babies at children's hospital in India"",
    ""link"": ""http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344"",
    ""description"": ""Four newborn babies have died after a fire broke out at a children's hospital in India, officials said."",
    ""pubDate"": ""Tue, 09 Nov 2021 07:51:00 +0000"",
    ""guid"": ""http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344"",
    ""enclosure"": {
      ""url"": ""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"",
      ""length"": ""0"",
      ""type"": ""image/jpeg""
    },
    ""thumbnail"": {
      ""media:url"": ""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"",
      ""media:width"": ""70"",
      ""media:height"": ""70"",
      ""media:xmlns"": ""http://search.yahoo.com/mrss/""
    },
    ""content"": {
      ""media:type"": ""image/jpeg"",
      ""media:url"": ""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"",
      ""media:xmlns"": ""http://search.yahoo.com/mrss/""
    }
  }
]";
            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("//item")
                .WithXmlNamespace("media", "http://search.yahoo.com/mrss/")
                .WithField("desc", xPath: "media:description")
                .Configure(c => c.AutoDiscoverColumns = true)
                .Configure(c => c.IncludeAllSchemaNS = true)
                )
            {
                var recs = r.ToArray();

                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void DynamicAutoDiscoverColumnsTest_1()
        {
            string xml = @"<rss xmlns:atom=""http://www.w3.org/2005/Atom"" xmlns:media=""http://search.yahoo.com/mrss/"" version=""2.0"">
    <channel>
        <item>
            <title>Fire kills four newborn babies at children's hospital in India</title>
            <link>http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344</link>
            <description>Four newborn babies have died after a fire broke out at a children's hospital in India, officials said.</description>
            <pubDate>Tue, 09 Nov 2021 07:51:00 +0000</pubDate>
            <guid>http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344</guid>
            <enclosure url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" length=""0"" type=""image/jpeg"" />
            <media:description type=""html"">A man carries a child out from the Kamla Nehru Children’s Hospital after a fire in the newborn care unit of the hospital killed four infants, in Bhopal, India, Monday, Nov. 8, 2021. There were 40 children in total in the unit, out of which 36 have been rescued, said Medical Education Minister Vishwas Sarang. (AP Photo) </media:description>
            <media:thumbnail url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" width=""70"" height=""70"" />
            <media:content type=""image/jpeg"" url=""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"" />
        </item>
    </channel>
</rss>";

            string expected = @"[
  {
    ""desc"": {
      ""media:type"": ""html"",
      ""#text"": ""A man carries a child out from the Kamla Nehru Children’s Hospital after a fire in the newborn care unit of the hospital killed four infants, in Bhopal, India, Monday, Nov. 8, 2021. There were 40 children in total in the unit, out of which 36 have been rescued, said Medical Education Minister Vishwas Sarang. (AP Photo) ""
    },
    ""title"": ""Fire kills four newborn babies at children's hospital in India"",
    ""link"": ""http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344"",
    ""description"": ""Four newborn babies have died after a fire broke out at a children's hospital in India, officials said."",
    ""pubDate"": ""Tue, 09 Nov 2021 07:51:00 +0000"",
    ""guid"": ""http://news.sky.com/story/india-fire-kills-four-newborn-babies-at-childrens-hospital-in-madhya-pradesh-12464344"",
    ""enclosure"": {
      ""url"": ""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"",
      ""length"": ""0"",
      ""type"": ""image/jpeg""
    },
    ""thumbnail"": {
      ""media:url"": ""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515"",
      ""media:width"": ""70"",
      ""media:height"": ""70""
    },
    ""content"": {
      ""media:type"": ""image/jpeg"",
      ""media:url"": ""https://e3.365dm.com/21/11/70x70/skynews-india-fire-childrens-hospital_5577072.jpg?20211109081515""
    }
  }
]";
            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("//item")
                .WithXmlNamespace("media", "http://search.yahoo.com/mrss/")
                .WithField("desc", xPath: "media:description")
                .Configure(c => c.AutoDiscoverColumns = true)
                .Configure(c => c.IncludeAllSchemaNS = false)
                )
            {
                var recs = r.ToArray();

                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void ReadSubNode_1()
        {
            string xml1 = @"<?xml version=""1.0"" encoding=""utf-8""?>
  <response>
   <error>
    <errorcode>1002</errorcode>
    <errortext>there is already an open session</errortext>
   </error>
</response>";


            string expected = @"{
  ""error"": {
    ""errorcode"": ""1002"",
    ""errortext"": ""there is already an open session""
  },
  ""returncode"": null,
  ""authkey"": null,
  ""data"": null
}";

            using (var r1 = ChoXmlReader.LoadText(xml1)
                .WithXPath("//response")
                .WithField("error", xPath: "/error")
                .WithField("returncode", xPath: "/returncode")
                .WithField("authkey", xPath: "/authkey")
                .WithField("data", xPath: "/data")
              )
            {
                var rec = r1.FirstOrDefault();

                if (rec != null)
                {
                    if (rec.error != null)
                        rec.error.PrintAsJson();
                    if (rec.returncode != null)
                    {
                        rec.returncode.PrintAsJson();
                    }
                    if (rec.authkey != null)
                    {
                        Console.WriteLine(rec.authkey);
                    }
                    if (rec.data != null)
                    {
                        foreach (var v in rec.data.vessels)
                            v.PrintAsJson();
                    }
                }

                var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void ReadSubNode_2()
        {
            string xml2 = @"<response>
	<returncode>
		<code>100</code>
		<description>successful</description>
	</returncode>
	<authkey>
		xxxx
	</authkey>
</response>";

            string expected = @"{
  ""error"": null,
  ""returncode"": {
    ""code"": ""100"",
    ""description"": ""successful""
  },
  ""authkey"": ""xxxx"",
  ""data"": null
}";

            using (var r1 = ChoXmlReader.LoadText(xml2)
                .WithXPath("//response")
                .WithField("error", xPath: "/error")
                .WithField("returncode", xPath: "/returncode")
                .WithField("authkey", xPath: "/authkey")
                .WithField("data", xPath: "/data")
              )
            {
                var rec = r1.FirstOrDefault();

                if (rec != null)
                {
                    if (rec.error != null)
                        rec.error.PrintAsJson();
                    if (rec.returncode != null)
                    {
                        rec.returncode.PrintAsJson();
                    }
                    if (rec.authkey != null)
                    {
                        Console.WriteLine(rec.authkey);
                    }
                    if (rec.data != null)
                    {
                        foreach (var v in rec.data.vessels)
                            v.PrintAsJson();
                    }
                }

                var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void ReadSubNode_3()
        {
            string xml3 = @"<response>
	<returncode></returncode>
	<data>
		<vessels>
			<vessel>
				<id>1</id>
				<name>v1</name>
			</vessel>
		</vessels>
	</data>
</response>";

            string expected = @"{
  ""error"": null,
  ""returncode"": null,
  ""authkey"": null,
  ""data"": {
    ""vessels"": {
      ""vessel"": {
        ""id"": ""1"",
        ""name"": ""v1""
      }
    }
  }
}";

            using (var r1 = ChoXmlReader.LoadText(xml3)
                .WithXPath("//response")
                .WithField("error", xPath: "/error")
                .WithField("returncode", xPath: "/returncode")
                .WithField("authkey", xPath: "/authkey")
                .WithField("data", xPath: "/data")
              )
            {
                var rec = r1.FirstOrDefault();

                if (rec != null)
                {
                    if (rec.error != null)
                        rec.error.PrintAsJson();
                    if (rec.returncode != null)
                    {
                        rec.returncode.PrintAsJson();
                    }
                    if (rec.authkey != null)
                    {
                        Console.WriteLine(rec.authkey);
                    }
                    if (rec.data != null)
                    {
                        //foreach (var v in rec.data.vessels)
                        //    v.Print();
                    }
                }

                var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void ReadSubNode_4()
        {
            string xml4 = @"<response>
	<returncode></returncode>
	<data>
		<vessels>
			<vessel>
				<id>1</id>
				<name>v1</name>
			</vessel>
			<vessel>
				<id>2</id>
				<name>v2</name>
			</vessel>
		</vessels>
	</data>
</response>";

            string expected = @"{
  ""error"": null,
  ""returncode"": null,
  ""authkey"": null,
  ""data"": {
    ""vessels"": [
      {
        ""id"": ""1"",
        ""name"": ""v1""
      },
      {
        ""id"": ""2"",
        ""name"": ""v2""
      }
    ]
  }
}";

            using (var r1 = ChoXmlReader.LoadText(xml4)
                .WithXPath("//response")
                .WithField("error", xPath: "/error")
                .WithField("returncode", xPath: "/returncode")
                .WithField("authkey", xPath: "/authkey")
                .WithField("data", xPath: "/data")
              )
            {
                var rec = r1.FirstOrDefault();

                if (rec != null)
                {
                    if (rec.error != null)
                        rec.error.PrintAsJson();
                    if (rec.returncode != null)
                    {
                        rec.returncode.PrintAsJson();
                    }
                    if (rec.authkey != null)
                    {
                        Console.WriteLine(rec.authkey);
                    }
                    if (rec.data != null)
                    {
                        foreach (var v in rec.data.vessels)
                            v.PrintAsJson();
                    }
                }

                var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Json2XMLWithNS()
        {
            //dynamic root = new ChoDynamicObject("item");

            //root.name = "item #1";
            //root.code = "item #2";

            //dynamic image = new ChoDynamicObject("image");
            //image.AddNamespace("i", "http://i.com");
            //image.url = "http://www.test.com/bar.jpg";

            //root.AddNamespace("foo", "http://temp.com");

            //root.image = image;
            //Console.WriteLine(root.GetXml());
            //return;

            string expected = @"<foo:item xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:foo=""http://temp.com"" xmlns:test=""http://test.com"">
  <foo:name>item #1</foo:name>
  <foo:code>itm-123</foo:code>
  <foo:image url=""http://www.test.com/bar.jpg"" />
</foo:item>";
            string json = @"
		{
		  'item': {
			'name': 'item #1',
			'code': 'itm-123',
			'image': {
			  '@url': 'http://www.test.com/bar.jpg'
			}
		  }
		}";

            StringBuilder xml = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoXmlWriter(xml)
                    .IgnoreRootName()
                    .IgnoreNodeName()
                    .WithDefaultXmlNamespace("foo", "http://temp.com")
                    .WithXmlNamespace("test", "http://test.com")
                    )
                {
                    w.Write(r);
                }
            }

            var actual = xml.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Json2XMLWithNS_1()
        {
            string expected = @"<foo:foo:item>
  <foo:name>item #1</foo:name>
  <foo:code>item #2</foo:code>
  <i:image>
    <i:url>http://www.test.com/bar.jpg</i:url>
  </i:image>
</foo:foo:item>";

            dynamic root = new ChoDynamicObject("item");

            root.name = "item #1";
            root.code = "item #2";

            dynamic image = new ChoDynamicObject("image");
            image.AddNamespace("i", "http://i.com");
            image.url = "http://www.test.com/bar.jpg";

            root.AddNamespace("foo", "http://temp.com");

            root.image = image;
            //root.GetXml().Print();

            ChoXmlNamespaceManager nsMgr = new ChoXmlNamespaceManager();
            nsMgr.AddNamespace("foo", "http://temp.com");
            nsMgr.AddNamespace("i", "http://i.com");

            var actual = ((ChoDynamicObject)root).GetXml(nsMgr: nsMgr);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void LoadSubnodeWithDifferentNS()
        {
            string json = @"
		{
		  'item': {
			'name': 'item #1',
			'code': 'itm-123',
			'image': {
			  '@url': 'http://www.test.com/bar.jpg'
			}
		  }
		}";

            string expected = @"<foo:item xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:foo=""http://temp.com"">
  <foo:name>item #1</foo:name>
  <foo:code>itm-123</foo:code>
  <foo:image url=""http://www.test.com/bar.jpg"" />
</foo:item>";

            StringBuilder xml = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoXmlWriter(xml)
                    .IgnoreRootName()
                    .IgnoreNodeName()
                    .WithDefaultXmlNamespace("foo", "http://temp.com")
                    )
                {
                    w.Write(r);
                }
            }

            var actual = xml.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DateTimeHandling()
        {
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
    <book id=""2"" date=""2013-30-11"">
        <author>Mark Colsberg</author>
        <title>Dolor sit amet</title>
        <price>5.95</price>
        <description>Lorem ipsum</description>
    </book>
</catalog>";

            string expected = @"[
  {
    ""Id"": 1,
    ""Date"": ""2012-01-02T00:00:00"",
    ""Author"": null,
    ""Price"": 44.95,
    ""Title"": ""XML Developer's Guide""
  },
  {
    ""Id"": 2,
    ""Date"": ""2013-11-30T00:00:00"",
    ""Author"": ""Mark Colsberg"",
    ""Price"": 5.95,
    ""Title"": ""Dolor sit amet""
  }
]";
            //ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "yyyy-dd-MM";
            using (var r = ChoXmlReader<Book>.LoadText(xml)
                   .WithXPath("//catalog/book", true)
                   .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                   .TypeConverterFormatSpec(ts => ts.DateTimeFormat = "yyyy-dd-MM")
                   )
            {
                var recs = r.ToArray();
                foreach (var rec in recs)
                {
                    Console.WriteLine(rec.Dump());
                }

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Book
        {
            [XmlAttribute("id")]
            public int Id { get; set; }
            [XmlAttribute("date")]
            public DateTime Date { get; set; }
            [XmlElement("author")]
            public string Author { get; set; }
            [XmlElement("price")]
            public double Price { get; set; }
            [XmlElement("title")]
            public string Title { get; set; }
        }
        [Test]
        public static void UseComplexXPath()
        {
            string xml = @"<?xml version='1.0' encoding='UTF-8'?>
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
";

            string expected = @"Value_0,Value_1
fizeofnpj-dzeifjzenf-ezfizef,6000009251";

            var nsManager = new XmlNamespaceManager(new NameTable());
            //register mapping of prefix to namespace uri 
            nsManager.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            nsManager.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");

            StringBuilder csv = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml)
                  .WithXmlNamespaceManager(nsManager)
                  .WithXPath("/m:properties")
                  .WithField("Value", "d:Guid | d:ObjectId")
                  .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                  .Configure(c => c.IgnoreNSPrefix = true)
                  .Configure(c => c.IgnoreRootDictionaryFieldPrefix = true)
                )
            {
                //p.Print();
                //return;
                var recs = p.ToArray();

                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                    )
                    w.Write(recs);
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ERDMXmlToDataTable()
        {
            string expected = @"[
  {
    ""Root_RootFilePath"": ""/temp/"",
    ""Root_MajorVersion"": ""1"",
    ""Root_MinorVersion"": ""2"",
    ""Root_Locale"": ""US"",
    ""Root_Description"": ""Test Case"",
    ""Root_DataInterchangeType"": ""Update"",
    ""Root_CaseId"": ""Case1"",
    ""Root_Fields_0_Field_Key"": ""A"",
    ""Root_Fields_0_Field_Name"": ""Doc Identifier"",
    ""Root_Fields_0_Field_DataType"": ""FixedLengthText"",
    ""Root_Fields_0_Field_MaxLength"": ""255"",
    ""Root_Fields_1_Field_Key"": ""B"",
    ""Root_Fields_1_Field_Name"": ""Group ID"",
    ""Root_Fields_1_Field_DataType"": ""FixedLengthText"",
    ""Root_Fields_1_Field_MaxLength"": ""255"",
    ""Root_Fields_2_Field_Key"": ""C"",
    ""Root_Fields_2_Field_Name"": ""Full Folder Path"",
    ""Root_Fields_2_Field_DataType"": ""FixedLengthText"",
    ""Root_Fields_2_Field_MaxLength"": ""255"",
    ""Root_Fields_3_Field_Key"": ""D"",
    ""Root_Fields_3_Field_Name"": ""Custodian"",
    ""Root_Fields_3_Field_DataType"": ""FixedLengthText"",
    ""Root_Fields_3_Field_MaxLength"": ""255"",
    ""Root_Fields_4_Field_Key"": ""E"",
    ""Root_Fields_4_Field_Name"": ""Email Author"",
    ""Root_Fields_4_Field_DataType"": ""LongText"",
    ""Root_Fields_4_Field_EDRMFieldMap"": ""From"",
    ""Root_Fields_5_Field_Key"": ""F"",
    ""Root_Fields_5_Field_Name"": ""Email BCC"",
    ""Root_Fields_5_Field_DataType"": ""LongText"",
    ""Root_Fields_5_Field_EDRMFieldMap"": ""BCC"",
    ""Root_Fields_6_Field_Key"": ""G"",
    ""Root_Fields_6_Field_Name"": ""Email CC"",
    ""Root_Fields_6_Field_DataType"": ""LongText"",
    ""Root_Fields_6_Field_EDRMFieldMap"": ""CC"",
    ""Root_Fields_7_Field_Key"": ""H"",
    ""Root_Fields_7_Field_Name"": ""Email Sent Date"",
    ""Root_Fields_7_Field_DataType"": ""DateTime"",
    ""Root_Fields_7_Field_EDRMFieldMap"": ""DateSent"",
    ""Root_Fields_8_Field_Key"": ""I"",
    ""Root_Fields_8_Field_Name"": ""Email Subject"",
    ""Root_Fields_8_Field_DataType"": ""LongText"",
    ""Root_Fields_8_Field_EDRMFieldMap"": ""Subject"",
    ""Root_Fields_9_Field_Key"": ""J"",
    ""Root_Fields_9_Field_Name"": ""Email To"",
    ""Root_Fields_9_Field_DataType"": ""LongText"",
    ""Root_Fields_9_Field_EDRMFieldMap"": ""To"",
    ""Root_Fields_10_Field_Key"": ""K"",
    ""Root_Fields_10_Field_Name"": ""Parent Doc ID"",
    ""Root_Fields_10_Field_DataType"": ""FixedLengthText"",
    ""Root_Fields_10_Field_MaxLength"": ""255"",
    ""Root_Fields_11_Field_Key"": ""L"",
    ""Root_Fields_11_Field_Name"": ""Responsiveness"",
    ""Root_Fields_11_Field_DataType"": ""SingleChoiceList"",
    ""Root_Fields_11_Field_Choices_0_Choice_Key"": ""C1"",
    ""Root_Fields_11_Field_Choices_0_Choice_Name"": ""Responsive"",
    ""Root_Fields_11_Field_Choices_1_Choice_Key"": ""C2"",
    ""Root_Fields_11_Field_Choices_1_Choice_Name"": ""Not Sure"",
    ""Root_Fields_11_Field_Choices_2_Choice_Key"": ""C3"",
    ""Root_Fields_11_Field_Choices_2_Choice_Name"": ""Not Responsive"",
    ""Root_Fields_12_Field_Key"": ""M"",
    ""Root_Fields_12_Field_Name"": ""Issues"",
    ""Root_Fields_12_Field_DataType"": ""MultipleChoiceList"",
    ""Root_Fields_12_Field_Choices_0_Choice_Key"": ""C1"",
    ""Root_Fields_12_Field_Choices_0_Choice_Name"": ""Issue 1"",
    ""Root_Fields_12_Field_Choices_0_Choice_Choice_Key"": ""C2"",
    ""Root_Fields_12_Field_Choices_0_Choice_Choice_Name"": ""Issue - Child 1"",
    ""Root_Fields_12_Field_Choices_1_Choice_Key"": ""C3"",
    ""Root_Fields_12_Field_Choices_1_Choice_Name"": ""Issue 2"",
    ""Root_Fields_12_Field_Choices_1_Choice_Choice_Key"": ""C4"",
    ""Root_Fields_12_Field_Choices_1_Choice_Choice_Name"": ""Issue - Child 1"",
    ""Root_Fields_13_Field_Key"": ""N"",
    ""Root_Fields_13_Field_Name"": ""Extracted Text"",
    ""Root_Fields_13_Field_DataType"": ""LongText"",
    ""Root_Fields_13_Field_IsTextPointer"": ""1"",
    ""Root_Batch_name"": ""Sample Batch"",
    ""Root_Batch_Documents_0_Document_MimeType"": ""text/plain"",
    ""Root_Batch_Documents_0_Document_DocType"": ""Text File"",
    ""Root_Batch_Documents_0_Document_DocID"": ""1"",
    ""Root_Batch_Documents_1_Document_MimeType"": ""text/plain"",
    ""Root_Batch_Documents_1_Document_DocType"": ""Text File 2"",
    ""Root_Batch_Documents_1_Document_DocID"": ""2"",
    ""Root_Batch_Documents_1_Document_Tags_Tag_TagValue"": ""Tag Value??"",
    ""Root_Batch_Documents_1_Document_Tags_Tag_TagName"": ""Tag Name??"",
    ""Root_Batch_Documents_1_Document_Tags_Tag_TagDataType"": ""LongText"",
    ""Root_Batch_Documents_1_Document_Tags_Tag_ModifiedBy"": ""Jane Doe"",
    ""Root_Batch_Documents_1_Document_Files_File_FileType"": ""7bit ASCII Doc"",
    ""Root_Batch_Documents_1_Document_Files_File_ExternalFile_MergeFileNum"": ""0"",
    ""Root_Batch_Documents_1_Document_Files_File_ExternalFile_MergeFileCount"": ""0"",
    ""Root_Batch_Documents_1_Document_Files_File_ExternalFile_Hash"": ""1234567890"",
    ""Root_Batch_Documents_1_Document_Files_File_ExternalFile_FileSize"": ""1000"",
    ""Root_Batch_Documents_1_Document_Files_File_ExternalFile_FilePath"": ""c:\\"",
    ""Root_Batch_Documents_1_Document_Files_File_ExternalFile_FileName"": ""data.txt"",
    ""Root_Batch_Documents_1_Document_Reviews_Review_ReviewId"": ""1"",
    ""Root_Batch_Documents_1_Document_Reviews_Review_Tag_TagValue"": ""Tag Value??"",
    ""Root_Batch_Documents_1_Document_Reviews_Review_Tag_TagName"": ""Tag Name??"",
    ""Root_Batch_Documents_1_Document_Reviews_Review_Tag_TagDataType"": ""LongText"",
    ""Root_Batch_Documents_1_Document_Reviews_Review_Tag_ModifiedBy"": ""Jane Doe"",
    ""Root_Batch_Documents_1_Document_Locations_Location_Custodian"": ""\n              John Doe\n            "",
    ""Root_Batch_Documents_1_Document_Locations_Location_LocationURI"": ""ATL"",
    ""Root_Batch_Documents_1_Document_Locations_Location_Description"": ""None"",
    ""Root_Batch_Documents_2_Document_MimeType"": ""text/plain"",
    ""Root_Batch_Documents_2_Document_DocType"": ""Text File 3"",
    ""Root_Batch_Documents_2_Document_DocID"": ""3"",
    ""Root_Batch_Documents_2_Document_FieldValues_A"": ""DOC00000"",
    ""Root_Batch_Documents_2_Document_FieldValues_B"": ""14B833B7794C67D86E49F71433C45FEC"",
    ""Root_Batch_Documents_2_Document_FieldValues_C"": ""Jane\\Inbox"",
    ""Root_Batch_Documents_2_Document_FieldValues_D"": ""Jane"",
    ""Root_Batch_Documents_2_Document_FieldValues_E"": ""Ed@email.com"",
    ""Root_Batch_Documents_2_Document_FieldValues_F"": ""Smith@email.com;Eric@email.com"",
    ""Root_Batch_Documents_2_Document_FieldValues_G"": ""Scott@email.com;Cindy@email.com;Sarah@email.com"",
    ""Root_Batch_Documents_2_Document_FieldValues_H"": ""2002-10-25"",
    ""Root_Batch_Documents_2_Document_FieldValues_I"": ""Meeting Minutes"",
    ""Root_Batch_Documents_2_Document_FieldValues_J"": ""Jane@email.com"",
    ""Root_Batch_Documents_2_Document_FieldValues_K"": null,
    ""Root_Batch_Documents_2_Document_FieldValues_L"": ""C1"",
    ""Root_Batch_Documents_2_Document_FieldValues_M"": ""C1;C2"",
    ""Root_Batch_Documents_2_Document_FieldValues_N"": ""file://server1/my documents/extractedtext/1.txt"",
    ""Root_Batch_Documents_2_Document_Files_0_File_FileType"": ""Text"",
    ""Root_Batch_Documents_2_Document_Files_0_File_InlineContent"": ""This is sample inline content."",
    ""Root_Batch_Documents_2_Document_Files_1_File_FileType"": ""Native"",
    ""Root_Batch_Documents_2_Document_Files_1_File_ExternalFile_FilePath"": ""c:\\"",
    ""Root_Batch_Documents_2_Document_Files_1_File_ExternalFile_FileName"": ""sample.doc"",
    ""Root_Batch_Documents_2_Document_Files_1_File_ExternalFile_FileSize"": ""32768"",
    ""Root_Batch_Documents_2_Document_Files_1_File_ExternalFile_Hash"": ""987654321"",
    ""Root_Batch_Documents_2_Document_Files_1_File_ExternalFile_HashType"": ""SHA-1"",
    ""Root_Batch_Documents_2_Document_Reviews_Review_ReviewId"": ""2"",
    ""Root_Batch_Documents_2_Document_Reviews_Review_FieldValues_L"": ""C1"",
    ""Root_Batch_Documents_2_Document_CustomDocumentInfo_SampleTag1_SampleTag1A"": ""This is nested customer info."",
    ""Root_Batch_Documents_2_Document_CustomDocumentInfo_SampleTag1_SampleTag1B"": ""This is additional nested customer info."",
    ""Root_Batch_Documents_2_Document_CustomDocumentInfo_SampleTag2"": ""This is more custom document info."",
    ""Root_Batch_Relationships_Relationship_Type"": ""NearDupe"",
    ""Root_Batch_Relationships_Relationship_ParentDocId"": ""2"",
    ""Root_Batch_Relationships_Relationship_ChildDocId"": ""1"",
    ""Root_Batch_Folders_Folder_FolderParentName"": """",
    ""Root_Batch_Folders_Folder_FolderName"": ""SampleFolder"",
    ""Root_Batch_Folders_Folder_Folder_FolderParentName"": ""SampleFolder"",
    ""Root_Batch_Folders_Folder_Folder_FolderName"": ""SampleFolder2"",
    ""Root_Batch_Folders_Folder_Folder_#text"": ""\n        "",
    ""Root_Batch_Folders_Folder_Document_0_Document_DocId"": ""1"",
    ""Root_Batch_Folders_Folder_Document_1_Document_DocId"": ""2""
  }
]";
            using (var r = new ChoXmlReader("EDRM-1.2-sample-file.xml")
                .WithXPath("//")
                //.Configure(c => c.ArrayNamePrefixMode = ChoArrayNamePrefixMode.ContainerNameOnly)
                )
            {
                //var recs = r.ToArray();
                var dt = r.AsDataTable();

                dt.Print();

                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void ERDMXmlToDataTable_1()
        {
            Assert.Ignore("Revisit the logic.");

            string expected = @"[
  {
    ""Root_RootFilePath"": ""/temp/"",
    ""Root_MajorVersion"": ""1"",
    ""Root_MinorVersion"": ""2"",
    ""Root_Locale"": ""US"",
    ""Root_Description"": ""Test Case"",
    ""Root_DataInterchangeType"": ""Update"",
    ""Root_CaseId"": ""Case1"",
    ""Root_Fields_0_Key"": ""A"",
    ""Root_Fields_0_Name"": ""Doc Identifier"",
    ""Root_Fields_0_DataType"": ""FixedLengthText"",
    ""Root_Fields_0_MaxLength"": ""255"",
    ""Root_Fields_1_Key"": ""B"",
    ""Root_Fields_1_Name"": ""Group ID"",
    ""Root_Fields_1_DataType"": ""FixedLengthText"",
    ""Root_Fields_1_MaxLength"": ""255"",
    ""Root_Fields_2_Key"": ""C"",
    ""Root_Fields_2_Name"": ""Full Folder Path"",
    ""Root_Fields_2_DataType"": ""FixedLengthText"",
    ""Root_Fields_2_MaxLength"": ""255"",
    ""Root_Fields_3_Key"": ""D"",
    ""Root_Fields_3_Name"": ""Custodian"",
    ""Root_Fields_3_DataType"": ""FixedLengthText"",
    ""Root_Fields_3_MaxLength"": ""255"",
    ""Root_Fields_4_Key"": ""E"",
    ""Root_Fields_4_Name"": ""Email Author"",
    ""Root_Fields_4_DataType"": ""LongText"",
    ""Root_Fields_4_EDRMFieldMap"": ""From"",
    ""Root_Fields_5_Key"": ""F"",
    ""Root_Fields_5_Name"": ""Email BCC"",
    ""Root_Fields_5_DataType"": ""LongText"",
    ""Root_Fields_5_EDRMFieldMap"": ""BCC"",
    ""Root_Fields_6_Key"": ""G"",
    ""Root_Fields_6_Name"": ""Email CC"",
    ""Root_Fields_6_DataType"": ""LongText"",
    ""Root_Fields_6_EDRMFieldMap"": ""CC"",
    ""Root_Fields_7_Key"": ""H"",
    ""Root_Fields_7_Name"": ""Email Sent Date"",
    ""Root_Fields_7_DataType"": ""DateTime"",
    ""Root_Fields_7_EDRMFieldMap"": ""DateSent"",
    ""Root_Fields_8_Key"": ""I"",
    ""Root_Fields_8_Name"": ""Email Subject"",
    ""Root_Fields_8_DataType"": ""LongText"",
    ""Root_Fields_8_EDRMFieldMap"": ""Subject"",
    ""Root_Fields_9_Key"": ""J"",
    ""Root_Fields_9_Name"": ""Email To"",
    ""Root_Fields_9_DataType"": ""LongText"",
    ""Root_Fields_9_EDRMFieldMap"": ""To"",
    ""Root_Fields_10_Key"": ""K"",
    ""Root_Fields_10_Name"": ""Parent Doc ID"",
    ""Root_Fields_10_DataType"": ""FixedLengthText"",
    ""Root_Fields_10_MaxLength"": ""255"",
    ""Root_Fields_11_Key"": ""L"",
    ""Root_Fields_11_Name"": ""Responsiveness"",
    ""Root_Fields_11_DataType"": ""SingleChoiceList"",
    ""Root_Fields_11_Choices_0_Key"": ""C1"",
    ""Root_Fields_11_Choices_0_Name"": ""Responsive"",
    ""Root_Fields_11_Choices_1_Key"": ""C2"",
    ""Root_Fields_11_Choices_1_Name"": ""Not Sure"",
    ""Root_Fields_11_Choices_2_Key"": ""C3"",
    ""Root_Fields_11_Choices_2_Name"": ""Not Responsive"",
    ""Root_Fields_12_Key"": ""M"",
    ""Root_Fields_12_Name"": ""Issues"",
    ""Root_Fields_12_DataType"": ""MultipleChoiceList"",
    ""Root_Fields_12_Choices_0_Key"": ""C1"",
    ""Root_Fields_12_Choices_0_Name"": ""Issue 1"",
    ""Root_Fields_12_Choices_0_Choice_Key"": ""C2"",
    ""Root_Fields_12_Choices_0_Choice_Name"": ""Issue - Child 1"",
    ""Root_Fields_12_Choices_1_Key"": ""C3"",
    ""Root_Fields_12_Choices_1_Name"": ""Issue 2"",
    ""Root_Fields_12_Choices_1_Choice_Key"": ""C4"",
    ""Root_Fields_12_Choices_1_Choice_Name"": ""Issue - Child 1"",
    ""Root_Fields_13_Key"": ""N"",
    ""Root_Fields_13_Name"": ""Extracted Text"",
    ""Root_Fields_13_DataType"": ""LongText"",
    ""Root_Fields_13_IsTextPointer"": ""1"",
    ""Root_Batch_name"": ""Sample Batch"",
    ""Root_Batch_Documents_0_MimeType"": ""text/plain"",
    ""Root_Batch_Documents_0_DocType"": ""Text File"",
    ""Root_Batch_Documents_0_DocID"": ""1"",
    ""Root_Batch_Documents_1_MimeType"": ""text/plain"",
    ""Root_Batch_Documents_1_DocType"": ""Text File 2"",
    ""Root_Batch_Documents_1_DocID"": ""2"",
    ""Root_Batch_Documents_1_Tags_Tag_TagValue"": ""Tag Value??"",
    ""Root_Batch_Documents_1_Tags_Tag_TagName"": ""Tag Name??"",
    ""Root_Batch_Documents_1_Tags_Tag_TagDataType"": ""LongText"",
    ""Root_Batch_Documents_1_Tags_Tag_ModifiedBy"": ""Jane Doe"",
    ""Root_Batch_Documents_1_Files_File_FileType"": ""7bit ASCII Doc"",
    ""Root_Batch_Documents_1_Files_File_ExternalFile_MergeFileNum"": ""0"",
    ""Root_Batch_Documents_1_Files_File_ExternalFile_MergeFileCount"": ""0"",
    ""Root_Batch_Documents_1_Files_File_ExternalFile_Hash"": ""1234567890"",
    ""Root_Batch_Documents_1_Files_File_ExternalFile_FileSize"": ""1000"",
    ""Root_Batch_Documents_1_Files_File_ExternalFile_FilePath"": ""c:\\"",
    ""Root_Batch_Documents_1_Files_File_ExternalFile_FileName"": ""data.txt"",
    ""Root_Batch_Documents_1_Reviews_Review_ReviewId"": ""1"",
    ""Root_Batch_Documents_1_Reviews_Review_Tag_TagValue"": ""Tag Value??"",
    ""Root_Batch_Documents_1_Reviews_Review_Tag_TagName"": ""Tag Name??"",
    ""Root_Batch_Documents_1_Reviews_Review_Tag_TagDataType"": ""LongText"",
    ""Root_Batch_Documents_1_Reviews_Review_Tag_ModifiedBy"": ""Jane Doe"",
    ""Root_Batch_Documents_1_Locations_Location_Custodian"": ""\n              John Doe\n            "",
    ""Root_Batch_Documents_1_Locations_Location_LocationURI"": ""ATL"",
    ""Root_Batch_Documents_1_Locations_Location_Description"": ""None"",
    ""Root_Batch_Documents_2_MimeType"": ""text/plain"",
    ""Root_Batch_Documents_2_DocType"": ""Text File 3"",
    ""Root_Batch_Documents_2_DocID"": ""3"",
    ""Root_Batch_Documents_2_FieldValues_A"": ""DOC00000"",
    ""Root_Batch_Documents_2_FieldValues_B"": ""14B833B7794C67D86E49F71433C45FEC"",
    ""Root_Batch_Documents_2_FieldValues_C"": ""Jane\\Inbox"",
    ""Root_Batch_Documents_2_FieldValues_D"": ""Jane"",
    ""Root_Batch_Documents_2_FieldValues_E"": ""Ed@email.com"",
    ""Root_Batch_Documents_2_FieldValues_F"": ""Smith@email.com;Eric@email.com"",
    ""Root_Batch_Documents_2_FieldValues_G"": ""Scott@email.com;Cindy@email.com;Sarah@email.com"",
    ""Root_Batch_Documents_2_FieldValues_H"": ""2002-10-25"",
    ""Root_Batch_Documents_2_FieldValues_I"": ""Meeting Minutes"",
    ""Root_Batch_Documents_2_FieldValues_J"": ""Jane@email.com"",
    ""Root_Batch_Documents_2_FieldValues_K"": null,
    ""Root_Batch_Documents_2_FieldValues_L"": ""C1"",
    ""Root_Batch_Documents_2_FieldValues_M"": ""C1;C2"",
    ""Root_Batch_Documents_2_FieldValues_N"": ""file://server1/my documents/extractedtext/1.txt"",
    ""Root_Batch_Documents_2_Files_0_FileType"": ""Text"",
    ""Root_Batch_Documents_2_Files_0_InlineContent"": ""This is sample inline content."",
    ""Root_Batch_Documents_2_Files_1_FileType"": ""Native"",
    ""Root_Batch_Documents_2_Files_1_ExternalFile_FilePath"": ""c:\\"",
    ""Root_Batch_Documents_2_Files_1_ExternalFile_FileName"": ""sample.doc"",
    ""Root_Batch_Documents_2_Files_1_ExternalFile_FileSize"": ""32768"",
    ""Root_Batch_Documents_2_Files_1_ExternalFile_Hash"": ""987654321"",
    ""Root_Batch_Documents_2_Files_1_ExternalFile_HashType"": ""SHA-1"",
    ""Root_Batch_Documents_2_Reviews_Review_ReviewId"": ""2"",
    ""Root_Batch_Documents_2_Reviews_Review_FieldValues_L"": ""C1"",
    ""Root_Batch_Documents_2_CustomDocumentInfo_SampleTag1_SampleTag1A"": ""This is nested customer info."",
    ""Root_Batch_Documents_2_CustomDocumentInfo_SampleTag1_SampleTag1B"": ""This is additional nested customer info."",
    ""Root_Batch_Documents_2_CustomDocumentInfo_SampleTag2"": ""This is more custom document info."",
    ""Root_Batch_Relationships_Relationship_Type"": ""NearDupe"",
    ""Root_Batch_Relationships_Relationship_ParentDocId"": ""2"",
    ""Root_Batch_Relationships_Relationship_ChildDocId"": ""1"",
    ""Root_Batch_Folders_Folder_FolderParentName"": """",
    ""Root_Batch_Folders_Folder_FolderName"": ""SampleFolder"",
    ""Root_Batch_Folders_Folder_Folder_FolderParentName"": ""SampleFolder"",
    ""Root_Batch_Folders_Folder_Folder_FolderName"": ""SampleFolder2"",
    ""Root_Batch_Folders_Folder_Folder_#text"": ""\n        "",
    ""Root_Batch_Folders_Folder_Document_0_DocId"": ""1"",
    ""Root_Batch_Folders_Folder_Document_1_DocId"": ""2""
  }
]";
            using (var r = new ChoXmlReader("EDRM-1.2-sample-file.xml")
                .WithXPath("//")
                .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                //.Configure(c => c.ArrayNamePrefixMode = ChoArrayNamePrefixMode.ContainerNameOnly)
                )
            {
                //var recs = r.ToArray();
                var dt = r.AsDataTable();

                dt.Print();

                var actual = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class MyObject
        {
            public int Number { get; set; }
            public string Name { get; set; }
            public int intProp { get; set; }
            public string stringProp { get; set; }
            public string doubleProp { get; set; }
            [ChoXPath("/MyChildObj")]
            public List<MyChildObject> myChildObjects { get; set; }
        }

        public class MyChildObject
        {
            public int Number { get; set; }
            public string Name { get; set; }
            public int childIntProp { get; set; }
            public string childStringProp { get; set; }
        }

        [Test]
        public static void ParseXml5()
        {
            string expected = @"[
  {
    ""Number"": 0,
    ""Name"": ""My First Object"",
    ""intProp"": 5,
    ""stringProp"": ""Str1"",
    ""doubleProp"": ""35.1"",
    ""myChildObjects"": [
      {
        ""Number"": 0,
        ""Name"": null,
        ""childIntProp"": 1,
        ""childStringProp"": ""CStr1""
      },
      {
        ""Number"": 0,
        ""Name"": null,
        ""childIntProp"": 15,
        ""childStringProp"": ""CStr2""
      }
    ]
  },
  {
    ""Number"": 145,
    ""Name"": ""My second Object"",
    ""intProp"": 96,
    ""stringProp"": ""Str2"",
    ""doubleProp"": ""+Inf"",
    ""myChildObjects"": [
      {
        ""Number"": 0,
        ""Name"": null,
        ""childIntProp"": 62,
        ""childStringProp"": ""CStr3""
      }
    ]
  },
  {
    ""Number"": 261,
    ""Name"": ""My last Object"",
    ""intProp"": 9,
    ""stringProp"": ""Str45"",
    ""doubleProp"": ""1.6449635e+07"",
    ""myChildObjects"": [
      {
        ""Number"": 0,
        ""Name"": null,
        ""childIntProp"": -1,
        ""childStringProp"": ""CStr41""
      },
      {
        ""Number"": 0,
        ""Name"": null,
        ""childIntProp"": 72,
        ""childStringProp"": ""CStr42""
      },
      {
        ""Number"": 0,
        ""Name"": null,
        ""childIntProp"": 64,
        ""childStringProp"": ""CStr222""
      }
    ]
  }
]";
            using (var r = new ChoXmlReader<MyObject>("XmlFile5.xml")
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());

                var recs = r.ToArray();
                string actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Serializable]
        public class ChoEazyCopyPropertyReplacer : IChoKeyValuePropertyReplacer
        {
            public static readonly ChoEazyCopyPropertyReplacer Instance = new ChoEazyCopyPropertyReplacer();

            #region Instance Data Members (Private)

            private readonly Dictionary<string, string> _availPropeties = new Dictionary<string, string>()
            {
                { "SRC_DIR", "Source Directory." },
                { "DEST_DIR", "Destination Directory." },
            };

            #endregion Instance Data Members (Private)

            public string SourceDirectory { get; set; }
            public string DestinationDirectory { get; set; }

            #region IChoPropertyReplacer Members

            public bool ContainsProperty(string propertyName)
            {
                if (_availPropeties.ContainsKey(propertyName))
                    return true;

                return false;
            }

            public string ReplaceProperty(string propertyName, string format)
            {
                if (String.IsNullOrEmpty(propertyName))
                    return propertyName;

                switch (propertyName)
                {
                    case "SRC_DIR":
                        return SourceDirectory;
                    case "DEST_DIR":
                        return DestinationDirectory;
                    default:
                        return propertyName;
                }
            }

            #endregion

            #region IChoPropertyReplacer Members

            public IEnumerable<KeyValuePair<string, string>> AvailablePropeties
            {
                get
                {
                    foreach (KeyValuePair<string, string> keyValue in _availPropeties)
                        yield return keyValue;
                }
            }

            public string Name
            {
                get { return this.GetType().FullName; }
            }

            public string GetPropertyDescription(string propertyName)
            {
                if (_availPropeties.ContainsKey(propertyName))
                    return _availPropeties[propertyName];
                else
                    return null;
            }

            #endregion
        }

        [DataContract(Name = "Person")]
        [XmlRoot("Person", Namespace = "http://schemas.datacontract.org/2004/07/Workflows.MassTransit.Hosting.Serialization")]
        public sealed class PersonY
        {
            [DataMember]
            public string Name { get; set; }

            [DataMember]
            [XmlArray]
            [XmlArrayItem(Namespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays")]
            public List<int> Numbers { get; set; }
        }

        [Test]
        public static void Xml2JSON1()
        {
            string xml = @"<Person xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/Workflows.MassTransit.Hosting.Serialization"">
    <Name>Test</Name>
    <Numbers xmlns:d2p1=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
        <d2p1:int>1</d2p1:int>
        <d2p1:int>2</d2p1:int>
        <d2p1:int>3</d2p1:int>
    </Numbers>
</Person>";

            StringBuilder xml1 = new StringBuilder();
            using (var w = new ChoXmlWriter(xml1)
                    .WithXmlNamespace("", "http://schemas.datacontract.org/2004/07/Workflows.MassTransit.Hosting.Serialization")
                    .WithXmlNamespace("d2p1", "http://schemas.microsoft.com/2003/10/Serialization/Arrays")
                    .IgnoreRootName()
                    .UseXmlSerialization()
                )
            {
                using (var r = ChoXmlReader<PersonY>.LoadText(xml)
                    .WithXPath("//")
                    .UseXmlSerialization()
                    //.WithXmlNamespace("x", "http://schemas.datacontract.org/2004/07/Workflows.MassTransit.Hosting.Serialization")
                    //.WithXmlNamespace("x", "http://schemas.datacontract.org/2004/07/Workflows.MassTransit.Hosting.Serialization")
                    //.WithXmlNamespace("d2p1", "http://schemas.microsoft.com/2003/10/Serialization/Arrays")
                    //.Configure(c => c.DefaultNamespacePrefix = "x")
                    .WithField(f => f.Numbers, isArray: false)
                    )
                {
                    //w.Write(r);
                    r.Print();
                    //foreach (var rec in r)
                    //    Console.WriteLine(rec.Dump());
                }
            }

            Console.WriteLine(xml1.ToString());
        }

        [DataContract(Name = "Person")]
        //[XmlRoot(Namespace = "http://schemas.datacontract.org/2004/07/Workflows.MassTransit.Hosting.Serialization")]
        public sealed class PersonX
        {
            [DataMember]
            //[XmlElement(Namespace = "http://schemas.datacontract.org/2004/07/Workflows.MassTransit.Hosting.Serialization")]
            public string Name { get; set; }

            [DataMember]
            public List<int> Numbers { get; set; }
        }

        [Test]
        public static void Xml2JSON()
        {
            PersonX person = new PersonX
            {
                Name = "Test",
                Numbers = new List<int> { 1, 2, 3 }
            };

            StringBuilder xml = new StringBuilder();
            using (var w = new ChoXmlWriter<PersonX>(xml)
                .IgnoreRootName()
                )
            {
                w.Write(person);
            }

            Console.WriteLine(xml.ToString());

            using (var r = ChoXmlReader<PersonX>.LoadText(xml.ToString())
                .WithXPath("//")
                .WithField(f => f.Numbers, isArray: false)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                StringBuilder json = new StringBuilder();
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);

                Console.WriteLine(json.ToString());
                return;
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }

            //                string xml = @"<PersonX xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.datacontract.org/2004/07/Workflows.MassTransit.Hosting.Serialization"">
            //    <Name>Test</Name>
            //    <Numbers xmlns:d2p1=""http://schemas.microsoft.com/2003/10/Serialization/Arrays"">
            //        <d2p1:int>1</d2p1:int>
            //        <d2p1:int>2</d2p1:int>
            //        <d2p1:int>3</d2p1:int>
            //    </Numbers>
            //</PersonX>";

            //            using (var r = ChoXmlReader<PersonX>.LoadText(xml)
            //                .WithXPath("//")
            //                .WithXmlNamespace("d2p1", "http://schemas.microsoft.com/2003/10/Serialization/Arrays")
            //                .WithXmlNamespace("", "http://schemas.datacontract.org/2004/07/Workflows.MassTransit.Hosting.Serialization")
            //                .WithXmlNamespace("i", "http://www.w3.org/2001/XMLSchema-instance")
            //                )
            //            {
            //                foreach (var rec in r)
            //                    Console.WriteLine(rec.Dump());
            //            }
        }

        public class Emp1
        {
            public string Name { get; set; }
            [ChoXPath("State/@Description")]
            public string StateDescription { get; set; }
            public string State { get; set; }
        }
        [Test]
        public static void Xml2Json2()
        {
            string xml = @"<Employee>
    <Name>Mark</Name>
    <State>GA</State>
</Employee>";

            StringBuilder json = new StringBuilder();
            using (var r = ChoXmlReader<Emp1>.LoadText(xml)
                .WithXPath("//")
                )
            {
                using (var w = new ChoJSONWriter<Emp1>(json)
                    .SupportMultipleContent()
                    .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.None)
                    //.UseJsonSerialization()
                    )
                    w.Write(r.First());

                Console.WriteLine(json.ToString());
                return;

                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        [Test]
        public static void GenerateXmlFromDatatable()
        {
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <XElement>
    <href>/__API__/order/1</href>
    <order_id>1</order_id>
    <OrderNo>1</OrderNo>
    <ErpOrderNo></ErpOrderNo>
    <Customer_href>/__API__/customer/42</Customer_href>
    <Customer_text>1</Customer_text>
    <State>DENIED</State>
    <PaymentState>PAID</PaymentState>
    <PaymentIsCaptured>false</PaymentIsCaptured>
    <CaptureTime></CaptureTime>
    <PaymentIsCancelled>false</PaymentIsCancelled>
    <CancelTime></CancelTime>
    <CreatedTime>2018-11-06T10:00:00</CreatedTime>
    <ChangedTime>2019-05-06T08:45:30</ChangedTime>
    <SyncedTime></SyncedTime>
  </XElement>
  <XElement>
    <href>/__API__/order/2</href>
    <order_id>2</order_id>
    <OrderNo>2</OrderNo>
    <ErpOrderNo></ErpOrderNo>
    <Customer_href>/__API__/customer/42</Customer_href>
    <Customer_text>1</Customer_text>
    <State>DENIED</State>
    <PaymentState></PaymentState>
    <PaymentIsCaptured>false</PaymentIsCaptured>
    <CaptureTime></CaptureTime>
    <PaymentIsCancelled>false</PaymentIsCancelled>
    <CancelTime></CancelTime>
    <CreatedTime>2018-11-06T10:49:47</CreatedTime>
    <ChangedTime>2019-05-06T08:45:30</ChangedTime>
    <SyncedTime></SyncedTime>
  </XElement>
</Root>";
            using (var r = new ChoXmlReader("sample92.xml")
                .WithMaxScanNodes(2)
                .WithXPath("Order")
                .Configure(c => c.IgnoreRootDictionaryFieldPrefix = true)
                )
            {
                //var recs = r.ToArray();
                var dt = r.AsDataTable();
                JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.Indented).Print();

                StringBuilder xml = new StringBuilder();
                using (var w = new ChoXmlWriter(xml))
                {
                    w.Write(dt);
                }

                var actual = xml.ToString();
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void TransformXml()
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
            [XmlElement("Gebied-NLD", Namespace = "http://www.kadaster.nl/schemas/lvbag/extract-selecties/v20200601")]
            public string GebiedNLD { get; set; }
        }

        [Test]
        public static void ReadAllNS()
        {
            string expectedNS = @"{
  ""xml"": ""http://www.w3.org/XML/1998/namespace"",
  ""Objecten-ref"": ""www.kadaster.nl/schemas/lvbag/imbag/objecten-ref/v20200601"",
  ""Objecten"": ""www.kadaster.nl/schemas/lvbag/imbag/objecten/v20200601"",
  ""Historie"": ""www.kadaster.nl/schemas/lvbag/imbag/historie/v20200601"",
  ""DatatypenNEN3610"": ""www.kadaster.nl/schemas/lvbag/imbag/datatypennen3610/v20200601"",
  ""KenmerkInOnderzoek"": ""www.kadaster.nl/schemas/lvbag/imbag/kenmerkinonderzoek/v20200601"",
  ""nen5825"": ""www.kadaster.nl/schemas/lvbag/imbag/nen5825/v20200601"",
  ""mlm"": ""http://www.kadaster.nl/schemas/lvbag/extract-deelbestand-mutaties-lvc/v20200601"",
  ""selecties-extract"": ""http://www.kadaster.nl/schemas/lvbag/extract-selecties/v20200601"",
  ""gml"": ""http://www.opengis.net/gml/3.2"",
  ""ml"": ""http://www.kadaster.nl/schemas/mutatielevering-generiek/1.0"",
  ""xsi"": ""http://www.w3.org/2001/XMLSchema-instance""
}";
            IDictionary<string, string> ns = ChoXmlReader.GetXmlNamespacesInScope("sample95.xml");
            ns.Print();

            var actualNS = JsonConvert.SerializeObject(ns, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expectedNS, actualNS);

            string expected = @"[
  {
    ""GebiedRegistratief"": {
      ""GebiedNLD"": ""Test""
    }
  }
]";

            List<object> recs = new List<object>();
            using (var r = new ChoXmlReader<bagInfo>("sample95.xml")
                .UseXmlSerialization()
                //.WithXmlNamespaces(ns)
                .WithXPath("mlm:bagInfo")
                )
            {
                foreach (var rec in r)
                {
                    ns = r.Configuration.GetXmlNamespacesInScope();
                    Console.WriteLine(r.Configuration.GetXmlNamespacesInScope().Dump());
                    Console.WriteLine(rec.Dump());

                    recs.Add(rec);
                }
            }

            var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);

            string expected2 = @"<?xml version=""1.2"" encoding=""utf-8""?>
<mlm:bagMutaties xmlns=""http://www.kadaster.nl/schemas/lvbag/extract-deelbestand-mutaties-lvc/v20200601"" xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <mlm:bagInfo />
</mlm:bagMutaties>";

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
            var actual2 = xml.ToString();
            Assert.AreEqual(expected2, actual2);
        }

        public class Root
        {
            [ChoXmlNodeRecordField(XPath = "//property1")]
            public string[] Properties { get; set; }
            [ChoXmlNodeRecordField(XPath = "//amount/*")]
            public double[] Amounts { get; set; }
        }

        [Test]
        public static void Test100()
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
            [ChoXmlNodeRecordField(XPath = "/Document/*/text()")]
            public string[] Document { get; set; }
        }

        [Test]
        public static void ElementsToArray()
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

            string expected = @"Header,Document
2020-03-20T09:08:29Z,""Test Data 123,Test Date 456""";
            using (var r = ChoXmlReader<ABCX>.LoadText(xml)
                //.WithXPath("/")
                //.WithField("Date", xPath: "/Header/Date")
                //.WithField("Code", xPath: "/Header/Code")
                //.WithField("Document", xPath: "/Document", fieldType: typeof(string[]))
                )
            {
                var recs = r.ToArray();
                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                StringBuilder csv = new StringBuilder();
                using (var w = new ChoCSVWriter<ABCX>(csv).WithFirstLineHeader())
                {
                    w.Write(recs);
                }

                var actual = csv.ToString(); //. JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }


        [Test]
        public static void ElementsToArrayDynamic()
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

            string expected = @"Header,Document
2020-03-20T09:08:29Z,""Test Data 123,Test Date 456""";
            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("/")
                .WithField("Header", xPath: "/Header/Date")
                .WithField("Document", xPath: "/Document/*/text()", fieldType: typeof(string[]))
                )
            {
                var recs = r.ToArray();
                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                StringBuilder csv = new StringBuilder();
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .UseNestedKeyFormat(false)
                    )
                {
                    w.Write(recs);
                }

                var actual = csv.ToString(); //. JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Item
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; }
            public int? Number { get; set; }
        }

        [Test]
        public static void MemoryTest()
        {
            Assert.Ignore();
            return;
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

        [Test]
        public static void Soap2JSONTest()
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

            string expected = @"[
  {
    ""Header"": {
      ""reportname"": ""ReportName"",
      ""reportstartdate"": ""2020-Jun-1"",
      ""reportenddate"": ""2020-Jun-1""
    },
    ""Body"": {
      ""reportresponse"": {
        ""row"": [
          {
            ""rowid"": ""1"",
            ""value1"": ""1"",
            ""value2"": ""1"",
            ""value3"": ""1""
          }
        ]
      }
    }
  }
]";

            StringBuilder json = new StringBuilder();
            using (var r = ChoXmlReader.LoadText(soap)
                .WithXmlNamespace("SOAP-ENV", "http://schemas.xmlsoap.org/soap/envelope/")
                .WithXPath("//SOAP-ENV:Envelope")
                .Configure(c => c.IgnoreNSPrefix = true)
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

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void LoadXmlFragmentTest()
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
            string expected = @"[
  {
    ""Id"": 10,
    ""Salary"": 2000.0
  },
  {
    ""Id"": 20,
    ""Salary"": 10000.0
  }
]";
            using (var r = ChoXmlReader.LoadxmlFragment(xml)
                .WithMaxScanNodes(10)
                )
            {
                var recs = r.ToArray();
                //r.Print();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

        }

        [Test]
        public static void CurrencyDynamicTest()
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
            string expected = @"[
  {
    ""Id"": 10,
    ""Salary"": 2000.0
  },
  {
    ""Id"": 20,
    ""Salary"": 10000.0
  }
]";

            using (var r = ChoXmlReader.LoadText(xml)
                .WithMaxScanNodes(10)
                .Configure(c => c.FlattenNode = true)
                )
            {
                var recs = r.ToArray();
                foreach (var rec in recs)
                {
                    Console.WriteLine(rec.Dump());
                }

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void CurrencyTest()
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
            string expected = @"[
  {
    ""Id"": 10,
    ""Salary"": 2000.0
  },
  {
    ""Id"": 20,
    ""Salary"": 10000.0
  }
]";

            using (var r = ChoXmlReader<EmpWithCurrency>.LoadText(xml)
                .WithMaxScanNodes(10)
                )
            {
                var recs = r.ToArray();
                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void XNameWithSpaceTest()
        {
            Assert.Ignore();
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

        [Test]
        public static void DefaultValueTest()
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
            string expected = @"[
  {
    ""Id"": 10,
    ""Name"": ""Tom""
  },
  {
    ""Id"": 20,
    ""Name"": ""Markx""
  }
]";
            using (var r = ChoXmlReader.LoadText(xml)
                .WithField("Id", fieldType: typeof(int))
                .WithField("Name", fieldType: typeof(string), defaultValue: "Markx")
                .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Any)
                )
            {
                var recs = r.ToArray();
                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void FallbacktValueTest()
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
            string expected = @"[
  {
    ""Id"": 10,
    ""Name"": ""Tom""
  },
  {
    ""Id"": 200,
    ""Name"": ""Mark""
  }
]";
            using (var r = ChoXmlReader.LoadText(xml)
                .WithField("Id", fieldType: typeof(int), fallbackValue: 200)
                .WithField("Name", fieldType: typeof(string), defaultValue: "Markx")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                var recs = r.ToArray();
                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void HugeXml2Json()
        {
            string expected = @"[
  {
    ""direction"": ""d1"",
    ""companyId"": ""c1"",
    ""nameId"": ""n1""
  },
  {
    ""direction"": ""d2"",
    ""companyId"": ""c2"",
    ""nameId"": ""n2""
  }
]";

            StringBuilder json = new StringBuilder();
            using (var r = new ChoXmlReader("sample94.xml")
                .WithXPath("/root/hugeArray/item")
                )
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }

            Console.WriteLine(json.ToString());

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Xml2JSON2()
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
                var recs = r.ToArray();

                var dt = recs.AsDataTable();

                //Console.WriteLine(r.First().layer.data.GetText());
                using (var w = new ChoJSONWriter(json))
                {
                    w.Write(recs);
                }
            }
            Console.WriteLine(json.ToString());
        }

        [Test]
        public static void ToKVPTest()
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

        [Test]
        public static void ExtractTextFromXml()
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
                .WithXPath("//nc:IdentificationID/text()", true)
                )
            {
                var recs = r.Select(kvp => kvp.Value).ToArray();
                Console.WriteLine(recs.Dump());

                CollectionAssert.AreEqual(recs, new string[] { "https://Test1.com", "https://Test2.com" });
            }
        }

        [Test]
        public static void FilterNodesTest()
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

            string expected = @"[
  {
    ""ProductName"": [
      ""Car"",
      ""lorry"",
      ""Car""
    ]
  }
]";
            using (var r = ChoXmlReader.LoadText(xml)
                .WithXPath("//PRODUCTDETAILS")
                )
            {
                List<object> recs = new List<object>();
                foreach (var rec in r)
                {
                    if (((IList)rec.ProductName).Contains("Car"))
                        recs.Add(rec);
                }
                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void Sample93Test()
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

        [Test]
        public static void LoadSoapXmlTest()
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

            string expected = @"[
  {
    ""soap:listdata"": {
      ""tmp:Name"": ""00141169"",
      ""tmp:CurrencyCode"": ""EUR"",
      ""tmp:Date"": ""2020-04-03""
    }
  }
]";
            using (var r = ChoXmlReader.LoadText(xml)
                .Configure(c => c.NamespaceManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/"))
                .Configure(c => c.NamespaceManager.AddNamespace("tmp", "http://tempuri.org/"))
                .Configure(c => c.RootName = "soap:Envelope")
                .Configure(c => c.NodeName = "Body")
                //.Configure(c => c.DefaultNamespacePrefix = "tmp")
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var recs = r.ToArray();
                //r.Print();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void TestSample91()
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

        [Test]
        public static void TestSample92()
        {
            string expected = @"href,order_id,OrderNo,ErpOrderNo,Customer_href,Customer_ID,State,PaymentState,PaymentIsCaptured,CaptureTime,PaymentIsCancelled,CancelTime,CreatedTime,ChangedTime,SyncedTime
/__API__/order/1,1,1,,/__API__/customer/42,1,DENIED,PAID,false,,false,,2018-11-06T15:00:00Z,2019-05-06T12:45:30Z,
/__API__/order/2,2,2,,/__API__/customer/42,1,DENIED,,false,,false,,2018-11-06T15:49:47Z,2019-05-06T12:45:30Z,";

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

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void PartialLoadTest()
        {
            using (var r = new ChoXmlReader<Car>("sample49.xml")
                .WithXPath("/Car")
                )
            {
                var recs = r.ToArray();

                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                Assert.AreEqual(recs[0].StockNumber, "1020");
                Assert.AreEqual(recs[0].Make, "Renault");

            }
        }

        [Test]
        public static void XmlRead1()
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
            string expected = @"[
  {
    ""IdAffidamento"": ""2325"",
    ""Praticas"": [
      {
        ""IdPratica"": ""0010193043084620""
      },
      {
        ""IdPratica"": ""0010193043084611""
      }
    ]
  },
  {
    ""IdAffidamento"": ""2325"",
    ""Praticas"": [
      {
        ""IdPratica"": ""0010193043084621""
      }
    ]
  }
]";
            using (var r = ChoXmlReader.LoadText(csv)
                .WithField("IdAffidamento")
                .WithField("Pratica", isArray: true)
                )
            {
                var recs = r.ToArray();
                foreach (var e in recs)
                {
                    Console.WriteLine(e.IdAffidamento);
                    foreach (var Pratica in e.Praticas)
                        Console.WriteLine(Pratica.IdPratica);
                }

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void Xml2JSONWithTabs()
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

        [XmlRoot("ButikOmbud")]
        public class StoreAssortmentViewModel : AssortmentViewModel
        {

        }
        [XmlRoot("ButikOmbud")]
        public class AgentAssortmentViewModel : AssortmentViewModel
        {

        }

        [Test]
        public static void XmlTypeTest()
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

            string expected = @"<ButikerOmbud xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <ButikOmbud xsi:Type=""StoreAssortmentViewModel"">
    <Typ>Butik</Typ>
    <Nr>2515</Nr>
  </ButikOmbud>
  <ButikOmbud xsi:Type=""StoreAssortmentViewModel"">
    <Typ>Butik</Typ>
    <Nr>2516</Nr>
  </ButikOmbud>
  <ButikOmbud xsi:Type=""AgentAssortmentViewModel"">
    <Typ>Ombud</Typ>
    <Nr>011703-91A</Nr>
  </ButikOmbud>
  <ButikOmbud xsi:Type=""AgentAssortmentViewModel"">
    <Typ>Ombud</Typ>
    <Nr>011703-92B</Nr>
  </ButikOmbud>
</ButikerOmbud>";


            StringBuilder output = new StringBuilder();
            using (var w = new ChoXmlWriter<AssortmentViewModel>(output)
                    .Configure(c => c.UseXmlSerialization = true)
                    .WithRootName("ButikerOmbud")
                    .WithXmlNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
                    .WithXmlNamespace("xsd", "http://www.w3.org/2001/XMLSchema")
                    .Configure(c => c.EmitDataType = true)

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

            var actual = output.ToString();
            Console.WriteLine(actual);

            Assert.AreEqual(expected, actual);
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

        [Test]
        public static void Xml2CSV2()
        {
            string expected = @"CentreName,Country,CustomerId,DOB,Email,ExpiryDate
Corporate Office,Austria,379,25/02/1991,farah@gmail.com,03/01/2020 08:01
Corporate Office,Egypt,988915,01/03/1986,hesh.a.metwally@gmail.com,07/01/2020 11:38";
            string actual = null;

            StringBuilder sb = new StringBuilder();

            using (var r = new ChoXmlReader(FileNameSample22XML)
                .WithXPath("b:MarketingAllCardholderData")
                .WithXmlNamespace("a", "schemas.datacontract.org/2004/07/ExternalClient.Responses")
                .WithXmlNamespace("b", "schemas.datacontract.org/2004/07/ExternalClient.Data.Classes")
                .Configure(c => c.IgnoreNSPrefix = true)
                )
            {
                var recs = r.ToArray();

                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    .Configure(c => c.UseNestedKeyFormat = false)
                    )
                    w.Write(recs);
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Xml2CSV2_1()
        {
            string expected = @"CentreName,Country,CustomerId,DOB,Email,ExpiryDate
Corporate Office,Austria,379,25/02/1991,farah@gmail.com,2020-03-01T08:01:00.0000000
Corporate Office,Egypt,988915,01/03/1986,hesh.a.metwally@gmail.com,2020-07-01T11:38:00.0000000";
            string actual = null;

            StringBuilder sb = new StringBuilder();

            using (var r = new ChoXmlReader(FileNameSample22XML)
                .WithXPath("b:MarketingAllCardholderData")
                .WithXmlNamespace("a", "schemas.datacontract.org/2004/07/ExternalClient.Responses")
                .WithXmlNamespace("b", "schemas.datacontract.org/2004/07/ExternalClient.Data.Classes")
                .Configure(c => c.IgnoreNSPrefix = true)
                .WithMaxScanNodes(1)
                )
            {
                var recs = r.ToArray();

                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    .Configure(c => c.UseNestedKeyFormat = false)
                    .TypeConverterFormatSpec(ts => ts.DateTimeFormat = "o")
                    )
                    w.Write(recs);
            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
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
                var recs = p.SelectMany(r =>
                        ((dynamic[])r.AnotherRandomLOne.ARandomLTwo.ARandomLTree.ARandomLFour).Select(r1 => new
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
                        })).ToArray();

                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.NestedKeySeparator = '-')
                    )
                    w.Write(recs);
            }
            actual = csv.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample50Test()
        {
            string expected = @"targetMarketAttributes/targetMarket,targetMarketAttributes/alternateItemIdentificationList/0/alternateItemIdentification/agency,targetMarketAttributes/alternateItemIdentificationList/0/alternateItemIdentification/id,targetMarketAttributes/alternateItemIdentificationList/1/alternateItemIdentification/agency,targetMarketAttributes/alternateItemIdentificationList/1/alternateItemIdentification/id,targetMarketAttributes/shortDescriptionList/shortDescription/lang,targetMarketAttributes/shortDescriptionList/shortDescription/#text,targetMarketAttributes/productDescriptionList/productDescription/lang,targetMarketAttributes/productDescriptionList/productDescription/#text,targetMarketAttributes/additionalDescriptionList/additionalDescription/lang,targetMarketAttributes/additionalDescriptionList/additionalDescription/#text,targetMarketAttributes/isDispatchUnitList/isDispatchUnit,targetMarketAttributes/isInvoiceUnitList/isInvoiceUnit,targetMarketAttributes/isOrderableUnitList/isOrderableUnit,targetMarketAttributes/packagingMarkedReturnable,targetMarketAttributes/minimumTradeItemLifespanFromProductionList/minimumTradeItemLifespanFromProduction,targetMarketAttributes/nonGTINPalletHi,targetMarketAttributes/nonGTINPalletTi,targetMarketAttributes/numberOfItemsPerPallet,targetMarketAttributes/hasBatchNumber,targetMarketAttributes/productMarkedRecyclable,targetMarketAttributes/depth/uom,targetMarketAttributes/depth/#text,targetMarketAttributes/height/uom,targetMarketAttributes/height/#text,targetMarketAttributes/width/uom,targetMarketAttributes/width/#text,targetMarketAttributes/grossWeight/uom,targetMarketAttributes/grossWeight/#text,targetMarketAttributes/netWeight/uom,targetMarketAttributes/netWeight/#text,targetMarketAttributes/totalUnitsPerCase,targetMarketAttributes/preDefinedFlex/alternateClassificationList/0/alternateClassification/code,targetMarketAttributes/preDefinedFlex/alternateClassificationList/0/alternateClassification/scheme,targetMarketAttributes/preDefinedFlex/alternateClassificationList/1/alternateClassification/code,targetMarketAttributes/preDefinedFlex/alternateClassificationList/1/alternateClassification/scheme,targetMarketAttributes/preDefinedFlex/alternateClassificationList/2/alternateClassification/code,targetMarketAttributes/preDefinedFlex/alternateClassificationList/2/alternateClassification/scheme,targetMarketAttributes/preDefinedFlex/alternateClassificationList/3/alternateClassification/code,targetMarketAttributes/preDefinedFlex/alternateClassificationList/3/alternateClassification/scheme,targetMarketAttributes/preDefinedFlex/alternateClassificationList/4/alternateClassification/code,targetMarketAttributes/preDefinedFlex/alternateClassificationList/4/alternateClassification/scheme,targetMarketAttributes/preDefinedFlex/brandOwnerAdditionalTradeItemIdentificationList/brandOwnerAdditionalTradeItemIdentification/brandOwnerAdditionalIdType,targetMarketAttributes/preDefinedFlex/brandOwnerAdditionalTradeItemIdentificationList/brandOwnerAdditionalTradeItemIdentification/brandOwnerAdditionalIdValue,targetMarketAttributes/preDefinedFlex/consumerSalesConditionList/consumerSalesCondition,targetMarketAttributes/preDefinedFlex/countryOfOriginList/countryOfOrigin,targetMarketAttributes/preDefinedFlex/dataCarrierList/dataCarrierTypeCode,targetMarketAttributes/preDefinedFlex/donationIdentificationNumberMarked,targetMarketAttributes/preDefinedFlex/doesTradeItemContainLatex,targetMarketAttributes/preDefinedFlex/exemptFromFDAPreMarketAuthorization,targetMarketAttributes/preDefinedFlex/fDA510KPremarketAuthorization,targetMarketAttributes/preDefinedFlex/fDAMedicalDeviceListingList/fDAMedicalDeviceListing,targetMarketAttributes/preDefinedFlex/gs1TradeItemIdentificationKey/code,targetMarketAttributes/preDefinedFlex/gs1TradeItemIdentificationKey/value,targetMarketAttributes/preDefinedFlex/isTradeItemManagedByManufactureDate,targetMarketAttributes/preDefinedFlex/manufacturerList/manufacturer/gln,targetMarketAttributes/preDefinedFlex/manufacturerDeclaredReusabilityType,targetMarketAttributes/preDefinedFlex/mRICompatibilityCode,targetMarketAttributes/preDefinedFlex/serialNumberLocationCodeList/serialNumberLocationCode,targetMarketAttributes/preDefinedFlex/tradeChannelList/tradeChannel,targetMarketAttributes/preDefinedFlex/tradeItemContactInfoList/tradeItemContactInfo/availableTime/lang,targetMarketAttributes/preDefinedFlex/tradeItemContactInfoList/tradeItemContactInfo/availableTime/#text,targetMarketAttributes/preDefinedFlex/tradeItemContactInfoList/tradeItemContactInfo/contactInfoGLN,targetMarketAttributes/preDefinedFlex/tradeItemContactInfoList/tradeItemContactInfo/contactType,targetMarketAttributes/preDefinedFlex/tradeItemContactInfoList/tradeItemContactInfo/targetMarketCommunicationChannel/communicationChannelList/communicationChannel/communicationChannelCode,targetMarketAttributes/preDefinedFlex/uDIDDeviceCount
US,Example,31321,Example,1,en,Example,en,Example,en,Example,No,No,No,No,1825,0,0,0,Yes,No,in,12,in,8,in,12,lb,0.3213,lb,0.3213,1,Example,Example,Example,Example,Example,Example,Example,Example,Example,Example,Example,Example,FALSE,US,Example,No,No,No,Example,Example,Example,14,true,0100000000000,SINGLE_USE,UNSPECIFIED,NOT_MARKED,Example,en,2019-02-08T00:00:00,0000000000002,ABC,TELEPHONE,1";
            string actual = null;

            StringBuilder msg = new StringBuilder();
            using (var p = new ChoXmlReader(FileNameSample50XML)
                .WithXPath("//targetMarketAttributes")
                .WithMaxScanNodes(10)
                )
            {
                var recs = p.ToArray();

                using (var w = new ChoCSVWriter(msg)
                    .WithFirstLineHeader()
                    .Configure(c => c.UseNestedKeyFormat = true)
                    .Configure(c => c.NestedKeySeparator = '/')
                    .Configure(c => c.ThrowAndStopOnMissingField = false)
                    )
                    w.Write(recs);
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

        [Test]
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

        [Test]
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
            using (var parser = ChoXmlReader<SyncInvoice>.LoadText(xml)
                .WithXPath("SyncInvoice")
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

        [Test]
        public static void DefaultNSTest()
        {
            string xml = @"<SyncInvoice xmlns=""http://schema.infor.com/InforOAGIS/2"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xsi:schemaLocation=""SyncInvoice.xsd"" languageCode=""IT"" />";

            string expected = @"[
  {
    ""LanguageCode"": ""IT"",
    ""ApplicationArea"": null
  }
]";
            using (var parser = ChoXmlReader<SyncInvoice>.LoadText(xml)
                .WithXPath("SyncInvoice")
                .WithXmlNamespace("x", "http://schema.infor.com/InforOAGIS/2")
                      )
            {
                var recs = parser.ToArray();

                foreach (var rec in recs)
                    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
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

        [Test]
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
        [Test]
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
        [Test]
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
                  .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                p.Print();
                return;
                using (var w = new ChoCSVWriter(Console.Out)
                    .WithFirstLineHeader()
                    )
                    w.Write(p);
            }

        }

        [Test]
        public static void CSVToXmlTest()
        {
            string expected = @"<Employees xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
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

        [Test]
        public static void MultipleXmlNS()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"lat",(double)25.0312615000 },{"lon",(double)121.3505846635 } },
                new ChoDynamicObject{{"lat",(double)25.0312520284 },{"lon",(double)121.3505897764 } },
                new ChoDynamicObject{{"lat",(double)25.0312457420 },{"lon",(double)121.3506018464 } },
                new ChoDynamicObject{{"lat",(double)25.0312426407 },{"lon",(double)121.3506035227 } },
            };
            List<object> recs = new List<object>();

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

            string expected1 = @"[
  {
    ""lat"": ""25.0312615000"",
    ""lon"": ""121.3505846635""
  },
  {
    ""lat"": ""25.0312520284"",
    ""lon"": ""121.3505897764""
  },
  {
    ""lat"": ""25.0312457420"",
    ""lon"": ""121.3506018464""
  },
  {
    ""lat"": ""25.0312426407"",
    ""lon"": ""121.3506035227""
  }
]";
            foreach (var rec in ChoXmlReader.LoadText(xml)
                .WithXPath("./trk/trkseg/trkpt")
                .WithField("lat")
                .WithField("lon")
                )
                recs.Add(rec);

            var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected1, actual);
            //CollectionAssert.AreEqual(expected, recs);
        }

        [Test]
        public static void XmlToJSON1_1()
        {
            string expected = @"[
  {
    ""@id"": ""1"",
    ""Name"": ""Mark"",
    ""Age"": ""35"",
    ""Gender"": ""Male"",
    ""DateOfBirth"": ""05-30-1980"",
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
    ""@id"": ""2"",
    ""Name"": ""Tom"",
    ""Age"": ""21"",
    ""Gender"": ""Female"",
    ""DateOfBirth"": ""01-01-2000"",
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

            var recs = ChoXmlReader.LoadText(xml, new ChoXmlRecordConfiguration()
                .WithXmlNamespace("http://www.medrad.com/ContrastDoseReport")).ToArray();

            actual = ChoJSONWriter.ToTextAll(recs,
                new ChoJSONRecordConfiguration().Configure(c => c.EnableXmlAttributePrefix = true));

            Assert.AreEqual(expected, actual);
        }

        [Test]
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

            actual = ChoXmlWriter.ToTextAll(ChoJSONReader.LoadText(json),
                new ChoXmlRecordConfiguration()
                .Configure(c => c.RootName = "ContrastDoseReport")
                .Configure(c => c.NodeName = "Patient")
                .Configure(c => c.DoNotEmitXmlNamespace = true)
                );

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

        [Test]
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

        [Test]
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

        [Test]
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

        [Test]
        public static void XmlToJSONKVP()
        {
            string expected = @"<Objects xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <Object>81963</Object>
  <Object>complete</Object>
  <Object>2018-07-30</Object>
  <Object>81194</Object>
  <Object>complete</Object>
  <Object>2018-07-30</Object>
</Objects>";

            string expected1 = @"<List`1s>
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

                var recs = p.ToArray();
                var recs1 = recs.Select(r => ((IList<dynamic>)r.propertiess).Select(r1 => r1.value).ToList()).ToArray();
                actual = ChoXmlWriter.ToTextAll(recs1);

            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
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

        [Test]
        public static void Sample48Test()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add("Type");
            expected.Columns.Add("Indice");
            expected.Columns.Add("Limites_Haut");
            expected.Columns.Add("Limites_Bas");
            expected.Columns.Add("Points_0_Point_id");
            expected.Columns.Add("Points_0_Point_X");
            expected.Columns.Add("Points_0_Point_Y");
            expected.Columns.Add("Points_0_Point_#text");
            expected.Columns.Add("Points_1_Point_id");
            expected.Columns.Add("Points_1_Point_X");
            expected.Columns.Add("Points_1_Point_Y");
            expected.Columns.Add("Points_1_Point_#text");
            expected.Columns.Add("Points_2_Point_id");
            expected.Columns.Add("Points_2_Point_X");
            expected.Columns.Add("Points_2_Point_Y");
            expected.Columns.Add("Points_2_Point_#text");
            expected.Rows.Add("Point", "859", "26.5", "43.2", "01", "45", "44", "12", "02", "5", "41", "5", "03", "4", "464", "3");
            expected.Rows.Add("Point", "256", "16.5", "12.2", "05", "6.5", "22", "5", "06", "58", "46.5", "5", "07", "98", "4.5", "6");

            var actual = new ChoXmlReader(FileNameSample48XML)
                .WithXPath("//Contour/Elements/Element")
                .Configure(c => c.TurnOffPluralization = false)
                .OfType<ChoDynamicObject>()
                .Select(i => i.Flatten(ignoreDictionaryFieldPrefix: false))
                .AsDataTable();

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            DataTableAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample48Test_1()
        {

            string expected = @"[
  {
    ""Type"": ""Point"",
    ""Indice"": 859,
    ""Limites_Haut"": ""26.5"",
    ""Limites_Bas"": ""43.2"",
    ""Points_0_Point_id"": ""01"",
    ""Points_0_Point_X"": ""45"",
    ""Points_0_Point_Y"": ""44"",
    ""Points_0_Point_#text"": ""12"",
    ""Points_1_Point_id"": ""02"",
    ""Points_1_Point_X"": ""5"",
    ""Points_1_Point_Y"": ""41"",
    ""Points_1_Point_#text"": ""5"",
    ""Points_2_Point_id"": ""03"",
    ""Points_2_Point_X"": ""4"",
    ""Points_2_Point_Y"": ""464"",
    ""Points_2_Point_#text"": ""3""
  },
  {
    ""Type"": ""Point"",
    ""Indice"": 256,
    ""Limites_Haut"": ""16.5"",
    ""Limites_Bas"": ""12.2"",
    ""Points_0_Point_id"": ""05"",
    ""Points_0_Point_X"": ""6.5"",
    ""Points_0_Point_Y"": ""22"",
    ""Points_0_Point_#text"": ""5"",
    ""Points_1_Point_id"": ""06"",
    ""Points_1_Point_X"": ""58"",
    ""Points_1_Point_Y"": ""46.5"",
    ""Points_1_Point_#text"": ""5"",
    ""Points_2_Point_id"": ""07"",
    ""Points_2_Point_X"": ""98"",
    ""Points_2_Point_Y"": ""4.5"",
    ""Points_2_Point_#text"": ""6""
  }
]";
            var actual = new ChoXmlReader(FileNameSample48XML)
                .WithXPath("//Contour/Elements/Element")
                .Configure(c => c.TurnOffPluralization = false)
                .WithMaxScanNodes(1)
                .OfType<ChoDynamicObject>()
                .Select(i => i.Flatten(ignoreRootDictionaryFieldPrefix: true, ignoreDictionaryFieldPrefix: false))
                .AsDataTable();

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);

            //DataTableAssert.AreEqual(expected, actual);
            Assert.AreEqual(expected, actualJson);
        }
        [Test]
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

        [Test]
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

        [Test]
        public static void Sample22Test()
        {
            //DataTable expected = new DataTable();
            //expected.Columns.Add("Age", typeof(long)).AllowDBNull = false;
            //expected.Columns.Add("DateOfBirth");
            //expected.Columns.Add("EmailAddress");
            //expected.Columns.Add("MobilePhone_CountryCode");
            //expected.Columns.Add("MobilePhone_Number");
            //expected.Columns.Add("WorkPhone_CountryCode");
            //expected.Columns.Add("WorkPhone_Number");
            //expected.Rows.Add((long)39, "06:07:1985:00:00", "abc@rentacar3.com", "1", "2049515487", "93", "1921525542");
            //expected.Rows.Add((long)29, "06:07:1989:00:00", "abc@rentacar2.com", "1", "2049515949", "93", "1921525125");

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

            string expected = @"[
  {
    ""Age"": ""39"",
    ""DateOfBirth"": ""06:07:1985:00:00"",
    ""EmailAddress"": ""abc@rentacar3.com"",
    ""MobilePhone_CountryCode"": ""1"",
    ""MobilePhone_Number"": ""2049515487"",
    ""WorkPhone_CountryCode"": ""93"",
    ""WorkPhone_Number"": ""1921525542""
  },
  {
    ""Age"": ""29"",
    ""DateOfBirth"": ""06:07:1989:00:00"",
    ""EmailAddress"": ""abc@rentacar2.com"",
    ""MobilePhone_CountryCode"": ""1"",
    ""MobilePhone_Number"": ""2049515949"",
    ""WorkPhone_CountryCode"": ""93"",
    ""WorkPhone_Number"": ""1921525125""
  }
]";
            using (var p = ChoXmlReader.LoadText(xml))
            {
                actual = p.Select(e => e.Flatten()).AsDataTable();

                var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actualJson);
            }

            //DataTableAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample21Test()
        {
            //DataTable expected = new DataTable();
            //expected.Columns.Add("Key", typeof(object)).AllowDBNull = false;
            //expected.Columns.Add("Value");
            //expected.Rows.Add("Key1", "79,0441326460292");
            //expected.Rows.Add("Key1", "76,0959542079328");
            //expected.Rows.Add("Key1", "74,3061819154758");
            //expected.Rows.Add("Key1", "78,687039788779");
            //expected.Rows.Add("Key2", "87,7110395931923");

            DataTable actual = null;
            string expected = @"[
  {
    ""Key"": ""Key1"",
    ""Value"": ""79,0441326460292""
  },
  {
    ""Key"": ""Key1"",
    ""Value"": ""76,0959542079328""
  },
  {
    ""Key"": ""Key1"",
    ""Value"": ""74,3061819154758""
  },
  {
    ""Key"": ""Key1"",
    ""Value"": ""78,687039788779""
  },
  {
    ""Key"": ""Key2"",
    ""Value"": ""87,7110395931923""
  }
]";
            using (var p = new ChoXmlReader(FileNameSample21XML)
                .WithField("Key")
                .WithField("Value", xPath: "/Values/string", isArray: true)
                .Configure(c => c.TurnOffPluralization = true)
                )
            {
                actual = p.SelectMany(r => ((Array)r.Value).OfType<string>().Select(r1 => new { Key = r.Key, Value = r1 })).AsDataTable();

                var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actualJson);
            }

            //DataTableAssert.AreEqual(expected, actual);
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

        [Test]
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

        [Test]
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

            string expected = @"[
  {
    ""ns3:fname"": ""mark"",
    ""ns3:lname"": ""joye"",
    ""ns3:CarCompany"": ""saab"",
    ""ns3:CarNumber"": ""9741"",
    ""ns3:IsInsured"": ""true"",
    ""ns3:safties"": null,
    ""ns3:CarDescription"": ""test Car"",
    ""ns3:collections"": {
      ""ns3:collection"": {
        ""ns3:XYZ"": ""1"",
        ""ns3:PQR"": ""11"",
        ""ns3:contactdetails"": [
          {
            ""ns3:contname"": ""DOM"",
            ""ns3:contnumber"": ""8787""
          },
          {
            ""ns3:contname"": ""COM"",
            ""ns3:contnumber"": ""4564"",
            ""ns3:addtionaldetails"": {
              ""ns3:addtionaldetail"": {
                ""ns3:description"": ""54657667""
              }
            }
          },
          {
            ""ns3:contname"": ""gf"",
            ""ns3:contnumber"": ""123"",
            ""ns3:addtionaldetails"": {
              ""ns3:addtionaldetail"": {
                ""ns3:description"": ""123""
              }
            }
          }
        ]
      }
    }
  }
]";
            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("//")
                .WithXmlNamespace("ns3", "http://www.CCKS.org/XRT/Form")
                )
            {
                var recs = p.ToArray();

                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                //Assert.AreEqual(expected, actual);
                //return;

                using (var w = new ChoJSONWriter(sb)
                    .Configure(c => c.SupportMultipleContent = false)
                    .Configure(c => c.KeepNSPrefix = true)
                    )
                {
                    w.Write(recs); //.Select(e => e.AddNamespace("ns3", "http://www.CCKS.org/XRT/Form")));
                }
            }

            Console.WriteLine(sb.ToString());
            var actual1 = sb.ToString();
            Assert.AreEqual(expected, actual1);
        }

        [Test]
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

            string xml = @"<CUST>
	<First_Name>Luke</First_Name>
	<Last_Name>Skywalker</Last_Name>
	<ID ID1=""1"">
		<Name><![CDATA[1234]]></Name>
	</ID>
</CUST>";

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

        [Test]
        public static void Sample46()
        {
            string expected = @"overallResult,arcDetect,lowerLimitMilliamps,name,numTests,startConditions,targetOutputKilovolts,testVoltageOutput,timeHoldSeconds,timeRampDownSeconds,timeRampUpSeconds,type,upperLimitMilliamps
Passed,0,0.00,HiPot 50Hz,1,StartKey,1.50,Back,2.0,0.0,0.0,HiPot50,20.00
Passed,,0.00,Power Leakage,1,,,,3.0,,,PowerLeakage,20.00";

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
                .Configure(c => c.UseXmlArray = true)
                )
            {
                var recs = p.ToArray();
                var x = recs.SelectMany(r => ((dynamic[])r.appliances).SelectMany(r1 => ((dynamic[])r1.test_set)
                    .Select(r2 => new { ((IList)r.appliances).OfType<dynamic>().FirstOrDefault().overallResult, r2.test }))).ToArray();

                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    .ThrowAndStopOnMissingField(false)
                    .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                    )
                {
                    w.Write(x);
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

        [Test]
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

        [Test]
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
                .WithXmlNamespace("jwplayer", "http://support.jwplayer.com/customer/portal/articles/1403635-media-format-reference#feeds")
                //.Configure(c => c.RetainXmlAttributesAsNative = true)
                .Configure(c => c.IgnoreNSPrefix = true)
                .Configure(c => c.IncludeAllSchemaNS = true)
                )
            {
                var recs = p.ToArray();

                using (var w = new ChoJSONWriter(sb)
                    .Configure(c => c.SupportMultipleContent = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    )
                {
                    w.Write(recs);
                }

            }
            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample43()
        {
            string expected = @"[
  {
    ""name"": ""slideshow"",
    ""xsl"": ""http://localhost:8080/Xsl-c.xslt"",
    ""categories"": [
      {
        ""name"": ""1234"",
        ""xsl"": ""http://localhost:8080/Xsl-b.xslt""
      }
    ]
  },
  {
    ""name"": ""article"",
    ""xsl"": ""http://localhost:8080/Xsl-a.xslt"",
    ""categories"": [
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
                .Configure(c => c.UseXmlArray = true)
                //.Setup(s => s.MembersDiscovered += (o, e) => e.Value.AddOrUpdate("category", typeof(Object[])))
                )
            {
                var recs = p.ToArray();
                using (var w = new ChoJSONWriter(sb))
                {
                    w.Write(recs);
                }

            }
            actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample43_1()
        {
            string expected = @"[
  {
    ""name"": ""slideshow"",
    ""xsl"": ""http://localhost:8080/Xsl-c.xslt"",
    ""categories"": [
      {
        ""name"": ""1234"",
        ""xsl"": ""http://localhost:8080/Xsl-b.xslt""
      }
    ]
  },
  {
    ""name"": ""article"",
    ""xsl"": ""http://localhost:8080/Xsl-a.xslt"",
    ""categories"": [
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
                .Configure(c => c.XmlArrayQualifier = (f, o) =>
                {
                    if (f == "category")
                        return true;

                    return null;
                })
                //.Setup(s => s.MembersDiscovered += (o, e) => e.Value.AddOrUpdate("category", typeof(Object[])))
                )
            {
                var recs = p.ToArray();
                using (var w = new ChoJSONWriter(sb))
                {
                    w.Write(recs);
                }

            }
            actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }
        public class EmpSample42
        {
            [DisplayName("First_Name")]
            public string FirstName { get; set; }
            public string Last_Name { get; set; }
            [DisplayName("ID")]
            public int EmpID { get; set; }
        }
        [Test]
        public static void Sample42()
        {
            string expected = @"[
  {
    ""First_Name"": ""Luke"",
    ""Last_Name"": ""Skywalker"",
    ""ID"": 1234
  },
  {
    ""First_Name"": ""Luke"",
    ""Last_Name"": ""Skywalker"",
    ""ID"": 1234
  }
]";
            string actual = null;

            string xml = @"<custs>
	<CUST>
		<First_Name>Luke</First_Name>
		<Last_Name>Skywalker</Last_Name>
		<ID><![CDATA[1234]]></ID>
	</CUST>
	<CUST>
		<First_Name>Luke</First_Name>
		<Last_Name>Skywalker</Last_Name>
		<ID><![CDATA[1234]]></ID>
	</CUST>
</custs>";
            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader<EmpSample42>.LoadText(xml)/*.WithXPath("/")*/
                .Configure(c => c.EmptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Empty)
                )
            {
                using (var w = new ChoJSONWriter<EmpSample42>(sb)
                    //.Configure(c => c.SupportMultipleContent = true)
                    //.Configure(c => c.RootName = "Emp")
                    //.Configure(c => c.IgnoreNodeName = true)
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }
        public class EmpSample42_1
        {
            [DisplayName("First_Name")]
            public string FirstName { get; set; }
            public string Last_Name { get; set; }
            [ChoXPath("ID")]
            public EmpID EmpID { get; set; }
        }
        [Test]
        public static void Sample42_1()
        {
            string expected = @"[
  {
    ""First_Name"": ""Luke"",
    ""Last_Name"": ""Skywalker"",
    ""EmpID"": {
      ""ID"": 1234
    }
  },
  {
    ""First_Name"": ""Luke"",
    ""Last_Name"": ""Skywalker"",
    ""EmpID"": {
      ""ID"": 1234
    }
  }
]";
            string actual = null;

            string xml = @"<custs>
	<CUST>
		<First_Name>Luke</First_Name>
		<Last_Name>Skywalker</Last_Name>
		<ID><![CDATA[1234]]></ID>
	</CUST>
	<CUST>
		<First_Name>Luke</First_Name>
		<Last_Name>Skywalker</Last_Name>
		<ID><![CDATA[1234]]></ID>
	</CUST>
</custs>";
            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader<EmpSample42_1>.LoadText(xml)/*.WithXPath("/")*/
                .Configure(c => c.EmptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Empty)
                .WithField(f => f.EmpID, mapper => mapper.ValueSelector(o => ((XElement)o).Value))
                )
            {
                using (var w = new ChoJSONWriter<EmpSample42_1>(sb)
                    //.Configure(c => c.SupportMultipleContent = true)
                    //.Configure(c => c.RootName = "Emp")
                    //.Configure(c => c.IgnoreNodeName = true)
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }
        public class EmpSample42_2
        {
            [DisplayName("First_Name")]
            public string FirstName { get; set; }
            public string Last_Name { get; set; }
            [DisplayName("ID")]
            public EmpID EmpID { get; set; }
        }
        [Test]
        public static void Sample42_2()
        {
            string expected = @"[
  {
    ""First_Name"": ""Luke"",
    ""Last_Name"": ""Skywalker"",
    ""ID"": {
      ""ID"": 1234
    }
  },
  {
    ""First_Name"": ""Luke"",
    ""Last_Name"": ""Skywalker"",
    ""ID"": {
      ""ID"": 1234
    }
  }
]";
            string actual = null;

            string xml = @"<custs>
	<CUST>
		<First_Name>Luke</First_Name>
		<Last_Name>Skywalker</Last_Name>
		<ID><![CDATA[1234]]></ID>
	</CUST>
	<CUST>
		<First_Name>Luke</First_Name>
		<Last_Name>Skywalker</Last_Name>
		<ID><![CDATA[1234]]></ID>
	</CUST>
</custs>";
            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader<EmpSample42_2>.LoadText(xml)/*.WithXPath("/")*/
                .Configure(c => c.EmptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Empty)
                .WithField(f => f.EmpID, mapper => mapper.ValueSelector(o => ((XElement)o).Value))
                )
            {
                using (var w = new ChoJSONWriter<EmpSample42_2>(sb)
                    //.Configure(c => c.SupportMultipleContent = true)
                    //.Configure(c => c.RootName = "Emp")
                    //.Configure(c => c.IgnoreNodeName = true)
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }
        public class EmpSample42_3
        {
            [DisplayName("First_Name")]
            public string FirstName { get; set; }
            public string Last_Name { get; set; }
            [DisplayName("ID")]
            public EmpID EmpID { get; set; }
        }
        [Test]
        public static void Sample42_3()
        {
            string expected = @"[
  {
    ""First_Name"": ""Luke"",
    ""Last_Name"": ""Skywalker"",
    ""ID"": {
      ""ID"": 1234
    }
  },
  {
    ""First_Name"": ""Luke"",
    ""Last_Name"": ""Skywalker"",
    ""ID"": {
      ""ID"": 1234
    }
  }
]";
            string actual = null;

            string xml = @"<custs>
	<CUST>
		<First_Name>Luke</First_Name>
		<Last_Name>Skywalker</Last_Name>
		<ID><![CDATA[1234]]></ID>
	</CUST>
	<CUST>
		<First_Name>Luke</First_Name>
		<Last_Name>Skywalker</Last_Name>
		<ID><![CDATA[1234]]></ID>
	</CUST>
</custs>";
            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader<EmpSample42_3>.LoadText(xml)/*.WithXPath("/")*/
                .Configure(c => c.EmptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Empty)
                .WithField(f => f.EmpID, mapper =>
                {
                    mapper.Configure(o => o.CustomNodeSelector = (o1) =>
                    {
                        List<object> objs = new List<object>();

                        XElement x = o1 as XElement;
                        objs.Add(x.XPathSelectElement("/ID"));
                        return objs;
                    });
                    mapper.ValueSelector(o => ((XElement)o).Value);
                })
                )
            {
                using (var w = new ChoJSONWriter<EmpSample42_3>(sb)
                    //.Configure(c => c.SupportMultipleContent = true)
                    //.Configure(c => c.RootName = "Emp")
                    //.Configure(c => c.IgnoreNodeName = true)
                    )
                    w.Write(p);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }
        [Test]
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

        [Test]
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

        [Test]
        public static void Sample39()
        {
            string expected = @"day_of_week_data,low_data,high_data,icon_data,condition_data
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
                var recs = p.ToArray();

                //var recs = p.ToArray().OfType<ChoDynamicObject>().Select(c =>
                //{
                //    c.ResetName();
                //    return c;
                //}).ToArray();
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    .Configure(c => c.IgnoreRootDictionaryFieldPrefix = true)
                    )
                    w.Write(recs);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample39_1()
        {
            string expected = @"forecast_conditions_day_of_week_data,forecast_conditions_low_data,forecast_conditions_high_data,forecast_conditions_icon_data,forecast_conditions_condition_data
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
                var recs = p.ToArray();

                //var recs = p.ToArray().OfType<ChoDynamicObject>().Select(c =>
                //{
                //    c.ResetName();
                //    return c;
                //}).ToArray();
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    )
                    w.Write(recs);
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
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

        [Test]
        public static void Sample37()
        {
            string expected = @"[
  {
    ""Products"": [
      {
        ""ProductCode"": ""C1010"",
        ""CategoryName"": ""Coins""
      },
      {
        ""ProductCode"": ""C1012"",
        ""CategoryName"": ""Coins""
      },
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

        [Test]
        public static void Sample36()
        {
            string expected = @"<DataRows xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <DataRow>
    <ColumnName>Value1</ColumnName>
    <ColumnName>Value3</ColumnName>
    <ColumnName>Value4</ColumnName>
    <ColumnName>Value5</ColumnName>
    <ColumnName>Value6</ColumnName>
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
                .Configure(c => c.DoNotEmitXmlNamespace = true)
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

        [Test]
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
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("//Values/*", true))
            {
                using (var w = new ChoCSVWriter(sb)
                    .WithFirstLineHeader()
                    )
                    w.Write(p.ToDictionary(r => r.Item, r => r.Value).ToDynamic());
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample34()
        {
            string expected = @"{
  ""Item"": {
    ""Name"": ""name"",
    ""Detail"": ""detail""
  }
}";
            string actual = null;

            string xml = @"<Items>
 <Item>
    <Name>name</Name>
    <Detail>detail</Detail>    
  </Item>
</Items>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("/Items")
                )
            {
                using (var w = new ChoJSONWriter(sb)
                    .Configure(c => c.SupportMultipleContent = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    )
                    w.Write(p.FirstOrDefault());
            }

            actual = sb.ToString();

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample33()
        {
            List<object> actual = new List<object>();

            string json = @"{
  ""id"": ""108013515952807"",
  ""posts"": {
    ""data"": [
      {
        ""id"": ""108013515952807_470186843068804"",
        ""created_time"": ""2013-05-14T20:43:28+0000""

      },
      {
        ""message"": ""TEKST"",
        ""id"": ""108013515952807_470178529736302"",
        ""created_time"": ""2013-05-14T20:22:07+0000""
      }
    ]
  }
}";
            string expected = @"[
  {
    ""id"": ""108013515952807_470186843068804"",
    ""created_time"": ""2013-05-14T16:43:28-04:00"",
    ""message"": null
  },
  {
    ""id"": ""108013515952807_470178529736302"",
    ""created_time"": ""2013-05-14T16:22:07-04:00"",
    ""message"": ""TEKST""
  }
]";
            using (var p = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..posts.data")
                .Configure(c => c.MaxScanRows = 10))
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actualJson);
        }

        [Test]
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

            using (var p = ChoXmlReader.LoadText(xml)
                .WithMaxScanNodes(1)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
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

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample30()
        {
            using (var p = new ChoXmlReader(FileNameSample30XML)
                .WithXPath("/")
                //.WithField("packages", fieldName: "Package")
                //.Configure(c => c.EmptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Ignore)
                .Configure(c => c.NullValueHandling = ChoNullValueHandling.Ignore)
                )
            {
                var recs = p.ToArray();

                using (var w = new ChoJSONWriter(FileNameSample30ActualJSON)
                    .Configure(c => c.SupportMultipleContent = true)
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    )
                {
                    w.Write(recs);
                }
            }

            var actualJson = File.ReadAllText(FileNameSample30ActualJSON);

            FileAssert.AreEqual(FileNameSample30ExpectedJSON, FileNameSample30ActualJSON);
        }

        [Test]
        public static void Sample30_1()
        {
            using (var p = new ChoXmlReader(FileNameSample30XML)
                .WithXPath("/")
                )
            {
                var recs = p.ToArray();

                using (var w = new ChoJSONWriter(FileNameSample30_1ActualJSON)
                    .Configure(c => c.SupportMultipleContent = true)
                    .Configure(c => c.IgnoreRootName = true)
                    .Configure(c => c.IgnoreNodeName = true)
                    )
                {
                    w.Write(recs);
                }
            }

            var actualJson = File.ReadAllText(FileNameSample30_1ActualJSON);

            FileAssert.AreEqual(FileNameSample30_1ExpectedJSON, FileNameSample30_1ActualJSON);
        }

        [Test]
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

        [Test]
        public static void XmlArrayTest()
        {
            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:json=""http://james.newtonking.com/projects/json"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema"">
  <ApplicationCrediential>
    <ConsumerKey>
      <Consumers json:Array=""true"" xsi:nil=""false"">
        <Name isActive=""false"">
        Tom
      </Name>
        <Name>Mark</Name>
      </Consumers>
    </ConsumerKey>
    <ConsumerSecret />
  </ApplicationCrediential>
</Root>";
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
                //.WithXmlNamespace("json", "http://james.newtonking.com/projects/json")
                //.WithXmlNamespace("xsi", "http://www.w3.org/2001/XMLSchema")
                .WithField("ConsumerKey")
                .WithField("ConsumerSecret")
            )
            {
                var recs = p.ToArray();
                using (var w = new ChoXmlWriter(msg)
                    .WithXmlNamespace("json", "http://james.newtonking.com/projects/json")
                    .WithXmlNamespace("xsi", "http://www.w3.org/2001/XMLSchema")
                    .Configure(c => c.OmitXsiNamespace = false)
                    )
                {
                    w.Write(recs);
                }

                //var x = p.First();
                //Console.WriteLine(ChoJSONWriter.ToText(x));
                //Console.WriteLine(ChoXmlWriter.ToText(x));
            }

            actual = msg.ToString().FormatXml();

            Assert.AreEqual(expected, actual);
        }

        [Test]
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

        [Test]
        public static void Sample20Test()
        {
            string expected = @"""GetItemRequest"": {
  ""ApplicationCrediential"": {
    ""ConsumerKey"": null,
    ""ConsumerSecret"": null
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

        [Test]
        public static void Sample21()
        {
            string expected = @"[
  {
    ""type"": ""MCS"",
    ""id"": ""id1"",
    ""description"": ""desc1""
  },
  {
    ""type"": ""MCS"",
    ""id"": ""id2"",
    ""description"": ""desc2""
  },
  {
    ""type"": ""MCM"",
    ""id"": ""id3"",
    ""description"": ""desc3""
  },
  {
    ""type"": ""MCM"",
    ""id"": ""id4"",
    ""description"": ""desc4""
  }
]";
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
                    actual.Add(rec); // ChoJSONWriter.ToText(rec));
            }

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actualJson);
        }

        [Test]
        public static void Sample19()
        {
            string expected = @"[
  {
    ""ID"": ""130"",
    ""EmployeeID"": ""3"",
    ""AllocationID"": ""114"",
    ""TaskID"": ""239"",
    ""ProjectID"": ""26"",
    ""ProjectName"": ""LIK Template""
  }
]";
            using (var p = new ChoXmlReader(FileNameSample19XML)
                .WithXmlNamespace("tlp", "http://www.timelog.com/XML/Schema/tlp/v4_4")
                .Configure(c => c.IgnoreNSPrefix = true)
                )
            {
                var actual = JsonConvert.SerializeObject(p.ToList(), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Sample19_1()
        {
            string expected = @"[
  {
    ""ID"": 130,
    ""EmployeeID"": 3,
    ""AllocationID"": 114,
    ""TaskID"": 239,
    ""ProjectID"": 26,
    ""ProjectName"": ""LIK Template""
  }
]";
            using (var p = new ChoXmlReader(FileNameSample19XML)
                .WithXmlNamespace("tlp", "http://www.timelog.com/XML/Schema/tlp/v4_4")
                .Configure(c => c.IgnoreNSPrefix = true)
                .WithMaxScanNodes(1)
                )
            {
                var actual = JsonConvert.SerializeObject(p.ToList(), Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void Sample18()
        {
            string expected = @"original_impot_no,price
891258,450
891258,432";

            StringBuilder csv = new StringBuilder();
            using (var p = new ChoXmlReader(FileNameSample18XML)
                .WithXmlNamespace("http://www.google.com/xml/impot//20016-02-31")
                )
            {
                var recs = p.ToArray();

                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    )
                {
                    w.Write(recs.SelectMany(r => ((IList<object>)r["product-lineitems"]).Cast<dynamic>().Select(r1 => new { original_impot_no = r["original-impot-no"], price = r1.price })));
                }

                var actual = csv.ToString();
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


            public static implicit operator EmpID(string value)
            {
                if (int.TryParse(value, out int val))
                    return new EmpID { ID = val };
                else
                    return null;
            }
        }

        [Test]
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


        [Test]
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
        public static string FileNameSample30_1ActualJSON => "sample30_1Actual.json";
        public static string FileNameSample30ExpectedJSON => "sample30Expected.json";
        public static string FileNameSample30_1ExpectedJSON => "sample30_1Expected.json";
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


        [Test]
        public static void HTMLTableToCSV()
        {
            string expected = @"tr_Lot,tr_Op,tr_Status,tr_iDispoStatus,tr_DispoBy,tr_DispoDate,tr_TRCount,tr_View
7649B703,6262,FAIL,FAIL,mly2,12/10/2016,1,/SS_PROD/Report/LotDispoHistSummRepPopUp.aspx?Lot=7649B703&amp;Location=6262
7649B703,6262,FAIL,FAIL,mly2,12/10/2016,1,/SS_PROD/Report/LotDispoHistSummRepPopUp.aspx?Lot=7649B703&amp;Location=6262";
            StringBuilder csv = new StringBuilder();
            using (var cr = new ChoCSVWriter(csv).WithFirstLineHeader())
            {
                using (var xr = new ChoXmlReader(FileNameHtmlTableXML).WithXPath("//tbody/tr")
                    .WithField("Lot", xPath: "td[1]", fieldType: typeof(string))
                    .WithField("Op", xPath: "td[2]", fieldType: typeof(long))
                    .WithField("Status", xPath: "td[3]", fieldType: typeof(string))
                    .WithField("iDispoStatus", xPath: "td[4]", fieldType: typeof(string))
                    .WithField("DispoBy", xPath: "td[5]", fieldType: typeof(string))
                    .WithField("DispoDate", xPath: "td[6]", fieldType: typeof(DateTime))
                    .WithField("TRCount", xPath: "td[7]", fieldType: typeof(long))
                    .WithField("View", xPath: "td[8]/a/@href", fieldType: typeof(string))
                )
                {
                    cr.Write(xr);
                }
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Xml2CSV3()
        {
            string expected = @"HouseNumber,RoomNumber,Timestamp,Color,Height,Scope,Code,Faucet
1,1,12/29/2017,Blue,23,1,,
1,2,12/29/2017,Black,35.2,1,1234,3
1,2,12/29/2017,Red,98.56,1,1234,2";

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
                StringBuilder csv = new StringBuilder();
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.IgnoreRootDictionaryFieldPrefix = true)
                    .Configure(c => c.IgnoreDictionaryFieldPrefix = true)
                    )
                {
                    var recs = xr.ToArray();
                    w.Write(recs);
                }

                var actual = csv.ToString();
                Assert.AreEqual(expected, actual);

                //string connectionstring = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ADVENTUREWORKS2012_DATA.MDF;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
                //using (SqlBulkCopy bcp = new SqlBulkCopy(connectionstring))
                //{
                //    bcp.DestinationTableName = "dbo.HOUSEINFO";
                //    bcp.EnableStreaming = true;
                //    bcp.BatchSize = 10000;
                //    bcp.BulkCopyTimeout = 0;
                //    bcp.NotifyAfter = 10;
                //    bcp.SqlRowsCopied += delegate (object sender, SqlRowsCopiedEventArgs e)
                //    {
                //        Console.WriteLine(e.RowsCopied.ToString("#,##0") + " rows copied.");
                //    };
                //    bcp.WriteToServer(xr.AsDataReader());
                //}
            }

        }

        [Test]
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

        [Test]
        public static void Sample12()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"SelectedIdValue",new SelectedIds {  Id = new int[] {108,110,111}} }}
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoXmlReader(FileNameSample12XML)
            .WithField("SelectedIdValue", xPath: "//SelectedIds", fieldType: typeof(SelectedIds))
            .Configure(c => c.TurnOffPluralization = true)
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

        [Test]
        public static void DynamicXmlTest()
        {
            string expected = @"<Item1 Id=""100"" Name=""Tom"">
  <StartDate @Value=""0001-01-11T00:00:00"" />
  <SelectedIds>
    <Id @Value=""101"" />
    <Id @Value=""102"" />
    <Id @Value=""103"" />
  </SelectedIds>
</Item1>";
            string actual = null;

            ChoDynamicObject src = new ChoDynamicObject("Item1");

            IDictionary<string, object> x = src as IDictionary<string, object>;
            x.Add("@Id", 100);
            x.Add("@Name", "Tom");

            //x.Add("@@Value", "Hello!");
            ChoDynamicObject sd = new ChoDynamicObject();
            ((IDictionary<string, object>)sd).Add("@@Value", "0001-01-11T00:00:00");

            x.Add("StartDate", sd);

            ChoDynamicObject id1 = new ChoDynamicObject("Id");
            ((IDictionary<string, object>)id1).Add("@@Value", 101);

            ChoDynamicObject id2 = new ChoDynamicObject("Id");
            ((IDictionary<string, object>)id2).Add("@@Value", 102);

            ChoDynamicObject id3 = new ChoDynamicObject("Id");
            ((IDictionary<string, object>)id3).Add("@@Value", 103);

            x.Add("SelectedIds", new object[] { id1, id2, id3 });

            actual = src.GetXml(xmlArrayQualifierOverride: (k, o) => true);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DynamicXmlTest_1()
        {
            string expected = @"<Item1 Id=""100"" Name=""Tom"">
  <StartDate @Value=""0001-01-11T00:00:00"" />
  <SelectedIds>
    <SelectedId @Value=""101"" />
    <SelectedId @Value=""102"" />
    <SelectedId @Value=""103"" />
  </SelectedIds>
</Item1>";
            string actual = null;

            ChoDynamicObject src = new ChoDynamicObject("Item1");

            IDictionary<string, object> x = src as IDictionary<string, object>;
            x.Add("@Id", 100);
            x.Add("@Name", "Tom");

            //x.Add("@@Value", "Hello!");
            ChoDynamicObject sd = new ChoDynamicObject();
            ((IDictionary<string, object>)sd).Add("@@Value", "0001-01-11T00:00:00");

            x.Add("StartDate", sd);

            ChoDynamicObject id1 = new ChoDynamicObject();
            ((IDictionary<string, object>)id1).Add("@@Value", 101);

            ChoDynamicObject id2 = new ChoDynamicObject();
            ((IDictionary<string, object>)id2).Add("@@Value", 102);

            ChoDynamicObject id3 = new ChoDynamicObject();
            ((IDictionary<string, object>)id3).Add("@@Value", 103);

            x.Add("SelectedIds", new object[] { id1, id2, id3 });

            actual = src.GetXml(xmlArrayQualifierOverride: (k, o) => true);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DynamicXmlTest_2()
        {
            string expected = @"<Item1 Id=""100"" Name=""Tom"">
  <StartDate Value=""0001-01-11T00:00:00"" />
  <SelectedIds>
    <SelectedId Value=""101"" />
    <SelectedId Value=""102"" />
    <SelectedId Value=""103"" />
  </SelectedIds>
</Item1>";
            string actual = null;

            ChoDynamicObject src = new ChoDynamicObject("Item1");
            src.SetAttribute("Id", 100);
            src.SetAttribute("Name", "Tom");

            //x.Add("@@Value", "Hello!");
            ChoDynamicObject sd = new ChoDynamicObject();
            sd.SetAttribute("Value", "0001-01-11T00:00:00");

            src.Add("StartDate", sd);

            ChoDynamicObject id1 = new ChoDynamicObject();
            id1.SetAttribute("@Value", 101);

            ChoDynamicObject id2 = new ChoDynamicObject();
            id2.SetAttribute("@Value", 102);

            ChoDynamicObject id3 = new ChoDynamicObject();
            id3.SetAttribute("@Value", 103);

            src.Add("SelectedIds", new object[] { id1, id2, id3 });

            actual = src.GetXml(xmlArrayQualifierOverride: (k, o) => true);

            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Sample15()
        {
            List<object> actual = new List<object>();

            string expected = @"[
  {
    ""SOAP-ENV:Header"": null,
    ""SOAP-ENV:Body"": {
      ""Status"": [
        {
          ""#text"": ""Success: 12345""
        }
      ]
    }
  }
]";
            string xml = @"<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"">
  <SOAP-ENV:Header></SOAP-ENV:Header>
  <SOAP-ENV:Body>
    <Status xmlns=""http://www.naptan.org.uk/"">
      <Message>Success: 12345</Message>
    </Status>
  </SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

            using (var parser = ChoXmlReader.LoadText(xml)
                .WithXPath("/")
                .WithXmlNamespace("SOAP-ENV", "http://schemas.xmlsoap.org/soap/envelope/")
                .WithXmlNamespace("http://www.naptan.org.uk/")
            )
            {
                foreach (dynamic rec in parser)
                {
                    actual.Add(rec);
                }
            }

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actualJson);
            //CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void Sample15_1()
        {
            string expected = @"[
  {
    ""SOAP-ENV:Header"": null,
    ""SOAP-ENV:Body"": {
      ""Status"": {
        ""Message"": ""Success: 12345""
      }
    }
  }
]";
            string xml = @"<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"">
  <SOAP-ENV:Header></SOAP-ENV:Header>
  <SOAP-ENV:Body>
    <Status>
      <Message>Success: 12345</Message>
    </Status>
  </SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

            List<object> actual = new List<object>();
            using (var parser = ChoXmlReader.LoadText(xml)
                .WithXPath("/")
                .WithXmlNamespace("SOAP-ENV", "http://schemas.xmlsoap.org/soap/envelope/")
            )
            {
                foreach (dynamic rec in parser)
                {
                    actual.Add(rec);
                }
            }

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actualJson);
        }
        [Test]
        public static void Sample15_2()
        {
            string expected = @"[
  {
    ""SOAP-ENV:Header"": null,
    ""SOAP-ENV:Body"": {}
  }
]";
            string xml = @"<SOAP-ENV:Envelope xmlns:SOAP-ENV=""http://schemas.xmlsoap.org/soap/envelope/"">
  <SOAP-ENV:Header></SOAP-ENV:Header>
  <SOAP-ENV:Body>
    <Status xmlns=""http://www.naptan.org.uk/"">
      <Message>Success: 12345</Message>
    </Status>
  </SOAP-ENV:Body>
</SOAP-ENV:Envelope>";

            List<object> actual = new List<object>();
            using (var parser = ChoXmlReader.LoadText(xml)
                .WithXPath("/")
                .WithXmlNamespace("SOAP-ENV", "http://schemas.xmlsoap.org/soap/envelope/")
            )
            {
                foreach (dynamic rec in parser)
                {
                    actual.Add(rec);
                }
            }

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actualJson);
        }
        [Test]
        public static void Sample14()
        {
            List<object> recs = new List<object>();
            using (var w = new ChoXmlWriter(FileNameSample14ActualXML)
                .Configure(c => c.DoNotEmitXmlNamespace = true)
                )
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

                        recs.Add(rec);
                    }
                }
            }

            var expected = File.ReadAllText(FileNameSample14ExpectedXML);
            var actual = File.ReadAllText(FileNameSample14ActualXML);

            Assert.AreEqual(expected, actual);
            //FileAssert.AreEqual(FileNameSample14ExpectedXML, FileNameSample14ActualXML);
            //Assert.Fail("Missing Book with id 101, but source-xml is a valid xml");
        }

        [Test]
        public static void NullableTest()
        {
            //object expected = new Item { Number = 100, ItemName = "TestName1", ItemId = 1 };

            string xml = @"<?xml version=""1.0""?>
    <Item Number = ""100"" ItemName = ""TestName1"" ItemId = ""1"" />";

            string expected = @"{
  ""ItemId"": 1,
  ""ItemName"": ""TestName1"",
  ""Number"": 100
}";
            XDocument doc = XDocument.Parse(xml);

            var rec = ChoXmlReader<Item>.LoadXElements(new XElement[] { doc.Root }).FirstOrDefault();

            var actual = JsonConvert.SerializeObject(rec, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Pivot1()
        {
            string expected = @"Column1,Column2,Column3
A_TempFZ1_Set,A_TempHZ2_Set,A_TempHZ3_Set
60,195,200";
            string actual = null;

            using (var parser = new ChoXmlReader(FileNamePivot1XML).WithXPath(@"//Values/*", true)
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

        [Test]
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

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            var expectedJson = JsonConvert.SerializeObject(expected, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expectedJson, actualJson);

            //CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void XmlNullTest()
        {
            string expected = @"[
  {
    ""AustrittDatum"": [
      ""2018-01-31+01:00"",
      null
    ]
  }
]";
            StringBuilder json = new StringBuilder();
            using (var parser = new ChoXmlReader(FileNameSample13XML)
            )
            {
                var recs = parser.Select(x => x.AustrittDatum).ToArray();
                //var c = parser.Select(x => (string)x.AustrittDatum).ToArray();
                using (var jw = new ChoJSONWriter(json))
                    //jw.Write(parser.ToArray());
                    jw.Write(new { AustrittDatum = recs });
                //jw.Write(new { AustrittDatum = parser.Select(x => (string)x.AustrittDatum.ToString()).ToArray() });
            }

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
            //FileAssert.AreEqual(FileNameXmlNullTestExpectedJSON, FileNameXmlNullTestActualJSON);
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

        [Test]
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

        [Test]
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

            var actual = File.ReadAllText(FileNameXmlToCSVSample6ActualCSV);
            var expected = File.ReadAllText(FileNameXmlToCSVSample6ExpectedCSV);

            FileAssert.AreEqual(FileNameXmlToCSVSample6ExpectedCSV, FileNameXmlToCSVSample6ActualCSV);
        }


        [Test]
        public static void XmlToCSVSample5()
        {
            string expected = @"PRICE_WIC,PRICE_DESCRIPTION,PRICE_VENDOR_NAME,PRICE_GROUP_NAME,PRICE_VPF_NAME,PRICE_CURRENCY_CODE,PRICE_AVAIL,PRICE_RETAIL_PRICE,PRICE_MY_PRICE,PRICE_WARRANTYTERM,PRICE_GROUP_ID,PRICE_VENDOR_ID,PRICE_SMALL_IMAGE,PRICE_PRODUCT_CARD,PRICE_EAN
GA-H110M-S2H,""GIGABYTE Main Board Desktop INTEL H110 (Socket LGA1151,2xDDR4,VGA/HDMI/DVI,1xPCIEX16/2xPCIEX1,USB3.0/USB2.0, 6xSATA III,LAN) micro ATX retail"",GIGABYTE,Main Board Desktop,,USD,0,56.40,52.71,36,32,170192,https://www.it4profit.com/catalogimg/wic/1/GA-H110M-S2H,https://content.it4profit.com/itshop/itemcard_cs.jsp?ITEM=151118121920215716&THEME=asbis&LANG=ro,4719331837310";

            StringBuilder csv = new StringBuilder();
            using (var parser = new ChoXmlReader(FileNameSample5XML).WithXPath("/PRICE")
            )
            {
                using (var writer = new ChoCSVWriter(csv).WithFirstLineHeader())
                    writer.Write(parser);
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
            //FileAssert.AreEqual(FileNameXmlToCSVSample5ExpectedCSV, FileNameXmlToCSVSample5ActualCSV);
        }

        [Test]
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

            var actualJson = File.ReadAllText(FileNameSample8ActualJSON);
            var expectedJson = File.ReadAllText(FileNameSample8ExpectedJSON);

            FileAssert.AreEqual(FileNameSample8ExpectedJSON, FileNameSample8ActualJSON);
        }

        [Test]
        public static void Sample9Test()
        {
            string expected = @"[
  {
    ""view_id"": ""2adaf1b2"",
    ""view_name"": ""Users by Function"",
    ""view_content_url"": ""ExampleWorkbook/sheets/UsersbyFunction"",
    ""view_total_count"": null,
    ""view_total_available"": 2
  },
  {
    ""view_id"": ""09ecb39a"",
    ""view_name"": ""Users by Site"",
    ""view_content_url"": ""ExampleWorkbook/sheets/UsersbySite"",
    ""view_total_count"": null,
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
                .WithField("view_total_count", xPath: "/usage/@totalViewCount", fieldType: typeof(int))
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
        [Test]
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
                .WithField("r1", xPath: "r1", fieldType: typeof(long))
                .WithField("r2", xPath: "r2/tr1", fieldType: typeof(long))
            )
            {
                actual = parser.ToList();
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample11Test()
        {
            //List<object> expected = new List<object>
            //{
            //    new ChoDynamicObject
            //    {
            //        {"id",1 },
            //        {"sons", new object[] {
            //            new ChoDynamicObject { { "id", "11" }, { "name", "Tom1" }, { "address", new ChoDynamicObject {{"street","10 River Rd" },{ "city", "Edison" },{ "state", "NJ" } } }, { "workbook", new ChoDynamicObject { { "id", "9fb2948d" } } },{ "owner", new ChoDynamicObject { { "id", "c2abaaa9" } } },{"usage",new ChoDynamicObject{ { "totalViewCount", "95" },{"#text","sdsad" } } } },
            //            new ChoDynamicObject { { "id", "12" }, { "name", "Tom2" }, { "address", new ChoDynamicObject {{"street","10 Madison Ave" },{ "city", "New York" },{ "state", "NY" } } }, { "workbook", new ChoDynamicObject { { "id", "9fb2948d" } } },{ "owner", new ChoDynamicObject { { "id", "c2abaaa9" } } },{"usage",new ChoDynamicObject{ { "totalViewCount", "95" },{"#text","sdsad" } } } }
            //        } } }
            //};

            string expected = @"[
  {
    ""id"": ""1"",
    ""sons"": [
      {
        ""id"": ""11"",
        ""name"": ""Tom1"",
        ""address"": {
          ""street"": ""10 River Rd"",
          ""city"": ""Edison"",
          ""state"": ""NJ""
        },
        ""workbook"": {
          ""id"": ""9fb2948d""
        },
        ""owner"": {
          ""id"": ""c2abaaa9""
        },
        ""usage"": {
          ""totalViewCount"": ""95"",
          ""#text"": ""sdsad""
        }
      },
      {
        ""id"": ""12"",
        ""name"": ""Tom2"",
        ""address"": {
          ""street"": ""10 Madison Ave"",
          ""city"": ""New York"",
          ""state"": ""NY""
        },
        ""workbook"": {
          ""id"": ""9fb2948d""
        },
        ""owner"": {
          ""id"": ""c2abaaa9""
        },
        ""usage"": {
          ""totalViewCount"": ""95"",
          ""#text"": ""sdsad""
        }
      }
    ]
  }
]";
            List<object> actual = new List<object>();

            using (var parser = new ChoXmlReader(FileNameSample11XML).WithXPath("/members/father")
                .WithField("id")
                .WithField("sons")
            )
            {
                actual = parser.ToList();
            }
            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);

            //CollectionAssert.AreEqual(expected, actual);
            Assert.AreEqual(expected, actualJson);
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

        [Test]
        public static void JSONToXmlSample4()
        {
            using (var parser = new ChoJSONReader<ProductionOrderFile>(FileNameJSONToXmlSample4JSON).Configure(c => c.UseJSONSerialization = true)
    )
            {
                using (var writer = new ChoXmlWriter<ProductionOrderFile>(FileNameJSONToXmlSample4ActualXML)
                    .Configure(c => c.UseXmlSerialization = true)
                    .Configure(c => c.DoNotEmitXmlNamespace = true)
                    )
                    writer.Write(parser);
            }
            var actual = File.ReadAllText(FileNameJSONToXmlSample4ExpectedXML);
            var expected = File.ReadAllText(FileNameJSONToXmlSample4ActualXML);

            Assert.AreEqual(actual, expected);
        }

        [Test]
        public static void XmlToJSONSample4()
        {
            string expected = @"[{""ProductionOrderName"":""ProOrd_Xml_001"",""ProductCode"":""Pro_EU_001"",""Batches"":[{""Name"":""Lote_Xml_01""}],""Levels"":[{""Id"":1,""Name"":""Nivel_1"",""PkgRatio"":120},{""Id"":2,""Name"":""Nivel_2"",""PkgRatio"":1}],""VariableData"":[{""VariableDataId"":1,""Value"":""Pro_EU_001"",""LevelId"":1},{""VariableDataId"":20,""Value"":""Lote_Xml_01"",""LevelId"":1},{""VariableDataId"":11,""Value"":""170101"",""LevelId"":1},{""VariableDataId"":17,""Value"":""210101"",""LevelId"":1},{""VariableDataId"":21,""Value"":""####################"",""LevelId"":1}]}]";

            StringBuilder json = new StringBuilder();
            using (var parser = new ChoXmlReader<ProductionOrderFile>(FileNameSample4XML)
                .WithXPath("/")
                .Configure(c => c.UseXmlSerialization = true)
                )
            {
                using (var writer = new ChoJSONWriter(json)
                    .Configure(c => c.UseJSONSerialization = true)
                    .Configure(c => c.SupportMultipleContent = false)
                    .Configure(c => c.Formatting = Newtonsoft.Json.Formatting.None)
                    )
                    writer.Write(parser);
            }

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);

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
        [Test]
        public static void XmlToJSONSample4_1()
        {
            string expected = @"[
  {
    ""ProductionOrderName"": ""ProOrd_Xml_001"",
    ""ProductCode"": ""Pro_EU_001"",
    ""Batches"": [
      {
        ""Name"": ""Lote_Xml_01""
      }
    ],
    ""Levels"": [
      {
        ""Id"": 1,
        ""Name"": ""Nivel_1"",
        ""PkgRatio"": 120
      },
      {
        ""Id"": 2,
        ""Name"": ""Nivel_2"",
        ""PkgRatio"": 1
      }
    ],
    ""VariableData"": [
      {
        ""VariableDataId"": 1,
        ""Value"": ""Pro_EU_001"",
        ""LevelId"": 1
      },
      {
        ""VariableDataId"": 20,
        ""Value"": ""Lote_Xml_01"",
        ""LevelId"": 1
      },
      {
        ""VariableDataId"": 11,
        ""Value"": ""170101"",
        ""LevelId"": 1
      },
      {
        ""VariableDataId"": 17,
        ""Value"": ""210101"",
        ""LevelId"": 1
      },
      {
        ""VariableDataId"": 21,
        ""Value"": ""####################"",
        ""LevelId"": 1
      }
    ]
  }
]";

            StringBuilder json = new StringBuilder();
            using (var parser = new ChoXmlReader<ProductionOrderFile>(FileNameSample4XML)
                .WithXPath("/")
                .Configure(c => c.UseXmlSerialization = true)
                )
            {
                using (var writer = new ChoJSONWriter(json)
                    .Configure(c => c.UseJSONSerialization = true)
                    .Configure(c => c.SupportMultipleContent = false)
                    )
                    writer.Write(parser);
            }

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);

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


        [Test]
        public static void XmlToCSVSample3()
        {
            string expected = @"id_int,scanTime_int,host_string,vuln_string,port_int,protocol_string
1,1414010812,Host.5,Vuln.6230,500,udp
2,1414010978,Host.6,Vuln.1191,22,tcp
3,1414010978,Host.6,Vuln.30535,22,tcp
4,1414010978,Host.6,Vuln.78682,22,tcp";

            StringBuilder csv = new StringBuilder();
            using (var parser = ChoXmlReader.LoadXElements(XDocument.Load(FileNameSample3XML).XPathSelectElements("//member[name='table']/value/array/data/value"))
                .WithField("id", xPath: "array/data/value[1]")
                .WithField("scanTime", xPath: "array/data/value[2]")
                .WithField("host", xPath: "array/data/value[3]")
                .WithField("vuln", xPath: "array/data/value[4]")
                .WithField("port", xPath: "array/data/value[5]")
                .WithField("protocol", xPath: "array/data/value[6]")
            )
            {
                using (var writer = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .Configure(c => c.IgnoreRootDictionaryFieldPrefix = true)
                    )
                    writer.Write(parser);

            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void XmlToCSVSample2()
        {
            string expected = @"message_messageID,message_orderNumber,message_model,message_tls,message_status,message_timestamp,message_message,message_aaaaaaaaaa,message_bbbbbbbb,message_ccccccccc,message_ddddddddd
12345,1111111,AA,22222,99,2014-04-25 08:27:17Z,,ff,L.f,333,n.998
12345,1111111,AA,22222,99,2014-04-25 08:27:17Z,,,,,";

            StringBuilder csv = new StringBuilder();
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

                var recs = parser.ToArray();
                using (var writer = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .ThrowAndStopOnMissingField(false)
                    )
                    writer.Write(recs);
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void XmlToCSVSample()
        {
            string expected = @"ff,L.f,333,n.998,j8,L.O,33333,K.9999";

            StringBuilder csv = new StringBuilder();
            using (var parser = new ChoXmlReader("sample2.xml").WithXPath("attributes/attribute")
                .WithField("Name", xPath: "@name")
                .WithField("Value", xPath: "@value")
                )
            {
                var recs = parser.ToArray();
                var vals = recs.Select(kvp => kvp.Value).ToArray();

                using (var writer = new ChoCSVWriter(csv))
                    writer.Write(vals.ToExpandoObject());
                //Console.WriteLine(ChoCSVWriter.ToText(parser.Select(kvp => kvp.Value).ToExpandoObject()));

                var actual = csv.ToString();
                Assert.AreEqual(expected, actual);
            }

            //FileAssert.AreEqual(FileNameXmlToCSVSampleExpectedCSV, FileNameXmlToCSVSampleActualCSV);

            // Expected file not checked in because of not existent XPath, maybee a Exception-test

        }

        [Test]
        public static void ToDataTable()
        {
            string xml = @"<Employees>
                <Employee Id='1'>
                    <Name isActive = 'true'>Tom</Name>
                </Employee>
                <Employee Id='2'>
                    <Name>Mark</Name>
                </Employee>
            </Employees>
        ";

            DataTable expected = new DataTable();
            expected.Columns.Add("Id", typeof(Int32)).AllowDBNull = false;
            expected.Columns.Add("Name", typeof(string));
            expected.Columns.Add("IsActive", typeof(bool)).AllowDBNull = true;
            expected.Rows.Add(1, "Tom", true);
            expected.Rows.Add(2, "Mark", DBNull.Value);

            DataTable actual = null;

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlReader<EmployeeRec>(reader)
                .ErrorMode(ChoErrorMode.ThrowAndStop))
            {
                writer.WriteLine(xml);

                writer.Flush();
                stream.Position = 0;

                //var recs = parser.ToArray();

                actual = parser.AsDataTable();
            }

            var actualJson = JsonConvert.SerializeObject(actual, Newtonsoft.Json.Formatting.Indented);
            var expectedJson = JsonConvert.SerializeObject(expected, Newtonsoft.Json.Formatting.Indented);

            Assert.AreEqual(expectedJson, actualJson);
            //DataTableAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void LoadTest()
        {
            Assert.Ignore();

            DateTime st = DateTime.Now;
            Console.WriteLine("Starting..." + st);
            using (var r = new ChoXmlReader(@"EPAXMLDownload1.xml").NotifyAfter(10000).WithXPath("Document/FacilitySite"))
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

        [Test]
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

        [Test]
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

        [Test]
        public static void ConfigFirstDynamicTest()
        {
            //List<object> expected = new List<object>
            //{
            //    new ChoDynamicObject {{"Id", (Int64)1}, { "Name", new ChoDynamicObject { { "isActive","true"},{ "#text", "Tom" } } } },
            //    new ChoDynamicObject {{"Id",(Int64)2}, { "Name", "Mark" } }
            //};
            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": {
      ""isActive"": ""true"",
      ""#text"": ""Tom""
    }
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark""
  }
]";

            List<object> recs = new List<object>();

            ChoXmlRecordConfiguration config = new ChoXmlRecordConfiguration();
            config.XmlRecordFieldConfigurations.Add(new ChoXmlRecordFieldConfiguration("Id") { FieldType = typeof(int) });
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
                    recs.Add(rec);
                }
            }

            var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void QuickTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {{"Id","1" }, { "Name", new ChoDynamicObject{ {"isActive", "true" },{ "#text", "Tom" } } } },
                new ChoDynamicObject {{"Id","2" }, { "Name", "Mark" } }
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

        [Test]
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

        [Test]
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
            using (var parser = new ChoXmlReader(reader)
                .WithXPath("/cont:contacts/cont:contact/cont:contact1")
                .WithXmlNamespace("cont", "www.tutorialspoint.com/profile").WithField("name", "cont:name")
                .Configure(c => c.IgnoreNSPrefix = true)
                )
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
            [ChoXmlNodeRecordField(XPath = "@Id")]
            [Required]
            public int? Id
            {
                get;
                set;
            }
            [ChoXmlNodeRecordField(XPath = "/Name")]
            [DefaultValue("XXXX")]
            public string Name
            {
                get;
                set;
            }
            [ChoXmlNodeRecordField(XPath = "/Name/@isActive")]
            public bool? IsActive
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
