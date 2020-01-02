using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Scalers {
	/*
			input kernel area naming convention:
			-----------------
			| A | B | C | D |
			----|---|---|---|
			| E | F | G | H | //evalute the four corners between F, G, J, K
			----|---|---|---| //input pixel is at position F
			| I | J | K | L |
			----|---|---|---|
			| M | N | O | P |
			-----------------
	*/
	// ReSharper disable once InconsistentNaming
	[ImmutableObject(true)]
	internal unsafe ref struct Kernel4x4 {
		private fixed int Data[4 * 4];

		public int this[int index] {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Data[index];
		}

		public int A { get => Data[0]; }
		public int B { get => Data[1]; }
		public int C { get => Data[2]; }
		public int D { get => Data[3]; }
		public int E { get => Data[4]; }
		public int F { get => Data[5]; }
		public int G { get => Data[6]; }
		public int H { get => Data[7]; }
		public int I { get => Data[8]; }
		public int J { get => Data[9]; }
		public int K { get => Data[10]; }
		public int L { get => Data[11]; }
		public int M { get => Data[12]; }
		public int N { get => Data[13]; }
		public int O { get => Data[14]; }
		public int P { get => Data[15]; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Kernel4x4 (
			int _0,
			int _1,
			int _2,
			int _3,
			int _4,
			int _5,
			int _6,
			int _7,
			int _8,
			int _9,
			int _10,
			int _11,
			int _12,
			int _13,
			int _14,
			int _15
		) {
			Data[0] = _0;
			Data[1] = _1;
			Data[2] = _2;
			Data[3] = _3;
			Data[4] = _4;
			Data[5] = _5;
			Data[6] = _6;
			Data[7] = _7;
			Data[8] = _8;
			Data[9] = _9;
			Data[10] = _10;
			Data[11] = _11;
			Data[12] = _12;
			Data[13] = _13;
			Data[14] = _14;
			Data[15] = _15;
		}
	}
}
