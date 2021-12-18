using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true)]
    public class ChoKnownTypeAttribute : ChoAllowMultipleAttribute
    {
        public Type Type
        {
            get;
            private set;
        }

        public string Value
        {
            get;
            private set;
        }
        public ChoKnownTypeAttribute(Type type, string value = null)
        {
            Type = type;
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Property)]
    public class ChoKnownTypeDiscriminatorAttribute : Attribute
    {
        public string Discriminator
        {
            get;
            private set;
        }

        public ChoKnownTypeDiscriminatorAttribute(string discriminator)
        {
            Discriminator = discriminator;
        }
    }
}
