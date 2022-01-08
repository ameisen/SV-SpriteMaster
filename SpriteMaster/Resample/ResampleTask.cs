using SpriteMaster.Extensions;
using SpriteMaster.Tasking;
using SpriteMaster.Types;
using System.Threading.Tasks;

namespace SpriteMaster.Resample;

#nullable enable

static class ResampleTask {
	private static readonly TaskFactory<ScaledTexture?> Factory = new(ThreadedTaskScheduler.Instance);

	private static ScaledTexture? Resample(object? parametersObj) => ResampleFunction((TaskParameters)parametersObj!);

	private static ScaledTexture? ResampleFunction(in TaskParameters parameters) {
		return new ScaledTexture(
			assetName: parameters.SpriteInfo.Reference.SafeName(),
			textureWrapper: parameters.SpriteInfo,
			sourceRectangle: parameters.SpriteInfo.Bounds,
			textureType: parameters.SpriteInfo.TextureType,
			async: parameters.Async,
			expectedScale: parameters.SpriteInfo.ExpectedScale
		);
	}

	private readonly record struct TaskParameters(
		SpriteInfo SpriteInfo,
		bool Async
	);

	internal static Task<ScaledTexture?> Dispatch(
		SpriteInfo spriteInfo,
		bool async
	) {
		var parameters = new TaskParameters(
			SpriteInfo: spriteInfo,
			Async: async
		);

		if (async) {
			return Factory.StartNew(Resample, parameters);
		}
		else {
			return Task<ScaledTexture?>.FromResult(ResampleFunction(parameters));
		}
	}


}
