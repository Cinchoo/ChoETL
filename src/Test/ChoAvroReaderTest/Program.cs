using ChoETL;
using Microsoft.Azure.Management.DataFactory.Models;
using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
using Microsoft.Hadoop.Avro.Schema;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace ChoAvroReaderTest
{
    class Program
    {
        public class Emp
        {
            public int? Id { get; set; }
            public string Name { get; set; }
            public string City { get; set; }
        }

        public class SensorData
        {
            public Location Position { get; set; }
            public byte[] Value { get; set; }
        }

        public struct Location
        {
            public int Floor { get; set; }
            public int Room { get; set; }
        }

        static void Issue241()
        {
            string csv = @"Id, Name
1, Tom
2, Mark";

            using (var r = ChoCSVReader.LoadText(csv)
                .WithFirstLineHeader()
                )
            {
                var dr = r.AsDataReader();
                using (var w = new ChoAvroWriter(@"C:\Temp\dr.avro")
                    )
                    w.Write(dr);
            }

            using (var r = new ChoAvroReader(@"C:\Temp\dr.avro")
            )
            {
                r.Print();
            }
        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            typeof(ChoJSONReader).GetAssemblyVersion().Print();
            TwitterSnappyAvroTest();

            return;
            //POCOTest();
            TwitterSnappyAvroTest();
        }

        static void TwitterSnappyAvroTest()
        {
            using (var r = new ChoAvroReader("twitter.snappy.avro")
                .Configure(c => c.UseAvroSerializer = false)
                .Configure(c => c.Codec = new CodecFactory().Create(AvroCompressionCodec.Snappy))
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                foreach (var rec in r)
                    rec.Print();
            }
        }

        static void TwitterAvroTest()
        {
            using (var r = new ChoAvroReader("twitter.avro")
                .Configure(c => c.UseAvroSerializer = false)
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                )
            {
                foreach (var rec in r)
                    rec.Print();
            }
        }

        static void SerializeAndDeserializeDynamicTest()
        {
            string path = "AvroSampleReflection.avro";
            //SerializeDynamicSampleFile(path);

            var dict = new Dictionary<string, object>();
            dict.Add("1", 3);
            dict.Add("2", new Location { Room = 243, Floor = 1 });

            ChoAvroRecordConfiguration config = null;
            AvroSerializerSettings sett1 = null;
            using (var w = new ChoAvroWriter(path)
                .WithAvroSerializer(AvroSerializer.Create<Dictionary<string, object>>(new AvroSerializerSettings() { Resolver = new ChoAvroPublicMemberContractResolver() }))
                .Configure(c => c.KnownTypes = new List<Type> { typeof(Location), typeof(string), typeof(int) })
                //.Configure(c => c.UseAvroSerializer = true)
                //.Configure(c => c.AvroSerializerSettings.Resolver = new AvroDataContractResolverEx())
                )
            {
                sett1 = w.Configuration.AvroSerializerSettings;
                config = w.Configuration;

                w.Write(dict);
                w.Write(dict);
                w.Write(dict);
            }
            //var sett = new AvroSerializerSettings();
            //sett.Resolver = new ChoAvroPublicMemberContractResolver(); // false) { Configuration = config };
            //sett.KnownTypes = new List<Type> { typeof(Location), typeof(string), typeof(int) };
            //var avroSerializer = AvroSerializer.Create<Dictionary<string, object>>(sett1);
            //using (var r = new StreamReader(path))
            //{
            //    var rec = avroSerializer.Deserialize(r.BaseStream);
            //    var rec2 = avroSerializer.Deserialize(r.BaseStream);
            //    var rec3 = avroSerializer.Deserialize(r.BaseStream);
            //    Console.WriteLine(rec.Dump());
            //    Console.WriteLine(rec2.Dump());
            //    Console.WriteLine(rec3.Dump());
            //    //var rec4 = avroSerializer.Deserialize(r);
            //}

            StringBuilder json = new StringBuilder();
            using (var r = new ChoAvroReader(path)
                .Configure(c => c.KnownTypes = new List<Type> { typeof(Location), typeof(string), typeof(int) })
                .Configure(c => c.UseAvroSerializer = true)
                //.Configure(c => c.AvroSerializerSettings = sett1)
                .Configure(c => c.NestedKeySeparator = '_')
                )
            {
                //var dt = r.AsDataTable();
                //Console.WriteLine(dt.Dump());
                //return;
                //foreach (var rec in r)
                //{
                //    Console.WriteLine(rec.Dump());
                //}
                //return;
                using (var w = new ChoJSONWriter(json)
                    .Configure(c => c.TurnOnAutoDiscoverJsonConverters = true)
                    )
                {
                    w.Write(r);
                }
            }
            Console.WriteLine(json.ToString());
        }

        static void POCOTest()
        {
            string path = "AvroPOCOSample1.avro";

            //SerializePOCOSampleFile(path);

            //var sett = new AvroSerializerSettings();
            //sett.Resolver = new ChoAvroPublicMemberContractResolver();
            //sett.KnownTypes = new List<Type> { typeof(Location), typeof(string) };
            //var avroSerializer = AvroSerializer.Create<SensorData>(sett);

            //using (var buffer = new StreamReader(File.OpenRead(path)))
            //{
            //    var actual1 = avroSerializer.Deserialize(buffer.BaseStream);
            //    var actual2 = avroSerializer.Deserialize(buffer.BaseStream);

            //    Console.WriteLine(actual1.Dump());
            //}
            //return;
            var testData = new List<SensorData>
                        {
                            new SensorData { Value = new byte[] { 1, 2, 3, 4, 5 }, Position = new Location { Room = 243, Floor = 1 } },
                            new SensorData { Value = new byte[] { 6, 7, 8, 9 }, Position = new Location { Room = 244, Floor = 1 } }
                        };

            using (var w = new ChoAvroWriter<SensorData>(path)
                )
            {
                //w.Write(testData);
                w.Write(new SensorData { Value = new byte[] { 1, 2, 3, 4, 5 }, Position = new Location { Room = 243, Floor = 1 } });
                w.Write(new SensorData { Value = new byte[] { 6, 7, 8, 9 }, Position = new Location { Room = 244, Floor = 1 } });
            }

            StringBuilder json = new StringBuilder();
            using (var r = new ChoAvroReader<SensorData>(path)
                //.WithAvroSerializer(AvroSerializer.Create<Dictionary<string, object>>(new AvroSerializerSettings()))
                )
            {
                foreach (var rec in r)
                    Console.WriteLine(rec.Dump());
                return;
                using (var w = new ChoJSONWriter(json)
                    .Configure(c => c.TurnOnAutoDiscoverJsonConverters = true)
                    )
                {
                    w.Write(r);
                }
            }
            Console.WriteLine(json.ToString());
        }

        public static void SerializeDynamicSampleFile(string path)
        {
            Console.WriteLine("SERIALIZATION USING GENERIC RECORD AND AVRO OBJECT CONTAINER FILES\n");

            Console.WriteLine("Defining the Schema and creating Sample Data Set...");

            //Define the schema in JSON
            const string Schema = @"{
                                ""type"":""record"",
                                ""name"":""Microsoft.Hadoop.Avro.Specifications.SensorData"",
                                ""fields"":
                                    [
                                        { 
                                            ""name"":""Location"", 
                                            ""type"":
                                                {
                                                    ""type"":""record"",
                                                    ""name"":""Microsoft.Hadoop.Avro.Specifications.Location"",
                                                    ""fields"":
                                                        [
                                                            { ""name"":""Floor"", ""type"":""int"" },
                                                            { ""name"":""Room"", ""type"":""int"" }
                                                        ]
                                                }
                                        },
                                        { ""name"":""Value"", ""type"":""bytes"" }
                                    ]
                            }";

            //Create a generic serializer based on the schema
            var serializer = AvroSerializer.CreateGeneric(Schema);
            var rootSchema = serializer.WriterSchema as RecordSchema;

            //Create a generic record to represent the data
            var testData = new List<AvroRecord>();

            dynamic expected1 = new AvroRecord(rootSchema);
            dynamic location1 = new AvroRecord(rootSchema.GetField("Location").TypeSchema);
            location1.Floor = 1;
            location1.Room = 243;
            expected1.Location = location1;
            expected1.Value = new byte[] { 1, 2, 3, 4, 5 };
            testData.Add(expected1);

            dynamic expected2 = new AvroRecord(rootSchema);
            dynamic location2 = new AvroRecord(rootSchema.GetField("Location").TypeSchema);
            location2.Floor = 1;
            location2.Room = 244;
            expected2.Location = location2;
            expected2.Value = new byte[] { 6, 7, 8, 9 };
            testData.Add(expected2);

            //Serializing and saving data to file
            //Create a MemoryStream buffer
            using (var buffer = new MemoryStream())
            {
                Console.WriteLine("Serializing Sample Data Set...");

                //Create a SequentialWriter instance for type SensorData which can serialize a sequence of SensorData objects to stream
                //Data will not be compressed (Null compression codec)
                using (var writer = AvroContainer.CreateGenericWriter(Schema, buffer, Codec.Null))
                {
                    using (var streamWriter = new SequentialWriter<object>(writer, 24))
                    {
                        // Serialize the data to stream using the sequential writer
                        testData.ForEach(streamWriter.Write);
                    }
                }

                Console.WriteLine("Saving serialized data to file...");

                //Save stream to file
                if (!WriteFile(buffer, path))
                {
                    Console.WriteLine("Error during file operation. Quitting method");
                    return;
                }
            }
        }

        public static void SerializePOCOSampleFile(string path)
        {
            Console.WriteLine("SERIALIZATION USING REFLECTION AND AVRO OBJECT CONTAINER FILES\n");

            //Create a data set using sample Class and struct
            var testData = new List<SensorData>
                        {
                            new SensorData { Value = new byte[] { 1, 2, 3, 4, 5 }, Position = new Location { Room = 243, Floor = 1 } },
                            new SensorData { Value = new byte[] { 6, 7, 8, 9 }, Position = new Location { Room = 244, Floor = 1 } }
                        };

            using (var w = new ChoAvroWriter<SensorData>(path))
                w.Write(testData);
            return;

            var sett = new AvroSerializerSettings();
            sett.Resolver = new AvroPublicMemberContractResolver();

            //Serializing and saving data to file
            //Creating a Memory Stream buffer
            using (var buffer = new MemoryStream())
            {
                Console.WriteLine("Serializing Sample Data Set...");

                //Create a SequentialWriter instance for type SensorData which can serialize a sequence of SensorData objects to stream
                //Data will be compressed using Deflate codec
                using (var w = AvroContainer.CreateWriter<SensorData>(buffer, sett, Codec.Deflate))
                {
                    using (var writer = new SequentialWriter<SensorData>(w, 24))
                    {
                        // Serialize the data to stream using the sequential writer
                        testData.ForEach(writer.Write);
                    }
                }

                //Save stream to file
                Console.WriteLine("Saving serialized data to file...");
                if (!WriteFile(buffer, path))
                {
                    Console.WriteLine("Error during file operation. Quitting method");
                    return;
                }
            }
            ////Reading and deserializing data
            ////Creating a Memory Stream buffer
            //using (var buffer = new MemoryStream())
            //{
            //    Console.WriteLine("Reading data from file...");

            //    //Reading data from Object Container File
            //    if (!ReadFile(buffer, path))
            //    {
            //        Console.WriteLine("Error during file operation. Quitting method");
            //        return;
            //    }

            //    Console.WriteLine("Deserializing Sample Data Set...");

            //    //Prepare the stream for deserializing the data
            //    buffer.Seek(0, SeekOrigin.Begin);

            //    //Create a SequentialReader for type SensorData which will derserialize all serialized objects from the given stream
            //    //It allows iterating over the deserialized objects because it implements IEnumerable<T> interface
            //    using (var reader = new SequentialReader<SensorData>(
            //        AvroContainer.CreateReader<SensorData>(buffer, true)))
            //    {
            //        var results = reader.Objects;

            //        //Finally, verify that deserialized data matches the original one
            //        Console.WriteLine("Comparing Initial and Deserialized Data Sets...");
            //        int count = 1;
            //        var pairs = testData.Zip(results, (serialized, deserialized) => new { expected = serialized, actual = deserialized });
            //        foreach (var pair in pairs)
            //        {
            //            bool isEqual = this.Equal(pair.expected, pair.actual);
            //            Console.WriteLine("For Pair {0} result of Data Set Identity Comparison is {1}", count, isEqual.ToString());
            //            count++;
            //        }
            //    }
            //}

            ////Delete the file
            //RemoveFile(path);
        }

        //Saving memory stream to a new file with the given path
        private static bool WriteFile(MemoryStream InputStream, string path)
        {
            if (File.Exists(path))
                File.Delete(path);

            if (!File.Exists(path))
            {
                try
                {
                    using (FileStream fs = File.Create(path))
                    {
                        InputStream.Seek(0, SeekOrigin.Begin);
                        InputStream.CopyTo(fs);
                    }
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("The following exception was thrown during creation and writing to the file \"{0}\"", path);
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
            else
            {
                Console.WriteLine("Can not create file \"{0}\". File already exists", path);
                return false;

            }
        }
    }
}
