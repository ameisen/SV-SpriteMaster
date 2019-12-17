using Microsoft.Xna.Framework.Graphics;
using SpriteMaster.Types;
using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.Drawing;
using System.IO;
using System.Reflection;

using XRectangle = Microsoft.Xna.Framework.Rectangle;

namespace SpriteMaster
{
	static class Helpers
	{
		internal static int RoundToInt(this float v)
		{
			return (int)Math.Round(v);
		}
		internal static int RoundToInt(this double v)
		{
			return (int)Math.Round(v);
		}
		internal static int RoundToNextInt(this float v)
		{
			return (int)Math.Ceiling(v);
		}
		internal static int RoundToNextInt(this double v)
		{
			return (int)Math.Ceiling(v);
		}
		internal static int TruncateToInt(this float v)
		{
			return (int)v;
		}
		internal static int TruncateToInt(this double v)
		{
			return (int)v;
		}

		internal static int ClampDimension(this int value)
		{
			return Math.Min(value, Config.ClampDimension);
		}

		internal static Vector2I ClampDimension(this in Vector2I value)
		{
			return value.Min(Config.ClampDimension);
		}

		internal static T Clamp<T>(this T v, in T min, in T max) where T : IComparable
		{
			if (v.CompareTo(min) < 0) return min;
			if (v.CompareTo(max) > 0) return max;
			return v;
		}

		internal static bool WithinInclusive<T>(this T v, in T min, in T max) where T : IComparable
		{
			return (v.CompareTo(min) >= 0 && v.CompareTo(max) <= 0);
		}

		internal static bool WithinExclusive<T>(this T v, in T min, in T max) where T : IComparable
		{
			return (v.CompareTo(min) > 0 && v.CompareTo(max) < 0);
		}

		internal static bool Within<T>(this T v, in T min, in T max) where T : IComparable
		{
			return WithinInclusive(v, min, max);
		}

		internal static IEnumerable<int> ToInclusive(this int from, int to)
		{
			if (from < to)
			{
				while (from <= to)
				{
					yield return from++;
				}
			}
			else
			{
				while (from >= to)
				{
					yield return from--;
				}
			}
		}

		internal static IEnumerable<int> ToExclusive(this int from, int to)
		{
			while (from < to)
			{
				yield return from++;
			}
			while (from > to)
			{
				yield return from--;
			}
		}

		internal static IEnumerable<int> To(this int from, int to)
		{
			return ToInclusive(from, to);
		}

		internal static IEnumerable<int> Until(this int from, int to)
		{
			return ToExclusive(from, to);
		}

		internal static IEnumerable<int> For (this int from, int count) {
			return ToExclusive(from, from + count);
		}

		internal static IEnumerable<long> ToInclusive(this long from, long to)
		{
			if (from < to)
			{
				while (from <= to)
				{
					yield return from++;
				}
			}
			else
			{
				while (from >= to)
				{
					yield return from--;
				}
			}
		}

		internal static IEnumerable<long> ToExclusive(this long from, long to)
		{
			while (from < to)
			{
				yield return from++;
			}
			while (from > to)
			{
				yield return from--;
			}
		}

		internal static IEnumerable<long> To(this long from, long to)
		{
			return ToInclusive(from, to);
		}

		internal static IEnumerable<long> Until(this long from, long to)
		{
			return ToExclusive(from, to);
		}

		internal static void Swap<T>(ref T l, ref T r)
		{
			(l, r) = (r, l);
			// T temp = l;
			// l = r;
			// r = temp;
		}

		internal static uint SizeBytes(this Texture2D texture)
		{
			// TODO sizeof(int)
			return (uint)texture.Width * (uint)texture.Height * (uint)sizeof(int);
		}

		internal static Bitmap Resize(this Bitmap source, in Vector2I size, System.Drawing.Drawing2D.InterpolationMode filter = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic, bool discard = true)
		{
			if (size == new Vector2I(source))
			{
				return source;
			}
			var output = new Bitmap(size.Width, size.Height);
			try
			{
				using (var g = Graphics.FromImage(output))
				{
					g.InterpolationMode = filter;
					g.DrawImage(source, 0, 0, output.Width, output.Height);
				}
				if (discard)
				{
					source.Dispose();
				}
				return output;
			}
			catch
			{
				output.Dispose();
				throw;
			}
		}

