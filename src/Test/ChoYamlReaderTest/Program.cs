using ChoETL;
using SharpYaml.Serialization;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using System.Net;
using static ChoYamlReaderTest.Program;

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
            using (var r = ChoYamlReader<Item>.LoadText(yamlText2)
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

        [Test]
        public static void ListTest()
        {
            string yaml = @"
jedis:
  - Yoda
  - Qui-Gon Jinn
  - Obi-Wan Kenobi
  - Luke Skywalker
";
            List<string> expected = new List<string>();
            expected.Add("Yoda");
            expected.Add("Qui-Gon Jinn");
            expected.Add("Obi-Wan Kenobi");
            expected.Add("Luke Skywalker");

            List<string> actual = new List<string>();
            using (var r = ChoYamlReader<string>.LoadText(yaml)
                .WithYamlPath("$.jedis[*]")
                )
            {
                actual.AddRange(r.ToArray());
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void DictTest()
        {
            string yaml = @"
jedi:
  name: Obi-Wan Kenobi
  home-planet: Stewjon
  species: human
  master: Qui-Gon Jinn
  height: 1.82m";

            Dictionary<string, string> expected = new Dictionary<string, string>();
            expected.Add("name", "Obi-Wan Kenobi");
            expected.Add("home-planet", "Stewjon");
            expected.Add("species", "human");
            expected.Add("master", "Qui-Gon Jinn");
            expected.Add("height", "1.82m");

            Dictionary<string, string> actual = new Dictionary<string, string>();
            using (var r = ChoYamlReader<IDictionary<string, object>>.LoadText(yaml)
                .WithYamlPath("$.jedi")
                )
            {
                foreach (var kvp in r.FirstOrDefault())
                    actual.Add(kvp.Key, kvp.Value as string);
            }
            CollectionAssert.AreEqual(expected, actual);
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
  - http://yahoo.com/
 
  # second item of `requests` list is a dictionary
  - url: http://example.com/
    method: GET
";

            using (var r = ChoYamlReader.LoadText(yaml)
                .WithYamlPath("$.requests[*]")
                .WithField(ChoYamlReader.NODE_VALUE)
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

        static void DeserializeEscapedChar()
        {
            string yaml = @"0.1 : Value 1
0.2 : Value 2
0.3 : Value 3";

            using (var r = ChoYamlReader.LoadText(yaml)
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }

        }

        public class UserScoreX
        {
            public int id { get; set; }
            public int value { get; set; }
        }

        public class UserInfoX
        {
            public string name { get; set; }
            public string teamname { get; set; }
            public string email { get; set; }
            public int[] players { get; set; }
            public UserScoreX[] scores { get; set; }
        }

        static void SelectiveNodeTestX()
        {
            string yaml = @"
users:
    - name: 1
      teamname: Tom
      email: tom@gmail.com
      players: [1, 2]
      scores:
        - id: 1
          value: 100
        - id: 2
          value: 200
";
            using (var r = ChoYamlReader<UserInfoX>.LoadText(yaml)
                .WithYamlPath("$.users[*]")
                .WithField(f => f.scores, itemConverter: o => o)
            )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }
        public class Root
        {
            public PeopleInfo people { get; set; }
            public CityInfo[] cities { get; set; }
        }
        public class PeopleInfo
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class CityInfo
        {
            public string id { get; set; }
            public string city { get; set; }
            public string state { get; set; }
            public string country { get; set; }
        }

        static void TestTwoLists()
        {
            string yaml = @"
creation: 2020-12-26
author: YannZeRookie
people:
  - id: 1
    name: Tom
  - id: 2
    name: Mark
cities:
  - id: 1
    city: San Francisco
    state: CA
    country: USA
  - id: 2
    city: Palo Alto
    state: CA
    country: USA
  - id: 3
    city: Minneapolis
    state: MN
    country: USA
";
            using (var r = ChoYamlReader<Root>.LoadText(yaml)
            )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        class OwnerData
        {
            public PossessionData[] Possessions { get; set; }
        }

        class PossessionData
        {
            public string Type { get; set; }
            public IDictionary<string, object> Description { get; set; }
        }

        static void ReadDynamicData()
        {
            string yaml = @"
possessions:
- type: car
  description:
    color: blue
    doors: 4
- type: computer
  description:
    disk: 1 TB
    memory: 16 MB
";
            using (var parser = ChoYamlReader<OwnerData>.LoadText(yaml))
            {
                foreach (var e in parser)
                {
                    string carColor = (string)e.Possessions[0].Description["color"];   // blue
                    foreach (var p in e.Possessions)
                    {
                        Console.WriteLine(p.Description.Dump());
                    }
                }
            }
        }

        public class Book
        {
            public string Title { get; set; }
            public List<Author> Authors { get; set; }
        }

        public class Author
        {
            public string Name { get; set; }
            public List<Book> Books { get; set; }
        }

        static void CircularRefRead()
        {
            List<Book> books1 = new List<Book> { new Book() { Title = "title1", Authors = new List<Author>() } };
            Author author1 = new Author()
            {
                Name = "name1",
                Books = books1
            };
            books1[0].Authors.Add(author1);

            var settings = new SerializerSettings { EmitAlias = true, EmitShortTypeName = true };
            var serializer = new Serializer(settings);
            var text = serializer.Serialize(author1);
            Console.WriteLine(text);

            StringBuilder yaml1 = new StringBuilder();
            var cf = new ChoYamlRecordConfiguration() { UseYamlSerialization = true };
            cf.YamlSerializerSettings.EmitTags = true;
            Console.WriteLine(ChoYamlWriter.Serialize<Author>(author1, cf));
            return;

            string yaml = @"
&o0 !ChoYamlReaderTest.Program+Author
Name: name1
Books:
  - Title: title1
    Authors:
      - *o0
";
            //var settings = new SerializerSettings { EmitAlias = true, EmitShortTypeName = true };
            //settings.RegisterAssembly(typeof(Author).Assembly);
            //settings.RegisterTagMapping("!o0", typeof(Author), true);
            //var serializer = new Serializer(settings);
            //var text = serializer.Deserialize(yaml, typeof(Author));
            //Console.WriteLine(text.Dump());


            //            using (var r = ChoYamlReader<Author>.LoadText(yaml)
            //                .YamlSerializerSettings(y => y.RegisterAssembly(typeof(Author).Assembly))
            //)
            //            {
            //                foreach (var rec in r)
            //                    Console.WriteLine(rec.Dump());
            //            }

        }

        public class ConfigOne
        {
            public string Name { get; set; }
            public string Stuff { get; set; }
        }

        static void SelectiveNodeLoad()
        {
            string yaml = @"
config_one:
  name: foo
  stuff: value

config_two:
  name: bar
  random: value
";

            Console.WriteLine(ChoYamlReader.DeserializeText<ConfigOne>(yaml, "config_one").FirstOrDefault().Dump());
            return;
            using (var r = ChoYamlReader<ConfigOne>.LoadText(yaml)
                .WithYamlPath("config_one")
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }

        }

        static void Test2()
        {
            string yaml = @"
gc:
  - clean:
      - location: blah blah
        pattern: blah
      - location: blah blah
        pattern: blah
    dependencies:
      location: blah
  - solution:
      results: blah
      framework: blah
    test:
      configuration: blah
      results: blah blah
";

            using (var r = ChoYamlReader.LoadText(yaml))
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        [ChoYamlRecordObject]
        public class Root142
        {
            [ChoYamlRecordField(FieldName = "version")]
            public string Version { get; set; }

            [ChoYamlRecordField(FieldName = "test")]
            public Tester test { get; set; }

            [ChoYamlRecordField(FieldName = "test2")]
            public Tester test2 { get; set; }

            [ChoYamlRecordField(FieldName = "config")]
            public Config Exas { get; set; }

        }

        public class Tester
        {
            [ChoYamlRecordField(FieldName = "cnf.assit")]
            public string Assit { get; set; }

            [ChoYamlRecordField(FieldName = "cnf.language")]
            public string Lang { get; set; } = "English";

            [ChoYamlRecordField(FieldName = "cnf.enable")]
            public string Layout { get; set; } = "model.two";

        }

        public class Config
        {
            [ChoYamlRecordField(FieldName = "cnf.assit")]
            public string Assit { get; set; }

            [ChoYamlRecordField(FieldName = "cnf.language")]
            public string Lang { get; set; } = "English";

            [ChoYamlRecordField(FieldName = "cnf.enable")]
            public string Model { get; set; } = "model.five";
        }

        static void Issue142()
        {
            string yaml = @"
version: 1.00

test:
    cnf.assit: Test1
    cnf.language: English1
    cnf.enable: Default1

test2:
    cnf.assit: Test2
    cnf.language: English2
    cnf.enable: Default2

config:
    cnf.assit: CCC
    cnf.language: English
    cnf.enable: Default
";
            ChoYamlRecordConfiguration config = new ChoYamlRecordConfiguration();
            config.ErrorMode = ChoErrorMode.ReportAndContinue;
            config.Encoding = Encoding.UTF8;

            using (var parser = ChoYamlReader<Root142>.LoadText(yaml, config))
            {
                foreach (var e in parser)
                {
                    Console.WriteLine(e.Dump());
                }
            }
        }

        public class Auto
        {
            public string Description { get; set; }
            public string Name { get; set; }
            public List<IVehicle> Vehicles { get; set; }
        }

        public interface IVehicle
        {
            string Type { get; set; }
            string Make { get; set; }
        }

        public class Car : IVehicle
        {
            public string Type { get; set; }
            public string Make { get; set; }
        }

        public class Truck : IVehicle
        {
            public string Type { get; set; }
            public string Make { get; set; }
        }

        static void InterfaceTest()
        {
            string yaml = @"
Description: All automobiles
Name: All autos
Vehicles: 
    - type: Car
      make: BMW
    - type: Truck
      make: Volvo
";

            //ChoYamlReader.DeserializeText<Auto>(yaml, configuration:
            //    new ChoYamlRecordConfiguration().Map<Auto, Auto.F>(f => f.Vehicles, m => m))
            using (var parser = ChoYamlReader<Auto>.LoadText(yaml)
                .WithField(f => f.Vehicles, itemRecordTypeSelector: (o) =>
                {
                    dynamic rec = ChoDynamicObject.New(o as IDictionary<object, object>);
                    if (rec.type == "Car")
                        return typeof(Car);
                    else
                        return typeof(Truck);
                })
                )
            {
                foreach (var e in parser)
                {
                    Console.WriteLine(e.Dump());
                }
            }

        }

        public class Thing
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }

            public List<IComponentX> Components { get; set; }
        }

        public class Thing1
        {
            public string Type { get; set; }
            public string Name { get; set; }
            public string DisplayName { get; set; }

            public List<IComponentX> Components { get; set; }
        }

        public interface IComponentX { }

        public class Explosive : IComponentX
        {
            public int Damage { get; set; }
            public int Range { get; set; }
        }
        public class Burnable : IComponentX
        {
            public int FlameSize { get; set; }
            public int HealthThreshold { get; set; }
        }

        static void DeserializeSubClassedItems()
        {
            string yaml = @"
Type: Item
Name: fuel_cansiter
DisplayName: Fuel Canister
Sprite: fuel_canister_1
MaxStackSize: 10
MaxHP: 15
Value: 30
Components:
      - Explosive:
        - Damage: 10
        - Range: 25
      - Burnable:
        - FlameSize: 10
        - HealthThreshold: 0.4
";


            using (var r = ChoYamlReader<Thing>.LoadText(yaml)
                .WithField(f => f.Components, itemConverter: (o) =>
                {
                    dynamic rec = ChoDynamicObject.New(o as IDictionary<object, object>);
                    if (rec.ContainsKey(nameof(Explosive)))
                    {
                        IList list = rec.Explosive.ToArray();
                        var dict = list.OfType<IDictionary<object, object>>().SelectMany(d => d.ToList()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        return dict.ConvertToObject(typeof(Explosive));
                    }
                    else if (rec.ContainsKey(nameof(Burnable)))
                    {
                        IList list = rec.Burnable.ToArray();
                        var dict = list.OfType<IDictionary<object, object>>().SelectMany(d => d.ToList()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                        return dict.ConvertToObject(typeof(Burnable));
                    }
                    else
                        return null;
                })
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
            }
        }

        static void DeserializeTypedYaml()
        {
            string yaml = @"
!Item
Name: fuel_cansiter
DisplayName: Fuel Canister
Components:
      - !Explosive
        Damage: 10
        Range: 25
      - !Burnable
        FlameSize: 10
        HealthThreshold: 6
";


            List<Thing1> list = null;
            using (var r = ChoYamlReader<Thing1>.LoadText(yaml)
                .WithTagMapping("!Item", typeof(Thing1))
                .WithTagMapping("!Explosive", typeof(Explosive))
                .WithTagMapping("!Burnable", typeof(Burnable))
                )
            {
                list = r.ToList();
                foreach (var rec in list)
                    Console.WriteLine(rec.Dump());
            }

            StringBuilder yamlOut = new StringBuilder();
            using (var w = new ChoYamlWriter<Thing1>(yamlOut)
                .WithTagMapping("!Item", typeof(Thing1))
                .WithTagMapping("!Explosive", typeof(Explosive))
                .WithTagMapping("!Burnable", typeof(Burnable))
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Configure(c => c.YamlSerializerSettings.EmitTags = true)
                .UseYamlSerialization()
                )
            {
                w.Write(list);
            }

            Console.WriteLine(yamlOut.ToString());
        }

        static void RemoveYamlField()
        {
            string yaml = @"
field1: 'test1'
field2: 'test2'
field3: 'test3'
";

            StringBuilder yamlOut = new StringBuilder();

            using (var r = ChoYamlReader.LoadText(yaml))
            {
                using (var w = new ChoYamlWriter(yamlOut))
                    w.Write(r.Select(rec =>
                    {
                        rec.Remove("field2"); return rec;
                    }
                    ));
            }

            Console.WriteLine(yamlOut.ToString());
        }

        [ChoYamlTagMap("!!")]
        public class ControlGroup
        {
            public string name { get; set; }
        }

        static void SecondaryTagTest()
        {
            string yaml = @"
!!ControlGroup
name: myGroup
";
            //settings.RegisterAssembly(typeof(Author).Assembly);
            using (var r = ChoYamlReader<ControlGroup>.LoadText(yaml)
                //.WithTagMapping("tag:yaml.org,2002:ControlGroup", typeof(ControlGroup))
                //.Configure(c => c.TurnOffAutoRegisterTagMap = true)
                .UseYamlSerialization()
                )
            {
                r.Print();
            }

        }

        static void Yaml2JsonTypeIssue()
        {
            string yaml = @"
EntityId:
    type: integer
    example: 1245

EntityIds:
    type: array
    items:
        $ref: EntityId
    example: [152, 6542, 23]
    isActive: true
";

            using (var r = ChoYamlReader.LoadText(yaml))
            {
                using (var w = new ChoJSONWriter(Console.Out)
                    .SupportMultipleContent(true)
                    .SingleElement()
                    )
                    w.Write(r);
            }
        }
        public class StrInterp
        {
            public string Template { get; set; }
            public IList<object> Args { get; set; }
        }
        static void NestedObject()
        {
            string yaml = @"Blah: !Str
  Template: ""My {0} says {1}""
  Args: [""dog"", !Str { Template: ""{0} and {1}"", Args: [""woof"", ""arf""] }]";

            using (var r = ChoYamlReader<StrInterp>.LoadText(yaml)
                .WithYamlPath("$Blah")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .WithTagMapping("!Str", typeof(StrInterp))
                )
            {
                var x = r.FirstOrDefault();
                x.Print();
            }
        }


        private const string yamlText3 = @"---
            receipt:    Oz-Ware Purchase Invoice
            date:        07/02/2019
...
---
            receipt:    Oz-Ware Purchase Invoice1
            date:        09/02/2019
";

        public class CustomerWithDate
        {
            public string Receipt { get; set; }
            public DateTime Date { get; set; }
        }

        static void DifferentDateFormatTest()
        {
            CultureInfo newCulture = CultureInfo.CreateSpecificCulture("en-GB");
            //Thread.CurrentThread.CurrentCulture = newCulture;

            StringBuilder json = new StringBuilder();
            using (var r = ChoYamlReader<CustomerWithDate>.LoadText(yamlText3))
            {
                r.First().Date.ToString("yyyy-MMM-dd").Print();
                return;
                using (var w = new ChoJSONWriter(json))
                    w.Write(r);
            }
            Console.WriteLine(json.ToString());
        }

        static void Test10()
        {
            string yaml = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1679279224369103304
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 4601260661584130271}
  - component: {fileID: 935130553128839326}
  - component: {fileID: 9068261088558206342}
  - component: {fileID: 5522740863684393372}
  m_Layer: 0
  m_Name: Cube (1)
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &4601260661584130271
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1679279224369103304}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: 6194676242869225411}
  m_Father: {fileID: 0}
  m_RootOrder: 0
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}";

            using (var r = ChoYamlReader.LoadText(yaml)
                .Configure(c => c.YamlTagMapResolver = (s) =>
                {
                    return s;
                })
                )
            {
                r.Print();
            }
        }

        public class Jobs
        {
            [YamlMember("job")]
            public string Job { get; set; }

            [YamlMember("displayName")]
            public string DisplayName { get; set; }

            [YamlMember("pool")]
            [ChoTypeConverter(typeof(PoolConverter))]
            public Pool Pool { get; set; }
            public override bool Equals(object other)
            {
                var toCompareWith = other as Jobs;
                if (toCompareWith == null)
                    return false;
                return this.Job == toCompareWith.Job &&
                   this.DisplayName == toCompareWith.DisplayName && this.Pool.Equals(toCompareWith.Pool);
            }
            public override int GetHashCode()
            {
                return new { Job, DisplayName, Pool }.GetHashCode();
            }
        }

        public class Pool
        {
            [YamlMember("name")]
            public string Name { get; set; }
            public override bool Equals(object other)
            {
                var toCompareWith = other as Pool;
                if (toCompareWith == null)
                    return false;
                return this.Name == toCompareWith.Name;
            }
            public override int GetHashCode()
            {
                return new { Name }.GetHashCode();
            }
        }

        public class PoolConverter : IChoValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is Pool)
                    return value;
                else if (value is string)
                    return new Pool { Name = value as string };
                else
                    return null;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public static void MixedMemberValueTest()
        {
            string yaml1 = @"
Jobs:
  - job: Job1
    displayName: DisplayName1
    pool:
      - name: firstPool

  - job: Job2
    displayName: DisplayName2
    pool: secondPool 
";
            List<Jobs> expected = new List<Jobs>
            {
                new Jobs { Job = "Job1", DisplayName = "DisplayName1", Pool = new Pool { Name = "firstPool" } },
                new Jobs { Job = "Job2", DisplayName = "DisplayName2", Pool = new Pool { Name = "secondPool" }},
            };

            List<Jobs> actual = new List<Jobs>();
            using (var r = ChoYamlReader<Jobs>.LoadText(yaml1)
                .WithYamlPath("Jobs[*]")
                //.WithField(f => f.Pool, valueConverter: o => o is Pool ? o : new Pool() { Name = o.ToNString() }, itemConverter: o => new Pool() { Name = "X"})
                )
            {
                actual.AddRange(r.ToArray());
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public static void KeyValuePairTest()
        {
            string yaml = @"
- key1: value1
- key2: value2
- key3: value3
";
            Dictionary<string, string> expected = new Dictionary<string, string>();
            expected.Add("key1", "value1");
            expected.Add("key2", "value2");
            expected.Add("key3", "value3");

            Dictionary<string, string> actual = new Dictionary<string, string>();
            using (var r = ChoYamlReader<Dictionary<string, object>>.LoadText(yaml)
                )
            {
                foreach (var x in r.ToArray())
                {
                    actual.Add(x.Keys.First(), x.Values.First() as string);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            KeyValuePairTest();
            return;

            //DeserializeTypedYaml();
            NestedObject();
            return;

            //DeserializeTypedYaml();
            DeserializeSubClassedItems();
            RemoveYamlField();
        }
    }
}

