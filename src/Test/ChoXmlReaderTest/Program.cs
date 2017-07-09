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
         
        static void Main(string[] args)
        {
            //dynamic p = new ChoPropertyBag();
            //p.Name = "Raj";
            //p.Zip = "10020";

            //foreach (var kvp in ChoExpandoObjectEx.ToExpandoObject(p))
            //    Console.WriteLine(kvp);
            //return;
            XmlToJSONSample4();
                JSONToXmlSample4();
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
                using (var writer = new ChoJSONWriter("sample3.json").Configure(c => c.UseJSONSerialization = true))
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

        static void XmlToCSVSample1()
        {
            using (var parser = new ChoXmlReader("sample.xml").WithXPath("Attributes/Attribute")
                .WithField("Name", xPath: "Name")
                .WithField("Value", xPath: "value")
                )
            {
                Console.WriteLine(ChoCSVWriter.ToText(parser.Where(kvp => (string)kvp.Name == "PersonN").Select(kvp => kvp.Value).ToExpandoObject()));
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

            public override string ToString()
            {
                return "{0}. {1}. {2}".FormatString(Id, Name, IsActive);
            }
        }
    }
}
