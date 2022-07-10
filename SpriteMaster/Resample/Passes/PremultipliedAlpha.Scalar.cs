using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Resample.Passes;

internal static partial class PremultipliedAlpha {
	[MethodImpl(Runtime.MethodImpl.Inline)]
	private static void ApplyScalar(Span<Color16> data, Vector2I size, bool full) {
		if (full) {
			ApplyScalarFull(data, size);
		}
		else {
			ApplyScalarHighPass(data, size);
		}
	}

	private static unsafe void ApplyScalarFull(Span<Color16> data, Vector2I size) {
		fixed (Color16* pDataRef = data) {
			Color16* pData = pDataRef;

			for (int i = 0; i < data.Length; ++i) {
				var item = *pData;

				var alpha = item.A;

				pData->SetRgb(
					item.R * alpha,
					item.G * alpha,
					item.B * alpha
				);

				++pData;
			}
		}
	}

	private static unsafe void ApplyScalarHighPass(Span<Color16> data, Vector2I size) {
		fixed (Color16* pDataRef = data) {
			Color16* pData = pDataRef;

			ushort maxAlpha = 0;

			for (int i = 0; i < data.Length; ++i) {
				var alpha = pData->A.Value;

				if (maxAlpha < alpha) {
					maxAlpha = alpha;
				}

				++pData;
			}

			pData = pDataRef;

			for (int i = 0; i < data.Length; ++i) {
				var item = *pData;

				var alpha = item.A;

				switch (alpha.Value) {
					case var _ when alpha.Value == maxAlpha:
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
	private static void ReverseScalar(Span<Color16> data, Vector2I size, bool full) {
		if (full) {
			ReverseScalarFull(data, size);
		}
		else {
			ReverseScalarHighPass(data, size);
		}
	}

	private static unsafe void ReverseScalarFull(Span<Color16> data, Vector2I size) {
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

	private static unsafe void ReverseScalarHighPass(Span<Color16> data, Vector2I size) {
		ushort lowPass = SMConfig.Resample.PremultiplicationLowPass;

		fixed (Color16* pDataRef = data) {
			Color16* pData = pDataRef;

			ushort maxAlpha = 0;

			for (int i = 0; i < data.Length; ++i) {
				var alpha = pData->A.Value;

				if (maxAlpha < alpha) {
					maxAlpha = alpha;
				}

				++pData;
			}

			pData = pDataRef;

			for (int i = 0; i < data.Length; ++i) {
				var item = *pData;

				var alpha = item.A;

				switch (alpha.Value) {
					case ushort.MaxValue:
					case var _ when alpha.Value <= lowPass:
						continue;
					case var _ when alpha.Value == maxAlpha:
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
