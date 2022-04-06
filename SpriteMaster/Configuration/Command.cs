using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SpriteMaster.Configuration;

static class Command {
	private static Serialize.Category Root => Serialize.Root;

	[CommandAttribute("config", "Config Commands")]
	public static void OnConsoleCommand(string command, Queue<string> arguments) {
		if (arguments.Count == 0) {
			EmitHelp(new());
			return;
		}

		var subCommand = arguments.Dequeue();

		switch (subCommand.ToLowerInvariant()) {
			case "help":
			case "h":
				EmitHelp(arguments);
				break;
			case "query":
			case "q":
				Query(arguments);
				break;
			case "set":
			case "s":
				Set(arguments);
				break;
		}
	}

	private static void EmitHelp(Queue<string> arguments) {
		if (arguments.Count == 0) {

		}
	}

	private static void DumpCategory(Serialize.Category category) {
		var output = new StringBuilder();
		if (category.Children.Count != 0) {
			output.AppendLine("Categories:");
			foreach (var subCategory in category.Children) {
				output.AppendLine($"\t{subCategory.Value.Name}");
			}
			if (category.Fields.Count != 0) {
				output.AppendLine();
			}
		}
		if (category.Fields.Count != 0) {
			output.AppendLine("Fields:");
			foreach (var field in category.Fields) {
				output.AppendLine($"\t{field.Value.Name}");
			}
		}
		Debug.Info(output.ToString());
	}

	private static List<Serialize.Category> GetHierarchy(Serialize.Category category) {
		if (category == Root) {
			return new List<Serialize.Category>();
		}

		var result = new List<Serialize.Category>() { category };

		var parent = category.Parent;
		while (parent is not null) {
			if (parent == Root) {
				break;
			}
			result.Add(parent);
			parent = parent.Parent;
		}

		result.Reverse();
		return result;
	}

	private static void Query(Queue<string> arguments) {
		if (arguments.Count == 0) {
			DumpCategory(Root);
			return;
		}

		var chainString = arguments.Dequeue();
		var chain = chainString.Split('.');

		Serialize.Category category = Root;

		foreach (var arg in chain) {
			var key = arg.ToLowerInvariant();

			if (category.Children.TryGetValue(key, out var subCategory)) {
				category = subCategory;
			}
			else if (category.Fields.TryGetValue(key, out var field)) {
				if (arguments.Count != 0) {
					Debug.Warning($"Unknown Category: {string.Join('.', GetHierarchy(category).Select(cat => cat.Name))}.{key} (found matching field)");
					return;
				}

				var fieldValue = field.GetValue(null);
				if (field.FieldType.IsEnum) {
					var validNames = string.Join(", ", Enum.GetNames(field.FieldType));
					Debug.Info($"{string.Join('.', GetHierarchy(category).Select(cat => cat.Name))}.{field.Name} = '{fieldValue}' ({field.FieldType.Name}: {validNames})");
				}
				else {
					Debug.Info($"{string.Join('.', GetHierarchy(category).Select(cat => cat.Name))}.{field.Name} = '{fieldValue}' ({field.FieldType.Name})");
				}
				return;
			}
			else {
				Debug.Warning($"Unknown Category: {string.Join('.', GetHierarchy(category).Select(cat => cat.Name))}.{key}");
				return;
			}
		}

		DumpCategory(category);
	}

	private static void Set(Queue<string> arguments) {
		if (arguments.Count == 0) {
			Debug.Warning("Nothing passed to set");
			return;
		}

		var chainString = arguments.Dequeue();
		if (arguments.Count == 0) {
			Debug.Warning("No value passed to set");
			return;
		}
		var value = arguments.Dequeue();
		var chain = chainString.Split('.');

		Serialize.Category category = Root;
		FieldInfo? field = null;

		foreach (var arg in chain) {
			var key = arg.ToLowerInvariant();

			if (category.Children.TryGetValue(key, out var subCategory)) {
				category = subCategory;
			}
			else if (category.Fields.TryGetValue(key, out field)) {
				if (arguments.Count != 0) {
					Debug.Warning($"Unknown Category: {string.Join('.', GetHierarchy(category).Select(cat => cat.Name))}.{arg} (found matching field)");
					return;
				}
			}
			else {
				Debug.Warning($"Unknown Field or Category: {string.Join('.', GetHierarchy(category).Select(cat => cat.Name))}.{arg}");
				return;
			}
		}
		if (field is null) {
			Debug.Warning($"Field not found: {chainString}");
			return;
		}

		if (field.FieldType == typeof(string)) {
			field.SetValue(null, value);
		}
		else if (field.FieldType.IsEnum) {
			var enumValue = Enum.Parse(field.FieldType, value);
			field.SetValue(null, enumValue);
		}
		else if (
			field.FieldType == typeof(byte) ||
			field.FieldType == typeof(ushort) ||
			field.FieldType == typeof(uint) ||
			field.FieldType == typeof(ulong)
		) {
			if (!ulong.TryParse(value, out var intValue)) {
				Debug.Warning($"Could not parse '{value}' as an unsigned integer");
			}
			field.SetValue(null, Convert.ChangeType(intValue, field.FieldType));
		}
		else if (
			field.FieldType == typeof(sbyte) ||
			field.FieldType == typeof(short) ||
			field.FieldType == typeof(int) ||
			field.FieldType == typeof(long)
		) {
			if (!long.TryParse(value, out var intValue)) {
				Debug.Warning($"Could not parse '{value}' as a signed integer");
			}
			field.SetValue(null, Convert.ChangeType(intValue, field.FieldType));
		}
		else if (field.FieldType == typeof(float) || field.FieldType == typeof(double)) {
			if (!double.TryParse(value, out var realValue)) {
				Debug.Warning($"Could not parse '{value}' as a floating-point value");
			}
			field.SetValue(null, Convert.ChangeType(realValue, field.FieldType));
		}
		else {
			throw new NotImplementedException($"Type not yet implemented: {field.FieldType}");
		}

		var options = field.GetCustomAttribute<Attributes.Options>();
		if (options is not null) {
			if (options.Flags.HasFlag(Attributes.Options.Flag.FlushTextureCache)) {
				Harmonize.Patches.TextureCache.Flush(reset: true);
			}
			if (options.Flags.HasFlag(Attributes.Options.Flag.FlushSuspendedSpriteCache)) {
				Caching.SuspendedSpriteCache.Purge();
			}
			if (options.Flags.HasFlag(Attributes.Options.Flag.FlushFileCache)) {
				Caching.FileCache.Purge(reset: true);
			}
			if (options.Flags.HasFlag(Attributes.Options.Flag.FlushResidentCache)) {
				Caching.ResidentCache.Purge();
			}
			if (options.Flags.HasFlag(Attributes.Options.Flag.ResetDisplay)) {
				// TODO
			}
			if (options.Flags.HasFlag(Attributes.Options.Flag.GarbageCollect)) {
				Extensions.Garbage.Collect(compact: true, blocking: true, background: false);
			}
			if (options.Flags.HasFlag(Attributes.Options.Flag.FlushMetaData)) {
				Metadata.Metadata.Purge();
			}
			if (options.Flags.HasFlag(Attributes.Options.Flag.GarbageCollect)) {
				Extensions.Garbage.Collect(compact: true, blocking: true, background: false);
			}
		}
	}
}
