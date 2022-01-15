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
	private fixed uint Data[4 * 4];

	internal readonly Color8 this[int index] => (Color8)Data[index];

	internal readonly Color8 A => this[0];
	internal readonly Color8 B => this[1];
	internal readonly Color8 C => this[2];
	internal readonly Color8 D => this[3];
	internal readonly Color8 E => this[4];
	internal readonly Color8 F => this[5];
	internal readonly Color8 G => this[6];
	internal readonly Color8 H => this[7];
	internal readonly Color8 I => this[8];
	internal readonly Color8 J => this[9];
	internal readonly Color8 K => this[10];
	internal readonly Color8 L => this[11];
	internal readonly Color8 M => this[12];
	internal readonly Color8 N => this[13];
	internal readonly Color8 O => this[14];
	internal readonly Color8 P => this[15];

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Kernel4x4(
		Color8 _0,
		Color8 _1,
		Color8 _2,
		Color8 _3,
		Color8 _4,
		Color8 _5,
		Color8 _6,
		Color8 _7,
		Color8 _8,
		Color8 _9,
		Color8 _10,
		Color8 _11,
		Color8 _12,
		Color8 _13,
		Color8 _14,
		Color8 _15
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
