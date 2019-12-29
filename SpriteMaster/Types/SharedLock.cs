using System;
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
			internal SharedCookie (ReaderWriterLock rwlock) {
				this.Lock = rwlock;
				this.Lock.AcquireReaderLock(-1);
			}

			internal bool IsDisposed {
				[SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
				get { return Lock == null; }
			}

			[SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			public void Dispose () {
				if (Lock == null) {
					return;
				}

				Contract.Assert(Lock.IsReaderLockHeld && !Lock.IsWriterLockHeld);
				if (Lock.IsReaderLockHeld) {
					Lock.ReleaseReaderLock();
				}
				Lock = null;
			}
		}
		internal struct ExclusiveCookie : IDisposable {
			private ReaderWriterLock Lock;

			[SecuritySafeCritical]
			internal ExclusiveCookie (ReaderWriterLock rwlock) {
				this.Lock = rwlock;
				this.Lock.AcquireWriterLock(-1);
			}

			internal bool IsDisposed {
				[SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
				get { return Lock == null; }
			}

			[SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			public void Dispose () {
				if (Lock == null) {
					return;
				}

				Contract.Assert(!Lock.IsReaderLockHeld && Lock.IsWriterLockHeld);
				if (Lock.IsWriterLockHeld) {
					Lock.ReleaseWriterLock();
				}
				Lock = null;
			}
		}

		internal struct PromotedCookie : IDisposable {
			private ReaderWriterLock Lock;
			private LockCookie Cookie;

			[SecuritySafeCritical]
			internal PromotedCookie (ReaderWriterLock rwlock) {
				this.Lock = rwlock;
				this.Cookie = this.Lock.UpgradeToWriterLock(-1);
			}

			internal bool IsDisposed {
				[SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
				get { return Lock == null; }
			}

			[SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			public void Dispose () {
				if (Lock == null) {
					return;
				}

				Contract.AssertTrue(Lock.IsWriterLockHeld);
				if (Lock.IsWriterLockHeld) {
					Lock.DowngradeFromWriterLock(ref Cookie);
				}
				Lock = null;
			}
		}

		~SharedLock () {
			Dispose();
			Lock = null;
		}

		internal bool IsLocked {
			[SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get { return Lock.IsReaderLockHeld || Lock.IsWriterLockHeld; }
		}

		internal bool IsSharedLock {
			[SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get { return Lock.IsReaderLockHeld; }
		}

		internal bool IsExclusiveLock {
			[SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			get { return Lock.IsWriterLockHeld; }
		}

		internal bool IsDisposed {
			get { return Lock == null; }
		}

		internal SharedCookie Shared {
			[SecuritySafeCritical]
			get {
				Contract.Assert(!IsLocked);
				return new SharedCookie(Lock);
			}
		}

		internal ExclusiveCookie Exclusive {
			[SecuritySafeCritical]
			get {
				Contract.Assert(!IsLocked);
				return new ExclusiveCookie(Lock);
			}
		}

		internal PromotedCookie Promote {
			[SecuritySafeCritical]
			get {
				Contract.Assert(!IsExclusiveLock && IsSharedLock);
				return new PromotedCookie(Lock);
			}
		}

		[SecuritySafeCritical, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
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
