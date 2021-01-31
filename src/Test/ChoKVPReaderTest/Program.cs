using ChoETL;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoKVPReaderTest
{
    [ChoKVPRecordObject(RecordStart = "BEGIN:VEVENT")]
    public class CalendarEvent : IChoNotifyKVPRecordRead
    {
        [ChoKVPRecordField(FieldName = "DTSTART;VALUE=DATE")]
        [ChoTypeConverter(typeof(ChoDateTimeConverter), Parameters = "yyyyMMdd")]
        public DateTime EventDate
        {
            get;
            set;
        }
        [ChoKVPRecordField(FieldName = "SUMMARY")]
        public string Holiday
        {
            get;
            set;
        }
        [ChoKVPRecordField(FieldName = "EMAIL")]
        public string Email
        {
            get;
            set;
        }

        public KeyValuePair<string, string>? ToKVP(string recText)
        {
            if (recText.StartsWith("EMAIL;"))
                return new KeyValuePair<string, string>("EMAIL", recText.RightOf("EMAIL;"));
            return null;
        }
    }

    [TestFixture]
    [SetCulture("en-US")] // TODO: Check if correct culture is used
    class Program
    {
        static void Main(string[] args)
        {
            QuickTest();
        }

        public class Event
        {
            public string ORGANIZER { get; set; }
            public string DTSTART { get; set; }
            public string DTEND { get; set; }
            public string LOCATION { get; set; }
            public string DESCRIPTION { get; set; }
        }

        [SetUp]
        public void Setup()
        {
            Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
            // Needs to be reset because of some tests changes these settings
            ChoTypeConverterFormatSpec.Instance.Reset();
            ChoXmlSettings.Reset();
        }

        //[Test]
        public static void QuickTest()
        {
            using (var r = new ChoKVPReader(FileNameSampleICS))
            {
                r.Configuration.RecordStart = "BEGIN:VEVENT";
                r.Configuration.RecordEnd = "END:VEVENT";
                r.Configuration.IgnoreEmptyLine = true;
                r.Configuration.Comment = ";";
                //foreach (dynamic item in r)
                //{
                //    Console.WriteLine(item.SUMMARY);
                //}

                Assert.Throws<ChoETL.ChoRecordConfigurationException>(() => r.Read());
            }

        }

        //[Test]
        public static void LoadINIFileTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{
                    { "102", "Προβολή" } ,
                    {"103", "Επιλογές" } ,
                    {"104", "Απεγκαταστάτης" } ,
                    {"105", "Εργαλεία" } ,
                    {"106", "Λειτουργία Ανίχνευσης" } ,
                    {"107", "Κατάλογος" } ,
                    {"108", "Εικονίδια" } ,
                    {"109","Λεπτομέρειες" } ,
                    {"110","Απεγκατάσταση" } ,
                    {"111","Απομάκρυνση Καταχώρησης" } ,
                    {"112","Ανανέωση" } ,
                    {"113","Είστε βέβαιοι πως θέλετε να απομακρύνετε της επιλεγμένη καταχώρηση;" } ,
                    {"114","Είστε βέβαιοι πως θέλετε να απεγκαταστήσετε την επιλεγμένη εφαρμογή;" } ,
                    {"115","Ενημέρωση" } ,
                    {"116","Βοήθεια" },
                    {"117","Βοήθεια Τρέχοντος Εργαλείου..." },
                    {"118","Αρχική σελίδα..." } ,
                    {"119","Περί..." } ,
                    {"120","Δεν είστε Διαχειριστής!" },
                    {"121","Είστε βέβαιοι ότι θέλετε να αφαιρέσετε το επιλεγμένο στοιχείο συστήματος?\\nSTOP, εκτός και είστε βέβαιοι τι κάνετε!" } ,
                    {"122", @"Το Revo Uninstaller σας παρουσιάζει όλα τα εγκατεστημένα προγράμματα και συστατικά για όλους τους χρήστες. Στον τύπο άποψης ""Λεπτομέρειες"", ή από τον κατάλογο επιλογών, μπορείτε να έχετε πρόσβαση σε πρόσθετες πληροφορίες (συνδέσεις και ιδιότητες για τις εγκαταστάσεις). Ένα βασικό χαρακτηριστικό γνώρισμα του Revo Uninstaller είναι η ""Λειτουργία Ανίχνευσης"". Αυτή η κατάσταση σας δίνει την ευελιξία να απεγκαταστήσετε, σταματήσετε, διαγράψετε ή να θέσετε εκτός λειτουργίας, προγράμματα από την αυτόματη εκκίνηση, με ένα κλικ." } ,
                    {"123","Αναζήτηση:" } ,
                    {"124","Εύρεση:" } ,
                    {"125","Είστε βέβαιοι ότι θέλετε να αφαιρέσετε το επιλεγμένο στοιχείο συστήματος από το Μητρώο?\\nΤο στοιχείο πιθανώς είναι απαραίτητο στο λειτουργικό!" } ,
                    {"126","δεν πρόκειται να εκκινήσει αυτόματα ξανά!" } ,
                    {"127","είναι ρυθμισμένο σε αυτόματη εκκίνηση" } ,
                    {"128","%s είναι εγκατεστημένο στην ίδια θέση με %s!\\nΠρόκειται να βρεθούν κατάλοιπα και από τις δύο εφαρμογές. Επιλέξτε με προσοχή ποιά θέλετε να απομακρύνετε!" } ,
                    {"129","Εξαναγκασμένη Απεγκατάσταση" } }
            };
            object[] actual = null;

            Assert.Warn(@"File C:\Program Files (x86)\VS Revo Group\Revo Uninstaller\lang\hellenic.ini not found, instead downloaded file from https://revouninstaller.net/revo_uninstaller_language_files_download.html and added to project. Please check.");

            using (var r = new ChoKVPReader(FileNameHellenicINI).WithDelimiter("="))
            {
                r.Configuration.RecordStart = "[Uninstaller Toolbar]";
                r.Configuration.RecordEnd = "[*";
                r.Configuration.IgnoreEmptyLine = true;
                r.Configuration.Comment = ";";

                actual = r.ToArray();
            }

            CollectionAssert.AreEqual(expected, actual.ToList());
        }

        //[Test]
        public static void LoadTest()
        {
            Assert.Warn(@"Original file ""C:\Users\raj\Documents\GitHub\ChoETL\src\Test\ChoKVPReaderTest\Maldives Holidays Calendar.ics"" not found. Used Copy (2) instead. Please check.");

            List<object> expected = new List<object>
            {
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20091111" },
{"DTEND;VALUE=DATE" , "20091112" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "pabnn448bckv1l1dn9s2jh7csg@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105319Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105319Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Republic Day" },
{"EMAIL;nraj38@yahoo.com,xyz@hotmail.com" , null },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20081207" },
{"DTEND;VALUE=DATE" , "20081208" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "50rdeu4ne5qehlkjr1ge7nchis@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101310Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101631Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Hajj Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20091218" },
{"DTEND;VALUE=DATE" , "20091219" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "8ci9gcgn729mpov445jqkqvcn0@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105432Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105432Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Islamic New Year (H1431)" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20090822" },
{"DTEND;VALUE=DATE" , "20090823" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "prcqegto4pbkkaqrc9grd8n8fo@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20090227T132445Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20090227T132445Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "1st Ramazan" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20090726" },
{"DTEND;VALUE=DATE" , "20090727" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "kdtt6i46hmgu2f4c8aeddkfstc@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105215Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20090227T133815Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Independence Day" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20081001" },
{"DTEND;VALUE=DATE" , "20081002" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "nlg9o78q4c3kf6l5c7t7ijjsuo@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101157Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101636Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Eid Al-Fithr" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20071221" },
{"DTEND;VALUE=DATE" , "20071222" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "2nahpdn9eondilb7rfrcnctha0@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071117T130834Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071215T112725Z" },
{"LOCATION" , null },
{"SEQUENCE" , "1" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Eid-al-Adha 3" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20090727" },
{"DTEND;VALUE=DATE" , "20090728" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "nq02qna0r9n9lp992qitjpc240@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105245Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105245Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "on the occasion of Independence Day)" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20081103" },
{"DTEND;VALUE=DATE" , "20081104" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "77jaig6ter5n98q5auji9i26ck@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101230Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101634Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Victory Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20071218" },
{"DTEND;VALUE=DATE" , "20071219" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "t68gtb844vcccqpvnbarb0tpi0@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071117T130833Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071215T112712Z" },
{"LOCATION" , null },
{"SEQUENCE" , "2" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Hajj Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20090226" },
{"DTEND;VALUE=DATE" , "20090227" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "m1hk0nofg1nt1dmoeqiebek1vg@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105041Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105041Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "National Day" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20071220" },
{"DTEND;VALUE=DATE" , "20071221" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "aekj9malki3d96l36ovqckppdc@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071117T130837Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071215T112715Z" },
{"LOCATION" , null },
{"SEQUENCE" , "1" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Eid-al-Adha 2" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20091126" },
{"DTEND;VALUE=DATE" , "20091127" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "lrbj1v8aar23vsmodlth3q8s8k@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105330Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105330Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Hajj Day" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20081002" },
{"DTEND;VALUE=DATE" , "20081004" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "opk4n5l8prsouddb06h6n4kqrs@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101217Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101635Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Holiday on the Ocassion of Eid Al-Fithr" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20071222" },
{"DTEND;VALUE=DATE" , "20071223" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "qpmvi6hlrri5e28b0jt96187bs@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071117T130835Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071215T112727Z" },
{"LOCATION" , null },
{"SEQUENCE" , "1" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Eid-al-Adha 4" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20080110" },
{"DTEND;VALUE=DATE" , "20080111" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "3cbnfpqurovt7nfrndjqgjrbb8@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T100932Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101628Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Islamic New Year" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20071103" },
{"DTEND;VALUE=DATE" , "20071104" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "7bv5g7d14olf0j5prs5l81enls@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071117T130835Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071117T130835Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Victory Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20091127" },
{"DTEND;VALUE=DATE" , "20091128" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "i862rgcfic88msk01r15ks3im8@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105340Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105340Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Eid-ul Al'haa" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20080309" },
{"DTEND;VALUE=DATE" , "20080310" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "pc2vv3eagdk2t3t6cmd4jiqpfs@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T100947Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101641Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "National Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20071219" },
{"DTEND;VALUE=DATE" , "20071220" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "t2jjtm18l58cr20ct7ojfmcf7o@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071117T130837Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071215T112712Z" },
{"LOCATION" , null },
{"SEQUENCE" , "1" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Eid-al-Adha" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20080727" },
{"DTEND;VALUE=DATE" , "20080728" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "p0iutbt9kebsgmiia8p18ko50c@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101126Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101638Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Holiday on the Ocassion of Independence Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20090309" },
{"DTEND;VALUE=DATE" , "20090310" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "ub60f66hpfbdlharpcgr0m92ig@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105108Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105108Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Prophet Muhammad's Birthday" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20100101" },
{"DTEND;VALUE=DATE" , "20100102" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "lk713pgk2g1k9bs91khei83q80@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105440Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105440Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "New Year" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20081229" },
{"DTEND;VALUE=DATE" , "20081230" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "eksspdae8c1bkoli14i2qj761c@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101431Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101642Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Islamic New Year" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20090920" },
{"DTEND;VALUE=DATE" , "20090921" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "8gt46k80he6drnvpaddj5j4a0o@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20090227T132530Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20090227T132714Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Eid-ul Fitr" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20080726" },
{"DTEND;VALUE=DATE" , "20080727" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "0k0oaqs4b9tunkcv1ei30klb70@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101045Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101638Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Independence Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20090329" },
{"DTEND;VALUE=DATE" , "20090330" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "r0e80tm9gh440n30k3gflqc7pk@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105128Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105128Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Maldives' Embracement to Islam" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20080101" },
{"DTEND;VALUE=DATE" , "20080102" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "rr8qnbrb4hl6vp6icthqe75t28@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071117T130838Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071117T130838Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "New Year's Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20081209" },
{"DTEND;VALUE=DATE" , "20081212" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "dic5ricfnofpr2tjlogicaq7co@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101401Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101630Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Holiday on the Ocassion of Eid Al-haa (H1429)" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20080408" },
{"DTEND;VALUE=DATE" , "20080409" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "acqqg08d4ui3aaa2rr4f67ibe8@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101030Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101639Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Maldives Embracement to Islam" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20071111" },
{"DTEND;VALUE=DATE" , "20071112" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "0sgdveua8e48nlk56ikgp7n94o@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071117T130834Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071117T130834Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Republic Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20080901" },
{"DTEND;VALUE=DATE" , "20080902" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "jsblbnk380vsrqihkvomt2debg@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101141Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101637Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "First of Ramazan" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20090101" },
{"DTEND;VALUE=DATE" , "20090102" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "3cguvtlvilnh4n2ppa2qasmkok@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101421Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101629Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "New Year Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20091103" },
{"DTEND;VALUE=DATE" , "20091104" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "qm0dj8t0m2d482s173k8f89na0@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105312Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105312Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Victory Day" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20080320" },
{"DTEND;VALUE=DATE" , "20080321" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "aepfkc7rnlabo6rrckvv1agn6s@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101006Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101640Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Prophet Mohamed's Birthday" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20081208" },
{"DTEND;VALUE=DATE" , "20081209" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "88rac67g9d2ra7lqiqk7aqdmtc@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101332Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101631Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Eid Al-haa (H1429)" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20081111" },
{"DTEND;VALUE=DATE" , "20081112" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "11peg9qbjvrn56vks2banc7m68@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20071119T101241Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20071119T101632Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "Republic Day" },
{"TRANSP" , "OPAQUE" },
{"CATEGORIES:http" , "//schemas.google.com/g/2005#event" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20091128" },
{"DTEND;VALUE=DATE" , "20091201" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "2r05dhksq28bpjvhisqe5mv84s@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20081209T105356Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20081209T105356Z" },
{"LOCATION" , null },
{"SEQUENCE" , "0" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "(on the occasion of Eid-ul Al'haa)" },
{"TRANSP" , "TRANSPARENT" }
},
new ChoDynamicObject {
{"DTSTART;VALUE=DATE" , "20090921" },
{"DTEND;VALUE=DATE" , "20090923" },
{"DTSTAMP" , "20090301T061852Z" },
{"UID" , "jcd3ohhehue6tfl1uoicao2ks4@google.com" },
{"CLASS" , "PRIVATE" },
{"CREATED" , "20090227T132543Z" },
{"DESCRIPTION" , null },
{"LAST-MODIFIED" , "20090227T133830Z" },
{"LOCATION" , null },
{"SEQUENCE" , "4" },
{"STATUS" , "CONFIRMED" },
{"SUMMARY" , "On the occassion of Eid-ul Fitr" },
{"TRANSP" , "TRANSPARENT" }
}
            };
            List<object> actual = new List<object>();

            using (var r = new ChoKVPReader(FileNameMaldivesHolidaysCalendarCopy2ICS).NotifyAfter(25))
            {
                r.Configuration.RecordStart = "BEGIN:VEVENT";
                r.Configuration.RecordEnd = "END:VEVENT";
                foreach (var item in r)
                {
                    actual.Add(item);
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }
        public static string FileNameSampleICS => "Sample.ics";
        public static string FileNameMaldivesHolidaysCalendarCopy2ICS => "Maldives Holidays Calendar - Copy (2).ics";
        public static string FileNameConvertToCSVTestActualCSV => "ConvertToCSVTestActual.csv";
        public static string FileNameConvertToCSVTestExpectedCSV => "ConvertToCSVTestExpected.csv";
        public static string FileNameConvertToCSVWithHeaderTestActualCSV => "ConvertToCSVWithHeaderTestActual.csv";
        public static string FileNameConvertToCSVWithHeaderTestExpectedCSV => "ConvertToCSVWithHeaderTestExpected.csv";
        public static string FileNameLoadPOCOTestActualCSV => "LoadPOCOTestActual.csv";
        public static string FileNameLoadPOCOTestExpectedCSV => "LoadPOCOTestExpected.csv";
        
        public static string FileNameHellenicINI => "hellenic.ini";

        //[Test]
        public static void ConvertToCSVTest()
        {
            Assert.Warn(@"Original file ""C: \Users\raj\Documents\GitHub\ChoETL\src\Test\ChoKVPReaderTest\Maldives Holidays Calendar.ics"" not found. Used Copy (2) instead. Please check.");

            using (var r = new ChoKVPReader(FileNameMaldivesHolidaysCalendarCopy2ICS).NotifyAfter(25))
            {
                r.Configuration.RecordStart = "BEGIN:VEVENT";
                r.Configuration.RecordEnd = "END:VEVENT";
                using (var c = new ChoCSVWriter(FileNameConvertToCSVTestActualCSV))
                {
                    foreach (var item in r)
                    {
                        c.Write(item);
                    }
                }
            }

            FileAssert.AreEqual(FileNameConvertToCSVTestExpectedCSV, FileNameConvertToCSVTestActualCSV);
        }

        //[Test]
        public static void ConvertToCSVWithHeaderTest()
        {
            Assert.Warn(@"Original File ""C: \Users\raj\Documents\GitHub\ChoETL\src\Test\ChoKVPReaderTest\Maldives Holidays Calendar.ics"" not found. Used Copy (2) instead. Please check.");

            using (var r = new ChoKVPReader(FileNameMaldivesHolidaysCalendarCopy2ICS).NotifyAfter(25))
            {
                r.Configuration.RecordStart = "BEGIN:VEVENT";
                r.Configuration.RecordEnd = "END:VEVENT";
                using (var c = new ChoCSVWriter(FileNameConvertToCSVWithHeaderTestActualCSV))
                {
                    foreach (var item in r)
                    {
                        c.Write(item);
                    }
                }
            }

            FileAssert.AreEqual(FileNameConvertToCSVWithHeaderTestExpectedCSV, FileNameConvertToCSVWithHeaderTestActualCSV);
        }

        //[Test]
        public static void LoadPOCOTest()
        {
            Assert.Warn(@"Original file ""C:\Users\raj\Documents\GitHub\ChoETL\src\Test\ChoKVPReaderTest\Maldives Holidays Calendar.ics"" not found. Used Copy (2) instead. Please check.");

            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Verbose;

            using (var r = new ChoKVPReader<CalendarEvent>(FileNameMaldivesHolidaysCalendarCopy2ICS))
            {
                using (var c = new ChoCSVWriter<CalendarEvent>(FileNameLoadPOCOTestActualCSV))
                {
                    foreach (var item in r)
                    {
                        c.Write(item);
                    }
                }
            }

            FileAssert.AreEqual(FileNameLoadPOCOTestExpectedCSV, FileNameLoadPOCOTestActualCSV);
            // TODO: Move line 426 to 432 in file ChoKVPRecordReader should solve that problem
        }

        //[Test]
        public static void QuickDynamicTest()
        {
            List<object> expected = new List<object>
            {
                new ChoDynamicObject{{"UID", @"uid1@example.com
 raj@example.com" },{"DTSTAMP", "19970714T170000Z" } }
            };
            List<object> actual = new List<object>();

            ChoKVPRecordConfiguration config = new ChoKVPRecordConfiguration();
            ChoKVPRecordFieldConfiguration idConfig = new ChoKVPRecordFieldConfiguration("UID");
            config.KVPRecordFieldConfigurations.Add(idConfig);
            config.KVPRecordFieldConfigurations.Add(new ChoKVPRecordFieldConfiguration("DTSTAMP"));
            //config.KVPRecordFieldConfigurations.Add(new ChoKVPRecordFieldConfiguration("DTSTART"));
            //config.KVPRecordFieldConfigurations.Add(new ChoKVPRecordFieldConfiguration("DTENDX"));
            //config.KVPRecordFieldConfigurations.Add(new ChoKVPRecordFieldConfiguration("DTEND"));

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoKVPReader(reader, config).ColumnCountStrict().ColumnOrderStrict())
            {
                writer.WriteLine("BEGIN:VCALENDAR");
                writer.WriteLine("VERSION:2.0");
                writer.WriteLine("PRODID:-//hacksw/handcal//NONSGML v1.0//EN");
                writer.WriteLine("[BEGIN:VEVENT");
                writer.WriteLine("UID:uid1@example.com");
                writer.WriteLine(" raj@example.com");
                writer.WriteLine("DTSTAMP:19970714T170000Z");
                //writer.WriteLine("ORGANIZER;CN=John Doe:MAILTO:john.doe@example.com");
                //writer.WriteLine("DTSTART:19970714T170000Z");
                //writer.WriteLine("DTEND:19970715T035959Z");
                //writer.WriteLine("SUMMARY:Bastille Day Party");
                //writer.WriteLine("END:VEVENT]");
                //writer.WriteLine("[BEGIN:VEVENT");
                //writer.WriteLine("UID:uid1@example.com");
                //writer.WriteLine("DTSTAMP:19970714T170000Z");
                //writer.WriteLine("ORGANIZER;CN=John Doe:MAILTO:john.doe@example.com");
                //writer.WriteLine("DTSTART:19970714T170000Z");
                //writer.WriteLine("DTEND:19970715T035959Z");
                //writer.WriteLine("SUMMARY:Bastille Day Party");
                writer.WriteLine("END:VEVENT]");
                writer.WriteLine("END:VCALENDAR");

                writer.Flush();
                stream.Position = 0;

                //parser.Configuration.RecordStart = "BEGIN:VCALENDAR";
                //parser.Configuration.RecordEnd = "END:VEVENT";

                //parser.Configuration.RecordStart = "[BEGIN:VEVENT";
                //parser.Configuration.RecordEnd = "END:VEVENT";

                parser.Configuration.RecordStart = "[BEGIN:VEVENT";
                parser.Configuration.RecordEnd = "END:VEVENT]";

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    actual.Add(rec);
                    Console.WriteLine(rec.ToStringEx());
                }
            }

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
