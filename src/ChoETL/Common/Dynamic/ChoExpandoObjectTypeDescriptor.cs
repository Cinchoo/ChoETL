using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public class ChoExpandoObjectTypeDescriptor : ICustomTypeDescriptor
    {
        private readonly IDictionary<string, object> m_Instance;

        public ChoExpandoObjectTypeDescriptor(dynamic instance)
        {
            m_Instance = instance as IDictionary<string, object>;
        }

        public string GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        public EventDescriptor GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        public string GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        public EventDescriptorCollection GetEvents(Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        public TypeConverter GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }

        public object GetPropertyOwner(PropertyDescriptor pd)
        {
            return m_Instance;
        }

        public AttributeCollection GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this, true);
        }

        public object GetEditor(Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        public PropertyDescriptor GetDefaultProperty()
        {
            return null;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return ((ICustomTypeDescriptor)this).GetProperties(new Attribute[0]);
        }

        public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return new PropertyDescriptorCollection(
                m_Instance.Keys
                          .Select(x => new ExpandoObjectPropertyDescriptor(m_Instance, x))
                          .ToArray<PropertyDescriptor>());
        }

        class ExpandoObjectPropertyDescriptor : PropertyDescriptor
        {
            private readonly IDictionary<string, object> m_Instance;
            private readonly string m_Name;

            public ExpandoObjectPropertyDescriptor(IDictionary<string, object> instance, string name)
                : base(name, null)
            {
                m_Instance = instance;
                m_Name = name;
            }

            public override Type PropertyType
            {
                get { return m_Instance[m_Name].GetType(); }
            }

            public override void SetValue(object component, object value)
            {
                m_Instance[m_Name] = value;
            }

            public override object GetValue(object component)
            {
                return m_Instance[m_Name];
            }

            public override bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public override Type ComponentType
            {
                get { return null; }
            }

            public override bool CanResetValue(object component)
            {
                return false;
            }

            public override void ResetValue(object component)
            {
            }

            public override bool ShouldSerializeValue(object component)
            {
                return false;
            }

            public override string Category
            {
                get { return string.Empty; }
            }

            public override string Description
            {
                get { return string.Empty; }
            }
        }
    }
}
