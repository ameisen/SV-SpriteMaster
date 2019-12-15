using System.Runtime.CompilerServices;

namespace xBRZNet2.Common
{
	internal sealed class IntPtr
	{
		private readonly int[] Array;
		public int Offset = 0;

		public int Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get { return Array[Offset]; }
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set { Array[Offset] = value; }
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]

		public IntPtr(in int[] array)
		{
			Array = array;
		}

		public static implicit operator int (in IntPtr pointer)
		{
			return pointer.Value;
		}
	}
}
