﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
#if !NETSTANDARD2_0
using System.Windows.Data;
#endif

namespace ChoETL
{
    public static class ChoConvert
    {
        public static readonly CultureInfo DefaultCulture = CultureInfo.CurrentCulture;
        private const string ImplicitOperatorMethodName = "op_Implicit";
        private const string ExplicitOperatorMethodName = "op_Explicit";

        public static bool TryConvertTo(object value, Type targetType, CultureInfo culture, out object output, 
            string propName = null, ChoRecordConfiguration config = null)
        {
            output = (object)null;
            try
            {
                output = ChoConvert.ConvertTo(value, targetType, culture, propName, config);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static object ConvertTo(object value, Type targetType, CultureInfo culture = null, string propName = null,
            ChoRecordConfiguration config = null)
        {
            if (value == null)
                return ChoConvert.ConvertTo(value, targetType, value, (object[])null, (object[])null, culture, propName, config);

            Type origType = targetType;
            Type type = value == null ? typeof(object) : value.GetType();
            if (type == origType)
                return value;

            return ChoConvert.ConvertTo(value, targetType, value, ChoTypeDescriptor.GetTypeConvertersForType(type), null, culture, 
                propName, config);
        }

        public static bool TryConvertFrom(object value, MemberInfo memberInfo, object sourceObject, CultureInfo culture,
            ChoRecordConfiguration config, out object output)
        {
            output = (object)null;
            ChoGuard.ArgumentNotNull((object)memberInfo, "MemberInfo");
            try
            {
                output = ChoConvert.ConvertFrom(value, memberInfo, sourceObject, culture, config);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static object ConvertFrom(object value, MemberInfo memberInfo, object sourceObject = null, CultureInfo culture = null,
            ChoRecordConfiguration config = null)
        {
            ChoGuard.ArgumentNotNull(memberInfo, "MemberInfo");
            return ChoConvert.ConvertFrom(value, ChoType.GetMemberType(memberInfo), sourceObject, ChoTypeDescriptor.GetTypeConverters(memberInfo), ChoTypeDescriptor.GetTypeConverterParams(memberInfo), culture, memberInfo.Name,
                config);
        }

        public static object ConvertFrom(object value, PropertyInfo propertyInfo, object sourceObject = null, CultureInfo culture = null,
            ChoRecordConfiguration config = null)
        {
            ChoGuard.ArgumentNotNull(propertyInfo, "PropertyInfo");
            return ChoConvert.ConvertFrom(value, propertyInfo.PropertyType, sourceObject, ChoTypeDescriptor.GetTypeConverters(propertyInfo), ChoTypeDescriptor.GetTypeConverterParams(propertyInfo), culture, propertyInfo.Name,
                config);
        }

        public static object ConvertFrom(object value, Type targetType, object sourceObject = null, object[] converters = null, object[] parameters = null, CultureInfo culture = null, string propName = null,
            ChoRecordConfiguration config = null)
        {
            if (value != null)
            {
                var valueType = value.GetType();
                if (valueType == targetType
                    || (targetType != typeof(object) && targetType.IsAssignableFrom(valueType)))
                {
                    //return value;
                }
            }

            Type origType = targetType;
            targetType = targetType.IsNullableType() ? targetType.GetUnderlyingType() : targetType;
            if (targetType != null && targetType.IsValueType && origType != null & origType.IsNullableType())
            {
                if (value == null || (value is string && ((string)value).IsNullOrWhiteSpace()))
                    return null;
            }

            object obj1 = value;
            if (targetType == (Type)null)
                return value;
            //if (targetType == typeof(object))
            //    return value;
            if (culture == null)
                culture = ChoConvert.DefaultCulture;

            bool collectionConvertersFound = false;
            if (converters != null)
            {
                collectionConvertersFound = converters.OfType<IChoCollectionConverter>().Any();
            }

            if (targetType != typeof(object))
            {
                if (sourceObject is IChoConvertible && !propName.IsNullOrWhiteSpace())
                {
                    var convObject = sourceObject as IChoConvertible;
                    object convPropValue = null;
                    if (convObject.Convert(propName, value, culture, out convPropValue))
                        return convPropValue;
                }

                if (!collectionConvertersFound)
                {
                    if (value is ICollection)
                    {
                        if (typeof(ICollection).IsAssignableFrom(targetType)
                            || (targetType.IsGenericType() && typeof(IDictionary<,>).IsAssignableFrom(targetType.GetGenericTypeDefinition())))
                        {

                        }
                        else
                            value = ((IEnumerable)value).FirstOrDefaultEx<object>();
                    }
                    if (value != null && typeof(IList).IsAssignableFrom(targetType) && !(value is IList))
                    {
                        value = new object[] { value };
                    }
                    //if (value != null && typeof(IDictionary<string, object>).IsAssignableFrom(targetType)
                    //    && value is IDictionary<string, object>)
                    //    return value;
                }
            }

            Type type = value == null ? typeof(object) : value.GetType();
            try
            {
                if (converters.IsNullOrEmpty())
                {
                    if (targetType == typeof(object))
                        converters = ChoTypeDescriptor.GetTypeConvertersForType(type);
                    else
                        converters = ChoTypeDescriptor.GetTypeConvertersForType(targetType);
                }

                if (converters == null || converters.Length == 0)
                {
                    if (value is IList)
                    {
                        var elementType = targetType.GetItemType();
                        if (config != null)
                        {
                            converters = config.GetConvertersForType(elementType, ((IList)value).OfType<object>().FirstOrDefault());
                            parameters = config.GetConverterParamsForType(elementType, ((IList)value).OfType<object>().FirstOrDefault());
                        }
                        if (converters == null || converters.Length == 0)
                            converters = ChoTypeDescriptor.GetTypeConvertersForType(elementType);
                    }
                }
                else if (value is IList && !converters.Any(c => typeof(IChoCollectionConverter).IsAssignableFrom(c.GetType())))
                {
                    object[] newconverters = null;
                    object[] newparameters = null;

                    var elementType = targetType.GetItemType();
                    if (config != null)
                    {
                        newconverters = config.GetConvertersForType(elementType, ((IList)value).OfType<object>().FirstOrDefault());
                        newparameters = config.GetConverterParamsForType(elementType, ((IList)value).OfType<object>().FirstOrDefault());
                    }
                    if (newconverters == null || newconverters.Length == 0)
                        newconverters = ChoTypeDescriptor.GetTypeConvertersForType(elementType);
                    if (newconverters != null && newconverters.Length > 0)
                    {
                        converters = newconverters;
                        parameters = newparameters;
                    }
                }

                if (converters != null && converters.Length > 0)
                {
                    object objArray = null;
                    for (int index = 0; index < converters.Length; ++index)
                    {
                        object conv = converters[index];
                        if (parameters != null && parameters.Length > 0)
                            objArray = parameters[index];

                        if (config != null && objArray == null)
                            objArray = new object[] { config.TypeConverterFormatSpec };

                        if (value is IList && !typeof(IChoCollectionConverter).IsAssignableFrom(conv.GetType()))
                        {
                            List<object> retValue = new List<object>();
                            object lVal = null;
                                
                            var elementType = targetType.GetItemType();
                            foreach (var item in (IList)value)
                            {
                                if (conv is TypeConverter)
                                {
                                    TypeConverter typeConverter = conv as TypeConverter;
                                    if (typeConverter.CanConvertFrom(type))
                                        lVal = typeConverter.ConvertFrom((ITypeDescriptorContext)null, culture, item);
                                }
#if !NETSTANDARD2_0
                                else if (conv is IValueConverter)
                                    lVal = ((IValueConverter)conv).Convert(item, elementType, (object)objArray, culture);
#endif
                                else if (conv is IChoValueConverter)
                                    lVal = ((IChoValueConverter)conv).Convert(item, elementType, (object)objArray, culture);

                                retValue.Add(lVal);
                            }

                            value = retValue.ToArray();
                        }
                        else
                        {
                            if (conv is TypeConverter)
                            {
                                TypeConverter typeConverter = conv as TypeConverter;
                                if (typeConverter.CanConvertFrom(type))
                                    value = typeConverter.ConvertFrom((ITypeDescriptorContext)null, culture, value);
                            }
#if !NETSTANDARD2_0
                            else if (conv is IValueConverter)
                                value = ((IValueConverter)conv).Convert(value, targetType, (object)objArray, culture);
#endif
                            else if (conv is IChoValueConverter)
                                value = ((IChoValueConverter)conv).Convert(value, targetType, (object)objArray, culture);
                        }
                    }
                    //if (value != obj1)
                    //    return value;
                }

                if (targetType == typeof(object))
                    return value;

                if (value == null)
                    return origType.Default();
                if (targetType.IsAssignableFrom(value.GetType()) || targetType == value.GetType())
                    return value;

                var srcType = value.GetType();
                var conv1 = TypeDescriptor.GetConverter(targetType);
                if (conv1 != null)
                {
                    try
                    {
                        if (conv1.CanConvertFrom(srcType))
                            return conv1.ConvertFrom(value);
                    }
                    catch { }
                }

                if (value is IConvertible)
                {
                    try
                    {
                        value = Convert.ChangeType(value, targetType, (IFormatProvider)culture);
                        if (obj1 != value)
                            return value;
                    }
                    catch
                    {
                    }
                }
                if (ChoConvert.TryConvertXPlicit(value, targetType, "op_Explicit", ref value)
                    || ChoConvert.TryConvertXPlicit(value, targetType, "op_Implicit", ref value))
                    return value;

                object convValue = null;
                if (origType.IsNullableType())
                    return null;
                else if (ChoConvert.TryConvertToSpecialValues(value, targetType, culture, out convValue))
                    return convValue;

                if (value is Array && typeof(IList).IsAssignableFrom(targetType))
                {
                    if (typeof(Array).IsAssignableFrom(targetType))
                    {
                        MethodInfo convertMethod = typeof(ChoConvert).GetMethod("ConvertToArray",
                        BindingFlags.NonPublic | BindingFlags.Static);
                        MethodInfo generic = convertMethod.MakeGenericMethod(new[] { targetType.GetItemType() });
                        return generic.Invoke(null, new object[] { value });
                    }
                    else
                    {
                        MethodInfo convertMethod = typeof(ChoConvert).GetMethod("ConvertToList",
                        BindingFlags.NonPublic | BindingFlags.Static);
                        MethodInfo generic = convertMethod.MakeGenericMethod(new[] { targetType.GetItemType() });
                        return generic.Invoke(null, new object[] { value });
                    }
                }
                else if (value is IList && typeof(Array).IsAssignableFrom(targetType))
                {
                    if (typeof(Array).IsAssignableFrom(targetType))
                    {
                        MethodInfo convertMethod = typeof(ChoConvert).GetMethod("ConvertListToArray",
                        BindingFlags.NonPublic | BindingFlags.Static);
                        MethodInfo generic = convertMethod.MakeGenericMethod(new[] { targetType.GetItemType() });
                        return generic.Invoke(null, new object[] { value });
                    }
                    else
                    {
                        MethodInfo convertMethod = typeof(ChoConvert).GetMethod("ConvertListToList",
                        BindingFlags.NonPublic | BindingFlags.Static);
                        MethodInfo generic = convertMethod.MakeGenericMethod(new[] { targetType.GetItemType() });
                        return generic.Invoke(null, new object[] { value });
                    }
                }
                throw new ApplicationException("Object conversion failed.");
            }
            catch (Exception ex)
            {
                if (type.IsSimple())
                    throw new ApplicationException(string.Format("Can't convert '{2}' value from '{0}' type to '{1}' type.", (object)type, (object)targetType, value), ex);
                throw new ApplicationException(string.Format("Can't convert object from '{0}' type to '{1}' type.", (object)type, (object)targetType), ex);
            }
        }
        private static T[] ConvertToArray<T>(Array input)
        {
            try
            {
                return input.Cast<T>().ToArray(); // Using LINQ for simplicity
            }
            catch
            {
                return input.OfType<object>().Select(o => o.ToNString()).CastEnumerable<T>().ToArray();
            }
        }
        private static List<T> ConvertToList<T>(Array input)
        {
            try
            {
                return input.Cast<T>().ToList(); // Using LINQ for simplicity
            }
            catch
            {
                return input.OfType<object>().Select(o => o.ToNString()).CastEnumerable<T>().ToList();
            }
        }
        private static T[] ConvertListToArray<T>(IList input)
        {
            try
            {
                return input.Cast<T>().ToArray(); // Using LINQ for simplicity
            }
            catch
            {
                return input.OfType<object>().Select(o => o.ToNString()).CastEnumerable<T>().ToArray();
            }
        }
        private static List<T> ConvertListToList<T>(IList input)
        {
            try
            {
                return input.Cast<T>().ToList(); // Using LINQ for simplicity
            }
            catch
            {
                return input.OfType<object>().Select(o => o.ToNString()).CastEnumerable<T>().ToList();
            }
        }

        private static bool TryConvertXPlicit(object value, Type destinationType, string operatorMethodName, ref object result)
        {
            return ChoConvert.TryConvertXPlicit(value, value.GetType(), destinationType, operatorMethodName, ref result) || ChoConvert.TryConvertXPlicit(value, destinationType, destinationType, operatorMethodName, ref result);
        }

        private static bool TryConvertXPlicit(object value, Type invokerType, Type destinationType, string xPlicitMethodName, ref object result)
        {
            foreach (MethodInfo methodInfo in Enumerable.Where<MethodInfo>((IEnumerable<MethodInfo>)invokerType.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public), (Func<MethodInfo, bool>)(m => m.Name == xPlicitMethodName)))
            {
                if (destinationType.IsAssignableFrom(methodInfo.ReturnType))
                {
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    if (Enumerable.Count<ParameterInfo>((IEnumerable<ParameterInfo>)parameters) == 1)
                    {
                        if (parameters[0].ParameterType == value.GetType())
                        {
                            try
                            {
                                result = methodInfo.Invoke((object)null, new object[1]
                                {
                                    value
                                });
                                return true;
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static bool TryConvertToSpecialValues(object value, Type targetType, CultureInfo culture, out object result)
        {
            result = (object)null;
            if (value is IList)
                return false;
            if (value is IDictionary)
                return false;

            if (value is string && ((string)value).Length == 0)
            {
                result = targetType.Default();
                return true;
            }
            if (targetType.IsEnum)
            {
                if (value is string)
                {
                    result = Enum.Parse(targetType, value as string);
                    if (Enum.IsDefined(targetType, result))
                        return true;
                }
                result = Enum.ToObject(targetType, value);
                return true;
            }
            if (value is string && targetType == typeof(Guid))
            {
                result = (object)new Guid(value as string);
                return true;
            }
            if (value is string && targetType == typeof(Version))
            {
                result = (object)new Version(value as string);
                return true;
            }
            if (targetType == typeof(string))
            {
                result = (object)value.ToString();
                return true;
            }
            return false;
        }

        public static bool TryConvertTo(object value, MemberInfo memberInfo, Type targetType, object sourceObject, 
            CultureInfo culture, ChoRecordConfiguration config, out object output)
        {
            output = (object)null;
            ChoGuard.ArgumentNotNull((object)memberInfo, "MemberInfo");
            try
            {
                output = ChoConvert.ConvertTo(value, targetType, sourceObject, ChoTypeDescriptor.GetTypeConverters(memberInfo), 
                    ChoTypeDescriptor.GetTypeConverterParams(memberInfo), culture, memberInfo.Name, config);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static object ConvertTo(object value, MemberInfo memberInfo, Type targetType, object sourceObject = null, 
            CultureInfo culture = null, ChoRecordConfiguration config = null)
        {
            ChoGuard.ArgumentNotNull((object)memberInfo, "MemberInfo");
            return ChoConvert.ConvertTo(value, targetType, sourceObject, ChoTypeDescriptor.GetTypeConverters(memberInfo), ChoTypeDescriptor.GetTypeConverterParams(memberInfo), culture,
                memberInfo.Name, config);
        }

        public static object ConvertTo(object value, Type targetType, object sourceObject, object[] converters, object[] parameters, CultureInfo culture,
            string propName = null, ChoRecordConfiguration config = null)
        {
            Type origType = targetType;
            targetType = targetType.IsNullableType() ? targetType.GetUnderlyingType() : targetType;
            object obj1 = value;
            if (targetType == (Type)null)
                return value;
            //if (targetType == typeof(object))
            //    return value;
            if (culture == null)
                culture = ChoConvert.DefaultCulture;
            Type type = value == null ? typeof(object) : value.GetType().GetUnderlyingType();

            try
            {
                if (sourceObject is IChoConvertible && !propName.IsNullOrWhiteSpace())
                {
                    var convObject = sourceObject as IChoConvertible;
                    object convPropValue = null;
                    if (convObject.ConvertBack(propName, value, targetType, culture, out convPropValue))
                        return convPropValue;
                }

                object objArray = null;
                if (converters.IsNullOrEmpty())
                    converters = ChoTypeDescriptor.GetTypeConvertersForType(targetType /*type*/);

                if (converters != null && converters.Length > 0)
                {
                    for (int index = 0; index < converters.Length; ++index)
                    {
                        object conv = converters[index];
                        if (parameters != null && parameters.Length > 0)
                            objArray = parameters[index];

                        if (config != null && objArray == null)
                            objArray = new object[] { config.TypeConverterFormatSpec };

                        if (value is IList && !typeof(IChoCollectionConverter).IsAssignableFrom(conv.GetType()))
                        {
                            List<object> retValue = new List<object>();
                            object lVal = null;
                            foreach (var item in (IList)value)
                            {
                                if (conv is TypeConverter)
                                {
                                    TypeConverter typeConverter = conv as TypeConverter;
                                    if (typeConverter.CanConvertFrom(type))
                                        lVal = typeConverter.ConvertTo((ITypeDescriptorContext)null, culture, item, targetType.GetItemType());
                                }
#if !NETSTANDARD2_0
                                else if (conv is IValueConverter)
                                    lVal = ((IValueConverter)conv).ConvertBack(item, targetType, (object)objArray, culture);
#endif
                                else if (conv is IChoValueConverter)
                                    lVal = ((IChoValueConverter)conv).ConvertBack(item, targetType, (object)objArray, culture);

                                retValue.Add(lVal);
                            }

                            value = retValue.ToArray();
                        }
                        else
                        {
                            if (conv is TypeConverter)
                            {
                                TypeConverter typeConverter = conv as TypeConverter;
                                if (typeConverter.CanConvertFrom(type))
                                    value = typeConverter.ConvertTo((ITypeDescriptorContext)null, culture, value, targetType);
                            }
#if !NETSTANDARD2_0
                            else if (conv is IValueConverter)
                                value = ((IValueConverter)conv).ConvertBack(value, targetType, (object)objArray, culture);
#endif
                            else if (conv is IChoValueConverter)
                                value = ((IChoValueConverter)conv).ConvertBack(value, targetType, (object)objArray, culture);
                        }

                    }
                    if (obj1 != value)
                        return value;
                }
                if (value == null)
                    return origType.Default();
                if (type == origType)
                    return value;
                if (targetType.IsAssignableFrom(value.GetType()) || targetType == value.GetType())
                    return value;

                var srcType = value.GetType();
                var conv1 = TypeDescriptor.GetConverter(targetType);
                if (conv1 != null)
                {
                    try
                    {
                        if (conv1.CanConvertFrom(srcType))
                            return conv1.ConvertFrom(value);
                    }
                    catch { }
                }

                if (value is IConvertible)
                {
                    try
                    {
                        value = Convert.ChangeType(value, targetType, (IFormatProvider)culture);
                        if (obj1 != value)
                            return value;
                    }
                    catch
                    {
                    }
                }
                if (ChoConvert.TryConvertXPlicit(value, targetType, "op_Explicit", ref value)
                    || ChoConvert.TryConvertXPlicit(value, targetType, "op_Implicit", ref value))
                    //|| (!origType.IsNullableType() && ChoConvert.TryConvertToSpecialValues(value, targetType, culture, out value)))
                    //  || ChoConvert.TryConvertToSpecialValues(value, targetType, culture, out value))
                    return value;

                if (targetType == typeof(
                     ChoDynamicObject))
                {
                    dynamic ret = new ChoDynamicObject();
                    ret.Value = value;
                    return ret;
                }

                object result = null;
                if (origType.IsNullableType())
                    return null;
                else if (ChoConvert.TryConvertToSpecialValues(value, targetType, culture, out result))
                    return result;

                throw new ApplicationException("Object conversion failed.");
            }
            catch (Exception ex)
            {
                if (type.IsSimple())
                    throw new ApplicationException(string.Format("Can't convert '{2}' value from '{0}' type to '{1}' type.", (object)type, (object)targetType, value), ex);
                throw new ApplicationException(string.Format("Can't convert object from '{0}' type to '{1}' type.", (object)type, (object)targetType), ex);
            }
        }

        // Summary:
        //     Returns an object of the specified type and whose value is equivalent to
        //     the specified object.
        //
        // Parameters:
        //   value:
        //     An object that implements the System.IConvertible interface.
        //
        //   conversionType:
        //     The type of object to return.
        //
        // Returns:
        //     An object whose type is conversionType and whose value is equivalent to value.-or-A
        //     null reference (Nothing in Visual Basic), if value is null and conversionType
        //     is not a value type.
        //
        // Exceptions:
        //   System.InvalidCastException:
        //     This conversion is not supported. -or-value is null and conversionType is
        //     a value type.-or-value does not implement the System.IConvertible interface.
        //
        //   System.FormatException:
        //     value is not in a format recognized by conversionType.
        //
        //   System.OverflowException:
        //     value represents a number that is out of the range of conversionType.
        //
        //   System.ArgumentNullException:
        //     conversionType is null.
        public static object ChangeType(object value, Type conversionType)
        {
            if (value == null)
                return ChoActivator.CreateInstanceAndInit(conversionType);

            if (conversionType.IsAssignableFrom(value.GetType()))
                return value;

            object dest = null;
            if (value is IConvertible)
                dest = Convert.ChangeType(value, conversionType);
            else
            {
                try
                {
                    dest = ChoActivator.CreateInstanceAndInit(conversionType);
                    value.CloneTo(dest);
                }
                catch
                {
                    dest = value;
                }
            }

            if (dest != null && !dest.GetType().IsSimple())
            {
                ChoObjectValidationMode m = GetValidationMode(value);
                if (m == ChoObjectValidationMode.MemberLevel)
                    ChoValidator.Validate(dest);
                else if (m == ChoObjectValidationMode.ObjectLevel)
                    ChoValidator.Validate(dest);
            }
            return dest;
        }

        public static object ChangeType<T>(object value, Type conversionType)
            where T : Attribute
        {
            if (value == null)
                return ChoActivator.CreateInstanceAndInit(conversionType);

            if (conversionType.IsAssignableFrom(value.GetType()))
                return value;

            object dest = null;
            if (value is IConvertible)
                dest = Convert.ChangeType(value, conversionType);
            else
            {
                dest = ChoActivator.CreateInstanceAndInit(conversionType);
                value.CloneTo<T>(dest);
            }

            if (dest != null && !dest.GetType().IsSimple())
            {
                ChoObjectValidationMode m = GetValidationMode(value);
                if (m == ChoObjectValidationMode.MemberLevel)
                    ChoValidator.Validate(dest);
                else if (m == ChoObjectValidationMode.ObjectLevel)
                    ChoValidator.Validate(dest);
            }
            return dest;
        }

        private static ChoObjectValidationMode GetValidationMode(object value)
        {
            if (value == null) return ChoObjectValidationMode.Off;

            ChoObjectAttribute attr = ChoType.GetAttribute<ChoObjectAttribute>(value.GetType());
            return attr != null ? attr.ObjectValidationMode : ChoObjectValidationMode.Off;
        }
    }

    public static class ChoCustomSerializer
    {
        public static object Deserialize(object value, Type targetType, object serializer = null, object serializerParams = null, CultureInfo culture = null, string propName = null)
        {
            Type type = value == null ? typeof(object) : value.GetType();
            if (serializer is TypeConverter)
            {
                TypeConverter typeConverter = serializer as TypeConverter;
                if (typeConverter.CanConvertFrom(type))
                    value = typeConverter.ConvertFrom((ITypeDescriptorContext)null, culture, value);
            }
#if !NETSTANDARD2_0
            else if (serializer is IValueConverter)
                value = ((IValueConverter)serializer).Convert(value, targetType, (object)serializerParams, culture);
#endif
            else if (serializer is IChoValueConverter)
                value = ((IChoValueConverter)serializer).Convert(value, targetType, (object)serializerParams, culture);

            return value;
        }

        public static object Serialize(object value, Type targetType, object serializer = null, object serializerParams = null, CultureInfo culture = null, string propName = null)
        {
            if (serializer is TypeConverter)
            {
                TypeConverter typeConverter = serializer as TypeConverter;
                if (typeConverter.CanConvertTo(targetType))
                    value = typeConverter.ConvertTo((ITypeDescriptorContext)null, culture, value, targetType);
            }
#if !NETSTANDARD2_0
            else if (serializer is IValueConverter)
                value = ((IValueConverter)serializer).ConvertBack(value, targetType, (object)serializerParams, culture);
#endif
            else if (serializer is IChoValueConverter)
                value = ((IChoValueConverter)serializer).ConvertBack(value, targetType, (object)serializerParams, culture);

            return value;
        }
    }
}
