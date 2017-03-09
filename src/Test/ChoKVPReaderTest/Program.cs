using ChoETL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoKVPReaderTest
{
    class Program
    {
        static void Main(string[] args)
        {
            QuickDynamicTest();
        }

        static void QuickDynamicTest()
        {
            ChoKVPRecordConfiguration config = new ChoKVPRecordConfiguration();
            ChoKVPRecordFieldConfiguration idConfig = new ChoKVPRecordFieldConfiguration("UID");
            config.KVPRecordFieldConfigurations.Add(idConfig);
            config.KVPRecordFieldConfigurations.Add(new ChoKVPRecordFieldConfiguration("DTSTAMP"));
            config.KVPRecordFieldConfigurations.Add(new ChoKVPRecordFieldConfiguration("DTSTART"));

            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var parser = new ChoKVPReader(reader, config))
            {
                writer.WriteLine("BEGIN:VCALENDAR");
                writer.WriteLine("VERSION:2.0");
                writer.WriteLine("PRODID:-//hacksw/handcal//NONSGML v1.0//EN");
                writer.WriteLine("BEGIN:VEVENT");
                writer.WriteLine("UID:uid1@example.com");
                writer.WriteLine(" raj@example.com");
                writer.WriteLine("DTSTAMP:19970714T170000Z");
                writer.WriteLine("ORGANIZER;CN=John Doe:MAILTO:john.doe@example.com");
                writer.WriteLine("DTSTART:19970714T170000Z");
                writer.WriteLine("DTEND:19970715T035959Z");
                writer.WriteLine("SUMMARY:Bastille Day Party");
                writer.WriteLine("END:VEVENT");
                writer.WriteLine("BEGIN:VEVENT");
                writer.WriteLine("UID:uid1@example.com");
                writer.WriteLine("DTSTAMP:19970714T170000Z");
                writer.WriteLine("ORGANIZER;CN=John Doe:MAILTO:john.doe@example.com");
                writer.WriteLine("DTSTART:19970714T170000Z");
                writer.WriteLine("DTEND:19970715T035959Z");
                writer.WriteLine("SUMMARY:Bastille Day Party");
                writer.WriteLine("END:VEVENT");
                writer.WriteLine("END:VCALENDAR");

                writer.Flush();
                stream.Position = 0;

                parser.Configuration.RecordStart = "BEGIN:VEVENT";
                parser.Configuration.RecordEnd = "END:VEVENT";

                object rec;
                while ((rec = parser.Read()) != null)
                {
                    Console.WriteLine(rec.ToStringEx());
                }
            }
        }
    }
}
