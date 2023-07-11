using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChoETL;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ChoCSVReaderTest.Test
{
    public class Issues
    {
        [Test]
        public void CanReadTabDelimitedWithBlankFieldValue()
        {
            using (var reader = ChoTSVReader.LoadText("A\t\tB").ErrorMode(ChoErrorMode.ThrowAndStop))
            {
                foreach (var record in reader)
                {
                    Assert.NotNull(record);
                    Assert.AreEqual("A", record.Column1);
                    Assert.Null(record.Column2);
                    Assert.AreEqual("B", record.Column3);
                }
            }
        }
        [Test]
        public static void Issues01_Test()
        {
            typeof(ChoCSVReader).GetAssemblyVersion().Print();
            "".Print();

            string expected = @"[
  {
    ""CustomerPo"": ""7WKHKLXF"",
    ""OrderName"": null,
    ""Line"": {
      ""ModelNumber"": ""RJ07-45-SS-BLACK"",
      ""ShipDate"": ""2021-02-10T00:00:00""
    },
    ""ShipToAddress"": {
      ""Address1"": {
        ""Value"": ""TESTADDRESS""
      },
      ""City"": {
        ""Value"": null
      },
      ""Zip"": {
        ""Value"": null
      }
    }
  },
  {
    ""CustomerPo"": null,
    ""OrderName"": null,
    ""Line"": {
      ""ModelNumber"": null,
      ""ShipDate"": ""0001-01-01T00:00:00""
    },
    ""ShipToAddress"": {
      ""Address1"": {
        ""Value"": null
      },
      ""City"": {
        ""Value"": null
      },
      ""Zip"": {
        ""Value"": null
      }
    }
  },
  {
    ""CustomerPo"": ""7SK78LRY"",
    ""OrderName"": null,
    ""Line"": {
      ""ModelNumber"": ""RJ07-45-SS-BLACK"",
      ""ShipDate"": ""2021-02-10T00:00:00""
    },
    ""ShipToAddress"": {
      ""Address1"": {
        ""Value"": ""TESTADDRESS""
      },
      ""City"": {
        ""Value"": null
      },
      ""Zip"": {
        ""Value"": null
      }
    }
  }
]";

            string csv = @"PO,Model Number,Window Start,Window End,Expected Date,Quantity Requested,Unit Cost,Coming From,Address1
                7WKHKLXF,RJ07-45-SS-BLACK,2/10/2021,2/17/2021,2/10/2021,152,51.51,New York,TestAddress
                ,,,,,,,,

                7SK78LRY,RJ07-45-SS-BLACK,2/10/2021,2/17/2021,2/10/2021,24,51.51,New York,TestAddress";

            var mapping = new Dictionary<string, string>
            {
                { "PO", "CustomerPo" },
                { "Model Number", "Line.ModelNumber" },
                { "Address1", "ShipToAddress.Address1" },
                { "Window Start", "Line.ShipDate" },
            };

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

            //Console.WriteLine(csv);
            List<string> headers = null;
            using (var r = new ChoCSVReader(stream)
                .WithFirstLineHeader()
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true))
                headers = ((string[])r.First().KeysArray).ToList();

            var config = new ChoCSVRecordConfiguration<SalesOrder1>()
                .WithFirstLineHeader()
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure(c => c.IgnoreEmptyLine = true)
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true);

            foreach (var header in headers)
            {
                if (mapping.TryGetValue(header, out var propName))
                    config.Map(propName, header);
            }

            using (var re = new ChoCSVReader<SalesOrder1>(stream, config)
                .WithMaxScanRows(2)
                .QuoteAllFields()
                .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Any))
            {
                var result = re.ToArray();
                result.Print();

                var actual = JsonConvert.SerializeObject(result, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class SalesAddress1
        {
            public CustomString Address1 { get; set; }
            public CustomString City { get; set; }
            public CustomString Zip { get; set; }
        }

        public class SalesOrder1
        {
            public string CustomerPo { get; set; }
            public string OrderName { get; set; }
            public SalesOrderLine Line { get; set; }
            public SalesAddress1 ShipToAddress { get; set; }
        }

        public class SalesOrderLine
        {
            public string ModelNumber { get; set; }
            public DateTime ShipDate { get; set; }
        }

        public struct CustomString
        {
            public string Value { get; }

            public CustomString(string val)
            {
                Value = val?.ToUpper();
            }

            public static implicit operator string(CustomString s) => s.Value;
            public static implicit operator CustomString(string s) => new CustomString(s);
            public override string ToString() => Value;
        }

    }
}
