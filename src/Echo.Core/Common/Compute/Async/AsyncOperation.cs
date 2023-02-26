using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading;
using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Compute.Async;

/// <summary>
/// An asynchronous <see cref="Operation"/> that integrates with the compute system.
/// </summary>
/// <remarks>
/// Using <see cref="ComputeTask"/>, the <see cref="AsyncOperation"/> works with the
/// C# async and await syntax for convenient divergent asynchronous programming.
/// </remarks>
public abstract class AsyncOperation : Operation
{
	protected AsyncOperation(ImmutableArray<IWorker> workers) : base(workers, 0)
	{
		Schedule((Action)(() => rootTask = Execute()));
	}

	BlockingCollection<TaskContext> partitions = new(new ConcurrentBag<TaskContext>());

	ComputeTask rootTask;

	/// <summary>
	/// Schedules an <see cref="Action"/> to be executed on the <see cref="Device"/>.
	/// </summary>
	/// <returns>A <see cref="ComputeTask"/> that will only complete once the scheduled item finishes entirely.</returns>
	public ComputeTask Schedule(Action action) => Schedule(_ => action(), 1);

	/// <summary>
	/// Schedules an <see cref="Action"/> to be executed many times on the <see cref="Device"/>.
	/// </summary>
	/// <returns>A <see cref="ComputeTask"/> that will only complete once the scheduled item finishes entirely.</returns>
	public ComputeTask Schedule(Action<uint> action, uint count)
	{
		var context = new TaskContext(action, count, (uint)WorkerCount);

		IncreaseTotalProcedure(context.partitionCount);
		for (int i = 0; i < context.partitionCount; i++) partitions.Add(context);

		return new ComputeTask(context);
	}

	/// <summary>
	/// Schedules an <see cref="Func{TResult}"/> to be executed on the <see cref="Device"/>.
	/// </summary>
	/// <returns>A <see cref="ComputeTask{T}"/> that will only complete once the scheduled item finishes entirely.</returns>
	public ComputeTask<T> Schedule<T>(Func<T> action)
	{
		var context = new TaskContext<T>(action);
		Ensure.AreEqual(context.partitionCount, 1u);

		IncreaseTotalProcedure(1);
		partitions.Add(context);

		return new ComputeTask<T>(context);
	}

	public sealed override bool Execute(IWorker worker)
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

	/// <summary>
	/// This is the entry of the <see cref="AsyncOperation"/>.
	/// </summary>
	/// <remarks>This will be invoked once on an <see cref="IWorker"/> thread, and it should call the different <see cref="Schedule(Action)"/> methods
	/// to subsequently distribute work for the entire <see cref="Device"/>. Note that async <see cref="ComputeTask"/> methods can only await on other
	/// <see cref="ComputeTask"/> objects, anything else is not allowed.</remarks>
	protected abstract ComputeTask Execute();

	protected sealed override void Execute(ref Procedure procedure, IWorker worker) => throw new NotSupportedException();

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (!disposing) return;
		partitions = null;
	}

	/// <summary>
	/// Creates a new <see cref="IOperationFactory"/> for an <see cref="AsyncOperation"/> from a delegate.
	/// </summary>
	public static FactoryFromDelegate New(Func<AsyncOperation, ComputeTask> action) => new(action);

	/// <summary>
	/// An <see cref="IOperationFactory"/> implementation for <see cref="New"/>.
	/// </summary>
	public readonly struct FactoryFromDelegate : IOperationFactory
	{
		public FactoryFromDelegate(Func<AsyncOperation, ComputeTask> root) => this.root = root;

		readonly Func<AsyncOperation, ComputeTask> root;

		public Operation CreateOperation(ImmutableArray<IWorker> workers) => new Impl(workers, root);

		sealed class Impl : AsyncOperation
		{
			public Impl(ImmutableArray<IWorker> workers, Func<AsyncOperation, ComputeTask> root) : base(workers) => this.root = root;

			readonly Func<AsyncOperation, ComputeTask> root;

			protected override ComputeTask Execute() => root(this);
		}
	}
}