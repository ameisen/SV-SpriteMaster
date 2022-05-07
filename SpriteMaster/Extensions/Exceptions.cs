using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using static SpriteMaster.Runtime;

namespace SpriteMaster.Extensions;

internal static class Exceptions {
	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.ErrorPath)]
	internal static void PrintTrace<T>(this T exception, [CallerMemberName] string caller = null!) where T : Exception => Debug.Trace(exception: exception, caller: caller);

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.ErrorPath)]
	internal static void PrintInfo<T>(this T exception, [CallerMemberName] string caller = null!) where T : Exception => Debug.Info(exception: exception, caller: caller);

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.ErrorPath)]
	internal static void PrintWarning<T>(this T exception, [CallerMemberName] string caller = null!) where T : Exception => Debug.Warning(exception: exception, caller: caller);

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.ErrorPath)]
	internal static void PrintError<T>(this T exception, [CallerMemberName] string caller = null!) where T : Exception => Debug.Error(exception: exception, caller: caller);

	[MethodImpl(MethodImpl.ErrorPath)]
	internal static string BuildArgumentException(string name, in object? value) => $"'{name}' = '{((value is null) ? "null" : value.GetType().FullName)}'";
}
