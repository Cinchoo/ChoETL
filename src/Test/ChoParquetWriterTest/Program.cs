using ChoETL;
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Globalization;
using System.Data.SqlClient;
using System.Data;

namespace ChoParquetWriterTest
{
    class Program
    {
        private static DateTime _currentDateTime = new DateTime(2023, 06, 10, 12, 10, 30);

        public enum EmployeeType
        {
            [System.ComponentModel.Description("Full Time Employee")]
            Permanent = 0,
            [System.ComponentModel.Description("Temporary Employee")]
            Temporary = 1,
            [System.ComponentModel.Description("Contract Employee")]
            Contract = 2
        }
        [Test]
        public static void QuickTest()
        {
            string filePath = "quicktest.parquet";

            string csv = @"Id, Name
1, Tom
2, Mark";

            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Tom""
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                .QuoteAllFields()
                )
            {
                using (var w = new ChoParquetWriter(filePath))
                {
                    w.Write(r);
                }
            }

            var actual = ReadParquetFileAsJSON(filePath);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Test1()
        {
            string filePath = "test1.parquet";

            string expected = @"[
  {
    ""Cust_ID"": ""TCF4338"",
    ""CustName"": ""INDEXABLE CUTTING TOOL"",
    ""CustOrder"": ""4/11/2016"",
    ""Salary"": 100000.0,
    ""Guid"": ""56531508-89c0-4ecf-afaf-cdf5aec56b19""
  },
  {
    ""Cust_ID"": ""CGO9650"",
    ""CustName"": ""Comercial Tecnipak Ltda"",
    ""CustOrder"": ""7/11/2016"",
    ""Salary"": 80000.0,
    ""Guid"": ""56531508-89c0-4ecf-afaf-cdf5aec56b19""
  }
]";

            string csv = @"Cust_ID,CustName,CustOrder,Salary,Guid
TCF4338,INDEXABLE CUTTING TOOL,4/11/2016,""$100,000"",56531508-89c0-4ecf-afaf-cdf5aec56b19
CGO9650,Comercial Tecnipak Ltda,7/11/2016,""$80,000"",56531508-89c0-4ecf-afaf-cdf5aec56b19";

            //ChoTypeConverterFormatSpec.Instance.TreatCurrencyAsDecimal = false;
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                .QuoteAllFields()
                .TypeConverterFormatSpec(ts => ts.TreatCurrencyAsDecimal = false)
                )
            {
                var recs = r.ToArray();
                using (var w = new ChoParquetWriter(filePath)
                    //.Configure(c => c.TreatDateTimeAsDateTimeOffset = false)
                    )
                {
                    w.Write(recs);
                }
            }

            var actual = ReadParquetFileAsJSON(filePath, null, new JsonSerializerSettings
            { 
                Formatting = Formatting.Indented,
                DateFormatString = "M/dd/yyyy",
            });
            Assert.AreEqual(expected, actual);
        }
        public enum SalesGroupEnum { None, First, Second };

        public class SalesItem
        {

            public Guid? salesItemId { get; set; }

            public Guid? tenantId { get; set; }

            public Guid? parentSalesItemId { get; set; }

            public Guid? salesId { get; set; }

            public Guid? productId { get; set; }

            public Guid? groupId { get; set; }

            public Guid? bundleId { get; set; }

            public string description { get; set; }

            public decimal? linePrice { get; set; }

            public int? quantity { get; set; }

            public decimal? detailAmount { get; set; }

            public decimal? taxRate { get; set; }

            public decimal? taxAmount { get; set; }

            public decimal? unitServiceFee { get; set; }

            public decimal? serviceFeeDetailAmount { get; set; }

            public decimal? serviceTaxRate { get; set; }

            public bool? isRevenue { get; set; } = true;

            public SalesGroupEnum groupEnum { get; set; }

            public string modifiedDate { get; set; }

            public bool? isDeleted { get; set; }

            public decimal? serviceFeeTaxAmount { get; set; }

            public List<SalesPOCO> derivativeSalesList { get; set; }

            //public List<SalesPOCO>? salesItemList { get; set; } = new();

            //public SalesList? salesList { get; set; }
        }

        public class SalesPOCO
        {
            public int salesId { get; set; }
            public string salesName { get; set; }

        }

        [Test]
        public static void Issue295()
        {
            string json = @"[
	{
		""salesItemId"": ""9720357f-3782-4f87-8bbe-f7f167ae3a11"",
		""tenantId"": ""9720357f-3782-4f87-8bbe-f7f167ae3a11"",
		""parentSalesItemId"": null,
		""salesId"": ""9720357f-3782-4f87-8bbe-f7f167ae3a11"",
		""productId"": ""9720357f-3782-4f87-8bbe-f7f167ae3a11"",
		""groupId"": null,
		""bundleId"": null,
		""description"": ""Test"",
		""linePrice"": 110.0,
		""quantity"": 1,
		""detailAmount"": 15.0,
		""taxRate"": 0.95,
		""taxAmount"": 6.53,
		""unitServiceFee"": 12.0,
		""serviceFeeDetailAmount"": 15.0,
		""serviceTaxRate"": 1.25,
		""isRevenue"": false,
		""groupEnum"": ""None"",
		""modifiedDate"": ""2020-01-01T00:00:00.100000"",
		""isDeleted"": false,
		""serviceFeeTaxAmount"": 13.0,
		""derivativeSalesList"": [
            {
                ""salesId"": 1,
                ""salesName"": ""Tom""
            },
            {
                ""salesId"": 2,
                ""salesName"": ""Mark""
            },
        ],
		""salesItemList"": [],
		""salesList"": null
	}
]";

            string expected = @"[
  {
    ""salesItemId"": ""9720357f-3782-4f87-8bbe-f7f167ae3a11"",
    ""tenantId"": ""9720357f-3782-4f87-8bbe-f7f167ae3a11"",
    ""parentSalesItemId"": null,
    ""salesId"": ""9720357f-3782-4f87-8bbe-f7f167ae3a11"",
    ""productId"": ""9720357f-3782-4f87-8bbe-f7f167ae3a11"",
    ""groupId"": null,
    ""bundleId"": null,
    ""description"": ""Test"",
    ""linePrice"": ""110"",
    ""quantity"": 1,
    ""detailAmount"": ""15"",
    ""taxRate"": ""0.95"",
    ""taxAmount"": ""6.53"",
    ""unitServiceFee"": ""12"",
    ""serviceFeeDetailAmount"": ""15"",
    ""serviceTaxRate"": ""1.25"",
    ""isRevenue"": false,
    ""groupEnum"": ""0"",
    ""modifiedDate"": ""2020-01-01T00:00:00.100000"",
    ""isDeleted"": false,
    ""serviceFeeTaxAmount"": ""13"",
    ""derivativeSalesList"": [
      {
        ""salesId"": 1,
        ""salesName"": ""Tom""
      },
      {
        ""salesId"": 2,
        ""salesName"": ""Mark""
      }
    ]
  }
]";

            string filePath = "Issue295.parquet";
            //ConvertJson2Parquet<SalesItem>(json, filePath);
            using (var r = ChoJSONReader<SalesItem>.LoadText(json)
                .JsonSerializationSettings(js => js.DateParseHandling = DateParseHandling.None)
                )
            {
                var recs = r.ToArray();
                recs.Print();

                using (var w = new ChoParquetWriter(filePath)
                    .Configure(c => c.TreatDateTimeAsString = true)
                    .TypeConverterFormatSpec(ts => ts.DateTimeOffsetFormat = "yyyy-MM-ddThh:mm:ss.fzzz")
                    .Configure(c => c.ArrayValueNamePrefix = String.Empty)
                    )
                {
                    foreach (var rec in recs)
                        w.Write(rec);
                }
            }

            var actual = ReadParquetFileAsJSON(filePath, jsonSerializerSettings: new JsonSerializerSettings()
            {
                DateFormatString = "yyyy-MM-ddThh:mm:ss.fzzz",
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Formatting = Formatting.Indented
            });
            Assert.AreEqual(expected, actual);
        }

        private static void ConvertJson2Parquet<T>(string json, string parquetFilePath)
        {
            using (var r = ChoJSONReader<T>.LoadText(json)
                .JsonSerializationSettings(js => js.DateParseHandling = DateParseHandling.DateTimeOffset)
                )
            {
                var recs = r.ToArray();

                recs.Print();

                using (var w = new ChoParquetWriter(parquetFilePath)
                    .Configure(c => c.TreatDateTimeAsString = true)
                    .TypeConverterFormatSpec(ts => ts.DateTimeOffsetFormat = "yyyy-MM-ddThh:mm:ss.fzzz")
                    .Configure(c => c.ArrayValueNamePrefix = String.Empty)
                    )
                {
                    foreach (var rec in recs)
                        w.Write(rec);
                }
            }
        }

        [Test]
        public static void Test1_1()
        {
            string filePath = "test1_1.parquet";

            string expected = @"[
  {
    ""Cust_ID"": ""TCF4338"",
    ""CustName"": ""INDEXABLE CUTTING TOOL"",
    ""CustOrder"": ""2016-04-11T12:00:00+00:00"",
    ""Salary"": 100000.0,
    ""Guid"": ""56531508-89c0-4ecf-afaf-cdf5aec56b19""
  },
  {
    ""Cust_ID"": ""CGO9650"",
    ""CustName"": ""Comercial Tecnipak Ltda"",
    ""CustOrder"": ""2016-07-11T12:00:00+00:00"",
    ""Salary"": 80000.0,
    ""Guid"": ""56531508-89c0-4ecf-afaf-cdf5aec56b19""
  }
]";

            string csv = @"Cust_ID,CustName,CustOrder,Salary,Guid
