using System;
using System.Text;

namespace SpriteMaster.Extensions;

static class HashExt {
	internal static int GetSafeHash(this string value) => (int)Hashing.Hash(Encoding.Unicode.GetBytes(value ?? ""));
	//internal static int GetSafeHash(this char[] value) => (int)Hashing.Hash(Encoding.Unicode.GetBytes(value ?? Array.Empty<char>()));
	internal static int GetSafeHash(this StringBuilder value) => value.ToString().GetSafeHash();

	internal static int GetSafeHash<T>(this T value) {
		if (value is string s) {
			return s.GetSafeHash();
		}
		else if (value is StringBuilder v) {
			return v.ToString().GetSafeHash();
		}
		return value.GetHashCode();
	}
}
