﻿using JetBrains.Annotations;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using SpriteMaster.Types.Fixed;
using StardewModdingAPI;
using StbImageSharp;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches;

using ImageResultObject = Object;

internal static partial class FileCache {
	private static class Stb {
		private const string StbNamespace = "StbImageSharp";

		internal static class ColorComponents {
			internal static readonly Type? ColorComponentsType =
				typeof(XTexture2D).Assembly.GetType($"{StbNamespace}.ColorComponents");

			internal static readonly int? RedGreenBlueAlpha =
				EnumExt.Parse<int>(ColorComponentsType, "RedGreenBlueAlpha");

			[MemberNotNullWhen(
				true,
				nameof(ColorComponentsType),
				nameof(RedGreenBlueAlpha)
			)]
			internal static bool Has { get; } =
				ColorComponentsType is not null &&
				RedGreenBlueAlpha is not null;
		}

		internal static class ImageResult {
			internal static readonly Type? ImageResultType =
				typeof(XTexture2D).Assembly.GetType($"{StbNamespace}.ImageResult");

			internal static readonly Func<byte[], int, object>? FromMemory = ImageResultType
				?.GetStaticMethod("FromMemory")?.CreateDelegate<Func<byte[], int, object>>();

			internal static readonly Func<object, int>? GetWidth =
				ImageResultType?.GetPropertyGetter<object, int>("Width");

			internal static readonly Func<object, int>? GetHeight =
				ImageResultType?.GetPropertyGetter<object, int>("Height");

			internal static readonly Func<object, byte[]>? GetData =
				ImageResultType?.GetPropertyGetter<object, byte[]>("Data");

			[MemberNotNullWhen(
				true,
				nameof(ImageResultType),
				nameof(FromMemory),
				nameof(GetWidth),
				nameof(GetHeight),
				nameof(GetData)
			)]
			internal static bool Has { get; } =
				ImageResultType is not null &&
				FromMemory is not null &&
				GetWidth is not null &&
				GetHeight is not null &&
				GetData is not null;
		}
	}

	[StructLayout(LayoutKind.Auto)]
	private readonly struct RawTextureData : IRawTextureData {
		private readonly Vector2I Size;
		private readonly XColor[] Data;

		[Pure]
		readonly int IRawTextureData.Width => Size.Width;
		[Pure]
		readonly int IRawTextureData.Height => Size.Height;
		[Pure]
		readonly XColor[] IRawTextureData.Data => Data;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal RawTextureData(Vector2I size, XColor[] data) {
			Size = size;
			Data = data;
		}
	}

	[HarmonizeSmapiVersionConditional(Comparator.GreaterThanOrEqual, "3.15.0")]
	[Harmonize(
		"StardewModdingAPI.Framework.ContentManagers.ModContentManager",
		"LoadRawImageData",
		Fixation.Prefix,
		PriorityLevel.Last,
		instance: true
	)]
	public static bool OnLoadRawImageData(object __instance, ref IRawTextureData __result, FileInfo file, bool forRawData) {
		if (!Stb.ColorComponents.Has || !Stb.ImageResult.Has) {
			return true;
		}

		string path = file.FullName;
		string resolvedPath = Path.GetFullPath(path);

		var rawData = File.ReadAllBytes(resolvedPath);
		try {
			var imageResult = Stb.ImageResult.FromMemory(rawData, Stb.ColorComponents.RedGreenBlueAlpha.Value);
			byte[] data = Stb.ImageResult.GetData(imageResult);
			var colorData = data.AsSpan<Color8>();

			ProcessTexture(colorData);
				
			XColor[] resultData = data.Convert<byte, XColor>();

			__result = new RawTextureData(
				size: (Stb.ImageResult.GetWidth(imageResult), Stb.ImageResult.GetHeight(imageResult)),
				resultData
			);

			return false;
		}
		catch (Exception ex) {
			// If there is an exception, swallow it and just go back to the normal execution path.
			Debug.Error($"{nameof(OnLoadRawImageData)} exception while processing '{path}'", ex);
			return true;
		}
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	private static void ProcessTexture(Span<Color8> data) {
		if (Avx2.IsSupported && Extensions.Simd.Support.Avx2) {
			ProcessTextureAvx2(data);
		}
		else if (Sse2.IsSupported) {
			ProcessTextureSse2(data);
		}
		else {
			ProcessTextureScalar(data);
		}
	}
}