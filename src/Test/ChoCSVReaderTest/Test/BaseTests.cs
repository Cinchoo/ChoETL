using ChoETL;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChoCSVReaderTest.Test
{
    public class BaseTests
    {
        static readonly string csv = @"Id,Name
1,Tom
2,Carl
3,Mark";

        static readonly string csvWithQuotes = @"Id,Name
1,""Tom""
2,Carl
3,Mark";
        static readonly string csvWithEOLInValues = @"Id,Name
1,""To
m""
2,Carl
3,Mark";

        static readonly List<ChoDynamicObject> dynamicExpected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name", "Tom"} },
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Carl"} },
                new ChoDynamicObject {{ "Id", "3" }, { "Name", "Mark"} }
            };

        static readonly List<ChoDynamicObject> dynamicWithEOLExpected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name", "To\r\nm"} },
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Carl"} },
                new ChoDynamicObject {{ "Id", "3" }, { "Name", "Mark"} }
            };

        static readonly List<Employee> pocoExpected = new List<Employee> {
                new Employee { Id = 1, Name = "Tom" },
                new Employee { Id = 2, Name = "Carl" },
                new Employee { Id = 3, Name = "Mark" }
            };

        static readonly List<EmployeeRec> pocoWithAnnotationExpected = new List<EmployeeRec> {
                new EmployeeRec { Identifier = 1, Name = "Tom" },
                new EmployeeRec { Identifier = 2, Name = "Carl" },
                new EmployeeRec { Identifier = 3, Name = "Mark" }
            };

        static readonly string currencyCSV = @"Id,Name,Salary
