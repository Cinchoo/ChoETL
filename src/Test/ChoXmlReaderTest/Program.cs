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
            using (var parser = new ChoXmlReader(new StringReader("<root><item>Demo</item></root>")))
            {
                writer.WriteLine(@"<books><book name=""xxx""></book><book name=""yyyy""></book></books>");

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
