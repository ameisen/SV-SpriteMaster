using System.Runtime.CompilerServices;

namespace SpriteMaster.Colors {
	internal static class ColorConstant {
		internal static class Shift {
			internal const int Alpha = 24;
			internal const int Red = 0;
			internal const int Green = 8;
			internal const int Blue = 16;
		}

		internal static class Mask {
			internal const uint Alpha = 0xFFU << Shift.Alpha;
			internal const uint Red = 0xFFU << Shift.Red;
			internal const uint Green = 0xFFU << Shift.Green;
			internal const uint Blue = 0xFFU << Shift.Blue;
		}
	}
}
