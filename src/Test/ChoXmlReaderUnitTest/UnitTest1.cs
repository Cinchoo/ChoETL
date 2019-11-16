using System;
using NUnit.Framework;

namespace ChoXmlReaderUnitTest
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        public void ComplexTest1()
        {
            string xml = @"<SalesLead>
    <Customer>
         <Name part=""first"">Foo</Name>
         <Name part=""last"">Bar</Name>
    </Customer>
</SalesLead>";
        }
    }
}
