using ChoETL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ChoCSVWriterTest.Test
{
    public class BaseTests
    {

        [Test]
        public static void TypeConverterTest()
        {
            string expected = @"IPAddress,Description
10.0.0.0,Main
10.0.128.0,Sub1
10.0.128.16,Sub2";

            List<IpAddressRecord> recs = new List<IpAddressRecord>
            {
                new IpAddressRecord { Description = "Main", IPAddress = IPAddress.Parse("10.0.0.0")},
                new IpAddressRecord { Description = "Sub1", IPAddress = IPAddress.Parse("10.0.128.0")},
                new IpAddressRecord { Description = "Sub2", IPAddress = IPAddress.Parse("10.0.128.16")},
            };

            StringBuilder actual = new StringBuilder();
            using (var w = new ChoCSVWriter<IpAddressRecord>(actual)
                .WithFirstLineHeader()
                )
            {
                w.Write(recs);
            }
            actual.Print();
            CollectionAssert.AreEqual(expected, actual.ToString());
        }

        [Test]
        public static void TypeConverterWithoutCSVRecordFieldAttrTest()
        {
            string expected = @"IPAddress,Description
10.0.0.0,Main
10.0.128.0,Sub1
10.0.128.16,Sub2";

            List<IpAddressRecordWithoutCSVRecordFieldAttr> recs = new List<IpAddressRecordWithoutCSVRecordFieldAttr>
            {
                new IpAddressRecordWithoutCSVRecordFieldAttr { Description = "Main", IPAddress = IPAddress.Parse("10.0.0.0")},
                new IpAddressRecordWithoutCSVRecordFieldAttr { Description = "Sub1", IPAddress = IPAddress.Parse("10.0.128.0")},
                new IpAddressRecordWithoutCSVRecordFieldAttr { Description = "Sub2", IPAddress = IPAddress.Parse("10.0.128.16")},
            };

            StringBuilder actual = new StringBuilder();
            using (var w = new ChoCSVWriter<IpAddressRecordWithoutCSVRecordFieldAttr>(actual)
                .WithFirstLineHeader()
                )
            {
                w.Write(recs);
            }
            actual.Print();
            CollectionAssert.AreEqual(expected, actual.ToString());
        }
    }
    [ChoCSVFileHeader]
    internal class IpAddressRecordWithoutCSVRecordFieldAttr
    {
        [ChoTypeConverter(typeof(IpAddressTypeConverter))]
        public IPAddress IPAddress { get; set; }
        public string Description { get; set; }


        public override bool Equals(object other)
        {
            var toCompareWith = other as IpAddressRecordWithoutCSVRecordFieldAttr;
            if (toCompareWith == null)
                return false;
            return this.Description == toCompareWith.Description &&
                this.IPAddress.Equals(toCompareWith.IPAddress);
        }
        public override int GetHashCode()
        {
            return new { Description, IPAddress }.GetHashCode();
        }
    }
    [ChoCSVFileHeader]
    internal class IpAddressRecord
    {
        [ChoCSVRecordField(FieldName = "IPAddress"),
         ChoTypeConverter(typeof(IpAddressTypeConverter))]
        public IPAddress IPAddress { get; set; }
        [ChoCSVRecordField(FieldName = "Description")]
        public string Description { get; set; }


        public override bool Equals(object other)
        {
            var toCompareWith = other as IpAddressRecord;
            if (toCompareWith == null)
                return false;
            return this.Description == toCompareWith.Description &&
                this.IPAddress.Equals(toCompareWith.IPAddress);
        }
        public override int GetHashCode()
        {
            return new { Description, IPAddress }.GetHashCode();
        }
    }
    internal class IpAddressTypeConverter : IChoValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string workValue;
            IPAddress workIpAddress;

            workValue = value as string;

            System.Diagnostics.Debug.WriteLine("Convert");
            Console.WriteLine("Convert");

            if (workValue is null)
            {
                return null;
            }

            if (IPAddress.TryParse(workValue, out workIpAddress))
            {
                return workIpAddress;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Diagnostics.Debug.WriteLine("ConvertBack");
            Console.WriteLine("ConvertBack");

            if (value == null)
            {
                return null;
            }
            return ((IPAddress)value).ToString();
        }
    }
}
