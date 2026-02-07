using Pastel;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using SpriteMaster.Types.MemoryCache;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SpriteMaster.Caching;

internal static partial class TextureFileCache {
	private static readonly IMemoryCache<string, XColor> Cache =
		AbstractMemoryCache<string, XColor>.Create(name: "File Cache", maxSize: SMConfig.TextureFileCache.MaxSize, compressed: true);

	private static readonly ConcurrentDictionary<string, Vector2I> TextureInfoCache = new();

	private static (Vector2I Size, XColor[] Data)? GetRawImageData(string resolvedPath, bool forRawData) {
		if (!Stb.Enabled || !SMConfig.TextureFileCache.Enabled) {
			return null;
		}

		if (TextureInfoCache.TryGetValue(resolvedPath, out var cachedSize)) {
			if (Cache.TryGet(resolvedPath, out var cachedValue)) {
				Debug.Trace($"Loading Texture '{resolvedPath}' from cache.".Pastel(DrawingColor.LightGreen));

				return (
					cachedSize,
					forRawData ? cachedValue.CloneFast() : cachedValue
				);
			}
		}

		return null;
	}

	private static (Vector2I Size, XColor[] Data)? LoadFromFile(string path, bool copyArray, bool swallowExceptions) {
		Debug.Trace($"Loading Texture '{path}' from file.");
		var rawData = File.ReadAllBytes(path);
		try {
			var imageResult = new Stb.ImageResult(rawData);
			byte[] data = imageResult.Data;
			var colorData = data.AsSpan<Color8>();

			ProcessTexture(colorData);

			// TODO : Horribly unsafe
			XColor[] resultData = data.Convert<byte, XColor>();

			Vector2I resultSize = imageResult.Size;
			TextureInfoCache.AddOrUpdate(path, resultSize, (_, _) => resultSize);
			Cache.Set(path, resultData);

			return (
				Size: resultSize,
				Data: copyArray ? resultData.CloneFast() : resultData
			);
		}
		catch (Exception ex) {
			if (!swallowExceptions) {
				// If there is an exception, swallow it and just go back to the normal execution path.
				Debug.Error($"{nameof(OnLoadRawImageData)} exception while processing '{path}'", ex);
			}

			return null;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	private static void ProcessTexture(Span<Color8> data) {
#if NETCOREAPP3_0_OR_GREATER
		if (UseAvx2) {
			ProcessTextureAvx2(data);
		}
		else if (UseSse2) {
			ProcessTextureSse2Unrolled(data);
		}
		else
#endif
		{
			ProcessTextureScalar(data);
		}
	}

	internal static void Purge() {
		var newCache = AbstractMemoryCache<string, XColor>.Create(name: "File Cache", maxSize: SMConfig.TextureFileCache.MaxSize, compressed: true);
		var oldCache = Interlocked.Exchange(ref Unsafe.AsRef(Cache), newCache);
		TextureInfoCache.Clear();
		oldCache?.Dispose();
	}

	internal static List<FileInfo> GetAllTextures(string root) {
		List<FileInfo> result = new();

		Queue<DirectoryInfo> pending = new();
		pending.Enqueue(new(root));

		while (pending.TryDequeue(out var directory)) {
			try {
				foreach (var child in directory.EnumerateFileSystemInfos()) {
					if (child is DirectoryInfo childDirectory) {
						pending.Enqueue(childDirectory);
					}
					else if (child is FileInfo childFile) {
						if (childFile.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase)) {
							result.Add(childFile);
						}
					}
				}
			}
			catch (Exception) {
				// swallow exceptions
			}
		}

		return result;
	}

	internal static void Precache() {
		if (!SMConfig.TextureFileCache.Enabled || !SMConfig.TextureFileCache.Precache) {
			return;
		}

		if (GetModsPath() is not {} rootDirectory) {
			// Could not derive Mods directory path :(
			return;
		}

		// Traverse all mods looking for '.png' files (and maybe '.tga' or '.dds'?)
		var allSpriteSheets = GetAllTextures(rootDirectory);

		Parallel.ForEach(
			allSpriteSheets, file => {
				var originalPriority = Thread.CurrentThread.Priority;
				Thread.CurrentThread.Priority = ThreadPriority.Lowest;
				try {
					LoadFromFile(path: file.FullName, copyArray: false, swallowExceptions: true, out _);
				}
				catch {
					// swallow exceptions
				}
				finally {
					Thread.CurrentThread.Priority = originalPriority;
				}
			}
		);
	}

	internal static long Size => Cache.SizeBytes;
}
