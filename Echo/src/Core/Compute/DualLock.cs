using System;
using System.Threading;
using CodeHelpers;

namespace Echo.Core.Compute;

/// <summary>
/// A <see cref="ReaderWriterLockSlim"/> created with <see cref="LockRecursionPolicy.SupportsRecursion"/>
/// and allows for fetching locks with using statements that are much simpler to use and create less errors.
/// </summary>
sealed class DualLock : ReaderWriterLockSlim
{
	public DualLock() : base(LockRecursionPolicy.SupportsRecursion) { }

	/// <summary>
	/// Enters the writer lock and fetches an <see cref="IDisposable"/> object that can be used to exit the writer lock.
	/// </summary>
	public ExitHandle FetchWriteLock()
	{
		EnterWriteLock();
		return ExitHandle.CreateWrite(this);
	}

	/// <summary>
	/// Enters the reader lock and fetches an <see cref="IDisposable"/> object that can be used to exit the reader lock.
	/// </summary>
	public ExitHandle FetchReadLock()
	{
		EnterReadLock();
		return ExitHandle.CreateRead(this);
	}

	/// <summary>
	/// Enters the upgradeable reader lock and fetches an <see cref="IDisposable"/> object that can be used to exit the upgradeable reader lock.
	/// </summary>
	public ExitHandle FetchUpgradeableReadLock()
	{
		EnterUpgradeableReadLock();
		return ExitHandle.CreateUpgradeableRead(this);
	}

	public struct ExitHandle : IDisposable
	{
		ExitHandle(DualLock locker, Mode mode)
		{
			this.locker = locker;
			this.mode = mode;
			disposed = 0;
		}

		readonly DualLock locker;
		readonly Mode mode;

		int disposed;

		public void Dispose()
		{
			if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0) return;

			switch (mode)
			{
				case Mode.Write:
				{
					locker.ExitWriteLock();
					break;
				}
				case Mode.Read:
				{
					locker.ExitReadLock();
					break;
				}
				case Mode.UpgradeableRead:
				{
					locker.ExitUpgradeableReadLock();
					break;
				}
				default: throw ExceptionHelper.Invalid(nameof(mode), mode, InvalidType.unexpected);
			}
		}

		public static ExitHandle CreateWrite(DualLock locker) => new(locker, Mode.Write);
		public static ExitHandle CreateRead(DualLock locker) => new(locker, Mode.Read);
		public static ExitHandle CreateUpgradeableRead(DualLock locker) => new(locker, Mode.UpgradeableRead);
	}

	enum Mode : byte
	{
		Write,
		Read,
		UpgradeableRead
	}
}