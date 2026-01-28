using System;

namespace SpriteMaster.Types;

internal abstract class SpriteMasterObject : IDisposable {
	private IPurgeable? Purgeable => this as IPurgeable;

	internal SpriteMasterObject() {
		if (Purgeable is {} purgeable) {
			MemoryMonitor.Manager.Register(purgeable);
		}
	}

	~SpriteMasterObject() {
		Debug.Trace($"{nameof(SpriteMasterObject)} ({this.GetType().FullName}) was not disposed before finalizer");
	}

	public virtual void Dispose() {
		GC.SuppressFinalize(this);

		if (Purgeable is { } purgeable) {
			MemoryMonitor.Manager.Unregister(purgeable);
		}
	}
}
