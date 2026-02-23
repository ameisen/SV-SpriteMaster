using System.IO;

namespace SpriteMaster.Extensions;

internal static class MemoryStreamExt {
	internal static byte[] GetArray(this MemoryStream stream) {
		var buffer = stream.GetBuffer();
		return buffer.Length == stream.Length ? buffer : stream.ToArray();
	}
}
