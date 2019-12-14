using System;
using System.Threading;

namespace SpriteMaster
{
	internal class SharedLock : IDisposable
	{
		private ReaderWriterLock Lock = new ReaderWriterLock();

		internal struct Promoted : IDisposable
		{
			private ReaderWriterLock Lock;
			private LockCookie Cookie;

			internal Promoted(ReaderWriterLock sharedLock, LockCookie cookie)
			{
				this.Lock = sharedLock;
				this.Cookie = cookie;
			}

			public void Dispose()
			{
				Lock.DowngradeFromWriterLock(ref Cookie);
			}
		}

		internal SharedLock LockShared()
		{
			Lock.AcquireReaderLock(-1);
			return this;
		}

		internal SharedLock LockExclusive()
		{
			Lock.AcquireWriterLock(-1);
			return this;
		}

		internal Promoted Promote()
		{
			return new Promoted(Lock, Lock.UpgradeToWriterLock(-1));
		}

		public void Dispose()
		{
			if (Lock.IsWriterLockHeld)
			{
				Lock.ReleaseWriterLock();
			}
			else
			{
				Lock.ReleaseReaderLock();
			}
		}
	}
}
