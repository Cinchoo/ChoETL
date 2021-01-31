using ChoETL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoXmlToCSVSample
{
    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {
        static void Main(string[] args)
        {
            Sample2();
        }

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }

        public static string FileNameSample1XML => "sample1.xml";
        public static string FileNameTest1ActualCSV => "Test1Actual.csv";
        public static string FileNameTest1ExpectedCSV => "Test1Expected.csv";
        public static string FileNameTest2ActualCSV => "Test2Actual.csv";
        public static string FileNameTest2ExpectedCSV => "Test2Expected.csv";
        public static string FileNameSample2XML => "sample2.xml";
        //[Test]
        public static void Sample2()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject {
                    { "impotno",(long)891258},
                    { "productlineitem",new object[] {
                        new ChoDynamicObject {
                            { "price", "450" },
                            { "red","6.50"},
                            { "Small-price","39" },
                            { "Big-price", "3229" },
                            { "lineitem-text","Grand create" },
                            { "basis","234.00"} },
                        new ChoDynamicObject {
                            { "price", "432" },
                            { "red","12"},
                            { "Small-price","44" },
                            { "Big-price", "34" },
                            { "lineitem-text","Small create" },
                            { "basis","44.00"} }
                        }
                    }
                },
                new ChoDynamicObject {
                    { "impotno",(long)991258},
                    { "productlineitem",new object[] {
                        new ChoDynamicObject {
                            { "price", "4500" },
                            { "red","6.50"},
                            { "Small-price","39" },
                            { "Big-price", "3229" },
                            { "lineitem-text","1Grand create" },
                            { "basis","234.00"} },
                        new ChoDynamicObject {
                            { "price", "4320" },
                            { "red","12"},
                            { "Small-price","44" },
                            { "Big-price", "34" },
                            { "lineitem-text","1Small create" },
                            { "basis","44.00"} }
                        }
                    }
                }
            };
            List<object> actual = new List<object>();

                using (var xmlReader = new ChoXmlReader(FileNameSample2XML, "http://tempuri.org").WithXPath("/impots/impot")
                    .WithField("impotno", xPath: "x:original-impot-no")
                    .WithField("productlineitem", xPath: "x:product-lineitems/x:product-lineitem")
                    //.Setup(s => s.BeforeRecordFieldLoad += (o, e) =>
                    //{
                    //    var x = e;
                    //})
                    )
                {
                    foreach (dynamic i in xmlReader)
                    {
                        actual.Add(i);

                    }
                }

                CollectionAssert.AreEqual(expected, actual);
        }

        //[Test]
        public static void Test2()
        {
            using (var csvWriter = new ChoCSVWriter(FileNameTest2ActualCSV).WithFirstLineHeader())
            {
                using (var xmlReader = new ChoXmlReader(FileNameSample1XML))
                    csvWriter.Write(xmlReader);
            }

            FileAssert.AreEqual(FileNameTest2ExpectedCSV, FileNameTest2ActualCSV);
        }

        //[Test]
        public static void Test1()
        {
            string _xml = @"
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

            using (var csvWriter = new ChoCSVWriter(FileNameTest1ActualCSV).WithFirstLineHeader())
            {
                using (var xmlReader = new ChoXmlReader(new StringReader(_xml.Trim())))
                    csvWriter.Write(xmlReader);
            }

            FileAssert.AreEqual(FileNameTest1ExpectedCSV, FileNameTest1ActualCSV);
        }
    }
}
