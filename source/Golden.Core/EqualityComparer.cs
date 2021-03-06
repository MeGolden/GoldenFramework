﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Golden
{
    public sealed class EqualityComparer<T> : IEqualityComparer<T>, System.Collections.IEqualityComparer
	{
        private static readonly Lazy<MethodInfo> mObjGetHashCode = new Lazy<MethodInfo>(() =>
        {
            return typeof(object).GetMethod("GetHashCode", BindingFlags.Public | BindingFlags.Instance);
        });
        private readonly Func<T, T, bool> _Comparer;
		private readonly Func<T, int> fnGetHashCode;

		public EqualityComparer(Func<T, T, bool> comparer):this(comparer, null)
		{
		}
		public EqualityComparer(Func<T, T, bool> comparer, Func<T, int> fnGetHashCode)
		{
			_Comparer = comparer;
			this.fnGetHashCode = fnGetHashCode;
		}
        bool System.Collections.IEqualityComparer.Equals(object x, object y)
        {
            if (!(x is T) || !(y is T)) return false;
            return Equals((T)x, (T)y);
        }
        public bool Equals(T x, T y)
		{
			return _Comparer(x, y);
		}
		int IEqualityComparer<T>.GetHashCode(T obj)
		{
			if (fnGetHashCode != null) return fnGetHashCode(obj);
			return obj.GetHashCode();
		}
        int System.Collections.IEqualityComparer.GetHashCode(object obj)
        {
            if (!(obj is T)) return 0;
            return ((IEqualityComparer<T>)this).GetHashCode((T)obj);
        }
        public static EqualityComparer<T> ByProperty<TProperty>(Expression<Func<T, TProperty>> property)
        {
            var eqMember = Utility.Utilities.GetMember(property);
            //Comparer
            var xParam = Expression.Parameter(typeof(T), "x");
            var yParam = Expression.Parameter(typeof(T), "y");
            var xMAcc = Expression.MakeMemberAccess(xParam, eqMember);
            var yMAcc = Expression.MakeMemberAccess(yParam, eqMember);
            var eqFunc = Expression.Equal(xMAcc, yMAcc);
            var mComparer = Expression.Lambda<Func<T, T, bool>>(eqFunc, xParam, yParam).Compile();
            //GetHashCode
            var oParam = Expression.Parameter(typeof(T), "o");
            var oMAcc = Expression.MakeMemberAccess(oParam, eqMember);
            var fnObjGetHashCode = Expression.Call(oMAcc, mObjGetHashCode.Value);
            var mGetHashCode = Expression.Lambda<Func<T, int>>(fnObjGetHashCode, oParam).Compile();

            return (EqualityComparer<T>)Activator.CreateInstance(typeof(EqualityComparer<>).MakeGenericType(typeof(T)), new object[] { mComparer, mGetHashCode });
        }
    }
}
