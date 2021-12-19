using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Threading;

namespace SpriteMaster {
	[SecuritySafeCritical]
	internal sealed class SharedLock : CriticalFinalizerObject, IDisposable {
		private ReaderWriterLock Lock = new ReaderWriterLock();

		internal struct SharedCookie : IDisposable {
			private ReaderWriterLock Lock;

			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			internal SharedCookie (ReaderWriterLock rwlock, int timeout) {
				Lock = null;
				rwlock.AcquireReaderLock(timeout);
				Lock = rwlock;
			}

			internal bool IsDisposed {
				[SecuritySafeCritical]
				[MethodImpl(Runtime.MethodImpl.Optimize)]
				get { return Lock == null; }
			}

			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			public void Dispose () {
				if (Lock == null) {
					return;
				}

				Lock.ReleaseReaderLock();

				Lock = null;
			}
		}
		internal struct ExclusiveCookie : IDisposable {
			private ReaderWriterLock Lock;

			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			internal ExclusiveCookie (ReaderWriterLock rwlock, int timeout) {
				Lock = null;
				rwlock.AcquireWriterLock(timeout);
				Lock = rwlock;
			}

			internal bool IsDisposed {
				[SecuritySafeCritical]
				[MethodImpl(Runtime.MethodImpl.Optimize)]
				get { return Lock == null; }
			}

			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			public void Dispose () {
				if (Lock == null) {
					return;
				}

				Lock.ReleaseWriterLock();

				Lock = null;
			}
		}

		internal struct PromotedCookie : IDisposable {
			private ReaderWriterLock Lock;
			private LockCookie Cookie;

			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			internal PromotedCookie (ReaderWriterLock rwlock, int timeout) {
				Lock = null;
				this.Cookie = rwlock.UpgradeToWriterLock(timeout);
				Lock = rwlock;
			}

			internal bool IsDisposed {
				[SecuritySafeCritical]
				[MethodImpl(Runtime.MethodImpl.Optimize)]
				get { return Lock == null; }
			}

			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			public void Dispose () {
				if (Lock == null) {
					return;
				}

				Lock.DowngradeFromWriterLock(ref Cookie);

				Lock = null;
			}
		}

		[MethodImpl(Runtime.MethodImpl.Optimize)]
		~SharedLock () {
			Dispose();
			Lock = null;
		}

		internal bool IsLocked {
			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get { return Lock.IsReaderLockHeld || Lock.IsWriterLockHeld; }
		}

		internal bool IsSharedLock {
			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get { return Lock.IsReaderLockHeld; }
		}

		internal bool IsExclusiveLock {
			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get { return Lock.IsWriterLockHeld; }
		}

		internal bool IsDisposed {
			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get { return Lock == null; }
		}

		internal SharedCookie Shared {
			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get {
				return new SharedCookie(Lock, -1);
			}
		}

		internal SharedCookie? TryShared {
			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get {
				try {
					return new SharedCookie(Lock, 0);
				}
				catch {
					return null;
				}
			}
		}

		internal ExclusiveCookie Exclusive {
			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get {
				return new ExclusiveCookie(Lock, -1);
			}
		}

		internal ExclusiveCookie? TryExclusive {
			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get {
				try {
					return new ExclusiveCookie(Lock, 0);
				}
				catch {
					return null;
				}
			}
		}

		internal PromotedCookie Promote {
			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get {
				//Contract.Assert(!IsExclusiveLock && IsSharedLock);
				return new PromotedCookie(Lock, -1);
			}
		}

		internal PromotedCookie? TryPromote {
			[SecuritySafeCritical]
			[MethodImpl(Runtime.MethodImpl.Optimize)]
			get {
				//Contract.Assert(!IsExclusiveLock && IsSharedLock);
				try {
					return new PromotedCookie(Lock, 0);
				}
				catch {
					return null;
				}
			}
		}

		[SecuritySafeCritical]
		[MethodImpl(Runtime.MethodImpl.Optimize)]
		public void Dispose () {
			if (Lock == null) {
				return;
			}

			if (Lock.IsWriterLockHeld) {
				Lock.ReleaseWriterLock();
			}
			else if (Lock.IsReaderLockHeld) {
				Lock.ReleaseReaderLock();
			}
		}
	}
}
