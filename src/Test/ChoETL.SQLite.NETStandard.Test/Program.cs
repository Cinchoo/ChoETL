using System;
using System.Diagnostics;
using System.Linq;

namespace ChoETL.SQLite.NETStandard.Test
{
    class Program
    {
        public class Emp
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string City { get; set; }
        }

        static void Main(string[] args)
        {
            StageJSONFile();
        }
        static void StageJSONFile()
        {
            string json = @"
    [
        {
            ""Id"": 1,
            ""Name"": ""Polo"",
            ""City"": ""New York""
        },
        {
            ""Id"": 2,
            ""Name"": ""328"",
            ""City"": ""Edison""
        }
    ]";
            ChoETLFrxBootstrap.TraceLevel = TraceLevel.Error;
            using (var r = ChoJSONReader<Emp>.LoadText(json)
                )
            {
                r.StageOnSQLite().Where(e => e.Id == 2).Print();
            }

        }

        static void StageCSVFile()
        {
            string csv = @"Id, Name, City
1, Tom, NY
2, Mark, NJ
3, Lou, FL
4, Smith, PA
5, Raj, DC
";

            using (var r = ChoCSVReader<Emp>.LoadText(csv)
                .WithFirstLineHeader())
            {
                r.StageOnSQLite();
            }

        }
    }
}
