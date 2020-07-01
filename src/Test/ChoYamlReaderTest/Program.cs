using ChoETL;
using SharpYaml.Serialization;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;

namespace ChoYamlReaderTest
{
    public enum Gender { Male, Female }
    public class Employee
    {
        public int Age { get; set; }
        public Gender Gender { get; set; }
    }
    public class Emp
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class A
    {
        public string Name { get; set; }
        [DefaultValue("tom@gmail.com")]
        public string Email { get; set; }
    }
    public class Customer
    {
        public string Receipt { get; set; }
        public DateTime Date { get; set; }
        [DisplayName("bill-to")]
        public Address BillTo { get; set; }
        [DisplayName("ship-To")]
        public Address ShipTo { get; set; }
        public List<Item> Items { get; set; }
        public string SpecialDelivery { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
    }

    public class Item
    {
        [DisplayName("part_no")]
        [DefaultValue("PART_X")]
        public string PartNo { get; set; }
    }
    public class MyModel
    {
        public FileConfig FileConfig { get; set; }
    }

    public class FileConfig
    {
        public string SourceFolder { get; set; }
        public string DestinationFolder { get; set; }
        public Scenario[] Scenarios { get; set; }
    }

    public class Scenario
    {
        public string Name { get; set; }
        //public List<Alteration> Alterations { get; set; }
    }

    public class Alteration
    {
        public string TableExtension { get; set; }
        public List<TableAlteration> Alterations { get; set; }
    }

    public class TableAlteration
    {
        public string Type { get; set; }
        public int SourceLineIndex { get; set; }
        public int DestinationLineIndex { get; set; }
        public string ColumnName { get; set; }
        public string NewValue { get; set; }
    }

    class Program
    {
        private const string yamlText1 = @"FileConfig: 
  sourceFolder: /home
  destinationFolder: /home/billy/my-test-case
  scenarios: 
  - name: first-scenario 
    alterations: 
    - tableExtension: ln
      alterations: 
      - type: copy-line
        sourceLineIndex: 0
        destinationLineIndex: 0
      - type: cell-change
        sourceLineIndex: 0
        columnName: FAKE_COL
        newValue: NEW_Value1
    - tableExtension: env
      alterations: 
      - type: cell-change
        sourceLineIndex: 0
        columnName: ID
        newValue: 10";


        private const string yamlText2 = @"---
            receipt:    Oz-Ware Purchase Invoice
            date:        2007-08-06
            customer:
                given:   Dorothy
                family:  Gale

            items:
                - part_no:   A4786
                  descrip:   Water Bucket (Filled)
                  price:     1.47
                  quantity:  4

                - part_no:   E1628
                  descrip:   High Heeled ""Ruby"" Slippers
                  price:     100.27
                  quantity:  1

            bill-to:  &id001
                street: |
                        123 Tornado Alley
                        Suite 16
                city:   East Westville
                state:  KS

            ship-to:  *id001

            specialDelivery:  >
                Follow the Yellow Brick
                Road to the Emerald City.
                Pay no attention to the
                man behind the curtain.
...
---
            receipt:    Oz-Ware Purchase Invoice1
            date:        2007-08-07
            customer:
                given:   Dorothy
                family:  Gale
";

        static void SelectiveNodeTest()
        {
            StringBuilder json = new StringBuilder();
            using (var r = ChoYamlReader.LoadText(yamlText2)
                .WithField("receipt")
                .WithField("date", fieldType: typeof(DateTimeOffset))
                )
            {
                //foreach (var rec in r)
                //    Console.WriteLine(rec.Dump());
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
        }


        static void YamlPathTest()
        {
            StringBuilder json = new StringBuilder();
            using (var r = ChoYamlReader< Item>.LoadText(yamlText2)
                .WithYamlPath("$items[*]")
                .IgnoreFieldValueMode(ChoIgnoreFieldValueMode.Empty)
                //.Configure(c => c.StringComparer = StringComparer.CurrentCulture)
                //.WithField("part_no", yamlPath: ".items[0].part_no")
                //.WithField("bill_street", yamlPath: ".bill-to.Street")

                //.WithField("price", fieldType: typeof(Double))
                )
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
        }

        static void Yaml2JSON()
        {
            StringBuilder json = new StringBuilder();
            using (var r = ChoYamlReader.LoadText(yamlText2))
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
        }

        static void Yaml2CSV()
        {
            StringBuilder csv = new StringBuilder();
            using (var r = ChoYamlReader.LoadText(yamlText2))
            {
                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    )
                    w.Write(r);
            }
            Console.WriteLine(csv.ToString());
        }

        static void YamlStreamTest()
        {
            YamlStream sr = new YamlStream();
            sr.Load(new StringReader(yamlText2));

            StringBuilder json = new StringBuilder();
            using (var r = new ChoYamlReader(sr)
                .WithField("receipt")
                .WithField("date", fieldType: typeof(DateTime))
                )
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
        }

        static void YamlObjectTest()
        {
            YamlStream sr = new YamlStream();
            sr.Load(new StringReader(yamlText2));

            StringBuilder json = new StringBuilder();
            using (var r = new ChoYamlReader(sr.Documents.First().RootNode)
                .WithField("receipt")
                .WithField("date", fieldType: typeof(DateTime))
                )
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
        }

        static void YamlDocTest()
        {
            YamlStream sr = new YamlStream();
            sr.Load(new StringReader(yamlText2));

            StringBuilder json = new StringBuilder();
            using (var r = new ChoYamlReader(sr.Documents.First())
                .WithField("receipt")
                .WithField("date", fieldType: typeof(DateTime))
                )
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
        }

