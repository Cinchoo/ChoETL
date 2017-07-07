using ChoETL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoJSONWriterTest
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var w = new ChoJSONWriter("test.json"))
            {
                w.Write(ChoEnumerable.AsEnumerable(() =>
                {
                    return new { Address = new string[] { "NJ", "NY" } ,Name = "Raj", Zip = "08837"};
                }));
                //w.Write(new { Name = "Raj", Zip = "08837", Address = new { City = "New York", State = "NY" } });
            }
        }
    }
}
