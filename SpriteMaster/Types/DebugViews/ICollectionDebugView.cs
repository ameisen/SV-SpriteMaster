using System.Collections.Generic;
using System.Diagnostics;

namespace SpriteMaster.Types.DebugViews;

// https://github.com/dotnet/runtime/blob/4019e83878a81465f6e42e8502b53bc5d1752f81/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/ICollectionDebugView.cs
sealed class ICollectionDebugView<T> {
	private readonly ICollection<T> Collection;

	internal ICollectionDebugView(ICollection<T> collection!!) => Collection = collection;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items {
		get {
			T[] items = new T[Collection.Count];
			Collection.CopyTo(items, 0);
			return items;
		}
	}
}
