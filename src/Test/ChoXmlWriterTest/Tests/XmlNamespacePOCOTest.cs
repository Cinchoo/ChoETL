using ChoETL;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ChoXmlWriterTest
{
    [XmlRoot(ElementName = "readCase", Namespace = "http://tempuri")]
    public class ReadCase1
    {
        [XmlElement(ElementName = "version", Namespace = "http://tempuri")]
        public string VersionAsOf { get; set; }
    }
    [XmlRoot(ElementName = "readCase", Namespace = "http://tempuri")]
    public class ReadCase
    {
        [XmlElement(ElementName = "version", Namespace = "http://tempuri")]
        public BaseUtcTimeStamp VersionAsOf { get; set; }
    }
    public struct BaseUtcTimeStamp
    {
        private string _utcTimestamp;
        [XmlText]
        public string UtcTimestamp { get => _utcTimestamp; set { } } // set is needed for XmlSerializer

        public BaseUtcTimeStamp(DateTime utcDateTime)
        {
            //if (utcDateTime.Kind != DateTimeKind.Utc)
            //{
            //    throw new ArgumentException("Given dateTime must be Utc.");
            //}

            _utcTimestamp = utcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }
    }

    public partial class Program
    {
        //Serialize xml 
        [Test]
        public static void XmlNSPOCOTest_1()
        {
            string expected = @"<case:readCases xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:case=""http://tempuri"">
  <case:readCase>
    <case:version>v1.0.0.1</case:version>
  </case:readCase>
</case:readCases>";

            StringBuilder actual = new StringBuilder();

            using (var w = new ChoXmlWriter<ReadCase1>(actual)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .WithXmlNamespace("case", "http://tempuri")
                .Configure(c => c.OmitXsiNamespace = true)
                )
            {
                w.Write(new ReadCase1
                {
                    VersionAsOf = "v1.0.0.1", //new BaseUtcTimeStamp(DateTime.Now)
                });
            }

            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expected, actual.ToString());
        }

        //Serialize xml using xmlserialization
        [Test]
        public static void XmlNSPOCOTest_2()
        {
            string expected = @"<case:readCases xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:case=""http://tempuri"">
  <case:readCase>
    <case:version>v1.0.0.1</case:version>
  </case:readCase>
</case:readCases>";

            StringBuilder actual = new StringBuilder();

            using (var w = new ChoXmlWriter<ReadCase1>(actual)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .UseXmlSerialization()
                .WithXmlNamespace("case", "http://tempuri")
                )
            {
                w.Write(new ReadCase1
                {
                    VersionAsOf = "v1.0.0.1", //new BaseUtcTimeStamp(DateTime.Now)
                });
            }

            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expected, actual.ToString());
        }

        //Serialize xml using xmlserialization
        [Test]
        public static void XmlTextTest_1()
        {
            string expected = @"<case:readCases xmlns:xml=""http://www.w3.org/XML/1998/namespace"" xmlns:case=""http://tempuri"">
  <case:readCase>
    <case:version>2023-01-01T11:30:00Z</case:version>
  </case:readCase>
</case:readCases>";

            StringBuilder actual = new StringBuilder();

            using (var w = new ChoXmlWriter<ReadCase>(actual)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .UseXmlSerialization()
                .WithXmlNamespace("case", "http://tempuri")
                )
            {
                w.Write(new ReadCase
                {
                    VersionAsOf = new BaseUtcTimeStamp(new DateTime(2023,01,01, 11, 30, 00))
                });
            }

            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expected, actual.ToString());
        }
    }
}
