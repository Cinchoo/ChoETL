using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoCSVFileHeaderConfiguration : ChoFileHeaderConfiguration
    {
        public readonly static ChoCSVFileHeaderConfiguration Default = new ChoCSVFileHeaderConfiguration();

        public ChoCSVFileHeaderConfiguration(Type recordType = null, CultureInfo culture = null) : base(recordType, culture)
        { }
    }
}
