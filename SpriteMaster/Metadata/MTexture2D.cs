using SpriteMaster.Extensions;
using System;
using SpriteDictionary = System.Collections.Generic.Dictionary<ulong, SpriteMaster.ScaledTexture>;

namespace SpriteMaster.Metadata {
	internal sealed class MTexture2D {
		public readonly SpriteDictionary SpriteTable = new SpriteDictionary();

		public long LastAccessFrame { get; private set; } = DrawState.CurrentFrame;
		private ulong _Hash = default;
		public ulong Hash {
			get {
				lock (this) {
					return _Hash;
				}
			}
			set {
				lock (this) {
					_Hash = value;
				}
			}
		}

		private WeakReference<byte[]> _CachedData = default;
		public byte[] CachedData {
			get {
				lock (this) {
					if (_CachedData != null && _CachedData.TryGetTarget(out var target)) {
						return target;
					}
					return null;
				}
			}
			set {
				lock (this) {
					_CachedData = (value == null) ? null : value.MakeWeak();
					_Hash = default;
				}
			}
		}

		public void UpdateLastAccess() {
			LastAccessFrame = DrawState.CurrentFrame;
		}

		public ulong GetHash(SpriteInfo info) {
			lock (this) {
				if (_Hash == default) {
					_Hash = info.Hash;
				}
				return _Hash;
			}
		}
	}
}
