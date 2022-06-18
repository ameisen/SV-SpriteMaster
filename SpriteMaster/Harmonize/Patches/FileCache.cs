#if false

using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static SpriteMaster.Harmonize.Harmonize;

namespace SpriteMaster.Harmonize.Patches;

internal static class FileCache {
	private static readonly Type? ImageResultType = typeof(XTexture2D).Assembly.GetType("StbImageSharp.ImageResult");
	private static readonly Type? ColorComponentsType = typeof(XTexture2D).Assembly.GetType("StbImageSharp.ColorComponents");

	private static readonly object? ColorComponents_RedGreenBlueAlpha =
		ColorComponentsType is null ? null : Enum.Parse(ColorComponentsType, "RedGreenBlueAlpha");
	private static readonly Func<Stream, object, object>? ImageResult_FromStream = ImageResultType?.GetStaticMethod("FromStream")?.CreateDelegate<Func<Stream, object, DuplicateWaitObjectException>>();
	private static readonly Func<object, int>? ImageResult_GetWidth = ImageResultType?.GetPropertyGetter<object, int>("Width");
	private static readonly Func<object, int>? ImageResult_GetHeight = ImageResultType?.GetPropertyGetter<object, int>("Height");
	private static readonly Func<object, byte[]>? ImageResult_GetData = ImageResultType?.GetPropertyGetter<object, byte[]>("Data");

	[MemberNotNullWhen(
		true,
		"ImageResultType",
		"ColorComponentsType",
		"ColorComponents_RedGreenBlueAlpha",
		"ImageResult_FromStream",
		"ImageResult_GetWidth",
		"ImageResult_GetHeight",
		"ImageResult_GetData"
	)]
	private static bool HasStb { get; } =
		ImageResultType is not null &&
		ColorComponentsType is not null &&
		ColorComponents_RedGreenBlueAlpha is not null &&
		ImageResult_FromStream is not null &&
		ImageResult_GetWidth is not null &&
		ImageResult_GetHeight is not null &&
		ImageResult_GetData is not null;

	/*
	[HarmonizeSmapiVersionConditional(Comparator.GreaterThanOrEqual, "3.15.0")]
	[HarmonizeTranspile(
		typeof(StardewModdingAPI.Framework.ModLoading.RewriteFacades.AccessToolsFacade),
		"StardewModdingAPI.Framework.ContentManagers.ModContentManager",
		"LoadImageFile",
		argumentTypes: new[] { typeof(IAssetName), typeof(FileInfo) },
		generic: Generic.Class,
		genericTypes: new[] { typeof(Texture2D), typeof(IRawTextureData) },
		instance: true
	)]
	public static IEnumerable<CodeInstruction> LoadImageFileTranspiler(
		IEnumerable<CodeInstruction> instructions,
		ILGenerator generator
	) {
		var preMethod = ((Func<object, IAssetName, FileInfo, Texture2D?>)OnLoadImageFile).Method;

		var codeInstructions = instructions as CodeInstruction[] ?? instructions.ToArray();

		IEnumerable<CodeInstruction> ApplyPatch() {
			var isNull = generator.DefineLabel();

			yield return new(OpCodes.Ldarg_0);
			yield return new(OpCodes.Ldarg_1);
			yield return new(OpCodes.Ldarg_2);
			yield return new(OpCodes.Call, preMethod);
			yield return new(OpCodes.Stloc_0);
			yield return new(OpCodes.Ldloc_0);
			yield return new(OpCodes.Brfalse_S, isNull);
			yield return new(OpCodes.Ldloc_0);
			yield return new(OpCodes.Ret);

			bool first = true;
			foreach (var instruction in codeInstructions) {
				if (first) {
					instruction.labels.Add(isNull);
					first = false;
				}

				yield return instruction;
			}
		}

		return ApplyPatch(); ;
	}
	*/

