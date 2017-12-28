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
    public class Program
    {
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
                using (var writer = new ChoCSVWriter("sample6.csv"))
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
                using (var writer = new ChoJSONWriter("sample9.json")
                    )
                {
                    foreach (dynamic rec in parser)
                        writer.Write(new { view_id = rec.view_id, view_name = rec.view_name, view_content_url = rec.view_content_url, view_total_count = rec.view_total_count, view_total_available = totalAvailable });
                    writer.Write(parser);
                }
            }
        }

        static void Sample10Test()
        {
            using (var parser = new ChoXmlReader("sample10.xml").WithXPath("/root/body/e1")
                .WithField("tag1", xPath: "en/tag1")
                .WithField("tag2", xPath: "en/tag2")
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
                foreach (dynamic rec in parser)
                {
                    Console.WriteLine(rec.DumpAsJson());
                }
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
                foreach (dynamic rec in parser)
                {
                    Console.WriteLine(ChoUtility.DumpAsJson(rec.sons));
                }
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

        static void Sample12()
        {
            using (var parser = new ChoXmlReader("sample12.xml")
                .WithField("SelectedIds/Id", fieldType: typeof(int[]))
            )
            {
                foreach (dynamic rec in parser)
                {
                    Console.WriteLine(ChoUtility.Dump(rec));
                }
            }
        }

        static void Main(string[] args)
        {
            Sample12();
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
