using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SpriteMaster {
	// https://stackoverflow.com/a/11898531
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	public sealed class UntracedAttribute : Attribute { }

	public static class DebugExtensions {
		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		public static bool IsUntraced (this MethodBase method) {
			return method.IsDefined(typeof(UntracedAttribute), true);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
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
		private const ConsoleColor InfoColor = ConsoleColor.White;
		private const ConsoleColor WarningColor = ConsoleColor.Yellow;
		private const ConsoleColor ErrorColor = ConsoleColor.Red;

		private static readonly string LocalLogPath = Path.Combine(Config.LocalRoot, $"{ModuleName}.log");
		private static readonly StreamWriter LogFile = null;

		static Debug () {
			if (Config.Debug.Logging.OwnLogFile) {
				// For some reason, on Linux it breaks if the log file could not be created?
				try {
					Directory.CreateDirectory(Path.GetDirectoryName(LocalLogPath));
					LogFile = new StreamWriter(
						path: LocalLogPath,
						append: false
					);
				}
				catch {
					WarningLn($"Could not create log file at {LocalLogPath}");
				}
			}
		}

		// Logging Stuff

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		static private string Format(this string memberName, bool format = true) {
			return (!format || memberName == null) ? "" : $"[{memberName}] ";
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), Untraced]
		static internal void Info (string message, bool format = true, [CallerMemberName] string caller = null) {
			if (!Config.Debug.Logging.LogInfo)
				return;
			Console.Error.DebugWriteStr($"{caller.Format(format)}{message}");
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), Untraced]
		static internal void Info<T>(T exception, [CallerMemberName] string caller = null) where T : Exception {
			if (!Config.Debug.Logging.LogInfo)
				return;
			InfoLn($"Exception: {exception.Message}", caller: caller);
			InfoLn(exception.GetStackTrace(), caller: caller);
		}

		[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden(), Untraced]
		static internal void InfoLn (string message, bool format = true, [CallerMemberName] string caller = null) {
			Info($"{message}\n", format, caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		static internal void Warning (string message, bool format = true, [CallerMemberName] string caller = null) {
			if (!Config.Debug.Logging.LogWarnings)
				return;
			Console.Error.DebugWrite(WarningColor, $"{caller.Format(format)}{message}");
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		static internal void Warning<T> (T exception, [CallerMemberName] string caller = null) where T : Exception {
			if (!Config.Debug.Logging.LogInfo)
				return;
			WarningLn($"Exception: {exception.Message}", caller: caller);
			WarningLn(exception.GetStackTrace(), caller: caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		static internal void WarningLn (string message, bool format = true, [CallerMemberName] string caller = null) {
			Warning($"{message}\n", format, caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		static internal void Error (string message, bool format = true, [CallerMemberName] string caller = null) {
			if (!Config.Debug.Logging.LogErrors)
				return;
			Console.Error.DebugWrite(ErrorColor, $"{caller.Format(format)}{message}");
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		static internal void Error<T> (T exception, [CallerMemberName] string caller = null) where T : Exception {
			if (!Config.Debug.Logging.LogInfo)
				return;
			ErrorLn($"Exception: {exception.Message}", caller: caller);
			ErrorLn(exception.GetStackTrace(), caller: caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		static internal void ErrorLn (string message, bool format = true, [CallerMemberName] string caller = null) {
			Error($"{message}\n", format, caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		static internal void Flush () {
			Console.Error.FlushAsync();
		}

		static internal void DumpMemory () {
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

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		static private void DebugWrite (this TextWriter writer, ConsoleColor color, string str) {
			lock (writer) {
				if (LogFile != null) {
					try {
						LogFile.Write(str);
					}
					catch { /* ignore errors */ }
				}

				Console.ForegroundColor = color;
				try {
					writer.DebugWriteStr(str, color);
				}
				finally {
					Console.ResetColor();
				}
			}
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		static private void DebugWriteStr (this TextWriter writer, string str, ConsoleColor color = ConsoleColor.White) {
			if (Config.Debug.Logging.UseSMAPI) {
				var logLevel = color switch {
					InfoColor => LogLevel.Info,
					WarningColor => LogLevel.Warn,
					_ => LogLevel.Error,
				};
				SpriteMaster.Self.Monitor.Log(str.TrimEnd(), logLevel);
			}
			else {
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
}
