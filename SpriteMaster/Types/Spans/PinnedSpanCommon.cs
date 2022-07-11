using SpriteMaster.Experimental;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SpriteMaster.Types.Spans;

internal static class PinnedSpanCommon {
	[Conditional("DEBUG"), Conditional("DEVELOPMENT"), Conditional("RELEASE"), MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void CheckPinnedWeak(object obj) {
		//var header = obj.GetHeader();

		//GC.GetGeneration(obj).AssertEqual(2);
	}
}
