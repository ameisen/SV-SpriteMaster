﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace SpriteMaster.Types {
	[DebuggerDisplay("{Value}")]

	[StructLayout(LayoutKind.Sequential, Pack = sizeof(ulong), Size = sizeof(ulong))]
	public struct VolatileULong :
		IComparable,
		IComparable<ulong>,
		IComparable<VolatileULong>,
		IEquatable<ulong>,
		IEquatable<VolatileULong>
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public /*readonly*/ int CompareTo (object obj) => obj switch {
			ulong value => CompareTo(value),
			VolatileULong value => CompareTo(value),
			_ => throw new ArgumentException($"{obj} is neither type {typeof(ulong)} nor {typeof(VolatileULong)}"),
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public /*readonly*/ int CompareTo (ulong other) => Value.CompareTo(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public /*readonly*/ int CompareTo (/*in*/ VolatileULong other) => Value.CompareTo(other.Value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override /*readonly*/ int GetHashCode() => Value.GetHashCode();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override /*readonly*/ bool Equals (object obj) => obj switch {
			ulong value => Equals(value),
			VolatileULong value => Equals(value),
			_ => throw new ArgumentException($"{obj} is neither type {typeof(ulong)} nor {typeof(VolatileULong)}"),
		};

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public /*readonly*/ bool Equals (ulong other) => Value.Equals(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public /*readonly*/ bool Equals (/*in*/ VolatileULong other) => Value.Equals(other.Value);

		private long _Value;
		public ulong Value {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			/*readonly*/ get => unchecked((ulong)Interlocked.Read(ref _Value));
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Interlocked.Exchange(ref _Value, unchecked((long)value));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public VolatileULong (ulong value = default) : this() => Value = value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator ulong (in VolatileULong value) => value.Value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator VolatileULong (ulong value) => new VolatileULong(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in VolatileULong lhs, in VolatileULong rhs) => lhs.Value == rhs.Value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in VolatileULong lhs, in VolatileULong rhs) => lhs.Value != rhs.Value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in VolatileULong lhs, ulong rhs) => lhs.Value == rhs;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in VolatileULong lhs, ulong rhs) => lhs.Value != rhs;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (ulong lhs, in VolatileULong rhs) => lhs == rhs.Value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (ulong lhs, in VolatileULong rhs) => lhs != rhs.Value;
	}
}
