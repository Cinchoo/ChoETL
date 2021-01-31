using ChoETL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoXmlToCSVConverterTest
{
    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {
        static void Main(string[] args)
        {
            XMLToCSVConverterTest();
        }

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }

        //[Test]
        public static void XMLToCSVConverterTest()
        {
            using (var xmlReader = new ChoXmlReader("Users.xml")
                .WithXPath("users/user")
                )
            {
                //foreach (var rec in xmlReader)
                //    Console.WriteLine(rec.Dump());
                //return;
                using (var csvWriter = new ChoCSVWriter("Users.csv").WithFirstLineHeader()
                    .Configure(c => c.UseNestedKeyFormat = false)
                    .WithField("Id").WithField("last_name", fieldName: "Name").ThrowAndStopOnMissingField())
                    csvWriter.Write(xmlReader);
            }
        }
    }
}
