using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Scalers {
	// ReSharper disable once InconsistentNaming
	[ImmutableObject(true)]
	internal unsafe ref struct Kernel3x3 {
		private fixed int Data[3 * 3];

		public int this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Data[index];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Kernel3x3(int _0, int _1, int _2, int _3, int _4, int _5, int _6, int _7, int _8) {
			Data[0] = _0;
			Data[1] = _1;
			Data[2] = _2;
			Data[3] = _3;
			Data[4] = _4;
			Data[5] = _5;
			Data[6] = _6;
			Data[7] = _7;
			Data[8] = _8;
		}
	}
}
