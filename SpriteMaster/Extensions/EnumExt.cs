using System;
using System.Collections.Generic;

namespace SpriteMaster.Extensions;

/// <summary>
/// Enumerator Extensions
/// </summary>
internal static class EnumExt {
	/// <summary>
	/// Return an array name and value pairs representing the enum
	/// </summary>
	/// <typeparam name="TEnum">Enumerator Type</typeparam>
	/// <returns>Array of name-value pairs</returns>
	internal static KeyValuePair<string, TEnum>[] Get<TEnum>() where TEnum : struct, Enum {
		var names = Enum.GetNames(typeof(TEnum));
		var result = new KeyValuePair<string, TEnum>[names.Length];
		for (int i = 0; i < names.Length; ++i) {
			result[i] = new(names[i], Enum.Parse<TEnum>(names[i]));
		}
		return result;
	}

	/// <summary>
	/// Return an array name and value pairs representing the enum
	/// </summary>
	/// <param name="type">Enumerator Type</param>
	/// <returns>Array of name-value pairs</returns>
	internal static KeyValuePair<string, int>[] Get(Type type) {
		var names = Enum.GetNames(type);
		var result = new KeyValuePair<string, int>[names.Length];
		for (int i = 0; i < names.Length; ++i) {
			result[i] = new(names[i], (int)Enum.Parse(type, names[i]));
		}
		return result;
	}
}
