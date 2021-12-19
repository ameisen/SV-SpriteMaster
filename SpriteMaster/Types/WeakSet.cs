using System.Runtime.CompilerServices;
using System.Security;

namespace SpriteMaster.Types {
	internal class WeakSet<T> where T : class {
		private const object Sentinel = null;

		private readonly ConditionalWeakTable<T, object> InternalTable = new ConditionalWeakTable<T, object>();
		private readonly SharedLock Lock = new SharedLock();

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		[SecuritySafeCritical]
		internal bool Contains(T obj) {
			using (Lock.Shared) {
				return InternalTable.TryGetValue(obj, out var _);
			}
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		[SecuritySafeCritical]
		internal bool Remove(T obj) {
			using (Lock.Exclusive) {
				return InternalTable.Remove(obj);
			}
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		[SecuritySafeCritical]
		internal bool Add(T obj) {
			try {
				using (Lock.Exclusive) {
					if (InternalTable.TryGetValue(obj, out var _)) {
						return false;
					}

					InternalTable.Add(obj, Sentinel);
					return true;
				}
			}
			catch {
				return false;
			}
		}
	}
}
