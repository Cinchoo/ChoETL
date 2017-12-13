using ChoETL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoXmlToCSVSample
{
    class Program
    {
        static string _xml = @"
<?xml version=""1.0"" encoding=""utf-8"" ?> 
<users>
    <user>
        <userId>1</userId> 
        <firstName>George</firstName> 
        <lastName>Washington</lastName> 
    </user>
    <user>
        <userId>2</userId> 
        <firstName>Abraham</firstName> 
        <lastName>Lincoln</lastName> 
    </user>
    ...
</users>
";
        static void Main(string[] args)
        {
            Test2();
        }

        private static void Test2()
        {
            using (var csvWriter = new ChoCSVWriter("sample1.csv").WithFirstLineHeader())
            {
                using (var xmlReader = new ChoXmlReader("sample1.xml"))
                    csvWriter.Write(xmlReader);
            }
        }

        private static void Test1()
        {
            using (var csvWriter = new ChoCSVWriter("users.csv").WithFirstLineHeader())
            {
                using (var xmlReader = new ChoXmlReader(new StringReader(_xml.Trim())))
                    csvWriter.Write(xmlReader);
            }
        }
    }
}
