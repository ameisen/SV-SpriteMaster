using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using static SpriteMaster.Runtime;

namespace SpriteMaster.Extensions;

static class Collections {
	[MethodImpl(MethodImpl.Hot)]
	internal static V GetOrAddDefault<K, V>(this Dictionary<K, V> dictionary, K key, Func<V> defaultGetter) {
		if (dictionary.TryGetValue(key, out V value)) {
			return value;
		}
		var newValue = defaultGetter.Invoke();
		dictionary.Add(key, newValue);
		return newValue;
	}
}
