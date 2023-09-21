using ChoETL;
using System;
using System.Data;
using System.Text;
using System.Linq;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using Newtonsoft.Json;
using NUnit.Framework.Constraints;
using System.IO;

namespace ChoParquetReaderTest
{
    class Program
    {
        [Test]
        public static void Test1()
        {
            string expected = @"Cust_ID,CustName,CustOrder,Salary,Guid
TCF4338,INDEXABLE CUTTING TOOL,4/11/2016 12:00:00 AM +00:00,100000,56531508-89c0-4ecf-afaf-cdf5aec56b19
CGO9650,Comercial Tecnipak Ltda,7/11/2016 12:00:00 AM +00:00,80000,56531508-89c0-4ecf-afaf-cdf5aec56b19";

            StringBuilder csv = new StringBuilder();
            using (var r = new ChoParquetReader(@"test1.parquet")
                .ParquetOptions(o => o.TreatByteArrayAsString = true)
                )
            {
                var recs = r.ToArray();
                recs.Print();

                using (var w = new ChoCSVWriter(csv)
                    .WithFirstLineHeader()
                    .UseNestedKeyFormat(false)
                    .TypeConverterFormatSpec(fs => fs.DateTimeFormat = "M/d/yyyy hh:mm:ss tt zzz")
                    )
                    w.Write(recs);
            }

            var actual = csv.ToString();
            Assert.AreEqual(expected, actual);
        }
        [Test]
        public static void DataTableTest()
        {
            string expected = @"[
  {
    ""Cust_ID"": ""TCF4338"",
    ""CustName"": ""INDEXABLE CUTTING TOOL"",
    ""CustOrder"": ""2016-04-11T00:00:00+00:00"",
    ""Salary"": 100000.0,
    ""Guid"": ""56531508-89c0-4ecf-afaf-cdf5aec56b19""
  },
  {
    ""Cust_ID"": ""CGO9650"",
    ""CustName"": ""Comercial Tecnipak Ltda"",
    ""CustOrder"": ""2016-07-11T00:00:00+00:00"",
    ""Salary"": 80000.0,
    ""Guid"": ""56531508-89c0-4ecf-afaf-cdf5aec56b19""
  }
]";
            StringBuilder csv = new StringBuilder();
            using (var r = new ChoParquetReader(@"test1.parquet")
                .ParquetOptions(o => o.TreatByteArrayAsString = true))
            {
                var dt = r.AsDataTable();
                var actual = JsonConvert.SerializeObject(dt, new JsonSerializerSettings() 
                { 
                    DateFormatString = "yyyy-MM-ddTHH:mm:sszzz", 
                    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                    Formatting = Formatting.Indented 
                });
                Assert.AreEqual(expected, actual);
            }

        }

