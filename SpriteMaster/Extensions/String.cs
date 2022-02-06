﻿using LinqFasterer;
using Pastel;
using SpriteMaster.Types;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions;

static class String {
	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string ToString<T>(this T? obj, in System.Drawing.Color color) => obj?.ToString().Pastel(color) ?? "[null]".Pastel(color);

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
				var temp = p[endIndex];
				p[endIndex] = p[i];
				p[i] = temp;
			}
		}

		return str;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static string Reversed(this string str) {
		Contracts.AssertNotNull(str);
		return new string(str).Reverse();
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

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal static bool EqualsInsensitive(this string str1, string str2) => str1.Equals(str2, System.StringComparison.InvariantCultureIgnoreCase);
}
