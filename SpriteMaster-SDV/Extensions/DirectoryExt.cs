namespace SpriteMaster.Extensions;

internal static class DirectoryExt {
	internal static bool CompressDirectory(string path, bool force = false) {
		if (Runtime.IsWindows) {
			return DirectoryExtWindows.CompressDirectory(path, force);
		}

		return false;
	}
}
