using GenericModConfigMenu;
using SpriteMaster.Extensions;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SpriteMaster.Configuration;

static class GMCM {
	private static volatile bool Initialized = false;

	internal static void Initialize(IModHelper help) {
		if (Initialized) {
			throw new Exception("GMCM already initialized");
		}
		Initialized = true;

		// https://github.com/spacechase0/StardewValleyMods/tree/develop/GenericModConfigMenu#for-c-mod-authors

		var configMenu = help.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
		if (configMenu is null)
			return;

		configMenu.Register(
			mod: SpriteMaster.Self.ModManifest,
			reset: Reset,
			save: Save
		);

		ProcessCategory(
			parent: null,
			category: Serialize.Root,
			advanced: false,
			manifest: SpriteMaster.Self.ModManifest,
			config: configMenu
		);

		ProcessCategory(
			parent: null,
			category: Serialize.Root,
			advanced: true,
			manifest: SpriteMaster.Self.ModManifest,
			config: configMenu
		);
	}

	private static void Reset() {
		if (Config.DefaultConfig is not null) {
			Serialize.Load(Config.DefaultConfig, retain: true);
			Config.DefaultConfig.Position = 0;
		}
	}

	private static void Save() {
		Serialize.Save(Configuration.Config.Path);
	}

	private static bool Hidden(FieldInfo field, bool advanced) {
		if (field.GetAttribute<Attributes.GMCMHiddenAttribute>(out var _)) {
			return true;
		}

		if (!advanced && field.GetCustomAttribute<Attributes.AdvancedAttribute>() is not null) {
			return true;
		}

		if (!IsFieldRepresentable(field)) {
			return true;
		}

		return Hidden(field.DeclaringType, advanced);
	}

	private static bool Hidden(Type? type, bool advanced) {
		if (type is null) {
			return false;
		}

		if (type.GetAttribute<Attributes.GMCMHiddenAttribute>(out var _)) {
			return true;
		}

		if (!advanced && type.GetCustomAttribute<Attributes.AdvancedAttribute>() is not null) {
			return true;
		}

		return Hidden(type.DeclaringType, advanced);
	}

	private static readonly string[][] Prefixes = {
		new[]{ "B" },
		new[]{ "KiB", "KB", "K" },
		new[]{ "MiB", "MB", "M" },
		new[]{ "GiB", "GB", "G" },
		new[]{ "TiB", "TB", "T" },
		new[]{ "PiB", "PB", "P" }
	};

	/// <summary>
	/// Returns the value to the given order of magnitude (power of 10)
	/// </summary>
	/// <param name="value">Value to get to the power of 10</param>
	/// <param name="order">What order of magnitude to return</param>
	/// <returns>Order-of-magnitude-adjusted value</returns>
	/// <exception cref="ArgumentOutOfRangeException"></exception>
	private static long SizeOrder(long value, int order) {
		if (order < 0) {
			throw new ArgumentOutOfRangeException(nameof(order), "parameter must not be negative");
		}
		var shift = 10 * order;

		// 1024^1 = (1024 << 0)
		// 1024^2 = (1024 << 10)
		// 1024^3 = (1024 << 20)
		return checked(value << (10 * order));
	}

	private static string FormatLong(long value) {
		string? Magnitude(int order, bool force = false) {
			if (force || value < SizeOrder(1024, order + 1)) {
				// This uses doubles because we want to get a fraction
				var divisor = SizeOrder(1024, order);
				var dValue = (double)value / divisor;
				var prefix = Prefixes[Math.Min(order, Prefixes.Length - 1)][0];
				return $"{dValue.ToString("0.##")} {prefix}";
			}
			return null;
		}
		return
			Magnitude(0) ??
			Magnitude(1) ??
			Magnitude(2) ??
			Magnitude(3) ??
			Magnitude(4) ??
			Magnitude(5, true)!;
	}

	private static long ParseLong(string value) {
		(int Order, string Prefix)? prefixResult = null;
		for (int i = 0; (prefixResult?.Order ?? 0) == 0 && i < Prefixes.Length; ++i) {
			foreach (var p in Prefixes[i]) {
				if (value.EndsWith(p, StringComparison.InvariantCultureIgnoreCase)) {
					prefixResult = (i + 1, p);
					break;
				}
			}
		}

		if (prefixResult is null) {
			return long.Parse(value);
		}

		value = value.Substring(value.Length - prefixResult.Value.Prefix.Length);

		long resultValue;

		// Try it as a pure integral-value
		if (long.TryParse(value, out long intValue)) {
			intValue *= SizeOrder(1024, prefixResult.Value.Order);
			resultValue = intValue;
		}
		// Otherwise, try it as a decimal value
		else if (double.TryParse(value, out double realValue)) {
			realValue *= SizeOrder(1024, prefixResult.Value.Order);
			resultValue = realValue.RoundToLong();
		}
		else {
			throw new FormatException($"Could not parse '{value}' as a numeric value");
		}

		return resultValue;
	}

