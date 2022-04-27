using System;
using System.Diagnostics.CodeAnalysis;

namespace SpriteMaster.Extensions;

static class WeakReferenceExt {
	internal static bool TryGet<T>(this WeakReference<T>? weakRef, [NotNullWhen(true)] out T? value) where T : class {
		if ((weakRef?.TryGetTarget(out value) ?? false) && value is not null) {
			return true;
		}

		value = null;
		return false;
	}
}
