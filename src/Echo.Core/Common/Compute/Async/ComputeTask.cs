using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Echo.Core.Common.Diagnostics;

namespace Echo.Core.Common.Compute.Async;

/// <summary>
/// A custom <see cref="Task"/> like struct used by the <see cref="AsyncOperation"/> for the asynchronous compute system.
/// </summary>
[AsyncMethodBuilder(typeof(ComputeTaskAsyncMethodBuilder))]
public readonly struct ComputeTask : ICriticalNotifyCompletion
{
	internal ComputeTask(TaskContext context) => this.context = context;

	readonly TaskContext context;

	public bool IsCompleted => context.IsFinished;

	/// <summary>
	/// Similar to <see cref="Task.CompletedTask"/>; gets a <see cref="CompletedTask"/> that is already completed.
	/// </summary>
	public static ComputeTask CompletedTask => new(TaskContext.completedContext);

	public ComputeTask GetAwaiter() => this;

	public void GetResult()
	{
		context.ThrowIfExceptionOccured();
		Ensure.IsTrue(IsCompleted);
	}

	void INotifyCompletion.OnCompleted(Action continuation) => context.Register(continuation);
	void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) => context.Register(continuation);

	/// <summary>
	/// Returns a new <see cref="ComputeTask{T}"/> that <see cref="ComputeTask{T}.IsCompleted"/> with a <paramref name="result"/>.
	/// </summary>
	/// <param name="result">The result of type <typeparamref name="T"/> to contain.</param>
	public static async ComputeTask<T> FromResult<T>(T result)
	{
		await CompletedTask;
		return result;
	}
}

/// <summary>
/// A custom <see cref="Task{T}"/> like struct used by the <see cref="AsyncOperation"/> for the asynchronous compute system.
/// </summary>
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

	void INotifyCompletion.OnCompleted(Action continuation) => context.Register(continuation);
	void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation) => context.Register(continuation);

	public static implicit operator ComputeTask(ComputeTask<T> task) => new(task.context);
}