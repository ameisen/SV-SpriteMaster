using System;
using System.Collections.Generic;
using System.IO;

namespace MusicMaster;

internal static class DirectoryCleanup {
	internal static void Cleanup() {
		if (Path.GetDirectoryName(typeof(DirectoryCleanup).Assembly.Location) is not { } root) {
			Debug.Trace("Could not determine root directory to perform cleanup");
			return;
		}

		HashSet<string> expected = new(StringComparer.OrdinalIgnoreCase){
			"FastExpressionCompiler.LightExpression",
			"libzstd",
			"LinqFasterer",
			"Microsoft.Toolkit.HighPerformance",
			"Pastel",
			"PriorityQueue",
			"MusicMaster",
			"Tomlyn",
			"ZstdNet"
		};

		foreach (var file in Directory.GetFiles(root)) {
			switch (Path.GetExtension(file).ToLowerInvariant()) {
				case ".dll":
				case ".so":
				case ".dylib":
				case ".pdb":
					break;
				default:
					continue;
			}

			var fileName = Path.GetFileNameWithoutExtension(file);

			if (!expected.Contains(fileName)) {
				Debug.Info($"Removing Outdated File: {Path.GetFileName(file)}");
				try {
					File.Delete(file);
				}
				catch {
					// swallow exceptions
				}
			}
		}
	}
}
