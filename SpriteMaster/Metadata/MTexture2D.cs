using System;
using System.Collections.Generic;

namespace SpriteMaster.Metadata {
	internal sealed class MTexture2D {
		public readonly Dictionary<ulong, ScaledTexture> SpriteTable = new Dictionary<ulong, ScaledTexture>();
		public long LastAccessFrame { get; private set; } = DrawState.CurrentFrame;
		public ulong Hash = 0;
		public WeakReference<byte[]> CachedData = null;

		public void UpdateLastAccess() {
			LastAccessFrame = DrawState.CurrentFrame;
		}
	}
}