        [Test]
        public static void ReadParquet52()
        {
            string expected = @"[
  {
    ""Product_Onboarding"": ""False"",
    ""Owner_name"": ""Karthi Mariappan S"",
    ""Owner_id"": ""2305460000000111005"",
    ""Owner_email"": ""karthi.s@hippovideo.io"",
    ""Mailing_State"": null,
    ""Competitor_Product"": null,
    ""Technologies_Used"": ""[\""google_adsense\"", \""facebook_advertiser\"", \""wordpress\"", \""instagram\"", \""facebook_connect\"", \""apache\"", \""hubspot\"", \""piwik\"", \""google_analytics\"", \""google_tag_manager\""]"",
    ""Account_Health"": null,
    ""Other_Country"": null,
    ""Renewal"": ""Monthly"",
    ""Demos_Given"": null,
    ""Pitched_Date"": null,
    ""Department"": null,
    ""$state"": ""save"",
    ""$process_flow"": ""False"",
    ""Video_Created"": null,
    ""id"": ""2305460000142878721"",
    ""CS_Updated_Date"": null,
    ""Data_Source"": ""Converted"",
    ""$approval_delegate"": false,
    ""$approval_approve"": false,
    ""$approval_reject"": false,
    ""$approval_resubmit"": false,
    ""Number_of_Users"": null,
    ""Products_Bought_0"": ""WIZ"",
    ""$data_source_details"": null,
    ""Created_Time"": ""2021-05-24T19:09:13+00:00"",
    ""Cross_sell_Op"": ""False"",
    ""HV_Account_Id"": 1423074,
    ""Video_Shared"": null,
    ""Upsell_Cross_sell_Date"": null,
    ""PU_RO"": 0,
    ""Contact_Established"": ""False"",
    ""Created_By_name"": ""Karthi Mariappan S"",
    ""Created_By_id"": ""2305460000000111005"",
    ""Created_By_email"": ""karthi.s@hippovideo.io"",
    ""Product_Adoption"": null,
    ""Demo_Request"": ""False"",
    ""UTM_Medium"": null,
    ""Description"": null,
    ""$review_process_approve"": false,
    ""$review_process_reject"": false,
    ""$review_process_resubmit"": false,
    ""Website"": null,
    ""Other_Zip"": null,
    ""Mailing_Street"": null,
    ""MRR"": 59,
    ""CS_Status"": null,
    ""Salutation"": null,
    ""Full_Name"": ""Aaron info@myprolificbrands.com"",
    ""Record_Image"": null,
    ""Renewed_Date"": ""2021-05-24"",
    ""Plan_Name"": ""hw-basicprosep2020"",
    ""Skype_ID"": null,
    ""Limit_exceeded"": null,
    ""Purchased_Date"": ""2021-05-24"",
    ""Use_Usecases"": ""Marketing"",
    ""Account_Name_name"": ""Prolific Brands"",
    ""Account_Name_id"": ""2305460000142878717"",
    ""Demo_Date"": null,
    ""Email_Opt_Out"": ""True"",
    ""Cancellation_Status"": null,
    ""Other_Street"": null,
    ""Mobile"": null,
    ""Territories"": null,
    ""$orchestration"": ""False"",
    ""Call_Result_Outbound"": null,
    ""Add_ons"": ""[\""hw-marketingpro-addon-june-2020-monthly-quantity-1\""]"",
    ""Drift_Link"": null,
    ""Trial_Expired_On"": ""2021-05-14T21:13:56+00:00"",
    ""Lead_Created_On"": ""2021-05-07T21:13:32+00:00"",
    ""Lead_Source"": ""direct"",
    ""CSM_Notes"": null,
    ""User_Occupation"": ""Marketing"",
    ""Lead_PU_Score"": 0,
    ""Email"": ""info@myprolificbrands.com"",
    ""Are_you_attending_dreamforce"": null,
    ""$currency_symbol"": ""$"",
    ""Other_Phone"": null,
    ""Licenses"": 1,
    ""Other_State"": null,
    ""$followers"": null,
    ""User_Category"": ""Business"",
    ""Funding_Raised"": null,
    ""LRT"": null,
    ""Last_Activity_Time"": ""2021-05-24T22:06:39+00:00"",
    ""User_agent"": ""Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36"",
    ""Industry"": ""Media"",
    ""Lead_RO_Score"": 125,
    ""Unsubscribed_Mode"": ""Manual"",
    ""Test_name"": ""Arjun R"",
    ""Test_id"": ""2305460000041922001"",
    ""Mailing_Country"": ""US"",
    ""Data_Processing_Basis_Details"": null,
    ""$approved"": ""True"",
    ""Outbound_Calling_Status"": null,
    ""Reporting_To"": null,
    ""Other_City"": null,
    ""User_Pilot_Mail_Client"": null,
    ""$followed"": ""False"",
    ""$editable"": ""True"",
    ""Product_Last_Activity_Time"": null,
    ""Referrer_URL"": null,
    ""Plan_Pitched"": null,
    ""Unqualified_Reason"": null,
    ""Demo_Notes"": null,
    ""UTM_Content"": null,
    ""Product_Training"": ""False"",
    ""Estimated_Annual_Revenue"": ""$1M-$10M"",
    ""Company_Name"": ""Prolific Brands"",
    ""Secondary_Email"": null,
    ""Not_Interested_Reason_Outbound"": null,
    ""MRR_Impact"": null,
    ""Mailing_Zip"": null,
    ""User_Pilot_CRM"": null,
    ""LinkedIn_Primary"": null,
    ""UTM_Campaign"": null,
    ""Unsubscribe_Promotional_Emails"": ""False"",
    ""First_Name"": ""Aaron"",
    ""Modified_By_name"": ""Karthi Mariappan S"",
    ""Modified_By_id"": ""2305460000000111005"",
    ""Modified_By_email"": ""karthi.s@hippovideo.io"",
    ""$review"": null,
    ""Lead_PQL_Score"": null,
    ""Number_of_Employees"": 30,
    ""Upsell_Op"": ""False"",
    ""Phone"": ""+18582829547"",
    ""Demo_Scheduled"": ""False"",
    ""Lead_ICP_Score"": 0,
    ""CSM"": null,
    ""Notes1"": null,
    ""Cancelled_On"": null,
    ""Modified_Time"": ""2021-05-24T22:06:39+00:00"",
    ""Funnel_Status"": ""Intention"",
    ""Integration"": null,
    ""Mailing_City"": null,
    ""IsValidEmail"": null,
    ""Contact_In_Future_Outbound"": null,
    ""Unsubscribed_Time"": ""2021-05-24T19:09:13+00:00"",
    ""Title"": ""Chief Revenue Officer/Chief Operations Officer/Chief Executive Officer"",
    ""Landing_Page"": ""https://www.hippovideo.io/"",
    ""Move_to_Free_Reason"": null,
    ""$stop_processing"": ""False"",
    ""Demo_Status"": null,
    ""Mutual_Success_Plan"": ""False"",
    ""Continent"": ""North America"",
    ""Last_Name"": ""info@myprolificbrands.com"",
    ""$in_merge"": ""False"",
    ""UTM_Source"": ""direct"",
    ""Trial_Extended_On"": null,
    ""$approval_state"": ""approved"",
    ""Account_Status"": ""Paid""
  }
]";

