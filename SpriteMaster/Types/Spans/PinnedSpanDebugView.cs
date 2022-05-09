using System.Diagnostics;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable ConvertToAutoProperty

namespace SpriteMaster.Types.Spans;

internal sealed class PinnedSpanDebugView<T> {
	private readonly T[] _array;

	public PinnedSpanDebugView(PinnedSpan<T> span) {
		_array = span.ToArray();
	}

	public PinnedSpanDebugView(ReadOnlyPinnedSpan<T> span) {
		_array = span.ToArray();
	}

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items => _array;
}
