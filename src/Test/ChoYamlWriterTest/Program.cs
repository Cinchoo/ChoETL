using ChoETL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ChoYamlWriterTest
{
    [ChoYamlRecordObject(ObjectValidationMode = ChoObjectValidationMode.MemberLevel, ErrorMode = ChoErrorMode.ReportAndContinue)]
    public class Emp
    {
        [DisplayName("Id")]
        public int ID { get; set; }
        public string Name { get; set; }

        public Address Address { get; set; }
    }

    public class Address
    {
        [DisplayName("street")]
        [StringLength(maximumLength: 5)]
        [ChoIgnoreMember]
        public string Street { get; set; }
        public string City { get; set; }
    }

    class Program
    {
        static void HelloWorld()
        {
            StringBuilder yaml = new StringBuilder();
            using (var w = new ChoYamlWriter(yaml)
                .WithField("Id", valueConverter: o => (int)o + 1)
                .WithField("Name")
                )
            {
                w.Write(new
                {
                    Id = 1,
                    Name = "Tom"
                });
            }

            Console.WriteLine(yaml.ToString());
        }

        static void SerializeDictionary()
        {
            StringBuilder yaml = new StringBuilder();
            using (var w = new ChoYamlWriter(yaml)
                )
            {
                w.Write(new Dictionary<string, object>()
                {
                    ["1"] = 1,
                    ["2"] = 2
                });
            }

            Console.WriteLine(yaml.ToString());
        }

        static void SerializeDictionaryArray()
        {
            StringBuilder yaml = new StringBuilder();
            List<Dictionary<string, object>> arr = new List<Dictionary<string, object>>();
            arr.Add(new Dictionary<string, object>()
            {
                ["1"] = 1,
                ["2"] = 2
            });
            arr.Add(new Dictionary<string, object>()
            {
                ["3"] = 1,
                ["4"] = 2
            });
            using (var w = new ChoYamlWriter(yaml)
                )
            {
                w.Write(arr);
            }

            Console.WriteLine(yaml.ToString());
        }

        static void SerializeList()
        {
            StringBuilder yaml = new StringBuilder();
            var x = new List<string>()
                {
                    "Tom",
                    "Mark"
                };
            using (var w = new ChoYamlWriter(yaml)
                )
            {
                w.Write(x);
            }

            Console.WriteLine(yaml.ToString());
        }

        static void SerializeListAsWhold()
        {
            StringBuilder yaml = new StringBuilder();
            object x = new List<string>()
                {
                    "Tom",
                    "Mark"
                };
            using (var w = new ChoYamlWriter(yaml)
                )
            {
                w.Write(x);
            }

            Console.WriteLine(yaml.ToString());
        }

        static void SerializeArrayList()
        {
            StringBuilder yaml = new StringBuilder();
            ArrayList arr = new ArrayList ();
            arr.Add(new Dictionary<string, object>()
            {
                ["1"] = 1,
                ["2"] = 2
            });
            arr.Add(new Dictionary<string, object>()
            {
                ["3"] = 1,
                ["4"] = 2
            });
            using (var w = new ChoYamlWriter(yaml)
                )
            {
                w.Write(arr);
            }

            Console.WriteLine(yaml.ToString());
        }

        static void SerializeValueTypes()
        {
            StringBuilder yaml = new StringBuilder();
            var x = new List<int>()
                {
                    1,
                    2
                };
            using (var w = new ChoYamlWriter(yaml)
                )
            {
                w.Write(x);
            }

            Console.WriteLine(yaml.ToString());
        }

        static void SerializeNullableTypes()
        {
            StringBuilder yaml = new StringBuilder();
            object x = new List<int?>()
                {
                    1,
                    2
                };
            using (var w = new ChoYamlWriter(yaml)
                )
            {
                w.Write(x);
            }

            Console.WriteLine(yaml.ToString());
        }

        static void POCOTest()
        {
            StringBuilder yaml = new StringBuilder();
            Emp e1 = new Emp
            {
                ID = 1,
                Name = "Tom",

                Address = new Address
                {
                    Street = "1 Main Street",
                    City = "NYC"
                }
            };

            using (var w = new ChoYamlWriter<Emp>(yaml)
                )
            {
                w.Write(e1);
            }

            Console.WriteLine(yaml.ToString());

        }

        static void WriteDataTableTest()
        {
            string csv = @"Id, Name
1, Tom
2, Mark";

            StringBuilder yaml = new StringBuilder();
            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                using (var w = new ChoYamlWriter(yaml)
                    //.ReuseSerializerObject(false)
                    )
                {
                    w.Write(r.AsDataTable());
                }
            }

            Console.WriteLine(yaml.ToString());
        }

        static void SerializeValueTypesOneAtATime()
        {
            StringBuilder yaml = new StringBuilder();
            using (var w = new ChoYamlWriter<int>(yaml)
                )
            {
                w.Write(1);
                w.Write(2);
            }

            Console.WriteLine(yaml.ToString());
        }

        public class MethodCall
        {
            public string MethodName { get; set; }
            public List<object> Arguments { get; set; }
        }

        static void CustomSerialization()
        {
            StringBuilder yaml = new StringBuilder();
            using (var w = new ChoYamlWriter(yaml)
                )
            {
                var rec = new MethodCall
                {
                    MethodName = "someName",
                    Arguments = new List<object>
                    {
                        "arg1",
                        "arg2"
                    }
                }.ToDictionaryFromObject(o => o.MethodName, o => o.Arguments);

                w.Write(rec);
            }

            Console.WriteLine(yaml.ToString());
        }


        public class MethodCall1
        {
            public string MethodName { get; set; }
            public List<object> Arguments { get; set; }
            [ChoFallbackValue("x")]
            [ChoDefaultValue("x")]
            public object barray { get; set; }
        }

        static void IgnoreFailedMembers()
        {
            StringBuilder yaml = new StringBuilder();
            using (var w = new ChoYamlWriter<MethodCall1>(yaml)
                )
            {
                var rec = new MethodCall1
                {
                    MethodName = "someName",
                    Arguments = new List<object>
                    {
                        "arg1",
                        "arg2"
                    },
                    barray = new EntryPointNotFoundException()
                };

                w.Write(rec);
            }

            Console.WriteLine(yaml.ToString());
        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Verbose;
            IgnoreFailedMembers();
        }
    }
}
