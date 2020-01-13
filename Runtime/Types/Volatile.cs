using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SpriteMaster.Types {
	[DebuggerDisplay("{Value}")]
	public struct Volatile<T> :
		IComparable,
		IComparable<T>,
		IComparable<Volatile<T>>,
		IEquatable<T>,
		IEquatable<Volatile<T>>
		where T : IComparable, IComparable<T>, IEquatable<T> {

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static T Read(ref T value) {
			switch (value) {
				case double v:
					return (T)(object)Thread.VolatileRead(ref v);
				case UIntPtr v:
					return (T)(object)Thread.VolatileRead(ref v);
				case IntPtr v:
					return (T)(object)Thread.VolatileRead(ref v);
				case uint v:
					return (T)(object)Thread.VolatileRead(ref v);
				case ushort v:
					return (T)(object)Thread.VolatileRead(ref v);
				case float v:
					return (T)(object)Thread.VolatileRead(ref v);
				case long v:
					return (T)(object)Thread.VolatileRead(ref v);
				case sbyte v:
					return (T)(object)Thread.VolatileRead(ref v);
				case byte v:
					return (T)(object)Thread.VolatileRead(ref v);
				case short v:
					return (T)(object)Thread.VolatileRead(ref v);
				case int v:
					return (T)(object)Thread.VolatileRead(ref v);
				case object v:
					return (T)Thread.VolatileRead(ref v);
				default:
					throw new Exception($"Type {value.GetType()} is not a volatile-capable type");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Write (ref T value, T setValue) {
			switch (value) {
				case double v:
					Thread.VolatileWrite(ref v, (double)(object)setValue); break;
				case UIntPtr v:
					Thread.VolatileWrite(ref v, (UIntPtr)(object)setValue); break;
				case IntPtr v:
					Thread.VolatileWrite(ref v, (IntPtr)(object)setValue); break;
				case uint v:
					Thread.VolatileWrite(ref v, (uint)(object)setValue); break;
				case ushort v:
					Thread.VolatileWrite(ref v, (ushort)(object)setValue); break;
				case float v:
					Thread.VolatileWrite(ref v, (float)(object)setValue); break;
				case long v:
					Thread.VolatileWrite(ref v, (long)(object)setValue); break;
				case sbyte v:
					Thread.VolatileWrite(ref v, (sbyte)(object)setValue); break;
				case byte v:
					Thread.VolatileWrite(ref v, (byte)(object)setValue); break;
				case short v:
					Thread.VolatileWrite(ref v, (short)(object)setValue); break;
				case int v:
					Thread.VolatileWrite(ref v, (int)(object)setValue); break;
				case object v:
					Thread.VolatileWrite(ref v, setValue); break;
				default:
					throw new Exception($"Type {value.GetType()} is not a volatile-capable type");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int CompareTo (object obj) {
			switch (obj) {
				case T value: return CompareTo(value);
				case Volatile<T> value: return CompareTo(value);
			}
			throw new ArgumentException($"{obj} is neither type {typeof(T)} nor {typeof(Volatile<T>)}");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int CompareTo (T other) {
			return Value.CompareTo(other);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int CompareTo (Volatile<T> other) {
			return Value.CompareTo(other.Value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() {
			return Value.GetHashCode();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals (object obj) {
			switch (obj) {
				case T value: return Equals(value);
				case Volatile<T> value: return Equals(value);
			}
			throw new ArgumentException($"{obj} is neither type {typeof(T)} nor {typeof(Volatile<T>)}");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals (T other) {
			return Value.Equals(other);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals (Volatile<T> other) {
			return Value.Equals(other.Value);
		}

		private T _Value;
		public T Value {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			/*readonly*/
			get => Read(ref _Value);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Write(ref _Value, value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Volatile(T value = default) {
			_Value = value;
			Value = value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator T (in Volatile<T> value) {
			return value.Value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator Volatile<T> (T value) {
			return new Volatile<T>(value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Volatile<T> lhs, in Volatile<T> rhs) {
			return !rhs.Equals(lhs);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Volatile<T> lhs, in Volatile<T> rhs) {
			return !rhs.Equals(lhs);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (in Volatile<T> lhs, T rhs) {
			return !rhs.Equals(lhs);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (in Volatile<T> lhs, T rhs) {
			return !rhs.Equals(lhs);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator == (T lhs, in Volatile<T> rhs) {
			return !rhs.Equals(lhs);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator != (T lhs, in Volatile<T> rhs) {
			return !rhs.Equals(lhs);
		}
	}
}
