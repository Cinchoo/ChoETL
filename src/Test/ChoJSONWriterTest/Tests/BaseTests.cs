using ChoETL;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace ChoJSONWriterTest.Tests
{
    public static class BaseTests
    {
        public enum Gender { Male, Female }

        public class Person
        {
            public int Age { get; set; }
            [ChoTypeConverter(typeof(ChoEnumNameConverter))]
            public Gender Gender { get; set; }
        }

        [Test]
        public static void EnumTest()
        {
            string expected = @"[
  {
    ""Age"": 35,
    ""Gender"": ""Female""
  }
]";
            StringBuilder actual = new StringBuilder();
            using (var w = new ChoJSONWriter<Person>(actual))
            {
                w.Write(new Person
                {
                    Age = 35,
                    Gender = Gender.Female,
                });
            }

            Assert.AreEqual(expected, actual.ToString());
        }

        public class Car
        {
            // included in JSON
            public string Model { get; set; }
            public DateTime Year { get; set; }
            public List<string> Features { get; set; }

            // ignored
            [JsonIgnore]
            public DateTime LastModified { get; set; }
        }

        [Test]
        public static void IgnorePropertyUsingJsonIgnoreTest()
        {
            string expected = @"[
  {
    ""Model"": ""BMW"",
    ""Year"": ""2003-05-24T00:00:00"",
    ""Features"": [
      ""F1"",
      ""F2""
    ]
  }
]";
            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<Car>(json)
                )
            {
                w.Write(new Car
                {
                    Model = "BMW",
                    Year = new DateTime(2003, 05, 24),
                    Features = new List<string>() { "F1", "F2" },
                    LastModified = DateTime.Now,
                });
            }

            string actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }


        public class Car1
        {
            // included in JSON
            public string Model { get; set; }
            public DateTime Year { get; set; }
            public List<string> Features { get; set; }

            // ignored
            [ChoIgnoreMember]
            public DateTime LastModified { get; set; }
        }

        [Test]
        public static void IgnorePropertyUsingChoIgnoreMemberTest()
        {
            string expected = @"[
  {
    ""Model"": ""BMW"",
    ""Year"": ""2003-05-24T00:00:00"",
    ""Features"": [
      ""F1"",
      ""F2""
    ]
  }
]";
            StringBuilder json = new StringBuilder();
            using (var w = new ChoJSONWriter<Car1>(json)
                )
            {
                w.Write(new Car1
                {
                    Model = "BMW",
                    Year = new DateTime(2003, 05, 24),
                    Features = new List<string>() { "F1", "F2" },
                    LastModified = DateTime.Now,
                });
            }

            string actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void CSV2JSON1()
        {
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

            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";
            StringBuilder json = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(p);
            }

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void CSV2JSON_WIthDupOrNoColumnNames()
        {
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

            string csv = @"Id, Name,
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";
            StringBuilder json = new StringBuilder();
            using (var p = ChoCSVReader.LoadText(csv)
                .WithField("Id", position: 1)
                .WithField("Name", position: 2)
                .WithField("City", position: 3)
                .WithFirstLineHeader(true)
                )
            {
                using (var w = new ChoJSONWriter(json))
                    w.Write(p);
            }

            var actual = json.ToString();
            Assert.AreEqual(expected, actual);
        }
    
    }
}
