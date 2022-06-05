using SpriteMaster.Types;
using StardewModdingAPI;
using System;
using System.Diagnostics;
using System.Text;

namespace SpriteMaster;

internal static partial class Debug {
	[DebuggerStepThrough, DebuggerHidden]
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

	[DebuggerStepThrough, DebuggerHidden]
	private static void DebugWrite(LogLevel level, string str) {
		if (LogFile is not null) {
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

	private static readonly ObjectPool<StringBuilder> StringBuilderPool = new(1);

	[DebuggerStepThrough, DebuggerHidden]
	private static void DebugWriteStr(string str, LogLevel level) {
		if (str.Contains("\n\n")) {
			using var builder = StringBuilderPool.GetSafe();

			builder.Value.EnsureCapacity(str.Length);

			char lastChar = '\0';
			foreach (var c in str)
			{
				if (c == '\n' && lastChar == '\n') {
					continue;
				}

				lastChar = c;
				builder.Value.Append(c);
			}

			str = builder.Value.ToString();
		}
		lock (IOLock) {
			SpriteMaster.Self.Monitor.Log(str, level);
		}

	}
}
