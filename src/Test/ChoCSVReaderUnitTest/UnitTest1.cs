using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using ChoETL;
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
        //[Test]
        public void POCOTest1()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("us-en");

            string csv = @"Id, Name
1, Raj
, ";

            foreach (var rec in ChoCSVReader<EmployeeRec>.LoadText(csv))
            {
                Trace.WriteLine(rec.ToString());
            }
            Assert.IsTrue(true);
        }

        //[Test]
        public void AsDataReaderTest()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("us-en");
            string csv = @"Id, Name 
2, Tom
                3, ";

            var dr = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .WithField("Id")
                .WithField("Name", valueConverter: (o) => o == null ? String.Empty : o)
                .Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any)
                .AsDataReader();
            while (dr.Read())
            {
                var x = dr[1];
            }
            Assert.IsTrue(true);
        }

    }
}
