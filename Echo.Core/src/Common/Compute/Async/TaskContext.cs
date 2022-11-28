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

	readonly Locker locker = new();
	readonly Action<uint> taskAction;
	readonly uint totalCount;

	Action continuationAction;

	uint launchedCount;
	uint finishedCount;

	public bool IsCompleted => InterlockedHelper.Read(ref finishedCount) == totalCount;

	public void Register(Action continuation)
	{
		using var _ = locker.Fetch();

		if (IsCompleted) continuation();
		else continuationAction = continuation;
	}

	bool Launch(out uint index)
	{
		index = Interlocked.Increment(ref launchedCount);
		return index <= totalCount;
	}

	void Execute(uint index)
	{
		taskAction(index - 1);

		if (Interlocked.Increment(ref finishedCount) == totalCount)
		{
			using var _ = locker.Fetch();
			continuationAction?.Invoke();
		}
	}

	public static bool TryPeekExecute(ConcurrentQueue<TaskContext> queue)
	{
		if (!queue.TryPeek(out TaskContext context)) return false;
		if (!context.Launch(out uint index)) return false;

		if (index == context.totalCount)
		{
			bool success = queue.TryDequeue(out TaskContext item);
			Ensure.IsTrue(success);
			Ensure.AreEqual(context, item);
		}

		context.Execute(index);
		return true;
	}
}