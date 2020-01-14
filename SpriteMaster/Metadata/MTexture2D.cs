using SpriteDictionary = System.Collections.Generic.Dictionary<ulong, SpriteMaster.ScaledTexture>;

using System.Runtime.Caching;
using System.Threading;
using System;
using Ionic.Zlib;
using System.IO;

using Microsoft.Xna.Framework.Graphics;

using SpriteMaster.Types;
using SpriteMaster.Extensions;
using System.Reflection;

namespace SpriteMaster.Metadata {
	internal sealed class MTexture2D {
		internal static readonly SharedLock DataCacheLock = new SharedLock();
		private static MemoryCache DataCache = (Config.MemoryCache.Enabled) ? new MemoryCache(name: "DataCache", config: null) : null;
		private static long CurrentID = 0U;

		public readonly SpriteDictionary SpriteTable = new SpriteDictionary();
		private readonly string UniqueIDString = Interlocked.Increment(ref CurrentID).ToString();

		private readonly SharedLock Lock = new SharedLock();

		private volatile int CompressorCount = 0;
		private readonly Semaphore CompressionSemaphore = new Semaphore(int.MaxValue, int.MaxValue);

		public volatile bool TracePrinted = false;
		public Volatile<ulong> UpdateToken { get; private set; } = 0;

		internal static void PurgeDataCache() {
			if (!Config.MemoryCache.Enabled) {
				return;
			}

			using (DataCacheLock.Shared) {
				DataCache.Dispose();
				DataCache = new MemoryCache(name: "DataCache", config: null);
			}
		}

		public Volatile<ulong> LastAccessFrame { get; private set; } = (ulong)DrawState.CurrentFrame;
		internal Volatile<ulong> Hash { get; private set; } = Hashing.Default;

		private static readonly MethodInfo ZlibBaseCompressBuffer;

		static MTexture2D() {
			ZlibBaseCompressBuffer = typeof(ZlibStream).Assembly.GetType("Ionic.Zlib.ZlibBaseStream").GetMethod("CompressBuffer", BindingFlags.Static | BindingFlags.Public);
			if (ZlibBaseCompressBuffer == null) {
				throw new NullReferenceException(nameof(ZlibBaseCompressBuffer));
			}
		}

		private static byte[] StreamCompress (byte[] data) {
			using (var val = new MemoryStream()) {
				using (var compressor = new System.IO.Compression.DeflateStream(val, System.IO.Compression.CompressionLevel.Optimal)) {
					return val.ToArray();
				}
			}
		}

		private static byte[] StreamDecompress (byte[] data) {
			using (var val = new MemoryStream()) {
				using (var compressor = new System.IO.Compression.DeflateStream(val, System.IO.Compression.CompressionMode.Decompress)) {
					return val.ToArray();
				}
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
				case Config.MemoryCache.Algorithm.COMPRESS:
					return StreamCompress(data);
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
				case Config.MemoryCache.Algorithm.COMPRESS:
					return StreamDecompress(data);
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

		public unsafe void Purge (Texture2D reference, Bounds? bounds, DataRef<byte> data) {
			lock (this) {
				if (data.IsNull) {
					CachedData = null;
					return;
				}

				var refSize = unchecked((int)reference.SizeBytes());

				bool forcePurge = false;

				try {
					// TODO : lock isn't granular enough.
					if (Config.MemoryCache.AlwaysFlush) {
						forcePurge = true;
					}
					else if (!bounds.HasValue && data.Offset == 0 && data.Length == refSize) {
						Debug.TraceLn("Overriding MTexture2D Cache in Purge");
						CachedData = data.Data;
					}
					// TODO : This doesn't update the compressed cache.
					else if (!bounds.HasValue && CachedData is var currentData && currentData != null) {
						Debug.TraceLn("Updating MTexture2D Cache in Purge");
						var byteSpan = data.Data;
						using (DataCacheLock.Exclusive) {
							using (Lock.Exclusive) {
								var untilOffset = Math.Min(currentData.Length - data.Offset, data.Length);
								foreach (int i in 0..untilOffset) {
									currentData[i + data.Offset] = byteSpan[i];
								}
								Hash = Hashing.Default;
								CachedData = currentData; // Force it to update the global cache.
							}
						}
					}
					else {
						Debug.TraceLn("Forcing full MTexture2D Purge");
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

		private bool CheckUpdateToken(ulong referenceToken) {
			using (Lock.Shared) {
				return UpdateToken == referenceToken;
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
						using (DataCacheLock.Shared) {
							if (Config.MemoryCache.Type != Config.MemoryCache.Algorithm.None && Config.MemoryCache.Async) {
								using (Lock.Promote) {
									int count = CompressorCount;
									if (count > 0) {
										foreach (int i in 0..count) {
											CompressionSemaphore.WaitOne();
										}
										CompressionSemaphore.Release(count);
										CompressorCount = 0;
									}
								}
							}

							compressedBuffer = DataCache[UniqueIDString] as byte[];
							if (compressedBuffer != null) {
								target = Decompress(compressedBuffer);
							}
							else {
								target = null;
							}
						}
						using (Lock.Promote) {
							_CachedData.SetTarget(target);
						}
					}
					return target;
				}
			}
			set {
				try {
					if (!Config.MemoryCache.Enabled)
						return;

					ulong currentUpdateToken;
					using (Lock.Exclusive) {
						currentUpdateToken = UpdateToken;
						UpdateToken = currentUpdateToken + 1;
					}

					TracePrinted = false;

					using (Lock.Shared) {
						//if (_CachedData.TryGetTarget(out var target) && target == value) {
						//	return;
						//}
						if (value == null) {
							using (Lock.Promote) {
								_CachedData.SetTarget(null);
							}
							using (DataCacheLock.Exclusive) {
								DataCache.Remove(UniqueIDString);
							}
						}
						else {
							bool queueCompress = false;

							// I suspect this is completing AFTER we get a call to purge again, and so is overwriting what is the correct data.
							// Doesn't explain why _not_ purging helps, though.
							if (Config.MemoryCache.Type != Config.MemoryCache.Algorithm.None && Config.MemoryCache.Async) {
								if (queueCompress) {
									using (Lock.Promote) {
										++CompressorCount;
										CompressionSemaphore.WaitOne();
										ThreadPool.QueueUserWorkItem((buffer) => {
											Thread.CurrentThread.Priority = ThreadPriority.Lowest;
											try {
												if (!CheckUpdateToken(currentUpdateToken)) {
													return;
												}
												var compressedData = Compress((byte[])buffer);
												using (DataCacheLock.Exclusive) {
													using (Lock.Exclusive) {
														if (currentUpdateToken != UpdateToken) {
															return;
														}

														DataCache[UniqueIDString] = compressedData;
													}
												}
											}
											finally {
												CompressionSemaphore.Release();
											}
										}, value);
									}
								}
							}
							else {
								DataCache[UniqueIDString] = Compress(value);
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
				if (hash == Hashing.Default) {
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
