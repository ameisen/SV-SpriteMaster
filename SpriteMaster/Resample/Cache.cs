using SpriteMaster.Extensions;
using SpriteMaster.Types;
using System;
using System.IO;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TeximpNet.Compression;

namespace SpriteMaster.Resample {
	internal static class Cache {
		private static readonly string TextureCacheName = "TextureCache";
		private static readonly string JunctionCacheName = $"{TextureCacheName}_Current";
		private static readonly Version AssemblyVersion = typeof(Upscaler).Assembly.GetName().Version;
		private static readonly string CacheName = $"{TextureCacheName}_{AssemblyVersion.ToString()}";
		private static readonly string LocalDataPath = Path.Combine(Config.LocalRoot, CacheName);
		private static readonly string DumpPath = Path.Combine(LocalDataPath, "dump");

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static string GetPath (params string[] path) {
			return Path.Combine(LocalDataPath, Path.Combine(path));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static string GetDumpPath (params string[] path) {
			return Path.Combine(DumpPath, Path.Combine(path));
		}

		internal static bool Fetch (
			string path,
			out Vector2I size,
			out TextureFormat format,
			out Vector2B wrapped,
			out Vector2I padding,
			out Vector2I blockPadding,
			out int[] data
		) {
			if (Config.Cache.Enabled && File.Exists(path)) {
				int retries = Config.Cache.LockRetries;

				while (retries-- > 0) {
					if (File.Exists(path)) {
						// https://stackoverflow.com/questions/1304/how-to-check-for-file-lock
						bool WasLocked (in IOException ex) {
							var errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);
							return errorCode == 32 || errorCode == 33;
						}

						try {
							using (var reader = new BinaryReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
								var version = reader.ReadString();
								if (version != AssemblyVersion.ToString()) {
									throw new IOException($"Texture Cache File out of date'{path}'");
								}
								size.X = reader.ReadInt32();
								size.Y = reader.ReadInt32();
								var getFormat = TextureFormat.Get((CompressionFormat)reader.ReadInt32());
								if (!getFormat.HasValue) {
									throw new InvalidDataException("Illegal compression format in cached texture");
								}
								format = getFormat.Value;
								wrapped.X = reader.ReadBoolean();
								wrapped.Y = reader.ReadBoolean();
								padding.X = reader.ReadInt32();
								padding.Y = reader.ReadInt32();
								blockPadding.X = reader.ReadInt32();
								blockPadding.Y = reader.ReadInt32();

								var remainingSize = reader.BaseStream.Length - reader.BaseStream.Position;
								data = new int[remainingSize / sizeof(int)];

								foreach (int i in 0.Until(data.Length)) {
									data[i] = reader.ReadInt32();
								}
							}
							return true;
						}
						catch (IOException ex) {
							if (WasLocked(ex)) {
								Debug.InfoLn($"File was locked when trying to load cache file '{path}': {ex.Message} [{retries} retries]");
								Thread.Sleep(Config.Cache.LockSleepMS);
							}
							else {
								Debug.WarningLn($"IOException when trying to load cache file '{path}': {ex.Message}");
								retries = 0;
							}
						}
					}
				}
			}
			size = Vector2I.Zero;
			format = TextureFormat.Color;
			wrapped = Vector2B.False;
			padding = Vector2I.Zero;
			blockPadding = Vector2I.Zero;
			data = null;
			return false;
		}

		internal static bool Save (
			string path,
			Vector2I size,
			TextureFormat format,
			Vector2B wrapped,
			Vector2I padding,
			Vector2I blockPadding,
			int[] data
		) {
			if (Config.Cache.Enabled) {
				try {
					using (var writer = new BinaryWriter(File.OpenWrite(path))) {
						writer.Write(AssemblyVersion.ToString());
						writer.Write(size.X);
						writer.Write(size.Y);
						writer.Write((int)(CompressionFormat)format);
						writer.Write(wrapped.X);
						writer.Write(wrapped.Y);
						writer.Write(padding.X);
						writer.Write(padding.Y);
						writer.Write(blockPadding.X);
						writer.Write(blockPadding.Y);

						foreach (var v in data) {
							writer.Write(v);
						}

						return true;
					}
				}
				catch { }
			}
			return false;
		}

		enum LinkType : int {
			File = 0,
			Directory = 1
		}

		[DllImport("kernel32.dll")]
		static extern bool CreateSymbolicLink (string Link, string Target, LinkType Type);

		private static bool IsSymbolic (string path) {
			var pathInfo = new FileInfo(path);
			return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
		}

		static Cache () {
			// Delete any old caches.
			try {
				foreach (var root in new string[] { Config.LocalRoot }) {
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
							if (endPath != CacheName && endPath != JunctionCacheName) {
								// If it doesn't match, it's outdated and should be deleted.
								Directory.Delete(directory, true);
							}
						}
						catch { /* Ignore failures */ }
					}
				}
			}
			catch { /* Ignore failures */ }

			if (Config.Cache.Enabled) {
				// Create the directory path
				Directory.CreateDirectory(LocalDataPath);

				// Mark the directory as compressed because this is very space wasteful and we are currently not performing compression.
				// https://stackoverflow.com/questions/624125/compress-a-folder-using-ntfs-compression-in-net
				try {
					var dir = new DirectoryInfo(LocalDataPath);
					if ((dir.Attributes & FileAttributes.Compressed) == 0) {
						var objectPath = $"Win32_Directory.Name='{dir.FullName.Replace("\\", @"\\").TrimEnd('\\')}'";
						using (ManagementObject obj = new ManagementObject(objectPath)) {
							using (obj.InvokeMethod("Compress", null, null)) {
								// I don't really care about the return value, 
								// if we enabled it great but it can also be done manually
								// if really needed
							}
						}
					}
				}
				catch { /* Ignore failures */ }
			}

			Directory.CreateDirectory(LocalDataPath);
			if (Config.Debug.Sprite.DumpReference || Config.Debug.Sprite.DumpResample) {
				Directory.CreateDirectory(DumpPath);
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
}
