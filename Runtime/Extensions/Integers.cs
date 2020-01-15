using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	public static class Integers {
		[DebuggerStepThrough, DebuggerHidden()]
		[Conditional("DEBUG")]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void CheckBit<T>(int bit) where T : unmanaged {
			unchecked {
#if DEBUG
				if (bit >= sizeof(T) * 8) {
					throw new ArgumentOutOfRangeException(nameof(bit));
				}
#endif
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetBit(this bool value, int bit) {
			unchecked {
#if DEBUG
				if (bit != 0) {
					throw new ArgumentOutOfRangeException(nameof(bit));
				}
#endif
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetBit (this sbyte value, int bit) {
			unchecked {
				CheckBit<sbyte>(bit);
				return (sbyte)((sbyte)(value >> bit) & 1) != 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetBit (this byte value, int bit) {
			unchecked {
				CheckBit<byte>(bit);
				return (byte)((byte)(value >> bit) & 1) != 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetBit (this short value, int bit) {
			unchecked {
				CheckBit<short>(bit);
				return (short)((short)(value >> bit) & 1) != 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetBit (this ushort value, int bit) {
			unchecked {
				CheckBit<ushort>(bit);
				return (ushort)((ushort)(value >> bit) & 1) != 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetBit (this int value, int bit) {
			unchecked {
				CheckBit<int>(bit);
				return ((value >> bit) & 1) != 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetBit (this uint value, int bit) {
			unchecked {
				CheckBit<uint>(bit);
				return ((value >> bit) & 1) != 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetBit (this long value, int bit) {
			unchecked {
				CheckBit<long>(bit);
				return ((value >> bit) & 1) != 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetBit (this ulong value, int bit) {
			unchecked {
				CheckBit<ulong>(bit);
				return ((value >> bit) & 1) != 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetBit (this in BigInteger value, int bit) {
			unchecked {
				return ((value >> bit) & 1) != 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SetBit(this ref bool value, int bit) {
			unchecked {
#if DEBUG
				if (bit != 0) {
					throw new ArgumentOutOfRangeException(nameof(bit));
				}
#endif
				value = true;
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static sbyte SetBit (this ref sbyte value, int bit) {
			unchecked {
				CheckBit<sbyte>(bit);
				value |= (sbyte)(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte SetBit (this ref byte value, int bit) {
			unchecked {
				CheckBit<byte>(bit);
				value |= (byte)(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short SetBit (this ref short value, int bit) {
			unchecked {
				CheckBit<short>(bit);
				value |= (short)(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort SetBit (this ref ushort value, int bit) {
			unchecked {
				CheckBit<ushort>(bit);
				value |= (ushort)(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SetBit (this ref int value, int bit) {
			unchecked {
				CheckBit<int>(bit);
				value |= (int)(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint SetBit (this ref uint value, int bit) {
			unchecked {
				CheckBit<uint>(bit);
				value |= (uint)(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long SetBit (this ref long value, int bit) {
			unchecked {
				CheckBit<long>(bit);
				value |= (long)(1L << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong SetBit (this ref ulong value, int bit) {
			unchecked {
				CheckBit<ulong>(bit);
				value |= (ulong)(1ul << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BigInteger SetBit (this ref BigInteger value, int bit) {
			unchecked {
				CheckBit<ulong>(bit);
				value |= BigInteger.One << bit;
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ClearBit (this ref bool value, int bit) {
			unchecked {
#if DEBUG
				if (bit != 0) {
					throw new ArgumentOutOfRangeException(nameof(bit));
				}
#endif
				value = false;
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static sbyte ClearBit (this ref sbyte value, int bit) {
			unchecked {
				CheckBit<sbyte>(bit);
				value &= (sbyte)~(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte ClearBit (this ref byte value, int bit) {
			unchecked {
				CheckBit<byte>(bit);
				value &= (byte)~(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short ClearBit (this ref short value, int bit) {
			unchecked {
				CheckBit<short>(bit);
				value &= (short)~(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort ClearBit (this ref ushort value, int bit) {
			unchecked {
				CheckBit<ushort>(bit);
				value &= (ushort)~(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int ClearBit (this ref int value, int bit) {
			unchecked {
				CheckBit<int>(bit);
				value &= (int)~(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint ClearBit (this ref uint value, int bit) {
			unchecked {
				CheckBit<uint>(bit);
				value &= (uint)~(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long ClearBit (this ref long value, int bit) {
			unchecked {
				CheckBit<long>(bit);
				value &= (long)~(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong ClearBit (this ref ulong value, int bit) {
			unchecked {
				CheckBit<ulong>(bit);
				value &= (ulong)~(1 << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static BigInteger ClearBit (this ref BigInteger value, int bit) {
			unchecked {
				CheckBit<ulong>(bit);
				// TODO : I don't think this will necessarily work, as the BigInteger shifted might not be large eonugh to mask against the entirety of 'value'.
				value &= ~(BigInteger.One << bit);
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SetBit (this ref bool value, int bit, bool condition) {
			unchecked {
#if DEBUG
				if (bit != 0) {
					throw new ArgumentOutOfRangeException(nameof(bit));
				}
#endif
				value = condition;
				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static sbyte SetBit (this ref sbyte value, int bit, bool condition) {
			unchecked {
				CheckBit<sbyte>(bit);
				var mask = (sbyte)(1 << bit);
				var flag = condition ? sbyte.MaxValue : 0;
				var Value = value;
				Value = (sbyte)((sbyte)(Value & (sbyte)~mask) | (sbyte)(flag & mask));
				return value = Value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte SetBit (this ref byte value, int bit, bool condition) {
			unchecked {
				CheckBit<byte>(bit);
				var mask = (byte)(1 << bit);
				var flag = condition ? byte.MaxValue : 0;
				var Value = value;
				Value = (byte)((byte)(Value & (byte)~mask) | (byte)(flag & mask));
				return value = Value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static short SetBit (this ref short value, int bit, bool condition) {
			unchecked {
				CheckBit<short>(bit);
				var mask = (ushort)(1 << bit);
				var flag = condition ? ushort.MaxValue : 0;
				var Value = (ushort)value;
				Value = (ushort)((ushort)(Value & (ushort)~mask) | (ushort)(flag & mask));
				return value = (short)Value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ushort SetBit (this ref ushort value, int bit, bool condition) {
			unchecked {
				CheckBit<ushort>(bit);
				var mask = (ushort)(1 << bit);
				var flag = condition ? ushort.MaxValue : 0;
				var Value = value;
				Value = (ushort)((ushort)(Value & (ushort)~mask) | (ushort)(flag & mask));
				return value = Value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SetBit (this ref int value, int bit, bool condition) {
			unchecked {
				CheckBit<int>(bit);
				var mask = (uint)(1 << bit);
				var flag = condition ? uint.MaxValue : 0;
				var Value = (uint)value;
				Value = (uint)((uint)(Value & (uint)~mask) | (uint)(flag & mask));
				return value = (int)Value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint SetBit (this ref uint value, int bit, bool condition) {
			unchecked {
				CheckBit<uint>(bit);
				var mask = (uint)(1 << bit);
				var flag = condition ? uint.MaxValue : 0;
				var Value = value;
				Value = (uint)((uint)(Value & (uint)~mask) | (uint)(flag & mask));
				return value = Value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long SetBit (this ref long value, int bit, bool condition) {
			unchecked {
				CheckBit<long>(bit);
				var mask = (ulong)(1ul << bit);
				var flag = condition ? ulong.MaxValue : 0;
				var Value = (ulong)value;
				Value = (ulong)((ulong)(Value & (ulong)~mask) | (ulong)(flag & mask));
				return value = (long)Value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong SetBit (this ref ulong value, int bit, bool condition) {
			unchecked {
				CheckBit<ulong>(bit);
				var mask = (ulong)(1ul << bit);
				var flag = condition ? ulong.MaxValue : 0;
				var Value = value;
				Value = (ulong)((ulong)(Value & (ulong)~mask) | (ulong)(flag & mask));
				return value = Value;
			}
		}
	}
}
