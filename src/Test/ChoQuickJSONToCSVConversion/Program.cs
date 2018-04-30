using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChoETL;


namespace ChoQuickJSONToCSVConversion
{
    class Program
    {
        static void Main(string[] args)
        {
			//ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Verbose;
			UsingProjection();
		}

        private static void QuickConversion()
        {
            using (var csv = new ChoCSVWriter("emp.csv").WithFirstLineHeader())
            {
                using (var json = new ChoJSONReader("emp.json"))
                {
                    csv.Write(json);
                }
            }
        }

		private static void SelectiveConversion()
		{
			using (var csv = new ChoCSVWriter("emp.csv").WithFirstLineHeader())
			{
				using (var json = new ChoJSONReader("emp.json")
					.WithField("FirstName")
					.WithField("LastName")
					.WithField("Age", fieldType: typeof(int))
					.WithField("StreetAddress", jsonPath: "$.address.streetAddress")
					.WithField("City", jsonPath: "$.address.city")
					.WithField("State", jsonPath: "$.address.state")
					.WithField("PostalCode", jsonPath: "$.address.postalCode")
					.WithField("Phone", jsonPath: "$.phoneNumber[?(@.type=='home')].number")
					.WithField("Fax", jsonPath: "$.phoneNumber[?(@.type=='fax')].number")
				)
				{
					csv.Write(json);
				}
			}
		}

		private static void UsingPOCO()
        {
            using (var csv = new ChoCSVWriter<Employee>("emp.csv").WithFirstLineHeader())
            {
                using (var json = new ChoJSONReader<Employee>("emp.json"))
                {
                    csv.Write(json);
                }
            }
        }

        private static void UsingProjection()
        {
            using (var csv = new ChoCSVWriter("emp.csv").WithFirstLineHeader())
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
