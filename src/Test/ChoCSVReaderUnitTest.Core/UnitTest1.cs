using Microsoft.VisualStudio.TestTools.UnitTesting;
using ChoETL;
using System;
using System.Diagnostics;

namespace ChoCSVReaderUnitTest.Core
{
    [TestClass]
    public class CSVReaderCore
    {
        [TestMethod]
        public void CSVTest1()
        {
            string csv = @"Id, Name 
2, Tom
3, Mark";

            foreach (var rec in ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader())
            {
                Trace.WriteLine((string)rec.Dump());
            }
            //var dr = ChoCSVReader.LoadText(csv)
            //    .WithFirstLineHeader()
            //    .WithField("Id")
            //    .WithField("Name", valueConverter: (o) => o == null ? String.Empty : o)
            //    .Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Any)
            //    .AsDataReader();
            //while (dr.Read())
            //{
            //    var x = dr[1];
            //}
            Assert.IsTrue(true);
        }
    }
}
