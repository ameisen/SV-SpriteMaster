﻿using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class Common {
		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static void ConditionalSet<T> (this ref T obj, bool conditional, in T value) where T : struct {
			if (conditional) {
				obj = value;
			}
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static void ConditionalSet<T> (this ref T obj, in T? value) where T : struct {
			if (value.HasValue) {
				obj = value.Value;
			}
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static WeakReference<T> MakeWeak<T> (this T obj) where T : class => new WeakReference<T>(obj);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static int ClampDimension (this int value) => Math.Min(value, Config.ClampDimension);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static Vector2I ClampDimension (this Vector2I value) => value.Min(Config.ClampDimension);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static void Swap<T> (ref T l, ref T r) {
			var temp = l;
			l = r;
			r = temp;
		}
	}
}