            using (var r = new ChoParquetReader("myData52.parquet"))
            {
                var rec = r.Take(1);
                    
                Console.WriteLine(rec.Dump());
                var actual = JsonConvert.SerializeObject(rec, new JsonSerializerSettings()
                {
                    DateFormatString = "yyyy-MM-ddTHH:mm:sszzz",
                    Formatting = Formatting.Indented,
                });
            
                Assert.AreEqual(expected, actual);
            }
        }

        //static void CSV2ParquetTest()
        //{
        //    using (var r = new ChoCSVReader(@"..\..\..\..\..\..\data\XBTUSD.csv")
        //        .Configure(c => c.LiteParsing = true)
        //        .NotifyAfter(100000)
        //        .OnRowsLoaded((o, e) => $"Rows Loaded: {e.RowsLoaded} <-- {DateTime.Now}".Print())
        //        .ThrowAndStopOnMissingField(false)
        //        )
        //    {
        //        //r.Loop();
        //        //return;
        //        using (var w = new ChoParquetWriter(@"..\..\..\..\..\..\data\XBTUSD.parquet")
        //            .Configure(c => c.RowGroupSize = 100000)
        //        .Configure(c => c.LiteParsing = true)
        //            )
        //            w.Write(r);
        //    }
        //}

        public class Trade
        {
            public long? Id { get; set; }
            public double? Price { get; set; }
            public double? Quantity { get; set; }
            public DateTime? CreateDateTime{ get; set; }
            public bool? IsActive { get; set; }
            public Decimal? Total { get; set; }
        }

        [Test]
        public static void WriteParquetWithNullableFields()
        {
            string filePath = @"Trade1.parquet";

            //Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            using (var w = new ChoParquetWriter<Trade>(filePath)
                .TypeConverterFormatSpec(ts => ts.DateTimeFormat = "MM^dd^yyyy")
                .TreatDateTimeAsString(true)
                )
            {
                w.Write(new Trade
                {
                    Id = 1,
                    Price = 1.3,
                    Quantity = 2.45,
                    CreateDateTime = null,
                });
            }

            string expected = @"{
  ""Id"": 1,
  ""Price"": 1.3,
  ""Quantity"": 2.45,
  ""CreateDateTime"": null,
  ""IsActive"": null,
  ""Total"": null
}";
            using (var r = new ChoParquetReader<Trade>(filePath))
            {
                var rec = r.First();
                var actual = JsonConvert.SerializeObject(rec, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        static void ParseLargeParquetTest()
        {
            using (var r = new ChoParquetReader(@"..\..\..\..\..\..\data\XBTUSD-Copy.parquet")
                .NotifyAfter(100000)
                .OnRowsLoaded((o, e) => $"Rows Loaded: {e.RowsLoaded} <-- {DateTime.Now}".Print())
                .ThrowAndStopOnMissingField(false)
                .Setup(s => s.BeforeRowGroupLoad += (o, e) => e.Skip = e.RowGroupIndex < 2)
                )
            {
                r.Loop();
            }

        }

        static void Main(string[] args)
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Error;
            WriteParquetWithNullableFields();
            return;
            //Issue144();
            return;
            WriteParquetWithNullableFields();
        }
        [Test]
        public static void Issue233()
        {
            string expected = @"{
  ""ClassNarrative"": ""Lloyd's Register (Contemplated) (2021-09-01)LR Class: 100 A1 Survey Type: Date: 2023-05 Class Notation: Class contemplated"",
  ""ClassificationSociety"": ""Lloyd's Register (Contemplated) 2021-09-01"",
  ""ClassificationSocietyCode"": ""LR"",
  ""CoreShipInd"": ""1"",
  ""DateOfBuild"": ""\/Date(1682899200000)\/"",
  ""Deadweight"": ""40000"",
  ""FlagCode"": ""LIB"",
  ""FlagName"": ""Liberia"",
  ""GrossTonnage"": ""25600"",
  ""GroupBeneficialOwner"": ""H Vogemann Holding GmbH"",
  ""GroupBeneficialOwnerCompanyCode"": ""5429984"",
  ""GroupBeneficialOwnerCountryOfDomicile"": ""Germany"",
  ""GroupBeneficialOwnerCountryOfDomicileCode"": ""GEU"",
  ""GroupBeneficialOwnerCountryOfRegistration"": ""Germany"",
  ""LastUpdateDate"": ""1648540697637+0100"",
  ""LeadShipInSeriesByIMONumber"": ""9941609"",
  ""NetTonnage"": ""18200"",
  ""Operator"": ""Vogemann GmbH"",
  ""OperatorCompanyCode"": ""5078199"",
  ""OperatorCountryOfControl"": ""Germany"",
  ""OperatorCountryOfDomicileCode"": ""GEU"",
  ""OperatorCountryOfDomicileName"": ""Germany"",
  ""OperatorCountryOfRegistration"": ""Germany"",
  ""RegisteredOwner"": ""Vogemann GmbH"",
  ""RegisteredOwnerCode"": ""5078199"",
  ""RegisteredOwnerCountryOfControl"": ""Germany"",
  ""RegisteredOwnerCountryOfDomicile"": ""Germany"",
  ""RegisteredOwnerCountryOfDomicileCode"": ""GEU"",
  ""RegisteredOwnerCountryOfRegistration"": ""Germany"",
  ""ShipManager"": ""Vogemann GmbH"",
  ""ShipManagerCompanyCode"": ""5078199"",
  ""ShipManagerCountryOfControl"": ""Germany"",
  ""ShipManagerCountryOfDomicileCode"": ""GEU"",
  ""ShipManagerCountryOfDomicileName"": ""Germany"",
  ""ShipManagerCountryOfRegistration"": ""Germany"",
  ""ShipName"": ""YANGFAN BC40K-VR05"",
  ""ShipStatus"": ""Under Construction"",
  ""ShipStatusCode"": ""U"",
  ""Shipbuilder"": ""Yangfan Group Co Ltd"",
  ""ShipbuilderCompanyCode"": ""CHR269080"",
  ""ShipbuilderFullStyle"": ""Yangfan Group Co Ltd - Zhoushan ZJ"",
  ""ShiptypeGroup"": ""Bulk Carrier-Handymax"",
  ""ShiptypeLevel2"": ""Bulk Carriers"",
  ""ShiptypeLevel3"": ""Bulk Dry"",
  ""ShiptypeLevel4"": ""Bulk Carrier"",
  ""ShiptypeLevel5"": ""Bulk Carrier"",
  ""ShiptypeLevel5HullType"": ""Ship Shape Including Multi-Hulls"",
  ""ShiptypeLevel5SubGroup"": ""Dry Bulk Cargo"",
  ""ShiptypeLevel5SubType"": ""Bulk Carrier"",
  ""SisterShipLinks"": ""9941609,9941611,9941623,9941635,9952658"",
  ""SpeedService"": ""0.00"",
  ""TechnicalManager"": ""  Unknown"",
  ""TechnicalManagerCode"": ""9991001"",
  ""YardNumber"": ""BC40K-VR05"",
  ""YearOfBuild"": ""2023"",
  ""ShipbuilderSubContractorShipyardYardHullNo"": null,
  ""TechnicalManagerCountryOfControl"": null,
  ""TechnicalManagerCountryOfDomicile"": null,
  ""TechnicalManagerCountryOfDomicileCode"": null,
  ""TechnicalManagerCountryOfRegistration"": null,
  ""DeliveryDate"": null,
  ""IHSLRorIMOShipNo"": 9952646
}";
            using (var r = new ChoParquetReader(@"ships.parquet")
                .IgnoreField("DataSetVersion1")
                .ParquetOptions(o => o.TreatByteArrayAsString = true)
                .ErrorMode(ChoErrorMode.ThrowAndStop)
                )
            {
                var rec = r.First();
                rec.Print();

                var actual = JsonConvert.SerializeObject(rec, new JsonSerializerSettings()
                {
                    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
                    Formatting = Formatting.Indented,
                });
                Assert.AreEqual(expected, actual);
            }
        }
        [Test]
        public static void MissingFieldValueTest()
        {
            string csv = @"Id,Name
1,
2,Carl
3,Mark";
            string parquetFilePath = "missingfieldvalue.parquet";
            CreateParquetFile(parquetFilePath, csv);

            string expected = @"[
  {
    ""Id"": ""1"",
    ""Name"": null
  },
  {
    ""Id"": ""2"",
    ""Name"": ""Carl""
  },
  {
    ""Id"": ""3"",
    ""Name"": ""Mark""
  }
]";
            using (var r = new ChoParquetReader(parquetFilePath))
            {
                var actual = JsonConvert.SerializeObject(r, Formatting.Indented);
                Assert.AreEqual(expected, actual);
            }
        }
        private static void CreateParquetFile(string parquetFilePath, string csv)
        {
            using (var r = ChoCSVReader.LoadText(csv)
                   .WithFirstLineHeader()
                  )
            {
                using (var w = new ChoParquetWriter(parquetFilePath))
                    w.Write(r);
            }
        }
        static string ReadParquetFile(string parquetOutputFilePath)
        {
            parquetOutputFilePath.Print();
            using (var r = new ChoParquetReader(parquetOutputFilePath))
            {
                var recs = r.ToArray();
                return JsonConvert.SerializeObject(recs, Formatting.Indented);
            }
        }
    }
}
