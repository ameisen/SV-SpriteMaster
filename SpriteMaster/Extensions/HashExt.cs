using System;
using System.Text;

namespace SpriteMaster.Extensions;

static class HashExt {
	internal static int GetSafeHash(this string value) => (int)Hashing.Hash(Encoding.Unicode.GetBytes(value ?? ""));
	internal static int GetSafeHash(this char[] value) => (int)Hashing.Hash(Encoding.Unicode.GetBytes(value ?? Array.Empty<char>()));
	internal static int GetSafeHash(this StringBuilder value) => value.ToString().GetSafeHash();

	internal static int GetSafeHash<T>(this T value) {
		switch (value) {
			case string str:
				return str.GetSafeHash();
			case char[] array:
				return array.GetSafeHash();
			case StringBuilder sb:
				return sb.GetSafeHash();
			default:
				return value.GetHashCode();
		}
	}
}
