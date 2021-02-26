using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;

namespace ChoETL
{
#if NETSTANDARD2_0

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class MetadataTypeAttribute : Attribute
    {
        private Type _metadataClassType;

        public Type MetadataClassType
        {
            get
            {
                if (_metadataClassType == null)
                {
                    throw new InvalidOperationException("Type cannot be null.");
                }

                return _metadataClassType;
            }
        }

        public MetadataTypeAttribute(Type metadataClassType)
        {
            _metadataClassType = metadataClassType;
        }

    }

    public class AssociatedMetadataTypeTypeDescriptionProvider : TypeDescriptionProvider
    {
        private Type _associatedMetadataType;
        public AssociatedMetadataTypeTypeDescriptionProvider(Type type)
            : base(TypeDescriptor.GetProvider(type))
        {
        }

        public AssociatedMetadataTypeTypeDescriptionProvider(Type type, Type associatedMetadataType)
            : this(type)
        {
            _associatedMetadataType = associatedMetadataType ?? throw new ArgumentNullException("associatedMetadataType");
        }

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance)
        {
            ICustomTypeDescriptor baseDescriptor = base.GetTypeDescriptor(objectType, instance);
            return new AssociatedMetadataTypeTypeDescriptor(baseDescriptor, objectType, _associatedMetadataType);
        }
    }

    internal class AssociatedMetadataTypeTypeDescriptor : CustomTypeDescriptor
    {
        private Type AssociatedMetadataType
        {
            get;
            set;
        }

        private bool IsSelfAssociated
        {
            get;
            set;
        }

        public AssociatedMetadataTypeTypeDescriptor(ICustomTypeDescriptor parent, Type type, Type associatedMetadataType)
            : base(parent)
        {
            AssociatedMetadataType = associatedMetadataType ?? TypeDescriptorCache.GetAssociatedMetadataType(type);
            IsSelfAssociated = (type == AssociatedMetadataType);
            if (AssociatedMetadataType != null)
            {
                TypeDescriptorCache.ValidateMetadataType(type, AssociatedMetadataType);
            }
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            return GetPropertiesWithMetadata(base.GetProperties(attributes));
        }

        public override PropertyDescriptorCollection GetProperties()
        {
            return GetPropertiesWithMetadata(base.GetProperties());
        }

        private PropertyDescriptorCollection GetPropertiesWithMetadata(PropertyDescriptorCollection originalCollection)
        {
            if (AssociatedMetadataType == null)
            {
                return originalCollection;
            }

            bool customDescriptorsCreated = false;
            List<PropertyDescriptor> tempPropertyDescriptors = new List<PropertyDescriptor>();
            foreach (PropertyDescriptor propDescriptor in originalCollection)
            {
                Attribute[] newMetadata = TypeDescriptorCache.GetAssociatedMetadata(AssociatedMetadataType, propDescriptor.Name);
                PropertyDescriptor descriptor = propDescriptor;
                if (newMetadata.Length > 0)
                {
                    // Create a metadata descriptor that wraps the property descriptor
                    descriptor = new MetadataPropertyDescriptorWrapper(propDescriptor, newMetadata);
                    customDescriptorsCreated = true;
                }

                tempPropertyDescriptors.Add(descriptor);
            }

            if (customDescriptorsCreated)
            {
                return new PropertyDescriptorCollection(tempPropertyDescriptors.ToArray(), true);
            }
            return originalCollection;
        }

        public override AttributeCollection GetAttributes()
        {
            // Since normal TD behavior is to return cached attribute instances on subsequent
            // calls to GetAttributes, we must be sure below to use the TD APIs to get both
            // the base and associated attributes
            AttributeCollection attributes = base.GetAttributes();
            if (AssociatedMetadataType != null && !IsSelfAssociated)
            {
                // Note that the use of TypeDescriptor.GetAttributes here opens up the possibility of
                // infinite recursion, in the corner case of two Types referencing each other as
                // metadata types (or a longer cycle), though the second condition above saves an immediate such
                // case where a Type refers to itself.
                Attribute[] newAttributes = TypeDescriptor.GetAttributes(AssociatedMetadataType).OfType<Attribute>().ToArray();
                attributes = AttributeCollection.FromExisting(attributes, newAttributes);
            }
            return attributes;
        }

        private static class TypeDescriptorCache
        {
            private static readonly Attribute[] emptyAttributes = new Attribute[0];
            // Stores the associated metadata type for a type
            private static readonly ConcurrentDictionary<Type, Type> _metadataTypeCache = new ConcurrentDictionary<Type, Type>();

            // Stores the attributes for a member info
            private static readonly ConcurrentDictionary<Tuple<Type, string>, Attribute[]> _typeMemberCache = new ConcurrentDictionary<Tuple<Type, string>, Attribute[]>();

            // Stores whether or not a type and associated metadata type has been checked for validity
            private static readonly ConcurrentDictionary<Tuple<Type, Type>, bool> _validatedMetadataTypeCache = new ConcurrentDictionary<Tuple<Type, Type>, bool>();

            public static void ValidateMetadataType(Type type, Type associatedType)
            {
                Tuple<Type, Type> typeTuple = new Tuple<Type, Type>(type, associatedType);
                if (!_validatedMetadataTypeCache.ContainsKey(typeTuple))
                {
                    CheckAssociatedMetadataType(type, associatedType);
                    _validatedMetadataTypeCache.TryAdd(typeTuple, true);
                }
            }

            public static Type GetAssociatedMetadataType(Type type)
            {
                Type associatedMetadataType = null;
                if (_metadataTypeCache.TryGetValue(type, out associatedMetadataType))
                {
                    return associatedMetadataType;
                }

                // Try association attribute
                MetadataTypeAttribute attribute = (MetadataTypeAttribute)Attribute.GetCustomAttribute(type, typeof(MetadataTypeAttribute));
                if (attribute != null)
                {
                    associatedMetadataType = attribute.MetadataClassType;
                }
                _metadataTypeCache.TryAdd(type, associatedMetadataType);
                return associatedMetadataType;
            }

            private static void CheckAssociatedMetadataType(Type mainType, Type associatedMetadataType)
            {
                // Only properties from main type
                HashSet<string> mainTypeMemberNames = new HashSet<string>(mainType.GetProperties().Select(p => p.Name));

                // Properties and fields from buddy type
                var buddyFields = associatedMetadataType.GetFields().Select(f => f.Name);
                var buddyProperties = associatedMetadataType.GetProperties().Select(p => p.Name);
                HashSet<string> buddyTypeMembers = new HashSet<string>(buddyFields.Concat(buddyProperties), StringComparer.Ordinal);

                // Buddy members should be a subset of the main type's members
                if (!buddyTypeMembers.IsSubsetOf(mainTypeMemberNames))
                {
                    // Reduce the buddy members to the set not contained in the main members
                    buddyTypeMembers.ExceptWith(mainTypeMemberNames);

                    throw new InvalidOperationException();
                    //String.Format(
                    //	CultureInfo.CurrentCulture,
                    //	DataAnnotationsResources.AssociatedMetadataTypeTypeDescriptor_MetadataTypeContainsUnknownProperties,
                    //	mainType.FullName,
                    //	String.Join(", ", buddyTypeMembers.ToArray())));
                }
            }

            public static Attribute[] GetAssociatedMetadata(Type type, string memberName)
            {
                var memberTuple = new Tuple<Type, string>(type, memberName);
                Attribute[] attributes;
                if (_typeMemberCache.TryGetValue(memberTuple, out attributes))
                {
                    return attributes;
                }

                // Allow fields and properties
                MemberTypes allowedMemberTypes = MemberTypes.Property | MemberTypes.Field;
                // Only public static/instance members
                BindingFlags searchFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
                // Try to find a matching member on type
                MemberInfo matchingMember = type.GetMember(memberName, allowedMemberTypes, searchFlags).FirstOrDefault();
                if (matchingMember != null)
                {
                    attributes = Attribute.GetCustomAttributes(matchingMember, true /* inherit */);
                }
                else
                {
                    attributes = emptyAttributes;
                }

                _typeMemberCache.TryAdd(memberTuple, attributes);
                return attributes;
            }
        }
    }
    internal class MetadataPropertyDescriptorWrapper : PropertyDescriptor
    {
        private PropertyDescriptor _descriptor;
        private bool _isReadOnly;

        public MetadataPropertyDescriptorWrapper(PropertyDescriptor descriptor, Attribute[] newAttributes)
            : base(descriptor, newAttributes)
        {
            _descriptor = descriptor;
            var readOnlyAttribute = newAttributes.OfType<ReadOnlyAttribute>().FirstOrDefault();
            _isReadOnly = (readOnlyAttribute != null ? readOnlyAttribute.IsReadOnly : false);
        }

        public override void AddValueChanged(object component, EventHandler handler) { _descriptor.AddValueChanged(component, handler); }

        public override bool CanResetValue(object component) { return _descriptor.CanResetValue(component); }

        public override Type ComponentType { get { return _descriptor.ComponentType; } }

        public override object GetValue(object component) { return _descriptor.GetValue(component); }

        public override bool IsReadOnly
        {
            get
            {
                // Dev10 Bug 594083
                // It's not enough to call the wrapped _descriptor because it does not know anything about
                // new attributes passed into the constructor of this class.
                return _isReadOnly || _descriptor.IsReadOnly;
            }
        }

        public override Type PropertyType { get { return _descriptor.PropertyType; } }

        public override void RemoveValueChanged(object component, EventHandler handler) { _descriptor.RemoveValueChanged(component, handler); }

        public override void ResetValue(object component) { _descriptor.ResetValue(component); }

        public override void SetValue(object component, object value) { _descriptor.SetValue(component, value); }

        public override bool ShouldSerializeValue(object component) { return _descriptor.ShouldSerializeValue(component); }

        public override bool SupportsChangeEvents { get { return _descriptor.SupportsChangeEvents; } }
    }

#endif
}
