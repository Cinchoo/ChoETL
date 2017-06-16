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

namespace ChoXmlReaderTest
{
    public class Program
    {
        private static string EmpXml = @"<Employees>
                <Employee Id='1'>
                    <Name>Tom</Name>
                </Employee>
                <Employee Id='2'>
                    <Name>Mark</Name>
                </Employee>
            </Employees>
        ";
         
        static void Main(string[] args)
        {
            POCOTest();
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
            using (var parser = new ChoXmlReader<EmployeeRec>(reader).UseXmlSerialization())
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

            public override string ToString()
            {
                return "{0}. {1}.".FormatString(Id, Name);
            }
        }
    }
}
