using System;
using ChoETL;

namespace ChoXmlWriterTest.Core
{
    class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            Json2Xml1();

        }

        public static void Json2Xml1()
        {
            string json = @"
[
    {
        ""firstName"": ""John"",
        ""lastName"": ""Smith"",
        ""age"": 25,
        ""address"": {
            ""streetAddress"": ""21 2nd Street"",
            ""city"": ""New York"",
            ""state"": ""NY"",
            ""postalCode"": ""10021""
        },
        ""phoneNumber"": [
            {
                ""type"": ""home"",
                ""number"": ""212 555-1234""
            },
            {
                ""type"": ""fax"",
                ""number"": ""646 555-4567""
            }
        ]
    },
    {
        ""firstName"": ""Tom"",
        ""lastName"": ""Mark"",
        ""age"": 50,
        ""address"": {
            ""streetAddress"": ""10 Main Street"",
            ""city"": ""Edison"",
            ""state"": ""NJ"",
            ""postalCode"": ""08837""
        },
        ""phoneNumber"": [
            {
                ""type"": ""home"",
                ""number"": ""732 555-1234""
            },
            {
                ""type"": ""fax"",
                ""number"": ""609 555-4567""
            }
        ]
    }
]
";
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoXmlWriter(Console.Out)
                    //.UseXmlSerialization()
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                       )
                {
                    w.Write(r);
                }
            }

        }
    }
}
