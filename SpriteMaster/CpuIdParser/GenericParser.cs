using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SpriteMaster.CpuIdParser;
internal sealed class GenericParser {
	[MethodImpl(Runtime.MethodImpl.RunOnce)]
	[SuppressMessage("ReSharper", "StringLiteralTypo")]
	internal static SystemInfo.ArchitectureResult Parse(in SystemInfo.CpuId id) {
		// I don't know what to do in this situation
		return new(
			"Unknown",
			new() { Avx2 = false, Avx512 = false }
		);
	}
}
