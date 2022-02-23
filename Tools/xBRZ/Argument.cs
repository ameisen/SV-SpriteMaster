using LinqFasterer;
using System.Text.RegularExpressions;

namespace xBRZ;

readonly record struct Argument(string Key, string? Value = null) {
	internal readonly bool IsCommand => Key[0] is '-' or '/';
	private static readonly Regex CommandPattern = new(@"^(?:--|-|/)(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
	internal readonly string? Command => IsCommand ? CommandPattern.Match(Key).Groups.ElementAtOrDefaultF(1)?.Value : null;

	public readonly override string ToString() => Value is null ? Key : $"{Key}={Value}";
}
