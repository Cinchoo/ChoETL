using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoJSONObjects
    {
        public static readonly JObject EmptyJObject = new JObject();
        public static readonly JValue UndefinedValue = JValue.CreateUndefined();
        public static readonly JValue NullValue = JValue.CreateNull();
        public static readonly JArray EmptyJArray = new JArray();
    }
}
