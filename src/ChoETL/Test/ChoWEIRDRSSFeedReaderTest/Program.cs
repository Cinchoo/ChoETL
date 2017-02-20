using ChoETL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ChoWEIRDRSSFeedReaderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            HttpWebRequest request = HttpWebRequest.Create("https://www.wired.com/feed/") as HttpWebRequest;

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            using (Stream responseStream = response.GetResponseStream())
            foreach (var item in new ChoXmlReader(responseStream).WithXPath("//rss/channel/item"))
            {
                Console.WriteLine(item.ToStringEx());
            }

            Console.WriteLine("Done.");
            Console.ReadLine();
        }
    }
}
