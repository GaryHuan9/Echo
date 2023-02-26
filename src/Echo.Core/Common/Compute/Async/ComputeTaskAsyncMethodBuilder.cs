using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Echo.Core.Common.Compute.Async;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public readonly struct ComputeTaskAsyncMethodBuilder
{
	public ComputeTaskAsyncMethodBuilder() { }

	readonly TaskContext context = new();

	public ComputeTask Task => new(context);

	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

	public void SetException(Exception exception) => context.SetException(exception);

	public void SetResult() => context.FinishOnce();

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
		where TAwaiter : INotifyCompletion
		where TStateMachine : IAsyncStateMachine
	{
		ThrowIfInvalidAwaiterType<TAwaiter>();
		awaiter.OnCompleted(stateMachine.MoveNext);
	}

	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
		where TAwaiter : ICriticalNotifyCompletion
		where TStateMachine : IAsyncStateMachine
	{
		ThrowIfInvalidAwaiterType<TAwaiter>();
		awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
	}

	[Obsolete($"As indicated by the {nameof(AsyncTaskMethodBuilder)} source, this is legacy.")]
	public void SetStateMachine(IAsyncStateMachine _) => throw new NotSupportedException();

	public static ComputeTaskAsyncMethodBuilder Create() => new();

	internal static void ThrowIfInvalidAwaiterType<TAwaiter>()
	{
		if (typeof(TAwaiter) == typeof(ComputeTask) || typeof(TAwaiter).GetGenericTypeDefinition() == typeof(ComputeTask<>)) return;
		throw new ArgumentException($"Cannot await a {nameof(TAwaiter)} of type {typeof(TAwaiter)} for a {nameof(ComputeTask)}.");
	}
}

[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public readonly struct ComputeTaskAsyncMethodBuilder<T>
{
	public ComputeTaskAsyncMethodBuilder() { }

	readonly TaskContext<T> context = new();

	public ComputeTask<T> Task => new(context);

	public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine => stateMachine.MoveNext();

	public void SetException(Exception exception) => context.SetException(exception);

	public void SetResult(T item) => context.FinishOnce(item);

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
		where TAwaiter : INotifyCompletion
		where TStateMachine : IAsyncStateMachine
	{
		ComputeTaskAsyncMethodBuilder.ThrowIfInvalidAwaiterType<TAwaiter>();
		awaiter.OnCompleted(stateMachine.MoveNext);
	}

	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
		where TAwaiter : ICriticalNotifyCompletion
		where TStateMachine : IAsyncStateMachine
	{
		ComputeTaskAsyncMethodBuilder.ThrowIfInvalidAwaiterType<TAwaiter>();
		awaiter.UnsafeOnCompleted(stateMachine.MoveNext);
	}

	[Obsolete($"As indicated by the {nameof(AsyncTaskMethodBuilder)} source, this is legacy.")]
	public void SetStateMachine(IAsyncStateMachine _) => throw new NotSupportedException();

	public static ComputeTaskAsyncMethodBuilder<T> Create() => new();
}