using SpriteDictionary = System.Collections.Generic.Dictionary<ulong, SpriteMaster.ScaledTexture>;

using System.Runtime.Caching;
using System.Threading;
using System;
using Ionic.Zlib;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Metadata {
	internal sealed class MTexture2D {
		private static readonly MemoryCache DataCache = (Config.MemoryCache.Enabled) ? new MemoryCache(name: "DataCache", config: null) : null;
		private static long CurrentID = 0U;

		public readonly SpriteDictionary SpriteTable = new SpriteDictionary();
		private readonly string UniqueIDString = Interlocked.Increment(ref CurrentID).ToString();

		public long LastAccessFrame { get; private set; } = DrawState.CurrentFrame;
		private ulong _Hash = default;
		public ulong Hash {
			get {
				lock (this) {
					return _Hash;
				}
			}
		}

		/*
		private byte[] _CachedData = default;
		public byte[] CachedData {
			get {
				lock (this) {
					return _CachedData;
				}
			}
			set {
				lock (this) {
					_CachedData = value;
					_Hash = default;
				}
			}
		}
		*/

		/*
		private CacheType _CachedData = new CacheType(null);
		public byte[] CachedData {
			get {
				lock (this) {
					if (_CachedData.TryGetTarget(out var target)) {
						return target;
					}
					return null;
				}
			}
			set {
				lock (this) {
					if (_CachedData.TryGetTarget(out var target) && target == value) {
						return;
					}
					_CachedData.SetTarget(value);
					_Hash = default;
				}
			}
		}
		*/

		private static ConditionalWeakTable<byte[], object> AlreadyCompressingTable = Config.MemoryCache.Enabled ? new ConditionalWeakTable<byte[], object>() : null;

		private static readonly MethodInfo ZlibBaseCompressBuffer;
		static MTexture2D() {
			ZlibBaseCompressBuffer = typeof(ZlibStream).Assembly.GetType("Ionic.Zlib.ZlibBaseStream").GetMethod("CompressBuffer", BindingFlags.Static | BindingFlags.Public);
			if (ZlibBaseCompressBuffer == null) {
				throw new NullReferenceException(nameof(ZlibBaseCompressBuffer));
			}
		}

		private static byte[] LZCompress(byte[] data) {
			using (var val = new MemoryStream()) {
				using (var compressor = new DeflateStream(val, CompressionMode.Compress, CompressionLevel.BestCompression)) {
					ZlibBaseCompressBuffer.Invoke(null, new object[] { data, compressor });
					return val.ToArray();
				}
			}
		}

		private static byte[] Compress(byte[] data) {
			switch (Config.MemoryCache.Type) {
				case Config.MemoryCache.Algorithm.None:
					return data;
				case Config.MemoryCache.Algorithm.LZ:
					return LZCompress(data);
				case Config.MemoryCache.Algorithm.LZMA: {
					/*
					using var outStream = new MemoryStream();
					using (var inStream = new XZCompressStream(outStream)) {
						inStream.Write(data, 0, data.Length);
					}
					return outStream.ToArray();
					*/
					throw new NotImplementedException("LZMA support not yet implemented.");
				}
				default:
					throw new Exception($"Unknown Compression Algorithm: {Config.MemoryCache.Type}");
			}
		}

		private static byte[] Decompress(byte[] data) {
			switch (Config.MemoryCache.Type) {
				case Config.MemoryCache.Algorithm.None:
					return data;
				case Config.MemoryCache.Algorithm.LZ:
					return DeflateStream.UncompressBuffer(data);
				case Config.MemoryCache.Algorithm.LZMA: {
					/*
					using var outStream = new MemoryStream();
					using (var inStream = new XZDecompressStream(outStream)) {
						inStream.Read(data, 0, data.Length);
					}
					return outStream.ToArray();
					*/
					throw new NotImplementedException("LZMA support not yet implemented.");
				}
				default:
					throw new Exception($"Unknown Compression Algorithm: {Config.MemoryCache.Type}");
			}
		}

		// TODO : this presently is not threadsafe.
		private readonly WeakReference<byte[]> _CachedData = (Config.MemoryCache.Enabled) ? new WeakReference<byte[]>(null) : null;
		public byte[] CachedData {
			get {
				if (!Config.MemoryCache.Enabled)
					return null;
				lock (this) {
					byte[] target = null;
					if (!_CachedData.TryGetTarget(out target) || target == null) {
						byte[] compressedBuffer;
						lock (DataCache) {
							compressedBuffer = DataCache[UniqueIDString] as byte[];
						}
						if (compressedBuffer != null) {
							target = Decompress(compressedBuffer);
						}
						else {
							target = null;
						}
						_CachedData.SetTarget(target);
					}
					return target;
				}
			}
			set {
				if (!Config.MemoryCache.Enabled)
					return;
				lock (this) {
					if (_CachedData.TryGetTarget(out var target) && target == value) {
						return;
					}
					lock (DataCache) {
						if (value == null) {
							DataCache.Remove(UniqueIDString);
							_CachedData.SetTarget(null);
						}
						else {
							if (!AlreadyCompressingTable.TryGetValue(value, out var _)) {
								try { AlreadyCompressingTable.Add(value, null); } catch { }
								ThreadPool.QueueUserWorkItem((buffer) => {
									var compressedData = Compress((byte[])buffer);
									lock (DataCache) {
										DataCache[UniqueIDString] = compressedData;
									}
								}, value);
							}
							_CachedData.SetTarget(value);
						}
						_Hash = default;
					}
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
