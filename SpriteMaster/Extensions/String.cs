using System;

namespace SpriteMaster.Extensions {
	internal static class String {
		internal static bool IsBlank (this string str) {
			return str == null || str == "";
		}

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
	}
}
