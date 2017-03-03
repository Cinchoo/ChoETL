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
    public class ChoManifoldFileHeaderConfiguration : ChoFileHeaderConfiguration
    {
        public ChoManifoldFileHeaderConfiguration(CultureInfo culture = null) : base(typeof(object), culture)
        { }
    }
}
