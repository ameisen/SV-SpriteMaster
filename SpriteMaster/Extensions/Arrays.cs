using SpriteMaster.Types;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace SpriteMaster.Extensions {
	public static class Arrays {
		// UnmanagedMemoryStream

		private sealed class WrappedUnmanagedMemoryStream<T> : UnmanagedMemoryStream {
			private readonly GCHandle Handle;
			private volatile bool IsDisposed = false;

			private unsafe WrappedUnmanagedMemoryStream(GCHandle handle, int offset, int size, FileAccess access) :
				base(
					(byte*)(handle.AddrOfPinnedObject() + (Marshal.SizeOf(typeof(T)) * offset)),
					size * Marshal.SizeOf(typeof(T)),
					size * Marshal.SizeOf(typeof(T)),
					access
				)
			{
				Handle = handle;
			}

			public static unsafe WrappedUnmanagedMemoryStream<T> Get(T[] data, int offset, int size, FileAccess access) {
				var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				try {
					return new WrappedUnmanagedMemoryStream<T>(handle, offset, size, access);
				}
				catch {
					handle.Free();
					throw;
				}
			}

			~WrappedUnmanagedMemoryStream() {
				Dispose(true);
			}

			[SecuritySafeCritical]
			protected override void Dispose (bool disposing) {
				if (!IsDisposed) {
					Handle.Free();
					IsDisposed = true;
				}
			}
		}

		public static unsafe UnmanagedMemoryStream Stream<T> (this T[] data) where T : struct {
			return WrappedUnmanagedMemoryStream<T>.Get(data, 0, data.Length, FileAccess.ReadWrite);
		}

		public static UnmanagedMemoryStream Stream<T> (this T[] data, int offset = 0, int length = -1, FileAccess access = FileAccess.ReadWrite) {
			if (length == -1) {
				length = data.Length - offset;
			}
			return WrappedUnmanagedMemoryStream<T>.Get(data, offset, length, access);
		}

		public static MemoryStream Stream(this byte[] data) {
			return new MemoryStream(data, 0, data.Length, true, true);
		}

		public static MemoryStream Stream(this byte[] data, int offset = 0, int length = -1, FileAccess access = FileAccess.ReadWrite) {
			if (length == -1) {
				length = data.Length - offset;
			}
			return new MemoryStream(data, offset, length, (access != FileAccess.Read), true);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Span<U> CastAs<T, U>(this T[] data) where T : unmanaged where U : unmanaged {
			return new Span<T>(data).As<U>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Reverse<T> (this T[] array) {
			Contract.AssertNotNull(array);
			Array.Reverse(array);
			return array;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Reversed<T> (this T[] array) {
			Contract.AssertNotNull(array);
			var result = (T[])array.Clone();
			Array.Reverse(result);
			return result;
		}
	}
}
