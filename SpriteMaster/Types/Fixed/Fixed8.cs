using System.Runtime.InteropServices;

namespace SpriteMaster.Types.Fixed;

[StructLayout(LayoutKind.Explicit, Pack = sizeof(byte), Size = sizeof(byte))]
struct Fixed8 {
	[FieldOffset(0)]
	private byte Value = 0;

	internal static byte FromU16(ushort value) => Colors.ColorConstant.Color16to8(value);

	internal Fixed8(byte value) => Value = value;
	internal Fixed8(Fixed8 value) => Value = value.Value;
	internal Fixed8(Fixed16 value) => Value = FromU16((ushort)value);

	public static explicit operator byte(Fixed8 value) => value.Value;
	public static implicit operator Fixed8(byte value) => new(value);
	public static explicit operator Fixed16(Fixed8 value) => new(Fixed16.FromU8(value.Value));
}
