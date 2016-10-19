using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public interface IChoPropertyReplacer
    {
        IEnumerable<KeyValuePair<string, string>> AvailablePropeties
        {
            get;
        }
    }

    public interface IChoKeyValuePropertyReplacer : IChoPropertyReplacer
    {
        bool ContainsProperty(string propertyName);
        string ReplaceProperty(string propertyName, string format);
        string GetPropertyDescription(string propertyName);
    }

    public interface IChoCustomPropertyReplacer : IChoPropertyReplacer
    {
        bool Format(ref string msg);
    }

    public class ChoUnknownProperyEventArgs : EventArgs
    {
        #region Instance Data Members

        public readonly string PropertyName;
        public readonly string Format;
        public string PropertyValue;
        public bool Resolved;

        #endregion Instance Data Members

        public ChoUnknownProperyEventArgs(string propertyName, string format)
        {
            PropertyName = propertyName;
            Format = format;
        }
    }
    public class ChoPropertyReplacer
    {
        public static readonly ChoPropertyReplacer Default = new ChoPropertyReplacer();

        public readonly List<IChoPropertyReplacer> Items = new List<IChoPropertyReplacer>();
        public event EventHandler<ChoUnknownProperyEventArgs> PropertyResolve;

        public ChoPropertyReplacer()
        {
            Items.Add(ChoEnvironmentVariablePropertyReplacer.Instance);
            Items.Add(ChoGlobalDictionaryPropertyReplacer.Instance);
        }

        public bool RaisePropertyReolve(string propertyName, string format, out string value)
        {
            value = null;
            var e = new ChoUnknownProperyEventArgs(propertyName, format);
            EventHandler<ChoUnknownProperyEventArgs> propertyResolve = PropertyResolve;
            if (propertyResolve != null)
            {
                propertyResolve(null, e);
            }
            value = e.PropertyValue;
            return e.Resolved;
        }
    }

}
