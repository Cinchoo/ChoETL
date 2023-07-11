using ChoETL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChoJSONReaderTest.Program;

namespace ChoJSONReaderTest.Tests
{
    public class MixedTests
    {
        [Test]
        public static void DecimalRoundingIssue_Test()
        {
            string json = @"[
{
    ""amount"": 26.61657491,
    ""value"": 26.61657491
}
]";

            var expected = 26.61657491;
            double actual = 0;
            using (var r = ChoJSONReader.LoadText(json)
                )
            {
                actual = r.First().amount;
            }

            Assert.AreEqual(expected, actual);

        }
    }
}
