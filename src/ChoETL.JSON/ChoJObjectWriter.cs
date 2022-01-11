using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoJObjectWriter : IDisposable
    {
        private bool _isDisposed = false;
        private readonly JsonWriter _writer;
        private readonly ChoJObjectLoadOptions? _options;
        private Formatting _formatting;
        private readonly Lazy<bool> _writeStartObject;
        private readonly bool _closeWriterOnDispose = false;
        private readonly List<JsonConverter> _converters = new List<JsonConverter>();

        public List<JsonConverter> Converters
        {
            get { return _converters; }
        }
        public Action<JsonReader> FormattedJsonReaderSetup
        {
            get;
            set;
        }
        public Formatting Formatting 
        { 
            get {  return _formatting; } 
            set
            {
                _formatting = value;
                _writer.Formatting = value;
            }
        }

        public ChoJObjectWriter(StringBuilder sb, ChoJObjectLoadOptions? options = null)
            : this(new StringWriter(sb), options)
        {
            _closeWriterOnDispose = true;
        }

        public ChoJObjectWriter(string filePath, ChoJObjectLoadOptions? options = null)
            : this(new StreamWriter(filePath), options)
        {
            _closeWriterOnDispose = true;
        }

        public ChoJObjectWriter(TextWriter writer, ChoJObjectLoadOptions? options = null)
            : this(new JsonTextWriter(writer), options)
        {
        }

        public ChoJObjectWriter(string propName, ChoJObjectWriter writer)
        {
            ChoGuard.ArgumentNotNull(writer, nameof(writer));
            ChoGuard.ArgumentNotNull(propName, nameof(propName));

            _writer = writer._writer;
            _options = writer._options;
            _formatting = writer.Formatting;

            _writeStartObject = new Lazy<bool>(() =>
            {
                _writer.WriteStartObject();
                return true;
            });

            var _ = writer._writeStartObject.Value;
            _writer.WritePropertyName(propName);
            var x = _writeStartObject.Value;
        }

        public ChoJObjectWriter(JsonWriter writer, ChoJObjectLoadOptions? options = null)
        {
            ChoGuard.ArgumentNotNull(writer, nameof(writer));

            _writer = writer;
            _options = options;
            _formatting = writer.Formatting;
            _writeStartObject = new Lazy<bool>(() =>
            {
                _writer.WriteStartObject();
                return true;
            });
        }

        public void WriteFormattedPropertyValue(string propName, string value)
        {
            ChoGuard.ArgumentNotNull(propName, nameof(propName));

            _writer.WritePropertyName(propName);
            if (value == null)
                _writer.WriteNull();
            else
                _writer.WriteFormattedRawValue(JsonConvert.SerializeObject(value), FormattedJsonReaderSetup);
        }

        public void WriteProperty(string propName, object value)
        {
            ChoGuard.ArgumentNotNull(propName, nameof(propName));

            CheckDisposed();
            var _ = _writeStartObject.Value;

            _writer.WritePropertyName(propName);
            WriteValue(value, true);
        }

        public void WriteValue(object value)
        {
            WriteValue(value, false);
        }

        private void WriteValue(object value, bool isPropValue)
        {
            CheckDisposed();
            var _ = _writeStartObject.Value;

            Type objType = value != null ? value.GetType() : typeof(object);

            var converter = GetJsonConverter(objType);
            if (converter != null)
            {
                var json = JsonConvert.SerializeObject(value, _formatting, converter);
                _writer.WriteFormattedRawValue(json, FormattedJsonReaderSetup);
                return;
            }

            if (value is IDictionary<string, object>)
            {
                if (isPropValue)
                    WriteDictionaryAsObject(value as IDictionary<string, object>);
                else
                    WriteDictionary(value as IDictionary<string, object>);
            }
            else if (value is JToken)
            {
                ((JToken)value).WriteTo(_writer);
            }
            else if (value is JsonReader)
            {
                _writer.WriteReader(value as JsonReader, _options);
            }
            else if (value is IList)
            {
                _writer.WriteStartArray();
                foreach (var item in (IList)value)
                    WriteValue(item, true);
                _writer.WriteEndArray();
            }
            else if (objType.IsAnonymousType())
            {
                WriteDictionary(value.ToSimpleDictionary());
            }
            else
            {
                _writer.WriteValue(value);
            }
        }

        private JsonConverter GetJsonConverter(Type objType)
        {
            if (objType == null || objType == typeof(object))
                return null;

            return null;
        }

        private void WriteDictionaryAsObject(IDictionary<string, object> dict)
        {
            _writer.WriteStartObject();
            WriteDictionary(dict);
            _writer.WriteEndObject();
        }

        private void WriteDictionary(IDictionary<string, object> dict)
        {
            foreach (var kvp in dict)
                WriteProperty(kvp.Key, kvp.Value);
        }

        public void Dispose()
        {
            Dispose(false);
        }

        protected void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        protected virtual void Dispose(bool finalize)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            if (_writeStartObject.IsValueCreated)
                _writer.WriteEndObject();
            if (_closeWriterOnDispose)
                _writer.Close();

            if (!finalize)
                GC.SuppressFinalize(this);
        }

        ~ChoJObjectWriter()
        {
            try
            {
                Dispose(true);
            }
            catch { }
        }
    }
}
