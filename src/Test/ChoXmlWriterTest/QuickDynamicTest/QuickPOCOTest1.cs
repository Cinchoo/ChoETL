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
    public class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public partial class Program
    {
        //Serialize POCO objects
        [Test]
        public static void QuickPOCOTest_1()
        {
            string expected = @"<Employees>
  <Employee>
    <Id>1</Id>
    <Name>Tom</Name>
  </Employee>
  <Employee>
    <Id>2</Id>
    <Name>Mark</Name>
  </Employee>
</Employees>";

            StringBuilder actual = new StringBuilder();

            List<Employee> objs = new List<Employee>();
            objs.Add(new Employee() { Id = 1, Name = "Tom" });
            objs.Add(new Employee() { Id = 2, Name = "Mark" });

            using (var parser = new ChoXmlWriter<Employee>(actual).WithXPath("Employees/Employee"))
            {
                parser.Configuration.DoNotEmitXmlNamespace = true;
                parser.Write(objs);
            }

            Console.WriteLine(actual);
            Assert.AreEqual(expected, actual.ToString());
        }
    }
}
