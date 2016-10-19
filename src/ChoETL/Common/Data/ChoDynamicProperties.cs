using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static class ChoDynamicProperties1
    {
        #region Delegates

        public delegate object GenericGetter(object target);

        public delegate void GenericSetter(object target, object value);

        #endregion

        public static IList<Property> CreatePropertyMethods(Type type)
        {
            var returnValue = new List<Property>();
            //returnValue.Add(new Property("CustomerID", typeof(string)));
            //return returnValue;
            foreach (MemberInfo info in ChoType.GetMembers(type))
            {
            }

            foreach (PropertyInfo prop in type.GetProperties())
            {
                returnValue.Add(new Property(prop));
            }
            return returnValue;
        }

        public static IList<Property> CreatePropertyMethods(IChoDynamicRecord rec)
        {
            var returnValue = new List<Property>();

            //foreach (PropertyInfo prop in rec.GetType().GetProperties())
            //{
            //    returnValue.Add(new Property(prop));
            //}
            //foreach (FieldInfo prop in rec.GetType().GetFields())
            //{
            //    returnValue.Add(new Property(prop));
            //}
            if (rec is IChoDynamicRecord)
            {
                foreach (string mn in ((IChoDynamicRecord)rec).GetDynamicMemberNames())
                    returnValue.Add(new Property(mn, ((IChoDynamicRecord)rec).GetDynamicMemberType(mn)));
            }
            return returnValue;
        }

        public static IList<Property> CreatePropertyMethods<T>()
        {
            var returnValue = new List<Property>();

            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                returnValue.Add(new Property(prop));
            }
            return returnValue;
        }


        /// <summary>
        /// Creates a dynamic setter for the property
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static GenericSetter CreateSetMethod(PropertyInfo propertyInfo)
        {
            /*
            * If there's no setter return null
            */
            MethodInfo setMethod = propertyInfo.GetSetMethod();
            if (setMethod == null)
                return null;

            /*
            * Create the dynamic method
            */
            var arguments = new Type[2];
            arguments[0] = arguments[1] = typeof(object);

            var setter = new DynamicMethod(
                String.Concat("_Set", propertyInfo.Name, "_"),
                typeof(void), arguments, propertyInfo.DeclaringType);
            ILGenerator generator = setter.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            generator.Emit(OpCodes.Ldarg_1);

            if (propertyInfo.PropertyType.IsClass)
                generator.Emit(OpCodes.Castclass, propertyInfo.PropertyType);
            else
                generator.Emit(OpCodes.Unbox_Any, propertyInfo.PropertyType);

            generator.EmitCall(OpCodes.Callvirt, setMethod, null);
            generator.Emit(OpCodes.Ret);

            /*
            * Create the delegate and return it
            */
            return (GenericSetter)setter.CreateDelegate(typeof(GenericSetter));
        }


        /// <summary>
        /// Creates a dynamic getter for the property
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        public static GenericGetter CreateGetMethod(PropertyInfo propertyInfo)
        {
            /*
            * If there's no getter return null
            */
            MethodInfo getMethod = propertyInfo.GetGetMethod();
            if (getMethod == null)
                return null;

            /*
            * Create the dynamic method
            */
            var arguments = new Type[1];
            arguments[0] = typeof(object);

            var getter = new DynamicMethod(
                String.Concat("_Get", propertyInfo.Name, "_"),
                typeof(object), arguments, propertyInfo.DeclaringType);
            ILGenerator generator = getter.GetILGenerator();
            generator.DeclareLocal(typeof(object));
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, propertyInfo.DeclaringType);
            generator.EmitCall(OpCodes.Callvirt, getMethod, null);

            if (!propertyInfo.PropertyType.IsClass)
                generator.Emit(OpCodes.Box, propertyInfo.PropertyType);

            generator.Emit(OpCodes.Ret);

            /*
            * Create the delegate and return it
            */
            return (GenericGetter)getter.CreateDelegate(typeof(GenericGetter));
        }

        #region Nested type: Property

        public class Property
        {
            public readonly GenericGetter Getter;
            public readonly PropertyInfo Info;
            public readonly GenericSetter Setter;
            public readonly FieldInfo FieldInfo;
            public readonly Type DynamicProperyType;
            public readonly string DynamicMemberName;
            public readonly bool IsDynamicProperty;

            public Property(PropertyInfo info)
            {
                Info = info;
                Setter = CreateSetMethod(info);
                Getter = CreateGetMethod(info);
            }

            public Property(FieldInfo info)
            {
                FieldInfo = info;
            }

            public Property(string memberName, Type memberType)
            {
                DynamicMemberName = memberName;
                DynamicProperyType = memberType;
                IsDynamicProperty = true;
            }

            public Type GetPropertyType()
            {
                if (Info != null)
                    return Info.PropertyType;
                else if (FieldInfo != null)
                    return FieldInfo.FieldType;
                else
                    return DynamicProperyType;
            }

            public object GetValue(object target)
            {
                if (Info != null)
                    return null;
                else if (FieldInfo != null)
                    return null;
                else
                    return ((IChoDynamicRecord)target).GetPropertyValue(DynamicMemberName);
            }

            public string GetName()
            {
                if (Info != null)
                    return null;
                else if (FieldInfo != null)
                    return null;
                else
                    return DynamicMemberName;
            }
        }

        #endregion

        ///// <summary>
        ///// An expression based Getter getter found in comments. untested.
        ///// Q: i don't see a reciprocal setter expression?
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="propName"></param>
        ///// <returns></returns>
        //public static Func<T> CreateGetPropValue<T>(string propName)
        //{
        //    var param = Expression.Parameter(typeof(object), "container");
        //    var func = Expression.Lambda(
        //    Expression.Convert(Expression.PropertyOrField(Expression.Convert(param, typeof(T)), propName), typeof(object)), param);
        //    return (Func<T>)func.Compile();
        //}
    }
}
