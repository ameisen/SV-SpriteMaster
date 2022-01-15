using SpriteMaster.Types;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpriteMaster.xBRZ.Scalers;

[ImmutableObject(true)]
unsafe ref struct Kernel3x3 {
	private fixed uint Data[3 * 3];

	internal readonly Color8 this[int index] => (Color8)Data[index];

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Kernel3x3(Color8 _0, Color8 _1, Color8 _2, Color8 _3, Color8 _4, Color8 _5, Color8 _6, Color8 _7, Color8 _8) {
		Data[0] = _0.AsPacked;
		Data[1] = _1.AsPacked;
		Data[2] = _2.AsPacked;
		Data[3] = _3.AsPacked;
		Data[4] = _4.AsPacked;
		Data[5] = _5.AsPacked;
		Data[6] = _6.AsPacked;
		Data[7] = _7.AsPacked;
		Data[8] = _8.AsPacked;
	}
}
