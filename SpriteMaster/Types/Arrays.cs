using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types;

internal static class Arrays {
	private static class EmptyArrayStatic<T> {
		[ImmutableObject(true)]
		internal static readonly T[] Value = Array.Empty<T>();
	}

	[Pure, MethodImpl(Runtime.MethodImpl.Hot)]
	[ImmutableObject(true)]
	internal static T[] Empty<T>() => EmptyArrayStatic<T>.Value;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static T[] Singleton<T>(T value) => new[] { value };

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static T[] Of<T>(params T[] values) => values;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static MemoryStream Stream(this byte[] data) => new(data, 0, data.Length, true, true);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	// ReSharper disable once MethodOverloadWithOptionalParameter
	internal static MemoryStream Stream(this byte[] data, int offset = 0, int length = -1, FileAccess access = FileAccess.ReadWrite) {
		if (length == -1) {
			length = data.Length - offset;
		}
		return new MemoryStream(data, offset, length, (access != FileAccess.Read), true);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static T[] Reverse<T>(this T[] array) {
		//Contract.AssertNotNull(array);
		Array.Reverse(array);
		return array;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static T[] Reversed<T>(this T[] array) {
		//Contract.AssertNotNull(array);
		var result = (T[])array.Clone();
		Array.Reverse(result);
		return result;
	}
}

internal static class Arrays<T> {
	[ImmutableObject(true)]
	internal static readonly T[] Empty = Arrays.Empty<T>();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static T[] Singleton(T value) => Arrays.Singleton(value);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static T[] Of(params T[] values) => Arrays.Of(values);
}
