using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types {
	internal static class Extensions {
		internal static FixedSpan<T> AsFixedSpan<T>(this T[] data) where T : unmanaged {
			return new FixedSpan<T>(data);
		}

		internal static FixedSpan<U> AsFixedSpan<T, U>(this T[] data) where T : unmanaged where U : unmanaged {
			using var intermediateSpan = new FixedSpan<T>(data);
			return intermediateSpan.As<U>();
		}

		internal static FixedSpan<T> AsFixedSpan<T>(this T[] data, int length) where T : unmanaged {
			return new FixedSpan<T>(data, length);
		}

		internal static FixedSpan<U> AsFixedSpan<T, U>(this T[] data, int length) where T : unmanaged where U : unmanaged {
			using var intermediateSpan = new FixedSpan<T>(data, length);
			return intermediateSpan.As<U>();
		}
	}

	[ImmutableObject(true)]
	internal struct FixedSpan<T> : IDisposable where T : unmanaged {
		private sealed class CollectionHandle : IDisposable {
			private GCHandle? Handle;

			[MethodImpl(Runtime.MethodImpl.Optimize)]
			internal CollectionHandle (GCHandle handle) {
				Handle = handle;
			}

			[MethodImpl(Runtime.MethodImpl.Optimize)]
			~CollectionHandle() => Dispose();

			[MethodImpl(Runtime.MethodImpl.Optimize)]
			public void Dispose() {
				if (Handle.HasValue) {
					Handle.Value.Free();
					Handle = null;
				}
			}
		}

		private CollectionHandle Handle;
		private WeakReference PinnedObject;
		internal readonly IntPtr Pointer;
		internal readonly int Length;
		private readonly int Size;

		private readonly static int TypeSize = Marshal.SizeOf(typeof(T));

		[Pure, MethodImpl(Runtime.MethodImpl.Optimize)]
		private readonly int GetOffset(int index) {
#if DEBUG
			if (index < 0 || index >= Length) {
				throw new IndexOutOfRangeException($"{nameof(index)}: {index} outside [0, {Length}]");
			}
#endif

			return index * TypeSize;
		}

		[Pure, MethodImpl(Runtime.MethodImpl.Optimize)]
		private readonly uint GetOffset (uint index) {
#if DEBUG
			if (index >= unchecked((uint)Length)) {
				throw new IndexOutOfRangeException($"{nameof(index)}: {index} outside [0, {Length}]");
			}
#endif

			return index * unchecked((uint)TypeSize);
		}

		internal readonly unsafe T this[int index] {
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get {
				T* ptr = (T*)(Pointer + GetOffset(index));
				return *ptr;
			}
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			set {
				T* ptr = (T*)(Pointer + GetOffset(index));
				*ptr = value;
			}
		}

		internal readonly unsafe T this[uint index] {
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get {
				T* ptr = (T*)(Pointer + unchecked((int)GetOffset(index)));
				return *ptr;
			}
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			set {
				T* ptr = (T*)(Pointer + unchecked((int)GetOffset(index)));
				*ptr = value;
			}
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal readonly T[] ToArray() {
			var result = new T[Length];
			for (int i = 0; i < Length; ++i) {
				result[i] = this[i];
			}
			return result;
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal readonly U[] ToArray<U>() where U : unmanaged {
			using var span = As<U>();
			return span.ToArray();
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal readonly unsafe ref T GetPinnableReference () {
			return ref *(T*)Pointer;
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public static implicit operator FixedSpan<T>(T[] array) => array.AsFixedSpan();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal FixedSpan(T[] data) : this(data, data.Length, data.Length * TypeSize) {}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal FixedSpan(T[] data, int length) : this(data, length, length * TypeSize) { }

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		private FixedSpan(object pinnedObject, int size) : this(pinnedObject, size / TypeSize, size) { }

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		private FixedSpan(object pinnedObject, int length, int size) {
			var handle = GCHandle.Alloc(pinnedObject, GCHandleType.Pinned);
			try {
				PinnedObject = new WeakReference(pinnedObject);
				Pointer = handle.AddrOfPinnedObject();
				Length = length;
				Size = size;
			}
			catch {
				handle.Free();
				throw;
			}

			Handle = new CollectionHandle(handle);
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		[Obsolete("Very Unsafe")]
		internal unsafe FixedSpan (T* data, int length, int size) {
			PinnedObject = null;
			Pointer = (IntPtr)data;
			Length = length;
			Size = size;
			Handle = null;
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		[Obsolete("Very Unsafe")]
		internal unsafe FixedSpan(T* data, int length) : this(data, length, length * TypeSize) {}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal readonly FixedSpan<U> As<U>() where U : unmanaged {
			// TODO add check for U == T
			if (PinnedObject == null) {
				return new FixedSpan<U>(Pointer, Size / Marshal.SizeOf(typeof(U)), Size);
			}
			else {
				return new FixedSpan<U>(PinnedObject.Target, Size);
			}
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public void Dispose() {
			if (Handle != null) {
				Handle.Dispose();
				Handle = null;
			}
			if (PinnedObject != null) {
				PinnedObject.Target = null;
				PinnedObject = null;
			}
		}

		internal unsafe sealed class Enumerator : IEnumerator<T>, IEnumerator {
			private readonly T* Span;
			private readonly int Length;
			private int Index = 0;

			[MethodImpl(Runtime.MethodImpl.Optimize)]
			internal Enumerator (in FixedSpan<T> span) {
				Span = (T *)span.Pointer;
				Length = span.Length;
			}

			public T Current {
				[MethodImpl(Runtime.MethodImpl.Optimize)]
				get { return Span[Index]; }
			}

			object IEnumerator.Current {
				[MethodImpl(Runtime.MethodImpl.Optimize)]
				get { return Span[Index]; }
			}

			[Pure, MethodImpl(Runtime.MethodImpl.Optimize)]
			public void Dispose () {}

			[MethodImpl(Runtime.MethodImpl.Optimize)]
			public bool MoveNext () {
				++Index;
				if (Index >= Length) {
					return false;
				}
				return true;
			}

			[MethodImpl(Runtime.MethodImpl.Optimize)]
			public void Reset () => Index = 0;
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public readonly IEnumerator<T> GetEnumerator () => new Enumerator(this);

		/*
		IEnumerator IEnumerable.GetEnumerator () {
			return new Enumerator(this);
		}
		*/
	}
}
