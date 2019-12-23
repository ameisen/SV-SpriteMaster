using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class Reflection {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static T AddRef<T> (this T type) where T : Type {
			return (T)type.MakeByRefType();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static T RemoveRef<T> (this T type) where T : Type {
			return (T)(type.IsByRef ? type.GetElementType() : type);
		}

		internal static string GetFullName (this MethodBase method) {
			return method.DeclaringType.Name + "::" + method.Name;
		}

		internal static string GetCurrentMethodName () {
			return MethodBase.GetCurrentMethod().GetFullName();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static T GetValue<T> (this FieldInfo field, object instance) {
			var result = field.GetValue(instance);
			return (T)result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool GetAttribute<T> (this MemberInfo member, out T attribute) where T : Attribute {
			attribute = member.GetCustomAttribute<T>();
			return attribute != null;
		}
	}
}
