using System;
using System.Runtime.CompilerServices;
using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Compute.Async;

[AsyncMethodBuilder(typeof(ComputeTaskAsyncMethodBuilder))]
public readonly struct ComputeTask : ICriticalNotifyCompletion
{
	internal ComputeTask(TaskContext context) => this.context = context;

	readonly TaskContext context;

	public bool IsCompleted => context.IsFinished;

	public ComputeTask GetAwaiter() => this;

	public void GetResult()
	{
		context.ThrowIfExceptionOccured();
		Ensure.IsTrue(IsCompleted);
	}

	public void OnCompleted(Action continuation) => context.Register(continuation);
	public void UnsafeOnCompleted(Action continuation) => context.Register(continuation);
}

[AsyncMethodBuilder(typeof(ComputeTaskAsyncMethodBuilder<>))]
public readonly struct ComputeTask<T> : ICriticalNotifyCompletion
{
	internal ComputeTask(TaskContext<T> context) => this.context = context;

	readonly TaskContext<T> context;

	public bool IsCompleted => context.IsFinished;

	public ComputeTask<T> GetAwaiter() => this;

	public T GetResult()
	{
		context.ThrowIfExceptionOccured();
		Ensure.IsTrue(IsCompleted);
		return context.GetResult();
	}

	public void OnCompleted(Action continuation) => context.Register(continuation);
	public void UnsafeOnCompleted(Action continuation) => context.Register(continuation);
}