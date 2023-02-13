using ChoETL;
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.IO;
using Newtonsoft.Json;

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
                .Configure(c => c.AutoArrayDiscovery = true)
                .Configure(c => c.ArrayIndexSeparator = '/')
                )
            {
                using (var w = new ChoParquetWriter("CSVArrayToParquet.parquet")
                    .Configure(c => c.UseNestedKeyFormat = false)
                    .WithField("id")
                    .WithField("name")
                    .WithField("friends", fieldType: typeof(byte[]), valueConverter: o => o.Serialize())
                    )
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
            using (var r = ChoJSONReader.LoadText(json)
                .WithJSONPath("$..facilities[*]", true)
                .WithField("id")
                .WithField("createdAt", fieldType: typeof(DateTimeOffset), valueConverter: o => DateTimeOffset.Now)
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                using (var w = new ChoParquetWriter("JSON2Parquet1.parquet")
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                    )
                {
                    w.Write(r);
                }
                return;

                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
                return;
                //var x = r.Select(r1 => { r1.Location = new Point(100); return r1; }).ToArray();
                //using (var w = new ChoParquetWriter<Facility>("JSON2Parquet1.parquet")
                //    .IgnoreField("Location.IsEmpty")
                //    )
                //{
                //    w.Write(x);
                //}
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
            using (var w = new ChoParquetWriter<int>("SerializeArray.parquet"))
            {
                w.Write(new int[] { 1, 2 });
                w.Write(new int[] { 3, 4 });
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
    new DateTime(2009, 12, 7), //, 23, 10, 0, DateTimeKind.Utc),
    new DateTime(2010, 1, 1, 9, 0, 0, DateTimeKind.Utc),
    new DateTime(2010, 2, 10, 10, 0, 0, DateTimeKind.Utc)
};

            using (var w = new ChoParquetWriter<DateTime>("DateTimeTest.parquet"))
                w.Write(dateList);
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

        static void ReadNWrite()
        {
            List<Employee> objs = new List<Employee>();
            objs.Add(new Employee() { Id = 1, Name = "Tom" });
            objs.Add(new Employee() { Id = 2, Name = "Mark" });

            using (var parser = new ChoParquetWriter("Emp.parquet"))
            {
                parser.Write(objs);
            }

            foreach (var e in new ChoParquetReader("Emp.parquet"))
                Console.WriteLine("Id: " + e.Id + " Name: " + e.Name);
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

        static void Json2Parquet()
        {
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
                //using (var w = new ChoParquetWriter("MyData.parquet")
                //    //.UseNestedKeyFormat()
                //    )
                //{
                //    w.Write(r.Select(rec1 => rec1.ToDictionary().Flatten().ToDictionary()));
                //}

                var x = ChoParquetWriter.SerializeAll(r); //.Select(rec1 => rec1.ToDictionary().Flatten().ToDictionary()));
                File.WriteAllBytes("MyData1.parquet", x);
            }
        }

        static void JsonToParquet52()
        {
            using (var r = new ChoJSONReader("sample52.json")
                .WithJSONPath("$..data")
                )
            {
                using (var w = new ChoParquetWriter("myData52.parquet"))
                    w.Write(r);
            }
        }

        static void DataTableTest()
        {
            string csv = @"Id, Name
1, Tom
2, Mark";

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                var dt = r.AsDataTable("Emp");

                using (var w = new ChoParquetWriter("datatable.parquet")
                    .Configure(c => c.CompressionMethod = Parquet.CompressionMethod.Gzip)
                    )
                {
                    w.Write(dt);
                    w.Close();

                    var s = w.Configuration.Schema;
                    s.Print();
                }
            }
        }

        static void Issue167()
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

            using (var r = ChoJSONReader<CompleteFile>.LoadText(json.ToString())
                .UseJsonSerialization()
                )
            {
                using (var w = new ChoParquetWriter("CompleteFile.parquet")
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

        static void Issue202()
        {
            using (var r = new ChoJSONReader("Issue202.json")
                //.UseJsonSerialization()
                )
            {
                using (var w = new ChoParquetWriter("Issue202.parquet")
                    .WithMaxScanRows(3)
                    .ThrowAndStopOnMissingField(false)
                    )
                {
                    w.Write(r);
                    //w.Write(r.Select(rec1 => rec1.FlattenToDictionary()));
                }

                //using (var w = new ChoParquetWriter<CompleteFile>("CompleteFile.parquet")
                //    .WithField(f => f.Samples, valueConverter: o => "x", fieldType: typeof(string))
                //    )
                //{
                //    w.Write(r); //.Select(rec1 => rec1.Flatten().ToDictionary()));
                //}
            }
        }


        static void Issue230()
        {
            using (var r = new ChoJSONReader("Sample230.json")
                )
            {
                using (var w = new ChoParquetWriter("Sample230.parquet")
                    .ThrowAndStopOnMissingField(false)
                    )
                {
                    w.Write(r);
                }
            }

            PrintParquetFile("Sample230.parquet");
        }

        static void PrintParquetFile(string parquetOutputFilePath)
        {
            parquetOutputFilePath.Print();
            using (var w = new ChoParquetReader(parquetOutputFilePath))
            {
                w.Print();
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

        public static void Issue251()
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            typeof(ChoParquetReader).GetAssemblyVersion().Print();

            using (var w = new ChoParquetWriter<Trade>(@"C:\Temp\Trade.parquet")
                  )
            {
                w.Write(new Trade
                {
                    Id = null,
                    Price = 1.3,
                    Quantity = 2.45,
                });
            }

            PrintParquetFile(@"C:\Temp\Trade.parquet");
        }

        public static void Issue251_1()
        {
            var tradeList = new List<Trade>();

            var trade1 = new Trade()
            {
                Quantity = 2
            };

            var trade2 = new Trade()
            {
                Id = 100
            };

            var trade3 = new Trade { Id = null, Price = 2.3, Quantity = 2.45, Name = "Name", CreateDate = DateTime.Now };

            tradeList.Add(trade1);
            tradeList.Add(trade2);
            tradeList.Add(trade3);

            //ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MM/dd/yy HH:mm:ss";
            using (var w = new ChoParquetWriter<Trade>(@$"C:\Temp\Trade3.parquet")
               .Configure(c => c.LiteParsing = true)
               .Configure(c => c.TypeConverterFormatSpec = new ChoTypeConverterFormatSpec { DateTimeFormat = "o" })
                  )
            {
                //foreach (var rec in tradeList)
                //    w.Write(rec);
                w.Write(tradeList);
            }
            PrintParquetFile(@$"C:\Temp\Trade3.parquet");

            using (var r = new ChoParquetReader<Trade>(@$"C:\Temp\Trade3.parquet")
               )
            {
                r.Print(); 
            }
        }

        //static void PrintParquetFile(string filePath)
        //{
        //    using (var r = new ChoParquetReader(filePath))
        //    {
        //        r.AsDataTable().Print();
        //    }
        //}

        public class Trade
        {
            public long? Id { get; set; }
            public double? Price { get; set; }
            public double? Quantity { get; set; }
            public string Name { get; set; }
            public DateTime? CreateDate { get; set; }
        }

        public static void Issue251_2()
        {
            dynamic x = new ChoDynamicObject();
            x.Id = null;
            x.Name = "Mark";
            x.CreatedDate = DateTime.Now;

            //ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MM/dd/yy HH:mm:ss";
            using (var w = new ChoParquetWriter(@$"C:\Temp\Trade3.parquet")
               .Configure(c => c.LiteParsing = true)
               .Configure(c => c.TypeConverterFormatSpec = new ChoTypeConverterFormatSpec { DateTimeFormat = "MM/dd/yyyy HH" })
                  )
            {
                //foreach (var rec in tradeList)
                //    w.Write(rec);
                w.Write(x);
            }
            PrintParquetFile(@$"C:\Temp\Trade3.parquet");
        }

        static void Main(string[] args)
        {
            Issue251_2();
            return;

            JsonToParquet52();
            return;

            EmptyFileTest();
            EnumTest();
            QuickTest();
            Test1();
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
