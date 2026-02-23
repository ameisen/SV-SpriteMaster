using SpriteMaster.Extensions;
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
		var originalColor = Console.ForegroundColor;
		Console.ForegroundColor = level.GetColor();
		try {
			DebugWriteStr(str, level);
		}
		finally {
			Console.ForegroundColor = originalColor;
		}
	}

	//[DebuggerStepThrough, DebuggerHidden]
	private static void DebugWriteStr(string str, LogLevel level) {
		if (str.Contains("\n\n")) {
			using var builder = ObjectPoolExt.Take<StringBuilder>(builder => builder.Clear());

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

		lock (IoLock) {
			WriteToLogImpl(str, level);
			Console.WriteLine(str);
		}

	}
}
