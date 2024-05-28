using System;
using System.Collections.Generic;
using ChoETL;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

public class Program
{
    public static void Main()
    {
        //ChoETLSettings.KeySeparator = '!';
        using (var r = ChoJSONReader<Member>.LoadText(json)
               .WithJSONPath("Member")
               .Configure(c => c.FlattenByNodeName = "Information")
                .UseJsonSerialization()
              )
        {
            using (var w = new ChoCSVWriter<Member>(Console.Out)
                   .WithFirstLineHeader()
                   .Configure(c => c.TypeConverterFormatSpec.DateTimeFormat = "yyyy-MM-dd HH:mm:ss")
                  )
            {
                w.Write(r);
            }

        }
    }

    static string json = @"{
  ""Member"": [
    {
      ""ExtractDate"": ""2024-03-18T13:29:50Z"",
      ""Information"": {
        ""Surname"": ""Smith"",
        ""FirstName"": ""John"",
        ""DateOfBirth"": ""1960-03-12"",
        ""Email"": ""test@test.com"",
        ""Telephone"": ""01234-123456"",
        ""Address"": {
          ""Line1"": ""1 Road"",
          ""Line2"": ""District"",
          ""Zipcode"": ""001555""
        },
        ""Employment"": [
          {
            ""EmployerName"": ""AAA Ltd"",
            ""StartDate"": ""1988-04-01"",
            ""EndDate"": ""1990-10-15""
          },
          {
            ""EmployerName"": ""ABC Ltd"",
            ""StartDate"": ""1991-01-25"",
            ""EndDate"": ""1995-11-30""
          }
        ]
      },
      ""AdditionalInfo"": [
        {
          ""Type"": ""A"",
          ""AlternateName"": ""Name"",
          ""AdditionalMessage"": [
            ""A"",
            ""B"",
            ""C""
          ]
        },
        {
          ""Type"": ""A"",
          ""AlternateName"": ""Name"",
          ""AdditionalMessage"": [
            ""A"",
            ""B"",
            ""C""
          ]
        }
      ]
    },
    {
      ""ExtractDate"": ""2024-03-18T13:29:50Z"",
      ""Information"": {
        ""Surname"": ""John"",
        ""FirstName"": ""Joe"",
        ""DateOfBirth"": ""1960-03-12"",
        ""Email"": ""test@test.com"",
        ""Telephone"": ""01234-123456"",
        ""Address"": {
          ""Line1"": ""1 Road"",
          ""Line2"": ""District"",
          ""Zipcode"": ""001555""
        },
        ""Employment"": [
          {
            ""EmployerName"": ""AAA Ltd"",
            ""StartDate"": ""1988-04-01"",
            ""EndDate"": ""1990-10-15""
          }
        ]
      },
      ""AdditionalInfo"": [
        {
          ""Type"": ""Z"",
          ""AlternateName"": ""NameZ"",
          ""AdditionalMessage"": [
            ""X"",
            ""Y"",
            ""Z""
          ]
        },
        {
          ""Type"": ""A"",
          ""AlternateName"": ""Name"",
          ""AdditionalMessage"": [
            ""A"",
            ""B"",
            ""C""
          ]
        }
      ]
    }
  ]
}";

    public class AdditionalInfo
    {
        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("AlternateName")]
        public string AlternateName { get; set; }

        [JsonProperty("AdditionalMessage")]
        public List<string> AdditionalMessage { get; set; }
    }

    public class Address
    {
        [JsonProperty("Line1")]
        public string AddressLine1 { get; set; }

        [JsonProperty("Line2")]
        public string AddressLine2 { get; set; }

        [JsonProperty("Zipcode")]
        public string Zipcode { get; set; }
    }

    public class Employment
    {
        [JsonProperty("EmployerName")]
        public string EmployerName { get; set; }

        [JsonProperty("StartDate")]
        public string EmploymentStartDate { get; set; }

        [JsonProperty("EndDate")]
        public string EmploymentEndDate { get; set; }
    }

    public class Information
    {
        [JsonProperty("Surname")]
        public string Surname { get; set; }

        [JsonProperty("FirstName")]
        public string FirstName { get; set; }

        [JsonProperty("DateOfBirth")]
        public string DateOfBirth { get; set; }

        [JsonProperty("Email")]
        public string Email { get; set; }

        [JsonProperty("Telephone")]
        public string Telephone { get; set; }

        [JsonProperty("Address")]
        public Address Address { get; set; }

        [JsonProperty("Employment")]
        [ChoTypeConverter(typeof(EmploymentConverter))]
        [ChoQuoteField(false)]
        public List<Employment> Employment { get; set; }
    }

    public class Member
    {
        [JsonProperty("ExtractDate")]
        public DateTime ExtractDate { get; set; }

        [JsonProperty("Information")]
        public Information Information { get; set; }

        [JsonProperty("AdditionalInfo")]
        [ChoTypeConverter(typeof(AdditionalInfoConverter))]
        [ChoQuoteField(false)]
        public List<AdditionalInfo> AdditionalInfo { get; set; }
    }

    public class Root
    {
        [JsonProperty("Member")]
        public List<Member> Member { get; set; }
    }

    public class EmploymentConverter : IChoValueConverter, IChoHeaderConverter, IChoCollectionConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var list = (value as ICollection<Employment>).Take(2).ToList();
            if (list.Count() < 2)
            {
                list.Add(new Employment());
            }
            return list.Select(f => new object[] { f.EmployerName, f.EmploymentStartDate, f.EmploymentEndDate }).Unfold().ToArray();
        }

        public string GetHeader(string name, string fieldName, object parameter, CultureInfo culture)
        {
            return "EmployerName,EmplymentStartDate,EmplymentStartEndDate,EmployerName2,EmplymentStartDate2,EmplymentStartEndDate2";
        }

    }
    public class AdditionalInfoConverter : IChoValueConverter, IChoHeaderConverter, IChoCollectionConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = (value as ICollection<AdditionalInfo>).FirstOrDefault();
            if (item == null)
                item = new AdditionalInfo();

            var list = new AdditionalInfo[] { item };
            return list.Select(f => new object[] { f.Type, f.AlternateName, String.Join("+", f.AdditionalMessage) }).Unfold().ToArray();
        }

        public string GetHeader(string name, string fieldName, object parameter, CultureInfo culture)
        {
            return "Type,AlternateName,AdditionalMessage";
        }

    }
}