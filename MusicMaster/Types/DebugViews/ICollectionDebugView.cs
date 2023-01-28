using JetBrains.Annotations;
using System.Collections.Generic;
using System.Diagnostics;

namespace MusicMaster.Types.DebugViews;

// https://github.com/dotnet/runtime/blob/4019e83878a81465f6e42e8502b53bc5d1752f81/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/ICollectionDebugView.cs
// ReSharper disable once InconsistentNaming
internal sealed class ICollectionDebugView<T> {
	private readonly ICollection<T> Collection;

	internal ICollectionDebugView(ICollection<T> collection) => Collection = collection;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	[UsedImplicitly]
	public T[] Items {
		get {
			var items = new T[Collection.Count];
			Collection.CopyTo(items, 0);
			return items;
		}
	}
}
