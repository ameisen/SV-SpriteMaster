using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using XRectangle = Microsoft.Xna.Framework.Rectangle;

namespace SpriteMaster {
	static class Helpers {
		internal static void ConditionalSet<T> (this ref T obj, bool conditional, in T value) where T : struct {
			if (conditional) {
				obj = value;
			}
		}

		internal static T AddRef<T> (this T type) where T : Type {
			return (T)type.MakeByRefType();
		}

		internal static T RemoveRef<T> (this T type) where T : Type {
			return (T)(type.IsByRef ? type.GetElementType() : type);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		internal static void PrintInfo<T> (this T exception, [CallerMemberName] string caller = null) where T : Exception {
			Debug.Info(exception: exception, caller: caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		internal static void PrintWarning<T> (this T exception, [CallerMemberName] string caller = null) where T : Exception {
			Debug.Warning(exception: exception, caller: caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		internal static void PrintError<T> (this T exception, [CallerMemberName] string caller = null) where T : Exception {
			Debug.Error(exception: exception, caller: caller);
		}

		[DebuggerStepThrough, DebuggerHidden(), Untraced]
		internal static void Print<T> (this T exception, [CallerMemberName] string caller = null) where T : Exception {
			exception.PrintWarning(caller);
		}

		internal static int NearestInt (this float v) {
			return (int)Math.Round(v);
		}
		internal static int NearestInt (this double v) {
			return (int)Math.Round(v);
		}
		internal static int NextInt (this float v) {
			return (int)Math.Ceiling(v);
		}
		internal static int NextInt (this double v) {
			return (int)Math.Ceiling(v);
		}
		internal static int TruncateInt (this float v) {
			return (int)v;
		}
		internal static int TruncateInt (this double v) {
			return (int)v;
		}

		internal static int ClampDimension (this int value) {
			return Math.Min(value, Config.ClampDimension);
		}

		internal static Vector2I ClampDimension (this Vector2I value) {
			return value.Min(Config.ClampDimension);
		}

		internal static T Clamp<T> (this T v, T min, T max) where T : IComparable, IComparable<T> {
			if (v.CompareTo(min) < 0)
				return min;
			if (v.CompareTo(max) > 0)
				return max;
			return v;
		}

		internal static bool WithinInclusive<T> (this T v, T min, T max) where T : IComparable, IComparable<T> {
			return (v.CompareTo(min) >= 0 && v.CompareTo(max) <= 0);
		}

		internal static bool WithinExclusive<T> (this T v, T min, T max) where T : IComparable, IComparable<T> {
			return (v.CompareTo(min) > 0 && v.CompareTo(max) < 0);
		}

		internal static bool Within<T> (this T v, T min, T max) where T : IComparable, IComparable<T> {
			return WithinInclusive(v, min, max);
		}

		internal static IEnumerable<int> ToInclusive (this int from, int to) {
			if (from >= to) {
				Swap(ref from, ref to);
			}
			while (from <= to) {
				yield return from++;
			}
		}

		internal static IEnumerable<int> ToExclusive (this int from, int to) {
			while (from < to) {
				yield return from++;
			}
			while (from > to) {
				yield return from--;
			}
		}

		internal static IEnumerable<int> To (this int from, int to) {
			return ToInclusive(from, to);
		}

		internal static IEnumerable<int> Until (this int from, int to) {
			return ToExclusive(from, to);
		}

		internal static IEnumerable<int> For (this int from, int count) {
			return ToExclusive(from, from + count);
		}

		internal static IEnumerable<long> ToInclusive (this long from, long to) {
			if (from < to) {
				while (from <= to) {
					yield return from++;
				}
			}
			else {
				while (from >= to) {
					yield return from--;
				}
			}
		}

		internal static IEnumerable<long> ToExclusive (this long from, long to) {
			while (from < to) {
				yield return from++;
			}
			while (from > to) {
				yield return from--;
			}
		}

		internal static IEnumerable<long> To (this long from, long to) {
			return ToInclusive(from, to);
		}

		internal static IEnumerable<long> Until (this long from, long to) {
			return ToExclusive(from, to);
		}

		internal static void Swap<T> (ref T l, ref T r) {
			(l, r) = (r, l);
		}

		internal static int Area (this Texture2D texture) {
			return texture.Width * texture.Height;
		}

		internal static long SizeBytes (this Texture2D texture) {
			switch (texture.Format) {
				case SurfaceFormat.Dxt1: return texture.Area() / 2;
			}

			long elementSize = texture.Format switch {
				SurfaceFormat.Color => 4,
				SurfaceFormat.Bgr565 => 2,
				SurfaceFormat.Bgra5551 => 2,
				SurfaceFormat.Bgra4444 => 2,
				SurfaceFormat.Dxt3 => 1,
				SurfaceFormat.Dxt5 => 1,
				SurfaceFormat.NormalizedByte2 => 2,
				SurfaceFormat.NormalizedByte4 => 4,
				SurfaceFormat.Rgba1010102 => 4,
				SurfaceFormat.Rg32 => 4,
				SurfaceFormat.Rgba64 => 8,
				SurfaceFormat.Alpha8 => 1,
				SurfaceFormat.Single => 4,
				SurfaceFormat.Vector2 => 8,
				SurfaceFormat.Vector4 => 16,
				SurfaceFormat.HalfSingle => 2,
				SurfaceFormat.HalfVector2 => 4,
				SurfaceFormat.HalfVector4 => 8,
				_ => throw new ArgumentException(nameof(texture))
			};

			return (long)texture.Area() * elementSize;
		}

		internal static long SizeBytes (this ScaledTexture.ManagedTexture2D texture) {
			return (long)texture.Area() * 4;
		}

		internal static Bitmap Resize (this Bitmap source, Vector2I size, System.Drawing.Drawing2D.InterpolationMode filter = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic, bool discard = true) {
			if (size == new Vector2I(source)) {
				return source;
			}
			var output = new Bitmap(size.Width, size.Height);
			try {
				using (var g = Graphics.FromImage(output)) {
					g.InterpolationMode = filter;
					g.DrawImage(source, 0, 0, output.Width, output.Height);
				}
				if (discard) {
					source.Dispose();
				}
				return output;
			}
			catch {
				output.Dispose();
				throw;
			}
		}

		// FNV-1a hash.
		internal static ulong HashFNV1 (this byte[] data) {
			const ulong prime = 0x100000001b3;
			ulong hash = 0xcbf29ce484222325;
			foreach (byte octet in data) {
				hash ^= octet;
				hash *= prime;
			}

			return hash;
		}

		/*
		internal static unsafe ulong HashFNV1<T>(this T[] data) where T : unmanaged
		{
			using (var handle = data.AsMemory().Pin())
			{
				var spannedData = new Span<byte>(handle.Pointer, data.Length * sizeof(T));
				return spannedData.ToArray().HashFNV1();
			}
		}
		*/

		private static xxHashConfig GetHashConfig () {
			var config = new xxHashConfig();
			config.HashSizeInBits = 64;
			return config;
		}
		private static readonly IxxHash HasherXX = xxHashFactory.Instance.Create(GetHashConfig());

		internal static ulong HashXX (this byte[] data) {
			var hashData = HasherXX.ComputeHash(data).Hash;
			return BitConverter.ToUInt64(hashData, 0);
		}

		internal static ulong HashXX (this byte[] data, int start, int length) {
			var hashData = HasherXX.ComputeHash(new MemoryStream(data, start, length)).Hash;
			return BitConverter.ToUInt64(hashData, 0);
		}

		/*
		internal static unsafe ulong HashXX<T>(this T[] data) where T : unmanaged
		{
			using (var handle = data.AsMemory().Pin())
			{
				var spannedData = new Span<byte>(handle.Pointer, data.Length * sizeof(T));
				return spannedData.ToArray().HashXX();
			}
		}
		*/

		internal static ulong Hash (this byte[] data) {
			return data.HashXX();
			//return data.HashFNV1();
		}

		internal static ulong Hash (this byte[] data, int start, int length) {
			return data.HashXX(start, length);
			//return data.HashFNV1();
		}

		/*
		internal static unsafe ulong Hash<T>(this T[] data) where T : unmanaged
		{
			return data.HashXX();
		}
		*/

		internal static bool Matches (this Texture2D texture, in XRectangle rectangle) {
			return rectangle.X == 0 && rectangle.Y == 0 && rectangle.Width == texture.Width && rectangle.Height == texture.Height;
		}

		internal static bool Matches (this Texture2D texture, in Rectangle rectangle) {
			return rectangle.X == 0 && rectangle.Y == 0 && rectangle.Width == texture.Width && rectangle.Height == texture.Height;
		}

		internal static bool Matches (this in XRectangle rectangle, Texture2D texture) {
			return texture.Matches(rectangle);
		}

		internal static bool Matches (this in Rectangle rectangle, Texture2D texture) {
			return texture.Matches(rectangle);
		}

		internal static ulong Hash (this Texture2D texture) {
			// TODO : make sure that the texture's stride is actually 4B * width
			byte[] data = new byte[texture.Width * texture.Height * sizeof(int)];
			texture.GetData(data);
			return data.Hash();
		}

		internal static ulong Hash (this in Rectangle rectangle) {
			return
				((ulong)rectangle.X & 0xFFFF) |
				(((ulong)rectangle.Y & 0xFFFF) << 16) |
				(((ulong)rectangle.Width & 0xFFFF) << 32) |
				(((ulong)rectangle.Height & 0xFFFF) << 48);
		}

		internal static ulong Hash (this in XRectangle rectangle) {
			return
				((ulong)rectangle.X & 0xFFFF) |
				(((ulong)rectangle.Y & 0xFFFF) << 16) |
				(((ulong)rectangle.Width & 0xFFFF) << 32) |
				(((ulong)rectangle.Height & 0xFFFF) << 48);
		}

		internal static ulong Hash (this in Bounds rectangle) {
			return
				((ulong)rectangle.X & 0xFFFF) |
				(((ulong)rectangle.Y & 0xFFFF) << 16) |
				(((ulong)rectangle.Width & 0xFFFF) << 32) |
				(((ulong)rectangle.Height & 0xFFFF) << 48);
		}

		internal static string SafeName (this Texture2D texture) {
			if (texture.Name != null && texture.Name != "") {
				return texture.Name;
			}

			return "Unknown";
		}

		internal static string SafeName (this ScaledTexture texture) {
			if (texture.Name != null && texture.Name != "") {
				return texture.Name;
			}

			return "Unknown";
		}

		internal static XRectangle ClampTo (this in XRectangle source, in XRectangle clamp) {
			var result = new XRectangle(source.X, source.Y, source.Width, source.Height);

			int leftDiff = clamp.Left - result.Left;
			if (leftDiff > 0) {
				result.X += leftDiff;
				result.Width -= leftDiff;
			}

			int topDiff = clamp.Top - result.Top;
			if (topDiff > 0) {
				result.Y += topDiff;
				result.Height -= topDiff;
			}

			int rightDiff = result.Right - clamp.Right;
			if (rightDiff > 0) {
				result.Width -= rightDiff;
			}

			int bottomDiff = result.Bottom - clamp.Bottom;
			if (bottomDiff > 0) {
				result.Height -= bottomDiff;
			}

			return result;
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte (this byte value, int offset) {
			Contract.AssertZero(offset);
			return value;
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte (this ushort value, int offset) {
			Contract.AssertLess(Math.Abs(offset), sizeof(ushort) * 8);
			return unchecked((byte)((value >> offset) & 0xFFU));
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte (this uint value, int offset) {
			Contract.AssertLess(Math.Abs(offset), sizeof(uint) * 8);
			return unchecked((byte)((value >> offset) & 0xFFU));
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte (this ulong value, int offset) {
			Contract.AssertLess(Math.Abs(offset), sizeof(ulong) * 8);
			return unchecked((byte)((value >> offset) & 0xFFU));
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte (this sbyte value, int offset) {
			return ExtractByte(unchecked((byte)value), offset);
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte (this short value, int offset) {
			return ExtractByte(unchecked((ushort)value), offset);
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte (this int value, int offset) {
			return ExtractByte(unchecked((uint)value), offset);
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte (this long value, int offset) {
			return ExtractByte(unchecked((ulong)value), offset);
		}

		internal static bool IsBlank (this string str) {
			return str == null || str == "";
		}

		internal static string GetFullName (this MethodBase method) {
			return method.DeclaringType.Name + "::" + method.Name;
		}

		internal static string GetCurrentMethodName () {
			return MethodBase.GetCurrentMethod().GetFullName();
		}

		internal static string Reverse (this string str) {
			Contract.AssertNotNull(str);

			unsafe {
				fixed (char* p = str) {
					foreach (int i in 0.To(str.Length / 2)) {
						int endIndex = (str.Length - i) - 1;
						Swap(ref p[i], ref p[endIndex]);
					}
				}
			}

			return str;
		}

		internal static string Reversed (this string str) {
			Contract.AssertNotNull(str);
			var strArray = str.ToCharArray().Reverse();
			return new string(strArray);
		}

		internal static T[] Reverse<T> (this T[] array) {
			Contract.AssertNotNull(array);
			Array.Reverse(array);
			return array;
		}

		internal static T[] Reversed<T> (this T[] array) {
			Contract.AssertNotNull(array);
			var result = (T[])array.Clone();
			Array.Reverse(result);
			return result;
		}

		internal static string Enquote (this string str, string quote = "\'") {
			if (str.StartsWith(quote) && str.EndsWith(quote)) {
				return str;
			}
			return quote + str + quote;
		}

		internal static string Delimit (this long number) {
			return number.ToString("G");
		}

		internal static string Delimit (this int number) {
			return number.ToString("G");
		}

		internal static string Delimit (this short number) {
			return number.ToString("G");
		}

		internal static string Delimit (this sbyte number) {
			return number.ToString("G");
		}

		internal static string Delimit (this ulong number) {
			return number.ToString("G");
		}

		internal static string Delimit (this uint number) {
			return number.ToString("G");
		}

		internal static string Delimit (this ushort number) {
			return number.ToString("G");
		}

		internal static string Delimit (this byte number) {
			return number.ToString("G");
		}

		internal static string Delimit (this long number, string delimiter = ",", uint delimitCount = 3) {
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit (this int number, string delimiter = ",", uint delimitCount = 3) {
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit (this short number, string delimiter = ",", uint delimitCount = 3) {
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit (this sbyte number, string delimiter = ",", uint delimitCount = 3) {
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit (this ulong number, string delimiter = ",", uint delimitCount = 3) {
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit (this uint number, string delimiter = ",", uint delimitCount = 3) {
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit (this ushort number, string delimiter = ",", uint delimitCount = 3) {
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit (this byte number, string delimiter = ",", uint delimitCount = 3) {
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		private static string Delimit (this string valueString, string delimiter, uint delimitCount) {
			Contract.AssertPositive(delimitCount);
			Contract.AssertTrue(delimiter.IsNormalized());

			delimiter = delimiter.Reversed();

			string result = "";
			char[] reversedString = valueString.ToCharArray().Reverse();
			foreach (int i in 0.Until(reversedString.Length)) {
				if (i != 0 && Char.IsNumber(reversedString[i]) && (i % delimitCount) == 0) {
					result += delimiter;
				}
				result += reversedString[i];
			}

			return result.Reverse().Normalize();
		}

		internal enum DataFormat {
			IEC,
			JEDEC,
			Metric
		}

		private static readonly string[] DecimalSuffixTable =
		{
			"B",
			"KB",
			"MB",
			"GB",
			"TB",
			"PB",
			"EB",
			"ZB",
			"YB",
			"HB"
		};

		private static readonly string[] BinarySuffixTable =
		{
			"B",
			"KiB",
			"MiB",
			"GiB",
			"TiB",
			"PiB",
			"EiB",
			"ZiB",
			"YiB",
			"HiB"
		};

		internal static string AsDataSize (this long value, DataFormat format = DataFormat.IEC, int decimals = 2) {
			Contract.AssertNotNegative(value);
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize (this int value, DataFormat format = DataFormat.IEC, int decimals = 2) {
			Contract.AssertNotNegative(value);
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize (this short value, DataFormat format = DataFormat.IEC, int decimals = 2) {
			Contract.AssertNotNegative(value);
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize (this sbyte value, DataFormat format = DataFormat.IEC, int decimals = 2) {
			Contract.AssertNotNegative(value);
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize (this uint value, DataFormat format = DataFormat.IEC, int decimals = 2) {
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize (this ushort value, DataFormat format = DataFormat.IEC, int decimals = 2) {
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize (this byte value, DataFormat format = DataFormat.IEC, int decimals = 2) {
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize (this ulong number, DataFormat format = DataFormat.IEC, int decimals = 2) {
			Contract.AssertNotNegative(decimals);
			uint fraction = (format == DataFormat.Metric) ? 1000U : 1024U;

			var SuffixTable = (format == DataFormat.IEC) ? BinarySuffixTable : DecimalSuffixTable;

			// Maintain fraction?
			double value = (double)number;
			// TODO : This can be done in constant time, but meh.
			int suffixIndex = 0;
			while (value >= fraction && suffixIndex < SuffixTable.Length) {
				value /= fraction;
				++suffixIndex;
			}

			return string.Format("{0:0.00}", value) + $" {SuffixTable[suffixIndex]}";
		}

		internal static T GetValue<T> (this FieldInfo field, in object instance) {
			var result = field.GetValue(instance);
			return (T)result;
		}
	}
}
