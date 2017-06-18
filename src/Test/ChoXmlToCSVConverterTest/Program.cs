using ChoETL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoXmlToCSVConverterTest
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var xmlReader = new ChoXmlReader("Users.xml"))
            {
                using (var csvWriter = new ChoCSVWriter("Users.csv").WithFirstLineHeader())
                    csvWriter.Write(xmlReader);
            }
        }
    }
}
