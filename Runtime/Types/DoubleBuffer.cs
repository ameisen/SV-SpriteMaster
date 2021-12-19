using SpriteMaster.Extensions;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SpriteMaster.Types {
	internal sealed class DoubleBuffer<T> {
		internal const uint BufferCount = 2;
		internal const int StartingIndex = 0;

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		private uint GetIndex (uint index) {
			return (index & 1U);
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		private T GetBuffer(uint index) {
			return (GetIndex(index) == 0U) ? Buffer0 : Buffer1;
		}

		internal static readonly Type<T> BufferType = Type<T>.This;

		// Regarding bounds checking on x86 and x64
		// https://stackoverflow.com/questions/16713076/array-bounds-check-efficiency-in-net-4-and-above
		private readonly T Buffer0;
		private readonly T Buffer1;

		private volatile int _CurrentBuffer = StartingIndex;

#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile
		private uint CurrentBufferAtomic => unchecked((uint)Volatile.Read(ref _CurrentBuffer));
#pragma warning restore CS0420

		internal uint CurrentBuffer {
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get => GetIndex(CurrentBufferAtomic);
		}

		internal uint NextBuffer {
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get => GetIndex(CurrentBufferAtomic + 1U);
		}

		internal T Current {
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get => GetBuffer(CurrentBufferAtomic);
		}
		internal T Next {
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get => GetBuffer(CurrentBufferAtomic + 1U);
		}

		internal T this[int index] => GetBuffer(unchecked((uint)index));
		internal T this[uint index] => GetBuffer(index);

		internal Tuple<T, T> Both {
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get {
				var currentIndex = CurrentBufferAtomic;
				return Tuple.Create(
					GetBuffer(currentIndex),
					GetBuffer(currentIndex + 1U)
				);
			}
		}

		internal Tuple<T, T> All => Both;
		internal DoubleBuffer(in T element0, in T element1) {
			Buffer0 = element0;
			Buffer1 = element1;
			Thread.MemoryBarrier();
		}

		internal DoubleBuffer (T[] elements) : this(
			elements[0],
			elements[1]
		) { }

		internal DoubleBuffer (params object[] parameters) : this(
			// We do, indeed, want to create two seperate instances.
			Reflection.CreateInstance<T>(parameters),
			Reflection.CreateInstance<T>(parameters)
		) { }

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal void Swap() => Interlocked.Increment(ref _CurrentBuffer);

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static implicit operator T (in DoubleBuffer<T> buffer) => buffer.Current;
	}
}
