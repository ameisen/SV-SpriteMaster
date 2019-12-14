#pragma warning disable 0162

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using AssertionException = System.Exception;
using NotNullReferenceException = System.Exception;
using BooleanException = System.Exception;
using ArgumentNotNullException = System.ArgumentOutOfRangeException;
using ArgumentBooleanException = System.ArgumentOutOfRangeException;

namespace SpriteMaster
{
	static internal class Debug
	{
		private static readonly string ModuleName = typeof(Debug).Namespace;

		private const bool AlwaysFlush = false;
		private const ConsoleColor WarningColor = ConsoleColor.Yellow;
		private const ConsoleColor ErrorColor = ConsoleColor.Red;

		private static readonly string LocalLogPath = Path.Combine(Config.LocalRoot, $"{ModuleName}.log");
		private static readonly StreamWriter LogFile = null;

		static Debug()
		{
			if (Config.Debug.Logging.OwnLogFile)
			{
				LogFile = new StreamWriter(LocalLogPath);
			}
		}

		// Assertion Stuff

		static private bool IsExceptionType(this Type type)
		{
			return type.IsSubclassOf(typeof(Exception));
		}

		static internal class Argument
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool Assert(bool predicate, in string message = "Argument is invalid", Type exception = null)
			{
				return Debug.Assert(predicate, message, exception ?? typeof(ArgumentOutOfRangeException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertNull<T>(in T value, in string message = "Variable is not null", Type exception = null)
			{
				return Assert(value == null, message, exception ?? typeof(ArgumentNotNullException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertNotNull<T>(in T value, in string message = "Variable is null", Type exception = null)
			{
				return Assert(value != null, message, exception ?? typeof(ArgumentNullException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertTrue(in bool value, in string message = "Variable is not true", Type exception = null)
			{
				return Assert(value == true, message, exception ?? typeof(ArgumentBooleanException));
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static internal bool AssertFalse(in bool value, in string message = "Variable is not false", Type exception = null)
			{
				return Assert(value == false, message, exception ?? typeof(ArgumentBooleanException));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgNull<T>(this T value, in string message = null, Type exception = null)
		{
			return Argument.AssertNull(value, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgNotNull<T>(this T value, in string message = null, Type exception = null)
		{
			return Argument.AssertNotNull(value, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertNull<T>(this T value, in string message = "Variable is not null", Type exception = null)
		{
			return Assert(value == null, message, exception ?? typeof(NotNullReferenceException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertNotNull<T>(this T value, in string message = "Variable is null", Type exception = null)
		{
			return Assert(value != null, message, exception ?? typeof(NullReferenceException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgTrue(this in bool value, in string message = null, Type exception = null)
		{
			return Argument.AssertTrue(value, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArgFalse(this in bool value, in string message = null, Type exception = null)
		{
			return Argument.AssertFalse(value, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertTrue(this in bool value, in string message = "Variable is not true", Type exception = null)
		{
			return Assert(value == true, message, exception ?? typeof(BooleanException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertFalse(this in bool value, in string message = "Variable is not false", Type exception = null)
		{
			return Assert(value == false, message, exception ?? typeof(BooleanException));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool AssertArg(bool predicate, in string message = null, Type exception = null)
		{
			return Argument.Assert(predicate, message, exception);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static internal bool Assert(bool predicate, in string message = "Variable's value is invalid", Type exception = null)
		{
			if (!exception.IsExceptionType())
			{
				throw new ArgumentOutOfRangeException("Provided assert exception type is not a subclass of Exception");
			}
			if (!predicate)
			{
				throw (AssertionException)Activator.CreateInstance(exception ?? typeof(AssertionException), new object[] { message });
			}
			return true;
		}

		// Logging Stuff

		static internal void Info(string str)
		{
			if (!Config.Debug.Logging.LogInfo) return;
			Console.Error.DebugWrite(str);
		}

		static internal void InfoLn(string str)
		{
			Info(str + "\n");
		}

		static internal void Warning(string str)
		{
			if (!Config.Debug.Logging.LogWarnings) return;
			Console.Error.DebugWrite(WarningColor, str);
		}

		static internal void WarningLn(string str)
		{
			Warning(str + "\n");
		}

		static internal void Error(string str)
		{
			if (!Config.Debug.Logging.LogErrors) return;
			Console.Error.DebugWrite(ErrorColor, str);
		}

		static internal void ErrorLn(string str)
		{
			Error(str + "\n");
		}

		static internal void DumpMemory()
		{
			if (!Config.Debug.TextureDump.Enabled)
				return;

			lock (Console.Error)
			{
				var duplicates = new Dictionary<string, List<Texture2D>>();
				bool haveDuplicates = false;

				var textureDump = ScaledTexture.TextureMap.GetDump();
				long totalSize = 0;
				long totalOriginalSize = 0;
				ErrorLn("Texture Dump:");
				foreach (var list in textureDump)
				{
					var referenceTexture = list.Key;
					long originalSize = (referenceTexture.Width * referenceTexture.Height * sizeof(int));
					bool referenceDisposed = referenceTexture.IsDisposed;
					totalOriginalSize += referenceDisposed ? 0 : originalSize;
					ErrorLn($"SpriteSheet: {referenceTexture.SafeName().Enquote()} :: Original Size: {originalSize.AsDataSize()}{(referenceDisposed ? " [DISPOSED]" : "")}");

					if (referenceTexture.Name != null && referenceTexture.Name != "" && !referenceTexture.IsDisposed)
					{
						List<Texture2D> duplicateList;
						if (!duplicates.TryGetValue(referenceTexture.Name, out duplicateList))
						{
							duplicateList = new List<Texture2D>();
							duplicates.Add(referenceTexture.Name, duplicateList);
						}
						duplicateList.Add(referenceTexture);
						haveDuplicates = haveDuplicates || (duplicateList.Count > 1);
					}

					foreach (var sprite in list.Value)
					{
						if (sprite.IsReady && sprite.Texture != null)
						{
							var spriteDisposed = sprite.Texture.IsDisposed;
							ErrorLn($"\tSprite: {sprite.OriginalSourceRectangle} :: {sprite.MemorySize.AsDataSize()}{(spriteDisposed ? " [DISPOSED]" : "")}");
							totalSize += spriteDisposed ? 0 : sprite.MemorySize;
						}
					}
				}
				ErrorLn($"Total Resampled Size: {totalSize.AsDataSize()}");
				ErrorLn($"Total Original Size: {totalOriginalSize.AsDataSize()}");
				ErrorLn($"Total Size: {(totalOriginalSize + totalSize).AsDataSize()}");
				
				if (haveDuplicates)
				{
					ErrorLn("Duplicates:");
					foreach (var duplicate in duplicates)
					{
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

		static private void DebugWrite(this TextWriter writer, in ConsoleColor color, in string str)
		{
			lock (writer)
			{
				try
				{
					LogFile.Write(str);
				}
				catch { /* ignore errors */ }

				Console.ForegroundColor = color;
				try
				{
					writer.DebugWrite(str);
				}
				finally
				{
					Console.ResetColor();
				}
			}
		}

		static private void DebugWrite(this TextWriter writer, in string str)
		{
			var strings = str.Split('\n');
			foreach (var line in strings) {
				if (line == "")
				{
					writer.Write("\n");
				}
				else
				{
					writer.Write($"[{ModuleName}] {line}");
				}
			}
			if (AlwaysFlush) writer.FlushAsync();
		}
	}
}
