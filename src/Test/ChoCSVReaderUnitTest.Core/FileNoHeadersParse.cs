using ChoETL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChoCSVReaderUnitTest.Core
{
    [TestFixture]
    public class FileNoHeadersParse
    {
        [Test]
        public void DuplicateCharBeforeSeparatorTest()
        {
            string csv = @"2,Tom,32,ThisShouldBeDescriptionValue";

            var rec = ChoCSVReader.LoadText(csv)
                .Configure(c => c.FileHeaderConfiguration.HasHeaderRecord = false)
                .WithField("Id")
                .WithField("Name")
                .WithField("Age")
                .WithField("Description")
                .First();

            Assert.IsNotNull(rec);
            Assert.AreEqual("2", rec.Id);
            Assert.AreEqual("Tom", rec.Name);
            Assert.AreEqual("32", rec.Age);
            Assert.AreEqual("ThisShouldBeDescriptionValue", rec.Description);
        }

        [Test]
        public void RightOfExample()
        {
            string csv = @"2,Tom,32,RightOfShouldReturnFromTomNotThis";

            var result = ChoUtility.RightOf(csv, "2,");
            Assert.AreEqual("Tom,32,RightOfShouldReturnFromTomNotThis", result);
        }
    }
}
