using System;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace SpriteMaster;

using LockType = ReaderWriterLockSlim;

sealed class SharedLock : CriticalFinalizerObject, IDisposable {
	private LockType Lock;

	internal ref struct ReadCookie {
		private LockType Lock = null;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private ReadCookie(LockType rwlock) => Lock = rwlock;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal static ReadCookie Create(LockType rwlock) {
			rwlock.EnterReadLock();
			return new(rwlock);
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal static ReadCookie TryCreate(LockType rwlock) => rwlock.TryEnterReadLock(0) ? new(rwlock) : new();

		private readonly bool IsDisposed => Lock is null;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		public void Dispose() {
			if (IsDisposed) {
				return;
			}

			Lock.ExitReadLock();
			Lock = null;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		public static implicit operator bool(in ReadCookie cookie) => !cookie.IsDisposed;
	}
	internal ref struct WriteCookie {
		private LockType Lock = null;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private WriteCookie(LockType rwlock) => Lock = rwlock;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal static WriteCookie Create(LockType rwlock) {
			rwlock.EnterWriteLock();
			return new(rwlock);
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal static WriteCookie TryCreate(LockType rwlock) => rwlock.TryEnterWriteLock(0) ? new(rwlock) : new();

		private readonly bool IsDisposed => Lock is null;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		public void Dispose() {
			if (IsDisposed) {
				return;
			}

			Lock.ExitWriteLock();
			Lock = null;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		public static implicit operator bool(in WriteCookie cookie) => !cookie.IsDisposed;
	}

	internal ref struct PromotedCookie {
		private LockType Lock = null;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		private PromotedCookie(LockType rwlock) {
			Lock = rwlock;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal static PromotedCookie Create(LockType rwlock) {
			rwlock.EnterUpgradeableReadLock();
			return new(rwlock);
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		internal static PromotedCookie TryCreate(LockType rwlock) => rwlock.TryEnterUpgradeableReadLock(0) ? new(rwlock) : new();

		private readonly bool IsDisposed => Lock is null;

		[MethodImpl(Runtime.MethodImpl.Hot)]
		public void Dispose() {
			if (IsDisposed) {
				return;
			}

			Lock.ExitUpgradeableReadLock();
			Lock = null;
		}

		[MethodImpl(Runtime.MethodImpl.Hot)]
		public static implicit operator bool(in PromotedCookie cookie) => !cookie.IsDisposed;
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	internal SharedLock(LockRecursionPolicy recursionPolicy = LockRecursionPolicy.NoRecursion) {
		Lock = new(recursionPolicy);
	}

	[MethodImpl(Runtime.MethodImpl.Hot)]
	~SharedLock() => Dispose();

	internal bool IsLocked => IsReadLock || IsWriteLock || IsPromotedLock;
	internal bool IsReadLock => Lock.IsReadLockHeld;
	internal bool IsWriteLock => Lock.IsWriteLockHeld;
	internal bool IsPromotedLock => Lock.IsUpgradeableReadLockHeld;
	internal bool IsDisposed => Lock == null;

	internal ReadCookie Read => ReadCookie.Create(Lock);
	internal ReadCookie TryRead => ReadCookie.TryCreate(Lock);
	internal WriteCookie Write => WriteCookie.Create(Lock);
	internal WriteCookie TryWrite => WriteCookie.TryCreate(Lock);
	internal PromotedCookie Promote => PromotedCookie.Create(Lock);
	internal PromotedCookie TryPromote => PromotedCookie.TryCreate(Lock);

	[MethodImpl(Runtime.MethodImpl.Hot)]
	public void Dispose() {
		if (Lock is null) {
			return;
		}

		if (IsPromotedLock) {
			Lock.ExitUpgradeableReadLock();
		}
		if (IsWriteLock) {
			Lock.ExitWriteLock();
		}
		else if (IsReadLock) {
			Lock.ExitReadLock();
		}

		Lock = null;
	}
}