	private static bool IsFieldRepresentable(FieldInfo field) {
		return
			field.FieldType == typeof(bool) ||
			field.FieldType == typeof(byte) ||
			field.FieldType == typeof(sbyte) ||
			field.FieldType == typeof(ushort) ||
			field.FieldType == typeof(short) ||
			field.FieldType == typeof(int) ||
			field.FieldType == typeof(float) ||
			field.FieldType == typeof(double) ||
			field.FieldType == typeof(string) ||
			field.FieldType == typeof(SButton) ||
			field.FieldType.IsEnum ||
			field.FieldType == typeof(long);
	}

	private static string FormatName(string name) {
		var result = new StringBuilder();

		char prevC = ' ';
		for (int i = 0; i < name.Length; ++i) {
			char c = name[i];
			if (i != 0 && char.IsUpper(c) && !char.IsUpper(prevC)) {
				result.Append(' ');
			}
			result.Append(c);
			prevC = c;
		}

		return result.ToString();
	}

	private static void ProcessField(FieldInfo field, bool advanced, IManifest manifest, IGenericModConfigMenuApi config) {
		if (Hidden(field, advanced)) {
			return;
		}

		var comments = field.GetCustomAttributes<Attributes.CommentAttribute>();
		string? comment = null;
		if (comments.Any()) {
			comment = "";
			foreach (var commentAttribute in comments) {
				comment += commentAttribute.Message;
				comment += '\n';
			}
			comment.Remove(comment.Length - 1);
		}

		var fieldType = field.FieldType;
		var fieldId = $"{field.ReflectedType?.FullName ?? "unknown"}.{field.Name}";
		Func<string> fieldName = () => FormatName(field.Name);
		Func<string>? tooltip = (comment is null) ? null : () => comment;

		if (fieldType == typeof(bool)) {
			config.AddBoolOption(
				mod: manifest,
				getValue: () => Command.GetValue<bool>(field),
				setValue: value => Command.SetValue<bool>(field, value),
				name: fieldName,
				tooltip: tooltip,
				fieldId: fieldId
			);
		}
		else if (fieldType == typeof(byte) || fieldType == typeof(sbyte) || fieldType == typeof(ushort) || fieldType == typeof(short) || fieldType == typeof(int)) {
			var limitAttribute = field.GetCustomAttribute<Attributes.LimitsIntAttribute>();

			config.AddNumberOption(
				mod: manifest,
				getValue: () => Command.GetValue<int>(field),
				setValue: value => Command.SetValue<int>(field, value),
				name: fieldName,
				tooltip: tooltip,
				min: limitAttribute?.GetMin<int>(fieldType),
				max: limitAttribute?.GetMax<int>(fieldType),
				interval: null,
				formatValue: null,
				fieldId: fieldId
			);
		}
		else if (fieldType == typeof(float)) {
			var limitAttribute = field.GetCustomAttribute<Attributes.LimitsRealAttribute>();

			config.AddNumberOption(
				mod: manifest,
				getValue: () => Command.GetValue<float>(field),
				setValue: value => Command.SetValue<float>(field, value),
				name: fieldName,
				tooltip: tooltip,
				min: limitAttribute?.GetMin<float>(),
				max: limitAttribute?.GetMax<float>(),
				interval: null,
				formatValue: null,
				fieldId: fieldId
			);
		}
		else if (fieldType == typeof(double)) {
			var limitAttribute = field.GetCustomAttribute<Attributes.LimitsRealAttribute>();

			config.AddNumberOption(
				mod: manifest,
				getValue: () => (float)Command.GetValue<double>(field),
				setValue: value => Command.SetValue<double>(field, value),
				name: fieldName,
				tooltip: tooltip,
				min: limitAttribute?.GetMin<float>(typeof(double)),
				max: limitAttribute?.GetMax<float>(typeof(double)),
				interval: null,
				formatValue: null,
				fieldId: fieldId
			);
		}
		else if (fieldType == typeof(string)) {
			config.AddTextOption(
				mod: manifest,
				getValue: () => Command.GetValue<string>(field),
				setValue: value => Command.SetValue<string>(field, value),
				name: fieldName,
				tooltip: tooltip,
				allowedValues: null,
				formatAllowedValue: null,
				fieldId: fieldId
			);
		}
		else if (fieldType == typeof(SButton)) {
			config.AddKeybind(
				mod: manifest,
				getValue: () => Command.GetValue<SButton>(field),
				setValue: value => Command.SetValue<SButton>(field, value),
				name: fieldName,
				tooltip: tooltip,
				fieldId: fieldId
			);
		}
		else if (fieldType.IsEnum) {
			Dictionary<int, string> enumMap = new();
			foreach (var enumPairs in EnumExt.Get(fieldType)) {
				_ = enumMap.TryAdd((int)enumPairs.Value, enumPairs.Key);
			}

			config.AddTextOption(
				mod: manifest,
				getValue: () => enumMap[Command.GetValue<int>(field)],
				setValue: value => Command.SetValue<int>(field, (int)Enum.Parse(fieldType, value)),
				name: fieldName,
				tooltip: tooltip,
				allowedValues: Enum.GetNames(fieldType).ToArray(),
				formatAllowedValue: null,
				fieldId: fieldId
			);
		}
		else if (fieldType == typeof(long)) {
			var limitAttribute = field.GetCustomAttribute<Attributes.LimitsIntAttribute>();

			config.AddTextOption(
				mod: manifest,
				getValue: () => FormatLong(Command.GetValue<long>(field)),
				setValue: value => Command.SetValue<long>(
					field,
					Math.Clamp(
						ParseLong(value),
						limitAttribute?.MinValue ?? long.MinValue,
						limitAttribute?.MaxValue ?? long.MaxValue
					)
				),
				name: fieldName,
				tooltip: tooltip,
				allowedValues: null,
				formatAllowedValue: null,
				fieldId: fieldId
			);
		}
		else {
			Debug.Error($"Cannot apply type '{fieldType.Name}' to GMCM");
		}
	}

