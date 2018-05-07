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
using System.Windows.Data;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Security;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;

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
    }
    public class Site
    {
        [Required(ErrorMessage = "SiteID can't be null")]
        //[ChoCSVRecordField(1)]
        public int SiteID { get; set; }
        [Required]
        public int House { get; set; }
        [ChoValidateObject]
        public SiteAddress SiteAddress { get; set; }
        public int Apartment { get; set; }
    }

    [ChoMetadataRefType(typeof(Site))]
    public class SiteMetadata
    {
        public int SiteID { get; set; }
        public int House { get; set; }
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
            Attr.Str = ChoUtility.CastTo<int>(obj.Attr_Str);
            Attr.Agi = ChoUtility.CastTo<int>(obj.Attr_Agi);

            Per = new PlayerPer();
            Per.Lea = ChoUtility.CastTo<int>(obj.Per_Lea);
            Per.Wor = ChoUtility.CastTo<int>(obj.Per_Wor);


            Skills = new PlayerSkills();
            Skills.WR = ChoUtility.CastTo<int>(obj.Skills_WR);
            Skills.TE = ChoUtility.CastTo<int>(obj.Skills_TE);
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
    class Program
    {
        static void ConvertToNestedObjects()
        {
            using (var json = new ChoJSONWriter("nested.json").Configure(c => c.UseJSONSerialization = false))
            {
                using (var csv = new ChoCSVReader("nested.csv").WithFirstLineHeader()
                    .Configure(c => c.NestedColumnSeparator = '/')
                    )
                    json.Write(csv.ExternalSort(new ChoLamdaComparer<dynamic>((e1, e2) => String.Compare(e1.description, e2.description))));
            }

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

        static void LoadPlanets()
        {
            using (var p = new ChoCSVReader("planets.csv").WithFirstLineHeader().Configure(c => c.Comments = new string[] { "#" })
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
                .Setup(r => r.RecordLoadError += (o,e) =>
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
                foreach (dynamic rec in p.Take(12).ExternalSort(new ChoLamdaComparer<dynamic>((e1, e2) => String.Compare(e1.pl_letter, e2.pl_letter))))
                    Console.WriteLine(rec.rowid + " " + rec.pl_letter);

                Console.WriteLine(p.IsValid);
                //using (var w = new ChoJSONWriter("planets.json"))
                //{
                //    w.Write(p);
                //}
            }

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

        static void FindDuplicates()
        {
            using (var parser = new ChoCSVReader("EmpDuplicates.csv").WithFirstLineHeader()
                .Configure(c => c.MaxScanRows = 5)
                )
            {
                var dt = parser.AsDataTable();
                //foreach (dynamic c in parser.GroupBy(r => r.Id).Where(g => g.Count() > 1).Select(g => g.FirstOrDefault()))
                //    Console.WriteLine(c.DumpAsJson());
            }
        }

        static void NestedQuotes()
        {
            //using (var parser = new ChoCSVReader("NestedQuotes.csv")
            //    .WithFields("name", "desc")
            //    )
            //{
            //    foreach (dynamic x in parser)
            //        Console.WriteLine(x.name + "-" + x.desc);
            //}

            using (var parser = new ChoCSVReader("NestedQuotes.csv"))
            {
                foreach (dynamic x in parser)
                    Console.WriteLine(x[0] + "-" + x[1]);
            }
        }

        static void CustomNewLine()
        {

            using (var parser = new ChoCSVReader("CustomNewLine.csv")
                .WithDelimiter("~")
                .WithEOLDelimiter("#####")
                )
            {
                foreach (dynamic x in parser)
                    Console.WriteLine(x.DumpAsJson());
            }
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

        static void GetHeadersTest()
        {
            using (var p = new ChoCSVReader("emp.csv").WithFirstLineHeader())
            {
                p.Read();
                Console.WriteLine(String.Join(", ", p.Context.Headers));
            }
        }

        static void QuotesInQuoteTest()
        {
            using (var p = new ChoCSVReader("EmpQuoteInQuote.csv"))
            {
                foreach (dynamic rec in p)
                    Console.WriteLine(rec.DumpAsJson());
            }
        }

        static void ReportEmptyLines()
        {
            using (var p = new ChoCSVReader("EmptyLines.csv").WithFirstLineHeader()
                .Setup(s => s.EmptyLineFound += (o, e) =>
                {
                    Console.WriteLine(e.LineNo);
                })
                //.Configure(c => c.IgnoreEmptyLine = true)
                )
            {
                foreach (dynamic rec in p)
                    Console.WriteLine(rec.DumpAsJson());
            }
        }

        static void EmptyValueTest()
        {
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
            }
        }

        static void CDataDataSetTest()
        {
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
                var dt = parser.AsDataTable();
                //object rec;
                //while ((rec = parser.Read()) != null)
                //{
                //    Console.WriteLine(rec.ToStringEx());
                //}
            }
        }

        static void QuoteValueTest()
        {
            using (var p = new ChoCSVReader("empwithsalary.csv").WithFirstLineHeader()
                .WithField("Id")
                .WithField("Name", quoteField: false)
                )
            {
                foreach (dynamic rec in p)
                    Console.WriteLine(rec.DumpAsJson());
            }
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
        static void Sample1()
        {
            using (var r = new StreamReader("Sample1.csv"))
            {
                foreach (var p in new ChoCSVReader<CRIContactModel>(r).WithFirstLineHeader()
                    .Configure(c => c.ObjectValidationMode = ChoObjectValidationMode.ObjectLevel)
                    )
                {
                    Console.WriteLine(p.Dump());
                }
            }
        }

        static void Pontos()
        {
            foreach (dynamic rec in new ChoCSVReader("pontos.csv").WithHeaderLineAt(9)
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true)
                .Configure(c => c.CultureName = "es-ES")
                )
            {
                Console.WriteLine(String.Format("{0}", (string)rec["CPF/CNPJ"]));
            }
        }

        static void Sample2()
        {
            var recs = new ChoCSVReader("Sample2.csv").WithFirstLineHeader().ToList();
            return;
            foreach (var p in new ChoCSVReader("Sample2.csv").WithFirstLineHeader()
                .Configure(c => c.TreatCurrencyAsDecimal = false)
                //.Configure(c => c.MaxScanRows = 10)
                )
            {
                Console.WriteLine(p.Dump());
            }
        }

        static void Sample4()
        {
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
                Console.WriteLine(p.Where(rec => rec.old == "hello").Select(rec => rec.newuser).First());
            }
        }
        static void MergeCSV1()
        {
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
            Console.WriteLine(csv3.ToString());
        }

        static void Test1()
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

            string csv = @"4.1,AB,2018-02-16 15:41:39,152,36,""{""A"":{ ""a1"":""A1""},""B"":{ ""b1"":""B1""}}"",""{""X"":"""",""Y"":""ya""}"",20";

            Console.WriteLine(ChoCSVReader.LoadText(csv).First().Dump());
        }

        static void CombineColumns()
        {
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
                .Setup(s => s.AfterRecordFieldLoad += (o,e) =>
                {
                    if (e.PropertyName == "Column2")
                    {
                        dynamic r = e.Record as dynamic;
                        r[1] = new DateTime(((DateTime)r[0]).Year, ((DateTime)r[0]).Month, ((DateTime)r[0]).Day, ((DateTime)r[1]).Hour, ((DateTime)r[1]).Minute, ((DateTime)r[1]).Second);
                    }
                })
                )
            {
                Console.WriteLine(rec.Dump());
            }
        }

        static void DiffCSV()
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

            public string City
            {
                get;
                set;
            }
        }

        [ChoRecordTypeCode("1")]
        public class Manager : IEmployee
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
        static void InterfaceTest()
        {
            using (var p = new ChoCSVReader<IEmployee>("InterfaceTest.csv")
                .WithFirstLineHeader()
                //.MapRecordFields<Employee>()
                .WithRecordSelector(1, typeof(Employee), typeof(Manager))
                )
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }

        [ChoCSVRecordObject(NullValue = "#NULL#")]
        public class Emp
        {
            [ChoCSVRecordField(1)]
            public int Id { get; set; }
            [ChoCSVRecordField(2)]
            public string Name { get; set; }
            [ChoCSVRecordField(3, NullValue = "#NULL#")]
            public string City { get; set; }
        }

        public class LocationDefinition
        {
            public string PlaceName { get; set; }
            public double Longitude { get; set; }
            public double Latitude { get; set; }
            public double Elevation { get; set; }
        }
        public class CountDefinition
        {
            public DateTime Date { get; set; }
            public int Count { get; set; }
        }

        static void MultiRecordsInfile()
        {
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
                    Console.WriteLine(ChoUtility.Dump(rec));

            }
        }

        static void Sample3()
        {
            using (var p = new ChoCSVReader<Site>("Sample3.csv")
                            .ClearFields()
                            .WithField(m => m.SiteID)
                            .WithField(m => m.SiteAddress.City)
                .WithFirstLineHeader(true)
                )
            {
                //foreach (var rec in p.ExternalSort(new ChoLamdaComparer<Site>((e1, e2) => e1.SiteID - e1.SiteID)))
                //{

                //}
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
                //Exception ex;
                //Console.WriteLine("IsValid: " + p.IsValid(out ex));
            }
        }
        public static void POCOSort()
        {
            using (var dr = new ChoCSVReader<EmployeeRec>(@"Test.csv").WithFirstLineHeader()
                .WithField(c => c.Id, valueConverter: (v) => Convert.ToInt32(v as string))
                )
            {
                //foreach (var rec in dr.ExternalSort(new ChoLamdaComparer<EmployeeRec>((e1, e2) => DateTime.Compare(e1.AddedDate, e1.AddedDate))))
                //{
                //	Console.WriteLine(rec.CustId);
                //}
            }
        }
        public static void DynamicSort()
        {
            using (var dr = new ChoCSVReader(@"Test.csv").WithFirstLineHeader())
            {
                foreach (var rec in dr.ExternalSort(new ChoLamdaComparer<dynamic>((e1, e2) => DateTime.Compare(e1.AddedDate, e1.AddedDate))))
                {
                    Console.WriteLine(rec.CustId);
                }
            }
        }

        static void CharDiscTest()
        {
            var csv = @"31350.2,3750.9188,S,14458.8652,E,7.98,50817,0,2.3,0,23
31350.4,3750.9204,S1,14458.867,E,6.66,50817,0,2.3,0,23";

            using (var p = new ChoCSVReader(new StringReader(csv))
                //.Configure(c => c.MaxScanRows = 10)
                )
            {
				foreach (var rec in p)
					Console.WriteLine(rec.Dump());
			}
        }

        public class Customer
        {
            [ChoTypeConverter(typeof(ChoIntConverter), Parameters = "0000")]
            public int CustId { get; set; }
            public string Name { get; set; }
            public decimal Balance { get; set; }
            public DateTime AddedDate { get; set; }
        }

        static void ConverterTest()
        {
            var csv = @"0001, Tom, 12.001, 1/1/2018
0002, Mark, 100.001, 12/1/2018";

            using (var p = new ChoCSVReader<Customer>(new StringReader(csv))
                //.Configure(c => c.MaxScanRows = 10)
                )
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }

        }

        public class EmpIgnoreCase
        {
            public int ID { get; set; }
        }
        static void NullValueTest()
        {
            string csv = @"Id, Name, City
1, Tom, {NULL}
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";
            //using (var p = new ChoCSVReader<EmpIgnoreCase>(new StringReader(csv))
            //	.WithFirstLineHeader()
            //	.Configure(c => c.FileHeaderConfiguration.IgnoreCase = false)
            //	)
            //{
            //	foreach (var rec in p)
            //		Console.WriteLine(rec.Dump());
            //}
            //	return;
            StringBuilder csvOut = new StringBuilder();
            using (var cp2 = new ChoCSVReader(new StringReader(csv))
                .WithFirstLineHeader()
                .Configure(c => c.NullValue = "{NULL}")
                    .Configure(c => c.FileHeaderConfiguration.IgnoreCase = false)
                )
            {
                foreach (var rec in cp2)
                    Console.WriteLine(rec.Id);
                //using (var cw = new ChoCSVWriter(new StringWriter(csvOut))
                //	.WithFirstLineHeader()
                //	.Configure(c => c.NullValue = "{NULL}")
                //)
                //{
                //	cw.Write(cp2);
                //}
            }

            Console.WriteLine(csvOut.ToString());
        }

        static void Sample10()
        {
            string csv = @"institution_id,UNITID,school_id,gss_code,year,Institution_Name,hdg_inst,toc_code
88,209612,65,823,2015,Pacific University,1,2
606,122612,752,202,2015,University of San Francisco,2,2
606,122612,752,401,2015,University of San Francisco,2,2";

            using (var p = new ChoCSVReader(new StringReader(csv))
                .WithFirstLineHeader()
                )
            {
                Console.WriteLine(ChoJSONWriter.ToTextAll(p));
            }
        }

        static void DateFormatTest()
        {
            string csv = @"Id, DateCreated, IsActive
                1, 20180201, A
                2, 20171120, B";

            using (var p = new ChoCSVReader(new StringReader(csv))
                .WithFirstLineHeader()
                .WithField("Id", fieldType: typeof(int))
                .WithField("DateCreated", fieldType: typeof(DateTime), formatText: "yyyyMMdd")
                .WithField("IsActive", fieldType: typeof(bool), formatText: "A")
                )
            {
                foreach (var rec in p)
                    Console.WriteLine(rec.Dump());
            }
        }

		[ChoCSVFileHeader]
		public class Consumer
		{
			public int Id { get; set; }
			[DisplayFormat(DataFormatString = "yyyyMMdd")]
			public DateTime DateCreated { get; set; }
			[DisplayFormat(DataFormatString = "A")]
			public bool IsActive { get; set; }
		}

		static void DateFormatTestUsingPOCO()
		{
			string csv = @"Id, DateCreated, IsActive
                1, 20180201, A
                2, 20171120, B";

			using (var p = new ChoCSVReader<Consumer>(new StringReader(csv)))
			{
				foreach (var rec in p)
					Console.WriteLine(rec.Dump());
			}
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
		}

		static void DateFormatTestUsingOptInPOCO()
		{
			string csv = @"Id, DateCreated, IsActive
                1, 20180201, A
                2, 20171120, B";

			using (var p = new ChoCSVReader<ConsumerOptIn>(new StringReader(csv)))
			{
				foreach (var rec in p)
					Console.WriteLine(rec.Dump());
			}
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

		static void Sample20()
		{
			string csv = @"""acme"" ""1"" ""1 / 1 / 2015""
""contoso"" ""34"" ""1/2/2018""
";

			using (var p = new ChoCSVReader(new StringReader(csv))
				.WithDelimiter(" ")
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

		static void Sample21()
		{
			using (var p = new ChoCSVReader("020180412_045106Cropped.csv")
				.WithFirstLineHeader()
				.Configure(c => c.FileHeaderConfiguration.IgnoreCase = false)
				)
			{
				var dr = p.AsDataReader();
				var dt = new DataTable();
				dt.Load(dr);
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

		static void ReadHeaderAt5()
		{
			string csv = @"v3,vf,gf
v1,c,z1,e
name,q1,q2,q3
a,0,1,2-Data";

			using (var p = ChoCSVReader.LoadText(csv)
				.WithHeaderLineAt(3)
				)
			{
				foreach (var rec in p)
					Console.WriteLine(rec.DumpAsJson());
			}
		}

		static void CSV2XmlTest()
		{
			string csv = @"Id, Name, City
				1, Tom, NY
				2, Mark, NJ
				3, Lou, FL
				4, Smith, PA
				5, Raj, DC";

			StringBuilder sb = new StringBuilder();
			using (var p = ChoCSVReader.LoadText(csv).WithFirstLineHeader())
			{
				using (var w = new ChoXmlWriter(sb)
					.Configure(c => c.RootName = "Emps")
					.Configure(c => c.NodeName = "Emp")
					)
				{
					w.Write(p);
				}
			}

			Console.WriteLine(sb.ToString());
		}

        static void MapTest()
        {
            string csv = @"Id, Name, City
				1, Tom, NY
				2, Mark, NJ
				3, Lou, FL
				4, Smith, PA
				5, Raj, DC";

            StringBuilder sb = new StringBuilder();
            using (var p = ChoCSVReader<EmployeeRec>.LoadText(csv)
                .WithFirstLineHeader()
                .WithField(m => m.Id)
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

        static void VariableFieldsTest()
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

        public static void DelimitedImportReaderChoCsvTest()
        {
            var errors = new List<Exception>();
            var rowCount = 0;

            using (var stream = File.Open(@"BadFile.csv", FileMode.Open))
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

            Console.WriteLine("Errors: " + errors.Count);
            Console.WriteLine("Total: " + rowCount);
        }

		static void Join()
		{
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
						);

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

			Console.WriteLine(sb.ToString());
		}

        static void Main(string[] args)
        {
			Join();
            return;

			CSV2XmlTest();
			return;

			ReadHeaderAt5();
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
            var x1 = r1.DeserializeText(txt1).FirstOrDefault();
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
            CultureSpecificDateTimeTest();
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

        static void CSVToXmlNodeTest()
        {
            using (var csv = new ChoCSVReader("NodeData.csv").WithFirstLineHeader(true)
                .WithFields("ID", "NODE", "PROCESS_STATE", "PREV_TIME_STAMP")
                )
            {
                using (var xml = new ChoXmlWriter("NodeData.xml").WithXPath("data-set/PDA_DATA"))
                    xml.Write(csv);
            }


        }

        static void HierarchyCSV()
        {
            using (var p = new ChoCSVReader("Players.csv").WithFirstLineHeader())
            {
                using (var w = new ChoJSONWriter<Players>("Players.json").Configure(c => c.UseJSONSerialization = true).Configure(c => c.SupportMultipleContent = true))
                {
                    w.Write(new Players { players = p.Select(e => new Player(e)).ToArray() });
                }
            }
        }
        static void LookupTest()
        {
            var zipSortCodeDict = File.ReadAllLines("zipCodes.csv").ToDictionary(line => line.Split("  ")[0], line => line.Split("  ")[1]);

            //var zipSortCodeDict = new ChoCSVReader("zipCodes.csv").WithDelimiter("   ").WithFirstLineHeader().ToDictionary(kvp => kvp.ZipCode, kvp => kvp.SortCode);
            //foreach (var item in zipSortCodeDict)
            //    Console.WriteLine(ChoUtility.ToStringEx(item));
            //get the sort code
            string zipCode = "49876";
            string sortCode = zipSortCodeDict[zipCode];
            Console.WriteLine(sortCode);
        }
        static void MergeCSV()
        {
            using (var p = new ChoCSVReader("mergeinput.csv").WithFirstLineHeader())
            {
                var recs = p.Where(r => !String.IsNullOrEmpty(r.szItemId)).GroupBy(r => r.szItemId)
                    .Select(g => new
                    {
                        szItemId = g.Key,
                        szName = g.Where(i1 => !String.IsNullOrEmpty(i1.szName)).Select(i1 => i1.szName).FirstOrDefault(),
                        lRetailStoreID = g.Where(i1 => !String.IsNullOrEmpty(i1.lRetailStoreID)).Select(i1 => i1.lRetailStoreID).FirstOrDefault(),
                        szDesc = g.Where(i1 => !String.IsNullOrEmpty(i1.szDesc)).Select(i1 => i1.szDesc).FirstOrDefault()
                    });

                using (var o = new ChoCSVWriter("mergeoutput.csv").WithFirstLineHeader())
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
        public static void CSVWithJSON()
        {
            using (var parser = new ChoCSVReader<EmpWithJSON>("emp1.csv"))
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
                using (var jp = new ChoJSONWriter("emp1.json"))
                    jp.Write(parser.Select(i => new { i.Id, i.Name, i.product_version_id, i.product_version_name }));

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
        }
        public static void CultureSpecificDateTimeTest()
        {
            string csvData =
    @"Id,Date,Account,Amount,Subcategory,Memo
 1,09/05/2017,XXX XXXXXX,-29.00,FT , [Sample string]
 2,09/05/2017,XXX XXXXXX,-20.00,FT ,[Sample string]
 3,25/05/2017,XXX XXXXXX,-6.30,PAYMENT,[Sample string]";

            List<Transaction> result = new List<Transaction>();

            using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(csvData)))
            using (StreamReader sr = new StreamReader(ms))
            {
                var csv = new ChoCSVReader<Transaction>(sr).WithFirstLineHeader();
                csv.TraceSwitch = ChoETLFramework.TraceSwitchOff;
                //csv.Configuration.Culture = CultureInfo.GetCultureInfo("en-GB");
                foreach (var t in csv)
                    Console.WriteLine(string.Format("{0:dd-MMM-yyyy}  {1}  {2,6}  {3,-7}  {4}",
                        t.Date, t.Account, t.Amount, t.Subcategory, t.Memo));
            }
        }
        public class EmpDetail
        {
            [ChoCSVRecordField(1, FieldName = "company name")]
            public string COMPANY_NAME { get; set; }
        }

        static void QuotedCSVTest()
        {
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

            foreach (dynamic rec in new ChoCSVReader("EmpQuote.csv").WithFirstLineHeader())
            {
                Console.WriteLine(rec.COMPANY_NAME);
                Console.WriteLine(rec.COMPANY_TYPE);
            }
        }

        static void CSVToXml()
        {

        }

        static void ErrorHandling()
        {
            var parser1 = new ChoCSVReader<EmployeeRec>("empwithsalary.csv").WithFirstLineHeader();

            using (var parser = new ChoCSVReader<EmployeeRec>("empwithsalary.csv").WithFirstLineHeader())
            {
                parser.RecordFieldLoadError += (o, e) =>
                {
                    Console.Write(e.Exception.Message);
                    e.Handled = true;
                };
                foreach (var i in parser)
                    Console.WriteLine(i.ToStringEx());
            }
        }

        static void IgnoreLineTest()
        {
            using (var parser = new ChoCSVReader("IgnoreLineFile.csv").WithFirstLineHeader())
            {
                parser.Configuration.Encoding = Encoding.BigEndianUnicode;

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
                foreach (var e in parser)
                    Console.WriteLine(e.ToStringEx());
            }
        }

        static void MultiLineColumnValue()
        {
            using (var parser = new ChoCSVReader("MultiLineValue.csv").WithFirstLineHeader())
            {
                parser.Configuration.MayContainEOLInData = true;

                foreach (var e in parser)
                    Console.WriteLine(e.ToStringEx());
            }
        }

        static void LoadTextTest()
        {
            string txt = "Id, Name\r\n1, Mark";
            foreach (var e in ChoCSVReader.LoadText(txt).WithFirstLineHeader())
            {
                Console.WriteLine(ChoUtility.ToStringEx(e));
            }
        }

        static void QuickTest()
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

        static void QuickDynamicTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader(reader).WithDelimiter(",")
                .IgnoreHeader()
                //.WithField("Id", typeof(int))
                //.WithField("Name", typeof(string), fieldName: "@Name $1")
                //.ColumnOrderStrict()
                )
            {
                writer.WriteLine("Id,@Name $1,Salary");
                writer.WriteLine("1,Carl,1000");
                writer.WriteLine("2,Mark,2000");
                writer.WriteLine("3,Tom,3000");

                writer.Flush();
                stream.Position = 0;

                dynamic rec;
                while ((rec = parser.Read()) != null)
                {
                    //Console.WriteLine(rec.Name);
                    Console.WriteLine(((object)rec).ToStringEx());
                }
            }
        }

        static void DateTimeTest()
        {
            ChoTypeConverterFormatSpec.Instance.DateTimeFormat = "MMM dd, yyyy";

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
                writer.WriteLine(@"1,Carl,12345679,""Jan 01, 2011"",0");
                writer.WriteLine(@"2,Mark,50000,""Sep 23, 1995"",1");
                writer.WriteLine(@"3,Tom,150000,""Apr 10, 1999"",1");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    Console.WriteLine(row.ToStringEx());
            }
        }

        static void UsingLinqTest()
        {
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
                    Console.WriteLine(row.ToStringEx());
            }
        }

        static void BoolTest()
        {
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
                    Console.WriteLine(row.ToStringEx());
            }
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
        static void EnumTest()
        {
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
            using (var parser = new ChoCSVReader(reader, config))
            {
                writer.WriteLine(@"1,Carl,12345679,01/10/2016,Full Time Employee");
                writer.WriteLine("2,Mark,50000,10/01/1995,Temporary Employee");
                writer.WriteLine("3,Tom,150000,01/01/1940,Contract Employee");

                writer.Flush();
                stream.Position = 0;

                object row = null;

                while ((row = parser.Read()) != null)
                    Console.WriteLine(row.ToStringEx());
            }
        }

        static void UsingFormatSpecs()
        {
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
                    Console.WriteLine(row.ToStringEx());
            }
        }

        static void ValidationOverridePOCOTest()
        {
            ChoCSVRecordConfiguration config = new ChoCSVRecordConfiguration();
            var idConfig = new ChoCSVRecordFieldConfiguration("Id", 1);
            idConfig.Validators = new ValidationAttribute[] { new RequiredAttribute() };
            config.CSVRecordFieldConfigurations.Add(idConfig);
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Name", 2));
            config.CSVRecordFieldConfigurations.Add(new ChoCSVRecordFieldConfiguration("Salary", 3) { FieldType = typeof(ChoCurrency) });

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRecWithCurrency>(reader, config))
            {
                parser.Configuration.ObjectValidationMode = ChoObjectValidationMode.ObjectLevel;

                writer.WriteLine("1,Carl,$100000");
                writer.WriteLine("2,Mark,$50000");
                writer.WriteLine("3,Tom,1000");

                writer.Flush();
                stream.Position = 0;

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        public class EmployeeRecWithCDATA
        {
            public int? Id { get; set; }
            public ChoCDATA Name { get; set; }
            public ChoCurrency? Salary { get; set; }
        }

        public class EmployeeRecWithCurrency
        {
            public int? Id { get; set; }
            public string Name { get; set; }
            //[ChoIgnoreMember]
            public ChoCurrency? Salary { get; set; }
        }

        static void CurrencyTest()
        {
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
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void CurrencyDynamicTest()
        {
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
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }

        static void AsDataReaderTest()
        {
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
                while (dr.Read())
                {
                    Console.WriteLine("Id: {0}, Name: {1}", dr[0], dr[1]);
                }
            }
        }

        static void AsDataTableTest()
        {
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRec>(reader))
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
                    Console.WriteLine("Id: {0}, Name: {1}", dr[0], dr[1]);
                }
            }
        }


        private static void OldTest()
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
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoCSVReader<EmployeeRec>(reader))
            {
                writer.WriteLine("Id,Name");
                writer.WriteLine("1,Carl");
                writer.WriteLine("2,Mark");
                writer.Flush();
                stream.Position = 0;
                //var dr = parser.AsDataReader();
                //while (dr.Read())
                //{
                //    Console.WriteLine(dr[0]);
                //}
                object row1 = null;

                //parser.Configuration.ColumnCountStrict = true;
                while ((row1 = parser.Read()) != null)
                {
                    Console.WriteLine(row1.ToStringEx());
                }
            }
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

    [ChoCSVFileHeader()]
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
        [StringLength(1)]
        [DefaultValue("ZZZ")]
        [ChoFallbackValue("XXX")]
        [ChoTypeConverter(typeof(NameFormatter))]
        [ChoTypeConverter(typeof(Name1Formatter))]
        public string Name { get; set; }

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
            throw new NotImplementedException();
        }

        public bool DoWhile(long index, object source)
        {
            throw new NotImplementedException();
        }
    }

    //[MetadataType(typeof(EmployeeRecMeta))]
    //[ChoCSVFileHeader(TrimOption = ChoFieldValueTrimOption.None)]
    [ChoCSVRecordObject(ErrorMode = ChoErrorMode.IgnoreAndContinue,
    IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any, ThrowAndStopOnMissingField = false)]
    public partial class EmployeeRec //: IChoNotifyRecordRead, IChoValidatable
    {
        [ChoCSVRecordField(1, FieldName = "id")]
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
