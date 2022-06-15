using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using CodeHelpers.Collections;
using CodeHelpers.Diagnostics;
using Echo.Core.Common;

namespace Echo.UserInterface.Core;

public static class ActionQueue
{
	static readonly BlockingCollection<Labeled<Action>> queue = new();
	static readonly HashSet<string> labels = new();
	static readonly Locker locker = new();

	static bool hasThread;

	public static void Enqueue(Action action, string label)
	{
		using var _ = locker.Fetch();

		if (!labels.Add(label))
		{
			LogList.Add($"Attempting to enqueue duplicate '{label}' into {nameof(ActionQueue)}.");
			return;
		}

		LogList.Add($"Successfully enqueued '{label}' into {nameof(ActionQueue)}.");
		queue.Add(new Labeled<Action>(label, action));

		if (hasThread) return;

		var thread = new Thread(ProcessQueue) { IsBackground = true };
		hasThread = true;
		thread.Start();
	}

	static void ProcessQueue()
	{
		foreach ((string label, Action action) in queue.GetConsumingEnumerable())
		{
			LogList.Add($"Dequeued and began executing '{label}' from {nameof(ActionQueue)}.");

			action();

			LogList.Add($"Completed executing '{label}' from {nameof(ActionQueue)}.");

			// ReSharper disable once RedundantAssignment
			bool removed = labels.Remove(label);
			Assert.IsTrue(removed);
		}
	}

	readonly record struct Labeled<T>(string Label, in T Item);
}