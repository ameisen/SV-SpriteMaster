﻿using SpriteMaster.Types;
using System.Runtime.CompilerServices;

namespace Benchmarks.Sprites.Methods.ReversePremultiply16.ByIndex;
internal static class ScalarFloat {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Reverse(Span<Color16> data, Vector2I size) {
		ushort lowPass = Common.PremultiplicationLowPass;

		for (int i = 0; i < data.Length; ++i) {
			var item = data[i];

			var alpha = item.A;
			var alphaFloat = 1.0f / (alpha.Value / 255.0f);

			switch (alpha.Value) {
				case ushort.MaxValue:
				case var _ when alpha.Value <= lowPass:
					continue;
				default:
					data[i].SetRgb(
						(ushort)((item.R.Value * alphaFloat) + 0.5f),
						(ushort)((item.G.Value * alphaFloat) + 0.5f),
						(ushort)((item.B.Value * alphaFloat) + 0.5f)
					);

					break;
			}
		}
	}
}