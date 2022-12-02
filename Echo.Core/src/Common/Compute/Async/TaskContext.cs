using System;
using System.Collections.Concurrent;
using System.Threading;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Threading;

namespace Echo.Core.Common.Compute.Async;

class TaskContext
{
	public TaskContext(Action<uint> action, uint count)
	{
		taskAction = action;
		totalCount = count;
	}

	public readonly uint totalCount;
	readonly Locker locker = new();
	readonly Action<uint> taskAction;

	Action continuationAction;

	uint launchedCount;
	uint finishedCount;

	public bool IsLaunched => InterlockedHelper.Read(ref launchedCount) >= totalCount;
	public bool IsFinished => InterlockedHelper.Read(ref finishedCount) == totalCount;

	static readonly Action emptyAction = () => { };

	public void Register(Action continuation)
	{
		using var _ = locker.Fetch();

		if (IsFinished)
		{
			continuation();
			continuationAction = emptyAction; //Ensure no double registration
		}
		else
		{
			Ensure.IsNull(continuationAction);
			continuationAction = continuation;
		}
	}

	public void TryLaunch(ref uint count, out uint start)
	{
		Ensure.IsTrue(count > 0);

		if (IsLaunched)
		{
			count = 0;
			start = default;
			return;
		}

		start = Interlocked.Add(ref launchedCount, count) - count;
		count = start < totalCount ? Math.Min(count, totalCount - start) : 0;
	}

	public void Execute(uint start, uint count, ref Procedure procedure)
	{
		Ensure.IsTrue(count > 0);
		Ensure.IsTrue(start + count <= totalCount);

		procedure.Begin(count);

		for (uint i = start; i < start + count; i++)
		{
			taskAction(i);
			procedure.Advance();
		}

		uint finished = Interlocked.Add(ref finishedCount, count);
		Ensure.IsTrue(finished <= totalCount);
		if (finished < totalCount) return;

		using var _ = locker.Fetch();
		continuationAction?.Invoke();
	}
}