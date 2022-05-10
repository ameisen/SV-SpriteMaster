using System.Diagnostics;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable ConvertToAutoProperty

namespace SpriteMaster.Types.Spans;

internal sealed class PinnedSpanDebugView<T> where T : unmanaged {
	private readonly T[] _array;

	public PinnedSpanDebugView(PinnedSpan<T> span) {
		_array = span.ToArray();
	}

	public PinnedSpanDebugView(ReadOnlyPinnedSpan<T> span) {
		_array = span.ToArray();
	}

	public PinnedSpanDebugView(PinnedSpan<T>.FixedSpan span) : this(span.AsSpan) {
	}

	public PinnedSpanDebugView(ReadOnlyPinnedSpan<T>.FixedSpan span) : this(span.AsSpan) {
	}

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items => _array;
}
