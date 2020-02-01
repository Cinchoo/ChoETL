using ChoETL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoCSVFileDiff
{
    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {
        static void Main(string[] args)
        {
            TestPlanetDiff();
        }
        static public string FileNamePlanets1CSV => "Planets1.csv";
        static public string FileNamePlanets2CSV => "Planets2.csv";
        static public string FileNamePlanetDiffCSV => "PlanetDiff.csv";
        static public string FileNamePlanetDiffExpected2CSV => "PlanetDiffExpected.csv";

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }

        [Test]
        public static void TestPlanetDiff()
        { 
            var input1 = new ChoCSVReader(FileNamePlanets1CSV).WithFirstLineHeader();
            var input2 = new ChoCSVReader(FileNamePlanets2CSV).WithFirstLineHeader();

            using (var output = new ChoCSVWriter(FileNamePlanetDiffCSV).WithFirstLineHeader())
            {
                output.Write(input1.OfType<ChoDynamicObject>().Except(input2.OfType<ChoDynamicObject>(), new ChoDynamicObjectEqualityComparer(new string[] { "rowid" })));
                output.Write(input2.OfType<ChoDynamicObject>().Except(input1.OfType<ChoDynamicObject>(), new ChoDynamicObjectEqualityComparer(new string[] { "rowid" })));
            }

            FileAssert.AreEqual(FileNamePlanetDiffExpected2CSV, FileNamePlanetDiffCSV);
            
            //respon
            //foreach (dynamic x in input1.OfType<ChoDynamicObject>().Except(input2.OfType<ChoDynamicObject>(), new ChoDynamicObjectEqualityComparer(new string[] { "rowid" })))
            //{
            //    Console.WriteLine(x.rowid);
            //}
        }
    }

}
