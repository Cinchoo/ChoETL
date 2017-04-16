using ChoETL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoKVPReaderTest
{
    [ChoKVPRecordObject(RecordStart = "BEGIN:VEVENT")]
    public class CalendarEvent
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
    }

    class Program
    {
        static void Main(string[] args)
        {
            LoadPOCOTest();
        }

        static void LoadINIFileTest()
        {
            using (var r = new ChoKVPReader(@"C:\Program Files (x86)\VS Revo Group\Revo Uninstaller\lang\hellenic.ini").WithDelimiter("="))
            {
                r.Configuration.RecordStart = "[Uninstaller Toolbar]";
                r.Configuration.RecordEnd = "[*";
                r.Configuration.IgnoreEmptyLine = true;
                r.Configuration.Comment = ";";
                foreach (dynamic item in r.ToArray())
                {
                    Console.WriteLine(item._102);
                    Console.WriteLine(((object)item).ToStringEx());
                }
            }
        }

        static void LoadTest()
        {
            using (var r = new ChoKVPReader(@"C:\Users\raj\Documents\GitHub\ChoETL\src\Test\ChoKVPReaderTest\Maldives Holidays Calendar.ics").NotifyAfter(25))
            {
                r.Configuration.RecordStart = "BEGIN:VEVENT";
                r.Configuration.RecordEnd = "END:VEVENT";
                foreach (var item in r)
                {
                    Console.WriteLine(item.ToStringEx());
                }
            }
        }

        static void ConvertToCSVTest()
        {
            using (var r = new ChoKVPReader(@"C:\Users\raj\Documents\GitHub\ChoETL\src\Test\ChoKVPReaderTest\Maldives Holidays Calendar.ics").NotifyAfter(25))
            {
                r.Configuration.RecordStart = "BEGIN:VEVENT";
                r.Configuration.RecordEnd = "END:VEVENT";
                using (var c = new ChoCSVWriter(Console.Out))
                {
                    foreach (var item in r)
                    {
                        c.Write(item);
                    }
                }
            }
        }

        static void LoadPOCOTest()
        {
            ChoETLFrxBootstrap.TraceLevel = System.Diagnostics.TraceLevel.Off;

            using (var r = new ChoKVPReader<CalendarEvent>(@"C:\Users\raj\Documents\GitHub\ChoETL\src\Test\ChoKVPReaderTest\Maldives Holidays Calendar.ics"))
            {
                using (var c = new ChoCSVWriter<CalendarEvent>(Console.Out))
                {
                    foreach (var item in r)
                    {
                        c.Write(item);
                    }
                }
            }
        }

        static void QuickDynamicTest()
        {
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
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }
    }
}
