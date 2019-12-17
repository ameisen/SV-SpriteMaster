using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace xBRZNet.Common {
	public static class IMath {
		[DebuggerStepThrough]
		private ref struct Argument<T> where T : unmanaged {
			public readonly T Value;
			public readonly string Name;

			[DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal Argument (T value, string name) {
				Value = value;
				Name = name;
			}

			[DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static implicit operator T (in Argument<T> arg) {
				return arg.Value;
			}
		}

		[DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Argument<T> AsArg<T> (this T value, string name) where T : unmanaged {
			return new Argument<T>(value, name);
		}

		[DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static string GetCheckNotNegativeMessage<T> (in Argument<T> argument, string memberName) where T : unmanaged {
			return memberName != null ?
				$"Argument {argument.Name} with value {argument.Value} from method {memberName} was less than zero" :
				$"Argument {argument.Name} with value {argument.Value} was less than zero";
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CheckNotNegative (
			this in Argument<int> argument,
			[CallerMemberName] string memberName = null
		) {
			if (argument < 0) {
				throw new ArgumentOutOfRangeException(GetCheckNotNegativeMessage(argument, memberName));
			}
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CheckNotNegative (
			this in Argument<long> argument,
			[CallerMemberName] string memberName = null
		) {
			if (argument < 0) {
				throw new ArgumentOutOfRangeException(GetCheckNotNegativeMessage(argument, memberName));
			}
		}

		// Precalculate square roots.
		private const int SQRT_MIN = 0;
		private const int SQRT_MAX = 100;
		private static readonly int[] SQRT_TABLE = new int[SQRT_MAX - SQRT_MIN];
		static IMath () {
			for (int i = 0; i < SQRT_TABLE.Length; ++i) {
				SQRT_TABLE[i] = (int)Math.Sqrt(i + SQRT_MIN);
			}
		}

		// Truncating Square Root
		internal static int Sqrt (this int value) {
			value.AsArg(nameof(value)).CheckNotNegative();

			if (value >= SQRT_MIN && value < SQRT_MAX) {
				return SQRT_TABLE[value];
			}

			return unchecked((int)Math.Sqrt(value));
		}

		// Truncating Square Root
		internal static uint Sqrt (this uint value) {
			if (value >= SQRT_MIN && value < SQRT_MAX) {
				return unchecked((uint)SQRT_TABLE[value]);
			}

			return unchecked((uint)Math.Sqrt(value));
		}

		// Truncating Square Root
		internal static long Sqrt (this long value) {
			value.AsArg(nameof(value)).CheckNotNegative();

			if (value >= SQRT_MIN && value < SQRT_MAX) {
				return (long)SQRT_TABLE[value];
			}

			return unchecked((long)Math.Sqrt(value));
		}

		// Truncating Square Root
		internal static ulong Sqrt (this ulong value) {
			if (value >= SQRT_MIN && value < SQRT_MAX) {
				return unchecked((ulong)SQRT_TABLE[value]);
			}

			return unchecked((ulong)Math.Sqrt(value));
		}
	}
}
