using ChoETL;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChoJSONReaderTest.Program;

namespace ChoJSONReaderTest.Tests
{
    public class DeserializeToConcreteClassesUsingDiscriminatorAttributeTests
    {
        static readonly string json = @"
[
  {
    ""id"": ""123-22"",
    ""albums"":
    [
      {
        ""id"": ""OUHDm"",
        ""title"": ""My most recent drawing. Spent over 100 hours."",
        ""is_album"": false
      },
      {
        ""id"": ""lDRB2"",
        ""title"": ""Imgur Office"",
        ""is_album"": true,
        ""images_count"": 3,
        ""images"": [
          {
            ""id"": ""24nLu"",
            ""link"": ""http://i.imgur.com/24nLu.jpg""
          },
          {
            ""id"": ""Ziz25"",
            ""link"": ""http://i.imgur.com/Ziz25.jpg""
          },
          {
            ""id"": ""9tzW6"",
            ""link"": ""http://i.imgur.com/9tzW6.jpg""
          }
        ]
      }
    ]
  }
]";

        private static List<IGalleryItemX> _expected = new List<IGalleryItemX>
        {
            new GalleryItemX
            {
                id = "OUHDm",
                title = "My most recent drawing. Spent over 100 hours.",
                is_album = false,
            },
            new GalleryAlbumX
            {
                id = "lDRB2",
                title = "Imgur Office",
                is_album = true,
                images_count = 3,
                images = new List<GalleryImageX>
                {
                    new GalleryImageX
                    {
                        id = "24nLu",
                        link = "http://i.imgur.com/24nLu.jpg",
                    },
                    new GalleryImageX
                    {
                        id = "Ziz25",
                        link = "http://i.imgur.com/Ziz25.jpg",
                    },
                    new GalleryImageX
                    {
                        id = "9tzW6",
                        link = "http://i.imgur.com/9tzW6.jpg",
                    },
                }
            }
        };

        [ChoKnownTypeDiscriminator("is_album")]
        [ChoKnownType(typeof(GalleryAlbumX), "true")]
        [ChoKnownType(typeof(GalleryItemX), "false")]
        public interface IGalleryItemX
        {
        }

        public class GalleryItemX : IGalleryItemX
        {
            public string id { get; set; }
            public string title { get; set; }
            public bool is_album { get; set; }

            public override bool Equals(object other)
            {
                var toCompareWith = other as GalleryItemX;
                if (toCompareWith == null)
                    return false;
                return this.id == toCompareWith.id;
            }
            public override int GetHashCode()
            {
                return new { id }.GetHashCode();
            }
        }

        public class GalleryAlbumX : GalleryItemX
        {
            public int images_count { get; set; }
            public List<GalleryImageX> images { get; set; }

            public override bool Equals(object other)
            {
                var toCompareWith = other as GalleryAlbumX;
                if (toCompareWith == null)
                    return false;
                return this.id == toCompareWith.id
                    && Enumerable.SequenceEqual(this.images, toCompareWith.images);
            }
            public override int GetHashCode()
            {
                return new { id }.GetHashCode();
            }
        }

        public class GalleryImageX
        {
            public string id { get; set; }
            public string link { get; set; }
            public override bool Equals(object other)
            {
                var toCompareWith = other as GalleryImageX;
                if (toCompareWith == null)
                    return false;
                return this.id == toCompareWith.id;
            }
            public override int GetHashCode()
            {
                return new { id }.GetHashCode();
            }
        }

        //public class RootAlblumX
        //{
        //    public string id { get; set; }
        //    //[JsonConverter(typeof(ChoKnownTypeConverter<IGalleryItemX>))]
        //    public List<IGalleryItemX> albums { get; set; }
        //}

        [Test]
        public static void DeserializeToConcreteClassesUsingDiscriminatorAttributeTest()
        {
            List<IGalleryItemX> actual = new List<IGalleryItemX>();
            using (var r = ChoJSONReader<IGalleryItemX>.LoadText(json)
                .WithJSONPath("$..albums")
                .Configure(c => c.SupportsMultiRecordTypes = true)
                //.UseJsonSerialization()
                //.JsonSerializationSettings(s => s.Converters.Add(Activator.CreateInstance<ChoKnownTypeConverter<IGalleryItemX>>()))
                )
            {
                actual.AddRange(r.ToArray());
            }

            CollectionAssert.AreEqual(_expected, actual);
        }
    }
}
