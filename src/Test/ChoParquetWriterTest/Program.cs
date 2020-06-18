using ChoETL;
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;

namespace ChoParquetWriterTest
{
    class Program
    {
        public enum EmployeeType
        {
            [Description("Full Time Employee")]
            Permanent = 0,
            [Description("Temporary Employee")]
            Temporary = 1,
            [Description("Contract Employee")]
            Contract = 2
        }

        static void QuickTest()
        {
            string csv = @"Id, Name
1, Tom
2, Mark";

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                .QuoteAllFields()
                )
            {
                using (var w = new ChoParquetWriter("quicktest.parquet"))
                {
                    w.Write(r);
                }
            }
        }

        static void Test1()
        {
            string csv = @"Cust_ID,CustName,CustOrder,Salary,Guid
TCF4338,INDEXABLE CUTTING TOOL,4/11/2016,""$100,000"",56531508-89c0-4ecf-afaf-cdf5aec56b19
CGO9650,Comercial Tecnipak Ltda,7/11/2016,""$80,000"",56531508-89c0-4ecf-afaf-cdf5aec56b19";

            ChoTypeConverterFormatSpec.Instance.TreatCurrencyAsDecimal = false;
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                .QuoteAllFields()
                )
            {
                using (var w = new ChoParquetWriter("test1.parquet"))
                {
                    w.Write(r);
                }
            }
        }

        static void EnumTest()
        {
            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Description;

            using (var w = new ChoParquetWriter("EnumTest.parquet")
                .WithField("Id")
                .WithField("Name")
                .WithField("EmpType", valueConverter: o => (int)o, fieldType: typeof(int))
                )
            {
                w.Write(new
                {
                    Id = 1,
                    Name = "Tom",
                    EmpType = EmployeeType.Permanent
                });
            }
            return;

            string csv = @"Id, Name, EmpType
1, Tom, Full Time Employee
2, Mark, Contract Employee";

            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Description;

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithField("Id", fieldType: typeof(int))
                .WithField("Name")
                .WithField("EmpType", fieldType: typeof(EmployeeType))
                )
            {
                using (var w = new ChoParquetWriter("EnumTest.parquet"))
                {
                    w.Write(r);
                }
            }

        }

        static void CSVArrayToParquet()
        {
            string csv = @"id,name,friends/0,friends/1
1,Tom,Dick,Harry";

            using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                //.Configure(c => c.AutoArrayDiscovery = true)
                //.Configure(c => c.ArrayIndexSeparator = '/')
                )
            {
                using (var w = new ChoParquetWriter("CSVArrayToParquet.parquet"))
                {
                    w.Write(r);
                }
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

        static void JSON2Parquet1()
        {
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
            using (var r = ChoJSONReader<Facility>.LoadText(json)
                .WithJSONPath("$..facilities", false)
                )
            {
                var x = r.Select(r1 => { r1.Location = new Point(100); return r1; }).ToArray();
                using (var w = new ChoParquetWriter<Facility>("JSON2Parquet1.parquet")
                    .IgnoreField("Location.IsEmpty")
                    )
                {
                    w.Write(x);
                }
            }
        }

        static void SerializeValue()
        {
            //byte[] x = ChoParquetWriter.Serialize(4);
            //File.WriteAllBytes("SerializeValue.parquet", ChoParquetWriter.Serialize(4));
            //return;
            using (var w = new ChoParquetWriter("SerializeValue.parquet"))
            {
                w.Write(4);
            }
        }

        static void SerializeArray()
        {
            using (var w = new ChoParquetWriter("SerializeArray.parquet"))
            {
                w.Write(new int[] { 1, 2 });
            }
        }

        static void SerializeDictionary()
        {
            using (var w = new ChoParquetWriter("SerializeDictionary.parquet"))
            {
                w.Write(new Dictionary<int, string>()
                {
                    [1] = "Tom",
                    [2] = "Mark"
                });

            }
        }

        static void ByteArrayTest()
        {
            using (var w = new ChoParquetWriter("ByteArrayTest.parquet"))
            {
                w.Write(new Dictionary<int, byte[]>()
                {
                    [1] = Encoding.Default.GetBytes("Tom"),
                    [2] = Encoding.Default.GetBytes("Mark")
                });

            }
        }

        static void SerializeDateTime()
        {
            IList<DateTime> dateList = new List<DateTime>
{
    new DateTime(2009, 12, 7, 23, 10, 0, DateTimeKind.Utc),
    new DateTime(2010, 1, 1, 9, 0, 0, DateTimeKind.Utc),
    new DateTime(2010, 2, 10, 10, 0, 0, DateTimeKind.Utc)
};

            using (var w = new ChoParquetWriter("DateTimeTest.parquet"))
                w.Write(dateList);
        }

        static void Main(string[] args)
        {
            EnumTest();
            QuickTest();
            Test1();
            EnumTest();
            CSVArrayToParquet();
            JSON2Parquet1();
            SerializeValue();
            SerializeArray();
            SerializeDictionary();
            ByteArrayTest();
            SerializeDateTime();
        }
    }
}
