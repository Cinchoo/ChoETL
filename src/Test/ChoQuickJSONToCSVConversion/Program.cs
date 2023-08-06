using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChoETL;
using NUnit.Framework;

namespace ChoQuickJSONToCSVConversion
{
    class Program
    {
        static void Main(string[] args)
        {
			//ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Verbose;
			UsingProjection();
		}
        [Test]
        public static void QuickConversion()
        {
            string expected = @"firstName,lastName,age,address_streetAddress,address_city,address_state,address_postalCode,phoneNumber_0_type,phoneNumber_0_number,phoneNumber_1_type,phoneNumber_1_number
John,Smith,25,21 2nd Street,New York,NY,10021,home,212 555-1234,fax,646 555-4567
Tom,Mark,50,10 Main Street,Edison,NJ,08837,home,732 555-1234,fax,609 555-4567";

            StringBuilder csvOut = new StringBuilder();
            using (var csv = new ChoCSVWriter(csvOut).WithFirstLineHeader())
            {
                using (var json = new ChoJSONReader("emp.json"))
                {
                    csv.Write(json);
                }
            }

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void QuickConversion_1()
        {
            string expected = @"firstName,lastName,age,address_streetAddress,address_city,address_state,address_postalCode,phoneNumber_0_type,phoneNumber_0_number,phoneNumber_1_type,phoneNumber_1_number
John,Smith,25,21 2nd Street,New York,NY,10021,home,212 555-1234,fax,646 555-4567
Tom,Mark,50,10 Main Street,Edison,NJ,08837,home,732 555-1234,fax,609 555-4567";

            string expected1 = @"firstName,lastName,age,streetAddress,city,state,postalCode,phoneNumber_0_type,phoneNumber_0_number,phoneNumber_1_type,phoneNumber_1_number
John,Smith,25,21 2nd Street,New York,NY,10021,home,212 555-1234,fax,646 555-4567
Tom,Mark,50,10 Main Street,Edison,NJ,08837,home,732 555-1234,fax,609 555-4567";

            StringBuilder csvOut = new StringBuilder();
            using (var csv = new ChoCSVWriter(csvOut)
                .WithFirstLineHeader()
                .Configure(c => c.IgnoreDictionaryFieldPrefix = false)
                )
            {
                using (var json = new ChoJSONReader("emp.json"))
                {
                    csv.Write(json);
                }
            }

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
		public static void SelectiveConversion()
		{
            string expected = @"FirstName,LastName,Age,StreetAddress,City,State,PostalCode,Phone,Fax
John,Smith,25,21 2nd Street,New York,NY,10021,212 555-1234,646 555-4567
Tom,Mark,50,10 Main Street,Edison,NJ,08837,732 555-1234,609 555-4567";

            StringBuilder csvOut = new StringBuilder();
            using (var csv = new ChoCSVWriter(csvOut).WithFirstLineHeader())
			{
				using (var json = new ChoJSONReader("emp.json")
					.WithField("FirstName")
					.WithField("LastName")
					.WithField("Age", fieldType: typeof(int))
					.WithField("StreetAddress", jsonPath: "$.address.streetAddress", isArray: false)
					.WithField("City", jsonPath: "$.address.city", isArray: false)
					.WithField("State", jsonPath: "$.address.state", isArray: false)
					.WithField("PostalCode", jsonPath: "$.address.postalCode", isArray: false)
					.WithField("Phone", jsonPath: "$.phoneNumber[?(@.type=='home')].number", isArray: false)
					.WithField("Fax", jsonPath: "$.phoneNumber[?(@.type=='fax')].number", isArray: false)
				)
				{
					csv.Write(json);
				}
			}

            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public static void UsingPOCO()
        {
            string expected = @"FirstName,LastName,Age,StreetAddress,City,State,PostalCode,Phone,Fax
John,Smith,25,21 2nd Street,New York,NY,10021,212 555-1234,646 555-4567
Tom,Mark,50,10 Main Street,Edison,NJ,08837,732 555-1234,609 555-4567";

            StringBuilder csvOut = new StringBuilder();
            using (var csv = new ChoCSVWriter<Employee>(csvOut).WithFirstLineHeader())
            {
                using (var json = new ChoJSONReader<Employee>("emp.json"))
                {
                    csv.Write(json);
                }
            }
            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void UsingProjection()
        {
            string expected = @"FirstName,LastName,Age,StreetAddress,City,State,PostalCode,Phone,Fax
John,Smith,25,21 2nd Street,New York,NY,10021,212 555-1234,646 555-4567
Tom,Mark,50,10 Main Street,Edison,NJ,08837,732 555-1234,609 555-4567";

            StringBuilder csvOut = new StringBuilder();
            using (var csv = new ChoCSVWriter(csvOut).WithFirstLineHeader())
            {
                using (var json = new ChoJSONReader("emp.json"))
                {
                    csv.Write(json.Cast<dynamic>().Select(i => new {
                        FirstName = i.firstName,
                        LastName = i.lastName,
                        Age = i.age,
                        StreetAddress = i.address.streetAddress,
                        City = i.address.city,
                        State = i.address.state,
                        PostalCode = i.address.postalCode,
                        Phone = i.phoneNumber[0].number,
                        Fax = i.phoneNumber[1].number
                    }));
                }
            }
            var actual = csvOut.ToString();
            Assert.AreEqual(expected, actual);
        }
    }

    public class Employee
    {
        [ChoJSONRecordField]
        public string FirstName { get; set; }
        [ChoJSONRecordField]
        public string LastName { get; set; }
        [ChoJSONRecordField]
        public int Age { get; set; }
        [ChoJSONRecordField(JSONPath = "$.address.streetAddress")]
        public string StreetAddress { get; set; }
        [ChoJSONRecordField(JSONPath = "$.address.city")]
        public string City { get; set; }
        [ChoJSONRecordField(JSONPath = "$.address.state")]
        public string State { get; set; }
        [ChoJSONRecordField(JSONPath = "$.address.postalCode")]
        public string PostalCode { get; set; }
        [ChoJSONRecordField(JSONPath = "$.phoneNumber[?(@.type=='home')].number")]
        public string Phone { get; set; }
        [ChoJSONRecordField(JSONPath = "$.phoneNumber[?(@.type=='fax')].number")]
        public string Fax { get; set; }
    }
}
