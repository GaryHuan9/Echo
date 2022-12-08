using System;
using System.Collections.Concurrent;
using System.Threading;
using Echo.Core.Common.Diagnostics;
using Echo.Core.Common.Threading;

namespace Echo.Core.Common.Compute.Async;

class TaskContext
{
	public TaskContext(Action<uint> taskAction, uint repeatCount, uint workerCount)
	{
		Ensure.IsTrue(repeatCount > 0);
		Ensure.IsTrue(workerCount > 0);

		this.taskAction = taskAction;
		this.repeatCount = repeatCount;

		partitionCount = Math.Min(repeatCount, workerCount);
		partitionSize = (repeatCount - 1) / partitionCount + 1;
	}

	public readonly uint partitionCount;
	readonly Action<uint> taskAction;
	readonly uint repeatCount;
	readonly uint partitionSize;

	uint launchedCount;
	uint finishedCount;

	readonly Locker locker = new();
	Action continuationAction = emptyAction;

	static readonly Action emptyAction = () => { };

	public bool IsFinished => InterlockedHelper.Read(ref finishedCount) == partitionCount;

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

	public void Execute(ref Procedure procedure)
	{
		uint launched = Interlocked.Increment(ref launchedCount) - 1;
		Ensure.IsTrue(launched < partitionCount);

		uint start = partitionSize * launched;
		uint end = Math.Min(repeatCount, start + partitionSize);

		Ensure.IsTrue(start < end);
		procedure.Begin(end - start);

		for (uint i = start; i < end; i++)
		{
			taskAction(i);
			procedure.Advance();
		}

		uint finished = Interlocked.Increment(ref finishedCount) - 1;
		Ensure.IsTrue(finished < partitionCount);
		if (finished + 1 < partitionCount) return;

		using var _ = locker.Fetch();
		continuationAction?.Invoke();
	}
}