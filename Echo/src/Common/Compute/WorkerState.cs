﻿using System;
using System.Numerics;
using CodeHelpers.Diagnostics;

namespace Echo.Common.Compute;

/// <summary>
/// Different states of a <see cref="Worker"/>.
/// </summary>
/// <remarks>Almost always, the value of enum should be one-hot (eg. <see cref="Idle"/> and <see cref="Pausing"/>
/// should not be present at the same time). The <see cref="FlagsAttribute"/> is only there for the convenience of
/// some implementation details in <see cref="Worker.AwaitStatus"/>.</remarks>
[Flags]
public enum WorkerState : uint
{
	/// <summary>
	/// The <see cref="Worker"/> is has no assigned <see cref="Operation"/>.
	/// </summary>
	Idle = 1 << 0,

	/// <summary>
	/// The <see cref="Worker"/> is currently executing an <see cref="Operation"/>.
	/// </summary>
	Running = 1 << 1,

	/// <summary>
	/// The <see cref="Worker"/> is executing an <see cref="Operation"/> but will pause at its earliest convenience.
	/// </summary>
	Pausing = 1 << 2,

	/// <summary>
	/// The <see cref="Worker"/> is executing an <see cref="Operation"/> but will abort as soon as possible.
	/// </summary>
	Aborting = 1 << 3,

	/// <summary>
	/// The <see cref="Worker"/> is paused and is not utilizing any computational resources.
	/// </summary>
	Awaiting = 1 << 4,

	/// <summary>
	/// The <see cref="Worker"/> is disposed and should no longer be used.
	/// </summary>
	Disposed = 1 << 5
}

public static class WorkerStateExtensions
{
	static readonly string[] workerStateLabels = Enum.GetNames<WorkerState>();

	/// <summary>
	/// Converts a <see cref="WorkerState"/> to be displayed.
	/// </summary>
	/// <param name="state">The <see cref="WorkerState"/> to be converted.</param>
	/// <returns>A display <see cref="string"/> representing the <see cref="WorkerState"/>.</returns>
	/// <remarks>The <paramref name="state"/> must have only one bit enabled, and it must be one of the named
	/// values of <see cref="WorkerState"/>, otherwise the behavior of this method is undefined!</remarks>
	public static string ToDisplayString(this WorkerState state)
	{
		uint integer = (uint)state;
		Assert.AreEqual(BitOperations.PopCount(integer), 1);
		int index = BitOperations.LeadingZeroCount(integer);
		return workerStateLabels[31 - index];
	}
}