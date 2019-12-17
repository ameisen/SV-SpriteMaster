using System.Runtime.CompilerServices;

namespace xBRZNet2.Common {
	internal struct IntPair {
		public int I;
		public int J;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public IntPair (int i, int j) {
			I = i;
			J = j;
		}
	}
}