		// FNV-1a hash.
		internal static ulong HashFNV1(this byte[] data)
		{
			const ulong prime = 0x100000001b3;
			ulong hash = 0xcbf29ce484222325;
			foreach (byte octet in data)
			{
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

		private static xxHashConfig GetHashConfig()
		{
			var config = new xxHashConfig();
			config.HashSizeInBits = 64;
			return config;
		}
		private static readonly IxxHash HasherXX = xxHashFactory.Instance.Create(GetHashConfig());

		internal static ulong HashXX(this byte[] data)
		{
			var hashData = HasherXX.ComputeHash(data).Hash;
			return BitConverter.ToUInt64(hashData, 0);
		}

		internal static ulong HashXX(this byte[] data, in int start, in int length)
		{
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

		internal static ulong Hash(this byte[] data)
		{
			return data.HashXX();
			//return data.HashFNV1();
		}

		internal static ulong Hash(this byte[] data, in int start, in int length)
		{
			return data.HashXX(start, length);
			//return data.HashFNV1();
		}

		/*
		internal static unsafe ulong Hash<T>(this T[] data) where T : unmanaged
		{
			return data.HashXX();
		}
		*/

		internal static bool Matches(this Texture2D texture, in XRectangle rectangle)
		{
			return rectangle.X == 0 && rectangle.Y == 0 && rectangle.Width == texture.Width && rectangle.Height == texture.Height;
		}

		internal static bool Matches(this Texture2D texture, in Rectangle rectangle)
		{
			return rectangle.X == 0 && rectangle.Y == 0 && rectangle.Width == texture.Width && rectangle.Height == texture.Height;
		}

		internal static bool Matches(this in XRectangle rectangle, in Texture2D texture)
		{
			return texture.Matches(rectangle);
		}

		internal static bool Matches(this in Rectangle rectangle, in Texture2D texture)
		{
			return texture.Matches(rectangle);
		}

		internal static ulong Hash(this Texture2D texture)
		{
			// TODO : make sure that the texture's stride is actually 4B * width
			byte[] data = new byte[texture.Width * texture.Height * sizeof(int)];
			texture.GetData(data);
			return data.Hash();
		}

		internal static ulong Hash(this in Rectangle rectangle)
		{
			return
				((ulong)rectangle.X & 0xFFFF) |
				(((ulong)rectangle.Y & 0xFFFF) << 16) |
				(((ulong)rectangle.Width & 0xFFFF) << 32) |
				(((ulong)rectangle.Height & 0xFFFF) << 48);
		}

		internal static ulong Hash(this in XRectangle rectangle)
		{
			return
				((ulong)rectangle.X & 0xFFFF) |
				(((ulong)rectangle.Y & 0xFFFF) << 16) |
				(((ulong)rectangle.Width & 0xFFFF) << 32) |
				(((ulong)rectangle.Height & 0xFFFF) << 48);
		}

		internal static ulong Hash(this in Bounds rectangle)
		{
			return
				((ulong)rectangle.X & 0xFFFF) |
				(((ulong)rectangle.Y & 0xFFFF) << 16) |
				(((ulong)rectangle.Width & 0xFFFF) << 32) |
				(((ulong)rectangle.Height & 0xFFFF) << 48);
		}

		internal static string SafeName(this Texture2D texture)
		{
			if (texture.Name != null && texture.Name != "")
			{
				return texture.Name;
			}

			return "Unknown";
		}

		internal static string SafeName(this ScaledTexture texture)
		{
			if (texture.Name != null && texture.Name != "")
			{
				return texture.Name;
			}

			return "Unknown";
		}

		internal static XRectangle ClampTo(this XRectangle source, in XRectangle clamp)
		{
			var result = new XRectangle(source.X, source.Y, source.Width, source.Height);

			int leftDiff = clamp.Left - result.Left;
			if (leftDiff > 0)
			{
				result.X += leftDiff;
				result.Width -= leftDiff;
			}

			int topDiff = clamp.Top - result.Top;
			if (topDiff > 0)
			{
				result.Y += topDiff;
				result.Height -= topDiff;
			}

			int rightDiff = result.Right - clamp.Right;
			if (rightDiff > 0)
			{
				result.Width -= rightDiff;
			}

			int bottomDiff = result.Bottom - clamp.Bottom;
			if (bottomDiff > 0)
			{
				result.Height -= bottomDiff;
			}

			return result;
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte(this in byte value, in int offset)
		{
			Contract.AssertZero(offset);
			return value;
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte(this in ushort value, in int offset)
		{
			Contract.AssertLess(Math.Abs(offset), sizeof(ushort) * 8);
			return unchecked((byte)((value >> offset) & 0xFFU));
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte(this in uint value, in int offset)
		{
			Contract.AssertLess(Math.Abs(offset), sizeof(uint) * 8);
			return unchecked((byte)((value >> offset) & 0xFFU));
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte(this in ulong value, in int offset)
		{
			Contract.AssertLess(Math.Abs(offset), sizeof(ulong) * 8);
			return unchecked((byte)((value >> offset) & 0xFFU));
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte(this in sbyte value, in int offset)
		{
			return ExtractByte(unchecked((byte)value), offset);
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte(this in short value, in int offset)
		{
			return ExtractByte(unchecked((ushort)value), offset);
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte(this in int value, in int offset)
		{
			return ExtractByte(unchecked((uint)value), offset);
		}

		// Extracts a byte (8 bits) worth of data from a provided value, from the given offset
		// Example: ExtractByte(0x00F0, 8) would return 0xF
		internal static byte ExtractByte(this in long value, in int offset)
		{
			return ExtractByte(unchecked((ulong)value), offset);
		}

		internal static bool IsBlank(this string str)
		{
			return str == null || str == "";
		}

		internal static string GetFullName(this MethodBase method)
		{
			return method.DeclaringType.Name + "::" + method.Name;
		}

		internal static string GetCurrentMethodName()
		{
			return MethodBase.GetCurrentMethod().GetFullName();
		}

		internal static string Reverse(this string str)
		{
			Contract.AssertNotNull(str);

			unsafe
			{
				fixed (char* p = str)
				{
					foreach (int i in 0.To(str.Length / 2))
					{
						int endIndex = (str.Length - i) - 1;
						Swap(ref p[i], ref p[endIndex]);
					}
				}
			}

			return str;
		}

		internal static string Reversed(this string str)
		{
			Contract.AssertNotNull(str);
			var strArray = str.ToCharArray().Reverse();
			return new string(strArray);
		}

		internal static T[] Reverse<T>(this T[] array)
		{
			Contract.AssertNotNull(array);
			Array.Reverse(array);
			return array;
		}

		internal static T[] Reversed<T>(this T[] array)
		{
			Contract.AssertNotNull(array);
			var result = (T[])array.Clone();
			Array.Reverse(result);
			return result;
		}

		internal static string Enquote(this string str, in string quote = "\'")
		{
			if (str.StartsWith(quote) && str.EndsWith(quote))
			{
				return str;
			}
			return quote + str + quote;
		}

		internal static string Delimit(this long number)
		{
			return number.ToString("G");
		}

		internal static string Delimit(this int number)
		{
			return number.ToString("G");
		}

		internal static string Delimit(this short number)
		{
			return number.ToString("G");
		}

		internal static string Delimit(this sbyte number)
		{
			return number.ToString("G");
		}

		internal static string Delimit(this ulong number)
		{
			return number.ToString("G");
		}

		internal static string Delimit(this uint number)
		{
			return number.ToString("G");
		}

		internal static string Delimit(this ushort number)
		{
			return number.ToString("G");
		}

		internal static string Delimit(this byte number)
		{
			return number.ToString("G");
		}

		internal static string Delimit(this long number, string delimiter = ",", in uint delimitCount = 3)
		{
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit(this int number, string delimiter = ",", in uint delimitCount = 3)
		{
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit(this short number, string delimiter = ",", in uint delimitCount = 3)
		{
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit(this sbyte number, string delimiter = ",", in uint delimitCount = 3)
		{
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit(this ulong number, string delimiter = ",", in uint delimitCount = 3)
		{
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit(this uint number, string delimiter = ",", in uint delimitCount = 3)
		{
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit(this ushort number, string delimiter = ",", in uint delimitCount = 3)
		{
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		internal static string Delimit(this byte number, string delimiter = ",", in uint delimitCount = 3)
		{
			return Delimit(number.ToString(), delimiter.Normalize(), delimitCount);
		}

		private static string Delimit(this string valueString, string delimiter, in uint delimitCount)
		{
			Contract.AssertPositive(delimitCount);
			Contract.AssertTrue(delimiter.IsNormalized());

			delimiter = delimiter.Reversed();

			string result = "";
			char[] reversedString = valueString.ToCharArray().Reverse();
			foreach (int i in 0.Until(reversedString.Length))
			{
				if (i != 0 && Char.IsNumber(reversedString[i]) && (i % delimitCount) == 0)
				{
					result += delimiter;
				}
				result += reversedString[i];
			}

			return result.Reverse().Normalize();
		}

		internal enum DataFormat
		{
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

		internal static string AsDataSize(this in long value, in DataFormat format = DataFormat.IEC, int decimals = 2)
		{
			Contract.AssertNotNegative(value);
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize(this in int value, in DataFormat format = DataFormat.IEC, int decimals = 2)
		{
			Contract.AssertNotNegative(value);
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize(this in short value, in DataFormat format = DataFormat.IEC, int decimals = 2)
		{
			Contract.AssertNotNegative(value);
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize(this in sbyte value, in DataFormat format = DataFormat.IEC, int decimals = 2)
		{
			Contract.AssertNotNegative(value);
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize(this in uint value, in DataFormat format = DataFormat.IEC, int decimals = 2)
		{
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize(this in ushort value, in DataFormat format = DataFormat.IEC, int decimals = 2)
		{
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize(this in byte value, in DataFormat format = DataFormat.IEC, int decimals = 2)
		{
			return AsDataSize((ulong)value, format, decimals);
		}

		internal static string AsDataSize(this in ulong number, in DataFormat format = DataFormat.IEC, int decimals = 2)
		{
			Contract.AssertNotNegative(decimals);
			uint fraction = (format == DataFormat.Metric) ? 1000U : 1024U;

			var SuffixTable = (format == DataFormat.IEC) ? BinarySuffixTable : DecimalSuffixTable;

			// Maintain fraction?
			double value = (double)number;
			// TODO : This can be done in constant time, but meh.
			int suffixIndex = 0;
			while (value >= fraction && suffixIndex < SuffixTable.Length)
			{
				value /= fraction;
				++suffixIndex;
			}

			return string.Format("{0:0.00}", value) + $" {SuffixTable[suffixIndex]}";
		}

		internal static T GetValue<T>(this FieldInfo field, in object instance) {
			var result = field.GetValue(instance);
			return (T)result;
		}
	}
}
