using System.Runtime.InteropServices;

namespace SpriteMaster.Types.Fixed;

[StructLayout(LayoutKind.Explicit, Pack = sizeof(ushort), Size = sizeof(ushort))]
struct Fixed16 {
	public static readonly Fixed16 Zero = new((ushort)0);

	[FieldOffset(0)]
	private ushort InternalValue = 0;

	internal readonly ushort Value => InternalValue;

	internal static ushort FromU8(byte value) => Colors.ColorConstant.Color8To16(value);

	internal Fixed16(ushort value) => InternalValue = value;
	internal Fixed16(Fixed16 value) => InternalValue = value.InternalValue;
	internal Fixed16(Fixed8 value) => InternalValue = FromU8((byte)value);

	public static Fixed16 operator /(Fixed16 numerator, Fixed16 denominator) {
		if (denominator == Fixed16.Zero) {
			return numerator;
		}
		ulong numeratorWidened = ((ulong)numerator.InternalValue) << 32;
		numeratorWidened -= numerator.InternalValue;
		ulong result = numeratorWidened / denominator.InternalValue;
		return (ushort)(result >> 16);
	}

	public static Fixed16 operator *(Fixed16 lhs, Fixed16 rhs) {
		int intermediate = lhs.InternalValue * rhs.InternalValue;
		intermediate += ushort.MaxValue;
		return (ushort)(intermediate >> 16);
	}

	public static bool operator ==(Fixed16 lhs, Fixed16 rhs) => lhs.InternalValue == rhs.InternalValue;
	public static bool operator !=(Fixed16 lhs, Fixed16 rhs) => lhs.InternalValue != rhs.InternalValue;

	public override readonly bool Equals(object obj) {
		if (obj is Fixed16 valueF) {
			return this == valueF;
		}
		if (obj is byte valueB) {
			return this.InternalValue == valueB;
		}
		return false;
	}

	public override readonly int GetHashCode() => InternalValue.GetHashCode();

	public static explicit operator ushort(Fixed16 value) => value.InternalValue;
	public static implicit operator Fixed16(ushort value) => new(value);
	public static explicit operator Fixed8(Fixed16 value) => new(Fixed8.FromU16(value.InternalValue));
}
