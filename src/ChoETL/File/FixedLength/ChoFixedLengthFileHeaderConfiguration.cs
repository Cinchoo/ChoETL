using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoFixedLengthFileHeaderConfiguration : ChoFileHeaderConfiguration
    {
        public ChoFixedLengthFileHeaderConfiguration(Type recordType = null, CultureInfo culture = null) : base(recordType, culture)
        { }
    }
}
