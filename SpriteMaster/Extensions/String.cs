using SpriteMaster.Types;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Extensions {
	internal static class String {
		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static bool IsBlank (this string str) => str == null || str == "";

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static string Reverse (this string str) {
			Contract.AssertNotNull(str);

			unsafe {
				fixed (char* p = str) {
					foreach (int i in 0.To(str.Length / 2)) {
						int endIndex = (str.Length - i) - 1;
						Common.Swap(ref p[i], ref p[endIndex]);
					}
				}
			}

			return str;
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static string Reversed (this string str) {
			Contract.AssertNotNull(str);
			var strArray = str.ToCharArray().Reverse();
			return new string(strArray);
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static string Enquote (this string str, string quote = "\'") {
			if (str.StartsWith(quote) && str.EndsWith(quote)) {
				return str;
			}
			return $"{quote}{str}{quote}";
		}

		private static readonly char[] NewlineChars = new[] { '\n', '\r' };
		[MethodImpl(Runtime.MethodImpl.Optimize)]
		internal static IEnumerable<string> Lines (this string str, bool removeEmpty = false) {
			var strings = str.Split(NewlineChars);
			var validLines = removeEmpty ? strings.Where(l => (l != null && l.Length > 0)) : strings.Where(l => l != null);
			return validLines;
		}
	}
}
