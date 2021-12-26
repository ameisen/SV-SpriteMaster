using System;
using System.Reflection;

namespace SpriteMaster.Extensions;

static class DelegateExt {
	internal static T CreateDelegate<T>(this MethodInfo method) where T : Delegate => (T)Delegate.CreateDelegate(typeof(T), method);
}
