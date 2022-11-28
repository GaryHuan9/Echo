using System;
using System.Runtime.CompilerServices;

namespace Echo.Core.Common.Compute.Async;

public class ComputeTaskAsyncMethodBuilder
{
	public ComputeTaskAsyncMethodBuilder() => Console.WriteLine(".ctor");

	public static ComputeTaskAsyncMethodBuilder Create() => new();

	public void Start<TStateMachine>(ref TStateMachine stateMachine)
		where TStateMachine : IAsyncStateMachine
	{
		Console.WriteLine("Start");
		stateMachine.MoveNext();
	}

	public void SetStateMachine(IAsyncStateMachine stateMachine) { }

	public void SetException(Exception exception) => throw new NotImplementedException();

	public void SetResult() => Console.WriteLine("SetResult");

	public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
		where TAwaiter : INotifyCompletion
		where TStateMachine : IAsyncStateMachine { }

	public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
		where TAwaiter : ICriticalNotifyCompletion
		where TStateMachine : IAsyncStateMachine { }

	public ComputeTask Task { get; }
}