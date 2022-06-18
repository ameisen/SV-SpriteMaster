using System.Diagnostics.Contracts;

namespace SpriteMaster.Types;

internal interface IByteSize {
	[Pure]
	long SizeBytes { get; }
}
