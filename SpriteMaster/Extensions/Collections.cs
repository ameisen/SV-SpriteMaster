using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpriteMaster.Extensions {
	internal static class Collections {
		internal static V GetOrAddDefault<K, V>(this Dictionary<K, V> dictionary, K key, Func<V> defaultGetter) {
			if (dictionary.TryGetValue(key, out V value)) {
				return value;
			}
			var newValue = defaultGetter.Invoke();
			dictionary.Add(key, newValue);
			return newValue;
		}
	}
}
