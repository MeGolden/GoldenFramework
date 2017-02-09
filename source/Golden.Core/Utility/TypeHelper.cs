﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Golden.Utility
{
    public static class TypeHelper
	{
		public static bool IsNullableValueType(Type type)
		{
			return ((type.IsGenericType && type.IsGenericTypeDefinition == false) && (typeof(Nullable<>) == type.GetGenericTypeDefinition()));
		}
		public static bool IsGenericType(Type type, Type genType)
		{
			return ((type.IsGenericType && type.IsGenericTypeDefinition == false) && (genType == type.GetGenericTypeDefinition()));
		}
		public static bool IsBoolean(Type type)
		{
			return (type == typeof(bool) || type == typeof(bool?));
		}
		public static bool IsDateTime(Type type)
		{
			return (type == typeof(DateTime) || type == typeof(DateTime?));
		}
		public static bool IsNumeric(Type type)
		{
			return (IsInteger(type) || IsFloat(type));
		}
		public static bool IsInteger(Type type)
		{
			type = GetNonNullableType(type);
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
			}
			return false;
		}
		public static bool IsFloat(Type type)
		{
			type = GetNonNullableType(type);
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Decimal:
				case TypeCode.Double:
				case TypeCode.Single:
					return true;
			}
			return false;
		}
		public static bool IsDecimalType(Type type)
		{
			return (type == typeof(decimal) || type == typeof(decimal?));
		}
		public static bool IsAnonymousType(Type type)
		{
			return (type.IsClass && type.IsSealed && type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false) && type.FullName.Contains("Anonymous"));
		}
		public static Type GetElementType(Type type)
		{
			Type ienum = FindIEnumerable(type);
			if (ienum == null) return null;
			return ienum.GetGenericArguments()[0];
		}
		public static Type FindIEnumerable(Type type)
		{
			if (type == null || type == typeof(string)) return null;
			if (type.IsArray) return typeof(IEnumerable<>).MakeGenericType(type.GetElementType());
			if (type.IsGenericType)
			{
				foreach (var arg in type.GetGenericArguments())
				{
					Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
					if (ienum.IsAssignableFrom(type)) return ienum;
				}
			}

			Type[] ifaces = type.GetInterfaces();
			if (ifaces.Length > 0)
			{
				foreach (var iface in ifaces)
				{
					Type ienum = FindIEnumerable(iface);
					if (ienum != null) return ienum;
				}
			}

			if (type.BaseType != null && type.BaseType != typeof(object)) return FindIEnumerable(type.BaseType);

			return null;
		}
		public static bool IsEnumerable(Type type)
		{
			return (FindIEnumerable(type) != null);
		}
		public static bool IsVirtualProperty(PropertyInfo info)
		{
			return (info.GetGetMethod(true) ?? info.GetSetMethod(true)).IsVirtual;
		}
		public static bool IsStructure(Type type)
		{
			return (type.IsValueType && type.IsEnum == false);
		}
        private static readonly Lazy<MethodInfo> mGetDefault = new Lazy<MethodInfo>(()=>
        {
            return typeof(TypeHelper).GetMember(nameof(GetDefault), BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Static)
                .OfType<MethodInfo>()
                .FirstOrDefault(m => m.IsGenericMethod);
        });
        private static T GetDefault<T>()
        {
            return default(T);
        }
		public static object GetDefault(Type type)
		{
            return mGetDefault.Value.MakeGenericMethod(type).Invoke(null, null);
		}
		public static Type GetNonNullableType(Type type)
		{
			while (IsNullableValueType(type)) type = System.Nullable.GetUnderlyingType(type);
			return type;
		}
		public static bool CanBeNull(Type type)
		{
            return (GetDefault(type) == null);
		}
		public static object GetMemberValue(MemberInfo member, object obj)
		{
			if (member.MemberType.HasFlag(MemberTypes.Field)) return ((FieldInfo)member).GetValue(obj);
			if (member.MemberType.HasFlag(MemberTypes.Property)) return ((PropertyInfo)member).GetValue(obj, null);
			throw new InvalidOperationException();
		}
		public static object GetMemberValue(string memberName, object obj)
		{
			var member = obj.GetType().GetMember(memberName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).FirstOrDefault();
			return GetMemberValue(member, obj);
		}
		public static object GetStaticMemberValue(string memberName, Type type)
		{
			var member = type.GetMember(memberName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).FirstOrDefault();
			return GetMemberValue(member, null);
		}
		public static void SetMemberValue(MemberInfo member, object value, object obj)
		{
			if (member.MemberType.HasFlag(MemberTypes.Field))
				((FieldInfo)member).SetValue(obj, value);
			else if (member.MemberType.HasFlag(MemberTypes.Property))
				((PropertyInfo)member).SetValue(obj, value, null);
			else
				throw new InvalidOperationException();
		}
		public static void SetMemberValue(string memberName, object value, object obj)
		{
			var member = obj.GetType().GetMember(memberName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).FirstOrDefault();
			SetMemberValue(member, value, obj);
		}
		public static Type GetMemberType(MemberInfo member)
		{
			if (member.MemberType.HasFlag(MemberTypes.Constructor)) return member.DeclaringType;
			if (member.MemberType.HasFlag(MemberTypes.Field)) return ((FieldInfo)member).FieldType;
			if (member.MemberType.HasFlag(MemberTypes.Method)) return ((MethodInfo)member).ReturnType;
			if (member.MemberType.HasFlag(MemberTypes.Property)) return ((PropertyInfo)member).PropertyType;
			return null;
		}
		public static MemberInfo[] GetMembers(Expression members)
		{
			var lambda = members as LambdaExpression;
			if (lambda == null) throw new ArgumentOutOfRangeException(nameof(members));
			var result = new List<MemberInfo>();
			var exp = lambda.Body;
			if (exp is UnaryExpression) exp = ((UnaryExpression)exp).Operand;
			if (exp.NodeType == ExpressionType.MemberAccess)
			{
				result.Add(((MemberExpression)exp).Member);
			}
			else if (exp.NodeType == ExpressionType.New)
			{
				result.AddRange(((NewExpression)exp).Arguments.OfType<MemberExpression>().Select(m => m.Member));
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(members));
			}
			return result.ToArray();
		}
		public static MemberInfo[] GetMembers<T>(Expression<Func<T, object>> members)
		{
			return GetMembers(members);
		}
	}
}
