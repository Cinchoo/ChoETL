using ChoETL;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoXmlWriterTest
{
    public partial class Program
    {
        //Serialize xml with no format
        [Test]
        public static void CSVW2XmlNoFormattingTest()
        {
            string csv = @"Id, First Name
1, Tom
2, Mark";

            string expectedXml = @"<Emps xmlns:xml=""http://www.w3.org/XML/1998/namespace""><Emp><Id>1</Id><FirstName>Tom</FirstName></Emp></Emps>";

            StringBuilder actual = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader())
            {
                using (var w = new ChoXmlWriter(actual)
                    .WithXPath("Emps/Emp")
                    .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                    //.IgnoreRootName()
                    //.IgnoreNodeName()
                    .Configure(c => c.Formatting = System.Xml.Formatting.None)
                    )
                {
                    w.Write(r.First());
                }
            }

            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expectedXml, actual.ToString());
        }

        //Serialize POCO with no format
        [Test]
        public static void POCOCSVW2XmlNoFormattingTest()
        {
            string csv = @"Id, Name
1, Tom
2, Mark";

            string expectedXml = @"<Emps xmlns:xml=""http://www.w3.org/XML/1998/namespace""><Emp><Id>1</Id><Name>Tom</Name></Emp></Emps>";
          
            StringBuilder actual = new StringBuilder();
            using (var r = ChoCSVReader<Emp>.LoadText(csv)
                .WithFirstLineHeader())
            {
                using (var w = new ChoXmlWriter<Emp>(actual)
                    .ErrorMode(ChoErrorMode.ThrowAndStop)
                    //.IgnoreRootName()
                    //.IgnoreNodeName()
                    .Configure(c => c.Formatting = System.Xml.Formatting.None)
                    )
                {
                    w.Write(r.First());
                }
            }

            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expectedXml, actual.ToString());
        }

        //Multilevel xpath write test
        [Test]
        public static void MultilevelXPathTest()
        {
            string expectedXml = @"<ticket>
  <Employees>
    <Employee>
      <Id>1</Id>
      <Name>Mark</Name>
    </Employee>
    <Employee>
      <Id>1</Id>
      <Name>Mark</Name>
    </Employee>
  </Employees>
</ticket>";
            StringBuilder actual = new StringBuilder();

            using (var parser = new ChoXmlWriter(actual).WithXPath("//ticket/Employees/Employee"))
            {
                parser.Configuration.DoNotEmitXmlNamespace = true;
                parser.Write(new object[] { new { Id = 1, Name = "Mark" }, new { Id = 1, Name = "Mark" } });
            }

            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expectedXml, actual.ToString());
        }

        [Test]
        public static void ListTest()
        {
            string expectedXml = @"<t1 xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <t2>
    <r4>
      <t5>
        <Employees>
          <Employee>Tom</Employee>
          <Employee>Mark</Employee>
        </Employees>
      </t5>
    </r4>
  </t2>
</t1>";
            StringBuilder actual = new StringBuilder();

            using (var w = new ChoXmlWriter<string>(actual).UseXmlSerialization().WithXPath("t1/t2/r4/t5/Employees/Employee"))
            {
                w.Write(new List<string> { "Tom", "Mark" });
            }

            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expectedXml, actual.ToString());
        }

        [Test]
        public static void DictTest()
        {
            string expectedXml = @"<Employees xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <Employee>
    <Key>1</Key>
    <Value>Tom</Value>
  </Employee>
  <Employee>
    <Key>2</Key>
    <Value>Mark</Value>
  </Employee>
</Employees>";
            StringBuilder actual = new StringBuilder();

            using (var w = new ChoXmlWriter(actual).UseXmlSerialization().WithXPath("Employees/Employee"))
            {
                w.Write(new Dictionary<int, string> { { 1, "Tom" }, { 2, "Mark" } });
            }

            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expectedXml, actual.ToString());
        }

        [Test]
        public static void RootAttributeWithNSTest()
        {
            string expectedXml = @"<FooBars xmlns=""urn:foobar1"">
  <FooBar>
    <Foo>Tom</Foo>
  </FooBar>
</FooBars>";
            StringBuilder actual = new StringBuilder();

            using (var w = new ChoXmlWriter<FooBar>(actual)
                .WithXmlNamespace("", "urn:foobar1")
                .Configure(c => c.DoNotEmitXmlNamespace = true)
                )
            {
                w.Write(new FooBar
                {
                    Foo = "Tom"
                });
            }

            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expectedXml, actual.ToString());
        }

        [Test]
        public static void CustomNodeNameTest()
        {
            string csv = @"Id, First Name
1, Tom
2, Mark";

            string expectedXml = @"<Root xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <Node1>
    <Id>1</Id>
    <FirstName>Tom</FirstName>
  </Node1>
  <Node2>
    <Id>2</Id>
    <FirstName>Mark</FirstName>
  </Node2>
</Root>";

            StringBuilder actual = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader())
            {
                using (var w = new ChoXmlWriter(actual)
                    .ErrorMode(ChoErrorMode.ThrowAndStop)
                    .Setup(s => s.CustomeNodeNameOverride += (o, e) =>
                    {
                        e.NodeName = $"Node{e.Index}";
                    })
                    )
                {
                    w.Write(r);
                }
            }

            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expectedXml, actual.ToString());

            //            string xml1 = @"<Root>
            //  <Node1>
            //    <Id>1</Id>
            //    <FirstName>Tom</FirstName>
            //  </Node1>
            //  <Node2>
            //    <Id>2</Id>
            //    <FirstName>Mark</FirstName>
            //  </Node2>
            //</Root>";

            //            using (var r = ChoXmlReader.LoadText(xml1))
            //            {
            //                foreach (var rec in r)
            //                    Console.WriteLine(rec.Dump());

            //            }

            //            return;


            //using (var reader = new ChoCSVReader("C:\\Server Media\\test3.csv")
            //    .WithFirstLineHeader()
            //    .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true)
            //    )
            //{
            //    using (var writer = new ChoXmlWriter(sb)
            //        .Configure(c => c.RootName = "Records")
            //        .Configure(c => c.NodeName = "Record")
            //        .Configure(c => c.EmptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Empty)
            //        .Configure(c => c.ErrorMode = ChoErrorMode.ThrowAndStop)
            //        )
            //    {
            //        writer.Write(reader.Select(r =>
            //        {
            //            r.RenameKey("Company Name", "CompanyName");
            //            return r;
            //        }));
            //    }
            //}
        }
        [Test]
        public static void NodeRenameTest()
        {
            string expectedXml = @"<Records xmlns:xml=""http://www.w3.org/XML/1998/namespace"">
  <Record>
    <Id>1</Id>
    <FirstName>Tom</FirstName>
  </Record>
  <Record>
    <Id>2</Id>
    <FirstName>Mark</FirstName>
  </Record>
</Records>";
            StringBuilder actual = new StringBuilder();

            string csv = @"Id, First Name
1, Tom
2, Mark";
            using (var reader = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                .Configure(c => c.FileHeaderConfiguration.IgnoreColumnsWithEmptyHeader = true)
                )
            {
                using (var writer = new ChoXmlWriter(actual)
                    .Configure(c => c.RootName = "Records")
                    .Configure(c => c.NodeName = "Record")
                    .Configure(c => c.EmptyXmlNodeValueHandling = ChoEmptyXmlNodeValueHandling.Empty)
                    .Configure(c => c.ErrorMode = ChoErrorMode.ThrowAndStop)
                    )
                {
                    writer.Write(reader.Select(r =>
                    {
                        r.RenameKey("First Name", "FirstName");
                        return r;
                    }));
                }
            }
            Console.WriteLine(actual.ToString());
            Assert.AreEqual(expectedXml, actual.ToString());
        }
    }
}
