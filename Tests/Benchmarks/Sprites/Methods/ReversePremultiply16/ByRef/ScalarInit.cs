using SpriteMaster.Types;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Benchmarks.Sprites.Methods.ReversePremultiply16.ByRef;
internal static class ScalarInit {
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Reverse(Span<Color16> data, Vector2I size) {
		ushort lowPass = Common.PremultiplicationLowPass;

		for (int i = 0; i < data.Length; ++i) {
			ref var refItem = ref Unsafe.Add(ref MemoryMarshal.GetReference(data), i);
			var item = refItem;

			var alpha = item.A;

			switch (alpha.Value) {
				case ushort.MaxValue:
				case var _ when alpha.Value <= lowPass:
					continue;
				default:
					refItem = new Color16(
						item.R.ClampedDivide(alpha),
						item.G.ClampedDivide(alpha),
						item.B.ClampedDivide(alpha),
						alpha
					);

					break;
			}
		}
	}
}