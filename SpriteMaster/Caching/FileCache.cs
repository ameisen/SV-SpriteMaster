﻿using Microsoft.Toolkit.HighPerformance;
using SpriteMaster.Extensions;
using SpriteMaster.Resample;
using SpriteMaster.Tasking;
using SpriteMaster.Types;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace SpriteMaster.Caching;

[SuppressUnmanagedCodeSecurity]
static class FileCache {
	private const string TextureCacheName = "TextureCache";
	private const string JunctionCacheName = $"{TextureCacheName}_Current";
	private static readonly Version AssemblyVersion = typeof(Resampler).Assembly.GetName().Version!;
	private static readonly Version RuntimeVersion = typeof(Runtime).Assembly.GetName().Version!;
	private static readonly ulong AssemblyHash = AssemblyVersion.GetSafeHash().Fuse(RuntimeVersion.GetSafeHash()).Unsigned();
	private static readonly string CacheName = $"{TextureCacheName}_{AssemblyVersion}";
	private static readonly string LocalDataPath = Path.Combine(Config.LocalRoot, CacheName);
	private static readonly string DumpPath = Path.Combine(LocalDataPath, "dump");

	private static readonly bool SystemCompression = false;

	private static readonly ThreadedTaskScheduler TaskScheduler = new(threadNameFunction: index => $"FileCache IO Thread #{index}");
	private static readonly TaskFactory TaskFactory = new(TaskScheduler);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string GetPath(params string[] path) => Path.Combine(LocalDataPath, Path.Combine(path));

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string GetDumpPath(params string[] path) => Path.Combine(DumpPath, Path.Combine(path));

	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	private struct CacheHeader {
		internal ulong Assembly = AssemblyHash;
		internal ulong ConfigHash = SerializeConfig.ConfigHash;
		internal ulong DataHash;
		internal Vector2I Size;
		internal Vector2I Padding;
		internal Vector2I BlockPadding;
		internal TextureFormat Format;
		[MarshalAs(UnmanagedType.U4)]
		internal Compression.Algorithm Algorithm;
		internal uint RefScale;
		internal uint UncompressedDataLength;
		internal uint DataLength;
		internal Vector2B Wrapped;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal static CacheHeader Read(BinaryReader reader) {
			CacheHeader newHeader = new();
			var newHeaderSpan = MemoryMarshal.CreateSpan(ref newHeader, 1).Cast<CacheHeader, byte>();
			reader.Read(newHeaderSpan);

			return newHeader;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal void Write(BinaryWriter writer) {
			var headerSpan = MemoryMarshal.CreateSpan(ref this, 1).Cast<CacheHeader, byte>();

			writer.Write(headerSpan);
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal void Validate(string path) {
			if (Assembly != AssemblyHash) {
				throw new IOException($"Texture Cache File out of date '{path}'");
			}

			if (Format == TextureFormat.None) {
				throw new InvalidDataException($"Illegal compression format in cached texture '{path}'");
			}
		}
	}

	enum SaveState {
		Saving = 0,
		Saved = 1
	}

	private sealed class Profiler {
		private readonly object Lock = new();
		private ulong FetchCount = 0UL;
		private ulong SumFetchTime = 0UL;
		private ulong StoreCount = 0UL;
		private ulong SumStoreTime = 0UL;

		internal ulong MeanFetchTime {
			get {
				lock (Lock) {
					return SumFetchTime / FetchCount;
				}
			}
		}

		internal ulong MeanStoreTime {
			get {
				lock (Lock) {
					return SumStoreTime / StoreCount;
				}
			}
		}

		internal Profiler() { }

		internal ulong AddFetchTime(ulong time) {
			lock (Lock) {
				++FetchCount;
				SumFetchTime += time;
				return SumFetchTime / FetchCount;
			}
		}

		internal ulong AddStoreTime(ulong time) {
			lock (Lock) {
				++StoreCount;
				SumStoreTime += time;
				return SumStoreTime / StoreCount;
			}
		}
	}
	private static readonly Profiler CacheProfiler = Config.FileCache.Profile && Config.FileCache.Enabled ? new() : null!;

	private static readonly ConcurrentDictionary<string, SaveState> SavingMap = Config.FileCache.Enabled ? new() : null!;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool Fetch(
		string path,
		out uint refScale,
		out Vector2I size,
		out TextureFormat format,
		out Vector2B wrapped,
		out Vector2I padding,
		out Vector2I blockPadding,
		out Span<byte> data
	) {
		refScale = 0;
		size = Vector2I.Zero;
		format = TextureFormat.Color;
		wrapped = Vector2B.False;
		padding = Vector2I.Zero;
		blockPadding = Vector2I.Zero;
		data = null;

		if (Config.FileCache.Enabled && File.Exists(path)) {
			int retries = Config.FileCache.LockRetries;

			while (retries-- > 0) {
				if (SavingMap.TryGetValue(path, out var state) && state != SaveState.Saved) {
					Thread.Sleep(Config.FileCache.LockSleepMS);
					continue;
				}

				// https://stackoverflow.com/questions/1304/how-to-check-for-file-lock
				static bool WasLocked(IOException ex) {
					var errorCode = Marshal.GetHRForException(ex) & (1 << 16) - 1;
					return errorCode is (32 or 33);
				}

				try {
					long start_time = Config.FileCache.Profile ? DateTime.Now.Ticks : 0L;

					using (var reader = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
						var header = CacheHeader.Read(reader);
						header.Validate(path);

						refScale = header.RefScale;
						size = header.Size;
						format = header.Format;
						wrapped = header.Wrapped;
						padding = header.Padding;
						blockPadding = header.BlockPadding;
						var uncompressedDataLength = header.UncompressedDataLength;
						var dataLength = header.DataLength;
						var dataHash = header.DataHash;

						var rawData = reader.ReadBytes((int)dataLength);

						if (rawData.Hash() != dataHash) {
							throw new IOException($"Cache File '{path}' is corrupted");
						}

						data = rawData.Decompress((int)uncompressedDataLength, header.Algorithm);
					}

					if (Config.FileCache.Profile) {
						var mean_ticks = CacheProfiler.AddFetchTime((ulong)(DateTime.Now.Ticks - start_time));
						Debug.InfoLn($"Mean Time Per Fetch: {(double)mean_ticks / TimeSpan.TicksPerMillisecond} ms");
					}

					return true;
				}
				catch (Exception ex) {
					switch (ex) {
						case FileNotFoundException _:
						case EndOfStreamException _:
						case IOException iox when !WasLocked(iox):
						default:
							ex.PrintWarning();
							try { File.Delete(path); } catch { }
							return false;
						case IOException iox when WasLocked(iox):
							Debug.TraceLn($"File was locked when trying to load cache file '{path}': {ex} - {ex.Message} [{retries} retries]");
							Thread.Sleep(Config.FileCache.LockSleepMS);
							break;
					}
				}
			}
		}
		return false;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool Save(
		string path,
		uint refScale,
		Vector2I size,
		TextureFormat format,
		Vector2B wrapped,
		Vector2I padding,
		Vector2I blockPadding,
		ReadOnlySpan<byte> data
	) {
		if (!Config.FileCache.Enabled) {
			return true;
		}
		if (!SavingMap.TryAdd(path, SaveState.Saving)) {
			return false;
		}

		TaskFactory.StartNew(obj => {
			var data = (byte[])obj!;
			Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;
			using var _ = new AsyncTracker($"File Cache Write {path}");
			bool failure = false;
			try {
				long start_time = Config.FileCache.Profile ? DateTime.Now.Ticks : 0L;

				using (var writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))) {
					if (!writer.BaseStream.CanWrite) {
						failure = true;
						return;
					}
					var algorithm = SystemCompression && !Config.FileCache.ForceCompress ? Compression.Algorithm.None : Config.FileCache.Compress;

					var compressedData = data.Compress(algorithm);

					if (compressedData.Length >= data.Length) {
						compressedData = data;
						algorithm = Compression.Algorithm.None;
					}

					new CacheHeader() {
						Algorithm = algorithm,
						RefScale = refScale,
						Size = size,
						Format = format,
						Wrapped = wrapped,
						Padding = padding,
						BlockPadding = blockPadding,
						UncompressedDataLength = (uint)data.Length,
						DataLength = (uint)compressedData.Length,
						DataHash = compressedData.Hash()
					}.Write(writer);

					writer.Write(compressedData);

					if (Config.FileCache.Profile) {
						writer.Flush();
					}
				}
				SavingMap.TryUpdate(path, SaveState.Saved, SaveState.Saving);

				if (Config.FileCache.Profile) {
					var mean_ticks = CacheProfiler.AddStoreTime((ulong)(DateTime.Now.Ticks - start_time));
					Debug.InfoLn($"Mean Time Per Store: {(double)mean_ticks / TimeSpan.TicksPerMillisecond} ms");
				}
			}
			catch (IOException ex) {
				Debug.TraceLn($"Failed to write texture cache file '{path}': {ex.Message}");
				failure = true;
			}
			catch (Exception ex) {
				Debug.WarningLn($"Internal Error writing texture cache file '{path}': {ex} - {ex.Message}");
				failure = true;
			}
			if (failure) {
				try { File.Delete(path); } catch { }
				SavingMap.TryRemove(path, out var _);
			}
		}, data.ToArray()); // TODO : eliminate this copy
		return true;
	}

	enum LinkType : int {
		File = 0,
		Directory = 1
	}

	[DllImport("kernel32.dll")]
	static extern bool CreateSymbolicLink(string Link, string Target, LinkType Type);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	private static bool IsSymbolic(string path) => new FileInfo(path).Attributes.HasFlag(FileAttributes.ReparsePoint);

	static FileCache() {
		Config.FileCache.Compress = Compression.GetPreferredAlgorithm(Config.FileCache.Compress);

		// Delete any old caches.
		try {
			foreach (var root in new[] { Config.LocalRoot }) {
				var directories = Directory.EnumerateDirectories(root);
				foreach (var directory in directories) {
					try {
						if (!Directory.Exists(directory)) {
							continue;
						}
						if (IsSymbolic(directory)) {
							continue;
						}
						var endPath = Path.GetFileName(directory);
						if (Config.FileCache.Purge || (endPath != CacheName && endPath != JunctionCacheName)) {
							// If it doesn't match, it's outdated and should be deleted.
							Directory.Delete(directory, true);
						}
					}
					catch { /* Ignore failures */ }
				}
			}
		}
		catch { /* Ignore failures */ }

		if (Config.FileCache.Enabled) {
			try {
				// Create the directory path
				Directory.CreateDirectory(LocalDataPath);
			}
			catch (Exception ex) {
				ex.PrintWarning();
			}
			try {
				if (Runtime.IsWindows) {
					// Use System compression if it is preferred and no other compression algorithm is supported for some reason.
					// https://stackoverflow.com/questions/624125/compress-a-folder-using-ntfs-compression-in-net
					if (Config.FileCache.PreferSystemCompression || (int)Config.FileCache.Compress <= (int)Compression.Algorithm.Deflate) {
						SystemCompression = NTFS.CompressDirectory(LocalDataPath);
					}
				}
			}
			catch (Exception ex) {
				ex.PrintWarning();
			}
		}

		try {
			Directory.CreateDirectory(LocalDataPath);
			if (Config.Debug.Sprite.DumpReference || Config.Debug.Sprite.DumpResample) {
				Directory.CreateDirectory(DumpPath);
			}
		}
		catch (Exception ex) {
			ex.PrintWarning();
		}

		// Set up a symbolic link to aid in debugging.
		try {
			Directory.Delete(Path.Combine(Config.LocalRoot, JunctionCacheName), false);
		}
		catch { /* Ignore failure */ }
		try {
			CreateSymbolicLink(
				Link: Path.Combine(Config.LocalRoot, JunctionCacheName),
				Target: Path.Combine(LocalDataPath),
				Type: LinkType.Directory
			);
		}
		catch { /* Ignore failure */ }
	}
}