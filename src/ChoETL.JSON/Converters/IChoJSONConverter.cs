using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoJSONConverter
    {
        JsonSerializer Serializer { get; set; }

        dynamic Context { get; set; }
    }
}
