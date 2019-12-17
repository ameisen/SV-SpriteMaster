using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.Runtime.CompilerServices;

namespace SpriteMaster {
	internal sealed class TextureWrapper {
		internal readonly Texture2D Reference;
		internal readonly Vector2I ReferenceSize;
		internal readonly Bounds Size;
		internal readonly Bounds IndexRectangle;
		internal readonly Vector2B Wrapped;
		internal readonly bool BlendEnabled;
		internal byte[] Data = null;
		private ulong _Hash = 0;

		private static ConditionalWeakTable<Texture2D, WeakReference<byte[]>> DataCache = new ConditionalWeakTable<Texture2D, WeakReference<byte[]>>();

		internal TextureWrapper (Texture2D reference, in Bounds dimensions, in Bounds indexRectangle) {
			ReferenceSize = new Vector2I(reference);
			Size = dimensions;
			IndexRectangle = indexRectangle;
			if (Size.Bottom > ReferenceSize.Height) {
				Size.Height -= (Size.Bottom - ReferenceSize.Height);
			}
			if (Size.Right > ReferenceSize.Width) {
				Size.Width -= (Size.Right - ReferenceSize.Width);
			}
			Reference = reference;

			if (DataCache.TryGetValue(reference, out var dataRef)) {
				if (!dataRef.TryGetTarget(out Data)) {
					DataCache.Remove(reference);
				}
			}

			if (Data == null) {
				Data = new byte[reference.Width * reference.Height * 4];
				reference.GetData(Data);
				DataCache.Add(reference, new WeakReference<byte[]>(Data));
			}

			BlendEnabled = Patches.CurrentBlendSourceMode != Blend.One;
			Wrapped = new Vector2B(
				Patches.CurrentAddressModeU == TextureAddressMode.Wrap,
				Patches.CurrentAddressModeV == TextureAddressMode.Wrap
			);
		}

		internal ulong Hash () {
			if (_Hash == 0) {
				_Hash = Data.Hash();
			}
			return _Hash;

			/*
			ulong hash = ulong.MaxValue;
			foreach (int y in Size.Top.Until(Size.Bottom))
			{
				int yOffset = y * ReferenceSize.Width;
				foreach (int x in Size.Left.Until(Size.Right))
				{
					int offset = yOffset + x;
					hash ^= Data.Hash(offset, Size.Width * sizeof(int));
				}
			}

			return hash;
			*/
		}

		internal void Dispose () {
			Data = null;
		}
	}
}
