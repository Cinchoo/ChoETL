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

namespace ChoCSVReaderTest
{
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
            using (var parser = new ChoCSVReader("EmpDuplicates.csv").WithFirstLineHeader())
            {
                foreach (dynamic c in parser.GroupBy(r => r.Id).Where(g => g.Count() > 1).Select(g => g.FirstOrDefault()))
                    Console.WriteLine(c.DumpAsJson());
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

        static void Main(string[] args)
        {
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
            using (var parser = new ChoCSVReader(reader).WithDelimiter(",").WithFirstLineHeader().WithField("Id", typeof(int)).
                WithField("Name", typeof(string), fieldName: "@Name $1").ColumnOrderStrict())
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

        public class EmployeeRecWithCurrency
        {
            public int? Id { get; set; }
            public string Name { get; set; }
            [ChoIgnoreMember]
            public ChoCurrency Salary { get; set; }
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

    //[MetadataType(typeof(EmployeeRecMeta))]
    [ChoCSVFileHeader(TrimOption = ChoFieldValueTrimOption.None)]
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