        static void POCOTest()
        {
            StringBuilder json = new StringBuilder();
            using (var r = ChoYamlReader<Customer>.LoadText(yamlText2))
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
        }
        static void Test1()
        {
            using (var r = ChoYamlReader<MyModel>.LoadText(yamlText1))
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
                return;
                Console.WriteLine(ChoJSONWriter<MyModel>.ToTextAll(r));
            }
        }
        static void ToDataTableTest()
        {
            string yaml = @"
emps:
    - id: 1
      name: Tom

    - id: 2
      name: Mark
";

            using (var r = ChoYamlReader.LoadText(yaml)
                .WithYamlPath("$.emps[*]")
                )
            {
                var dt = r.AsDataTable();
                Console.WriteLine(dt.Dump());
            }
        }
        static void DefaultValueTest()
        {
            string yaml = @"name: buddhika";
            using (var r = ChoYamlReader<A>.LoadText(yaml))
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }

        }

        static void HelloWorldTest()
        {
            string yaml = @"
emps:
    - id: 1
      name: Tom

    - id: 2
      name: Mark
";

            using (var r = ChoYamlReader.LoadText(yaml)
                //.WithYamlPath("$.emps[*]")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void ListTest()
        {
            string yaml = @"
jedis:
  - Yoda
  - Qui-Gon Jinn
  - Obi-Wan Kenobi
  - Luke Skywalker
";

            using (var r = ChoYamlReader<string>.LoadText(yaml)
                .WithYamlPath("$.jedis[*]")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void DictTest()
        {
            string yaml = @"
jedi:
  name: Obi-Wan Kenobi
  home-planet: Stewjon
  species: human
  master: Qui-Gon Jinn
  height: 1.82m";

            using (var r = ChoYamlReader<IDictionary>.LoadText(yaml)
                .WithYamlPath("$.jedi")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void List2DictTest()
        {
            string yaml = @"
jedis:
  - Yoda
  - Qui-Gon Jinn
  - Obi-Wan Kenobi
  - Luke Skywalker
";

            using (var r = ChoYamlReader<IDictionary>.LoadText(yaml)
                .WithYamlPath("$.jedis")
                .Configure(c => c.CustomNodeSelecter = o =>
                {
                    dynamic d = o as dynamic;
                    var x = ((IList)d.Value).OfType<object>().ToDictionary(kvp => kvp.ToString(), kvp => kvp);
                    return x;
                })
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void NestedDictTest()
        {
            string yaml = @"
requests:
  # first item of `requests` list is just a string
  - http://example1.com/
 
  # second item of `requests` list is a dictionary
  - url: http://example.com/
    method: GET
";

            using (var r = ChoYamlReader.LoadText(yaml)
                .WithYamlPath("$.requests[*]")
                .WithField("Value")
                .WithField("url")
                .WithField("method")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }


        static void ArrayOfIntTest()
        {
            string yaml = @"episodes: [1, 2, 3, 4, 5, 6, 7]";

            using (var r = ChoYamlReader<int>.LoadText(yaml)
                .WithYamlPath("$.episodes[*]")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }
        static void EnumTest()
        {
            string yaml = @"
emps:
    - Age: 15
      Gender: Male

    - Age: 25
      Gender: Female
";

            using (var r = ChoYamlReader<Employee>.LoadText(yaml)
                .WithYamlPath("$.emps[*]")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void LoadDictKeysTest()
        {
            string yaml = @"
Age: 15
Gender: Male
";

            using (var r = ChoYamlReader.LoadText(yaml)
                .WithYamlPath("$.^")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }
        static void Yaml2XmlTest()
        {
    string yaml = @"
emps:
    - id: 1
      name: Tom

    - id: 2
      name: Mark
";

            StringBuilder xml = new StringBuilder();
            using (var r = ChoYamlReader.LoadText(yaml).WithYamlPath("$.emps[*]"))
            {
                using (var w = new ChoXmlWriter(xml)
                  .WithRootName("Emps")
                  .WithNodeName("Emp")
                  )
                    w.Write(r);
            }
            Console.WriteLine(xml.ToString());
        }
        static void ToComplexDataTableTest()
        {
            using (var r = ChoYamlReader.LoadText(yamlText2)
                )
            {
                var dt = r.AsDataTable();
                Console.WriteLine(dt.Dump());
            }
        }

        static void DeserializeObjectTest()
        {
            string yaml = @"
id: 1
name: Tom
";
            Console.WriteLine(ChoYamlReader.DeserializeText<Emp>(yaml).FirstOrDefault().Dump());
        }

        static void DeserializeCollectioTest()
        {
            string yaml = @"
emps: 
    - Tom
    - Mark
";
            var emps = ChoYamlReader.DeserializeText<string>(yaml, "$.emps[*]").ToList();
            Console.WriteLine(emps.Dump());
        }

        static void DeserializeDictTest()
        {
            string yaml = @"
id: 1
name: Tom
";
            Console.WriteLine(ChoYamlReader.DeserializeText<Dictionary<string, object>>(yaml).FirstOrDefault().Dump());
        }

        public class UserInfo
        {
            [ChoYamlRecordField(YamlPath = "$.name")]
            public string name { get; set; }
            [ChoYamlRecordField(YamlPath = "$.teamname")]
            public string teamname { get; set; }
            [ChoYamlRecordField(YamlPath = "$.email")]
            public string email { get; set; }
            [ChoYamlRecordField(YamlPath = "$.players")]
            public int[] players { get; set; }
        }
        static void SelectiveNodeTest1()
        {
            string yaml = @"
users:
    - name: 1
      teamname: Tom
      email: xx@gmail.com
      players: [1, 2]
";
            using (var r = ChoYamlReader<UserInfo>.LoadText(yaml)
                .WithYamlPath("$.users[*]")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;

            SelectiveNodeTest1();
        }
    }
}
