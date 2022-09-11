using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Extensions.Reflection;
using SpriteMaster.Types;
using SpriteMaster.Types.Pooling;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace SpriteMaster.Mitigations.PyTK;

internal static class Textures {
	private static readonly Type? MappedTexture2DType = ReflectionExt.GetTypeExt("PyTK.Types.MappedTexture2D");
	private static readonly Type? ScaledTexture2DType = ReflectionExt.GetTypeExt("PyTK.Types.ScaledTexture2D");

#pragma warning disable CS8714
	private static readonly Func<XTexture2D, Dictionary<XRectangle?, Texture2D>>? GetTextureMap = MappedTexture2DType?.GetMemberGetter<XTexture2D, Dictionary<XRectangle?, Texture2D>>("Map");
#pragma warning restore CS8714
	private static readonly Func<XTexture2D, Texture2D>? GetScaledTextureUnderlying = ScaledTexture2DType?.GetMemberGetter<XTexture2D, Texture2D>("STexture");

	[MemberNotNullWhen(true, nameof(GetTextureMap))]
	private static bool IsMappedTexture(this XTexture2D texture) =>
		GetTextureMap is not null && MappedTexture2DType is { } mappedTexture2DType && texture.GetType().IsAssignableTo(mappedTexture2DType);

	[MemberNotNullWhen(true, nameof(GetScaledTextureUnderlying))]
	private static bool IsScaledTexture(this XTexture2D texture) =>
		GetScaledTextureUnderlying is not null && ScaledTexture2DType is { } scaledTexture2DType && texture.GetType().IsAssignableTo(scaledTexture2DType);

	private static XTexture2D? ParseTexture(this XTexture2D texture) {
		if (IsMappedTexture(texture)) {
			if (GetTextureMap(texture) is {} map) {
				foreach (var mappedTexture in map.Values) {
					if (!string.IsNullOrEmpty(mappedTexture.Name)) {
						return mappedTexture;
					}
				}

				if (map.Count > 0) {
					return map.First().Value;
				}
			}
		}
		else if (IsScaledTexture(texture)) {
			if (GetScaledTextureUnderlying(texture) is {} underlyingTexture) {
				return underlyingTexture;
			}
		}

		return null;
	}

	internal static XTexture2D GetUnderlyingTexture(this XTexture2D texture, out bool hasUnderlying) {
		bool hadUnderlying = false;

		using var seenSet = ObjectPoolExt.TakeLazy<HashSet<XTexture2D>>();

		while (texture.ParseTexture() is {} parsedTexture) {
			if (!seenSet.Value.Add(parsedTexture)) {
				break;
			}
			texture = parsedTexture;
			hadUnderlying = true;
		}

		hasUnderlying = hadUnderlying;

		return texture;
	}
}
