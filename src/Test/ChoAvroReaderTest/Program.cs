using ChoETL;
using Microsoft.Hadoop.Avro;
using Microsoft.Hadoop.Avro.Container;
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

        static void Main(string[] args)
        {
            POCOTest();
        }

        static void POCOTest()
        {
            string path = "AvroSampleReflection.avro";

            SerializeSampleFile(path);
            StringBuilder json = new StringBuilder();
            using (var r = new ChoAvroReader<SensorData>(path)
                )
            {
                using (var w = new ChoJSONWriter(json)
                    .Configure(c => c.TurnOnAutoDiscoverJsonConverters = true)
                    )
                {
                    w.Write(r);
                    //foreach (var rec in r)
                    //    Console.WriteLine(rec.Dump());
                }
            }
            Console.WriteLine(json.ToString());
        }

        public static void SerializeSampleFile(string path)
        {
            Console.WriteLine("SERIALIZATION USING REFLECTION AND AVRO OBJECT CONTAINER FILES\n");

            //Create a data set using sample Class and struct
            var testData = new List<SensorData>
                        {
                            new SensorData { Value = new byte[] { 1, 2, 3, 4, 5 }, Position = new Location { Room = 243, Floor = 1 } },
                            new SensorData { Value = new byte[] { 6, 7, 8, 9 }, Position = new Location { Room = 244, Floor = 1 } }
                        };

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
