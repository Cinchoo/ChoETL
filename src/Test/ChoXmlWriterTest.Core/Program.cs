using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using ChoETL;

namespace ChoXmlWriterTest.Core
{
    public class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            Json2Xml2();

        }
        public class Address
        {
            public string streetAddress { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string postalCode { get; set; }
        }

        public class PhoneNumber
        {
            public string type { get; set; }
            public string number { get; set; }
        }

        //[ChoXmlRecordObject(XPath = "Emps1/Emp")]
        [XmlRoot("Emp")]
        public class Employee
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public int age { get; set; }
            public Address address { get; set; }
            public List<PhoneNumber> phoneNumber { get; set; }
        }
        public static void Json2Xml2()
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

            using (var r = ChoJSONReader<Employee>.LoadText(json))
            {
                using (var w = new ChoXmlWriter<Employee>(Console.Out)
                    //.WithNodeName("C")
                      )
                {
                    w.Write(r);
                }
            }

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
                    .UseXmlSerialization()
                    .Configure(c => c.OmitXsiNamespace = true)
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                       )
                {
                    w.Write(r);
                }
            }

        }
    }
}
