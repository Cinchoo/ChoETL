using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChoXmlReaderUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
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
