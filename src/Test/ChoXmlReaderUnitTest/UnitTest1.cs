using System;
using System.IO;
using System.Net;
using ChoETL;
using NUnit.Framework;

namespace ChoXmlReaderUnitTest
{
    [TestFixture]
    public class UnitTest1
    {
        //[Test]
        public void ComplexTest1()
        {
            string xml = @"<SalesLead>
    <Customer>
         <Name part=""first"">Foo</Name>
         <Name part=""last"">Bar</Name>
    </Customer>
</SalesLead>";
        }
        public void Test1()
        {
            HttpWebRequest request = HttpWebRequest.Create("https://www.wired.com/feed/") as HttpWebRequest;

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            using (Stream responseStream = response.GetResponseStream())
                foreach (var item in new ChoXmlReader(responseStream))
                {
                    Console.WriteLine(item.ToStringEx());
                }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
