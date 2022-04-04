using SpriteMaster.Types;
using System.Runtime.CompilerServices;

// TODO : Handle X or Y-only scaling, since the game has a lot of 1xY and Xx1 sprites - 1D textures.
namespace SpriteMaster.Resample.Scalers.EPX;

sealed class Config : Resample.Scalers.Config {
	internal const int MaxScale = 2;


	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal Config(
		Vector2B wrapped,
		bool hasAlpha = true,
		bool gammaCorrected = true
	) : base(
		wrapped: wrapped,
		hasAlpha: hasAlpha,
		gammaCorrected: gammaCorrected
	) {
	}
}
