using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChoETL
{
    public static partial class ChoLinqEx
    {
        public static IEnumerable<T> Touch<T>(this IEnumerable<T> items) =>
            items == null || items.Count() == 0 ? Enumerable.Repeat(ChoActivator.CreateInstance<T>(), 1) : items;
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static string GetMemberName<T>(this Expression<Func<T, object>> field)
        {
            return (field.Body as MemberExpression ?? ((UnaryExpression)field.Body).Operand as MemberExpression).Member.Name;
        }

        public static PropertyInfo GetPropertyInfo<TClass, TField>(this Expression<Func<TClass, TField>> field)
        {
            return ((PropertyInfo)(field.Body as MemberExpression ?? ((UnaryExpression)field.Body).Operand as MemberExpression).Member);
        }

        public static PropertyDescriptor GetPropertyDescriptor<TClass, TField>(this Expression<Func<TClass, TField>> field)
        {
            return GetPropertyDescriptor((PropertyInfo)(field.Body as MemberExpression ?? ((UnaryExpression)field.Body).Operand as MemberExpression).Member);
        }

        public static PropertyDescriptor GetPropertyDescriptor(this PropertyInfo propertyInfo)
        {
            return TypeDescriptor.GetProperties(propertyInfo.DeclaringType)[propertyInfo.Name];
        }

        public static Type GetPropertyType<TClass, TField>(this Expression<Func<TClass, TField>> field)
        {
            return ((PropertyInfo)(field.Body as MemberExpression ?? ((UnaryExpression)field.Body).Operand as MemberExpression).Member).PropertyType;
        }

        public static string GetMemberName<TClass, TField>(this Expression<Func<TClass, TField>> field)
        {
            return (field.Body as MemberExpression ?? ((UnaryExpression)field.Body).Operand as MemberExpression).Member.Name;
        }

        public static Type GetReflectedType<TClass, TField>(this Expression<Func<TClass, TField>> field)
        {
            return (field.Body as MemberExpression ?? ((UnaryExpression)field.Body).Operand as MemberExpression).Member.ReflectedType;
        }

        public static MemberInfo GetMemberInfo<TClass, TField>(this Expression<Func<TClass, TField>> field)
        {
            return (field.Body as MemberExpression ?? ((UnaryExpression)field.Body).Operand as MemberExpression).Member;
        }

        public static string GetFullyQualifiedMemberName<TClass, TField>(this Expression<Func<TClass, TField>> field)
        {
            MemberExpression memberExp;
            if (!TryFindMemberExpression(field.Body, out memberExp))
                return string.Empty;

            var memberNames = new Stack<string>();
            do
            {
                memberNames.Push(memberExp.Member.Name);
            }
            while (TryFindMemberExpression(memberExp.Expression, out memberExp));

            return string.Join(".", memberNames.ToArray());
        }

        private static bool TryFindMemberExpression(Expression exp, out MemberExpression memberExp)
        {
            memberExp = exp as MemberExpression;
            if (memberExp != null)
            {
                // heyo! that was easy enough
                return true;
            }

            // if the compiler created an automatic conversion,
            // it'll look something like...
            // obj => Convert(obj.Property) [e.g., int -> object]
            // OR:
            // obj => ConvertChecked(obj.Property) [e.g., int -> long]
            // ...which are the cases checked in IsConversion
            if (IsConversion(exp) && exp is UnaryExpression)
            {
                memberExp = ((UnaryExpression)exp).Operand as MemberExpression;
                if (memberExp != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsConversion(Expression exp)
        {
            return (
                exp.NodeType == ExpressionType.Convert ||
                exp.NodeType == ExpressionType.ConvertChecked
            );
        }
    }
}