	private static string? GetCategoryName(Serialize.Category category) {
		if (string.IsNullOrEmpty(category.Name)) {
			return null;
		}

		var names = new List<string>();
		foreach (var currentCategory in category.ParentTraverser) {
			if (!string.IsNullOrEmpty(currentCategory.Name)) {
				names.Add(currentCategory.Name);
			}
		}

		names.Reverse();

		return string.Join('.', names);
	}

	private static bool IsCategoryValid(Serialize.Category category, bool advanced) {
		if (Hidden(category.Type, advanced)) {
			return false;
		}

		foreach (var field in category.Fields.Values) {
			if (!Hidden(field, advanced)) {
				return true;
			}
		}

		foreach (var child in category.Children.Values) {
			if (IsCategoryValid(child, advanced)) {
				return true;
			}
		}

		return false;
	}

	private static void ProcessCategory(Serialize.Category? parent, Serialize.Category category, bool advanced, IManifest manifest, IGenericModConfigMenuApi config) {
		if (parent is null) {
			if (advanced) {
				config.AddPage(
					mod: manifest,
					pageId: "advanced",
					pageTitle: () => "Advanced Settings"
				);
			}
			else {
				config.AddPageLink(
					mod: manifest,
					pageId: "advanced",
					text: () => "[ Advanced Settings ]",
					tooltip: () => "Display Advanced Settings"
				);
				config.AddSectionTitle(mod: manifest, text: () => "");
			}
		}

		if (category.Name.Length != 0) {
			if (category.Type.GetAttribute<Attributes.CommentAttribute>(out var commentAttribute)) {
				config.AddSectionTitle(manifest, () => GetCategoryName(category) ?? "", () => commentAttribute.Message);
			}
			else {
				config.AddSectionTitle(manifest, () => GetCategoryName(category) ?? "");
			}
		}

		foreach (var field in category.Fields.Values) {
			ProcessField(field, advanced, manifest, config);
		}

		foreach (var child in category.Children.Values) {
			if (Hidden(child.Type, advanced)) {
				continue;
			}

			if (!IsCategoryValid(child, advanced)) {
				continue;
			}

			ProcessCategory(
				category,
				child,
				advanced,
				manifest,
				config
			);
		}
	}
}
