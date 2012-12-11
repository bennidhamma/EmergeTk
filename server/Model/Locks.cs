using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace EmergeTk.Model
{
	#region Locks utility class
	public static class Locks
	{
		public static void GetUpgradealeReadLock(ReaderWriterLockSlim locks)
		{
			locks.EnterUpgradeableReadLock ();
		}

		public static void GetReadOnlyLock(ReaderWriterLockSlim locks)
		{
			locks.EnterReadLock ();
		}

		public static void GetWriteLock(ReaderWriterLockSlim locks)
		{
			locks.EnterWriteLock ();
		}

		public static void ReleaseReadOnlyLock(ReaderWriterLockSlim locks)
		{
			if (locks.IsReadLockHeld)
				locks.ExitReadLock();
		}

		public static void ReleaseUpgradeableReadLock(ReaderWriterLockSlim locks)
		{
			if (locks.IsUpgradeableReadLockHeld)
				locks.ExitUpgradeableReadLock();
		}

		public static void ReleaseWriteLock(ReaderWriterLockSlim locks)
		{
			if (locks.IsWriteLockHeld)
				locks.ExitWriteLock();
		}

		public static void ReleaseLock(ReaderWriterLockSlim locks)
		{
			ReleaseWriteLock(locks);
			ReleaseUpgradeableReadLock(locks);
			ReleaseReadOnlyLock(locks);
		}

		public static ReaderWriterLockSlim GetLockInstance()
		{
			return GetLockInstance(LockRecursionPolicy.NoRecursion);
		}

		public static ReaderWriterLockSlim GetLockInstance(LockRecursionPolicy recursionPolicy)
		{
			return new ReaderWriterLockSlim(recursionPolicy);
		}
	}

	public abstract class BaseLock : IDisposable
	{
		protected ReaderWriterLockSlim _Locks;
		public BaseLock(ReaderWriterLockSlim locks)
		{
			_Locks = locks;
		}

		public abstract void Dispose();
	}

	public class ReadLock : BaseLock
	{
		public ReadLock(ReaderWriterLockSlim locks)
			: base(locks)
		{
			Locks.GetReadOnlyLock(this._Locks);
		}

		public override void Dispose()
		{
			Locks.ReleaseUpgradeableReadLock(this._Locks);
		}
	}

	public class ReadOnlyLock : BaseLock
	{
		public ReadOnlyLock(ReaderWriterLockSlim locks)
			: base(locks)
		{
			Locks.GetReadOnlyLock(this._Locks);
		}

		public override void Dispose()
		{
			Locks.ReleaseReadOnlyLock(this._Locks);
		}
	}

	public class WriteLock : BaseLock
	{
		public WriteLock(ReaderWriterLockSlim locks)
			: base(locks)
		{
			Locks.GetWriteLock(this._Locks);
		}

		public override void Dispose()
		{
			Locks.ReleaseWriteLock(this._Locks);
		}
	}
	#endregion
}
