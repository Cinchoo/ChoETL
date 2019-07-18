using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using ChoETL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChoCSVReaderUnitTest
{
    [TestClass]
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
        [TestMethod]
        public void POCOTest1()
        {
            string csv = @"Id, Name
1, Raj
, ";

            foreach (var rec in ChoCSVReader<EmployeeRec>.LoadText(csv))
            {
                Trace.WriteLine(rec.ToString());
            }
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void AsDataReaderTest()
        {
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
