using System;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Threading;

namespace SpriteMaster {
	[SecuritySafeCritical]
	internal sealed class SharedLock : CriticalFinalizerObject, IDisposable {
		private readonly ReaderWriterLock Lock = new ReaderWriterLock();

		internal struct Promoted : IDisposable {
			private readonly ReaderWriterLock Lock;
			private LockCookie Cookie;

			internal Promoted (ReaderWriterLock sharedLock, in LockCookie cookie) {
				this.Lock = sharedLock;
				this.Cookie = cookie;
			}

			public void Dispose () {
				Lock.DowngradeFromWriterLock(ref Cookie);
			}
		}

		internal SharedLock LockShared () {
			Lock.AcquireReaderLock(-1);
			return this;
		}

		internal SharedLock LockExclusive () {
			Lock.AcquireWriterLock(-1);
			return this;
		}

		internal Promoted Promote () {
			return new Promoted(Lock, Lock.UpgradeToWriterLock(-1));
		}

		public void Dispose () {
			if (Lock.IsWriterLockHeld) {
				Lock.ReleaseWriterLock();
			}
			else {
				Lock.ReleaseReaderLock();
			}
		}
	}
}
