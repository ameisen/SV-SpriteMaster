using System;
using System.Runtime.InteropServices;

namespace SpriteMaster.Types;

[StructLayout(LayoutKind.Auto)]
internal readonly struct Button {
	[Flags]
	enum ModifierFlags {
		None = 0,
		Alt = 1 << 0,
		Control = 1 << 1,
		Shift = 1 << 2,
		Command = 1 << 3,
		// Sub-modifiers
		LeftAlt = 1 << 4,
		RightAlt = 1 << 5,
		LeftControl = 1 << 6,
		RightControl = 1 << 7,
		LeftShift = 1 << 8,
		RightShift = 1 << 9,
		LeftCommand = 1 << 10,
		RightCommand = 1 << 11
	}
}