	/*
	[HarmonizeSmapiVersionConditional(Comparator.GreaterThanOrEqual, "3.15.0")]
	[HarmonizeTranspile(
		typeof(StardewModdingAPI.Framework.ModLoading.RewriteFacades.AccessToolsFacade),
		"StardewModdingAPI.Framework.ContentManagers.ModContentManager",
		"LoadExact",
		argumentTypes: new[] { typeof(IAssetName), typeof(bool) },
		generic: Generic.Class,
		genericTypes: new[] { typeof(Texture2D) },
		instance: true
	)]
	public static IEnumerable<CodeInstruction> LoadExactTranspiler<T>(
		IEnumerable<CodeInstruction> instructions,
		ILGenerator generator
	) where T : Texture2D {
		var preMethod = ((Func<object, IAssetName, FileInfo, XTexture2D?>)OnLoadImageFile).Method;

		var originalTargetMethod =
			typeof(StardewModdingAPI.Framework.ModLoading.RewriteFacades.AccessToolsFacade).Assembly
				.GetType("StardewModdingAPI.Framework.ContentManagers.ModContentManager")
				?.GetInstanceMethod("LoadImageFile")
				?.MakeGenericMethod(typeof(Texture2D));

		var codeInstructions = instructions as CodeInstruction[] ?? instructions.ToArray();

		if (originalTargetMethod is null) {
			Debug.Error("Could not apply OnLoadImageFile patch: could not find MethodInfo for LoadImageFile");
			return codeInstructions;
		}

		bool IsTargetMethodCaller(CodeInstruction instruction) {
			return
				instruction.opcode.Value == OpCodes.Call.Value &&
				ReferenceEquals(instruction.operand, originalTargetMethod);
		}

		IEnumerable<CodeInstruction> ApplyPatch() {
			foreach (var instruction in codeInstructions) {
				if (IsTargetMethodCaller(instruction)) {
					instruction.operand = preMethod;
				}

				yield return instruction;
			}
		}

		return ApplyPatch();
	}
	*/

	/*
	[HarmonizeSmapiVersionConditional(Comparator.GreaterThanOrEqual, "3.15.0")]
	[Harmonize(
		"StardewModdingAPI.Framework.ContentManagers.ModContentManager",
		"LoadExact",
		Fixation.Prefix,
		PriorityLevel.Last,
		generic: Generic.Class,
		genericTypes: new[] { typeof(Texture2D), typeof(IRawTextureData), typeof(object) },
		instance: true
	)]
	public static bool OnLoadExact(object __instance, ref object __result, IAssetName assetName, bool useCache) {
		return true;
	}
	*/

	[HarmonizeSmapiVersionConditional(Comparator.GreaterThanOrEqual, "3.15.0")]
	[HarmonizeTranspile(
		typeof(StardewModdingAPI.Framework.ModLoading.RewriteFacades.AccessToolsFacade),
		"StardewModdingAPI.Framework.ContentManagers.ModContentManager",
		"LoadImageFile",
		argumentTypes: new[] { typeof(IAssetName), typeof(FileInfo) },
		generic: Generic.Class,
		genericTypes: new[] { typeof(Texture2D), typeof(IRawTextureData), typeof(object) },
		instance: true
	)]
	public static IEnumerable<CodeInstruction> OnLoadImageFileTranspiler<T>(
		IEnumerable<CodeInstruction> instructions,
		ILGenerator generator
	) {
		var preMethod = ((Func<object, IAssetName, FileInfo, object>)OnLoadImageFile).Method;

		var codeInstructions = instructions as CodeInstruction[] ?? instructions.ToArray();

		IEnumerable<CodeInstruction> ApplyPatch() {
			yield return new(OpCodes.Ldarg_0);
			yield return new(OpCodes.Ldarg_1);
			yield return new(OpCodes.Ldarg_2);
			yield return new(OpCodes.Call, preMethod);
			yield return new(OpCodes.Ret);
		}

		return ApplyPatch();
	}

	/*
	[HarmonizeSmapiVersionConditional(Comparator.GreaterThanOrEqual, "3.15.0")]
	[Harmonize(
		"StardewModdingAPI.Framework.ContentManagers.ModContentManager",
		"LoadImageFile",
		Fixation.Prefix,
		PriorityLevel.Last,
		generic: Generic.Class,
		genericTypes: new[] { typeof(Texture2D), typeof(IRawTextureData), typeof(object) },
		instance: true
	)]
	*/
	public static object OnLoadImageFile(object __instance, IAssetName assetName, FileInfo file) {
		return null;
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
		return true;
	}

	[MethodImpl(Runtime.MethodImpl.Inline)]
	private static void PremultiplyAlpha(byte[] data) {
		var colors = data.AsSpan<XColor>();

		for (int i = 0; i < colors.Length; i++) {
			var pixel = colors[i];
			if (pixel.A is (byte.MinValue or byte.MaxValue))
				continue; // no need to change fully transparent/opaque pixels

			colors[i] = new(pixel.R * pixel.A / byte.MaxValue, pixel.G * pixel.A / byte.MaxValue, pixel.B * pixel.A / byte.MaxValue, pixel.A); // slower version: Color.FromNonPremultiplied(data[i].ToVector4())
		}
	}
}

#endif
