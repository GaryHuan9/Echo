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

	public override bool Execute(IWorker worker)
	{
		//TODO procedure

		var spinner = new SpinWait();

		while (!queue.IsEmpty)
		{
			if (TaskContext.TryPeekExecute(queue))
			{
				return true;
			}

			spinner.SpinOnce();
		}

		worker.Pause();

		return false;
	}

	protected override void Execute(ref Procedure procedure, IWorker worker)
	{
		procedure.Begin(1);
	}

	public static Factory New(Func<AsyncOperation, ComputeTask> action) => new(action);

	public readonly struct Factory : IOperationFactory
	{
		public Factory(Func<AsyncOperation, ComputeTask> root) => this.root = root;

		readonly Func<AsyncOperation, ComputeTask> root;

		public Operation CreateOperation(ImmutableArray<IWorker> workers) => new AsyncOperation(workers, root);
	}
}