TCF4338,INDEXABLE CUTTING TOOL,4/11/2016,""$100,000"",56531508-89c0-4ecf-afaf-cdf5aec56b19
CGO9650,Comercial Tecnipak Ltda,7/11/2016,""$80,000"",56531508-89c0-4ecf-afaf-cdf5aec56b19";

            //ChoTypeConverterFormatSpec.Instance.TreatCurrencyAsDecimal = false;
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                .QuoteAllFields()
                .TypeConverterFormatSpec(ts => ts.TreatCurrencyAsDecimal = false)
                )
            {
                var recs = r.ToArray();
                using (var w = new ChoParquetWriter(filePath)
                    //.Configure(c => c.TreatDateTimeAsDateTimeOffset = true)
                    )
                {
                    w.Write(recs);
                }
            }

            var actual = ReadParquetFileAsJSON(filePath, jsonSerializerSettings: new JsonSerializerSettings()
            {
                DateFormatString = "yyyy-MM-ddThh:mm:sszzz",
                DateTimeZoneHandling= DateTimeZoneHandling.Utc,
                Formatting = Formatting.Indented
            });
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void EnumTest()
        {
            string filePath = "EnumTest.parquet";

            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Tom"",
    ""EmpType"": 1
  }
]";
            //ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Description;

            using (var w = new ChoParquetWriter(filePath)
                .WithField("Id")
                .WithField("Name")
                .WithField("EmpType")
                .TypeConverterFormatSpec(ts => ts.EnumFormat = ChoEnumFormatSpec.Description)
                )
            {
                w.Write(new
                {
                    Id = 1,
                    Name = "Tom",
                    EmpType = EmployeeType.Temporary
                });
            }

            using (var r = new ChoParquetReader(filePath)
                .WithField("Id")
                .WithField("Name")
                .WithField("EmpType", fieldType: typeof(EmployeeType))
                .TypeConverterFormatSpec(ts => ts.EnumFormat = ChoEnumFormatSpec.Description)
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CSVArrayToParquet()
        {
            string filePath = "CSVArrayToParquet.parquet";
            string csv = @"id,name,friends/0,friends/1
1,Tom,Dick,Harry";

            using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                .Configure(c => c.AutoArrayDiscovery = true)
                .Configure(c => c.ArrayIndexSeparator = '/')
                )
            {
                using (var w = new ChoParquetWriter(filePath)
                    .Configure(c => c.UseNestedKeyFormat = false)
                    .WithField("id")
                    .WithField("name")
                    .WithField("friends", fieldType: typeof(byte[]), valueConverter: o => o.Serialize())
                    )
                {
                    w.Write(r);
                }
            }

            string expected = @"[
  {
    ""id"": ""1"",
    ""name"": ""Tom"",
    ""friends"": [
      ""Dick"",
      ""Harry""
    ]
  }
]";
            using (var r = new ChoParquetReader(filePath)
                .WithField("id")
                .WithField("name")
                .WithField("friends", fieldType: typeof(byte[]), valueConverter: o => ((byte[])o).Deserialize<object[]>())
                .TypeConverterFormatSpec(ts => ts.EnumFormat = ChoEnumFormatSpec.Description)
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Facility
        {
            [ChoJSONRecordField(FieldName = "id")]
            public int? Id { get; set; }
            [ChoJSONRecordField]
            public string Name { get; set; }
            [ChoIgnoreMember] //Ignore Uuid
            public string Uuid { get; set; }
            [ChoJSONRecordField]
            public string CreatedAt { get; set; }
            [ChoJSONRecordField]
            public string UpdatedAt { get; set; }
            [ChoJSONRecordField]
            public bool Active { get; set; }

            public Point Location { get; set; }
        }
        [Test]
        public static void JSON2Parquet1()
        {
            string filePath = "JSON2Parquet1.parquet";

            string json = @"{
    ""facilities"": [
        {
            ""id"": 39205,
            ""name"": ""Sample1"",
            ""uuid"": ""ac2f3464-c425-4063-86ad-163521b1d610"",
            ""createdAt"": ""2019-03-06T14:25:32Z"",
            ""updatedAt"": ""2019-03-06T14:29:31Z"",
            ""active"": true
        },
        {
            ""id"": 35907,
            ""name"": ""Sample2"",
            ""uuid"": ""d371debb-f030-4c1e-b198-5eb562ceac0f"",
            ""createdAt"": ""2019-02-21T09:33:25Z"",
            ""updatedAt"": ""2019-02-21T09:33:25Z"",
            ""active"": true
        }
    ]
}
";
            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..facilities[*]", true)
                .WithField("id")
                .WithField("createdAt", fieldType: typeof(DateTimeOffset), valueConverter: o => new DateTimeOffset((DateTime)o))
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                using (var w = new ChoParquetWriter(filePath)
                    .Configure(c => c.TreatDateTimeOffsetAsString = true)
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                    )
                {
                    w.Write(r);
                }
            }

            string expected = @"[
  {
    ""id"": 39205,
    ""createdAt"": ""2019-03-06T14:25:32+00:00""
  },
  {
    ""id"": 35907,
    ""createdAt"": ""2019-02-21T09:33:25+00:00""
  }
]";
            using (var r = new ChoParquetReader(filePath)
                .WithField("id")
                .WithField("createdAt", fieldType: typeof(DateTimeOffset))
                .TypeConverterFormatSpec(ts => ts.EnumFormat = ChoEnumFormatSpec.Description)
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void SerializeValues()
        {
            string filePath = "SerializeValue.parquet";

            //byte[] x = ChoParquetWriter.Serialize(4);
            //File.WriteAllBytes("SerializeValue.parquet", ChoParquetWriter.Serialize(4));
            //return;
            using (var w = new ChoParquetWriter(filePath))
            {
                w.Write(4);
                w.Write(10);
            }

            string expected = @"[
  {
    ""Value"": 4
  },
  {
    ""Value"": 10
  }
]";

            var actual = ReadParquetFileAsJSON(filePath);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void SerializeArray()
        {
            string filePath = "SerializeArray.parquet";
            using (var w = new ChoParquetWriter<int>(filePath))
            {
                w.Write(new int[] { 1, 2 });
                w.Write(new int[] { 3, 4 });
            }
            string expected = @"[
  {
    ""Value"": 1
  },
  {
    ""Value"": 2
  },
  {
    ""Value"": 3
  },
  {
    ""Value"": 4
  }
]";

            var actual = ReadParquetFileAsJSON(filePath);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void SerializeDictionary()
        {
            string filePath = "SerializeDictionary.parquet";
            using (var w = new ChoParquetWriter(filePath))
            {
                w.Write(new Dictionary<int, string>()
                {
                    [1] = "Tom",
                    [2] = "Mark"
                });

            }
            string expected = @"[
  {
    ""Key"": 1,
    ""Value"": ""Tom""
  },
  {
    ""Key"": 2,
    ""Value"": ""Mark""
  }
]";

            var actual = ReadParquetFileAsJSON(filePath);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void ByteArrayTest()
        {
            string filePath = "ByteArrayTest.parquet";
            using (var w = new ChoParquetWriter("ByteArrayTest.parquet")
                .UseNestedKeyFormat(false)
                )
            {
                w.Write(new Dictionary<int, byte[]>()
                {
                    [1] = Encoding.Default.GetBytes("Tom"),
                    [2] = Encoding.Default.GetBytes("Mark")
                });

            }
            string expected = @"[
  {
    ""Key"": 1,
    ""Value"": ""VG9t""
  },
  {
    ""Key"": 2,
    ""Value"": ""TWFyaw==""
  }
]";

            var actual = ReadParquetFileAsJSON(filePath);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void SerializeDateTime()
        {
            string filePath = "DateTimeTest.parquet";
            IList<DateTime> dateList = new List<DateTime>
{
    new DateTime(2009, 12, 7), //, 23, 10, 0, DateTimeKind.Utc),
    new DateTime(2010, 1, 1, 9, 0, 0, DateTimeKind.Utc),
    new DateTime(2010, 2, 10, 10, 0, 0, DateTimeKind.Utc)
};

            using (var w = new ChoParquetWriter<DateTime>(filePath)
               .TypeConverterFormatSpec(ts => ts.DateTimeFormat = "o")
                )
                w.Write(dateList);

            string expected = @"[
  {
    ""Value"": ""2009-12-07T00:00:00Z""
  },
  {
    ""Value"": ""2010-01-01T09:00:00Z""
  },
  {
    ""Value"": ""2010-02-10T10:00:00Z""
  }
]";

            using (var r = new ChoParquetReader(filePath)
                )
            {
                var recs = r.ToArray();
                var actual = ReadParquetFileAsJSON(filePath);
                Assert.AreEqual(expected, actual);
            }
        }

        static void EmptyFileTest()
        {
            using (var w = new ChoParquetWriter("EmptyFile.parquet"))
            {
                //w.Write((dynamic)null);
            }
        }

        public class Employee
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public static void ReadNWrite()
        {
            string filePath = "Emp.parquet";
            List<Employee> objs = new List<Employee>();
            objs.Add(new Employee() { Id = 1, Name = "Tom" });
            objs.Add(new Employee() { Id = 2, Name = "Mark" });

            using (var parser = new ChoParquetWriter<Employee>(filePath))
            {
                parser.Write(objs);
            }

            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Tom""
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark""
  }
]";
            using (var r = new ChoParquetReader<Employee>(filePath))
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        public class MyData
        {
            public Health Health { get; set; }
            public Safety Safety { get; set; }
            public List<Climate> Climate { get; set; }
        }
        public class Health
        {
            public int Id { get; set; }
            public bool Status { get; set; }
        }
        public class Safety
        {
            public int Id { get; set; }
            public bool Status { get; set; }
        }
        public class Climate
        {
            public int Id { get; set; }
            public bool Status { get; set; }
        }
        [Test]
        public static void Json2Parquet()
        {
            string filePath = "Json2Parquet.parquet";
            string json = @"
[{
	""Health"": {
		""Id"": 99,
		""Status"": false
	},
	""Safety"": {
		""Id"": 3,
		""Fire"": 1
	},
	""Climate"": [{
		""Id"": 0,
		""State"": 2
	}]
}]";

            using (var r = ChoJSONReader<MyData>.LoadText(json)
                .UseJsonSerialization())
            {
                using (var w = new ChoParquetWriter(filePath)
                    .UseNestedKeyFormat()
                    .Configure(c => c.ArrayValueNamePrefix = String.Empty)
                    )
                {
                    var recs = r.ToArray();
                    //var dict = recs.Select(r1 => r1.FlattenToDictionary()).ToArray(); //.Select(rec1 => rec1.ToDictionary().Flatten().ToDictionary());
                    w.Write(recs);
                }

                var jsonOut = ReadParquetFileAsJSON(filePath);
                jsonOut.Print();
                var csv = ReadParquetFileAsCSV(filePath);
                csv.Print();

                //var x = ChoParquetWriter.SerializeAll(r.Select(rec1 => rec1.ToDictionary().Flatten().ToDictionary()));
                //File.WriteAllBytes(filePath, x);
            }

            string expected = @"[
  {
    ""Health"": {
      ""Id"": 99,
      ""Status"": false
    },
    ""Safety"": {
      ""Id"": 3,
      ""Status"": false
    },
    ""Climate"": [
      {
        ""Id"": 0,
        ""Status"": false
      }
    ]
  }
]";
            using (var r = new ChoParquetReader<MyData>(filePath)
                    .UseNestedKeyFormat()
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Json2ParquetWithArrayOfInts()
        {
            string filePath = "Json2Parquet.parquet";
            string json = @"
[{
	""Climate"": [{
		""Id"": 0,
		""State"": 2
	}],
	""Health"": {
		""Id"": 99,
		""Status"": false
	},
	""Safety"": {
		""Id"": 3,
		""Fire"": 1
	},
    ""Values"":[
        1,
        2
    ]
}]";

            using (var r = ChoJSONReader.LoadText(json)
                .UseJsonSerialization())
            {
                using (var w = new ChoParquetWriter(filePath)
                    )
                {
                    var recs = r.ToArray();
                    //var dict = recs.Select(r1 => r1.FlattenToDictionary()).ToArray(); //.Select(rec1 => rec1.ToDictionary().Flatten().ToDictionary());
                    w.Write(recs);
                }

                //var x = ChoParquetWriter.SerializeAll(r.Select(rec1 => rec1.ToDictionary().Flatten().ToDictionary()));
                //File.WriteAllBytes(filePath, x);
            }

            string expected = @"[
  {
    ""Climate"": [
      {
        ""Id"": 0,
        ""State"": 2
      }
    ],
    ""Health"": {
      ""Id"": 99,
      ""Status"": false
    },
    ""Safety"": {
      ""Id"": 3,
      ""Fire"": 1
    },
    ""Values"": [
      1,
      2
    ]
  }
]";
            using (var r = new ChoParquetReader(filePath)
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void JsonToParquet52()
        {
            string filePath = "JsonToParquet52.parquet";

            using (var r = new ChoJSONReader("sample52.json")
                .WithJSONPath("$..data")
                .UseJsonSerialization()
                .JsonSerializationSettings(s => s.DateParseHandling = DateParseHandling.None)
                .JsonSerializationSettings(s => s.NullValueHandling = NullValueHandling.Include)
                )
            {
                using (var w = new ChoParquetWriter(filePath)
                    .ThrowAndStopOnMissingField(false)
                    )
                    w.Write(r);
            }

            string expected = null;
            using (var r = new ChoParquetReader(filePath)
                .Setup(s => s.MembersDiscovered += (o, e) =>
                {
                    var args = e as ChoEventArgs<IDictionary<string, Type>>;
                    if (args.Value.ContainsKey("Number_of_Users"))
                        args.Value["Number_of_Users"] = typeof(long);

                    if (args.Value.ContainsKey("MRR"))
                        args.Value["MRR"] = typeof(long?);
                })
                .TypeConverterFormatSpec(ts => ts.DateTimeFormat = "o")
                )
            {
                var recs = r.ToArray().Select(r =>
                {
                    if (r.LinkedIn_Status == null)
                        r.LinkedIn_Status = new object[] { };
                    if (r.Tag == null)
                        r.Tag = new object[] { };

                    foreach (var key in r.KeysArray)
                    {
                        if (key == "PU_RO" || key == "Lead_PU_Score" || key == "Lead_ICP_Score")
                            continue;
                        if (r[key] is long && (long)r[key] == 0)
                            r[key] = null;
                        if (r[key] is int && (int)r[key] == 0)
                            r[key] = null;
                    }
                    return r;
                });
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                File.WriteAllText("sample52out.json", actual);
                Assert.Ignore();
            }
        }
        [Test]
        public static void DataTableTest()
        {
            string filePath = "datatable.parquet";
            string csv = @"Id, Name
1, Tom
2, Mark";

            string expected = @"[
  {
    ""Id"": ""1"",
    ""Name"": ""Tom""
  },
  {
    ""Id"": ""2"",
    ""Name"": ""Mark""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                var dt = r.AsDataTable("Emp");

                using (var w = new ChoParquetWriter(filePath)
                    .Configure(c => c.CompressionMethod = Parquet.CompressionMethod.Gzip)
                    )
                {
                    w.Write(dt);
                    w.Close();

                    var s = w.Configuration.Schema;
                    s.Print();
                }
            }
            var actual = ReadParquetFileAsJSON(filePath);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Issue167()
        {
            var completeFile = new CompleteFile
            {
                DataSource = "DataSource",
                DataType = 1,
                Samples = new Samples
                {
                    Samples1 = new List<Data>
                    {
                        new Data
                        {
                            Prop00 = 0,
                            Prop01 = "Prop01",
                            Properties = new Properties
                            {
                                 Prop10 = 1,
                                 Prop11 = 2,
                            }
                        },
                        new Data
                        {
                            Prop00 = 0,
                            Prop01 = "Prop01",
                            Properties = new Properties
                            {
                                 Prop10 = 1,
                                 Prop11 = 2,
                            }
                        },
                        new Data
                        {
                            Prop00 = 0,
                            Prop01 = "Prop01",
                            Properties = new Properties
                            {
                                 Prop10 = 1,
                                 Prop11 = 2,
                            }
                        },
                    },
                    Samples2 = new List<Data>
                    {
                        new Data
                        {
                            Prop00 = 0,
                            Prop01 = "Prop01",
                            Properties = new Properties
                            {
                                 Prop10 = 1,
                                 Prop11 = 2,
                            }
                        },
                        new Data
                        {
                            Prop00 = 0,
                            Prop01 = "Prop01",
                            Properties = new Properties
                            {
                                 Prop10 = 1,
                                 Prop11 = 2,
                            }
                        },
                        new Data
                        {
                            Prop00 = 0,
                            Prop01 = "Prop01",
                            Properties = new Properties
                            {
                                 Prop10 = 1,
                                 Prop11 = 2,
                            }
                        },
                    },
                    Samples3 = new List<Data>
                    {
                        new Data
                        {
                            Prop00 = 0,
                            Prop01 = "Prop01",
                            Properties = new Properties
                            {
                                 Prop10 = 1,
                                 Prop11 = 2,
                            }
                        },
                        new Data
                        {
                            Prop00 = 0,
                            Prop01 = "Prop01",
                            Properties = new Properties
                            {
                                 Prop10 = 1,
                                 Prop11 = 2,
                            }
                        },
                        new Data
                        {
                            Prop00 = 0,
                            Prop01 = "Prop01",
                            Properties = new Properties
                            {
                                 Prop10 = 1,
                                 Prop11 = 2,
                            }
                        },
                    },
                    Sample4 = new Data
                    {
                        Prop00 = 0,
                        Prop01 = "Prop01",
                        Properties = new Properties
                        {
                            Prop10 = 1,
                            Prop11 = 2,
                        }
                    },

                }
            };

            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<CompleteFile>(json)
                )
            {
                w.Write(completeFile);
            }

            json.Print();

            string filePath = "CompleteFile.parquet";
            string expected = @"[
  {
    ""DataSource"": ""DataSource"",
    ""DataType"": 1,
    ""Samples_Samples1_Value0_Prop00"": 0,
    ""Samples_Samples1_Value0_Prop01"": ""Prop01"",
    ""Samples_Samples1_Value0_Properties_prop10_x"": 1,
    ""Samples_Samples1_Value0_Properties_Prop11"": 2,
    ""Samples_Samples1_Value1_Prop00"": 0,
    ""Samples_Samples1_Value1_Prop01"": ""Prop01"",
    ""Samples_Samples1_Value1_Properties_prop10_x"": 1,
    ""Samples_Samples1_Value1_Properties_Prop11"": 2,
    ""Samples_Samples1_Value2_Prop00"": 0,
    ""Samples_Samples1_Value2_Prop01"": ""Prop01"",
    ""Samples_Samples1_Value2_Properties_prop10_x"": 1,
    ""Samples_Samples1_Value2_Properties_Prop11"": 2,
    ""Samples_Samples2_Value0_Prop00"": 0,
    ""Samples_Samples2_Value0_Prop01"": ""Prop01"",
    ""Samples_Samples2_Value0_Properties_prop10_x"": 1,
    ""Samples_Samples2_Value0_Properties_Prop11"": 2,
    ""Samples_Samples2_Value1_Prop00"": 0,
    ""Samples_Samples2_Value1_Prop01"": ""Prop01"",
    ""Samples_Samples2_Value1_Properties_prop10_x"": 1,
    ""Samples_Samples2_Value1_Properties_Prop11"": 2,
    ""Samples_Samples2_Value2_Prop00"": 0,
    ""Samples_Samples2_Value2_Prop01"": ""Prop01"",
    ""Samples_Samples2_Value2_Properties_prop10_x"": 1,
    ""Samples_Samples2_Value2_Properties_Prop11"": 2,
    ""Samples_Samples3_Value0_Prop00"": 0,
    ""Samples_Samples3_Value0_Prop01"": ""Prop01"",
    ""Samples_Samples3_Value0_Properties_prop10_x"": 1,
    ""Samples_Samples3_Value0_Properties_Prop11"": 2,
    ""Samples_Samples3_Value1_Prop00"": 0,
    ""Samples_Samples3_Value1_Prop01"": ""Prop01"",
    ""Samples_Samples3_Value1_Properties_prop10_x"": 1,
    ""Samples_Samples3_Value1_Properties_Prop11"": 2,
    ""Samples_Samples3_Value2_Prop00"": 0,
    ""Samples_Samples3_Value2_Prop01"": ""Prop01"",
    ""Samples_Samples3_Value2_Properties_prop10_x"": 1,
    ""Samples_Samples3_Value2_Properties_Prop11"": 2,
    ""Samples_Sample4_Prop00"": 0,
    ""Samples_Sample4_Prop01"": ""Prop01"",
    ""Samples_Sample4_Properties_prop10_x"": 1,
    ""Samples_Sample4_Properties_Prop11"": 2
  }
]";
            using (var r = ChoJSONReader<CompleteFile>.LoadText(json.ToString())
                .UseJsonSerialization()
                )
            {
                using (var w = new ChoParquetWriter(filePath)
                    )
                {
                    w.Write(r.Select(rec1 => rec1.FlattenToDictionary()));
                }

                //using (var w = new ChoParquetWriter<CompleteFile>("CompleteFile.parquet")
                //    .WithField(f => f.Samples, valueConverter: o => "x", fieldType: typeof(string))
                //    )
                //{
                //    w.Write(r); //.Select(rec1 => rec1.Flatten().ToDictionary()));
                //}
            }

            var actual = ReadParquetFileAsJSON(filePath);
            Assert.AreEqual(expected, actual);

        }

        public class CompleteFile
        {
            public string DataSource { get; set; }
            public long? DataType { get; set; }
            //public GeneralData GeneralData { get; set; }
            public Samples Samples { get; set; }
        }

        public class Samples
        {
            public IList<Data> Samples1 { get; set; }
            public IList<Data> Samples2 { get; set; }
            public IList<Data> Samples3 { get; set; }
            public Data Sample4 { get; set; }
        }

        public class Data
        {
            public long? Prop00 { get; set; }
            public string Prop01 { get; set; }
            public Properties Properties { get; set; }
            //public MyImage Image { get; set; }
        }

        public class Properties
        {
            [JsonProperty("prop10_x")]
            [DisplayName("prop10_x")]
            public long? Prop10 { get; set; }
            public long? Prop11 { get; set; }
        }

        [Test]
        public static void Issue202()
        {
            string filePath = "Issue202.parquet";

            using (var r = new ChoJSONReader("Issue202.json")
                //.UseJsonSerialization()
                )
            {
                using (var w = new ChoParquetWriter(filePath)
                    .WithMaxScanRows(3)
                    .ThrowAndStopOnMissingField(false)
                    )
                {
                    var recs = r.ToArray();
                    w.Write(recs);
                    //w.Write(r.Select(rec1 => rec1.FlattenToDictionary()));
                }

                //using (var w = new ChoParquetWriter<CompleteFile>("CompleteFile.parquet")
                //    .WithField(f => f.Samples, valueConverter: o => "x", fieldType: typeof(string))
                //    )
                //{
                //    w.Write(r); //.Select(rec1 => rec1.Flatten().ToDictionary()));
                //}
            }

            var actual = ReadParquetFileAsJSON(filePath);
            File.WriteAllText("Issue202Out.json", actual);
            var expected = File.ReadAllText("Issue202Expected.json");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Issue230()
        {
            string filePath = "Sample230.parquet";
            using (var r = new ChoJSONReader("Sample230.json")
                .UseJsonSerialization()
                .JsonSerializationSettings(s => s.DateParseHandling = DateParseHandling.None)
                )
            {
                using (var w = new ChoParquetWriter(filePath)
                    .ThrowAndStopOnMissingField(false)
                    )
                {
                    w.Write(r);
                }
            }

            var actual = ReadParquetFileAsJSON(filePath);
            File.WriteAllText("Sample230Out.json", actual);
            var expected = File.ReadAllText("Sample230Expected.json");
            Assert.Ignore(); //.AreEqual(expected, actual);
        }

        static void PrintParquetFile(string parquetOutputFilePath)
        {
            parquetOutputFilePath.Print();
            using (var w = new ChoParquetReader(parquetOutputFilePath))
            {
                w.Print();
            }
        }
        static string ReadParquetFileAsJSON(string parquetOutputFilePath, int? recCount = null, JsonSerializerSettings jsonSerializerSettings = null)
        {
            jsonSerializerSettings = jsonSerializerSettings ?? new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
            };
            parquetOutputFilePath.Print();
            using (var r = new ChoParquetReader(parquetOutputFilePath))
            {
                var recs = recCount == null ? r.ToArray() : r.Take(recCount.Value).ToArray();
                return JsonConvert.SerializeObject(recs, jsonSerializerSettings);
            }
        }
        static string ReadParquetFileAsCSV(string parquetOutputFilePath, int? recCount = null)
        {
            parquetOutputFilePath.Print();
            using (var r = new ChoParquetReader(parquetOutputFilePath))
            {
                var recs = recCount == null ? r.ToArray() : r.Take(recCount.Value).ToArray();
                return ChoCSVWriter.ToTextAll(recs, new ChoCSVRecordConfiguration().WithFirstLineHeader());
            }
        }
        static string ReadParquetFileAsCSV<T>(string parquetOutputFilePath, int? recCount = null)
            where T : class
        {
            parquetOutputFilePath.Print();
            using (var r = new ChoParquetReader(parquetOutputFilePath))
            {
                var recs = recCount == null ? r.ToArray() : r.Take(recCount.Value).ToArray();
                return ChoCSVWriter<T>.ToTextAll(recs, new ChoCSVRecordConfiguration().WithFirstLineHeader());
            }
        }

        static void Issue244()
        {
            string csv = @"Id, Name, DateJoined
1, Tom, 1/2/2022
2,,";

            ChoETLFrxBootstrap.CustomObjectToString = (t) =>
            {
                if (t == DBNull.Value)
                    return "DBNull";
                else
                    return null;
            };

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                .QuoteAllFields()
                )
            {
                var dt = r.AsDataTable();
                dt.Rows[1]["Name"] = DBNull.Value;
                dt.Rows[1]["DateJoined"] = DBNull.Value;
                dt.Rows[1]["DateJoined"].Print();
                dt.Print();
                using (var w = new ChoParquetWriter("quicktest.parquet"))
                {
                    w.Write(dt);
                }
            }
        }
        [Test]
        public static void POCODateTimeWithMemberConverterTest()
        {
            string filePath = @$"POCODateTimeWithMemberConverterTest.parquet";

            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            typeof(ChoParquetReader).GetAssemblyVersion().Print();

            string expected = @"[
  {
    ""Id"": null,
    ""Price"": 1.3,
    ""Quantity"": 2.45,
    ""Name"": null,
    ""CreateDate"": null
  }
]";
            using (var w = new ChoParquetWriter<TradeWithDateConverter>(filePath)
                  )
            {
                w.Write(new TradeWithDateConverter
                {
                    Id = null,
                    Price = 1.3,
                    Quantity = 2.45,
                });
            }

            PrintParquetFile(filePath);

            TradeWithDateConverter[] recs = null;
            using (var r = new ChoParquetReader<TradeWithDateConverter>(filePath)
               )
            {
                recs = r.ToArray();
                recs.Print();
            }

            var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
            Assert.AreEqual(expected, actual);

            //var actual = ReadParquetFileAsJSON(filePath);
            //Assert.AreEqual(expected, actual);
        }

        public class TradeWith_NO_DateConverter
        {
            public long? Id { get; set; }
            public double? Price { get; set; }
            public double? Quantity { get; set; }
            public string Name { get; set; }
            public DateTime? CreateDate { get; set; }
        }
        [Test]
        public static void POCODateTimeAsStringTest()
        {
            string filePath = @$"POCODateTimeAsStringTest.parquet";
            var tradeList = new List<TradeWith_NO_DateConverter>();

            var trade1 = new TradeWith_NO_DateConverter()
            {
                Quantity = 2
            };

            var trade2 = new TradeWith_NO_DateConverter()
            {
                Id = 100
            };

            var trade3 = new TradeWith_NO_DateConverter { Id = null, Price = 2.3, Quantity = 2.45, Name = "Name", CreateDate = _currentDateTime };

            tradeList.Add(trade1);
            tradeList.Add(trade2);
            tradeList.Add(trade3);

            string expected = @"[
  {
    ""Id"": null,
    ""Price"": null,
    ""Quantity"": 2.0,
    ""Name"": null,
    ""CreateDate"": null
  },
  {
    ""Id"": 100,
    ""Price"": null,
    ""Quantity"": null,
    ""Name"": null,
    ""CreateDate"": null
  },
  {
    ""Id"": null,
    ""Price"": 2.3,
    ""Quantity"": 2.45,
    ""Name"": ""Name"",
    ""CreateDate"": ""2023-06-10T12:10:30Z""
  }
]";
            //ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MM/dd/yy HH:mm:ss";
            using (var w = new ChoParquetWriter<TradeWith_NO_DateConverter>(filePath)
               .Configure(c => c.TypeConverterFormatSpec = new ChoTypeConverterFormatSpec { DateTimeFormat = "o" })
                  )
            {
                //foreach (var rec in tradeList)
                //    w.Write(rec);
                w.Write(tradeList);
            }
            PrintParquetFile(filePath);

            TradeWithDateConverter[] recs = null;
            using (var r = new ChoParquetReader<TradeWithDateConverter>(filePath)
               )
            {
                recs = r.ToArray();
                recs.Print();
            }

            var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void DynamicDateTimeAsStringTest()
        {
            string filePath = @$"DynamicDateTimeAsStringTest.parquet";
            dynamic x = new ChoDynamicObject();
            x.Id = null;
            x.Name = "Mark";
            x.CreatedDate = _currentDateTime;

            string expected = @"[
  {
    ""Id"": null,
    ""Name"": ""Mark"",
    ""CreatedDate"": ""06/10/2023 12""
  }
]";
            //ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MM/dd/yy HH:mm:ss";
            using (var w = new ChoParquetWriter(filePath)
               .Configure(c => c.TypeConverterFormatSpec = new ChoTypeConverterFormatSpec { DateTimeFormat = "MM/dd/yyyy HH" })
               .TreatDateTimeAsString()
                  )
            {
                //foreach (var rec in tradeList)
                //    w.Write(rec);
                w.Write(x);
            }
            PrintParquetFile(filePath);

            var actual = ReadParquetFileAsJSON(filePath);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void POCOTreatDateTimeAsDateTimeOffsetTest()
        {
            string filePath = @$"POCOTreatDateTimeAsDateTimeOffsetTest.parquet";

            var tradeList = new List<TradeWithDateConverter>();

            var trade1 = new TradeWithDateConverter()
            {
                Quantity = 2
            };

            var trade2 = new TradeWithDateConverter()
            {
                Id = 100
            };

            var trade3 = new TradeWithDateConverter { Id = null, Price = 2.3, Quantity = 2.45, Name = "Name", CreateDate = _currentDateTime };
            trade3.CreateDate = DateTime.SpecifyKind(trade3.CreateDate.Value, DateTimeKind.Utc);

            tradeList.Add(trade1);
            tradeList.Add(trade2);
            tradeList.Add(trade3);

            string expected = @"[
  {
    ""Id"": null,
    ""Price"": null,
    ""Quantity"": 2.0,
    ""Name"": null,
    ""CreateDate"": null
  },
  {
    ""Id"": 100,
    ""Price"": null,
    ""Quantity"": null,
    ""Name"": null,
    ""CreateDate"": null
  },
  {
    ""Id"": null,
    ""Price"": 2.3,
    ""Quantity"": 2.45,
    ""Name"": ""Name"",
    ""CreateDate"": ""2023-06-10T12:10:30Z""
  }
]";
            using (var w = new ChoParquetWriter<TradeWithDateConverter>(filePath)
                //.TreatDateTimeAsDateTimeOffset()
                  )
            {
                w.Write(tradeList);
            }
            //PrintParquetFile(filePath);

            using (var r = new ChoParquetReader<TradeWithDateConverter>(filePath))
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void POCOTreatDateTimeAsDateTimeOffsetTestWith_NO_Converter()
        {
            string filePath = @$"POCOTreatDateTimeAsDateTimeOffsetTestWith_NO_Converter.parquet";

            var tradeList = new List<TradeWith_NO_DateConverter>();

            var trade1 = new TradeWith_NO_DateConverter()
            {
                Quantity = 2
            };

            var trade2 = new TradeWith_NO_DateConverter()
            {
                Id = 100
            };

            var trade3 = new TradeWith_NO_DateConverter { Id = null, Price = 2.3, Quantity = 2.45, Name = "Name", CreateDate = _currentDateTime };
            trade3.CreateDate = DateTime.SpecifyKind(trade3.CreateDate.Value, DateTimeKind.Utc);

            tradeList.Add(trade1);
            tradeList.Add(trade2);
            tradeList.Add(trade3);

            string expected = @"[
  {
    ""Id"": null,
    ""Price"": null,
    ""Quantity"": ""2"",
    ""Name"": null,
    ""CreateDate"": ""0001-01-01T12:00:00+00:00""
  },
  {
    ""Id"": ""100"",
    ""Price"": null,
    ""Quantity"": null,
    ""Name"": null,
    ""CreateDate"": ""0001-01-01T12:00:00+00:00""
  },
  {
    ""Id"": null,
    ""Price"": ""2.3"",
    ""Quantity"": ""2.45"",
    ""Name"": ""Name"",
    ""CreateDate"": ""2023-06-10T12:10:30+00:00""
  }
]";
            //ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MM/dd/yy HH:mm:ss";
            //ChoTypeDescriptor.RegisterTypeConverterForType(typeof(DateTimeOffset), new ChoDateTimeOffsetConverter());
            using (var w = new ChoParquetWriter<TradeWith_NO_DateConverter>(filePath)
                //.TreatDateTimeAsDateTimeOffset()
                  )
            {
                //foreach (var rec in tradeList)
                //    w.Write(rec);
                w.Write(tradeList);
            }
            PrintParquetFile(filePath);

            using (var r = new ChoParquetReader<TradeWithDateConverter>(filePath)
               )
            {
                r.Print();
            }

            var actual = ReadParquetFileAsJSON(filePath, jsonSerializerSettings: new JsonSerializerSettings()
            {
                DateFormatString = "yyyy-MM-ddThh:mm:sszzz",
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Formatting = Formatting.Indented
            });
            Assert.AreEqual(expected, actual);
        }

        //static void PrintParquetFile(string filePath)
        //{
        //    using (var r = new ChoParquetReader(filePath))
        //    {
        //        r.AsDataTable().Print();
        //    }
        //}

        public class TradeWithDateConverter
        {
            public long? Id { get; set; }
            public double? Price { get; set; }
            public double? Quantity { get; set; }
            public string Name { get; set; }
            [ChoTypeConverter(typeof(ChoDateTimeOffsetConverter))]
            public DateTime? CreateDate { get; set; }
        }

        //[ChoNativeType(typeof(DateTimeOffset))]
        public class ChoDateTimeOffsetConverter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is DateTimeOffset)
                {
                    if ((DateTimeOffset)value == DateTimeOffset.MinValue)
                        return null;
                    else
                        return ((DateTimeOffset)value).LocalDateTime;
                }
                else if (value is DateTime)
                {
                    if ((DateTime)value == DateTime.MinValue)
                        return null;
                    else
                        return ((DateTime)value);
                }
                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value;
            }
        }

        [Test]
        public static void Issue285()
        {
            string filePath = @$"Issue285.parquet";
            string json = @"{
  ""Stype"": ""BaseDecorator"",
  ""Decorators"": [
    {
      ""Stype"": ""FiscalInformationDecorator"",
      ""FiscalInformation"": {
        ""Stype"": ""FiscalInformation"",
        ""UUID"": ""02d0c973-727e-449e-bb4e-45dddbd7dbeb""
      }
    },
    {
      ""Stype"": ""DocumentInformationDecorator"",
      ""DocumentInformation"": {
        ""Stype"": ""DocumentInformation"",
        ""DocumentModelID"": ""7ec7b1d4-f94f-42b5-ba36-77701cdf1db4""
      }
    },
    {
      ""Stype"": ""IssuingInformationDecorator"",
      ""IssuingInformation"": {
        ""Stype"": ""IssuingInformation"",
        ""RFC"": ""PRR890126QC2""
      }
    }
  ],
  ""InstanceID"": ""78091f6e-e458-4a23-abfe-fe286b24b59a"",
  ""company"": ""d6038f2d-787c-427b-8eaf-4d9eea44a24a""
}";

            string expected = @"[
  {
    ""Decorators"": ""[\r\n  {\r\n    \""Stype\"": \""FiscalInformationDecorator\"",\r\n    \""FiscalInformation\"": {\r\n      \""Stype\"": \""FiscalInformation\"",\r\n      \""UUID\"": \""02d0c973-727e-449e-bb4e-45dddbd7dbeb\""\r\n    }\r\n  },\r\n  {\r\n    \""Stype\"": \""DocumentInformationDecorator\"",\r\n    \""DocumentInformation\"": {\r\n      \""Stype\"": \""DocumentInformation\"",\r\n      \""DocumentModelID\"": \""7ec7b1d4-f94f-42b5-ba36-77701cdf1db4\""\r\n    }\r\n  },\r\n  {\r\n    \""Stype\"": \""IssuingInformationDecorator\"",\r\n    \""IssuingInformation\"": {\r\n      \""Stype\"": \""IssuingInformation\"",\r\n      \""RFC\"": \""PRR890126QC2\""\r\n    }\r\n  }\r\n]"",
    ""InstanceID"": ""78091f6e-e458-4a23-abfe-fe286b24b59a"",
    ""company"": ""d6038f2d-787c-427b-8eaf-4d9eea44a24a""
  }
]";
            //var stringJson = JArray.FromObject(deserialized_jsons).ToString();
            using (var r = ChoJSONReader.LoadText(json).ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .WithField("Decorators", customSerializer: o => o.ToString())
                .WithField("InstanceID")
                .WithField("company")
                )
            {
                using (var w = new ChoParquetWriter(filePath,
                    new ChoParquetRecordConfiguration { CompressionMethod = Parquet.CompressionMethod.Snappy })
                .ThrowAndStopOnMissingField(false)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue))
                {
                    w.Write(r);
                }
            }

            var actual = ReadParquetFileAsJSON(filePath);
            Assert.AreEqual(expected, actual);
        }

        public class Decorator
        {
            public string Stype { get; set; }
            public FiscalInformation FiscalInformation { get; set; }
            public DocumentInformation DocumentInformation { get; set; }
            public IssuingInformation IssuingInformation { get; set; }
        }

        public class FiscalInformation
        {
            public string Stype { get; set; }
            public string UUID { get; set; }
        }

        public class DocumentInformation
        {
            public string Stype { get; set; }
            public string DocumentModelID { get; set; }
        }

        public class IssuingInformation
        {
            public string Stype { get; set; }
            public string RFC { get; set; }
        }

        [Test]
        public static void Issue285_1()
        {
            string filePath = @$"Issue285_1.parquet";
            string json = @"{
  ""Stype"": ""BaseDecorator"",
  ""Decorators"": [
    {
      ""Stype"": ""FiscalInformationDecorator"",
      ""FiscalInformation"": {
        ""Stype"": ""FiscalInformation"",
        ""UUID"": ""02d0c973-727e-449e-bb4e-45dddbd7dbeb""
      }
    },
    {
      ""Stype"": ""DocumentInformationDecorator"",
      ""DocumentInformation"": {
        ""Stype"": ""DocumentInformation"",
        ""DocumentModelID"": ""7ec7b1d4-f94f-42b5-ba36-77701cdf1db4""
      }
    },
    {
      ""Stype"": ""IssuingInformationDecorator"",
      ""IssuingInformation"": {
        ""Stype"": ""IssuingInformation"",
        ""RFC"": ""PRR890126QC2""
      }
    }
  ],
  ""InstanceID"": ""78091f6e-e458-4a23-abfe-fe286b24b59a"",
  ""company"": ""d6038f2d-787c-427b-8eaf-4d9eea44a24a""
}";

            string expected = @"[
  {
    ""Stype"": ""BaseDecorator"",
    ""Decorators"": [
      {
        ""Stype"": ""FiscalInformationDecorator"",
        ""FiscalInformation"": {
          ""Stype"": ""FiscalInformation"",
          ""UUID"": ""02d0c973-727e-449e-bb4e-45dddbd7dbeb""
        }
      },
      {
        ""Stype"": ""DocumentInformationDecorator"",
        ""DocumentInformation"": {
          ""Stype"": ""DocumentInformation"",
          ""DocumentModelID"": ""7ec7b1d4-f94f-42b5-ba36-77701cdf1db4""
        }
      },
      {
        ""Stype"": ""IssuingInformationDecorator"",
        ""IssuingInformation"": {
          ""Stype"": ""IssuingInformation"",
          ""RFC"": ""PRR890126QC2""
        }
      }
    ],
    ""InstanceID"": ""78091f6e-e458-4a23-abfe-fe286b24b59a"",
    ""company"": ""d6038f2d-787c-427b-8eaf-4d9eea44a24a""
  }
]";
            //var stringJson = JArray.FromObject(deserialized_jsons).ToString();
            using (var r = ChoJSONReader.LoadText(json).ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .WithField("Stype")
                .WithField("Decorators", fieldType: typeof(List<Decorator>))
                .WithField("InstanceID")
                .WithField("company")
                )
            {
                var recs = r.ToArray();

                using (var w = new ChoParquetWriter(filePath,
                    new ChoParquetRecordConfiguration { CompressionMethod = Parquet.CompressionMethod.Snappy })
                    .WithField("Stype")
                    .WithField("Decorators", customSerializer: o => JsonConvert.SerializeObject(o))
                    .WithField("InstanceID")
                    .WithField("company")
                    .UseNestedKeyFormat(false)
                    .ThrowAndStopOnMissingField(false)
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
                {
                    w.Write(recs);
                }
            }

            using (var r = new ChoParquetReader(filePath)
                    .WithField("Stype")
                    .WithField("Decorators", customSerializer: o => JsonConvert.DeserializeObject<List<Decorator>>(o.ToString()))
                    .WithField("InstanceID")
                    .WithField("company")
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                Assert.AreEqual(expected, actual);
            }

        }

        [Test]
        public static void Issue285_2()
        {
            string filePath = @$"Issue285_1.parquet";
            string json = @"{
  ""Stype"": ""BaseDecorator"",
  ""Decorators"": [
    {
      ""Stype"": ""FiscalInformationDecorator"",
      ""FiscalInformation"": {
        ""Stype"": ""FiscalInformation"",
        ""UUID"": ""02d0c973-727e-449e-bb4e-45dddbd7dbeb""
      }
    },
    {
      ""Stype"": ""DocumentInformationDecorator"",
      ""DocumentInformation"": {
        ""Stype"": ""DocumentInformation"",
        ""DocumentModelID"": ""7ec7b1d4-f94f-42b5-ba36-77701cdf1db4""
      }
    },
    {
      ""Stype"": ""IssuingInformationDecorator"",
      ""IssuingInformation"": {
        ""Stype"": ""IssuingInformation"",
        ""RFC"": ""PRR890126QC2""
      }
    }
  ],
  ""InstanceID"": ""78091f6e-e458-4a23-abfe-fe286b24b59a"",
  ""company"": ""d6038f2d-787c-427b-8eaf-4d9eea44a24a""
}";

            string expected = @"[
  {
    ""Stype"": ""BaseDecorator"",
    ""Decorators"": [
      {
        ""Stype"": ""FiscalInformationDecorator"",
        ""FiscalInformation"": {
          ""Stype"": ""FiscalInformation"",
          ""UUID"": ""02d0c973-727e-449e-bb4e-45dddbd7dbeb""
        }
      },
      {
        ""Stype"": ""DocumentInformationDecorator"",
        ""DocumentInformation"": {
          ""Stype"": ""DocumentInformation"",
          ""DocumentModelID"": ""7ec7b1d4-f94f-42b5-ba36-77701cdf1db4""
        }
      },
      {
        ""Stype"": ""IssuingInformationDecorator"",
        ""IssuingInformation"": {
          ""Stype"": ""IssuingInformation"",
          ""RFC"": ""PRR890126QC2""
        }
      }
    ],
    ""InstanceID"": ""78091f6e-e458-4a23-abfe-fe286b24b59a"",
    ""company"": ""d6038f2d-787c-427b-8eaf-4d9eea44a24a""
  }
]";
            //var stringJson = JArray.FromObject(deserialized_jsons).ToString();
            using (var r = ChoJSONReader.LoadText(json).ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .WithField("Stype")
                .WithField("Decorators", fieldType: typeof(List<Decorator>))
                .WithField("InstanceID")
                .WithField("company")
                )
            {
                var recs = r.ToArray();

                using (var w = new ChoParquetWriter(filePath,
                    new ChoParquetRecordConfiguration { CompressionMethod = Parquet.CompressionMethod.Snappy })
                    .WithField("Stype")
                    .WithField("Decorators", customSerializer: o => JsonConvert.SerializeObject(o))
                    .WithField("InstanceID")
                    .WithField("company")
                    .UseNestedKeyFormat(false)
                    .ThrowAndStopOnMissingField(false)
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
                {
                    w.Write(recs);
                }
            }

            using (var r = new ChoParquetReader(filePath)
                    .WithField("Stype")
                    .WithField("Decorators", fieldType: typeof(List<Decorator>))
                    .WithField("InstanceID")
                    .WithField("company")
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                Assert.AreEqual(expected, actual);
            }

        }
        public class Trade
        {
            public long? Id { get; set; }
            public double? Price { get; set; }
            public double? Quantity { get; set; }
            public DateTime? CreateDateTime { get; set; }
            public bool? IsActive { get; set; }
            public Decimal? Total { get; set; }
        }

        [Test]
        public static void DB2ParquetTest()
        {
            Assert.Ignore();

            string filePath = @$"Trade.parquet";
            var dbFilePath = Path.GetFullPath(@"..\..\..\..\..\..\data\db\localdb.mdf");

            using (var conn = new SqlConnection($"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={dbFilePath};Integrated Security=True;Connect Timeout=30"))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT * FROM Trade", conn);

                var dr = cmd.ExecuteReader();

                using (var w = new ChoParquetWriter<Trade>(filePath)
                    //.Configure(c => c.LiteParsing = true)
                    .Configure(c => c.RowGroupSize = 5000)
                    .NotifyAfter(100000)
                    .OnRowsWritten((o, e) => $"Rows Loaded: {e.RowsWritten} <-- {DateTime.Now}".Print())
                    .ThrowAndStopOnMissingField(false)
                    )
                {
                    w.Write(dr);
                }
            }
            string expected = @"[
  {
    ""Id"": ""1381095255"",
    ""Price"": ""122"",
    ""Quantity"": ""0.1"",
    ""CreateDateTime"": null,
    ""IsActive"": null,
    ""Total"": null
  },
  {
    ""Id"": ""1381179030"",
    ""Price"": ""123.61"",
    ""Quantity"": ""0.1"",
    ""CreateDateTime"": null,
    ""IsActive"": null,
    ""Total"": null
  }
]";
            var actual = ReadParquetFileAsJSON(filePath, 2);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Issue144()
        {
            string filePath = @$"emp.parquet";
            var dbFilePath = Path.GetFullPath(@"..\..\..\..\..\assets\db\Northwind.mdf");
            dbFilePath.Print();

            string connectionString = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={dbFilePath};Integrated Security=True;Connect Timeout=30";
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            SqlCommand command = new SqlCommand("SELECT * FROM Employees", conn);
            using (var r = command.ExecuteReader(CommandBehavior.CloseConnection))
            {
                using (var parser = new ChoParquetWriter(filePath)
                    .Configure(c => c.CompressionMethod = Parquet.CompressionMethod.Gzip)
                    .Configure(c => c.RowGroupSize = 1000)
                   .TreatDateTimeAsString()
                    .NotifyAfter(1000)
                    .OnRowsWritten((o, e) => $"Rows: {e.RowsWritten} <--- {DateTime.Now}".Print())
                    .Setup(s => s.BeforeRecordFieldWrite += (o, e) =>
                    {
                        if (e.Source == DBNull.Value)
                            e.Source = null;
                    })
                    )
                {
                    if (r.HasRows)
                    {
                        parser.Write(r);
                    }
                }
            }

            string expected = @"[
  {
    ""EmployeeID"": 1,
    ""LastName"": ""Davolio"",
    ""FirstName"": ""Nancy"",
    ""Title"": ""Sales Representative"",
    ""TitleOfCourtesy"": ""Ms."",
    ""BirthDate"": ""12/8/1948"",
    ""HireDate"": ""5/1/1992"",
    ""Address"": ""507 - 20th Ave. E.\r\nApt. 2A"",
    ""City"": ""Seattle"",
    ""Region"": ""WA"",
    ""PostalCode"": ""98122"",
    ""Country"": ""USA"",
    ""HomePhone"": ""(206) 555-9857"",
    ""Extension"": ""5467"",
    ""Photo"": ""FRwvAAIAAAANAA4AFAAhAP////9CaXRtYXAgSW1hZ2UAUGFpbnQuUGljdHVyZQABBQAAAgAAAAcAAABQQnJ1c2gAAAAAAAAAAAAgVAAAQk0gVAAAAAAAAHYAAAAoAAAAwAAAAN8AAAABAAQAAAAAAKBTAADODgAA2A4AAAAAAAAAAAAAAAAAAAAAgAAAgAAAAICAAIAAAACAAIAAgIAAAMDAwACAgIAAAAD/AAD/AAAA//8A/wAAAP8A/wD//wAA////AP8MsMkACwkJAAAKAJAJAAAAAAkJoJqQCwkACpCgAAAAD//v///////////LnPz++v/////t//7e/97+/P//2toA2QAAkAkAkAAAAAAJCgAJC8AACQCQAAAACgCsoODg4PDpypAAqcsMAACQkOAAAAkJCwAA0AkAkAAACQAAmgAP///////+//////yt69vf39//////7+3//v////78rwyaCg0AAJoAAAAAAAAACQkKAAsAmpAACQAAkAwJAJAJAPqQraAAkLALAAAAAJAACQAAAJAJqbAJAJoAAAAAwAC//////v///////8+fvL77/+v////+/f/+/f/P/v/5vAsAkJAAkAAJAAAAAAAAkAAAkJAJoMCwAACwAAmgDg4ODvytoMkAAMsAkAAAmpoJoACwCQCQkA0AqQkACQkKAAD////+//7///////Dw/9/trf3//////v797+/8/e/sDwyawAoAoJAAAACQAAAACQqQCgAAkJAACgAADKAMCQCQkPkA2gCakADaANAAAAnAkAAAkLAAqamwkAAAAKwAAAv/7///////////7anfD/n////w//////3+/97/79/5qaAJCQkAkACQAAAAAAAJCpAAkJqQAKkNCQAAqQALDgysoPrpqcoACpmpCwAAAJCwAJCQAAkAkNAACakAAJAAAJ/////////v///+2t68/e/9ra3//////+/vy+/e2+/K0NDgCgAAAJAAAAAAAACQCQCQAAAACQAKAACQAAAAAJqQDfCQypAJCQ6QkAsACQqQCQAAkLAJoJqQkJAACQsACf///////v/////9CenPv78P///v/e/////9/9/r/v2pCwqQkAqQCQAAAAAAAAAAAAAAkJAAkKkJAAoAAKywCwwOCvDwvAkAAAmtqQkAAAkPAACQCQkAmQmpoJAAAAAAAP/+///v/+/+///qnp+/3t//3//f///P///v7+38/e/AANAACQAAAAAAAAAAAAAAkAkACgCQCQAAsAkAAJAAwAoJwP8PC8oLCgAJDwqakACQsJAJAAkAALDQkKkAAJoAC//////////////Q+enw++8Pr8v+/9//////3/r/r577wKnAoAkJAAkAAAAAAJAAAAAAkAAAAAsAAAAKAAAAsMkKAPCw8AnAAJAAsAnAALAK0ACwCZqQmpCwCZAAAAnAD////+//7/7///ypDw/P/f3/3//f2+//3///78/P3+/ACQCpAAAAoJAAAAAAAAAACwAJAJAAAJAJCayQAAqQAKDAnPAPANoAkAAJCfC5CcAJmgkAmgAAAJCQmgCQAJoJ//////////3//umc8P2+nvr8vp777//ev9v////w/t8KkK0ACQCQkAAAAAAAAAAAkAkAAAAAAACaAJAAkAANoJAKCvra2wCayQAAAAkACpAJrQAJAJCQm8sJyZAAkACa//////7//v7+/9rLD9rf+9+f/f39/f///+///+/PD77Q6QCcAAAAAAAAAAAAAAAAAAAACQAAAAAAmgAAAJCwwA6cAP2pDprJoKkKkKmpCa0Am5AK0KkACQkLCpAAAJC9/////////////AmtCw8L7a/p76++v9r9//2//////P/pAJoLAAAAkKkAAAAAAAAAkAkAAAAAAAkACbAAAAAAsAAK0PreuekADQAJAJDQsJmpoPCQCQCQkLCQmQAAAACe///////v//z/wPDa/Pn9n9+f+f39/v//79/t///vz/7engkAAJAAAJAAAAAAAAAAAAAJAJAAAAAJnACQAADwAAkAAP263K2pAKkAAACwCcqcCZoAsAkKnpALAKAAkAn7/////v//7+/8CwmtCa3r7+nvD8vvn73//////////+/wAJAAkAAJAAAAAAAAAAAJAJAAAACQCQCaCpAAAAkAAArKCvrtq/mtqQDanJCempmpyamZCQCQCQmQmQAAAAC8//////7//f/LwPDantv9+f+d+/+f7f/r3/7b///+///sCaywAAAACQkAAAAAAAAAAACQCQAAAJAAkAAAkACgAAAA0P2+2s+8vLoJoKAJCaybqZ4KmpAJCwC8qQAAAACb/////////v8AkJ6cue2vzw/rzw/p+8/f+///z///////4JAJAAAAoAAAAAAAAAAAAAAAAAAAAACaCQkAAJCQAAAAC//p/byem8kAkJkAsJsNmgmZwACQkJoJCQAAkAkP///+///v//yQqQCw2tvfv/29uf2/z/v/7f2/+//////+kAsMAJCQkAkAAAAAAAAAAJAAAAkAAArQkKAAmgAAAJCaCfn/6+vr2tutC8oJwLywvJsAqQkLDwmQsAAAAAC5///////978oMnK0Nrb6+2tvtr8v8vbz9/+/8/f/////tAACakAAAAJAAAAAAAAAAAAAAAAAAAJAAAJCQAADwkAAA2v6tv9vfD60KkJCwsAkLC9C5kAAAkAAJAAAAkAkP/////v/v/9CaAJCw8P35/+2/37y///+v+//7+///////4JAAAAAAkAAAAAAAAAAAAAAAAAAAAAkAkAAAkJAAAAqamvna2vy/vL+fCw8JC5qckLkOkJCQuQmwkAAAAACQv/7/////7+AJCa0Ly9r+n9vL6f/a2v35/f3/z5//////wAAJCQkACwCQAAAAAAAACQAAAJAACaAJqQAAAAsJ4JAMkP6trfvw29rwvAkPkAALCw25CwAJAAAJCwAAAAAJ///////v/wkMCsmtva/b/r7/3/D//9v/7/v//+/////+sAAACgAAAAAAAAAAAAAAAAAAAAAAsJAAAAkAmpAAAACQqfDfrw/p6+vb6bCwvAkL0JqakAmpAJCwAAAACQCb////7//9/AAKkJD5772+2/35/p/72+//2/z/n5/////9AAAACQAJCQAAAAAAAAAAAAAAAAAJwAAKkJoAAACQCaAKnP6g0P2/vJytvwkNCwCZCwm56QCQsAkJCQAAAAAAD+/////v7wAJDa8Pnt7b/Pv+n/rf79/L/////vn////+kAAJAAAAAAkAAAAAAAAAAAAAAAkLAAAJAAAAsPCgAA8NC/3/r6/tv7+envC6mtoKnJrQmgkACQAAAAAAAAAJvf///+///gkAqQnw+/v9v97//9/737//6f29+f7/////wJAAAJAAAAAAAAAAAAAAAAAAAADwAACQmpCQCQkJrQAAAPvv3/2/6empub0J25DQmwkLkJAJAAsJCQAAAJCQD//v////7QANDLy9re3v//+f2v+f/t+f3+/v//n5///5sAAAAACQCQAAAAAAAAAAAAAAAAsJAAkACQAAnwrAAACgvPzwvtr/n5rfD/qasAsJ6amp6QCQkAkAAAAAAAAA++/////t8AkAmw8P37+/y9/+/97+37/++/n73w/+///+AAAAAAAAAAAAAAAAAAAAAAAACfAAAAAJvKkAsAkJoK0NCfqeya+e8K260L35yfCwkJCZmpAKCQAACQAAAAAJCf///+/+/ACw4Pn/r9/f//r9v/vfvfz5/P/fv/vb///9mgAAsAAAAJAAAAAAAAAAAAALywsAAAmgCQAJ6eAACQCgoP/L/tr5v9rbvwsLsJnanwvLDQAJAJCQAAAAAACQsN//7///8AAJnw8P36/r3/3//P///r//+968+fy9rf/60AAAAAAAAAAAAAAAAAAAAAAAkNAACQCQsAnwkJCwDAkNCf6/y62+8L2tqf28n6C5AJAJmpCQAKAAAAkAAAAACa/t///vwJCQ6fn769//+////73p/98PD/vfvvv/2///CQAAAAAAAAAAAAAAAAAAAAC5qaAAkAkJAPoLygAAsLDgoPDwvt/vm8ra36mpqQmembC5qQAAkAkAAAAAAAAAmt///e/9oADwva/P3/+f/P/p////+ev9/63r/by8vp7wmgAAnAAAAAAAAAAAAAAAAAkMCQCQCpALD5CQAJANAAwJDfrP7a+fnr25qb29vbC5qQmQ0LCQCQAJAAAAAAAJCa3v7//vAJAJ8Pn7/L/v//n/+fz57/2vD9v9D9vb2fn/AJAAoAAAAAAAAAAAAAAAAAsJDpAJCQucmgDgkMAKAAsMsP2w8PD76cvPntqamtvLmtoLCQmpAAkAAAkAAAAAAJv9////wAmsva/ev/372//97/v/n6/f8Pnr++/vra2+kAAJAAAAAAAAAAAAAAAAAAmgkAAAkLCr0JCQCwqQDwyw6fDtr56em7y56729vbCa0JmQkAkACQAAAAAAAAAACaD//v7wAADby9+/3w+97//7/97f79+trf6fy9vZ+9C9CwAAywAAAAAAAAAAAAAAAJyQqQCpC8nQCgALAAnAsJrAkPC5ra2/C8u8udrampC9qbCpoJoAkACQAACQAAAAkJ2+///eCQ2w+/vt6////9rf37+/vfrb/628va2++QvQCQAJAAAAAAAAAAAAAAAACaAAkAmZqQsKCQuckPALwKwLDvsMCa2p/b/Lnrub29uQuQmckAmQCQAAAAAJAAAAAArf7f7wAAqfD8/b39/f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oA8ACakLCgsAAAAAAAAAAAAAAAAAAAAAAAAJCQkLCamtC9rfn/////Cp+p8Py9rb6fvp6amfCQubwLCakAkJAAAJCQAAAAAAAACQsK2/v//////9/+3/2968va0J4JwAkACaAAAMkMAAAAAAAAAAAAAAAAAAAAAJoJAACgCQmp8LvL2v/9////CfCenpva+/nw+bm9rwmpAAmQAJAJAAAJCQAAAAAAAAAAAACdvf/////////5+frb3fD5rakOmpDLAMmtqQAJCQAAAAAAAAAAkAAAAAAJCQAACQkJC8kPDw2fqfvb///7Cenpqb+vn57b+tramZqckLmgmwCaAACQAAkAAAAAAAAAkJn7///////////f/v3/6w/a2QnpAMqQCQAAAAqaAAAAAAAAAAAAAAAAAAkAAACQAKAAsAranpq+n9++/f/fAL8L2sn5+/u8vbm5y8kLCQAJAJCQCQAAkJAAAAAAAAAAAOv/////////////n5+tvfmtrPCa2pAJ4LCQkJwAkKAAAAAAAAAAAAAAAAAKkJAJCQkNCQmtsL3pr7ntv///AAvan7y+28+/mvywuakJsJCQkLAJAACQAAkAAAAAAAAACZ+f//////////////3/y96fmw8JCcvAkMvKDgkArQkAAAAAAAAJAAAAAACQAAAAAAALALyw+Qvb2t6w6fy+AAmtravbv7n5/bn5C9C8CQ8ACQkKkAAAkLAAAAAAAAAAu///////////3//fz569va3w/NmtraCbypAJCQoJAAAAAAAAAAAAAAAAAAAAAJAJCQsACQkPAPmt6akPnr/rAJrb2tn63w8Pqbyw+QuQmwkJmpAJAACQAAAAAAAAAAAL3//////////////7+/3a3tsP2wrQmsmsCQy8kACcALAAAAAAAAAAAAAACQkAkAAAoACQkAvLC7DwsPDw/vwPAACgvb6f+/n5+tvZCwkLAJCwCQqQCQALCQAAAAAAAACb///////////////f39r/n7zwvL2a0LyQvKkAoPAKkAAAAAAAAAAAAAAAAAAACQCQkLAAoJC5ycsPDwmrD9v+kLyQC9vp/a8Lnwu+mfDQkLAJCQkJAACQAJAAAAAAAAn/v////////////9///L2f6fvfD8vprQvLCdDw0AkAAJAAAAAAAAAJAAAJAAAAAAAJAAkJwLDasLzwsLycsK2rDAsKnp6fv7296fnJvpsLCQkKkLCakAAAkAAAAAAAAJ+9////////////////2//vnw+p+byQ2pyw2goJoLypygCQAAAAAAAAAAAACQAJAAmgqQAAkAmpy8ucvAsLD9rfALwNvL+9r56wubC/CZAJCQCZAJCQAAkJAAAAAAAACb///////////////f+ev8n57fCfy8mtqcsNqemeDQCQCcoAAAAAAAAAAACQAAAAAJAAkAALALy8sPnqkLDp6amvAACwCw2v3/n9vp+QvKmanJoAkAkAkAAACQAAAAAAn//////////////////9+f8Pmp3gmtrbyfDa0JygmtrLAAkAAAAAAAAAAAAAAACQAACQAAkACQkPCQ6ZDw+bDw8PAJwLyan7+/++nwv5uZoNCwkJqQCwCQCQAAAAAAAAm9v///////////////D/np+e3auekLkOmgsJranJ6QkMmgAAAAAAAAAAAAAAAAAACwCpAACQALCw6bysqfDg8LC7AAqQoJ6cvfD5+b0PDa2QsJAAkJAJAAAAkAAAAAAL///////////////////56/D5q9Dw8MD5y9Da2p6anLy5rQ2pAAAAAAAAAAkAAAAAkAkACQCgmg0L0KnbngufC8kPCgnLDanr+vvb+vCwmpsLCakJCQCQAAAJAAAAAAC9v////////////////9va2ckJDQ8AnpuQrQ+tqcmpypAAwLAAAAAAAAAAAAAAAJAAAAAAAAAJAJC8Cp2g6fngCwvvyQC8C8ufn5+8vb25+QkJCckAALAJAAkAAAAAAAC///////////////////+dmpmgkJCfCQytmvCenpraCenpqQCekAAAAAAAAAAJAAAJAJAJAAkACQvLDQqfmtrQ8PCbCskLDaDa+fD7y8vLD56a2pCakAkAkAAJAAAAAAnb3//////////f/////9vpyQ2dCaAAnpsK0J6Z6ekNvJqcnp4AoAAAAAAAAAAAAAAAAAAAAAAJCgCcsJ2g+/ALCw8NALAPCp+p+vv5+/n58LkJCQvJCQCQAAAAAAAAAJ+//////////9v/////2fyfm9qQvJ0JAAycDwnp8J6aC8vKkAkJDQAAAAAACQAAAAkAkAAACQAACQmgCaC9vKngnpAKCfywCZ6cv5/a29sLC56QsJCQAAkAAAAAAAAAALv/////////n7/////b3/n5DQkNkJAACQmpsLyw8PDw0JCdrLy8oAAAAAAAAAAAAAAAAJAJAACakA6Q2g+enp8J6Q8NAAsPAKm/n/r/va+fnLm9CakJqQCQkAAJAAAACd/f//////////37//29uZyekJCQCQkJAACQycvfD56QvK2gCQAJCcAAAAAAAAAAAAAJAAAAAJAAAJALCZnp6eDamgALAAnp6Q6frb+fq9vpqcsLkJCwkJAAAAAAAAAAAL+/////////+fr9/9vQ0AmZD5y7yeCQkJAJCpCtvL2+kNDa0PDwoLAAAAAAAAAAAAkAAAAAAACQkKkJoOkPC5oPDJANoACemsmp+/373629+bD5ywkJAACQAAAAAAAACb3///////////2/+QAJqfvv/++cvgkAAAAACQ+a28vJ6ampCwkJ0AAAAAAAAAAACQAAkAAAkAAAAJC8kJra0OnAkAAKmgCwvbC8veu/+9v6mwmasJDakJAAAAAAAAAAD///////////2trQkAmb8P////mv8J4PAAAAAAkPmtqanJ6csMvAoPAAAAAAAAAAAAAAAJAAAJAJCQ8LDa0PDpCw4AAPAACa2t6b+//fnr258PC9nakJAAkJAAAAAAAAm9v//////////72pAAAP37/b/ryengkAAAkAAAra/b3NCwmpypCpyQAAAAAAAAAAAAkAAACQAAAA2gkNoJqQCaDJAAAJ+pDAmpvA/5+v+frfub0LqZqQmQAAAAAAAAAAC//f////////+f+crQCQsNrZvQsJCQCQCQAACQkJraq8vLyena0KnpoAAAAAAAAAAAAJAAAAkAkLCQqQkJ2p4NAAAAAOkAqayeC/n7/9v/263pC528kLCpAAAAAAAAAAvb//////////3/npmQkJCZkAAJCQAJAAkAAAAAC8mt2a0AmpCpqckA0AAAAAAAAAAAAAAAAAAACgAPCaywoMkAoACQCbCpAAmgv57/z7/wvZ+5vQsLkJCQCQAAAAAAAAn/+/////////vw////mQkPCb2ZyQCQCZAAmpDa0AvbCtC9vJ6cngoPCgAAAAAAAACQAAAJAAAAkJCQmtsJDbCwyQAMnvkKmtrQAPufvfn/+vm8sL29qekJAAAAAAAAAJuf3/////////3/vb///9+ZvJCakJ0LwMkLwNoJC9C8vQ8ACpCaCZyQkAAAAAAAAAAAAAAAAACwAAoLywAAsMAJAACw8A+gCamskJ7/+/6fvby52wuakJALAAAAAAAAAA/7//////////+f+t//////29vJ/anJua2tsA0OAK0Lywn5yenp4KmsDwAAAAAAAAAAAAkAAJAJCQkJrbyQyanKCpDLy/ALCgCpoAufrfv72/vamtmtuakJCQAAAAAAAAm/+///////////n/v///////3/C9+94NCQDaCwn5rcsPAAsJCQmcDQsAAAAAAAAAAAkAAAAAAAAACamgkKmtCwkMqQoJCwALDQALD//728v5y5+Zrb0JDQAAAAAAAAAJ/9///////9v9v5/b3///3/3/+wn8vLCZ6emprQ8A27DQnpDQ8PDgsLDAAAAAAAAAAAAAAACQCQCanpyQmpAAvAyg8OkPoAqQsLwA2/+e//8PvamtubD5CakJAAAAAAAJv//f/////////ev///////+//J6b28ngkA0NCanbrA+ememgkAkJAMkLAAAAAJAAAAAAAAAAAAAJCavLDADwmprfrwCw2pCgCwsKAL/5+fv9qfma2tsJqQAAAAAAAAAAn7+////////9v58Pn9/////f//np8LCQqbCw8NoNm9Cp6QnLDwyw8JrQAAAAAAAACQAAAAAAkAkAvJCQmpkADQr/37wPqQqQsNrQmt6//r37/wvJubvakJCQAAAAAAAJ/f////////37/fnb////////29CfDQnpwMkNCw2w8L/QkPCQ0JqcCeALwAAACaAAAAAAAAAAAAAJCw8J6QoJoL3//ssLAKAKALC8oJvem9+/+b27Db0JCakAAAAAAAAJv73//f//////362/y///////v8vpCeAAC5rQsNsPCckLDwC8sKnAsAnwAJAAkAAAAAAACQCQAJC8vakPCayQy8//7byssAsJqa8PDw+//6np/+sNupv56QCQkAAAAAAL3//////////b+9vJvb/f//3//b2ekAkJAACw0Ly5y7y8kL0JDJCwDaAAnACQoAAAAAAAAAAAkAAJCpywvAvLAPD9+ssLywCgAJCw8PD5/tv/v5+w28sJkJoAAAAAAAAAvb/7////////35+8m9////v9+/+Z6QAAkMkLDQnLnJvby8ram8DQsJ6QqQngnAAAkAAAAAAAAJCwmQvJy8C8sL8On54OsAoKmgoPCa8Pvbyw+fvbuZ/7CwmQkAAAAAAJv//f//////////nbnLn739Cbn9sPkAAAAJAAmg+Q+enpnLkJwJsAkMkOkLAJALAAAAAAAAAAAAAJ6ekLAJrangC8oKCZALCQAAmpoAn62v+f//2tD/uQ0JAAAAAAAAAL29v/////////29vpy9///5n5/bD9DpmpCakNvJkPnpy569DLCwwPCpoJAAywnwAAAAAAAACQkJCempCwy8vLywsLDwmvsACgoKCayeCf/9vtr7/7+an7mtCwkJAAAAAJv/////////////35sJCf//////2/vayQ8NoACfDw+fvenQucnLCQyQCcrakOAJAAAAAAAAAAAAsJqevL6a2ssKDKAKAA+pAJCQoJoJoJr627/f+9v5+ekJAJAAAAAAAJ29vf//////////vf35+f///////a25rJCw0JqQvby8mtqfy5qcsLAJ4JAJCQng8AAAAAAAAACQC8kJy8vPCw6csJqQsPAKCgoKkKCwCQ+f/9+/vf6fvp+akACQAAAAAAv//////////9//378PD5//////3//NmenLAJyemtvb/b26nA2wnAngkKkMoJoJAJAAAAAJAAkAkJrakA/6zwsKAKAKAJCwCQCQCgkK2vD7y/r//729vbkJCakAAAAACZ+fv//////////f+/35+f///////9ubDpywCwnp/a2tC8vJ29sMsLCQsNCpCenQ+eAAAAAACQAJqa2prL2t8LDwsLy8sPoKCgoKCQqQsJrb/9//n/v6+/CwkACQkAAAAJv9///////////7/fv/2//9//////7ema0MkNqdqfvb/fn+vLybycqcAA0AkJCpAAAAAAAAAACw0JqcC9raD8oA6enp6g+QkAkACgCgCpAMv7+f8P/5/b/byZAAAAAAAK2////////////9//37/b3//////f294JqQCa2p8NrQsPCZ2w8MsJALywqcoAsPCQ8AAAAAAJAAmtmr2vytoL27Dw+v3/CgoKCpoJoJqeCwvP//v7/fu/mwmgkJAAAAAJn9v////////////9/9//////////vanawLC8kPD72/35/+npD5Da2gkJAKnJDQvLAAAAAAkACQsL7QvwCQDwoMsPDQ6rALCQCQAKCaAAkA//v//9+/ntvLkJAAkAAAAJ+//////////////b//+/////////3p2gmcnLv/np2tq8sJD5+a28kJysAJAACa0ACQAAAAAJoLCfmvwNDr2prbrL6vvcsAoKCgoJCgsLDpAP/5/7/7+7+a0JCQAAAAAJv////////////f//+9//////////+9rZ4AsJyQ+evb/b3/+emtkJraCZoAkLAAC9oAAACQAAkA+trQsKkMDwmg2wkACrywkAkAmgCQCwsOsJr///v9/fvbmpoJCQAAAAmf/////////9////3//f///////9/L2gm5y8v56drfD9qdr57a6ekNoMkAAAkJAJyQAAAACQCQn62sAJCwsPCaAOC8kJoAoKCgoAoKmgCwDK37/9+fv5rbyZCQAAAAAJv///////////+/29/5//////////+9rbwMsLCenr2w+a362em9npra0LAAAAAACamgAAAAAAsNsJqakMrA8AvJrQsKCukKCwkLCakJoJra2gm9+/v78L/7kKkAkJAAAJy////////////f/////////////9/LyQCbDQ3p+dvP2t+p2vnp6a0JqQwAAAkAkJwAAAAAkJCavLwACgkLALAKkLAJqZ4JAAoAoAoKCwsKkADr///an5mw+ZCQAAAAAJvb/////////////fv9///////////5Dw8MmtueD+2wvanevQ+emtq8npqQAAAAAKngAAAAAAD5ywvADQAAmsmtrA8OAKmgqQCwmpAJoKCw6QkNv9v/m/Dbnp2pCQAAAAm9//////////////3//b3///////nv8JCw0Ky9ub79r9qZ69rb3p2p6cAAAAAJCQkJAAAACQsKvAAJoACsoLCampqQsNoJCgoAoA6wy5ypCgAL/7/56Qu56ZqQAAkAAJCfv///////////////v//////////5Dw0JqdvJ78kL0L3pnK2suenLCakAAACQAAvAAACQkACdCwCaAJAJCQDp7awKwKkKAAkKCakAsAqcoJALy9v/vbkNuaCQCQAAkAvb//////////3//9//3////////96c8JoKnay60Jv+y9qay/m9vLy8npwAAAAACpyaAAAACpsKAMsMkAyaCgqamgmpC9oAqaCpoAoLCamgmgkJ//+/mtC5D5kJAJAACQC5////////////D6/b69/f///////7DQDZytvNrawJm8ntucvLy8sJ6aAAAAAAkJoMAAAAkADJqQAACpoACQkNrJ4KAK0LAAAACwng6tqaCaAAC9rf+bkJua2wkACQAJkPm///////+9vp/by936+/3////9vNsLCgsAmp2tvKwKkA+py8va36nJ6QAACQAMmwAAAAAJCwwLwLDAmgoKCwmgCQsPCgCwsLAOoLCaDpCtramv+/vp6anJsJAJAAAACbn//////9/f+fC8kKkNntv//////7wA0JDJ4MqQCQkJC5DanbyfCcupAAAAAAkLDJAAAACakAsACQCw4JAAkKnLCgAAsAoAAACpCaypqQqakAy9v9+ZkJCbyakAAAAJnw2/3/////+/D8vQvJD5rb//////y8kJCsCwkJnAvLysDAy568usnrycoAAJCtCpywAAAJAADLCcsA8AmgqaCtCgvJCvCwkLCgsKDgsOmpnprLAL36/6mwsJuQmwkJAACZqb/////5/f+b2tCakAkPn//////awKCQkACeCtAAkLCQsOnLDbrQnpyQAACQnLALkAAAkJqQygnpraCQAJCamtAKkLwAoAqQCQqQ6QoOsPC8vJrb29rQnLC8CQAACQmp28v9v/+/+8vACQAJCa2w+f////35kA0LycsJAACQAAAACQsJ4NC8sLAAAAAKCw8MAAAAAA2pvaAACgoKCgCpAKkAoMsKCaAKCgmgmtCwCgsLCwv/v/mbCQnbkAkAAAkJqb2//b/fDwkJ4KnAsNCdv7////+soJCgCwDa0LAAAAAAkACemw8Ly8sAAACckNALAAAJoJqcoJy+kJAJCQsArQ4LybCwCgmpAJoL4KvJqfDw8NC9vbywmwmpoJCQCQm5m/n72p2wkJ4AANAJyQrbD9///9rZCeCQkA8AAMAMAJAAAJDwkOkNDwwAAAmgDakJAAAACanLDwsAAKCgoKAKkKmgCvAJqaAKCgAAC8Cq0KmqCw8L+/ufAJCQmwAAAACa0J69vaAJD/8AwAAKAJAJ+/////+gAJAMAJC8AAAAAAAAAAAJ4J6w8JoAAAAJqQAAAAAJAJ68kPCpoACQAAqQ6wAJ6Q+gAJoAkKmgALCaC8vZvLD56f+bkLCfAPCQkAkJuZmevZAAm//gAArJ4AkAn5////39oAmpAADvCgAAAAAAAAmgnwnLDw0AAACQwPAPAAAAC8kL8K0AALCgsJCpAOmgoPALCgCwoAAJoA4MsLCqywvL+/nr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CpCQoMupAACgmgsLCaCQAKAKCQoACQvL/56b+fv/kJoJn5qanQkJDwkLn5vPuf+f29va+fvrn+2/vf/rz+vL372tvLrKkAAAAAAAAAAAAAAKnp6ayakAsAAKmgqQAKkAAKCgALyaCpAAoJoAoACgqQCQCgALAKCb2//9v729rQkACw25+5qQmb29Ca257wvbra352t+f75/P3629vb376erby8mcAAAAAAAAAAAAAAkNCaAAsACgALCgAAkKmgCpoAAJoPCgAAoKkKALAKAAAKCgsAsACwme//vav9r7makAmfsNsAkLDwmpu9ufm9vL358L//nt+e+/r9/e2v6e2928vJ4KAAAAAAAAAAAAAAAKC8nLAKAACgCQqaCgAAoAAJoKAJsAsACQqQsAqQsKAAAAAAAKAAoJqf+/y/8P+tqZoJ26n5AAmfna0J6enL29qen/Dw/7/5/f2/r7/b357avLyanAAAAAAAAAAAAAAAkJ8LoAoJqaCaCgAJALCgkKCgAAAOmgAKAACgCpCgAJCgkKCgoJoJAA2/37/b35/b2tngvdufmQCwuZufmb+by5+b2tvfn8v/vr/P39r8vvn96entqQkAAAAAAAAAAAAACgn8CwCgAAAAkKCgoACaAJAKCwoL8LCgCgAAsKCaCgAAoAkAkAAKC5oL//+/++n/rauZC6nrCwkJ2vCw+8nam8vPvby7z73p/f29vL+f3b6en56awKAAAAAAAAAAAAAAkJ6QvKkAoKCgoAkAAKAACgqQAAAOAACQCQqaAAmgkAoAAKAKCgoAkAyf+9vL/9v729Cem9+50JC5qZnLkJup/L27y8+8+9+/y/r/7/z/6+n58OmtDQAAAAAAAAAAAJCayevKAAAKkAkAAAoAsAAKCQAAsKmrmgoKCgAACwoKCgCwoAAAAAkAoLAA////v/8PvL256bn5qQnL2em9qenQufntvb/fva/Pvf+fn58Pn976357aCpAAAAAAAAAAAAAJCpAMCwsAAAoACwAAAAoJoKmgAAAMsJAAAAoLAACQAJAACaCwCwCgkAmpC/vb2//9+avLnp6fnpqQupCa2ZqfDw+by9r579v969z+8P/56fnPrantkMAAAAAAAAAAAAAAkLwLAACgCgAKAACgCpAAAAALAKALygoJCgAACgsKmgoAAAAAAAoAoJrAvf/+va2/r9+fD5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CgAKAKAAsAsACQCgAKAKCpqQoAkACQAAsLCgoAkJALCgsAAJoKAMoLCgCaAAAAAAAJCpAAAAn77/3/////vb7b2emvmcvp/Lufn9vdr9vLz96enp4ACgoAALAAsKAAAAAJoAAAmgCgAAAKAAoJoAAOCgCwoKCgqQAACQAKCgoACQCgoACQqQAAAAoAqaAKAKAKAACpCgAPvf//r////tvr+t+f652wvfzw8L+v2v29vgvL2sngCQCwoACgAAmgCgsAAACgAAAACgoAoAkAAKANvJoACQAJAKmgoKmgAAkKCgAJAKCgkKmtqaAAAACQAACgCgqQCQAAnr////2////fz77+n+vP+vv///z5r9r+2/y8oJoAsKAAkKkAoAAAkAAACgqQoAoKAJAAkAoKAJAKAKwLCgoKCtoJAAAJqaCpAAsKCpAACgAAAACwAKCgmgAACQAAoAAAAJ////7///v/+/29vb2/n/3/np++/a/5D8sPDawAAAkKCgALAKCgoAoKAAAAAJAJAAoAoKAAAKCtsAsAAAkAkADKCpoKAJAAmgAAkAoLALCgsKAAoJAAAAqQoACgCQoAAACf3////P/L3///7+////vv//7b2tvP8LwAAAALCgoAAAoACQAAAAAACQAKCgoACgCwAAAJoAAKywCgoKCgoLCwkKyQoKkKAAoAoAkAoACQANCgAKAKAJAAAKkACgAAAAAAr5//////////n5/////b+f/+/+/w4MvAAAsACQAKkAmgCgqQoLAJoKAAAAAKAAAAoAmgAKAPkKCQkAAAkAAAoAmgkACgCwCaCaCgCaAKCgqQoACQCgCgAAAAoAsAoAAAkA+f/////p8P////rfv/7//9//C/nwAKCpAKCgqQCgAAoAAAAACgAACwCQoACwoACaAAoJAKDpAKCpqQoKCwCQoACgqQoACgCgAJoACgkJAKkAoKAAAAoACgkAAJCQkAAPC8m//////////9//////3r/Ang4MsAkACpAAAAoAoJAAoKAKAAAKAACgCQoAkAoACgkKCvkMoAAACgAJAKCgCakJCpAKAAAJCgAKCQCgoACpAAkKAAkKAKAAoKCgCaAADL/Ly/n/////////z/3+qfDwCf2wDwoKkAqaCgAJCgoAkAAAAKCQAKAACgAKCpAKkAoAAMqwCwmgAAsAoJALAA4KAKAJqaCgAAqQoKAAywAAoAoACwAAkACgkAAKAACwCQrQ/P///e3///vb//6f3g/KAL7fAAAAoAAAkAsKAACgoAsAqQCgAAAKAAqQAKAAoAAKkL0KAAoJqaALCaCgCwmtqQmgAAkAqQAAAAmpoKmgAAAAoAoAoAAACgkACpAAsK0LCwvKD7//7w/vy8ngC8sJy8v62pqQALCgoKAACwCQAAAAAAAAoJoJCgAKmgCwALCgCsCgkKAKAACwCgyQsPrangoACgCgAKCgsKAAAAAAmgCgAAAAAKkKAAoLAAoKAJCskAy9+8sAkJ6QvLwAvJ4OsL2toAAKCgCQAAALAAoKCgCgCgoAAAAAAJCtAJoACgAJoLsAoJqQDprL6amvD5//8JwLCaAJoAkAAJoKmtoKAACQALCpoAAACwAACgCQAKCQrLAAAAAAAAAAAADwAKkJCsvamgoAkAoKmpoAqekAAAAAAAALAKAAoAqaC+C+kACgAM8JCgCgkKm9vLDb/////6mgrAmgAKCgoACQ4ACQoAoAoAAACwoKAAsACQoAsAoKkAAAAAAAAAAAAKALywoKCaCsAAmgoJAAAACwCp6wsKkKkAkAAACgCaAPAL3L6ekKkJoKCQ6QoJrb/w///////88JywoAqQAJAKCgCaCgkKAKAKCgAACQAKAKCgAKAJAJCpqaAAAACgkAsJywAAkAmgkLCwAAAKCaCgAAsPnwAACgCgoAoAAACg2gv8v/DwoACunpAKkKCa3//L2///////qeug2pAKmgqQAJCgnACgkAkAkAoACgoAAAAAAAAKAKAAAACgsJAACgAAoKCwoKAACgAAoAoACgCQqa378OCgAAAAAAAAqQoJq9/L/9/62poLAAqQoJypq9+/D7/////735/foAoAAACgsKAAoLAAoKCgCpALAACwCaAKmgoAoAqaAKCQAAoLAJrakJAAAJoLALCgAJALAACgAJqfywkAoAoAAKkAAAkKyev////fDgANupCpCgsA2vvLvt+/v//fv///2tCpCpAAAAmpAAqQAAAKAAoAoAAAoACQAAkAAJAAqQoKCgAAoKAAoKCgoAAAoAAAsKAACpAAoKnpvKAAAAAAoAAKAKAJq56f3///+fALwAqenLALCfDwyb/en76/3//56aAAoAqaCwoAoLDgsKkAmgAAALAAAAoAAAoAmgoJAAAAkJoPCQmpAAkAkKAACaCwAACgAAoJAA2toJCgmgCpAACgAACgAOn/////DgoMqekLCwCwvw8Nu+mpsPn5+//+2gCwkLAAAAALCQAAAAAKAACpoACgCgAKCgAKAAAKCgsAoKCQCgoAoAoAoACpoAAAALAJoKAKCpoLDwoAAJAACgAAAKCQD5////+86QkLmprw8JrJ6f++8Nra356wvf/braAKCsoKCwsACgoLCgoAALAAAAAJAJAACQAAAKkAAAAAAAoKAAAACQAAALAACgoAoACgAAkAAAn/+fAAoAoAAAoJoACgsKv9//7bCgoM8JCpDgmpv//5/72/v72toLvtsAmpCQkJAMC8vAkAAJAJoACgCgoKAAoLAAqQoAoKkACgsJCQsLAKCgoLAACgAJCpCgAAoAoKmgqf/p6QAAAKAAAAAJAAC8m///2skAALqa+euQoP////8Prf3/ywvf2/DpoAoKCgravLCpoLCgoKAKCQCQAAAKAAAKAKCQAACgsAAKCgAACpAAAACgqQoAAAAJCgCQAAAJv///CgCgAACpCgCgoKkL7f79oLCpoJ2vkLDKn7///9qfn7/9vp+///ywAKnpya2r376eAACQAACQCgoAoAoAAAoAAAAKAKAAAAoAAACgAAAKkKCQAAkKAKAKCQAKAKAL7fnwAACQCpAAAAAAAJDr2/n6DwAAAOsJ68sJqf////8Kn///+f////vAsJC6vpvf/98JCwoKCwoKAAAAkAkAmgCwCpoAAJALCpCpALALCgoAoAoKCgoAoJoAoAoAsAsAm74AqaAAAAoAoJoAmgqQ688NoKCgAJ65C54Kmt///w28qf///////9+8AKydvf////76AAkAAAAJCpCgCgCgAAAAAAAJoAoAAAAAoAAACQCQAJAJCQAJAACQAJAAAAAKDAmpAAAKAAAAAAAKAJCpALCgkJCQoPkOngmpy7//+/oJv/////////8LCwu///////+csKCpoLCgoAoAoAoAoAoAoAoKAKAKAKAKAKCgoKCgoKCgoKCgoKCgoKCgoKCwmpoAoAoACgCgCgCgCgqaCwqaCgoKALCpoJoAuf///wALD/////////+eAL3///////8KAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBQAAAAAAALStBf4="",
    ""Notes"": ""Education includes a BA in psychology from Colorado State University in 1970.  She also completed \""The Art of the Cold Call.\""  Nancy is a member of Toastmasters International."",
    ""ReportsTo"": 2,
    ""PhotoPath"": ""http://accweb/emmployees/davolio.bmp""
  },
  {
    ""EmployeeID"": 2,
    ""LastName"": ""Fuller"",
    ""FirstName"": ""Andrew"",
    ""Title"": ""Vice President, Sales"",
    ""TitleOfCourtesy"": ""Dr."",
    ""BirthDate"": ""2/19/1952"",
    ""HireDate"": ""8/14/1992"",
    ""Address"": ""908 W. Capital Way"",
    ""City"": ""Tacoma"",
    ""Region"": ""WA"",
    ""PostalCode"": ""98401"",
    ""Country"": ""USA"",
    ""HomePhone"": ""(206) 555-9482"",
    ""Extension"": ""3457"",
    ""Photo"": ""FRwvAAIAAAANAA4AFAAhAP////9CaXRtYXAgSW1hZ2UAUGFpbnQuUGljdHVyZQABBQAAAgAAAAcAAABQQnJ1c2gAAAAAAAAAAAAgVAAAQk0gVAAAAAAAAHYAAAAoAAAAwAAAAN8AAAABAAQAAAAAAKBTAADODgAA2A4AAAAAAAAAAAAAAAAAAAAAgAAAgAAAAICAAIAAAACAAIAAgIAAAMDAwACAgIAAAAD/AAD/AAAA//8A/wAAAP8A/wD//wAA////APAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACf///////////////////+AAAAwAv+AAAAqQAP//////z////8AJ/8AAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn////////////////////9sJ8AAAAA/L/+ngv//////w////wJ//4AAAAAAAAAAAAJAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAv///////////////////////4AAAkAnwn/wL//////8P/////v/8AAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ//////////////////////8PCwAAD/AAAAC////////f///w+f4AAAAAAAAAAAAAAAAAAAAJAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP/////////////////////+Cf/8Cp8AAPwAv///////6///z/78AAAAAAAAAAAAAJAAAAkAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC///////////////////////AJy+CcAAAAAL/////////f/8v8kAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn///////////////////////wACf8L+p8Km/////////7////AAAAAAAAAAACQAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA////////////////////////8An8vA3//9//////////+fD8AAAAAAAAAAAAwAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ////////////////////////AK8AAAAAAL//////////7+AAAAAAAAAAAADQAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACf/////////////////////////p/AC/AJC///////////38AAAAAAAAAAAMAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD//////////////////////////Qnr/Jra/////////////gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAv/////////////////////////AAAAAACf////////////wAAAAAAAAAAJwAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ///////////////////////////wv9v///////////////AAAAAAAAAAkAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP//////////////////////////////////////////////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC//////////////////////////5+f25///////////////+AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAv////////////////////////f2//7//mf/////////////8AAAACQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ///////////////////////9u//5/fvf/5v////////////wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP/////////////////////5+//fvf+//7//+f///////////wAAAAAAAAAAAAAAAACQoACcsNqamvnpvpAKAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC/////////////////////n///+/+/v9+///+b///////////gAAAAAAAAAAnKkKkPAMkL2pran/n56b2bDJwPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn///////////////////+f//vf/5/Q+fnfv///n//////////AAAAAAAAA2tqZ4Jyw+by9rZ+b2wvLmtr8uan/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA//////////////////37/7+f+9qfCtCwoJ25v/+//////////gkAAAAJywub2pn5vbD5sLmrD5rfm9rbmby9qfAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAL/////////////////b+/2/zw8P38va0Nnwvv37///////////AAAAAAACcvPC8sPC9uen56duemw8L2w/pua2/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC////////////////9v//5/w+f+8sL0Pmp4A0JC9+//////////AAAAAwLn7m5vbn5vanpuemanprfmfmtuZ8PnvAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn///////////////vb29rerb2pyfnw+9ranbDw+enb//n//////gAAALCdqcvPCemp6fm9D5rb25+anpra2+nw+fAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/////////////76fv76f29vLy/vJ6fng29oNsJ4J6cuf///////AAAkACp+5m58J+fmw8Lmemtranp+fm5rZqbD/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAL///////////9+f37zf2tqekLnJy/vL/b8Pn7yememtD//5/////gAAwNvbDa0PC8m8vPm8vJvbm9ufCw8Pm+np+fAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACf//////////27v/uc+wvLDwn96fqQ25y9D56cv5npD5qQv/n////AAAC5qa25vwvbvLm5vL272p7anpufnwvJufmvAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn/////////n5vfyw3LDbydvb8Knpn/D8sPuen7yesPCe28+b+b//8ACQnL29sPCbyw29ra25qcvbm5+enpqb2/yw+fnw8NAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA////////2b+f+5/LC8sNvp6Q29ueCcsJrQ29qdvZyZ6Z6bDw/////A8Km8vLy58LnbCw+bnp25sPDan5+fnpqbnbD/n5m7+ZDAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP//////+wv/D7za28npvLDQmvCeDZ+anw2p4J0KAPC8vanw8PD7n/8AAJy9ufm8vfCw+fkPy8sPD5v58Lyw+fn56w+fsL2v28+a0JoMvLAAAAAAAAAAAAAAAAAAAAAAC//////wvZ/5+cmtoJ8JycsL/Qnpn6mtnp+fmfD5+ZvZsP0P+fnp//8AmtqbDwvLmwsPnpr5ubnwuQ+Qufm9sLy8udsPnwvZqbD5Dw25Cf8AAAAAAAAAAAAAAAAAAAAAn////7yfnvnw8Lyanamempy9C/2ekN/an5vLy8vby+2+35v5nw+em//AAAnJ+b256fnQufkPnp6fDbD7zwvLyfm/yan7y9q9vJueubrb+prwAAAAAAAAAAAAAAAAAAAA////6Qva2/npDQvJ6Q6Q8PkL0Lqf+5qfnp+fn58Nvby5++nvC/np6f8AkJCwvLDZvLqbyw+by5mp8L0Jufm5up6Zvpqf+Qna2w252tm8nb2cAAAAAAAAAAAAAAAAAAAP///Qm9+58LyempyakPnLCQvan53wn9n9rby9vem/vfvcvZ25/f+/2f4ADgANC5+6292tufC8va2fC8v7y56enbn62b2/mp+rkNqfC5rbm+mrwAAAAAAAAAAAAAAAAAC///y5vPsP0NCpCcsJy5yw/by9+evb8L6b29v56b/9D7378PsNvw/9vpAAkJCwsNsNmpqZ7a2b2purn5ucuen58Ly9utqfnanZ6bnpvJ+p6Z+cvAAAAAAAAAAAAAAAAAn//pna2w3wsL0A8LD6nAudsNvan5D5//29v/3/n9vb+fv9v/n7y/n6+fvAAKwAya2wvb2tubDwva2c8Lz735sLD5va2b2vq9q9mtuem8n5npr5mwAAAAAAAAAAAAAAAA//mevb37CenADwn8kNvbDw2629C/+fmfv/25uf+9+9v5+f29/b3f/f+v2/CQCQAJqfC8ua2tufC9q5vbnwsP29sLy5rQvfnanavQvb0LD56b2a0PAAAAAAAAAAAAAAAL/wnpmtsNsACamfAJvbyw+anNvL/b3w/5vb/f//n/vf/f/7//v/up+/n5/LwAmgkOnJvbnLn5y5+Qna2tqfn5ra29vw8Ly72p+p0Ly5q5+am8vpv5sAAAAAAAAAAAAACf4J+drZraDQ8Nngn/Dwufnp26nb256/2/3/2/n5//n7+/n/29/5/fvf/a+/sAAAAJCa2py7ywu8vL65vbn56a25vJqfm9udvanbr5vL0NqfDbmekNDwAAAAAAAAAAAAD/mbyw8OkNoJCwqdsJ+f3p6dqd69rf/b/b+/v///+f///fvfv/n//5/72/28/wCcsArJn7nJvbyfm5kPyw8Pm9sPCb3wvJrfy56dkPm9qb2p8NqZ65vLAAAAAAAAAAAAvwDbDbmw8AkLydC8vampuemp+/n5+5+9v9/f3/n////9///9+/+9v7/b77/72fAAAJAKyfq8mtsLyem5vbnw8Pnw+pqfm/mvuem+vQva28ufC58Pmem98AAAAAAAAAAA/AvQ+Q0NAPDQmp+b2tvfD5+fDQ+fvf/fn/v7+///n/n/v9/7/f/f////n9vfvr8AkMCQmw2bn5D5vwvLyw+b256b0Pnw8J6fnpnpkL2tsLnpm9rbnpnwsAAAAAAAAAAL8L2pmtoLCQmp8PCen7y5+cvJ+/n7372r2/3/39+f+////7+f//v7/f/b/7/r/98AALAKmtup6emw8J29ufmtqem9qb2tuen9uQ+by8vbDa28vLmQ+eqfnwAAAAAAAAAPwNrbyQCcng+cufn58J/a2/n729/569v9vb/5+////fn5/f//+/3/+/+//9vfn6/wAAkJyanb256fn5qbzw/bn58L28ufD5sL7bDwsJC9udsL29r5qZ2p68kAAAAAAAC/wJ0JrQ2gCZoLyw8Pn5vfnL+enwn/n/n729v/n9+fn/+/+9+9//+9////2/+9+9vfCQDAAJ6anpmp6fnpubC88Pn5qb2psPn/mw+b0L2w8L29Ca2enwufmZrAAAAAAAD+Cam8kLAJDw2dvb29v8+7m5yb2/+f29vb29+b2/v//5/b3/v/vfn/vb+9//3/vf+/4AsJqckNufDb2wufDw+bm9qenwvby56fsPm8C8sPm8vam/mpsPnpra2wAAAAAA/w2p8JAAyemtuesPva+bv8/8u9rb28ufn9van9v5/9uf+9v7/b/7/7//3/v7+72/+f8AAAALD7ra2wsPnp+fm8vLn58L28uem9+Zrb0Jvby729rQvQ+Qm9sJC9AAAAAA//CcngnpsJ8Jr5nw+fn929ub39vfvb372t+b25+fn7373/2/29+9vf2/+//f/f/w/73wAA0AkJ25qfnw+fmprb2w8PCem/y5vLz52pqcvbnQsL29q9D7ya29vLkAAAAAn/ybCQAADwn52+n5/5+r27z/m+m8n/vb2bD9rb29+f+fvb/fv73/+/v7//v7+/n/n/v/CQoJ6anp0Ly5Cw+fnw+fm5+56b28u/uanb2pC8sL29qb0LmQm9sLCfDgAAAAC8oJ8MkNCQ8PvJ+fC/3fv8mb7b/b+dnr+9ufm9vbn/n9+9v73/+9vf39+f//35+5/w/fAAkAAJ+fC9uen5vLy7nprakPn5rb0Pnp+tqcvb28vQuem9vpr56cn7CQAAAAAAmfALCwvpvbyfC/nwu735v/29ufn7v5yfn5n9C9+5/7///9+fvfv/v7//+fv/+f+fv7wAANCemwvanp2p6bm8+Z+fn5ra2+n/mwmb0LnpqZqfDbyamfkPmwqcvAAAAAAAywsADbycvJv7/J+f/d+f+bv737+f2fv5+f+725mfn5+fm/v9u/29//////+///v///8AmgoJy9C5+am9sNrbmvC8sPm/m9qf7by8vQm9vL2p8Lnp6Q+a2dm5m8kAAAAJrZwNoJv5m/29n7/5v7v5/9/b+fn7+9vbn5/Z+f+fn529/Z+f37//vb/5+/39v/27/b/wCQCanr0PD5ra25+enb256a2w8L2tuQufmp6byan5C9qZvbC9sLra8JrAAAAAmaCQna0PD5vL+9rf+f2/vb+9v/+f372/+fm/nwnw+evbn/n/v/+///2///v7+fv9+//wAMkAmZqb2w29sPC5+prbn5vbn56fvLnpqcuem52wvan7ywnLDw2fmekAAAAJy8Cw8Ln5/a35/b2/n9v729/f/5m9u9vfn7/9n5+fn5n5/5v729/7+///vb3/n//fvf/wALALD60PC8uprb28vb2w8PDw8Pm/memb2pn56esPCfkJ+em9uZq5y5+QkAAAuQnJm8mp+9u9v5v5/7/f/7+5+f/737/7+fn7/b29sP+fn7/f//vf/f+9//+fvb+///n/AAAMkJu5+b2f2prbm9qfm5vbnwvL6Z6emeC5vZ+b2p+wuZ6a2tnLvLDwqQAJy8qawPn9Dw37/fn/m9/5+f/b/5+fv9vb//+fu9vb2Zvbydv5+///v73/vb+///3//7//sJyQrQ0PDwsLn5+enr2w8Pm8ufm/28m5vJva2pqemfDb3pm9ubC5y5+fnLkJuQkNm58Lvb+fn7+9/7+/v73/m//5/b+/2/n/372tvJy5v72/3//7//v7/739+/+/n//7wAoAkLC5+en56anp/bD9vQvL2p6fsJvp6b2tufn5nrm/C5ra2g28uempCwwL2gnpra29/b2/+9n/+9vb39v5/9uf2/n9v9v5+dvb2/uf29vfv/v9+f39+9+/v9vf//+f8AkJC8m8sJuem9vbmp+amr29qfn/na2fnanbya28ucvJva29vZqb0L29vbkNrQCfn58Ln6nb37/b3////7/b2/2/vf+//b37//vb29n9vb2/29//v/v737/////7/7//8AAOALD5+fD58Lyw+fD5+dvLnwsP+pqbqdqbvZqby5+9qZqamtnLnwvLC8sJkNqcsPn9rfv9vfv/vb/7/fv7+b/5+5/b2/vb29mfnb29vZ/Zv7//+f2/+9vb2/v9+9+//wCQkJyanpsPn5vbD58PDwu8uf2/Cfn8nanw2p28uekL28nb2am56Z+Z+ZAL6anL256b+/2//735+/+f+//9/72fvfn7/5+9uf////+fn7m/3///v/v9vf//////v///vwAArJqfm5y56emtueufub372prfnwmpup+9uesLnLm8vLm+mpy8m/C/Dw+ckMm5y9n9nZv9uf+//f////vbudv737v9ufDfn9v/29/b+d/9v/+f/5vb//v5/5/7/////wAJCQnLy8ufn5vbD5nwnp6Qvb2/sNvbycva2p28m96fm9rZvamb8J8JsJCby5rLn6+b+/2//5/b+/v7+9//3735+f2fDZuf2b25+b+f372////pn//b+/3////f/7+f/5AMCgub2py56a2tvb6b+fm/y8sLy6mtubD5van56bmp6b2vCdrZDwnw29CfANm8udvPn7+fnfu9//3/37+/+9u/n78Judn5vZ/b39vb+f/f//+QCfm9n7v7+/v////73+CakMDwvbnpvbnwsPnw8Ly5vb29+dvbytuf6Z8LmenbnwuZ+wmw+b2psLD7CaDb27256f+f+5/fv5+9v//fvb39vZ29nLmb2/m9uf2529m9mQAAkJrb+d/f3//7+f//+/AAAJCb2p6by88Pn5rbn5vemprfqa2tubnwmem9D5+w8L2+kJ6fm8ufCcucnp2/D9qf2/n7/f2//////5/735+QAACQm5y9nZnJCQkJCQCQkAnb3L2fn/v/v9////v///AJywuem9mtvbm58L2w8Ly5vb2vnJua0Nrb2pramvDbn5rZvakLy56Q2wnwCZqdv5/5v9v5v7/73/vb//+fvbkAsJCQCQkAkACQnJrQkJnLCfm9u9v7/5+f+/v5//37//AAAAy56a29up6en9qfm9ufy8ufu8n5ub2wvQm9mb8PC9u8udvbnLnpAJC/mtnw/bntvb3/29vf+///n/v9+9qdm9sNkNCQm529m9mfn5+dvZ+f2/n9+/v5//////v/+/wJqQkPn5qa2fn5sL2p6a2puby/ybqfDwvfmp8LDwm5+QnLnpCw+56QAA/QDavLm9+b2/v7/b+////7/737+fn7/b35vbnb2fmb2fn5vb273737/5/7/9//+fn7+///n/sAwKDwsPn5vpqen/m9vbn56embm8nLkL2prZCw29v8vL+56Q+duckAAJmwm5258Pm9v529/7/fvb+f29v/n5+/2/m/25+9vb29+9nb2fndv5+9+f+f+fvb///9//2///wJqQmb356byfn5vLDw8Ly8uby/0Ju9rQufkPvZqbyb2wkPn5Crybn5+Q6QvJrfn8v/D/v/n5+/////v/+f///b/f/b/fnb29vb3/+9+9v72//7/5//////+fv7/7//+/8AAAy8sLn5+pqa25+bm9vbD8sL68nLm5yw+QmtnpvwvLnwsPnZvZ/fn5npy/mp6b0PvZ2/v/n9v9vf25/5+/29v5+fn/+f/b3/////vf+f////n/v9v9v9v9/fv//5/78AnLAL28sPn9vZrfDw8LC9ub2/mbC56cufnp+bqfCfm56b2wmp2/+///CQqQ+fn5v5+/+f3b/7+/+/v/+f/9v/n/+9v5/5+f+/ufnb373/+////5/b///7/7+/3/v/+9/wAACbD5+fqa2r25+fm9va2tqfDw2ekLnpqZqcnan7y8uekNrZ/53/2/kPnfnp69+fnb37+7+f3735/fn/vb/b/7n/n5+fm/n7372/+f/7/f2/+f+/+fv/+fn/+//7///wmgnw+bD72fm9mvnpra8Pm58NsJq5+cufnp2rC9sNuZy5+bm/n/vb/98J6a2bnLnw+/v5/f//vfv/+/+9/729v9+f+fv5/5/fn9v5+/v9v/v5//n5//35///7/9//37/8CcAJvJ+8vpra+em5+b2w8PC728namp6QuanZ8L2w+pvLDw2f+f+9v/+Qn5rZ+96fn9n/m/n/+9vb/b3729v/2b29v52/n/v/+/3//f29+/2/+f//+fv/n/n///+/+///AJC8C7Db29m9n58PDwvbn5+fsJsPnJm9vJ8LC9qfna25+Z+9v5vfn78NsL28n5vb+b/5/5/5vf//n7+/v//bv9np+evb/5/wn/v/2/v/vb/fn729v//b+9+9v7////n/AKAJvQ+5+a8Lqem9ufnw8Ly/DbDbC5vLCbDwnanbC9uekP+dvf/52/0LD9vLv5+fn/25+f+f//vb+9/9/fn73b+Z29n5+f+fvb25v9n5/9v5+9///b2//b//+f/7////wJDw2pvesPn535vLDw8Ln9uducuw2em9vJ+fqfmtvLD52529ufn/vfDb2a250L2/6bv9v5/729+9vb2/v7+f+9u/vb+en7352/n9/b+/n73/n/ufv/v9v/vf////+fv/8AALD7y735sPqw+fm9u88Lz62pybqZrbCwsJ2p+am9sLCfv/2/+f+/sAvbDavfD5n9n72/n/v7/b//vZ/f29n7398NvbnZv/+e+/v9vf/b+9+f//29+f29+/v/v///+f+gkAkJvZqen529qbyw2bn5ufvQucnw2a2cn5qfCdvLDb29mb/5//n9yfCfC9mp+f+fvfv9v5/fn/29//u/v/+9vbn7D5+/2fn5nb37//v9vb/729v/v7//v9//////vb/AANrby/n5+evbnw+fvtqenvkLnpsJutmpuenwn7C52pvb//3/v//bCwnwvb6fm/n7273b/f/7/5/7mb35/5+fn729+f29v/8Jyw+f/72f//29/735/f373/////+//Z/wCaCQvw8LD7npy5ua2bn5+Z6cma2a2bDa2pC9sNnwuf39vb+f/5+/Cf6fnpn5/L+9v9u9v5+fn/vf//2/n/n5+fnbmb/b/Zn5udv5mf+9vb//ufv/+/vf+/v/v///+//wAAkPnbn5+a2/sPD5vp6em/mp65rZrQsJvb0L26mtqbv/+fn/n//b0Amtuenan5/L2/2/n/v/+f+9+b+dv7/7+f6dv9m9vwCf252/wJCb29vf/9+fn/+/3/////+///v9AJ6Qq9qen58J+b28m9ufD/6Qmema2728kJqfCdvZnf29v5/7/5CfC9vbDZvp+9ufvb+f+9/b3/n/v/n/n9+9/5mwnbwACdubn/vfuZ+dv5/5vbv/+9//v///////vb3+CaAPnbvbnpv/C8sL8Ly5+bmfnp2tqckLC/nwvLCan7+f+d+fyQn7Da0P2b6fnb/b2/n/nb+/+f/5/5+9ufvfuQ+f2wm52a2f/b27//n7//+5//39vb+f////////////AACQmvyw+by5n5+fn9uem/ywubCb25rZ8JC9m9vJ+9/5/6n5kJv9qbmwutmb+/m/+f+fv735//v/n/3735+9/fn/vb29vZv9n//f+f///9vfub+/////2////7//vb//AJDpv5vfnpvL6byw+prbD58NDw8JvLmpn58Lyampn/nb/9udv9v8nL3pyb6dqf/Zv5/5/fv/+f/f//vfvbvfm9vb2//by9n7/7+9/5v///m939//n9vf/7///////5//kOkA0PCwvb29vLn5n72tufuambmtqfDakLCfmtvZ/5+/+Z2//5/5qdqdv8n735v/+fm/v5/b//+//5/72927/bn739m/mb+f/9/7/////5n7v/vb///7/////////5v/wJCam5+fmtq/m9qa+fvby/kNvLybnwm5y9n5rZsL+9vb2fvf+f/Qnam+mZv5+f/bn7/f/f+9vb3/+f+f+fv98J/Z+5/9n/n///n//////5//35//37+f//////+/+9/78ArAntran5vQnp+fnpywvby5Cwnw8PDakLC8mw2f3b/9m9+//9vwsNvZvL28v5+9+9+9v5+f//vf///9v/mfn5n/mfkL+f+f+f/b//+/+f+f+/+fv////////////5/f8JCam5ub2vC/+byw+fuf2/vL29qZuZsJ+cmbybqb+5/7+b35/7/5y7y+n5rb2/n737372/35+f/5+9//+f+9vbCb/b29v5/5+9vf////+5/5/////9v//////////5//+gAJytranb2QvLn5vw8LD/mQkLnLy8D5C5vpmtn/2f/9/9v/+5/QCcmZ8J+fvfvb+fv9/5+//72//fv73/n/+9n5m8vb2fmf/b/7/72/n/+//5/9v//9+//////7/7/7/Qngmb29upr/ufC8sPn9ub2p65ywm5uQ8PCZ6Zvbm9/7n5/b/f/wvbD/C/D5n72/29/bvb/9uf///7/f+/n9vZ8J/bn52/n/2/2/////+fv9+/+//9v///////v/+9+f/wAJqemp+fn5Dwvbn56an/nJnJqZ8NDLmQvLkPn/n7/9+9v/n5nwkPsJ+duf+9vfv7+9+9mf/9vfn9v739+//729qcn5qdvb/b////+9v/373//b/7///////////b/5/wCQy5+enp8L+fmtvpu96euaC9npC5ucsPma2wm9+f/7373/+/+fDZ+fnr25+f+9n9v73/+9vf/7+/3/+/n5+9vb27kPn5+9v9v///////v/v5///f/7////////+//b/5oKkPC5+an9ra2w282pv/nJ0LCQ+ekLnQsNCZ//Cfv/+fm/3wnwmpD5+ZvLn737+b/f+9vb+/m9/9v5/f+//f/9vd+Z+fnb+f///7///5/9//+f+/////////////35/w0JD5vQv9+a25vLvbvby/kLC52tsJC9CwnakPm5n73///n/+Z8A29vakP29/5+fvf29vf//29vfvb+9u/n9m/v5+auf35+/373///////v7////v/v////7//+/+/+9v/AAmw8P+bC/mt6b2py8vb6cvLCQnbwL0PmpqZ/f+f//vb2f/Ln5qQ+b+bn5ufv5/b+//7/5vfv72/n/35+b+f2/n535ub35+fv////73739v9v/29//////+////wvb/8Cay9ubnp/Q+bmtvfufm9mpma2pqZsJqQDZ0Jv5+b+9/9sL/Z8A29vJ/Quf29vb+9vb29vf+9/fnf+b/5/9vb+fvbC9n5vbn5+///n/+f+/37/b/7/b/7///f/7/8mdv/4JCby8v5q/np+amp8Ly7+Q6dCckOna2puaAL/5/Zn/v7/Zm/AACem5mtnrvf+fn9v/vf+/37+/+/n9vb+bn52/29/by9+/29///7/5v/n/u9v/n///+////7////Cb/9CQran5sPnw+fD5/5rbn/DLmamwv5sJmcnJuZ/5+b/73///D5AMm9vfD5vZz7n727/b2/n5+9vbn5+fv/n9+fufm9uZ+fmdv/v//9+f/5/5/f2p/b+f/f/5/////wCd/w8A25+a35+fmpsPkL2w+duQvpwNkJC8sLC8AJv5/Qvf+f+9+fCQCa2pufn7uf2fvf2//9+/+fn9+fn5+f+/v529vJvLn5+/+Z//v7n72fmtqbn5+9v7+///v/+/v/Cb/wCpqfC9sPDw+enw+9rby7y9Cbmg+a0JycmbkJ/9v5m735/5vwAACZn929qd+b+f27+fm72b2tsLD56/n5vb2/nw+8vfnb35//v//9vfv8+b39ufn729/9v//////wCZ/w0Anp/L+bv5vLCbnp8Ln9sAvJyZC9qbmprQ0Jv5/b39+/8L/QDpAPmanwn7n9v/vfv7/fv9vb29+b2Z2/29vf+fmfm5+9ufvf//8L/56b29ub2/n5/7/7//v///+/CQ/wAJ6bm9rw2/y9vL37n9qbyb2bCwvQkNrQkJqQ/9v9m/v9+d8AkA6bz725+f27/bn52f27nan5+fm9uesJm/29ufn5v9rb3/n7//2fyb2fm9v/////+/2/+f//v//8n/vwqem8vL2fvwubC9qw8Ln/vACw0Nmp8LkJy9AJv/n56f//2/sAAJANudvPnpvb29+fv5vJ+9ufvb/73529rZr7/b28nb29vb/5v/rbufn5vb35vb///7///7/////wuf/ckJvb25+py9va0L29va29uZrbCw+QkNqakLkPmf+fmf/7n9rQAOCa2r25+fn5/7+dvfm/Db29v9vfvb+b29mdu9rbm9vb29vf//kN+9vZ/9v/2Qmp//+ZCfn////60L+p6a2tran/uemtsNra29rfngmQ0JAPC5nJC8CQv/mf27298L0AAJAJudqfn72/29+/m68Nuby9vb2w29mdvbv5/b296b2/vb/7/wn7/a2////ZC9vfvQAAvb+////wC/rakNm/m98L3pvan5ufmvm/AJvLCw25DQ8LyZsLnfn/mcvfm9oAAACd7bn5rZ+9u/v5vZmbnpm8n/n9v5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kAAKDQCp/pv/+Z+frb+Z//AAkAAJC/m5n5//v/vf////n///+/+9/7//n5///7//v///////////vaCvvLywoAAAkJyQ2pyeCckNCbyQnpnw8JsJkJCQkOkAAAAAAAAAAACQybmduZ6bkPn5/bCfvL/Qnp/b35vf+9v//////7////3///////+f///9v9/f/7////+//7//8JvZyw8MAAAAAAAAAAAAkAAAAPAACQAAkAAAAAAACQAAAACQCQAAAJAAkA8L3/n9+b8Pm/2tn5v/mduf+9////+9///73//5//+//9///7////+/2/+/////////////3gD6/QmpAAAAAAAAAAAAAAAAAA8AAAAAAAAAAAAAAADwAAAAAAAAkAAAAJDQsJ+5rZ29+b27+f+f+//5n73735//v/n//////7////v//9/5/b/b////////////v///uQCQvp7eAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAkADAAAAADQAAAAC8Cb2fvf2an73tvfn/n5/b2///+/+/+f////////+f//////2///////////////////////nLAJ8Lmp6cAAAAAAAAAAAAAAAJ4AAAAAAAAAAAAAAAAAmgAAAJAAAMAAkAsAmvDbC9+9ub/5/5+/+/vf+Zvf/9v/2///+/2///+f/5//v///v//////////////////7ywALy97fAAAAAAAAAAAAAAAAAOmgkAAAAAAAAAAAAADAAAkAAAAAqQAJDJANCZ25/bvL/9vbm9vfn9v5//+9v////7//3///n///v/+/3/+9////////////////////vACfnwsLywAAAAAAAAAAAAAAALwAoAAAAAAAAAAAAAkACQAAAAAAAAAAAK0L0L8Pm8n5mfn/2/+9+/3/vb3//b/b///b+/+/+///3/3/+///v/v////////////////9qQAAvp8J8MAAAAAAAAAAAAAAAMsAAAAAAAAAAAAAAAAAAAAAkAAJwAkACQkAC8m9rb+f/t+9v9vb/b+/3/+/+/+/2////f//n/v5+/v/v/n//5//n/v////////////78AkLybwLwLAAAAAAAAAAAAAAALwAAAAAALAAAAAAAAAMAAwAAAAAAAAAAA+skJvJ29n9ubvb37//v/39v7/9//3///n///vb/9///////b//vb///9////////////+9AJAP2sC/C9AAAAAAAAAAAAAAAMsAmgwAAAAAAAAAAJ4JAAkAAAAAkAAAAACfCcm6m/sL3/2/ufm9/b+///3737////+/+9////////29v//Z///7//2////////////a8OCa+fAPDa4AAAAAAAAAAAAAALwAAAkAAAwA4AoAAAkACQAAAAAAAADQAJALkLyfnJ/b+b+d/9+/v9/b+fv/v/+fvb/9/7/9v/v//b////m/mdv9+/v////////////5CQCfAK2/C9AAAAAAAAAAAAAAAOmgAAoAAAsAkAkAAAAAAAAAAAAAAJCgCQAJ6QuQ+9qfn9n7+b/f2/v/n//b35////3/vf/////b+/+/+f3pn7+fv/35//////////+emgkNsNDwv+AAAAAAAAAAAAAAALwAAAAAAAAAAAAAAAAAAMAAAAAAAACQAA0Am8kPkJ/bn7+/2/2/v/2///n/v/+9vfv7//+///v//9//nwCZ6Z/5/9u//7////////vwvA8KAL6bzQAAAAAAAAAAAAAAANoAoJAAAACsoAAACQAACQAAAAAAAAAMCQC8kLyQv5C9v5/b/fv5/b/9vb+//b3/+//9/7///9///7+9CQufmfn/+b/Zmcm5n//////PCQAJDQnAvpAAAAAAAAAAAAAAAOnAAAAAAACQAMoAAAAAAAAAAAAAAAAJAKCa3Ju9nLme2fn9vb/fv/n////5//vb/5+/v9/b+b/5+f/JkJnwm/2/n/m/6QAPvL////+fAJD8C/C88A8AAAAAAAAAAAAAAPCgAACgAAAACwAAkAAAAAAAAAAACQAAAJwJqQnAufD5v5v5+/29/b/729v/+///3//f/b//3/+///CwD78J/fvZ8J/wkLnZCZ////mgvAqQvACbC8AAAAAAAAAAAAAAAKyQAAAAAAAOAAAAAAAAAAAAAAAAAAAACQAAnLybnL25/b/fv9v7+/29v//fvf+f+/+////7/9/wsAkJudC7/72bCfkJCQygD/////D5yQnLDwmsnQwAAAAAAAAAAAAAANoAwAAAAJoJDgAAAAAAAAAAAAAAAAkJAAvQqQucucvZ+8m9/b29vfv/35+//7/7/5//2//9/7n52ZAMnwnfnQsJ+9rQvAkAm/////kAoJoMkLybCgAAAAAAAAAA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AAAAAAAACcAAAAAAAAAJAAAAAAAAAAAAAAAACQqQkLyQAAAAAAAAAAAAAAAAAAAAAAAAAACQypAAANCQuQDpAACQvJAJDADQAAAAAAAAqaAAAAAAAAAAAPAPCgAAAAAAAAAAAAAACgAACgkAAAAAAAAAAAAAAAAAAJAAAA0ADQC8kAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAkAAKwA0AAArAAAAMqQoAAAAAAAAKAMAAAAkAAAAAAA8AAAAAAAAACgAAAAAAAACQCcALAAAAAAAAAAAAAAAAAACQAJAAkAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAyQwACQkLAAvAkKCaCakAkAAAALAACwywoAAAoAAAAAAPCwqQAAAAAAAJCgAAAAAJoAAAAAkJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAAAAvArAkACaAJAADAAPAAAAAAoAAJoJAAAAAAAAAAALwAAMAMAJALAKAAALAAAAAAAAkAAAD+AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAAAAJAJAADLwJwAnAkACQAAAAAAAADgmsoAAAAAAAAAAPqQygsAkACgAAAAAAAAAAAMkKwAAAAMANCQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkACQAAAAAAAAAAkJqQAAAACgALwAAAAAoJAACQ4JAAAAAAAAAAAJwKkAAKAKAAAAAAAAAAAACQAJANoAkAAAAAAJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAAAAkA6QAAAKwAAAkAAAAJAAAAAAAAAKAJoKmsAAAAAAAAAAAOvAAAAAAAkAAAAAAAAAAAAAAAAAAAAAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAAAMkJCwwAALAPAAnAAAAAAAkAAACcCaAAAAAAAAAAANqaAAAAAAoAAAAAAAAAAAAKAAsAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAAAAAAA2gAAAAkAoAkADLCwAAAAAAoAAACgrAAAAAAAAAAAAPDAAAAAAADAoAAAAACgAAAAAAAOkAywAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQwAAAAAAACQAJ6QAAoJwAAAsAAAAAAAAAAAAKAJCaAAAAAAAAAAANoAAAoAAJAJAAAAAAAAAAAAAACQAJAAkKwJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAACQAAkJAACQAJwAAAAAAAAKAAAAkAoAAAAAAAAAAAAPANAACaAACgAAAAAAAAAAAAAAAAAAAAAJAAkAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAAAAAAAwKyQAAywAAAAAAAAAJAAAACgAAAAAACgAAAAAK8AoAAAyvAAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACakAAAALAAAAAAAAAAAACgAKAJoACpwKAAAACgAPAAAAoJAAAAAAAAAAAKAAAAAAAAAACQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAkAAAAAAACQAAkADAAJAAkAAAAAAAAAAAAKAACQAAAADAoJDwALCcAAwAAJAKAAAAAAAJoAAAAAAACgAACwDAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQ0AAAAAAAAAAAAAAAAA4L2pCeCQC8AAAACgAAAAAAAAAAmgAAsKkAoAAAygAPAAAKAAAAAAAAAAAAAAAAAAAAAAAAmgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAAAnACQAAAAAAnAAA4AALwAAAAAAAAAAAAAAACgAAoAAADwwAAAsAAK0AAAAACwAAAAoAAAAAAAAAAAAAAAAJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAAAAAAJAAAAAAAAAJAJAJAAAAAAAAAAAAAAAAAAAAAACQqeALAAAADrwNoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACcAAAAAAAAAAAAAAAAAJCQC8oACaywAAAAAAAAAAAAAAAAAACgngwAmgAAAAqQoKkAAAAAAAAAAAAAAAAAAAAAAAAAAAoMCQCcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACcAAAAAAAK0AkAywkAAAAAAAAAAAAAAAAAAACQoLCwrAAAAJAPANoAAAAAAAAAAAAACgAAAAAAAAAAAACQoAAAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAAAAAAAAAqQAJyg8JCwAJAMAAAAAAAAAAAAAAAAAAoLCg0AAACQAAAK2gAPAAAAAAAAAAAAkKAAAAAAAAAKAAoJAAAPAKwLAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAmgAAAACQAAANAAsAAAAAAAAAAAAAAAAAkAwMvKCgAACgAAAAAPAPAAAAAAAAAAAADJAAkAAAAAAAAAAACgkACQAAycAAAAAAAAAAAAAAkAAAAJAAAACeAAAAkACQAA8ADJwKkLDAC8AAALwAAAAAAAAAAAAAAAAADKmpAJAJoLAAAAAACwAA8AAAAAAAAAALAArAoAAAAAAADaAKCQAAAAAJAAAAAAAAAAAACQAAAJDAAAoAAAAAkJAAAAAACQAJAACcAACwkAALDwAAAAAAAAAAAAAAAAAKCpAACgAAAAAACpAJoAoPAAAAAAAACgAACwCQAAoAAAoJoAyQDKALAAAAAAAAAAAAAAAAAACQAAkACQANAMkAAAAAAAAAAAkKCwAAkAAAAAkMkAAAAAAAAAAAAAAKAAAAkACgAACgAAAAAAoMAAAA8AAAAAAAANAAAMoMCgAAAAAAALCgAAAAALAAoJCg0AAACQAAAAAMAAAAAA0AAAAAAAAAAAkAAAAAAAkKwJwADbywAAAAAAAAAAAAAAAAAAmgCgAAAAAACgAAAACgvLAPAAAAAAAAAKAAAAkKkAAKAAAAAAAMsAAADAAJAACQAKAAAAAACQAJAAAAAAAACQAArQAAkAAA0A4JDa2pAACQ8AAAAAAAAAAAAAAACgAJAKAAAAAAAAAACQAA6akAAAAAAAAAAAAAngCgAAAAAAkAAAAACwywAACemgAAAADAAAAJAA0AAAmgDAAAAAAAAAAJAAvArLDwCgkAAAAAAAsAAAAAAAAAAAAAAAAAAAoAoAAAAAAAAAAAAMkKkMrJoAoPAAAAAAAKCQCcAACgAAoAkAoAAAAAAAAAAAmgAAqQAJAAAACpDgAACQwLAADwAAAAAAAAkAAJAAAAAAAACQAMsAAAAAAAAAAAAAAKCQAAAAAAAAAAAADAoAoACgmgCpAPAAAAAAAACgwAoAAACgAAAAAAAAAAAKAKngAMCwAAAAAAkKnACQANCpAAAJAAAAAAAAAAAAAACQAMkJAPANqQAAAAAAAAAAAAAAAADgAAAAAAAAAACsALCamtDaAAAAAPAAAAAACQAJoAkMCQAAAAoAAAAKkACQCQALywAAAACwAOAAAAAACwAAAJDgAAkAkACQkAAJAAwAkAAACQCwAAAAAAAAAAAAAAAAAAAAAAAAAAAAkJoAsAygwKCgAAAAoA8AAAAACgAAAAALDgAAAAAAAAAACgoAAA68AAAMsAAAkAAMsACgwACcAKCQAAAAAAAAAJAAAAkAAKngvAvAAAAAAAAAAAAAAAAACgkAAAAAAJqawOALDpoJCpAACwAAAPAAAADpAA2gAAAAAAkMAAAAAAAAAJAAAKkKmgCwALAAoJCwALyQkKAAsNAACcCcAAAAAAkAAAqQDQCQAAAAAAAAAAAAAAAAAAAAAACgAAAACsAAqwsMAAAKAAAAAAAAAA8JAAkAAKAAAAAAAAoJoAAAAAAAAAAAAJ6cAPAADACQwKwK2grKCQ2gwAAMCgAAALAAAAANCQAMoAAAAAAAAAAAAAAAAAAAAAAAAAAACgCwALDpwAywmgkAAAAAAAAAAPAAAKAArQAAAAAAAAAAAAAAAAAAAAAAAACgsAAAsACgmgC8ANCQygAJALCQkAkACQAJAPAKCgAJAAAAAAAAAAAAAAAAAACgAAAAAAAJAAAAsACaCwoAoAoAAAAKAAoAAAoAAAAAAAAAAAAAAAAAAAAAAKkAAAAAAAsLwKAAAAAAAJAAmgoKkAkAqcCgAMCgAAwOCwC8nAAAAAAAAAAAAAAAAAAAAAAAAAAAAAoAoJCgAJoAAAAAAAAAAAAAAAAAoPAAoAAJAAAAAAAAAAAAAAAAAAAAAAAAqQDAqckACeAAAA4KAAycCsCsAAAMCpDAAAqQnA8KAAoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKAJCgCpCgmgAAAAAAAJAAAAAPDwAMsACgAAAACsAAAAAAAAAAAAAAAAALCwCgoAoAkJCgkJDpoACQAAkKkLAAAAAJAKCwDwAADaAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKAAAAAAAAAAAAAAAAoAAAAPAACaAKAJAAAAAAkAAAAAAAAAAAAAAKDAAA8AAAAACgCQoOAAALAKkAoAAAAAqQAAAAwOkACwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8ArADJAAAAAAAAoAAAAAAAAAAJAAkAkAraAAAAAAsMvLypAAAAAAAJANoAAJAAmgAJCwAADAsACgAKAAALwAAAAAAAAAAAAAAAAAAAAAAAAACgAAAAAAAAAAAAAAAAAPAJCpCgAAoAAAAAAAAAAAAAAAAKAAoACpAAALAACwywCgmssAvAAACgCgAAAAAAAAAAAJAACwwAAADQAAAAmsAAAAAAAAAAAAAAAAAAAAoACpAAAAoAAAAAAAAAAAAAAK2gAAAA4JAAAAAAAAoAAACQ2prADAAAAAAAkAAJALALCaCQAAAACQAAAACQAAAAAAAKAOCpAKkAkNoAAAAAAAvAAAAAAAAAAAAAAAAAAAwAAAAAAAAAAAAAAAAAAAoAAPAAAAqQkMAKAJCgAAAAAKkKoMAJqQCaAAqaAACgAArQysngAAALAKCQCQoAAAAAAAkJCwkADwCgCgAAAAAKCQAAAAAAAAAAAAAAAAAACwkAAAAAoAAAAAAAAAAAAAAAAArAAAAAoKAAAKCQCQAACQypyaCaAKAAAAAACgkAqemgsJoAAAAAAMAA4AAAAAAAsMoMrArLAAAAAAAAAAAACgCgkAAAAAAAAAAAAAAJDAoAAAoAAAAAAAAAAAAAAAAAAPCaAAAAAADQAAAAoAAACpqQoAAAwAAACQDAAAAAAAAAAAAJoJoACamgAAAAAAAACQAJC8kA6QAAAAAAAAAAAAAAoMAAAAAAAAAAAAAKC8AAAAAAAAAAAAAAoAAAAAAAAPAAAAAAAJCgAAAAAKkAAMCtDwCwsAAACssKkKAAALDwALwAwAyaAADJCwALAAAACgmgAKCwCgAAAAAAAAAAAAAAAKAAAAAAAAAAAAAAAJ4AAAAAAAAAAAAAAAAAAAAAAA8AAAAAAKAAAAAAAAAKmpraAAAAAAAAAAAAAAAAAAAAAACwCwoAAAsKAACgAAAAAAAAsJwMvAAAAAAAAAAAAAAAAACgAAAAAAAAAAAAAKAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAoLAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBQAAAAAAAImtBf4="",
    ""Notes"": ""Andrew received his BTS commercial in 1974 and a Ph.D. in international marketing from the University of Dallas in 1981.  He is fluent in French and Italian and reads German.  He joined the company as a sales representative, was promoted to sales manager in January 1992 and to vice president of sales in March 1993.  Andrew is a member of the Sales Management Roundtable, the Seattle Chamber of Commerce, and the Pacific Rim Importers Association."",
    ""ReportsTo"": 0,
    ""PhotoPath"": ""http://accweb/emmployees/fuller.bmp""
  }
]";

            var actual = ReadParquetFileAsJSON(filePath, 2);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Issue144_NoSetup()
        {
            string filePath = @$"emp.parquet";
            var dbFilePath = Path.GetFullPath(@"..\..\..\..\..\assets\db\Northwind.mdf");
            dbFilePath.Print();

            string connectionString = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={dbFilePath};Integrated Security=True;Connect Timeout=30";
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            SqlCommand command = new SqlCommand("SELECT * FROM Employees", conn);
            using (var r = command.ExecuteReader(CommandBehavior.CloseConnection))
            {
                using (var parser = new ChoParquetWriter(filePath)
                    .Configure(c => c.CompressionMethod = Parquet.CompressionMethod.Gzip)
                    .Configure(c => c.RowGroupSize = 1000)
                   .TreatDateTimeAsString()
                    .NotifyAfter(1000)
                    .OnRowsWritten((o, e) => $"Rows: {e.RowsWritten} <--- {DateTime.Now}".Print())
                    )
                {
                    if (r.HasRows)
                    {
                        parser.Write(r);
                    }
                }
            }

            string expected = @"[
  {
    ""EmployeeID"": 1,
    ""LastName"": ""Davolio"",
    ""FirstName"": ""Nancy"",
    ""Title"": ""Sales Representative"",
    ""TitleOfCourtesy"": ""Ms."",
    ""BirthDate"": ""12/8/1948"",
    ""HireDate"": ""5/1/1992"",
    ""Address"": ""507 - 20th Ave. E.\r\nApt. 2A"",
    ""City"": ""Seattle"",
    ""Region"": ""WA"",
    ""PostalCode"": ""98122"",
    ""Country"": ""USA"",
    ""HomePhone"": ""(206) 555-9857"",
    ""Extension"": ""5467"",
    ""Photo"": ""FRwvAAIAAAANAA4AFAAhAP////9CaXRtYXAgSW1hZ2UAUGFpbnQuUGljdHVyZQABBQAAAgAAAAcAAABQQnJ1c2gAAAAAAAAAAAAgVAAAQk0gVAAAAAAAAHYAAAAoAAAAwAAAAN8AAAABAAQAAAAAAKBTAADODgAA2A4AAAAAAAAAAAAAAAAAAAAAgAAAgAAAAICAAIAAAACAAIAAgIAAAMDAwACAgIAAAAD/AAD/AAAA//8A/wAAAP8A/wD//wAA////AP8MsMkACwkJAAAKAJAJAAAAAAkJoJqQCwkACpCgAAAAD//v///////////LnPz++v/////t//7e/97+/P//2toA2QAAkAkAkAAAAAAJCgAJC8AACQCQAAAACgCsoODg4PDpypAAqcsMAACQkOAAAAkJCwAA0AkAkAAACQAAmgAP///////+//////yt69vf39//////7+3//v////78rwyaCg0AAJoAAAAAAAAACQkKAAsAmpAACQAAkAwJAJAJAPqQraAAkLALAAAAAJAACQAAAJAJqbAJAJoAAAAAwAC//////v///////8+fvL77/+v////+/f/+/f/P/v/5vAsAkJAAkAAJAAAAAAAAkAAAkJAJoMCwAACwAAmgDg4ODvytoMkAAMsAkAAAmpoJoACwCQCQkA0AqQkACQkKAAD////+//7///////Dw/9/trf3//////v797+/8/e/sDwyawAoAoJAAAACQAAAACQqQCgAAkJAACgAADKAMCQCQkPkA2gCakADaANAAAAnAkAAAkLAAqamwkAAAAKwAAAv/7///////////7anfD/n////w//////3+/97/79/5qaAJCQkAkACQAAAAAAAJCpAAkJqQAKkNCQAAqQALDgysoPrpqcoACpmpCwAAAJCwAJCQAAkAkNAACakAAJAAAJ/////////v///+2t68/e/9ra3//////+/vy+/e2+/K0NDgCgAAAJAAAAAAAACQCQCQAAAACQAKAACQAAAAAJqQDfCQypAJCQ6QkAsACQqQCQAAkLAJoJqQkJAACQsACf///////v/////9CenPv78P///v/e/////9/9/r/v2pCwqQkAqQCQAAAAAAAAAAAAAAkJAAkKkJAAoAAKywCwwOCvDwvAkAAAmtqQkAAAkPAACQCQkAmQmpoJAAAAAAAP/+///v/+/+///qnp+/3t//3//f///P///v7+38/e/AANAACQAAAAAAAAAAAAAAkAkACgCQCQAAsAkAAJAAwAoJwP8PC8oLCgAJDwqakACQsJAJAAkAALDQkKkAAJoAC//////////////Q+enw++8Pr8v+/9//////3/r/r577wKnAoAkJAAkAAAAAAJAAAAAAkAAAAAsAAAAKAAAAsMkKAPCw8AnAAJAAsAnAALAK0ACwCZqQmpCwCZAAAAnAD////+//7/7///ypDw/P/f3/3//f2+//3///78/P3+/ACQCpAAAAoJAAAAAAAAAACwAJAJAAAJAJCayQAAqQAKDAnPAPANoAkAAJCfC5CcAJmgkAmgAAAJCQmgCQAJoJ//////////3//umc8P2+nvr8vp777//ev9v////w/t8KkK0ACQCQkAAAAAAAAAAAkAkAAAAAAACaAJAAkAANoJAKCvra2wCayQAAAAkACpAJrQAJAJCQm8sJyZAAkACa//////7//v7+/9rLD9rf+9+f/f39/f///+///+/PD77Q6QCcAAAAAAAAAAAAAAAAAAAACQAAAAAAmgAAAJCwwA6cAP2pDprJoKkKkKmpCa0Am5AK0KkACQkLCpAAAJC9/////////////AmtCw8L7a/p76++v9r9//2//////P/pAJoLAAAAkKkAAAAAAAAAkAkAAAAAAAkACbAAAAAAsAAK0PreuekADQAJAJDQsJmpoPCQCQCQkLCQmQAAAACe///////v//z/wPDa/Pn9n9+f+f39/v//79/t///vz/7engkAAJAAAJAAAAAAAAAAAAAJAJAAAAAJnACQAADwAAkAAP263K2pAKkAAACwCcqcCZoAsAkKnpALAKAAkAn7/////v//7+/8CwmtCa3r7+nvD8vvn73//////////+/wAJAAkAAJAAAAAAAAAAAJAJAAAACQCQCaCpAAAAkAAArKCvrtq/mtqQDanJCempmpyamZCQCQCQmQmQAAAAC8//////7//f/LwPDantv9+f+d+/+f7f/r3/7b///+///sCaywAAAACQkAAAAAAAAAAACQCQAAAJAAkAAAkACgAAAA0P2+2s+8vLoJoKAJCaybqZ4KmpAJCwC8qQAAAACb/////////v8AkJ6cue2vzw/rzw/p+8/f+///z///////4JAJAAAAoAAAAAAAAAAAAAAAAAAAAACaCQkAAJCQAAAAC//p/byem8kAkJkAsJsNmgmZwACQkJoJCQAAkAkP///+///v//yQqQCw2tvfv/29uf2/z/v/7f2/+//////+kAsMAJCQkAkAAAAAAAAAAJAAAAkAAArQkKAAmgAAAJCaCfn/6+vr2tutC8oJwLywvJsAqQkLDwmQsAAAAAC5///////978oMnK0Nrb6+2tvtr8v8vbz9/+/8/f/////tAACakAAAAJAAAAAAAAAAAAAAAAAAAJAAAJCQAADwkAAA2v6tv9vfD60KkJCwsAkLC9C5kAAAkAAJAAAAkAkP/////v/v/9CaAJCw8P35/+2/37y///+v+//7+///////4JAAAAAAkAAAAAAAAAAAAAAAAAAAAAkAkAAAkJAAAAqamvna2vy/vL+fCw8JC5qckLkOkJCQuQmwkAAAAACQv/7/////7+AJCa0Ly9r+n9vL6f/a2v35/f3/z5//////wAAJCQkACwCQAAAAAAAACQAAAJAACaAJqQAAAAsJ4JAMkP6trfvw29rwvAkPkAALCw25CwAJAAAJCwAAAAAJ///////v/wkMCsmtva/b/r7/3/D//9v/7/v//+/////+sAAACgAAAAAAAAAAAAAAAAAAAAAAsJAAAAkAmpAAAACQqfDfrw/p6+vb6bCwvAkL0JqakAmpAJCwAAAACQCb////7//9/AAKkJD5772+2/35/p/72+//2/z/n5/////9AAAACQAJCQAAAAAAAAAAAAAAAAAJwAAKkJoAAACQCaAKnP6g0P2/vJytvwkNCwCZCwm56QCQsAkJCQAAAAAAD+/////v7wAJDa8Pnt7b/Pv+n/rf79/L/////vn////+kAAJAAAAAAkAAAAAAAAAAAAAAAkLAAAJAAAAsPCgAA8NC/3/r6/tv7+envC6mtoKnJrQmgkACQAAAAAAAAAJvf///+///gkAqQnw+/v9v97//9/737//6f29+f7/////wJAAAJAAAAAAAAAAAAAAAAAAAADwAACQmpCQCQkJrQAAAPvv3/2/6empub0J25DQmwkLkJAJAAsJCQAAAJCQD//v////7QANDLy9re3v//+f2v+f/t+f3+/v//n5///5sAAAAACQCQAAAAAAAAAAAAAAAAsJAAkACQAAnwrAAACgvPzwvtr/n5rfD/qasAsJ6amp6QCQkAkAAAAAAAAA++/////t8AkAmw8P37+/y9/+/97+37/++/n73w/+///+AAAAAAAAAAAAAAAAAAAAAAAACfAAAAAJvKkAsAkJoK0NCfqeya+e8K260L35yfCwkJCZmpAKCQAACQAAAAAJCf///+/+/ACw4Pn/r9/f//r9v/vfvfz5/P/fv/vb///9mgAAsAAAAJAAAAAAAAAAAAALywsAAAmgCQAJ6eAACQCgoP/L/tr5v9rbvwsLsJnanwvLDQAJAJCQAAAAAACQsN//7///8AAJnw8P36/r3/3//P///r//+968+fy9rf/60AAAAAAAAAAAAAAAAAAAAAAAkNAACQCQsAnwkJCwDAkNCf6/y62+8L2tqf28n6C5AJAJmpCQAKAAAAkAAAAACa/t///vwJCQ6fn769//+////73p/98PD/vfvvv/2///CQAAAAAAAAAAAAAAAAAAAAC5qaAAkAkJAPoLygAAsLDgoPDwvt/vm8ra36mpqQmembC5qQAAkAkAAAAAAAAAmt///e/9oADwva/P3/+f/P/p////+ev9/63r/by8vp7wmgAAnAAAAAAAAAAAAAAAAAkMCQCQCpALD5CQAJANAAwJDfrP7a+fnr25qb29vbC5qQmQ0LCQCQAJAAAAAAAJCa3v7//vAJAJ8Pn7/L/v//n/+fz57/2vD9v9D9vb2fn/AJAAoAAAAAAAAAAAAAAAAAsJDpAJCQucmgDgkMAKAAsMsP2w8PD76cvPntqamtvLmtoLCQmpAAkAAAkAAAAAAJv9////wAmsva/ev/372//97/v/n6/f8Pnr++/vra2+kAAJAAAAAAAAAAAAAAAAAAmgkAAAkLCr0JCQCwqQDwyw6fDtr56em7y56729vbCa0JmQkAkACQAAAAAAAAAACaD//v7wAADby9+/3w+97//7/97f79+trf6fy9vZ+9C9CwAAywAAAAAAAAAAAAAAAJyQqQCpC8nQCgALAAnAsJrAkPC5ra2/C8u8udrampC9qbCpoJoAkACQAACQAAAAkJ2+///eCQ2w+/vt6////9rf37+/vfrb/628va2++QvQCQAJAAAAAAAAAAAAAAAACaAAkAmZqQsKCQuckPALwKwLDvsMCa2p/b/Lnrub29uQuQmckAmQCQAAAAAJAAAAAArf7f7wAAqfD8/b39/f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oA8ACakLCgsAAAAAAAAAAAAAAAAAAAAAAAAJCQkLCamtC9rfn/////Cp+p8Py9rb6fvp6amfCQubwLCakAkJAAAJCQAAAAAAAACQsK2/v//////9/+3/2968va0J4JwAkACaAAAMkMAAAAAAAAAAAAAAAAAAAAAJoJAACgCQmp8LvL2v/9////CfCenpva+/nw+bm9rwmpAAmQAJAJAAAJCQAAAAAAAAAAAACdvf/////////5+frb3fD5rakOmpDLAMmtqQAJCQAAAAAAAAAAkAAAAAAJCQAACQkJC8kPDw2fqfvb///7Cenpqb+vn57b+tramZqckLmgmwCaAACQAAkAAAAAAAAAkJn7///////////f/v3/6w/a2QnpAMqQCQAAAAqaAAAAAAAAAAAAAAAAAAkAAACQAKAAsAranpq+n9++/f/fAL8L2sn5+/u8vbm5y8kLCQAJAJCQCQAAkJAAAAAAAAAAAOv/////////////n5+tvfmtrPCa2pAJ4LCQkJwAkKAAAAAAAAAAAAAAAAAKkJAJCQkNCQmtsL3pr7ntv///AAvan7y+28+/mvywuakJsJCQkLAJAACQAAkAAAAAAAAACZ+f//////////////3/y96fmw8JCcvAkMvKDgkArQkAAAAAAAAJAAAAAACQAAAAAAALALyw+Qvb2t6w6fy+AAmtravbv7n5/bn5C9C8CQ8ACQkKkAAAkLAAAAAAAAAAu///////////3//fz569va3w/NmtraCbypAJCQoJAAAAAAAAAAAAAAAAAAAAAJAJCQsACQkPAPmt6akPnr/rAJrb2tn63w8Pqbyw+QuQmwkJmpAJAACQAAAAAAAAAAAL3//////////////7+/3a3tsP2wrQmsmsCQy8kACcALAAAAAAAAAAAAAACQkAkAAAoACQkAvLC7DwsPDw/vwPAACgvb6f+/n5+tvZCwkLAJCwCQqQCQALCQAAAAAAAACb///////////////f39r/n7zwvL2a0LyQvKkAoPAKkAAAAAAAAAAAAAAAAAAACQCQkLAAoJC5ycsPDwmrD9v+kLyQC9vp/a8Lnwu+mfDQkLAJCQkJAACQAJAAAAAAAAn/v////////////9///L2f6fvfD8vprQvLCdDw0AkAAJAAAAAAAAAJAAAJAAAAAAAJAAkJwLDasLzwsLycsK2rDAsKnp6fv7296fnJvpsLCQkKkLCakAAAkAAAAAAAAJ+9////////////////2//vnw+p+byQ2pyw2goJoLypygCQAAAAAAAAAAAACQAJAAmgqQAAkAmpy8ucvAsLD9rfALwNvL+9r56wubC/CZAJCQCZAJCQAAkJAAAAAAAACb///////////////f+ev8n57fCfy8mtqcsNqemeDQCQCcoAAAAAAAAAAACQAAAAAJAAkAALALy8sPnqkLDp6amvAACwCw2v3/n9vp+QvKmanJoAkAkAkAAACQAAAAAAn//////////////////9+f8Pmp3gmtrbyfDa0JygmtrLAAkAAAAAAAAAAAAAAACQAACQAAkACQkPCQ6ZDw+bDw8PAJwLyan7+/++nwv5uZoNCwkJqQCwCQCQAAAAAAAAm9v///////////////D/np+e3auekLkOmgsJranJ6QkMmgAAAAAAAAAAAAAAAAAACwCpAACQALCw6bysqfDg8LC7AAqQoJ6cvfD5+b0PDa2QsJAAkJAJAAAAkAAAAAAL///////////////////56/D5q9Dw8MD5y9Da2p6anLy5rQ2pAAAAAAAAAAkAAAAAkAkACQCgmg0L0KnbngufC8kPCgnLDanr+vvb+vCwmpsLCakJCQCQAAAJAAAAAAC9v////////////////9va2ckJDQ8AnpuQrQ+tqcmpypAAwLAAAAAAAAAAAAAAAJAAAAAAAAAJAJC8Cp2g6fngCwvvyQC8C8ufn5+8vb25+QkJCckAALAJAAkAAAAAAAC///////////////////+dmpmgkJCfCQytmvCenpraCenpqQCekAAAAAAAAAAJAAAJAJAJAAkACQvLDQqfmtrQ8PCbCskLDaDa+fD7y8vLD56a2pCakAkAkAAJAAAAAAnb3//////////f/////9vpyQ2dCaAAnpsK0J6Z6ekNvJqcnp4AoAAAAAAAAAAAAAAAAAAAAAAJCgCcsJ2g+/ALCw8NALAPCp+p+vv5+/n58LkJCQvJCQCQAAAAAAAAAJ+//////////9v/////2fyfm9qQvJ0JAAycDwnp8J6aC8vKkAkJDQAAAAAACQAAAAkAkAAACQAACQmgCaC9vKngnpAKCfywCZ6cv5/a29sLC56QsJCQAAkAAAAAAAAAALv/////////n7/////b3/n5DQkNkJAACQmpsLyw8PDw0JCdrLy8oAAAAAAAAAAAAAAAAJAJAACakA6Q2g+enp8J6Q8NAAsPAKm/n/r/va+fnLm9CakJqQCQkAAJAAAACd/f//////////37//29uZyekJCQCQkJAACQycvfD56QvK2gCQAJCcAAAAAAAAAAAAAJAAAAAJAAAJALCZnp6eDamgALAAnp6Q6frb+fq9vpqcsLkJCwkJAAAAAAAAAAAL+/////////+fr9/9vQ0AmZD5y7yeCQkJAJCpCtvL2+kNDa0PDwoLAAAAAAAAAAAAkAAAAAAACQkKkJoOkPC5oPDJANoACemsmp+/373629+bD5ywkJAACQAAAAAAAACb3///////////2/+QAJqfvv/++cvgkAAAAACQ+a28vJ6ampCwkJ0AAAAAAAAAAACQAAkAAAkAAAAJC8kJra0OnAkAAKmgCwvbC8veu/+9v6mwmasJDakJAAAAAAAAAAD///////////2trQkAmb8P////mv8J4PAAAAAAkPmtqanJ6csMvAoPAAAAAAAAAAAAAAAJAAAJAJCQ8LDa0PDpCw4AAPAACa2t6b+//fnr258PC9nakJAAkJAAAAAAAAm9v//////////72pAAAP37/b/ryengkAAAkAAAra/b3NCwmpypCpyQAAAAAAAAAAAAkAAACQAAAA2gkNoJqQCaDJAAAJ+pDAmpvA/5+v+frfub0LqZqQmQAAAAAAAAAAC//f////////+f+crQCQsNrZvQsJCQCQCQAACQkJraq8vLyena0KnpoAAAAAAAAAAAAJAAAAkAkLCQqQkJ2p4NAAAAAOkAqayeC/n7/9v/263pC528kLCpAAAAAAAAAAvb//////////3/npmQkJCZkAAJCQAJAAkAAAAAC8mt2a0AmpCpqckA0AAAAAAAAAAAAAAAAAAACgAPCaywoMkAoACQCbCpAAmgv57/z7/wvZ+5vQsLkJCQCQAAAAAAAAn/+/////////vw////mQkPCb2ZyQCQCZAAmpDa0AvbCtC9vJ6cngoPCgAAAAAAAACQAAAJAAAAkJCQmtsJDbCwyQAMnvkKmtrQAPufvfn/+vm8sL29qekJAAAAAAAAAJuf3/////////3/vb///9+ZvJCakJ0LwMkLwNoJC9C8vQ8ACpCaCZyQkAAAAAAAAAAAAAAAAACwAAoLywAAsMAJAACw8A+gCamskJ7/+/6fvby52wuakJALAAAAAAAAAA/7//////////+f+t//////29vJ/anJua2tsA0OAK0Lywn5yenp4KmsDwAAAAAAAAAAAAkAAJAJCQkJrbyQyanKCpDLy/ALCgCpoAufrfv72/vamtmtuakJCQAAAAAAAAm/+///////////n/v///////3/C9+94NCQDaCwn5rcsPAAsJCQmcDQsAAAAAAAAAAAkAAAAAAAAACamgkKmtCwkMqQoJCwALDQALD//728v5y5+Zrb0JDQAAAAAAAAAJ/9///////9v9v5/b3///3/3/+wn8vLCZ6emprQ8A27DQnpDQ8PDgsLDAAAAAAAAAAAAAAACQCQCanpyQmpAAvAyg8OkPoAqQsLwA2/+e//8PvamtubD5CakJAAAAAAAJv//f/////////ev///////+//J6b28ngkA0NCanbrA+ememgkAkJAMkLAAAAAJAAAAAAAAAAAAAJCavLDADwmprfrwCw2pCgCwsKAL/5+fv9qfma2tsJqQAAAAAAAAAAn7+////////9v58Pn9/////f//np8LCQqbCw8NoNm9Cp6QnLDwyw8JrQAAAAAAAACQAAAAAAkAkAvJCQmpkADQr/37wPqQqQsNrQmt6//r37/wvJubvakJCQAAAAAAAJ/f////////37/fnb////////29CfDQnpwMkNCw2w8L/QkPCQ0JqcCeALwAAACaAAAAAAAAAAAAAJCw8J6QoJoL3//ssLAKAKALC8oJvem9+/+b27Db0JCakAAAAAAAAJv73//f//////362/y///////v8vpCeAAC5rQsNsPCckLDwC8sKnAsAnwAJAAkAAAAAAACQCQAJC8vakPCayQy8//7byssAsJqa8PDw+//6np/+sNupv56QCQkAAAAAAL3//////////b+9vJvb/f//3//b2ekAkJAACw0Ly5y7y8kL0JDJCwDaAAnACQoAAAAAAAAAAAkAAJCpywvAvLAPD9+ssLywCgAJCw8PD5/tv/v5+w28sJkJoAAAAAAAAAvb/7////////35+8m9////v9+/+Z6QAAkMkLDQnLnJvby8ram8DQsJ6QqQngnAAAkAAAAAAAAJCwmQvJy8C8sL8On54OsAoKmgoPCa8Pvbyw+fvbuZ/7CwmQkAAAAAAJv//f//////////nbnLn739Cbn9sPkAAAAJAAmg+Q+enpnLkJwJsAkMkOkLAJALAAAAAAAAAAAAAJ6ekLAJrangC8oKCZALCQAAmpoAn62v+f//2tD/uQ0JAAAAAAAAAL29v/////////29vpy9///5n5/bD9DpmpCakNvJkPnpy569DLCwwPCpoJAAywnwAAAAAAAACQkJCempCwy8vLywsLDwmvsACgoKCayeCf/9vtr7/7+an7mtCwkJAAAAAJv/////////////35sJCf//////2/vayQ8NoACfDw+fvenQucnLCQyQCcrakOAJAAAAAAAAAAAAsJqevL6a2ssKDKAKAA+pAJCQoJoJoJr627/f+9v5+ekJAJAAAAAAAJ29vf//////////vf35+f///////a25rJCw0JqQvby8mtqfy5qcsLAJ4JAJCQng8AAAAAAAAACQC8kJy8vPCw6csJqQsPAKCgoKkKCwCQ+f/9+/vf6fvp+akACQAAAAAAv//////////9//378PD5//////3//NmenLAJyemtvb/b26nA2wnAngkKkMoJoJAJAAAAAJAAkAkJrakA/6zwsKAKAKAJCwCQCQCgkK2vD7y/r//729vbkJCakAAAAACZ+fv//////////f+/35+f///////9ubDpywCwnp/a2tC8vJ29sMsLCQsNCpCenQ+eAAAAAACQAJqa2prL2t8LDwsLy8sPoKCgoKCQqQsJrb/9//n/v6+/CwkACQkAAAAJv9///////////7/fv/2//9//////7ema0MkNqdqfvb/fn+vLybycqcAA0AkJCpAAAAAAAAAACw0JqcC9raD8oA6enp6g+QkAkACgCgCpAMv7+f8P/5/b/byZAAAAAAAK2////////////9//37/b3//////f294JqQCa2p8NrQsPCZ2w8MsJALywqcoAsPCQ8AAAAAAJAAmtmr2vytoL27Dw+v3/CgoKCpoJoJqeCwvP//v7/fu/mwmgkJAAAAAJn9v////////////9/9//////////vanawLC8kPD72/35/+npD5Da2gkJAKnJDQvLAAAAAAkACQsL7QvwCQDwoMsPDQ6rALCQCQAKCaAAkA//v//9+/ntvLkJAAkAAAAJ+//////////////b//+/////////3p2gmcnLv/np2tq8sJD5+a28kJysAJAACa0ACQAAAAAJoLCfmvwNDr2prbrL6vvcsAoKCgoJCgsLDpAP/5/7/7+7+a0JCQAAAAAJv////////////f//+9//////////+9rZ4AsJyQ+evb/b3/+emtkJraCZoAkLAAC9oAAACQAAkA+trQsKkMDwmg2wkACrywkAkAmgCQCwsOsJr///v9/fvbmpoJCQAAAAmf/////////9////3//f///////9/L2gm5y8v56drfD9qdr57a6ekNoMkAAAkJAJyQAAAACQCQn62sAJCwsPCaAOC8kJoAoKCgoAoKmgCwDK37/9+fv5rbyZCQAAAAAJv///////////+/29/5//////////+9rbwMsLCenr2w+a362em9npra0LAAAAAACamgAAAAAAsNsJqakMrA8AvJrQsKCukKCwkLCakJoJra2gm9+/v78L/7kKkAkJAAAJy////////////f/////////////9/LyQCbDQ3p+dvP2t+p2vnp6a0JqQwAAAkAkJwAAAAAkJCavLwACgkLALAKkLAJqZ4JAAoAoAoKCwsKkADr///an5mw+ZCQAAAAAJvb/////////////fv9///////////5Dw8MmtueD+2wvanevQ+emtq8npqQAAAAAKngAAAAAAD5ywvADQAAmsmtrA8OAKmgqQCwmpAJoKCw6QkNv9v/m/Dbnp2pCQAAAAm9//////////////3//b3///////nv8JCw0Ky9ub79r9qZ69rb3p2p6cAAAAAJCQkJAAAACQsKvAAJoACsoLCampqQsNoJCgoAoA6wy5ypCgAL/7/56Qu56ZqQAAkAAJCfv///////////////v//////////5Dw0JqdvJ78kL0L3pnK2suenLCakAAACQAAvAAACQkACdCwCaAJAJCQDp7awKwKkKAAkKCakAsAqcoJALy9v/vbkNuaCQCQAAkAvb//////////3//9//3////////96c8JoKnay60Jv+y9qay/m9vLy8npwAAAAACpyaAAAACpsKAMsMkAyaCgqamgmpC9oAqaCpoAoLCamgmgkJ//+/mtC5D5kJAJAACQC5////////////D6/b69/f///////7DQDZytvNrawJm8ntucvLy8sJ6aAAAAAAkJoMAAAAkADJqQAACpoACQkNrJ4KAK0LAAAACwng6tqaCaAAC9rf+bkJua2wkACQAJkPm///////+9vp/by936+/3////9vNsLCgsAmp2tvKwKkA+py8va36nJ6QAACQAMmwAAAAAJCwwLwLDAmgoKCwmgCQsPCgCwsLAOoLCaDpCtramv+/vp6anJsJAJAAAACbn//////9/f+fC8kKkNntv//////7wA0JDJ4MqQCQkJC5DanbyfCcupAAAAAAkLDJAAAACakAsACQCw4JAAkKnLCgAAsAoAAACpCaypqQqakAy9v9+ZkJCbyakAAAAJnw2/3/////+/D8vQvJD5rb//////y8kJCsCwkJnAvLysDAy568usnrycoAAJCtCpywAAAJAADLCcsA8AmgqaCtCgvJCvCwkLCgsKDgsOmpnprLAL36/6mwsJuQmwkJAACZqb/////5/f+b2tCakAkPn//////awKCQkACeCtAAkLCQsOnLDbrQnpyQAACQnLALkAAAkJqQygnpraCQAJCamtAKkLwAoAqQCQqQ6QoOsPC8vJrb29rQnLC8CQAACQmp28v9v/+/+8vACQAJCa2w+f////35kA0LycsJAACQAAAACQsJ4NC8sLAAAAAKCw8MAAAAAA2pvaAACgoKCgCpAKkAoMsKCaAKCgmgmtCwCgsLCwv/v/mbCQnbkAkAAAkJqb2//b/fDwkJ4KnAsNCdv7////+soJCgCwDa0LAAAAAAkACemw8Ly8sAAACckNALAAAJoJqcoJy+kJAJCQsArQ4LybCwCgmpAJoL4KvJqfDw8NC9vbywmwmpoJCQCQm5m/n72p2wkJ4AANAJyQrbD9///9rZCeCQkA8AAMAMAJAAAJDwkOkNDwwAAAmgDakJAAAACanLDwsAAKCgoKAKkKmgCvAJqaAKCgAAC8Cq0KmqCw8L+/ufAJCQmwAAAACa0J69vaAJD/8AwAAKAJAJ+/////+gAJAMAJC8AAAAAAAAAAAJ4J6w8JoAAAAJqQAAAAAJAJ68kPCpoACQAAqQ6wAJ6Q+gAJoAkKmgALCaC8vZvLD56f+bkLCfAPCQkAkJuZmevZAAm//gAArJ4AkAn5////39oAmpAADvCgAAAAAAAAmgnwnLDw0AAACQwPAPAAAAC8kL8K0AALCgsJCpAOmgoPALCgCwoAAJoA4MsLCqywvL+/nr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CpCQoMupAACgmgsLCaCQAKAKCQoACQvL/56b+fv/kJoJn5qanQkJDwkLn5vPuf+f29va+fvrn+2/vf/rz+vL372tvLrKkAAAAAAAAAAAAAAKnp6ayakAsAAKmgqQAKkAAKCgALyaCpAAoJoAoACgqQCQCgALAKCb2//9v729rQkACw25+5qQmb29Ca257wvbra352t+f75/P3629vb376erby8mcAAAAAAAAAAAAAAkNCaAAsACgALCgAAkKmgCpoAAJoPCgAAoKkKALAKAAAKCgsAsACwme//vav9r7makAmfsNsAkLDwmpu9ufm9vL358L//nt+e+/r9/e2v6e2928vJ4KAAAAAAAAAAAAAAAKC8nLAKAACgCQqaCgAAoAAJoKAJsAsACQqQsAqQsKAAAAAAAKAAoJqf+/y/8P+tqZoJ26n5AAmfna0J6enL29qen/Dw/7/5/f2/r7/b357avLyanAAAAAAAAAAAAAAAkJ8LoAoJqaCaCgAJALCgkKCgAAAOmgAKAACgCpCgAJCgkKCgoJoJAA2/37/b35/b2tngvdufmQCwuZufmb+by5+b2tvfn8v/vr/P39r8vvn96entqQkAAAAAAAAAAAAACgn8CwCgAAAAkKCgoACaAJAKCwoL8LCgCgAAsKCaCgAAoAkAkAAKC5oL//+/++n/rauZC6nrCwkJ2vCw+8nam8vPvby7z73p/f29vL+f3b6en56awKAAAAAAAAAAAAAAkJ6QvKkAoKCgoAkAAKAACgqQAAAOAACQCQqaAAmgkAoAAKAKCgoAkAyf+9vL/9v729Cem9+50JC5qZnLkJup/L27y8+8+9+/y/r/7/z/6+n58OmtDQAAAAAAAAAAAJCayevKAAAKkAkAAAoAsAAKCQAAsKmrmgoKCgAACwoKCgCwoAAAAAkAoLAA////v/8PvL256bn5qQnL2em9qenQufntvb/fva/Pvf+fn58Pn976357aCpAAAAAAAAAAAAAJCpAMCwsAAAoACwAAAAoJoKmgAAAMsJAAAAoLAACQAJAACaCwCwCgkAmpC/vb2//9+avLnp6fnpqQupCa2ZqfDw+by9r579v969z+8P/56fnPrantkMAAAAAAAAAAAAAAkLwLAACgCgAKAACgCpAAAAALAKALygoJCgAACgsKmgoAAAAAAAoAoJrAvf/+va2/r9+fD5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CgAKAKAAsAsACQCgAKAKCpqQoAkACQAAsLCgoAkJALCgsAAJoKAMoLCgCaAAAAAAAJCpAAAAn77/3/////vb7b2emvmcvp/Lufn9vdr9vLz96enp4ACgoAALAAsKAAAAAJoAAAmgCgAAAKAAoJoAAOCgCwoKCgqQAACQAKCgoACQCgoACQqQAAAAoAqaAKAKAKAACpCgAPvf//r////tvr+t+f652wvfzw8L+v2v29vgvL2sngCQCwoACgAAmgCgsAAACgAAAACgoAoAkAAKANvJoACQAJAKmgoKmgAAkKCgAJAKCgkKmtqaAAAACQAACgCgqQCQAAnr////2////fz77+n+vP+vv///z5r9r+2/y8oJoAsKAAkKkAoAAAkAAACgqQoAoKAJAAkAoKAJAKAKwLCgoKCtoJAAAJqaCpAAsKCpAACgAAAACwAKCgmgAACQAAoAAAAJ////7///v/+/29vb2/n/3/np++/a/5D8sPDawAAAkKCgALAKCgoAoKAAAAAJAJAAoAoKAAAKCtsAsAAAkAkADKCpoKAJAAmgAAkAoLALCgsKAAoJAAAAqQoACgCQoAAACf3////P/L3///7+////vv//7b2tvP8LwAAAALCgoAAAoACQAAAAAACQAKCgoACgCwAAAJoAAKywCgoKCgoLCwkKyQoKkKAAoAoAkAoACQANCgAKAKAJAAAKkACgAAAAAAr5//////////n5/////b+f/+/+/w4MvAAAsACQAKkAmgCgqQoLAJoKAAAAAKAAAAoAmgAKAPkKCQkAAAkAAAoAmgkACgCwCaCaCgCaAKCgqQoACQCgCgAAAAoAsAoAAAkA+f/////p8P////rfv/7//9//C/nwAKCpAKCgqQCgAAoAAAAACgAACwCQoACwoACaAAoJAKDpAKCpqQoKCwCQoACgqQoACgCgAJoACgkJAKkAoKAAAAoACgkAAJCQkAAPC8m//////////9//////3r/Ang4MsAkACpAAAAoAoJAAoKAKAAAKAACgCQoAkAoACgkKCvkMoAAACgAJAKCgCakJCpAKAAAJCgAKCQCgoACpAAkKAAkKAKAAoKCgCaAADL/Ly/n/////////z/3+qfDwCf2wDwoKkAqaCgAJCgoAkAAAAKCQAKAACgAKCpAKkAoAAMqwCwmgAAsAoJALAA4KAKAJqaCgAAqQoKAAywAAoAoACwAAkACgkAAKAACwCQrQ/P///e3///vb//6f3g/KAL7fAAAAoAAAkAsKAACgoAsAqQCgAAAKAAqQAKAAoAAKkL0KAAoJqaALCaCgCwmtqQmgAAkAqQAAAAmpoKmgAAAAoAoAoAAACgkACpAAsK0LCwvKD7//7w/vy8ngC8sJy8v62pqQALCgoKAACwCQAAAAAAAAoJoJCgAKmgCwALCgCsCgkKAKAACwCgyQsPrangoACgCgAKCgsKAAAAAAmgCgAAAAAKkKAAoLAAoKAJCskAy9+8sAkJ6QvLwAvJ4OsL2toAAKCgCQAAALAAoKCgCgCgoAAAAAAJCtAJoACgAJoLsAoJqQDprL6amvD5//8JwLCaAJoAkAAJoKmtoKAACQALCpoAAACwAACgCQAKCQrLAAAAAAAAAAAADwAKkJCsvamgoAkAoKmpoAqekAAAAAAAALAKAAoAqaC+C+kACgAM8JCgCgkKm9vLDb/////6mgrAmgAKCgoACQ4ACQoAoAoAAACwoKAAsACQoAsAoKkAAAAAAAAAAAAKALywoKCaCsAAmgoJAAAACwCp6wsKkKkAkAAACgCaAPAL3L6ekKkJoKCQ6QoJrb/w///////88JywoAqQAJAKCgCaCgkKAKAKCgAACQAKAKCgAKAJAJCpqaAAAACgkAsJywAAkAmgkLCwAAAKCaCgAAsPnwAACgCgoAoAAACg2gv8v/DwoACunpAKkKCa3//L2///////qeug2pAKmgqQAJCgnACgkAkAkAoACgoAAAAAAAAKAKAAAACgsJAACgAAoKCwoKAACgAAoAoACgCQqa378OCgAAAAAAAAqQoJq9/L/9/62poLAAqQoJypq9+/D7/////735/foAoAAACgsKAAoLAAoKCgCpALAACwCaAKmgoAoAqaAKCQAAoLAJrakJAAAJoLALCgAJALAACgAJqfywkAoAoAAKkAAAkKyev////fDgANupCpCgsA2vvLvt+/v//fv///2tCpCpAAAAmpAAqQAAAKAAoAoAAAoACQAAkAAJAAqQoKCgAAoKAAoKCgoAAAoAAAsKAACpAAoKnpvKAAAAAAoAAKAKAJq56f3///+fALwAqenLALCfDwyb/en76/3//56aAAoAqaCwoAoLDgsKkAmgAAALAAAAoAAAoAmgoJAAAAkJoPCQmpAAkAkKAACaCwAACgAAoJAA2toJCgmgCpAACgAACgAOn/////DgoMqekLCwCwvw8Nu+mpsPn5+//+2gCwkLAAAAALCQAAAAAKAACpoACgCgAKCgAKAAAKCgsAoKCQCgoAoAoAoACpoAAAALAJoKAKCpoLDwoAAJAACgAAAKCQD5////+86QkLmprw8JrJ6f++8Nra356wvf/braAKCsoKCwsACgoLCgoAALAAAAAJAJAACQAAAKkAAAAAAAoKAAAACQAAALAACgoAoACgAAkAAAn/+fAAoAoAAAoJoACgsKv9//7bCgoM8JCpDgmpv//5/72/v72toLvtsAmpCQkJAMC8vAkAAJAJoACgCgoKAAoLAAqQoAoKkACgsJCQsLAKCgoLAACgAJCpCgAAoAoKmgqf/p6QAAAKAAAAAJAAC8m///2skAALqa+euQoP////8Prf3/ywvf2/DpoAoKCgravLCpoLCgoKAKCQCQAAAKAAAKAKCQAACgsAAKCgAACpAAAACgqQoAAAAJCgCQAAAJv///CgCgAACpCgCgoKkL7f79oLCpoJ2vkLDKn7///9qfn7/9vp+///ywAKnpya2r376eAACQAACQCgoAoAoAAAoAAAAKAKAAAAoAAACgAAAKkKCQAAkKAKAKCQAKAKAL7fnwAACQCpAAAAAAAJDr2/n6DwAAAOsJ68sJqf////8Kn///+f////vAsJC6vpvf/98JCwoKCwoKAAAAkAkAmgCwCpoAAJALCpCpALALCgoAoAoKCgoAoJoAoAoAsAsAm74AqaAAAAoAoJoAmgqQ688NoKCgAJ65C54Kmt///w28qf///////9+8AKydvf////76AAkAAAAJCpCgCgCgAAAAAAAJoAoAAAAAoAAACQCQAJAJCQAJAACQAJAAAAAKDAmpAAAKAAAAAAAKAJCpALCgkJCQoPkOngmpy7//+/oJv/////////8LCwu///////+csKCpoLCgoAoAoAoAoAoAoAoKAKAKAKAKAKCgoKCgoKCgoKCgoKCgoKCgoKCwmpoAoAoACgCgCgCgCgqaCwqaCgoKALCpoJoAuf///wALD/////////+eAL3///////8KAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBQAAAAAAALStBf4="",
    ""Notes"": ""Education includes a BA in psychology from Colorado State University in 1970.  She also completed \""The Art of the Cold Call.\""  Nancy is a member of Toastmasters International."",
    ""ReportsTo"": 2,
    ""PhotoPath"": ""http://accweb/emmployees/davolio.bmp""
  },
  {
    ""EmployeeID"": 2,
    ""LastName"": ""Fuller"",
    ""FirstName"": ""Andrew"",
    ""Title"": ""Vice President, Sales"",
    ""TitleOfCourtesy"": ""Dr."",
    ""BirthDate"": ""2/19/1952"",
    ""HireDate"": ""8/14/1992"",
    ""Address"": ""908 W. Capital Way"",
    ""City"": ""Tacoma"",
    ""Region"": ""WA"",
    ""PostalCode"": ""98401"",
    ""Country"": ""USA"",
    ""HomePhone"": ""(206) 555-9482"",
    ""Extension"": ""3457"",
    ""Photo"": ""FRwvAAIAAAANAA4AFAAhAP////9CaXRtYXAgSW1hZ2UAUGFpbnQuUGljdHVyZQABBQAAAgAAAAcAAABQQnJ1c2gAAAAAAAAAAAAgVAAAQk0gVAAAAAAAAHYAAAAoAAAAwAAAAN8AAAABAAQAAAAAAKBTAADODgAA2A4AAAAAAAAAAAAAAAAAAAAAgAAAgAAAAICAAIAAAACAAIAAgIAAAMDAwACAgIAAAAD/AAD/AAAA//8A/wAAAP8A/wD//wAA////APAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACf///////////////////+AAAAwAv+AAAAqQAP//////z////8AJ/8AAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn////////////////////9sJ8AAAAA/L/+ngv//////w////wJ//4AAAAAAAAAAAAJAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAv///////////////////////4AAAkAnwn/wL//////8P/////v/8AAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ//////////////////////8PCwAAD/AAAAC////////f///w+f4AAAAAAAAAAAAAAAAAAAAJAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP/////////////////////+Cf/8Cp8AAPwAv///////6///z/78AAAAAAAAAAAAAJAAAAkAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC///////////////////////AJy+CcAAAAAL/////////f/8v8kAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn///////////////////////wACf8L+p8Km/////////7////AAAAAAAAAAACQAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA////////////////////////8An8vA3//9//////////+fD8AAAAAAAAAAAAwAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ////////////////////////AK8AAAAAAL//////////7+AAAAAAAAAAAADQAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACf/////////////////////////p/AC/AJC///////////38AAAAAAAAAAAMAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD//////////////////////////Qnr/Jra/////////////gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAv/////////////////////////AAAAAACf////////////wAAAAAAAAAAJwAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ///////////////////////////wv9v///////////////AAAAAAAAAAkAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP//////////////////////////////////////////////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC//////////////////////////5+f25///////////////+AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAv////////////////////////f2//7//mf/////////////8AAAACQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ///////////////////////9u//5/fvf/5v////////////wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP/////////////////////5+//fvf+//7//+f///////////wAAAAAAAAAAAAAAAACQoACcsNqamvnpvpAKAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC/////////////////////n///+/+/v9+///+b///////////gAAAAAAAAAAnKkKkPAMkL2pran/n56b2bDJwPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn///////////////////+f//vf/5/Q+fnfv///n//////////AAAAAAAAA2tqZ4Jyw+by9rZ+b2wvLmtr8uan/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA//////////////////37/7+f+9qfCtCwoJ25v/+//////////gkAAAAJywub2pn5vbD5sLmrD5rfm9rbmby9qfAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAL/////////////////b+/2/zw8P38va0Nnwvv37///////////AAAAAAACcvPC8sPC9uen56duemw8L2w/pua2/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC////////////////9v//5/w+f+8sL0Pmp4A0JC9+//////////AAAAAwLn7m5vbn5vanpuemanprfmfmtuZ8PnvAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn///////////////vb29rerb2pyfnw+9ranbDw+enb//n//////gAAALCdqcvPCemp6fm9D5rb25+anpra2+nw+fAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/////////////76fv76f29vLy/vJ6fng29oNsJ4J6cuf///////AAAkACp+5m58J+fmw8Lmemtranp+fm5rZqbD/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAL///////////9+f37zf2tqekLnJy/vL/b8Pn7yememtD//5/////gAAwNvbDa0PC8m8vPm8vJvbm9ufCw8Pm+np+fAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACf//////////27v/uc+wvLDwn96fqQ25y9D56cv5npD5qQv/n////AAAC5qa25vwvbvLm5vL272p7anpufnwvJufmvAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn/////////n5vfyw3LDbydvb8Knpn/D8sPuen7yesPCe28+b+b//8ACQnL29sPCbyw29ra25qcvbm5+enpqb2/yw+fnw8NAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA////////2b+f+5/LC8sNvp6Q29ueCcsJrQ29qdvZyZ6Z6bDw/////A8Km8vLy58LnbCw+bnp25sPDan5+fnpqbnbD/n5m7+ZDAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP//////+wv/D7za28npvLDQmvCeDZ+anw2p4J0KAPC8vanw8PD7n/8AAJy9ufm8vfCw+fkPy8sPD5v58Lyw+fn56w+fsL2v28+a0JoMvLAAAAAAAAAAAAAAAAAAAAAAC//////wvZ/5+cmtoJ8JycsL/Qnpn6mtnp+fmfD5+ZvZsP0P+fnp//8AmtqbDwvLmwsPnpr5ubnwuQ+Qufm9sLy8udsPnwvZqbD5Dw25Cf8AAAAAAAAAAAAAAAAAAAAAn////7yfnvnw8Lyanamempy9C/2ekN/an5vLy8vby+2+35v5nw+em//AAAnJ+b256fnQufkPnp6fDbD7zwvLyfm/yan7y9q9vJueubrb+prwAAAAAAAAAAAAAAAAAAAA////6Qva2/npDQvJ6Q6Q8PkL0Lqf+5qfnp+fn58Nvby5++nvC/np6f8AkJCwvLDZvLqbyw+by5mp8L0Jufm5up6Zvpqf+Qna2w252tm8nb2cAAAAAAAAAAAAAAAAAAAP///Qm9+58LyempyakPnLCQvan53wn9n9rby9vem/vfvcvZ25/f+/2f4ADgANC5+6292tufC8va2fC8v7y56enbn62b2/mp+rkNqfC5rbm+mrwAAAAAAAAAAAAAAAAAC///y5vPsP0NCpCcsJy5yw/by9+evb8L6b29v56b/9D7378PsNvw/9vpAAkJCwsNsNmpqZ7a2b2purn5ucuen58Ly9utqfnanZ6bnpvJ+p6Z+cvAAAAAAAAAAAAAAAAAn//pna2w3wsL0A8LD6nAudsNvan5D5//29v/3/n9vb+fv9v/n7y/n6+fvAAKwAya2wvb2tubDwva2c8Lz735sLD5va2b2vq9q9mtuem8n5npr5mwAAAAAAAAAAAAAAAA//mevb37CenADwn8kNvbDw2629C/+fmfv/25uf+9+9v5+f29/b3f/f+v2/CQCQAJqfC8ua2tufC9q5vbnwsP29sLy5rQvfnanavQvb0LD56b2a0PAAAAAAAAAAAAAAAL/wnpmtsNsACamfAJvbyw+anNvL/b3w/5vb/f//n/vf/f/7//v/up+/n5/LwAmgkOnJvbnLn5y5+Qna2tqfn5ra29vw8Ly72p+p0Ly5q5+am8vpv5sAAAAAAAAAAAAACf4J+drZraDQ8Nngn/Dwufnp26nb256/2/3/2/n5//n7+/n/29/5/fvf/a+/sAAAAJCa2py7ywu8vL65vbn56a25vJqfm9udvanbr5vL0NqfDbmekNDwAAAAAAAAAAAAD/mbyw8OkNoJCwqdsJ+f3p6dqd69rf/b/b+/v///+f///fvfv/n//5/72/28/wCcsArJn7nJvbyfm5kPyw8Pm9sPCb3wvJrfy56dkPm9qb2p8NqZ65vLAAAAAAAAAAAAvwDbDbmw8AkLydC8vampuemp+/n5+5+9v9/f3/n////9///9+/+9v7/b77/72fAAAJAKyfq8mtsLyem5vbnw8Pnw+pqfm/mvuem+vQva28ufC58Pmem98AAAAAAAAAAA/AvQ+Q0NAPDQmp+b2tvfD5+fDQ+fvf/fn/v7+///n/n/v9/7/f/f////n9vfvr8AkMCQmw2bn5D5vwvLyw+b256b0Pnw8J6fnpnpkL2tsLnpm9rbnpnwsAAAAAAAAAAL8L2pmtoLCQmp8PCen7y5+cvJ+/n7372r2/3/39+f+////7+f//v7/f/b/7/r/98AALAKmtup6emw8J29ufmtqem9qb2tuen9uQ+by8vbDa28vLmQ+eqfnwAAAAAAAAAPwNrbyQCcng+cufn58J/a2/n729/569v9vb/5+////fn5/f//+/3/+/+//9vfn6/wAAkJyanb256fn5qbzw/bn58L28ufD5sL7bDwsJC9udsL29r5qZ2p68kAAAAAAAC/wJ0JrQ2gCZoLyw8Pn5vfnL+enwn/n/n729v/n9+fn/+/+9+9//+9////2/+9+9vfCQDAAJ6anpmp6fnpubC88Pn5qb2psPn/mw+b0L2w8L29Ca2enwufmZrAAAAAAAD+Cam8kLAJDw2dvb29v8+7m5yb2/+f29vb29+b2/v//5/b3/v/vfn/vb+9//3/vf+/4AsJqckNufDb2wufDw+bm9qenwvby56fsPm8C8sPm8vam/mpsPnpra2wAAAAAA/w2p8JAAyemtuesPva+bv8/8u9rb28ufn9van9v5/9uf+9v7/b/7/7//3/v7+72/+f8AAAALD7ra2wsPnp+fm8vLn58L28uem9+Zrb0Jvby729rQvQ+Qm9sJC9AAAAAA//CcngnpsJ8Jr5nw+fn929ub39vfvb372t+b25+fn7373/2/29+9vf2/+//f/f/w/73wAA0AkJ25qfnw+fmprb2w8PCem/y5vLz52pqcvbnQsL29q9D7ya29vLkAAAAAn/ybCQAADwn52+n5/5+r27z/m+m8n/vb2bD9rb29+f+fvb/fv73/+/v7//v7+/n/n/v/CQoJ6anp0Ly5Cw+fnw+fm5+56b28u/uanb2pC8sL29qb0LmQm9sLCfDgAAAAC8oJ8MkNCQ8PvJ+fC/3fv8mb7b/b+dnr+9ufm9vbn/n9+9v73/+9vf39+f//35+5/w/fAAkAAJ+fC9uen5vLy7nprakPn5rb0Pnp+tqcvb28vQuem9vpr56cn7CQAAAAAAmfALCwvpvbyfC/nwu735v/29ufn7v5yfn5n9C9+5/7///9+fvfv/v7//+fv/+f+fv7wAANCemwvanp2p6bm8+Z+fn5ra2+n/mwmb0LnpqZqfDbyamfkPmwqcvAAAAAAAywsADbycvJv7/J+f/d+f+bv737+f2fv5+f+725mfn5+fm/v9u/29//////+///v///8AmgoJy9C5+am9sNrbmvC8sPm/m9qf7by8vQm9vL2p8Lnp6Q+a2dm5m8kAAAAJrZwNoJv5m/29n7/5v7v5/9/b+fn7+9vbn5/Z+f+fn529/Z+f37//vb/5+/39v/27/b/wCQCanr0PD5ra25+enb256a2w8L2tuQufmp6byan5C9qZvbC9sLra8JrAAAAAmaCQna0PD5vL+9rf+f2/vb+9v/+f372/+fm/nwnw+evbn/n/v/+///2///v7+fv9+//wAMkAmZqb2w29sPC5+prbn5vbn56fvLnpqcuem52wvan7ywnLDw2fmekAAAAJy8Cw8Ln5/a35/b2/n9v729/f/5m9u9vfn7/9n5+fn5n5/5v729/7+///vb3/n//fvf/wALALD60PC8uprb28vb2w8PDw8Pm/memb2pn56esPCfkJ+em9uZq5y5+QkAAAuQnJm8mp+9u9v5v5/7/f/7+5+f/737/7+fn7/b29sP+fn7/f//vf/f+9//+fvb+///n/AAAMkJu5+b2f2prbm9qfm5vbnwvL6Z6emeC5vZ+b2p+wuZ6a2tnLvLDwqQAJy8qawPn9Dw37/fn/m9/5+f/b/5+fv9vb//+fu9vb2Zvbydv5+///v73/vb+///3//7//sJyQrQ0PDwsLn5+enr2w8Pm8ufm/28m5vJva2pqemfDb3pm9ubC5y5+fnLkJuQkNm58Lvb+fn7+9/7+/v73/m//5/b+/2/n/372tvJy5v72/3//7//v7/739+/+/n//7wAoAkLC5+en56anp/bD9vQvL2p6fsJvp6b2tufn5nrm/C5ra2g28uempCwwL2gnpra29/b2/+9n/+9vb39v5/9uf2/n9v9v5+dvb2/uf29vfv/v9+f39+9+/v9vf//+f8AkJC8m8sJuem9vbmp+amr29qfn/na2fnanbya28ucvJva29vZqb0L29vbkNrQCfn58Ln6nb37/b3////7/b2/2/vf+//b37//vb29n9vb2/29//v/v737/////7/7//8AAOALD5+fD58Lyw+fD5+dvLnwsP+pqbqdqbvZqby5+9qZqamtnLnwvLC8sJkNqcsPn9rfv9vfv/vb/7/fv7+b/5+5/b2/vb29mfnb29vZ/Zv7//+f2/+9vb2/v9+9+//wCQkJyanpsPn5vbD58PDwu8uf2/Cfn8nanw2p28uekL28nb2am56Z+Z+ZAL6anL256b+/2//735+/+f+//9/72fvfn7/5+9uf////+fn7m/3///v/v9vf//////v///vwAArJqfm5y56emtueufub372prfnwmpup+9uesLnLm8vLm+mpy8m/C/Dw+ckMm5y9n9nZv9uf+//f////vbudv737v9ufDfn9v/29/b+d/9v/+f/5vb//v5/5/7/////wAJCQnLy8ufn5vbD5nwnp6Qvb2/sNvbycva2p28m96fm9rZvamb8J8JsJCby5rLn6+b+/2//5/b+/v7+9//3735+f2fDZuf2b25+b+f372////pn//b+/3////f/7+f/5AMCgub2py56a2tvb6b+fm/y8sLy6mtubD5van56bmp6b2vCdrZDwnw29CfANm8udvPn7+fnfu9//3/37+/+9u/n78Judn5vZ/b39vb+f/f//+QCfm9n7v7+/v////73+CakMDwvbnpvbnwsPnw8Ly5vb29+dvbytuf6Z8LmenbnwuZ+wmw+b2psLD7CaDb27256f+f+5/fv5+9v//fvb39vZ29nLmb2/m9uf2529m9mQAAkJrb+d/f3//7+f//+/AAAJCb2p6by88Pn5rbn5vemprfqa2tubnwmem9D5+w8L2+kJ6fm8ufCcucnp2/D9qf2/n7/f2//////5/735+QAACQm5y9nZnJCQkJCQCQkAnb3L2fn/v/v9////v///AJywuem9mtvbm58L2w8Ly5vb2vnJua0Nrb2pramvDbn5rZvakLy56Q2wnwCZqdv5/5v9v5v7/73/vb//+fvbkAsJCQCQkAkACQnJrQkJnLCfm9u9v7/5+f+/v5//37//AAAAy56a29up6en9qfm9ufy8ufu8n5ub2wvQm9mb8PC9u8udvbnLnpAJC/mtnw/bntvb3/29vf+///n/v9+9qdm9sNkNCQm529m9mfn5+dvZ+f2/n9+/v5//////v/+/wJqQkPn5qa2fn5sL2p6a2puby/ybqfDwvfmp8LDwm5+QnLnpCw+56QAA/QDavLm9+b2/v7/b+////7/737+fn7/b35vbnb2fmb2fn5vb273737/5/7/9//+fn7+///n/sAwKDwsPn5vpqen/m9vbn56embm8nLkL2prZCw29v8vL+56Q+duckAAJmwm5258Pm9v529/7/fvb+f29v/n5+/2/m/25+9vb29+9nb2fndv5+9+f+f+fvb///9//2///wJqQmb356byfn5vLDw8Ly8uby/0Ju9rQufkPvZqbyb2wkPn5Crybn5+Q6QvJrfn8v/D/v/n5+/////v/+f///b/f/b/fnb29vb3/+9+9v72//7/5//////+fv7/7//+/8AAAy8sLn5+pqa25+bm9vbD8sL68nLm5yw+QmtnpvwvLnwsPnZvZ/fn5npy/mp6b0PvZ2/v/n9v9vf25/5+/29v5+fn/+f/b3/////vf+f////n/v9v9v9v9/fv//5/78AnLAL28sPn9vZrfDw8LC9ub2/mbC56cufnp+bqfCfm56b2wmp2/+///CQqQ+fn5v5+/+f3b/7+/+/v/+f/9v/n/+9v5/5+f+/ufnb373/+////5/b///7/7+/3/v/+9/wAACbD5+fqa2r25+fm9va2tqfDw2ekLnpqZqcnan7y8uekNrZ/53/2/kPnfnp69+fnb37+7+f3735/fn/vb/b/7n/n5+fm/n7372/+f/7/f2/+f+/+fv/+fn/+//7///wmgnw+bD72fm9mvnpra8Pm58NsJq5+cufnp2rC9sNuZy5+bm/n/vb/98J6a2bnLnw+/v5/f//vfv/+/+9/729v9+f+fv5/5/fn9v5+/v9v/v5//n5//35///7/9//37/8CcAJvJ+8vpra+em5+b2w8PC728namp6QuanZ8L2w+pvLDw2f+f+9v/+Qn5rZ+96fn9n/m/n/+9vb/b3729v/2b29v52/n/v/+/3//f29+/2/+f//+fv/n/n///+/+///AJC8C7Db29m9n58PDwvbn5+fsJsPnJm9vJ8LC9qfna25+Z+9v5vfn78NsL28n5vb+b/5/5/5vf//n7+/v//bv9np+evb/5/wn/v/2/v/vb/fn729v//b+9+9v7////n/AKAJvQ+5+a8Lqem9ufnw8Ly/DbDbC5vLCbDwnanbC9uekP+dvf/52/0LD9vLv5+fn/25+f+f//vb+9/9/fn73b+Z29n5+f+fvb25v9n5/9v5+9///b2//b//+f/7////wJDw2pvesPn535vLDw8Ln9uducuw2em9vJ+fqfmtvLD52529ufn/vfDb2a250L2/6bv9v5/729+9vb2/v7+f+9u/vb+en7352/n9/b+/n73/n/ufv/v9v/vf////+fv/8AALD7y735sPqw+fm9u88Lz62pybqZrbCwsJ2p+am9sLCfv/2/+f+/sAvbDavfD5n9n72/n/v7/b//vZ/f29n7398NvbnZv/+e+/v9vf/b+9+f//29+f29+/v/v///+f+gkAkJvZqen529qbyw2bn5ufvQucnw2a2cn5qfCdvLDb29mb/5//n9yfCfC9mp+f+fvfv9v5/fn/29//u/v/+9vbn7D5+/2fn5nb37//v9vb/729v/v7//v9//////vb/AANrby/n5+evbnw+fvtqenvkLnpsJutmpuenwn7C52pvb//3/v//bCwnwvb6fm/n7273b/f/7/5/7mb35/5+fn729+f29v/8Jyw+f/72f//29/735/f373/////+//Z/wCaCQvw8LD7npy5ua2bn5+Z6cma2a2bDa2pC9sNnwuf39vb+f/5+/Cf6fnpn5/L+9v9u9v5+fn/vf//2/n/n5+fnbmb/b/Zn5udv5mf+9vb//ufv/+/vf+/v/v///+//wAAkPnbn5+a2/sPD5vp6em/mp65rZrQsJvb0L26mtqbv/+fn/n//b0Amtuenan5/L2/2/n/v/+f+9+b+dv7/7+f6dv9m9vwCf252/wJCb29vf/9+fn/+/3/////+///v9AJ6Qq9qen58J+b28m9ufD/6Qmema2728kJqfCdvZnf29v5/7/5CfC9vbDZvp+9ufvb+f+9/b3/n/v/n/n9+9/5mwnbwACdubn/vfuZ+dv5/5vbv/+9//v///////vb3+CaAPnbvbnpv/C8sL8Ly5+bmfnp2tqckLC/nwvLCan7+f+d+fyQn7Da0P2b6fnb/b2/n/nb+/+f/5/5+9ufvfuQ+f2wm52a2f/b27//n7//+5//39vb+f////////////AACQmvyw+by5n5+fn9uem/ywubCb25rZ8JC9m9vJ+9/5/6n5kJv9qbmwutmb+/m/+f+fv735//v/n/3735+9/fn/vb29vZv9n//f+f///9vfub+/////2////7//vb//AJDpv5vfnpvL6byw+prbD58NDw8JvLmpn58Lyampn/nb/9udv9v8nL3pyb6dqf/Zv5/5/fv/+f/f//vfvbvfm9vb2//by9n7/7+9/5v///m939//n9vf/7///////5//kOkA0PCwvb29vLn5n72tufuambmtqfDakLCfmtvZ/5+/+Z2//5/5qdqdv8n735v/+fm/v5/b//+//5/72927/bn739m/mb+f/9/7/////5n7v/vb///7/////////5v/wJCam5+fmtq/m9qa+fvby/kNvLybnwm5y9n5rZsL+9vb2fvf+f/Qnam+mZv5+f/bn7/f/f+9vb3/+f+f+fv98J/Z+5/9n/n///n//////5//35//37+f//////+/+9/78ArAntran5vQnp+fnpywvby5Cwnw8PDakLC8mw2f3b/9m9+//9vwsNvZvL28v5+9+9+9v5+f//vf///9v/mfn5n/mfkL+f+f+f/b//+/+f+f+/+fv////////////5/f8JCam5ub2vC/+byw+fuf2/vL29qZuZsJ+cmbybqb+5/7+b35/7/5y7y+n5rb2/n737372/35+f/5+9//+f+9vbCb/b29v5/5+9vf////+5/5/////9v//////////5//+gAJytranb2QvLn5vw8LD/mQkLnLy8D5C5vpmtn/2f/9/9v/+5/QCcmZ8J+fvfvb+fv9/5+//72//fv73/n/+9n5m8vb2fmf/b/7/72/n/+//5/9v//9+//////7/7/7/Qngmb29upr/ufC8sPn9ub2p65ywm5uQ8PCZ6Zvbm9/7n5/b/f/wvbD/C/D5n72/29/bvb/9uf///7/f+/n9vZ8J/bn52/n/2/2/////+fv9+/+//9v///////v/+9+f/wAJqemp+fn5Dwvbn56an/nJnJqZ8NDLmQvLkPn/n7/9+9v/n5nwkPsJ+duf+9vfv7+9+9mf/9vfn9v739+//729qcn5qdvb/b////+9v/373//b/7///////////b/5/wCQy5+enp8L+fmtvpu96euaC9npC5ucsPma2wm9+f/7373/+/+fDZ+fnr25+f+9n9v73/+9vf/7+/3/+/n5+9vb27kPn5+9v9v///////v/v5///f/7////////+//b/5oKkPC5+an9ra2w282pv/nJ0LCQ+ekLnQsNCZ//Cfv/+fm/3wnwmpD5+ZvLn737+b/f+9vb+/m9/9v5/f+//f/9vd+Z+fnb+f///7///5/9//+f+/////////////35/w0JD5vQv9+a25vLvbvby/kLC52tsJC9CwnakPm5n73///n/+Z8A29vakP29/5+fvf29vf//29vfvb+9u/n9m/v5+auf35+/373///////v7////v/v////7//+/+/+9v/AAmw8P+bC/mt6b2py8vb6cvLCQnbwL0PmpqZ/f+f//vb2f/Ln5qQ+b+bn5ufv5/b+//7/5vfv72/n/35+b+f2/n535ub35+fv////73739v9v/29//////+////wvb/8Cay9ubnp/Q+bmtvfufm9mpma2pqZsJqQDZ0Jv5+b+9/9sL/Z8A29vJ/Quf29vb+9vb29vf+9/fnf+b/5/9vb+fvbC9n5vbn5+///n/+f+/37/b/7/b/7///f/7/8mdv/4JCby8v5q/np+amp8Ly7+Q6dCckOna2puaAL/5/Zn/v7/Zm/AACem5mtnrvf+fn9v/vf+/37+/+/n9vb+bn52/29/by9+/29///7/5v/n/u9v/n///+////7////Cb/9CQran5sPnw+fD5/5rbn/DLmamwv5sJmcnJuZ/5+b/73///D5AMm9vfD5vZz7n727/b2/n5+9vbn5+fv/n9+fufm9uZ+fmdv/v//9+f/5/5/f2p/b+f/f/5/////wCd/w8A25+a35+fmpsPkL2w+duQvpwNkJC8sLC8AJv5/Qvf+f+9+fCQCa2pufn7uf2fvf2//9+/+fn9+fn5+f+/v529vJvLn5+/+Z//v7n72fmtqbn5+9v7+///v/+/v/Cb/wCpqfC9sPDw+enw+9rby7y9Cbmg+a0JycmbkJ/9v5m735/5vwAACZn929qd+b+f27+fm72b2tsLD56/n5vb2/nw+8vfnb35//v//9vfv8+b39ufn729/9v//////wCZ/w0Anp/L+bv5vLCbnp8Ln9sAvJyZC9qbmprQ0Jv5/b39+/8L/QDpAPmanwn7n9v/vfv7/fv9vb29+b2Z2/29vf+fmfm5+9ufvf//8L/56b29ub2/n5/7/7//v///+/CQ/wAJ6bm9rw2/y9vL37n9qbyb2bCwvQkNrQkJqQ/9v9m/v9+d8AkA6bz725+f27/bn52f27nan5+fm9uesJm/29ufn5v9rb3/n7//2fyb2fm9v/////+/2/+f//v//8n/vwqem8vL2fvwubC9qw8Ln/vACw0Nmp8LkJy9AJv/n56f//2/sAAJANudvPnpvb29+fv5vJ+9ufvb/73529rZr7/b28nb29vb/5v/rbufn5vb35vb///7///7/////wuf/ckJvb25+py9va0L29va29uZrbCw+QkNqakLkPmf+fmf/7n9rQAOCa2r25+fn5/7+dvfm/Db29v9vfvb+b29mdu9rbm9vb29vf//kN+9vZ/9v/2Qmp//+ZCfn////60L+p6a2tran/uemtsNra29rfngmQ0JAPC5nJC8CQv/mf27298L0AAJAJudqfn72/29+/m68Nuby9vb2w29mdvbv5/b296b2/vb/7/wn7/a2////ZC9vfvQAAvb+////wC/rakNm/m98L3pvan5ufmvm/AJvLCw25DQ8LyZsLnfn/mcvfm9oAAACd7bn5rZ+9u/v5vZmbnpm8n/n9v5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kAAKDQCp/pv/+Z+frb+Z//AAkAAJC/m5n5//v/vf////n///+/+9/7//n5///7//v///////////vaCvvLywoAAAkJyQ2pyeCckNCbyQnpnw8JsJkJCQkOkAAAAAAAAAAACQybmduZ6bkPn5/bCfvL/Qnp/b35vf+9v//////7////3///////+f///9v9/f/7////+//7//8JvZyw8MAAAAAAAAAAAAkAAAAPAACQAAkAAAAAAACQAAAACQCQAAAJAAkA8L3/n9+b8Pm/2tn5v/mduf+9////+9///73//5//+//9///7////+/2/+/////////////3gD6/QmpAAAAAAAAAAAAAAAAAA8AAAAAAAAAAAAAAADwAAAAAAAAkAAAAJDQsJ+5rZ29+b27+f+f+//5n73735//v/n//////7////v//9/5/b/b////////////v///uQCQvp7eAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAkADAAAAADQAAAAC8Cb2fvf2an73tvfn/n5/b2///+/+/+f////////+f//////2///////////////////////nLAJ8Lmp6cAAAAAAAAAAAAAAAJ4AAAAAAAAAAAAAAAAAmgAAAJAAAMAAkAsAmvDbC9+9ub/5/5+/+/vf+Zvf/9v/2///+/2///+f/5//v///v//////////////////7ywALy97fAAAAAAAAAAAAAAAAAOmgkAAAAAAAAAAAAADAAAkAAAAAqQAJDJANCZ25/bvL/9vbm9vfn9v5//+9v////7//3///n///v/+/3/+9////////////////////vACfnwsLywAAAAAAAAAAAAAAALwAoAAAAAAAAAAAAAkACQAAAAAAAAAAAK0L0L8Pm8n5mfn/2/+9+/3/vb3//b/b///b+/+/+///3/3/+///v/v////////////////9qQAAvp8J8MAAAAAAAAAAAAAAAMsAAAAAAAAAAAAAAAAAAAAAkAAJwAkACQkAC8m9rb+f/t+9v9vb/b+/3/+/+/+/2////f//n/v5+/v/v/n//5//n/v////////////78AkLybwLwLAAAAAAAAAAAAAAALwAAAAAALAAAAAAAAAMAAwAAAAAAAAAAA+skJvJ29n9ubvb37//v/39v7/9//3///n///vb/9///////b//vb///9////////////+9AJAP2sC/C9AAAAAAAAAAAAAAAMsAmgwAAAAAAAAAAJ4JAAkAAAAAkAAAAACfCcm6m/sL3/2/ufm9/b+///3737////+/+9////////29v//Z///7//2////////////a8OCa+fAPDa4AAAAAAAAAAAAAALwAAAkAAAwA4AoAAAkACQAAAAAAAADQAJALkLyfnJ/b+b+d/9+/v9/b+fv/v/+fvb/9/7/9v/v//b////m/mdv9+/v////////////5CQCfAK2/C9AAAAAAAAAAAAAAAOmgAAoAAAsAkAkAAAAAAAAAAAAAAJCgCQAJ6QuQ+9qfn9n7+b/f2/v/n//b35////3/vf/////b+/+/+f3pn7+fv/35//////////+emgkNsNDwv+AAAAAAAAAAAAAAALwAAAAAAAAAAAAAAAAAAMAAAAAAAACQAA0Am8kPkJ/bn7+/2/2/v/2///n/v/+9vfv7//+///v//9//nwCZ6Z/5/9u//7////////vwvA8KAL6bzQAAAAAAAAAAAAAAANoAoJAAAACsoAAACQAACQAAAAAAAAAMCQC8kLyQv5C9v5/b/fv5/b/9vb+//b3/+//9/7///9///7+9CQufmfn/+b/Zmcm5n//////PCQAJDQnAvpAAAAAAAAAAAAAAAOnAAAAAAACQAMoAAAAAAAAAAAAAAAAJAKCa3Ju9nLme2fn9vb/fv/n////5//vb/5+/v9/b+b/5+f/JkJnwm/2/n/m/6QAPvL////+fAJD8C/C88A8AAAAAAAAAAAAAAPCgAACgAAAACwAAkAAAAAAAAAAACQAAAJwJqQnAufD5v5v5+/29/b/729v/+///3//f/b//3/+///CwD78J/fvZ8J/wkLnZCZ////mgvAqQvACbC8AAAAAAAAAAAAAAAKyQAAAAAAAOAAAAAAAAAAAAAAAAAAAACQAAnLybnL25/b/fv9v7+/29v//fvf+f+/+////7/9/wsAkJudC7/72bCfkJCQygD/////D5yQnLDwmsnQwAAAAAAAAAAAAAANoAwAAAAJoJDgAAAAAAAAAAAAAAAAkJAAvQqQucucvZ+8m9/b29vfv/35+//7/7/5//2//9/7n52ZAMnwnfnQsJ+9rQvAkAm/////kAoJoMkLybCgAAAAAAAAAA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AAAAAAAACcAAAAAAAAAJAAAAAAAAAAAAAAAACQqQkLyQAAAAAAAAAAAAAAAAAAAAAAAAAACQypAAANCQuQDpAACQvJAJDADQAAAAAAAAqaAAAAAAAAAAAPAPCgAAAAAAAAAAAAAACgAACgkAAAAAAAAAAAAAAAAAAJAAAA0ADQC8kAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAkAAKwA0AAArAAAAMqQoAAAAAAAAKAMAAAAkAAAAAAA8AAAAAAAAACgAAAAAAAACQCcALAAAAAAAAAAAAAAAAAACQAJAAkAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAyQwACQkLAAvAkKCaCakAkAAAALAACwywoAAAoAAAAAAPCwqQAAAAAAAJCgAAAAAJoAAAAAkJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAAAAvArAkACaAJAADAAPAAAAAAoAAJoJAAAAAAAAAAALwAAMAMAJALAKAAALAAAAAAAAkAAAD+AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAAAAJAJAADLwJwAnAkACQAAAAAAAADgmsoAAAAAAAAAAPqQygsAkACgAAAAAAAAAAAMkKwAAAAMANCQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkACQAAAAAAAAAAkJqQAAAACgALwAAAAAoJAACQ4JAAAAAAAAAAAJwKkAAKAKAAAAAAAAAAAACQAJANoAkAAAAAAJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAAAAkA6QAAAKwAAAkAAAAJAAAAAAAAAKAJoKmsAAAAAAAAAAAOvAAAAAAAkAAAAAAAAAAAAAAAAAAAAAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAAAMkJCwwAALAPAAnAAAAAAAkAAACcCaAAAAAAAAAAANqaAAAAAAoAAAAAAAAAAAAKAAsAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAAAAAAA2gAAAAkAoAkADLCwAAAAAAoAAACgrAAAAAAAAAAAAPDAAAAAAADAoAAAAACgAAAAAAAOkAywAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQwAAAAAAACQAJ6QAAoJwAAAsAAAAAAAAAAAAKAJCaAAAAAAAAAAANoAAAoAAJAJAAAAAAAAAAAAAACQAJAAkKwJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAACQAAkJAACQAJwAAAAAAAAKAAAAkAoAAAAAAAAAAAAPANAACaAACgAAAAAAAAAAAAAAAAAAAAAJAAkAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAAAAAAAwKyQAAywAAAAAAAAAJAAAACgAAAAAACgAAAAAK8AoAAAyvAAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACakAAAALAAAAAAAAAAAACgAKAJoACpwKAAAACgAPAAAAoJAAAAAAAAAAAKAAAAAAAAAACQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAkAAAAAAACQAAkADAAJAAkAAAAAAAAAAAAKAACQAAAADAoJDwALCcAAwAAJAKAAAAAAAJoAAAAAAACgAACwDAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQ0AAAAAAAAAAAAAAAAA4L2pCeCQC8AAAACgAAAAAAAAAAmgAAsKkAoAAAygAPAAAKAAAAAAAAAAAAAAAAAAAAAAAAmgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAAAnACQAAAAAAnAAA4AALwAAAAAAAAAAAAAAACgAAoAAADwwAAAsAAK0AAAAACwAAAAoAAAAAAAAAAAAAAAAJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAAAAAAJAAAAAAAAAJAJAJAAAAAAAAAAAAAAAAAAAAAACQqeALAAAADrwNoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACcAAAAAAAAAAAAAAAAAJCQC8oACaywAAAAAAAAAAAAAAAAAACgngwAmgAAAAqQoKkAAAAAAAAAAAAAAAAAAAAAAAAAAAoMCQCcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACcAAAAAAAK0AkAywkAAAAAAAAAAAAAAAAAAACQoLCwrAAAAJAPANoAAAAAAAAAAAAACgAAAAAAAAAAAACQoAAAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAAAAAAAAAqQAJyg8JCwAJAMAAAAAAAAAAAAAAAAAAoLCg0AAACQAAAK2gAPAAAAAAAAAAAAkKAAAAAAAAAKAAoJAAAPAKwLAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAmgAAAACQAAANAAsAAAAAAAAAAAAAAAAAkAwMvKCgAACgAAAAAPAPAAAAAAAAAAAADJAAkAAAAAAAAAAACgkACQAAycAAAAAAAAAAAAAAkAAAAJAAAACeAAAAkACQAA8ADJwKkLDAC8AAALwAAAAAAAAAAAAAAAAADKmpAJAJoLAAAAAACwAA8AAAAAAAAAALAArAoAAAAAAADaAKCQAAAAAJAAAAAAAAAAAACQAAAJDAAAoAAAAAkJAAAAAACQAJAACcAACwkAALDwAAAAAAAAAAAAAAAAAKCpAACgAAAAAACpAJoAoPAAAAAAAACgAACwCQAAoAAAoJoAyQDKALAAAAAAAAAAAAAAAAAACQAAkACQANAMkAAAAAAAAAAAkKCwAAkAAAAAkMkAAAAAAAAAAAAAAKAAAAkACgAACgAAAAAAoMAAAA8AAAAAAAANAAAMoMCgAAAAAAALCgAAAAALAAoJCg0AAACQAAAAAMAAAAAA0AAAAAAAAAAAkAAAAAAAkKwJwADbywAAAAAAAAAAAAAAAAAAmgCgAAAAAACgAAAACgvLAPAAAAAAAAAKAAAAkKkAAKAAAAAAAMsAAADAAJAACQAKAAAAAACQAJAAAAAAAACQAArQAAkAAA0A4JDa2pAACQ8AAAAAAAAAAAAAAACgAJAKAAAAAAAAAACQAA6akAAAAAAAAAAAAAngCgAAAAAAkAAAAACwywAACemgAAAADAAAAJAA0AAAmgDAAAAAAAAAAJAAvArLDwCgkAAAAAAAsAAAAAAAAAAAAAAAAAAAoAoAAAAAAAAAAAAMkKkMrJoAoPAAAAAAAKCQCcAACgAAoAkAoAAAAAAAAAAAmgAAqQAJAAAACpDgAACQwLAADwAAAAAAAAkAAJAAAAAAAACQAMsAAAAAAAAAAAAAAKCQAAAAAAAAAAAADAoAoACgmgCpAPAAAAAAAACgwAoAAACgAAAAAAAAAAAKAKngAMCwAAAAAAkKnACQANCpAAAJAAAAAAAAAAAAAACQAMkJAPANqQAAAAAAAAAAAAAAAADgAAAAAAAAAACsALCamtDaAAAAAPAAAAAACQAJoAkMCQAAAAoAAAAKkACQCQALywAAAACwAOAAAAAACwAAAJDgAAkAkACQkAAJAAwAkAAACQCwAAAAAAAAAAAAAAAAAAAAAAAAAAAAkJoAsAygwKCgAAAAoA8AAAAACgAAAAALDgAAAAAAAAAACgoAAA68AAAMsAAAkAAMsACgwACcAKCQAAAAAAAAAJAAAAkAAKngvAvAAAAAAAAAAAAAAAAACgkAAAAAAJqawOALDpoJCpAACwAAAPAAAADpAA2gAAAAAAkMAAAAAAAAAJAAAKkKmgCwALAAoJCwALyQkKAAsNAACcCcAAAAAAkAAAqQDQCQAAAAAAAAAAAAAAAAAAAAAACgAAAACsAAqwsMAAAKAAAAAAAAAA8JAAkAAKAAAAAAAAoJoAAAAAAAAAAAAJ6cAPAADACQwKwK2grKCQ2gwAAMCgAAALAAAAANCQAMoAAAAAAAAAAAAAAAAAAAAAAAAAAACgCwALDpwAywmgkAAAAAAAAAAPAAAKAArQAAAAAAAAAAAAAAAAAAAAAAAACgsAAAsACgmgC8ANCQygAJALCQkAkACQAJAPAKCgAJAAAAAAAAAAAAAAAAAACgAAAAAAAJAAAAsACaCwoAoAoAAAAKAAoAAAoAAAAAAAAAAAAAAAAAAAAAAKkAAAAAAAsLwKAAAAAAAJAAmgoKkAkAqcCgAMCgAAwOCwC8nAAAAAAAAAAAAAAAAAAAAAAAAAAAAAoAoJCgAJoAAAAAAAAAAAAAAAAAoPAAoAAJAAAAAAAAAAAAAAAAAAAAAAAAqQDAqckACeAAAA4KAAycCsCsAAAMCpDAAAqQnA8KAAoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKAJCgCpCgmgAAAAAAAJAAAAAPDwAMsACgAAAACsAAAAAAAAAAAAAAAAALCwCgoAoAkJCgkJDpoACQAAkKkLAAAAAJAKCwDwAADaAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKAAAAAAAAAAAAAAAAoAAAAPAACaAKAJAAAAAAkAAAAAAAAAAAAAAKDAAA8AAAAACgCQoOAAALAKkAoAAAAAqQAAAAwOkACwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8ArADJAAAAAAAAoAAAAAAAAAAJAAkAkAraAAAAAAsMvLypAAAAAAAJANoAAJAAmgAJCwAADAsACgAKAAALwAAAAAAAAAAAAAAAAAAAAAAAAACgAAAAAAAAAAAAAAAAAPAJCpCgAAoAAAAAAAAAAAAAAAAKAAoACpAAALAACwywCgmssAvAAACgCgAAAAAAAAAAAJAACwwAAADQAAAAmsAAAAAAAAAAAAAAAAAAAAoACpAAAAoAAAAAAAAAAAAAAK2gAAAA4JAAAAAAAAoAAACQ2prADAAAAAAAkAAJALALCaCQAAAACQAAAACQAAAAAAAKAOCpAKkAkNoAAAAAAAvAAAAAAAAAAAAAAAAAAAwAAAAAAAAAAAAAAAAAAAoAAPAAAAqQkMAKAJCgAAAAAKkKoMAJqQCaAAqaAACgAArQysngAAALAKCQCQoAAAAAAAkJCwkADwCgCgAAAAAKCQAAAAAAAAAAAAAAAAAACwkAAAAAoAAAAAAAAAAAAAAAAArAAAAAoKAAAKCQCQAACQypyaCaAKAAAAAACgkAqemgsJoAAAAAAMAA4AAAAAAAsMoMrArLAAAAAAAAAAAACgCgkAAAAAAAAAAAAAAJDAoAAAoAAAAAAAAAAAAAAAAAAPCaAAAAAADQAAAAoAAACpqQoAAAwAAACQDAAAAAAAAAAAAJoJoACamgAAAAAAAACQAJC8kA6QAAAAAAAAAAAAAAoMAAAAAAAAAAAAAKC8AAAAAAAAAAAAAAoAAAAAAAAPAAAAAAAJCgAAAAAKkAAMCtDwCwsAAACssKkKAAALDwALwAwAyaAADJCwALAAAACgmgAKCwCgAAAAAAAAAAAAAAAKAAAAAAAAAAAAAAAJ4AAAAAAAAAAAAAAAAAAAAAAA8AAAAAAKAAAAAAAAAKmpraAAAAAAAAAAAAAAAAAAAAAACwCwoAAAsKAACgAAAAAAAAsJwMvAAAAAAAAAAAAAAAAACgAAAAAAAAAAAAAKAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAoLAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBQAAAAAAAImtBf4="",
    ""Notes"": ""Andrew received his BTS commercial in 1974 and a Ph.D. in international marketing from the University of Dallas in 1981.  He is fluent in French and Italian and reads German.  He joined the company as a sales representative, was promoted to sales manager in January 1992 and to vice president of sales in March 1993.  Andrew is a member of the Sales Management Roundtable, the Seattle Chamber of Commerce, and the Pacific Rim Importers Association."",
    ""ReportsTo"": 0,
    ""PhotoPath"": ""http://accweb/emmployees/fuller.bmp""
  }
]";

            var actual = ReadParquetFileAsJSON(filePath, 2);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Issue144_UsingFieldLevelValueConverter()
        {
            string filePath = @$"emp.parquet";
            var dbFilePath = Path.GetFullPath(@"..\..\..\..\..\assets\db\Northwind.mdf");
            dbFilePath.Print();

            string connectionString = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={dbFilePath};Integrated Security=True;Connect Timeout=30";
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            SqlCommand command = new SqlCommand("SELECT * FROM Employees", conn);
            using (var r = command.ExecuteReader(CommandBehavior.CloseConnection))
            {
                using (var parser = new ChoParquetWriter(filePath)
                    .WithField("LastName")
                    .WithField("FirstName")
                    .WithField("ReportsTo", valueConverter: o => o == DBNull.Value ? null : o)
                    .Configure(c => c.CompressionMethod = Parquet.CompressionMethod.Gzip)
                    .Configure(c => c.RowGroupSize = 1000)
                    .NotifyAfter(1000)
                    .OnRowsWritten((o, e) => $"Rows: {e.RowsWritten} <--- {DateTime.Now}".Print())
                    )
                {
                    if (r.HasRows)
                    {
                        parser.Write(r);
                    }
                }
            }

            string expected = @"[
  {
    ""LastName"": ""Davolio"",
    ""FirstName"": ""Nancy"",
    ""ReportsTo"": 2
  },
  {
    ""LastName"": ""Fuller"",
    ""FirstName"": ""Andrew"",
    ""ReportsTo"": 0
  }
]";

            var actual = ReadParquetFileAsJSON(filePath, 2);
            Assert.AreEqual(expected, actual);
        }

        [ChoTypeConverter(typeof(int))]
        public class ChoDBNullConverter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value == DBNull.Value)
                    return null;

                return value;
            }
        }
        [Test]
        public static void Issue144_UsingCustomConverter()
        {
            string filePath = @$"emp.parquet";
            var dbFilePath = Path.GetFullPath(@"..\..\..\..\..\assets\db\Northwind.mdf");
            dbFilePath.Print();
            //C:\Users\nraj39\source\repos\ChoETL\src\Assets\Db\Northwind.MDF
            string connectionString = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename={dbFilePath};Integrated Security=True;Connect Timeout=30";
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            SqlCommand command = new SqlCommand("SELECT * FROM Employees", conn);
            using (var r = command.ExecuteReader(CommandBehavior.CloseConnection))
            {
                using (var parser = new ChoParquetWriter(filePath)
                    .Configure(c => c.CompressionMethod = Parquet.CompressionMethod.Gzip)
                    .Configure(c => c.RowGroupSize = 1000)
                   .TreatDateTimeAsString()
                    .NotifyAfter(1000)
                    .OnRowsWritten((o, e) => $"Rows: {e.RowsWritten} <--- {DateTime.Now}".Print())
                    )
                {
                    if (r.HasRows)
                    {
                        parser.Write(r);
                    }
                }
            }

            string expected = @"[
  {
    ""EmployeeID"": 1,
    ""LastName"": ""Davolio"",
    ""FirstName"": ""Nancy"",
    ""Title"": ""Sales Representative"",
    ""TitleOfCourtesy"": ""Ms."",
    ""BirthDate"": ""12/8/1948"",
    ""HireDate"": ""5/1/1992"",
    ""Address"": ""507 - 20th Ave. E.\r\nApt. 2A"",
    ""City"": ""Seattle"",
    ""Region"": ""WA"",
    ""PostalCode"": ""98122"",
    ""Country"": ""USA"",
    ""HomePhone"": ""(206) 555-9857"",
    ""Extension"": ""5467"",
    ""Photo"": ""FRwvAAIAAAANAA4AFAAhAP////9CaXRtYXAgSW1hZ2UAUGFpbnQuUGljdHVyZQABBQAAAgAAAAcAAABQQnJ1c2gAAAAAAAAAAAAgVAAAQk0gVAAAAAAAAHYAAAAoAAAAwAAAAN8AAAABAAQAAAAAAKBTAADODgAA2A4AAAAAAAAAAAAAAAAAAAAAgAAAgAAAAICAAIAAAACAAIAAgIAAAMDAwACAgIAAAAD/AAD/AAAA//8A/wAAAP8A/wD//wAA////AP8MsMkACwkJAAAKAJAJAAAAAAkJoJqQCwkACpCgAAAAD//v///////////LnPz++v/////t//7e/97+/P//2toA2QAAkAkAkAAAAAAJCgAJC8AACQCQAAAACgCsoODg4PDpypAAqcsMAACQkOAAAAkJCwAA0AkAkAAACQAAmgAP///////+//////yt69vf39//////7+3//v////78rwyaCg0AAJoAAAAAAAAACQkKAAsAmpAACQAAkAwJAJAJAPqQraAAkLALAAAAAJAACQAAAJAJqbAJAJoAAAAAwAC//////v///////8+fvL77/+v////+/f/+/f/P/v/5vAsAkJAAkAAJAAAAAAAAkAAAkJAJoMCwAACwAAmgDg4ODvytoMkAAMsAkAAAmpoJoACwCQCQkA0AqQkACQkKAAD////+//7///////Dw/9/trf3//////v797+/8/e/sDwyawAoAoJAAAACQAAAACQqQCgAAkJAACgAADKAMCQCQkPkA2gCakADaANAAAAnAkAAAkLAAqamwkAAAAKwAAAv/7///////////7anfD/n////w//////3+/97/79/5qaAJCQkAkACQAAAAAAAJCpAAkJqQAKkNCQAAqQALDgysoPrpqcoACpmpCwAAAJCwAJCQAAkAkNAACakAAJAAAJ/////////v///+2t68/e/9ra3//////+/vy+/e2+/K0NDgCgAAAJAAAAAAAACQCQCQAAAACQAKAACQAAAAAJqQDfCQypAJCQ6QkAsACQqQCQAAkLAJoJqQkJAACQsACf///////v/////9CenPv78P///v/e/////9/9/r/v2pCwqQkAqQCQAAAAAAAAAAAAAAkJAAkKkJAAoAAKywCwwOCvDwvAkAAAmtqQkAAAkPAACQCQkAmQmpoJAAAAAAAP/+///v/+/+///qnp+/3t//3//f///P///v7+38/e/AANAACQAAAAAAAAAAAAAAkAkACgCQCQAAsAkAAJAAwAoJwP8PC8oLCgAJDwqakACQsJAJAAkAALDQkKkAAJoAC//////////////Q+enw++8Pr8v+/9//////3/r/r577wKnAoAkJAAkAAAAAAJAAAAAAkAAAAAsAAAAKAAAAsMkKAPCw8AnAAJAAsAnAALAK0ACwCZqQmpCwCZAAAAnAD////+//7/7///ypDw/P/f3/3//f2+//3///78/P3+/ACQCpAAAAoJAAAAAAAAAACwAJAJAAAJAJCayQAAqQAKDAnPAPANoAkAAJCfC5CcAJmgkAmgAAAJCQmgCQAJoJ//////////3//umc8P2+nvr8vp777//ev9v////w/t8KkK0ACQCQkAAAAAAAAAAAkAkAAAAAAACaAJAAkAANoJAKCvra2wCayQAAAAkACpAJrQAJAJCQm8sJyZAAkACa//////7//v7+/9rLD9rf+9+f/f39/f///+///+/PD77Q6QCcAAAAAAAAAAAAAAAAAAAACQAAAAAAmgAAAJCwwA6cAP2pDprJoKkKkKmpCa0Am5AK0KkACQkLCpAAAJC9/////////////AmtCw8L7a/p76++v9r9//2//////P/pAJoLAAAAkKkAAAAAAAAAkAkAAAAAAAkACbAAAAAAsAAK0PreuekADQAJAJDQsJmpoPCQCQCQkLCQmQAAAACe///////v//z/wPDa/Pn9n9+f+f39/v//79/t///vz/7engkAAJAAAJAAAAAAAAAAAAAJAJAAAAAJnACQAADwAAkAAP263K2pAKkAAACwCcqcCZoAsAkKnpALAKAAkAn7/////v//7+/8CwmtCa3r7+nvD8vvn73//////////+/wAJAAkAAJAAAAAAAAAAAJAJAAAACQCQCaCpAAAAkAAArKCvrtq/mtqQDanJCempmpyamZCQCQCQmQmQAAAAC8//////7//f/LwPDantv9+f+d+/+f7f/r3/7b///+///sCaywAAAACQkAAAAAAAAAAACQCQAAAJAAkAAAkACgAAAA0P2+2s+8vLoJoKAJCaybqZ4KmpAJCwC8qQAAAACb/////////v8AkJ6cue2vzw/rzw/p+8/f+///z///////4JAJAAAAoAAAAAAAAAAAAAAAAAAAAACaCQkAAJCQAAAAC//p/byem8kAkJkAsJsNmgmZwACQkJoJCQAAkAkP///+///v//yQqQCw2tvfv/29uf2/z/v/7f2/+//////+kAsMAJCQkAkAAAAAAAAAAJAAAAkAAArQkKAAmgAAAJCaCfn/6+vr2tutC8oJwLywvJsAqQkLDwmQsAAAAAC5///////978oMnK0Nrb6+2tvtr8v8vbz9/+/8/f/////tAACakAAAAJAAAAAAAAAAAAAAAAAAAJAAAJCQAADwkAAA2v6tv9vfD60KkJCwsAkLC9C5kAAAkAAJAAAAkAkP/////v/v/9CaAJCw8P35/+2/37y///+v+//7+///////4JAAAAAAkAAAAAAAAAAAAAAAAAAAAAkAkAAAkJAAAAqamvna2vy/vL+fCw8JC5qckLkOkJCQuQmwkAAAAACQv/7/////7+AJCa0Ly9r+n9vL6f/a2v35/f3/z5//////wAAJCQkACwCQAAAAAAAACQAAAJAACaAJqQAAAAsJ4JAMkP6trfvw29rwvAkPkAALCw25CwAJAAAJCwAAAAAJ///////v/wkMCsmtva/b/r7/3/D//9v/7/v//+/////+sAAACgAAAAAAAAAAAAAAAAAAAAAAsJAAAAkAmpAAAACQqfDfrw/p6+vb6bCwvAkL0JqakAmpAJCwAAAACQCb////7//9/AAKkJD5772+2/35/p/72+//2/z/n5/////9AAAACQAJCQAAAAAAAAAAAAAAAAAJwAAKkJoAAACQCaAKnP6g0P2/vJytvwkNCwCZCwm56QCQsAkJCQAAAAAAD+/////v7wAJDa8Pnt7b/Pv+n/rf79/L/////vn////+kAAJAAAAAAkAAAAAAAAAAAAAAAkLAAAJAAAAsPCgAA8NC/3/r6/tv7+envC6mtoKnJrQmgkACQAAAAAAAAAJvf///+///gkAqQnw+/v9v97//9/737//6f29+f7/////wJAAAJAAAAAAAAAAAAAAAAAAAADwAACQmpCQCQkJrQAAAPvv3/2/6empub0J25DQmwkLkJAJAAsJCQAAAJCQD//v////7QANDLy9re3v//+f2v+f/t+f3+/v//n5///5sAAAAACQCQAAAAAAAAAAAAAAAAsJAAkACQAAnwrAAACgvPzwvtr/n5rfD/qasAsJ6amp6QCQkAkAAAAAAAAA++/////t8AkAmw8P37+/y9/+/97+37/++/n73w/+///+AAAAAAAAAAAAAAAAAAAAAAAACfAAAAAJvKkAsAkJoK0NCfqeya+e8K260L35yfCwkJCZmpAKCQAACQAAAAAJCf///+/+/ACw4Pn/r9/f//r9v/vfvfz5/P/fv/vb///9mgAAsAAAAJAAAAAAAAAAAAALywsAAAmgCQAJ6eAACQCgoP/L/tr5v9rbvwsLsJnanwvLDQAJAJCQAAAAAACQsN//7///8AAJnw8P36/r3/3//P///r//+968+fy9rf/60AAAAAAAAAAAAAAAAAAAAAAAkNAACQCQsAnwkJCwDAkNCf6/y62+8L2tqf28n6C5AJAJmpCQAKAAAAkAAAAACa/t///vwJCQ6fn769//+////73p/98PD/vfvvv/2///CQAAAAAAAAAAAAAAAAAAAAC5qaAAkAkJAPoLygAAsLDgoPDwvt/vm8ra36mpqQmembC5qQAAkAkAAAAAAAAAmt///e/9oADwva/P3/+f/P/p////+ev9/63r/by8vp7wmgAAnAAAAAAAAAAAAAAAAAkMCQCQCpALD5CQAJANAAwJDfrP7a+fnr25qb29vbC5qQmQ0LCQCQAJAAAAAAAJCa3v7//vAJAJ8Pn7/L/v//n/+fz57/2vD9v9D9vb2fn/AJAAoAAAAAAAAAAAAAAAAAsJDpAJCQucmgDgkMAKAAsMsP2w8PD76cvPntqamtvLmtoLCQmpAAkAAAkAAAAAAJv9////wAmsva/ev/372//97/v/n6/f8Pnr++/vra2+kAAJAAAAAAAAAAAAAAAAAAmgkAAAkLCr0JCQCwqQDwyw6fDtr56em7y56729vbCa0JmQkAkACQAAAAAAAAAACaD//v7wAADby9+/3w+97//7/97f79+trf6fy9vZ+9C9CwAAywAAAAAAAAAAAAAAAJyQqQCpC8nQCgALAAnAsJrAkPC5ra2/C8u8udrampC9qbCpoJoAkACQAACQAAAAkJ2+///eCQ2w+/vt6////9rf37+/vfrb/628va2++QvQCQAJAAAAAAAAAAAAAAAACaAAkAmZqQsKCQuckPALwKwLDvsMCa2p/b/Lnrub29uQuQmckAmQCQAAAAAJAAAAAArf7f7wAAqfD8/b39/f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oA8ACakLCgsAAAAAAAAAAAAAAAAAAAAAAAAJCQkLCamtC9rfn/////Cp+p8Py9rb6fvp6amfCQubwLCakAkJAAAJCQAAAAAAAACQsK2/v//////9/+3/2968va0J4JwAkACaAAAMkMAAAAAAAAAAAAAAAAAAAAAJoJAACgCQmp8LvL2v/9////CfCenpva+/nw+bm9rwmpAAmQAJAJAAAJCQAAAAAAAAAAAACdvf/////////5+frb3fD5rakOmpDLAMmtqQAJCQAAAAAAAAAAkAAAAAAJCQAACQkJC8kPDw2fqfvb///7Cenpqb+vn57b+tramZqckLmgmwCaAACQAAkAAAAAAAAAkJn7///////////f/v3/6w/a2QnpAMqQCQAAAAqaAAAAAAAAAAAAAAAAAAkAAACQAKAAsAranpq+n9++/f/fAL8L2sn5+/u8vbm5y8kLCQAJAJCQCQAAkJAAAAAAAAAAAOv/////////////n5+tvfmtrPCa2pAJ4LCQkJwAkKAAAAAAAAAAAAAAAAAKkJAJCQkNCQmtsL3pr7ntv///AAvan7y+28+/mvywuakJsJCQkLAJAACQAAkAAAAAAAAACZ+f//////////////3/y96fmw8JCcvAkMvKDgkArQkAAAAAAAAJAAAAAACQAAAAAAALALyw+Qvb2t6w6fy+AAmtravbv7n5/bn5C9C8CQ8ACQkKkAAAkLAAAAAAAAAAu///////////3//fz569va3w/NmtraCbypAJCQoJAAAAAAAAAAAAAAAAAAAAAJAJCQsACQkPAPmt6akPnr/rAJrb2tn63w8Pqbyw+QuQmwkJmpAJAACQAAAAAAAAAAAL3//////////////7+/3a3tsP2wrQmsmsCQy8kACcALAAAAAAAAAAAAAACQkAkAAAoACQkAvLC7DwsPDw/vwPAACgvb6f+/n5+tvZCwkLAJCwCQqQCQALCQAAAAAAAACb///////////////f39r/n7zwvL2a0LyQvKkAoPAKkAAAAAAAAAAAAAAAAAAACQCQkLAAoJC5ycsPDwmrD9v+kLyQC9vp/a8Lnwu+mfDQkLAJCQkJAACQAJAAAAAAAAn/v////////////9///L2f6fvfD8vprQvLCdDw0AkAAJAAAAAAAAAJAAAJAAAAAAAJAAkJwLDasLzwsLycsK2rDAsKnp6fv7296fnJvpsLCQkKkLCakAAAkAAAAAAAAJ+9////////////////2//vnw+p+byQ2pyw2goJoLypygCQAAAAAAAAAAAACQAJAAmgqQAAkAmpy8ucvAsLD9rfALwNvL+9r56wubC/CZAJCQCZAJCQAAkJAAAAAAAACb///////////////f+ev8n57fCfy8mtqcsNqemeDQCQCcoAAAAAAAAAAACQAAAAAJAAkAALALy8sPnqkLDp6amvAACwCw2v3/n9vp+QvKmanJoAkAkAkAAACQAAAAAAn//////////////////9+f8Pmp3gmtrbyfDa0JygmtrLAAkAAAAAAAAAAAAAAACQAACQAAkACQkPCQ6ZDw+bDw8PAJwLyan7+/++nwv5uZoNCwkJqQCwCQCQAAAAAAAAm9v///////////////D/np+e3auekLkOmgsJranJ6QkMmgAAAAAAAAAAAAAAAAAACwCpAACQALCw6bysqfDg8LC7AAqQoJ6cvfD5+b0PDa2QsJAAkJAJAAAAkAAAAAAL///////////////////56/D5q9Dw8MD5y9Da2p6anLy5rQ2pAAAAAAAAAAkAAAAAkAkACQCgmg0L0KnbngufC8kPCgnLDanr+vvb+vCwmpsLCakJCQCQAAAJAAAAAAC9v////////////////9va2ckJDQ8AnpuQrQ+tqcmpypAAwLAAAAAAAAAAAAAAAJAAAAAAAAAJAJC8Cp2g6fngCwvvyQC8C8ufn5+8vb25+QkJCckAALAJAAkAAAAAAAC///////////////////+dmpmgkJCfCQytmvCenpraCenpqQCekAAAAAAAAAAJAAAJAJAJAAkACQvLDQqfmtrQ8PCbCskLDaDa+fD7y8vLD56a2pCakAkAkAAJAAAAAAnb3//////////f/////9vpyQ2dCaAAnpsK0J6Z6ekNvJqcnp4AoAAAAAAAAAAAAAAAAAAAAAAJCgCcsJ2g+/ALCw8NALAPCp+p+vv5+/n58LkJCQvJCQCQAAAAAAAAAJ+//////////9v/////2fyfm9qQvJ0JAAycDwnp8J6aC8vKkAkJDQAAAAAACQAAAAkAkAAACQAACQmgCaC9vKngnpAKCfywCZ6cv5/a29sLC56QsJCQAAkAAAAAAAAAALv/////////n7/////b3/n5DQkNkJAACQmpsLyw8PDw0JCdrLy8oAAAAAAAAAAAAAAAAJAJAACakA6Q2g+enp8J6Q8NAAsPAKm/n/r/va+fnLm9CakJqQCQkAAJAAAACd/f//////////37//29uZyekJCQCQkJAACQycvfD56QvK2gCQAJCcAAAAAAAAAAAAAJAAAAAJAAAJALCZnp6eDamgALAAnp6Q6frb+fq9vpqcsLkJCwkJAAAAAAAAAAAL+/////////+fr9/9vQ0AmZD5y7yeCQkJAJCpCtvL2+kNDa0PDwoLAAAAAAAAAAAAkAAAAAAACQkKkJoOkPC5oPDJANoACemsmp+/373629+bD5ywkJAACQAAAAAAAACb3///////////2/+QAJqfvv/++cvgkAAAAACQ+a28vJ6ampCwkJ0AAAAAAAAAAACQAAkAAAkAAAAJC8kJra0OnAkAAKmgCwvbC8veu/+9v6mwmasJDakJAAAAAAAAAAD///////////2trQkAmb8P////mv8J4PAAAAAAkPmtqanJ6csMvAoPAAAAAAAAAAAAAAAJAAAJAJCQ8LDa0PDpCw4AAPAACa2t6b+//fnr258PC9nakJAAkJAAAAAAAAm9v//////////72pAAAP37/b/ryengkAAAkAAAra/b3NCwmpypCpyQAAAAAAAAAAAAkAAACQAAAA2gkNoJqQCaDJAAAJ+pDAmpvA/5+v+frfub0LqZqQmQAAAAAAAAAAC//f////////+f+crQCQsNrZvQsJCQCQCQAACQkJraq8vLyena0KnpoAAAAAAAAAAAAJAAAAkAkLCQqQkJ2p4NAAAAAOkAqayeC/n7/9v/263pC528kLCpAAAAAAAAAAvb//////////3/npmQkJCZkAAJCQAJAAkAAAAAC8mt2a0AmpCpqckA0AAAAAAAAAAAAAAAAAAACgAPCaywoMkAoACQCbCpAAmgv57/z7/wvZ+5vQsLkJCQCQAAAAAAAAn/+/////////vw////mQkPCb2ZyQCQCZAAmpDa0AvbCtC9vJ6cngoPCgAAAAAAAACQAAAJAAAAkJCQmtsJDbCwyQAMnvkKmtrQAPufvfn/+vm8sL29qekJAAAAAAAAAJuf3/////////3/vb///9+ZvJCakJ0LwMkLwNoJC9C8vQ8ACpCaCZyQkAAAAAAAAAAAAAAAAACwAAoLywAAsMAJAACw8A+gCamskJ7/+/6fvby52wuakJALAAAAAAAAAA/7//////////+f+t//////29vJ/anJua2tsA0OAK0Lywn5yenp4KmsDwAAAAAAAAAAAAkAAJAJCQkJrbyQyanKCpDLy/ALCgCpoAufrfv72/vamtmtuakJCQAAAAAAAAm/+///////////n/v///////3/C9+94NCQDaCwn5rcsPAAsJCQmcDQsAAAAAAAAAAAkAAAAAAAAACamgkKmtCwkMqQoJCwALDQALD//728v5y5+Zrb0JDQAAAAAAAAAJ/9///////9v9v5/b3///3/3/+wn8vLCZ6emprQ8A27DQnpDQ8PDgsLDAAAAAAAAAAAAAAACQCQCanpyQmpAAvAyg8OkPoAqQsLwA2/+e//8PvamtubD5CakJAAAAAAAJv//f/////////ev///////+//J6b28ngkA0NCanbrA+ememgkAkJAMkLAAAAAJAAAAAAAAAAAAAJCavLDADwmprfrwCw2pCgCwsKAL/5+fv9qfma2tsJqQAAAAAAAAAAn7+////////9v58Pn9/////f//np8LCQqbCw8NoNm9Cp6QnLDwyw8JrQAAAAAAAACQAAAAAAkAkAvJCQmpkADQr/37wPqQqQsNrQmt6//r37/wvJubvakJCQAAAAAAAJ/f////////37/fnb////////29CfDQnpwMkNCw2w8L/QkPCQ0JqcCeALwAAACaAAAAAAAAAAAAAJCw8J6QoJoL3//ssLAKAKALC8oJvem9+/+b27Db0JCakAAAAAAAAJv73//f//////362/y///////v8vpCeAAC5rQsNsPCckLDwC8sKnAsAnwAJAAkAAAAAAACQCQAJC8vakPCayQy8//7byssAsJqa8PDw+//6np/+sNupv56QCQkAAAAAAL3//////////b+9vJvb/f//3//b2ekAkJAACw0Ly5y7y8kL0JDJCwDaAAnACQoAAAAAAAAAAAkAAJCpywvAvLAPD9+ssLywCgAJCw8PD5/tv/v5+w28sJkJoAAAAAAAAAvb/7////////35+8m9////v9+/+Z6QAAkMkLDQnLnJvby8ram8DQsJ6QqQngnAAAkAAAAAAAAJCwmQvJy8C8sL8On54OsAoKmgoPCa8Pvbyw+fvbuZ/7CwmQkAAAAAAJv//f//////////nbnLn739Cbn9sPkAAAAJAAmg+Q+enpnLkJwJsAkMkOkLAJALAAAAAAAAAAAAAJ6ekLAJrangC8oKCZALCQAAmpoAn62v+f//2tD/uQ0JAAAAAAAAAL29v/////////29vpy9///5n5/bD9DpmpCakNvJkPnpy569DLCwwPCpoJAAywnwAAAAAAAACQkJCempCwy8vLywsLDwmvsACgoKCayeCf/9vtr7/7+an7mtCwkJAAAAAJv/////////////35sJCf//////2/vayQ8NoACfDw+fvenQucnLCQyQCcrakOAJAAAAAAAAAAAAsJqevL6a2ssKDKAKAA+pAJCQoJoJoJr627/f+9v5+ekJAJAAAAAAAJ29vf//////////vf35+f///////a25rJCw0JqQvby8mtqfy5qcsLAJ4JAJCQng8AAAAAAAAACQC8kJy8vPCw6csJqQsPAKCgoKkKCwCQ+f/9+/vf6fvp+akACQAAAAAAv//////////9//378PD5//////3//NmenLAJyemtvb/b26nA2wnAngkKkMoJoJAJAAAAAJAAkAkJrakA/6zwsKAKAKAJCwCQCQCgkK2vD7y/r//729vbkJCakAAAAACZ+fv//////////f+/35+f///////9ubDpywCwnp/a2tC8vJ29sMsLCQsNCpCenQ+eAAAAAACQAJqa2prL2t8LDwsLy8sPoKCgoKCQqQsJrb/9//n/v6+/CwkACQkAAAAJv9///////////7/fv/2//9//////7ema0MkNqdqfvb/fn+vLybycqcAA0AkJCpAAAAAAAAAACw0JqcC9raD8oA6enp6g+QkAkACgCgCpAMv7+f8P/5/b/byZAAAAAAAK2////////////9//37/b3//////f294JqQCa2p8NrQsPCZ2w8MsJALywqcoAsPCQ8AAAAAAJAAmtmr2vytoL27Dw+v3/CgoKCpoJoJqeCwvP//v7/fu/mwmgkJAAAAAJn9v////////////9/9//////////vanawLC8kPD72/35/+npD5Da2gkJAKnJDQvLAAAAAAkACQsL7QvwCQDwoMsPDQ6rALCQCQAKCaAAkA//v//9+/ntvLkJAAkAAAAJ+//////////////b//+/////////3p2gmcnLv/np2tq8sJD5+a28kJysAJAACa0ACQAAAAAJoLCfmvwNDr2prbrL6vvcsAoKCgoJCgsLDpAP/5/7/7+7+a0JCQAAAAAJv////////////f//+9//////////+9rZ4AsJyQ+evb/b3/+emtkJraCZoAkLAAC9oAAACQAAkA+trQsKkMDwmg2wkACrywkAkAmgCQCwsOsJr///v9/fvbmpoJCQAAAAmf/////////9////3//f///////9/L2gm5y8v56drfD9qdr57a6ekNoMkAAAkJAJyQAAAACQCQn62sAJCwsPCaAOC8kJoAoKCgoAoKmgCwDK37/9+fv5rbyZCQAAAAAJv///////////+/29/5//////////+9rbwMsLCenr2w+a362em9npra0LAAAAAACamgAAAAAAsNsJqakMrA8AvJrQsKCukKCwkLCakJoJra2gm9+/v78L/7kKkAkJAAAJy////////////f/////////////9/LyQCbDQ3p+dvP2t+p2vnp6a0JqQwAAAkAkJwAAAAAkJCavLwACgkLALAKkLAJqZ4JAAoAoAoKCwsKkADr///an5mw+ZCQAAAAAJvb/////////////fv9///////////5Dw8MmtueD+2wvanevQ+emtq8npqQAAAAAKngAAAAAAD5ywvADQAAmsmtrA8OAKmgqQCwmpAJoKCw6QkNv9v/m/Dbnp2pCQAAAAm9//////////////3//b3///////nv8JCw0Ky9ub79r9qZ69rb3p2p6cAAAAAJCQkJAAAACQsKvAAJoACsoLCampqQsNoJCgoAoA6wy5ypCgAL/7/56Qu56ZqQAAkAAJCfv///////////////v//////////5Dw0JqdvJ78kL0L3pnK2suenLCakAAACQAAvAAACQkACdCwCaAJAJCQDp7awKwKkKAAkKCakAsAqcoJALy9v/vbkNuaCQCQAAkAvb//////////3//9//3////////96c8JoKnay60Jv+y9qay/m9vLy8npwAAAAACpyaAAAACpsKAMsMkAyaCgqamgmpC9oAqaCpoAoLCamgmgkJ//+/mtC5D5kJAJAACQC5////////////D6/b69/f///////7DQDZytvNrawJm8ntucvLy8sJ6aAAAAAAkJoMAAAAkADJqQAACpoACQkNrJ4KAK0LAAAACwng6tqaCaAAC9rf+bkJua2wkACQAJkPm///////+9vp/by936+/3////9vNsLCgsAmp2tvKwKkA+py8va36nJ6QAACQAMmwAAAAAJCwwLwLDAmgoKCwmgCQsPCgCwsLAOoLCaDpCtramv+/vp6anJsJAJAAAACbn//////9/f+fC8kKkNntv//////7wA0JDJ4MqQCQkJC5DanbyfCcupAAAAAAkLDJAAAACakAsACQCw4JAAkKnLCgAAsAoAAACpCaypqQqakAy9v9+ZkJCbyakAAAAJnw2/3/////+/D8vQvJD5rb//////y8kJCsCwkJnAvLysDAy568usnrycoAAJCtCpywAAAJAADLCcsA8AmgqaCtCgvJCvCwkLCgsKDgsOmpnprLAL36/6mwsJuQmwkJAACZqb/////5/f+b2tCakAkPn//////awKCQkACeCtAAkLCQsOnLDbrQnpyQAACQnLALkAAAkJqQygnpraCQAJCamtAKkLwAoAqQCQqQ6QoOsPC8vJrb29rQnLC8CQAACQmp28v9v/+/+8vACQAJCa2w+f////35kA0LycsJAACQAAAACQsJ4NC8sLAAAAAKCw8MAAAAAA2pvaAACgoKCgCpAKkAoMsKCaAKCgmgmtCwCgsLCwv/v/mbCQnbkAkAAAkJqb2//b/fDwkJ4KnAsNCdv7////+soJCgCwDa0LAAAAAAkACemw8Ly8sAAACckNALAAAJoJqcoJy+kJAJCQsArQ4LybCwCgmpAJoL4KvJqfDw8NC9vbywmwmpoJCQCQm5m/n72p2wkJ4AANAJyQrbD9///9rZCeCQkA8AAMAMAJAAAJDwkOkNDwwAAAmgDakJAAAACanLDwsAAKCgoKAKkKmgCvAJqaAKCgAAC8Cq0KmqCw8L+/ufAJCQmwAAAACa0J69vaAJD/8AwAAKAJAJ+/////+gAJAMAJC8AAAAAAAAAAAJ4J6w8JoAAAAJqQAAAAAJAJ68kPCpoACQAAqQ6wAJ6Q+gAJoAkKmgALCaC8vZvLD56f+bkLCfAPCQkAkJuZmevZAAm//gAArJ4AkAn5////39oAmpAADvCgAAAAAAAAmgnwnLDw0AAACQwPAPAAAAC8kL8K0AALCgsJCpAOmgoPALCgCwoAAJoA4MsLCqywvL+/nr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CpCQoMupAACgmgsLCaCQAKAKCQoACQvL/56b+fv/kJoJn5qanQkJDwkLn5vPuf+f29va+fvrn+2/vf/rz+vL372tvLrKkAAAAAAAAAAAAAAKnp6ayakAsAAKmgqQAKkAAKCgALyaCpAAoJoAoACgqQCQCgALAKCb2//9v729rQkACw25+5qQmb29Ca257wvbra352t+f75/P3629vb376erby8mcAAAAAAAAAAAAAAkNCaAAsACgALCgAAkKmgCpoAAJoPCgAAoKkKALAKAAAKCgsAsACwme//vav9r7makAmfsNsAkLDwmpu9ufm9vL358L//nt+e+/r9/e2v6e2928vJ4KAAAAAAAAAAAAAAAKC8nLAKAACgCQqaCgAAoAAJoKAJsAsACQqQsAqQsKAAAAAAAKAAoJqf+/y/8P+tqZoJ26n5AAmfna0J6enL29qen/Dw/7/5/f2/r7/b357avLyanAAAAAAAAAAAAAAAkJ8LoAoJqaCaCgAJALCgkKCgAAAOmgAKAACgCpCgAJCgkKCgoJoJAA2/37/b35/b2tngvdufmQCwuZufmb+by5+b2tvfn8v/vr/P39r8vvn96entqQkAAAAAAAAAAAAACgn8CwCgAAAAkKCgoACaAJAKCwoL8LCgCgAAsKCaCgAAoAkAkAAKC5oL//+/++n/rauZC6nrCwkJ2vCw+8nam8vPvby7z73p/f29vL+f3b6en56awKAAAAAAAAAAAAAAkJ6QvKkAoKCgoAkAAKAACgqQAAAOAACQCQqaAAmgkAoAAKAKCgoAkAyf+9vL/9v729Cem9+50JC5qZnLkJup/L27y8+8+9+/y/r/7/z/6+n58OmtDQAAAAAAAAAAAJCayevKAAAKkAkAAAoAsAAKCQAAsKmrmgoKCgAACwoKCgCwoAAAAAkAoLAA////v/8PvL256bn5qQnL2em9qenQufntvb/fva/Pvf+fn58Pn976357aCpAAAAAAAAAAAAAJCpAMCwsAAAoACwAAAAoJoKmgAAAMsJAAAAoLAACQAJAACaCwCwCgkAmpC/vb2//9+avLnp6fnpqQupCa2ZqfDw+by9r579v969z+8P/56fnPrantkMAAAAAAAAAAAAAAkLwLAACgCgAKAACgCpAAAAALAKALygoJCgAACgsKmgoAAAAAAAoAoJrAvf/+va2/r9+fD5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CgAKAKAAsAsACQCgAKAKCpqQoAkACQAAsLCgoAkJALCgsAAJoKAMoLCgCaAAAAAAAJCpAAAAn77/3/////vb7b2emvmcvp/Lufn9vdr9vLz96enp4ACgoAALAAsKAAAAAJoAAAmgCgAAAKAAoJoAAOCgCwoKCgqQAACQAKCgoACQCgoACQqQAAAAoAqaAKAKAKAACpCgAPvf//r////tvr+t+f652wvfzw8L+v2v29vgvL2sngCQCwoACgAAmgCgsAAACgAAAACgoAoAkAAKANvJoACQAJAKmgoKmgAAkKCgAJAKCgkKmtqaAAAACQAACgCgqQCQAAnr////2////fz77+n+vP+vv///z5r9r+2/y8oJoAsKAAkKkAoAAAkAAACgqQoAoKAJAAkAoKAJAKAKwLCgoKCtoJAAAJqaCpAAsKCpAACgAAAACwAKCgmgAACQAAoAAAAJ////7///v/+/29vb2/n/3/np++/a/5D8sPDawAAAkKCgALAKCgoAoKAAAAAJAJAAoAoKAAAKCtsAsAAAkAkADKCpoKAJAAmgAAkAoLALCgsKAAoJAAAAqQoACgCQoAAACf3////P/L3///7+////vv//7b2tvP8LwAAAALCgoAAAoACQAAAAAACQAKCgoACgCwAAAJoAAKywCgoKCgoLCwkKyQoKkKAAoAoAkAoACQANCgAKAKAJAAAKkACgAAAAAAr5//////////n5/////b+f/+/+/w4MvAAAsACQAKkAmgCgqQoLAJoKAAAAAKAAAAoAmgAKAPkKCQkAAAkAAAoAmgkACgCwCaCaCgCaAKCgqQoACQCgCgAAAAoAsAoAAAkA+f/////p8P////rfv/7//9//C/nwAKCpAKCgqQCgAAoAAAAACgAACwCQoACwoACaAAoJAKDpAKCpqQoKCwCQoACgqQoACgCgAJoACgkJAKkAoKAAAAoACgkAAJCQkAAPC8m//////////9//////3r/Ang4MsAkACpAAAAoAoJAAoKAKAAAKAACgCQoAkAoACgkKCvkMoAAACgAJAKCgCakJCpAKAAAJCgAKCQCgoACpAAkKAAkKAKAAoKCgCaAADL/Ly/n/////////z/3+qfDwCf2wDwoKkAqaCgAJCgoAkAAAAKCQAKAACgAKCpAKkAoAAMqwCwmgAAsAoJALAA4KAKAJqaCgAAqQoKAAywAAoAoACwAAkACgkAAKAACwCQrQ/P///e3///vb//6f3g/KAL7fAAAAoAAAkAsKAACgoAsAqQCgAAAKAAqQAKAAoAAKkL0KAAoJqaALCaCgCwmtqQmgAAkAqQAAAAmpoKmgAAAAoAoAoAAACgkACpAAsK0LCwvKD7//7w/vy8ngC8sJy8v62pqQALCgoKAACwCQAAAAAAAAoJoJCgAKmgCwALCgCsCgkKAKAACwCgyQsPrangoACgCgAKCgsKAAAAAAmgCgAAAAAKkKAAoLAAoKAJCskAy9+8sAkJ6QvLwAvJ4OsL2toAAKCgCQAAALAAoKCgCgCgoAAAAAAJCtAJoACgAJoLsAoJqQDprL6amvD5//8JwLCaAJoAkAAJoKmtoKAACQALCpoAAACwAACgCQAKCQrLAAAAAAAAAAAADwAKkJCsvamgoAkAoKmpoAqekAAAAAAAALAKAAoAqaC+C+kACgAM8JCgCgkKm9vLDb/////6mgrAmgAKCgoACQ4ACQoAoAoAAACwoKAAsACQoAsAoKkAAAAAAAAAAAAKALywoKCaCsAAmgoJAAAACwCp6wsKkKkAkAAACgCaAPAL3L6ekKkJoKCQ6QoJrb/w///////88JywoAqQAJAKCgCaCgkKAKAKCgAACQAKAKCgAKAJAJCpqaAAAACgkAsJywAAkAmgkLCwAAAKCaCgAAsPnwAACgCgoAoAAACg2gv8v/DwoACunpAKkKCa3//L2///////qeug2pAKmgqQAJCgnACgkAkAkAoACgoAAAAAAAAKAKAAAACgsJAACgAAoKCwoKAACgAAoAoACgCQqa378OCgAAAAAAAAqQoJq9/L/9/62poLAAqQoJypq9+/D7/////735/foAoAAACgsKAAoLAAoKCgCpALAACwCaAKmgoAoAqaAKCQAAoLAJrakJAAAJoLALCgAJALAACgAJqfywkAoAoAAKkAAAkKyev////fDgANupCpCgsA2vvLvt+/v//fv///2tCpCpAAAAmpAAqQAAAKAAoAoAAAoACQAAkAAJAAqQoKCgAAoKAAoKCgoAAAoAAAsKAACpAAoKnpvKAAAAAAoAAKAKAJq56f3///+fALwAqenLALCfDwyb/en76/3//56aAAoAqaCwoAoLDgsKkAmgAAALAAAAoAAAoAmgoJAAAAkJoPCQmpAAkAkKAACaCwAACgAAoJAA2toJCgmgCpAACgAACgAOn/////DgoMqekLCwCwvw8Nu+mpsPn5+//+2gCwkLAAAAALCQAAAAAKAACpoACgCgAKCgAKAAAKCgsAoKCQCgoAoAoAoACpoAAAALAJoKAKCpoLDwoAAJAACgAAAKCQD5////+86QkLmprw8JrJ6f++8Nra356wvf/braAKCsoKCwsACgoLCgoAALAAAAAJAJAACQAAAKkAAAAAAAoKAAAACQAAALAACgoAoACgAAkAAAn/+fAAoAoAAAoJoACgsKv9//7bCgoM8JCpDgmpv//5/72/v72toLvtsAmpCQkJAMC8vAkAAJAJoACgCgoKAAoLAAqQoAoKkACgsJCQsLAKCgoLAACgAJCpCgAAoAoKmgqf/p6QAAAKAAAAAJAAC8m///2skAALqa+euQoP////8Prf3/ywvf2/DpoAoKCgravLCpoLCgoKAKCQCQAAAKAAAKAKCQAACgsAAKCgAACpAAAACgqQoAAAAJCgCQAAAJv///CgCgAACpCgCgoKkL7f79oLCpoJ2vkLDKn7///9qfn7/9vp+///ywAKnpya2r376eAACQAACQCgoAoAoAAAoAAAAKAKAAAAoAAACgAAAKkKCQAAkKAKAKCQAKAKAL7fnwAACQCpAAAAAAAJDr2/n6DwAAAOsJ68sJqf////8Kn///+f////vAsJC6vpvf/98JCwoKCwoKAAAAkAkAmgCwCpoAAJALCpCpALALCgoAoAoKCgoAoJoAoAoAsAsAm74AqaAAAAoAoJoAmgqQ688NoKCgAJ65C54Kmt///w28qf///////9+8AKydvf////76AAkAAAAJCpCgCgCgAAAAAAAJoAoAAAAAoAAACQCQAJAJCQAJAACQAJAAAAAKDAmpAAAKAAAAAAAKAJCpALCgkJCQoPkOngmpy7//+/oJv/////////8LCwu///////+csKCpoLCgoAoAoAoAoAoAoAoKAKAKAKAKAKCgoKCgoKCgoKCgoKCgoKCgoKCwmpoAoAoACgCgCgCgCgqaCwqaCgoKALCpoJoAuf///wALD/////////+eAL3///////8KAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBQAAAAAAALStBf4="",
    ""Notes"": ""Education includes a BA in psychology from Colorado State University in 1970.  She also completed \""The Art of the Cold Call.\""  Nancy is a member of Toastmasters International."",
    ""ReportsTo"": 2,
    ""PhotoPath"": ""http://accweb/emmployees/davolio.bmp""
  },
  {
    ""EmployeeID"": 2,
    ""LastName"": ""Fuller"",
    ""FirstName"": ""Andrew"",
    ""Title"": ""Vice President, Sales"",
    ""TitleOfCourtesy"": ""Dr."",
    ""BirthDate"": ""2/19/1952"",
    ""HireDate"": ""8/14/1992"",
    ""Address"": ""908 W. Capital Way"",
    ""City"": ""Tacoma"",
    ""Region"": ""WA"",
    ""PostalCode"": ""98401"",
    ""Country"": ""USA"",
    ""HomePhone"": ""(206) 555-9482"",
    ""Extension"": ""3457"",
    ""Photo"": ""FRwvAAIAAAANAA4AFAAhAP////9CaXRtYXAgSW1hZ2UAUGFpbnQuUGljdHVyZQABBQAAAgAAAAcAAABQQnJ1c2gAAAAAAAAAAAAgVAAAQk0gVAAAAAAAAHYAAAAoAAAAwAAAAN8AAAABAAQAAAAAAKBTAADODgAA2A4AAAAAAAAAAAAAAAAAAAAAgAAAgAAAAICAAIAAAACAAIAAgIAAAMDAwACAgIAAAAD/AAD/AAAA//8A/wAAAP8A/wD//wAA////APAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACf///////////////////+AAAAwAv+AAAAqQAP//////z////8AJ/8AAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn////////////////////9sJ8AAAAA/L/+ngv//////w////wJ//4AAAAAAAAAAAAJAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAv///////////////////////4AAAkAnwn/wL//////8P/////v/8AAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ//////////////////////8PCwAAD/AAAAC////////f///w+f4AAAAAAAAAAAAAAAAAAAAJAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP/////////////////////+Cf/8Cp8AAPwAv///////6///z/78AAAAAAAAAAAAAJAAAAkAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC///////////////////////AJy+CcAAAAAL/////////f/8v8kAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn///////////////////////wACf8L+p8Km/////////7////AAAAAAAAAAACQAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA////////////////////////8An8vA3//9//////////+fD8AAAAAAAAAAAAwAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ////////////////////////AK8AAAAAAL//////////7+AAAAAAAAAAAADQAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACf/////////////////////////p/AC/AJC///////////38AAAAAAAAAAAMAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAD//////////////////////////Qnr/Jra/////////////gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAv/////////////////////////AAAAAACf////////////wAAAAAAAAAAJwAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ///////////////////////////wv9v///////////////AAAAAAAAAAkAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP//////////////////////////////////////////////AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC//////////////////////////5+f25///////////////+AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAv////////////////////////f2//7//mf/////////////8AAAACQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJ///////////////////////9u//5/fvf/5v////////////wAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP/////////////////////5+//fvf+//7//+f///////////wAAAAAAAAAAAAAAAACQoACcsNqamvnpvpAKAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC/////////////////////n///+/+/v9+///+b///////////gAAAAAAAAAAnKkKkPAMkL2pran/n56b2bDJwPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn///////////////////+f//vf/5/Q+fnfv///n//////////AAAAAAAAA2tqZ4Jyw+by9rZ+b2wvLmtr8uan/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA//////////////////37/7+f+9qfCtCwoJ25v/+//////////gkAAAAJywub2pn5vbD5sLmrD5rfm9rbmby9qfAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAL/////////////////b+/2/zw8P38va0Nnwvv37///////////AAAAAAACcvPC8sPC9uen56duemw8L2w/pua2/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAC////////////////9v//5/w+f+8sL0Pmp4A0JC9+//////////AAAAAwLn7m5vbn5vanpuemanprfmfmtuZ8PnvAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn///////////////vb29rerb2pyfnw+9ranbDw+enb//n//////gAAALCdqcvPCemp6fm9D5rb25+anpra2+nw+fAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA/////////////76fv76f29vLy/vJ6fng29oNsJ4J6cuf///////AAAkACp+5m58J+fmw8Lmemtranp+fm5rZqbD/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAL///////////9+f37zf2tqekLnJy/vL/b8Pn7yememtD//5/////gAAwNvbDa0PC8m8vPm8vJvbm9ufCw8Pm+np+fAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACf//////////27v/uc+wvLDwn96fqQ25y9D56cv5npD5qQv/n////AAAC5qa25vwvbvLm5vL272p7anpufnwvJufmvAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAn/////////n5vfyw3LDbydvb8Knpn/D8sPuen7yesPCe28+b+b//8ACQnL29sPCbyw29ra25qcvbm5+enpqb2/yw+fnw8NAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA////////2b+f+5/LC8sNvp6Q29ueCcsJrQ29qdvZyZ6Z6bDw/////A8Km8vLy58LnbCw+bnp25sPDan5+fnpqbnbD/n5m7+ZDAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAP//////+wv/D7za28npvLDQmvCeDZ+anw2p4J0KAPC8vanw8PD7n/8AAJy9ufm8vfCw+fkPy8sPD5v58Lyw+fn56w+fsL2v28+a0JoMvLAAAAAAAAAAAAAAAAAAAAAAC//////wvZ/5+cmtoJ8JycsL/Qnpn6mtnp+fmfD5+ZvZsP0P+fnp//8AmtqbDwvLmwsPnpr5ubnwuQ+Qufm9sLy8udsPnwvZqbD5Dw25Cf8AAAAAAAAAAAAAAAAAAAAAn////7yfnvnw8Lyanamempy9C/2ekN/an5vLy8vby+2+35v5nw+em//AAAnJ+b256fnQufkPnp6fDbD7zwvLyfm/yan7y9q9vJueubrb+prwAAAAAAAAAAAAAAAAAAAA////6Qva2/npDQvJ6Q6Q8PkL0Lqf+5qfnp+fn58Nvby5++nvC/np6f8AkJCwvLDZvLqbyw+by5mp8L0Jufm5up6Zvpqf+Qna2w252tm8nb2cAAAAAAAAAAAAAAAAAAAP///Qm9+58LyempyakPnLCQvan53wn9n9rby9vem/vfvcvZ25/f+/2f4ADgANC5+6292tufC8va2fC8v7y56enbn62b2/mp+rkNqfC5rbm+mrwAAAAAAAAAAAAAAAAAC///y5vPsP0NCpCcsJy5yw/by9+evb8L6b29v56b/9D7378PsNvw/9vpAAkJCwsNsNmpqZ7a2b2purn5ucuen58Ly9utqfnanZ6bnpvJ+p6Z+cvAAAAAAAAAAAAAAAAAn//pna2w3wsL0A8LD6nAudsNvan5D5//29v/3/n9vb+fv9v/n7y/n6+fvAAKwAya2wvb2tubDwva2c8Lz735sLD5va2b2vq9q9mtuem8n5npr5mwAAAAAAAAAAAAAAAA//mevb37CenADwn8kNvbDw2629C/+fmfv/25uf+9+9v5+f29/b3f/f+v2/CQCQAJqfC8ua2tufC9q5vbnwsP29sLy5rQvfnanavQvb0LD56b2a0PAAAAAAAAAAAAAAAL/wnpmtsNsACamfAJvbyw+anNvL/b3w/5vb/f//n/vf/f/7//v/up+/n5/LwAmgkOnJvbnLn5y5+Qna2tqfn5ra29vw8Ly72p+p0Ly5q5+am8vpv5sAAAAAAAAAAAAACf4J+drZraDQ8Nngn/Dwufnp26nb256/2/3/2/n5//n7+/n/29/5/fvf/a+/sAAAAJCa2py7ywu8vL65vbn56a25vJqfm9udvanbr5vL0NqfDbmekNDwAAAAAAAAAAAAD/mbyw8OkNoJCwqdsJ+f3p6dqd69rf/b/b+/v///+f///fvfv/n//5/72/28/wCcsArJn7nJvbyfm5kPyw8Pm9sPCb3wvJrfy56dkPm9qb2p8NqZ65vLAAAAAAAAAAAAvwDbDbmw8AkLydC8vampuemp+/n5+5+9v9/f3/n////9///9+/+9v7/b77/72fAAAJAKyfq8mtsLyem5vbnw8Pnw+pqfm/mvuem+vQva28ufC58Pmem98AAAAAAAAAAA/AvQ+Q0NAPDQmp+b2tvfD5+fDQ+fvf/fn/v7+///n/n/v9/7/f/f////n9vfvr8AkMCQmw2bn5D5vwvLyw+b256b0Pnw8J6fnpnpkL2tsLnpm9rbnpnwsAAAAAAAAAAL8L2pmtoLCQmp8PCen7y5+cvJ+/n7372r2/3/39+f+////7+f//v7/f/b/7/r/98AALAKmtup6emw8J29ufmtqem9qb2tuen9uQ+by8vbDa28vLmQ+eqfnwAAAAAAAAAPwNrbyQCcng+cufn58J/a2/n729/569v9vb/5+////fn5/f//+/3/+/+//9vfn6/wAAkJyanb256fn5qbzw/bn58L28ufD5sL7bDwsJC9udsL29r5qZ2p68kAAAAAAAC/wJ0JrQ2gCZoLyw8Pn5vfnL+enwn/n/n729v/n9+fn/+/+9+9//+9////2/+9+9vfCQDAAJ6anpmp6fnpubC88Pn5qb2psPn/mw+b0L2w8L29Ca2enwufmZrAAAAAAAD+Cam8kLAJDw2dvb29v8+7m5yb2/+f29vb29+b2/v//5/b3/v/vfn/vb+9//3/vf+/4AsJqckNufDb2wufDw+bm9qenwvby56fsPm8C8sPm8vam/mpsPnpra2wAAAAAA/w2p8JAAyemtuesPva+bv8/8u9rb28ufn9van9v5/9uf+9v7/b/7/7//3/v7+72/+f8AAAALD7ra2wsPnp+fm8vLn58L28uem9+Zrb0Jvby729rQvQ+Qm9sJC9AAAAAA//CcngnpsJ8Jr5nw+fn929ub39vfvb372t+b25+fn7373/2/29+9vf2/+//f/f/w/73wAA0AkJ25qfnw+fmprb2w8PCem/y5vLz52pqcvbnQsL29q9D7ya29vLkAAAAAn/ybCQAADwn52+n5/5+r27z/m+m8n/vb2bD9rb29+f+fvb/fv73/+/v7//v7+/n/n/v/CQoJ6anp0Ly5Cw+fnw+fm5+56b28u/uanb2pC8sL29qb0LmQm9sLCfDgAAAAC8oJ8MkNCQ8PvJ+fC/3fv8mb7b/b+dnr+9ufm9vbn/n9+9v73/+9vf39+f//35+5/w/fAAkAAJ+fC9uen5vLy7nprakPn5rb0Pnp+tqcvb28vQuem9vpr56cn7CQAAAAAAmfALCwvpvbyfC/nwu735v/29ufn7v5yfn5n9C9+5/7///9+fvfv/v7//+fv/+f+fv7wAANCemwvanp2p6bm8+Z+fn5ra2+n/mwmb0LnpqZqfDbyamfkPmwqcvAAAAAAAywsADbycvJv7/J+f/d+f+bv737+f2fv5+f+725mfn5+fm/v9u/29//////+///v///8AmgoJy9C5+am9sNrbmvC8sPm/m9qf7by8vQm9vL2p8Lnp6Q+a2dm5m8kAAAAJrZwNoJv5m/29n7/5v7v5/9/b+fn7+9vbn5/Z+f+fn529/Z+f37//vb/5+/39v/27/b/wCQCanr0PD5ra25+enb256a2w8L2tuQufmp6byan5C9qZvbC9sLra8JrAAAAAmaCQna0PD5vL+9rf+f2/vb+9v/+f372/+fm/nwnw+evbn/n/v/+///2///v7+fv9+//wAMkAmZqb2w29sPC5+prbn5vbn56fvLnpqcuem52wvan7ywnLDw2fmekAAAAJy8Cw8Ln5/a35/b2/n9v729/f/5m9u9vfn7/9n5+fn5n5/5v729/7+///vb3/n//fvf/wALALD60PC8uprb28vb2w8PDw8Pm/memb2pn56esPCfkJ+em9uZq5y5+QkAAAuQnJm8mp+9u9v5v5/7/f/7+5+f/737/7+fn7/b29sP+fn7/f//vf/f+9//+fvb+///n/AAAMkJu5+b2f2prbm9qfm5vbnwvL6Z6emeC5vZ+b2p+wuZ6a2tnLvLDwqQAJy8qawPn9Dw37/fn/m9/5+f/b/5+fv9vb//+fu9vb2Zvbydv5+///v73/vb+///3//7//sJyQrQ0PDwsLn5+enr2w8Pm8ufm/28m5vJva2pqemfDb3pm9ubC5y5+fnLkJuQkNm58Lvb+fn7+9/7+/v73/m//5/b+/2/n/372tvJy5v72/3//7//v7/739+/+/n//7wAoAkLC5+en56anp/bD9vQvL2p6fsJvp6b2tufn5nrm/C5ra2g28uempCwwL2gnpra29/b2/+9n/+9vb39v5/9uf2/n9v9v5+dvb2/uf29vfv/v9+f39+9+/v9vf//+f8AkJC8m8sJuem9vbmp+amr29qfn/na2fnanbya28ucvJva29vZqb0L29vbkNrQCfn58Ln6nb37/b3////7/b2/2/vf+//b37//vb29n9vb2/29//v/v737/////7/7//8AAOALD5+fD58Lyw+fD5+dvLnwsP+pqbqdqbvZqby5+9qZqamtnLnwvLC8sJkNqcsPn9rfv9vfv/vb/7/fv7+b/5+5/b2/vb29mfnb29vZ/Zv7//+f2/+9vb2/v9+9+//wCQkJyanpsPn5vbD58PDwu8uf2/Cfn8nanw2p28uekL28nb2am56Z+Z+ZAL6anL256b+/2//735+/+f+//9/72fvfn7/5+9uf////+fn7m/3///v/v9vf//////v///vwAArJqfm5y56emtueufub372prfnwmpup+9uesLnLm8vLm+mpy8m/C/Dw+ckMm5y9n9nZv9uf+//f////vbudv737v9ufDfn9v/29/b+d/9v/+f/5vb//v5/5/7/////wAJCQnLy8ufn5vbD5nwnp6Qvb2/sNvbycva2p28m96fm9rZvamb8J8JsJCby5rLn6+b+/2//5/b+/v7+9//3735+f2fDZuf2b25+b+f372////pn//b+/3////f/7+f/5AMCgub2py56a2tvb6b+fm/y8sLy6mtubD5van56bmp6b2vCdrZDwnw29CfANm8udvPn7+fnfu9//3/37+/+9u/n78Judn5vZ/b39vb+f/f//+QCfm9n7v7+/v////73+CakMDwvbnpvbnwsPnw8Ly5vb29+dvbytuf6Z8LmenbnwuZ+wmw+b2psLD7CaDb27256f+f+5/fv5+9v//fvb39vZ29nLmb2/m9uf2529m9mQAAkJrb+d/f3//7+f//+/AAAJCb2p6by88Pn5rbn5vemprfqa2tubnwmem9D5+w8L2+kJ6fm8ufCcucnp2/D9qf2/n7/f2//////5/735+QAACQm5y9nZnJCQkJCQCQkAnb3L2fn/v/v9////v///AJywuem9mtvbm58L2w8Ly5vb2vnJua0Nrb2pramvDbn5rZvakLy56Q2wnwCZqdv5/5v9v5v7/73/vb//+fvbkAsJCQCQkAkACQnJrQkJnLCfm9u9v7/5+f+/v5//37//AAAAy56a29up6en9qfm9ufy8ufu8n5ub2wvQm9mb8PC9u8udvbnLnpAJC/mtnw/bntvb3/29vf+///n/v9+9qdm9sNkNCQm529m9mfn5+dvZ+f2/n9+/v5//////v/+/wJqQkPn5qa2fn5sL2p6a2puby/ybqfDwvfmp8LDwm5+QnLnpCw+56QAA/QDavLm9+b2/v7/b+////7/737+fn7/b35vbnb2fmb2fn5vb273737/5/7/9//+fn7+///n/sAwKDwsPn5vpqen/m9vbn56embm8nLkL2prZCw29v8vL+56Q+duckAAJmwm5258Pm9v529/7/fvb+f29v/n5+/2/m/25+9vb29+9nb2fndv5+9+f+f+fvb///9//2///wJqQmb356byfn5vLDw8Ly8uby/0Ju9rQufkPvZqbyb2wkPn5Crybn5+Q6QvJrfn8v/D/v/n5+/////v/+f///b/f/b/fnb29vb3/+9+9v72//7/5//////+fv7/7//+/8AAAy8sLn5+pqa25+bm9vbD8sL68nLm5yw+QmtnpvwvLnwsPnZvZ/fn5npy/mp6b0PvZ2/v/n9v9vf25/5+/29v5+fn/+f/b3/////vf+f////n/v9v9v9v9/fv//5/78AnLAL28sPn9vZrfDw8LC9ub2/mbC56cufnp+bqfCfm56b2wmp2/+///CQqQ+fn5v5+/+f3b/7+/+/v/+f/9v/n/+9v5/5+f+/ufnb373/+////5/b///7/7+/3/v/+9/wAACbD5+fqa2r25+fm9va2tqfDw2ekLnpqZqcnan7y8uekNrZ/53/2/kPnfnp69+fnb37+7+f3735/fn/vb/b/7n/n5+fm/n7372/+f/7/f2/+f+/+fv/+fn/+//7///wmgnw+bD72fm9mvnpra8Pm58NsJq5+cufnp2rC9sNuZy5+bm/n/vb/98J6a2bnLnw+/v5/f//vfv/+/+9/729v9+f+fv5/5/fn9v5+/v9v/v5//n5//35///7/9//37/8CcAJvJ+8vpra+em5+b2w8PC728namp6QuanZ8L2w+pvLDw2f+f+9v/+Qn5rZ+96fn9n/m/n/+9vb/b3729v/2b29v52/n/v/+/3//f29+/2/+f//+fv/n/n///+/+///AJC8C7Db29m9n58PDwvbn5+fsJsPnJm9vJ8LC9qfna25+Z+9v5vfn78NsL28n5vb+b/5/5/5vf//n7+/v//bv9np+evb/5/wn/v/2/v/vb/fn729v//b+9+9v7////n/AKAJvQ+5+a8Lqem9ufnw8Ly/DbDbC5vLCbDwnanbC9uekP+dvf/52/0LD9vLv5+fn/25+f+f//vb+9/9/fn73b+Z29n5+f+fvb25v9n5/9v5+9///b2//b//+f/7////wJDw2pvesPn535vLDw8Ln9uducuw2em9vJ+fqfmtvLD52529ufn/vfDb2a250L2/6bv9v5/729+9vb2/v7+f+9u/vb+en7352/n9/b+/n73/n/ufv/v9v/vf////+fv/8AALD7y735sPqw+fm9u88Lz62pybqZrbCwsJ2p+am9sLCfv/2/+f+/sAvbDavfD5n9n72/n/v7/b//vZ/f29n7398NvbnZv/+e+/v9vf/b+9+f//29+f29+/v/v///+f+gkAkJvZqen529qbyw2bn5ufvQucnw2a2cn5qfCdvLDb29mb/5//n9yfCfC9mp+f+fvfv9v5/fn/29//u/v/+9vbn7D5+/2fn5nb37//v9vb/729v/v7//v9//////vb/AANrby/n5+evbnw+fvtqenvkLnpsJutmpuenwn7C52pvb//3/v//bCwnwvb6fm/n7273b/f/7/5/7mb35/5+fn729+f29v/8Jyw+f/72f//29/735/f373/////+//Z/wCaCQvw8LD7npy5ua2bn5+Z6cma2a2bDa2pC9sNnwuf39vb+f/5+/Cf6fnpn5/L+9v9u9v5+fn/vf//2/n/n5+fnbmb/b/Zn5udv5mf+9vb//ufv/+/vf+/v/v///+//wAAkPnbn5+a2/sPD5vp6em/mp65rZrQsJvb0L26mtqbv/+fn/n//b0Amtuenan5/L2/2/n/v/+f+9+b+dv7/7+f6dv9m9vwCf252/wJCb29vf/9+fn/+/3/////+///v9AJ6Qq9qen58J+b28m9ufD/6Qmema2728kJqfCdvZnf29v5/7/5CfC9vbDZvp+9ufvb+f+9/b3/n/v/n/n9+9/5mwnbwACdubn/vfuZ+dv5/5vbv/+9//v///////vb3+CaAPnbvbnpv/C8sL8Ly5+bmfnp2tqckLC/nwvLCan7+f+d+fyQn7Da0P2b6fnb/b2/n/nb+/+f/5/5+9ufvfuQ+f2wm52a2f/b27//n7//+5//39vb+f////////////AACQmvyw+by5n5+fn9uem/ywubCb25rZ8JC9m9vJ+9/5/6n5kJv9qbmwutmb+/m/+f+fv735//v/n/3735+9/fn/vb29vZv9n//f+f///9vfub+/////2////7//vb//AJDpv5vfnpvL6byw+prbD58NDw8JvLmpn58Lyampn/nb/9udv9v8nL3pyb6dqf/Zv5/5/fv/+f/f//vfvbvfm9vb2//by9n7/7+9/5v///m939//n9vf/7///////5//kOkA0PCwvb29vLn5n72tufuambmtqfDakLCfmtvZ/5+/+Z2//5/5qdqdv8n735v/+fm/v5/b//+//5/72927/bn739m/mb+f/9/7/////5n7v/vb///7/////////5v/wJCam5+fmtq/m9qa+fvby/kNvLybnwm5y9n5rZsL+9vb2fvf+f/Qnam+mZv5+f/bn7/f/f+9vb3/+f+f+fv98J/Z+5/9n/n///n//////5//35//37+f//////+/+9/78ArAntran5vQnp+fnpywvby5Cwnw8PDakLC8mw2f3b/9m9+//9vwsNvZvL28v5+9+9+9v5+f//vf///9v/mfn5n/mfkL+f+f+f/b//+/+f+f+/+fv////////////5/f8JCam5ub2vC/+byw+fuf2/vL29qZuZsJ+cmbybqb+5/7+b35/7/5y7y+n5rb2/n737372/35+f/5+9//+f+9vbCb/b29v5/5+9vf////+5/5/////9v//////////5//+gAJytranb2QvLn5vw8LD/mQkLnLy8D5C5vpmtn/2f/9/9v/+5/QCcmZ8J+fvfvb+fv9/5+//72//fv73/n/+9n5m8vb2fmf/b/7/72/n/+//5/9v//9+//////7/7/7/Qngmb29upr/ufC8sPn9ub2p65ywm5uQ8PCZ6Zvbm9/7n5/b/f/wvbD/C/D5n72/29/bvb/9uf///7/f+/n9vZ8J/bn52/n/2/2/////+fv9+/+//9v///////v/+9+f/wAJqemp+fn5Dwvbn56an/nJnJqZ8NDLmQvLkPn/n7/9+9v/n5nwkPsJ+duf+9vfv7+9+9mf/9vfn9v739+//729qcn5qdvb/b////+9v/373//b/7///////////b/5/wCQy5+enp8L+fmtvpu96euaC9npC5ucsPma2wm9+f/7373/+/+fDZ+fnr25+f+9n9v73/+9vf/7+/3/+/n5+9vb27kPn5+9v9v///////v/v5///f/7////////+//b/5oKkPC5+an9ra2w282pv/nJ0LCQ+ekLnQsNCZ//Cfv/+fm/3wnwmpD5+ZvLn737+b/f+9vb+/m9/9v5/f+//f/9vd+Z+fnb+f///7///5/9//+f+/////////////35/w0JD5vQv9+a25vLvbvby/kLC52tsJC9CwnakPm5n73///n/+Z8A29vakP29/5+fvf29vf//29vfvb+9u/n9m/v5+auf35+/373///////v7////v/v////7//+/+/+9v/AAmw8P+bC/mt6b2py8vb6cvLCQnbwL0PmpqZ/f+f//vb2f/Ln5qQ+b+bn5ufv5/b+//7/5vfv72/n/35+b+f2/n535ub35+fv////73739v9v/29//////+////wvb/8Cay9ubnp/Q+bmtvfufm9mpma2pqZsJqQDZ0Jv5+b+9/9sL/Z8A29vJ/Quf29vb+9vb29vf+9/fnf+b/5/9vb+fvbC9n5vbn5+///n/+f+/37/b/7/b/7///f/7/8mdv/4JCby8v5q/np+amp8Ly7+Q6dCckOna2puaAL/5/Zn/v7/Zm/AACem5mtnrvf+fn9v/vf+/37+/+/n9vb+bn52/29/by9+/29///7/5v/n/u9v/n///+////7////Cb/9CQran5sPnw+fD5/5rbn/DLmamwv5sJmcnJuZ/5+b/73///D5AMm9vfD5vZz7n727/b2/n5+9vbn5+fv/n9+fufm9uZ+fmdv/v//9+f/5/5/f2p/b+f/f/5/////wCd/w8A25+a35+fmpsPkL2w+duQvpwNkJC8sLC8AJv5/Qvf+f+9+fCQCa2pufn7uf2fvf2//9+/+fn9+fn5+f+/v529vJvLn5+/+Z//v7n72fmtqbn5+9v7+///v/+/v/Cb/wCpqfC9sPDw+enw+9rby7y9Cbmg+a0JycmbkJ/9v5m735/5vwAACZn929qd+b+f27+fm72b2tsLD56/n5vb2/nw+8vfnb35//v//9vfv8+b39ufn729/9v//////wCZ/w0Anp/L+bv5vLCbnp8Ln9sAvJyZC9qbmprQ0Jv5/b39+/8L/QDpAPmanwn7n9v/vfv7/fv9vb29+b2Z2/29vf+fmfm5+9ufvf//8L/56b29ub2/n5/7/7//v///+/CQ/wAJ6bm9rw2/y9vL37n9qbyb2bCwvQkNrQkJqQ/9v9m/v9+d8AkA6bz725+f27/bn52f27nan5+fm9uesJm/29ufn5v9rb3/n7//2fyb2fm9v/////+/2/+f//v//8n/vwqem8vL2fvwubC9qw8Ln/vACw0Nmp8LkJy9AJv/n56f//2/sAAJANudvPnpvb29+fv5vJ+9ufvb/73529rZr7/b28nb29vb/5v/rbufn5vb35vb///7///7/////wuf/ckJvb25+py9va0L29va29uZrbCw+QkNqakLkPmf+fmf/7n9rQAOCa2r25+fn5/7+dvfm/Db29v9vfvb+b29mdu9rbm9vb29vf//kN+9vZ/9v/2Qmp//+ZCfn////60L+p6a2tran/uemtsNra29rfngmQ0JAPC5nJC8CQv/mf27298L0AAJAJudqfn72/29+/m68Nuby9vb2w29mdvbv5/b296b2/vb/7/wn7/a2////ZC9vfvQAAvb+////wC/rakNm/m98L3pvan5ufmvm/AJvLCw25DQ8LyZsLnfn/mcvfm9oAAACd7bn5rZ+9u/v5vZmbnpm8n/n9v5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kAAKDQCp/pv/+Z+frb+Z//AAkAAJC/m5n5//v/vf////n///+/+9/7//n5///7//v///////////vaCvvLywoAAAkJyQ2pyeCckNCbyQnpnw8JsJkJCQkOkAAAAAAAAAAACQybmduZ6bkPn5/bCfvL/Qnp/b35vf+9v//////7////3///////+f///9v9/f/7////+//7//8JvZyw8MAAAAAAAAAAAAkAAAAPAACQAAkAAAAAAACQAAAACQCQAAAJAAkA8L3/n9+b8Pm/2tn5v/mduf+9////+9///73//5//+//9///7////+/2/+/////////////3gD6/QmpAAAAAAAAAAAAAAAAAA8AAAAAAAAAAAAAAADwAAAAAAAAkAAAAJDQsJ+5rZ29+b27+f+f+//5n73735//v/n//////7////v//9/5/b/b////////////v///uQCQvp7eAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAkADAAAAADQAAAAC8Cb2fvf2an73tvfn/n5/b2///+/+/+f////////+f//////2///////////////////////nLAJ8Lmp6cAAAAAAAAAAAAAAAJ4AAAAAAAAAAAAAAAAAmgAAAJAAAMAAkAsAmvDbC9+9ub/5/5+/+/vf+Zvf/9v/2///+/2///+f/5//v///v//////////////////7ywALy97fAAAAAAAAAAAAAAAAAOmgkAAAAAAAAAAAAADAAAkAAAAAqQAJDJANCZ25/bvL/9vbm9vfn9v5//+9v////7//3///n///v/+/3/+9////////////////////vACfnwsLywAAAAAAAAAAAAAAALwAoAAAAAAAAAAAAAkACQAAAAAAAAAAAK0L0L8Pm8n5mfn/2/+9+/3/vb3//b/b///b+/+/+///3/3/+///v/v////////////////9qQAAvp8J8MAAAAAAAAAAAAAAAMsAAAAAAAAAAAAAAAAAAAAAkAAJwAkACQkAC8m9rb+f/t+9v9vb/b+/3/+/+/+/2////f//n/v5+/v/v/n//5//n/v////////////78AkLybwLwLAAAAAAAAAAAAAAALwAAAAAALAAAAAAAAAMAAwAAAAAAAAAAA+skJvJ29n9ubvb37//v/39v7/9//3///n///vb/9///////b//vb///9////////////+9AJAP2sC/C9AAAAAAAAAAAAAAAMsAmgwAAAAAAAAAAJ4JAAkAAAAAkAAAAACfCcm6m/sL3/2/ufm9/b+///3737////+/+9////////29v//Z///7//2////////////a8OCa+fAPDa4AAAAAAAAAAAAAALwAAAkAAAwA4AoAAAkACQAAAAAAAADQAJALkLyfnJ/b+b+d/9+/v9/b+fv/v/+fvb/9/7/9v/v//b////m/mdv9+/v////////////5CQCfAK2/C9AAAAAAAAAAAAAAAOmgAAoAAAsAkAkAAAAAAAAAAAAAAJCgCQAJ6QuQ+9qfn9n7+b/f2/v/n//b35////3/vf/////b+/+/+f3pn7+fv/35//////////+emgkNsNDwv+AAAAAAAAAAAAAAALwAAAAAAAAAAAAAAAAAAMAAAAAAAACQAA0Am8kPkJ/bn7+/2/2/v/2///n/v/+9vfv7//+///v//9//nwCZ6Z/5/9u//7////////vwvA8KAL6bzQAAAAAAAAAAAAAAANoAoJAAAACsoAAACQAACQAAAAAAAAAMCQC8kLyQv5C9v5/b/fv5/b/9vb+//b3/+//9/7///9///7+9CQufmfn/+b/Zmcm5n//////PCQAJDQnAvpAAAAAAAAAAAAAAAOnAAAAAAACQAMoAAAAAAAAAAAAAAAAJAKCa3Ju9nLme2fn9vb/fv/n////5//vb/5+/v9/b+b/5+f/JkJnwm/2/n/m/6QAPvL////+fAJD8C/C88A8AAAAAAAAAAAAAAPCgAACgAAAACwAAkAAAAAAAAAAACQAAAJwJqQnAufD5v5v5+/29/b/729v/+///3//f/b//3/+///CwD78J/fvZ8J/wkLnZCZ////mgvAqQvACbC8AAAAAAAAAAAAAAAKyQAAAAAAAOAAAAAAAAAAAAAAAAAAAACQAAnLybnL25/b/fv9v7+/29v//fvf+f+/+////7/9/wsAkJudC7/72bCfkJCQygD/////D5yQnLDwmsnQwAAAAAAAAAAAAAANoAwAAAAJoJDgAAAAAAAAAAAAAAAAkJAAvQqQucucvZ+8m9/b29vfv/35+//7/7/5//2//9/7n52ZAMnwnfnQsJ+9rQvAkAm/////kAoJoMkLybCgAAAAAAAAAA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AAAAAAAACcAAAAAAAAAJAAAAAAAAAAAAAAAACQqQkLyQAAAAAAAAAAAAAAAAAAAAAAAAAACQypAAANCQuQDpAACQvJAJDADQAAAAAAAAqaAAAAAAAAAAAPAPCgAAAAAAAAAAAAAACgAACgkAAAAAAAAAAAAAAAAAAJAAAA0ADQC8kAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAkAAKwA0AAArAAAAMqQoAAAAAAAAKAMAAAAkAAAAAAA8AAAAAAAAACgAAAAAAAACQCcALAAAAAAAAAAAAAAAAAACQAJAAkAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAyQwACQkLAAvAkKCaCakAkAAAALAACwywoAAAoAAAAAAPCwqQAAAAAAAJCgAAAAAJoAAAAAkJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAAAAvArAkACaAJAADAAPAAAAAAoAAJoJAAAAAAAAAAALwAAMAMAJALAKAAALAAAAAAAAkAAAD+AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAAAAJAJAADLwJwAnAkACQAAAAAAAADgmsoAAAAAAAAAAPqQygsAkACgAAAAAAAAAAAMkKwAAAAMANCQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkACQAAAAAAAAAAkJqQAAAACgALwAAAAAoJAACQ4JAAAAAAAAAAAJwKkAAKAKAAAAAAAAAAAACQAJANoAkAAAAAAJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAAAAkA6QAAAKwAAAkAAAAJAAAAAAAAAKAJoKmsAAAAAAAAAAAOvAAAAAAAkAAAAAAAAAAAAAAAAAAAAAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAAAMkJCwwAALAPAAnAAAAAAAkAAACcCaAAAAAAAAAAANqaAAAAAAoAAAAAAAAAAAAKAAsAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAAAAAAA2gAAAAkAoAkADLCwAAAAAAoAAACgrAAAAAAAAAAAAPDAAAAAAADAoAAAAACgAAAAAAAOkAywAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQwAAAAAAACQAJ6QAAoJwAAAsAAAAAAAAAAAAKAJCaAAAAAAAAAAANoAAAoAAJAJAAAAAAAAAAAAAACQAJAAkKwJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAJAACQAAkJAACQAJwAAAAAAAAKAAAAkAoAAAAAAAAAAAAPANAACaAACgAAAAAAAAAAAAAAAAAAAAAJAAkAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAAAAAAAwKyQAAywAAAAAAAAAJAAAACgAAAAAACgAAAAAK8AoAAAyvAAAAAACgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACakAAAALAAAAAAAAAAAACgAKAJoACpwKAAAACgAPAAAAoJAAAAAAAAAAAKAAAAAAAAAACQAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAkAAAAAAACQAAkADAAJAAkAAAAAAAAAAAAKAACQAAAADAoJDwALCcAAwAAJAKAAAAAAAJoAAAAAAACgAACwDAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQ0AAAAAAAAAAAAAAAAA4L2pCeCQC8AAAACgAAAAAAAAAAmgAAsKkAoAAAygAPAAAKAAAAAAAAAAAAAAAAAAAAAAAAmgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAAAnACQAAAAAAnAAA4AALwAAAAAAAAAAAAAAACgAAoAAADwwAAAsAAK0AAAAACwAAAAoAAAAAAAAAAAAAAAAJAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAAAAAAJAAAAAAAAAJAJAJAAAAAAAAAAAAAAAAAAAAAACQqeALAAAADrwNoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACcAAAAAAAAAAAAAAAAAJCQC8oACaywAAAAAAAAAAAAAAAAAACgngwAmgAAAAqQoKkAAAAAAAAAAAAAAAAAAAAAAAAAAAoMCQCcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACcAAAAAAAK0AkAywkAAAAAAAAAAAAAAAAAAACQoLCwrAAAAJAPANoAAAAAAAAAAAAACgAAAAAAAAAAAACQoAAAkAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAkAAAAAAAAAAAqQAJyg8JCwAJAMAAAAAAAAAAAAAAAAAAoLCg0AAACQAAAK2gAPAAAAAAAAAAAAkKAAAAAAAAAKAAoJAAAPAKwLAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAACQAAmgAAAACQAAANAAsAAAAAAAAAAAAAAAAAkAwMvKCgAACgAAAAAPAPAAAAAAAAAAAADJAAkAAAAAAAAAAACgkACQAAycAAAAAAAAAAAAAAkAAAAJAAAACeAAAAkACQAA8ADJwKkLDAC8AAALwAAAAAAAAAAAAAAAAADKmpAJAJoLAAAAAACwAA8AAAAAAAAAALAArAoAAAAAAADaAKCQAAAAAJAAAAAAAAAAAACQAAAJDAAAoAAAAAkJAAAAAACQAJAACcAACwkAALDwAAAAAAAAAAAAAAAAAKCpAACgAAAAAACpAJoAoPAAAAAAAACgAACwCQAAoAAAoJoAyQDKALAAAAAAAAAAAAAAAAAACQAAkACQANAMkAAAAAAAAAAAkKCwAAkAAAAAkMkAAAAAAAAAAAAAAKAAAAkACgAACgAAAAAAoMAAAA8AAAAAAAANAAAMoMCgAAAAAAALCgAAAAALAAoJCg0AAACQAAAAAMAAAAAA0AAAAAAAAAAAkAAAAAAAkKwJwADbywAAAAAAAAAAAAAAAAAAmgCgAAAAAACgAAAACgvLAPAAAAAAAAAKAAAAkKkAAKAAAAAAAMsAAADAAJAACQAKAAAAAACQAJAAAAAAAACQAArQAAkAAA0A4JDa2pAACQ8AAAAAAAAAAAAAAACgAJAKAAAAAAAAAACQAA6akAAAAAAAAAAAAAngCgAAAAAAkAAAAACwywAACemgAAAADAAAAJAA0AAAmgDAAAAAAAAAAJAAvArLDwCgkAAAAAAAsAAAAAAAAAAAAAAAAAAAoAoAAAAAAAAAAAAMkKkMrJoAoPAAAAAAAKCQCcAACgAAoAkAoAAAAAAAAAAAmgAAqQAJAAAACpDgAACQwLAADwAAAAAAAAkAAJAAAAAAAACQAMsAAAAAAAAAAAAAAKCQAAAAAAAAAAAADAoAoACgmgCpAPAAAAAAAACgwAoAAACgAAAAAAAAAAAKAKngAMCwAAAAAAkKnACQANCpAAAJAAAAAAAAAAAAAACQAMkJAPANqQAAAAAAAAAAAAAAAADgAAAAAAAAAACsALCamtDaAAAAAPAAAAAACQAJoAkMCQAAAAoAAAAKkACQCQALywAAAACwAOAAAAAACwAAAJDgAAkAkACQkAAJAAwAkAAACQCwAAAAAAAAAAAAAAAAAAAAAAAAAAAAkJoAsAygwKCgAAAAoA8AAAAACgAAAAALDgAAAAAAAAAACgoAAA68AAAMsAAAkAAMsACgwACcAKCQAAAAAAAAAJAAAAkAAKngvAvAAAAAAAAAAAAAAAAACgkAAAAAAJqawOALDpoJCpAACwAAAPAAAADpAA2gAAAAAAkMAAAAAAAAAJAAAKkKmgCwALAAoJCwALyQkKAAsNAACcCcAAAAAAkAAAqQDQCQAAAAAAAAAAAAAAAAAAAAAACgAAAACsAAqwsMAAAKAAAAAAAAAA8JAAkAAKAAAAAAAAoJoAAAAAAAAAAAAJ6cAPAADACQwKwK2grKCQ2gwAAMCgAAALAAAAANCQAMoAAAAAAAAAAAAAAAAAAAAAAAAAAACgCwALDpwAywmgkAAAAAAAAAAPAAAKAArQAAAAAAAAAAAAAAAAAAAAAAAACgsAAAsACgmgC8ANCQygAJALCQkAkACQAJAPAKCgAJAAAAAAAAAAAAAAAAAACgAAAAAAAJAAAAsACaCwoAoAoAAAAKAAoAAAoAAAAAAAAAAAAAAAAAAAAAAKkAAAAAAAsLwKAAAAAAAJAAmgoKkAkAqcCgAMCgAAwOCwC8nAAAAAAAAAAAAAAAAAAAAAAAAAAAAAoAoJCgAJoAAAAAAAAAAAAAAAAAoPAAoAAJAAAAAAAAAAAAAAAAAAAAAAAAqQDAqckACeAAAA4KAAycCsCsAAAMCpDAAAqQnA8KAAoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKAJCgCpCgmgAAAAAAAJAAAAAPDwAMsACgAAAACsAAAAAAAAAAAAAAAAALCwCgoAoAkJCgkJDpoACQAAkKkLAAAAAJAKCwDwAADaAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAKAAAAAAAAAAAAAAAAoAAAAPAACaAKAJAAAAAAkAAAAAAAAAAAAAAKDAAA8AAAAACgCQoOAAALAKkAoAAAAAqQAAAAwOkACwAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA8ArADJAAAAAAAAoAAAAAAAAAAJAAkAkAraAAAAAAsMvLypAAAAAAAJANoAAJAAmgAJCwAADAsACgAKAAALwAAAAAAAAAAAAAAAAAAAAAAAAACgAAAAAAAAAAAAAAAAAPAJCpCgAAoAAAAAAAAAAAAAAAAKAAoACpAAALAACwywCgmssAvAAACgCgAAAAAAAAAAAJAACwwAAADQAAAAmsAAAAAAAAAAAAAAAAAAAAoACpAAAAoAAAAAAAAAAAAAAK2gAAAA4JAAAAAAAAoAAACQ2prADAAAAAAAkAAJALALCaCQAAAACQAAAACQAAAAAAAKAOCpAKkAkNoAAAAAAAvAAAAAAAAAAAAAAAAAAAwAAAAAAAAAAAAAAAAAAAoAAPAAAAqQkMAKAJCgAAAAAKkKoMAJqQCaAAqaAACgAArQysngAAALAKCQCQoAAAAAAAkJCwkADwCgCgAAAAAKCQAAAAAAAAAAAAAAAAAACwkAAAAAoAAAAAAAAAAAAAAAAArAAAAAoKAAAKCQCQAACQypyaCaAKAAAAAACgkAqemgsJoAAAAAAMAA4AAAAAAAsMoMrArLAAAAAAAAAAAACgCgkAAAAAAAAAAAAAAJDAoAAAoAAAAAAAAAAAAAAAAAAPCaAAAAAADQAAAAoAAACpqQoAAAwAAACQDAAAAAAAAAAAAJoJoACamgAAAAAAAACQAJC8kA6QAAAAAAAAAAAAAAoMAAAAAAAAAAAAAKC8AAAAAAAAAAAAAAoAAAAAAAAPAAAAAAAJCgAAAAAKkAAMCtDwCwsAAACssKkKAAALDwALwAwAyaAADJCwALAAAACgmgAKCwCgAAAAAAAAAAAAAAAKAAAAAAAAAAAAAAAJ4AAAAAAAAAAAAAAAAAAAAAAA8AAAAAAKAAAAAAAAAKmpraAAAAAAAAAAAAAAAAAAAAAACwCwoAAAsKAACgAAAAAAAAsJwMvAAAAAAAAAAAAAAAAACgAAAAAAAAAAAAAKAAAAAAAAAAAAAAAAAAAAAAAPAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAoLAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABBQAAAAAAAImtBf4="",
    ""Notes"": ""Andrew received his BTS commercial in 1974 and a Ph.D. in international marketing from the University of Dallas in 1981.  He is fluent in French and Italian and reads German.  He joined the company as a sales representative, was promoted to sales manager in January 1992 and to vice president of sales in March 1993.  Andrew is a member of the Sales Management Roundtable, the Seattle Chamber of Commerce, and the Pacific Rim Importers Association."",
    ""ReportsTo"": 0,
    ""PhotoPath"": ""http://accweb/emmployees/fuller.bmp""
  }
]";

            var actual = ReadParquetFileAsJSON(filePath, 2);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Issue251_IssueWithNullableValues()
        {
            string filePath = "Issue251_IssueWithNullableValues.parquet";

            string expected = @"Id,Price,Quantity,CreateDateTime,IsActive,Total
,,2.45,,,";
            using (var w = new ChoParquetWriter<Trade>(filePath)
                   .TreatDateTimeAsString()
                )
            {
                w.Write(new Trade
                {
                    Id = null,
                    Price = null,
                    Quantity = 2.45,
                });
            }


            var actual = ReadParquetFileAsCSV(filePath);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Issue139()
        {
            string json = @"[
	{
		""Health"": {
			""Id"": 99,
			""Status"": false
		},
		""Safety"": {
			""Id"": 3,
			""Fire"": 1
		},
		""Climate"": [
			{
				""Id"": 0,
				""State"": 2
			}
		]
	}
]";
        }


        static void Main(string[] args)
        {
            Issue285();
            return;

            JsonToParquet52();
            return;

            EmptyFileTest();
            EnumTest();
            QuickTest();
            Test1();
            CSVArrayToParquet();
            JSON2Parquet1();
            //SerializeValue();
            SerializeArray();
            SerializeDictionary();
            ByteArrayTest();
            SerializeDateTime();
        }
    }
}
