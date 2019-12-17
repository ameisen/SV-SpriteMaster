using System;
using System.Reflection;

namespace SpriteMaster.Extensions {
	internal static class Reflection {
		internal static T AddRef<T> (this T type) where T : Type {
			return (T)type.MakeByRefType();
		}

		internal static T RemoveRef<T> (this T type) where T : Type {
			return (T)(type.IsByRef ? type.GetElementType() : type);
		}

		internal static string GetFullName (this MethodBase method) {
			return method.DeclaringType.Name + "::" + method.Name;
		}

		internal static string GetCurrentMethodName () {
			return MethodBase.GetCurrentMethod().GetFullName();
		}

		internal static T GetValue<T> (this FieldInfo field, object instance) {
			var result = field.GetValue(instance);
			return (T)result;
		}
	}
}
