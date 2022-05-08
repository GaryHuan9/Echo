using System;
using System.Threading;

namespace Echo.Common;

public sealed class Locker
{
	bool signaling = true;

	public bool Signaling
	{
		get
		{
			using var _ = Fetch();
			return signaling;
		}
		set
		{
			using var _ = Fetch();
			bool old = signaling;
			signaling = value;

			if (old & !value) Monitor.PulseAll(this);
		}
	}

	public ReleaseHandle Fetch()
	{
		Monitor.Enter(this);
		return new ReleaseHandle(this);
	}

	public void Wait()
	{
		using var _ = Fetch();
		if (!signaling) return;
		Monitor.Wait(this);
	}

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