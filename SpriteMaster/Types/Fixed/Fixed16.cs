using System.Runtime.InteropServices;

namespace SpriteMaster.Types.Fixed;

[StructLayout(LayoutKind.Explicit, Pack = sizeof(ushort), Size = sizeof(ushort))]
struct Fixed16 {
	[FieldOffset(0)]
	private ushort Value = 0;

	internal static ushort FromU8(byte value) => Colors.ColorConstant.Color8To16(value);

	internal Fixed16(ushort value) => Value = value;
	internal Fixed16(Fixed16 value) => Value = value.Value;
	internal Fixed16(Fixed8 value) => Value = FromU8((byte)value);

	public static explicit operator ushort(Fixed16 value) => value.Value;
	public static implicit operator Fixed16(ushort value) => new(value);
	public static explicit operator Fixed8(Fixed16 value) => new(Fixed8.FromU16(value.Value));
}
