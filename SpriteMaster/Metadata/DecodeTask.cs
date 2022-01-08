using SpriteMaster.Metadata;
using SpriteMaster.Tasking;
using System;
using System.Threading.Tasks;

namespace SpriteMaster.Resample;

#nullable enable

static class DecodeTask {
	private static readonly TaskFactory Factory = new(ThreadedTaskScheduler.Instance);

	private static void Decode(object? metadata) => DecodeFunction(metadata! as MTexture2D);

	private static void DecodeFunction(MTexture2D? metadata) {
		var rawData = metadata!.CachedRawData;
		if (rawData == null) {
			return;
		}

		var uncompressedData = Resample.TextureDecode.DecodeBlockCompressedTexture(metadata!.Format, metadata!.Size, rawData);
		if (uncompressedData.IsEmpty) {
			throw new InvalidOperationException("Compressed data failed to decompress");
		}
		metadata!.SetCachedDataUnsafe(uncompressedData);
	}

	internal static Task Dispatch(MTexture2D metadata) => Factory.StartNew(Decode, metadata);
}
