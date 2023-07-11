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
        //Serialize ExpandoObjects
        [Test]
        public static void QuickDynamicTest_1()
        {
            string expected = @"<Employees>
  <Employee>
    <Id>1</Id>
    <Name>Mark</Name>
  </Employee>
  <Employee>
    <Id>2</Id>
    <Name>Jason</Name>
  </Employee>
</Employees>";

            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Jason";

            StringBuilder actual = new StringBuilder();
            using (var parser = new ChoXmlWriter(actual).WithXPath("Employees/Employee")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue))
            {
                parser.Configuration.DoNotEmitXmlNamespace = true;
                parser.Write(new[] { rec1, rec2 });
            }

            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual.ToString());
        }

        //Serialize ExpandoObjects using XmlSerialization
        [Test]
        public static void QuickDynamicTest_2()
        {
            string expected = @"<Employees>
  <Employee>
    <Id>1</Id>
    <Name>Mark</Name>
  </Employee>
  <Employee>
    <Id>2</Id>
    <Name>Jason</Name>
  </Employee>
</Employees>";

            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Jason";

            StringBuilder actual = new StringBuilder();
            using (var parser = new ChoXmlWriter(actual).WithXPath("Employees/Employee")
                .UseXmlSerialization()
                .ErrorMode(ChoErrorMode.IgnoreAndContinue))
            {
                parser.Configuration.OmitXmlDeclaration = true;
                parser.Configuration.DoNotEmitXmlNamespace = true;
                parser.Write(new[] { rec1, rec2 });
            }

            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual.ToString());
        }

        //Serialize ChoDynamicObject
        [Test]
        public static void QuickDynamicTest_3()
        {
            string expected = @"<Employees>
  <Employee>
    <Id>1</Id>
    <Name>Mark</Name>
  </Employee>
  <Employee>
    <Id>2</Id>
    <Name>Jason</Name>
  </Employee>
</Employees>";

            dynamic rec1 = new ChoDynamicObject();
            rec1.Id = 1;
            rec1.Name = "Mark";

            dynamic rec2 = new ChoDynamicObject();
            rec2.Id = 2;
            rec2.Name = "Jason";

            StringBuilder actual = new StringBuilder();
            using (var parser = new ChoXmlWriter(actual).WithXPath("Employees/Employee")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue))
            {
                parser.Configuration.OmitXmlDeclaration = true;
                parser.Configuration.DoNotEmitXmlNamespace = true;
                parser.Write(new[] { rec1, rec2 });
            }

            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual.ToString());
        }

        //Serialize ChoDynamicObject using XmlSerialization
        [Test]
        public static void QuickDynamicTest_4()
        {
            string expected = @"<Employees>
  <Employee>
    <Id>1</Id>
    <Name>Mark</Name>
  </Employee>
  <Employee>
    <Id>2</Id>
    <Name>Jason</Name>
  </Employee>
</Employees>";

            dynamic rec1 = new ChoDynamicObject();
            rec1.Id = 1;
            rec1.Name = "Mark";

            dynamic rec2 = new ChoDynamicObject();
            rec2.Id = 2;
            rec2.Name = "Jason";

            StringBuilder actual = new StringBuilder();
            using (var parser = new ChoXmlWriter(actual).WithXPath("Employees/Employee")
                .UseXmlSerialization()
                .ErrorMode(ChoErrorMode.IgnoreAndContinue))
            {
                parser.Configuration.OmitXmlDeclaration = true;
                parser.Configuration.DoNotEmitXmlNamespace = true;
                parser.Write(new[] { rec1, rec2 });
            }

            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual.ToString());
        }

        [Test]
        public static void QuickDynamicTest_5()
        {
            string expected = @"<Employees xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <Employee>
    <Id>1</Id>
    <Name>Mark</Name>
    <IsActive>true</IsActive>
    <Message><![CDATA[Test]]></Message>
    <ArrayOfAnyType>
      <anyType xsi:type=""xsd:int"">1</anyType>
      <anyType xsi:type=""xsd:string"">abc</anyType>
    </ArrayOfAnyType>
    <Lint>1</Lint>
    <Lint>2</Lint>
    <HT>
      <a1>abc</a1>
      <a2>abc</a2>
    </HT>
    <Dict>
      <b1>abc</b1>
      <b2>abc</b2>
    </Dict>
  </Employee>
  <Employee>
    <Id>2</Id>
    <Name>Jason</Name>
    <IsActive>true</IsActive>
    <Message><![CDATA[Test]]></Message>
  </Employee>
</Employees>";

            string actual = null;

            ArrayList al = new ArrayList();
            al.Add(1);
            al.Add("abc");

            List<int> lint = new List<int>() { 1, 2 };

            Hashtable ht = new Hashtable();
            ht.Add("a1", "abc");
            ht.Add("a2", "abc");

            ChoSerializableDictionary<string, string> dict = new ChoSerializableDictionary<string, string>();
            dict.Add("b1", "abc");
            dict.Add("b2", "abc");

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";
            rec1.IsActive = true;
            rec1.Message = new ChoCDATA("Test");
            rec1.Array = al;
            rec1.Lint = lint;
            rec1.HT = ht;
            rec1.Dict = dict;
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Jason";
            rec2.IsActive = true;
            rec2.Message = new ChoCDATA("Test");
            objs.Add(rec2);

            StringBuilder sb = new StringBuilder();
            using (var parser = new ChoXmlWriter(sb).WithXPath("Employees/Employee")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Null)
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure(c => c.UseXmlArray = false)
                .Configure(c => c.TurnOffPluralization = true)
                .Configure(c => c.OmitXsiNamespace= false)
                .WithXmlNamespace("xsi", ChoXmlSettings.XmlSchemaInstanceNamespace)
                //.UseXmlSerialization()
                )
            {
                parser.Configuration.DoNotEmitXmlNamespace = true;
                parser.Write(objs);
            }
            actual = sb.ToString();

            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void QuickDynamicTest_6()
        {
            string expected = @"<Employees>
  <Employee>
    <Id>1</Id>
    <Name>Mark</Name>
    <IsActive>True</IsActive>
    <Message><![CDATA[Test]]></Message>
    <Arrays>
      <Arrays>1</Arrays>
      <Arrays>abc</Arrays>
    </Arrays>
    <Lint>1</Lint>
    <Lint>2</Lint>
    <HT>
      <a1>abc</a1>
      <a2>abc</a2>
    </HT>
    <Dict>
      <b1>abc</b1>
      <b2>abc</b2>
    </Dict>
  </Employee>
  <Employee>
    <Id>2</Id>
    <Name>Jason</Name>
    <IsActive>True</IsActive>
    <Message><![CDATA[Test]]></Message>
  </Employee>
</Employees>";

            string actual = null;

            ArrayList al = new ArrayList();
            al.Add(1);
            al.Add("abc");

            List<int> lint = new List<int>() { 1, 2 };

            Hashtable ht = new Hashtable();
            ht.Add("a1", "abc");
            ht.Add("a2", "abc");

            ChoSerializableDictionary<string, string> dict = new ChoSerializableDictionary<string, string>();
            dict.Add("b1", "abc");
            dict.Add("b2", "abc");

            List<ExpandoObject> objs = new List<ExpandoObject>();
            dynamic rec1 = new ExpandoObject();
            rec1.Id = 1;
            rec1.Name = "Mark";
            rec1.IsActive = true;
            rec1.Message = new ChoCDATA("Test");
            rec1.Array = al;
            rec1.Lint = lint;
            rec1.HT = ht;
            rec1.Dict = dict;
            objs.Add(rec1);

            dynamic rec2 = new ExpandoObject();
            rec2.Id = 2;
            rec2.Name = "Jason";
            rec2.IsActive = true;
            rec2.Message = new ChoCDATA("Test");
            objs.Add(rec2);

            StringBuilder sb = new StringBuilder();
            using (var parser = new ChoXmlWriter(sb).WithXPath("Employees/Employee")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Configure(c => c.IgnoreFieldValueMode = ChoIgnoreFieldValueMode.Null)
                .Configure(c => c.ThrowAndStopOnMissingField = false)
                .Configure(c => c.UseXmlArray = false)
                .Configure(c => c.TurnOffPluralization = false)
                .UseXmlSerialization()
                )
            {
                parser.Configuration.DoNotEmitXmlNamespace = true;
                parser.Write(objs);
            }
            actual = sb.ToString();

            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual);
        }
    }
}
