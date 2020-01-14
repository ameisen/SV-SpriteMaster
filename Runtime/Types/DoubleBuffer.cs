using SpriteMaster.Extensions;
using System;

namespace SpriteMaster.Types {
	public sealed class DoubleBuffer<T> {
		public const int BufferCount = 2;
		public static readonly Type BufferType = typeof(T);

		private readonly T[] BufferedObjects = new T[BufferCount];
		public int CurrentBuffer { get; private set; } = 0;

		public T Current { get => BufferedObjects[CurrentBuffer]; }
		public T Next { get => BufferedObjects[(CurrentBuffer + 1) & 1]; }

		public DoubleBuffer(in T element0, in T element1) {
			BufferedObjects[0] = element0;
			BufferedObjects[1] = element1;
		}

		public DoubleBuffer (T[] elements) : this(elements[0], elements[1]) { }

		public DoubleBuffer (params object[] parameters) : this(
			Reflection.CreateInstance<T>(parameters),
			Reflection.CreateInstance<T>(parameters)
		) { }

		public void Swap() {
			CurrentBuffer += 1;
			CurrentBuffer &= 1;
		}

		public static implicit operator T (DoubleBuffer<T> buffer) {
			return buffer.Current;
		}
	}
}
