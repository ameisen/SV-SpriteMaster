﻿using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace SpriteMaster.Resample.Passes;

static class PremultipliedAlpha {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Apply(Span<Color16> data, in Vector2I size) {
		foreach (ref Color16 color in data) {
			color.R *= color.A;
			color.G *= color.A;
			color.B *= color.A;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static void Reverse(Span<Color16> data, in Vector2I size) {
		foreach (ref Color16 color in data) {
			color.R /= color.A;
			color.G /= color.A;
			color.B /= color.A;
		}
	}
}