using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics;
using System.Linq;
using ChoETL;
using Newtonsoft.Json;
using NUnit.Framework;

namespace ChoCSVReaderUnitTest
{
    [TestFixture]
    public class UnitTest1
    {
        [ChoCSVFileHeader]
        [ChoCSVRecordObject(ObjectValidationMode = ChoObjectValidationMode.MemberLevel)]
        public class EmployeeRec
        {
            [DefaultValue("XXXX")]
            public string Name
            {
                get;
                set;
            }
            [Required]
            public int? Id
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
        public void POCOTest1()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("us-en");

            string csv = @"Id, Name
1, Mark
2, Tom";

            string expected = @"[
  {
    ""Name"": ""Mark"",
    ""Id"": 1
  },
  {
    ""Name"": ""Tom"",
    ""Id"": 2
  }
]";
            using (var r = ChoCSVReader<EmployeeRec>.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                var recs = r.ToArray();

                var actual = JsonConvert.SerializeObject(recs, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void AsDataReaderTest()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("us-en");
            string csv = @"Id, Name 
2, Tom
3, ";
            string expected = @"[
  {
    ""Id"": ""2"",
    ""Name"": ""Tom""
  },
  {
    ""Id"": ""3"",
    ""Name"": null
  }
]";
            var dr = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithField("Id")
                .WithField("Name", valueConverter: (o) => o == null ? String.Empty : o)
                .Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any)
                .AsDataReader();

            var dt = new DataTable();
            dt.Load(dr);
            var actual = JsonConvert.SerializeObject(dt, Formatting.Indented);
            Assert.AreEqual(expected, actual);
        }

    }
}
