using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Compute.Async;

public sealed class AsyncOperation : Operation
{
	AsyncOperation(ImmutableArray<IWorker> workers, Func<AsyncOperation, ComputeTask> root) : base(workers, 0)
	{
		Schedule((Action)(() => rootTask = root(this)));
	}

	BlockingCollection<TaskContext> partitions = new(new ConcurrentBag<TaskContext>());

	ComputeTask rootTask;

	public ComputeTask Schedule(Action action) => Schedule(_ => action(), 1);

	public ComputeTask Schedule(Action<uint> action, uint count)
	{
		var context = new TaskContext(action, count, (uint)WorkerCount);
		Interlocked.Add(ref totalProcedure, context.partitionCount);
		for (int i = 0; i < context.partitionCount; i++) partitions.Add(context);

		return new ComputeTask(context);
	}

	public ComputeTask<T> Schedule<T>(Func<T> action)
	{
		var context = new TaskContext<T>(action);
		Ensure.AreEqual(context.partitionCount, 1u);
		Interlocked.Increment(ref totalProcedure);

		partitions.Add(context);
		return new ComputeTask<T>(context);
	}

	public override bool Execute(IWorker worker)
	{
		//Try get one task context partition
		if (!partitions.TryTake(out TaskContext partition))
		{
			//Block and wait for one to become available
			if (!worker.Await(partitions, out partition))
			{
				Ensure.IsTrue(partitions.IsCompleted);
				return false;
			}

			Ensure.IsNotNull(partition);
		}

		//Execute a partition of the task as a procedure
		uint index = Interlocked.Increment(ref nextProcedure) - 1;
		ref WorkerData data = ref workerData[worker.Index];

		data.ThrowIfInconsistent(worker.Guid);
		data.procedure = new Procedure(index);
		partition.Execute(ref data.procedure);

		//Update progress
		if (CompleteProcedure(ref data)) return true;

		//All procedures (task partitions) are done and there is no way for us to schedule more, then the entire operation is finished.
		//Note that if any task still has the potential of scheduling more, then we would not be here since totalProcedure would be higher.

		partitions.CompleteAdding();
		rootTask.GetResult();
		return false;
	}

	protected override void Execute(ref Procedure procedure, IWorker worker) => throw new NotSupportedException();

	public static Factory New(Func<AsyncOperation, ComputeTask> action) => new(action);

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (!disposing) return;
		partitions = null;
	}

	public readonly struct Factory : IOperationFactory
	{
		public Factory(Func<AsyncOperation, ComputeTask> root) => this.root = root;

		readonly Func<AsyncOperation, ComputeTask> root;

		public Operation CreateOperation(ImmutableArray<IWorker> workers) => new AsyncOperation(workers, root);
	}
}