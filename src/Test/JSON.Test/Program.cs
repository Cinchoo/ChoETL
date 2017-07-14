using ChoETL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JSON.Test
{
    class Program
    {
        static string JSON =@"
         {
           ""Name"": ""Apple"",
           ""Expiry"": ""\u123"",
           ""Tizes"": [
             ""Small""
           ]
    }";

    static void Main(string[] args)
        {
            Console.WriteLine(ChoJSONParser.Parse(JSON).ToStringEx());
        }
    }
}
