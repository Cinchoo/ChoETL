using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL.Benchmark
{
    public static class TestClassGenerator
    {
        public static TestClass GetTestClassType1()
        {
            return new TestClass
            {
                Int = 1,
                NullableInt = null,
                String = Guid.NewGuid().ToString(),
                Guid = Guid.NewGuid(),
                NullableGuid = null,
                DateTime = new DateTime(2020, 1, 1),
                NullableDateTime = null,
                Binary = Guid.NewGuid().ToByteArray(),
                Boolean = true,
                Int16 = 2,
                Int64 = 3,
                Decimal = new decimal(1.1),
                Single = 4,
                Double = 5
            };
        }

        public static TestClass GetTestClassType2()
        {
            return new TestClass
            {
                Int = 11,
                NullableInt = 5,
                String = Guid.NewGuid().ToString(),
                Guid = Guid.NewGuid(),
                NullableGuid = null,
                DateTime = new DateTime(2021, 2, 2),
                NullableDateTime = null,
                Binary = Guid.NewGuid().ToByteArray(),
                Boolean = true,
                Int16 = 21,
                Int64 = 31,
                Decimal = new decimal(11.11),
                Single = 41,
                Double = 51
            };
        }

        public static IEnumerable<TestClass> GetTestEnumerable()
        {
            return new[]
            {
                GetTestClassType1(),
                GetTestClassType2()
            };
        }

        public static IEnumerable<TestClass> GetTestEnumerable(int num)
        {
            var list = new List<TestClass>();
            for (var i = 0; i < num; i++)
            {
                if (i % 2 == 0)
                {
                    list.Add(GetTestClassType1());
                }
                else
                {
                    list.Add(GetTestClassType2());
                }
            }

            return list;
        }

        public static IEnumerable<TestClass> GetTestEnumerable1(int num)
        {
            var list = new List<TestClass>();
            for (var i = 0; i < num; i++)
            {
                list.Add(GetTestClassType1());
            }

            return list;
        }

        public static IEnumerable<TestClass> GetTestEnumerable2(int num)
        {
            var list = new List<TestClass>();
            for (var i = 0; i < num; i++)
            {
                list.Add(GetTestClassType2());
            }

            return list;
        }
    }
}
