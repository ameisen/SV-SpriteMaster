﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SpriteMaster.Extensions {
	internal static class Untraced {
		[DebuggerStepThrough, DebuggerHidden()]
		internal static bool IsUntraced (this MethodBase method) => method != null && (method.IsDefined(typeof(DebuggerStepThroughAttribute), true) || method.IsDefined(typeof(DebuggerHiddenAttribute), true));

		[DebuggerStepThrough, DebuggerHidden()]
		internal static string GetStackTrace (this Exception e) {
			var tracedStrings = new List<string>();
			foreach (var frame in new StackTrace(e, true).GetFrames()) {
				if (!frame.GetMethod().IsUntraced()) {
					tracedStrings.Add(new StackTrace(frame).ToString());
				}
			}

			return string.Concat(tracedStrings);
		}
	}
}
