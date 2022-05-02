using SpriteMaster.Types;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Metadata;

static class Metadata {
	private static readonly ConditionalWeakTable<XTexture2D, Texture2DMeta> Texture2DMetaTable = new();

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static Texture2DMeta Meta(this XTexture2D @this) {
#if DEBUG
		if (@this is InternalTexture2D) {
			Debugger.Break();
		}
#endif
		return Texture2DMetaTable.GetValue(@this, key => new(key));
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool TryMeta(this XTexture2D @this, [NotNullWhen(true)] out Texture2DMeta? value) => Texture2DMetaTable.TryGetValue(@this, out value);

	internal static void Purge() {
		Texture2DMetaTable.Clear();
	}

	internal static void FlushValidations() {
		foreach (var p in Texture2DMetaTable) {
			p.Value.Validation = null;
		}
	}
}

