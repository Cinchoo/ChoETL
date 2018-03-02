using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [DataContract]
    public class ChoCSVRecordTypeConfiguration : ChoRecordTypeConfiguration
    {
        [DataMember]
        public int Position
        {
            get;
            set;
        }
    }
}
