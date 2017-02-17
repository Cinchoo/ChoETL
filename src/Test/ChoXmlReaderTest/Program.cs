using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChoETL;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace ChoXmlReaderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            QuickTest();
        }

        static void LoadTextTest()
        {
            foreach (var x in ChoXmlReader.LoadText(@"<books><book name=""xxx"" author=""Tom""><title>C++</title></book><book name=""yyyy""></book></books>"))
            {
                Console.WriteLine(x.ToStringEx());
            }
        }

        static void QuickTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlReader(reader))
            {
                writer.WriteLine(@"<employee id='1'><name>Tom</name><addr>123 street</addr></employee>");

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
            using (var parser = new ChoXmlReader(reader).WithXmlNamespace("cont", "www.tutorialspoint.com/profile", true).WithField("name", "//cont:name"))
            {
                writer.WriteLine(@"<cont:contacts xmlns:cont=""www.tutorialspoint.com/profile"">
                <cont:contact >
                    <cont:contact >
                       <cont:name>Tanmay Patilx</cont:name>
                       <cont:company>TutorialsPoint</cont:company>
                       <cont:phone> (011) 123 - 4567 </cont:phone>
                    </cont:contact> 
                   <cont:name>Tanmay Patil</cont:name>
                   <cont:company>TutorialsPoint</cont:company>
                   <cont:phone> (011) 123 - 4567 </cont:phone>
                </cont:contact> 
                <cont:contact >
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
    }
}
