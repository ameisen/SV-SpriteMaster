using SpriteMaster.Types;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Scalers;

/*
		input kernel area naming convention:
		┌───┬───┬───┬───┐
		│ A │ B │ C │ D │
		├───┼───┼───┼───┤
		│ E │ F │ G │ H │ //evalute the four corners between F, G, J, K
		├───┼───┼───┼───┤ //input pixel is at position F
		│ I │ J │ K │ L │
		├───┼───┼───┼───┤
		│ M │ N │ O │ P │
		└───┴───┴───┴───┘
*/
[ImmutableObject(true)]
unsafe ref struct Kernel4x4 {
	private fixed ulong Data[4 * 4];

	internal readonly Color16 this[int index] => (Color16)Data[index];

	internal readonly Color16 A => this[0];
	internal readonly Color16 B => this[1];
	internal readonly Color16 C => this[2];
	internal readonly Color16 D => this[3];
	internal readonly Color16 E => this[4];
	internal readonly Color16 F => this[5];
	internal readonly Color16 G => this[6];
	internal readonly Color16 H => this[7];
	internal readonly Color16 I => this[8];
	internal readonly Color16 J => this[9];
	internal readonly Color16 K => this[10];
	internal readonly Color16 L => this[11];
	internal readonly Color16 M => this[12];
	internal readonly Color16 N => this[13];
	internal readonly Color16 O => this[14];
	internal readonly Color16 P => this[15];

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Kernel4x4(
		Color16 _0,
		Color16 _1,
		Color16 _2,
		Color16 _3,
		Color16 _4,
		Color16 _5,
		Color16 _6,
		Color16 _7,
		Color16 _8,
		Color16 _9,
		Color16 _10,
		Color16 _11,
		Color16 _12,
		Color16 _13,
		Color16 _14,
		Color16 _15
	) {
		Data[0] = _0.AsPacked;
		Data[1] = _1.AsPacked;
		Data[2] = _2.AsPacked;
		Data[3] = _3.AsPacked;
		Data[4] = _4.AsPacked;
		Data[5] = _5.AsPacked;
		Data[6] = _6.AsPacked;
		Data[7] = _7.AsPacked;
		Data[8] = _8.AsPacked;
		Data[9] = _9.AsPacked;
		Data[10] = _10.AsPacked;
		Data[11] = _11.AsPacked;
		Data[12] = _12.AsPacked;
		Data[13] = _13.AsPacked;
		Data[14] = _14.AsPacked;
		Data[15] = _15.AsPacked;
	}
}
