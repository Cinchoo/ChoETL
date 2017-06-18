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
                using (var csvWriter = new ChoCSVWriter("Users.csv").WithFirstLineHeader().
                    WithField("Id", fieldPosition: 1).WithField("last_name1", fieldName: "Name", fieldPosition: 10).ThrowAndStopOnMissingField())
                    csvWriter.Write(xmlReader);
            }
        }
    }
}
