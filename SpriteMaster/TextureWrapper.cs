using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using XRectangle = Microsoft.Xna.Framework.Rectangle;

namespace SpriteMaster
{
	internal class TextureWrapper
	{
		internal readonly Texture2D Reference;
		internal readonly Dimensions ReferenceSize;
		internal readonly XRectangle Size;
		internal readonly Vector2B Wrapped;
		internal readonly bool BlendEnabled;
		internal byte[] Data = null;
		private ulong _Hash = 0;

		private static ConditionalWeakTable<Texture2D, WeakReference<byte[]>> DataCache = new ConditionalWeakTable<Texture2D, WeakReference<byte[]>>();

		internal TextureWrapper(in Texture2D reference, in XRectangle dimensions)
		{
			ReferenceSize = Dimensions.From(reference);
			Size = dimensions;
			if (Size.Bottom > ReferenceSize.Height)
			{
				Size.Height -= (Size.Bottom - ReferenceSize.Height);
			}
			if (Size.Right > ReferenceSize.Width)
			{
				Size.Width -= (Size.Right - ReferenceSize.Width);
			}
			Reference = reference;

			if (DataCache.TryGetValue(reference, out var dataRef))
			{
				if (!dataRef.TryGetTarget(out Data))
				{
					DataCache.Remove(reference);
				}
			}

			if (Data == null)
			{
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

		internal ulong Hash()
		{
			if (_Hash == 0)
			{
				_Hash = Data.Hash();
			}
			return _Hash;

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
		}

		internal void Dispose()
		{
			if (Data != null)
			{
				Data = null;
			}
		}
	}
}
