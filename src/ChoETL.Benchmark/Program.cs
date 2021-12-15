using System;
using System.Diagnostics;
using ChoETL;


namespace ChoETL.Benchmark
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = TraceLevel.Off;
            ToDataTableTest1();
        }

        static void ToDataTableTest1()
        {
            var data = TestClassGenerator.GetTestEnumerable(100000);

            for (int i = 0; i < 10; i++)
            {
                Stopwatch w = Stopwatch.StartNew();
                var dt = data.AsDataTable();
                //dt.Print();
                //break;
                w.Stop();
                w.ElapsedMilliseconds.ToString().Print();
            }
        }
    }
}
