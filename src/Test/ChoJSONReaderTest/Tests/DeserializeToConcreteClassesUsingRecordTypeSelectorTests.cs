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
    public class DeserializeToConcreteClassesUsingRecordTypeSelectorTests
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

        private static List<IGalleryItem> _expected = new List<IGalleryItem>
        {
            new GalleryItem
            {
                id = "OUHDm",
                title = "My most recent drawing. Spent over 100 hours.",
                is_album = false,
            },
            new GalleryAlbum
            {
                id = "lDRB2",
                title = "Imgur Office",
                is_album = true,
                images_count = 3,
                images = new List<GalleryImage>
                {
                    new GalleryImage
                    {
                        id = "24nLu",
                        link = "http://i.imgur.com/24nLu.jpg",
                    },
                    new GalleryImage
                    {
                        id = "Ziz25",
                        link = "http://i.imgur.com/Ziz25.jpg",
                    },
                    new GalleryImage
                    {
                        id = "9tzW6",
                        link = "http://i.imgur.com/9tzW6.jpg",
                    },
                }
            }
        };

        public interface IGalleryItem
        {
        }

        public class GalleryItem : IGalleryItem
        {
            public string id { get; set; }
            public string title { get; set; }
            public bool is_album { get; set; }

            public override bool Equals(object other)
            {
                var toCompareWith = other as GalleryItem;
                if (toCompareWith == null)
                    return false;
                return this.id == toCompareWith.id;
            }
            public override int GetHashCode()
            {
                return new { id }.GetHashCode();
            }
        }

        public class GalleryAlbum : GalleryItem
        {
            public int images_count { get; set; }
            public List<GalleryImage> images { get; set; }

            public override bool Equals(object other)
            {
                var toCompareWith = other as GalleryAlbum;
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

        public class GalleryImage
        {
            public string id { get; set; }
            public string link { get; set; }

            public override bool Equals(object other)
            {
                var toCompareWith = other as GalleryImage;
                if (toCompareWith == null)
                    return false;
                return this.id == toCompareWith.id;
            }
            public override int GetHashCode()
            {
                return new { id }.GetHashCode();
            }
        }

        [Test]
        public static void DeserializeToConcreteClassesUsingRecordTypeSelector()
        {
            List<IGalleryItem> actual = new List<IGalleryItem>();
            using (var r = ChoJSONReader<IGalleryItem>.LoadText(json)
                .WithJSONPath("$..albums")
                .ErrorMode(ChoErrorMode.IgnoreAndContinue)
                .Configure(c => c.RecordTypeSelector = o =>
                {
                    if (o is Tuple<long, JObject> tuple)
                    {
                        dynamic dobj = tuple;
                        switch ((bool)dobj.Item2.is_album)
                        {
                            case true:
                                return typeof(GalleryAlbum);
                            default:
                                return typeof(GalleryItem);
                        }
                    }
                    return null;
                })
                .Configure(c => c.SupportsMultiRecordTypes = true)
                )
            {
                actual.AddRange(r.ToArray());
            }

            CollectionAssert.AreEqual(_expected, actual);
        }
    }
}
