using LinqFasterer;
using Pastel;
using SpriteMaster.Types;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions;

static class String {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string ToString<T>(this T obj, in System.Drawing.Color color) => obj.ToString().Pastel(color);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsEmpty(this string str) => str.Length == 0;

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool IsBlank(this string str) => string.IsNullOrEmpty(str);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static unsafe string Reverse(this string str) {
		Contracts.AssertNotNull(str);

		fixed (char* p = str) {
			for (int i = 0; i < str.Length / 2; ++i) {
				int endIndex = (str.Length - i) - 1;
				(p[endIndex], p[i]) = (p[i], p[endIndex]);
			}
		}

		return str;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string Reversed(this string str) {
		Contracts.AssertNotNull(str);
		var strArray = str.ToCharArray().Reverse();
		return new(strArray);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string Enquote(this string str, char quote = '\'') {
		if (str.Length >= 2 && str[0] == quote && str[^1] == quote) {
			return str;
		}
		return $"{quote}{str}{quote}";
	}

	private static readonly char[] NewlineChars = new[] { '\n', '\r' };
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static IEnumerable<string> Lines(this string str, bool removeEmpty = false) {
		var strings = str.Split(NewlineChars);
		var validLines = removeEmpty ? strings.WhereF(l => !l.IsBlank()) : strings.WhereF(l => l is not null);
		return validLines;
	}
}