1,Tom,$100000
2,Carl,$70000
3,Mark,$50000";
        static readonly List<ChoDynamicObject> currencyExpected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", 1 }, { "Name","Tom"}, { "Salary" , new ChoCurrency(100000)  } },
                new ChoDynamicObject {{ "Id", 2 }, { "Name","Carl"}, { "Salary" , new ChoCurrency(70000)  } },
                new ChoDynamicObject {{ "Id", 3 }, { "Name","Mark"}, { "Salary" , new ChoCurrency(50000)  } }
            };
        static readonly List<ChoDynamicObject> currencyAsDecimalExpected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", 1 }, { "Name","Tom"}, { "Salary" , 100000.0  } },
                new ChoDynamicObject {{ "Id", 2 }, { "Name","Carl"}, { "Salary" , 70000.0  } },
                new ChoDynamicObject {{ "Id", 3 }, { "Name","Mark"}, { "Salary" , 50000.0  } }
            };

        [Test]
        public static void QuickLoadUsingIterator()
        {
            List<object> actual = new List<object>();
            foreach (dynamic e in ChoCSVReader.LoadText(csv).WithFirstLineHeader())
            {
                actual.Add(e);
            }
            CollectionAssert.AreEqual(dynamicExpected, actual);
        }

        [Test]
        public static void QuickLoadUsingLoop()
        {
            List<object> actual = new List<object>();
            dynamic rec;
            using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader())
            {
                while ((rec = r.Read()) != null)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(dynamicExpected, actual);
        }

        [Test]
        public static void QuickPOCOLoadUsingIterator()
        {
            List<Employee> actual = new List<Employee>();
            foreach (var e in ChoCSVReader<Employee>.LoadText(csv).WithFirstLineHeader())
            {
                actual.Add(e);
            }
            CollectionAssert.AreEqual(pocoExpected, actual);
        }
        [Test]
        public static void QuickPOCOLoadUsingLoop()
        {
            List<Employee> actual = new List<Employee>();
            Employee rec;
            using (var r = ChoCSVReader<Employee>.LoadText(csv).WithFirstLineHeader())
            {
                while ((rec = r.Read()) != null)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(pocoExpected, actual);
        }

        [Test]
        public static void LoadUsingConfig()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));

            List<object> actual = new List<object>();
            using (var r = ChoCSVReader.LoadText(csv, config).WithFirstLineHeader())
            {
                actual.AddRange(r.ToArray());
            }
            CollectionAssert.AreEqual(dynamicExpected, actual);
        }

        [Test]
        public static void LoadPOCOUsingConfig()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));

            List<Employee> actual = new List<Employee>();
            using (var r = ChoCSVReader<Employee>.LoadText(csv, config).WithFirstLineHeader())
            {
                actual.AddRange(r.ToArray());
            }
            CollectionAssert.AreEqual(pocoExpected, actual);
        }

        [Test]
        public static void LoadPOCOWithAnnotation()
        {
            List<EmployeeRec> actual = new List<EmployeeRec>();
            using (var r = ChoCSVReader<EmployeeRec>.LoadText(csv))
            {
                actual.AddRange(r.ToArray());
            }
            CollectionAssert.AreEqual(pocoWithAnnotationExpected, actual);
        }

        [Test]
        public static void AsDataReaderTest()
        {
            List<EmployeeRec> actual = new List<EmployeeRec>();
            using (var r = ChoCSVReader<EmployeeRec>.LoadText(csv))
            {
                IDataReader dr = r.AsDataReader();
                while (dr.Read())
                {
                    actual.Add(new EmployeeRec { Identifier = dr["Id"].CastTo<int>(), Name = dr[1].CastTo<string>() });
                }

            }
            CollectionAssert.AreEqual(pocoWithAnnotationExpected, actual);
        }
        [Test]
        public static void AsDataTableTest()
        {
            List<EmployeeRec> actual = new List<EmployeeRec>();
            using (var r = ChoCSVReader<EmployeeRec>.LoadText(csv))
            {
                DataTable dt = r.AsDataTable();
                foreach (DataRow dr in dt.Rows)
                {
                    actual.Add(new EmployeeRec { Identifier = dr[0].CastTo<int>(), Name = dr[1].CastTo<string>() });
                }
            }
            CollectionAssert.AreEqual(pocoWithAnnotationExpected, actual);
        }

        [Test]
        public static void CurrencyDynamicTestUsingConfig()
        {
            List<object> actual = new List<object>();

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2) { FieldType = typeof(string) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });

            using (var parser = ChoCSVReader.LoadText(currencyCSV, config).WithFirstLineHeader())
            {
                actual.AddRange(parser.ToArray());
            }
            CollectionAssert.AreEqual(currencyExpected, actual);
        }

        [Test]
        public static void CurrencyDynamicTestUsingFluentAPI()
        {
            List<object> actual = new List<object>();

            using (var parser = ChoCSVReader.LoadText(currencyCSV)
                .WithFirstLineHeader()
                .WithField("Id", fieldType: typeof(int))
                .WithField("Name", fieldType: typeof(string))
                .WithField("Salary", fieldType: typeof(ChoCurrency))
                )
            {
                actual.AddRange(parser.ToArray());
            }
            CollectionAssert.AreEqual(currencyExpected, actual);
        }

        [Test]
        public static void CurrencyDynamicTestUsingAutoDetectMode()
        {
            List<object> actual = new List<object>();

            using (var parser = ChoCSVReader.LoadText(currencyCSV)
                .WithFirstLineHeader()
                .WithMaxScanRows(1)
                .Configure(c => c.TypeConverterFormatSpec.TreatCurrencyAsDecimal = false)
                )
            {
                actual.AddRange(parser.ToArray());
            }
            CollectionAssert.AreEqual(currencyExpected, actual);
        }

        [Test]
        public static void CurrencyAsDecimalDynamicTestUsingConfig()
        {
            List<object> actual = new List<object>();

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2) { FieldType = typeof(string) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(decimal) });

            using (var parser = ChoCSVReader.LoadText(currencyCSV, config).WithFirstLineHeader())
            {
                actual.AddRange(parser.ToArray());
            }
            CollectionAssert.AreEqual(currencyAsDecimalExpected, actual);
        }

        [Test]
        public static void CurrencyAsDecimalDynamicTestUsingFluentAPI()
        {
            List<object> actual = new List<object>();

            using (var parser = ChoCSVReader.LoadText(currencyCSV)
                .WithFirstLineHeader()
                .WithField("Id", fieldType: typeof(int))
                .WithField("Name", fieldType: typeof(string))
                .WithField("Salary", fieldType: typeof(decimal))
                )
            {
                actual.AddRange(parser.ToArray());
            }
            CollectionAssert.AreEqual(currencyAsDecimalExpected, actual);
        }

        [Test]
        public static void CurrencyAsDecimalDynamicTestUsingAutoDetectMode()
        {
            List<object> actual = new List<object>();

            using (var parser = ChoCSVReader.LoadText(currencyCSV)
                .WithFirstLineHeader()
                .WithMaxScanRows(1)
                //.Configure(c => c.TypeConverterFormatSpec.TreatCurrencyAsDecimal = true)
                )
            {
                actual.AddRange(parser.ToArray());
            }
            CollectionAssert.AreEqual(currencyAsDecimalExpected, actual);
        }

        [Test]
        public static void CSVWithQuoteValuesTest()
        {
            List<object> actual = new List<object>();
            dynamic rec;
            using (var r = ChoCSVReader.LoadText(csvWithQuotes)
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                )
            {
                while ((rec = r.Read()) != null)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(dynamicExpected, actual);
        }

        [Test]
        public static void CSVWithEOLInValuesTest()
        {
            List<object> actual = new List<object>();
            dynamic rec;
            using (var r = ChoCSVReader.LoadText(csvWithEOLInValues)
                .WithFirstLineHeader()
                .MayContainEOLInData()
                )
            {
                while ((rec = r.Read()) != null)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(dynamicWithEOLExpected, actual);
        }
        [Test]
        public static void LoadByIndexOrNameTest()
        {
            string csv = @"Id, Name, Zip
1, Tom, 10010
2, Mark, 08830";
            List<EmpWithZip> expected = new List<EmpWithZip> {
                new EmpWithZip { Identifier = 1, EmpName = "Tom", ZipCode = "10010" },
                new EmpWithZip { Identifier = 2, EmpName = "Mark", ZipCode = "08830" },
            };

            List<EmpWithZip> actual = new List<EmpWithZip>();
            using (var r = ChoCSVReader<EmpWithZip>.LoadText(csv)
                .WithFirstLineHeader(false)
                .ThrowAndStopOnMissingField(false)
                .Configure(c => c.AllowLoadingFieldByPosition = true)
                )
                actual.AddRange(r.ToArray());

            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void HeaderLineAtTest()
        {
            string csv = @"Generic Text
Generict Text
Header 1,Header 2,Header 3,Header 4 
Val 1,Val 2,Val 3,Val 4 
Val 1,Val 2,Val 3,Val 4";

            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Col1", "Val 1" }, { "Col2", "Val 2" }, { "Col3", "Val 3" }, { "Col4", "Val 4" }},
                new ChoDynamicObject {{ "Col1", "Val 1" }, { "Col2", "Val 2" }, { "Col3", "Val 3" }, { "Col4", "Val 4" }},
            };

            List<object> actual = new List<object>();
            using (var r = ChoCSVReader.LoadText(csv)
                   .WithField("Col1")
                   .WithField("Col2")
                   .WithField("Col3")
                   .WithField("Col4")
                   .HeaderLineAt(3)
                   .WithFirstLineHeader(true)
                   .ThrowAndStopOnMissingField(false)
                  )
            {
                actual.AddRange(r.ToArray());
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void ScientificNotationdecimalsTest()
        {
            string expected = @"[
  {
    ""a"": 1.2,
    ""b"": 3.4,
    ""RN"": null,
    ""TimeStamp"": null
  },
  {
    ""a"": 1.2E-05,
    ""b"": 7.8,
    ""RN"": null,
    ""TimeStamp"": null
  }
]";

            //ChoTypeConverterFormatSpec.Instance.LongNumberStyle = NumberStyles.Number | NumberStyles.AllowExponent;
            string csv = @"a,b
1.2,3.4
1.2e-05,7.8";

            using (var r = ChoCSVReader<ScientificNotationdecimal>.LoadText(csv)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                )
            {
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
                //r.Print();
                //r.WithField(f => f.RN, () => r.RecordNumber);
                //r.WithField(f => f.TimeStamp, () => DateTime.Now);

                ////r.Print();
                //foreach (var rec in r.Select(s => new { RowNo = r.RecordNumber, Rec = s }))
                //    rec.Print();
            }
        }

        [Test]
        public static void TypeConverterTest()
        {
            string csv = @"IPAddress,Description
10.0.0.0,Main
10.0.128.0,Sub1
10.0.128.16,Sub2";

            List<IpAddressRecord> expected = new List<IpAddressRecord>
            {
                new IpAddressRecord { Description = "Main", IPAddress = IPAddress.Parse("10.0.0.0")},
                new IpAddressRecord { Description = "Sub1", IPAddress = IPAddress.Parse("10.0.128.0")},
                new IpAddressRecord { Description = "Sub2", IPAddress = IPAddress.Parse("10.0.128.16")},
            };

            List<IpAddressRecord> actual = new List<IpAddressRecord>();
            using (var r = ChoCSVReader<IpAddressRecord>.LoadText(csv)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                )
            {
                actual.AddRange(r.ToArray());
                actual.Print();
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void DefaultValueTest()
        {
            string csv = @"Id, Name
1, 
2, Mark";

            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name", "XXX" }},
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Mark" }},
            };
            List<object> actual = new List<object>();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithField("Id")
                .WithField("Name", defaultValue: "XXX")
                )
                actual.AddRange(r.ToArray());

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DefaultValuePOCOTest()
        {
            string csv = @"Id, Name
1, 
2, Mark";

            List<EmployeeRec> expected = new List<EmployeeRec> {
                new EmployeeRec { Identifier = 1, Name = "XXXX"},
                new EmployeeRec { Identifier = 2, Name = "Mark"},
            };
            List<EmployeeRec> actual = new List<EmployeeRec>();
            using (var r = ChoCSVReader<EmployeeRec>.LoadText(csv)
                )
                actual.AddRange(r.ToArray());

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ValidateCSVDynamic_AtMemberLevel_Test()
        {
            Assert.Throws<ChoReaderException>(delegate
            {
                string csv = @"Id, Name
, 
2, Mark";

                List<object> actual = new List<object>();

                using (var r = ChoCSVReader.LoadText(csv)
                    .WithFirstLineHeader()
                    .ValidationMode(ChoObjectValidationMode.MemberLevel)
                    .WithField("Id", propertyValidator: o => o != null)
                    .WithField("Name")
                )
                {
                    actual.AddRange(r.ToArray());
                }
            });
        }

        [Test]
        public static void ValidateCSVDynamic_AtObjectLevel_Test()
        {
            Assert.Throws<ValidationException>(delegate
            {
                string csv = @"Id, Name
, 
2, Mark";

                List<object> actual = new List<object>();

                using (var r = ChoCSVReader.LoadText(csv)
                    .WithFirstLineHeader()
                    .ValidationMode(ChoObjectValidationMode.ObjectLevel)
                    .WithField("Id")
                    .WithField("Name")
                    .ObjectLevelValidator(rec =>
                    {
                        if (rec.Id == null)
                            throw new ValidationException("Id value is required.");

                        return true;
                    })
                )
                {
                    actual.AddRange(r.ToArray());
                }
            });
        }

        [Test]
        public static void ValidateCSVPOCO_AtMemberLevel_Test()
        {
            Assert.Throws<ChoReaderException>(delegate
            {
                string csv = @"Id, Name
, 
2, Mark";

                List<EmployeeRec> actual = new List<EmployeeRec>();

                using (var r = ChoCSVReader<EmployeeRec>.LoadText(csv)
                    .ValidationMode(ChoObjectValidationMode.MemberLevel)
                    )
                {
                    actual.AddRange(r.ToArray());
                }
            });
        }

        [Test]
        public static void ValidateCSVPOCO_AtObjectLevel_Test()
        {
            Assert.Throws<ValidationException>(delegate
            {
                string csv = @"Id, Name
, 
2, Mark";

                List<EmployeeRec> actual = new List<EmployeeRec>();

                using (var r = ChoCSVReader<EmployeeRec>.LoadText(csv)
                    )
                {
                    actual.AddRange(r.ToArray());
                }
            });
        }

        [Test]
        public static void ValidateCSVDynamic_WithIgnoreAndContinue_AtObjectLevel_Test()
        {
            string csv = @"Id, Name
, 
2, Mark";

            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Mark" }},
            };
            List<object> actual = new List<object>();

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .ValidationMode(ChoObjectValidationMode.ObjectLevel)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .WithField("Id")
                .WithField("Name", defaultValue: "XXXX")
                .ObjectLevelValidator((rec) =>
                {
                    if (rec.Id == null)
                        throw new ValidationException("Id field is required.");
                    return true;
                })
                )
            {
                actual.AddRange(r.ToArray());
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ValidateCSVDynamic_WithIgnoreAndContinue_AtMemberLevel_Test()
        {
            string csv = @"Id, Name
, 
2, Mark";

            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", null }, { "Name", "XXXX" }},
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Mark" }},
            };
            List<object> actual = new List<object>();

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .ValidationMode(ChoObjectValidationMode.MemberLevel)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .WithField("Id", propertyValidator: o => o != null)
                .WithField("Name", defaultValue: "XXXX")
                )
            {
                actual.AddRange(r.ToArray());
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ValidateCSVPOCO_WithIgnoreAndContinue_Test()
        {
            string csv = @"Id, Name
, 
2, Mark";

            List<EmployeeRec> expected = new List<EmployeeRec> {
                new EmployeeRec { Identifier = 2, Name = "Mark"},
            };
            List<EmployeeRec> actual = new List<EmployeeRec>();

            using (var r = ChoCSVReader<EmployeeRec>.LoadText(csv)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                actual.AddRange(r.ToArray());
            }

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ValidateCSVDynamic_WithReportAndContinue_Test()
        {
            string csv = @"Id, Name
, 
2, Mark";

            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", null }, { "Name", "XXXX" }},
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Mark" }},
            };
            List<object> actual = new List<object>();
            long failedRecLineIndex = 0;
            string errorMsg = null;

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .ErrorMode(ChoErrorMode.ReportAndContinue)
                .ValidationMode(ChoObjectValidationMode.MemberLevel)
                .WithField("Id", m => m.Value.Validator = v =>
                {
                    if (v == null)
                        throw new ValidationException("Id field is required.");

                    return true;
                })
                .WithField("Name", defaultValue: "XXXX")
                .Setup(s => s.RecordFieldLoadError += (o, e) =>
                {
                    failedRecLineIndex = e.Index;
                    errorMsg = e.Exception.Message;
                    e.Handled = true;
                })
                )
            {
                actual.AddRange(r.ToArray());
            }

            Assert.AreEqual(failedRecLineIndex, 2);
            Assert.AreEqual(errorMsg, "Id field is required.");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ValidateCSVPOCO_WithReportAndContinue_Test()
        {
            string csv = @"Id, Name
, 
2, Mark";

            List<EmployeeRec> expected = new List<EmployeeRec> {
                new EmployeeRec { Identifier = null, Name = "XXXX"},
                new EmployeeRec { Identifier = 2, Name = "Mark"},
            };
            List<EmployeeRec> actual = new List<EmployeeRec>();

            long failedRecLineIndex = 0;
            string errorMsg = null;
            using (var r = ChoCSVReader<EmployeeRec>.LoadText(csv)
                .ErrorMode(ChoErrorMode.ReportAndContinue)
                .Setup(s => s.RecordLoadError += (o, e) =>
                {
                    failedRecLineIndex = e.Index;
                    errorMsg = e.Exception.Message;
                    e.Handled = true;
                })
                )
            {
                actual.AddRange(r.ToArray());
            }

            Assert.AreEqual(failedRecLineIndex, 2);
            Assert.AreEqual(errorMsg, "Failed to validate 'ChoCSVReaderTest.Test.EmployeeRec' object. \r\nThe Identifier field is required.\r\n");
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void ExcelSeparatorTest()
        {
            string csv = @"sep=|
Id|Name
1|Tom 
2|Mark";

            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name", "Tom" }},
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Mark" }},
            };
            List<object> actual = new List<object>();

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                actual.AddRange(r.ToArray());
            }

            Assert.AreEqual(expected, actual);
        }
    }
    public class ScientificNotationdecimal
    {
        //[ChoTypeConverterParams(Parameters = NumberStyles.Number | NumberStyles.AllowExponent)]
        public double a { get; set; }
        //[ChoTypeConverterParams(Parameters = NumberStyles.Number | NumberStyles.AllowExponent)]
        public double b { get; set; }
        public long? RN { get; set; }
        public DateTime? TimeStamp { get; set; }
    }

    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override bool Equals(object other)
        {
            var toCompareWith = other as Employee;
            if (toCompareWith == null)
                return false;
            return this.Id == toCompareWith.Id &&
                this.Name == toCompareWith.Name;
        }
        public override int GetHashCode()
        {
            return new { Id, Name }.GetHashCode();
        }
    }
    public class EmpWithZip
    {
        [ChoFieldPosition(1)]
        public int Identifier { get; set; }
        [DisplayName("Name")]
        public string EmpName { get; set; }
        [DisplayName("Zip")]
        public string ZipCode { get; set; }

        public override bool Equals(object other)
        {
            var toCompareWith = other as EmpWithZip;
            if (toCompareWith == null)
                return false;
            return this.Identifier == toCompareWith.Identifier;
        }
        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }

    [ChoCSVFileHeader]
    internal class IpAddressRecord
    {
        [ChoCSVRecordField(FieldName = "Description")]
        public string Description { get; set; }

        [ChoCSVRecordField(FieldName = "IPAddress"),
         ChoTypeConverter(typeof(IpAddressTypeConverter))]
        public IPAddress IPAddress { get; set; }

        public override bool Equals(object other)
        {
            var toCompareWith = other as IpAddressRecord;
            if (toCompareWith == null)
                return false;
            return this.Description == toCompareWith.Description &&
                this.IPAddress.Equals(toCompareWith.IPAddress);
        }
        public override int GetHashCode()
        {
            return new { Description, IPAddress }.GetHashCode();
        }
    }
    internal class IpAddressTypeConverter : IChoValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string workValue;
            IPAddress workIpAddress;

            workValue = value as string;

            System.Diagnostics.Debug.WriteLine("Convert");
            Console.WriteLine("Convert");

            if (workValue is null)
            {
                return null;
            }

            if (IPAddress.TryParse(workValue, out workIpAddress))
            {
                return workIpAddress;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.WriteLine("ConvertBack");
            Console.WriteLine("ConvertBack");

            if (value == null)
            {
                return null;
            }
            return ((IPAddress)value).ToString();
        }
    }
    [ChoCSVFileHeader]
    [ChoCSVRecordObject(ObjectValidationMode = ChoObjectValidationMode.ObjectLevel)]
    public class EmployeeRec
    {
        [Required]
        [ChoCSVRecordField(FieldName = "Id")]
        public int? Identifier
        {
            get;
            set;
        }
        [DefaultValue("XXXX")]
        [ChoCSVRecordField(FieldName = "Name")]
        public string Name
        {
            get;
            set;
        }

        public override string ToString()
        {
            return $"{Identifier}. {Name}.";
        }

        public override bool Equals(object other)
        {
            var toCompareWith = other as EmployeeRec;
            if (toCompareWith == null)
                return false;
            return this.Identifier == toCompareWith.Identifier &&
                this.Name == toCompareWith.Name;
        }
        public override int GetHashCode()
        {
            return new { Identifier, Name }.GetHashCode();
        }
    }
}
