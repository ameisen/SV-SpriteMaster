﻿using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Resample.Passes;

internal static partial class PremultipliedAlpha {
	[MethodImpl(Runtime.MethodImpl.Inline)]
	private static unsafe void ApplyScalar(Span<Color16> data, Vector2I size) {
		fixed (Color16* pDataRef = data) {
			Color16* pData = pDataRef;

			for (int i = 0; i < data.Length; ++i) {
				var item = *pData;

				var alpha = item.A;

				switch (alpha.Value) {
					case byte.MaxValue:
						continue;
					default:
						pData->SetRgb(
							item.R * alpha,
							item.G * alpha,
							item.B * alpha
						);
						break;
				}

				++pData;
			}
		}
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	private static unsafe void ReverseScalar(Span<Color16> data, Vector2I size) {
		ushort lowPass = SMConfig.Resample.PremultiplicationLowPass;

		fixed (Color16* pDataRef = data) {
			Color16* pData = pDataRef;

			for (int i = 0; i < data.Length; ++i) {
				var item = *pData;

				var alpha = item.A;

				switch (alpha.Value) {
					case ushort.MaxValue:
					case var _ when alpha.Value <= lowPass:
						continue;
					default:
						pData->SetRgb(
							item.R.ClampedDivide(alpha),
							item.G.ClampedDivide(alpha),
							item.B.ClampedDivide(alpha)
						);

						break;
				}

				++pData;
			}
		}
	}
}
