using System;
using System.Runtime.CompilerServices;

namespace Echo.Core.Common.Compute.Async;

[AsyncMethodBuilder(typeof(ComputeTaskAsyncMethodBuilder))]
public readonly struct ComputeTask : ICriticalNotifyCompletion
{
	internal ComputeTask(TaskContext context = null) => this.context = context;

	readonly TaskContext context;

	public bool IsCompleted => context?.IsCompleted != false;

	public ComputeTask GetAwaiter() => this;

	public void GetResult() { }

	public void OnCompleted(Action continuation) => context.Register(continuation);
	public void UnsafeOnCompleted(Action continuation) => context.Register(continuation);
}