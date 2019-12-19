﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class Exceptions {
		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		internal static void PrintInfo<T> (this T exception, [CallerMemberName] string caller = null) where T : Exception {
			Debug.Info(exception: exception, caller: caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		internal static void PrintWarning<T> (this T exception, [CallerMemberName] string caller = null) where T : Exception {
			Debug.Warning(exception: exception, caller: caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		internal static void PrintError<T> (this T exception, [CallerMemberName] string caller = null) where T : Exception {
			Debug.Error(exception: exception, caller: caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		internal static void Print<T> (this T exception, [CallerMemberName] string caller = null) where T : Exception {
			exception.PrintWarning(caller);
		}
	}
}