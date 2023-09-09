using NUnit.Framework;
using ChoETL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Security;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System.Data.SqlClient;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;
using UnitTestHelper;
using System.Runtime.Serialization;
using ChoCSVReaderTest.Test;
using System.Security.Cryptography;
using NUnit.Framework.Constraints;
using System.Windows.Navigation;
using System.Configuration;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoCSVReaderTest
{
    public class SitePostal
    {
        [Required(ErrorMessage = "State is required")]
        [RegularExpression("^[A-Z][A-Z]$", ErrorMessage = "Incorrect zip code.")]
        public string State { get; set; }
        [Required]
        [RegularExpression("^[0-9][0-9]*$")]
        public string Zip { get; set; }

        public override bool Equals(object obj)
        {
            var postal = obj as SitePostal;
            return postal != null &&
                   State == postal.State &&
                   Zip == postal.Zip;
        }

        public override int GetHashCode()
        {
            var hashCode = -1083755174;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(State);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Zip);
            return hashCode;
        }
    }
    public class SiteAddress
    {
        [Required]
        //[ChoCSVRecordField(3)]
        public string Street { get; set; }
        [Required]
        [RegularExpression("^[a-zA-Z][a-zA-Z ]*$")]
        public string City { get; set; }
        [ChoValidateObject]
        public SitePostal SitePostal { get; set; }

        public override bool Equals(object obj)
        {
            var address = obj as SiteAddress;
            return address != null &&
                   Street == address.Street &&
                   City == address.City &&
                   EqualityComparer<SitePostal>.Default.Equals(SitePostal, address.SitePostal);
        }

        public override int GetHashCode()
        {
            var hashCode = -1241015971;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Street);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(City);
            hashCode = hashCode * -1521134295 + EqualityComparer<SitePostal>.Default.GetHashCode(SitePostal);
            return hashCode;
        }
    }
    public class Site
    {
        public int SiteID { get; set; }
        public string House { get; set; }
        public SiteAddress SiteAddress { get; set; }

        public override bool Equals(object obj)
        {
            var site = obj as Site;
            return site != null &&
                   SiteID == site.SiteID &&
                   House == site.House &&
                   EqualityComparer<SiteAddress>.Default.Equals(SiteAddress, site.SiteAddress);
        }

        public override int GetHashCode()
        {
            var hashCode = -448004452;
            hashCode = hashCode * -1521134295 + SiteID.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(House);
            hashCode = hashCode * -1521134295 + EqualityComparer<SiteAddress>.Default.GetHashCode(SiteAddress);
            return hashCode;
        }
    }

    [ChoMetadataRefType(typeof(Site))]
    [ChoCSVRecordObject(ObjectValidationMode = ChoObjectValidationMode.ObjectLevel, ErrorMode = ChoErrorMode.IgnoreAndContinue, IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any, ThrowAndStopOnMissingField = false)]
    public class SiteMetadata
    {
        [Required(ErrorMessage = "SiteID can't be null")]
        //[ChoCSVRecordField(1)]
        public int SiteID { get; set; }
        [Required]
        public string House { get; set; }
        [ChoValidateObject]
        public SiteAddress SiteAddress { get; set; }
        //public int Apartment { get; set; }
    }

    public class EmpWithAddress
    {
        public int Id { get; set; }
        [ChoCSVRecordField(2)]
        public string Name { get; set; }
        [ChoCSVRecordField(3)]
        public string JsonValue { get; set; }
        [ChoIgnoreMember]
        public string product_version_id { get; set; }
        [ChoIgnoreMember]
        public string product_version_name { get; set; }
    }

    public class PlayerAttr
    {
        public int Str { get; set; }
        public int Agi { get; set; }

    }
    public class PlayerPer
    {
        public int Lea { get; set; }
        public int Wor { get; set; }

    }
    public class PlayerSkills
    {
        public int WR { get; set; }
        public int TE { get; set; }

    }
    public class Player
    {
        public Player(dynamic obj)
        {
            Id = ChoUtility.CastTo<int>(obj.Id);
            Sea = ChoUtility.CastTo<int>(obj.Sea);
            First = obj.First;
            Last = obj.Last;
            Team = obj.Team;
            Coll = obj.Coll;
            Num = ChoUtility.CastTo<int>(obj.Num);
            Age = ChoUtility.CastTo<int>(obj.Age);
            Hgt = ChoUtility.CastTo<int>(obj.Hgt);
            Wgt = ChoUtility.CastTo<int>(obj.Wgt);
            Pos = obj.Pos;
            Flg = String.IsNullOrEmpty(obj.Flg) ? "None" : obj.Flg;
            Trait = String.IsNullOrEmpty(obj.Trait) ? "None" : obj.Trait;

            Attr = new PlayerAttr();
            Attr.Str = ChoUtility.CastTo<int>(obj.Attr.Str);
            Attr.Agi = ChoUtility.CastTo<int>(obj.Attr.Agi);

            Per = new PlayerPer();
            Per.Lea = ChoUtility.CastTo<int>(obj.Per.Lea);
            Per.Wor = ChoUtility.CastTo<int>(obj.Per.Wor);


            Skills = new PlayerSkills();
            Skills.WR = ChoUtility.CastTo<int>(obj.Skills.WR);
            Skills.TE = ChoUtility.CastTo<int>(obj.Skills.TE);
        }

        public int Id { get; set; }
        public int Sea { get; set; }
        public string First { get; set; }
        public string Last { get; set; }
        public string Team { get; set; }
        public string Coll { get; set; }
        public int Num { get; set; }
        public int Age { get; set; }
        public int Hgt { get; set; }
        public int Wgt { get; set; }
        public string Pos { get; set; }

        public PlayerAttr Attr { get; set; }
        public PlayerPer Per { get; set; }

        public PlayerSkills Skills { get; set; }
        public string Flg { get; set; }
        public string Trait { get; set; }
    }

    public class Players
    {
        public Player[] players { get; set; }
    }

    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {
        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
            ChoTypeConverter.Global.Add(typeof(CustomString2), new GlobalStringToCustomString2Converter());
        }

        public static string FileNameSample3CSV => "Sample3.csv";
        public static string FileNameSampleDataCSV => "SampleData.csv";
        public static string FileNameNodeDataXML => "NodeData.xml";
        public static string FileNameEmp1CSV => "emp1.csv";
        public static string FileNameNestedJSON => "nested.json";
        public static string FileNameNestedCSV => "nested.csv";
        public static string FileNameCustomNewLineCSV => "CustomNewLine.csv";
        public static string FileNameBadFileCSV => "BadFile.csv";
        public static string FileNameEmpCSV => "Emp.csv";
        public static string FileNameDoubleQuotesTestCSV => "DoubleQuotesTest.csv";
        public static string FileNameTestCSV => "Test.csv";
        public static string FileNameEmpWithSalaryCSV => "empwithsalary.csv";
        public static string FileNameQuoteEscapeCSV => "QuoteEscape.csv";
        public static string FileNameEmpDuplicatesCSV => "EmpDuplicates.csv";
        public static string FileNamePlayersCSV => "Players.csv";
        public static string FileNamePlayersJSON => "Players.json";
        public static string FileNameIgnoreLineFileCSV => "IgnoreLineFile.csv";
        public static string FileNameInterfaceTestCSV => "InterfaceTest.csv";
        public static string FileNameIssue43CSV => "issue43.csv";
        public static string FileNameETLsampletestCSV => @"ETLsampletest.csv";
        public static string FileNamePlanetsCSV => "planets.csv";
        public static string FileNameZipCodesCSV => "zipCodes.csv";
        public static string FileNameMergeInputCSV => "mergeinput.csv";
        public static string FileNameMergeOutputCSV => "mergeoutput.csv";
        public static string FileNameMultiLineValueCSV => "MultiLineValue.csv";
        public static string FileNameNestedQuotesCSV => "NestedQuotes.csv";
        public static string FileNamePontosCSV => "pontos.csv";
        public static string FileNameEmpQuoteCSV => "EmpQuote.csv";
        public static string FileNameQuoteInQouteCSV => "EmpQuoteInQuote.csv";
        public static string FileNameEmptyLinesCSV => "EmptyLines.csv";
        public static string FileNameSample1CSV => "Sample1.csv";
        public static string FileNameSample2CSV => "Sample2.csv";
        public static string FileName020180412_045106CroppedCSV => "020180412_045106Cropped.csv";
        public static string FileNameSample5CSV => "Sample5.csv";
        public static string FileNameSample6CSV => "Sample6.csv";
        public static string FileNameSample7CSV => "Sample7.csv";
        public static string FileNameSolarTempCSV => "SolarTemp.csv";
        public static string FileNameZipCodesExCSV => "ZipCodesEx.csv";

        public class Emp1
        {
            public string Name { get; set; }
            public string Other { get; set; }
            public string MyId { get; set; }

            public override bool Equals(object obj)
            {
                var employee = obj as Emp1;
                return employee != null &&
                       MyId == employee.MyId &&
                       Name == employee.Name &&
                       Other == employee.Other;
            }

            public override int GetHashCode()
            {
                var hashCode = -1768068776;
                hashCode = hashCode * -1521134295 + MyId.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Other);
                return hashCode;
            }
        }

        public class GlobalStringToCustomString2Converter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                nameof(GlobalStringToCustomString2Converter).Print();
                if (value is string val)
                {
                    return new CustomString2(val);
                }
                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class LocalStringToCustomStringConverter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                nameof(LocalStringToCustomStringConverter).Print();
                if (value is string val)
                {
                    return new CustomString(val);
                }
                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class StringToCustomStringConverter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                nameof(StringToCustomStringConverter).Print();
                if (value is string val)
                {
                    return new CustomString(val);
                }
                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public struct CustomString
        {
            public string Value { get; }

            public CustomString(string val)
            {
                if (string.IsNullOrWhiteSpace(val))
                {
                    Value = null;
                }
                else
                {
                    Value = val.ToUpper();
                }
            }

            public static implicit operator string(CustomString s) => s.Value;
            public static explicit operator CustomString(string s) => new CustomString(s);
            public override string ToString() => Value;
        }
        public class SalesAddress
        {
            [ChoTypeConverter(typeof(StringToCustomStringConverter))]
            public CustomString Street { get; set; }
            [ChoTypeConverter(typeof(StringToCustomStringConverter))]
            public CustomString City { get; set; }
            [ChoTypeConverter(typeof(StringToCustomStringConverter))]
            public CustomString Zip { get; set; }
        }

        public class SalesOrder
        {
            public string OrderNo { get; set; }
            public string OrderName { get; set; }
            public SalesAddress SalesAddress { get; set; }
        }

        [Test]
        public static void LoadCSVIntoCustomStringObject_MemberLevelConverter()
        {
            string csv = @"OrderNo, OrderName, SalesAddress.Street, SalesAddress.City, SalesAddress.Zip
1, Name1, Main Street, New York, 10010";

            string expected = @"[
  {
    ""OrderNo"": ""1"",
    ""OrderName"": ""Name1"",
    ""SalesAddress"": {
      ""Street"": {
        ""Value"": ""MAIN STREET""
      },
      ""City"": {
        ""Value"": ""NEW YORK""
      },
      ""Zip"": {
        ""Value"": ""10010""
      }
    }
  }
]";
            using (var r = ChoCSVReader<SalesOrder>.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                actual.ToString();

                Assert.AreEqual(expected, actual);
            }

        }
        public class SalesAddress1
        {
            public CustomString Street { get; set; }
            public CustomString City { get; set; }
            public CustomString Zip { get; set; }
        }

        public class SalesOrder1
        {
            public string OrderNo { get; set; }
            public string OrderName { get; set; }
            public SalesAddress1 SalesAddress { get; set; }
        }

        [Test]
        public static void LoadCSVIntoCustomStringObject_UsingConfig_NoConverters()
        {
            string csv = @"OrderNo, OrderName, Street, City, Zip
1, Name1, Main Street, New York, 10010";

            string expected = @"[
  {
    ""OrderNo"": ""1"",
    ""OrderName"": ""Name1"",
    ""SalesAddress"": {
      ""Street"": {
        ""Value"": ""MAIN STREET""
      },
      ""City"": {
        ""Value"": ""NEW YORK""
      },
      ""Zip"": {
        ""Value"": ""10010""
      }
    }
  }
]";
            var config = new ChoCSVRecordConfiguration<SalesOrder1>()
                .WithFirstLineHeader()
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure(c => c.IgnoreEmptyLine = true)
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true);

            config.Map(f => f.SalesAddress.Street, "Street");
            config.Map(f => f.SalesAddress.City, "City");
            config.Map(f => f.SalesAddress.Zip, "Zip");

            using (var r = ChoCSVReader<SalesOrder1>.LoadText(csv, config)
                .WithFirstLineHeader()
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                actual.ToString();

                Assert.AreEqual(expected, actual);
            }

        }

        public struct CustomString2
        {
            public string Value { get; }

            public CustomString2(string val)
            {
                if (string.IsNullOrWhiteSpace(val))
                {
                    Value = null;
                }
                else
                {
                    Value = val.ToUpper();
                }
            }

            public static implicit operator string(CustomString2 s) => s.Value;
            public static explicit operator CustomString2(string s) => new CustomString2(s);
            public override string ToString() => Value;
        }

        public class SalesAddress2
        {
            public CustomString2 Street { get; set; }
            public CustomString2 City { get; set; }
            public CustomString2 Zip { get; set; }
        }

        public class SalesOrder2
        {
            public string OrderNo { get; set; }
            public string OrderName { get; set; }
            public SalesAddress2 SalesAddress { get; set; }
        }

        [Test]
        public static void LoadCSVIntoCustomStringObject_UsingGlobalConverter()
        {
            string csv = @"OrderNo, OrderName, Street, City, Zip
1, Name1, Main Street, New York, 10010";

            string expected = @"[
  {
    ""OrderNo"": ""1"",
    ""OrderName"": ""Name1"",
    ""SalesAddress"": {
      ""Street"": {
        ""Value"": ""MAIN STREET""
      },
      ""City"": {
        ""Value"": ""NEW YORK""
      },
      ""Zip"": {
        ""Value"": ""10010""
      }
    }
  }
]";
            //Global converter setup at [Setup] routine
            //ChoTypeConverter.Global.Add(typeof(CustomString2), new GlobalStringToCustomStringConverter());

            var config = new ChoCSVRecordConfiguration<SalesOrder2>()
                .WithFirstLineHeader()
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure(c => c.IgnoreEmptyLine = true)
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true);

            config.Map(f => f.SalesAddress.Street, "Street");
            config.Map(f => f.SalesAddress.City, "City");
            config.Map(f => f.SalesAddress.Zip, "Zip");

            using (var r = ChoCSVReader<SalesOrder2>.LoadText(csv, config)
                .WithFirstLineHeader()
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                actual.ToString();

                Assert.AreEqual(expected, actual);
            }

        }

        [Test]
        public static void LoadCSVIntoCustomStringObject_UsingLocalTypeConverter()
        {
            string csv = @"OrderNo, OrderName, Street, City, Zip
1, Name1, Main Street, New York, 10010";

            string expected = @"[
  {
    ""OrderNo"": ""1"",
    ""OrderName"": ""Name1"",
    ""SalesAddress"": {
      ""Street"": {
        ""Value"": ""MAIN STREET""
      },
      ""City"": {
        ""Value"": ""NEW YORK""
      },
      ""Zip"": {
        ""Value"": ""10010""
      }
    }
  }
]";
            var config = new ChoCSVRecordConfiguration<SalesOrder1>()
                .WithFirstLineHeader()
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure(c => c.IgnoreEmptyLine = true)
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true);

            config.RegisterTypeConverterForType<CustomString>(new LocalStringToCustomStringConverter());

            config.Map(f => f.SalesAddress.Street, "Street");
            config.Map(f => f.SalesAddress.City, "City");
            config.Map(f => f.SalesAddress.Zip, "Zip");

            using (var r = ChoCSVReader<SalesOrder1>.LoadText(csv, config)
                .WithFirstLineHeader()
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                actual.ToString();

                Assert.AreEqual(expected, actual);
            }

        }


        public class DeclarativeStringToCustomStringConverter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                nameof(LocalStringToCustomStringConverter).Print();
                if (value is string val)
                {
                    return new CustomString(val);
                }
                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class SalesAddress3
        {
            public CustomString3 Street { get; set; }
            public CustomString3 City { get; set; }
            public CustomString3 Zip { get; set; }
        }

        public class SalesOrder3
        {
            public string OrderNo { get; set; }
            public string OrderName { get; set; }
            public SalesAddress3 SalesAddress { get; set; }
        }
        [ChoTypeConverter(typeof(StringToCustomString3Converter))]
        public struct CustomString3
        {
            public string Value { get; }

            public CustomString3(string val)
            {
                if (string.IsNullOrWhiteSpace(val))
                {
                    Value = null;
                }
                else
                {
                    Value = val.ToUpper();
                }
            }

            public static implicit operator string(CustomString3 s) => s.Value;
            public static explicit operator CustomString3(string s) => new CustomString3(s);
            public override string ToString() => Value;
        }
        public class StringToCustomString3Converter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                nameof(StringToCustomString3Converter).Print();
                if (value is string val)
                {
                    return new CustomString3(val);
                }
                return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }


        [Test]
        public static void LoadCSVIntoCustomStringObject_UsingDeclarativeTypeConverter()
        {
            string csv = @"OrderNo, OrderName, Street, City, Zip
1, Name1, Main Street, New York, 10010";

            string expected = @"[
  {
    ""OrderNo"": ""1"",
    ""OrderName"": ""Name1"",
    ""SalesAddress"": {
      ""Street"": {
        ""Value"": ""MAIN STREET""
      },
      ""City"": {
        ""Value"": ""NEW YORK""
      },
      ""Zip"": {
        ""Value"": ""10010""
      }
    }
  }
]";
            var config = new ChoCSVRecordConfiguration<SalesOrder3>()
                .WithFirstLineHeader()
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure(c => c.IgnoreEmptyLine = true)
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true);

            config.Map(f => f.SalesAddress.Street, "Street");
            config.Map(f => f.SalesAddress.City, "City");
            config.Map(f => f.SalesAddress.Zip, "Zip");

            using (var r = ChoCSVReader<SalesOrder3>.LoadText(csv, config)
                .WithFirstLineHeader()
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                actual.ToString();

                Assert.AreEqual(expected, actual);
            }

        }
        [Test]
        public static void MapColumnAtRuntimeTest()
        {
            List<Emp1> expected = new List<Emp1>
            {
                new Emp1 { MyId = "1", Name = "Tom", Other = "NY" },
                new Emp1 { MyId = "2", Name = "Mark", Other = "NJ" },
                new Emp1 { MyId = "3", Name = "Lou", Other = "FL" },
                new Emp1 { MyId = "4", Name = "Smith", Other = "PA" },
                new Emp1 { MyId = "5", Name = "Raj", Other = "DC" },
            };

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("MyId", 1));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Other", 3));
            config.WithFirstLineHeader(true);

            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";
            List<Emp1> actual = new List<Emp1>();
            using (var p = ChoCSVReader<Emp1>.LoadText(csv, Encoding.ASCII, config))
            {
                actual.AddRange(p);
            }

            CollectionAssert.AreEqual(expected, actual);
        }


        [Test]
        public static void MapColumnAtRuntimeTest1()
        {
            List<Emp1> expected = new List<Emp1>
            {
                new Emp1 { MyId = "1", Name = "Tom", Other = "NY" },
                new Emp1 { MyId = "2", Name = "Mark", Other = "NJ" },
                new Emp1 { MyId = "3", Name = "Lou", Other = "FL" },
                new Emp1 { MyId = "4", Name = "Smith", Other = "PA" },
                new Emp1 { MyId = "5", Name = "Raj", Other = "DC" },
            };

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("MyId", 0) { FieldName = "Id" });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 0) { FieldName = "Name" });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Other", 0) { FieldName = "City" });
            config.WithFirstLineHeader(false);

            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";
            List<Emp1> actual = new List<Emp1>();
            using (var p = ChoCSVReader<Emp1>.LoadText(csv, Encoding.ASCII, config))
            {
                actual.AddRange(p);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [ChoCSVFileHeader(IgnoreHeader = true)]
        public class Emp2
        {
            [ChoCSVRecordField(2)]
            public string Name { get; set; }
            [ChoCSVRecordField(3)]
            public string Other { get; set; }
            [ChoCSVRecordField(1)]
            public string MyId { get; set; }

            public override bool Equals(object obj)
            {
                var employee = obj as Emp2;
                return employee != null &&
                       MyId == employee.MyId &&
                       Name == employee.Name &&
                       Other == employee.Other;
            }

            public override int GetHashCode()
            {
                var hashCode = -1768068776;
                hashCode = hashCode * -1521134295 + MyId.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Other);
                return hashCode;
            }
        }


        [Test]
        public static void MapColumnAtRuntimeTest2()
        {
            List<Emp2> expected = new List<Emp2>
            {
                new Emp2 { MyId = "1", Name = "Tom", Other = "NY" },
                new Emp2 { MyId = "2", Name = "Mark", Other = "NJ" },
                new Emp2 { MyId = "3", Name = "Lou", Other = "FL" },
                new Emp2 { MyId = "4", Name = "Smith", Other = "PA" },
                new Emp2 { MyId = "5", Name = "Raj", Other = "DC" },
            };

            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";
            List<Emp2> actual = new List<Emp2>();
            using (var p = ChoCSVReader<Emp2>.LoadText(csv, Encoding.ASCII))
            {
                actual.AddRange(p);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void ConvertToNestedObjects()
        {
            string expected = @"[
  {
    ""id"": ""0"",
    ""name"": ""Test123"",
    ""category"": {
      ""0"": ""15"",
      ""name"": ""Cat123"",
      ""subcategory"": {
        ""id"": ""10"",
        ""name"": ""SubCat123""
      }
    },
    ""description"": ""Desc123""
  }
]";
            using (var json = new ChoJSONWriter(FileNameNestedJSON)
                .Configure(c => c.UseJSONSerialization = false)
                .Configure(c => c.JsonArrayQualifier = (key, obj) =>
                {
                    if (key == "category")
                        return false;

                    return null;
                })
            )
            {
                using (var csv = new ChoCSVReader(FileNameNestedCSV).WithFirstLineHeader()
                    .Configure(c => c.NestedKeySeparator = '/')
                    )
                {
                    var recs = csv.ToArray();
                    json.Write(recs.ExternalSort(new ChoLamdaComparer<dynamic>((e1, e2) => String.Compare(e1.description, e2.description))));
                }
            }

            string actual = new StreamReader(FileNameNestedJSON).ReadToEnd();
            Assert.AreEqual(expected, actual);
            // TODO: Change simple string compare to better JSON content compare
            return;
            ExpandoObject dict = new ExpandoObject();
            IDictionary<string, object> root = dict as IDictionary<string, object>;

            root.Add("id", 1);
            root.Add("name", "NYC");
            root.Add("category/id /", 11);
            root.Add("category /name ", "NJ");
            root.Add("category/subcategory/id", 111);
            root.Add("category/subcategory/name", "MA");

            using (var json = new ChoJSONWriter<dynamic>("nested.json"))
                json.Write(dict.ConvertToNestedObject());
        }

        [Test]
        public static void LoadPlanets()
        {
            List<string> expected = new List<string> {
                "9 c",
                "1 b",
                "10 b",
                "11 b",
                "12A b",
                "2 b",
                "3 b",
                "4 b",
                "5 b",
                "6 b",
                "7 b",
                "8 b"
            };
            List<object> actual = new List<object>();

            using (var p = new ChoCSVReader(FileNamePlanetsCSV).WithFirstLineHeader().Configure(c => c.Comments = new string[] { "#" })
                //.Configure(c => c.CultureName = "en-CA")
                //.Configure(c => c.MaxScanRows = 10)
                .Setup(r => r.BeforeRecordLoad += (o, e) =>
                {
                    e.Skip = ((string)e.Source).StartsWith("3490");
                })
                .Setup(r => r.MembersDiscovered += (o, e) =>
                {
                    //e.Value["rowid"] = typeof(long);
                })
                .Setup(r => r.RecordLoadError += (o, e) =>
                {
                    Console.WriteLine("@@" + e.Source.ToNString());
                    e.Handled = true;
                })
                .Setup(r => r.AfterRecordLoad += (o, e) =>
                {
                    Console.WriteLine("!!" + e.Source.ToNString());
                })
                )
            {
                foreach (dynamic rec in p.Take(12).ExternalSort(new ChoLamdaComparer<dynamic>((e1, e2) => Math.Sign(-2 * String.Compare(e1.pl_letter, e2.pl_letter) + String.Compare(e1.rowid, e2.rowid)))))
                    actual.Add(rec.rowid + " " + rec.pl_letter);
                //                Console.WriteLine(rec.rowid + " " + rec.pl_letter);

                Assert.IsTrue(p.IsValid);
                //using (var w = new ChoJSONWriter("planets.json"))
                //{
                //    w.Write(p);
                //}
            }
            CollectionAssert.AreEqual(expected, actual);

            //foreach (var x in new ChoCSVReader("planets1.csv").WithFirstLineHeader().Configure(c => c.Comments = new string[] { "#" }).Take(1))
            //{
            //    Console.WriteLine(x.Count);

            //    //Console.WriteLine(ChoUtility.ToStringEx(x));
            //}
        }

        public class Quote
        {
            [ChoCSVRecordField(14)]
            public int F1 { get; set; }
            //[DefaultValue(10)]
            [ChoCSVRecordField(15)]
            public int F2 { get; set; }
            [ChoCSVRecordField(16)]
            public int F3 { get; set; }
        }

        [Test]
        public static void FindDuplicates()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", (int)3 }, { "Name", "Lou"} },
                new ChoDynamicObject {{ "Id", (int)4 }, { "Name", "Austin" } }
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoCSVReader(FileNameEmpDuplicatesCSV).WithFirstLineHeader()
                .Configure(c => c.MaxScanRows = 5)
                )
            {
                foreach (dynamic c in parser.GroupBy(r => r.Id).Where(g => g.Count() > 1).Select(g => g.FirstOrDefault()))
                    actual.Add(c);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void NestedQuotes()
        {
            //List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
            //    new ChoDynamicObject {{ "Column1", "Name1" }, { "Column2", "A test, which fails all the time" }, },
            //    new ChoDynamicObject {{ "Column1", "Name2" }, { "Column2", "A test, which fails all the time" }, },
            //    new ChoDynamicObject {{ "Column1", "Name3" }, { "Column2", "A test, which fails all the time" }, }
            //};
            //List<object> actual = new List<object>();

            //using (var parser = new ChoCSVReader("NestedQuotes.csv")
            //    .WithFields("name", "desc")
            //    )
            //{
            //    foreach (dynamic x in parser)
            //        Console.WriteLine(x.name + "-" + x.desc);
            //}

            string expected = @"[
  {
    ""Column1"": ""Name1"",
    ""Column2"": ""A test, which \""fails\"" all the time""
  },
  {
    ""Column1"": ""Name2"",
    ""Column2"": ""A test, which \""fails\"" all the time""
  },
  {
    ""Column1"": ""Name3"",
    ""Column2"": ""A test, which \""fails\"" all the time""
  }
]";
            using (var parser = new ChoCSVReader(FileNameNestedQuotesCSV)
                .MayHaveQuotedFields())
            {
                //foreach (dynamic x in parser)
                //    actual.Add(x);
                var recs = parser.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void CustomNewLine()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Column1", "1'" }, { "Column2","'2'"}, { "Column3" , "'3" } },
                new ChoDynamicObject {{ "Column1", "11'" }, { "Column2","'12'"}, { "Column3" , "'13" } }
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoCSVReader(FileNameCustomNewLineCSV)
                .WithDelimiter("~")
                .WithEOLDelimiter("#####")
                )
            {
                foreach (dynamic x in parser)
                    actual.Add(x);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class People //: IChoCustomColumnMappable
        {
            [ChoCSVRecordField(1, AltFieldNames = "Id, Id_Person")]
            public int PersonId { get; set; }
            [ChoCSVRecordField(2, AltFieldNames = "First_Name", QuoteField = true)]
            public string Name { get; set; }
            [ChoCSVRecordField(3, AltFieldNames = "Document, Phone", QuoteField = true)]
            public string Doc { get; set; }

            //public bool MapColumn(int colPos, string colName, out string newColName)
            //{
            //    newColName = null;
            //    if (colName == "Id" || colName == "Id_Person")
            //    {
            //        newColName = nameof(PersonId);
            //        return true;
            //    }
            //    if (colName == "Name" || colName == "First_Name")
            //    {
            //        newColName = nameof(Name);
            //        return true;
            //    }
            //    if (colName == "Document" || colName == "Phone")
            //    {
            //        newColName = nameof(Doc);
            //        return true;
            //    }
            //    return false;
            //}
        }

        [Test]
        public static void GetHeadersTest()
        {
            string expected = "Id, Name";
            object actual;
            using (var p = new ChoCSVReader(FileNameEmpCSV).WithFirstLineHeader()
                .MayHaveQuotedFields())
            {
                p.Read();
                actual = String.Join(", ", p.Context.Headers);
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void QuotesInQuoteTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {
                    { "Column1", "0"},
                    { "Column2", "0"},
                    { "Column3", "0"},
                    { "Column4", "0"},
                    { "Column5", "0"},
                    { "Column6", "0"},
                    { "Column7", "0"},
                    { "Column8", "2017-01-03T00:00:00"},
                    { "Column9", null},
                    { "Column10", "72d7a7e9-8700-4014-916c-a85e9a6b1ac5"},
                    { "Column11", "1"},
                    { "Column12", "REF212U"},
                    { "Column13", "CREATED"},
                    { "Column14", "Evan job for \"Publish Bug\""},
                    { "Column15", "Changzhou"},
                    { "Column16", "China"},
                    { "Column17", "31.77359"},
                    { "Column18", "119.95401"},
                    { "Column19", "Jiangsu"}}
            };
            List<object> actual = new List<object>();

            using (var p = new ChoCSVReader(FileNameQuoteInQouteCSV)
                .MayHaveQuotedFields())
            {
                foreach (dynamic rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void ReportEmptyLines()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name", "Tom Cassawaw"}},
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Carl'Malcolm" } },
                new ChoDynamicObject {{ "Id", "3" }, { "Name", "Mark"}}
            };
            List<long> expectedEmptyLineNo = new List<long> { 3, 5, 6 };
            List<object> actual = new List<object>();
            List<object> actualEmptyLineNo = new List<object>();

            using (var p = new ChoCSVReader(FileNameEmptyLinesCSV).WithFirstLineHeader()
                .Setup(s => s.EmptyLineFound += (o, e) =>
                {
                    actualEmptyLineNo.Add(e.LineNo);
                })
                .Configure(c => c.IgnoreEmptyLine = true)
                )
            {
                foreach (dynamic rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
            CollectionAssert.AreEqual(expectedEmptyLineNo, actualEmptyLineNo);
        }

        [Test]
        public static void EmptyValueTest()
        {
            List<string> expected = new List<string>()
            { "Id: 1, Name: Carl, Salary: ", "Id: , Name: Mark, Salary: 2000", "Id: 3, Name: Tom, Salary: 3000"};
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader).WithDelimiter(",").WithFirstLineHeader()
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                )
            {
                writer.WriteLine("Id,Name,Salary");
                writer.WriteLine("1,Carl,");
                writer.WriteLine(",Mark,2000");
                writer.WriteLine("3,Tom,3000");

                writer.Flush();
                stream.Position = 0;

                //foreach (dynamic rec in parser)
                //    Console.WriteLine(rec["Id"]);
                var dt = parser.AsDataTable();
                //object rec;
                //while ((rec = parser.Read()) != null)
                //{
                //    Console.WriteLine(rec.ToStringEx());
                //}
                foreach (DataRow dr in dt.Rows)
                    actual.Add(String.Format("Id: {0}, Name: {1}, Salary: {2}", dr[0], dr[1], dr[2]));
                CollectionAssert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void CDataDataSetTest()
        {
            List<EmployeeRecWithCDATA> expected = new List<EmployeeRecWithCDATA>()
            {
                new EmployeeRecWithCDATA { Id=1, Name = new ChoCDATA("Carl")},
                new EmployeeRecWithCDATA { Id=2, Name = new ChoCDATA(""), Salary=2000},
                new EmployeeRecWithCDATA { Id=3, Name = new ChoCDATA(), Salary=3000}
            };
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRecWithCDATA>(reader).WithDelimiter(",").WithFirstLineHeader()
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                )
            {
                writer.WriteLine("Id,Name,Salary");
                writer.WriteLine("1,<![CDATA[Carl]]>,");
                writer.WriteLine("2,<![CDATA[]]>,2000");
                writer.WriteLine("3,,3000");

                writer.Flush();
                stream.Position = 0;

                //foreach (var rec in parser)
                //	Console.WriteLine(rec.Dump());
                var actual = parser.ToList();
                CollectionAssert.AreEqual(expected, actual);
                //                var dt = parser.AsDataTable();

                //object rec;
                //while ((rec = parser.Read()) != null)
                //{
                //    Console.WriteLine(rec.ToStringEx());
                //}
            }
        }

        [Test]
        public static void QuoteValueTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "4" }, { "Name", "NN"} },
                new ChoDynamicObject {{ "Id", "1" }, { "Name", "Raj's"} },
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Tom"} }
            };
            List<object> actual = new List<object>();

            using (var p = new ChoCSVReader(FileNameEmpWithSalaryCSV).WithFirstLineHeader()
                .WithField("Id")
                .WithField("Name", quoteField: true)
                )
            {
                foreach (dynamic rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class CRIContactModel
        {
            public int ID { get; set; }

            [Required]
            [StringLength(25)]
            public string FirstName { get; set; }

            [Required]
            [StringLength(25)]
            public string LastName { get; set; }

            [Required]
            [StringLength(50)]
            public string JobTitle { get; set; }

            [Required]
            [StringLength(50)]
            public string Department { get; set; }

            [Required]
            [StringLength(150)]
            public string Email { get; set; }
        }
        [Test]
        public static void Sample1()
        {
            string csv = @"""ID"",""FirstName"",""LastName"",""JobTitle"",""Department"",""Email""
""1"","""",""Moore"",""Program Manager "",""Housing & Supportive Services"",""MooreN@AFHouston.org""";

            using (var parser = ChoCSVReader<CRIContactModel>.LoadText(csv).WithFirstLineHeader()
                .MayHaveQuotedFields()
                .Configure(c => c.ObjectValidationMode = ChoObjectValidationMode.ObjectLevel))
            {
                var enumerator = parser.GetEnumerator();
                Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => enumerator.MoveNext());

                /*                foreach (var p in new ChoCSVReader<CRIContactModel>(r).WithFirstLineHeader()
                                    .Configure(c => c.ObjectValidationMode = ChoObjectValidationMode.ObjectLevel)
                                    )
                                {
                                    Console.WriteLine(p.Dump());
                                }
                 */
            }
        }

        [Test]
        public static void Pontos()
        {
            List<string> expected = new List<string> {
"005.240.196-06","593.092.106-72","101.297.626-28","430.115.275-04","115.200.976-11","033.674.366-15","122.252.446-50","055.224.256-01","100.739.416-11","100.739.416-11","068.313.346-28","031.872.776-59","819.305.346-04","068.313.346-28","042.209.156-13","097.272.066-97","505.107.976-87","092.793.576-70","405.146.766-04","063.133.976-06","067.079.326-40","067.079.326-40","007.734.136-85","024.075.006-36","496.353.796-68","017.787.046-03","024.075.006-36","975.983.726-91","975.983.726-91","651.615.726-04","027.500.626-32","759.794.976-68","861.099.286-15","043.332.236-52","101.297.626-28","027.500.626-32","072.980.986-24","101.297.626-28","078.293.936-80","086.135.336-63","100.739.416-11","080.548.296-20","824.515.676-00","075.402.236-69","073.896.916-89","22.695.514/0001-41","073.896.916-89","095.098.336-57","094.867.886-00","22.695.514/0001-41","047.644.736-40","532.750.396-87","047.644.736-40","047.644.736-40","080.548.296-20","097.272.066-97","095.298.816-06","067.079.326-40","127.497.666-99","457.785.316-72","091.812.756-41","091.812.756-41","095.298.816-06","593.092.106-72","119.921.137-00","051.060.306-84","076.640.456-06","063.133.976-06","049.972.726-63","017.787.046-03","513.073.166-20","103.259.466-71","614.612.886-49","063.133.976-06","081.908.236-80","509.271.166-34","095.714.356-78","042.209.156-13","824.515.676-00","042.209.156-13","162.398.848-98","049.129.296-11","834.238.346-68","092.793.576-70","466.621.626-04","124.791.116-01","132.706.896-67","896.867.216-49","200.384.536-49","648.755.806-06","086.058.806-86","498.009.906-82","119.921.137-00","095.298.816-06","962.793.036-91","012.230.086-65","532.750.396-87","785.781.246-34","552.811.686-49","043.332.236-52","119.270.496-79","055.142.336-69","110.930.006-94","26.146.556/0001-84","043.332.236-52","573.661.876-15","080.548.296-20","945.618.936-87","060.618.436-82","573.661.876-15","883.178.896-53","964.230.646-87","068.313.346-28","976.111.406-63","119.921.137-00","267.731.018-08","061.507.346-84","043.332.236-52","070.697.326-78","008.441.047-79","532.750.396-87","405.146.766-04","430.115.275-04","632.679.426-91","094.867.886-00","964.244.276-00","095.714.356-78","043.332.236-52","100.739.416-11","028.425.816-44","055.475.626-96","051.060.306-84","593.087.706-82","097.831.576-63","291.204.386-72","100.739.416-11","457.785.316-72","100.739.416-11","062.376.626-42","614.612.886-49","291.204.386-72","109.632.586-13","017.787.046-03","063.317.546-36","466.580.256-49","087.910.346-98","111.299.096-89","074.077.616-98","045.900.356-93","127.587.786-96","258.860.706-30","850.578.197-04","049.129.296-11","012.230.086-65","016.326.916-55","064.117.266-47","069.616.026-90","021.072.716-07","030.314.876-48","121.438.926-08","834.138.206-72","064.117.266-47","834.138.206-72","109.632.586-13","563.334.806-06","349.103.366-72","121.438.926-08","100.076.916-02","648.755.806-06","095.555.836-09","850.578.197-04","363.258.805-87","076.640.456-06","703.421.646-00","190.447.716-04","054.196.806-88","011.810.106-40","016.322.826-40","668.565.236-53","593.092.106-72","072.980.986-24","498.009.906-82","103.704.876-80","072.980.986-24","22.695.514/0001-41","505.107.976-87","095.555.836-09","291.204.386-72","007.734.136-85","291.204.386-72","975.983.726-91","824.515.676-00","030.314.876-48","074.077.616-98","100.739.416-11","094.977.956-37","059.974.496-02","110.930.006-94","134.385.376-13","080.548.296-20","042.485.026-58","063.742.286-42","22.695.514/0001-41","059.974.496-02","405.146.766-04","109.024.626-95","094.977.956-37","472.095.186-49","146.946.386-53","063.133.976-06","080.548.296-20","146.946.386-53","070.697.326-78","670.933.296-91","013.326.876-44","405.666.926-00","013.494.961-70","041.768.856-37","070.697.326-78","041.768.856-37","459.281.506-87","063.317.546-36","080.548.296-20","614.612.886-49","013.381.256-13","081.595.596-07","834.238.346-68","050.830.836-47","850.578.197-04","834.216.886-72","063.317.546-36","013.381.256-13","466.621.626-04","059.448.385-90","850.578.197-04","522.048.636-53","030.314.876-48","107.753.456-60","095.555.836-09","563.334.806-06","133.202.586-21","133.202.586-21","050.147.726-84","080.962.036-73","095.714.356-78","573.031.776-04","101.740.146-27","661.797.856-00","614.612.886-49","496.353.796-68","729.692.456-04","729.692.456-04","115.245.746-27","661.797.856-00","087.910.346-98","086.058.806-86","614.612.886-49","096.881.426-38","108.715.526-63","036.883.446-85","349.096.726-72","22.695.514/0001-41","945.639.006-30","081.139.626-62","593.097.096-34","824.515.676-00","029.235.756-79","349.096.726-72","330.710.871-95","027.500.626-32","904.137.846-49","472.095.186-49","066.627.156-97","291.204.386-72","470.249.766-91","824.515.676-00","291.204.386-72","022.583.656-43","522.048.636-53","509.271.166-34","498.009.906-82","045.900.356-93","068.313.346-28","087.231.286-07","976.111.406-63","405.598.586-04","110.930.006-94","040.495.106-69","048.827.396-07","124.791.116-01","013.326.876-44","430.113.815-34","039.058.896-25","472.751.546-68","357.701.938-75","051.060.306-84","22.695.514/0001-41","087.231.286-07","100.076.916-02","038.181.686-95","041.768.856-37","405.666.926-00","133.202.586-21","113.908.936-69","638.202.266-72","075.532.556-70","113.908.936-69","119.921.137-00","057.619.326-79","100.739.416-11","050.147.726-84","113.908.936-69","097.687.206-46","430.115.275-04","412.827.348-14","110.702.886-83","054.819.486-64","094.710.426-74","094.867.886-00","066.984.046-75","113.908.936-69","050.147.726-84","054.819.486-64","094.710.426-74","081.595.596-07","087.380.476-75","498.009.906-82","013.326.876-44","883.178.896-53","457.785.316-72","850.578.197-04","074.077.616-98","466.621.626-04","059.974.496-02","076.640.456-06","097.580.866-42","044.719.266-32","111.299.096-89","091.812.756-41","091.812.756-41","097.272.066-97","029.411.486-63","765.059.108-59","112.301.656-90","267.731.018-08","092.793.576-70","100.076.916-02","055.142.336-69","084.678.826-89","008.441.047-79","042.485.026-58","107.753.456-60","349.103.366-72","019.634.356-99","405.146.766-04","097.272.066-97","077.908.946-41","466.621.626-04","405.146.766-04","703.421.646-00","466.621.626-04","050.830.836-47","850.578.197-04","097.272.066-97","054.455.136-29","077.908.946-41","594.952.436-53","533.208.676-87","091.812.756-41","522.048.636-53","522.048.636-53","074.077.616-98","091.812.756-41","007.734.136-85","115.245.746-27","106.484.906-70","824.515.676-00","118.876.976-67","785.781.246-34","113.019.596-18","073.896.916-89","100.739.416-11","073.896.916-89","116.495.086-03","112.070.466-98","472.751.546-68","059.448.385-90","291.204.386-72","045.570.986-65","054.819.486-64","120.462.146-25","096.262.486-10","096.262.486-10","405.666.926-00","054.196.806-88","054.819.486-64","670.933.296-91","113.383.746-80","087.380.476-75","834.138.206-72","304.730.806-30","015.507.896-86","080.548.296-20","011.589.416-09","045.570.986-65","030.314.876-48","030.314.876-48","061.653.526-00","513.073.166-20","121.803.026-73","522.048.636-53","045.570.986-65","349.063.986-34","077.996.516-79","124.791.116-01","496.353.796-68","093.997.056-25","115.245.746-27","964.230.646-87","093.997.056-25","081.595.596-07","013.381.256-13","101.740.146-27","033.674.366-15","533.208.676-87","029.235.756-79","121.236.146-61","850.578.197-04","009.754.256-33","130.587.616-43","614.612.886-49","107.753.456-60","041.768.856-37","013.326.876-44","059.774.626-54","009.750.716-45","070.697.326-78","824.515.676-00","594.144.136-34","824.515.676-00","508.400.536-49","086.058.806-86","070.697.326-78","087.910.346-98","112.603.086-42","081.908.236-80","106.484.906-70","112.070.466-98","505.107.976-87","077.996.516-79","349.103.366-72","031.872.776-59","060.248.286-08","073.896.916-89","655.219.606-78","655.212.936-04","976.098.466-00","112.070.466-98","533.208.676-87","765.059.108-59","349.096.726-72","533.208.676-87","075.532.556-70","291.204.386-72","096.262.486-10","593.087.706-82","122.252.446-50","048.229.756-57","096.262.486-10","348.877.208-07","348.877.208-07","169.528.986-20","136.845.086-59","055.142.336-69","223.952.228-36","127.834.726-78","127.834.726-78","077.996.516-79","077.996.516-79","099.057.916-63","109.632.586-13","029.256.376-04","029.256.376-04","107.753.456-60","550.383.796-72","729.692.456-04","095.555.836-09","036.883.446-85","070.697.326-78","430.115.275-04","513.073.166-20","095.714.356-78","759.794.976-68","991.564.586-49","513.364.216-49","022.583.656-43","029.471.246-18","098.884.246-70","113.383.746-80","113.383.746-80","145.153.596-14","22.695.514/0001-41","550.383.796-72","119.921.137-00","066.627.156-97","498.009.906-82","119.270.496-79","991.564.586-49","081.595.596-07","991.564.586-49","084.797.136-82","522.048.636-53","272.880.566-00","522.048.636-53","422.943.986-53","22.695.514/0001-41","532.750.396-87","344.758.478-50","084.797.136-82","111.299.096-89","363.258.805-87","017.787.046-03","017.787.046-03","472.751.546-68","058.985.696-00","029.411.486-63","759.099.608-49","533.208.676-87","819.305.346-04","111.266.696-60","573.661.876-15","101.740.146-27","115.200.976-11","894.585.836-91","073.041.146-06","703.421.646-00","119.921.137-00","291.204.386-72","834.238.346-68","073.041.146-06","126.928.926-82","291.204.386-72","834.238.346-68","072.350.636-10","072.350.636-10","059.448.385-90","633.777.213-04","070.697.326-78","060.248.286-08","009.750.716-45","007.385.666-59","466.621.626-04","030.314.876-48","009.750.716-45","614.612.886-49","614.612.886-49","106.087.956-58","081.595.596-07","096.262.486-10","125.705.586-05","036.883.446-85","073.041.146-06","073.041.146-06","073.041.146-06","721.322.306-25","513.361.036-04","945.639.346-15","962.793.036-91","291.204.386-72","934.827.367-15","291.204.386-72","513.361.036-04","041.768.856-37","258.860.706-30","126.928.926-82","126.928.926-82","126.928.926-82","363.258.805-87","349.096.726-72","080.548.296-20","124.791.116-01","029.471.246-18","721.322.306-25","824.515.676-00","119.921.137-00","119.921.137-00","430.115.275-04","121.384.776-14","049.972.726-63","824.515.676-00","070.697.326-78","331.875.018-26","045.570.986-65","498.009.906-82","111.299.096-89","100.739.416-11","127.497.666-99","824.515.676-00","834.238.346-68","058.985.696-00","063.133.976-06","066.397.316-30","039.058.896-25","016.454.376-71","016.454.376-71","466.621.626-04","496.353.796-68","073.896.916-89","063.427.536-44","000.846.597-50","095.298.816-06","127.587.786-96","200.384.536-49","109.349.906-03","304.730.806-30","030.314.876-48","114.074.606-50","136.845.086-59","054.819.486-64","834.138.206-72","896.867.216-49","035.990.616-88","095.714.356-78","964.244.276-00","095.714.356-78","097.272.066-97","079.873.976-28","430.115.275-04","430.115.275-04","614.612.886-49","101.297.626-28","052.568.236-80","136.590.076-25","072.469.886-88","101.297.626-28","836.299.126-72","027.500.626-32","593.087.706-82","136.590.076-25","095.714.356-78","092.793.576-70","060.248.286-08","128.230.166-78","405.666.926-00","472.095.186-49","593.087.706-82","116.495.086-03","834.179.826-34","729.692.456-04","081.138.886-75","824.515.676-00","073.896.916-89","132.706.896-67","132.706.896-67","405.146.766-04","060.352.196-75","430.115.275-04","132.706.896-67","059.774.626-54","422.943.986-53","067.551.386-30","128.230.166-78","405.666.926-00","109.349.906-03","109.349.906-03","064.117.266-47","079.835.836-05","121.236.816-95","067.551.386-30","081.138.886-75","090.179.586-00","121.236.816-95","140.894.666-10","127.587.786-96","067.551.386-30","067.551.386-30","035.990.616-88","819.305.346-04","138.334.606-28","038.704.586-40","834.238.346-68","118.669.186-72","118.669.186-72","533.208.676-87","061.507.346-84","072.350.636-10","069.751.856-62","118.669.186-72","049.515.846-11","070.857.256-13","109.349.906-03","124.820.196-56","063.133.976-06","011.589.416-09","112.070.466-98","563.334.806-06","996.273.106-20","765.059.108-59","134.385.376-13","007.734.136-85","472.095.186-49","785.781.246-34","113.383.746-80","563.334.806-06","079.835.836-05","031.176.946-25","834.238.346-68","563.334.806-06","030.314.876-48","106.484.906-70","661.797.856-00","116.495.086-03","563.334.806-06","158.200.677-62","834.238.346-68","091.005.947-09","107.753.456-60","496.353.796-68","101.740.146-27","614.612.886-49","115.069.526-90","011.589.416-09","134.385.376-13","291.204.386-72","070.857.256-13","127.587.786-96","036.883.446-85","078.293.936-80","363.258.805-87","513.049.456-34","513.049.456-34","086.135.336-63","134.566.346-33","991.564.586-49","110.004.556-29","097.241.446-07","091.005.947-09","991.564.586-49","134.566.346-33","045.559.076-12","134.566.346-33","073.206.496-16","975.983.726-91","060.352.196-75","050.147.726-84","127.846.186-84","140.894.666-10","146.097.046-25","063.133.976-06","043.433.146-50","035.990.616-88","975.983.726-91","22.695.514/0001-41","091.005.947-09","022.583.246-10","975.983.726-91","091.005.947-09","134.385.376-13","22.695.514/0001-41","059.847.586-95","007.734.136-85","054.196.806-88","081.809.686-18","008.441.047-79","081.809.686-18","073.206.496-16","964.230.646-87","000.846.597-50","054.819.486-64","081.809.686-18","081.809.686-18","100.739.416-11","944.710.886-53","040.155.355-88","121.803.026-73","134.323.296-10","081.139.626-62","036.487.386-82","063.317.546-36","038.704.586-40","071.517.176-36","834.238.346-68","095.714.356-78","267.731.018-08","493.354.056-04","573.661.876-15","106.484.906-70","066.984.046-75","304.730.806-30","072.350.636-10","055.211.276-31","111.266.696-60","094.977.956-37","063.427.536-44","703.421.646-00","122.412.826-50","945.618.936-87","095.714.356-78","092.793.576-70","306.760.616-72","103.704.876-80","073.896.916-89","127.846.186-84","069.971.856-22","304.730.806-30","115.245.746-27","096.262.486-10","069.751.856-62","834.179.826-34","493.354.056-04","085.692.086-00","115.245.746-27","093.973.196-71","615.401.536-49","030.314.876-48","043.883.526-32","111.237.896-06","614.612.886-49","030.314.876-48","091.005.947-09","112.301.656-90","614.612.886-49","615.401.536-49","062.376.626-42","099.549.366-96","078.230.166-56","038.181.686-95","112.070.466-98","030.314.876-48","834.216.886-72","099.549.366-96","111.237.896-06","038.181.686-95","115.659.166-03","048.346.196-21","127.834.726-78","049.515.846-11","016.463.506-85","097.831.576-63","036.487.386-82","040.155.355-88","119.921.137-00","834.216.886-72","080.962.036-73","721.339.616-15","169.528.986-20","013.381.256-13","048.229.756-57","601.213.436-34","043.433.146-50","032.057.636-10","059.546.046-14","027.500.626-32","130.427.376-83","038.181.686-95","128.230.166-78","080.548.296-20","095.098.336-57","513.364.216-49","128.022.706-05","088.322.716-99","064.117.266-47","127.497.666-99","308.584.706-59","267.731.018-08","134.566.346-33","045.559.076-12","045.559.076-12","008.441.047-79","134.566.346-33","026.606.846-42","084.797.136-82","084.797.136-82","621.656.826-49","129.205.596-05","655.219.606-78","088.322.716-99","991.564.586-49","118.876.976-67","063.930.826-07","091.005.947-09","084.797.136-82","550.383.796-72","021.072.716-07","035.990.616-88","405.146.766-04","721.322.306-25","550.383.796-72","000.846.597-50","063.427.536-44","405.154.946-15","032.327.486-27","099.057.916-63","405.146.766-04","024.055.916-93","550.383.796-72","573.661.876-15","024.055.916-93","765.059.108-59","095.555.836-09","129.205.596-05","086.135.336-63","550.383.796-72","405.146.766-04","765.059.108-59","078.230.166-56","127.497.666-99","127.587.786-96","522.048.636-53","104.104.456-90","109.632.586-13","291.204.386-72","042.941.026-32","127.497.666-99","073.007.376-92","078.230.166-56","22.695.514/0001-41","042.941.026-32","493.354.056-04","144.429.896-80","721.322.306-25","721.322.306-25","669.895.156-00","024.055.916-93","113.019.596-18","144.429.896-80","098.884.246-70","991.564.586-49","111.299.096-89","073.896.916-89","019.634.356-99","072.350.636-10","975.983.726-91","119.921.137-00","016.326.916-55","000.846.597-50","060.248.286-08","550.383.796-72","854.371.666-72","092.793.576-70","113.383.746-80","027.449.596-18","834.216.886-72","593.087.706-82","044.719.266-32","904.137.846-49","111.237.896-06","048.346.196-21","059.448.385-90","405.154.946-15","009.750.716-45","130.587.616-43","118.897.236-73","084.797.136-82","126.987.266-44","136.845.086-59","091.005.947-09","048.346.196-21","136.845.086-59","126.987.266-44","077.996.516-79","068.313.346-28","087.910.346-98","063.133.976-06","533.208.676-87","533.208.676-87","109.024.626-95","513.049.456-34","031.872.776-59","036.863.846-47","349.096.726-72","109.024.626-95","121.236.816-95","258.860.706-30","040.394.375-27","109.194.886-09","032.534.626-76","976.128.636-34","976.128.636-34","976.128.636-34","042.485.026-58","422.943.986-53","405.146.766-04","136.845.086-59","040.394.375-27","405.146.766-04","120.462.026-19","120.462.146-25","894.442.806-91","024.055.916-93","466.603.056-53","109.194.886-09","648.750.166-20","053.229.306-12","109.194.886-09","976.128.636-34","043.883.526-32","063.930.826-07","039.058.896-25","086.135.336-63","088.611.346-61","060.618.436-82","088.611.346-61","975.983.726-91","648.750.166-20","762.771.356-00","015.161.946-83","010.602.006-43","015.161.946-83","039.058.896-25","005.240.196-06","121.438.826-45","785.781.246-34","111.299.096-89","133.202.586-21","066.627.156-97","104.104.456-90","104.104.456-90","015.161.946-83","055.475.626-96","040.155.355-88","081.595.596-07","127.846.186-84","267.731.018-08","121.438.826-45","040.155.355-88","079.835.836-05","017.787.046-03","834.138.206-72","079.835.836-05","070.697.326-78","027.500.626-32","060.618.436-82","087.910.346-98","308.025.476-72","092.341.836-90","120.462.146-25","040.155.355-88","055.475.626-96","054.196.806-88","078.293.936-80","022.463.556-56","422.943.986-53","086.135.336-63","012.230.086-65","126.928.926-82","126.928.926-82","042.941.026-32","107.753.456-60","107.753.456-60","850.578.197-04","095.714.356-78","095.714.356-78","638.202.266-72","073.206.496-16","077.908.946-41","049.340.776-69","077.908.946-41","337.267.106-63","964.230.646-87","077.908.946-41","107.753.456-60","030.314.876-48","132.706.896-67","009.750.716-45","085.978.406-17","128.022.706-05","130.552.136-61","140.894.666-10","030.314.876-48","109.194.886-09","093.973.196-71","785.781.246-34","466.621.626-04","056.117.806-28","140.894.666-10","120.462.026-19","904.137.846-49","472.095.186-49","077.996.516-79","964.230.726-04","048.346.196-21","834.238.346-68","904.137.846-49","101.740.146-27","088.611.346-61","135.432.246-03","086.058.806-86","091.005.947-09","080.548.296-20","267.731.018-08","000.846.597-50","081.595.596-07","130.552.136-61","070.697.326-78","099.506.066-59","001.582.386-56","001.582.386-56","991.564.586-49","054.196.806-88","001.582.386-56","112.301.656-90","267.731.018-08","013.020.286-00","291.204.386-72","067.551.386-30","291.204.386-72","027.449.596-18","040.394.375-27","162.398.848-98","304.730.806-30","088.611.346-61","130.552.136-61","122.412.826-50","060.352.196-75","033.674.366-15","045.559.076-12","648.750.166-20","218.161.906-91","064.117.266-47","040.394.375-27","218.161.906-91","305.651.036-87","115.659.166-03","405.154.946-15","218.161.906-91","496.353.796-68","850.578.197-04","060.352.196-75","064.117.266-47","834.179.826-34","021.072.716-07","107.028.166-20","027.449.596-18","039.182.026-58","118.669.186-72","472.095.186-49","258.860.706-30","044.719.266-32","072.350.636-10","720.117.426-68","493.354.056-04","048.346.196-21","337.171.766-68","120.462.026-19","029.471.246-18","493.354.056-04","550.383.796-72","070.697.326-78","785.781.246-34","032.327.486-27","080.548.296-20","836.299.126-72","169.528.986-20","430.115.275-04","045.570.986-65","005.240.196-06","550.383.796-72","048.229.756-57","405.154.946-15","024.055.916-93","122.252.446-50","127.834.726-78","111.299.096-89","964.230.726-04","092.793.576-70","834.138.206-72","029.471.246-18","099.549.366-96","059.774.626-54","133.202.586-21","120.462.026-19","013.020.286-00","027.449.596-18","094.867.886-00","048.346.196-21","305.651.036-87","308.025.476-72","861.099.286-15","045.559.076-12","085.978.406-17","305.651.036-87","036.487.386-82","036.487.386-82","036.487.386-82","861.099.286-15","072.980.986-24","593.087.706-82","861.099.286-15","200.384.536-49","136.590.076-25","169.528.986-20","110.543.466-40","029.411.486-63","200.384.536-49","013.020.286-00","200.384.536-49","084.696.116-40","037.209.596-86","047.343.046-02","119.921.137-00","063.133.976-06","976.128.636-34","405.154.946-15","048.346.196-21","593.087.706-82","720.117.426-68","081.809.686-18","048.229.756-57","036.883.446-85","081.809.686-18","091.655.907-67","037.209.596-86","513.049.456-34","109.349.906-03","944.710.886-53","405.146.766-04","007.734.136-85","405.146.766-04","065.708.666-56","073.896.916-89","010.602.006-43","762.771.356-00","134.323.296-10","128.230.166-78","111.299.096-89","084.797.136-82","063.133.976-06","573.661.876-15","073.206.496-16","785.781.246-34","120.462.146-25","459.291.146-68","067.341.166-46","472.751.546-68","103.259.466-71","029.411.486-63","072.980.986-24","053.229.306-12","070.697.326-78","079.873.976-28","088.611.346-61","344.758.478-50","007.734.136-85","079.478.836-02","850.578.197-04","060.618.436-82","030.314.876-48","059.005.856-89","070.857.256-13","962.793.036-91","067.551.386-30","042.941.026-32","016.326.916-55","542.945.336-68","883.178.896-53","017.787.046-03","148.603.406-39","106.484.906-70","405.675.836-00","064.117.266-47","044.719.266-32","405.675.836-00","000.846.597-50","055.556.266-22","064.498.436-83","000.846.597-50","048.346.196-21","067.341.166-46","095.098.336-57","962.793.036-91","073.896.916-89","036.883.446-85","130.552.136-61","129.205.596-05","054.196.806-88","720.117.426-68","016.454.376-71","093.997.056-25","22.695.514/0001-41","945.639.346-15","720.117.426-68","058.285.956-52","136.590.076-25","045.559.076-12","022.583.656-43","081.441.466-46","720.117.426-68","033.674.366-15","041.768.856-37","110.930.006-94","305.651.036-87","680.237.746-20","045.570.986-65","054.199.696-70","594.144.136-34","084.696.116-40","861.099.286-15","834.179.826-34","047.343.046-02","130.599.116-81","070.697.326-78","060.248.286-08","041.774.496-01","028.447.416-90","099.506.066-59","024.055.916-93","096.262.486-10","070.697.326-78","099.506.066-59","038.704.586-40","934.827.367-15","638.202.266-72","127.587.786-96","472.095.186-49","976.128.636-34","013.020.286-00","145.153.596-14","024.055.916-93","065.708.666-56","027.449.596-18","050.147.726-84","542.945.336-68","055.560.476-42","200.384.536-49","007.734.136-85","072.980.986-24","109.353.736-16","008.441.047-79","008.441.047-79","088.611.346-61","031.938.666-01","096.262.486-10","063.317.546-36","291.204.386-72","091.005.947-09","027.449.596-18","013.381.256-13","070.857.256-13","030.314.876-48","601.213.436-34","785.781.246-34","042.941.026-32","834.216.886-72","134.323.296-10","081.138.886-75","337.267.106-63","594.747.856-00","130.587.616-43","051.284.406-21","109.632.586-13","048.346.196-21","594.747.856-00","573.031.776-04","834.238.346-68","337.267.106-63","084.797.136-82","819.305.346-04","084.797.136-82","016.970.956-66","337.267.106-63","016.970.956-66","063.133.976-06","120.462.026-19","252.878.936-04","027.449.596-18","064.174.486-22","459.281.506-87","118.876.976-67","962.793.036-91","552.395.286-91","22.695.514/0001-41","405.146.766-04","027.455.496-85","861.099.286-15","964.230.726-04","22.695.514/0001-41","513.049.456-34","073.597.016-51","573.661.876-15","088.611.346-61","088.611.346-61","573.661.876-15","088.611.346-61","059.774.626-54","024.055.916-93","308.025.476-72","109.194.886-09","058.985.696-00","976.098.466-00","128.230.166-78","421.126.646-20","075.167.306-45","075.167.306-45","976.098.466-00","072.980.986-24","073.206.496-16","594.747.856-00","594.747.856-00","505.107.976-87","136.590.076-25","085.466.376-28","110.376.056-40","060.618.436-82","079.873.976-28","661.797.856-00","252.878.936-04","120.462.026-19","051.276.416-66","015.431.886-80","430.113.815-34","051.276.416-66","051.060.306-84","602.561.336-20","079.478.836-02","252.878.936-04","058.285.956-52","472.751.546-68","080.548.296-20","076.814.546-59","113.019.596-18","073.007.376-92","051.060.306-84","060.618.436-82","060.618.436-82","079.835.836-05","127.834.726-78","048.346.196-21","050.216.926-58","638.202.266-72","042.941.026-32","457.785.316-72","070.697.326-78","470.257.786-72","670.933.296-91","218.104.786-34","116.263.156-20","031.872.776-59","096.262.486-10","116.263.156-20","470.257.786-72","883.178.896-53","104.104.456-90","022.583.656-43","819.305.346-04","112.070.466-98","024.055.916-93","024.055.916-93","121.236.816-95","457.785.316-72","563.334.806-06","055.224.256-01","904.137.846-49","594.747.856-00","056.838.126-23","059.847.586-95","614.612.886-49","457.785.316-72","017.739.076-03","039.182.026-58","101.740.146-27","093.997.056-25","058.305.256-82","594.747.856-00","594.747.856-00","029.471.246-18","405.154.946-15","005.240.196-06","044.719.266-32","013.020.286-00","563.334.806-06","109.194.886-09","013.020.286-00","550.383.796-72","850.578.197-04","033.674.366-15","095.555.836-09","007.734.136-85","073.206.496-16","496.353.796-68","013.494.961-70","045.570.986-65","024.055.916-93","101.740.146-27","042.941.026-32","405.154.946-15","405.154.946-15","115.471.256-75","048.346.196-21","040.394.375-27","106.484.906-70","110.930.006-94","099.057.916-63","022.583.656-43","112.603.086-42","055.211.276-31","091.005.947-09","013.020.286-00","063.133.976-06","252.878.936-04","834.114.886-20","058.285.956-52","051.284.406-21","22.695.514/0001-41","127.587.786-96","944.710.886-53","648.750.166-20","218.161.906-91","308.025.476-72","136.590.076-25","090.179.586-00","072.469.886-88","036.883.446-85","024.055.916-93","135.860.116-01","883.178.896-53","872.829.256-15","045.900.356-93","088.611.346-61","000.846.597-50","056.838.126-23","019.634.356-99","625.176.946-72","096.262.486-10","991.564.586-49","036.883.446-85","472.095.186-49","000.846.597-50","084.696.116-40","015.161.946-83","513.364.216-49","120.462.026-19","615.401.536-49","028.468.605-05","861.099.286-15","964.230.726-04","861.099.286-15","594.747.856-00","513.364.216-49","552.811.686-49","088.611.346-61","088.611.346-61","051.276.416-66","103.704.876-80","000.846.597-50","000.846.597-50","127.587.786-96","430.115.275-04","092.341.136-40","022.463.556-56","022.583.656-43","680.471.936-00","861.099.286-15","121.437.566-90","759.794.976-68","472.095.186-49","026.606.846-42","872.829.256-15","861.099.286-15","22.695.514/0001-41","005.240.196-06","026.606.846-42","861.099.286-15","032.327.486-27","945.639.346-15","904.322.826-53","109.194.886-09","024.055.916-93","405.146.766-04","144.280.846-25","140.894.666-10","027.455.496-85","054.819.486-64","042.941.026-32","357.701.938-75","028.468.605-05","038.704.586-40","421.126.646-20","542.945.336-68","405.675.836-00","084.058.206-47","072.350.636-10","430.115.275-04","138.334.606-28","112.070.466-98","058.305.256-82","405.675.836-00","138.334.606-28","070.857.256-13","007.734.136-85","054.405.396-62","127.205.926-09","036.997.556-14","100.739.416-11","116.946.166-22","036.997.556-14","116.495.086-03","109.632.586-13","016.326.916-55","017.739.076-03","593.097.096-34","405.152.656-91","064.117.266-47","834.238.346-68","661.797.856-00","055.556.266-22","496.353.796-68","023.728.216-01","119.921.137-00","022.583.656-43","145.153.596-14","145.153.596-14","964.230.646-87","078.230.166-56","252.878.936-04","337.171.766-68","469.482.106-78","007.734.136-85","508.400.536-49","731.594.336-68","128.022.706-05","190.447.716-04","072.980.986-24","115.245.746-27","648.750.166-20","092.793.576-70","469.482.106-78","070.697.326-78","110.004.556-29","041.227.286-57","027.449.596-18","085.978.406-17","092.341.136-40","099.057.916-63","092.341.136-40","084.696.116-40","124.820.196-56","036.997.556-14","904.322.826-53","110.004.556-29","22.695.514/0001-41","109.353.736-16","091.005.947-09","252.878.936-04","101.740.146-27","079.873.976-28","015.431.886-80","054.196.806-88","073.896.916-89","073.896.916-89","22.695.514/0001-41","156.244.536-71","904.322.826-53","092.341.136-40","115.245.746-27","080.548.296-20","072.469.886-88","135.432.246-03","127.846.186-84","075.402.236-69","078.230.166-56","308.584.706-59","305.651.036-87","028.468.605-05","308.025.476-72","000.846.597-50","029.471.246-18","081.908.236-80","22.695.514/0001-41","075.402.236-69","057.148.746-76","026.606.846-42","148.603.406-39","026.606.846-42","026.606.846-42","045.570.986-65","135.025.596-35","111.299.096-89","077.908.946-41","015.161.946-83","337.171.766-68","964.230.646-87","112.070.466-98","834.179.826-34","019.634.356-99","128.230.166-78","111.266.696-60","040.394.375-27","015.161.946-83","015.161.946-83","073.007.376-92","496.353.796-68","128.022.706-05","087.380.476-75","944.710.886-53","128.022.706-05","060.248.286-08","058.305.256-82","090.234.876-05","000.846.597-50","027.449.596-18","042.941.026-32","615.401.536-49","041.141.316-39","041.141.316-39","097.979.376-90","085.692.086-00","785.781.246-34","090.234.876-05","118.669.186-72","493.354.056-04","078.293.936-80","308.025.476-72","850.578.197-04","017.787.046-03","056.838.126-23","045.570.986-65","060.248.286-08","013.020.286-00","047.382.836-77","059.847.586-95","054.405.396-62","720.117.426-68","094.977.956-37","031.872.776-59","055.224.256-01","091.005.947-09","109.393.376-30","169.528.986-20","048.346.196-21","064.117.266-47","069.615.046-80","106.484.906-70","043.433.146-50","087.910.346-98","069.615.046-80","092.102.626-97","132.706.896-67","097.272.066-97","027.449.596-18","115.024.746-03","048.229.756-57","055.224.256-01","013.020.286-00","121.803.026-73","061.653.526-00","669.895.156-00","337.171.766-68","070.857.256-13","513.049.456-34","430.115.275-04","027.449.596-18","110.815.646-01","112.070.466-98","101.297.626-28","834.216.886-72","085.692.086-00","148.603.406-39","124.791.116-01","076.814.546-59","042.485.026-58","680.471.936-00","466.603.056-53","405.154.946-15","099.057.916-63","072.980.986-24","015.431.886-80","085.692.086-00","472.095.186-49","106.087.956-58","099.057.916-63","291.204.386-72","648.750.166-20","834.216.886-72","305.651.036-87","162.398.848-98","041.774.496-01","060.248.286-08","305.651.036-87","060.248.286-08","033.674.366-15","056.564.536-69","013.020.286-00","112.099.006-88","028.390.186-16","044.719.266-32","091.005.947-09","043.433.146-50","037.209.596-86","976.098.466-00","291.204.386-72","028.468.605-05","111.299.096-89","059.847.586-95","063.930.826-07","072.469.886-88","013.320.946-60","594.747.856-00","106.258.776-67","058.305.256-82","054.819.486-64","976.098.466-00","144.280.846-25","084.797.136-82","005.240.196-06","119.270.496-79","000.846.597-50","308.025.476-72","115.245.746-27","054.196.806-88","550.383.796-72","141.990.066-80","126.928.926-82","834.138.206-72","022.583.656-43","126.928.926-82","834.138.206-72","058.285.956-52","058.285.956-52","524.610.926-72","031.872.776-59","324.946.388-44","976.128.636-34","087.910.346-98","064.117.266-47","127.497.666-99","076.814.546-59","104.104.456-90","087.910.346-98","060.618.436-82","564.216.306-00","055.224.256-01","252.878.936-04","116.946.166-22","048.229.756-57","063.930.826-07","059.774.626-54","130.587.616-43","550.383.796-72","107.753.456-60","024.055.916-93","116.263.156-20","009.750.716-45","059.774.626-54","405.146.766-04","347.143.278-79","032.909.086-07","550.383.796-72","064.174.486-22","556.512.916-87","077.996.516-79","092.102.626-97","472.751.546-68","059.774.626-54","148.603.406-39","405.602.876-15","834.238.346-68","148.603.406-39","127.846.186-84","091.005.947-09","097.831.576-63","048.346.196-21","103.555.526-39","116.495.086-03","072.350.636-10","096.881.426-38","092.793.576-70","040.155.355-88","000.846.597-50","090.806.326-19","063.133.976-06","991.564.586-49","121.803.026-73","291.204.386-72","145.153.596-14","049.972.726-63","073.206.496-16","073.206.496-16","162.398.848-98","115.245.746-27","802.041.636-68","043.433.146-50","077.996.516-79","834.238.346-68","846.522.846-91","036.863.846-47","088.611.346-61","015.431.886-80","493.039.966-15","925.601.136-00","027.455.496-85","041.768.856-37","110.930.006-94","110.930.006-94","094.977.956-37","058.285.956-52","058.285.956-52","136.845.086-59","337.267.106-63","614.612.886-49","337.171.766-68","031.872.776-59","005.240.196-06","059.448.385-90","337.171.766-68","305.651.036-87","850.578.197-04","086.058.806-86","135.432.246-03","039.058.896-25","112.070.466-98","136.845.086-59","861.099.286-15","066.984.046-75","096.881.426-38","041.774.496-01","802.041.636-68","106.484.906-70","148.603.406-39","079.478.836-02","028.468.605-05","200.384.536-49"
,"073.206.496-16","136.845.086-59","041.141.316-39","079.835.836-05","550.383.796-72","092.793.576-70","115.200.976-11","024.055.916-93","106.484.906-70","085.902.486-58","106.484.906-70","028.468.605-05","030.314.876-48","113.019.596-18","092.793.576-70","027.449.596-18","128.230.166-78","405.602.876-15","086.058.806-86","088.611.346-61","130.599.116-81","145.153.596-14","469.482.106-78","22.695.514/0001-41","000.846.597-50","091.005.947-09","148.603.406-39","493.039.966-15","024.055.916-93","000.846.597-50","573.661.876-15","964.230.726-04","042.209.156-13","096.881.426-38","308.025.476-72","073.206.496-16","027.500.626-32","563.334.806-06","593.097.096-34","015.161.946-83","053.230.466-78","002.547.176-75","070.697.326-78","563.334.806-06","124.489.486-90","084.797.136-82","012.665.186-81","127.497.666-99","040.588.906-22","099.057.916-63","007.747.616-60","405.598.586-04","169.528.986-20","053.230.466-78","054.819.486-64","421.126.646-20","116.495.086-03","073.896.916-89","145.153.596-14","145.153.596-14","012.665.186-81","012.665.186-81","945.639.346-15","145.153.596-14","134.858.846-26","030.314.876-48","030.314.876-48","044.360.766-44","036.883.446-85","069.751.856-62","975.983.726-91","623.257.526-15","023.728.216-01","057.148.746-76","113.961.556-43","097.831.576-63","668.565.236-53","668.565.236-53","037.209.596-86","022.583.656-43","009.750.716-45","124.791.116-01","099.057.916-63","053.230.466-78","013.020.286-00","045.570.986-65","072.980.986-24","095.098.336-57","091.005.947-09","022.463.556-56","127.846.186-84","075.587.426-92","759.099.608-49","894.442.806-91","470.257.786-72","056.838.126-23","041.768.856-37","759.099.608-49","103.885.956-55","110.815.646-01","759.099.608-49","508.400.536-49","042.941.026-32","090.234.876-05","096.881.426-38","116.263.156-20","140.234.776-64","513.073.166-20","136.845.086-59","850.578.197-04","037.209.596-86","073.896.916-89","305.651.036-87","308.025.476-72","077.908.946-41","042.209.156-13","127.497.666-99","084.797.136-82","009.750.716-45","145.153.596-14","109.393.376-30","061.507.346-84","040.394.375-27","044.719.266-32","096.262.486-10","109.393.376-30","017.739.076-03","136.845.086-59","074.222.586-08","614.612.886-49","043.332.236-52","614.612.886-49","048.346.196-21","109.632.586-13","075.587.426-92","158.200.677-62","140.234.776-64","106.258.776-67","337.171.766-68","099.506.066-59","036.883.446-85","127.846.186-84","648.750.166-20","110.815.646-01","112.070.466-98","136.845.086-59","964.230.646-87","017.787.046-03","493.039.966-15","009.750.716-45","470.249.766-91","024.055.916-93","129.205.596-05","027.455.496-85","103.259.466-71","103.259.466-71","136.590.076-25","040.155.355-88","036.863.846-47","084.696.116-40","073.041.146-06","552.395.286-91","072.980.986-24","169.528.986-20","073.041.146-06","103.954.766-40","090.179.586-00","063.930.826-07","063.930.826-07","076.207.006-41","076.207.006-41","099.057.916-63","021.072.716-07","056.838.126-23","145.153.596-14","119.270.496-79","092.341.136-40","834.179.826-34","041.141.316-39","002.547.176-75","073.007.376-92","127.497.666-99","017.787.046-03","127.497.666-99","064.117.266-47","131.662.096-46","075.587.426-92","762.771.356-00","041.768.856-37","041.768.856-37","638.202.266-72","073.206.496-16","904.137.846-49","077.908.946-41","048.190.796-38","012.665.186-81","072.980.986-24","112.485.866-03","075.587.426-92","252.878.936-04","063.133.976-06","104.104.456-90","405.154.946-15","564.216.306-00","883.178.896-53","015.161.946-83","962.793.036-91","975.983.726-91","133.202.586-21","042.941.026-32","050.830.836-47","145.153.596-14","115.245.746-27","145.153.596-14","053.230.466-78","013.020.286-00","042.209.156-13","028.468.605-05","028.468.605-05","904.137.846-49","169.528.986-20","127.497.666-99","058.285.956-52","076.207.006-41","127.846.186-84","091.005.947-09","121.236.816-95","036.487.386-82","044.360.766-44","044.360.766-44","044.360.766-44","136.845.086-59","041.227.286-57","200.384.536-49","141.990.066-80","633.777.213-04","141.990.066-80","112.603.086-42","430.115.275-04","128.230.166-78","513.073.166-20","601.213.436-34","053.090.626-02","053.090.626-02","061.507.346-84","111.299.096-89","008.441.047-79","614.612.886-49","648.750.166-20","076.814.546-59","055.556.266-22","145.153.596-14","027.449.596-18","622.283.586-49","036.863.846-47","135.860.116-01","337.171.766-68","405.146.766-04","129.205.596-05","054.819.486-64","405.146.766-04","542.945.336-68","513.361.036-04","129.616.646-59","027.449.596-18","129.616.646-59","112.070.466-98","074.887.116-02","022.327.946-36","470.249.766-91","419.044.356-53","405.154.946-15","037.209.596-86","041.141.316-39","027.449.596-18","013.320.946-60","116.263.156-20","103.555.526-39","043.433.146-50","051.276.416-66","124.489.486-90","136.845.086-59","044.360.766-44","051.276.416-66","513.364.216-49","055.211.276-31","091.005.947-09","029.411.486-63","962.793.036-91","145.153.596-14","091.005.947-09","022.327.946-36","005.240.196-06","064.117.266-47","077.908.946-41","655.219.606-78","121.236.816-95","405.154.946-15","110.815.646-01","036.863.846-47","043.433.146-50","075.402.236-69","387.112.066-91","148.603.406-39","762.771.356-00","047.343.046-02","022.583.656-43","075.402.236-69","087.380.476-75","466.621.626-04","111.299.096-89","996.273.106-20","114.074.606-50","075.587.426-92","964.230.646-87","074.222.586-08","944.710.886-53","145.153.596-14","050.147.726-84","429.111.256-49","106.258.776-67","308.025.476-72","038.704.586-40","029.411.486-63","041.774.496-01","405.675.836-00","140.234.776-64","074.222.586-08","042.941.026-32","107.753.456-60","008.928.836-00","405.675.836-00","466.621.626-04","045.559.076-12","090.234.876-05","050.830.836-47","770.977.506-34","044.719.266-32","785.781.246-34","846.522.846-91","405.666.926-00","060.352.196-75","493.039.966-15","834.238.346-68","834.179.826-34","081.908.236-80","024.055.916-93","070.857.256-13","103.885.956-55","022.327.946-36","040.155.355-88","071.943.326-63","027.449.596-18","136.845.086-59","036.863.846-47","099.506.066-59","071.943.326-63","762.771.356-00","060.248.286-08","861.099.286-15","091.005.947-09","103.704.876-80","053.090.626-02","091.005.947-09","097.831.576-63","097.272.066-97","335.505.656-15","335.505.656-15","041.141.316-39","119.921.137-00","055.556.266-22","145.153.596-14","337.267.106-63","110.376.056-40","109.632.586-13","041.227.286-57","140.234.776-64","064.117.266-47","043.332.236-52","041.227.286-57","550.383.796-72","064.117.266-47","335.505.656-15","078.293.936-80","904.322.826-53","904.322.826-53"

            };
            List<object> actual = new List<object>();

            foreach (dynamic rec in new ChoCSVReader(FileNamePontosCSV).WithHeaderLineAt(9)
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true)
                .Configure(c => c.CultureName = "es-ES")
                .MayHaveQuotedFields()
                .Configure(c => c.AllowReturnPartialLoadedRecs = false)
                .Setup(s => s.RecordLoadError += (o, e) => e.Handled = true)
                .ThrowAndStopOnBadData(false)
                )
            {
                actual.Add(String.Format("{0}", (string)rec["CPF/CNPJ"]));
                Console.WriteLine(String.Format("{0}", (string)rec["CPF/CNPJ"]));
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void HandleBadRecords()
        {
            string expected = @"[
  "","",
  "",""
]";

            List<string> badCSVLines = new List<string>();
            foreach (dynamic rec in new ChoCSVReader(FileNamePontosCSV).WithHeaderLineAt(9)
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true)
                .Configure(c => c.CultureName = "es-ES")
                .MayHaveQuotedFields()
                .Configure(c => c.AllowReturnPartialLoadedRecs = false)
                .Setup(s => s.RecordLoadError += (o, e) => e.Handled = true)
                .ThrowAndStopOnBadData(true)
                .Setup(s => s.BadDataFound += (o, e) =>
                {
                    $"Bad Line: {e.Line}".Print();
                    badCSVLines.Add(e.Line);
                    e.Handled = true;
                })
                )
            {
            }

            var actual = JsonConvert.SerializeObject(badCSVLines, Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample2()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Cust_ID", "TCF4338" }, { "CustName", "INDEXABLE CUTTING TOOL" }, { "CustOrder", "4/11/2016 0:00" }, { "Salary", "$100,000" } },
                new ChoDynamicObject {{ "Cust_ID", "CGO9650" }, { "CustName", "Comercial Tecnipak Ltda" }, { "CustOrder", "7/11/2016 0:00" }, { "Salary", "$80,000" } }
            };

            //var actual = new ChoCSVReader(FileNameSample2CSV).WithFirstLineHeader().ToList();
            //CollectionAssert.AreEqual(expected, actual);

            foreach (var p in new ChoCSVReader("Sample2.csv").WithFirstLineHeader()
                //.Configure(c => c.TypeConverterFormatSpec.TreatCurrencyAsDecimal = false)
                .QuoteAllFields()
                .MayHaveQuotedFields()
                //.WithMaxScanRows(2)
                //.WithField("Cust_ID")
                //.WithField("Salary", fieldType: typeof(Decimal))
                //.Configure(c => c.MaxScanRows = 10)
                )
            {
                Console.WriteLine(p.Dump());
            }
        }

        [Test]
        public static void Sample4()
        {
            List<string> expected = new List<string> { "terion" };
            List<object> actual;

            string csv = @"old,newuser,newpassword
firstlinetomakesure,firstnewusername,firstnewpassword
adslusernameplaintext,thisisthenewuser,andthisisthenewpassword
hello,terion,nadiomn
somethingdownhere,thisisthelastuser,andthisisthelastpassword 
11,12,13
21,22,23 
31,32,33";

            using (var p = new ChoCSVReader(new StringReader(csv))
                .WithFirstLineHeader()
                )
            {
                actual = p.Where(rec => rec.old == "hello").Select(rec => rec.newuser).ToList();
            }
            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void MergeCSV1()
        {
            string expected = @"Id	Name	City
1	Tom	Las Vegas
2	Mark	Dallas";

            string CSV1 = @"Id	Name	City
1	Tom	New York
2	Mark	FairFax";

            string CSV2 = @"Id	City
1	Las Vegas
2	Dallas";

            dynamic rec1 = null;
            dynamic rec2 = null;
            StringBuilder csv3 = new StringBuilder();
            using (var csvOut = new ChoCSVWriter(new StringWriter(csv3))
                .WithFirstLineHeader()
                .WithDelimiter("\t")
                )
            {
                using (var csv1 = new ChoCSVReader(new StringReader(CSV1))
                    .WithFirstLineHeader()
                    .WithDelimiter("\t")
                    )
                {
                    using (var csv2 = new ChoCSVReader(new StringReader(CSV2))
                        .WithFirstLineHeader()
                        .WithDelimiter("\t")
                        )
                    {
                        while ((rec1 = csv1.Read()) != null && (rec2 = csv2.Read()) != null)
                        {
                            rec1.City = rec2.City;
                            csvOut.Write(rec1);
                        }
                    }
                }
            }
            Assert.AreEqual(expected, csv3.ToString());
        }

        [Test]
        public static void Test1()
        {
            //string csv = @"4.1,AB,2018-02-16 15:41:39,152,36,""{""A"":{ ""a1"":""A1""},,20";
            //using (TextFieldParser parser = new TextFieldParser(new StringReader(csv)))
            //{
            //	parser.TextFieldType = FieldType.Delimited;
            //	parser.SetDelimiters(",");
            //	parser.TrimWhiteSpace = true;
            //	parser.HasFieldsEnclosedInQuotes = true;
            //	// I tried HasFieldsEnclosedInQuotes with true and false.

            //	string[] fields = new string[] { };

            //	while (!parser.EndOfData)
            //	{
            //		try
            //		{
            //			fields = parser.ReadFields();
            //		}
            //		catch (MalformedLineException e)
            //		{
            //			Console.WriteLine($"MalformedLineException when parsing CSV");
            //		}
            //		//
            //		//do something of fields...
            //	}
            //}
            ChoDynamicObject expected = new ChoDynamicObject {
                { "Column1", "4.1" },
                { "Column2", "AB" },
                { "Column3", "2018-02-16 15:41:39" },
                { "Column4", "152" },
                { "Column5", "36" },
                { "Column6", @"{""A"":{ ""a1"":""A1""},""B"":{ ""b1"":""B1""}}" },
                { "Column7", @"{""X"":"",""Y"":""ya""}" },
                { "Column8", "20" }
            };
            string csv = @"4.1,AB,2018-02-16 15:41:39,152,36,""{""A"":{ ""a1"":""A1""},""B"":{ ""b1"":""B1""}}"",""{""X"":"""",""Y"":""ya""}"",20";

            var actual = ChoCSVReader.LoadText(csv).MayHaveQuotedFields().First();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void CombineColumns()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject>() {
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,09,56,0)}, { "Column3", "1.2985"}, {"Column4","1.2986"},{"Column5","1.2979"},{"Column6","1.2981"},{"Column7","103"}},
new ChoDynamicObject{ {"Column1","2011.01.08"},{"Column2", new DateTime(2011,1,8,09,57,0)}, { "Column3", "1.2981"}, {"Column4","1.2982"},{"Column5","1.2979"},{"Column6","1.2982"},{"Column7","75"}},
new ChoDynamicObject{ {"Column1","2011.01.09"},{"Column2", new DateTime(2011,1,9,09,58,0)}, { "Column3", "1.2982"}, {"Column4","1.2982"},{"Column5","1.2976"},{"Column6","1.2977"},{"Column7","83"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,09,59,0)}, { "Column3", "1.2977"}, {"Column4","1.2981"},{"Column5","1.2977"},{"Column6","1.2980"},{"Column7","97"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,10,00,0)}, { "Column3", "1.2980"}, {"Column4","1.2980"},{"Column5","1.2978"},{"Column6","1.2979"},{"Column7","101"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,10,01,0)}, { "Column3", "1.2980"}, {"Column4","1.2981"},{"Column5","1.2978"},{"Column6","1.2978"},{"Column7","57"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,10,02,0)}, { "Column3", "1.2978"}, {"Column4","1.2979"},{"Column5","1.2977"},{"Column6","1.2978"},{"Column7","86"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,10,03,0)}, { "Column3", "1.2978"}, {"Column4","1.2978"},{"Column5","1.2973"},{"Column6","1.2973"},{"Column7","84"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,10,04,0)}, { "Column3", "1.2973"}, {"Column4","1.2976"},{"Column5","1.2973"},{"Column6","1.2975"},{"Column7","71"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,10,05,0)}, { "Column3", "1.2974"}, {"Column4","1.2977"},{"Column5","1.2974"},{"Column6","1.2977"},{"Column7","53"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,10,06,0)}, { "Column3", "1.2977"}, {"Column4","1.2979"},{"Column5","1.2976"},{"Column6","1.2978"},{"Column7","57"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,10,07,0)}, { "Column3", "1.2978"}, {"Column4","1.2978"},{"Column5","1.2976"},{"Column6","1.2976"},{"Column7","53"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,10,08,0)}, { "Column3", "1.2976"}, {"Column4","1.2980"},{"Column5","1.2976"},{"Column6","1.2980"},{"Column7","58"}},
new ChoDynamicObject{ {"Column1","2011.01.07"},{"Column2", new DateTime(2011,1,7,10,09,0)}, { "Column3", "1.2979"}, {"Column4","1.2985"},{"Column5","1.2979"},{"Column6","1.2980"},{"Column7","63"}}
            };

            List<object> actual = new List<object>();

            var csv = @"2011.01.07,09:56,1.2985,1.2986,1.2979,1.2981,103
2011.01.08,09:57,1.2981,1.2982,1.2979,1.2982,75
2011.01.09,09:58,1.2982,1.2982,1.2976,1.2977,83
2011.01.07,09:59,1.2977,1.2981,1.2977,1.2980,97
2011.01.07,10:00,1.2980,1.2980,1.2978,1.2979,101
2011.01.07,10:01,1.2980,1.2981,1.2978,1.2978,57
2011.01.07,10:02,1.2978,1.2979,1.2977,1.2978,86
2011.01.07,10:03,1.2978,1.2978,1.2973,1.2973,84
2011.01.07,10:04,1.2973,1.2976,1.2973,1.2975,71
2011.01.07,10:05,1.2974,1.2977,1.2974,1.2977,53
2011.01.07,10:06,1.2977,1.2979,1.2976,1.2978,57
2011.01.07,10:07,1.2978,1.2978,1.2976,1.2976,53
2011.01.07,10:08,1.2976,1.2980,1.2976,1.2980,58
2011.01.07,10:09,1.2979,1.2985,1.2979,1.2980,63";

            foreach (var rec in ChoCSVReader.LoadText(csv)
                .Setup(s => s.AfterRecordFieldLoad += (o, e) =>
                {
                    if (e.PropertyName == "Column2")
                    {
                        dynamic r = e.Record as dynamic;
                        var dtr0 = Convert.ToDateTime(r[0]);
                        var dtr1 = Convert.ToDateTime(r[1]);

                        r[1] = new DateTime(dtr0.Year, dtr0.Month, dtr0.Day, dtr1.Hour, dtr1.Minute, dtr1.Second);
                    }
                })
                )
            {
                actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void DiffCSV()
        {
            string csv1 = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";

            string csv2 = @"Id, Name, City
3, Lou, FL
5, Raj, DC
";

            HashSet<long> lookup = null;
            using (var cp2 = new ChoCSVReader(new StringReader(csv2))
                .WithFirstLineHeader()
                .WithMaxScanRows(1)
                .Setup(p => p.DoWhile += (o, e) =>
                {
                    string line = e.Source as string;
                    e.Stop = line.StartsWith("** Some Match **");
                })
                )
            {
                lookup = new HashSet<long>(cp2.Select(rec => rec.Id).Cast<long>().ToArray());
            }

            StringBuilder csvOut = new StringBuilder();
            using (var cw = new ChoCSVWriter(new StringWriter(csvOut))
                .WithFirstLineHeader()
                )
            {
                using (var cp1 = new ChoCSVReader(new StringReader(csv1))
                    .WithMaxScanRows(1)
                    .WithFirstLineHeader()
                    )
                {
                    foreach (var rec in cp1)
                    {
                        if (lookup.Contains(rec.Id))
                            continue;

                        cw.Write(rec);
                    }
                }
            }

            Console.WriteLine(csvOut.ToString());
        }

        public interface IEmployee
        {
            int Id { get; set; }
            string Name { get; set; }
        }

        public class Employee : IEmployee
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string City { get; set; }

            public override bool Equals(object obj)
            {
                var employee = obj as Employee;
                return employee != null &&
                       Id == employee.Id &&
                       Name == employee.Name &&
                       City == employee.City;
            }

            public override int GetHashCode()
            {
                var hashCode = -1768068776;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(City);
                return hashCode;
            }
        }

        [ChoRecordTypeCode("1")]
        public class Manager : IEmployee
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                var manager = obj as Manager;
                return manager != null &&
                       Id == manager.Id &&
                       Name == manager.Name;
            }

            public override int GetHashCode()
            {
                var hashCode = -1919740922;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                return hashCode;
            }
        }

        [ChoRecordTypeCode("2")]
        public class Manager1
        {
            public int Id
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }
        }
        [Test]
        public static void InterfaceTest()
        {
            List<IEmployee> expected = new List<IEmployee> {
                new Employee{ Id=4, Name="Tom", City="Edison"},
                new Manager{ Id=1, Name="Mark"},
                new Employee{ Id=2, Name="Gom", City="Clark"},
                new Employee{ Id=3, Name="Smith", City="Newark"}
                //new ChoDynamicObject {{ "Id", (int)1 }, { "DateCreated", new DateTime(2018,2,1)}, { "IsActive" , true } },
                //new ChoDynamicObject {{ "Id", (int)2 }, { "DateCreated", new DateTime(2017,11,20)}, { "IsActive" , false } }
            };
            List<object> actual = new List<object>();

            using (var p = new ChoCSVReader<IEmployee>(FileNameInterfaceTestCSV)
                .WithFirstLineHeader()
                //.MapRecordFields<Employee>()
                .WithRecordSelector(1, typeof(Employee), typeof(Manager))
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class LocationDefinition
        {
            public string PlaceName { get; set; }
            public double Longitude { get; set; }
            public double Latitude { get; set; }
            public double Elevation { get; set; }

            public override bool Equals(object obj)
            {
                var definition = obj as LocationDefinition;
                return definition != null &&
                       PlaceName == definition.PlaceName &&
                       Longitude == definition.Longitude &&
                       Latitude == definition.Latitude &&
                       Elevation == definition.Elevation;
            }

            public override int GetHashCode()
            {
                var hashCode = 536176542;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PlaceName);
                hashCode = hashCode * -1521134295 + Longitude.GetHashCode();
                hashCode = hashCode * -1521134295 + Latitude.GetHashCode();
                hashCode = hashCode * -1521134295 + Elevation.GetHashCode();
                return hashCode;
            }
        }
        public class CountDefinition
        {
            public DateTime Date { get; set; }
            public int Count { get; set; }

            public override bool Equals(object obj)
            {
                var definition = obj as CountDefinition;
                return definition != null &&
                       Date == definition.Date &&
                       Count == definition.Count;
            }

            public override int GetHashCode()
            {
                var hashCode = 379610101;
                hashCode = hashCode * -1521134295 + Date.GetHashCode();
                hashCode = hashCode * -1521134295 + Count.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void MultiRecordsInfile()
        {
            List<object> expected = new List<object> {
                new LocationDefinition { PlaceName = "NameString", Longitude = 123.456, Latitude = 56.78, Elevation = 40},
                new CountDefinition {Date = new DateTime(2012,1,1), Count = 1},
                new CountDefinition {Date = new DateTime(2012,2,1), Count = 3},
                new CountDefinition {Date = new DateTime(2012,3,1), Count = 10},
                new CountDefinition {Date = new DateTime(2012,4,2), Count = 6}
            };
            List<object> actual = new List<object>();

            string csv = @"PlaceName,Longitude,Latitude,Elevation
NameString,123.456,56.78,40

Date,Count
1/1/2012,1
2/1/2012,3
3/1/2012,10
4/2/2012,6";

            using (var p = new ChoCSVReader(new StringReader(csv))
                .WithCustomRecordSelector((l) =>
                {
                    Tuple<long, string> kvp = l as Tuple<long, string>;
                    if (kvp.Item1 == 1 || kvp.Item1 == 3 || kvp.Item1 == 4)
                        return null;

                    if (kvp.Item1 < 4)
                        return typeof(LocationDefinition);
                    else
                        return typeof(CountDefinition);
                }
                )
                //.MapRecordFields(typeof(LocationDefinition), typeof(CountDefinition))
                //.Configure(c => c.ThrowAndStopOnMissingField = false)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample3()
        {
            List<Site> expected = new List<Site> {
                new Site{SiteID = 44, House = "608646" , SiteAddress = new SiteAddress{ City = "ODESSA"         , Street = "TEXAS AVE",        SitePostal = new SitePostal{ State = "TX", Zip = "79762"}} },
                new Site{SiteID = 44, House = "487460", SiteAddress = new SiteAddress{ City = "CORPUS CHRISTI"  , Street = "EVERHART RD",  SitePostal = new SitePostal{ State = "TX", Zip = "78413"}} },
                new Site{SiteID = 44, House = "275543", SiteAddress = new SiteAddress{ City = "SAN MARCOS"      , Street = "EDWARD GARY",  SitePostal = new SitePostal{ State = "TX", Zip = "78666"} } }
            };
            List<object> actual = new List<object>();
            using (var p = new ChoCSVReader<Site>(FileNameSample3CSV)
                //.ClearFields()
                //.WithField(m => m.SiteID)
                //.WithField(m => m.SiteAddress.City)
                .WithFirstLineHeader(true)
                )
            {
                //foreach (var rec in p.ExternalSort(new ChoLamdaComparer<Site>((e1, e2) => e1.SiteID - e1.SiteID)))
                //{

                //}
                foreach (var rec in p)
                    actual.Add(rec);
                //Exception ex;
                //Console.WriteLine("IsValid: " + p.IsValid(out ex));
            }
            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void POCOSort()
        {
            Assert.Ignore();

            using (var dr = new ChoCSVReader<EmployeeRec>(FileNameTestCSV).WithFirstLineHeader()
                .WithField(c => c.Id, valueConverter: (v) => Convert.ToInt32(v as string))
                )
            {
                //foreach (var rec in dr.ExternalSort(new ChoLamdaComparer<EmployeeRec>((e1, e2) => DateTime.Compare(e1.AddedDate, e1.AddedDate))))
                //{
                //	Console.WriteLine(rec.CustId);
                //}
            }
        }
        [Test]
        public static void DynamicSort()
        {
            //using (var dr = new ChoCSVReader(FileNameTestCSV).WithFirstLineHeader())
            //{
            //    foreach (var rec in dr.ExternalSort(new ChoLamdaComparer<dynamic>((e1, e2) => DateTime.Compare(e1.AddedDate, e1.AddedDate))))
            //    {
            //        Console.WriteLine(rec.CustId);
            //    }
            //}

            Assert.Ignore();
        }

        [Test]
        public static void CharDiscTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject>() {
//                new ChoDynamicObject((IList<object>)new List<object>{"31350.2","3750.9188","S","14458.8652","E","7.98","50817","0","2.3","0","23"}),
//                new ChoDynamicObject((IList<object>)new List<object>{"31350.4","3750.9204","S1","14458.867","E","6.66","50817","0","2.3","0","23"})
                new ChoDynamicObject{ { "Column1", "31350.2" }, { "Column2", "3750.9188" }, { "Column3", "S" }, { "Column4",  "14458.8652" }, { "Column5", "E" }, { "Column6", "7.98" }, { "Column7", "50817" }, { "Column8", "0" }, { "Column9", "2.3" }, { "Column10", "0"}, { "Column11", "23"} },
                new ChoDynamicObject{ { "Column1", "31350.4" }, { "Column2", "3750.9204" }, { "Column3", "S1" }, { "Column4", "14458.867" }, { "Column5", "E" }, { "Column6", "6.66" }, { "Column7", "50817" }, { "Column8", "0" }, { "Column9", "2.3" }, { "Column10", "0" }, { "Column11", "23" } }
            };
            List<object> actual = new List<object>();

            var csv = @"31350.2,3750.9188,S,14458.8652,E,7.98,50817,0,2.3,0,23
31350.4,3750.9204,S1,14458.867,E,6.66,50817,0,2.3,0,23";

            using (var p = new ChoCSVReader(new StringReader(csv))
                //.Configure(c => c.MaxScanRows = 10)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
                //                    Console.WriteLine(rec.Dump());
            }
            CollectionAssert.AreEqual(expected, actual);
            //throw new Exception("Wrong implementation of indexer, check test case. Expected list with IList<object> wrongly constructur fails.");
            // TODO: expected list with IList<object> should 
        }

        public class Customer
        {
            [ChoTypeConverter(typeof(ChoIntConverter), Parameters = "0000")]
            public int CustId { get; set; }
            public string Name { get; set; }
            public decimal Balance { get; set; }
            public DateTime AddedDate { get; set; }

            public override bool Equals(object obj)
            {
                var customer = obj as Customer;
                return customer != null &&
                       CustId == customer.CustId &&
                       Name == customer.Name &&
                       Balance == customer.Balance &&
                       AddedDate == customer.AddedDate;
            }

            public override int GetHashCode()
            {
                var hashCode = -1218692519;
                hashCode = hashCode * -1521134295 + CustId.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + Balance.GetHashCode();
                hashCode = hashCode * -1521134295 + AddedDate.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void ConverterTest()
        {
            List<Customer> expected = new List<Customer> {
                new Customer { CustId=1, Name="Tom", Balance = new Decimal(12.001), AddedDate= new DateTime(2018,1,1) },
                new Customer { CustId=2, Name="Mark", Balance = new Decimal(100.001), AddedDate= new DateTime(2018,12,1) }
            };
            List<object> actual = new List<object>();

            var csv = @"0001, Tom, 12.001, 1/1/2018
0002, Mark, 100.001, 12/1/2018";

            using (var p = new ChoCSVReader<Customer>(new StringReader(csv))
                //.Configure(c => c.MaxScanRows = 10)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class EmpIgnoreCase
        {
            public int ID { get; set; }
        }
        [Test]
        public static void NullValueTest()
        {
            string csv = @"Id, Name, City
1, Tom, {NULL}
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";

            string expected = @"[
  {
    ""Id"": ""1"",
    ""Name"": ""Tom"",
    ""City"": null
  },
  {
    ""Id"": ""2"",
    ""Name"": ""Mark"",
    ""City"": ""NJ""
  },
  {
    ""Id"": ""3"",
    ""Name"": ""Lou"",
    ""City"": ""FL""
  },
  {
    ""Id"": ""4"",
    ""Name"": ""Smith"",
    ""City"": ""PA""
  },
  {
    ""Id"": ""5"",
    ""Name"": ""Raj"",
    ""City"": ""DC""
  }
]";

            StringBuilder csvOut = new StringBuilder();
            using (var cp2 = new ChoCSVReader(new StringReader(csv))
                .WithFirstLineHeader()
                .Configure(c => c.NullValue = "{NULL}")
                .Configure(c => c.FileHeaderConfiguration.IgnoreCase = false)
                )
            {
                var actual = JsonConvert.SerializeObject(cp2, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void Sample10()
        {
            string expected = @"[
  {
    ""institution_id"": ""88"",
    ""UNITID"": ""209612"",
    ""school_id"": ""65"",
    ""gss_code"": ""823"",
    ""year"": ""2015"",
    ""Institution_Name"": ""Pacific University"",
    ""hdg_inst"": ""1"",
    ""toc_code"": ""2""
  },
  {
    ""institution_id"": ""606"",
    ""UNITID"": ""122612"",
    ""school_id"": ""752"",
    ""gss_code"": ""202"",
    ""year"": ""2015"",
    ""Institution_Name"": ""University of San Francisco"",
    ""hdg_inst"": ""2"",
    ""toc_code"": ""2""
  },
  {
    ""institution_id"": ""606"",
    ""UNITID"": ""122612"",
    ""school_id"": ""752"",
    ""gss_code"": ""401"",
    ""year"": ""2015"",
    ""Institution_Name"": ""University of San Francisco"",
    ""hdg_inst"": ""2"",
    ""toc_code"": ""2""
  }
]";
            string actual = "";

            string csv = @"institution_id,UNITID,school_id,gss_code,year,Institution_Name,hdg_inst,toc_code
88,209612,65,823,2015,Pacific University,1,2
606,122612,752,202,2015,University of San Francisco,2,2
606,122612,752,401,2015,University of San Francisco,2,2";

            using (var p = new ChoCSVReader(new StringReader(csv))
                .WithFirstLineHeader()
                )
            {
                actual = ChoJSONWriter.ToTextAll(p);
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DateFormatTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", (int)1 }, { "DateCreated", new DateTime(2018,2,1)}, { "IsActive" , true } },
                new ChoDynamicObject {{ "Id", (int)2 }, { "DateCreated", new DateTime(2017,11,20)}, { "IsActive" , false } }
            };
            List<object> actual = new List<object>();

            string csv = @"Id, DateCreated, IsActive
                1, 20180201, A
                2, 20171120, B";

            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.Custom;
            using (var p = new ChoCSVReader(new StringReader(csv))
                .WithFirstLineHeader()
                .WithField("Id", fieldType: typeof(int))
                .WithField("DateCreated", fieldType: typeof(DateTime), formatText: "yyyyMMdd")
                .WithField("IsActive", fieldType: typeof(bool), formatText: "A")
                //.Configure(c => c.RegisterTypeConverterForType<bool>(new ChoBooleanCustomConverter()))
                //.WithField("IsActive", fieldType: typeof(bool), valueConverter: o => o.ToNString() == "A")
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [ChoCSVFileHeader]
        public class Consumer
        {
            public int Id { get; set; }
            [DisplayFormat(DataFormatString = "yyyyMMdd")]
            public DateTime DateCreated { get; set; }
            [DisplayFormat(DataFormatString = "A")]
            public bool IsActive { get; set; }

            public override bool Equals(object obj)
            {
                var consumer = obj as Consumer;
                return consumer != null &&
                       Id == consumer.Id &&
                       DateCreated == consumer.DateCreated &&
                       IsActive == consumer.IsActive;
            }

            public override int GetHashCode()
            {
                var hashCode = 673371854;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + DateCreated.GetHashCode();
                hashCode = hashCode * -1521134295 + IsActive.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void DateFormatTestUsingPOCO()
        {
            List<Consumer> expected = new List<Consumer> {
                new Consumer { Id = 1, DateCreated = new DateTime(2018,2,1), IsActive = true },
                new Consumer { Id = 2, DateCreated = new DateTime(2017,11,20), IsActive = false }
            };
            List<object> actual = new List<object>();

            string csv = @"Id, DateCreated, IsActive
                1, 20180201, A
                2, 20171120, B";

            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.Custom;
            using (var p = new ChoCSVReader<Consumer>(new StringReader(csv)))
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }
        [ChoCSVFileHeader]
        public class ConsumerOptIn
        {
            [ChoCSVRecordField(1)]
            public int Id { get; set; }
            [ChoCSVRecordField(2, FormatText = "yyyyMMdd")]
            public DateTime DateCreated { get; set; }
            [ChoCSVRecordField(3, FormatText = "A")]
            public bool IsActive { get; set; }

            public override bool Equals(object obj)
            {
                var @in = obj as ConsumerOptIn;
                return @in != null &&
                       Id == @in.Id &&
                       DateCreated == @in.DateCreated &&
                       IsActive == @in.IsActive;
            }

            public override int GetHashCode()
            {
                var hashCode = 673371854;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + DateCreated.GetHashCode();
                hashCode = hashCode * -1521134295 + IsActive.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void DateFormatTestUsingOptInPOCO()
        {
            List<ConsumerOptIn> expected = new List<ConsumerOptIn> {
                new ConsumerOptIn { Id = 1, DateCreated = new DateTime(2018,2,1), IsActive = true },
                new ConsumerOptIn { Id = 2, DateCreated = new DateTime(2017,11,20), IsActive = false }
            };
            List<object> actual = new List<object>();

            string csv = @"Id, DateCreated, IsActive
                1, 20180201, A
                2, 20171120, B";

            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.Custom;
            using (var p = new ChoCSVReader<ConsumerOptIn>(new StringReader(csv)))
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
            //Assert.That(expected, Is.EquivalentTo(actual));
        }

        public class ImportRow
        {
            public int ImportId { get; set; }
            public int RowIndex { get; set; }
            public string fields { get; set; }
        }

        public class ValueObject
        {
            public string value { get; set; }
        }

        [Test]
        public static void Sample20()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add(new DataColumn("ImportId", typeof(int)) { AllowDBNull = false });
            expected.Columns.Add(new DataColumn("RowIndex", typeof(int)) { AllowDBNull = false });
            expected.Columns.Add(new DataColumn("fields", typeof(string)));
            expected.Rows.Add(42, 0, "[{\"value\":\"acme\"},{\"value\":\"1\"},{\"value\":\"1 / 1 / 2015\"}]");// "acme", 1, new DateTime(2015, 1, 1));
            expected.Rows.Add(42, 1, "[{\"value\":\"contoso\"},{\"value\":\"34\"},{\"value\":\"1/2/2018\"}]");// "contoso", 34, new DateTime(2018, 1, 2));
            expected.AcceptChanges();

            string csv = @"""acme"" ""1"" ""1 / 1 / 2015""
""contoso"" ""34"" ""1/2/2018""
";

            using (var p = new ChoCSVReader(new StringReader(csv))
                .WithDelimiter(" ")
                .MayHaveQuotedFields()
                )
            {
                int rowIndex = 0;

                var dr = new ChoEnumerableDataReader(p.Select(r => new ImportRow
                {
                    ImportId = 42,
                    RowIndex = rowIndex++,
                    fields = JsonConvert.SerializeObject(((ChoDynamicObject)r).Values.Select(r1 => new ValueObject { value = r1.ToNString() }))
                }
                ));
                DataTable dt = new DataTable();
                dt.Load(dr);

                UnitTestHelper.DataTableAssert.AreEqual(expected, dt);
                //foreach (var rec in p.Select(r => new ImportRow
                //	{
                //		ImportId = 42,
                //		RowIndex = rowIndex++,
                //		fields = JsonConvert.SerializeObject(((ChoDynamicObject)r).Values.Select(r1 => new ValueObject { value = r1.ToNString() }))
                //	}
                //	)
                //)
                //{

                //	Console.WriteLine(rec.Dump());
                //}
            }
        }

        [Test]
        public static void Sample21()
        {
            DataTable expected = new DataTable();
            expected.Columns.AddRange(new DataColumn[] {
                new DataColumn("face"), new DataColumn("confidence"), new DataColumn("gaze_0_x"), new DataColumn("gaze_0_y"), new DataColumn("gaze_0_z"), new DataColumn("gaze_1_x"), new DataColumn("gaze_1_y"), new DataColumn("gaze_1_z"), new DataColumn("gaze_angle_x"), new DataColumn("gaze_angle_y"), new DataColumn("eye_lmk_x_0"), new DataColumn("eye_lmk_x_1"), new DataColumn("eye_lmk_x_2"), new DataColumn("eye_lmk_x_3"), new DataColumn("eye_lmk_x_4"), new DataColumn("eye_lmk_x_5"), new DataColumn("eye_lmk_x_6"), new DataColumn("eye_lmk_x_7"), new DataColumn("eye_lmk_x_8"), new DataColumn("eye_lmk_x_9"), new DataColumn("eye_lmk_x_10"), new DataColumn("eye_lmk_x_11"), new DataColumn("eye_lmk_x_12"), new DataColumn("eye_lmk_x_13"), new DataColumn("eye_lmk_x_14"), new DataColumn("eye_lmk_x_15"), new DataColumn("eye_lmk_x_16"), new DataColumn("eye_lmk_x_17"), new DataColumn("eye_lmk_x_18"), new DataColumn("eye_lmk_x_19"), new DataColumn("eye_lmk_x_20"), new DataColumn("eye_lmk_x_21"), new DataColumn("eye_lmk_x_22"), new DataColumn("eye_lmk_x_23"), new DataColumn("eye_lmk_x_24"), new DataColumn("eye_lmk_x_25"), new DataColumn("eye_lmk_x_26"), new DataColumn("eye_lmk_x_27"), new DataColumn("eye_lmk_x_28"), new DataColumn("eye_lmk_x_29"), new DataColumn("eye_lmk_x_30"), new DataColumn("eye_lmk_x_31"), new DataColumn("eye_lmk_x_32"), new DataColumn("eye_lmk_x_33"), new DataColumn("eye_lmk_x_34"), new DataColumn("eye_lmk_x_35"), new DataColumn("eye_lmk_x_36"), new DataColumn("eye_lmk_x_37"), new DataColumn("eye_lmk_x_38"), new DataColumn("eye_lmk_x_39"), new DataColumn("eye_lmk_x_40"), new DataColumn("eye_lmk_x_41"), new DataColumn("eye_lmk_x_42"), new DataColumn("eye_lmk_x_43"), new DataColumn("eye_lmk_x_44"), new DataColumn("eye_lmk_x_45"), new DataColumn("eye_lmk_x_46"), new DataColumn("eye_lmk_x_47"), new DataColumn("eye_lmk_x_48"), new DataColumn("eye_lmk_x_49"), new DataColumn("eye_lmk_x_50"), new DataColumn("eye_lmk_x_51"), new DataColumn("eye_lmk_x_52"), new DataColumn("eye_lmk_x_53"), new DataColumn("eye_lmk_x_54"), new DataColumn("eye_lmk_x_55"), new DataColumn("eye_lmk_y_0"), new DataColumn("eye_lmk_y_1"), new DataColumn("eye_lmk_y_2"), new DataColumn("eye_lmk_y_3"), new DataColumn("eye_lmk_y_4"), new DataColumn("eye_lmk_y_5"), new DataColumn("eye_lmk_y_6"), new DataColumn("eye_lmk_y_7"), new DataColumn("eye_lmk_y_8"), new DataColumn("eye_lmk_y_9"), new DataColumn("eye_lmk_y_10"), new DataColumn("eye_lmk_y_11"), new DataColumn("eye_lmk_y_12"), new DataColumn("eye_lmk_y_13"), new DataColumn("eye_lmk_y_14"), new DataColumn("eye_lmk_y_15"), new DataColumn("eye_lmk_y_16"), new DataColumn("eye_lmk_y_17"), new DataColumn("eye_lmk_y_18"), new DataColumn("eye_lmk_y_19"), new DataColumn("eye_lmk_y_20"), new DataColumn("eye_lmk_y_21"), new DataColumn("eye_lmk_y_22"), new DataColumn("eye_lmk_y_23"), new DataColumn("eye_lmk_y_24"), new DataColumn("eye_lmk_y_25"), new DataColumn("eye_lmk_y_26"), new DataColumn("eye_lmk_y_27"), new DataColumn("eye_lmk_y_28"), new DataColumn("eye_lmk_y_29"), new DataColumn("eye_lmk_y_30"), new DataColumn("eye_lmk_y_31"), new DataColumn("eye_lmk_y_32"), new DataColumn("eye_lmk_y_33"), new DataColumn("eye_lmk_y_34"), new DataColumn("eye_lmk_y_35"), new DataColumn("eye_lmk_y_36"), new DataColumn("eye_lmk_y_37"), new DataColumn("eye_lmk_y_38"), new DataColumn("eye_lmk_y_39"), new DataColumn("eye_lmk_y_40"), new DataColumn("eye_lmk_y_41"), new DataColumn("eye_lmk_y_42"), new DataColumn("eye_lmk_y_43"), new DataColumn("eye_lmk_y_44"), new DataColumn("eye_lmk_y_45"), new DataColumn("eye_lmk_y_46"), new DataColumn("eye_lmk_y_47"), new DataColumn("eye_lmk_y_48"), new DataColumn("eye_lmk_y_49"), new DataColumn("eye_lmk_y_50"), new DataColumn("eye_lmk_y_51"), new DataColumn("eye_lmk_y_52"), new DataColumn("eye_lmk_y_53"), new DataColumn("eye_lmk_y_54"), new DataColumn("eye_lmk_y_55"), new DataColumn("eye_lmk_X_01"), new DataColumn("eye_lmk_X_110"), new DataColumn("eye_lmk_X_210"), new DataColumn("eye_lmk_X_310"), new DataColumn("eye_lmk_X_410"),new DataColumn("eye_lmk_X_56"), new DataColumn("eye_lmk_X_61"),new DataColumn("eye_lmk_X_71"), new DataColumn("eye_lmk_X_81"), new DataColumn("eye_lmk_X_91"),new DataColumn("eye_lmk_X_101"), new DataColumn("eye_lmk_X_111"), new DataColumn("eye_lmk_X_121"), new DataColumn("eye_lmk_X_131"), new DataColumn("eye_lmk_X_141"), new DataColumn("eye_lmk_X_151"), new DataColumn("eye_lmk_X_161"), new DataColumn("eye_lmk_X_171"), new DataColumn("eye_lmk_X_181"), new DataColumn("eye_lmk_X_191"), new DataColumn("eye_lmk_X_201"), new DataColumn("eye_lmk_X_211"), new DataColumn("eye_lmk_X_221"), new DataColumn("eye_lmk_X_231"), new DataColumn("eye_lmk_X_241"), new DataColumn("eye_lmk_X_251"), new DataColumn("eye_lmk_X_261"), new DataColumn("eye_lmk_X_271"), new DataColumn("eye_lmk_X_281"), new DataColumn("eye_lmk_X_291"), new DataColumn("eye_lmk_X_301"), new DataColumn("eye_lmk_X_311"), new DataColumn("eye_lmk_X_321"), new DataColumn("eye_lmk_X_331"), new DataColumn("eye_lmk_X_341"), new DataColumn("eye_lmk_X_351"), new DataColumn("eye_lmk_X_361"), new DataColumn("eye_lmk_X_371"), new DataColumn("eye_lmk_X_381"), new DataColumn("eye_lmk_X_391"), new DataColumn("eye_lmk_X_401"), new DataColumn("eye_lmk_X_411"), new DataColumn("eye_lmk_X_421"), new DataColumn("eye_lmk_X_431"), new DataColumn("eye_lmk_X_441"), new DataColumn("eye_lmk_X_451"), new DataColumn("eye_lmk_X_461"), new DataColumn("eye_lmk_X_471"), new DataColumn("eye_lmk_X_481"), new DataColumn("eye_lmk_X_491"), new DataColumn("eye_lmk_X_501"), new DataColumn("eye_lmk_X_511"), new DataColumn("eye_lmk_X_521"), new DataColumn("eye_lmk_X_531"), new DataColumn("eye_lmk_X_541"), new DataColumn("eye_lmk_X_551"), new DataColumn("eye_lmk_Y_01"), new DataColumn("eye_lmk_Y_110"), new DataColumn("eye_lmk_Y_210"), new DataColumn("eye_lmk_Y_310"), new DataColumn("eye_lmk_Y_410"), new DataColumn("eye_lmk_Y_56"), new DataColumn("eye_lmk_Y_61"), new DataColumn("eye_lmk_Y_71"), new DataColumn("eye_lmk_Y_81"), new DataColumn("eye_lmk_Y_91"), new DataColumn("eye_lmk_Y_101"), new DataColumn("eye_lmk_Y_111"), new DataColumn("eye_lmk_Y_121"), new DataColumn("eye_lmk_Y_131"), new DataColumn("eye_lmk_Y_141"), new DataColumn("eye_lmk_Y_151"), new DataColumn("eye_lmk_Y_161"), new DataColumn("eye_lmk_Y_171"), new DataColumn("eye_lmk_Y_181"), new DataColumn("eye_lmk_Y_191"), new DataColumn("eye_lmk_Y_201"), new DataColumn("eye_lmk_Y_211"), new DataColumn("eye_lmk_Y_221"), new DataColumn("eye_lmk_Y_231"), new DataColumn("eye_lmk_Y_241"), new DataColumn("eye_lmk_Y_251"), new DataColumn("eye_lmk_Y_261"), new DataColumn("eye_lmk_Y_271"), new DataColumn("eye_lmk_Y_281"), new DataColumn("eye_lmk_Y_291"), new DataColumn("eye_lmk_Y_301"), new DataColumn("eye_lmk_Y_311"), new DataColumn("eye_lmk_Y_321"), new DataColumn("eye_lmk_Y_331"), new DataColumn("eye_lmk_Y_341"), new DataColumn("eye_lmk_Y_351"), new DataColumn("eye_lmk_Y_361"), new DataColumn("eye_lmk_Y_371"), new DataColumn("eye_lmk_Y_381"), new DataColumn("eye_lmk_Y_391"), new DataColumn("eye_lmk_Y_401"), new DataColumn("eye_lmk_Y_411"), new DataColumn("eye_lmk_Y_421"), new DataColumn("eye_lmk_Y_431"), new DataColumn("eye_lmk_Y_441"), new DataColumn("eye_lmk_Y_451"), new DataColumn("eye_lmk_Y_461"), new DataColumn("eye_lmk_Y_471"), new DataColumn("eye_lmk_Y_481"), new DataColumn("eye_lmk_Y_491"), new DataColumn("eye_lmk_Y_501"), new DataColumn("eye_lmk_Y_511"), new DataColumn("eye_lmk_Y_521"), new DataColumn("eye_lmk_Y_531"), new DataColumn("eye_lmk_Y_541"), new DataColumn("eye_lmk_Y_551"), new DataColumn("eye_lmk_Z_0"), new DataColumn("eye_lmk_Z_1"), new DataColumn("eye_lmk_Z_2"), new DataColumn("eye_lmk_Z_3"), new DataColumn("eye_lmk_Z_4"), new DataColumn("eye_lmk_Z_5"), new DataColumn("eye_lmk_Z_6"), new DataColumn("eye_lmk_Z_7"), new DataColumn("eye_lmk_Z_8"), new DataColumn("eye_lmk_Z_9"), new DataColumn("eye_lmk_Z_10"), new DataColumn("eye_lmk_Z_11"), new DataColumn("eye_lmk_Z_12"), new DataColumn("eye_lmk_Z_13"), new DataColumn("eye_lmk_Z_14"), new DataColumn("eye_lmk_Z_15"), new DataColumn("eye_lmk_Z_16"), new DataColumn("eye_lmk_Z_17"), new DataColumn("eye_lmk_Z_18"), new DataColumn("eye_lmk_Z_19"), new DataColumn("eye_lmk_Z_20"), new DataColumn("eye_lmk_Z_21"), new DataColumn("eye_lmk_Z_22"), new DataColumn("eye_lmk_Z_23"), new DataColumn("eye_lmk_Z_24"), new DataColumn("eye_lmk_Z_25"), new DataColumn("eye_lmk_Z_26"), new DataColumn("eye_lmk_Z_27"), new DataColumn("eye_lmk_Z_28"), new DataColumn("eye_lmk_Z_29"), new DataColumn("eye_lmk_Z_30"), new DataColumn("eye_lmk_Z_31"), new DataColumn("eye_lmk_Z_32"), new DataColumn("eye_lmk_Z_33"), new DataColumn("eye_lmk_Z_34"), new DataColumn("eye_lmk_Z_35"), new DataColumn("eye_lmk_Z_36"), new DataColumn("eye_lmk_Z_37"), new DataColumn("eye_lmk_Z_38"), new DataColumn("eye_lmk_Z_39"), new DataColumn("eye_lmk_Z_40"), new DataColumn("eye_lmk_Z_41"), new DataColumn("eye_lmk_Z_42"), new DataColumn("eye_lmk_Z_43"), new DataColumn("eye_lmk_Z_44"), new DataColumn("eye_lmk_Z_45"), new DataColumn("eye_lmk_Z_46"), new DataColumn("eye_lmk_Z_47"), new DataColumn("eye_lmk_Z_48"), new DataColumn("eye_lmk_Z_49"), new DataColumn("eye_lmk_Z_50"), new DataColumn("eye_lmk_Z_51"), new DataColumn("eye_lmk_Z_52"), new DataColumn("eye_lmk_Z_53"), new DataColumn("eye_lmk_Z_54"), new DataColumn("eye_lmk_Z_55"), new DataColumn("pose_Tx"), new DataColumn("pose_Ty"), new DataColumn("pose_Tz"), new DataColumn("pose_Rx"), new DataColumn("pose_Ry"), new DataColumn("pose_Rz"), new DataColumn("x_0"), new DataColumn("x_1"), new DataColumn("x_2"),new DataColumn("x_3"), new DataColumn("x_4"), new DataColumn("x_5"), new DataColumn("x_6"), new DataColumn("x_7"), new DataColumn("x_8"), new DataColumn("x_9"), new DataColumn("x_10"), new DataColumn("x_11"), new DataColumn("x_12"), new DataColumn("x_13"), new DataColumn("x_14"), new DataColumn("x_15"), new DataColumn("x_16"), new DataColumn("x_17"), new DataColumn("x_18"), new DataColumn("x_19"), new DataColumn("x_20"), new DataColumn("x_21"), new DataColumn("x_22"), new DataColumn("x_23"), new DataColumn("x_24"), new DataColumn("x_25"), new DataColumn("x_26"), new DataColumn("x_27"), new DataColumn("x_28"), new DataColumn("x_29"), new DataColumn("x_30"), new DataColumn("x_31"), new DataColumn("x_32"), new DataColumn("x_33"), new DataColumn("x_34"), new DataColumn("x_35"), new DataColumn("x_36"), new DataColumn("x_37"), new DataColumn("x_38"), new DataColumn("x_39"), new DataColumn("x_40"), new DataColumn("x_41"), new DataColumn("x_42"), new DataColumn("x_43"), new DataColumn("x_44"), new DataColumn("x_45"), new DataColumn("x_46"), new DataColumn("x_47"), new DataColumn("x_48"), new DataColumn("x_49"), new DataColumn("x_50"), new DataColumn("x_51"), new DataColumn("x_52"), new DataColumn("x_53"), new DataColumn("x_54"), new DataColumn("x_55"), new DataColumn("x_56"), new DataColumn("x_57"), new DataColumn("x_58"), new DataColumn("x_59"), new DataColumn("x_60"), new DataColumn("x_61"), new DataColumn("x_62"), new DataColumn("x_63"), new DataColumn("x_64"), new DataColumn("x_65"), new DataColumn("x_66"), new DataColumn("x_67"), new DataColumn("y_0"), new DataColumn("y_1"), new DataColumn("y_2"), new DataColumn("y_3"), new DataColumn("y_4"), new DataColumn("y_5"), new DataColumn("y_6"), new DataColumn("y_7"), new DataColumn("y_8"), new DataColumn("y_9"), new DataColumn("y_10"), new DataColumn("y_11"), new DataColumn("y_12"), new DataColumn("y_13"), new DataColumn("y_14"), new DataColumn("y_15"), new DataColumn("y_16"), new DataColumn("y_17"), new DataColumn("y_18"), new DataColumn("y_19"), new DataColumn("y_20"), new DataColumn("y_21"), new DataColumn("y_22"), new DataColumn("y_23"), new DataColumn("y_24"), new DataColumn("y_25"), new DataColumn("y_26"), new DataColumn("y_27"), new DataColumn("y_28"), new DataColumn("y_29"), new DataColumn("y_30"), new DataColumn("y_31"), new DataColumn("y_32"), new DataColumn("y_33"), new DataColumn("y_34"), new DataColumn("y_35"), new DataColumn("y_36"), new DataColumn("y_37"), new DataColumn("y_38"), new DataColumn("y_39"), new DataColumn("y_40"), new DataColumn("y_41"), new DataColumn("y_42"), new DataColumn("y_43"), new DataColumn("y_44"), new DataColumn("y_45"), new DataColumn("y_46"), new DataColumn("y_47"), new DataColumn("y_48"), new DataColumn("y_49"), new DataColumn("y_50"), new DataColumn("y_51"), new DataColumn("y_52"), new DataColumn("y_53"), new DataColumn("y_54"), new DataColumn("y_55"), new DataColumn("y_56"), new DataColumn("y_57"), new DataColumn("y_58"), new DataColumn("y_59"), new DataColumn("y_60"), new DataColumn("y_61"), new DataColumn("y_62"), new DataColumn("y_63"), new DataColumn("y_64"), new DataColumn("y_65"), new DataColumn("y_66"), new DataColumn("y_67"),  new DataColumn("X_01"), new DataColumn("X_110"), new DataColumn("X_210"), new DataColumn("X_310"), new DataColumn("X_410"), new DataColumn("X_510"), new DataColumn("X_68"), new DataColumn("X_71"), new DataColumn("X_81"), new DataColumn("X_91"), new DataColumn("X_101"), new DataColumn("X_111"), new DataColumn("X_121"), new DataColumn("X_131"), new DataColumn("X_141"), new DataColumn("X_151"), new DataColumn("X_161"), new DataColumn("X_171"), new DataColumn("X_181"), new DataColumn("X_191"), new DataColumn("X_201"), new DataColumn("X_211"), new DataColumn("X_221"), new DataColumn("X_231"), new DataColumn("X_241"), new DataColumn("X_251"), new DataColumn("X_261"), new DataColumn("X_271"), new DataColumn("X_281"), new DataColumn("X_291"), new DataColumn("X_301"), new DataColumn("X_311"), new DataColumn("X_321"), new DataColumn("X_331"), new DataColumn("X_341"), new DataColumn("X_351"), new DataColumn("X_361"), new DataColumn("X_371"), new DataColumn("X_381"), new DataColumn("X_391"), new DataColumn("X_401"), new DataColumn("X_411"), new DataColumn("X_421"), new DataColumn("X_431"), new DataColumn("X_441"), new DataColumn("X_451"), new DataColumn("X_461"), new DataColumn("X_471"), new DataColumn("X_481"), new DataColumn("X_491"), new DataColumn("X_501"), new DataColumn("X_511"), new DataColumn("X_521"), new DataColumn("X_531"), new DataColumn("X_541"), new DataColumn("X_551"), new DataColumn("X_561"), new DataColumn("X_571"), new DataColumn("X_581"), new DataColumn("X_591"), new DataColumn("X_601"), new DataColumn("X_611"), new DataColumn("X_621"), new DataColumn("X_631"), new DataColumn("X_641"), new DataColumn("X_651"), new DataColumn("X_661"), new DataColumn("X_671"), new DataColumn("Y_01"), new DataColumn("Y_110"), new DataColumn("Y_210"), new DataColumn("Y_310"), new DataColumn("Y_410"), new DataColumn("Y_510"), new DataColumn("Y_68"), new DataColumn("Y_71"), new DataColumn("Y_81"), new DataColumn("Y_91"), new DataColumn("Y_101"), new DataColumn("Y_111"), new DataColumn("Y_121"), new DataColumn("Y_131"), new DataColumn("Y_141"), new DataColumn("Y_151"), new DataColumn("Y_161"), new DataColumn("Y_171"), new DataColumn("Y_181"), new DataColumn("Y_191"), new DataColumn("Y_201"), new DataColumn("Y_211"), new DataColumn("Y_221"), new DataColumn("Y_231"), new DataColumn("Y_241"), new DataColumn("Y_251"), new DataColumn("Y_261"), new DataColumn("Y_271"), new DataColumn("Y_281"), new DataColumn("Y_291"), new DataColumn("Y_301"), new DataColumn("Y_311"), new DataColumn("Y_321"), new DataColumn("Y_331"), new DataColumn("Y_341"), new DataColumn("Y_351"), new DataColumn("Y_361"), new DataColumn("Y_371"), new DataColumn("Y_381"), new DataColumn("Y_391"), new DataColumn("Y_401"), new DataColumn("Y_411"), new DataColumn("Y_421"), new DataColumn("Y_431"), new DataColumn("Y_441"), new DataColumn("Y_451"), new DataColumn("Y_461"), new DataColumn("Y_471"), new DataColumn("Y_481"), new DataColumn("Y_491"), new DataColumn("Y_501"), new DataColumn("Y_511"), new DataColumn("Y_521"), new DataColumn("Y_531"), new DataColumn("Y_541"), new DataColumn("Y_551"), new DataColumn("Y_561"), new DataColumn("Y_571"), new DataColumn("Y_581"), new DataColumn("Y_591"), new DataColumn("Y_601"), new DataColumn("Y_611"), new DataColumn("Y_621"), new DataColumn("Y_631"), new DataColumn("Y_641"), new DataColumn("Y_651"), new DataColumn("Y_661"), new DataColumn("Y_671"), new DataColumn("Z_0"), new DataColumn("Z_1"), new DataColumn("Z_2"), new DataColumn("Z_3"), new DataColumn("Z_4"), new DataColumn("Z_5"), new DataColumn("Z_6"), new DataColumn("Z_7"), new DataColumn("Z_8"), new DataColumn("Z_9"), new DataColumn("Z_10"), new DataColumn("Z_11"), new DataColumn("Z_12"), new DataColumn("Z_13"), new DataColumn("Z_14"), new DataColumn("Z_15"), new DataColumn("Z_16"), new DataColumn("Z_17"), new DataColumn("Z_18"), new DataColumn("Z_19"), new DataColumn("Z_20"), new DataColumn("Z_21"), new DataColumn("Z_22"), new DataColumn("Z_23"), new DataColumn("Z_24"), new DataColumn("Z_25"), new DataColumn("Z_26"), new DataColumn("Z_27"), new DataColumn("Z_28"), new DataColumn("Z_29"), new DataColumn("Z_30"), new DataColumn("Z_31"), new DataColumn("Z_32"), new DataColumn("Z_33"), new DataColumn("Z_34"), new DataColumn("Z_35"), new DataColumn("Z_36"), new DataColumn("Z_37"), new DataColumn("Z_38"), new DataColumn("Z_39"), new DataColumn("Z_40"), new DataColumn("Z_41"), new DataColumn("Z_42"), new DataColumn("Z_43"), new DataColumn("Z_44"), new DataColumn("Z_45"), new DataColumn("Z_46"), new DataColumn("Z_47"), new DataColumn("Z_48"), new DataColumn("Z_49"), new DataColumn("Z_50"), new DataColumn("Z_51"), new DataColumn("Z_52"), new DataColumn("Z_53"), new DataColumn("Z_54"), new DataColumn("Z_55"), new DataColumn("Z_56"), new DataColumn("Z_57"), new DataColumn("Z_58"), new DataColumn("Z_59"), new DataColumn("Z_60"), new DataColumn("Z_61"), new DataColumn("Z_62"), new DataColumn("Z_63"), new DataColumn("Z_64"), new DataColumn("Z_65"), new DataColumn("Z_66"), new DataColumn("Z_67"), new DataColumn("p_scale"), new DataColumn("p_rx"), new DataColumn("p_ry"), new DataColumn("p_rz"), new DataColumn("p_tx"), new DataColumn("p_ty"), new DataColumn("p_0"), new DataColumn("p_1"), new DataColumn("p_2"), new DataColumn("p_3"), new DataColumn("p_4"), new DataColumn("p_5"), new DataColumn("p_6"), new DataColumn("p_7"), new DataColumn("p_8"), new DataColumn("p_9"), new DataColumn("p_10"), new DataColumn("p_11"), new DataColumn("p_12"), new DataColumn("p_13"), new DataColumn("p_14"), new DataColumn("p_15"), new DataColumn("p_16"), new DataColumn("p_17"), new DataColumn("p_18"), new DataColumn("p_19"), new DataColumn("p_20"), new DataColumn("p_21"), new DataColumn("p_22"), new DataColumn("p_23"), new DataColumn("p_24"), new DataColumn("p_25"), new DataColumn("p_26"), new DataColumn("p_27"), new DataColumn("p_28"), new DataColumn("p_29"), new DataColumn("p_30"), new DataColumn("p_31"), new DataColumn("p_32"), new DataColumn("p_33"), new DataColumn("AU01_r"), new DataColumn("AU02_r"), new DataColumn("AU04_r"), new DataColumn("AU05_r"), new DataColumn("AU06_r"), new DataColumn("AU07_r"), new DataColumn("AU09_r"), new DataColumn("AU10_r"), new DataColumn("AU12_r"), new DataColumn("AU14_r"), new DataColumn("AU15_r"), new DataColumn("AU17_r"), new DataColumn("AU20_r"), new DataColumn("AU23_r"), new DataColumn("AU25_r"), new DataColumn("AU26_r"), new DataColumn("AU45_r"), new DataColumn("AU01_c"), new DataColumn("AU02_c"), new DataColumn("AU04_c"), new DataColumn("AU05_c"), new DataColumn("AU06_c"), new DataColumn("AU07_c"), new DataColumn("AU09_c"), new DataColumn("AU10_c"), new DataColumn("AU12_c"), new DataColumn("AU14_c"), new DataColumn("AU15_c"), new DataColumn("AU17_c"), new DataColumn("AU20_c"), new DataColumn("AU23_c"), new DataColumn("AU25_c"), new DataColumn("AU26_c"), new DataColumn("AU28_c"), new DataColumn("AU45_c")
            });
            //            expected.Rows.Add("7602281", "0.983", "0.003957", "-0.006063", "-0.999974", "-0.036896", "0.002067", "-0.999317", "-0.016", "-0.002", "280.1", "284.1", "291.5", "297.9", "299.6", "296.1", "288.2", "281.8", "271.7", "275.9", "282.3", "290.6", "298.7", "304.1", "307.4", "301.8", "294.5", "287.0", "280.2", "274.8", "286.5", "289.4", "292.8", "294.6", "293.8", "290.9", "287.5", "285.7", "372.4", "376.1", "383.4", "390.1", "392.3", "388.6", "381.3", "373.9", "362.3", "367.7", "374.8", "383.6", "391.4", "396.8", "399.5", "395.5", "389.6", "382.4", "374.9", "367.5", "378.4", "381.5", "384.7", "386.4", "385.4", "382.4", "379.1", "377.5", "252.0", "245.2", "243.3", "247.4", "255.1", "262.5", "263.7", "259.6", "253.8", "249.5", "247.4", "247.4", "250.9", "256.0", "262.4", "264.4", "264.3", "263.5", "261.5", "258.4", "256.4", "258.2", "257.4", "254.3", "250.8", "248.9", "249.8", "252.9", "265.7", "258.7", "256.6", "260.6", "268.5", "275.5", "277.6", "274.1", "270.9", "265.6", "262.0", "261.0", "263.7", "268.2", "274.1", "276.5", "277.0", "276.4", "275.1", "273.6", "270.2", "272.0", "271.0", "267.9", "264.4", "262.6", "263.5", "266.7", "-22.2", "-20.0", "-15.8", "-12.2", "-11.3", "-13.2", "-17.6", "-21.2", "-27.4", "-24.8", "-20.9", "-16.2", "-11.7", "-8.8", "-7.0", "-10.1", "-14.1", "-18.3", "-22.2", "-25.4", "-18.6", "-17.0", "-15.1", "-14.1", "-14.5", "-16.2", "-18.1", "-19.1", "28.4", "30.5", "34.6", "38.4", "39.6", "37.4", "33.3", "29.2", "23.0", "25.8", "29.6", "34.4", "39.0", "42.5", "44.4", "41.7", "38.0", "33.9", "29.7", "25.8", "31.8", "33.5", "35.4", "36.3", "35.8", "34.1", "32.3", "31.3", "6.7", "2.9", "1.8", "4.1", "8.3", "12.4", "13.2", "10.9", "7.8", "5.3", "4.1", "4.1", "6.0", "8.9", "12.4", "13.5", "13.4", "13.0", "12.0", "10.3", "9.1", "10.1", "9.6", "7.9", "6.0", "5.0", "5.4", "7.2", "13.9", "10.2", "9.1", "11.3", "15.6", "19.4", "20.4", "18.5", "16.8", "13.9", "11.9", "11.4", "13.0", "15.6", "19.0", "20.1", "20.2", "19.7", "19.0", "18.2", "16.4", "17.5", "17.0", "15.3", "13.3", "12.3", "12.8", "14.5", "278.4", "277.9", "277.0", "276.2", "276.0", "276.6", "277.3", "278.1", "284.0", "280.6", "277.3", "275.2", "275.2", "276.5", "278.1", "277.1", "276.4", "276.8", "278.5", "281.1", "278.4", "278.0", "277.6", "277.4", "277.5", "277.9", "278.3", "278.5", "270.8", "271.7", "273.0", "273.8", "273.7", "272.8", "271.5", "270.9", "272.3", "271.1", "270.3", "270.7", "273.0", "276.2", "279.4", "276.0", "273.0", "271.1", "270.5", "271.2", "272.3", "272.7", "273.3", "273.7", "273.7", "273.3", "272.8", "272.4", "5.1", "57.6", "331.5", "-0.011", "0.024", "0.135", "234.5", "231.7", "232.5", "235.1", "242.5", "255.7", "270.8", "289.7", "312.5", "335.5", "356.9", "376.9", "394.4", "407.8", "418.0", "425.6", "430.1", "251.0", "268.3", "289.0", "308.1", "324.6", "357.2", "376.8", "395.9", "413.8", "423.8", "339.0", "336.6", "334.4", "332.2", "307.3", "317.0", "327.2", "338.7", "348.9", "270.5", "282.8", "297.8", "308.5", "295.2", "279.9", "362.0", "376.0", "390.4", "399.7", "389.6", "375.4", "281.1", "298.2", "313.5", "323.0", "334.3", "347.5", "359.6", "345.1", "331.0", "319.2", "308.3", "294.4", "287.4", "311.9", "322.0", "333.3", "353.2", "332.7", "321.4", "311.2", "251.9", "283.1", "315.4", "346.4", "375.8", "401.6", "423.3", "439.9", "446.3", "445.4", "432.3", "413.7", "392.3", "367.2", "339.9", "311.4", "282.3", "230.3", "221.1", "221.8", "228.3", "238.7", "242.9", "237.6", "237.5", "243.2", "256.5", "261.8", "283.4", "304.9", "327.0", "335.9", "341.4", "346.4", "344.8", "341.9", "254.3", "247.7", "250.8", "263.0", "264.2", "262.1", "270.6", "261.4", "263.6", "273.0", "277.5", "275.5", "370.9", "368.5", "367.5", "372.0", "370.7", "375.8", "381.6", "392.6", "395.5", "395.1", "392.3", "385.0", "373.3", "376.6", "379.2", "379.3", "382.2", "380.3", "380.3", "377.6", "-63.5", "-66.5", "-66.7", "-65.1", "-58.8", "-47.8", "-35.6", "-21.2", "-5.2", "10.9", "26.8", "42.4", "55.8", "65.7", "72.7", "77.4", "80.1", "-46.4", "-33.9", "-20.0", "-7.5", "2.9", "23.3", "36.0", "49.1", "61.9", "70.4", "12.0", "10.2", "8.6", "7.2", "-8.0", "-1.9", "4.4", "11.6", "18.2", "-33.0", "-24.4", "-14.5", "-7.5", "-16.2", "-26.3", "27.6", "36.7", "46.3", "53.2", "45.9", "36.4", "-26.2", "-14.1", "-4.2", "1.9", "9.2", "17.9", "26.7", "16.4", "7.1", "-0.5", "-7.5", "-16.7", "-21.7", "-5.2", "1.3", "8.6", "22.2", "8.2", "0.9", "-5.6", "8.8", "32.5", "57.5", "81.7", "102.9", "120.0", "132.5", "139.8", "142.5", "144.2", "139.7", "129.4", "114.2", "95.1", "74.1", "52.3", "30.7", "-6.5", "-12.4", "-11.7", "-7.4", "-0.8", "1.8", "-1.5", "-1.6", "2.1", "11.2", "13.7", "26.7", "39.0", "51.3", "60.7", "63.2", "65.8", "65.2", "64.1", "9.5", "5.1", "7.0", "15.0", "15.8", "14.5", "20.1", "14.1", "15.5", "22.0", "24.7", "23.3", "88.2", "83.4", "81.4", "84.1", "83.6", "88.5", "95.3", "99.9", "100.3", "99.5", "98.0", "94.7", "88.8", "88.0", "89.4", "90.0", "95.0", "90.4", "89.9", "88.4", "371.3", "376.6", "381.5", "383.8", "379.0", "371.4", "361.6", "349.6", "345.5", "351.1", "363.2", "372.6", "375.0", "373.7", "370.9", "366.6", "363.7", "336.4", "328.2", "321.4", "314.9", "311.3", "312.8", "316.9", "323.2", "330.3", "339.2", "314.3", "307.5", "300.7", "294.6", "316.3", "311.8", "309.2", "311.4", "314.7", "332.5", "328.3", "326.3", "326.5", "326.7", "328.5", "327.8", "328.3", "329.3", "333.8", "329.7", "328.4", "336.8", "324.5", "319.0", "318.6", "319.6", "325.8", "336.5", "327.3", "322.5", "321.0", "321.7", "326.6", "333.2", "322.1", "321.2", "323.0", "333.8", "322.2", "320.4", "321.4", "1.497", "0.178", "0.028", "0.133", "327.885", "327.552", "-27723", "16039", "-11693", "29110", "3.572", "18.196", "2.053", "2.836", "3.558", "-0.799", "1025", "0.529", "-3210", "0.481", "-0.217", "1305", "-1365", "-3293", "0.601", "-2550", "-2233", "-1885", "-0.675", "2725", "-1815", "-1740", "-0.618", "-0.101", "-0.200", "0.149", "0.535", "-0.775", "0.304", "-0.156", "1.61", "0.10", "0.00", "1.91", "0.00", "0.52", "0.00", "0.00", "0.00", "0.84", "0.96", "0.30", "0.00", "0.49", "0.94", "0.00", "0.10", "1.0", "0.0", "0.0", "1.0", "0.0", "0.0", "0.0", "1.0", "0.0", "1.0", "1.0", "0.0", "0.0", "0.0", "1.0", "0.0", "1.0", "0.0");
            expected.Rows.Add("7602281", "0.983", "0.003957", "-0.006063", "-0.999974", "-0.036896", "0.002067", "-0.999317", "-0.016", "-0.002", "280.1", "284.1", "291.5", "297.9", "299.6", "296.1", "288.2", "281.8", "271.7", "275.9", "282.3", "290.6", "298.7", "304.1", "307.4", "301.8", "294.5", "287.0", "280.2", "274.8", "286.5", "289.4", "292.8", "294.6", "293.8", "290.9", "287.5", "285.7", "372.4", "376.1", "383.4", "390.1", "392.3", "388.6", "381.3", "373.9", "362.3", "367.7", "374.8", "383.6", "391.4", "396.8", "399.5", "395.5", "389.6", "382.4", "374.9", "367.5", "378.4", "381.5", "384.7", "386.4", "385.4", "382.4", "379.1", "377.5", "252.0", "245.2", "243.3", "247.4", "255.1", "262.5", "263.7", "259.6", "253.8", "249.5", "247.4", "247.4", "250.9", "256.0", "262.4", "264.4", "264.3", "263.5", "261.5", "258.4", "256.4", "258.2", "257.4", "254.3", "250.8", "248.9", "249.8", "252.9", "265.7", "258.7", "256.6", "260.6", "268.5", "275.5", "277.6", "274.1", "270.9", "265.6", "262.0", "261.0", "263.7", "268.2", "274.1", "276.5", "277.0", "276.4", "275.1", "273.6", "270.2", "272.0", "271.0", "267.9", "264.4", "262.6", "263.5", "266.7", "-22.2", "-20.0", "-15.8", "-12.2", "-11.3", "-13.2", "-17.6", "-21.2", "-27.4", "-24.8", "-20.9", "-16.2", "-11.7", "-8.8", "-7.0", "-10.1", "-14.1", "-18.3", "-22.2", "-25.4", "-18.6", "-17.0", "-15.1", "-14.1", "-14.5", "-16.2", "-18.1", "-19.1", "28.4", "30.5", "34.6", "38.4", "39.6", "37.4", "33.3", "29.2", "23.0", "25.8", "29.6", "34.4", "39.0", "42.5", "44.4", "41.7", "38.0", "33.9", "29.7", "25.8", "31.8", "33.5", "35.4", "36.3", "35.8", "34.1", "32.3", "31.3", "6.7", "2.9", "1.8", "4.1", "8.3", "12.4", "13.2", "10.9", "7.8", "5.3", "4.1", "4.1", "6.0", "8.9", "12.4", "13.5", "13.4", "13.0", "12.0", "10.3", "9.1", "10.1", "9.6", "7.9", "6.0", "5.0", "5.4", "7.2", "13.9", "10.2", "9.1", "11.3", "15.6", "19.4", "20.4", "18.5", "16.8", "13.9", "11.9", "11.4", "13.0", "15.6", "19.0", "20.1", "20.2", "19.7", "19.0", "18.2", "16.4", "17.5", "17.0", "15.3", "13.3", "12.3", "12.8", "14.5", "278.4", "277.9", "277.0", "276.2", "276.0", "276.6", "277.3", "278.1", "284.0", "280.6", "277.3", "275.2", "275.2", "276.5", "278.1", "277.1", "276.4", "276.8", "278.5", "281.1", "278.4", "278.0", "277.6", "277.4", "277.5", "277.9", "278.3", "278.5", "270.8", "271.7", "273.0", "273.8", "273.7", "272.8", "271.5", "270.9", "272.3", "271.1", "270.3", "270.7", "273.0", "276.2", "279.4", "276.0", "273.0", "271.1", "270.5", "271.2", "272.3", "272.7", "273.3", "273.7", "273.7", "273.3", "272.8", "272.4", "5.1", "57.6", "331.5", "-0.011", "0.024", "0.135", "234.5", "231.7", "232.5", "235.1", "242.5", "255.7", "270.8", "289.7", "312.5", "335.5", "356.9", "376.9", "394.4", "407.8", "418.0", "425.6", "430.1", "251.0", "268.3", "289.0", "308.1", "324.6", "357.2", "376.8", "395.9", "413.8", "423.8", "339.0", "336.6", "334.4", "332.2", "307.3", "317.0", "327.2", "338.7", "348.9", "270.5", "282.8", "297.8", "308.5", "295.2", "279.9", "362.0", "376.0", "390.4", "399.7", "389.6", "375.4", "281.1", "298.2", "313.5", "323.0", "334.3", "347.5", "359.6", "345.1", "331.0", "319.2", "308.3", "294.4", "287.4", "311.9", "322.0", "333.3", "353.2", "332.7", "321.4", "311.2", "251.9", "283.1", "315.4", "346.4", "375.8", "401.6", "423.3", "439.9", "446.3", "445.4", "432.3", "413.7", "392.3", "367.2", "339.9", "311.4", "282.3", "230.3", "221.1", "221.8", "228.3", "238.7", "242.9", "237.6", "237.5", "243.2", "256.5", "261.8", "283.4", "304.9", "327.0", "335.9", "341.4", "346.4", "344.8", "341.9", "254.3", "247.7", "250.8", "263.0", "264.2", "262.1", "270.6", "261.4", "263.6", "273.0", "277.5", "275.5", "370.9", "368.5", "367.5", "372.0", "370.7", "375.8", "381.6", "392.6", "395.5", "395.1", "392.3", "385.0", "373.3", "376.6", "379.2", "379.3", "382.2", "380.3", "380.3", "377.6", "-63.5", "-66.5", "-66.7", "-65.1", "-58.8", "-47.8", "-35.6", "-21.2", "-5.2", "10.9", "26.8", "42.4", "55.8", "65.7", "72.7", "77.4", "80.1", "-46.4", "-33.9", "-20.0", "-7.5", "2.9", "23.3", "36.0", "49.1", "61.9", "70.4", "12.0", "10.2", "8.6", "7.2", "-8.0", "-1.9", "4.4", "11.6", "18.2", "-33.0", "-24.4", "-14.5", "-7.5", "-16.2", "-26.3", "27.6", "36.7", "46.3", "53.2", "45.9", "36.4", "-26.2", "-14.1", "-4.2", "1.9", "9.2", "17.9", "26.7", "16.4", "7.1", "-0.5", "-7.5", "-16.7", "-21.7", "-5.2", "1.3", "8.6", "22.2", "8.2", "0.9", "-5.6", "8.8", "32.5", "57.5", "81.7", "102.9", "120.0", "132.5", "139.8", "142.5", "144.2", "139.7", "129.4", "114.2", "95.1", "74.1", "52.3", "30.7", "-6.5", "-12.4", "-11.7", "-7.4", "-0.8", "1.8", "-1.5", "-1.6", "2.1", "11.2", "13.7", "26.7", "39.0", "51.3", "60.7", "63.2", "65.8", "65.2", "64.1", "9.5", "5.1", "7.0", "15.0", "15.8", "14.5", "20.1", "14.1", "15.5", "22.0", "24.7", "23.3", "88.2", "83.4", "81.4", "84.1", "83.6", "88.5", "95.3", "99.9", "100.3", "99.5", "98.0", "94.7", "88.8", "88.0", "89.4", "90.0", "95.0", "90.4", "89.9", "88.4", "371.3", "376.6", "381.5", "383.8", "379.0", "371.4", "361.6", "349.6", "345.5", "351.1", "363.2", "372.6", "375.0", "373.7", "370.9", "366.6", "363.7", "336.4", "328.2", "321.4", "314.9", "311.3", "312.8", "316.9", "323.2", "330.3", "339.2", "314.3", "307.5", "300.7", "294.6", "316.3", "311.8", "309.2", "311.4", "314.7", "332.5", "328.3", "326.3", "326.5", "326.7", "328.5", "327.8", "328.3", "329.3", "333.8", "329.7", "328.4", "336.8", "324.5", "319.0", "318.6", "319.6", "325.8", "336.5", "327.3", "322.5", "321.0", "321.7", "326.6", "333.2", "322.1", "321.2", "323.0", "333.8", "322.2", "320.4", "321.4", "1.497", "0.178", "0.028", "0.133", "327.885", "327.552", "-27.723", "16.039", "-11.693", "29.110", "3.572", "18.196", "2.053", "2.836", "3.558", "-0.799", "1.025", "0.529", "-3.210", "0.481", "-0.217", "1.305", "-1.365", "-3.293", "0.601", "-2.550", "-2.233", "-1.885", "-0.675", "2.725", "-1.815", "-1.740", "-0.618", "-0.101", "-0.200", "0.149", "0.535", "-0.775", "0.304", "-0.156", "1.61", "0.10", "0.00", "1.91", "0.00", "0.52", "0.00", "0.00", "0.00", "0.84", "0.96", "0.30", "0.00", "0.49", "0.94", "0.00", "0.10", "1.0", "0.0", "0.0", "1.0", "0.0", "0.0", "0.0", "1.0", "0.0", "1.0", "1.0", "0.0", "0.0", "0.0", "1.0", "0.0", "1.0", "0.0");
            expected.AcceptChanges();

            using (var p = new ChoCSVReader(FileName020180412_045106CroppedCSV)
                .WithFirstLineHeader()
                .Configure(c => c.FileHeaderConfiguration.IgnoreCase = false)
                )
            {
                var dr = p.AsDataReader();
                var dt = new DataTable();
                dt.Load(dr);

                UnitTestHelper.DataTableAssert.AreEqual(expected, dt);
            }

            return;
            foreach (var p in new ChoCSVReader("020180412_045106Cropped.csv")
                .WithFirstLineHeader()
                .Configure(c => c.FileHeaderConfiguration.IgnoreCase = false)
                )
            {
                Console.WriteLine(p.DumpAsJson());
            }
        }

        [Test]
        public static void ReadHeaderAt3()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "name", "a" }, { "q1", "0"}, { "q2" , "1"} , { "q3", "2-Data" } }
            };
            List<object> actual = new List<object>();

            string csv = @"v3,vf,gf
v1,c,z1,e
name,q1,q2,q3
a,0,1,2-Data";

            using (var p = ChoCSVReader.LoadText(csv)
                .WithHeaderLineAt(3)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void CSV2XmlTest()
        {
            string expected = @"<Emps>
  <Emp>
    <Id>1</Id>
    <Name>Tom</Name>
  </Emp>
  <Emp>
    <Id>2</Id>
    <Name>Mark</Name>
  </Emp>
  <Emp>
    <Id>3</Id>
    <Name>Lou</Name>
  </Emp>
  <Emp>
    <Id>4</Id>
    <Name>Smith</Name>
  </Emp>
  <Emp>
    <Id>5</Id>
    <Name>Raj</Name>
  </Emp>
</Emps>";

            string csv = @"Id, Name, City
                1, Tom, NY
                2, Mark, NJ
                3, Lou, FL
                4, Smith, PA
                5, Raj, DC";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                .WithField("Id")
                .WithField("Name")
                )
            {
                using (var w = new ChoXmlWriter(sb)
                    .Configure(c => c.RootName = "Emps")
                    .Configure(c => c.NodeName = "Emp")
                    .Configure(c => c.DoNotEmitXmlNamespace = true)
                    )
                {
                    w.Write(p);
                }
            }

            string actual = sb.ToString();
            Assert.AreEqual(expected, actual);
            // TODO: Change simple string compare to better XML-content compare
        }

        [Test]
        public static void MapTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id 1", "1" }, { "Name", "Tom"}, { "City" , "NY" } },
                new ChoDynamicObject {{ "Id 1", "2" }, { "Name", "Mark"}, { "City" , "NJ" } },
                new ChoDynamicObject {{ "Id 1", "3" }, { "Name", "Lou"}, { "City" , "FL" } },
                new ChoDynamicObject {{ "Id 1", "4" }, { "Name", "Smith"}, { "City" , "PA" } },
                new ChoDynamicObject {{ "Id 1", "5" }, { "Name", "Raj"}, { "City" , "DC" } }
            };
            List<object> actual = new List<object>();

            string csv = @"Id 1, Name, City
                1, Tom, NY
                2, Mark, NJ
                3, Lou, FL
                4, Smith, PA
                5, Raj, DC";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                //.WithField(m => m.Id)
                )
            {
                foreach (var rec in p)
                    actual.Add(rec);

                //using (var w = new ChoXmlWriter(sb)
                //    .Configure(c => c.RootName = "Emps")
                //    .Configure(c => c.NodeName = "Emp")
                //    )
                //{
                //    w.Write(p);
                //}
            }
            CollectionAssert.AreEqual(expected, actual);

        }

        [Test]
        public static void VariableFieldsTest()
        {
            //ChoETLFrxBootstrap.TraceLevel = TraceLevel.Verbose;

            string csv = @"Id, Name, City
                1, Tom, NY
                2, Mark, NJ, 100
                3, Lou, FL
                4, Smith, PA
                5, Raj, DC";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader(true)
                .Configure(c => c.MaxScanRows = 5)
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                )
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.DumpAsJson());

                //using (var w = new ChoXmlWriter(sb)
                //    .Configure(c => c.RootName = "Emps")
                //    .Configure(c => c.NodeName = "Emp")
                //    )
                //{
                //    w.Write(p);
                //}
            }

        }

        [Test]
        public static void DelimitedImportReaderChoCsvTest()
        {
            var errors = new List<Exception>();
            var rowCount = 0;

            using (var stream = File.Open(ChoPath.GetFullPath(FileNameBadFileCSV), FileMode.Open))
            {
                using (var reader = new ChoCSVReader(stream).WithDelimiter("\t").WithFirstLineHeader()
                    .Configure(c => c.MaxScanRows = 0)
                    )
                {
                    reader.RecordLoadError += (sender, e) =>
                    {
                        errors.Add(e.Exception);
                        e.Handled = true;
                    };

                    var dataReader = reader.AsDataReader();

                    var x = dataReader.GetSchemaTable();
                    while (dataReader.Read())
                    {
                        rowCount++;
                    }
                }
            }

            Assert.AreEqual(errors.Count.ToString() + "," + rowCount.ToString(), "0,11");
        }

        [Test]
        public void Join()
        {
            string expected = @"StudentSisId,Name,Relationship
111111,Betty,Mother
111111,Betty,Father
222222,Veronica,Mother
333333,Jughead,
444444,Archie,Father";

            string csv1 = @"StudentSisId,Name
111111,Betty
222222,Veronica
333333,Jughead
444444,Archie";

            string csv2 = @"StudentSisId,Relationship
111111,Mother
111111,Father
222222,Mother
444444,Father
";

            StringBuilder sb = new StringBuilder();
            using (var p1 = ChoCSVReader.LoadText(csv1)
                .WithFirstLineHeader()
                )
            {
                using (var p2 = ChoCSVReader.LoadText(csv2)
                    .WithFirstLineHeader()
                    )
                {
                    var j1 = p1.LeftJoin(p2, r1 => r1.StudentSisId,
                        (r1) => new { r1.StudentSisId, r1.Name, Relationship = (string)null },
                        (r1, r2) => new { r1.StudentSisId, r1.Name, Relationship = r2 != null ? (string)r2.Relationship : null }
                        ).ToArray();

                    foreach (object rec in j1)
                    {
                        Console.WriteLine(rec);
                    }
                    //var j1 = from r1 in p1
                    //		 join r2 in p2
                    //			on r1.StudentSisId equals r2.StudentSisId into p22
                    //			from r22 in p22.DefaultIfEmpty()
                    //		select new { StudentSisId = r1.StudentSisId, Name = r1.Name, Relationship = r22 != null ? r22.Relationship : null };

                    using (var w = new ChoCSVWriter(sb)
                        .WithFirstLineHeader()
                        )
                        w.Write(j1);
                }
            }

            var actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void CSV2JSON()
        {
            string expected = "[\r\n {\r\n  \"Id\": \"1\",\r\n  \"Name\": \"Tom\",\r\n  \"City\": \"NY\"\r\n },\r\n {\r\n  \"Id\": \"2\",\r\n  \"Name\": \"Mark\",\r\n  \"City\": \"NJ\"\r\n },\r\n {\r\n  \"Id\": \"3\",\r\n  \"Name\": \"Lou\",\r\n  \"City\": \"FL\"\r\n },\r\n {\r\n  \"Id\": \"4\",\r\n  \"Name\": \"Smith\",\r\n  \"City\": \"PA\"\r\n },\r\n {\r\n  \"Id\": \"5\",\r\n  \"Name\": \"Raj\",\r\n  \"City\": \"DC\"\r\n }\r\n]";

            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoJSONWriter(sb))
                    w.Write(p);
            }

            string actual = sb.ToString();
            Console.WriteLine(actual);

            //Assert.AreEqual(expected, actual);
            // TODO: Change simple string compare to better JSON content compare
        }

        [Test]
        public static void DoubleQuotesFix()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject { {"Column1", "something not qualified" }, { "Column2", "12\" x 12\" something qualified, becuase it has a comma" } , {"Column3","one more without a qualifier" } }
            };

            List<object> actual = new List<object>();

            using (var x = new ChoCSVReader(FileNameDoubleQuotesTestCSV)
                .MayHaveQuotedFields())
            {
                foreach (var rec in x)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample5()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add();
            expected.Columns.Add();
            expected.Columns.Add();
            expected.Rows.Add("0.0.0.0", "0.255.255.255", "ZZ");
            expected.Rows.Add("1.0.0.0", "1.0.0.255", "AU");
            expected.Rows.Add("1.0.1.0", "1.0.3.255", "CN");
            expected.Rows.Add("1.0.4.0", "1.0.7.255", "AU");
            expected.Rows.Add("1.0.8.0", "1.0.15.255", "CN");
            expected.Rows.Add("1.0.16.0", "1.0.31.255", "JP");
            expected.Rows.Add("1.0.32.0", "1.0.63.255", "CN");
            expected.Rows.Add("1.0.64.0", "1.0.127.255", "JP");
            expected.Rows.Add("1.0.128.0", "1.0.255.255", "TH");
            expected.Rows.Add("1.1.0.0", "1.1.0.255", "CN");
            expected.Rows.Add("1.1.1.0", "1.1.1.255", "AU");
            expected.Rows.Add("1.1.2.0", "1.1.63.255", "CN");
            expected.Rows.Add("1.1.64.0", "1.1.127.255", "JP");
            expected.Rows.Add("1.1.128.0", "1.1.255.255", "TH");
            expected.AcceptChanges();
            DataTable actual;

            using (var p = new ChoCSVReader(FileNameSample5CSV)
                .MayHaveQuotedFields())
            {
                actual = p.AsDataTable();
            }
            UnitTestHelper.DataTableAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample6()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add();
            expected.Columns.Add();
            expected.Columns.Add();
            expected.Rows.Add("0.0.0.0", "0.255.255.255", "ZZ");
            expected.Rows.Add("1.0.0.0", "1.0.0.255", "AU");
            expected.Rows.Add("1.0.1.0", "1.0.3.255", "CN");
            expected.Rows.Add("1.0.4.0", "1.0.7.255", "AU");
            expected.Rows.Add("1.0.8.0", "1.0.15.255", "CN");
            expected.Rows.Add("1.0.16.0", "1.0.31.255", "JP");
            expected.Rows.Add("1.0.32.0", "1.0.63.255", "CN");
            expected.Rows.Add("1.0.64.0", "1.0.127.255", "JP");
            expected.Rows.Add("1.0.128.0", "1.0.255.255", "TH");
            expected.Rows.Add("1.1.0.0", "1.1.0.255", "CN");
            expected.Rows.Add("1.1.1.0", "1.1.1.255", "AU");
            expected.Rows.Add("1.1.2.0", "1.1.63.255", "CN");
            expected.Rows.Add("1.1.64.0", "1.1.127.255", "JP");
            expected.Rows.Add("1.1.128.0", "1.1.255.255", "TH");
            expected.AcceptChanges();
            DataTable actual;

            using (var p = new ChoCSVReader(FileNameSample6CSV)
                .MayHaveQuotedFields())
            {
                p.SanitizeLine += (o, e) =>
                {
                    string line = e.Line as string;
                    if (line != null)
                    {
                        line = line.Substring(1, line.Length - 2);
                        e.Line = line.Replace(@"""""", @"""");
                    }
                };

                actual = p.AsDataTable();
            }
            UnitTestHelper.DataTableAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Sample61()
        {
            DataTable expected = new DataTable();
            expected.Columns.Add();
            expected.Columns.Add();
            expected.Columns.Add();
            expected.Rows.Add("0.0.0.0", "0.255.255.255", "ZZ");
            expected.Rows.Add("1.0.0.0", "1.0.0.255", "AU");
            expected.Rows.Add("1.0.1.0", "1.0.3.255", "CN");
            expected.Rows.Add("1.0.4.0", "1.0.7.255", "AU");
            expected.Rows.Add("1.0.8.0", "1.0.15.255", "CN");
            expected.Rows.Add("1.0.16.0", "1.0.31.255", "JP");
            expected.Rows.Add("1.0.32.0", "1.0.63.255", "CN");
            expected.Rows.Add("1.0.64.0", "1.0.127.255", "JP");
            expected.Rows.Add("1.0.128.0", "1.0.255.255", "TH");
            expected.Rows.Add("1.1.0.0", "1.1.0.255", "CN");
            expected.Rows.Add("1.1.1.0", "1.1.1.255", "AU");
            expected.Rows.Add("1.1.2.0", "1.1.63.255", "CN");
            expected.Rows.Add("1.1.64.0", "1.1.127.255", "JP");
            expected.Rows.Add("1.1.128.0", "1.1.255.255", "TH");
            expected.AcceptChanges();
            DataTable actual;

            using (var p = new ChoCSVReader(FileNameSample6CSV)
                .MayHaveQuotedFields())
            {
                var lines = p.Select(r => (string)r[0]).ToArray();
                using (var p1 = ChoCSVReader.LoadLines(lines)
                    .MayHaveQuotedFields())
                {
                    actual = p1.AsDataTable();
                }
            }
            UnitTestHelper.DataTableAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void SepInValueTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Column1", "2" }, { "Column2", "1016" }, { "Column3", "7/31/2008 14:22" }, { "Column4", "Geoff Dalgas" }, { "Column5", "6/5/2011 22:21" }, { "Column6", "http://stackoverflow.com" }, { "Column7", "Corvallis, OR" }, { "Column8", "7679" }, { "Column9", "351" }, { "Column10", "81" }, { "Column11", "b437f461b3fd27387c5d8ab47a293d35" }, { "Column12", "34" }, }
            };
            List<object> actual = new List<object>();

            string csv = @"2,1016,7/31/2008 14:22,Geoff Dalgas,6/5/2011 22:21,http://stackoverflow.com,""Corvallis, OR"",7679,351,81,b437f461b3fd27387c5d8ab47a293d35,34";

            using (var p = ChoCSVReader.LoadText(csv)
                .MayHaveQuotedFields()
                )
            {
                actual = p.ToList();
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        public class BoolObject
        {
            [DisplayFormat(DataFormatString = "yyyyMMdd")]
            public DateTime Created { get; set; }
            public string Name { get; set; }
            public bool? Active { get; set; }

            public override bool Equals(object obj)
            {
                var @object = obj as BoolObject;
                return @object != null &&
                       Created == @object.Created &&
                       Name == @object.Name &&
                       Active == @object.Active;
            }

            public override int GetHashCode()
            {
                var hashCode = -1463536688;
                hashCode = hashCode * -1521134295 + Created.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + Active.GetHashCode();
                return hashCode;
            }
        }
        [Test]
        public static void BoolTest1()
        {
            BoolObject expected = new BoolObject() { Created = new DateTime(2018, 1, 1), Name = "Raj" };
            string csv = @"20180101,Raj,";

            using (var p = ChoCSVReader<BoolObject>.LoadText(csv)
                .ThrowAndStopOnMissingField(false))
            {
                var actual = p.FirstOrDefault();
                Assert.AreEqual(expected, actual);
                //                Console.WriteLine(p.FirstOrDefault().Dump());
            }
        }

        [Test]
        public static void TransposeTest()
        {
            string expected = @"A1;A2;A3;A4;A5
B1;B2;B3;B4;B5
C1;C2;C3;C4;C5
D1;D2;D3;D4;D5
E1;E2;E3;E4;E5";

            string csv = @"A1;B1;C1;D1;E1
A2;B2;C2;D2;E2
A3;B3;C3;D3;E3
A4;B4;C4;D4;E4
A5;B5;C5;D5;E5
";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithDelimiter(";")
                .ThrowAndStopOnMissingField(false)
                )
            {
                using (var w = new ChoCSVWriter(sb)
                    .WithDelimiter(";")
                    )
                {
                    w.Write(p.Cast<ChoDynamicObject>().Transpose(false));
                }
            }

            string actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void TransposeTest1()
        {
            string expected = @"a1,a2,a3,a4,a5
b1,b2,b3,b4,b5
c1,c2,c3,c4,c5
d1,d2,d3,d4,d5
e1,e2,e3,e4,e5";

            string csv = @"a1,b1,c1,d1,e1
a2,b2,c2,d2,e2
a3,b3,c3,d3,e3
a4,b4,c4,d4,e4
a5,b5,c5,d5,e5
";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv))
            {
                using (var w = new ChoCSVWriter(sb))
                    w.Write(p.Cast<ChoDynamicObject>().Transpose(false));
            }
            string actual = sb.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void FixNewLine()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "CODE", "A" }, { "COMPANY NAME", "My Name , LLC"}, { "DATE" , "2018-01-28" }, { "ACTION" , "BUY" } },
                new ChoDynamicObject {{ "CODE", "B" }, { "COMPANY NAME", "Your Name , LLC"}, { "DATE" , "2018-01-25" }, { "ACTION" , "SELL" } },
                new ChoDynamicObject {{ "CODE", "C" }, { "COMPANY NAME", "All Name , LLC"}, { "DATE" , "2018-01-21" }, { "ACTION" , "SELL" } },
                new ChoDynamicObject {{ "CODE", "D" }, { "COMPANY NAME", "World Name , LLC"}, { "DATE" , "2018-01-20" }, { "ACTION" , "BUY" } }
            };
            List<object> actual = new List<object>();

            string csv = @"CODE,COMPANY NAME, DATE, ACTION
A,My Name , LLC,2018-01-28,BUY
B,Your Name , LLC,2018-01-25,SELL
C,
All Name , LLC,2018-01-21,SELL
D,World Name , LLC,2018-01-20,BUY";

            string bufferLine = null;
            var reader = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                .Setup(s => s.BeforeRecordLoad += (o, e) =>
                {
                    string line = (string)e.Source;
                    string[] tokens = line.Split(",");

                    if (tokens.Length == 5)
                    {
                        //Fix the second and third value with quotes
                        e.Source = @"{0},""{1},{2}"",{3}, {4}".FormatString(tokens[0], tokens[1], tokens[2], tokens[3], tokens[4]);
                    }
                    else
                    {
                        //Fix the breaking lines, assume that some csv lines broken into max 2 lines
                        if (bufferLine == null)
                        {
                            bufferLine = line;
                            e.Skip = true;
                        }
                        else
                        {
                            line = bufferLine + line;
                            tokens = line.Split(",");
                            e.Source = @"{0},""{1},{2}"",{3}, {4}".FormatString(tokens[0], tokens[1], tokens[2], tokens[3], tokens[4]);
                            line = null;
                        }
                    }
                });

            foreach (var rec in reader)
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void CSV2JSONWithEmptyArray()
        {
            string expected = @"[
  {
    ""text"": ""1"",
    ""intentName"": ""2"",
    ""entityLabels"": []
  },
  {
    ""text"": ""2"",
    ""intentName"": ""1"",
    ""entityLabels"": []
  }
]";
            //string expected = "[\r\n {\r\n  \"text\": \"1\",\r\n  \"intentName\": \"2\",\r\n  \"entityLabels\": null\r\n },\r\n {\r\n  \"text\": \"2\",\r\n  \"intentName\": \"1\",\r\n  \"entityLabels\": null\r\n }\r\n]";
            //string expected = "[\r\n {\r\n  \"text\": \"1\",\r\n  \"intentName\": \"2\",\r\n  \"entityLabels\": \"null\"\r\n },\r\n {\r\n  \"text\": \"2\",\r\n  \"intentName\": \"1\",\r\n  \"entityLabels\": \"null\"\r\n }\r\n]";

            string csv = @"text,intentName,entityLabels
        1,2,null
        2,1,null
";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithField("text")
                .WithField("intentName")
                .WithField("entityLabels", valueConverter: o => o.ToNString() == "null" ? new object[] { } : o)
                )

            {
                using (var w = new ChoJSONWriter(sb)
                    )
                    w.Write(p);
            }

            string actual = sb.ToString();

            Assert.AreEqual(expected, actual);
            // TODO: Check correct expected string
            // TODO: Change simple string compare to better JSON-object compare
        }


        public class AccountBalance
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public List<string> lastTwelveMonths { get; set; }

            public override bool Equals(object obj)
            {
                var balance = obj as AccountBalance;
                return balance != null &&
                       ID == balance.ID &&
                       Name == balance.Name &&
                       new ListEqualityComparer<string>().Equals(lastTwelveMonths, balance.lastTwelveMonths);
            }

            public override int GetHashCode()
            {
                var hashCode = -3043115;
                hashCode = hashCode * -1521134295 + ID.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<List<string>>.Default.GetHashCode(lastTwelveMonths);
                return hashCode;
            }
        }

        [Test]
        public static void NestedObjectIgnoreFirstLineHeaderTest()
        {
            // During changing to NUnit-Tests found error when using WithFirstLineHeader(true)

            string csv = @"AccountId, Name, Jan, Feb, Mar, Dec
1, Anne, 1000.00, 400.00, 500.00,200.00";
            List<AccountBalance> x;
            using (var p = ChoCSVReader<AccountBalance>.LoadText(csv)
                .WithFirstLineHeader(true)
                .WithField(m => m.lastTwelveMonths, valueSelector: v =>
                {
                    int d = v.Count;
                    Assert.AreNotEqual(d, 0, "This is a empty dynamic object.");
                    return null;
                }))
                x = p.ToList(); // Iterate
        }

        [Test]
        public static void NestedObjectTest()
        {
            AccountBalance[] expected = new AccountBalance[]
            {
                new AccountBalance { ID=1, Name="Anne", lastTwelveMonths = new List<string>{" 1000.00"," 400.00", " 500.00","200.00"} },
                new AccountBalance { ID=2, Name="John", lastTwelveMonths = new List<string>{" 900.00"," 500.00", " 500.00","1200.00"} },
                new AccountBalance { ID=3, Name="Brit", lastTwelveMonths = new List<string>{" 600.00"," 600.00", " 500.00","2200.00"} }
            };
            AccountBalance[] actual = null;

            string csv = @"ID, Name, Jan, Feb, Mar, Dec
1, Anne, 1000.00, 400.00, 500.00,200.00
2, John, 900.00, 500.00, 500.00,1200.00
3, Brit, 600.00, 600.00, 500.00,2200.00";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader<AccountBalance>.LoadText(csv)
                .WithFirstLineHeader(false)
                .ThrowAndStopOnMissingField(false)
                .WithField(m => m.lastTwelveMonths, valueSelector: v =>
                {
                    List<string> list = new List<string>();
                    //list.Add(v.Column5);
                    list.Add(v.Jan);
                    list.Add(v.Feb);
                    list.Add(v.Mar);
                    list.Add(v.Dec);
                    return list;
                })
                )
            {
                actual = p.ToArray();

                using (var w = new ChoCSVWriter<AccountBalance>(sb)
                    .WithFirstLineHeader()
                    .WithField(f => f.lastTwelveMonths, fieldName: "Mon,Tue", valueSelector: v =>
                    {
                        System.Collections.IList array = v.lastTwelveMonths as System.Collections.IList;
                        return new object[] { array[0], array[1] };
                    })
                    )
                {
                    w.Write(actual);
                }

                //foreach (var rec in p)
                //    Console.WriteLine(rec.Dump());
            }

            CollectionAssert.AreEqual(expected, actual);

            Console.WriteLine(sb.ToString());
        }

        [Test]
        public static void CaptureError()
        {

            List<ChoDynamicObject> expected = new List<ChoDynamicObject>() {
                new ChoDynamicObject { { "FirstName", "Jo\"hn\"" }, { "LastName", "Doe" } },
                new ChoDynamicObject { { "FirstName", "Jane" }, { "LastName", "Doe" } } };
            List<object> actual = new List<object>();
            List<object> expectedErrors = new List<object> { "JaneDoe" };
            List<object> actualErrors = new List<object>();

            string csv = @"FirstName, LastName
JaneDoe
Jo""hn"",Doe
Jane,Doe
";

            foreach (var rec in ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .Setup(s => s.RecordLoadError += (o, e) => { actualErrors.Add(e.Source); Console.WriteLine(e.Source.ToNString()); e.Handled = true; })
                )
                actual.Add(rec);
            //                Console.WriteLine(rec.Dump());
            CollectionAssert.AreEqual(expectedErrors, actualErrors);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void ColumnCountStrictTest()
        {
            string csv = @"FirstName,LastName,City
Jane,Doe,Edison
Tom,Mark,NewYork
";
            var parser = ChoCSVReader.LoadText(csv)
                .WithFields("FirstName", "LastName")
                .WithFirstLineHeader()
                .ColumnCountStrict();
            var enumerator = parser.GetEnumerator();
            Assert.Throws<ChoETL.ChoParserException>(() => enumerator.MoveNext());
            /*
            foreach (var rec in parser
                )
                Console.WriteLine(rec.Dump());
            */

        }

        [Test]
        public static void CSVTest1()
        {
            List<string> expected = new List<string>() { "Value3 a, Value 3b", "Value 5", "Value2 a, Value 2b", "Value 4", "Value 1" };
            List<object> actual = new List<object>();

            string csv = @"Header 1,Header 2,Header 3,Header 4,Header 5
Value 1,""Value2 a, Value 2b"",""Value3 a, Value 3b"",Value 4,Value 5";

            foreach (var rec in ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                )
            {
                actual.Add(rec["Header 3"]);
                actual.Add(rec["Header 5"]);
                actual.Add(rec["Header 2"]);
                actual.Add(rec["Header 4"]);
                actual.Add(rec["Header 1"]);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void OddTest1()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "AccountOwnerEmail", "v-dakash@catalysis.com" },
                { "PartnerName", @"""HEY""? Tester" },
                { "EnrollmentID", "12345789" },
                { "Customer", @"Catalysis" },
                { "LicensingProgram", "LLC." },
                { "Country", "Enterprise 6 TEST" },
                { "Culture", "etc" },
                { "Issue", "etc" }
                }
            };
            List<object> actual = new List<object>();

            string csv = @"AccountOwnerEmail,  PartnerName, EnrollmentID, Customer, LicensingProgram, Country, Culture, Issue
v-dakash@catalysis.com,""HEY""? Tester, 12345789,""Catalysis"", LLC., Enterprise 6 TEST, etc,etc";

            foreach (dynamic rec in ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                )
                actual.Add(rec);
            //            Console.WriteLine(rec.Dump());
            CollectionAssert.AreEqual(expected, actual);
        }

        public class EmployeeX
        {
            public string DepartmentPosition { get; set; }
            public string ParentDepartmentPosition { get; set; }
            public string JobTitle { get; set; }
            public string Role { get; set; }
            public string Location { get; set; }
            public string NameLocation { get; set; }
            public string EmployeeStatus { get; set; }

            public override bool Equals(object obj)
            {
                var x = obj as EmployeeX;
                return x != null &&
                       DepartmentPosition == x.DepartmentPosition &&
                       ParentDepartmentPosition == x.ParentDepartmentPosition &&
                       JobTitle == x.JobTitle &&
                       Role == x.Role &&
                       Location == x.Location &&
                       NameLocation == x.NameLocation &&
                       EmployeeStatus == x.EmployeeStatus;
            }

            public override int GetHashCode()
            {
                var hashCode = 351156953;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DepartmentPosition);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ParentDepartmentPosition);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(JobTitle);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Role);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Location);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(NameLocation);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EmployeeStatus);
                return hashCode;
            }
        }

        [Test]
        public static void TestX()
        {
            List<EmployeeX> expected = new List<EmployeeX>{
                new EmployeeX {
                DepartmentPosition = "50382018",
                ParentDepartmentPosition ="50319368" ,
                JobTitle = "eBusiness Manager" ,
                Role = "IT02" ,
                Location = "3350_FIB4",
                NameLocation = "IT" ,
                EmployeeStatus = "2480" },
                new EmployeeX {
                DepartmentPosition = "50370383",
                ParentDepartmentPosition ="50373053" ,
                JobTitle = "CRM Manager" ,
                Role = "IT01" ,
                Location = "3200_FIB3",
                NameLocation = "xyz" ,
                EmployeeStatus = "2480" },
                new EmployeeX {
                DepartmentPosition = "50320067",
                ParentDepartmentPosition ="50341107" ,
                JobTitle = "VP, Business Information Officer" ,
                Role = "IT03" ,
                Location = "3200_FI89",
                NameLocation = "xyz" ,
                EmployeeStatus = "2480" },
                new EmployeeX {
                DepartmentPosition = "50299061",
                ParentDepartmentPosition ="50350088" ,
                JobTitle = "Project Expert" ,
                Role = "IT02" ,
                Location = "8118_FI09",
                NameLocation = "abc" ,
                EmployeeStatus = "2480" }
            };
            List<object> actual = new List<object>();
            string csv = @"50382018,50319368,eBusiness Manager,IT02,3350_FIB4,IT,2480
50370383,50373053,CRM Manager,IT01,3200_FIB3,xyz,2480
50320067,50341107,""VP, Business Information Officer"",IT03,3200_FI89,xyz,2480
50299061,50350088,Project Expert, IT02,8118_FI09,abc,2480";

            foreach (var rec in ChoCSVReader<EmployeeX>.LoadText(csv)
                .MayHaveQuotedFields())
                actual.Add(rec);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Issue21()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Column1", "Col11" }, {"Column2","Col21"}, { "Column3","Col31" } },
                new ChoDynamicObject {{ "Column1", "Some1" }, {"Column2",null}, { "Column3","23213" } },
                new ChoDynamicObject {{ "Column1", "Some2" }, {"Column2",null}, { "Column3","234324" } }
            };
            List<object> actual = new List<object>();

            string csv = @"Col1|Col2|Col3
Col11|Col21|Col31
Some1||23213
Some2||234324";

            foreach (var rec in ChoCSVReader.LoadText(csv)
                .WithDelimiter("|")
                .WithFirstLineHeader(true)
                //.WithHeaderLineAt(2, false)
                )
                actual.Add(rec);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void MaxScanRowsAfterHeaderRowAt()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "ID", "1s" }, { "Name", "Raj" } },
                new ChoDynamicObject {{ "ID", "2" }, { "Name", "Mark"} },
                new ChoDynamicObject {{ "ID", "3" }, { "Name", "Tom"} }
            };
            List<object> actual = new List<object>();

            string csv = @"#sdfdsfsdfsdfdsf
#fdsfdsfsdfsdf
#sdfdsfdsfdsff
ID, Name
1s, Raj
2, Mark
3, Tom";

            foreach (var rec in ChoCSVReader.LoadText(csv)
                .WithHeaderLineAt(4)
                .WithMaxScanRows(2)
                )
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void RenameCol()
        {
            string expected = @"Test1,Test2
1,David
2,Bob";
            StringBuilder csvIn = new StringBuilder(@"ID,Name
1, David
2, Bob");

            StringBuilder csvOut = new StringBuilder();

            using (var r = new ChoCSVReader(csvIn)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoCSVWriter(csvOut)
                    .WithFirstLineHeader()
                    )
                    w.Write(r.Select(r1 => new { Test1 = r1.ID, Test2 = r1.Name }));
            }

            Assert.AreEqual(expected, csvOut.ToString());
        }

        [Test]
        public static void RenameCol1()
        {
            string expected = @"Test,Test2
1,David
2,Bob";

            StringBuilder csvIn = new StringBuilder(@"ID,Name
1, David
2, Bob");

            StringBuilder csvOut = new StringBuilder();

            using (var r = new ChoCSVReader(csvIn)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoCSVWriter(csvOut)
                    .WithFirstLineHeader()
                    .Setup(s => s.FileHeaderWrite += (o, e) =>
                    {
                        e.HeaderText = "Test,Test2";
                    })
                    )
                    w.Write(r);
            }

            Assert.AreEqual(expected, csvOut.ToString());
        }

        [Test]
        public static void ToList()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject>
            {
                new ChoDynamicObject{{"ID",(int)1}, { "Name", "David" }, { "Date", new DateTime(2018, 1, 1) } }
            };
            List<object> actual = new List<object>();

            StringBuilder csvIn = new StringBuilder(@"ID,Name,Date
1, David, 1/1/2018
2, Bob, 2/12/2019");

            using (var r = new ChoCSVReader(csvIn)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                )
            {
                foreach (IDictionary<string, object> rec in r.Take(1))
                {
                    actual.Add(rec);
                    //foreach (var kvp in rec)
                    //    Console.WriteLine($"{kvp.Key} - {r.Configuration[kvp.Key].FieldType}");
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void Tab1Test()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "CustomerId", (long)22160 }, { "CustomerName", "MANSFIELD BROTHERS HEATING & AIR" }, { "InvoiceId", "sss.001" }, { "PurchaseDate", new DateTime(2017,2,8) }, { "ProductId", (long)193792 }, { "PurchasedAmount", (double)69.374 }, { "PurchasedQuantity", (long)2 }, { "LocationId", (long)30 } },
                new ChoDynamicObject {{ "CustomerId", (long)27849 }, { "CustomerName", "OWSLEY SUPPLY LLC  - EQUIPMENT" },     { "InvoiceId", "sss.001" }, { "PurchaseDate", new DateTime(2017, 3,14) }, { "ProductId", (long)123906 }, { "PurchasedAmount", (double)70.409 }, { "PurchasedQuantity", (long)1 }, { "LocationId", (long)2 } },
                new ChoDynamicObject {{ "CustomerId", (long)27849 }, { "CustomerName", "OWSLEY SUPPLY LLC  - EQUIPMENT" },     { "InvoiceId", "sss.001" }, { "PurchaseDate", new DateTime(2017, 3, 14) }, { "ProductId", (long)40961 }, { "PurchasedAmount", (double)10 }, { "PurchasedQuantity", (long)1 }, { "LocationId", (long)2 } },
                new ChoDynamicObject {{ "CustomerId", (long)16794 }, { "CustomerName", "ALEXANDER GILMORE dba AL'S HEATING" }, { "InvoiceId", "sss.001" }, { "PurchaseDate", new DateTime(2017, 1, 25) }, { "ProductId", (long)116511 }, { "PurchasedAmount", (double)63.016 }, { "PurchasedQuantity", (long)1 }, { "LocationId", (long)15 } },
                new ChoDynamicObject {{ "CustomerId", (long)16794 }, { "CustomerName", "ALEXANDER GILMORE dba AL'S HEATING" }, { "InvoiceId", "sss.001" }, { "PurchaseDate", new DateTime(2017, 1, 25) }, { "ProductId", (long)116511 }, { "PurchasedAmount", (double)-63.016 }, { "PurchasedQuantity", (long)-1 }, { "LocationId", (long)15 } },
                new ChoDynamicObject {{ "CustomerId", (long)16794 }, { "CustomerName", "ALEXANDER GILMORE dba AL'S HEATING" }, { "InvoiceId", "sss.001" }, { "PurchaseDate", new DateTime(2017, 1, 25) }, { "ProductId", (long)122636 }, { "PurchasedAmount", (double)30.748 }, { "PurchasedQuantity", (long)1 }, { "LocationId", (long)15 } },
                new ChoDynamicObject {{ "CustomerId", (long)16794 }, { "CustomerName", "ALEXANDER GILMORE dba AL'S HEATING" }, { "InvoiceId", "sss.001" }, { "PurchaseDate", new DateTime(2017, 1, 25) }, { "ProductId", (long)137661 }, { "PurchasedAmount", (double)432.976 }, { "PurchasedQuantity", (long)1 }, { "LocationId", (long)15 } },
                new ChoDynamicObject {{ "CustomerId", (long)16794 }, { "CustomerName", "ALEXANDER GILMORE dba AL'S HEATING" }, { "InvoiceId", "sss.001" }, { "PurchaseDate", new DateTime(2017, 1, 25) }, { "ProductId", (long)137661 }, { "PurchasedAmount", (double)-432.976 }, { "PurchasedQuantity", (long)-1 }, { "LocationId", (long)15 } }
            };
            List<object> actual = new List<object>();

            ChoETLFrxBootstrap.Log = (s) => Trace.WriteLine(s);

            //            string csv = @"* Select	d  : 02:02:12 20 MAR 2017						
            //* Shippi	g Date >= 01/20/2017 ; Shipping Dat	<= 03/20/2017	; Shipping	Branch = 2	9,15,19,21,22,	5,26,27,2	,29,30,31,
            //********	***********************************	**************	**********	**********	**************	*********	**********

            //CUSTOMER	CUSTOMER NAME	INVOICE ID	PURCHASE	PRODUCT ID	PURCHASED	PURCHASED	LOCATION
            //ID			DATE		AMOUNT	QUANTITY	ID
            //22160	MANSFIELD BROTHERS HEATING & AIR	sss.001	02/08/2017	193792	69.374	2	30
            //27849	OWSLEY SUPPLY LLC  - EQUIPMENT	sss.001	03/14/2017	123906	70.409	1	2
            //27849	OWSLEY SUPPLY LLC  - EQUIPMENT	sss.001	03/14/2017	40961	10.000	1	2
            //16794	ALEXANDER GILMORE dba AL'S HEATING	sss.001	01/25/2017	116511	63.016	1	15
            //16794	ALEXANDER GILMORE dba AL'S HEATING	sss.001	01/25/2017	116511	-63.016	-1	15
            //16794	ALEXANDER GILMORE dba AL'S HEATING	sss.001	01/25/2017	122636	30.748	1	15
            //16794	ALEXANDER GILMORE dba AL'S HEATING	sss.001	01/25/2017	137661	432.976	1	15
            //16794	ALEXANDER GILMORE dba AL'S HEATING	sss.001	01/25/2017	137661	-432.976	-1	15";

            string csv = @"Meeting Summary
Total Number of Participants	4 
Meeting Title	Test 
Meeting Start Time	3/11/2021, 12:57:48 PM 
Meeting End Time	3/11/2021, 1:28:13 PM 

Full Name	Join Time	Leave Time	Duration	Email	Role 
Smith, John.	3/11/2021, 12:57:48 PM	3/11/2021, 1:28:08 PM	30m 20s	TEST39@test.com	Presenter 
Marshall, Micah D.	3/11/2021, 12:59:11 PM	3/11/2021, 1:28:07 PM	28m 56s	TEST18@test.com	Presenter 
Hugh, Grant V.	3/11/2021, 1:00:08 PM	3/11/2021, 1:28:13 PM	28m 5s	TEST5@test.com	Organizer 
Cole, Brad R.	3/11/2021, 1:27:03 PM 3/11/2021, 1:28:07 PM	1m 4s	TEST4@test.COM	Presenter";

            try
            {
                foreach (var rec in ChoCSVReader.LoadText(csv)
                    .HeaderLineAt(7)
                    .WithDelimiter("\t")
                    .ConfigureHeader(h => h.HasHeaderRecord = true)
                    )
                {
                    Console.WriteLine(rec["Full Name"]);
                    Console.WriteLine(rec["Join Time"]);
                }
                //CollectionAssert.AreEqual(expected, actual);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //public partial class EmployeeRec
        //{
        //    public int Id { get; set; }
        //    public string Name { get; set; }
        //}
        [Test]
        public static void SimpleCSVTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name", "Carl"}, { "Extra" , "1" } },
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Tom"}, { "Extra" , "1" } },
                new ChoDynamicObject {{ "Id", "3" }, { "Name", "Mark"}, { "Extra" , "1" } }
            };
            List<object> actual = new List<object>();

            string csv = @"Id, Name
1, Carl
2, Tom
3, Mark";

            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                foreach (var rec in p.Select(r =>
                {
                    r.Extra = "1";
                    return r;
                }))
                {
                    actual.Add(rec);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
            //using (var parser = new ChoCSVReader(blobFile.OpenRead())
            //    .Select(row =>
            //    {
            //        row.FileLogId = 0;
            //        return row;
            //    })
            //    .AsDataReader())
            //{
            //    bulkCopy.WriteToServer(parser);
            //}
        }

        public class EmpNull
        {
            public int? Id { get; set; }
            public string Name { get; set; }
        }

        [Test]
        public static void NullableColumnAsDataTable()
        {
            //            object[][] expected = new object[][] { new object[]{ (int)5, (string)"SFG" }, new object[]{ 1, "Tom" } };
            var expected = new DataTable();
            expected.Columns.Add(new DataColumn("Id", typeof(int)));
            expected.Columns.Add(new DataColumn("Name", typeof(string)));
            expected.Rows.Add(null, null);
            expected.Rows.Add(1, "Tom");
            expected.AcceptChanges();

            string csv = @"Id, Name
,
1, Tom";

            var dt = ChoCSVReader<EmpNull>.LoadText(csv)
                .WithFirstLineHeader()
                .Configure(c => c.NullValue = "")
                //.WithField("Id", fieldType: typeof(int?))
                //.WithField("Name", fieldType: typeof(string))
                .AsDataReader();

            var actual = dt.GetSchemaTable();

            DataTable actual2 = new DataTable();
            actual2.Load(dt);

            UnitTestHelper.DataTableAssert.AreEqual(expected, actual2);
        }



        //[Test]
        public static void LargeNoOfColumnsTest()
        {
            for (int i = 0; i < 5; i++)
            {
                Stopwatch sw = Stopwatch.StartNew();
                foreach (var rec in new ChoCSVReader(FileNameETLsampletestCSV))
                {
                    Console.WriteLine(rec.Column1);
                }
                sw.Stop();
                Console.WriteLine(sw.Elapsed.TotalSeconds);
            }

            Assert.Ignore();
        }

        [Test]
        public static void BoolIssue()
        {
            string csvIn = FileNameSampleDataCSV;

            using (var r = new ChoCSVReader(csvIn)
                .WithFirstLineHeader()
                .WithMaxScanRows(10)
                )
            {
                foreach (IDictionary<string, object> rec in r.Take(1))
                {
                    foreach (var kvp in rec)
                        Console.WriteLine($"{kvp.Key} - {r.Configuration[kvp.Key].FieldType}");
                }
            }
        }

        [Test]
        public static void BcpTest()
        {
            Assert.Ignore();
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDb;Initial Catalog=EFSample.SchoolContext;Integrated Security=True";

            string csv = @"TeacherId, TeacherName
1, Tom
2, Mark";

            using (var p = ChoCSVReader.LoadText(csv))
                p.Bcp(connectionString, "Teachers");
        }

        [Test]
        public static void ZipCodeBcpTest()
        {
            Assert.Ignore();
            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDb;Initial Catalog=EFSample.SchoolContext;Integrated Security=True";

            using (var r = new ChoCSVReader(FileNameZipCodesExCSV)
                .WithFirstLineHeader()
                .Configure(c => c.FieldValueTrimOption = ChoFieldValueTrimOption.None)
                )
            {
                //foreach (var rec in r.Take(10))
                //    Console.WriteLine(rec.Dump());

                r.Bcp(connectionString, "ZipCodes");
            }

        }

        [Test]
        public static void ZipCodeLoadTest()
        {
            Assert.Ignore();

            string connectionString = @"Data Source=(LocalDB)\MSSQLLocalDb;Initial Catalog=EFSample.SchoolContext;Integrated Security=True";
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("SELECT * FROM ZipCodes", conn))
                {
                    using (var w = new ChoCSVWriter(FileNameZipCodesExCSV)
                        .WithFirstLineHeader()
                        .QuoteAllFields()
                        )
                    {
                        w.Write(cmd.ExecuteReader());
                    }
                }
            }

            Assert.Ignore();
        }

        [Test]
        public static void Issue43()
        {
            List<object> expected = new List<object> {
                (int)4113, null
            };
            List<object> actual = new List<object>();

            //            var c2 = new ChoCSVReader("issue43.csv")
            //                .WithFirstLineHeader().WithMaxScanRows(1)
            //                .Configure(c => c.ErrorMode = ChoErrorMode.ReportAndContinue)
            //                .Count();
            //            Console.WriteLine(c2);
            //            return;

            foreach (var rec in new ChoCSVReader(FileNameIssue43CSV)
                .WithFirstLineHeader().WithMaxScanRows(1)
                .Configure(c => c.ErrorMode = ChoErrorMode.IgnoreAndContinue)
                //.Where(r => r.Emp_Nbr == "BACKFLSH")
                )
            {
                actual.Add(rec.Emp_Nbr);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void EscapeQuoteTest()
        {
            string expected = @"[
  {
    ""Field1"": ""Line 3 Field 1"",
    ""Field2"": null,
    ""Field3"": ""Line 3 Field 3\r\nx\""Line 4 Field 1\""\t\""\""\t\""Line 4 Field 3""
  }
]";
            using (var reader = new ChoCSVReader(FileNameQuoteEscapeCSV)
                .WithDelimiter("\t")
                .WithFirstLineHeader()
                .Configure(x =>
                {
                    //x.QuoteEscapeChar = '\0';
                    x.MayContainEOLInData = true;
                    x.MaxScanRows = 0;
                    x.IgnoreEmptyLine = true;
                    x.ThrowAndStopOnMissingField = false;
                    x.QuoteAllFields = true;
                }))
            {
                //var dataReader = reader.AsDataReader();

                //while (dataReader.Read())
                //{
                //    Console.WriteLine($"{dataReader[0]}, {dataReader[1]}, {dataReader[2]}");
                //}
                var recs = reader.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class FooBar
        {
            public int FooID { get; set; }
            public string FooProperty1 { get; set; }
            public List<Bar> Bars { get; set; }

            public override bool Equals(object obj)
            {
                var bar = obj as FooBar;
                return bar != null &&
                       FooID == bar.FooID &&
                       FooProperty1 == bar.FooProperty1 &&
                       new ListEqualityComparer<Bar>().Equals(Bars, bar.Bars);
            }

            public override int GetHashCode()
            {
                var hashCode = 1534047906;
                hashCode = hashCode * -1521134295 + FooID.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FooProperty1);
                hashCode = hashCode * -1521134295 + EqualityComparer<List<Bar>>.Default.GetHashCode(Bars);
                return hashCode;
            }
        }

        public class Bar
        {
            public int BarID { get; set; }
            public string BarProperty1 { get; set; }
            public string BarProperty2 { get; set; }
            public string BarProperty3 { get; set; }

            public override bool Equals(object obj)
            {
                var bar = obj as Bar;
                return bar != null &&
                       BarID == bar.BarID &&
                       BarProperty1 == bar.BarProperty1 &&
                       BarProperty2 == bar.BarProperty2 &&
                       BarProperty3 == bar.BarProperty3;
            }

            public override int GetHashCode()
            {
                var hashCode = 843256140;
                hashCode = hashCode * -1521134295 + BarID.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BarProperty1);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BarProperty2);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(BarProperty3);
                return hashCode;
            }
        }

        [Test]
        public static void NestedCSV1()
        {
            List<FooBar> expectedFromReader = new List<FooBar> {
                new FooBar { FooID = 1, FooProperty1 = "Prop1", Bars=new List<Bar> {
                      new Bar{ BarID=2, BarProperty1="BP21", BarProperty2 = "BP22", BarProperty3 = "BP23"},
                      new Bar{ BarID=3, BarProperty1="BP31", BarProperty2 = "BP32", BarProperty3 = "BP33"}
                    }
                }
            };
            List<object> actualFromReader = new List<object> { };
            string actualFromWriter = "";

            string csv = @"FooID,FooProperty1,BarID_1,BarProperty1_1,BarProperty2_1,BarProperty3_1,BarID_2,BarProperty1_2,BarProperty2_2,BarProperty3_2
1,Prop1,2,BP21,BP22,BP23,3,BP31,BP32,BP33";
            StringBuilder csvOut = new StringBuilder();

            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                .Configure(c => c.NestedKeySeparator = '_'))
            {
                using (var w = new ChoCSVWriter<FooBar>(csvOut)
                     .WithFirstLineHeader()
                     .WithField(r => r.Bars, headerSelector: () => "BarID_1,BarProperty1_1,BarProperty2_1,BarProperty3_1,BarID_2,BarProperty1_2,BarProperty2_2,BarProperty3_2",
                        valueSelector: (o) =>
                        {
                            var r = (FooBar)o;
                            return $"{r.Bars[0].BarID},{r.Bars[0].BarProperty1},{r.Bars[0].BarProperty2},{r.Bars[0].BarProperty3},{r.Bars[1].BarID},{r.Bars[1].BarProperty1},{r.Bars[1].BarProperty2},{r.Bars[1].BarProperty3}";
                        })
                )
                {
                    var recs = p.ToArray();
                    foreach (var rec in recs.Select(r =>
                    {
                        return new FooBar
                        {
                            FooID = ChoUtility.CastTo<int>(r.FooID),
                            FooProperty1 = ChoUtility.CastTo<string>(r.FooProperty1),
                            Bars = new List<Bar>
                            {
                                new Bar
                                {
                                    BarID = ChoUtility.CastTo<int>(r.BarID[0]),
                                    BarProperty1 = ChoUtility.CastTo<string>(r.BarProperty1[0]),
                                    BarProperty2 = ChoUtility.CastTo<string>(r.BarProperty2[0]),
                                    BarProperty3 = ChoUtility.CastTo<string>(r.BarProperty3[0]),
                                },
                                 new Bar
                                {
                                    BarID = ChoUtility.CastTo<int>(r.BarID[1]),
                                    BarProperty1 = ChoUtility.CastTo<string>(r.BarProperty1[1]),
                                    BarProperty2 = ChoUtility.CastTo<string>(r.BarProperty2[1]),
                                    BarProperty3 = ChoUtility.CastTo<string>(r.BarProperty3[1]),
                                },
                           }
                        };

                    }))
                    {
                        //Console.WriteLine(rec.Dump());
                        actualFromReader.Add(rec);
                        w.Write(rec);
                    }
                }
            }
            CollectionAssert.AreEqual(expectedFromReader, actualFromReader);


            Console.WriteLine(csvOut.ToString());
            actualFromWriter = csvOut.ToString();
            Assert.AreEqual(csv, actualFromWriter);

            /*            using (var p = ChoCSVReader<FooBar>.LoadText(csvOut.ToString())
                            .WithFirstLineHeader()
                            .WithField(r => r.Bars, valueConverter: (o) => new List<Bar>())
                            .ThrowAndStopOnMissingField(false)
                            .Configure(c=> c.NestedColumnSeparator = '_'))
                            actual = p.ToList();
                        CollectionAssert.AreEqual(expected, actual);
            */
            //StringBuilder csvOut = new StringBuilder();
            //using (var p = ChoCSVReader<FooBar>.LoadText(csv)
            //    .WithFirstLineHeader()
            //    .WithField(r => r.Bars, valueConverter: (o) => new List<Bar>())
            //    .ThrowAndStopOnMissingField(false)
            //    .Configure(c => c.NestedColumnSeparator = '_'))

            //{
            //    using (var w = new ChoCSVWriter<FooBar>(csvOut)
            //        .WithFirstLineHeader()
            //        .WithField(r => r.Bars, headerSelector: () => "col1, col2", valueSelector: (o) => "x,y")
            //        )
            //    {
            //        w.Write(p);
            //    }
            //}
            //Console.WriteLine(csvOut.ToString());
        }

        [Test]
        public static void DisposeOnForEach()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Column1", "Id" }, { "Column2", "Name"}},
                new ChoDynamicObject {{ "Column1", "4" }, { "Column2", "1234,abc" } },
                new ChoDynamicObject {{ "Column1", "1" }, { "Column2", "15\\\""}},
                new ChoDynamicObject {{ "Column1", "2" }, { "Column2", @"Gom\" } },
                new ChoDynamicObject {{ "Column1", "3" }, { "Column2", "a"}}
            };
            List<object> actual = new List<object>();
            foreach (var rec in new ChoCSVReader(FileNameEmpCSV)
                .MayHaveQuotedFields())
                actual.Add(rec);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void GetRecordsAsDictionaryTest()
        {
            List<string> expected = new List<string> {
                "Column1", "Id","Column2", "Name",
                "Column1", "4","Column2", "1234,abc",
                "Column1", "1","Column2", "15\\\"",
                "Column1", "2","Column2", "Gom\\",
                "Column1", "3","Column2", "a"};
            List<object> actual = new List<object>();
            foreach (var rec in new ChoCSVReader(FileNameEmpCSV)
                .MayHaveQuotedFields()
                )
            {
                foreach (var kvp in rec)
                {
                    actual.Add(kvp.Key);
                    actual.Add(kvp.Value);
                    Console.WriteLine(kvp.Key);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void SpaceColumnsTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "First Name","Tom"}, { "Last Name", "Smith"} },
                new ChoDynamicObject {{ "First Name","Mark"}, { "Last Name", "Hartigan"} }
            };
            List<object> actual = new List<object>();

            string csv = @"First Name, Last Name
Tom, Smith
Mark, Hartigan";

            foreach (var rec in ChoCSVReader.LoadText(csv).WithFirstLineHeader().OfType<IDictionary<string, object>>())
                actual.Add(rec);

            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void UnicodeTest()
        {
            string expected = @"[
  {
    ""Endereço_4"": ""1"",
    ""Endereço_5"": ""11""
  },
  {
    ""Endereço_4"": ""2"",
    ""Endereço_5"": ""22""
  }
]";

            string csv = @"Endereço_4, Endereço_5
1, 11
2, 22";
            StringBuilder output = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader())
            {
                using (var w = new ChoJSONWriter(output))
                    w.Write(r);
            }

            string actual = output.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void DuplicateFields()
        {
            StringBuilder csvIn = new StringBuilder(@"ID,Name,Name
1, David, 1/1/2018
2, Bob, 2/12/2019");

            using (var r = new ChoCSVReader(csvIn)
                .WithFirstLineHeader()
                )
            {
                var enumerator = r.GetEnumerator();
                Assert.Throws<ChoRecordConfigurationException>(() => enumerator.MoveNext());
            }
        }

        [Test]
        public static void SolarTemp()
        {
            string expected = @"[
  {
    ""Year"": null,
    ""Month"": null,
    ""Hh"": null
  },
  {
    ""Year"": ""2005"",
    ""Month"": ""Jan"",
    ""Hh"": ""0""
  }
]";

            List<object> actual = new List<object>();
            using (var r = new ChoCSVReader(FileNameSolarTempCSV)
                .WithDelimiter("\t\t")
                .MayHaveQuotedFields()
                .WithHeaderLineAt(6)
                .ThrowAndStopOnMissingField(false)
                .Configure(c => c.IgnoreEmptyLine = false)
                //.Configure(c => c.AllowReturnPartialLoadedRecs = true)
                //.Setup(s => s.RecordLoadError += (o, e) => e.Handled = true)
                )
            {
                foreach (var rec in r)
                    actual.Add(rec);
            }
            Assert.AreEqual(actual.Count, 136);
            var payload = JsonConvert.SerializeObject(actual.Take(2), Formatting.Indented);
            Assert.AreEqual(expected, payload);
        }

        [Test]
        public static void SolarTemp_1()
        {
            string expected = @"[
  {
    ""Year"": ""2005"",
    ""Month"": ""Jan"",
    ""Hh"": ""0""
  },
  {
    ""Year"": ""2005"",
    ""Month"": ""Feb"",
    ""Hh"": ""0""
  }
]";

            List<object> actual = new List<object>();
            using (var r = new ChoCSVReader(FileNameSolarTempCSV)
                .WithDelimiter("\t\t")
                .MayHaveQuotedFields()
                .WithHeaderLineAt(6)
                .ThrowAndStopOnMissingField(false)
                .Configure(c => c.IgnoreEmptyLine = true)
                //.Configure(c => c.AllowReturnPartialLoadedRecs = true)
                //.Setup(s => s.RecordLoadError += (o, e) => e.Handled = true)
                )
            {
                foreach (var rec in r)
                    actual.Add(rec);
            }
            Assert.AreEqual(actual.Count, 134);
            var payload = JsonConvert.SerializeObject(actual.Take(2), Formatting.Indented);
            Assert.AreEqual(expected, payload);
        }

        public class PlantType
        {
            public string Plant { get; set; }
            public int Material { get; set; }
            public double Density { get; set; }
            public int StorageLocation { get; set; }

            public override bool Equals(object obj)
            {
                var type = obj as PlantType;
                return type != null &&
                       Plant == type.Plant &&
                       Material == type.Material &&
                       Density == type.Density &&
                       StorageLocation == type.StorageLocation;
            }

            public override int GetHashCode()
            {
                var hashCode = -2100988798;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Plant);
                hashCode = hashCode * -1521134295 + Material.GetHashCode();
                hashCode = hashCode * -1521134295 + Density.GetHashCode();
                hashCode = hashCode * -1521134295 + StorageLocation.GetHashCode();
                return hashCode;
            }
        }

        [Test]
        public static void Sample7Test()
        {
            List<PlantType> expected = new List<PlantType> {
                new PlantType { Plant = "FRED", Material = 10000477, Density = 64.3008, StorageLocation = 3300 },
                new PlantType { Plant = "FRED", Material = 10000479, Density = 62.612, StorageLocation = 3275 },
                new PlantType { Plant = "FRED", Material = 10000517, Density = 90, StorageLocation = 3550 },
                new PlantType { Plant = "FRED", Material = 10000517, Density = 72, StorageLocation = 3550},
                new PlantType { Plant = "FRED", Material = 10000532, Density = 90, StorageLocation = 3550 },
                new PlantType { Plant = "FRED", Material = 10000532, Density = 72, StorageLocation = 3550 },
                new PlantType { Plant = "FRED", Material = 10000550, Density = 97, StorageLocation = 3050 }
            };
            List<object> actual = new List<object>();

            using (var p = new ChoCSVReader<PlantType>(FileNameSample7CSV)
                .WithFirstLineHeader(true)
                )
            {
                foreach (var rec in p)
                {
                    actual.Add(rec);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }


        public class StudentInfo
        {
            public string Id { get; set; }
            //[DisplayName("Std")]
            public Student Student { get; set; }
            //[Range(1, 2)]
            [Range(0, 1)]
            public Course[] Courses { get; set; }

            [ChoDictionaryKey("K1,K2,K3")]
            public Dictionary<string, string> Grades { get; set; }
            [Range(2, 3)]
            //[Range(1, 2)]
            [DisplayName("Sub")]
            public string[] Subjects { get; set; }
            //[DisplayName("Teach")]
            public Teacher Teacher { get; set; }
            [Range(0, 1)]
            [DisplayName("Prof")]
            public List<string> Profs { get; set; }

            public StudentInfo()
            {
                Courses = new Course[3];
                Grades = new Dictionary<string, string>();
                Subjects = new string[5];
                //Profs = new List<string>();
            }

            public override bool Equals(object obj)
            {
                var info = obj as StudentInfo;
                return info != null &&
                       Id == info.Id &&
                       EqualityComparer<Student>.Default.Equals(Student, info.Student) &&
                       new ArrayEqualityComparer<Course>().Equals(Courses, info.Courses) &&
                       new DictionaryEqualityComparer<string, string>().Equals(Grades, info.Grades) &&
                       new ArrayEqualityComparer<string>().Equals(Subjects, info.Subjects) &&
                       EqualityComparer<Teacher>.Default.Equals(Teacher, info.Teacher) &&
                       new ListEqualityComparer<string>().Equals(Profs, info.Profs);
            }

            public override int GetHashCode()
            {
                var hashCode = -1412250211;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
                hashCode = hashCode * -1521134295 + EqualityComparer<Student>.Default.GetHashCode(Student);
                hashCode = hashCode * -1521134295 + new ArrayEqualityComparer<Course>().GetHashCode(Courses);
                hashCode = hashCode * -1521134295 + new DictionaryEqualityComparer<string, string>().GetHashCode(Grades);
                hashCode = hashCode * -1521134295 + new ArrayEqualityComparer<string>().GetHashCode(Subjects);
                hashCode = hashCode * -1521134295 + EqualityComparer<Teacher>.Default.GetHashCode(Teacher);
                hashCode = hashCode * -1521134295 + new ListEqualityComparer<string>().GetHashCode(Profs);
                return hashCode;
            }
        }

        public class Student
        {
            //[ChoFieldPosition(2)]
            [DisplayName("Name")]
            public string Name { get; set; }

            public Address Address { get; set; }

            public override bool Equals(object obj)
            {
                var student = obj as Student;
                return student != null &&
                       Name == student.Name &&
                       EqualityComparer<Address>.Default.Equals(Address, student.Address);
            }

            public override int GetHashCode()
            {
                var hashCode = -1876505879;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<Address>.Default.GetHashCode(Address);
                return hashCode;
            }
        }

        public class Address
        {
            [DisplayName("Street")]
            public string Street { get; set; }
            [DisplayName("City")]
            public string City { get; set; }

            public override bool Equals(object obj)
            {
                var address = obj as Address;
                return address != null &&
                       Street == address.Street &&
                       City == address.City;
            }

            public override int GetHashCode()
            {
                var hashCode = -1577962384;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Street);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(City);
                return hashCode;
            }
        }

        public class Teacher
        {
            public string Id { get; set; }
            [DisplayName("TeachName")]
            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                var teacher = obj as Teacher;
                return teacher != null &&
                       Id == teacher.Id &&
                       Name == teacher.Name;
            }

            public override int GetHashCode()
            {
                var hashCode = -1919740922;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                return hashCode;
            }
        }

        public class Course
        {
            [DisplayName("CreId")]
            public string CourseId { get; set; }
            [DisplayName("CreName")]
            public string CourseName { get; set; }

            public override bool Equals(object obj)
            {
                var course = obj as Course;
                return course != null &&
                       CourseId == course.CourseId &&
                       CourseName == course.CourseName;
            }

            public override int GetHashCode()
            {
                var hashCode = -1457526968;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CourseId);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CourseName);
                return hashCode;
            }
        }

        [Test]
        public static void CSV2ComplexObj()
        {
            List<StudentInfo> expected = new List<StudentInfo>() {
                new StudentInfo { Id = "1", Student = new Student { Name = "Tom", Address = new Address { Street = "St1", City = "New York" } }, Courses = new Course[3] { null, new Course { CourseId = "CI0", CourseName = "CN0" }, new Course { CourseId = "CI1", CourseName = "CN1" } }, Subjects = new string[5] { null, "S1", "S2", "S3", null }, Teacher = new Teacher{ Id = "TId1", Name = "TName1" } , Profs = new List<string> { "P0", "P1" } },
                new StudentInfo { Id = "2", Student = new Student { Name = "Mark", Address = new Address { Street = "St1", City = "Boston" } }, Courses = new Course[3] { null, new Course { CourseId = "CI20", CourseName = "CN20" }, new Course { CourseId = "CI21", CourseName = "CN21" } }, Subjects = new string[5] { null, "S21", "S22", "S23", null }, Teacher = new Teacher{ Id = "TId21", Name = "TName21" } , Profs = new List<string> { "P20", "P21" } }
            };
            expected[0].Grades.Add("K1", "K1");
            expected[0].Grades.Add("K2", "K2");
            expected[0].Grades.Add("K3", "K3");
            expected[1].Grades.Add("K1", "K21");
            expected[1].Grades.Add("K2", "K22");
            expected[1].Grades.Add("K3", "K23");
            List<StudentInfo> actual = new List<StudentInfo>();

            string csv = @"Id, Name, Street, City, CreId_1, CreName_1, CreId_2, CreName_2, K1,K2,K3,Sub_1,Sub_2,Sub_3,Sub_4,Prof_0,Prof_1,Teach.Id,TeachName
1, Tom, St1, New York, CI0, CN0, CI1, CN1, K1, K2, K3, S1, S2, S3, S4, P0, P1, TId1, TName1
2, Mark, St1, Boston, CI20, CN20, CI21, CN21, K21, K22, K23, S21, S22, S23, S24,P20, P21, TId21, TName21
";
            //                   csv = @"Id, Name, Street, City, CreId_1, CreName_1, CreId_2, CreName_2, K1,K2,K3,Sub_0,Sub_1,Sub_2,Sub_3,Prof_0,Prof_1,Teach.Id,TeachName
            //1, Tom, St1, New York, CI0, CN0, CI1, CN1, K1, K2, K3, S1, S2, S3, S4, P0, P1, TId1, TName1
            //2, Mark, St1, Boston, CI20, CN20, CI21, CN21, K21, K22, K23, S21, S22, S23, S24,P20, P21, TId21, TName21
            //";

            var config = new ChoCSVRecordConfiguration<StudentInfo>()
            .Map("Student.Name")
            .Map("Student.Address.Street")
            .Map("Student.Address.City")
            .Map("Id")
            .IndexMap("Courses", typeof(Course[]), 1, 2, m =>
            {
                if (m.Value.FieldName == "CourseId")
                    m.FieldName("CreId");
                else if (m.Value.FieldName == "CourseName")
                    m.FieldName("CreName");
            })
            .DictionaryMap("Grades", typeof(Dictionary<string, string>), new string[] { "K1", "K2", "K3" })
            .IndexMap("Subjects", typeof(string[]), 1, 3, m => m.FieldName("Sub"))
            .Map("Teacher.Id", m => m.FieldName("Teach.Id"))
            .Map("Teacher.Name", m => m.FieldName("TeachName"))
            .IndexMap("Profs", typeof(string[]), 0, 1, m => m.FieldName("Prof"))

            //.Map(f => f.Student.Name)
            //.Map(f => f.Id)
            //.Map(f => f.Student.Address.Street)
            //.Map(f => f.Student.Address.City)
            //.IndexMap(f => f.Courses, 1, 2, m =>
            //{
            //    if (m.Value.FieldName == "CourseId")
            //        m.FieldName("CreId");
            //    else if (m.Value.FieldName == "CourseName")
            //        m.FieldName("CreName");
            //})
            //.DictionaryMap(f => f.Grades, new string[] { "K1", "K2" })
            //.IndexMap(f => f.Subjects, 1, 3, m => m.FieldName("Sub"))
            //.Map(f => f.Teacher.Id, m => m.FieldName("Teach.Id"))
            //.Map(f => f.Teacher.Name, m => m.FieldName("TeachName"))
            //.IndexMap(f => f.Profs, 0, 1, m => m.FieldName("Prof"))
            ;
            using (var r = ChoCSVReader<StudentInfo>.LoadText(csv, configuration: config)
                //.Index(c => c.Courses, 1, 2)
                .WithFirstLineHeader()
                )
            {
                foreach (var rec in r)
                {
                    actual.Add(rec);
                    //Console.WriteLine(rec.Dump());
                }
            }

            expected.Print();
            actual.Print();
            CollectionAssert.AreEqual(expected, actual);
        }

        public class Headers
        {
            public string TransactionFrom { get; set; }
            public string TransactionTo { get; set; }
            [ChoIgnoreMember]
            public List<Transaction1> Transactions { get; set; }

            public override bool Equals(object obj)
            {
                var headers = obj as Headers;
                return headers != null &&
                       TransactionFrom == headers.TransactionFrom &&
                       TransactionTo == headers.TransactionTo &&
                       new ListEqualityComparer<Transaction1>().Equals(Transactions, headers.Transactions);
            }

            public override int GetHashCode()
            {
                var hashCode = -1815474691;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TransactionFrom);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TransactionTo);
                hashCode = hashCode * -1521134295 + new ListEqualityComparer<Transaction1>().GetHashCode(Transactions);
                return hashCode;
            }
        }

        public class Transaction1
        {
            [ChoFieldPosition(4)]
            public string logisticCode { get; set; }
            [ChoFieldPosition(5)]
            public string siteId { get; set; }
            [ChoFieldPosition(6)]
            public string userId { get; set; }
            [ChoFieldPosition(2)]
            public string dateOfTransaction { get; set; }
            [ChoFieldPosition(1)]
            public string price { get; set; }

            public override bool Equals(object obj)
            {
                var transaction = obj as Transaction1;
                return transaction != null &&
                       logisticCode == transaction.logisticCode &&
                       siteId == transaction.siteId &&
                       userId == transaction.userId &&
                       dateOfTransaction == transaction.dateOfTransaction &&
                       price == transaction.price;
            }

            public override int GetHashCode()
            {
                var hashCode = -1097465422;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(logisticCode);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(siteId);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(userId);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(dateOfTransaction);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(price);
                return hashCode;
            }
        }
        [Test]
        public static void MultiRecordTypeTest1()
        {
            string csv = @"2019-12-01T00:00:00.000Z;2019-12-10T23:59:59.999Z
50;false;2019-12-03T15:00:12.077Z;005033971003;48;141;2019-12-03T00:00:00.000Z;2019-12-03T23:59:59.999Z
100;false;2019-12-02T12:38:05.989Z;005740784001;80;311;2019-12-02T00:00:00.000Z;2019-12-02T23:59:59.999Z";

            string expected = @"[
  {
    ""TransactionFrom"": ""2019-12-01T00:00:00.000Z"",
    ""TransactionTo"": ""2019-12-10T23:59:59.999Z"",
    ""Transactions"": [
      {
        ""logisticCode"": ""005033971003"",
        ""siteId"": ""48"",
        ""userId"": ""141"",
        ""dateOfTransaction"": ""false"",
        ""price"": ""50""
      },
      {
        ""logisticCode"": ""005740784001"",
        ""siteId"": ""80"",
        ""userId"": ""311"",
        ""dateOfTransaction"": ""false"",
        ""price"": ""100""
      }
    ]
  }
]";
            StringBuilder json = new StringBuilder();
            string csvSeparator = ";";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithDelimiter(csvSeparator)
                .WithCustomRecordSelector(o =>
                {
                    string line = ((Tuple<long, string>)o).Item2;

                    if (line.SplitNTrim(csvSeparator).Length == 2)
                        return typeof(Headers);
                    else
                        return typeof(Transaction1);
                })
                )
            {
                using (var w = new ChoJSONWriter<Headers>(json)
                    .WithField(f => f.TransactionFrom)
                    .WithField(f => f.TransactionTo)
                    .WithField(f => f.Transactions)
                    )
                {
                    w.Write(r.GroupWhile(r1 => r1.GetType() != typeof(Headers))
                        .Select(g =>
                        {
                            Headers master = (Headers)g.First();
                            master.Transactions = g.Skip(1).Cast<Transaction1>().ToList();
                            return master;
                        }));
                }
            }

            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void MultiRecordTypeTest()
        {
            List<object> expected = new List<object> {
                new Headers { TransactionFrom = "2019-12-01T00:00:00.000Z", TransactionTo = "2019-12-10T23:59:59.999Z", Transactions = new List<Transaction1>() {
                    new Transaction1 { logisticCode = "005033971003", siteId = "48", userId = "141", dateOfTransaction = "false", price = "50" },
                    new Transaction1 { logisticCode = "005740784001", siteId = "80", userId = "311", dateOfTransaction = "false", price = "100" } } },
                //new Headers { TransactionFrom = "2019-12-01T00:00:00.000Z", TransactionTo = "2019-12-10T23:59:59.999Z", Transactions = new List<Transaction1>() {
                //    new Transaction1 { logisticCode = "005033971003", siteId = "48", userId = "141", dateOfTransaction = "false", price = "50" },
                //    new Transaction1 { logisticCode = "005740784001", siteId = "80", userId = "311", dateOfTransaction = "false", price = "100" } } }
            };
            List<Headers> actual = null;

            string csv = @"2019-12-01T00:00:00.000Z;2019-12-10T23:59:59.999Z
50;false;2019-12-03T15:00:12.077Z;005033971003;48;141;2019-12-03T00:00:00.000Z;2019-12-03T23:59:59.999Z
100;false;2019-12-02T12:38:05.989Z;005740784001;80;311;2019-12-02T00:00:00.000Z;2019-12-02T23:59:59.999Z";

            string csvSeparator = ";";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithDelimiter(csvSeparator)
                .WithCustomRecordSelector(o =>
                {
                    string line = ((Tuple<long, string>)o).Item2;

                    if (line.SplitNTrim(csvSeparator).Length == 2)
                        return typeof(Headers);
                    else
                        return typeof(Transaction1);
                })
                )
            {
                actual = r.GroupWhile(r1 => r1.GetType() != typeof(Headers))
                    .Select(g =>
                    {
                        Headers master = (Headers)g.First();
                        master.Transactions = g.Skip(1).Cast<Transaction1>().ToList();
                        return master;
                    }).ToList();
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        [Test]
        public static void JSON2CSVWithSpecialCharsInHeader()
        {
            string csv = @"Id.x, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                //.WithMaxScanRows(2)
                )
            {
                p.RecordFieldTypeAssessment += (o, e) =>
                {
                    e.FieldTypes["Id.x"] = typeof(short);
                };

                p.MembersDiscovered += (o, e) =>
                {
                    var ft = e.Value; // p.Configuration.CSVRecordFieldConfigurations.Select(f => f.FieldType).ToList();
                    ft["Id.x"] = typeof(short);
                    ft["Name"] = typeof(string);
                    ft["City"] = typeof(string);
                };

                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
                //using (var w = new ChoJSONWriter(sb))
                //    w.Write(p);
            }

            Console.WriteLine(sb.ToString());
        }


        public class StudentInfoMap
        {
            public string Id1 { get; set; }
            public string Name1 { get; set; }
            [Range(0, 1)]
            public Course1[] Courses { get; set; }
        }

        public class StudentInfo1
        {
            public string Id { get; set; }
            public string Name { get; set; }
            [Range(0, 1)]
            public Course1[] Courses { get; set; }

            [DisplayName("Grade")]
            [Range(1, 3)]
            public List<string> Grades { get; set; }

            public StudentInfo1()
            {
                Courses = new Course1[2];
            }
        }
        public class Course1
        {
            [DisplayName("CreId")]
            public string CourseId { get; set; }
            [DisplayName("CreName")]
            public string CourseName { get; set; }
        }

        [Test]
        public static void CSV2ComplexObject()
        {
            string csv = @"Id, Name, CreId_0, CreName_0, CreId_1, CreName_1,Grade_1,Grade_2,Grade_3
1, Tom, CI0, CN0, CI1, CN1,A,B,C
2, Mark, CI20, CN20, CI21, CN21,A,B,C
";

            string expected = @"[
  {
    ""Id"": ""1"",
    ""Name"": ""Tom"",
    ""Courses"": [
      {
        ""CourseId"": ""CI0"",
        ""CourseName"": ""CN0""
      },
      {
        ""CourseId"": ""CI1"",
        ""CourseName"": ""CN1""
      }
    ],
    ""Grades"": [
      ""A"",
      ""B""
    ]
  },
  {
    ""Id"": ""2"",
    ""Name"": ""Mark"",
    ""Courses"": [
      {
        ""CourseId"": ""CI20"",
        ""CourseName"": ""CN20""
      },
      {
        ""CourseId"": ""CI21"",
        ""CourseName"": ""CN21""
      }
    ],
    ""Grades"": [
      ""A"",
      ""B""
    ]
  }
]";
            var config = new ChoCSVRecordConfiguration<StudentInfo1>()
                .Map(f => f.Grades, "Grade")
                .Map(f => f.Id)
                .IndexMap(f => f.Courses, 0, 1)
                .IndexMap(f => f.Grades, 1, 3, m => m.FieldName("Grade"))
                .MapForType<Course1>(f => f.CourseId, "CreId")
                .MapForType<Course1>(f => f.CourseName, "CreName")
                .WithFirstLineHeader()
                ;

            config.CSVRecordFieldConfigurations.Select(f => f.Name).ToArray().Print();

            using (var r = ChoCSVReader<StudentInfo1>.LoadText(csv)
                .WithField(o => o.Id)
                //.WithField(o => o.Courses.FirstOrDefault().CourseId, fieldName: "CreId")
                .WithFieldForType<Course1>(o => o.CourseId, fieldName: "CreId")
                .WithFieldForType<Course1>(o => o.CourseName, fieldName: "CreName")
                .Index(o => o.Courses, 0, 1)
                .Index(f => f.Grades, 1, 2, m => m.FieldName("Grade"))
                .WithField(f => f.Grades, fieldName: "Grade")
                .WithFirstLineHeader()
                //.MapRecordFields<StudentInfoMap>()
                )
            {
                r.Configuration.CSVRecordFieldConfigurations.Select(f => f.Name).ToArray().Print();

                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
                //foreach (var rec in r)
                //{
                //    Console.WriteLine(rec.Dump());
                //}
            }
        }

        public class StudentInfo2
        {
            public string Id { get; set; }
            public string Name { get; set; }
            [ChoDictionaryKey("K1, K2")]
            public Dictionary<string, string> Grades { get; set; } = new Dictionary<string, string>();
        }
        [Test]
        public static void CSV2DictionaryMemberTest()
        {
            string csv = @"Id, Name, K1, K2
1, Tom, A, B
2, Mark, C, D";
            string expected = @"[
  {
    ""Id"": ""1"",
    ""Name"": ""Tom"",
    ""Grades"": {
      ""K1"": ""A"",
      ""K2"": ""B""
    }
  },
  {
    ""Id"": ""2"",
    ""Name"": ""Mark"",
    ""Grades"": {
      ""K1"": ""C"",
      ""K2"": ""D""
    }
  }
]";
            var config = new ChoCSVRecordConfiguration<StudentInfo2>()
                .Map(f => f.Id)
                .Map(f => f.Name)
                .DictionaryMap(f => f.Grades, new string[] { "K1", "K2" })
                //.WithFirstLineHeader()
                ;

            using (var r = ChoCSVReader<StudentInfo2>.LoadText(csv, config)
                //.WithField(o => o.Id)
                ////.WithField(o => o.Courses.FirstOrDefault().CourseId, fieldName: "CreId")
                //.WithFieldForType<Course1>(o => o.CourseId, fieldName: "CreId")
                //.WithFieldForType<Course1>(o => o.CourseName, fieldName: "CreName")
                //.Index(o => o.Courses, 0, 1)
                //.Index(f => f.Grades, 0, 1)
                //.WithField(f => f.Grades, fieldName: "Grade")
                //.WithFirstLineHeader()
                //.MapRecordFields<StudentInfoMap>()
                )
            {
                //foreach (var rec in r)
                //{
                //    Console.WriteLine(rec.Dump());
                //}
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void ReadAndCloseTest()
        {
            var r = new ChoCSVReader("sample1.csv").WithFirstLineHeader();
            r.MayHaveQuotedFields();

            dynamic rec = null;

            while ((rec = r.Read()) != null)
            {
                Console.WriteLine($"{r.Context.Headers[0]}: {rec[0]}");
                Console.WriteLine($"{r.Context.Headers[1]}: {rec[1]}");
                Console.WriteLine($"{r.Context.Headers[2]}: {rec[2]}");

                Assert.AreEqual(r.Context.Headers[0], "ID");
                Assert.AreEqual(r.Context.Headers[1], "FirstName");
                Assert.AreEqual(r.Context.Headers[2], "LastName");

                Assert.AreEqual(rec[0], "1");
                Assert.AreEqual(rec[1], "Smith");
                Assert.AreEqual(rec[2], "Moore");
            }
        }
        [Test]
        public static void CSVArrayToJSON()
        {
            string csv = @"Id,name,nestedobject/id,nestedarray/0/name, nestedarray/0/city, nestedarray/1/name, nestedarray/200/city
1,name,2,namelist10, citylist10,namelist11, citylist11
2,name1,3,namelist20, citylist20,namelist21, citylist21";

            string expected = @"{
  {
    ""Id"": ""1"",
    ""name"": ""name"",
    ""nestedobject"": {
      ""id"": ""2""
    },
    ""nestedarray"": [
      {
        ""name"": ""namelist10"",
        ""city"": ""citylist10""
      },
      {
        ""name"": ""namelist11""
      }
    ]
  },
  {
    ""Id"": ""2"",
    ""name"": ""name1"",
    ""nestedobject"": {
      ""id"": ""3""
    },
    ""nestedarray"": [
      {
        ""name"": ""namelist20"",
        ""city"": ""citylist20""
      },
      {
        ""name"": ""namelist21""
      }
    ]
  }
}";
            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter(json)
                .Configure(c => c.SupportMultipleContent = true)
                )
            {
                using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                    .Configure(c => c.NestedKeySeparator = '/')
                    .Configure(c => c.ArrayValueNamePrefix = String.Empty)
                    )
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void CSVArrayToXml()
        {
            string csv = @"Id,name,nestedobject/id,nestedarray/0/name, nestedarray/0/city, nestedarray/1/name, nestedarray/200/city
1,name,2,namelist10, citylist10,namelist11, citylist11
2,name1,3,namelist20, citylist20,namelist21, citylist21";

            string expected = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <XElement>
    <Id>1</Id>
    <name>name</name>
    <nestedobject>
      <id>2</id>
    </nestedobject>
    <nestedarray>
      <name>namelist10</name>
      <city>citylist10</city>
    </nestedarray>
    <nestedarray>
      <name>namelist11</name>
    </nestedarray>
  </XElement>
  <XElement>
    <Id>2</Id>
    <name>name1</name>
    <nestedobject>
      <id>3</id>
    </nestedobject>
    <nestedarray>
      <name>namelist20</name>
      <city>citylist20</city>
    </nestedarray>
    <nestedarray>
      <name>namelist21</name>
    </nestedarray>
  </XElement>
</Root>";
            StringBuilder xml = new StringBuilder();
            using (var w = new ChoXmlWriter(xml))
            {
                using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                    .Configure(c => c.NestedKeySeparator = '/')
                    )
                    w.Write(r);
            }
            Console.WriteLine(xml.ToString());

            var actual = xml.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void TestNestedProperty()
        {
            string csv = @"Id,name,nestedobject/id,nestedarray/0/name, nestedarray/0/city, nestedarray/1/name, nestedarray/200/city
1,name,2,namelist10, citylist10,namelist11, citylist11
2,name1,3,namelist20, citylist20,namelist21, citylist21";

            using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                .Configure(c => c.NestedKeySeparator = '/')
                )
            {
                var rec = r.FirstOrDefault();
                Assert.AreEqual(rec.GetNestedPropertyValue("nestedarray.0.city"), "citylist10");
                rec.SetNestedPropertyValue("nestedarray.0.city", "New York");
                Assert.AreEqual(rec.GetNestedPropertyValue("nestedarray.0.city"), "New York");
                //foreach (var rec in r)
                //{
                //    Console.WriteLine($"{rec.GetNestedPropertyValue("nestedarray.0.city")}");
                //    rec.SetNestedPropertyValue("nestedarray.0.city", "New York");
                //    Console.WriteLine($"{rec.GetNestedPropertyValue("nestedarray.0.city")}");
                //}
            }
        }
        public class Names
        {
            public string Name { get; set; }
            public List<int> NumbersA { get; set; } = new List<int>();
            public List<int> NumbersB { get; set; } = new List<int>();
            public Dictionary<int, string> Map { get; set; } = new Dictionary<int, string>();

        }
        [Test]
        public static void ListFieldTest()
        {
            string csv = @"name,numbersA,numbersB, Map
Bob,""1,2,3,4"",""6,7,8,9"",""1=Mark;2=Tom""";

            string expected = @"[
  {
    ""Name"": ""Bob"",
    ""NumbersA"": [
      1,
      2,
      3,
      4
    ],
    ""NumbersB"": [
      6,
      7,
      8,
      9
    ],
    ""Map"": {
      ""1"": ""Mark"",
      ""2"": ""Tom""
    }
  }
]";
            using (var r = ChoCSVReader<Names>.LoadText(csv)
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [ChoCSVRecordObject(NullValue = "#NULL#")]
        public class Emp
        {
            [ChoCSVRecordField(1, NullValue = "NULL")]
            public int? Id { get; set; }
            [ChoCSVRecordField(2)]
            public string Name { get; set; }
            [ChoCSVRecordField(3, NullValue = "#NULL#")]
            public string City { get; set; }
        }

        public static void TestNewMappingMethod()
        {
            string csv = @"Id, Name, City
NULL, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";

            var config = new ChoCSVRecordConfiguration<Emp>();
            config.Map(emp => emp.Name, f => f.FieldName("Name"));
            config.Map(emp => emp.City, f => f.FieldName("City"));
            config.Map(emp => emp.Id, f => f.FieldName("Id"));
            config.WithFirstLineHeader(false);

            using (var reader = ChoCSVReader<Emp>.LoadText(csv, Encoding.ASCII, config))
            {
                reader.AfterRecordLoad += (sender, args) => { };
                Console.WriteLine(reader.ToList().Dump());
            }
        }

        public class Fruit
        {
            //[DisplayName("name")]
            public string Name { get; set; }
            //[DisplayName("binomial name")]
            public string BinomialName { get; set; }
            //[Range(0, 2)]
            //[DisplayName("major_producers")]
            public List<string> MajorProducers { get; set; }
            public Nutrition Nutrition { get; set; }
        }

        public class Nutrition
        {
            //[DisplayName("nutrition_carbohydrates")]
            public string Carbohydrates { get; set; }
            //[DisplayName("nutrition_fat")]
            public string Fat { get; set; }
            //[DisplayName("nutrition_protein")]
            public string Protein { get; set; }
        }
        [Test]
        public static void CSV2JSON2()
        {
            string csv = @"name,binomial name,major_producers_0,major_producers_1,major_producers_2,nutrition_carbohydrates,nutrition_fat,nutrition_protein
Apple,Malus domestica,China,United States,Turkey,13.81g,0.17g,0.26g
Orange,Citrus x sinensis,Brazil,United States,India,11.75g,0.12g,0.94g";

            string expected = @"{
  ""Fruits"": [
    {
      ""Name"": ""Apple"",
      ""BinomialName"": ""Malus domestica"",
      ""MajorProducers"": [
        ""China"",
        ""United States"",
        ""Turkey""
      ],
      ""Nutrition"": {
        ""Carbohydrates"": ""13.81g"",
        ""Fat"": ""0.17g"",
        ""Protein"": ""0.26g""
      }
    },
    {
      ""Name"": ""Orange"",
      ""BinomialName"": ""Citrus x sinensis"",
      ""MajorProducers"": [
        ""Brazil"",
        ""United States"",
        ""India""
      ],
      ""Nutrition"": {
        ""Carbohydrates"": ""11.75g"",
        ""Fat"": ""0.12g"",
        ""Protein"": ""0.94g""
      }
    }
  ]
}";
            var config = new ChoCSVRecordConfiguration<Fruit>()
                .IndexMap(f => f.MajorProducers, 0, 2, m => m.FieldName("major_producers"))
                .Map(f => f.Nutrition.Carbohydrates, m => m.FieldName("nutrition_carbohydrates"))
                .Map(f => f.Nutrition.Fat, m => m.FieldName("nutrition_fat"))
                .Map(f => f.Nutrition.Protein, m => m.FieldName("nutrition_protein"))
                .Map(f => f.Name, m => m.FieldName("name"))
                .Map(f => f.BinomialName, m => m.FieldName("binomial name"))

                //.Index("MajorProducers", typeof(string[]), 0, 2, m => m.FieldName("major_producers"))
                //.MapRecordField("Nutrition.Carbohydrates", m => m.FieldName("nutrition_carbohydrates"))
                //.MapRecordField("Name", m => m.FieldName("name"))
                //.MapRecordField("BinomialName", m => m.FieldName("binomial name"))
                ;
            StringBuilder json = new StringBuilder();
            using (var r = ChoCSVReader<Fruit>.LoadText(csv, configuration: config)
                .WithFirstLineHeader()
                .Configure(c => c.ItemSeparator = '_')
                )
            {
                using (var w = new ChoJSONWriter(json)
                    .Configure(c => c.SupportMultipleContent = true)
                    )
                {
                    w.Write(new { Fruits = r.ToArray() });
                }
            }
            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class Fruit1
        {
            [DisplayName("name")]
            public string Name { get; set; }
            [DisplayName("binomial name")]
            public string BinomialName { get; set; }
            [Range(0, 2)]
            [DisplayName("major_producers")]
            public List<string> MajorProducers { get; set; }
            public Nutrition1 Nutrition { get; set; }
        }

        public class Nutrition1
        {
            [DisplayName("nutrition_carbohydrates")]
            public string Carbohydrates { get; set; }
            [DisplayName("nutrition_fat")]
            public string Fat { get; set; }
            [DisplayName("nutrition_protein")]
            public string Protein { get; set; }
        }
        [Test]
        public static void CSV2JSON2_1()
        {
            string csv = @"name,binomial name,major_producers_0,major_producers_1,major_producers_2,nutrition_carbohydrates,nutrition_fat,nutrition_protein
Apple,Malus domestica,China,United States,Turkey,13.81g,0.17g,0.26g
Orange,Citrus x sinensis,Brazil,United States,India,11.75g,0.12g,0.94g";

            string expected = @"{
  ""Fruits"": [
    {
      ""Name"": ""Apple"",
      ""BinomialName"": ""Malus domestica"",
      ""MajorProducers"": [
        ""China"",
        ""United States"",
        ""Turkey""
      ],
      ""Nutrition"": {
        ""Carbohydrates"": ""13.81g"",
        ""Fat"": ""0.17g"",
        ""Protein"": ""0.26g""
      }
    },
    {
      ""Name"": ""Orange"",
      ""BinomialName"": ""Citrus x sinensis"",
      ""MajorProducers"": [
        ""Brazil"",
        ""United States"",
        ""India""
      ],
      ""Nutrition"": {
        ""Carbohydrates"": ""11.75g"",
        ""Fat"": ""0.12g"",
        ""Protein"": ""0.94g""
      }
    }
  ]
}";
            var config = new ChoCSVRecordConfiguration<Fruit1>()
                //.Index("MajorProducers", typeof(string[]), 0, 2, m => m.FieldName("major_producers"))
                //.MapRecordField("Nutrition.Carbohydrates", m => m.FieldName("nutrition_carbohydrates"))
                //.MapRecordField("Name", m => m.FieldName("name"))
                //.MapRecordField("BinomialName", m => m.FieldName("binomial name"))
                ;
            StringBuilder json = new StringBuilder();
            using (var r = ChoCSVReader<Fruit>.LoadText(csv, configuration: config)
                .WithFirstLineHeader()
                .Configure(c => c.ItemSeparator = '_')
                )
            {
                using (var w = new ChoJSONWriter(json)
                    .Configure(c => c.SupportMultipleContent = true)
                    )
                {
                    w.Write(new { Fruits = r.ToArray() });
                }
            }
            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void ExcelFieldTest()
        {
            string csv = @"Id,Name,Salary
1,Carl,=""10000""
2,Mark,=""5000""
3,Tom,=""2000""";

            string expected = @"[
  {
    ""Id"": ""1"",
    ""Name"": ""Carl"",
    ""Salary"": ""10000""
  },
  {
    ""Id"": ""2"",
    ""Name"": ""Mark"",
    ""Salary"": ""5000""
  },
  {
    ""Id"": ""3"",
    ""Name"": ""Tom"",
    ""Salary"": ""2000""
  }
]";
            using (var parser = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithField("Id")
                .WithField("Name")
                .WithField("Salary")
                .MayHaveQuotedFields()
                //.WithField("Salary", m => m.Configure(c => c.ExcelField = true))
                .Configure(c => c.ImplicitExcelFieldValueHandling = true)
                )
            {
                var recs = parser.ToArray();
                foreach (var rec in recs)
                {
                    Console.WriteLine(String.Format("Id: {0}", rec.Id));
                    Console.WriteLine(String.Format("Name: {0}", rec.Name));
                    Console.WriteLine(String.Format("Salary: {0}", rec.Salary));

                }

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void QuoteAllFieldsTest()
        {
            string csv = @"""1"",1/2/2010,""The sample(""adasdad"") asdada"",""I was pooping in the door """"Stinky"""", so I'll be damn"",""AK""";

            string expected = @"[
  {
    ""Column1"": ""1"",
    ""Column2"": ""1/2/2010"",
    ""Column3"": ""The sample(\""adasdad\"") asdada"",
    ""Column4"": ""I was pooping in the door \""Stinky\"", so I'll be damn"",
    ""Column5"": ""AK""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .QuoteAllFields()
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class EmployeeRecMap : IChoNotifyRecordFieldConfigurable
        {
            [ChoCSVRecordField()]
            public int Id { get; set; }
            [ChoCSVRecordField(FieldName = "_name")]
            public string Name { get; set; }
            [ChoCSVRecordField()]
            public string City { get; set; }

            public void RecondFieldConfigure(ChoRecordFieldConfiguration fieldConfiguration)
            {
            }
        }
        [Test]
        public static void MapRecordFieldsTest()
        {
            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";
            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Tom"",
    ""Salary"": 0,
    ""City"": ""NY""
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark"",
    ""Salary"": 0,
    ""City"": ""NJ""
  },
  {
    ""Id"": 3,
    ""Name"": ""Lou"",
    ""Salary"": 0,
    ""City"": ""FL""
  },
  {
    ""Id"": 4,
    ""Name"": ""Smith"",
    ""Salary"": 0,
    ""City"": ""PA""
  },
  {
    ""Id"": 5,
    ""Name"": ""Raj"",
    ""Salary"": 0,
    ""City"": ""DC""
  }
]";

            var config = new ChoCSVRecordConfiguration().MapRecordFields<EmployeeRec>();

            using (var p = ChoCSVReader<EmployeeRec>.LoadText(csv, configuration: config)
                .WithFirstLineHeader()
                )
            {
                //foreach (var rec in p)
                //    Console.WriteLine(rec.Dump());
                var recs = p.ToArray();

                recs.Print();

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Test11()
        {
            string expected = @"[
  {
    ""CreationTime"": ""4/13/2020 2:21:12 AM"",
    ""Operation"": ""FileAccessed"",
    ""OrganizationId"": ""51abcde6-7de4-48b6-8418-cb3f1234f80b"",
    ""RecordType"": ""6"",
    ""UserType"": ""0"",
    ""Workload"": ""TestPoint"",
    ""ObjectId"": ""https://test.TestPoint.com/sites/Test1010/HeroGraphic/download.jfif"",
    ""UserId"": ""admin@test.gmail.com"",
    ""ClientIP"": ""40.115.253.28"",
    ""CorrelationId"": ""4038489f-a098-b000-8271-1ee73772c3a2"",
    ""EventSource"": ""TestPoint"",
    ""Id"": ""3a56bd4d-6e5f-4e3a-a7d5-08d7df5155c0"",
    ""ItemType"": ""File"",
    ""ListId"": ""eafd7d29-a7e6-4899-8609-aed5dd3f4dff"",
    ""ListItemUniqueId"": ""07aaf062-cd4d-44a7-9346-b4b92dfde804"",
    ""MessageTime"": ""1/1/0001 12:00:00 AM"",
    ""O365LogContentCreated"": ""4/13/2020 2:25:09 AM"",
    ""O365LogContentType"": ""Audit.TestPoint"",
    ""PartitionKey"": ""2020"",
    ""Policy"": ""0"",
    ""Site"": ""abcdef"",
    ""SiteUrl"": ""https://test.TestPoint.com/sites/Test1010/"",
    ""SourceFileExtension"": ""jfif"",
    ""SourceFileName"": ""download.jfif"",
    ""SourceRelativeUrl"": ""HeroGraphic"",
    ""Timestamp"": ""1/1/0001 12:00:00 AM +00:00"",
    ""UserAgent"": ""NONISV|TestPointPnP|PnPCore/3.18.2002.0"",
    ""UserKey"": ""i:0h.f|membership|100300008d844fb9@live.com"",
    ""Version"": ""1"",
    ""WebId"": ""4123wqe-9e10-1a90-9b50-2999f6d76e10"",
    ""CustomUniqueId"": null,
    ""ApplicationId"": null,
    ""EventData"": null,
    ""ModifiedProperties"": null,
    ""TargetUserOrGroupName"": null,
    ""TargetUserOrGroupType"": null
  },
  {
    ""CreationTime"": ""4/13/2020 2:21:11 AM"",
    ""Operation"": ""FileAccessed"",
    ""OrganizationId"": ""51abcde6-7de4-48b6-8418-cb3f1234f80b"",
    ""RecordType"": ""6"",
    ""UserType"": ""0"",
    ""Workload"": ""TestPoint"",
    ""ObjectId"": ""https://test.TestPoint.com/sites/Test1010/HeroGraphic/crab-icon-fun-crab.jpg"",
    ""UserId"": ""admin@test.gmail.com"",
    ""ClientIP"": ""40.115.253.28"",
    ""CorrelationId"": ""4038489f-008a-b000-8271-19e9fdfa17e5"",
    ""EventSource"": ""TestPoint"",
    ""Id"": ""123456789"",
    ""ItemType"": ""File"",
    ""ListId"": ""eafd7d29-a7e6-4899-8609-aed5dd3f4dff"",
    ""ListItemUniqueId"": ""94802df0-dbc3-440f-ae8f-90d8bd918609"",
    ""MessageTime"": ""1/1/0001 12:00:00 AM"",
    ""O365LogContentCreated"": ""4/13/2020 2:25:09 AM"",
    ""O365LogContentType"": ""Audit.TestPoint"",
    ""PartitionKey"": ""2020"",
    ""Policy"": ""0"",
    ""Site"": ""abcdef"",
    ""SiteUrl"": ""https://test.TestPoint.com/sites/Test1010/"",
    ""SourceFileExtension"": ""jpg"",
    ""SourceFileName"": ""crab-icon-fun-crab.jpg"",
    ""SourceRelativeUrl"": ""HeroGraphic"",
    ""Timestamp"": ""1/1/0001 12:00:00 AM +00:00"",
    ""UserAgent"": ""NONISV|TestPointPnP|PnPCore/3.18.2002.0"",
    ""UserKey"": ""i:0h.f|membership|100300008d844fb9@live.com"",
    ""Version"": ""1"",
    ""WebId"": ""4123wqe-9e10-1a90-9b50-2999f6d76e10"",
    ""CustomUniqueId"": null,
    ""ApplicationId"": null,
    ""EventData"": null,
    ""ModifiedProperties"": null,
    ""TargetUserOrGroupName"": null,
    ""TargetUserOrGroupType"": null
  }
]";
            using (var csvReader = new ChoCSVReader("sample8.csv")
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                .QuoteAllFields(true)
                //.WithMaxScanRows(10)
                //.Configure(c => c.TurnOnMultiLineHeaderSupport = true)
                )
            {
                csvReader.MultiLineHeader += (o, e) =>
                {
                    if (e.LineNo <= 2)
                        e.IsHeader = true;
                    else
                        e.IsHeader = false;
                };


                //foreach (var rec in csvReader)
                //    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(csvReader, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void MultiLineHeaderTest()
        {
            string csv = @"* Select	d  : 02:02:12 20 MAR 2017						
* Shippi	g Date >= 01/20/2017 ; Shipping Dat	<= 03/20/2017	; Shipping	Branch = 2	9,15,19,21,22,	5,26,27,2	,29,30,31,
********	***********************************	**************	**********	**********	**************	*********	**********
							
CUSTOMER	CUSTOMER NAME	INVOICE ID	PURCHASE	PRODUCT ID	PURCHASED	PURCHASED QTY	LOCATION
ID	DATE	AMOUNT	QUANTITY ID
22160	MANSFIELD BROTHERS HEATING & AIR	sss.001	02/08/2017	193792	69.374	2	30
27849	OWSLEY SUPPLY LLC  - EQUIPMENT	sss.001	03/14/2017	123906	70.409	1	2
27849	OWSLEY SUPPLY LLC  - EQUIPMENT	sss.001	03/14/2017	40961	10.000	1	2
16794	ALEXANDER GILMORE dba AL'S HEATING	sss.001	01/25/2017	116511	63.016	1	15
16794	ALEXANDER GILMORE dba AL'S HEATING	sss.001	01/25/2017	116511	-63.016	-1	15
16794	ALEXANDER GILMORE dba AL'S HEATING	sss.001	01/25/2017	122636	30.748	1	15
16794	ALEXANDER GILMORE dba AL'S HEATING	sss.001	01/25/2017	137661	432.976	1	15
16794	ALEXANDER GILMORE dba AL'S HEATING	sss.001	01/25/2017	137661	-432.976	-1	15";


            string expected = @"[
  {
    ""CUSTOMER"": 22160,
    ""CUSTOMER NAME"": ""MANSFIELD BROTHERS HEATING & AIR"",
    ""INVOICE ID"": ""sss.001"",
    ""PURCHASE"": ""2017-02-08T00:00:00"",
    ""PRODUCT ID"": 193792,
    ""PURCHASED"": 69.374,
    ""PURCHASED QTY"": 2,
    ""LOCATIONID"": 30,
    ""DATE"": null,
    ""AMOUNT"": null,
    ""QUANTITY ID"": null
  },
  {
    ""CUSTOMER"": 27849,
    ""CUSTOMER NAME"": ""OWSLEY SUPPLY LLC  - EQUIPMENT"",
    ""INVOICE ID"": ""sss.001"",
    ""PURCHASE"": ""2017-03-14T00:00:00"",
    ""PRODUCT ID"": 123906,
    ""PURCHASED"": 70.409,
    ""PURCHASED QTY"": 1,
    ""LOCATIONID"": 2,
    ""DATE"": null,
    ""AMOUNT"": null,
    ""QUANTITY ID"": null
  },
  {
    ""CUSTOMER"": 27849,
    ""CUSTOMER NAME"": ""OWSLEY SUPPLY LLC  - EQUIPMENT"",
    ""INVOICE ID"": ""sss.001"",
    ""PURCHASE"": ""2017-03-14T00:00:00"",
    ""PRODUCT ID"": 40961,
    ""PURCHASED"": 10.0,
    ""PURCHASED QTY"": 1,
    ""LOCATIONID"": 2,
    ""DATE"": null,
    ""AMOUNT"": null,
    ""QUANTITY ID"": null
  },
  {
    ""CUSTOMER"": 16794,
    ""CUSTOMER NAME"": ""ALEXANDER GILMORE dba AL'S HEATING"",
    ""INVOICE ID"": ""sss.001"",
    ""PURCHASE"": ""2017-01-25T00:00:00"",
    ""PRODUCT ID"": 116511,
    ""PURCHASED"": 63.016,
    ""PURCHASED QTY"": 1,
    ""LOCATIONID"": 15,
    ""DATE"": null,
    ""AMOUNT"": null,
    ""QUANTITY ID"": null
  },
  {
    ""CUSTOMER"": 16794,
    ""CUSTOMER NAME"": ""ALEXANDER GILMORE dba AL'S HEATING"",
    ""INVOICE ID"": ""sss.001"",
    ""PURCHASE"": ""2017-01-25T00:00:00"",
    ""PRODUCT ID"": 116511,
    ""PURCHASED"": -63.016,
    ""PURCHASED QTY"": -1,
    ""LOCATIONID"": 15,
    ""DATE"": null,
    ""AMOUNT"": null,
    ""QUANTITY ID"": null
  },
  {
    ""CUSTOMER"": 16794,
    ""CUSTOMER NAME"": ""ALEXANDER GILMORE dba AL'S HEATING"",
    ""INVOICE ID"": ""sss.001"",
    ""PURCHASE"": ""2017-01-25T00:00:00"",
    ""PRODUCT ID"": 122636,
    ""PURCHASED"": 30.748,
    ""PURCHASED QTY"": 1,
    ""LOCATIONID"": 15,
    ""DATE"": null,
    ""AMOUNT"": null,
    ""QUANTITY ID"": null
  },
  {
    ""CUSTOMER"": 16794,
    ""CUSTOMER NAME"": ""ALEXANDER GILMORE dba AL'S HEATING"",
    ""INVOICE ID"": ""sss.001"",
    ""PURCHASE"": ""2017-01-25T00:00:00"",
    ""PRODUCT ID"": 137661,
    ""PURCHASED"": 432.976,
    ""PURCHASED QTY"": 1,
    ""LOCATIONID"": 15,
    ""DATE"": null,
    ""AMOUNT"": null,
    ""QUANTITY ID"": null
  },
  {
    ""CUSTOMER"": 16794,
    ""CUSTOMER NAME"": ""ALEXANDER GILMORE dba AL'S HEATING"",
    ""INVOICE ID"": ""sss.001"",
    ""PURCHASE"": ""2017-01-25T00:00:00"",
    ""PRODUCT ID"": 137661,
    ""PURCHASED"": -432.976,
    ""PURCHASED QTY"": -1,
    ""LOCATIONID"": 15,
    ""DATE"": null,
    ""AMOUNT"": null,
    ""QUANTITY ID"": null
  }
]";
            using (var r = ChoTSVReader.New(new StringBuilder(csv)) // ChoTSVReader.LoadText(csv)
                .WithMaxScanRows(2)
                .Setup(s =>
                {
                    s.SkipUntil += (o, e) =>
                    {
                        e.Skip = e.Index <= 4;
                    };
                })
                .Setup(s =>
                {
                    s.MultiLineHeader += (o, e) =>
                    {
                        if (e.LineNo <= 6)
                            e.IsHeader = true;
                        else
                            e.IsHeader = false;
                    };
                })
                .Configure(c => c.TurnOnMultiLineHeaderSupport = true)
                .ThrowAndStopOnMissingField(false)
                )
            {
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void CSVArrayToJSON1()
        {
            string csv = @"id,name,friends/0,friends/1
1,Tom,Dick,Harry";

            string expected = @"{
  ""id"": ""1"",
  ""name"": ""Tom"",
  ""friends"": [
    ""Dick"",
    ""Harry""
  ]
}";
            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter(json)
                .Configure(c => c.SupportMultipleContent = true)
                .Configure(c => c.SingleElement = true)
                )
            {
                using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                    .Configure(c => c.AutoArrayDiscovery = true)
                    .Configure(c => c.ArrayIndexSeparator = '/')
                    )
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }


        [ChoCSVFileHeader]
        [ChoCSVRecordObject("|")]
        public partial class ArchivoCliente
        {
            [DisplayFormat(DataFormatString = "dd/MM/yy hh:mm")]
            public DateTime FECHA_FRANQUEO { get; set; } // datetime2(7), not null
            public string ID_INCIDENCIA { get; set; } // nvarchar(7), not null
            public string CIF { get; set; } // nvarchar(9), not null
            public string PERSONA_CONTACTO { get; set; } // nvarchar(50), not null
        }
        [Test]
        public static void CustomDateTimeTest()
        {
            string csv = @"FECHA_FRANQUEO|ID_INCIDENCIA|CIF|PERSONA_CONTACTO
14/04/20 09:44|7093927|bbbbbbbbb|RAFA
14/04/20 09:02|7093933|aaaaaaaaa|Maria / Roger";

            string expected = @"[
  {
    ""FECHA_FRANQUEO"": ""2020-04-14T09:44:00"",
    ""ID_INCIDENCIA"": ""7093927"",
    ""CIF"": ""bbbbbbbbb"",
    ""PERSONA_CONTACTO"": ""RAFA""
  },
  {
    ""FECHA_FRANQUEO"": ""2020-04-14T09:02:00"",
    ""ID_INCIDENCIA"": ""7093933"",
    ""CIF"": ""aaaaaaaaa"",
    ""PERSONA_CONTACTO"": ""Maria / Roger""
  }
]";
            using (var r = ChoCSVReader<ArchivoCliente>.LoadText(csv))
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

            return;

            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "dd/MM/yy hh:mm";

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithDelimiter("|")
                .WithMaxScanRows(2)
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }
        [Test]
        public static void CustomDateTimeTest_Dynamic()
        {
            string csv = @"FECHA_FRANQUEO|ID_INCIDENCIA|CIF|PERSONA_CONTACTO
14/04/20 09:44|7093927|bbbbbbbbb|RAFA
14/04/20 09:02|7093933|aaaaaaaaa|Maria / Roger";

            string expected = @"[
  {
    ""FECHA_FRANQUEO"": ""2020-04-14T09:44:00"",
    ""ID_INCIDENCIA"": 7093927,
    ""CIF"": ""bbbbbbbbb"",
    ""PERSONA_CONTACTO"": ""RAFA""
  },
  {
    ""FECHA_FRANQUEO"": ""2020-04-14T09:02:00"",
    ""ID_INCIDENCIA"": 7093933,
    ""CIF"": ""aaaaaaaaa"",
    ""PERSONA_CONTACTO"": ""Maria / Roger""
  }
]";
            //ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "dd/MM/yy hh:mm";

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithDelimiter("|")
                .WithMaxScanRows(2)
                .TypeConverterFormatSpec(t => t.DateTimeFormat = "dd/MM/yy hh:mm")
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void ConvertCsvStream()
        {
            string csvText = "\"Invoice Number\"\n\"1583-03\"\n\"1589-00\"";

            string expected = @"[
  {
    ""Invoice Number"": ""1583-03""
  },
  {
    ""Invoice Number"": ""1589-00""
  }
]";
            StringBuilder json = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csvText)
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                //using (var w = new ChoJSONWriter(json))
                //    w.Write(r);
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
            //Console.WriteLine(json.ToString());
        }
        [Test]
        public static void TSV2Xml()
        {
            string tsv = @"Time	Object	pmPdDrb	pmPdcDlSrb
00:45	EUtranCellFDD=GNL02294_7A_1	2588007	1626
00:45	EUtranCellFDD=GNL02294_7B_1	18550	32
00:45	EUtranCellFDD=GNL02294_7C_1	26199	38
00:45	EUtranCellFDD=GNL02294_9A_1	3857243	751";

            string expected = @"<xmlnodes xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <xmlnode>
    <Time>00:45</Time>
    <Object>EUtranCellFDD=GNL02294_7A_1</Object>
    <pmPdDrb>2588007</pmPdDrb>
    <pmPdcDlSrb>1626</pmPdcDlSrb>
  </xmlnode>
  <xmlnode>
    <Time>00:45</Time>
    <Object>EUtranCellFDD=GNL02294_7B_1</Object>
    <pmPdDrb>18550</pmPdDrb>
    <pmPdcDlSrb>32</pmPdcDlSrb>
  </xmlnode>
  <xmlnode>
    <Time>00:45</Time>
    <Object>EUtranCellFDD=GNL02294_7C_1</Object>
    <pmPdDrb>26199</pmPdDrb>
    <pmPdcDlSrb>38</pmPdcDlSrb>
  </xmlnode>
  <xmlnode>
    <Time>00:45</Time>
    <Object>EUtranCellFDD=GNL02294_9A_1</Object>
    <pmPdDrb>3857243</pmPdDrb>
    <pmPdcDlSrb>751</pmPdcDlSrb>
  </xmlnode>
</xmlnodes>";
            StringBuilder xml = new StringBuilder();
            using (var r = ChoTSVReader.LoadText(tsv)
                .WithFirstLineHeader()
                )
            {
                //var dt = r.AsDataTable();
                //return;
                using (var w = new ChoXmlWriter(xml)
                    .Configure(c => c.RootName = "xmlnodes")
                    .Configure(c => c.NodeName = "xmlnode")
                    )
                    w.Write(r);
            }
            Console.WriteLine(xml.ToString());
            var actual = xml.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void DuplicateNameInDynamicModeTest()
        {
            string csv = @"Id, Name, Name, City, City
1, Tom, Mark, NY, NYC
2, Kevin, Fahey, NJ, NJX";

            string expected = @"[
  {
    ""Id"": ""1"",
    ""Name"": ""Tom"",
    ""Name_2"": ""Mark"",
    ""City"": ""NY"",
    ""City_2"": ""NYC""
  },
  {
    ""Id"": ""2"",
    ""Name"": ""Kevin"",
    ""Name_2"": ""Fahey"",
    ""City"": ""NJ"",
    ""City_2"": ""NJX""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .AutoIncrementDuplicateColumnNames()
                //.ArrayIndexSeparator('_')
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void SkipEmptyLinesTest()
        {
            string csv = "^^Id, Name^^1, Tom^^2, Mark";

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
                .Configure(c => c.IgnoreEmptyLine = true)
                .Configure(c => c.MayContainEOLInData = false)
                .WithEOLDelimiter("^^")
                .Configure(c => c.MaxLineSize = 1000005)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void DoNotSkipEmptyLinesTest()
        {
            string csv = "^^Id, Name^^1, Tom^^2, Mark";

            string expected = @"[
  {},
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
                .Configure(c => c.IgnoreEmptyLine = false)
                .Configure(c => c.MayContainEOLInData = false)
                .WithEOLDelimiter("^^")
                .Configure(c => c.MaxLineSize = 1000005)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void GuidTest()
        {
            string csv = @"Id, Guid
10, cc6f0116-589a-4cf1-8605-a4eb6ab3bd34";

            string expected = @"[
  {
    ""Id"": 10,
    ""Guid"": ""cc6f0116-589a-4cf1-8605-a4eb6ab3bd34""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(1)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void DefaultValueTest()
        {
            string csv = @"Id, Guid
10, cc6f0116-589a-4cf1-8605-a4eb6ab3bd34
20, 
";
            string expected = @"[
  {
    ""Id"": 10,
    ""Guid"": ""cc6f0116-589a-4cf1-8605-a4eb6ab3bd34""
  },
  {
    ""Id"": 20,
    ""Guid"": ""f5e7bdc9-d8b8-4129-8cf4-4150d481c523""
  }
]";

            string defaultGuid = "f5e7bdc9-d8b8-4129-8cf4-4150d481c523";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithField("Id", fieldType: typeof(int))
                .WithField("Guid", fieldType: typeof(Guid), defaultValue: defaultGuid)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void FallbacktValueTest()
        {
            string csv = @"Id;Guid
10;cc6f0116-589a-4cf1-8605-a4eb6ab3bd34
20;cc6f0116-589a-4cf1-8605-a4eb6ab3bd34
30a;cc6f0116-589a-4cf1-8605-a4eb6ab3bd34
";

            string expected = @"[
  {
    ""Id"": 10,
    ""Guid"": ""cc6f0116-589a-4cf1-8605-a4eb6ab3bd34""
  },
  {
    ""Id"": 20,
    ""Guid"": ""cc6f0116-589a-4cf1-8605-a4eb6ab3bd34""
  },
  {
    ""Id"": 200,
    ""Guid"": ""cc6f0116-589a-4cf1-8605-a4eb6ab3bd34""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(1)
                .AutoDetectDelimiter()
                .WithField("Id", fieldType: typeof(int), fallbackValue: 200)
                .WithField("Guid", fieldType: typeof(Guid), defaultValue: Guid.NewGuid())
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CSV2JSONGroupBy()
        {
            string csv = @"Category,BookId,BookName
Mystery,1,My first book
Biography,2,My second book
Mystery,3,My third book
Crime,4,My fourth book
Romance,5,My fifth book
Biography,6,My sixth book
Mystery,7,My seventh book
Romance,8,My eight book
SciFi,9,My ninth book
Poetry,10,My tenth book";

            string expected = @"[
  {
    ""Mystery"": {
      ""1"": {
        ""Name"": ""My first book""
      },
      ""3"": {
        ""Name"": ""My third book""
      },
      ""7"": {
        ""Name"": ""My seventh book""
      }
    },
    ""Biography"": {
      ""2"": {
        ""Name"": ""My second book""
      },
      ""6"": {
        ""Name"": ""My sixth book""
      }
    },
    ""Crime"": {
      ""4"": {
        ""Name"": ""My fourth book""
      }
    },
    ""Romance"": {
      ""5"": {
        ""Name"": ""My fifth book""
      },
      ""8"": {
        ""Name"": ""My eight book""
      }
    },
    ""SciFi"": {
      ""9"": {
        ""Name"": ""My ninth book""
      }
    },
    ""Poetry"": {
      ""10"": {
        ""Name"": ""My tenth book""
      }
    }
  }
]";
            StringBuilder json = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoJSONWriter(json))
                {
                    w.Write(r.GroupBy(r1 => r1.Category)
                        .Select(g => new KeyValuePair<string, object>(g.Key, g.ToArray().ToDictionary(kvp => kvp.BookId, kvp => new { Name = kvp.BookName }))).ToDictionary()
                        );
                }
            }

            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        [ChoCSVFileHeader(IgnoreHeader = true)]
        [ChoCSVRecordObject(QuoteAllFields = true)]
        public class Insurance
        {
            //[ChoCSVRecordField(FieldName = "Short Name")]
            public string ShortName { get; set; }

            //[ChoCSVRecordField(FieldName = "Cust Key")]
            public string CustKey { get; set; }

            //[ChoCSVRecordField(FieldName = "Old CIF Key")]
            public string OldCIFKey { get; set; }
        }
        [Test]
        public static void Issue90()
        {
            string csv = @"Short Name,Cust Key,Old CIF Key
Fire & Casualty,00000000000001,AAA FIR CA 00
""Doe, John"",00000000000008,JDOEPD.0J5J.00
Legal Research &,00000000000009,AALE RES WR 00
Indoor Advertisi Llc,00000000000011,CO MA L00";

            string expected = @"[
  {
    ""ShortName"": ""Fire & Casualty"",
    ""CustKey"": ""00000000000001"",
    ""OldCIFKey"": ""AAA FIR CA 00""
  },
  {
    ""ShortName"": ""Doe, John"",
    ""CustKey"": ""00000000000008"",
    ""OldCIFKey"": ""JDOEPD.0J5J.00""
  },
  {
    ""ShortName"": ""Legal Research &"",
    ""CustKey"": ""00000000000009"",
    ""OldCIFKey"": ""AALE RES WR 00""
  },
  {
    ""ShortName"": ""Indoor Advertisi Llc"",
    ""CustKey"": ""00000000000011"",
    ""OldCIFKey"": ""CO MA L00""
  }
]";
            using (var r = ChoCSVReader<Insurance>.LoadText(csv)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void FadaSignTest()
        {
            //using (var reader = new ChoCSVReader(new StreamReader("COUNTY.csv", Encoding.GetEncoding("iso-8859-1")))
            //        .WithFirstLineHeader()
            //        .WithDelimiter("\t")
            //        )
            //{
            //    var dt = reader.AsDataTable();
            //}
            //return;

            string csv = @"Id, Name
1, Athchóirigh an Téacs: Gaeilge (ga)
2, CHIARRAÍ";

            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Athch�irigh an T�acs: Gaeilge (ga)""
  },
  {
    ""Id"": 2,
    ""Name"": ""CHIARRA""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv, Encoding.GetEncoding(1252))
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                .Configure(c => c.Culture = new System.Globalization.CultureInfo("ga-IE"))
                )
            {
                //var dt = r.AsDataTable();
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CultureSpecificDateTimeTest()
        {
            string csv =
@"Id,Date,Account,Amount,Subcategory,Memo
 1,09/05/2017,XXX XXXXXX,-29.00,FT , [Sample string]
 2,09/05/2017,XXX XXXXXX,-20.00,FT ,[Sample string]
 3,25/05/2017,XXX XXXXXX,-6.30,PAYMENT,[Sample string]";

            string expected = @"[
  {
    ""Id"": 1,
    ""Date"": ""2017-05-09T00:00:00"",
    ""Account"": ""XXX XXXXXX"",
    ""Amount"": -29.0,
    ""Subcategory"": ""FT"",
    ""Memo"": ""[Sample string]""
  },
  {
    ""Id"": 2,
    ""Date"": ""2017-05-09T00:00:00"",
    ""Account"": ""XXX XXXXXX"",
    ""Amount"": -20.0,
    ""Subcategory"": ""FT"",
    ""Memo"": ""[Sample string]""
  },
  {
    ""Id"": 3,
    ""Date"": ""2017-05-25T00:00:00"",
    ""Account"": ""XXX XXXXXX"",
    ""Amount"": -6.3,
    ""Subcategory"": ""PAYMENT"",
    ""Memo"": ""[Sample string]""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                .Configure(c => c.Culture = new System.Globalization.CultureInfo("en-GB"))
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void SpecialCSVTest()
        {
            string csv = @"A, B
X , 27
Y , 27
S, 58 M
S, T% x 100
, 121, 86, 100, 168, 128, 111, 22, 82, 129, 127, 106, 69, 51, 55, 80, 26, 47, 61, 44, 52, 62, 80, 37, 65, 55, 57
105, 125, 129, 156, 114, 93, 85, 96, 58, 54, 33, 19, 69, 86, 79, 124, 75, 59, 33, 53, 42, 66, 66, 94, 96, 113, 92
189, 156, 91, 127, 178, 117, 85, 129, 72, 38, 86, 77, 94, 75, 71, 17, 49, 39, 48, 84, 64, 54, 81, 37, 45, 80, 80
105, 149, 134, 134, 151, 91, 153, 101, 64, 43, 37, 51, 78, 64, 27, 15, 23, 41, 79, 116, 161, 98, 105, 66, 98, 95, 28
122, 98, 113, 114, 159, 114, 63, 79, 50, 52, 59, 30, 43, 94, 63, 9, 52, 68, 59, 66, 89, 107, 65, 53, 185, 62, 24
57, 75, 39, 75, 44, 44, 30, 59, 44, 62, 88, 57, 32, 111, 16, 8, 44, 38, 29, 68, 73, 52, 27, 26, 82, 47, 49
101, 64, 48, 34, 45, 90, 105, 56, 37, 37, 51, 47, 51, 60, 30, 30, 27, 66, 75, 80, 134, 86, 28, 30, 106, 64, 61
61, 129, 100, 44, 108, 131, 123, 77, 96, 76, 62, 68, 56, 48, 61, 30, 83, 125, 87, 44, 21, 67, 56, 29, 71, 54, 50";

            string expected = @"[
  {
    ""Column1"": null,
    ""Column2"": 121,
    ""Column3"": 86,
    ""Column4"": 100,
    ""Column5"": 168,
    ""Column6"": 128,
    ""Column7"": 111,
    ""Column8"": 22,
    ""Column9"": 82,
    ""Column10"": 129,
    ""Column11"": 127,
    ""Column12"": 106,
    ""Column13"": 69,
    ""Column14"": 51,
    ""Column15"": 55,
    ""Column16"": 80,
    ""Column17"": 26,
    ""Column18"": 47,
    ""Column19"": 61,
    ""Column20"": 44,
    ""Column21"": 52,
    ""Column22"": 62,
    ""Column23"": 80,
    ""Column24"": 37,
    ""Column25"": 65,
    ""Column26"": 55,
    ""Column27"": 57
  },
  {
    ""Column1"": ""105"",
    ""Column2"": 125,
    ""Column3"": 129,
    ""Column4"": 156,
    ""Column5"": 114,
    ""Column6"": 93,
    ""Column7"": 85,
    ""Column8"": 96,
    ""Column9"": 58,
    ""Column10"": 54,
    ""Column11"": 33,
    ""Column12"": 19,
    ""Column13"": 69,
    ""Column14"": 86,
    ""Column15"": 79,
    ""Column16"": 124,
    ""Column17"": 75,
    ""Column18"": 59,
    ""Column19"": 33,
    ""Column20"": 53,
    ""Column21"": 42,
    ""Column22"": 66,
    ""Column23"": 66,
    ""Column24"": 94,
    ""Column25"": 96,
    ""Column26"": 113,
    ""Column27"": 92
  },
  {
    ""Column1"": ""189"",
    ""Column2"": 156,
    ""Column3"": 91,
    ""Column4"": 127,
    ""Column5"": 178,
    ""Column6"": 117,
    ""Column7"": 85,
    ""Column8"": 129,
    ""Column9"": 72,
    ""Column10"": 38,
    ""Column11"": 86,
    ""Column12"": 77,
    ""Column13"": 94,
    ""Column14"": 75,
    ""Column15"": 71,
    ""Column16"": 17,
    ""Column17"": 49,
    ""Column18"": 39,
    ""Column19"": 48,
    ""Column20"": 84,
    ""Column21"": 64,
    ""Column22"": 54,
    ""Column23"": 81,
    ""Column24"": 37,
    ""Column25"": 45,
    ""Column26"": 80,
    ""Column27"": 80
  },
  {
    ""Column1"": ""105"",
    ""Column2"": 149,
    ""Column3"": 134,
    ""Column4"": 134,
    ""Column5"": 151,
    ""Column6"": 91,
    ""Column7"": 153,
    ""Column8"": 101,
    ""Column9"": 64,
    ""Column10"": 43,
    ""Column11"": 37,
    ""Column12"": 51,
    ""Column13"": 78,
    ""Column14"": 64,
    ""Column15"": 27,
    ""Column16"": 15,
    ""Column17"": 23,
    ""Column18"": 41,
    ""Column19"": 79,
    ""Column20"": 116,
    ""Column21"": 161,
    ""Column22"": 98,
    ""Column23"": 105,
    ""Column24"": 66,
    ""Column25"": 98,
    ""Column26"": 95,
    ""Column27"": 28
  },
  {
    ""Column1"": ""122"",
    ""Column2"": 98,
    ""Column3"": 113,
    ""Column4"": 114,
    ""Column5"": 159,
    ""Column6"": 114,
    ""Column7"": 63,
    ""Column8"": 79,
    ""Column9"": 50,
    ""Column10"": 52,
    ""Column11"": 59,
    ""Column12"": 30,
    ""Column13"": 43,
    ""Column14"": 94,
    ""Column15"": 63,
    ""Column16"": 9,
    ""Column17"": 52,
    ""Column18"": 68,
    ""Column19"": 59,
    ""Column20"": 66,
    ""Column21"": 89,
    ""Column22"": 107,
    ""Column23"": 65,
    ""Column24"": 53,
    ""Column25"": 185,
    ""Column26"": 62,
    ""Column27"": 24
  },
  {
    ""Column1"": ""57"",
    ""Column2"": 75,
    ""Column3"": 39,
    ""Column4"": 75,
    ""Column5"": 44,
    ""Column6"": 44,
    ""Column7"": 30,
    ""Column8"": 59,
    ""Column9"": 44,
    ""Column10"": 62,
    ""Column11"": 88,
    ""Column12"": 57,
    ""Column13"": 32,
    ""Column14"": 111,
    ""Column15"": 16,
    ""Column16"": 8,
    ""Column17"": 44,
    ""Column18"": 38,
    ""Column19"": 29,
    ""Column20"": 68,
    ""Column21"": 73,
    ""Column22"": 52,
    ""Column23"": 27,
    ""Column24"": 26,
    ""Column25"": 82,
    ""Column26"": 47,
    ""Column27"": 49
  },
  {
    ""Column1"": ""101"",
    ""Column2"": 64,
    ""Column3"": 48,
    ""Column4"": 34,
    ""Column5"": 45,
    ""Column6"": 90,
    ""Column7"": 105,
    ""Column8"": 56,
    ""Column9"": 37,
    ""Column10"": 37,
    ""Column11"": 51,
    ""Column12"": 47,
    ""Column13"": 51,
    ""Column14"": 60,
    ""Column15"": 30,
    ""Column16"": 30,
    ""Column17"": 27,
    ""Column18"": 66,
    ""Column19"": 75,
    ""Column20"": 80,
    ""Column21"": 134,
    ""Column22"": 86,
    ""Column23"": 28,
    ""Column24"": 30,
    ""Column25"": 106,
    ""Column26"": 64,
    ""Column27"": 61
  },
  {
    ""Column1"": ""61"",
    ""Column2"": 129,
    ""Column3"": 100,
    ""Column4"": 44,
    ""Column5"": 108,
    ""Column6"": 131,
    ""Column7"": 123,
    ""Column8"": 77,
    ""Column9"": 96,
    ""Column10"": 76,
    ""Column11"": 62,
    ""Column12"": 68,
    ""Column13"": 56,
    ""Column14"": 48,
    ""Column15"": 61,
    ""Column16"": 30,
    ""Column17"": 83,
    ""Column18"": 125,
    ""Column19"": 87,
    ""Column20"": 44,
    ""Column21"": 21,
    ""Column22"": 67,
    ""Column23"": 56,
    ""Column24"": 29,
    ""Column25"": 71,
    ""Column26"": 54,
    ""Column27"": 50
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithMaxScanRows(1)
                .Setup(s => s.SkipUntil += (o, e) =>
                {
                    e.Skip = e.Index <= 5;
                })
                .Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.None)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                //List<double> l = new List<double>();
                //foreach (var rec in r)
                //{
                //    l.AddRange(((object[])rec.ValuesArray).Select(v => v.CastTo<int>() / 100.0).ToArray());
                //}

                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class EmployeeZ
        {
            public int Id { get; set; }
            [Required]
            public string Name { get; set; }
        }
        [Test]
        public static void HeaderNotMatchTest()
        {
            string csv = @"Id, Name1
1, Tom
2, Mark";
            string expected = null;
            using (var r = ChoCSVReader<EmployeeZ>.LoadText(csv)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField()
                .Setup(s => s.RecordLoadError += (o, e) =>
                {
                    Console.WriteLine(e.Exception.Message);
                    if (e.Exception is ChoMissingRecordFieldException)
                    {
                        e.Handled = true;
                        var obj = e.Record as EmployeeZ;
                        obj.Name = "XXX";
                    }
                    else
                        e.Handled = true;
                })
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                Assert.Throws<ChoMissingCSVColumnException>(() =>
                {
                    var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                    Assert.AreEqual(expected, actual);
                });
            }
        }
        public class EmployeeY
        {
            public int id { get; set; }
            public string Name { get; set; }
            public string Status { get; set; }

        }
        [Test]
        public static void MissingDataTest()
        {
            string csv = @"Id, Name, Status
1, Tom, Active
2, Mark";
            string expected = null;
            using (var r = ChoCSVReader<EmployeeY>.LoadText(csv)
                .WithFirstLineHeader()
                .Setup(s => s.RecordLoadError += (o, e) =>
                {
                    Console.WriteLine(e.Exception.Message);
                    if (e.Exception is ChoMissingRecordFieldException)
                        e.Handled = false;
                    else
                        e.Handled = true;
                }))
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                Assert.Throws<ChoMissingRecordFieldException>(() =>
                {
                    var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                    Assert.AreEqual(expected, actual);
                });
            }

            return;

            //            string csv = @"Id, Name
            //1, Tom
            //2";

            //            using (var r = ChoCSVReader<EmployeeZ>.LoadText(csv)
            //                .WithFirstLineHeader()
            //                .Setup(s => s.RecordLoadError += (o, e) =>
            //                {
            //                    Console.WriteLine(e.Exception.Message);
            //                    e.Handled = true;
            //                })
            //                //.ThrowAndStopOnMissingField()
            //                )
            //            {
            //                foreach (var rec in r)
            //                    Console.WriteLine(rec.Dump());
            //            }
        }
        [Test]
        public static void NestedColumnSeparatorTest()
        {
            string csv = @"id,name,category/0,category/name,category/subcategory/id,category/subcategory/name,description
0,Test123,15,Cat123,10,SubCat123,Desc123";

            string expected = @"[
  {
    ""id"": ""0"",
    ""name"": ""Test123"",
    ""category"": {
      ""0"": ""15"",
      ""name"": ""Cat123"",
      ""subcategory"": {
        ""id"": ""10"",
        ""name"": ""SubCat123""
      }
    },
    ""description"": ""Desc123""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .Configure(c => c.NestedKeySeparator = '/'))
            {
                //foreach (var x in csv) Console.WriteLine(x.DumpAsJson());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void FluentAPIMappingTest()
        {
            string csv = @"Id, Name

2, Mark";
            string expected = @"[
  {
    ""Id"": 2,
    ""Name"": ""XX""
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark""
  }
]";

            var c = new ChoCSVRecordConfiguration<EmployeeZ>();
            c.Map(m => m.Id, m => m.DefaultValue(2));
            c.Map(m => m.Name, m => m.FallbackValue("XX"));

            using (var r = ChoCSVReader<EmployeeZ>.LoadText(csv, c)
                .WithFirstLineHeader()
                .ValidationMode(ChoObjectValidationMode.MemberLevel)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                //.ThrowAndStopOnMissingCSVColumn(false)
                .ThrowAndStopOnMissingField(false)
                //.Setup(s => s.RecordLoadError += (o, e) =>
                //{
                //    Console.WriteLine(e.Exception.Message);
                //    if (e.Exception is ChoMissingRecordFieldException)
                //    {
                //        e.Handled = true;
                //        var obj = e.Record as EmployeeZ;
                //        obj.Name = "XXX";
                //    }
                //    else
                //        e.Handled = true;
                //})
                //.ThrowAndStopOnMissingField()
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void IgnoreEmptyLineTest()
        {
            string csv = @"Id, Name

2, Mark";
            string expected = @"[
  {
    ""Id"": 2,
    ""Name"": ""Mark""
  }
]";

            var c = new ChoCSVRecordConfiguration<EmployeeZ>();
            c.Map(m => m.Id, m => m.DefaultValue(2));
            c.Map(m => m.Name, m => m.FallbackValue("XX"));

            using (var r = ChoCSVReader<EmployeeZ>.LoadText(csv, c)
                .WithFirstLineHeader()
                .Configure(c1 => c1.IgnoreEmptyLine = true)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .ThrowAndStopOnMissingField(false)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CSV2JSONWithArraySupport()
        {
            string csv = @"id;name;conditions/section_0;conditions/property_0;conditions/operator_0;conditions/value_0;conditions/section_1;conditions/property_1;conditions/operator_1;conditions/value_1
acf12d17-058e-451e-8449-60948055f6af;TEST1;Item;type;Equal;flight;Data;airlineCode;Equal;DL";

            string expected = @"[
  {
    ""id"": ""acf12d17-058e-451e-8449-60948055f6af"",
    ""name"": ""TEST1"",
    ""conditions"": [
      {
        ""section"": ""Item"",
        ""property"": ""type"",
        ""operator"": ""Equal"",
        ""value"": ""flight""
      },
      {
        ""section"": ""Data"",
        ""property"": ""airlineCode"",
        ""operator"": ""Equal"",
        ""value"": ""DL""
      }
    ]
  }
]";
            StringBuilder json = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithDelimiter(";")
                .WithFirstLineHeader()
                .NestedKeySeparator('/')
                //.AutoArrayDiscovery()
                .ArrayIndexSeparator('_')
                )
            {
                using (var w = new ChoJSONWriter(json))
                {
                    w.Write(r.Select(r1 => new
                    {
                        r1.id,
                        r1.name,
                        conditions = r1.conditions.Zip()
                    }));
                }
            }

            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void CSV2JSONWithArraySupport_AutoDiscovery()
        {
            string csv = @"id;name;conditions/section_0;conditions/property_0;conditions/operator_0;conditions/value_0;conditions/section_1;conditions/property_1;conditions/operator_1;conditions/value_1
acf12d17-058e-451e-8449-60948055f6af;TEST1;Item;type;Equal;flight;Data;airlineCode;Equal;DL";

            string expected = @"[
  {
    ""id"": ""acf12d17-058e-451e-8449-60948055f6af"",
    ""name"": ""TEST1"",
    ""conditions"": [
      {
        ""section"": ""Item"",
        ""property"": ""type"",
        ""operator"": ""Equal"",
        ""value"": ""flight""
      },
      {
        ""section"": ""Data"",
        ""property"": ""airlineCode"",
        ""operator"": ""Equal"",
        ""value"": ""DL""
      }
    ]
  }
]";
            StringBuilder json = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithDelimiter(";")
                .WithFirstLineHeader()
                .NestedKeySeparator('/')
                .AutoArrayDiscovery()
                .ArrayIndexSeparator('_')
                )
            {
                using (var w = new ChoJSONWriter(json))
                {
                    w.Write(r.Select(r1 => new
                    {
                        r1.id,
                        r1.name,
                        conditions = r1.conditions.Zip()
                    }));
                }
            }

            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        [DataContract]
        public class Employee2
        {
            public string Name { get; set; }
        }

        [DataContract]
        public class Manager2 : Employee2
        {
            [DataMember]
            public double Salary { get; set; }
            [DataMember]
            public string Department { get; set; }
        }
        [Test]
        public static void DictionaryTest()
        {
            string csv = @"Key,Name,Salary,Department
1,Tom,10000,IT
2,Tom,10000,IT";

            string expected = @"{
  ""1"": {
    ""Salary"": 10000.0,
    ""Department"": ""IT""
  },
  ""2"": {
    ""Salary"": 10000.0,
    ""Department"": ""IT""
  }
}";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                Dictionary<string, Manager2> recs = new Dictionary<string, Manager2>();
                foreach (var rec in r.ToDictionary(r1 => r1.Key, r1 => new Manager2
                {
                    Name = r1.Name,
                    Salary = ChoUtility.CastTo<double>(r1.Salary),
                    Department = r1.Department
                }))
                    recs.Add(rec.Key, rec.Value);
                //Console.WriteLine(rec.DumpAsJson());

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void JSON2CSV()
        {
            string json = @"{
  ""Serilog"": {
    ""MinimumLevel"": ""Debug"",
    ""WriteTo"": {
      ""Name"": ""RollingFile"",
      ""Args"": {
        ""formatter"": ""Serilog.Formatting.Json.JsonFormatter, Serilog"",
        ""pathFormat"": ""C:\\Logs\\logConfig-{Date}.txt""
      }
    }
  }
}";
            string expected = @"Serilog:MinimumLevel,Serilog:WriteTo:Name,Serilog:WriteTo:Args:formatter,Serilog:WriteTo:Args:pathFormat
Debug,RollingFile,Serilog.Formatting.Json.JsonFormatter, Serilog,C:\Logs\logConfig-{Date}.txt";
            StringBuilder csv = new StringBuilder();
            using (var r = ChoJSONReader.LoadText(json))
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Flatten(':').Dump());

                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .QuoteAllFields(false))
                {
                    w.Write(r.Select(rec => rec.Flatten(':')));
                }
                var actual = csv.ToString();
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CheckIfCSVFileIsEmpty1()
        {
            string csv = @"Id, Name";
            //1, Tom";

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader())
            {
                var IsCSVEmpty = r.Count() == 0;
                Console.WriteLine($"Is CSV file empty?: {IsCSVEmpty}");
                Assert.AreEqual(IsCSVEmpty, true);
            }
        }
        [Test]
        public static void CheckIfCSVFileIsEmpty2()
        {
            string csv = @"Id, Name";
            //1, Tom";

            bool IsCSVEmpty = true;
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .Setup(s => s.BeforeRecordLoad += (o, e) => IsCSVEmpty = false)
                )
            {
                foreach (var rec in r)
                {

                }
            }

            Console.WriteLine($"Is CSV file empty?: {IsCSVEmpty}");
            Assert.AreEqual(IsCSVEmpty, true);
        }

        [Test]
        public static void CheckIfCSVFileIsEmpty3()
        {
            string csv = @"Id, Name";

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .Configure(c => c.StateInitializer = cf =>
                {
                    cf.Context.IsCSVEmpty = true;
                })
                )
            {
                //Subscribe to before record event
                r.BeforeRecordLoad += (o, e) =>
                {
                    var reader = o as ChoReader;

                    //Set IsCSVEmpty to false, since this event gets called for each row
                    reader.Context.IsCSVEmpty = false;
                };

                //loop throw record to load csv file
                foreach (var rec in r)
                {

                }

                Console.WriteLine($"Is CSV file empty?: {r.Context.IsCSVEmpty}");
                Assert.AreEqual(r.Context.IsCSVEmpty, true);
            }
        }
        [Test]
        public static void CheckIfCSVFileIsNOTEmpty1()
        {
            string csv = @"Id, Name;
1, Tom";

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader())
            {
                var IsCSVEmpty = r.Count() == 0;
                Console.WriteLine($"Is CSV file empty?: {IsCSVEmpty}");
                Assert.AreEqual(IsCSVEmpty, false);
            }
        }
        [Test]
        public static void CheckIfCSVFileIsNOTEmpty2()
        {
            string csv = @"Id, Name;
1, Tom";

            bool IsCSVEmpty = true;
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .Setup(s => s.BeforeRecordLoad += (o, e) => IsCSVEmpty = false)
                )
            {
                foreach (var rec in r)
                {

                }
            }

            Console.WriteLine($"Is CSV file empty?: {IsCSVEmpty}");
            Assert.AreEqual(IsCSVEmpty, false);
        }

        [Test]
        public static void CheckIfCSVFileIsNOTEmpty3()
        {
            string csv = @"Id, Name;
1, Tom";

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                //Initialize IsCSVEmpty to true
                r.Context.IsCSVEmpty = true;

                //Subscribe to before record event
                r.BeforeRecordLoad += (o, e) =>
                {
                    var reader = o as ChoReader;

                    //Set IsCSVEmpty to false, since this event gets called for each row
                    reader.Context.IsCSVEmpty = false;
                };

                //loop throw record to load csv file
                foreach (var rec in r)
                {

                }

                Console.WriteLine($"Is CSV file empty?: {r.Context.IsCSVEmpty}");
                Assert.AreEqual(r.Context.IsCSVEmpty, false);
            }
        }
        [Test]
        public static void CombineCSVFields1()
        {
            string csv = @"Date,Time
""8/3/2020"",""14:58:48""
""8/3/2020"",""14:58:48""
""8/3/2020"",""14:58:48""
""8/3/2020"",""16:41:10""
""8/3/2020"",""16:41:13""";

            string expected = @"[
  {
    ""Date"": ""8/3/2020"",
    ""Time"": ""14:58:48"",
    ""DateTime"": ""2020-08-03T14:58:48""
  },
  {
    ""Date"": ""8/3/2020"",
    ""Time"": ""14:58:48"",
    ""DateTime"": ""2020-08-03T14:58:48""
  },
  {
    ""Date"": ""8/3/2020"",
    ""Time"": ""14:58:48"",
    ""DateTime"": ""2020-08-03T14:58:48""
  },
  {
    ""Date"": ""8/3/2020"",
    ""Time"": ""16:41:10"",
    ""DateTime"": ""2020-08-03T16:41:10""
  },
  {
    ""Date"": ""8/3/2020"",
    ""Time"": ""16:41:13"",
    ""DateTime"": ""2020-08-03T16:41:13""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .QuoteAllFields()
                .WithFirstLineHeader()
                .WithField("Date")
                .WithField("Time")
                .WithField("DateTime", valueSelector: o =>
                {
                    var da = o as dynamic;
                    return Convert.ToDateTime($"{da.Date} {da.Time}");
                }, optional: true)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CombineCSVFields2()
        {
            string csv = @"Date,Time
""8/3/2020"",""14:58:48""
""8/3/2020"",""14:58:48""
""8/3/2020"",""14:58:48""
""8/3/2020"",""16:41:10""
""8/3/2020"",""16:41:13""";

            string expected = @"[
  {
    ""Date"": ""8/3/2020"",
    ""Time"": ""14:58:48"",
    ""DateTime"": ""2020-08-03T14:58:48""
  },
  {
    ""Date"": ""8/3/2020"",
    ""Time"": ""14:58:48"",
    ""DateTime"": ""2020-08-03T14:58:48""
  },
  {
    ""Date"": ""8/3/2020"",
    ""Time"": ""14:58:48"",
    ""DateTime"": ""2020-08-03T14:58:48""
  },
  {
    ""Date"": ""8/3/2020"",
    ""Time"": ""16:41:10"",
    ""DateTime"": ""2020-08-03T16:41:10""
  },
  {
    ""Date"": ""8/3/2020"",
    ""Time"": ""16:41:13"",
    ""DateTime"": ""2020-08-03T16:41:13""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .QuoteAllFields()
                .WithFirstLineHeader()
                .WithField("Date")
                .WithField("Time")
                //.ThrowAndStopOnMissingCSVColumn(false)
                .WithField("DateTime", valueSelector: o =>
                {
                    var da = o as dynamic;
                    return Convert.ToDateTime($"{da.Date} {da.Time}");
                })
                .ThrowAndStopOnMissingField(false)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CombineCSVFields3()
        {
            string csv = @"Date,Time
""8/3/2020"",""14:58:48""
""8/3/2020"",""14:58:48""
""8/3/2020"",""14:58:48""
""8/3/2020"",""16:41:10""
""8/3/2020"",""16:41:13""";

            string expected = @"[
  ""2020-08-03T14:58:48"",
  ""2020-08-03T14:58:48"",
  ""2020-08-03T14:58:48"",
  ""2020-08-03T16:41:10"",
  ""2020-08-03T16:41:13""
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .QuoteAllFields()
                .WithFirstLineHeader()
                )
            {
                //foreach (var rec in r.Select(da => Convert.ToDateTime($"{da.Date} {da.Time}")))
                //    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(r.Select(da => Convert.ToDateTime($"{da.Date} {da.Time}")), Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void DuplicateFieldsTest()
        {
            string csv = @"Key,Name,Salary,Name
1,Tom,10000,IT
2,Tom,10000,IT";

            string expected = @"[
  {
    ""Key"": ""1"",
    ""Name"": ""Tom"",
    ""Salary"": ""10000"",
    ""Dept"": ""IT""
  },
  {
    ""Key"": ""2"",
    ""Name"": ""Tom"",
    ""Salary"": ""10000"",
    ""Dept"": ""IT""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader(true)
                .WithFields("Key", "Name", "Salary", "Dept")
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CSV2Xml()
        {
            string csv = @"ID;name;addresses;street;streetNR
1;peter;;streetTest;58784
1;peter;;street2;04512";

            string expected = @"<XElement xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <ID>1</ID>
  <Name>peter</Name>
  <Address>
    <Street>streetTest</Street>
    <plz>58784</plz>
  </Address>
  <Address>
    <Street>street2</Street>
    <plz>04512</plz>
  </Address>
</XElement>";

            StringBuilder xml = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithDelimiter(";")
                )
            {
                var items = r.GroupBy(r1 => r1.ID).Select(r2 => new
                {
                    ID = r2.Key,
                    Name = r2.First().name,
                    Address = r2.Select(r3 => new { Street = r3.street, plz = r3.streetNR }).ToArray()
                }).ToArray();

                using (var w = new ChoXmlWriter(xml)
                    .IgnoreRootName()
                    )
                    w.Write(items);

                //foreach (var rec in r.GroupBy(r1 => r1.ID).Select(r2 => new
                //{
                //    ID = r2.Key,
                //    Name = r2.First().name,
                //    Address = r2.Select(r3 => new { r3.street, plz = r3.streetNR }).ToArray()
                //}))
                //    Console.WriteLine(rec.Dump());
            }

            Console.WriteLine(xml.ToString());

            var actual = xml.ToString();
            Assert.AreEqual(expected, actual);
        }

        [ChoCSVRecordObject(delimiter: ",", ErrorMode = ChoErrorMode.ThrowAndStop)]
        [ChoCSVFileHeader()]
        public class Model
        {
            [ChoCSVRecordField(FieldName = "FirstHeader")]
            public string FirstHeader { get; set; }

            [ChoCSVRecordField(FieldName = "Second header")]
            public string SecondHeader { get; set; }
        }

        [Test]
        public static void LoadCSVByFieldNames()
        {
            string csv = @"FirstHeader,Second header
1,Tom
2,Mark";
            string expected = @"[
  {
    ""FirstHeader"": ""1"",
    ""SecondHeader"": ""Tom""
  },
  {
    ""FirstHeader"": ""2"",
    ""SecondHeader"": ""Mark""
  }
]";
            using (var r = ChoCSVReader<Model>.LoadText(csv)
                .WithFirstLineHeader(true)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void CSV2JSON3()
        {
            string csv = @"id,personal_information/empid, personal_information/name
1,2,Edd";

            string expected = @"[
  {
    ""id"": 1,
    ""personal_information"": {
      ""empid"": 2,
      ""name"": ""Edd""
    }
  }
]";
            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter(json))
            {
                using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader()
                    .Configure(c => c.NestedKeySeparator = '/')
                    .WithMaxScanRows(1)
                    )
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class EmployeeA
        {
            public int id { get; set; }
            public string Name { get; set; }
            public EmpStatus Status { get; set; }

        }

        public enum EmpStatus { Active, Inactive };
        [Test]
        public static void CaptureError1()
        {
            string csv = @"Id, Name, Status
1, Tom, Active
2, Mark, Active1";

            string expected = @"";
            using (var r = ChoCSVReader<EmployeeA>.LoadText(csv)
                .WithFirstLineHeader()
                .ErrorMode(ChoErrorMode.ThrowAndStop)
                //.Setup(s => s.RecordLoadError += (o, e) =>
                //{
                //    Console.WriteLine(e.Exception.Message);
                //    //if (e.Exception is ChoMissingRecordFieldException)
                //    //    e.Handled = false;
                //    //else
                //    e.Handled = true;
                //})
                )
            {

                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                Assert.Catch<ChoReaderException>(() =>
                {
                    var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                    Assert.AreEqual(expected, actual);
                }
                );
            }
        }
        [Test]
        public static void CaptureError2()
        {
            string csv = @"Id, Name, Status
1, Tom, Active
2, Mark, Active1";

            string expected = @"[
  {
    ""id"": 1,
    ""Name"": ""Tom"",
    ""Status"": 0
  },
  {
    ""id"": 2,
    ""Name"": ""Mark"",
    ""Status"": 0
  }
]";
            using (var r = ChoCSVReader<EmployeeA>.LoadText(csv)
                .WithFirstLineHeader()
                .ErrorMode(ChoErrorMode.ReportAndContinue)
                .Setup(s => s.RecordLoadError += (o, e) =>
                {
                    Console.WriteLine(e.Exception.Message);
                    //if (e.Exception is ChoMissingRecordFieldException)
                    //    e.Handled = false;
                    //else
                    e.Handled = true;
                })
                )
            {

                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Issue123()
        {
            string csv = @"ERROR_CODE|ERROR_DESCRIPTION|IS_FILE_ERROR|ERROR_SEVERITY|FILE_NAME|
F1004|File is a duplicate|TRUE|ERROR|TEST_VISITS_IA_270084601_20201202192520.csv|";

            string expected = @"[
  {
    ""ERROR_CODE"": ""F1004"",
    ""ERROR_DESCRIPTION"": ""File is a duplicate"",
    ""IS_FILE_ERROR"": ""TRUE"",
    ""ERROR_SEVERITY"": ""ERROR"",
    ""FILE_NAME"": ""TEST_VISITS_IA_270084601_20201202192520.csv""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .QuoteAllFields().WithFirstLineHeader(false).WithDelimiter("|")
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true)
                )
            {
                //foreach (var rec in r)
                //{
                //    //rec.Remove("_Column1");
                //    Console.WriteLine(rec.Dump());
                //}
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }


        public class EmployeeB
        {
            public int id { get; set; }
            public string Name { get; set; }
            public double Salary { get; set; }

        }
        [Test]
        public static void DoubleValueWithExponentFormat()
        {
            string csv = @"Id, Name, Salary
1, Tom, ""3,27E+11""
2, Mark, ""10,27E+11""";

            string expected = @"[
  {
    ""id"": 1,
    ""Name"": ""Tom"",
    ""Salary"": 32700000000000.0
  },
  {
    ""id"": 2,
    ""Name"": ""Mark"",
    ""Salary"": 102700000000000.0
  }
]";
            using (var r = ChoCSVReader<EmployeeB>.LoadText(csv)
                .WithFirstLineHeader()
                .QuoteAllFields()
                .ErrorMode(ChoErrorMode.ThrowAndStop)
                //.Setup(s => s.RecordLoadError += (o, e) =>
                //{
                //    Console.WriteLine(e.Exception.Message);
                //    //if (e.Exception is ChoMissingRecordFieldException)
                //    //    e.Handled = false;
                //    //else
                //    e.Handled = true;
                //})
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Issue135Rec
        {
            [ChoCSVRecordField(FieldName = "split Payment Data", QuoteField = false)]
            public string SplitPaymentData { get; set; }
        }
        [Test]
        public static void Issue135()
        {
            string csv = @"SomeField1, Split Payment Data
value1,""{""""split.amount"""":""""1794"""",""""split.currencyCode"""":""""USD"""",""""split.nrOfItems"""":""""1""""}""
";
            string expected = @"[
  {
    ""SplitPaymentData"": ""{\""split.amount\"":\""1794\"",\""split.currencyCode\"":\""USD\"",\""split.nrOfItems\"":\""1\""}""
  }
]";
            using (var r = ChoCSVReader<Issue135Rec>.LoadText(csv)
                .WithFirstLineHeader()
                .QuoteAllFields(true)
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        public class VisitExport
        {
            public int? Count { get; set; }
            public string CustomerName { get; set; }
            public string CustomerAddress { get; set; }
        }
        [Test]
        public static void CheckCSVFileValid()
        {
            string csv = @"Count, CustomerName, CustomerAddress1
1, Mark, 1 Main St";

            string expected = null;
            using (var r = ChoCSVReader<VisitExport>.LoadText(csv)
                .ThrowAndStopOnMissingField()
                .ColumnCountStrict()
                .WithFirstLineHeader()
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                Assert.Catch<ChoMissingCSVColumnException>(() =>
                {
                    var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                    Assert.AreEqual(expected, actual);
                });
            }
        }

        public class Channel
        {
            [JsonProperty("id")]
            public int Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("alias")]
            public string Alias { get; set; }

            [JsonProperty("value")]
            public double Value { get; set; }

            [JsonProperty("status")]
            public int Status { get; set; }

            [JsonProperty("valid")]
            public bool Valid { get; set; }

            [JsonProperty("description")]
            //[DisplayName("description")]
            public string Description { get; set; }
        }

        public class Datum
        {
            [JsonProperty("datetime")]
            public string Datetime { get; set; }

            [JsonProperty("channels")]
            [Range(0, 3)]
            public Channel[] Channels { get; set; }
        }

        public class StationDetailsCall : Container
        {
            [JsonProperty("stationId")]
            public int StationId { get; set; }

            [JsonProperty("data")]
            [Range(0, 1)]
            public Datum[] Data { get; set; }
        }
        [Test]
        public static void Json2CSV()
        {
            StationDetailsCall station = new StationDetailsCall();
            station.StationId = 1;
            station.Data = new Datum[] {
     new Datum() { Channels = new Channel[] { new Channel() { Id = 1, Name = "name1" }, new Channel() { Id = 2, Name = "name2" } }, Datetime = new DateTime(1000).ToString() },
     new Datum() { Channels = new Channel[] { new Channel() { Id = 3, Name = "name3" }, new Channel() { Id = 4, Name = "name4" } }, Datetime = new DateTime(1000).ToString() },
};


            //var rec = ChoType.GetDeclaringRecord("Data.Channels.Id", station, 0);
            //Console.WriteLine(rec.Dump());
            //return;

            string expected = @"StationId,Data.Datetime_0,Data.Channels_0.Id_0,Data.Channels_0.Name_0,Data.Channels_0.Alias_0,Data.Channels_0.Value_0,Data.Channels_0.Status_0,Data.Channels_0.Valid_0,Data.Channels_0.Description_0,Data.Channels_0.Id_1,Data.Channels_0.Name_1,Data.Channels_0.Alias_1,Data.Channels_0.Value_1,Data.Channels_0.Status_1,Data.Channels_0.Valid_1,Data.Channels_0.Description_1,Data.Channels_0.Id_2,Data.Channels_0.Name_2,Data.Channels_0.Alias_2,Data.Channels_0.Value_2,Data.Channels_0.Status_2,Data.Channels_0.Valid_2,Data.Channels_0.Description_2,Data.Channels_0.Id_3,Data.Channels_0.Name_3,Data.Channels_0.Alias_3,Data.Channels_0.Value_3,Data.Channels_0.Status_3,Data.Channels_0.Valid_3,Data.Channels_0.Description_3,Data.Datetime_1,Data.Channels_1.Id_0,Data.Channels_1.Name_0,Data.Channels_1.Alias_0,Data.Channels_1.Value_0,Data.Channels_1.Status_0,Data.Channels_1.Valid_0,Data.Channels_1.Description_0,Data.Channels_1.Id_1,Data.Channels_1.Name_1,Data.Channels_1.Alias_1,Data.Channels_1.Value_1,Data.Channels_1.Status_1,Data.Channels_1.Valid_1,Data.Channels_1.Description_1,Data.Channels_1.Id_2,Data.Channels_1.Name_2,Data.Channels_1.Alias_2,Data.Channels_1.Value_2,Data.Channels_1.Status_2,Data.Channels_1.Valid_2,Data.Channels_1.Description_2,Data.Channels_1.Id_3,Data.Channels_1.Name_3,Data.Channels_1.Alias_3,Data.Channels_1.Value_3,Data.Channels_1.Status_3,Data.Channels_1.Valid_3,Data.Channels_1.Description_3
1,1/1/0001 12:00:00 AM,1,name1,,0,0,False,,2,name2,,0,0,False,,,,,,,False,,,,,,,False,,1/1/0001 12:00:00 AM,1,name1,,0,0,False,,2,name2,,0,0,False,,,,,,,False,,,,,,,False,";
            var csv = new StringBuilder();
            string json = JsonConvert.SerializeObject(station);
            using (var r = ChoJSONReader.LoadText(json))
            {
                using (var w = new ChoCSVWriter<StationDetailsCall>(csv)
                    .WithFirstLineHeader()
                    .UseNestedKeyFormat(true)
                    .ThrowAndStopOnMissingField(false)
                    .IgnoreField(f => f.Components.Count)
                    )
                {
                    w.Write(station);
                }
            }
            Console.WriteLine(csv.ToString());

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Issue151()
        {
            string csv = @"Id, Name
1, Tom
2, Mark
3, Smith";

            string expected = @"[
  {
    ""Id"": ""1"",
    ""Name"": ""Tom""
  },
  {
    ""Id"": ""2"",
    ""Name"": ""Mark""
  },
  {
    ""Id"": ""3"",
    ""Name"": ""Smith""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(5)
                .ErrorMode(ChoErrorMode.ReportAndContinue)
                )
            {
                var dr = r.AsDataReader();

                var dt = new DataTable();
                dt.Load(dr);
                Console.WriteLine(dt.Dump());

                var actual = JsonConvert.SerializeObject(dt, Formatting.Indented);
                Assert.AreEqual(expected, actual);
                return;

                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        [Test]
        public static void CalcField()
        {
            string csv = @"Id, FirstName, LastName
1, Tom, Smith
2, Mark, Hartigan
3, Smith, Paul";

            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""Tom Smith""
  },
  {
    ""Id"": 2,
    ""Name"": ""Mark Hartigan""
  },
  {
    ""Id"": 3,
    ""Name"": ""Smith Paul""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(5)
                .WithField("Id")
                .WithField("Name", valueSelector: o => o.FirstName + o.LastName)
                //.ThrowAndStopOnMissingCSVColumn(false)
                .ThrowAndStopOnMissingField(false)
                .ErrorMode(ChoErrorMode.ReportAndContinue)
                )
            {
                //var dr = r.AsDataReader();

                //var dt = new DataTable();
                //dt.Load(dr);
                //Console.WriteLine(dt.Dump());
                //return;

                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());

                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class EyeExcitation
        {
            public int Dt { get; set; }
            public double[] Leds { get; set; } = new double[6];
        }
        [Test]
        public static void CSVToArrayColumn()
        {
            string csv = @"Dt, LED1 R, LED2 R, LED3 R
0, 1, 2, 3
1, 0.5, 0.7, 5";

            string expected = @"[
  {
    ""Dt"": 0,
    ""Leds"": [
      1.0
    ]
  },
  {
    ""Dt"": 1,
    ""Leds"": [
      0.5
    ]
  }
]";
            using (var r = ChoCSVReader<EyeExcitation>.LoadText(csv)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                .WithField(f => f.Dt)
                .WithField(f => f.Leds, valueSelector: v => new double[] { v.GetValue<double>("LED1 R") })
                )
            {
                //r.Print();
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void PositionNeutralCSVLoad()
        {
            string csv1 = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";

            string csv2 = @"Name, Id, City
Tom, 1, NY
Mark, 2, NJ
Lou, 3, FL
Smith, 4, PA
Raj, 5, DC
";
            string expected = @"[
  {
    ""Id"": ""1"",
    ""Name"": ""Tom"",
    ""City"": ""NY""
  },
  {
    ""Id"": ""2"",
    ""Name"": ""Mark"",
    ""City"": ""NJ""
  },
  {
    ""Id"": ""3"",
    ""Name"": ""Lou"",
    ""City"": ""FL""
  },
  {
    ""Id"": ""4"",
    ""Name"": ""Smith"",
    ""City"": ""PA""
  },
  {
    ""Id"": ""5"",
    ""Name"": ""Raj"",
    ""City"": ""DC""
  }
]";

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id"));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name"));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("City"));
            config.WithFirstLineHeader();

            using (var r = ChoCSVReader.LoadText(csv2, config))
            {
                //r.Print();
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void BackslashTest1()
        {
            string csv = @"c1,c2,c3
A,""A\B\"",C
A,B,C
A, B, C";

            string expected = @"[
  {
    ""c1"": ""A"",
    ""c2"": ""A\\B\\"",
    ""c3"": ""C""
  },
  {
    ""c1"": ""A"",
    ""c2"": ""B"",
    ""c3"": ""C""
  },
  {
    ""c1"": ""A"",
    ""c2"": ""B"",
    ""c3"": ""C""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .QuoteAllFields()
                )
            {
                //using (var w = new ChoJSONWriter(Console.Out)
                //    )
                //    w.Write(r);
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);

            }
        }

        [Test]
        public static void BackslashTest()
        {
            string csv = @"column1,column2,column3
Row1Column0,""Row1\Column2"",Row1Column3
Row2Column0,""Row2
Column2"",Row2Column3
Row3Column0,Row3Column2,Row3Column3";

            string expected = @"[
  {
    ""column1"": ""Row1Column0"",
    ""column2"": ""Row1\\Column2"",
    ""column3"": ""Row1Column3""
  },
  {
    ""column1"": ""Row2Column0"",
    ""column2"": ""Row2\r\nColumn2"",
    ""column3"": ""Row2Column3""
  },
  {
    ""column1"": ""Row3Column0"",
    ""column2"": ""Row3Column2"",
    ""column3"": ""Row3Column3""
  }
]";
            StringBuilder json = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .MayContainEOLInData(true)
                .ThrowAndStopOnMissingField(false)
                .MayHaveQuotedFields()
                //.Configure(c => c.QuoteEscapeChar = '\\')
                )
            {
                //r.Print();
                //return;
                using (var w = new ChoJSONWriter(json)
                    //.Configure(c => c.DefaultArrayHandling = false)
                    .UseJsonSerialization()
                    )
                    w.Write(r);
            }

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }
        public class ChoBooleanCustomConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (targetType != typeof(bool))
                    return value;

                if (value is string)
                {
                    string txt = value as string;
                    txt = txt.NTrim();

                    if (txt.IsNull())
                        return false;

                    ChoBooleanFormatSpec booleanFormat = parameter.GetValueAt(0, ChoBooleanFormatSpec.Custom);
                    switch (booleanFormat)
                    {
                        case ChoBooleanFormatSpec.YOrN:
                            if (txt.Length == 1)
                                return txt[0] == 'Y' || txt[0] == 'y' ? true : false;
                            else
                                return false;
                        case ChoBooleanFormatSpec.TOrF:
                            if (txt.Length == 1)
                                return txt[0] == 'T' || txt[0] == 't' ? true : false;
                            else
                                return false;
                        case ChoBooleanFormatSpec.TrueOrFalse:
                            return String.Compare(txt, "true", true) == 0 ? true : false;
                        case ChoBooleanFormatSpec.YesOrNo:
                            return String.Compare(txt, "yes", true) == 0 ? true : false;
                        case ChoBooleanFormatSpec.ZeroOrOne:
                            if (txt.Length == 1)
                                return txt[0] == '1' ? true : false;
                            else
                                return false;
                        default:
                            string boolTxt = parameter.GetValueAt<string>(0);
                            if (boolTxt.IsNullOrWhiteSpace())
                            {
                                if (txt.Length == 1)
                                {
                                    return txt[0] == 'Y' || txt[0] == 'y' || txt[0] == '1' ? true : false;
                                }
                                else
                                {
                                    return String.Compare(txt, "true", true) == 0 || String.Compare(txt, "yes", true) == 0 ? true : false;
                                }
                            }
                            else
                                return String.Compare(txt, boolTxt, true) == 0 ? true : false;
                    }
                }
                else
                    return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (targetType == typeof(string))
                {
                    if (value == null)
                        value = false;

                    if (value is bool)
                    {
                        bool boolValue = (bool)value;

                        ChoBooleanFormatSpec booleanFormat = parameter.GetValueAt(0, ChoTypeConverterFormatSpec.Instance.BooleanFormat);
                        switch (booleanFormat)
                        {
                            case ChoBooleanFormatSpec.TOrF:
                                return boolValue ? "T" : "F";
                            case ChoBooleanFormatSpec.YOrN:
                                return boolValue ? "Y" : "N";
                            case ChoBooleanFormatSpec.TrueOrFalse:
                                return boolValue ? "True" : "False";
                            case ChoBooleanFormatSpec.YesOrNo:
                                return boolValue ? "Yes" : "No";
                            default:
                                return boolValue ? "1" : "0";
                        }
                    }
                }

                return value;
            }
        }
        [Test]
        public static void ConvertDecimal()
        {
            string csv = @"Title,Amount,NHS,Reference,GoCardless ID,email,surname,firstname,Full Name,DOB,Age,Right Lens,Left Lens,RightLensMonthlyAmount,LeftLensMonthlyAmount,LensMonthlyAmount,FeeMonthlyAmount,VAT Basis,LensBespokePrice,CareOnly,Notes
Mrs,24.3,N,100247,CUXXX,email@gmail.com,User,Test,Test User,17/09/1957,64,DAILIES® AquaComfort PLUS 30 Pack,DAILIES® AquaComfort PLUS 30 Pack,16.5,16.5,33,6.35,,,,";

            string expected = @"[
  {
    ""Title"": ""Mrs"",
    ""Amount"": 24.3,
    ""NHS"": false,
    ""Reference"": 100247,
    ""GoCardless ID"": ""CUXXX"",
    ""email"": ""email@gmail.com"",
    ""surname"": ""User"",
    ""firstname"": ""Test"",
    ""Full Name"": ""Test User"",
    ""DOB"": ""1957-09-17T00:00:00"",
    ""Age"": 64,
    ""Right Lens"": ""DAILIES® AquaComfort PLUS 30 Pack"",
    ""Left Lens"": ""DAILIES® AquaComfort PLUS 30 Pack"",
    ""RightLensMonthlyAmount"": 16.5,
    ""LeftLensMonthlyAmount"": 16.5,
    ""LensMonthlyAmount"": 33,
    ""FeeMonthlyAmount"": 6.35,
    ""VAT Basis"": null,
    ""LensBespokePrice"": null,
    ""CareOnly"": null,
    ""Notes"": null
  }
]";
            //ChoTypeDescriptor.RegisterTypeConvertersForType(typeof(Boolean), new object[] { new ChoBooleanCustomConverter() });
            //ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "dd/MM/yyyy";
            //ChoFieldTypeAssessor.Instance.FieldTypeAssessor += (t, fs, ci) =>
            //{
            //    if (t.ToNString().Length == 1 && (t.ToNString() == "Y" || t.ToNString() == "N"))
            //    {
            //        return typeof(bool);
            //    }
            //    return null;
            //};

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithMaxScanRows(2)
                .Configure(c => c.RegisterTypeConverterForType<Boolean>(new ChoBooleanCustomConverter()))
                .TypeConverterFormatSpec(tf => tf.DateTimeFormat = "dd/MM/yyyy")
                .Configure(c => c.FieldTypeAssessor = new ChoFieldTypeAssessor((k, t, fs, ci) =>
                {
                    if (t.ToNString().Length == 1 && (t.ToNString() == "Y" || t.ToNString() == "N"))
                    {
                        return typeof(bool);
                    }
                    return null;
                }))
                )
            {
                //r.Print();
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void ValidateSample()
        {
            string csv = @"Id, Name, City
1, Tom, Austin
2, Mark, New York";

            string expected = null;
            using (var r = ChoCSVReader<Emp>.LoadText(csv)
                .WithFirstLineHeader()
                .ErrorMode(ChoErrorMode.ReportAndContinue)
                .Configure(c => c.ObjectValidationMode = ChoObjectValidationMode.MemberLevel)
                .WithField(f => f.Id, m => m.Value.Validator = v => false)
                )
            {
                //r.Print();

                Assert.Catch<ValidationException>(() =>
                {
                    var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                    Assert.AreEqual(expected, actual);
                });
            }
        }
        [Test]
        public static void MergeDifferentCSV()
        {
            string csv1 = @"col1, col2, col3
val1, val2, val3
val11, val21, val31";

            string csv2 = @"col1, col3
val4, val5
val41, val51";

            string csv3 = @"col1, col4
val6, val7
val61, val71";

            string expected = @"col1,col2,col3,col4
val1,val2,val3,
val11,val21,val31,
val4,,val5,
val41,,val51,
val6,,,val7
val61,,,val71";

            ChoCSVRecordConfiguration config = null;
            List<object> items = new List<object>();
            using (var r1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader())
            {
                using (var r2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader())
                {
                    using (var r3 = ChoCSVReader.LoadText(csv3).WithFirstLineHeader())
                    {
                        items.Add(r1.First());
                        items.Add(r2.First());
                        items.Add(r3.First());
                    }
                }
            }

            StringBuilder csv = new StringBuilder();
            using (var w = new ChoCSVWriter(csv)
               .WithFirstLineHeader()
               .WithMaxScanRows(5)
               .ThrowAndStopOnMissingField(false)
              )
            {
                w.Write(items);

                //Capture configuration for later use to merge CSV files
                config = w.Configuration;
            }

            StringBuilder csvOut = new StringBuilder();
            using (var r1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader())
            {
                using (var r2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader())
                {
                    using (var r3 = ChoCSVReader.LoadText(csv3).WithFirstLineHeader())
                    {

                        using (var w = new ChoCSVWriter(csvOut, config)
                               .WithFirstLineHeader()
                              )
                        {
                            w.Write(Enumerable.Concat(r1, r2).Concat(r3));
                        }
                    }
                }
            }
            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }

        [ChoCSVFileHeader]
        [ChoCSVRecordObject(ObjectValidationMode = ChoObjectValidationMode.ObjectLevel)]
        public class EmployeeRecDefaultValueTest
        {
            [Required]
            public int? Id
            {
                get;
                set;
            }
            [DefaultValue("XXXX")]
            public string Name
            {
                get;
                set;
            }

            public override string ToString()
            {
                return $"{Id}. {Name}.";
            }
        }
        [Test]
        public static void DefaultValueTest1()
        {
            string csv = @"Id,Name
1,
2,Carl
3,Mark";
            string expected = @"[
  {
    ""Id"": 1,
    ""Name"": ""XXXX""
  },
  {
    ""Id"": 2,
    ""Name"": ""Carl""
  },
  {
    ""Id"": 3,
    ""Name"": ""Mark""
  }
]";

            using (var r = ChoCSVReader<EmployeeRecDefaultValueTest>.LoadText(csv)
                     .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                     .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Any)
                    )
            {
                //Console.WriteLine($"Id: {rec.Id}");
                //Console.WriteLine($"Name: {rec.Name}");

                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class Article
        {
            public string ArticleNumber { get; set; }
            [Range(1, 7)]
            public int[] Shops { get; set; }
        }

        [Test]
        public static void CustomFieldSerialization()
        {
            string csv = @"ArticleNumber;Shop1;Shop2;Shop3;Shop4;Shop5;Shop6;Shop7
123455;50;51;52;53;54;55;56";

            string expected = @"[
  {
    ""ArticleNumber"": ""123455"",
    ""Shops"": [
      50,
      51,
      52,
      53,
      54,
      55,
      56
    ]
  }
]";

            StringBuilder csvOut = new StringBuilder();
            using (var r = ChoCSVReader<Article>.LoadText(csv)
                .WithFirstLineHeader()
                .WithDelimiter(";")
                .WithField(f => f.Shops, valueSelector: o => ((IDictionary<string, object>)o).Where(kvp => kvp.Key.StartsWith("Shop")).Select(kvp => kvp.Value).ToArray())
                .ThrowAndStopOnMissingField(false)
                )
            {
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);

                //r.Print();
                //return;
                //using (var w = new ChoCSVWriter<Article>(csvOut)
                //    .WithFirstLineHeader()
                //    .Configure(c => c.ArrayIndexSeparator = '\0')
                //    //.Setup(s => s.BeforeRecordFieldWrite += (o, e) =>
                //    //{
                //    //    if (e.PropertyName.StartsWith("Shop"))
                //    //        e.Source = 1;
                //    //})
                //    )
                //{
                //    w.Write(r);
                //}
            }
        }

        [Test]
        public static void CustomFieldSerialization_1()
        {
            string csv = @"ArticleNumber;Shop1;Shop2;Shop3;Shop4;Shop5;Shop6;Shop7
123455;50;51;52;53;54;55;56";

            string expected = @"ArticleNumber,Shops1,Shops2,Shops3,Shops4,Shops5,Shops6,Shops7
123455,51,52,53,54,55,56,";

            StringBuilder csvOut = new StringBuilder();
            using (var r = ChoCSVReader<Article>.LoadText(csv)
                .WithFirstLineHeader()
                .WithDelimiter(";")
                .WithField(f => f.Shops, valueSelector: o => ((IDictionary<string, object>)o).Where(kvp => kvp.Key.StartsWith("Shop")).Select(kvp => kvp.Value).ToArray())
                .ThrowAndStopOnMissingField(false)
                )
            {
                //r.Print();
                //return;
                using (var w = new ChoCSVWriter<Article>(csvOut)
                    .WithFirstLineHeader()
                    .Configure(c => c.ArrayIndexSeparator = '\0')
                    //.Setup(s => s.BeforeRecordFieldWrite += (o, e) =>
                    //{
                    //    if (e.PropertyName.StartsWith("Shop"))
                    //        e.Source = 1;
                    //})
                    )
                {
                    w.Write(r);
                }
            }
            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void DynamicTest()
        {
            string csv = @"Id,Name
1,Tom
2,Carl
3,Mark";
            string expected = @"[
  {
    ""Id"": ""1"",
    ""Name"": ""Tom""
  },
  {
    ""Id"": ""2"",
    ""Name"": ""Carl""
  },
  {
    ""Id"": ""3"",
    ""Name"": ""Mark""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader())
            {
                //var emp = new Emp()
                //{
                //    Id = rec.Id,
                //    Name = rec.Name,
                //};
                //Console.WriteLine($"Id: {emp.Id}");
                //Console.WriteLine($"Name: {emp.Name}");
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Issue168()
        {
            string csv1 = @"Header1,Header2,Header3
""Value1"",""Val
ue2"",""Value3""";
            string csv2 = @"""Header1"",""Header2"",""Header3""
""Value1"",""Val
ue2"",""Value3""";
            string csv3 = @"Header1,Header2,Header3
Value1,""Val
ue2"",Value3";

            string expected = @"[
  {
    ""Header1"": ""Value1"",
    ""Header2"": ""Val\r\nue2"",
    ""Header3"": ""Value3""
  }
]";

            using (var r = ChoCSVReader.LoadText(csv1)
                .WithFirstLineHeader()
                .MayContainEOLInData(true)
                .QuoteAllFields())
            {
                //r.Print();
                var actual1 = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual1);
            }
            using (var r = ChoCSVReader.LoadText(csv2)
                .WithFirstLineHeader()
                .MayContainEOLInData(true)
                .QuoteAllFields())
            {
                //r.Print();
                var actual2 = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual2);
            }
            using (var r = ChoCSVReader.LoadText(csv3)
                .WithFirstLineHeader()
                .MayContainEOLInData(true)
                .QuoteAllFields())
            {
                //r.Print();
                var actual3 = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual3);
            }
        }

        public class ScientificNotationdecimal
        {
            [ChoTypeConverterParams(Parameters = NumberStyles.Number | NumberStyles.AllowExponent)]
            public double a { get; set; }
            [ChoTypeConverterParams(Parameters = NumberStyles.Number | NumberStyles.AllowExponent)]
            public double b { get; set; }
            public long RN { get; set; }
            public DateTime TimeStamp { get; set; }
        }
        [Test]
        public static void ScientificNotationdecimals()
        {
            ChoTypeConverterFormatSpec.Instance.LongNumberStyle = NumberStyles.Number | NumberStyles.AllowExponent;
            string csv = @"a,b
1.2,3.4
1.2e-05,7.8";

            string expected = @"[
  {
    ""RowNo"": 1,
    ""Rec"": {
      ""a"": ""1.2"",
      ""b"": ""3.4"",
      ""RN"": 1,
      ""TimeStamp"": ""2023-01-01T00:00:00""
    }
  },
  {
    ""RowNo"": 2,
    ""Rec"": {
      ""a"": ""1.2e-05"",
      ""b"": ""7.8"",
      ""RN"": 2,
      ""TimeStamp"": ""2023-01-01T00:00:00""
    }
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                )
            {
                r.WithField("a").WithField("b").WithField("RN", () => r.RecordNumber)
                    .WithField("TimeStamp", () => new DateTime(2023, 1, 1));

                //r.Print();
                var recs = r.Select(s => new { RowNo = r.RecordNumber, Rec = s }).ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void ScientificNotationdecimals1()
        {
            ChoTypeConverterFormatSpec.Instance.LongNumberStyle = NumberStyles.Number | NumberStyles.AllowExponent;
            string csv = @"a,b
1.2,3.4
1.2e-05,7.8";

            string expected = @"[
  {
    ""RowNo"": 1,
    ""Rec"": {
      ""a"": 1.2,
      ""b"": 3.4,
      ""RN"": 1,
      ""TimeStamp"": ""2023-01-01T00:00:00""
    }
  },
  {
    ""RowNo"": 2,
    ""Rec"": {
      ""a"": 1.2E-05,
      ""b"": 7.8,
      ""RN"": 2,
      ""TimeStamp"": ""2023-01-01T00:00:00""
    }
  }
]";
            using (var r = ChoCSVReader<ScientificNotationdecimal>.LoadText(csv)
                .WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false)
                )
            {
                r.WithField(f => f.RN, () => r.RecordNumber);
                r.WithField(f => f.TimeStamp, () => new DateTime(2023, 1, 1));

                //r.Print();
                var recs = r.Select(s => new { RowNo = r.RecordNumber, Rec = s }).ToArray();
                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class EmpWithZip
        {
            [ChoFieldPosition(1)]
            public string Identifier { get; set; }
            [DisplayName("Name")]
            public string EmpName { get; set; }
            [DisplayName("Zip")]
            public string ZipCode { get; set; }
        }

        [Test]
        public static void LoadByIndexOrName()
        {
            string csv = @"Id, Name, Zip
1, Tom, 10010
2, Mark, 08830";

            string expected = @"[
  {
    ""Identifier"": ""1"",
    ""EmpName"": ""Tom"",
    ""ZipCode"": ""10010""
  },
  {
    ""Identifier"": ""2"",
    ""EmpName"": ""Mark"",
    ""ZipCode"": ""08830""
  }
]";
            using (var r = ChoCSVReader<EmpWithZip>.LoadText(csv)
                .WithFirstLineHeader(false)
                .ThrowAndStopOnMissingField(false)
                .Configure(c => c.AllowLoadingFieldByPosition = true)
                )
            {
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void ReadMixedCSVRecords()
        {
            typeof(ChoCSVReader).GetAssemblyVersion().Print();
            "".Print();

            string csv = @"Generic Text
Generict Text
Header 1,Header 2,Header 3,Header 4 
Val 1,Val 2,Val 3,Val 4 
Val 1,Val 2,Val 3,Val 4";


            string expected = @"[
  {
    ""Col1"": ""Generict Text"",
    ""Col2"": null,
    ""Col3"": null,
    ""Col4"": null
  },
  {
    ""Col1"": ""Header 1"",
    ""Col2"": ""Header 2"",
    ""Col3"": ""Header 3"",
    ""Col4"": ""Header 4""
  },
  {
    ""Col1"": ""Val 1"",
    ""Col2"": ""Val 2"",
    ""Col3"": ""Val 3"",
    ""Col4"": ""Val 4""
  },
  {
    ""Col1"": ""Val 1"",
    ""Col2"": ""Val 2"",
    ""Col3"": ""Val 3"",
    ""Col4"": ""Val 4""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                   .WithField("Col1")
                   .WithField("Col2")
                   .WithField("Col3")
                   .WithField("Col4").WithFirstLineHeader(true).ThrowAndStopOnMissingField(false)
                  )
            {
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Issue227()
        {
            string csv = @"ID,TIN,PROVIDER,NEIGHBOR,DRIVERNO,DRIVERNO2,LASTNAME,FIRSTNAME
""LO""""PEZ JAINA"",""052965646"",""1760638008"",""BRYAN, RICKY"",""898082"",""899082"",""LO""""PEZ JAINA"",""ROBERTO""";

            string expected = @"[
  {
    ""ID"": ""LO\""PEZ JAINA"",
    ""TIN"": ""052965646"",
    ""PROVIDER"": ""1760638008"",
    ""NEIGHBOR"": ""BRYAN, RICKY"",
    ""DRIVERNO"": ""898082"",
    ""DRIVERNO2"": ""899082"",
    ""LASTNAME"": ""LO\""PEZ JAINA"",
    ""FIRSTNAME"": ""ROBERTO""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv).WithFirstLineHeader().QuoteAllFields()
                .ThrowAndStopOnMissingField(false))
            {
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class MyClass
        {
            public string SITE_ID { get; set; }
            public string HOUSE { get; set; }
        }
        [Test]
        public static void Issue242()
        {
            string csv = @"SITE_ID,HOUSE,STREET,CITY,STATE,ZIP,APARTMENT
44,,PORT ROYAL,CORPUS CHRISTI,TX,,2
44,608646,TEXAS AVE,ODESSA,TX,79762,
44,487460,EVERHART RD,CORPUS CHRISTI,TX,78413,
44,275543,EDWARD GARY,SAN MARCOS,TX,78666,4
44,136811,MAGNOLIA AVE,SAN ANTONIO,TX1,,1";

            var productList = new List<MyClass>();

            string expected = @"[
  {
    ""SITE_ID"": ""44"",
    ""HOUSE"": """"
  },
  {
    ""SITE_ID"": ""44"",
    ""HOUSE"": ""608646""
  },
  {
    ""SITE_ID"": ""44"",
    ""HOUSE"": ""487460""
  },
  {
    ""SITE_ID"": ""44"",
    ""HOUSE"": ""275543""
  },
  {
    ""SITE_ID"": ""44"",
    ""HOUSE"": ""136811""
  }
]";
            using (var r = new ChoCSVLiteReader())
            {
                productList.AddRange(r.ReadText<MyClass>(csv, true, mapper: (lineno, cols, rec) =>
                {
                    rec.SITE_ID = cols[0];
                    rec.HOUSE = cols[1];
                }));
            }
            var actual = JsonConvert.SerializeObject(productList, Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Issue252()
        {
            string csv = @"Id, Name, Address
""1"", ""Tom"",""""
""2"","""",""""";

            var config = new ChoCSVRecordConfiguration
            {
                AutoDiscoverColumns = true,
                AutoDiscoverFieldTypes = true,
                ThrowAndStopOnMissingField = false,
                QuoteAllFields = true,
                NullValue = "",
                FileHeaderConfiguration = new ChoCSVFileHeaderConfiguration
                {
                    IgnoreColumnsWithEmptyHeader = true,
                    HasHeaderRecord = true,
                    QuoteAllHeaders = true
                }
            };

            string expected = @"Id:1
Name:Tom
Address:NULL
Id:2
Name:NULL
Address:NULL
";

            StringBuilder actualOut = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv) //, config)
                    .WithFirstLineHeader()
                    .QuoteAllFields()
                )
            {
                var dr = r.AsDataReader();
                while (dr.Read())
                {
                    //This "for" iterates through each column of the current row!
                    for (int i = 0; i < dr.FieldCount; i++)
                    {
                        Console.WriteLine(dr.GetName(i) + ":" + dr.GetValue(i).ToNString("NULL"));
                        actualOut.AppendLine(dr.GetName(i) + ":" + dr.GetValue(i).ToNString("NULL"));
                    }
                }
            }

            var actual = actualOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void Issue245()
        {
            string csv = @"Freight Terms,,Shipper Location ID,Shipper First Name,,,,Deliver to Line 1,Deliver to Line 2,Deliver to City,Deliver to State,Deliver to Zip,,,,,Handling Unit Count,Piece Count,,,,,,Product ID,BOL (DN en SAP),Carrier,PO Number,SO Number,PO Date,Batch,Customer Part Number,Shipping Notes,T_Tags,Send Freight bill to,Deletion flag
PREPAID US, ,US24,WOLONG US LRD, , , ,JEMA MOTORS AND AUTOMATION,10827 ELGAR LANE,TOMBALL,TX,77375, , , , ,PC,2, , , , , ,5KS143CWL205,80048901, ,Test AVRT SFTP 1,1000020196,0,31012, ,shipping marks header shipping marks line 1,t tags header t tags line 1,""WOLONG ELECTRIC INDUSTRIAL MOTORS c / o Interlog Services, North America 25 Research Drive Ann Arbor, MI 48103 LOC. 31 ACCT BCJM401754"", 
PREPAID US, , US24, WOLONG US LRD, , , , JEMA MOTORS AND AUTOMATION,10827 ELGAR LANE, TOMBALL, TX,77375, , , , ,PC,1, , , , , ,5KS143STE109,80048901, ,Test AVRT SFTP 1,1000020196,0,PRT591098, ,shipping marks header shipping marks line 2,t tags header t tags line 2,""WOLONG ELECTRIC INDUSTRIAL MOTORS c/o Interlog Services, North America 25 Research Drive Ann Arbor, MI 48103 LOC. 31 ACCT BCJM401754"", ";

            string expected = @"[
  {
    ""Freight Terms"": ""PREPAID US"",
    ""Shipper Location ID"": ""US24"",
    ""Shipper First Name"": ""WOLONG US LRD"",
    ""Deliver to Line 1"": ""JEMA MOTORS AND AUTOMATION"",
    ""Deliver to Line 2"": ""10827 ELGAR LANE"",
    ""Deliver to City"": ""TOMBALL"",
    ""Deliver to State"": ""TX"",
    ""Deliver to Zip"": ""77375"",
    ""Handling Unit Count"": ""PC"",
    ""Piece Count"": ""2"",
    ""Product ID"": ""5KS143CWL205"",
    ""BOL (DN en SAP)"": ""80048901"",
    ""Carrier"": null,
    ""PO Number"": ""Test AVRT SFTP 1"",
    ""SO Number"": ""1000020196"",
    ""PO Date"": ""0"",
    ""Batch"": ""31012"",
    ""Customer Part Number"": null,
    ""Shipping Notes"": ""shipping marks header shipping marks line 1"",
    ""T_Tags"": ""t tags header t tags line 1"",
    ""Send Freight bill to"": ""WOLONG ELECTRIC INDUSTRIAL MOTORS c / o Interlog Services, North America 25 Research Drive Ann Arbor, MI 48103 LOC. 31 ACCT BCJM401754"",
    ""Deletion flag"": null
  },
  {
    ""Freight Terms"": ""PREPAID US"",
    ""Shipper Location ID"": ""US24"",
    ""Shipper First Name"": ""WOLONG US LRD"",
    ""Deliver to Line 1"": ""JEMA MOTORS AND AUTOMATION"",
    ""Deliver to Line 2"": ""10827 ELGAR LANE"",
    ""Deliver to City"": ""TOMBALL"",
    ""Deliver to State"": ""TX"",
    ""Deliver to Zip"": ""77375"",
    ""Handling Unit Count"": ""PC"",
    ""Piece Count"": ""1"",
    ""Product ID"": ""5KS143STE109"",
    ""BOL (DN en SAP)"": ""80048901"",
    ""Carrier"": null,
    ""PO Number"": ""Test AVRT SFTP 1"",
    ""SO Number"": ""1000020196"",
    ""PO Date"": ""0"",
    ""Batch"": ""PRT591098"",
    ""Customer Part Number"": null,
    ""Shipping Notes"": ""shipping marks header shipping marks line 2"",
    ""T_Tags"": ""t tags header t tags line 2"",
    ""Send Freight bill to"": ""WOLONG ELECTRIC INDUSTRIAL MOTORS c/o Interlog Services, North America 25 Research Drive Ann Arbor, MI 48103 LOC. 31 ACCT BCJM401754"",
    ""Deletion flag"": null
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .ConfigureHeader(c => c.IgnoreColumnsWithEmptyHeader = true)
                .ThrowAndStopOnBadData(false)
                )
            {
                //using (var w = new ChoJSONWriter(Console.Out))
                //    w.Write(r);
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void Issue259()
        {
            Console.WriteLine(typeof(ChoCSVReader).GetAssemblyVersion());

            string csv = @"CapCode,CapId,Manufacturer,ShortModel,Model,ModelIntroduced,ModelDiscontinued,ShortDerivative,Derivative,DerivativeIntroducedYear,DerivativeDiscontinuedYear,AvailableNewVehicle,VehicleType
AAAD019  SC A    CXB,18723,ADIVA,AD1,AD1 200 (20-21),2020,,,AD1 200 (2020-2021),2020,,True,
CMMV090RIQU A    HZO,21416,CAN-AM,MAVERICK,MAVERICK X RS SAS TURBO RR,2022,,,""Maverick X RS SAS Turbo RR Intense Blue, Carbon Bl"",2022,,True,";

            string expected = @"[
  {
    ""CapCode"": ""AAAD019  SC A    CXB"",
    ""CapId"": 18723,
    ""Manufacturer"": ""ADIVA"",
    ""ShortModel"": ""AD1"",
    ""Model"": ""AD1 200 (20-21)"",
    ""ModelIntroduced"": 2020,
    ""Derivative"": ""AD1 200 (2020-2021)"",
    ""DerivativeIntroducedYear"": 2020,
    ""AvailableNewVehicle"": ""True""
  },
  {
    ""CapCode"": ""CMMV090RIQU A    HZO"",
    ""CapId"": 21416,
    ""Manufacturer"": ""CAN-AM"",
    ""ShortModel"": ""MAVERICK"",
    ""Model"": ""MAVERICK X RS SAS TURBO RR"",
    ""ModelIntroduced"": 2022,
    ""Derivative"": ""Maverick X RS SAS Turbo RR Intense Blue, Carbon Bl"",
    ""DerivativeIntroducedYear"": 2022,
    ""AvailableNewVehicle"": ""True""
  }
]";
            using (var r = ChoCSVReader<CapVehicleCodeDesc>.LoadText(csv)
                     .WithFirstLineHeader().MayHaveQuotedFields()
                    )
            {
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class CapVehicleCodeDesc
        {
            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string CapCode { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int CapId { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Manufacturer { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string ShortModel { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Model { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? ModelIntroduced { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? ModelDiscontinued { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string ShortDerivative { get; set; }

            [ChoCSVRecordField(QuoteField = false)]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Derivative { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? DerivativeIntroducedYear { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? DerivativeDiscontinuedYear { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string AvailableNewVehicle { get; set; }

            [ChoCSVRecordField]
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string VehicleType { get; set; }
        }

        public class Sequence
        {
            public int SequenceId { get; set; }
            public int NextId { get; set; }
        }

        [Test]
        public void ReadAndWriteDictionary()
        {
            string expected = @"Key,Value.SequenceId,Value.NextId
1,1,2
2,2,3";
            var data = new Dictionary<int, Sequence>()
            {
                { 1, new Sequence { SequenceId = 1, NextId = 2} },
                { 2, new Sequence { SequenceId = 2, NextId = 3} },
            };

            StringBuilder csv = new StringBuilder();
            using (var w = new ChoCSVWriter(csv)
                .WithFirstLineHeader())
            {
                w.Write(data);
            }

            csv.Print();
            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);

            string expected1 = @"{
  ""1"": {
    ""SequenceId"": 1,
    ""NextId"": 2
  },
  ""2"": {
    ""SequenceId"": 2,
    ""NextId"": 3
  }
}";
            using (var r = ChoCSVReader.LoadText(actual)
                .WithFirstLineHeader())
            {
                var recs = r.ToArray();
                var x = recs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToObject<Sequence>());
                var actual1 = JsonConvert.SerializeObject(x, Formatting.Indented);
                //actual1.Print();

                Assert.AreEqual(expected1, actual1);
            }
        }

        public class BehaviouralData
        {
            public float ActionZ { get; set; } = 1.0f;
            public float ActionX { get; set; } = 2.0f;
            public int TargetBallAgentHashCode { get; set; } = 42;
            public Vector3Ex TargetBallLocalPosition { get; set; } = new Vector3(1, 2, 3);
            public bool CollectBehaviouralData { get; set; } = true;
            public DateTime ActionTime { get; set; } = new DateTime(2023, 06, 10);
            public DateTime Time { get; set; } = new DateTime(2023, 06, 10, 11, 30, 12);
        }

        public class Vector3Ex
        {
            [DisplayName("x")]
            public float x => _value.x;
            [DisplayName("y")]
            public float y => _value.y;
            [DisplayName("z")]
            public float z => _value.z;

            private Vector3 _value;

            public Vector3Ex(Vector3 value)
            {
                _value = value;
            }

            public static implicit operator Vector3(Vector3Ex s) => s._value;
            public static implicit operator Vector3Ex(Vector3 s) => new Vector3Ex(s);
        }

        [ChoMetadataRefType(typeof(Vector3))]
        //[ChoCSVRecordObject(ObjectValidationMode = ChoObjectValidationMode.ObjectLevel, ErrorMode = ChoErrorMode.IgnoreAndContinue, IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any, ThrowAndStopOnMissingField = false)]
        public class Vector2Metadata
        {
            public float x { get; set; }
            public float y;
            //public float Y { get { return y; } }
        }

        [Test]
        public static void WriteFieldsFromExteenalClass()
        {
            BehaviouralData[] data = new[] { new BehaviouralData { }, new BehaviouralData { }, new BehaviouralData { } };

            string expected = @"ActionZ,ActionX,TargetBallAgentHashCode,x,y,z,CollectBehaviouralData,ActionTime,Time
1,2,42,1,2,3,True,6/10/2023,6/10/2023
1,2,42,1,2,3,True,6/10/2023,6/10/2023
1,2,42,1,2,3,True,6/10/2023,6/10/2023";

            StringBuilder csv = new StringBuilder();
            using (var w = new ChoCSVWriter<BehaviouralData>(csv)
                .WithFirstLineHeader()
                )
            {
                w.Write(data);
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }

        public class WorkerModel
        {
            public string VoterId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }

            public PhoneNumberModel PhoneNumber { get; set; }
        }

        public class PhoneNumberModel
        {
            public string AreaCode { get; set; }
            public string Prefix { get; set; }
            public string LineNumber { get; set; }

            public static implicit operator PhoneNumberModel(string s) => new PhoneNumberModel
            {
                AreaCode = s.Split('-')[0],
                Prefix = s.Split('-')[1],
                LineNumber = s.Split('-')[2]
            };

            public override string ToString()
            {
                return $"{AreaCode}-{Prefix}-{LineNumber}";
            }
        }
        [Test]
        public static void LoadComplexFieldUsingImplicitOperator()
        {
            string expected = @"[
  {
    ""VoterId"": ""1"",
    ""FirstName"": ""Tom"",
    ""LastName"": ""Smith"",
    ""Email"": ""tsmith@gmail.com"",
    ""PhoneNumber"": {
      ""AreaCode"": ""732"",
      ""Prefix"": ""232"",
      ""LineNumber"": ""1234""
    }
  }
]";

            var cf = new ChoCSVRecordConfiguration<WorkerModel>();
            cf.WithFirstLineHeader();
            cf.ClearFields();
            cf.Map(f => f.VoterId, m =>
            {
                m.FieldName("Id");
            });
            cf.Map(f => f.FirstName);
            cf.Map(f => f.LastName);
            cf.Map(f => f.Email);
            cf.Map(f => f.PhoneNumber);

            string csv = @"Id,FirstName,LastName,Email,PhoneNumber
1,Tom,Smith,tsmith@gmail.com,732-232-1234";

            List<WorkerModel> records = new List<WorkerModel>();
            using (var r = ChoCSVReader<WorkerModel>.LoadText(csv, cf)
                )
            {
                var recs = r.ToArray();
                records.AddRange(recs);

                recs.Print();

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

            StringBuilder csvOut = new StringBuilder();
            using (var w = new ChoCSVWriter<WorkerModel>(csvOut, cf)
                .WithFirstLineHeader())
            {
                w.Write(records);
            }

            var actual1 = csvOut.ToString();
            Assert.AreEqual(csv, actual1);
        }
        [Test]
        public static void LoadComplexFieldUsingValueConverter()
        {
            string expected = @"[
  {
    ""VoterId"": ""1"",
    ""FirstName"": ""Tom"",
    ""LastName"": ""Smith"",
    ""Email"": ""tsmith@gmail.com"",
    ""PhoneNumber"": {
      ""AreaCode"": ""732"",
      ""Prefix"": ""232"",
      ""LineNumber"": ""1234""
    }
  }
]";

            var cf = new ChoCSVRecordConfiguration<WorkerModel>();
            cf.WithFirstLineHeader();
            cf.ClearFields();
            cf.Map(f => f.VoterId, m =>
            {
                m.FieldName("Id");
            });
            cf.Map(f => f.FirstName);
            cf.Map(f => f.LastName);
            cf.Map(f => f.Email);
            cf.Map(f => f.PhoneNumber, m =>
            {
                m.ValueConverter(s => new PhoneNumberModel { AreaCode = s.ToNString().Split('-')[0], Prefix = s.ToNString().Split('-')[1], LineNumber = s.ToNString().Split('-')[2] });
                m.ValueConverterBack(s => s.ToNString());
            });

            string csv = @"Id,FirstName,LastName,Email,PhoneNumber
1,Tom,Smith,tsmith@gmail.com,732-232-1234";

            List<WorkerModel> records = new List<WorkerModel>();
            using (var r = ChoCSVReader<WorkerModel>.LoadText(csv, cf)
                )
            {
                var recs = r.ToArray();
                records.AddRange(recs);
                recs.Print();

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

            StringBuilder csvOut = new StringBuilder();
            using (var w = new ChoCSVWriter<WorkerModel>(csvOut, cf)
                .WithFirstLineHeader())
            {
                w.Write(records);
            }

            var actual1 = csvOut.ToString();
            Assert.AreEqual(csv, actual1);
        }

        [ChoCSVFileHeader(IgnoreHeader = true)]
        public class EpiDataNames
        {
            [ChoFieldPosition(0)]
            public string Type { get; set; }
            [ChoFieldPosition(1)]
            public string Value { get; set; }
        }
        [ChoCSVFileHeader(IgnoreHeader = true)]
        public class EpiLot
        {
            public string GLot { get; set; }
            public string Id { get; set; }
            public string Slot { get; set; }
            public string Scribe { get; set; }
        }
        [Test]
        public static void ReadCSVWithDiffernetFormattedData()
        {
            string csv = @"Metals:,E10
Al,0.1906
Ca,0.1132
Co,0.01951
Cu,0.5824
Cu,0.02383
Fe,0.03828
K,0.09577
Li,0.03024
Mg,0.007145
Na,0.1833
Ni,0.3236
Pb,0.0005787
Ti,0.4931
Tl,0.001887
Zn,0.07644

GLot,id,Slot,Scribe,Diameter,MPD,SResistivity,SThickness,TTV,LTV,Warp,Bow,S_U_A,Ep,Epi_L,Epi_Layer,Epi_Layer_2,EThick,E2thick,E2Dope,E2DopeT,E2DopeMax,E2DopeMin
31075046-001,XFB-LE00674.CP10023+001-12,1,22C1285,149.98,0,0.0217,334.71,1.91,1.03,5.35,-0.91,99.590582,1.0,1.0E18,9.8,1.12,9.9,9.6,9926193600000000,4.5574,10834500800000000,9551876800000000";

            string expected = @"[
  {
    ""Type"": ""0.1906"",
    ""Value"": ""Al""
  },
  {
    ""Type"": ""0.1132"",
    ""Value"": ""Ca""
  }
]";
            using (var r = ChoCSVReader<EpiDataNames>.LoadText(csv)
                .IgnoreEmptyLine(true)
                .Setup(s => s.DoWhile += (o, e) =>
                {
                    e.Stop = e.Source.ToNString().StartsWith("GLot,id");
                })
                )
            {
                var recs = r.ToArray();

                recs.Print();

                var actual = JsonConvert.SerializeObject(recs.Take(2), Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }

            string expected1 = @"[
  {
    ""GLot"": ""31075046-001"",
    ""Id"": ""XFB-LE00674.CP10023+001-12"",
    ""Slot"": ""1"",
    ""Scribe"": ""22C1285""
  }
]";
            using (var r = ChoCSVReader<EpiLot>.LoadText(csv)
                 .IgnoreEmptyLine(true)
                 .Setup(s => s.SkipUntil += (o, e) =>
                 {
                     e.Skip = !e.Source.ToNString().StartsWith("GLot,id");
                 })
                 )
            {
                var recs = r.ToArray();

                recs.Print();

                var actual1 = JsonConvert.SerializeObject(recs.Take(2), Formatting.Indented);
                Assert.AreEqual(expected1, actual1);
            }
        }

        [Test]
        public static void QuoteValuesTest()
        {
            string expected = @"[
  {
    ""Id"": ""1"",
    ""Message"": ""\""Close Time @ 3:00 P.M, 48\"" L x 40\"" W x 49\"" H""
  },
  {
    ""Id"": ""2"",
    ""Message"": ""Triple \""R\"" Truck Parts ""
  },
  {
    ""Id"": ""3"",
    ""Message"": ""KEEP SHRINK WRAP AND BLUE TAPE INTACT NON-STACKABLE -----\""APPOINTMENTS TO BE SCHEDULED VIA OPEN DOCK\"" ""
  },
  {
    ""Id"": ""4"",
    ""Message"": ""150Lx3Wx3\""H (3) / 150LX4Wx3 (2)""
  },
  {
    ""Id"": ""5"",
    ""Message"": ""** DO NOT STACK FREIGHT**. FCFS - Handle with care. (GPS may show \""east\"" Business Center Dr. That is ok)""
  }
]";
            using (var r = new ChoCSVReader("QuotedValues.csv")
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                .Configure(c => c.QuoteEscapeChar = '"')
                .Configure(c => c.FieldValueTrimOption = ChoFieldValueTrimOption.None)
                .ThrowAndStopOnBadData(false)
                )
            {
                var recs = r.ToArray();

                recs.Print();

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void TrimCSVValuesTest()
        {
            string expected = @"[
  {
    ""Id"": ""1"",
    ""Message"": ""\""Close Time @ 3:00 P.M, 48\"" L x 40\"" W x 49\"" H""
  },
  {
    ""Id"": ""2"",
    ""Message"": ""Triple \""R\"" Truck Parts""
  },
  {
    ""Id"": ""3"",
    ""Message"": ""KEEP SHRINK WRAP AND BLUE TAPE INTACT NON-STACKABLE -----\""APPOINTMENTS TO BE SCHEDULED VIA OPEN DOCK\""""
  },
  {
    ""Id"": ""4"",
    ""Message"": ""150Lx3Wx3\""H (3) / 150LX4Wx3 (2)""
  },
  {
    ""Id"": ""5"",
    ""Message"": ""** DO NOT STACK FREIGHT**. FCFS - Handle with care. (GPS may show \""east\"" Business Center Dr. That is ok)""
  }
]";
            using (var r = new ChoCSVReader("QuotedValues.csv")
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                .Configure(c => c.QuoteEscapeChar = '"')
                .Configure(c => c.FieldValueTrimOption = ChoFieldValueTrimOption.Trim)
                .ThrowAndStopOnBadData(false)
                )
            {
                var recs = r.ToArray();

                recs.Print();

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void QuoteValuesTest_WithEOLInsideValue()
        {
            string expected = @"[
  {
    ""Id"": ""1"",
    ""Message"": ""\""Close Time @ 3:00 P.M, 48\"" L x 40\"" W x 49\"" H""
  },
  {
    ""Id"": ""2"",
    ""Message"": ""Triple \""R\"" Truck Parts""
  },
  {
    ""Id"": ""3"",
    ""Message"": ""KEEP SHRINK WRAP AND BLUE TAPE INTACT NON-STACKABLE -----\""APPOINTMENTS TO BE SCHEDULED VIA OPEN DOCK\""""
  },
  {
    ""Id"": ""4"",
    ""Message"": ""150Lx3Wx3\""\""H (3) / 150LX4Wx3 (2)""
  },
  {
    ""Id"": ""5"",
    ""Message"": ""** DO NOT STACK FREIGHT**. FCFS - Handle with care. (GPS may show \""east\"" Business Center Dr. That is ok)""
  },
  {
    ""Id"": ""6"",
    ""Message"": ""Line 3 Field 3\r\nx\""Line 4 Field 1""
  }
]";
            using (var r = new ChoCSVReader("QuotedValuesWithEOLInside.csv")
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                .MayContainEOLInData()
                .Configure(c => c.QuoteEscapeChar = '"')
                .ThrowAndStopOnBadData(false)
                )
            {
                var recs = r.ToArray();

                recs.Print();

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void LoadBadDataValesTest()
        {
            string expected = @"[
  {
    ""Id"": ""1"",
    ""Message"": ""Close Time @ 3:00 P.M, 48\"" L x 40\"" W x 49\"" H, 38\"" L x 38\"" W x 20\"" H""
  },
  {
    ""Id"": ""2"",
    ""Message"": ""Triple \""R\"" Truck Parts ""
  },
  {
    ""Id"": ""3"",
    ""Message"": ""KEEP SHRINK WRAP AND BLUE TAPE INTACT NON-STACKABLE -----\""APPOINTMENTS TO BE SCHEDULED VIA OPEN DOCK\"" ""
  },
  {
    ""Id"": ""4"",
    ""Message"": ""150Lx3Wx3\""H (3) / 150LX4Wx3 (2)""
  },
  {
    ""Id"": ""5"",
    ""Message"": ""** DO NOT STACK FREIGHT**. FCFS - Handle with care. (GPS may show \""east\"" Business Center Dr. That is ok)""
  }
]";
            using (var r = new ChoCSVReader("QuotedValues.csv")
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                .JoinExtraFieldValues(true, true)
                .Configure(c => c.FieldValueTrimOption = ChoFieldValueTrimOption.None)
                )
            {
                var recs = r.ToArray();

                recs.Print();

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public static void IncorrectCSVFieldsTest()
        {
            string expected = @"[
  {
    ""Id"": ""1"",
    ""Message"": ""\""Close Time @ 3:00 P.M, 48\"" L x 40\"" W x 49\"" H""
  },
  {
    ""Id"": ""2"",
    ""Message"": ""Triple \""R\"" Truck Parts""
  },
  {
    ""Id"": ""3"",
    ""Message"": ""KEEP SHRINK WRAP AND BLUE TAPE INTACT NON-STACKABLE -----\""APPOINTMENTS TO BE SCHEDULED VIA OPEN DOCK\""""
  },
  {
    ""Id"": ""4"",
    ""Message"": ""150Lx3Wx3\""H (3) / 150LX4Wx3 (2)""
  },
  {
    ""Id"": ""5"",
    ""Message"": ""** DO NOT STACK FREIGHT**. FCFS - Handle with care. (GPS may show \""east\"" Business Center Dr. That is ok)""
  }
]";
            using (var r = new ChoCSVReader("QuotedValues.csv")
                .WithFirstLineHeader()
                .MayHaveQuotedFields()
                .Configure(c => c.QuoteEscapeChar = '"')
                .Configure(c => c.FieldValueTrimOption = ChoFieldValueTrimOption.Trim)
                .ThrowAndStopOnMissingField(true)
                .ThrowAndStopOnBadData(true)
                )
            {
                Assert.Catch<ChoBadDataException>(() =>
                {
                    var recs = r.ToArray();

                    recs.Print();

                    var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                    Assert.AreEqual(expected, actual);
                });
            }

        }

        [Test]
        public static void CSV2JSONWithDynamicObjectNames()
        {
            string csv = @"VE NUMMER;VE NAAM;EFFECTDATUM;EINDDATUM
123;abc;20221001;
124;def;20221001;
125;ter;20221001;
126;ddf;20221001;";

            string expected = @"[
  {
    ""Id"": ""123"",
    ""Name"": ""abc"",
    ""Date"": ""20221001""
  },
  {
    ""Id"": ""124"",
    ""Name"": ""def"",
    ""Date"": ""20221001""
  },
  {
    ""Id"": ""125"",
    ""Name"": ""ter"",
    ""Date"": ""20221001""
  },
  {
    ""Id"": ""126"",
    ""Name"": ""ddf"",
    ""Date"": ""20221001""
  }
]";
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFields("Id", "Name", "Date")
                .WithFirstLineHeader(true)
                .WithDelimiter(";")
                )
            {
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = TraceLevel.Off;
            string line1 = "\"Close Time @ 3:00 P.M, 48\" L x 40\" W x 49\" H, 38\" L x 38\" W x 20\" H\"";
            string line = @"""Line 3 Field 1""	""""	""Line 3 Field 3
x""Line 4 Field 1""	""""	""Line 4 Field 3""";
            string line2 = @"v-dakash@catalysis.com,""HEY""? Tester, 12345789,""Catalysis"", LLC., Enterprise 6 TEST, etc,etc ,etc";
            var vals = line.Split(",", ChoStringSplitOptions.AllowQuotes, '"');
            return;
            CSV2ComplexObj();
            return;

            ReadMixedCSVRecords();
            return;

            LoadByIndexOrName();
            return;

            PositionNeutralCSVLoad();
            return;

            CSV2ComplexObject();
            return;

            CSV2JSON2();

            return;

            CSV2ComplexObject();
            return;
            ListFieldTest();
            return;
            string csv = @"Id,Name,Salary
    1,Carl,10000    
    2,Mark,5000
    3,Tom,2000";

            using (var parser = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithField("Id")
                .WithField("Name")
                .WithField("Salary", typeof(float))
                )
            {
                foreach (var rec in parser)
                {
                    Console.WriteLine(rec.Dump());
                }
            }
            return;

            //        foreach (var p in new ChoCSVReader("Sample2.csv").WithFirstLineHeader()
            //.Configure(c => c.TreatCurrencyAsDecimal = true)
            ////.Configure(c => c.MaxScanRows = 10)
            //)
            //        {
            //            Console.WriteLine(p.Dump());
            //        }
            return;
            ReadAndCloseTest();
            return;

            CSV2ComplexObject();
            return;
            JSON2CSVWithSpecialCharsInHeader();
            return;

            MultiRecordTypeTest();
            return;

            Sample3();
            return;

            //            string csv = @"""Line 3 Field 1"","""",""Line 3 Field 3
            //x""Line 4 Field 1""   """"  ""Line 4 Field 3""";
            //            var t = csv.FastSplit(',', '"', '"');
            //            foreach (var tc in t)
            //                Console.WriteLine(tc);

            //            return;

            SimpleCSVTest();
            return;


            //ZipCodeLoadTest();
            ZipCodeBcpTest();
            return;

            NullableColumnAsDataTable();
            return;

            SimpleCSVTest();
            return;

            ColumnCountStrictTest();
            return;

            CSV2JSONWithEmptyArray();
            return;

            TransposeTest();
            return;

            SepInValueTest();
            return;

            CSV2XmlTest();
            return;

            ReadHeaderAt3();
            return;
            Sample21();
            return;

            DateFormatTestUsingOptInPOCO();
            return;

            Sample10();
            return;

            NullValueTest();
            return;

            QuickDynamicTest();
            return;
            //MultiRecordsInfile();
            //return;

            //MultiRecordsInfile();
            //return;

            Sample2();
            return;

            //NullValueTest();
            //         return;

            InterfaceTest();
            return;

            Sample3();
            return;
            //DiffCSV();
            //return;

            //CombineColumns();
            //return;
            Sample3();
            return;
            MergeCSV1();
            return;

            //Sample4();
            //return;

            //Sample3();
            //return;

            Pontos();
            return;
            Sample1();
            return;
            EmptyValueTest();
            return;
            CDataDataSetTest();
            return;
            QuoteValueTest();
            return;

            ReportEmptyLines();
            return;
            //ChoETLFrxBootstrap.IsSandboxEnvironment = true;
            string txt1 = @"Id;Name;Document
1;Matheus;555777
2;Clarice;567890";
            string txt2 = @"""Id_Person"";""First_Name"";""Phone""
3; ""John""; ""999 -9999""";
            string txt3 = @"Id;Name
1;Matheus
2;Clarice";

            var r1 = new ChoCSVReader<People>().WithFirstLineHeader().WithDelimiter(";");
            //var x1 = r1.DeserializeText(txt1).FirstOrDefault();
            string[] h = r1.Context.Headers;
            Console.WriteLine(String.Join(",", h));
            return;
            foreach (var rec in ChoCSVReader<People>.LoadText(txt3).WithFirstLineHeader().WithDelimiter(";").ThrowAndStopOnMissingField(false)
                )
                Console.WriteLine(ChoUtility.Dump(rec));

            return;
            CustomNewLine();
            return;
            NestedQuotes();
            return;
            ConvertToNestedObjects();
            return;
            //System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("it");

            //using (var p = new ChoCSVReader("Bosch Luglio 2017.csv")
            //    .Configure((c) => c.MayContainEOLInData = true) //Handle newline chars in data
            //    .Configure(c => c.Encoding = Encoding.GetEncoding("iso-8859-1")) //Specify the encoding for reading
            //    .WithField("CodArt", 1) //first column
            //    .WithField("Descrizione", 2) //second column
            //    .WithField("Prezzo", 3, fieldType: typeof(decimal)) //third column
            //    .Setup(c => c.BeforeRecordLoad += (o, e) =>
            //    {
            //        e.Source = e.Source.CastTo<string>().Replace(@"""", String.Empty); //Remove the quotes
            //    }) //Scrub the data
            //    )
            //{
            //    //var dt = p.AsDataTable();

            //    foreach (var rec in p)
            //        Console.WriteLine(rec.Prezzo);
            //}
            //return;
            //using (var parser = new ChoCSVReader("Dict1.csv")
            //    .WithField("AR_ID", 7)
            //    .WithField("AR_TYPE", 8)
            //    .WithFirstLineHeader(true)
            //    .Configure(c => c.IgnoreEmptyLine = true)
            //    )
            //{
            //    var dict = parser.ToDictionary(item => item.AR_ID, item => item.AR_TYPE);
            //    foreach (var kvp in dict)
            //        Console.WriteLine(kvp.Key + " " + kvp.Value);
            //}
            //return;

            //return;
            //using (var parser = new ChoCSVReader("IgnoreLineFile1.csv")
            //    .WithField("PolicyNumber", 1)
            //    .WithField("VinNumber", 2)
            //    .Configure(c => c.IgnoreEmptyLine = true)
            //    .Configure(c => c.ColumnCountStrict = true)
            //    )
            //{
            //    using (var writer = new ChoJSONWriter("ignoreLineFile1.json")
            //            .WithField("PolicyNumber", fieldName: "Policy Number")
            //            .WithField("VinNumber", fieldName: "Vin Number")
            //        )
            //        writer.Write(parser.Skip(1));
            //}
            //return;

            //foreach (dynamic rec in new ChoCSVReader("emp.csv").WithFirstLineHeader()
            //    .WithFields(" id ", "Name")
            //    .Configure(c => c.FileHeaderConfiguration.IgnoreCase = false)
            //    .Configure(c => c.FileHeaderConfiguration.TrimOption = ChoFieldValueTrimOption.None)
            //    .Configure(c => c.ThrowAndStopOnMissingField = true)
            //    //.Configure(c => c.ColumnOrderStrict = false)
            //    )
            //{
            //    Console.WriteLine(rec.id);
            //    //Console.WriteLine(rec[" id "]);
            //}
            //return;
            //foreach (var rec in new ChoCSVReader<EmployeeRec>("emp.csv")
            //    .Configure(c => c.FileHeaderConfiguration.IgnoreCase = false)
            //    .Configure(c => c.ThrowAndStopOnMissingField = true)
            //    )
            //{
            //    Console.WriteLine(rec.Id);
            //}

            //return;

            //Set the culture, if your system different from the file type
            //HierarchyCSV();
            //return;
            //using (var r = new ChoCSVReader<Quote>("CurrencyQuotes.csv").WithDelimiter(";"))
            //{
            //    foreach (var rec in r)
            //        Console.WriteLine(rec.F1);

            //    Console.WriteLine(r.IsValid);
            //}

            //return;
            foreach (dynamic rec in new ChoCSVReader("CurrencyQuotes.csv").WithDelimiter(";")
                .WithField("F1", 14)
                .WithField("F2", 15)
                .WithField("F3", 16)
                .Configure(c => c.ErrorMode = ChoErrorMode.ReportAndContinue)
                )
            {
                Console.WriteLine("{0}", rec.F1);
            }
            return;
            //string txt = @"ZipCode  SortCode  3rd  ";
            //foreach (var x2 in txt.Split("  ", ChoStringSplitOptions.All, '"'))
            //    Console.WriteLine(x2);
            //return;
            CultureSpecificDateTimeTest(false);
            return;


            var x = 1;
            //Console.WriteLine(@_2);

            ////var identifierRegex = new System.Text.RegularExpressions.Regex(@"(?<=^| )(?!\d)\w+|(?<= )(?!\d)\w+(?= |$)");
            ////Console.WriteLine(Regex.Replace("1sas3", @"(?<=^| )(?!\d)\w+|(?<= )(?!\d)\w+(?= |$)", "_"));
            ////return;
            //var i = Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#").CreateValidIdentifier("@Main 12");
            //Console.WriteLine(i.ToValidVariableName());
            //return;
            QuotedCSVTest();
        }

        [Test]
        public static void CSVToXmlNodeTest()
        {
            string expected = @"<data-set>
  <PDA_DATA>
    <ID>206609474</ID>
    <NODE>2175</NODE>
    <PROCESS_STATE>47</PROCESS_STATE>
    <PREV_TIME_STAMP>31.03.2015 00:01:25</PREV_TIME_STAMP>
  </PDA_DATA>
  <PDA_DATA>
    <ID>206609475</ID>
    <NODE>2175</NODE>
    <PROCESS_STATE>47</PROCESS_STATE>
    <PREV_TIME_STAMP>31.03.2015 00:02:25</PREV_TIME_STAMP>
  </PDA_DATA>
  <PDA_DATA>
    <ID>206609476</ID>
    <NODE>2175</NODE>
    <PROCESS_STATE>47</PROCESS_STATE>
    <PREV_TIME_STAMP>31.03.2015 00:03:25</PREV_TIME_STAMP>
  </PDA_DATA>
</data-set>";

            using (var csv = new ChoCSVReader("NodeData.csv").WithFirstLineHeader(true)
                .WithFields("ID", "NODE", "PROCESS_STATE", "PREV_TIME_STAMP")
                )
            {
                using (var xml = new ChoXmlWriter("NodeData.xml").WithXPath("data-set/PDA_DATA")
                    .Configure(c => c.DoNotEmitXmlNamespace = true))
                    xml.Write(csv);
            }

            var tmpReader = new StreamReader(ChoPath.GetFullPath(FileNameNodeDataXML));
            string actual = tmpReader.ReadToEnd();
            Assert.AreEqual(expected, actual);
            // TODO: Change simple string compare to better XML content compare
        }

        [Test]
        public static void HierarchyCSV()
        {
            string expected = @"{
  ""players"": [
    {
      ""Id"": 2938,
      ""Sea"": 2018,
      ""First"": ""David"",
      ""Last"": ""Bush"",
      ""Team"": null,
      ""Coll"": ""Stanford"",
      ""Num"": 19,
      ""Age"": 21,
      ""Hgt"": 76,
      ""Wgt"": 212,
      ""Pos"": ""QB"",
      ""Attr"": {
        ""Str"": 68,
        ""Agi"": 55
      },
      ""Per"": {
        ""Lea"": 34,
        ""Wor"": 71
      },
      ""Skills"": {
        ""WR"": 0,
        ""TE"": 0
      },
      ""Flg"": ""None"",
      ""Trait"": ""None""
    }
  ]
}";

            using (var p = new ChoCSVReader(FileNamePlayersCSV).WithFirstLineHeader()
                .MayHaveQuotedFields()
                .NestedKeySeparator('/')
                //.Configure(c => c.AllowNestedConversion = true)
                )
            {
                using (var w = new ChoJSONWriter<Players>(FileNamePlayersJSON).Configure(c => c.UseJSONSerialization = true).Configure(c => c.SupportMultipleContent = true))
                {
                    w.Write(new Players { players = p.Select(e => new Player(e)).ToArray() });
                }
            }
            string actual = new StreamReader(FileNamePlayersJSON).ReadToEnd();
            Assert.AreEqual(expected, actual);
            // TODO: Change simple string compare to better JSON content compare
        }
        [Test]
        public static void LookupTest()
        {
            var zipSortCodeDict = File.ReadAllLines(FileNameZipCodesCSV).ToDictionary(line => line.Split("   ")[0], line => line.Split("   ")[1]);

            var zipSortCodeDict2 = new ChoCSVReader(FileNameZipCodesCSV).WithDelimiter("   ").WithFirstLineHeader().ToDictionary(kvp => kvp.ZipCode, kvp => kvp.SortCode);

            zipSortCodeDict.Remove("ZipCode");
            CollectionAssert.AreEqual(zipSortCodeDict, zipSortCodeDict2);
        }

        //[Test]
        public static void MergeCSV()
        {
            using (var p = new ChoCSVReader(FileNameMergeInputCSV).WithFirstLineHeader()
                .ThrowAndStopOnMissingField(false))
            {
                var recs = p.Where(r => !String.IsNullOrEmpty(r.szItemId)).GroupBy(r => r.szItemId)
                    .Select(g => new
                    {
                        szItemId = g.Key,
                        szName = g.Where(i1 => !String.IsNullOrEmpty(i1.szName)).Select(i1 => i1.szName).FirstOrDefault(),
                        lRetailStoreID = g.Where(i1 => !String.IsNullOrEmpty(i1.lRetailStoreID)).Select(i1 => i1.lRetailStoreID).FirstOrDefault(),
                        szDesc = g.Where(i1 => !String.IsNullOrEmpty(i1.szDesc)).Select(i1 => i1.szDesc).FirstOrDefault()
                    }).ToArray();

                using (var o = new ChoCSVWriter(FileNameMergeOutputCSV).WithFirstLineHeader())
                {
                    o.Write(recs);
                }
            }
        }

        [ChoCSVRecordObject("|")]
        public class EmpWithJSON
        {
            [ChoCSVRecordField(1)]
            public int Id { get; set; }
            [ChoCSVRecordField(2)]
            public string Name { get; set; }
            [ChoCSVRecordField(3)]
            public string JsonValue { get; set; }
            [ChoIgnoreMember]
            public string product_version_id { get; set; }
            [ChoIgnoreMember]
            public string product_version_name { get; set; }
        }
        [Test]
        public static void CSVWithJSON()
        {
            string expected = @"[
  {
    ""Id"": 4,
    ""Name"": ""Tom"",
    ""product_version_id"": ""932"",
    ""product_version_name"": ""S7""
  },
  {
    ""Id"": 1,
    ""Name"": ""Mark"",
    ""product_version_id"": ""123"",
    ""product_version_name"": ""F1""
  }
]";
            StringBuilder json = new StringBuilder();
            using (var parser = new ChoCSVReader<EmpWithJSON>(FileNameEmp1CSV))
            {
                parser.BeforeRecordFieldLoad += (o, e) =>
                {
                    if (e.PropertyName == "JsonValue")
                    {
                        EmpWithJSON rec = e.Record as EmpWithJSON;
                        dynamic jobject = ChoJSONReader.LoadText((string)e.Source).FirstOrDefault();
                        rec.product_version_id = jobject.product_version_id;
                        rec.product_version_name = jobject.product_version_name;
                        e.Skip = true;
                    }
                };
                using (var jp = new ChoJSONWriter(json))
                    jp.Write(parser.Select(i => new { i.Id, i.Name, i.product_version_id, i.product_version_name }));

                var actual = json.ToString();
                Assert.AreEqual(expected, actual);
                //foreach (var rec in parser)
                //    Console.WriteLine(rec.product_version_id);
            }
        }

        class Transaction
        {
            public string Id { get; set; }
            public DateTime Date { get; set; }
            public string Account { get; set; }
            public decimal Amount { get; set; }
            public string Subcategory { get; set; }
            public string Memo { get; set; }

            public override bool Equals(object obj)
            {
                var transaction = obj as Transaction;
                return transaction != null &&
                       Id == transaction.Id &&
                       Date == transaction.Date &&
                       Account == transaction.Account &&
                       Amount == transaction.Amount &&
                       Subcategory == transaction.Subcategory &&
                       Memo == transaction.Memo;
            }

            public override int GetHashCode()
            {
                var hashCode = 727656570;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Id);
                hashCode = hashCode * -1521134295 + Date.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Account);
                hashCode = hashCode * -1521134295 + Amount.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Subcategory);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Memo);
                return hashCode;
            }
        }
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public static void CultureSpecificDateTimeTest(bool setCorrectCulture)
        {
            List<Transaction> expected = new List<Transaction>
            {
                new Transaction{ Id= "1", Date=new DateTime(2017,5,9), Account="XXX XXXXXX", Amount= new decimal(-29), Subcategory = "FT", Memo = "[Sample string]"  },
                new Transaction{ Id= "2", Date=new DateTime(2017,5,9), Account="XXX XXXXXX", Amount= new decimal(-20), Subcategory = "FT", Memo = "[Sample string]"  },
                new Transaction{ Id= "3", Date=new DateTime(2017,5,25), Account="XXX XXXXXX", Amount= new decimal(-6.3), Subcategory = "PAYMENT", Memo = "[Sample string]"  }
            };
            string csvData = @"Id,Date,Account,Amount,Subcategory,Memo
 1,09/05/2017,XXX XXXXXX,-29.00,FT , [Sample string]
 2,09/05/2017,XXX XXXXXX,-20.00,FT ,[Sample string]
 3,25/05/2017,XXX XXXXXX,-6.30,PAYMENT,[Sample string]";

            List<object> result = new List<object>();

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(csvData)))
            using (StreamReader sr = new StreamReader(ms))
            {
                var csv = new ChoCSVReader<Transaction>(sr).WithFirstLineHeader();
                csv.TraceSwitch = ChoETLFramework.TraceSwitchOff;
                if (setCorrectCulture)
                {
                    csv.Configuration.Culture = CultureInfo.GetCultureInfo("en-GB");
                    foreach (var t in csv)
                        result.Add(t);
                    CollectionAssert.AreEqual(expected, result);
                }
                else
                {
                    var enumerator = csv.GetEnumerator();
                    enumerator.MoveNext();
                    enumerator.MoveNext();
                    Assert.Throws<ChoReaderException>(() => enumerator.MoveNext());
                }
            }
        }
        public class EmpDetail
        {
            [ChoCSVRecordField(1, FieldName = "company name")]
            public string COMPANY_NAME { get; set; }
        }

        [Test]
        public static void QuotedCSVTest()
        {
            List<string> expected = new List<string> { "Bbc Worldwide Labs, Bounce Republic Ltd", "Broadcast Media" };
            List<object> actual = new List<object>();
            //using (var engine = new ChoCSVReader<EmpDetail>("EmpQuote.csv").WithFirstLineHeader())
            //{
            //    engine.Configuration.FileHeaderConfiguration.IgnoreCase = true;
            //    foreach (dynamic item in engine)
            //    {
            //        Console.WriteLine(item.COMPANY_NAME);
            //    }
            //}
            //return;
            //using (var engine  = new ChoCSVReader("EmpQuote.csv").WithFirstLineHeader())
            //{
            //    engine.Configuration.FileHeaderConfiguration.IgnoreCase = true;
            //    foreach (dynamic item in engine)
            //    {
            //        Console.WriteLine(item.COMPANY_NAME);
            //        Console.WriteLine(item.COMPANY_type);
            //    }
            //}

            foreach (dynamic rec in new ChoCSVReader(FileNameEmpQuoteCSV).WithFirstLineHeader()
                .MayHaveQuotedFields())
            {
                actual.Add(rec["COMPANY NAME"]);
                actual.Add(rec["COMPANY TYPE"]);
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void ErrorHandling()
        {
            //throw new Exception("Don't know whats expected. Event not raised. Maybe a destroyed test case. EmployeeRec is referenced 6 times. Why use a second reader?");
            //var parser1 = new ChoCSVReader<EmployeeRec>(FileNameEmpWithSalaryCSV).WithFirstLineHeader();

            using (var parser = new ChoCSVReader<EmployeeRec>(FileNameEmpWithSalaryCSV).WithFirstLineHeader()
                .MayHaveQuotedFields())
            {
                parser.RecordFieldLoadError += (o, e) =>
                {
                    Console.Write(e.Exception.Message);
                    e.Handled = true;
                };

                var a = parser.ToArray();
            }
        }

        [Test]
        public static void IgnoreLineTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name", "Tom Cassawaw" } }
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoCSVReader(FileNameIgnoreLineFileCSV).WithFirstLineHeader())
            {
                parser.BeforeRecordLoad += (o, e) =>
                {
                    if (e.Source != null)
                    {
                        e.Skip = ((string)e.Source).StartsWith("%");
                    }
                };
                parser.BeforeRecordFieldLoad += (o, e) =>
                {
                    //if (e.PropertyName == "Id")
                    //    e.Skip = true;
                };

                parser.AfterRecordFieldLoad += (o, e) =>
                {
                    if (e.Source.ToNString() == "2")
                        e.Stop = true;
                };
                parser.AfterRecordLoad += (o, e) =>
                {
                    e.Stop = false;
                };
                foreach (var rec in parser)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void MultiLineColumnValue()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name", @"Tom
Cassawaw"} },
                new ChoDynamicObject {{ "Id", "2" }, { "Name", @"Carl"} },
                new ChoDynamicObject {{ "Id", "3" }, { "Name", @"Mark"} }
            };
            List<object> actual = new List<object>();

            using (var parser = new ChoCSVReader(FileNameMultiLineValueCSV).WithFirstLineHeader()
                .MayHaveQuotedFields()
                )
            {
                parser.Configuration.MayContainEOLInData = true;

                foreach (var rec in parser)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void LoadTextTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name", "Mark"} }
            };
            List<object> actual = new List<object>();

            string txt = "Id, Name\r\n1, Mark";
            foreach (var e in ChoCSVReader.LoadText(txt).WithFirstLineHeader())
            {
                actual.Add(e);
                Console.WriteLine(ChoUtility.ToStringEx(e));
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void QuickTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRecWithCurrency>(reader).WithDelimiter(",").WithFirstLineHeader())
            {
                writer.WriteLine("Id,Name,Salary");
                writer.WriteLine("1,Carl,1000");
                writer.WriteLine("2,Mark,2000");
                writer.WriteLine("3,Tom,3000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        [Test]
        public static void QuickDynamicTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name", "Carl"}, { "Salary", "10000" } },
                new ChoDynamicObject {{ "Id", "2" }, { "Name", "Mark"}, { "Salary", "5000" } },
                new ChoDynamicObject {{ "Id", "3" }, { "Name", "Tom"}, { "Salary", "2000" } }
            };
            List<object> actual = new List<object>();

            string csv = @"Id,Name,Salary
1,Carl,10000    
2,Mark,5000
3,Tom,2000";

            using (var parser = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .QuoteAllFields()
                )
            {
                foreach (var rec in parser)
                {
                    actual.Add(rec);
                    Console.WriteLine(String.Format("Id: {0}", rec.Id));
                    Console.WriteLine(String.Format("Name: {0}", rec.Name));
                    Console.WriteLine(String.Format("Salary: {0}", rec.Salary));
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void DateTimeTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject { { "Id", (int)1 }, { "Name", "Carl" },{ "Salary", new ChoCurrency(12345679) },{ "JoinedDate", new DateTime(2011, 1, 1) },{ "Active", false } },
                new ChoDynamicObject { { "Id", (int)2 }, { "Name", "Mark" },{ "Salary", new ChoCurrency(50000) },{ "JoinedDate", new DateTime(1995, 9, 23) },{ "Active", true } },
                new ChoDynamicObject { { "Id", (int)3 }, { "Name", "Tom" },{ "Salary", new ChoCurrency(150000) },{ "JoinedDate", new DateTime(1999, 4, 10) },{ "Active", true } }
            };
            List<object> actual = new List<object>();

            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MMM dd, yyyy";
            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.ZeroOrOne;

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("JoinedDate", 4) { FieldType = typeof(DateTime) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Active", 5) { FieldType = typeof(bool) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config)
                .MayHaveQuotedFields())
            {
                writer.WriteLine(@"1,Carl,12345679,""Jan 01, 2011"",0");
                writer.WriteLine(@"2,Mark,50000,""Sep 23, 1995"",1");
                writer.WriteLine(@"3,Tom,150000,""Apr 10, 1999"",1");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    actual.Add(row);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void UsingLinqTest()
        {
            CultureInfo savedCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            // TODO: Constructor for ChoCurrency with setting Currency property to make CultureInfo-Setting not necessary any more
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("se-SE");
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", (int)1 }, { "Name", "Carl" }, { "Salary", new ChoCurrency(12345679) }, { "JoinedDate", new DateTime(2017,10,10)}, { "EmployeeNo" , (int)5 } },
                new ChoDynamicObject {{ "Id", (int)2 }, { "Name", "Markl" }, { "Salary", new ChoCurrency(50000) }, { "JoinedDate", new DateTime(2001,10,1)}, { "EmployeeNo" , (int)6 } },
                new ChoDynamicObject {{ "Id", (int)3 }, { "Name", "Toml" }, { "Salary", new ChoCurrency(150000) }, { "JoinedDate", new DateTime(1996,1,25)}, { "EmployeeNo" , (int)9 } }
            };
            System.Threading.Thread.CurrentThread.CurrentCulture = savedCulture;

            List<object> actual = new List<object>();
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.Culture = new System.Globalization.CultureInfo("se-SE");
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("JoinedDate", 4) { FieldType = typeof(DateTime) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("EmployeeNo", 5) { FieldType = typeof(int) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine(@"1,Carl,12.345679 kr,2017-10-10,  5    ");
                writer.WriteLine("2,Markl,50000 kr,2001-10-01,  6    ");
                writer.WriteLine("3,Toml,150000 kr,1996-01-25,  9    ");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    actual.Add(row);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void BoolTest()
        {
            List<ChoDynamicObject> compareList = new List<ChoDynamicObject>();
            compareList.Add(new ChoDynamicObject
            {
                { "Id", (Int32)1 },
                { "Name", (String)"Carl" },
                { "Salary", new ChoCurrency(12345679) },
                {"JoinedDate",new DateTime(2016,1,10) }, // TODO: Check if correct Date is 1st of October or 10th of January
                {"Active",false }
            });
            compareList.Add(new ChoDynamicObject
            {
                { "Id", (Int32)2 },
                { "Name", (String)"Mark" },
                { "Salary", new ChoCurrency(50000) },
                {"JoinedDate",new DateTime(1995,10,1) }, // TODO: Check if correct Date is 1st of October or 10th of January
                {"Active",true }
            });
            compareList.Add(new ChoDynamicObject
            {
                { "Id", (Int32)3 },
                { "Name", (String)"Tom" },
                { "Salary", new ChoCurrency(150000) },
                {"JoinedDate",new DateTime(1940,1,1) },
                {"Active",true }
            });

            List<object> resultList = new List<object>();

            ChoTypeConverterFormatSpec.Instance.BooleanFormat = ChoBooleanFormatSpec.ZeroOrOne;

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("JoinedDate", 4) { FieldType = typeof(DateTime) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Active", 5) { FieldType = typeof(bool) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine(@"1,Carl,12345679,01/10/2016,0");
                writer.WriteLine("2,Mark,50000,10/01/1995,1");
                writer.WriteLine("3,Tom,150000,01/01/1940,1");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    resultList.Add(row);
                //Console.WriteLine(row.ToStringEx()); // Not necessary for NUnit-Test
            }
            CollectionAssert.AreEqual(compareList, resultList);
        }

        public enum EmployeeType
        {
            [Description("Full Time Employee")]
            Permanent = 0,
            [Description("Temporary Employee")]
            Temporary = 1,
            [Description("Contract Employee")]
            Contract = 2
        }
        [Test]
        public static void EnumTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", (int)1 }, { "Name", "Carl" }, { "Salary", new ChoCurrency(12345679) }, { "JoinedDate", new DateTime(2016,1,10)}, { "EmployeeType" , EmployeeType.Permanent } },
                new ChoDynamicObject {{ "Id", (int)2 }, { "Name", "Mark" }, { "Salary", new ChoCurrency(50000) }, { "JoinedDate", new DateTime(1995,10,1)}, { "EmployeeType" , EmployeeType.Temporary } },
                new ChoDynamicObject {{ "Id", (int)3 }, { "Name", "Tom" }, { "Salary", new ChoCurrency(150000) }, { "JoinedDate", new DateTime(1940,1,1)}, { "EmployeeType" , EmployeeType.Contract } },
            };
            List<object> actual = new List<object>();

            ChoTypeConverterFormatSpec.Instance.EnumFormat = ChoEnumFormatSpec.Description;

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("JoinedDate", 4) { FieldType = typeof(DateTime) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("EmployeeType", 5) { FieldType = typeof(EmployeeType) });
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config)
                //.WithFirstLineHeader()
                )
            {
                writer.WriteLine(@"1,Carl,12345679,01/10/2016,Full Time Employee");
                writer.WriteLine("2,Mark,50000,10/01/1995,Temporary Employee");
                writer.WriteLine("3,Tom,150000,01/01/1940,Contract Employee");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    actual.Add(row);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void UsingFormatSpecs()
        {
            CultureInfo savedCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            // TODO: Constructor for ChoCurrency with setting Currency property to make CultureInfo-Setting not necessary any more
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("se-SE");
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", (int)1 }, { "Name", "Carl" }, { "Salary", new ChoCurrency(12345679) }, { "JoinedDate", new DateTime(2017,10,10)}, { "EmployeeNo" , (int)-5 } },
                new ChoDynamicObject {{ "Id", (int)2 }, { "Name", "Markl" }, { "Salary", new ChoCurrency(50000) }, { "JoinedDate", new DateTime(2001,10,1)}, { "EmployeeNo" , (int)6 } },
                new ChoDynamicObject {{ "Id", (int)3 }, { "Name", "Toml" }, { "Salary", new ChoCurrency(150000) }, { "JoinedDate", new DateTime(1996,1,25)}, { "EmployeeNo" , (int)9 } }
            };
            System.Threading.Thread.CurrentThread.CurrentCulture = savedCulture;

            List<object> actual = new List<object>();

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.Culture = new System.Globalization.CultureInfo("se-SE");
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1) { FieldType = typeof(int) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("JoinedDate", 4) { FieldType = typeof(DateTime) });
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("EmployeeNo", 5) { FieldType = typeof(int) });

            ChoTypeConverterFormatSpec.Instance.IntNumberStyle = NumberStyles.AllowParentheses;

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine(@"1,Carl,12.345679 kr,2017-10-10,  (5)    ");
                writer.WriteLine("2,Markl,50000 kr,2001-10-01,  6    ");
                writer.WriteLine("3,Toml,150000 kr,1996-01-25,  9    ");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    actual.Add(row);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void ValidationOverridePOCOTest()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            var idConfig = new ChoCSVRecordFieldConfiguration("Id", 1);
            idConfig.Validators = new ValidationAttribute[] { new RequiredAttribute() };
            config.CSVRecordFieldConfigurations.Add(idConfig);
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });
            config.WithFirstLineHeader(true);

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRecWithCurrency>(reader, config)
                //                .WithFirstLineHeader()
                )
            {
                parser.Configuration.ObjectValidationMode = ChoObjectValidationMode.ObjectLevel;

                writer.WriteLine("Id,Name,Salary1");
                writer.WriteLine("1,Carl,$100000");
                writer.WriteLine("2,Mark,");
                writer.WriteLine("3,Tom,1000");
                writer.WriteLine(",Tim,$50000");

                writer.Flush();
                stream.Position = 0;

                EmployeeRecWithCurrency rec = null;
                EmployeeRecWithCurrency expected = null;
                Assert.DoesNotThrow(() => rec = parser.Read(), "Header line not ignored");
                expected = new EmployeeRecWithCurrency { Id = 1, Name = "Carl", Salary = new ChoCurrency(100000) };
                Assert.AreEqual(expected, rec);

                Assert.DoesNotThrow(() => rec = parser.Read());
                expected = new EmployeeRecWithCurrency { Id = 2, Name = "Mark" };
                Assert.AreEqual(expected, rec);

                Assert.DoesNotThrow(() => rec = parser.Read(), "Nullable Salary");
                expected = new EmployeeRecWithCurrency { Id = 3, Name = "Tom", Salary = new ChoCurrency(1000) };
                Assert.AreEqual(expected, rec);

                Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() => rec = parser.Read(), "Missing Id should not be loaded because of RequiredAttribute");
            }
        }

        public class EmployeeRecWithCDATA
        {
            public int? Id { get; set; }
            public ChoCDATA Name { get; set; }
            public ChoCurrency? Salary { get; set; }

            public override bool Equals(object obj)
            {
                var cDATA = obj as EmployeeRecWithCDATA;
                return cDATA != null &&
                       EqualityComparer<int?>.Default.Equals(Id, cDATA.Id) &&
                       EqualityComparer<ChoCDATA>.Default.Equals(Name, cDATA.Name) &&
                       EqualityComparer<ChoCurrency?>.Default.Equals(Salary, cDATA.Salary);
            }

            public override int GetHashCode()
            {
                var hashCode = -1858601383;
                hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Id);
                hashCode = hashCode * -1521134295 + EqualityComparer<ChoCDATA>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<ChoCurrency?>.Default.GetHashCode(Salary);
                return hashCode;
            }
        }

        public class EmployeeRecWithCurrency
        {
            public int? Id { get; set; }
            public string Name { get; set; }
            //[ChoIgnoreMember]
            public ChoCurrency? Salary { get; set; }

            public override bool Equals(object obj)
            {
                var currency = obj as EmployeeRecWithCurrency;
                return currency != null &&
                       EqualityComparer<int?>.Default.Equals(Id, currency.Id) &&
                       Name == currency.Name &&
                       EqualityComparer<ChoCurrency?>.Default.Equals(Salary, currency.Salary);
            }

            public override int GetHashCode()
            {
                var hashCode = -1858601383;
                hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(Id);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<ChoCurrency?>.Default.GetHashCode(Salary);
                return hashCode;
            }
        }

        [Test]
        public static void CurrencyTest()
        {
            List<EmployeeRecWithCurrency> expected = new List<EmployeeRecWithCurrency> {
                new EmployeeRecWithCurrency { Id= 1 , Name="Carl", Salary = new ChoCurrency(100000)   },
                new EmployeeRecWithCurrency {Id=2,Name="Mark",Salary=new ChoCurrency(50000)   },
                new EmployeeRecWithCurrency {Id=3,Name="Tom" ,Salary=new ChoCurrency(1000)   }
            };
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRecWithCurrency>(reader))
            {
                writer.WriteLine("1,Carl,$100000");
                writer.WriteLine("2,Mark,$50000");
                writer.WriteLine("3,Tom,1000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void CurrencyDynamicTest()
        {
            List<ChoDynamicObject> expected = new List<ChoDynamicObject> {
                new ChoDynamicObject {{ "Id", "1" }, { "Name","Carl"}, { "Salary" , new ChoCurrency(100000)  } },
                new ChoDynamicObject {{ "Id", "2" }, { "Name","Mark"}, { "Salary" , new ChoCurrency(50000)  } },
                new ChoDynamicObject {{ "Id", "3" }, { "Name","Tom"}, { "Salary" , new ChoCurrency(1000)  } }
            };
            List<object> actual = new List<object>();

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Id", 1));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine("1,Carl,$100000");
                writer.WriteLine("2,Mark,$50000");
                writer.WriteLine("3,Tom,1000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                    actual.Add(rec);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void TimespanIssue()
        {
            string csv = @" A; B; C; D; E; F; G; H
a;b;;2021-05-06;e;11:00;3;9";

            //ChoType.IsTypeSimple = t => t == typeof(TimeSpan) ? (bool?)true : null;

            string expected = @"[
  {
    ""a"": ""a"",
    ""b"": ""b"",
    ""c"": null,
    ""d"": ""2021-05-06T00:00:00"",
    ""e"": ""e"",
    ""f"": ""11:00:00"",
    ""g"": ""3"",
    ""h"": ""9""
  }
]";
            using (var r = ChoCSVReader<MyClass1>.LoadText(csv)
                .WithFirstLineHeader()
                .WithDelimiter(";")
                //.WithField(f => f.f, valueConverter: o => TimeSpan.Parse(o.ToNString()))
                )
            {
                //r.Print();
                var recs = r.ToArray();
                var actual = JsonConvert.SerializeObject(recs, Newtonsoft.Json.Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        public class MyClass1
        {
            public string a { get; set; }

            public string b { get; set; }

            public string c { get; set; }

            public DateTime d { get; set; }

            public string e { get; set; }

            public TimeSpan f { get; set; }

            public string g { get; set; }

            public string h { get; set; }
        }

        [Test]
        public static void AsDataReaderTest()
        {
            List<string> expected = new List<string>()
            { "Id: 1, Name: Carl", "Id: 2, Name: Mark", "Id: 3, Name: Tom"};
            //            { new EmployeeRec{ Id = 1, Name = "Carl"}, new EmployeeRec{ Id = 2, Name = "Mark"}, new EmployeeRec{Id = 3, Name = "Tom"}};
            List<object> actual = new List<object>();

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRec>(reader))
            {
                writer.WriteLine("1,Carl");
                writer.WriteLine("2,Mark");
                writer.WriteLine("3,Tom");

                writer.Flush();
                stream.Position = 0;

                IDataReader dr = parser.AsDataReader();
                StringBuilder sb = new StringBuilder();
                while (dr.Read())
                {
                    actual.Add(String.Format("Id: {0}, Name: {1}", dr[0], dr[1]));
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void AsDataTableTest()
        {
            List<string> expected = new List<string>()
            { "Id: 1, Name: Carl", "Id: 2, Name: Mark", "Id: 3, Name: Tom"};
            /*            DataTable expected = new DataTable();
                        expected.Columns.Add("Id", typeof(int));
                        expected.Columns.Add("Name", typeof(string));
                        expected.Rows.Add(2, "Mark");
                        expected.Rows.Add(3, "Tom");
            */ // I found no Assert method for testing DataTable
            List<object> actual = new List<object>();


            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRec>(reader)
                .WithFirstLineHeader())
            {
                writer.WriteLine("id,name");
                writer.WriteLine("1,Carl");
                writer.WriteLine("2,Mark");
                writer.WriteLine("3,Tom");

                writer.Flush();
                stream.Position = 0;

                DataTable dt = parser.AsDataTable();

                foreach (DataRow dr in dt.Rows)
                {
                    actual.Add(String.Format("Id: {0}, Name: {1}", dr[0], dr[1]));
                    Console.WriteLine("Id: {0}, Name: {1}", dr[0], dr[1]);
                }
            }
            CollectionAssert.AreEqual(expected, actual);
        }


        public static void OldTest()
        {
            //var t = ChoTypeDescriptor.GetPropetyAttributes<ChoTypeConverterAttribute>(ChoTypeDescriptor.GetProperty<ChoTypeConverterAttribute>(typeof(EmployeeRecMeta), "Name")).ToArray();
            //return;

            //ChoMetadataObjectCache.Default.Attach(typeof(EmployeeRec), new EmployeeRecMeta());
            //string v = @"4,'123\r\n4,abc'";
            //foreach (var ss in v.SplitNTrim(",", ChoStringSplitOptions.None, '\''))
            //    Console.WriteLine(ss + "-");
            //return;

            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            //config.AutoDiscoverColumns = false;
            config.FileHeaderConfiguration.HasHeaderRecord = true;
            //config.CSVFileHeaderConfiguration.FillChar = '$';
            config.ThrowAndStopOnMissingField = false;
            //config.HasExcelSeparator = true;
            config.ColumnCountStrict = false;
            //config.MapRecordFields<EmployeeRec>();
            ChoCSVRecordFieldConfiguration idConfig = new ChoCSVRecordFieldConfiguration("Id", 1);
            idConfig.AddConverter(new IntConverter());
            config.CSVRecordFieldConfigurations.Add(idConfig);
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name1", 2));

            dynamic rec = new ExpandoObject();
            rec.Id = 1;
            rec.Name = "Raj";

            //using (var wr = new ChoCSVWriter("EmpOut.csv", config))
            //{
            //    wr.Write(new List<ExpandoObject>() { rec });
            //}

            //List<EmployeeRec> recs = new List<EmployeeRec>();
            //recs.Add(new EmployeeRec() { Id = 1, Name = "Raj" });
            //recs.Add(new EmployeeRec() { Id = 2, Name = "Mark" });

            //using (var stream = new MemoryStream())
            //using (var reader = new StreamReader(stream))
            //using (var writer = new StreamWriter(stream))
            //using (var parser = new ChoCSVWriter<EmployeeRec>(writer, config))
            //{
            //    parser.Write(recs);
            //    writer.Flush();
            //    stream.Position = 0;

            //    Console.WriteLine(reader.ReadToEnd());
            //}
            //return;

            //string txt = "Id, Name\r\n1, Mark";
            //foreach (var e in ChoCSVReader.LoadText(txt))
            //    Console.WriteLine(e.ToStringEx());
            //return;
            //dynamic row;
            //using (var stream = new MemoryStream())
            //using (var reader = new StreamReader(stream))
            //using (var writer = new StreamWriter(stream))
            //using (var parser = new ChoCSVReader(reader, config))
            //{
            //    //writer.WriteLine("Id,Name");
            //    writer.WriteLine("1,Carl");
            //    writer.WriteLine("2,Mark");
            //    writer.Flush();
            //    stream.Position = 0;

            //    while ((row = parser.Read()) != null)
            //    {
            //        Console.WriteLine(row.Id);
            //    }
            //}
            //return;

            //DataTable dt = new ChoCSVReader<EmployeeRec>("Emp.csv").AsDataTable();
            //var z = dt.Rows.Count;
            //return;

            foreach (var item in new ChoCSVReader<EmployeeRec>("Emp.csv"))
                Console.WriteLine(item.ToStringEx());
            return;

            //var reader = new ChoCSVReader<EmployeeRec>("Emp.csv");
            //var rec = (object)null;

            //while ((rec = reader.Read()) != null)
            //    Console.WriteLine(rec.ToStringEx());

            //var config = new ChoCSVRecordConfiguration(typeof(EmployeeRec));
            //var e = new ChoCSVReader("Emp.csv", config);
            //dynamic i;
            //while ((i = e.Read()) != null)
            //    Console.WriteLine(i.Id);

            ChoETLFramework.Initialize();
        }
    }
    public class IntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class NameFormatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return String.Format("{0}zzzz".FormatString(value));
        }
    }

    public class Name1Formatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return String.Format("{0}@@@@".FormatString(value));
        }
    }

    //[ChoCSVFileHeader()]
    [ChoCSVRecordObject(Encoding = "UTF-32", ErrorMode = ChoErrorMode.ReportAndContinue,
    IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any, ThrowAndStopOnMissingField = false,
        ObjectValidationMode = ChoObjectValidationMode.Off)]
    public class EmployeeRecMeta : IChoNotifyRecordRead //, IChoValidatable
    {
        [ChoCSVRecordField(1, FieldName = "id", ErrorMode = ChoErrorMode.ReportAndContinue)]
        [ChoTypeConverter(typeof(IntConverter))]
        [Range(1, 1, ErrorMessage = "Id must be > 0.")]
        //[ChoFallbackValue(1)]
        public int Id { get; set; }
        [ChoCSVRecordField(2, FieldName = "Name")]
        //[StringLength(1)]
        [DefaultValue("ZZZ")]
        [ChoFallbackValue("XXX")]
        [ChoTypeConverter(typeof(NameFormatter))]
        [ChoTypeConverter(typeof(Name1Formatter))]
        public string Name { get; set; }

        [ChoCSVRecordField]
        public string City { get; set; }

        public bool AfterRecordFieldLoad(object target, long index, string propName, object value)
        {
            throw new NotImplementedException();
        }

        public bool AfterRecordLoad(object target, long index, object source, ref bool skip)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordLoad(object target, long index, ref object source)
        {
            throw new NotImplementedException();
        }

        public bool BeginLoad(object source)
        {
            throw new NotImplementedException();
        }

        public void EndLoad(object source)
        {
            throw new NotImplementedException();
        }

        public bool RecordFieldLoadError(object target, long index, string propName, object value, Exception ex)
        {
            return true;
        }

        public bool RecordLoadError(object target, long index, object source, Exception ex)
        {
            throw new NotImplementedException();
        }

        public bool SkipUntil(long index, object source)
        {
            return index <= 2 ? true : false;
        }

        public bool DoWhile(long index, object source)
        {
            throw new NotImplementedException();
        }
    }

    [MetadataType(typeof(EmployeeRecMeta))]
    //[ChoCSVFileHeader(TrimOption = ChoFieldValueTrimOption.None)]
    [ChoCSVRecordObject(ErrorMode = ChoErrorMode.IgnoreAndContinue,
    IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any, ThrowAndStopOnMissingField = false)]
    public partial class EmployeeRec //: IChoNotifyRecordRead, IChoValidatable
    {
        [ChoCSVRecordField(1, FieldName = "Id")]
        //[ChoTypeConverter(typeof(IntConverter))]
        //[Range(1, int.MaxValue, ErrorMessage = "Id must be > 0.")]
        //[ChoFallbackValue(1)]
        public int Id { get; set; }

        [ChoCSVRecordField(2, FieldName = "Name")]
        //[Required]
        //[DefaultValue("ZZZ")]
        //[ChoFallbackValue("XXX")]
        public string Name { get; set; }

        //[ChoCSVRecordField(3, FieldName = "Salary")]
        public int Salary { get; set; }
        //[ChoCSVRecordField(3, FieldName = "Address")]
        //public string Address { get; set; }

        [ChoCSVRecordField(3, FieldName = "City")]
        public string City { get; set; }

        public bool AfterRecordFieldLoad(object target, long index, string propName, object value)
        {
            throw new NotImplementedException();
        }

        public bool AfterRecordLoad(object target, long index, object source)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordFieldLoad(object target, long index, string propName, ref object value)
        {
            throw new NotImplementedException();
        }

        public bool BeforeRecordLoad(object target, long index, ref object source)
        {
            throw new NotImplementedException();
        }

        public bool BeginLoad(object source)
        {
            throw new NotImplementedException();
        }

        public void EndLoad(object source)
        {
            throw new NotImplementedException();
        }

        public bool RecordFieldLoadError(object target, long index, string propName, object value, Exception ex)
        {
            throw new NotImplementedException();
        }

        public bool RecordLoadError(object target, long index, object source, Exception ex)
        {
            throw new NotImplementedException();
        }

        public bool TryValidate(object target, ICollection<ValidationResult> validationResults)
        {
            return true;
        }

        public bool TryValidateFor(object target, string memberName, ICollection<ValidationResult> validationResults)
        {
            return true;
        }

        public void Validate(object target)
        {
        }

        public void ValidateFor(object target, string memberName)
        {
        }
    }
}
