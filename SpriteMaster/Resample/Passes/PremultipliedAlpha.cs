using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace SpriteMaster.Resample.Passes;

static class PremultipliedAlpha {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Apply(Span<Color8> data, in Vector2I size) {
		foreach (ref Color8 color in data) {
			color.R *= color.A;
			color.G *= color.A;
			color.B *= color.A;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Reverse(Span<Color8> data, in Vector2I size) {
		foreach (ref Color8 color in data) {
			color.R /= color.A;
			color.G /= color.A;
			color.B /= color.A;
		}
	}
}
