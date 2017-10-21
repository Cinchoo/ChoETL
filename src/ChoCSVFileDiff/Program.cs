using ChoETL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoCSVFileDiff
{
    class Program
    {
        static void Main(string[] args)
        {
            var input1 = new ChoCSVReader("planets1.csv").WithFirstLineHeader();
            var input2 = new ChoCSVReader("planets2.csv").WithFirstLineHeader();

            using (var output = new ChoCSVWriter("planetDiff.csv").WithFirstLineHeader())
            {
                output.Write(input1.OfType<ChoDynamicObject>().Except(input2.OfType<ChoDynamicObject>(), new ChoDynamicObjectEqualityComparer(new string[] { "rowid" })));
                output.Write(input2.OfType<ChoDynamicObject>().Except(input1.OfType<ChoDynamicObject>(), new ChoDynamicObjectEqualityComparer(new string[] { "rowid" })));
            }

            //foreach (dynamic x in input1.OfType<ChoDynamicObject>().Except(input2.OfType<ChoDynamicObject>(), new ChoDynamicObjectEqualityComparer(new string[] { "rowid" })))
            //{
            //    Console.WriteLine(x.rowid);
            //}
        }
    }

}
