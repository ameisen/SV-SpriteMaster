using System;
using System.Diagnostics.CodeAnalysis;

namespace SpriteMaster.Extensions;

internal static class WeakReferenceExt {
	internal static bool TryGet<T>(this WeakReference<T>? weakRef, [NotNullWhen(true)] out T? value) where T : class {
		if (weakRef?.TryGetTarget(out value) ?? false) {
			return true;
		}

		value = null;
		return false;
	}
}
