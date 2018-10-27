using System;
using ChoETL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChoCSVReaderUnitTest
{
    [TestClass]
    public class UnitTest1
    {
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
