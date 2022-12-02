using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Compute.Async;

public sealed class AsyncOperation : Operation
{
	AsyncOperation(ImmutableArray<IWorker> workers, Func<AsyncOperation, ComputeTask> root) : base(workers, 1) { }

	readonly ConcurrentQueue<TaskContext> queue = new();
	readonly ManualResetEventSlim resetEvent = new();

	uint queueSize;

	public ComputeTask Schedule(Action action) => Schedule(_ => action(), 1);

	public ComputeTask Schedule(Action<uint> action, uint count)
	{
		var context = new TaskContext(action, count);
		uint size = Interlocked.Increment(ref queueSize);

		queue.Enqueue(context);

		if (size == 1) resetEvent.Set();
		return new ComputeTask(context);
	}

	// public ComputeTask<T> Schedule<T>(Func<T> action)
	// {
	// 	throw new NotImplementedException();
	// }

	public override bool Execute(IWorker worker)
	{
		TaskContext context = null;
		uint count = 0;
		uint start = 0;

		var spinner = new SpinWait();
		bool empty = queue.IsEmpty;

		while (!empty)
		{
			if (queue.TryPeek(out context))
			{
				//Try launch a partition of the task for this worker
				count = (context.totalCount - 1) / (uint)WorkerCount + 1;
				context.TryLaunch(ref count, out start);

				if (count > 0) break;
			}

			//Try again later
			spinner.SpinOnce();
			empty = queue.IsEmpty;
		}

		if (empty)
		{
			//TODO
		}

		if (context.totalCount == start + count)
		{
			//Just launched the last iteration of this task, remove it from queue
			bool success = queue.TryDequeue(out TaskContext dequeued);
			Ensure.IsTrue(success);
			Ensure.AreEqual(dequeued, context);

			uint size = Interlocked.Decrement(ref queueSize);
			if (size == 0) resetEvent.Reset();
		}

		//Execute the task
		ref WorkerData data = ref workerData[worker.Index];
		data.ThrowIfInconsistent(worker.Guid);

		data.procedure = new Procedure(default); //TODO procedure index
		context.Execute(start, count, ref data.procedure);

		return true;
	}

	protected override void Execute(ref Procedure procedure, IWorker worker) => throw new NotSupportedException();

	public static Factory New(Func<AsyncOperation, ComputeTask> action) => new(action);

	public readonly struct Factory : IOperationFactory
	{
		public Factory(Func<AsyncOperation, ComputeTask> root) => this.root = root;

		readonly Func<AsyncOperation, ComputeTask> root;

		public Operation CreateOperation(ImmutableArray<IWorker> workers) => new AsyncOperation(workers, root);
	}
}