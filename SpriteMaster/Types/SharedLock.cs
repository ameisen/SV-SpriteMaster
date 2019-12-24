using System;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Threading;

namespace SpriteMaster {
	[SecuritySafeCritical]
	internal sealed class SharedLock : CriticalFinalizerObject, IDisposable {
		private ReaderWriterLock Lock = new ReaderWriterLock();


		internal struct Promoted : IDisposable {
			private ReaderWriterLock Lock;
			private LockCookie Cookie;

			internal Promoted (ReaderWriterLock sharedLock) {
				this.Lock = sharedLock;
				this.Cookie = sharedLock.UpgradeToWriterLock(-1);
			}

			internal bool IsDisposed {
				get { return Lock == null; }
			}

			public void Dispose () {
				if (Lock == null) {
					return;
				}

				Lock.DowngradeFromWriterLock(ref Cookie);
				Lock = null;
			}
		}

		~SharedLock () {
			Dispose();
			Lock = null;
		}

		internal bool IsDisposed {
			get { return Lock == null; }
		}

		internal SharedLock Shared {
			get { return LockShared(); }
		}

		internal SharedLock Exclusive {
			get { return LockExclusive(); }
		}

		internal Promoted Promote {
			get { return LockPromote(); }
		}

		private SharedLock LockShared () {
			Lock.AcquireReaderLock(-1);
			return this;
		}

		private SharedLock LockExclusive () {
			Lock.AcquireWriterLock(-1);
			return this;
		}

		private Promoted LockPromote () {
			return new Promoted(Lock);
		}

		public void Dispose () {
			if (Lock == null) {
				return;
			}

			if (Lock.IsWriterLockHeld) {
				Lock.ReleaseWriterLock();
			}
			else {
				Lock.ReleaseReaderLock();
			}
		}
	}
}
