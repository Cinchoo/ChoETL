using ChoETL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoCSVWriterTest.Test
{
    public class CSVFileDiffTest
    {
        [Test]
        public static void CSVDiffWithStatus()
        {
            string csv1 = @"ID,name
1,Danny
2,Fred
3,Sam";

            string csv2 = @"ID,name
1,Danny
3,Pamela
4,Fernando";

            //ChoTypeComparerCache.Instance.ScanAndLoad();

            var r1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader().WithMaxScanRows(1).OfType<ChoDynamicObject>();
            var r2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader().WithMaxScanRows(1).OfType<ChoDynamicObject>();

            string expectedOutput = @"ID,name,Status
1,Danny,Unchanged
2,Fred,Deleted
3,Pamela,Changed
4,Fernando,New";

            StringBuilder output = new StringBuilder();
            using (var w = new ChoCSVWriter(output).WithFirstLineHeader())
            {
                foreach (var t in r1.Compare(r2, "ID", "name"))
                {
                    dynamic v1 = t.MasterRecord as dynamic;
                    dynamic v2 = t.DetailRecord as dynamic;
                    if (t.Status == CompareStatus.Unchanged || t.Status == CompareStatus.Deleted)
                    {
                        v1.Status = t.Status.ToString();
                        w.Write(v1);
                    }
                    else
                    {
                        v2.Status = t.Status.ToString();
                        w.Write(v2);
                    }
                }
            }

            output.Print();
            Assert.AreEqual(expectedOutput, output.ToString());
        }

        [Test]
        public static void UnsortedCSVDiffWithStatus()
        {
            string csv1 = @"ID,name		
1,Danny-Unchanged
2,Fred-Deleted
6,Marc-Unchanged
3,Sam-Unchanged
7,Rob-Unchanged";

            string csv2 = @"ID,name
1,Danny-Unchanged
3,Sam-Unchanged
4,Fernando-New
6,Marc-Unchanged
8,Lars-New
7,Rob-Unchanged";

            string expectedOutput = @"ID,name,Status
1,Danny-Unchanged,Unchanged
2,Fred-Deleted,Deleted
3,Sam-Unchanged,Unchanged
4,Fernando-New,New
6,Marc-Unchanged,Unchanged
7,Rob-Unchanged,Unchanged
8,Lars-New,New";

            var r1 = ChoCSVReader.LoadText(csv1).WithFirstLineHeader().WithMaxScanRows(1).OrderBy(r => r.Id).OfType<ChoDynamicObject>();
            var r2 = ChoCSVReader.LoadText(csv2).WithFirstLineHeader().WithMaxScanRows(1).OrderBy(r => r.Id).OfType<ChoDynamicObject>();

            StringBuilder output = new StringBuilder();
            using (var w = new ChoCSVWriter(output).WithFirstLineHeader())
            {
                foreach (var t in r1.Compare(r2, "ID", "name"))
                {
                    dynamic v1 = t.MasterRecord as dynamic;
                    dynamic v2 = t.DetailRecord as dynamic;
                    if (t.Status == CompareStatus.Unchanged || t.Status == CompareStatus.Deleted)
                    {
                        v1.Status = t.Status.ToString();
                        w.Write(v1);
                    }
                    else
                    {
                        v2.Status = t.Status.ToString();
                        w.Write(v2);
                    }
                }
            }

            output.Print();
            Assert.AreEqual(expectedOutput, output.ToString());
        }
    }
}
