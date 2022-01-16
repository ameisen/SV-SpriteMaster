using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Extensions;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using static SpriteMaster.Runtime;

#nullable enable

namespace SpriteMaster;
static class Debug {
	private static readonly string ModuleName = typeof(Debug).Namespace!;

	private static class Color {
		internal const ConsoleColor Trace = ConsoleColor.Gray;
		internal const ConsoleColor Info = ConsoleColor.White;
		internal const ConsoleColor Warning = ConsoleColor.Yellow;
		internal const ConsoleColor Error = ConsoleColor.Red;
		internal const ConsoleColor Fatal = ConsoleColor.Red;
	}

	private static readonly string LocalLogPath = Path.Combine(Config.LocalRoot, $"{ModuleName}.log");
	private static readonly StreamWriter? LogFile = null;

	private static readonly object IOLock = new();

	static Debug() {
		if (Config.Debug.Logging.OwnLogFile) {
			// For some reason, on Linux it breaks if the log file could not be created?
			try {
				Directory.CreateDirectory(Path.GetDirectoryName(LocalLogPath)!);
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

	[DebuggerStepThrough, DebuggerHidden()]
	private static string ParseException(Exception exception) {
		var output = new StringBuilder();
		output.AppendLine($"Exception: {exception.GetType().Name} : {exception.Message}\n{exception.GetStackTrace()}");
		Exception currentException = exception;
		var exceptionSet = new HashSet<Exception>() { exception };
		while (currentException.InnerException is not null && exceptionSet.Add(currentException.InnerException)) {
			var innerException = currentException.InnerException;
			output.AppendLine("---");
			output.AppendLine($"InnerException: {innerException.GetType().Name} : {innerException.Message}\n{innerException.GetStackTrace()}");
			currentException = innerException;
		}
		return output.ToString();
	}

	[DebuggerStepThrough, DebuggerHidden()]
	private static string Format(this string memberName, bool format = true) {
		return (!format || memberName is null) ? "" : $"[{memberName}] ";
	}

	[DebuggerStepThrough, DebuggerHidden()]
	internal static bool CheckLogLevel(LogLevel logLevel) => Config.Debug.Logging.LogLevel <= logLevel;

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden()]
	internal static void Trace(string message, bool format = true, [CallerMemberName] string caller = null!) {
		if (!CheckLogLevel(LogLevel.Trace))
			return;
		DebugWriteStr($"{caller.Format(format)}{message}", LogLevel.Trace);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden()]
	internal static void Trace<T>(T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (!CheckLogLevel(LogLevel.Trace))
			return;
		TraceLn(ParseException(exception), caller: caller);
	}

	[Conditional("TRACE"), DebuggerStepThrough, DebuggerHidden()]
	internal static void Trace<T>(string message, T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (!CheckLogLevel(LogLevel.Trace))
			return;
		TraceLn($"{message}\n{ParseException(exception)}", caller: caller);
	}

	[Conditional("DEBUG"), DebuggerStepThrough, DebuggerHidden()]
	internal static void TraceLn(string message, bool format = true, [CallerMemberName] string caller = null!) {
		Trace($"{message}\n", format, caller);
	}

	[Conditional("TRACE"), DebuggerStepThrough, DebuggerHidden()]
	internal static void Info(string message, bool format = true, [CallerMemberName] string caller = null!) {
		if (!CheckLogLevel(LogLevel.Debug))
			return;
		DebugWriteStr($"{caller.Format(format)}{message}", LogLevel.Debug);
	}

	[Conditional("TRACE"), DebuggerStepThrough, DebuggerHidden()]
	internal static void Info<T>(T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (!CheckLogLevel(LogLevel.Debug))
			return;
		InfoLn(ParseException(exception), caller: caller);
	}

	[Conditional("TRACE"), DebuggerStepThrough, DebuggerHidden()]
	internal static void Info<T>(string message, T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (!CheckLogLevel(LogLevel.Debug))
			return;
		InfoLn($"{message}\n{ParseException(exception)}", caller: caller);
	}

	[Conditional("TRACE"), DebuggerStepThrough, DebuggerHidden()]
	internal static void InfoLn(string message, bool format = true, [CallerMemberName] string caller = null!) {
		Info($"{message}\n", format, caller);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Hot)]
	internal static void Message(string message, bool format = true, [CallerMemberName] string caller = null!) {
		if (!CheckLogLevel(LogLevel.Info))
			return;
		DebugWriteStr($"{caller.Format(format)}{message}", LogLevel.Info);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Hot)]
	internal static void Message<T>(T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (!CheckLogLevel(LogLevel.Info))
			return;
		MessageLn(ParseException(exception));
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Hot)]
	internal static void MessageLn(string message, bool format = true, [CallerMemberName] string caller = null!) {
		Message($"{message}\n", format, caller);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void Warning(string message, bool format = true, [CallerMemberName] string caller = null!) {
		if (!CheckLogLevel(LogLevel.Warn))
			return;
		DebugWrite(LogLevel.Warn, $"{caller.Format(format)}{message}");
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void Warning<T>(T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (!CheckLogLevel(LogLevel.Warn))
			return;
		WarningLn(ParseException(exception), caller: caller);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void WarningLn(string message, bool format = true, [CallerMemberName] string caller = null!) {
		Warning($"{message}\n", format, caller);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void Error(string message, bool format = true, [CallerMemberName] string caller = null!) {
		if (!CheckLogLevel(LogLevel.Error))
			return;
		DebugWrite(LogLevel.Error, $"{caller.Format(format)}{message}");
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void Error<T>(T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (!CheckLogLevel(LogLevel.Error))
			return;
		ErrorLn(ParseException(exception), caller: caller);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void Error<T>(string message, T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (!CheckLogLevel(LogLevel.Error))
			return;
		ErrorLn($"{message}\n{ParseException(exception)}", caller: caller);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void ErrorLn(string message, bool format = true, [CallerMemberName] string caller = null!) {
		Error($"{message}\n", format, caller);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void Fatal(string message, bool format = true, [CallerMemberName] string caller = null!) {
		try {
			if (!CheckLogLevel(LogLevel.Alert))
				return;
			DebugWrite(LogLevel.Alert, $"{caller.Format(format)}{message}");
		}
		finally {
			if (!Config.ForcedDisable) {
				DebugWrite(LogLevel.Alert, "Fatal Error encountered, shutting down SpriteMaster");
				Config.ForcedDisable = true;
			}
		}
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void Fatal<T>(T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (!CheckLogLevel(LogLevel.Alert))
			return;
		FatalLn(ParseException(exception), caller: caller);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void Fatal<T>(string message, T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (!CheckLogLevel(LogLevel.Alert))
			return;
		FatalLn($"{message}\n{ParseException(exception)}", caller: caller);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void FatalLn(string message, bool format = true, [CallerMemberName] string caller = null!) {
		Fatal($"{message}\n", format, caller);
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void ConditionalError(bool condition, string message, bool format = true, [CallerMemberName] string caller = null!) {
		if (condition) {
			Error(message: message, format: format, caller: caller);
		}
		else {
			Trace(message: message, format: format, caller: caller);
		}
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void ConditionalError<T>(bool condition, T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (condition) {
			Error<T>(exception: exception, caller: caller);
		}
		else {
			Trace<T>(exception: exception, caller: caller);
		}
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void ConditionalError<T>(bool condition, string message, T exception, [CallerMemberName] string caller = null!) where T : Exception {
		if (condition) {
			Error<T>(message: message, exception: exception, caller: caller);
		}
		else {
			Trace(message: message, caller: caller);
		}
	}

	[DebuggerStepThrough, DebuggerHidden()]
	[MethodImpl(MethodImpl.Cold)]
	internal static void ConditionalErrorLn(bool condition, string message, bool format = true, [CallerMemberName] string caller = null!) {
		if (condition) {
			ErrorLn(message: message, format: format, caller: caller);
		}
		else {
			TraceLn(message: message, format: format, caller: caller);
		}
	}


	[DebuggerStepThrough, DebuggerHidden()]
	internal static void Flush() => _ = Console.Error.FlushAsync();

	internal static void DumpMemory() {
		lock (Console.Error) {
			var duplicates = new Dictionary<string, List<Texture2D>>();
			bool haveDuplicates = false;

			var textureDump = ScaledTexture.SpriteMap.GetDump();
			long totalSize = 0;
			long totalOriginalSize = 0;
			ErrorLn("Texture Dump:");
			foreach (var list in textureDump) {
				var referenceTexture = list.Key;
				long originalSize = (referenceTexture.Area() * sizeof(int));
				bool referenceDisposed = referenceTexture.IsDisposed;
				totalOriginalSize += referenceDisposed ? 0 : originalSize;
				ErrorLn($"SpriteSheet: {referenceTexture.SafeName().Enquote()} :: Original Size: {originalSize.AsDataSize()}{(referenceDisposed ? " [DISPOSED]" : "")}");

				if (!referenceTexture.Anonymous() && !referenceTexture.IsDisposed) {
					if (!duplicates.TryGetValue(referenceTexture.SafeName(), out var duplicateList)) {
						duplicateList = new List<Texture2D>();
						duplicates.Add(referenceTexture.SafeName(), duplicateList);
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
						size += subDuplicate.Area() * sizeof(int);
					}

					ErrorLn($"\t{duplicate.Key.Enquote()} :: {duplicate.Value.Count.Delimit()} duplicates :: Total Size: {size.AsDataSize()}");
				}
			}

			Console.Error.Flush();
		}
	}

	[DebuggerStepThrough, DebuggerHidden()]
	private static ConsoleColor GetColor(this LogLevel @this) {
		return @this switch {
			LogLevel.Debug => Color.Trace,
			LogLevel.Info => Color.Info,
			LogLevel.Warn => Color.Warning,
			LogLevel.Error => Color.Error,
			LogLevel.Alert => Color.Fatal,
			_ => ConsoleColor.White,
		};
	}

	[DebuggerStepThrough, DebuggerHidden()]
	private static void DebugWrite(LogLevel level, string str) {
		if (LogFile != null) {
			try {
				var prefix = level switch {
					LogLevel.Debug => 'T',
					LogLevel.Info => 'I',
					LogLevel.Warn => 'W',
					LogLevel.Error => 'E',
					LogLevel.Alert => 'F',
					_ => '?',
				};

				LogFile.Write($"[{prefix}] {str}");
			}
			catch { /* ignore errors */ }
		}

		var originalColor = Console.ForegroundColor;
		Console.ForegroundColor = level.GetColor();
		try {
			DebugWriteStr(str, level);
		}
		finally {
			Console.ForegroundColor = originalColor;
		}
	}

	[DebuggerStepThrough, DebuggerHidden()]
	private static void DebugWriteStr(string str, LogLevel level) {
		var lines = str.Lines(removeEmpty: true);
		var fullString = string.Join("\n", lines);
		lock (IOLock) {
			SpriteMaster.Self.Monitor.Log(fullString, level);
			/*
			foreach (var line in lines) {
				SpriteMaster.Self.Monitor.Log(line.TrimEnd(), level);
			}
			*/
		}

	}
}
