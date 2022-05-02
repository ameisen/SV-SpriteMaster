using System.Linq;
using System.Text;

namespace SpriteMaster;

static partial class ConsoleSupport {
	private static void InvokeHelp(string? unknownCommand = null) {
		var output = new StringBuilder();
		output.AppendLine();
		output.AppendLine(Versioning.StringHeader);
		if (unknownCommand is not null) {
			output.AppendLine($"Unknown Command: '{unknownCommand}'");
		}
		output.AppendLine("Help Command Guide");
		output.AppendLine();

		int maxKeyLength = CommandMap.Keys.Max(k => k.Length);

		foreach (var kv in CommandMap) {
			output.AppendLine($"{kv.Key.PadRight(maxKeyLength)} : {kv.Value.Description}");
		}

		Debug.Message(output.ToString());
	}
}
