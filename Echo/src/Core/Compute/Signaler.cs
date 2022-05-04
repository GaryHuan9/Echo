using System.Threading;

namespace Echo.Core.Compute;

sealed class Signaler
{
	readonly object locker = new();

	bool disabled;

	public bool Enabled
	{
		get
		{
			lock (locker) return !disabled;
		}
		set
		{
			lock (locker)
			{
				bool old = !disabled;
				disabled = !value;

				if (!old & disabled) Monitor.PulseAll(locker);
			}
		}
	}

	public void Wait()
	{
		lock (locker)
		{
			if (disabled) return;
			Monitor.Wait(locker);
		}
	}

	public void Signal()
	{
		lock (locker) Monitor.PulseAll(locker);
	}
}