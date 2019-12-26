using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Metadata;
using SpriteMaster.Types;
using System;

namespace SpriteMaster {
	internal sealed class SpriteInfo {
		internal readonly Texture2D Reference;
		internal readonly Vector2I ReferenceSize;
		internal readonly Bounds Size;
		internal readonly Vector2B Wrapped;
		internal readonly bool BlendEnabled;
		internal byte[] Data { get; private set; } = default;
		private ulong _Hash = default;
		public ulong Hash {
			get {
				if (_Hash == default) {
					_Hash = Data.Hash();
				}
				return _Hash;
			}
		}

		private static unsafe byte[] MakeByteArray<T>(DataRef<T> data, int referenceSize = 0) where T : struct {
			if (data.Data is byte[] byteData) {
				return byteData;
			}

			try {
				referenceSize = (referenceSize == 0) ? (data.Length * typeof(T).Size()) : referenceSize;
				var newData = new byte[referenceSize];

				var byteSpan = data.Data.CastAs<T, byte>();
				foreach (var i in 0.Until(referenceSize)) {
					newData[i] = byteSpan[i];
				}
				return newData;
			}
			catch (Exception ex) {
				ex.PrintInfo();
				return null;
			}
		}

		// Attempt to update the bytedata cache for the reference texture, or purge if it that makes more sense or if updating
		// is not plausible.
		internal static unsafe void Purge<T>(Texture2D reference, Bounds? bounds, DataRef<T> data) where T : struct {
			if (data.IsNull) {
				reference.Meta().CachedData = null;
				return;
			}

			var typeSize = typeof(T).Size();
			var refSize = reference.Area() * typeSize;

			bool forcePurge = false;

			var meta = reference.Meta();

			try {
				if (data.Offset == 0 && data.Length >= refSize) {
					var newByteArray = MakeByteArray(data, refSize);
					forcePurge |= (newByteArray == null);
					meta.CachedData = newByteArray;
				}
				else if (meta.CachedData is var currentData && currentData != null) {
					var byteSpan = data.Data.CastAs<T, byte>();
					var untilOffset = Math.Min(currentData.Length - data.Offset, data.Length * typeSize);
					foreach (var i in 0.Until(untilOffset)) {
						currentData[i + data.Offset] = byteSpan[i];
					}
				}
				else {
					forcePurge = true;
				}
			}
			catch (Exception ex) {
				ex.PrintInfo();
				forcePurge = true;
			}

			if (forcePurge) {
				reference.Meta().CachedData = null;
			}
		}

		// TODO : thread safety?
		internal static void UpdateCache(Texture2D reference, byte[] data) {
			reference.Meta().CachedData = data;
		}

		internal SpriteInfo (Texture2D reference, in Bounds dimensions) {
			ReferenceSize = new Vector2I(reference);
			Size = dimensions;
			if (Size.Bottom > ReferenceSize.Height) {
				Size.Height -= (Size.Bottom - ReferenceSize.Height);
			}
			if (Size.Right > ReferenceSize.Width) {
				Size.Width -= (Size.Right - ReferenceSize.Width);
			}
			Reference = reference;

			if (Data == null) {
				Data = new byte[reference.SizeBytes()];
				reference.GetData(Data);
				UpdateCache(reference, Data);
			}

			BlendEnabled = DrawState.CurrentBlendSourceMode != Blend.One;
			Wrapped = new Vector2B(
				DrawState.CurrentAddressModeU == TextureAddressMode.Wrap,
				DrawState.CurrentAddressModeV == TextureAddressMode.Wrap
			);
		}

		internal void Dispose () {
			Data = default;
		}
	}
}
