using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;

namespace Echo.Core.Common.Compute.Async;

public sealed class AsyncOperation : Operation
{
	AsyncOperation(ImmutableArray<IWorker> workers, Func<AsyncOperation, ComputeTask> root) : base(workers, 1) { }

	readonly ConcurrentQueue<TaskContext> queue;

	public ComputeTask Schedule(Action action) => Schedule(_ => action(), 1);

	public ComputeTask Schedule(Action<uint> action, uint count)
	{
		var context = new TaskContext(action, count);

		queue.Enqueue(context);

		return new ComputeTask(context);
	}

	// public ComputeTask<T> Schedule<T>(Func<T> action)
	// {
	// 	throw new NotImplementedException();
	// }

	protected override void Execute(ref Procedure procedure, IWorker worker)
	{
		procedure.Begin(1);

		var spinner = new SpinWait();

		while (!queue.IsEmpty)
		{
			if (TaskContext.TryPeekExecute(queue))
			{
				procedure.Advance();
				return;
			}

			spinner.SpinOnce();
		}
	}
}