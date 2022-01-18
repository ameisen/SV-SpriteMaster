using SpriteMaster.Extensions;
using SpriteMaster.Tasking;
using System.Threading.Tasks;

namespace SpriteMaster.Resample;

static class ResampleTask {
	private static readonly TaskFactory<ManagedSpriteInstance?> Factory = new(ThreadedTaskScheduler.Instance);

	private static ManagedSpriteInstance? Resample(object? parametersObj) => ResampleFunction((TaskParameters)parametersObj!);

	private static ManagedSpriteInstance? ResampleFunction(in TaskParameters parameters) {
		return new ManagedSpriteInstance(
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

	internal static Task<ManagedSpriteInstance?> Dispatch(
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
			return Task<ManagedSpriteInstance?>.FromResult(ResampleFunction(parameters));
		}
	}

}
