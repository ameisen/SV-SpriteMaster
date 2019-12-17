#pragma warning disable 0162

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace SpriteMaster {
	// https://stackoverflow.com/a/11898531
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class UntracedAttribute : Attribute { }

	public static class DebugExtensions {
		public static bool IsUntraced (this MethodBase method) {
			return method.IsDefined(typeof(UntracedAttribute), true);
		}

		[Untraced]
		public static string GetStackTrace (this Exception e) {
			var tracedFrames = new List<StackFrame>();
			foreach (var frame in new StackTrace(e, true).GetFrames()) {
				if (!frame.GetMethod().IsUntraced()) {
					tracedFrames.Add(frame);
				}
			}

			var tracedStrings = new List<string>();
			foreach (var frame in tracedFrames) {
				tracedStrings.Add(new StackTrace(frame).ToString());
			}

			return string.Concat(tracedStrings);
		}
	}

	internal static class Debug {
		private static readonly string ModuleName = typeof(Debug).Namespace;

		private const bool AlwaysFlush = false;
		private const ConsoleColor WarningColor = ConsoleColor.Yellow;
		private const ConsoleColor ErrorColor = ConsoleColor.Red;

		private static readonly string LocalLogPath = Path.Combine(Config.LocalRoot, $"{ModuleName}.log");
		private static readonly StreamWriter LogFile = null;

		static Debug () {
			if (Config.Debug.Logging.OwnLogFile) {
				LogFile = new StreamWriter(LocalLogPath);
			}
		}

		// Logging Stuff

		[Untraced]
		static internal void Info (string str) {
			if (!Config.Debug.Logging.LogInfo)
				return;
			Console.Error.DebugWrite(str);
		}

		[Untraced]
		static internal void InfoLn (string str) {
			Info(str + "\n");
		}

		[Untraced]
		static internal void Warning (string str) {
			if (!Config.Debug.Logging.LogWarnings)
				return;
			Console.Error.DebugWrite(WarningColor, str);
		}

		[Untraced]
		static internal void WarningLn (string str) {
			Warning(str + "\n");
		}

		[Untraced]
		static internal void Error (string str) {
			if (!Config.Debug.Logging.LogErrors)
				return;
			Console.Error.DebugWrite(ErrorColor, str);
		}

		[Untraced]
		static internal void ErrorLn (string str) {
			Error(str + "\n");
		}

		[Untraced]
		static internal void Flush () {
			Console.Error.FlushAsync();
		}

		static internal void DumpMemory () {
			if (!Config.Debug.CacheDump.Enabled)
				return;

			lock (Console.Error) {
				var duplicates = new Dictionary<string, List<Texture2D>>();
				bool haveDuplicates = false;

				var textureDump = ScaledTexture.TextureMap.GetDump();
				long totalSize = 0;
				long totalOriginalSize = 0;
				ErrorLn("Texture Dump:");
				foreach (var list in textureDump) {
					var referenceTexture = list.Key;
					long originalSize = (referenceTexture.Width * referenceTexture.Height * sizeof(int));
					bool referenceDisposed = referenceTexture.IsDisposed;
					totalOriginalSize += referenceDisposed ? 0 : originalSize;
					ErrorLn($"SpriteSheet: {referenceTexture.SafeName().Enquote()} :: Original Size: {originalSize.AsDataSize()}{(referenceDisposed ? " [DISPOSED]" : "")}");

					if (referenceTexture.Name != null && referenceTexture.Name != "" && !referenceTexture.IsDisposed) {
						List<Texture2D> duplicateList;
						if (!duplicates.TryGetValue(referenceTexture.Name, out duplicateList)) {
							duplicateList = new List<Texture2D>();
							duplicates.Add(referenceTexture.Name, duplicateList);
						}
						duplicateList.Add(referenceTexture);
						haveDuplicates = haveDuplicates || (duplicateList.Count > 1);
					}

					foreach (var sprite in list.Value) {
						if (sprite.IsReady && sprite.Texture != null) {
							var spriteDisposed = sprite.Texture.IsDisposed;
							ErrorLn($"\tSprite: {sprite.OriginalSourceRectangle} :: {sprite.MemorySize.AsDataSize()}{(spriteDisposed ? " [DISPOSED]" : "")}");
							totalSize += spriteDisposed ? 0 : sprite.MemorySize;
						}
					}
				}
				ErrorLn($"Total Resampled Size: {totalSize.AsDataSize()}");
				ErrorLn($"Total Original Size: {totalOriginalSize.AsDataSize()}");
				ErrorLn($"Total Size: {(totalOriginalSize + totalSize).AsDataSize()}");

				if (haveDuplicates) {
					ErrorLn("Duplicates:");
					foreach (var duplicate in duplicates) {
						long size = 0;
						foreach (var subDuplicate in duplicate.Value) {
							size += subDuplicate.Width * subDuplicate.Height * sizeof(int);
						}

						ErrorLn($"\t{duplicate.Key.Enquote()} :: {duplicate.Value.Count.Delimit()} duplicates :: Total Size: {size.AsDataSize()}");
					}
				}

				Console.Error.Flush();
			}
		}

		[Untraced]
		static private void DebugWrite (this TextWriter writer, in ConsoleColor color, in string str) {
			lock (writer) {
				try {
					LogFile.Write(str);
				}
				catch { /* ignore errors */ }

				Console.ForegroundColor = color;
				try {
					writer.DebugWrite(str);
				}
				finally {
					Console.ResetColor();
				}
			}
		}

		[Untraced]
		static private void DebugWrite (this TextWriter writer, in string str) {
			var strings = str.Split('\n');
			foreach (var line in strings) {
				if (line == "") {
					writer.Write("\n");
				}
				else {
					writer.Write($"[{ModuleName}] {line}");
				}
			}
			if (AlwaysFlush)
				writer.FlushAsync();
		}
	}
}
