using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions;

internal static class Untraced {
	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(Runtime.MethodImpl.ErrorPath)]
	internal static bool IsUntraced(this MethodBase method) => method is not null && (method.IsDefined(typeof(DebuggerStepThroughAttribute), true) || method.IsDefined(typeof(DebuggerHiddenAttribute), true));

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(Runtime.MethodImpl.ErrorPath)]
	internal static string GetStackTrace(this Exception e) {
		var tracedStrings = new List<string>();
		foreach (var frame in new StackTrace(e, true).GetFrames()) {
			if (!frame.GetMethod()?.IsUntraced() ?? false) {
				tracedStrings.Add(new StackTrace(frame).ToString());
			}
		}

		return string.Concat(tracedStrings);
	}
}
