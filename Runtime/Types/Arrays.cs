using JetBrains.Annotations;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

using JBPure = JetBrains.Annotations.PureAttribute;
using Pure = System.Diagnostics.Contracts.PureAttribute;

namespace SpriteMaster.Types {
	public static class Arrays {
		internal static class EmptyArrayStatic<T> {
			[ImmutableObject(true), NotNull]
			internal static readonly T[] Value = new T[0];
		}

		[Pure, JBPure, MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ImmutableObject(true), NotNull]
		public static T[] Empty<T> () => EmptyArrayStatic<T>.Value;
	}

	public static class Array<T> {
		[ImmutableObject(true), NotNull]
		public static readonly T[] Empty = Arrays.Empty<T>();
	}
}
