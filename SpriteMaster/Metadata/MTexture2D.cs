using SpriteDictionary = System.Collections.Generic.Dictionary<ulong, SpriteMaster.ScaledTexture>;

using System.Runtime.Caching;
using System.Threading;
using System;
using Ionic.Zlib;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using SpriteMaster.Extensions;

namespace SpriteMaster.Metadata {
	internal sealed class MTexture2D {
		internal static readonly object DataCacheLock = new object();
		private static MemoryCache DataCache = (Config.MemoryCache.Enabled) ? new MemoryCache(name: "DataCache", config: null) : null;
		private static long CurrentID = 0U;

		public readonly SpriteDictionary SpriteTable = new SpriteDictionary();
		private readonly string UniqueIDString = Interlocked.Increment(ref CurrentID).ToString();

		private readonly SharedLock Lock = new SharedLock();

		public volatile bool TracePrinted = false;

		internal static void PurgeDataCache() {
			if (!Config.MemoryCache.Enabled) {
				return;
			}

			lock (DataCacheLock) {
				DataCache.Dispose();
				DataCache = new MemoryCache(name: "DataCache", config: null);
			}
		}

		public long _LastAccessFrame = Thread.VolatileRead(ref DrawState.CurrentFrame);
		public long LastAccessFrame {
			get {
				using (Lock.Shared)
					return Thread.VolatileRead(ref _LastAccessFrame);
			}
			private set {
				using (Lock.Exclusive)
					Thread.VolatileWrite(ref _LastAccessFrame, value);
			}
		}
		private ulong _Hash = default;
		public ulong Hash {
			get {
				using (Lock.Shared) {
					return Thread.VolatileRead(ref _Hash);
				}
			}
			private set {
				using (Lock.Exclusive) {
					Thread.VolatileWrite(ref _Hash, value);
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

		public bool HasCachedData {
			get {
				if (!Config.MemoryCache.Enabled)
					return false;

				using (Lock.Shared) {
					return (_CachedData.TryGetTarget(out var target) && target != null);
				}
			}
		}

		private static unsafe byte[] MakeByteArray<T> (DataRef<T> data, int referenceSize = 0) where T : struct {
			if (data.Data is byte[] byteData) {
				return (byte[])byteData.Clone();
			}

			try {
				referenceSize = (referenceSize == 0) ? (data.Length * typeof(T).Size()) : referenceSize;
				var newData = new byte[referenceSize];

				var byteSpan = data.Data.CastAs<T, byte>();
				foreach (int i in 0..referenceSize) {
					newData[i] = byteSpan[i];
				}
				return newData;
			}
			catch (Exception ex) {
				ex.PrintInfo();
				return null;
			}
		}

		public unsafe void Purge<T> (Texture2D reference, Bounds? bounds, DataRef<T> data) where T : struct {
			lock (this) {

				if (data.IsNull) {
					CachedData = null;
					return;
				}

				var typeSize = typeof(T).Size();
				var refSize = unchecked((int)reference.SizeBytes());

				bool forcePurge = false;

				try {
					// TODO : lock isn't granular enough.
					if (Config.MemoryCache.AlwaysFlush) {
						forcePurge = true;
					}
					else if (!bounds.HasValue && data.Offset == 0 && (data.Length * typeSize) == refSize) {
						Debug.TraceLn("Overriding MTExture2D Cache in Purge");
						var newByteArray = MakeByteArray(data, refSize);
						forcePurge |= (newByteArray == null);
						CachedData = newByteArray;
					}
					else if (!bounds.HasValue && CachedData is var currentData && currentData != null) {
						Debug.TraceLn("Updating MTexture2D Cache in Purge");
						var byteSpan = data.Data.CastAs<T, byte>();
						lock (DataCacheLock) {
							using (Lock.Exclusive) {
								var untilOffset = Math.Min(currentData.Length - data.Offset, data.Length * typeSize);
								foreach (int i in 0..untilOffset) {
									currentData[i + data.Offset] = byteSpan[i];
								}
								Hash = default;
							}
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

				// TODO : maybe we need to purge more often?
				if (forcePurge) {
					CachedData = null;
				}
			}
		}

		public byte[] CachedData {
			get {
				if (!Config.MemoryCache.Enabled)
					return null;

				using (Lock.Shared) {
					byte[] target = null;
					if (!_CachedData.TryGetTarget(out target) || target == null) {
						byte[] compressedBuffer;
						lock (DataCacheLock) {
							compressedBuffer = DataCache[UniqueIDString] as byte[];
						}
						if (compressedBuffer != null) {
							target = Decompress(compressedBuffer);
						}
						else {
							target = null;
						}
						using (Lock.Promote) {
							_CachedData.SetTarget(target);
						}
					}
					return target;
				}
			}
			set {
				if (!Config.MemoryCache.Enabled)
					return;

				TracePrinted = false;

				try {
					using (Lock.Shared) {
						if (_CachedData.TryGetTarget(out var target) && target == value) {
							return;
						}
						if (value == null) {
							using (Lock.Promote) {
								_CachedData.SetTarget(null);
							}
							lock (DataCacheLock) {
								DataCache.Remove(UniqueIDString);
							}
						}
						else {
							bool queueCompress = false;
							lock (AlreadyCompressingTable) {
								if (!AlreadyCompressingTable.TryGetValue(value, out var _)) {
									try { AlreadyCompressingTable.Add(value, null); }
									catch { }
									queueCompress = true;
								}
							}

							if (queueCompress) {
								ThreadPool.QueueUserWorkItem((buffer) => {
									var compressedData = Compress((byte[])buffer);
									lock (DataCacheLock) {
										DataCache[UniqueIDString] = compressedData;
										using (Lock.Exclusive) {
											Hash = default;
										}
									}
								}, value);
							}

							using (Lock.Promote) {
								_CachedData.SetTarget(value);
							}
						}
					}
				}
				finally {
					Hash = default;
				}
			}
		}

		public void UpdateLastAccess() {
			LastAccessFrame = DrawState.CurrentFrame;
		}

		public ulong GetHash(SpriteInfo info) {
			using (Lock.Shared) {
				ulong hash = Hash;
				if (hash == default) {
					hash = info.Hash;
					using (Lock.Promote) {
						Hash = hash;
					}
				}
				return hash;
			}
		}
	}
}
