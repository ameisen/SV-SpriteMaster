using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using static StardewValley.Menus.CharacterCustomization;

namespace SpriteMaster;

sealed class ManagedTexture2D : InternalTexture2D {
	private static ulong TotalAllocatedSize = 0L;
	private static volatile uint TotalManagedTextures = 0;
	private const bool UseMips = false;

	internal readonly WeakReference<Texture2D> Reference;
	internal readonly ManagedSpriteInstance Texture;
	internal readonly Vector2I Dimensions;

	internal static void DumpStats(List<string> output) {
		output.AddRange(new[]{
			"\tManagedTexture2D:",
			$"\t\tTotal Managed Textures : {TotalManagedTextures}",
			$"\t\tTotal Texture Size     : {Interlocked.Read(ref TotalAllocatedSize).AsDataSize()}"
			});
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal ManagedTexture2D(
		ManagedSpriteInstance texture,
		Texture2D reference,
		Vector2I dimensions,
		SurfaceFormat format,
		string? name = null
	) : base(reference.GraphicsDevice.IsDisposed ? DrawState.Device : reference.GraphicsDevice, dimensions.Width, dimensions.Height, UseMips, format) {
		this.Name = name ?? $"{reference.SafeName()} [internal managed <{format}>]";

		Reference = reference.MakeWeak();
		Texture = texture;
		Dimensions = dimensions - texture.BlockPadding;

		reference.Disposing += (resource, _) => OnParentDispose(resource as Texture2D);

		Interlocked.Add(ref TotalAllocatedSize, (ulong)this.SizeBytes());
		Interlocked.Increment(ref TotalManagedTextures);

		Garbage.MarkOwned(format, dimensions.Area);
		Disposing += (resource, _) => {
			Garbage.UnmarkOwned(format, dimensions.Area);
			Interlocked.Add(ref TotalAllocatedSize, (ulong)-this.SizeBytes());
			Interlocked.Decrement(ref TotalManagedTextures);
		};
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	~ManagedTexture2D() {
		if (!IsDisposed) {
			//Debug.Error($"Memory leak: ManagedTexture2D '{Name}' was finalized without the Dispose method called");
			Dispose(false);
		}
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private void OnParentDispose(Texture2D? referenceTexture) {
		if (!IsDisposed) {
			Debug.TraceLn($"Disposing ManagedTexture2D '{Name}'");
			Dispose();
		}

		referenceTexture?.Meta().Dispose();
	}
}
