using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChoETL;
using System.IO;

namespace ChoXmlReaderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            QuickTest();
        }

        static void QuickTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoXmlReader(reader)) //.WithFields("name", "author", "title").ColumnCountStrict()) // new StringReader("<books><book><name>C++</name><author>Mark</author></book><book><name>VB</name><author>Tom</author></book><book><name>Java</name><author>Tom</author></book></books>")))
            {
                writer.WriteLine(@"<books><book name=""xxx"" author=""Tom""><title>C++</title></book><book name=""yyyy""></book></books>");

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
