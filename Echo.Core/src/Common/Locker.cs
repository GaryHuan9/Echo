using System;
using System.Threading;

namespace Echo.Core.Common;

/// <summary>
/// A simple mutually exclusive lock with enhanced features.
/// </summary>
/// <remarks>The 'lock' keyword can also be used on instances of <see cref="Locker"/> in conjunction with the instance methods.</remarks>
public sealed class Locker
{
	bool _signaling = true;

	/// <summary>
	/// Whether the <see cref="Signal"/> method is signaling.
	/// </summary>
	/// <remarks>If this property is assigned to false, all threads
	/// blocked by <see cref="Wait"/> will be released.</remarks>
	public bool Signaling
	{
		get
		{
			using var _ = Fetch();
			return _signaling;
		}
		set
		{
			using var _ = Fetch();
			bool old = _signaling;
			_signaling = value;

			if (old & !value) Monitor.PulseAll(this);
		}
	}

	/// <summary>
	/// Enters the lock which can be exited by invoking <see cref="IDisposable.Dispose"/> on the returned <see cref="ReleaseHandle"/>.
	/// </summary>
	/// <remarks>This method should be used directly with the C# 'using' statements.</remarks>
	public ReleaseHandle Fetch()
	{
		Monitor.Enter(this);
		return new ReleaseHandle(this);
	}

	/// <summary>
	/// If <see cref="Signaling"/> is true, blocks the calling thread until <see cref="Signal"/> is invoked.
	/// </summary>
	public void Wait()
	{
		using var _ = Fetch();
		if (!_signaling) return;
		Monitor.Wait(this);
	}

	/// <summary>
	/// Releases all threads blocked by <see cref="Wait"/>.
	/// </summary>
	public void Signal()
	{
		using var _ = Fetch();
		Monitor.PulseAll(this);
	}

	public readonly struct ReleaseHandle : IDisposable
	{
		public ReleaseHandle(Locker locker) => this.locker = locker;

		readonly Locker locker;

		void IDisposable.Dispose() => Monitor.Exit(locker);
	}
}