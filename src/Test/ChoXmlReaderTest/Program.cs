using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChoETL;
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
    }
    public class SelectedIds
    {
        [XmlElement]
        public int[] Id;
    }

    public class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }
    }

    public class RoomInfo
    {
        public int RoomNumber { get; set; }
    }

    public class Table
    {
        public string Color { get; set; }
    }

    public class Emp
    {
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;
            Sample49Test();
        }

        static void XmlToJSONKVP()
        {
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

                Console.WriteLine(ChoXmlWriter.ToTextAll(p.Select(r => ((IList<dynamic>)r.propertiess).Select(r1 => r1.value).ToList())));

            }
        }

        static void Sample49Test()
        {
using (var r = new ChoXmlReader("Sample49.xml")
    .WithXPath("/CarCollection/Cars/Car")
    .WithMaxScanRows(10)
    //.Configure(c => c.MaxScanRows = 10)
    )
{
    Console.WriteLine(ChoJSONWriter.ToTextAll(r));
}
        }

        static void Sample48Test()
        {
            var dt = new ChoXmlReader("sample48.xml")
                .WithXPath("//Contour/Elements/Element")
                .Select(i => i.Flatten())
                .AsDataTable();
        }

        static void XmlNSTest()
        {
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
                    Console.WriteLine(x.Dump());
            }
        }

        static void XmlToJSONNumberTest()
        {
            string xml = @"<Report xmlns:json=""http://james.newtonking.com/projects/json"">
<ReportItem>
    <Name>MyObjectName</Name>
    <Revenue>99999.45</Revenue>
</ReportItem>
</Report>";

            var x = ChoXmlReader.DeserializeText(xml);
            Console.WriteLine(x.DumpAsJson());
        }
        static void Sample22Test()
        {
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
                var dt = p.Select(e => e.Flatten()).AsDataTable();
            }
        }

        static void Sample21Test()
        {
            using (var p = new ChoXmlReader("sample21.xml")
                .WithField("Key")
                .WithField("Value", xPath: "/Values/string")
                )
            {
                var dt = p.SelectMany(r => ((Array)r.Value).OfType<string>().Select(r1 => new { Key = r.Key, Value = r1})).AsDataTable();
            }
        }

        public class Naptan
        {
            [ChoXmlNodeRecordField(XPath = "/z:AtcoCode")]
            public string AtcoCode { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:NaptanCode")]
            public string NaptanCode { get; set; }
            [ChoXmlNodeRecordField(XPath = "/z:Place/z:Location/z:Translation/z:Longitude")]
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
        }

        static void Sample20()
        {
            using (var p = new ChoXmlReader<Naptan>("sample20.xml")
                .WithXPath("//NaPTAN/StopPoints/StopPoint")
                .WithXmlNamespace("z", "http://www.naptan.org.uk/")
                )
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void NSTest()
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
        }

        static void CDATATest()
        {
            string ID = null;

            string xml = @"<CUST><First_Name>Luke</First_Name> <Last_Name>Skywalker</Last_Name> <ID ID1=""1""><Name><![CDATA[1234]]></Name></ID> </CUST>";

            using (var p = new ChoXmlReader(new StringReader(xml))
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                //.Configure(c => c.RetainXmlAttributesAsNative = true)
                .WithXPath("/")
                )
            {
                Console.WriteLine(ChoJSONWriter.ToTextAll(p));
                //foreach (var rec in p)
                //	Console.WriteLine(rec.Dump());
            }
        }
        static void Sample46()
        {
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
                    w.Write(p.SelectMany(r => ((dynamic[])r.appliance.test_sets).Select(r1 => new { r.appliance.overallResult, test = r1.test })));

                using (var csv = new ChoCSVReader(sb)
                                    .WithFirstLineHeader()
                    )
                {
                    var dt = csv.AsDataTable();
                }
            }

            Console.WriteLine(sb.ToString());
        }

        static void Sample45()
        {
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

            Console.WriteLine(sb.ToString());
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
                foreach (dynamic child in (IList)obj.ChildObjects.ChildObjects)
                {
                    foreach (object rec in Flatten(child))
                        yield return rec;
                }
            }
        }

        static void Sample44()
        {
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
            Console.WriteLine(sb.ToString());


        }
        static void Sample43()
        {
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
        static void Sample42()
        {
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

            Console.WriteLine(sb.ToString());
        }

        static void Sample41()
        {
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

            Console.WriteLine(sb.ToString());
        }

        static void Sample40()
        {
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

            Console.WriteLine(sb.ToString());

        }

        static void Sample39()
        {
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

            Console.WriteLine(sb.ToString());
        }

        static void Sample38()
        {
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

            Console.WriteLine(sb.ToString());
        }

        static void Sample37()
        {
            string xml = @"<Products>
  <Product ProductCode=""C1010"" CategoryName=""Coins"" />
  <Product ProductCode=""C1012"" CategoryName=""Coins"" />
  <Product ProductCode=""C1013"" CategoryName=""Coins"" />
</Products>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml).WithXPath("/")
                //.Configure(c => c.RetainXmlAttributesAsNative = false)
                )
            {
                Console.WriteLine(ChoJSONWriter.ToTextAll(p.ToArray()));
                //foreach (var rec in p)
                //    Console.WriteLine(rec.Dump());
                //using (var w = new ChoCSVWriter(sb)
                //	.WithFirstLineHeader()
                //	)
                //	w.Write(p);
            }

            Console.WriteLine(sb.ToString());

        }

        static void Sample36()
        {
            string xml = @"<root>
  <DataRow>
    <ColumnName>Value1</ColumnName>
    <ColumnName>Value3</ColumnName>
    <ColumnName>Value4</ColumnName>
    <ColumnName>Value5</ColumnName>
    <ColumnName>Value6</ColumnName>
  </DataRow>
</root>";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoXmlReader.LoadText(xml)
                .Configure(c => c.RetainAsXmlAwareObjects = true)
                )
            {
                Console.WriteLine(ChoXmlWriter.ToTextAll(p.ToArray()));
                //foreach (var rec in p)
                //    Console.WriteLine(rec.Dump());
                //using (var w = new ChoCSVWriter(sb)
                //	.WithFirstLineHeader()
                //	)
                //	w.Write(p);
            }

            Console.WriteLine(sb.ToString());

        }

        static void Sample35()
        {
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

            Console.WriteLine(sb.ToString());
        }

        static void Sample34()
        {
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

            Console.WriteLine(sb.ToString());
        }

        static void Sample33()
        {
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
                    Console.WriteLine(rec.Dump());
            }
        }

        static void Sample32()
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
                    Console.WriteLine(rec.Dump());
            }
        }
        static void Sample31()
        {
            string xml = @"<Columns>
 <Column Name=""key1"" DataType=""Boolean"">True</Column>
 <Column Name=""key2"" DataType=""String"">Hello World</Column>
 <Column Name=""key3"" DataType=""Integer"">999</Column>
</Columns>";

            using (var p = ChoXmlReader.LoadText(xml))
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void Sample30()
        {
            StringBuilder msg = new StringBuilder();
            using (var p = new ChoXmlReader("sample30.xml")
                .WithXPath("/")
                //.WithField("packages", fieldName: "Package")
                .Configure(c => c.EmptyXmlNodeValueHandling =  ChoEmptyXmlNodeValueHandling.Ignore)
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
            Console.WriteLine(msg.ToString());
        }

        static void JSONArrayTest()
        {
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
                    Console.WriteLine(ChoJSONWriter.ToText(rec));

                    //var x = p.First();
                    //Console.WriteLine(ChoJSONWriter.ToText(x));
                    //Console.WriteLine(ChoXmlWriter.ToText(x));
            }

            Console.WriteLine(msg.ToString());
        }

        static void Sample22()
        {
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
                    Console.WriteLine(rec.Dump());
            }
        }

        static void Sample20Test()
        {
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
                Console.WriteLine(ChoJSONWriter.ToText(x));
            }
        }
        static void Sample21()
        {
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
                    Console.WriteLine(ChoJSONWriter.ToText(rec));
            }

        }

        static void Sample19()
        {
            using (var p = new ChoXmlReader("sample19.xml")
                //.WithXmlNamespace("tlp", "http://www.timelog.com/XML/Schema/tlp/v4_4")
                )
            {
                //foreach (var rec in p)
                //	Console.WriteLine(rec.Dump());
                //return;
                using (var w = new ChoCSVWriter(Console.Out)
        .WithFirstLineHeader()
        )
                {
                    w.Write(p);
                }

                Console.WriteLine();
            }
        }
        static void Sample18()
        {
            using (var p = new ChoXmlReader("sample18.xml")
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

        static void NoEncodeTest()
        {
            using (var xr = new ChoXmlReader("NoEncode.xml")
                .WithField("id", encodeValue: false)
            )
            {
                foreach (dynamic rec in xr)
                    Console.WriteLine(rec.id);
            }
        }


        static void Sample17()
        {
            using (var xr = new ChoXmlReader("Sample17.xml").WithXPath("//HouseInfo")
                .WithField("HouseNumber", fieldType: typeof(int))
                .WithField("RoomInfos", xPath: "//HouseLog/RoomInfo", fieldType: typeof(List<RoomInfo>))
                .WithField("Furnitures", xPath: "//HouseLog/RoomInfo/Furnitures/Table", fieldType: typeof(Table))
            )
            {
                foreach (dynamic rec in xr)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void HTMLTableToCSV()
        {
            using (var cr = new ChoCSVWriter("HtmlTable.csv").WithFirstLineHeader())
            {
                using (var xr = new ChoXmlReader("HTMLTable.xml").WithXPath("//tbody/tr")
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
        }

        static void BulkLoad1()
        {
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

        static void Sample16()
        {
            using (var parser = new ChoXmlReader("sample16.xml")
            )
            {
                var dict = parser.ToDictionary(i => (string)i.name, i => (object)i.value, StringComparer.CurrentCultureIgnoreCase);
                var person = dict.ToObject<Person>();
                {
                    Console.WriteLine("{0}", person.DateOfBirth);
                }
            }
        }

        static void Sample12()
        {
            using (var parser = new ChoXmlReader("sample12.xml")
            .WithField("SelectedIdValue", xPath: "//SelectedIds", fieldType: typeof(SelectedIds))
            )
            {
                foreach (dynamic rec in parser)
                {
                    Console.WriteLine("{0}", rec.GetXml());
                }
            }
        }

        public static void DynamicXmlTest()
        {
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

            Console.WriteLine(src.GetXml());
        }

        public static void Sample15()
        {
            using (var parser = new ChoXmlReader("sample15.xml")
            )
            {
                foreach (dynamic rec in parser)
                {
                    Console.WriteLine(ChoUtility.Dump(rec));
                }
            }
        }

        public static void Sample14()
        {
            using (var w = new ChoXmlWriter("sample14out.xml"))
            {
                using (var parser = new ChoXmlReader("sample14.xml")
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
        }

        public static void NullableTest()
        {
            string xml = @"<?xml version=""1.0""?>
    <Item Number = ""100"" ItemName = ""TestName1"" ItemId = ""1"" />";

            XDocument doc = XDocument.Parse(xml);

            var item = ChoXmlReader<Item>.LoadXElements(new XElement[] { doc.Root }).FirstOrDefault();
            Console.WriteLine($"ItemId: {item.ItemId}");
            Console.WriteLine($"ItemName: {item.ItemName}");
            Console.WriteLine($"Number: {item.Number}");
            Console.WriteLine($"Created: {item.Created}");
        }

        public static void Pivot1()
        {
            using (var parser = new ChoXmlReader("pivot1.xml").WithXPath(@"//Values/*")
                .WithField("Item")
                .WithField("Value")
            )
            {
                Console.WriteLine(ChoCSVWriter.ToTextAll(parser.Cast<ChoDynamicObject>().Transpose(false), 
                    new ChoCSVRecordConfiguration().Configure(c => c.FileHeaderConfiguration.HasHeaderRecord = true)));
            }
        }

        static void Sample6()
        {
            using (var parser = new ChoXmlReader<JobApplication>("sample6.xml")
            )
            {
                foreach (dynamic rec in parser)
                {
                    Console.WriteLine(ChoUtility.Dump(rec));
                }
            }
        }

        private static void XmlNullTest()
        {
            using (var parser = new ChoXmlReader("sample13.xml")
            )
            {
                //var c = parser.Select(x => (string)x.AustrittDatum).ToArray();
                using (var jw = new ChoJSONWriter("sample13.json"))
                    jw.Write(new { AustrittDatum = parser.Select(x => (string)x.AustrittDatum).ToArray() });
            }

        }

        private static string EmpXml = @"<Employees>
                <Employee Id='1'>
                    <Name isActive = 'true'>Tom</Name>
                </Employee>
                <Employee Id='2'>
                    <Name>Mark</Name>
                </Employee>
            </Employees>
        ";

        static void XmlToCSVSample7()
        {
            using (var parser = new ChoXmlReader("sample7.xml").WithXPath("/UpdateDB/Transaction")
                .WithField("Table", xPath: "/Insert/Table")
                .WithField("szCustomerID", xPath: "/Insert/Set/szCustomerID")
                .WithField("szCustomerName", xPath: "/Insert/Set/szCustomerName")
                .WithField("szExternalID", xPath: "/Insert/Set/szExternalID")
                )
            {
                using (var writer = new ChoCSVWriter("sample7.csv").WithFirstLineHeader())
                    writer.Write(parser.Where(r => r.Table == "CUSTOMER").Select(r => new { szCustomerID = r.szCustomerID, szCustomerName = r.szCustomerName, szExternalID = r.szExternalID }));
            }

        }

        static void XmlToCSVSample6()
        {
            using (var parser = new ChoXmlReader("sample6.xml").WithXPath("JobApplications")
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
                using (var writer = new ChoCSVWriter("sample6.csv").WithFirstLineHeader())
                    writer.Write(parser);
            }

        }


        static void XmlToCSVSample5()
        {
            using (var parser = new ChoXmlReader("sample5.xml").WithXPath("/PRICE")
            )
            {
                using (var writer = new ChoCSVWriter("sample5.csv").WithFirstLineHeader())
                    writer.Write(parser);
            }

        }

        static void Sample8Test()
        {
            using (var parser = new ChoXmlReader("sample8.xml").WithXPath("/root/data")
                .WithField("id", xPath: "@name")
                .WithField("text", xPath: "/value")
            )
            {
                using (var writer = new ChoJSONWriter("sample8.json")
                    .Configure(c => c.SupportMultipleContent = true)
                    )
                    writer.Write(new { Texts = parser.ToArray() });
            }
        }

        static void Sample9Test()
        {
            StringBuilder sb = new StringBuilder();
            //using (var parser = new ChoXmlReader("sample9.xml", "abc.com/api").WithXPath("/")
            //    )
            //{
            //    var x = parser.SelectMany(r1 => ((dynamic[])r1.views).Select(r2 =>
            //    new
            //    {
            //        view_id = r2.id,
            //        view_name = r2.name,
            //        view_content_url = r2.contentUrl,
            //        view_total_count = Int32.Parse(r1.pagination.totalAvailable)
            //    }));

            //    Console.WriteLine(ChoJSONWriter.ToTextAll(x));
            //}

            //return;

            int totalAvailable;
            using (var parser = new ChoXmlReader("sample9.xml", "abc.com/api").WithXPath("/tsResponse/pagination")
                .WithField("totalAvailable", fieldType: typeof(int))
                .WithField("pageNumber", fieldType: typeof(int))
            )
            {
                totalAvailable = parser.FirstOrDefault().totalAvailable;
            }

            using (var parser = new ChoXmlReader("sample9.xml", "abc.com/api").WithXPath("/tsResponse/views/view")
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

            Console.WriteLine(sb.ToString());
        }

        static void Sample10Test()
        {
            using (var parser = new ChoXmlReader("sample10.xml").WithXPath("/root/body/e1")
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
                Console.WriteLine(ChoJSONWriter.ToTextAll(parser));

                //foreach (dynamic rec in parser)
                //{
                //    Console.WriteLine(rec.Dump());
                //}
                //using (var writer = new ChoJSONWriter("sample10.json")
                //    )
                //    writer.Write(parser);
            }
        }

        static void Sample11Test()
        {
            using (var parser = new ChoXmlReader("sample11.xml").WithXPath("/members/father")
                .WithField("id")
                .WithField("sons" )
            )
            {
                Console.WriteLine(ChoJSONWriter.ToTextAll(parser));
            }
        }

        static void xMain1(string[] args)
        {
            Sample43();
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
        static void JSONToXmlSample4()
        {
            using (var parser = new ChoJSONReader<ProductionOrderFile>("sample3.json").Configure(c => c.UseJSONSerialization = true)
    )
            {
                using (var writer = new ChoXmlWriter<ProductionOrderFile>("sample31.xml").Configure(c => c.UseXmlSerialization = true))
                    writer.Write(parser);
                return;
            }
        }

        static void XmlToJSONSample4()
        {
            using (var parser = new ChoXmlReader<ProductionOrderFile>("sample4.xml").WithXPath("/").Configure(c => c.UseXmlSerialization = true)
                )
            {
                using (var writer = new ChoJSONWriter("sample3.json").Configure(c => c.UseJSONSerialization = true).Configure(c => c.SupportMultipleContent = false).Configure(c => c.Formatting = Newtonsoft.Json.Formatting.None)
                    )
                    writer.Write(parser);

                //foreach (var x in parser)
                //{
                //    Console.WriteLine(x.ProductionOrderName);
                //    Console.WriteLine("{0}", ((ICollection)x.Batches).Count);
                //    Console.WriteLine("{0}", ((ICollection)x.VariableDatas).Count);
                //}
            }

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

        static void XmlToCSVSample3()
        {
            using (var parser = ChoXmlReader.LoadXElements(XDocument.Load("sample3.xml").XPathSelectElements("//member[name='table']/value/array/data/value"))
                .WithField("id", xPath: "array/data/value[1]")
                .WithField("scanTime", xPath: "array/data/value[2]")
                .WithField("host", xPath: "array/data/value[3]")
                .WithField("vuln", xPath: "array/data/value[4]")
                .WithField("port", xPath: "array/data/value[5]")
                .WithField("protocol", xPath: "array/data/value[6]")
            )
            {
                using (var writer = new ChoCSVWriter("sample3.csv").WithFirstLineHeader())
                    writer.Write(parser);

            }

        }
        static void XmlToCSVSample2()
        {
            using (var parser = new ChoXmlReader("sample2.xml")
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
                using (var writer = new ChoCSVWriter("sample2.csv").WithFirstLineHeader())
                    writer.Write(parser);
            }

        }

        static void XmlToCSVSample()
        {
            using (var parser = new ChoXmlReader("sample.xml").WithXPath("Attributes/Attribute")
                .WithField("Name", xPath: "Name")
                .WithField("Value", xPath: "value")
                )
            {
                using (var writer = new ChoCSVWriter("sample.csv"))
                    writer.Write(parser.Select(kvp => kvp.Value).ToExpandoObject());
                //Console.WriteLine(ChoCSVWriter.ToText(parser.Select(kvp => kvp.Value).ToExpandoObject()));
            }

        }

        static void ToDataTable()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlReader<EmployeeRec>(reader))
            {
                writer.WriteLine(EmpXml);

                writer.Flush();
                stream.Position = 0;

                var dt = parser.AsDataTable();
            }
        }

        static void LoadTest()
        {
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

        static void LoadTextTest()
        {
            foreach (var x in ChoXmlReader.LoadText(@"<books><book name=""xxx"" author=""Tom""><title>C++</title></book><book name=""yyyy""></book></books>"))
            {
                Console.WriteLine(x.ToStringEx());
            }
        }

        static void POCOTest()
        {
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
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void ConfigFirstDynamicTest()
        {
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
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void QuickTest()
        {
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
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void CodeFirstTest()
        {
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
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void QuickTestWithXmlNS()
        {
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
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        public partial class EmployeeRecSimple
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [XmlRoot(ElementName = "Employee")]
        public class EmployeeRec
        {
            [XmlAttribute]
            [ChoXmlNodeRecordField(XPath = "//@Id" )]
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

            public override string ToString()
            {
                return "{0}. {1}. {2}".FormatString(Id, Name, IsActive);
            }
        }
    }
}
